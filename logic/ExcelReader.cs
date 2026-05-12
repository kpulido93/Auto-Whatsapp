using ClosedXML.Excel;

namespace Automate_Whatsapp.Logic
{
    public sealed class ExcelMessagePreviewRow
    {
        public int RowNumber { get; init; }
        public string CountryCode { get; init; } = "";
        public string Phone { get; init; } = "";
        public string Message { get; init; } = "";
        public bool ToAudio { get; init; }
        public bool IsValid { get; init; }
        public string ValidationStatus { get; init; } = "";
    }

    public static class ExcelReader
    {
        // Devuelve lista de (teléfono, mensaje, toAudio)
        public static List<(string countrycode, string phone, string message, bool toAudio)> ReadMessages(string filePath)
        {
            return ReadPreview(filePath)
                .Where(row => row.IsValid)
                .Select(row => (row.CountryCode, row.Phone, row.Message, row.ToAudio))
                .ToList();
        }

        public static List<ExcelMessagePreviewRow> ReadPreview(string filePath)
        {
            var rows = new List<ExcelMessagePreviewRow>();

            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet(1);

                // Asume que la primera fila es encabezado, empieza en la 2
                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    string countrycode = row.Cell(1).GetString();
                    string phone = row.Cell(2).GetString().Trim().Replace(" ", "");
                    string message = row.Cell(3).GetString().Trim();
                    bool toAudio = ParseToAudio(row.Cell(4));

                    var validationIssues = new List<string>();
                    if (string.IsNullOrWhiteSpace(phone))
                    {
                        validationIssues.Add("Falta teléfono");
                    }

                    if (string.IsNullOrWhiteSpace(message))
                    {
                        validationIssues.Add("Falta mensaje");
                    }

                    rows.Add(new ExcelMessagePreviewRow
                    {
                        RowNumber = row.RowNumber(),
                        CountryCode = countrycode,
                        Phone = phone,
                        Message = message,
                        ToAudio = toAudio,
                        IsValid = validationIssues.Count == 0,
                        ValidationStatus = validationIssues.Count == 0
                            ? "Válida"
                            : string.Join(", ", validationIssues)
                    });
                }
            }

            return rows;
        }

        private static bool ParseToAudio(IXLCell cell)
        {
            bool toAudio = false;

            if (cell.IsEmpty())
            {
                return toAudio;
            }

            if (cell.DataType == XLDataType.Boolean)
            {
                return cell.GetBoolean();
            }

            // Intenta convertir texto a bool, acepta "true", "false", "1", "0"
            if (bool.TryParse(cell.GetString().Trim().ToLower(), out bool result))
            {
                return result;
            }

            if (int.TryParse(cell.GetString().Trim(), out int intResult))
            {
                return intResult != 0;
            }

            return toAudio;
        }
    }
}
