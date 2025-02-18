using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoronaDVH.GMath
{
    public struct Vector2l : IComparable<Vector2l>, IEquatable<Vector2l>
    {
        public long X;
        public long Y;

        public Vector2l(Vector2l v) { X = v.X; Y = v.Y; }
        public Vector2l(long f) { X = Y = f; }
        public Vector2l(long x, long y) { this.X = x; this.Y = y; }
        public Vector2l(long[] v2) { X = v2[0]; Y = v2[1]; }

        static public readonly Vector2l Zero = new Vector2l(0, 0);
        static public readonly Vector2l One = new Vector2l(1, 1);
        static public readonly Vector2l AxisX = new Vector2l(1, 0);
        static public readonly Vector2l AxisY = new Vector2l(0, 1);

        public long this[long key]
        {
            get { return (key == 0) ? X : Y; }
            set { if (key == 0) X = value; else Y = value; }
        }

        public long[] array
        {
            get { return new long[] { X, Y }; }
        }

        public void Add(long s) { X += s; Y += s; }




        public static Vector2l operator -(Vector2l v)
        {
            return new Vector2l(-v.X, -v.Y);
        }

        public static Vector2l operator *(long f, Vector2l v)
        {
            return new Vector2l(f * v.X, f * v.Y);
        }
        public static Vector2l operator *(Vector2l v, long f)
        {
            return new Vector2l(f * v.X, f * v.Y);
        }
        public static Vector2l operator /(Vector2l v, long f)
        {
            return new Vector2l(v.X / f, v.Y / f);
        }
        public static Vector2l operator /(long f, Vector2l v)
        {
            return new Vector2l(f / v.X, f / v.Y);
        }

        public static Vector2l operator *(Vector2l a, Vector2l b)
        {
            return new Vector2l(a.X * b.X, a.Y * b.Y);
        }
        public static Vector2l operator /(Vector2l a, Vector2l b)
        {
            return new Vector2l(a.X / b.X, a.Y / b.Y);
        }


        public static Vector2l operator +(Vector2l v0, Vector2l v1)
        {
            return new Vector2l(v0.X + v1.X, v0.Y + v1.Y);
        }
        public static Vector2l operator +(Vector2l v0, long f)
        {
            return new Vector2l(v0.X + f, v0.Y + f);
        }

        public static Vector2l operator -(Vector2l v0, Vector2l v1)
        {
            return new Vector2l(v0.X - v1.X, v0.Y - v1.Y);
        }
        public static Vector2l operator -(Vector2l v0, long f)
        {
            return new Vector2l(v0.X - f, v0.Y - f);
        }



        public static bool operator ==(Vector2l a, Vector2l b)
        {
            return (a.X == b.X && a.Y == b.Y);
        }
        public static bool operator !=(Vector2l a, Vector2l b)
        {
            return (a.X != b.X || a.Y != b.Y);
        }
        public override bool Equals(object obj)
        {
            return this == (Vector2l)obj;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)2166136261;
                // Suitable nullity checks etc, of course :)
                hash = (hash * 16777619) ^ X.GetHashCode();
                hash = (hash * 16777619) ^ Y.GetHashCode();
                return hash;
            }

        }
        public int CompareTo(Vector2l other)
        {
            if (X != other.X)
                return X < other.X ? -1 : 1;
            else if (Y != other.Y)
                return Y < other.Y ? -1 : 1;
            return 0;
        }
        public bool Equals(Vector2l other)
        {
            return (X == other.X && Y == other.Y);
        }



        public override string ToString()
        {
            return string.Format("{0} {1}", X, Y);
        }
    }
}
