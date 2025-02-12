using CoronaDVH.Geometry;
using CoronaDVH.GMath;

namespace CoronaDVH.Helpers
{
    public class PolyFill
    {
        public static void FillSlice<T>(DenseGrid3T<T> grid, PolyLine2d poly, int sliceZ, T value)
        {
            var polySegments = poly.GetSegments().Where(s => s.Direction.Y != 0).ToList();
            double minY = double.MaxValue;
            double maxY = double.MinValue;

            foreach (var seg in polySegments)
            {
                maxY = Math.Max(maxY, Math.Max(seg.P0.Y, seg.P1.Y));
                minY = Math.Min(minY, Math.Min(seg.P0.Y, seg.P1.Y));
            }
            for (int y = (int)Math.Floor(minY); y < (int)Math.Ceiling(maxY); ++y)
            {
                var scanLine = new Segment2d(new Vector2d(0, y), new Vector2d(grid.Dimensions.X, y));
                var intersections = polySegments.Select(s =>
                {
                    var segCheck = new IntrSegment2Segment2(s, scanLine);
                    segCheck.Compute();
                    return segCheck;
                })
                .Where(s => s.Result == IntersectionResult.Intersects)
                .OrderBy(o => o.Point0.X).ToList();


                if (!intersections.Any()) { continue; }
                if (intersections.Count == 1)
                {
                    //Grazing pass of a single segment .. color in?
                }
                else
                {
                    List<double> pureIntersects = new List<double>();
                    //Clean up intersections
                    double lastX = intersections[0].Point0.X;
                    double lastYDir = Math.Sign(intersections[0].Segment1.Direction.Y);
                    pureIntersects.Add(lastX);

                    for (int i = 1; i < intersections.Count; i++)
                    {
                        var currentX = intersections[i].Point0.X;
                        var yDir = Math.Sign(intersections[i].Segment1.Direction.Y);
                        if (currentX == lastX && yDir != lastYDir)
                        {
                            pureIntersects.Add(currentX);
                        }
                        else if (currentX != lastX)
                        {
                            pureIntersects.Add(currentX);
                        }

                        lastX = currentX;
                        lastYDir = yDir;
                    }

                    if (pureIntersects.Count == 1)
                    {
                        grid[(int)pureIntersects[0], y, sliceZ] = value;
                    }
                    else
                    {
                        var sections = pureIntersects.ChunkIn(2);
                        foreach (var section in sections)
                        {
                            if (section.Count() == 2)
                            {
                                var start = section.ElementAt(0);
                                var end = section.ElementAt(1);
                                for (int x = (int)start; x < end; ++x)
                                    grid[x, y, sliceZ] = value;
                            }

                        }
                    }
                }
            }
        }
    }
}
