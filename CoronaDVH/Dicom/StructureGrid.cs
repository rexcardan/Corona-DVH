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
    public class VarianStructureGrid : OrientedGrid3f
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
           
        }
    }
}
