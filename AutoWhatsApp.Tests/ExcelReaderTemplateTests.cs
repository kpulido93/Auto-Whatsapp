using ClosedXML.Excel;
using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class ExcelReaderTemplateTests
{
    [Fact]
    public void ReadPreview_WhenTemplateIsSelected_ResolvesMessageUsingNormalizedHeaders()
    {
        string tempDir = CreateTempDirectory();
        string excelPath = Path.Combine(tempDir, "mensajes.xlsx");
        string templateConfigPath = Path.Combine(tempDir, MessageTemplateStore.ConfigFileName);

        try
        {
            CreateWorkbook(
                excelPath,
                [
                    " TELÉFONO ",
                    "Código país",
                    "mensaje",
                    "Plantilla",
                    " banco ",
                    "Nombre Deudor",
                    "toAudio",
                    " OPTIN ",
                    "optInSource",
                    "optInAt"
                ],
                ["3001112233", "57", "ignorado", "1", "Bancolombia", "Ana Pérez", "true", "1", "Formulario web", "2026-05-22"]);

            IReadOnlyList<ExcelMessagePreviewRow> rows = ExcelReader.ReadPreview(excelPath, templateConfigPath);

            ExcelMessagePreviewRow row = Assert.Single(rows);
            Assert.True(row.IsValid);
            Assert.Equal(1, row.TemplateNumber);
            Assert.Equal("Bancolombia", row.Bank);
            Assert.Equal("Ana Pérez", row.DebtorName);
            Assert.True(row.ToAudio);
            Assert.Equal("Hola Ana Pérez, te contactamos de Bancolombia.", row.ResolvedMessage);
            Assert.Equal("Hola Ana Pérez, te contactamos de Bancolombia.", row.Message);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ReadPreview_WhenTemplateIsEmpty_UsesLegacyMessageColumn()
    {
        string tempDir = CreateTempDirectory();
        string excelPath = Path.Combine(tempDir, "mensajes.xlsx");

        try
        {
            CreateWorkbook(
                excelPath,
                ["Código país", "Teléfono", "Mensaje", "toAudio", "optIn", "optInSource", "optInAt"],
                ["57", "3001112233", "Mensaje legado", "0", "true", "Formulario web", "2026-05-22"]);

            ExcelMessagePreviewRow row = Assert.Single(ExcelReader.ReadPreview(excelPath));

            Assert.True(row.IsValid);
            Assert.Null(row.TemplateNumber);
            Assert.Equal("Mensaje legado", row.Message);
            Assert.Equal("Mensaje legado", row.ResolvedMessage);
            Assert.False(row.ToAudio);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ReadPreview_WhenTemplateDoesNotExist_MarksRowInvalid()
    {
        string tempDir = CreateTempDirectory();
        string excelPath = Path.Combine(tempDir, "mensajes.xlsx");
        string templateConfigPath = Path.Combine(tempDir, MessageTemplateStore.ConfigFileName);

        try
        {
            CreateWorkbook(
                excelPath,
                ["Código país", "Teléfono", "Plantilla", "Banco", "Nombre deudor"],
                ["57", "3001112233", "99", "Bancolombia", "Ana Pérez"]);

            ExcelMessagePreviewRow row = Assert.Single(ExcelReader.ReadPreview(excelPath, templateConfigPath));
            IReadOnlyList<(string countrycode, string phone, string message, bool toAudio)> validMessages = ExcelReader.ReadMessages(excelPath, templateConfigPath);

            Assert.False(row.IsValid);
            Assert.Equal(99, row.TemplateNumber);
            Assert.Contains("plantilla 99", row.ValidationStatus, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(validMessages);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ReadPreview_WhenTemplateIsUsedWithoutBank_MarksRowInvalid()
    {
        string tempDir = CreateTempDirectory();
        string excelPath = Path.Combine(tempDir, "mensajes.xlsx");
        string templateConfigPath = Path.Combine(tempDir, MessageTemplateStore.ConfigFileName);

        try
        {
            CreateWorkbook(
                excelPath,
                ["Código país", "Teléfono", "Plantilla", "Banco", "Nombre deudor"],
                ["57", "3001112233", "1", "", "Ana Pérez"]);

            ExcelMessagePreviewRow row = Assert.Single(ExcelReader.ReadPreview(excelPath, templateConfigPath));

            Assert.False(row.IsValid);
            Assert.Contains("Falta Banco", row.ValidationStatus, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ReadPreview_WhenTemplateIsUsedWithoutDebtorName_MarksRowInvalid()
    {
        string tempDir = CreateTempDirectory();
        string excelPath = Path.Combine(tempDir, "mensajes.xlsx");
        string templateConfigPath = Path.Combine(tempDir, MessageTemplateStore.ConfigFileName);

        try
        {
            CreateWorkbook(
                excelPath,
                ["Código país", "Teléfono", "Plantilla", "Banco", "Nombre deudor"],
                ["57", "3001112233", "1", "Bancolombia", ""]);

            ExcelMessagePreviewRow row = Assert.Single(ExcelReader.ReadPreview(excelPath, templateConfigPath));

            Assert.False(row.IsValid);
            Assert.Contains("Falta Nombre deudor", row.ValidationStatus, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ReadPreview_KeepsToAudioAndOptInValidations()
    {
        string tempDir = CreateTempDirectory();
        string excelPath = Path.Combine(tempDir, "mensajes.xlsx");
        string templateConfigPath = Path.Combine(tempDir, MessageTemplateStore.ConfigFileName);

        try
        {
            CreateWorkbook(
                excelPath,
                ["Código país", "Teléfono", "Mensaje", "toAudio", "optIn", "optInSource", "optInAt"],
                ["57", "3001112233", "Mensaje legado", "talvez", "true", "", "fecha"]);

            ExcelMessagePreviewRow row = Assert.Single(ExcelReader.ReadPreview(excelPath, templateConfigPath));

            Assert.False(row.IsValid);
            Assert.Contains("toAudio inválido", row.ValidationStatus, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Falta optInSource", row.ValidationStatus, StringComparison.Ordinal);
            Assert.Contains("optInAt inválido", row.ValidationStatus, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void CreateWorkbook(string filePath, IReadOnlyList<string> headers, params object?[][] rows)
    {
        using var workbook = new XLWorkbook();
        IXLWorksheet sheet = workbook.Worksheets.Add("Mensajes");

        for (int column = 0; column < headers.Count; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            object?[] values = rows[rowIndex];
            for (int column = 0; column < values.Length; column++)
            {
                sheet.Cell(rowIndex + 2, column + 1).Value = values[column]?.ToString() ?? string.Empty;
            }
        }

        workbook.SaveAs(filePath);
    }

    private static string CreateTempDirectory()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }
}
