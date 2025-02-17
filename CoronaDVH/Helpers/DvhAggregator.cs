using CoronaDVH.Dicom;
using CoronaDVH.Geometry;
using CoronaDVH.GMath;
using CoronaDVH.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CoronaDVH.Helpers
{
    public class DvhAggregator
    {
        public DvhData Aggregate(OrientedGrid3f dvhDose, OrientedGrid3f ctGrid, RTStructure str)
        {
            var sdf = new VarianStructureGrid(str, ct);
            var sdfRsmpled = sdf.ResampleOn(dose);

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
