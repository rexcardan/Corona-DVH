using CoronaDVH.GMath;
using CoronaDVH.Helpers;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoronaDVH.Dicom
{
    public class StructureSet
    {
        public static List<RTStructure> FromFile(string structureFile)
        {
            var logger = ServiceLocator.ServiceProvider.GetRequiredService<ILogger<StructureSet>>();
            var dcm = DicomFile.Open(structureFile);
            var metas = dcm.Dataset.GetSequence(DicomTag.StructureSetROISequence).Items.Select(i =>
            {
                var meta = new RTStructure();
                meta.StructureId = i.GetString(DicomTag.ROIName);
                meta.ROINumber = i.GetValues<int>(DicomTag.ROINumber)[0];
                return meta;
            }).ToList();

            for (int k = 0; k < metas.Count; k++)
            {
                var meta = metas[k];
                try
                {
                    var comatch = dcm.Dataset.GetSequence(DicomTag.ROIContourSequence)
                        .Items.FirstOrDefault(i => i.GetValues<int>(DicomTag.ReferencedROINumber)[0] == meta.ROINumber);
                    var romatch = dcm.Dataset.GetSequence(DicomTag.RTROIObservationsSequence)
                        .Items.FirstOrDefault(i => i.GetValues<int>(DicomTag.ReferencedROINumber)[0] == meta.ROINumber);

                    var colorValues = comatch.GetValues<int>(DicomTag.ROIDisplayColor);
                    meta.Color = new RgbaColor((byte)colorValues[0], (byte)colorValues[1], (byte)colorValues[2]);
                    meta.DicomType = romatch.GetString(DicomTag.RTROIInterpretedType);
                    meta.Name = romatch.GetString(DicomTag.ROIObservationLabelRETIRED);

                    var hasContours = comatch.GetSequence(DicomTag.ContourSequence) != null;
                    if (!hasContours) { continue; }

                    Dictionary<double, List<RTStructureContour>> allSliceContours = new Dictionary<double, List<RTStructureContour>>();
                    foreach (var slice in comatch.GetSequence(DicomTag.ContourSequence).Items)
                    {
                        var contours = slice.GetValues<float>(DicomTag.ContourData);
                        if (contours.Length < 2 || contours.Length % 3 != 0)
                        {
                            logger.LogError($"Slice for structure {meta.StructureId} has {contours.Length} contour points. Not divisible by 3! Can't process.");
                            continue;
                        }
                        try
                        {
                            // Use the third coordinate (Z) from the first point as the key.
                            double sliceZ = contours[2];

                            if (!allSliceContours.ContainsKey(sliceZ))
                            {
                                allSliceContours[sliceZ] = new List<RTStructureContour>();
                            }

                            List<Vector2d> closedContour = new List<Vector2d>();
                            for (int i = 0; i < contours.Length; i += 3)
                            {
                                // Use X and Y; ignore Z since we’re working slice-by-slice.
                                closedContour.Add(new Vector2d(contours[i], contours[i + 1]));
                            }
                            var poly = new PolyLine2d(closedContour);

                            bool isHole = poly.IsClockwise();

                            allSliceContours[sliceZ].Add(new RTStructureContour { Contour = poly, IsHole = isHole });
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e.ToString());
                        }
                    }

                    meta.Contours = allSliceContours;
                }
                catch (Exception e)
                {
                    logger.LogError(e.ToString());
                }
            }

            return metas;
        }
    }
}
