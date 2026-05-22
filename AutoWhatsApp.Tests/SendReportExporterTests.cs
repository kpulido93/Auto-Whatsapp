using Automate_Whatsapp.Logic;
using ClosedXML.Excel;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class SendReportExporterTests
{
    [Fact]
    public void ExportToExcel_CreatesReporteSheetWithExactHeadersAndRowsInOrder()
    {
        string tempDir = CreateTempDirectory();
        string filePath = Path.Combine(tempDir, "reporte.xlsx");
        var results = new[]
        {
            new WhatsAppMessageResult(
                "573001112233",
                true,
                false,
                WhatsAppHealthStatus.Ready,
                "Mensaje enviado.",
                "Linea 1",
                "Ana Perez",
                "Enviado",
                "Sí"),
            new WhatsAppMessageResult(
                "573001112244",
                false,
                false,
                WhatsAppHealthStatus.InvalidDestinationNumber,
                "No tiene WhatsApp.",
                "Linea 2",
                "Luis Gomez",
                "No enviado - número inválido",
                "No")
        };

        try
        {
            SendReportExporter.ExportToExcel(filePath, results);

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet("Reporte");

            Assert.Equal("Numero", worksheet.Cell(1, 1).GetString());
            Assert.Equal("Nombre", worksheet.Cell(1, 2).GetString());
            Assert.Equal("LineaWP", worksheet.Cell(1, 3).GetString());
            Assert.Equal("Estado Mensaje", worksheet.Cell(1, 4).GetString());
            Assert.Equal("TieneWhatsapp", worksheet.Cell(1, 5).GetString());

            Assert.Equal("573001112233", worksheet.Cell(2, 1).GetString());
            Assert.Equal("Ana Perez", worksheet.Cell(2, 2).GetString());
            Assert.Equal("Linea 1", worksheet.Cell(2, 3).GetString());
            Assert.Equal("Enviado", worksheet.Cell(2, 4).GetString());
            Assert.Equal("Sí", worksheet.Cell(2, 5).GetString());

            Assert.Equal("573001112244", worksheet.Cell(3, 1).GetString());
            Assert.Equal("Luis Gomez", worksheet.Cell(3, 2).GetString());
            Assert.Equal("Linea 2", worksheet.Cell(3, 3).GetString());
            Assert.Equal("No enviado - número inválido", worksheet.Cell(3, 4).GetString());
            Assert.Equal("No", worksheet.Cell(3, 5).GetString());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ExportToExcel_WhenResultsAreEmpty_WritesOnlyHeaders()
    {
        string tempDir = CreateTempDirectory();
        string filePath = Path.Combine(tempDir, "reporte-vacio.xlsx");

        try
        {
            SendReportExporter.ExportToExcel(filePath, Array.Empty<WhatsAppMessageResult>());

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet("Reporte");

            Assert.Equal("Numero", worksheet.Cell(1, 1).GetString());
            Assert.Equal("Nombre", worksheet.Cell(1, 2).GetString());
            Assert.Equal("LineaWP", worksheet.Cell(1, 3).GetString());
            Assert.Equal("Estado Mensaje", worksheet.Cell(1, 4).GetString());
            Assert.Equal("TieneWhatsapp", worksheet.Cell(1, 5).GetString());
            Assert.True(worksheet.Row(2).Cells(1, 5).All(cell => string.IsNullOrEmpty(cell.GetString())));
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
