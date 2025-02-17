using CoronaDVH.GMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CoronaDVH.Geometry
{
    public class OrientedGrid3f : OrientedGridBase<float>
    {
        public OrientedGrid3f() : base()
        {
        }

        public OrientedGrid3f(DenseGrid3f copy)
        {
            Buffer = new float[copy.Buffer.Length];
            Array.Copy(copy.Buffer, Buffer, Buffer.Length);
            Dimensions = copy.Dimensions;
        }

        public OrientedGrid3f(OrientedGrid3f copy) : base(copy)
        {
        }

        public OrientedGrid3f(Vector3i dimensions, float initialValue) : this(dimensions.X, dimensions.Y, dimensions.Z, initialValue)
        {
        }

        public OrientedGrid3f(int ni, int nj, int nk, float initialValue) : base(ni, nj, nk, initialValue)
        {
        }

        public new OrientedGrid3f EmptyClone()
        {
            var clone = new OrientedGrid3f(Dimensions.X, Dimensions.Y, Dimensions.Z, default)
            {
                Orientation = new Frame3f(Orientation),
                CellSize = new Vector3f(CellSize)
            };
            return clone;
        }

        public OrientedGrid3f ResampleOn(OrientedGrid3f toGrid)
        {
            var resampled = toGrid.EmptyClone();
            var indices = resampled.Indices().ToList();
            Parallel.ForEach(indices, idx =>
            {
                var pt = toGrid.GridToWorld(idx);
                resampled[idx] = this.ValueAt(pt);
            });
            return resampled;
        }

        public override float FloatValueAt(int x, int y, int z)
        {
            return this[x, y, z];
        }

        /// <summary>
        /// Returns a NxN block of voxels centered at the input position (used in chamfering)
        /// </summary>
        /// <param name="cx">center x</param>
        /// <param name="cy">center y</param>
        /// <param name="cz">center z</param>
        /// <param name="size">the size N of the block</param>
        /// <returns>an NxN block of voxels centered at the input position</returns>
        public float[] GetFloatBlock(int cx, int cy, int cz, int size)
        {
            var block = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var sampleX = cx - 1 + x;
                    var sampleY = cy - 1 + y;
                    if (sampleX < 0 || sampleX > Dimensions.X - 1 || sampleY < 0 || sampleY > Dimensions.Y - 1)
                    {
                        block[IndexUtil.ToGrid2Linear(x, y, size)] = (float)(object)this.Outside;
                    }
                    else
                    {
                        block[IndexUtil.ToGrid2Linear(x, y, size)] = FloatValueAt(sampleX, sampleY, cz);
                    }
                }
            }
            return block;
        }
    }
}
