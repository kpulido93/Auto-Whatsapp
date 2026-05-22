using Automate_Whatsapp.Logic;
using ClosedXML.Excel;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class ExcelReaderTests
{
    [Fact]
    public void ReadPreviewResult_WhenOptInUsesSupportedValues_ClassifiesRowsCorrectly()
    {
        string filePath = CreateWorkbook(
            ("Código país", "Teléfono", "Mensaje", "toAudio", "optIn", "optInSource", "optInAt"),
            ("57", "3001111111", "mensaje 1", "false", "true", "formulario web", "2026-05-01"),
            ("57", "3001111112", "mensaje 2", "false", "false", "", ""),
            ("57", "3001111113", "mensaje 3", "1", "1", "evento presencial", "2026-05-02"),
            ("57", "3001111114", "mensaje 4", "0", "0", "", ""));

        try
        {
            var result = ExcelReader.ReadPreviewResult(filePath);

            Assert.Empty(result.MissingConsentColumns);
            Assert.Equal(4, result.Rows.Count);

            Assert.True(result.Rows[0].IsSendable);
            Assert.Equal(true, result.Rows[0].OptIn);
            Assert.Equal("formulario web", result.Rows[0].OptInSource);
            Assert.Equal(new DateTime(2026, 5, 1), result.Rows[0].OptInAt?.Date);

            Assert.False(result.Rows[1].IsSendable);
            Assert.True(result.Rows[1].LacksExplicitOptIn);
            Assert.Equal(false, result.Rows[1].OptIn);

            Assert.True(result.Rows[2].IsSendable);
            Assert.True(result.Rows[2].ToAudio);
            Assert.Equal(true, result.Rows[2].OptIn);

            Assert.False(result.Rows[3].IsSendable);
            Assert.True(result.Rows[3].LacksExplicitOptIn);
            Assert.Equal(false, result.Rows[3].OptIn);
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void ReadPreviewResult_WhenOptInColumnIsMissing_WarnsAndMarksRowsAsWithoutConsent()
    {
        string filePath = CreateWorkbook(
            ("Código país", "Teléfono", "Mensaje", "toAudio"),
            ("57", "3001111111", "mensaje sin consentimiento", "false"));

        try
        {
            var result = ExcelReader.ReadPreviewResult(filePath);
            var row = Assert.Single(result.Rows);

            Assert.Contains("optIn", result.MissingConsentColumns);
            Assert.Contains("optInSource", result.MissingConsentColumns);
            Assert.Contains("optInAt", result.MissingConsentColumns);
            Assert.False(row.IsSendable);
            Assert.False(row.HasInvalidData);
            Assert.True(row.LacksExplicitOptIn);
            Assert.Contains("falta columna optIn", row.ValidationStatus, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void ReadOutboundMessages_WhenPhoneIsValidButOptInIsFalse_ExcludesRowFromSendFlow()
    {
        string filePath = CreateWorkbook(
            ("Código país", "Teléfono", "Mensaje", "toAudio", "optIn", "optInSource", "optInAt"),
            ("57", "3001111111", "mensaje bloqueado", "false", "false", "", ""));

        try
        {
            var messages = ExcelReader.ReadOutboundMessages(filePath);
            var previewRow = Assert.Single(ExcelReader.ReadPreview(filePath));

            Assert.Empty(messages);
            Assert.False(previewRow.IsSendable);
            Assert.True(previewRow.LacksExplicitOptIn);
            Assert.False(previewRow.HasInvalidData);
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void ReadPreviewResult_WhenContactIsBlockedByDoNotContact_MarksRowAsNotSendable()
    {
        string filePath = CreateWorkbook(
            ("Código país", "Teléfono", "Mensaje", "toAudio", "optIn", "optInSource", "optInAt"),
            ("57", "(300) 111-1111", "mensaje permitido", "false", "true", "formulario web", "2026-05-01"));

        var blockedEntries = new[]
        {
            new DoNotContactEntry("+57", "3001111111", "solicitud del usuario")
        };

        try
        {
            var previewRow = Assert.Single(ExcelReader.ReadPreview(filePath, blockedEntries));
            var outboundMessages = ExcelReader.ReadOutboundMessages(filePath, blockedEntries);

            Assert.False(previewRow.IsSendable);
            Assert.True(previewRow.IsBlockedByDoNotContact);
            Assert.False(previewRow.HasInvalidData);
            Assert.False(previewRow.LacksExplicitOptIn);
            Assert.Equal("Bloqueado por lista no contactar", previewRow.ValidationStatus);
            Assert.Empty(outboundMessages);
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    [Fact]
    public void CreateTemplate_IncludesConsentColumns()
    {
        string filePath = Path.Combine(Path.GetTempPath(), "AutoWhatsAppTests", $"{Guid.NewGuid():N}.xlsx");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        try
        {
            ExcelTemplateService.CreateTemplate(filePath);

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet("Mensajes");

            Assert.Equal("Plantilla", worksheet.Cell(1, 4).GetString());
            Assert.Equal("Banco", worksheet.Cell(1, 5).GetString());
            Assert.Equal("Nombre deudor", worksheet.Cell(1, 6).GetString());
            Assert.Equal("toAudio", worksheet.Cell(1, 7).GetString());
            Assert.Equal("optIn", worksheet.Cell(1, 8).GetString());
            Assert.Equal("optInSource", worksheet.Cell(1, 9).GetString());
            Assert.Equal("optInAt", worksheet.Cell(1, 10).GetString());
        }
        finally
        {
            DeleteFile(filePath);
        }
    }

    private static string CreateWorkbook((string, string, string, string, string, string, string) headers, params (string, string, string, string, string, string, string)[] rows)
    {
        string filePath = Path.Combine(Path.GetTempPath(), "AutoWhatsAppTests", $"{Guid.NewGuid():N}.xlsx");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Mensajes");

        worksheet.Cell(1, 1).Value = headers.Item1;
        worksheet.Cell(1, 2).Value = headers.Item2;
        worksheet.Cell(1, 3).Value = headers.Item3;
        worksheet.Cell(1, 4).Value = headers.Item4;
        worksheet.Cell(1, 5).Value = headers.Item5;
        worksheet.Cell(1, 6).Value = headers.Item6;
        worksheet.Cell(1, 7).Value = headers.Item7;

        for (int index = 0; index < rows.Length; index++)
        {
            int rowNumber = index + 2;
            worksheet.Cell(rowNumber, 1).Value = rows[index].Item1;
            worksheet.Cell(rowNumber, 2).Value = rows[index].Item2;
            worksheet.Cell(rowNumber, 3).Value = rows[index].Item3;
            worksheet.Cell(rowNumber, 4).Value = rows[index].Item4;
            worksheet.Cell(rowNumber, 5).Value = rows[index].Item5;
            worksheet.Cell(rowNumber, 6).Value = rows[index].Item6;
            worksheet.Cell(rowNumber, 7).Value = rows[index].Item7;
        }

        workbook.SaveAs(filePath);
        return filePath;
    }

    private static string CreateWorkbook((string, string, string, string) headers, params (string, string, string, string)[] rows)
    {
        string filePath = Path.Combine(Path.GetTempPath(), "AutoWhatsAppTests", $"{Guid.NewGuid():N}.xlsx");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Mensajes");

        worksheet.Cell(1, 1).Value = headers.Item1;
        worksheet.Cell(1, 2).Value = headers.Item2;
        worksheet.Cell(1, 3).Value = headers.Item3;
        worksheet.Cell(1, 4).Value = headers.Item4;

        for (int index = 0; index < rows.Length; index++)
        {
            int rowNumber = index + 2;
            worksheet.Cell(rowNumber, 1).Value = rows[index].Item1;
            worksheet.Cell(rowNumber, 2).Value = rows[index].Item2;
            worksheet.Cell(rowNumber, 3).Value = rows[index].Item3;
            worksheet.Cell(rowNumber, 4).Value = rows[index].Item4;
        }

        workbook.SaveAs(filePath);
        return filePath;
    }

    private static void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
