using CoronaConsole;
using CoronaDVH.Dicom;
using CoronaDVH.Geometry;
using CoronaDVH.Helpers;

LogHelper.ConfigureLogging();

//Download files from https://varian.box.com/s/5bjlifnl7dpsilb11vdxiric2vexmnx4
var testCasePath = @"C:\Exp\GBM_RTOG0825DICOM"; //Replace with the folder where you placed the files
var rootFiles = Directory.GetFiles(testCasePath, "*.dcm");

//Load CT
var ctFiles = rootFiles
    .Where(f => Path.GetFileName(f).StartsWith("CT")).ToArray();
var ct = ImageGrid.FromFiles(ctFiles);

//Load Dose
var doseFile = rootFiles.FirstOrDefault(f => Path.GetFileName(f).StartsWith("RD"));
var dose = DoseToGrid.FromFile(doseFile);

//Load Structures
var structureFile = rootFiles.FirstOrDefault(f => Path.GetFileName(f).StartsWith("RS"));
var structures = StructureSet.FromFile(structureFile);

// Known ground truth values (From Eclipse plan with 1824.7 max dose)
var groundTruth = new Dictionary<string, (double Volume, double MinDose, double MaxDose, double MeanDose)>
        {
            { "Brain Stem", (29.4, 278.3, 1292.3, 789.70) },
            { "CTV46", (184.6, 44.2, 1824.7, 1087.3) },
            { "Dose 65.89[Gy]", (0, 1667.5, 1715.8, 1702.9) },
            { "GTV", (1, 1552.9, 1704, 1637.7) },
            { "Lt Eye", (8.7, 90.5, 330.5, 194.9) },
            { "Lt Lens", (0.2, 127, 213.7, 159.6) },
            { "Lt Optic Nerve", (0.6, 169.5, 392.4, 248.3) },
            { "Lt Parotid", (18.4, 8.2, 70.6, 29.9) },
            { "Optic Chiasm", (2.4, 308, 881.1, 464.2) },
            { "PRV BS+3mm", (51.1, 172.4, 1581.6, 789) },
            { "PRV Optics+3mm", (56.3, 60.1, 1498.7, 376.3) },
            { "PTV46", (636, 22.2, 1824.7, 687.4) },
            { "PTV46opt", (485, 22.2, 1657.1, 414.1) },
            { "PTV60", (119.3, 445.5, 1824.7, 1602.2) },
            { "PTV60BS", (1.9, 969.1, 1290.1, 1127.7) },
            { "PTV60chiasm", (0.3, 445.5, 873.4, 660.1) },
            { "PTV60opt", (117.8, 688.6, 1824.7, 1613.5) },
            { "Patient", (3565.8, 0.3, 1824.7, 295.7) },
            { "Rt Eye", (8.1, 127.5, 767.6, 348.9) },
            { "Rt Lens", (0.2, 147.6, 270, 197) },
            { "Rt Optic Nerve", (0.8, 242.5, 773.7, 398.2) },
            { "Rt Parotid", (21.1, 15, 464, 70.5) },
            { "pituitary", (0.7, 331.6, 991, 517.8) }
        };

Console.WriteLine("Str Name , Volume , Min Dose , Max Dose , Mean Dose");
foreach (var str in structures)
{
    if (groundTruth.ContainsKey(str.Name))
    {
        var groundTruthStr = groundTruth[str.Name];
        var tmax = dose.Buffer.Max();
        var mask = StructureMask.FromContours(str, dose);
        tmax = dose.Buffer.Max();

        var dvh = DvhAggregator.Aggregate(dose, mask);
        var max = dvh.MaxDose * 100;//cGy
        var min = dvh.MinDose * 100; //cGy
        var mean = dvh.MeanDose * 100;//cGy
        var volume = dvh.Volume / 1000; //mm3 => cm3 

        Console.Write(str.Name);
        Console.Write(" , ");
        Console.Write($"{volume:f2}/{groundTruthStr.Volume:f2}");
        Console.Write(" , ");
        Console.Write($"{min:f2}/{groundTruthStr.MinDose:f2}");
        Console.Write(" , ");
        Console.Write($"{max:f2}/{groundTruthStr.MaxDose:f2}");
        Console.Write(" , ");
        Console.WriteLine($"{mean:f2}/{groundTruthStr.MeanDose:f2}");
    }
}
