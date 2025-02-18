using ClipperLib;
using CoronaDVH.GMath;
using CoronaDVH.Helpers;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<CoronaDVH.GMath.Vector2l>>;

namespace CoronaDVH.Dicom
{
    public class RTStructure
    {
        public Dictionary<double, List<RTStructureContour>> Contours { get; set; } = new Dictionary<double, List<RTStructureContour>>();

        public List<Paths> GetContoursOnSlice(double z)
        {
            // Find contours where z is within 0.001 of the key.
            var contours = Contours
                .Where(c => Math.Abs(c.Key - z) < 0.001)
                .SelectMany(c => c.Value)
                .ToList();

            // Separate outer contours from holes.
            var outers = contours.Where(c => !c.IsHole).ToList();
            var holes = contours.Where(c => c.IsHole).ToList();

            List<Paths> combined = new List<Paths>();

            foreach (var outer in outers)
            {
                // Use the centroid as a quick check to see if a hole lies inside this outer contour.
                var relevantHoles = holes.Where(h => outer.Contour.Contains(h.GetCentroid())).ToList();

                // Perform the subtraction operation, which returns a Paths object.
                Paths subtracted = PolyHelper.ClipperSubtract(outer, relevantHoles);
                if (subtracted != null && subtracted.Count > 0)
                {
                    // Each 'subtracted' represents one group (the outer with its holes removed).
                    combined.Add(subtracted);
                }
            }

            return combined;
        }

        public string StructureId { get; set; }
        public RgbaColor Color { get; set; }
        public int ROINumber { get; set; }
        public string DicomType { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return StructureId;
        }

        public List<RTSliceContourSet> GetContoursInRange(float sliceMinZ, float sliceMaxZ)
        {
            var ordered = Contours.OrderBy(c => c.Key).ToList();
            List<RTSliceContourSet> sliceContours = new List<RTSliceContourSet>();
            foreach (var slice in ordered)
            {
                if(slice.Key >= sliceMinZ && slice.Key <= sliceMaxZ)
                {
                    var contours = GetContoursOnSlice(slice.Key);
                    if (contours.Count > 0)
                    {
                        sliceContours.Add(new RTSliceContourSet(slice.Key, contours));
                    }
                }
            }
            return sliceContours;
        }
    }
}
