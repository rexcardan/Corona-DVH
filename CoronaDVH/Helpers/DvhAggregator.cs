using CoronaDVH.Dicom;
using CoronaDVH.Geometry;
using CoronaDVH.GMath;
using CoronaDVH.Models;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<CoronaDVH.GMath.Vector2l>>;

namespace CoronaDVH.Helpers
{
    public class DvhAggregator
    {
        public static DvhData ComputeDvh(OrientedGrid3f dvhDose, OrientedGrid3f ctGrid, RTStructure str, float binWidth = 0.1f)
        {
            Dictionary<int, float> doseHistogram = new Dictionary<int, float>();

            double totalVolume = 0f;
            double doseVolumeSum = 0f;
            double maxDose = float.MinValue;
            double minDose = float.MaxValue;

            // CT grid parameters.
            float ctSliceThickness = ctGrid.CellSize.Z;
            float ctOriginZ = ctGrid.Orientation.Origin.Z;

            Vector3f dVoxelSize = dvhDose.CellSize;
            double dVoxelVolume = dvhDose.CellVolume;

            for (int dz = 0; dz < dvhDose.Dimensions.Z; dz++)
            {
                // Get the real-world lower corner of the dose slice.
                Vector3f dSliceReal = dvhDose.GridToWorld(new Vector3i(0, 0, dz));
                float voxelZMin = dSliceReal.Z;
                // Correct: add the voxel's Z size instead of doubling dSliceReal.Z.
                float voxelZMax = dSliceReal.Z + dVoxelSize.Z;

                // Get contours from the structure that intersect the voxel's Z range.
                var sliceContoursGrouped = str.GetContoursInRange(voxelZMin, voxelZMax);
                if (sliceContoursGrouped.Count == 0)
                    continue; // No contours in this range

                // Process each dose voxel in the X-Y plane.
                for (int dy = 0; dy < dvhDose.Dimensions.Y; dy++)
                {
                    for (int dx = 0; dx < dvhDose.Dimensions.X; dx++)
                    {
                        double voxelIntersectionVolume = 0.0;
                        Vector3f dosePtReal = dvhDose.GridToWorld(new Vector3i(dx, dy, dz));
                        float voxelXMin = dosePtReal.X;
                        float voxelXMax = dosePtReal.X + dVoxelSize.X;
                        float voxelYMin = dosePtReal.Y;
                        float voxelYMax = dosePtReal.Y + dVoxelSize.Y;
                        double voxelVolume = dVoxelVolume;

                        // Create the 2D rectangle (in CT grid space) for the voxel's projection.
                        Vector3f voxelCorner1 = new Vector3f(voxelXMin, voxelYMin, voxelZMin);
                        Vector3f voxelCorner2 = new Vector3f(voxelXMax, voxelYMax, voxelZMax);
                        Vector3f ctCorner1 = ctGrid.WorldToGrid(voxelCorner1);
                        Vector3f ctCorner2 = ctGrid.WorldToGrid(voxelCorner2);
                        Vector2d rectMin = new Vector2d(Math.Min(ctCorner1.X, ctCorner2.X), Math.Min(ctCorner1.Y, ctCorner2.Y));
                        Vector2d rectMax = new Vector2d(Math.Max(ctCorner1.X, ctCorner2.X), Math.Max(ctCorner1.Y, ctCorner2.Y));
                        var voxelRect = PolyHelper.CreateRectangle(rectMin, rectMax);

                        // Loop over each CT slice group (each group representing contours from one CT slice)
                        foreach (var sliceGroup in sliceContoursGrouped)
                        {
                            // Assume each group provides a slice's Z coordinate (e.g., sliceGroup.SliceZ)
                            double sliceZ = sliceGroup.SliceZ;
                            // Compute the CT slice's Z bounds.
                            double sliceZMin = sliceZ - ctSliceThickness / 2f;
                            double sliceZMax = sliceZ + ctSliceThickness / 2f;
                            // Compute overlap between dose voxel Z range and this CT slice.
                            double overlapZ = Math.Max(0, Math.Min(voxelZMax, sliceZMax) - Math.Max(voxelZMin, sliceZMin));
                            if (overlapZ <= 0)
                                continue;

                            // Process each contour in this CT slice.
                            foreach (var contour in sliceGroup.Contours)
                            {
                                var intersectionPoly = PolyHelper.IntersectPolygons(voxelRect, contour);
                                double intersectionArea = PolyHelper.ComputeArea(intersectionPoly);
                                voxelIntersectionVolume += intersectionArea * overlapZ;
                            }
                        }

                        // Compute the fraction of the voxel inside the structure.
                        float fractionInside = (float)(voxelIntersectionVolume / voxelVolume);
                        if (fractionInside <= 0)
                            continue; // Skip voxel if no part is inside

                        // Get dose value.
                        float doseValue = dvhDose[dx, dy, dz];
                        double voxelInsideVolume = dVoxelVolume * fractionInside;

                        // Update summary statistics.
                        totalVolume += voxelInsideVolume;
                        doseVolumeSum += doseValue * voxelInsideVolume;
                        maxDose = Math.Max(maxDose, doseValue);
                        minDose = Math.Min(minDose, doseValue);

                        // Bin the dose value.
                        int binIndex = (int)(doseValue / binWidth);
                        if (!doseHistogram.ContainsKey(binIndex))
                            doseHistogram[binIndex] = 0;
                        doseHistogram[binIndex] += (float)voxelInsideVolume;
                    }
                }
            }

            // Build DVH data.
            DvhData dvhData = new DvhData
            {
                Volume = totalVolume,
                MaxDose = maxDose,
                MinDose = minDose,
                MeanDose = (totalVolume > 0) ? (doseVolumeSum / totalVolume) : 0
            };

            foreach (var kvp in doseHistogram.OrderBy(kvp => kvp.Key))
            {
                float binDose = kvp.Key * binWidth;
                float volume = kvp.Value;
                dvhData.Points.Add(new DvhPoint(binDose, volume));
            }

            return dvhData;
        }

    }
}
