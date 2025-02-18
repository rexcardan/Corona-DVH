using ClipperLib;
using CoronaDVH.GMath;
using CoronaDVH.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoronaDVH.Dicom
{
    public class RTStructureContour
    {
        public PolyLine2d Contour { get; set; }
        public bool IsHole { get; set; }

        public ClipperOffset ToClipperOffset()
        {
            var offset = new ClipperOffset();
            var intPts = new List<Vector2l>();
            foreach (var pt in Contour)
            {
                intPts.Add(new Vector2l(
                    (long)Math.Round(pt.X * PolyHelper.FloatToIntScale),
                    (long)Math.Round(pt.Y * PolyHelper.FloatToIntScale)));
            }
            // Both holes and outer contours are closed polygons, so always use etClosedPolygon.
            offset.AddPath(intPts, JoinType.jtRound, EndType.etClosedPolygon);
            return offset;
        }

        public Vector2d GetCentroid()
        {
            if(Contour == null)
            {
                return new Vector2d(double.NaN, double.NaN);
            }
            else
            {
                return Contour.GetCentroid();
            }
        }
    }
}
