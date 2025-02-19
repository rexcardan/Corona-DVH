using CoronaDVH.Geometry;
using CoronaDVH.GMath;

namespace CoronaDVH.Dicom
{
    public class StructureMask : OrientedGrid3f
    {
        // Create a fractional mask by oversampling the dose grid.
        public static StructureMask FromContours(RTStructure str, OrientedGrid3f dose, int oversampleFactor = 2)
        {
            // Determine grid geometry from dose volume
            var baseSpacing = dose.CellSize;
            var highResSpacing = baseSpacing / oversampleFactor;
            // Determine grid extents covering the structure (could use dose extents or structure bounds)
            // For simplicity, use dose grid extents:
            var origin = dose.Orientation.Origin;
            int nx = dose.Dimensions[0] * oversampleFactor;
            int ny = dose.Dimensions[1] * oversampleFactor;
            int nz = dose.Dimensions[2] * oversampleFactor;

            var highResMask = new OrientedGrid3f(nx, ny, nz, 0);

            // Rasterize the contours into the high-resolution grid:
            // For each slice of the high-res grid, determine which pixels lie inside the contour polygon of that slice.
            // For each voxel center, use IsInsideSurface to check if it's inside the 3D closed surface.
            for (int iz = 0; iz < nz; ++iz)
            {
                var doseZReal = dose.GridToWorld(new Vector3i(0, 0, iz)).Z;
                var doseZRealNext = doseZReal + dose.CellSize.Z;

                var sliceContours = str.Contours.Where(k => k.Key >= doseZReal && k.Key < doseZRealNext);
                if (!sliceContours.Any()) { continue; } //Skip slice

                var best = sliceContours.OrderBy(k => Math.Abs(k.Key - doseZReal)).FirstOrDefault();
                
                for (int ix = 0; ix < nx; ++ix)
                {
                    for (int iy = 0; iy < ny; ++iy)
                    {

                        // Compute world coordinates of voxel center:
                        Vector3f worldPoint = origin
                            + new Vector3f((ix + 0.5) * highResSpacing.X,
                                          (iy + 0.5) * highResSpacing.Y,
                                          (iz + 0.5) * highResSpacing.Z);

                        if (IsInsideSurface(worldPoint))
                        {
                            highResMask[ix, iy, iz] = 1.0f;
                        }
                        else
                        {
                            highResMask[ix, iy, iz] = 0.0f;
                        }
                    }
                }
            }

            // Downsample highResMask to fractional mask at original dose resolution
            float[,,] fracMask = new float[dose.Dimensions[0], dose.Dimensions[1], dose.Dimensions[2]];
            for (int i = 0; i < dose.Dimensions[0]; ++i)
            {
                for (int j = 0; j < dose.Dimensions[1]; ++j)
                {
                    for (int k = 0; k < dose.Dimensions[2]; ++k)
                    {
                        // Average the oversampled voxels corresponding to this low-res voxel
                        int startX = i * oversampleFactor;
                        int startY = j * oversampleFactor;
                        int startZ = k * oversampleFactor;
                        int countInside = 0;
                        int total = oversampleFactor * oversampleFactor * oversampleFactor;
                        for (int ii = 0; ii < oversampleFactor; ++ii)
                        {
                            for (int jj = 0; jj < oversampleFactor; ++jj)
                            {
                                for (int kk = 0; kk < oversampleFactor; ++kk)
                                {
                                    if (highResMask[startX + ii, startY + jj, startZ + kk] > 0.5f)
                                    {
                                        countInside++;
                                    }
                                }
                            }
                        }
                        fracMask[i, j, k] = (float)countInside / total; // fractional occupancy 0.0–1.0
                    }
                }
            }

            // Return the structure mask aligned to dose grid
            return new StructureMask { Fraction = fracMask, Origin = dose.Origin, Spacing = dose.Spacing, Dimensions = dose.Dimensions };
        }
    }
}

