using CoronaDVH.Geometry;
using CoronaDVH.GMath;
using CoronaDVH.Helpers;
using CoronaDVH.Models;

namespace CoronaDVH.Dicom
{
    /// <summary>
    /// A grid representation of a structure where the values of the grid are a truncated signed distance function represented by the Herman Chamfer
    /// algorithm. The values are positive inside the structure and negative outside. The closer to the boundary the value is near 0. An absolute value of 5 or less
    /// should be considered a boundary voxel
    /// Herman's chamfer algorithm as described in Shape-based interpolation:1992. Herman et al. http://ieeexplore.ieee.org/document/135915/
    /// </summary>
    public class VarianStructureGrid : OrientedVolumeGrid<float>
    {
        public VarianStructureGrid(RTStructure str, OrientedGrid3f imageGrid) : base(imageGrid.Dimensions, -99)
        {
            CellSize = imageGrid.CellSize;
            Orientation = imageGrid.Orientation;
            Outside = -99;

            //Find bounds of structure
            var strBounds = new AxisAlignedBox3f() { Min = new Vector3f(float.MaxValue), Max = new Vector3f(float.MinValue) };
            foreach (var ctr in str.Contours)
            {
                strBounds.Min.Z = (float)Math.Min(ctr.Key, strBounds.Min.Z);
                strBounds.Max.Z = (float)Math.Max(ctr.Key, strBounds.Max.Z);
                foreach (var polyList in str.Contours.Values)
                {
                    foreach (var poly in polyList)
                    {
                        var bounds = poly.Bounds;
                        strBounds.Min.X = (float)Math.Min(bounds.Min.X, strBounds.Min.X);
                        strBounds.Max.X = (float)Math.Max(bounds.Max.X, strBounds.Max.X);
                        strBounds.Min.Y = (float)Math.Min(bounds.Min.Y, strBounds.Min.Y);
                        strBounds.Max.Y = (float)Math.Max(bounds.Max.Y, strBounds.Max.Y);
                    }
                }
            }
            StructureBounds = strBounds;

            Parallel.For(0, Dimensions.Z, (Action<int>)((z) =>
            {
                var zPt = base.GridToWorld(new Vector3i(0, 0, z));
                var minZ = zPt.Z - CellSize.Z / 2;
                var maxZ = zPt.Z + CellSize.Z / 2;

                foreach (var contourList in str.Contours)
                {
                    if (contourList.Key >= minZ && contourList.Key < maxZ)
                    {
                        foreach (var ctr in contourList.Value)
                        {
                            //Transform contour to image coordinates
                            var contourPts = ctr
                                .Select(pt => imageGrid.WorldToGrid(new Vector3f(pt.X, pt.Y, contourList.Key)))
                                .Select(pt => new Vector2d(pt.X, pt.Y))
                                .ToList();

                            var imageContour = new PolyLine2d(contourPts);
                            PolyFill.FillSlice(this, imageContour, z, 99);
                            var noParams = new VolFillParams<float>()
                            {
                                InitialInsideValue = 99,
                                InitialOutsideValue = -99,
                                InsideBorderValue = 5,
                                OutsideBorderValue = -5,
                                IsInside = (v) => v > 0,
                                IsOutside = (v) => v < 0,
                            };

                            HermanChamfer.Initialize(this, z, noParams);
                            HermanChamfer.Chamfer3x3(this, z, noParams, ChamferTemplates.NEAR_OPTIMAL_PASS1);
                            HermanChamfer.Chamfer3x3(this, z, noParams, ChamferTemplates.NEAR_OPTIMAL_PASS2, reverseOrder: true);
                        }
                    }
                }
            }));
        }
        public AxisAlignedBox3f StructureBounds { get; set; }
        public double CellVolume => CellSize.X * CellSize.Y * CellSize.Z;

        public DvhData AggregateDvh(OrientedGrid3f dvhDose, int borderVoxelSplit = 2)
        {
            // Ensure dose grid and structure grid are in the same coordinate space.
            if (dvhDose.Orientation.Origin != this.Orientation.Origin
               || dvhDose.Orientation.Rotation != this.Orientation.Rotation
               || dvhDose.CellSize != this.CellSize)
            {
                throw new ArgumentException("Dose grid has to be in same coordinates as structure grid");
            }

            var indices = this.Indices().ToList();
            var dvhData = new DvhData();

            // Compute the volume of a full voxel and the volume of each subdivided subvoxel.
            var voxVolume = (float)CellVolume;
            var subVoxVolume = (float)(voxVolume / Math.Pow(borderVoxelSplit, 3));

            // Use Parallel.ForEach with thread-local storage to avoid contention when adding to dvhData.Points.
            Parallel.ForEach(
                indices,
                () => new List<DvhPoint>(),  // Initialize a thread-local list.
                (idx, loopState, localPoints) =>
                {
                    var pt = this.GridToWorld(idx);
                    var insideOutside = this[idx];

                    if (insideOutside > 5) // Voxel is entirely inside.
                    {
                        var dose = dvhDose.ValueAt(pt);
                        localPoints.Add(new DvhPoint(dose, voxVolume));
                    }
                    else if (insideOutside >= -5 && insideOutside <= 5) // Voxel is on the boundary.
                    {
                        // Determine subvoxel dimensions.
                        var subXSize = CellSize.X / borderVoxelSplit;
                        var subYSize = CellSize.Y / borderVoxelSplit;
                        var subZSize = CellSize.Z / borderVoxelSplit;

                        // For each subvoxel, compute its center.
                        // We assume 'pt' is the center of the voxel, so the voxel spans from -CellSize/2 to +CellSize/2.
                        for (int i = 0; i < borderVoxelSplit; i++)
                        {
                            // Offset in the X direction.
                            var offsetX = -CellSize.X / 2 + subXSize / 2 + i * subXSize;
                            for (int j = 0; j < borderVoxelSplit; j++)
                            {
                                // Offset in the Y direction.
                                var offsetY = -CellSize.Y / 2 + subYSize / 2 + j * subYSize;
                                for (int k = 0; k < borderVoxelSplit; k++)
                                {
                                    // Offset in the Z direction.
                                    var offsetZ = -CellSize.Z / 2 + subZSize / 2 + k * subZSize;
                                    var subVoxelCenter = new Vector3f(pt.X + offsetX, pt.Y + offsetY, pt.Z + offsetZ);

                                    var subDose = dvhDose.ValueAt(subVoxelCenter);
                                    if (subDose > 0)
                                    {
                                        localPoints.Add(new DvhPoint(subDose, subVoxVolume));
                                    }
                                }
                            }
                        }
                    }
                    return localPoints;
                },
                localPoints =>
                {
                    // Merge thread-local lists into the global collection.
                    lock (dvhData.Points)
                    {
                        dvhData.Points.AddRange(localPoints);
                    }
                }
            );

            // Calculate DVH summary statistics.
            dvhData.MaxDose = dvhData.Points.Max(p => p.Dose);
            dvhData.MeanDose = dvhData.Points.Sum(p => p.Dose * p.Volume) / dvhData.Points.Sum(p => p.Volume);
            dvhData.Volume = dvhData.Points.Sum(p => p.Volume);
            dvhData.MinDose = dvhData.Points.Min(p => p.Dose);

            return dvhData;
        }
    }
}
