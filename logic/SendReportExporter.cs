using ClosedXML.Excel;

namespace Automate_Whatsapp.Logic;

public static class SendReportExporter
{
    private const string WorksheetName = "Reporte";

    public static void ExportToExcel(string filePath, IReadOnlyList<WhatsAppMessageResult> results)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(results);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(WorksheetName);

        worksheet.Cell(1, 1).Value = "Numero";
        worksheet.Cell(1, 2).Value = "Nombre";
        worksheet.Cell(1, 3).Value = "LineaWP";
        worksheet.Cell(1, 4).Value = "Estado Mensaje";
        worksheet.Cell(1, 5).Value = "TieneWhatsapp";

        var headerRange = worksheet.Range("A1:E1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(219, 234, 254);
        headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        for (int index = 0; index < results.Count; index++)
        {
            WhatsAppMessageResult result = results[index];
            int rowNumber = index + 2;

            worksheet.Cell(rowNumber, 1).Value = result.Number;
            worksheet.Cell(rowNumber, 2).Value = result.Name;
            worksheet.Cell(rowNumber, 3).Value = result.LineaWP;
            worksheet.Cell(rowNumber, 4).Value = result.MessageState;
            worksheet.Cell(rowNumber, 5).Value = result.HasWhatsApp;
        }

        worksheet.SheetView.FreezeRows(1);
        worksheet.Range("A1:E1").SetAutoFilter();
        worksheet.Columns().AdjustToContents();

        workbook.SaveAs(filePath);
    }
}
