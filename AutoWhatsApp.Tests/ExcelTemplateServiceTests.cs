using System.IO.Compression;
using System.Xml.Linq;
using ClosedXML.Excel;
using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class ExcelTemplateServiceTests
{
    [Fact]
    public void CreateTemplate_CreatesExpectedHeadersAndInstructions()
    {
        string tempDir = CreateTempDirectory();
        string filePath = Path.Combine(tempDir, ExcelTemplateService.DefaultFileName);

        try
        {
            ExcelTemplateService.CreateTemplate(filePath);

            using var workbook = new XLWorkbook(filePath);
            IXLWorksheet messagesSheet = workbook.Worksheet("Mensajes");
            IXLWorksheet instructionsSheet = workbook.Worksheet("Instrucciones");

            Assert.Equal("Código país", messagesSheet.Cell(1, 1).GetString());
            Assert.Equal("Teléfono", messagesSheet.Cell(1, 2).GetString());
            Assert.Equal("Mensaje", messagesSheet.Cell(1, 3).GetString());
            Assert.Equal("Plantilla", messagesSheet.Cell(1, 4).GetString());
            Assert.Equal("Banco", messagesSheet.Cell(1, 5).GetString());
            Assert.Equal("Nombre deudor", messagesSheet.Cell(1, 6).GetString());
            Assert.Equal("toAudio", messagesSheet.Cell(1, 7).GetString());
            Assert.Equal("optIn", messagesSheet.Cell(1, 8).GetString());
            Assert.Equal("optInSource", messagesSheet.Cell(1, 9).GetString());
            Assert.Equal("optInAt", messagesSheet.Cell(1, 10).GetString());

            string instructionsText = string.Join(
                "\n",
                Enumerable.Range(1, 22).Select(row => instructionsSheet.Cell(row, 1).GetString()));

            Assert.Contains("Si usas Plantilla, escribe 1, 2 o 3", instructionsText, StringComparison.Ordinal);
            Assert.Contains("Banco reemplaza {Banco} y Nombre deudor reemplaza {NombreDeudor}", instructionsText, StringComparison.Ordinal);
            Assert.Contains("Mensaje como modo legacy", instructionsText, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void CreateTemplate_AddsExpectedListValidations()
    {
        string tempDir = CreateTempDirectory();
        string filePath = Path.Combine(tempDir, ExcelTemplateService.DefaultFileName);

        try
        {
            ExcelTemplateService.CreateTemplate(filePath);

            using ZipArchive archive = ZipFile.OpenRead(filePath);
            ZipArchiveEntry sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
                ?? throw new InvalidOperationException("No se encontró el XML de la hoja Mensajes.");

            using Stream stream = sheetEntry.Open();
            XDocument sheetDocument = XDocument.Load(stream);
            XNamespace spreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            List<XElement> validations = sheetDocument
                .Descendants(spreadsheetNs + "dataValidation")
                .ToList();

            Assert.Contains(
                validations,
                validation => (string?)validation.Attribute("sqref") == "D2:D1000"
                    && (string?)validation.Element(spreadsheetNs + "formula1") == "\"1,2,3\"");
            Assert.Contains(
                validations,
                validation => (string?)validation.Attribute("sqref") == "G2:G1000"
                    && (string?)validation.Element(spreadsheetNs + "formula1") == "\"true,false,1,0\"");
            Assert.Contains(
                validations,
                validation => (string?)validation.Attribute("sqref") == "H2:H1000"
                    && (string?)validation.Element(spreadsheetNs + "formula1") == "\"true,false,1,0\"");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }
}
