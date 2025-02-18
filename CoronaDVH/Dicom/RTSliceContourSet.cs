using CoronaDVH.GMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<CoronaDVH.GMath.Vector2l>>;

namespace CoronaDVH.Dicom
{
    public class RTSliceContourSet
    {
        public double SliceZ { get; set; }
        public List<Paths> Contours { get; set; } = new List<Paths>();

        public RTSliceContourSet(double sliceZ, List<Paths> contours)
        {
            this.SliceZ = sliceZ;
            this.Contours = contours;
        }
    }
}
