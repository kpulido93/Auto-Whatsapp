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
            messagesSheet.Cell(1, 4).Value = "Plantilla";
            messagesSheet.Cell(1, 5).Value = "Banco";
            messagesSheet.Cell(1, 6).Value = "Nombre deudor";
            messagesSheet.Cell(1, 7).Value = "toAudio";
            messagesSheet.Cell(1, 8).Value = "optIn";
            messagesSheet.Cell(1, 9).Value = "optInSource";
            messagesSheet.Cell(1, 10).Value = "optInAt";

            var headerRange = messagesSheet.Range("A1:J1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 252, 231);
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            messagesSheet.SheetView.FreezeRows(1);
            messagesSheet.Range("A1:J1").SetAutoFilter();
            messagesSheet.Column(1).Width = 16;
            messagesSheet.Column(2).Width = 18;
            messagesSheet.Column(3).Width = 48;
            messagesSheet.Column(4).Width = 12;
            messagesSheet.Column(5).Width = 22;
            messagesSheet.Column(6).Width = 24;
            messagesSheet.Column(7).Width = 14;
            messagesSheet.Column(8).Width = 12;
            messagesSheet.Column(9).Width = 24;
            messagesSheet.Column(10).Width = 20;
            messagesSheet.Column(10).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";

            var templateValidation = messagesSheet.Range("D2:D1000").CreateDataValidation();
            templateValidation.List("\"1,2,3\"", true);
            templateValidation.IgnoreBlanks = true;
            templateValidation.InCellDropdown = true;
            templateValidation.ShowInputMessage = true;
            templateValidation.InputTitle = "Plantillas disponibles";
            templateValidation.InputMessage = "Usa 1, 2 o 3 para generar el mensaje desde una plantilla.";
            templateValidation.ShowErrorMessage = true;
            templateValidation.ErrorTitle = "Plantilla inválida";
            templateValidation.ErrorMessage = "Usa 1, 2 o 3.";

            var toAudioValidation = messagesSheet.Range("G2:G1000").CreateDataValidation();
            toAudioValidation.List("\"true,false,1,0\"", true);
            toAudioValidation.IgnoreBlanks = true;
            toAudioValidation.InCellDropdown = true;
            toAudioValidation.ShowInputMessage = true;
            toAudioValidation.InputTitle = "Valores permitidos";
            toAudioValidation.InputMessage = "Usa true, false, 1 o 0.";
            toAudioValidation.ShowErrorMessage = true;
            toAudioValidation.ErrorTitle = "toAudio inválido";
            toAudioValidation.ErrorMessage = "Usa true, false, 1 o 0.";

            var optInValidation = messagesSheet.Range("H2:H1000").CreateDataValidation();
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
            instructionsSheet.Cell(5, 1).Value = "3. Si usas Plantilla, escribe 1, 2 o 3 y completa Banco y Nombre deudor.";
            instructionsSheet.Cell(6, 1).Value = "4. Si Plantilla queda vacía, la app seguirá usando Mensaje como modo legacy.";
            instructionsSheet.Cell(7, 1).Value = "5. Banco reemplaza {Banco} y Nombre deudor reemplaza {NombreDeudor}.";
            instructionsSheet.Cell(8, 1).Value = "6. En toAudio y optIn usa sólo true, false, 1 o 0.";
            instructionsSheet.Cell(9, 1).Value = "7. Si optIn es true o 1, completa optInSource con el origen del consentimiento.";
            instructionsSheet.Cell(10, 1).Value = "8. optInAt es opcional y puede registrarse como fecha u hora válidas.";
            instructionsSheet.Cell(11, 1).Value = "9. Esta plantilla no incluye filas listas para envío real.";

            instructionsSheet.Cell(13, 1).Value = "Campo";
            instructionsSheet.Cell(13, 2).Value = "Ejemplo seguro";
            instructionsSheet.Cell(13, 3).Value = "Notas";

            instructionsSheet.Cell(14, 1).Value = "Código país";
            instructionsSheet.Cell(14, 2).Value = "57";
            instructionsSheet.Cell(14, 3).Value = "Sólo el indicativo, sin +.";

            instructionsSheet.Cell(15, 1).Value = "Teléfono";
            instructionsSheet.Cell(15, 2).Value = "0000000000";
            instructionsSheet.Cell(15, 3).Value = "Reemplaza por un número real antes de enviar.";

            instructionsSheet.Cell(16, 1).Value = "Mensaje";
            instructionsSheet.Cell(16, 2).Value = "Mensaje de prueba interno";
            instructionsSheet.Cell(16, 3).Value = "Modo legacy. Se usa sólo cuando Plantilla está vacía.";

            instructionsSheet.Cell(17, 1).Value = "Plantilla";
            instructionsSheet.Cell(17, 2).Value = "1";
            instructionsSheet.Cell(17, 3).Value = "Usa 1, 2 o 3 para generar el mensaje automáticamente.";

            instructionsSheet.Cell(18, 1).Value = "Banco";
            instructionsSheet.Cell(18, 2).Value = "Bancolombia";
            instructionsSheet.Cell(18, 3).Value = "Reemplaza la variable {Banco} cuando uses Plantilla.";

            instructionsSheet.Cell(19, 1).Value = "Nombre deudor";
            instructionsSheet.Cell(19, 2).Value = "Ana Pérez";
            instructionsSheet.Cell(19, 3).Value = "Reemplaza la variable {NombreDeudor} cuando uses Plantilla.";

            instructionsSheet.Cell(20, 1).Value = "toAudio";
            instructionsSheet.Cell(20, 2).Value = "false";
            instructionsSheet.Cell(20, 3).Value = "true o 1 convierte el mensaje a audio.";

            instructionsSheet.Cell(21, 1).Value = "optIn";
            instructionsSheet.Cell(21, 2).Value = "true";
            instructionsSheet.Cell(21, 3).Value = "Sólo se enviarán filas con consentimiento explícito.";

            instructionsSheet.Cell(22, 1).Value = "optInSource";
            instructionsSheet.Cell(22, 2).Value = "Formulario web";
            instructionsSheet.Cell(22, 3).Value = "Requerido cuando optIn es true o 1.";

            instructionsSheet.Cell(23, 1).Value = "optInAt";
            instructionsSheet.Cell(23, 2).Value = "2026-05-22 10:30";
            instructionsSheet.Cell(23, 3).Value = "Opcional. Usa formato ISO o una fecha válida.";

            var headerRange = instructionsSheet.Range("A13:C13");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(219, 234, 254);
            instructionsSheet.Columns().AdjustToContents();
        }
    }
}
