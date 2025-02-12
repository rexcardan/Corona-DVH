using CoronaConsole;
using CoronaDVH.Dicom;
using CoronaDVH.Geometry;

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

//Resample dose to CT grid
var dvhDose = dose.ResampleOn(ct);
foreach (var str in structures)
{
    var sdf = new VarianStructureGrid(str, ct);
    var dvh = sdf.AggregateDvh(dvhDose);
}
