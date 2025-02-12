using CoronaDVH.GMath;

namespace CoronaDVH.Dicom
{
    public interface ISliceContours
    {
        Dictionary<double, List<PolyLine2d>> Contours { get; set; }
    }
}
