using CoronaConsole;
using CoronaDVH.Dicom;
using CoronaDVH.Geometry;
using CoronaDVH.Helpers;
using Spectre.Console;

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

var groundTruth = GroundTruth.Eclipse18_1_with_1824_7_max_dose();
var table = new Table();
table.AddColumn("Structure");
table.AddColumn("Volume (cm³)");
table.AddColumn("Min Dose (cGy)");
table.AddColumn("Max Dose (cGy)");
table.AddColumn("Mean Dose (cGy)");

foreach (var str in structures)
{
    if (groundTruth.ContainsKey(str.Name))
    {
        var groundTruthStr = groundTruth[str.Name];
        var dvh = DvhAggregator.Aggregate(dose, StructureMask.FromContours(str, dose));

        var maxDose = dvh.MaxDose * 100;
        var minDose = dvh.MinDose * 100;
        var meanDose = dvh.MeanDose * 100;
        var volume = dvh.Volume / 1000;

        double RelErr(double v1, double v2) => Math.Abs(v2) < 1e-9 ? double.NaN : Math.Abs(v1 - v2) / v2;

        // Function to apply red color if relative error > 5%
        string Highlight(double v1, double v2)
        {
            double error = RelErr(v1, v2);
            string formatted = $"{v1:F2} / {v2:F2} ({error:P2})";
            return error > 0.05 ? $"[red]{formatted}[/]" : formatted;
        }

        table.AddRow(
            Markup.Escape(str.Name),
            Highlight(volume, groundTruthStr.Volume),
            Highlight(minDose, groundTruthStr.MinDose),
            Highlight(maxDose, groundTruthStr.MaxDose),
            Highlight(meanDose, groundTruthStr.MeanDose)
        );
    }
}

AnsiConsole.Write(table);