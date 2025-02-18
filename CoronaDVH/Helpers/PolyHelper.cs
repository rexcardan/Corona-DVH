using ClipperLib;
using CoronaDVH.Dicom;
using CoronaDVH.GMath;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<CoronaDVH.GMath.Vector2l>>;

namespace CoronaDVH.Helpers
{
    public class PolyHelper
    {
        internal const double FloatToIntScale = 1000.0; // parameter to scale significant digits

        public static Paths CreateRectangle(Vector2d rectMin, Vector2d rectMax)
        {
            var rectMaxX = (long)Math.Round(rectMax.X * PolyHelper.FloatToIntScale);
            var rectMaxY = (long)Math.Round(rectMax.Y * PolyHelper.FloatToIntScale);
            var rectMinX = (long)Math.Round(rectMin.X * PolyHelper.FloatToIntScale);
            var rectMinY = (long)Math.Round(rectMin.Y * PolyHelper.FloatToIntScale);

            // Create the rectangle as a list of integer points.
            List<Vector2l> intPts = new List<Vector2l>
            {
                new Vector2l(rectMinX, rectMinY),
                new Vector2l(rectMaxX, rectMinY),
                new Vector2l(rectMaxX, rectMaxY),
                new Vector2l(rectMinX, rectMaxY)
            };

            // Wrap the rectangle in a Paths object (a List<List<Vector2l>>).
            Paths rectPaths = new Paths();
            rectPaths.Add(intPts);
            return rectPaths;
        }

        public static Paths ClipperSubtract(RTStructureContour outer, List<RTStructureContour> relevantHoles)
        {
            // Create a new Clipper instance for Boolean operations.
            Clipper clipper = new Clipper();

            // Convert the outer contour to integer coordinates.
            List<Vector2l> outerPath = new List<Vector2l>();
            foreach (var pt in outer.Contour)
            {
                outerPath.Add(new Vector2l(
                    (long)Math.Round(pt.X * PolyHelper.FloatToIntScale),
                    (long)Math.Round(pt.Y * PolyHelper.FloatToIntScale)));
            }
            // Add the outer polygon as the subject (closed polygon).
            clipper.AddPath(outerPath, PolyType.ptSubject, true);

            // Add each relevant hole as a clip path.
            foreach (var hole in relevantHoles)
            {
                List<Vector2l> holePath = new List<Vector2l>();
                foreach (var pt in hole.Contour)
                {
                    holePath.Add(new Vector2l(
                        (long)Math.Round(pt.X * PolyHelper.FloatToIntScale),
                        (long)Math.Round(pt.Y * PolyHelper.FloatToIntScale)));
                }
                clipper.AddPath(holePath, PolyType.ptClip, true);
            }

            // Prepare the container for the result.
            Paths solution = new Paths();
            // Execute the difference operation using NonZero fill type.
            bool succeeded = clipper.Execute(ClipType.ctDifference, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
            if (!succeeded)
            {
                // Optionally, return an empty Paths instead of null.
                return new Paths();
            }
            return solution;
        }

        public static double ComputeArea(Paths intersectionPoly)
        {
            double totalArea = 0.0;
            // Loop over each polygon in the Paths object.
            foreach (var path in intersectionPoly)
            {
                int count = path.Count;
                if (count < 3)
                    continue; // Not a valid polygon.

                double area = 0.0;
                // Use the shoelace formula.
                for (int i = 0; i < count; i++)
                {
                    Vector2l current = path[i];
                    Vector2l next = path[(i + 1) % count];
                    area += (current.X * next.Y - next.X * current.Y);
                }
                totalArea += Math.Abs(area) / 2.0;
            }

            // Convert the area back to the original coordinate system.
            double scaleSquared = PolyHelper.FloatToIntScale * PolyHelper.FloatToIntScale;
            return totalArea / scaleSquared;
        }

        public static Paths IntersectPolygons(Paths voxelRect, Paths structurePath)
        {
            // Create a new Clipper instance for performing Boolean operations.
            Clipper clipper = new Clipper();

            // Add the voxel rectangle as the subject.
            // The 'true' parameter indicates that these are closed paths.
            clipper.AddPaths(voxelRect, PolyType.ptSubject, true);

            // Add the structure path as the clip.
            clipper.AddPaths(structurePath, PolyType.ptClip, true);

            // Prepare a container for the result.
            Paths solution = new Paths();

            // Execute the intersection operation.
            // Using NonZero fill types here (adjust as needed).
            bool succeeded = clipper.Execute(
                ClipType.ctIntersection,
                solution,
                PolyFillType.pftNonZero,
                PolyFillType.pftNonZero);

            // If the operation failed, return an empty Paths collection.
            if (!succeeded)
                return new Paths();

            // Return the intersected polygon paths.
            return solution;
        }
    }
}
