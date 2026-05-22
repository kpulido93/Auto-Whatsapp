using System.Globalization;
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
        public bool? OptIn { get; init; }
        public string OptInDisplay => OptIn.HasValue ? (OptIn.Value ? "true" : "false") : "";
        public string OptInSource { get; init; } = "";
        public DateTime? OptInAt { get; init; }
        public string OptInAtDisplay => OptInAt.HasValue
            ? OptInAt.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : "";
        public bool HasInvalidData { get; init; }
        public bool IsBlockedByDoNotContact { get; init; }
        public bool LacksExplicitOptIn { get; init; }
        public bool IsSendable { get; init; }
        public bool IsValid => IsSendable;
        public string ValidationStatus { get; init; } = "";
    }

    public sealed class ExcelPreviewReadResult
    {
        public List<ExcelMessagePreviewRow> Rows { get; init; } = new();
        public IReadOnlyList<string> MissingConsentColumns { get; init; } = Array.Empty<string>();
        public string ConsentWarning => MissingConsentColumns.Count == 0
            ? ""
            : $"Faltan columnas de consentimiento: {string.Join(", ", MissingConsentColumns)}. Las filas sin opt-in explícito no se enviarán.";
    }

    public static class ExcelReader
    {
        // Devuelve lista de (teléfono, mensaje, toAudio)
        public static List<(string countrycode, string phone, string message, bool toAudio)> ReadMessages(
            string filePath,
            IReadOnlyCollection<DoNotContactEntry>? doNotContactEntries = null)
        {
            return ReadOutboundMessages(filePath, doNotContactEntries)
                .Select(row => (row.CountryCode, row.Phone, row.Message, row.ToAudio))
                .ToList();
        }

        public static List<OutboundMessage> ReadOutboundMessages(
            string filePath,
            IReadOnlyCollection<DoNotContactEntry>? doNotContactEntries = null)
        {
            return ReadPreviewResult(filePath, doNotContactEntries)
                .Rows
                .Where(row => row.IsSendable)
                .Select(row => new OutboundMessage(
                    row.CountryCode,
                    row.Phone,
                    row.Message,
                    row.ToAudio,
                    true,
                    row.OptInSource,
                    row.OptInAt))
                .ToList();
        }

        public static List<ExcelMessagePreviewRow> ReadPreview(
            string filePath,
            IReadOnlyCollection<DoNotContactEntry>? doNotContactEntries = null)
        {
            return ReadPreviewResult(filePath, doNotContactEntries).Rows;
        }

        public static ExcelPreviewReadResult ReadPreviewResult(
            string filePath,
            IReadOnlyCollection<DoNotContactEntry>? doNotContactEntries = null)
        {
            var rows = new List<ExcelMessagePreviewRow>();
            var blockedPhones = DoNotContactStore.CreateNormalizedPhoneSet(doNotContactEntries);

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);
            var headerRow = worksheet.FirstRowUsed();
            if (headerRow == null)
            {
                return new ExcelPreviewReadResult { Rows = rows };
            }

            var headerMap = BuildHeaderMap(headerRow);
            int optInColumnIndex = FindColumnIndex(headerMap, "optin");
            int optInSourceColumnIndex = FindColumnIndex(headerMap, "optinsource");
            int optInAtColumnIndex = FindColumnIndex(headerMap, "optinat");

            foreach (var row in worksheet.RowsUsed().Where(item => item.RowNumber() > headerRow.RowNumber()))
            {
                string countryCode = row.Cell(1).GetString().Trim();
                string phone = row.Cell(2).GetString().Trim().Replace(" ", "");
                string message = row.Cell(3).GetString().Trim();
                string normalizedPhone = DoNotContactEntry.NormalizePhone(countryCode, phone);
                bool toAudio = ParseToAudio(row.Cell(4));
                bool? optIn = null;
                string optInSource = GetOptionalCellString(row, optInSourceColumnIndex);
                DateTime? optInAt = null;

                var validationIssues = new List<string>();
                if (string.IsNullOrWhiteSpace(phone))
                {
                    validationIssues.Add("Falta teléfono");
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    validationIssues.Add("Falta mensaje");
                }

                if (optInColumnIndex >= 0 && !TryParseOptIn(row.Cell(optInColumnIndex), out optIn))
                {
                    validationIssues.Add("optIn inválido (usa true, false, 1 o 0)");
                }

                if (optIn == true)
                {
                    if (optInSourceColumnIndex < 0)
                    {
                        validationIssues.Add("Falta columna optInSource");
                    }
                    else if (string.IsNullOrWhiteSpace(optInSource))
                    {
                        validationIssues.Add("Falta optInSource");
                    }
                }

                if (!TryParseOptionalDate(GetOptionalCell(row, optInAtColumnIndex), out optInAt))
                {
                    validationIssues.Add("optInAt inválida");
                }

                bool hasInvalidData = validationIssues.Count > 0;
                bool isBlockedByDoNotContact = !hasInvalidData
                    && !string.IsNullOrWhiteSpace(normalizedPhone)
                    && blockedPhones.Contains(normalizedPhone);
                bool lacksExplicitOptIn = !hasInvalidData
                    && !isBlockedByDoNotContact
                    && optIn != true;
                bool isSendable = !hasInvalidData
                    && !isBlockedByDoNotContact
                    && !lacksExplicitOptIn;

                rows.Add(new ExcelMessagePreviewRow
                {
                    RowNumber = row.RowNumber(),
                    CountryCode = countryCode,
                    Phone = phone,
                    Message = message,
                    ToAudio = toAudio,
                    OptIn = optIn,
                    OptInSource = optInSource,
                    OptInAt = optInAt,
                    HasInvalidData = hasInvalidData,
                    IsBlockedByDoNotContact = isBlockedByDoNotContact,
                    LacksExplicitOptIn = lacksExplicitOptIn,
                    IsSendable = isSendable,
                    ValidationStatus = hasInvalidData
                        ? string.Join(", ", validationIssues)
                        : isBlockedByDoNotContact
                            ? "Bloqueado por lista no contactar"
                        : GetConsentStatus(optInColumnIndex, optIn)
                });
            }

            return new ExcelPreviewReadResult
            {
                Rows = rows,
                MissingConsentColumns = GetMissingConsentColumns(
                    optInColumnIndex,
                    optInSourceColumnIndex,
                    optInAtColumnIndex)
            };
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

            if (bool.TryParse(cell.GetString().Trim().ToLowerInvariant(), out bool result))
            {
                return result;
            }

            if (int.TryParse(cell.GetString().Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int intResult))
            {
                return intResult != 0;
            }

            return toAudio;
        }

        private static bool TryParseOptIn(IXLCell cell, out bool? optIn)
        {
            optIn = null;

            if (cell.IsEmpty())
            {
                return true;
            }

            if (cell.DataType == XLDataType.Boolean)
            {
                optIn = cell.GetBoolean();
                return true;
            }

            string rawValue = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (bool.TryParse(rawValue, out bool boolValue))
            {
                optIn = boolValue;
                return true;
            }

            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numericValue)
                && (numericValue == 0 || numericValue == 1))
            {
                optIn = numericValue == 1;
                return true;
            }

            return false;
        }

        private static bool TryParseOptionalDate(IXLCell? cell, out DateTime? dateValue)
        {
            dateValue = null;

            if (cell == null || cell.IsEmpty())
            {
                return true;
            }

            if (cell.TryGetValue(out DateTime typedDate))
            {
                dateValue = typedDate;
                return true;
            }

            string rawValue = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (DateTime.TryParse(rawValue, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime parsedDate)
                || DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsedDate))
            {
                dateValue = parsedDate;
                return true;
            }

            return false;
        }

        private static Dictionary<string, int> BuildHeaderMap(IXLRow headerRow)
        {
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var cell in headerRow.CellsUsed())
            {
                string normalizedHeader = NormalizeHeader(cell.GetString());
                if (string.IsNullOrWhiteSpace(normalizedHeader) || headerMap.ContainsKey(normalizedHeader))
                {
                    continue;
                }

                headerMap[normalizedHeader] = cell.Address.ColumnNumber;
            }

            return headerMap;
        }

        private static int FindColumnIndex(IReadOnlyDictionary<string, int> headerMap, string normalizedHeader)
        {
            return headerMap.TryGetValue(normalizedHeader, out int columnIndex)
                ? columnIndex
                : -1;
        }

        private static IReadOnlyList<string> GetMissingConsentColumns(
            int optInColumnIndex,
            int optInSourceColumnIndex,
            int optInAtColumnIndex)
        {
            var missingColumns = new List<string>();

            if (optInColumnIndex < 0)
            {
                missingColumns.Add("optIn");
            }

            if (optInSourceColumnIndex < 0)
            {
                missingColumns.Add("optInSource");
            }

            if (optInAtColumnIndex < 0)
            {
                missingColumns.Add("optInAt");
            }

            return missingColumns;
        }

        private static string GetConsentStatus(int optInColumnIndex, bool? optIn)
        {
            if (optIn == true)
            {
                return "Enviable";
            }

            if (optInColumnIndex < 0)
            {
                return "Sin opt-in explícito (falta columna optIn)";
            }

            return optIn == false
                ? "Sin opt-in explícito"
                : "Falta opt-in explícito";
        }

        private static string GetOptionalCellString(IXLRow row, int columnIndex)
        {
            return columnIndex > 0
                ? row.Cell(columnIndex).GetString().Trim()
                : "";
        }

        private static IXLCell? GetOptionalCell(IXLRow row, int columnIndex)
        {
            return columnIndex > 0 ? row.Cell(columnIndex) : null;
        }

        private static string NormalizeHeader(string value)
        {
            return string.Concat(value
                .Trim()
                .Where(char.IsLetterOrDigit))
                .ToLowerInvariant();
        }
    }
}
