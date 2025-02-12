﻿using FellowOakDicom.Imaging.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoronaDVH.GMath
{
    public struct Line2d
    {
        public Vector2d Origin;
        public Vector2d Direction;

        public Line2d(Vector2d origin, Vector2d direction)
        {
            this.Origin = origin;
            this.Direction = direction;
        }

        public Line2d(ref Vector2d origin, ref Vector2d direction)
        {
            this.Origin = origin;
            this.Direction = direction;
        }

        public static Line2d FromPoints(Vector2d p0, Vector2d p1)
        {
            return new Line2d(p0, (p1 - p0).Normalized);
        }
        public static Line2d FromPoints(ref Vector2d p0, ref Vector2d p1)
        {
            return new Line2d(p0, (p1 - p0).Normalized);
        }

        // parameter is distance along Line
        public Vector2d PointAt(double d)
        {
            return Origin + d * Direction;
        }

        public double Project(Vector2d p)
        {
            return (p - Origin).Dot(Direction);
        }

        public double DistanceSquared(Vector2d p)
        {
            double t = (p - Origin).Dot(Direction);
            Vector2d proj = Origin + t * Direction;
            return (proj - p).LengthSquared;
        }



        /// <summary>
        /// Returns:
        ///   +1, on right of line
        ///   -1, on left of line
        ///    0, on the line
        /// </summary>
        public int WhichSide(Vector2d test, double tol = 0)
        {
            double x0 = test.X - Origin.X;
            double y0 = test.Y - Origin.Y;
            double x1 = Direction.X;
            double y1 = Direction.Y;
            double det = x0 * y1 - x1 * y0;
            return (det > tol ? +1 : (det < -tol ? -1 : 0));
        }
        public int WhichSide(ref Vector2d test, double tol = 0)
        {
            double x0 = test.X - Origin.X;
            double y0 = test.Y - Origin.Y;
            double x1 = Direction.X;
            double y1 = Direction.Y;
            double det = x0 * y1 - x1 * y0;
            return (det > tol ? +1 : (det < -tol ? -1 : 0));
        }



        /// <summary>
        /// Calculate intersection point between this line and another one.
        /// Returns Vector2d.MaxValue if lines are parallel.
        /// </summary>
        /// <returns></returns>
        public Vector2d IntersectionPoint(ref Line2d other, double dotThresh = MathUtil.ZeroTolerance)
        {
            // see IntrLine2Line2 for explanation of algorithm
            Vector2d diff = other.Origin - Origin;
            double D0DotPerpD1 = Direction.DotPerp(other.Direction);
            if (Math.Abs(D0DotPerpD1) > dotThresh)
            {                    // Lines intersect in a single point.
                double invD0DotPerpD1 = ((double)1) / D0DotPerpD1;
                double diffDotPerpD1 = diff.DotPerp(other.Direction);
                double s = diffDotPerpD1 * invD0DotPerpD1;
                return Origin + s * Direction;
            }
            // Lines are parallel.
            return Vector2d.MaxValue;
        }
    }
}
