using CoronaDVH.Geometry;
using CoronaDVH.GMath;
using static CoronaDVH.Helpers.SearchDirection;

namespace CoronaDVH.Helpers
{
    /// <summary>
    /// Herman's chamfer algorithm as described in Shape-based interpolation:1992. Herman et al. http://ieeexplore.ieee.org/document/135915/
    /// </summary>
    public class HermanChamfer
    {
        /// <summary>
        /// Initializes a grid based on Figure 7 in reference
        /// </summary>
        /// <param name="binaryGrid">an input grid of two values (inside and outside)</param>
        /// <param name="sliceZ">the current slice</param>
        /// <param name="ip">a set of parameters which defines the possible values in the grid</param>
        public static void Initialize<T>(OrientedGridBase<T> binaryGrid, int sliceZ, VolFillParams<T> ip)
        {
            var allSearch = new SearchDirection[] { UP, DOWN, LEFT, RIGHT };


            for (int y = 1; y < binaryGrid.Dimensions.Y - 1; ++y)
            {
                //Boundary Free checks
                for (int x = 1; x < binaryGrid.Dimensions.X - 1; ++x)
                {
                    if (binaryGrid[x, y, sliceZ].Equals(ip.InitialInsideValue))
                    {
                        binaryGrid.SearchNeigborsToSet(new Vector3i(x, y, sliceZ), allSearch, ip.IsOutside, ip.InsideBorderValue);
                    }
                    else if (binaryGrid[x, y, sliceZ].Equals(ip.InitialOutsideValue))
                    {
                        binaryGrid.SearchNeigborsToSet(new Vector3i(x, y, sliceZ), allSearch, ip.IsInside, ip.OutsideBorderValue);
                    }
                }

                //L-BORDER
                if (binaryGrid[0, y, sliceZ].Equals(ip.InitialInsideValue))
                {
                    binaryGrid[0, y, sliceZ] = ip.InsideBorderValue;
                }
                else if (binaryGrid[0, y, sliceZ].Equals(ip.InitialOutsideValue))
                {
                    binaryGrid.SearchNeigborsToSet(new Vector3i(0, y, sliceZ), new SearchDirection[] { RIGHT, UP, DOWN }, ip.IsInside, ip.OutsideBorderValue);
                }

                //R-BORDER
                if (binaryGrid[binaryGrid.Dimensions.X - 1, y, sliceZ].Equals(ip.InitialInsideValue))
                {
                    binaryGrid[binaryGrid.Dimensions.X - 1, y, sliceZ] = ip.InsideBorderValue;
                }
                else if (binaryGrid[binaryGrid.Dimensions.X - 1, y, sliceZ].Equals(ip.InitialOutsideValue))
                {
                    binaryGrid.SearchNeigborsToSet(new Vector3i(binaryGrid.Dimensions.X - 1, y, sliceZ), new SearchDirection[] { LEFT, UP, DOWN }, ip.IsInside, ip.OutsideBorderValue);
                }
            }
            //TOP Border
            for (int x = 1; x < binaryGrid.Dimensions.X - 1; ++x)
            {
                if (binaryGrid[x, 0, sliceZ].Equals(ip.InitialInsideValue))
                {
                    binaryGrid[x, 0, sliceZ] = ip.InsideBorderValue;
                }
                else if (binaryGrid[x, 0, sliceZ].Equals(ip.InitialOutsideValue))
                {
                    binaryGrid.SearchNeigborsToSet(new Vector3i(x, 0, sliceZ), new SearchDirection[] { DOWN, LEFT, RIGHT }, ip.IsInside, ip.OutsideBorderValue);
                }
            }

            //BOTTOM Border
            for (int x = 1; x < binaryGrid.Dimensions.X - 1; ++x)
            {
                if (binaryGrid[x, binaryGrid.Dimensions.Y - 1, sliceZ].Equals(ip.InitialInsideValue))
                {
                    binaryGrid[x, binaryGrid.Dimensions.Y - 1, sliceZ] = ip.InsideBorderValue;
                }
                else if (binaryGrid[x, binaryGrid.Dimensions.Y - 1, sliceZ].Equals(ip.InitialOutsideValue))
                {
                    binaryGrid.SearchNeigborsToSet(new Vector3i(x, binaryGrid.Dimensions.Y - 1, sliceZ), new SearchDirection[] { UP, LEFT, RIGHT }, ip.IsInside, ip.OutsideBorderValue);
                }
            }
        }

        /// <summary>
        /// Performs a chamfering process described in http://ieeexplore.ieee.org/document/135915/
        /// </summary>
        /// <param name="initialGrid">an initialized Herman Chamfer grid</param>
        /// <param name="sliceZ">the current slice</param>
        /// <param name="ip">a set of parameters which defines the possible values in the grid</param>
        /// <param name="template">3x3 chamfer template to apply - use float.NaN to represent empty cells</param>
        public static void Chamfer3x3<T>(OrientedGrid3f initialGrid, int sliceZ, VolFillParams<T> ip, float[] template, bool reverseOrder = false)
        {
            var loopOp = new Action<int, int>((x, y) =>
            {
                var block = initialGrid.GetFloatBlock(x, y, sliceZ, 3);

                if (block[4].Equals(ip.InitialInsideValue))
                {
                    var sums = new float[template.Length];
                    for (int i = 0; i < template.Length; ++i)
                    {
                        if (float.IsNaN(template[i])) { sums[i] = float.MaxValue; }
                        else { sums[i] = block[i] + template[i]; }
                    }
                    initialGrid[x, y, sliceZ] = sums.Min();
                }
                else if (block[4].Equals(ip.InitialOutsideValue))
                {
                    var differences = new float[template.Length];
                    for (int i = 0; i < template.Length; ++i)
                    {
                        if (float.IsNaN(template[i])) { differences[i] = float.MinValue; }
                        else { differences[i] = block[i] - template[i]; }
                    }
                    initialGrid[x, y, sliceZ] = differences.Max();
                }
            });

            if (reverseOrder)
            {
                for (int y = initialGrid.Dimensions.Y - 1; y >= 0; --y)
                {
                    for (int x = initialGrid.Dimensions.X - 1; x >= 0; --x)
                    {
                        loopOp(x, y);
                    }
                }
            }
            else
            {
                for (int y = 0; y < initialGrid.Dimensions.Y; ++y)
                {
                    for (int x = 0; x < initialGrid.Dimensions.X; ++x)
                    {
                        loopOp(x, y);
                    }
                }
            }
        }
    }
}
