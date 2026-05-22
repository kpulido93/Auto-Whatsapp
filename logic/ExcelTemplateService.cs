using ClosedXML.Excel;

namespace Automate_Whatsapp.Logic
{
    public static class ExcelTemplateService
    {
        public const string DefaultFileName = "plantilla_auto_whatsapp.xlsx";

        public static void CreateTemplate(string filePath)
        {
            using var workbook = new XLWorkbook();
            var messagesSheet = workbook.Worksheets.Add("Mensajes");

            messagesSheet.Cell(1, 1).Value = "Código país";
            messagesSheet.Cell(1, 2).Value = "Teléfono";
            messagesSheet.Cell(1, 3).Value = "Mensaje";
            messagesSheet.Cell(1, 4).Value = "toAudio";
            messagesSheet.Cell(1, 5).Value = "optIn";
            messagesSheet.Cell(1, 6).Value = "optInSource";
            messagesSheet.Cell(1, 7).Value = "optInAt";

            var headerRange = messagesSheet.Range("A1:G1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 252, 231);
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            messagesSheet.SheetView.FreezeRows(1);
            messagesSheet.Range("A1:G1").SetAutoFilter();
            messagesSheet.Column(1).Width = 16;
            messagesSheet.Column(2).Width = 18;
            messagesSheet.Column(3).Width = 48;
            messagesSheet.Column(4).Width = 14;
            messagesSheet.Column(5).Width = 12;
            messagesSheet.Column(6).Width = 24;
            messagesSheet.Column(7).Width = 16;
            messagesSheet.Column(7).Style.DateFormat.Format = "yyyy-mm-dd";

            var toAudioValidation = messagesSheet.Range("D2:D1000").CreateDataValidation();
            toAudioValidation.List("\"true,false,1,0\"", true);
            toAudioValidation.IgnoreBlanks = true;
            toAudioValidation.InCellDropdown = true;
            toAudioValidation.ShowInputMessage = true;
            toAudioValidation.InputTitle = "Valores permitidos";
            toAudioValidation.InputMessage = "Usa true, false, 1 o 0.";
            toAudioValidation.ShowErrorMessage = true;
            toAudioValidation.ErrorTitle = "toAudio inválido";
            toAudioValidation.ErrorMessage = "Usa true, false, 1 o 0.";

            var optInValidation = messagesSheet.Range("E2:E1000").CreateDataValidation();
            optInValidation.List("\"true,false,1,0\"", true);
            optInValidation.IgnoreBlanks = true;
            optInValidation.InCellDropdown = true;
            optInValidation.ShowInputMessage = true;
            optInValidation.InputTitle = "Valores permitidos";
            optInValidation.InputMessage = "Usa true, false, 1 o 0.";
            optInValidation.ShowErrorMessage = true;
            optInValidation.ErrorTitle = "optIn inválido";
            optInValidation.ErrorMessage = "Usa true, false, 1 o 0.";

            AddInstructionsSheet(workbook);

            workbook.SaveAs(filePath);
        }

        private static void AddInstructionsSheet(XLWorkbook workbook)
        {
            var instructionsSheet = workbook.Worksheets.Add("Instrucciones");

            instructionsSheet.Cell(1, 1).Value = "Cómo llenar la hoja Mensajes";
            instructionsSheet.Cell(1, 1).Style.Font.Bold = true;
            instructionsSheet.Cell(1, 1).Style.Font.FontSize = 14;

            instructionsSheet.Cell(3, 1).Value = "1. Conserva los encabezados exactos en la fila 1.";
            instructionsSheet.Cell(4, 1).Value = "2. Escribe tus datos desde la fila 2 de la hoja Mensajes.";
            instructionsSheet.Cell(5, 1).Value = "3. No dejes Teléfono ni Mensaje vacíos en filas que quieras enviar.";
            instructionsSheet.Cell(6, 1).Value = "4. En toAudio usa sólo true, false, 1 o 0.";
            instructionsSheet.Cell(7, 1).Value = "5. En optIn usa sólo true, false, 1 o 0.";
            instructionsSheet.Cell(8, 1).Value = "6. Si optIn es true o 1, completa optInSource con el origen del consentimiento.";
            instructionsSheet.Cell(9, 1).Value = "7. optInAt es opcional y puede registrarse como fecha.";
            instructionsSheet.Cell(10, 1).Value = "8. Esta plantilla no incluye filas listas para envío real.";

            instructionsSheet.Cell(12, 1).Value = "Campo";
            instructionsSheet.Cell(12, 2).Value = "Ejemplo seguro";
            instructionsSheet.Cell(12, 3).Value = "Notas";

            instructionsSheet.Cell(13, 1).Value = "Código país";
            instructionsSheet.Cell(13, 2).Value = "57";
            instructionsSheet.Cell(13, 3).Value = "Sólo el indicativo, sin +.";

            instructionsSheet.Cell(14, 1).Value = "Teléfono";
            instructionsSheet.Cell(14, 2).Value = "0000000000";
            instructionsSheet.Cell(14, 3).Value = "Reemplaza por un número real antes de enviar.";

            instructionsSheet.Cell(15, 1).Value = "Mensaje";
            instructionsSheet.Cell(15, 2).Value = "Mensaje de prueba interno";
            instructionsSheet.Cell(15, 3).Value = "Reemplaza por el texto final.";

            instructionsSheet.Cell(16, 1).Value = "toAudio";
            instructionsSheet.Cell(16, 2).Value = "false";
            instructionsSheet.Cell(16, 3).Value = "true o 1 convierte el mensaje a audio.";

            instructionsSheet.Cell(17, 1).Value = "optIn";
            instructionsSheet.Cell(17, 2).Value = "true";
            instructionsSheet.Cell(17, 3).Value = "Sólo se enviarán filas con true o 1.";

            instructionsSheet.Cell(18, 1).Value = "optInSource";
            instructionsSheet.Cell(18, 2).Value = "formulario web";
            instructionsSheet.Cell(18, 3).Value = "Obligatorio cuando optIn es true o 1.";

            instructionsSheet.Cell(19, 1).Value = "optInAt";
            instructionsSheet.Cell(19, 2).Value = "2026-05-21";
            instructionsSheet.Cell(19, 3).Value = "Opcional. Usa formato ISO si conoces la fecha.";

            var headerRange = instructionsSheet.Range("A12:C12");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(219, 234, 254);
            instructionsSheet.Columns().AdjustToContents();
        }
    }
}
