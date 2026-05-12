using ClosedXML.Excel;

namespace Automate_Whatsapp.Logic
{
    public static class ExcelReader
    {
        // Devuelve lista de (teléfono, mensaje, toAudio)
        public static List<(string countrycode, string phone, string message, bool toAudio)> ReadMessages(string filePath)
        {
            var list = new List<(string countrycode, string phone, string message, bool toAudio)>();

            using (var workbook = new XLWorkbook(filePath))

            {
                var worksheet = workbook.Worksheet(1);

                // Asume que la primera fila es encabezado, empieza en la 2
                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    string countrycode = row.Cell(1).GetString();
                    string phone = row.Cell(2).GetString().Trim().Replace(" ", "");
                    string message = row.Cell(3).GetString().Trim();

                    bool toAudio = false;
                    var cell = row.Cell(4);

                    if (!cell.IsEmpty())
                    {
                        if (cell.DataType == XLDataType.Boolean)
                        {
                            toAudio = cell.GetBoolean();
                        }
                        else
                        {
                            // Intenta convertir texto a bool, acepta "true", "false", "1", "0"
                            if (bool.TryParse(cell.GetString().Trim().ToLower(), out bool result))
                            {
                                toAudio = result;
                            }
                            else if (int.TryParse(cell.GetString().Trim(), out int intResult))
                            {
                                toAudio = intResult != 0;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(phone) && !string.IsNullOrEmpty(message))
                    {
                        list.Add((countrycode, phone, message, toAudio));
                    }
                }

                return list;
            }
        }
    }
}
