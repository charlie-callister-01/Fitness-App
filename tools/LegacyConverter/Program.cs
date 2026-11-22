using System.Text;
using Core.Serialisation;
using LegacyConverter;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: LegacyConverter <FitnessFolder> <OutputFolder>");
    return 1;
}

var fitnessFolder = Path.GetFullPath(args[0]);
var outputFolder = Path.GetFullPath(args[1]);

var folder = LegacyFolderLoader.Load(fitnessFolder);
var result = LegacyConverter.LegacyConverter.Convert(folder);

Directory.CreateDirectory(outputFolder);

var datasetPath = Path.Combine(outputFolder, "example-dataset.json");
var reportPath = Path.Combine(outputFolder, "conversion-report.md");

var json = ExchangeFormat.Write(result.Document, indented: true);
var report = ReportWriter.Format(result);

var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
File.WriteAllText(datasetPath, json, utf8NoBom);
File.WriteAllText(reportPath, report, utf8NoBom);

Console.WriteLine($"Wrote {datasetPath}");
Console.WriteLine($"Wrote {reportPath}");
return 0;
