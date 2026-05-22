using System.Globalization;
using System.Text;
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
        public int? TemplateNumber { get; init; }
        public string Bank { get; init; } = "";
        public string DebtorName { get; init; } = "";
        public string ResolvedMessage { get; init; } = "";
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
        private const string ValidRowStatus = "Válida";
        private const string InvalidToAudioMessage = "toAudio inválido. Usa true, false, 1 o 0.";
        private const string InvalidOptInMessage = "optIn inválido. Usa true, false, 1 o 0.";
        private const string MissingOptInSourceMessage = "Falta optInSource";
        private const string InvalidOptInAtMessage = "optInAt inválido. Usa una fecha válida.";
        private const string InvalidTemplateNumberMessage = "Plantilla inválida. Usa un número entero.";
        private const string BlockedByDoNotContactMessage = "Bloqueado por lista no contactar";

        private static readonly string CountryCodeHeader = NormalizeHeader("Código país");
        private static readonly string PhoneHeader = NormalizeHeader("Teléfono");
        private static readonly string MessageHeader = NormalizeHeader("Mensaje");
        private static readonly string TemplateHeader = NormalizeHeader("Plantilla");
        private static readonly string BankHeader = NormalizeHeader("Banco");
        private static readonly string DebtorNameHeader = NormalizeHeader("Nombre deudor");
        private static readonly string ToAudioHeader = NormalizeHeader("toAudio");
        private static readonly string OptInHeader = NormalizeHeader("optIn");
        private static readonly string OptInSourceHeader = NormalizeHeader("optInSource");
        private static readonly string OptInAtHeader = NormalizeHeader("optInAt");

        public static List<(string countrycode, string phone, string message, bool toAudio)> ReadMessages(
            string filePath,
            IReadOnlyCollection<DoNotContactEntry>? doNotContactEntries = null)
        {
            return ReadPreviewResult(filePath, doNotContactEntries).Rows
                .Where(row => row.IsSendable)
                .Select(row => (row.CountryCode, row.Phone, row.Message, row.ToAudio))
                .ToList();
        }

        public static List<(string countrycode, string phone, string message, bool toAudio)> ReadMessages(
            string filePath,
            string? messageTemplateConfigPath)
        {
            return ReadPreviewResult(filePath, null, messageTemplateConfigPath).Rows
                .Where(row => row.IsSendable)
                .Select(row => (row.CountryCode, row.Phone, row.Message, row.ToAudio))
                .ToList();
        }

        public static List<OutboundMessage> ReadOutboundMessages(
            string filePath,
            IReadOnlyCollection<DoNotContactEntry>? doNotContactEntries = null,
            string? messageTemplateConfigPath = null)
        {
            return ReadPreviewResult(filePath, doNotContactEntries, messageTemplateConfigPath)
                .Rows
                .Where(row => row.IsSendable)
                .Select(row => new OutboundMessage(
                    row.CountryCode,
                    row.Phone,
                    row.Message,
                    row.ToAudio,
                    true,
                    row.OptInSource,
                    row.OptInAt,
                    row.DebtorName,
                    row.TemplateNumber,
                    row.Bank))
                .ToList();
        }

        public static List<ExcelMessagePreviewRow> ReadPreview(
            string filePath,
            IReadOnlyCollection<DoNotContactEntry>? doNotContactEntries = null)
        {
            return ReadPreviewResult(filePath, doNotContactEntries).Rows;
        }

        public static List<ExcelMessagePreviewRow> ReadPreview(
            string filePath,
            string? messageTemplateConfigPath)
        {
            return ReadPreviewResult(filePath, null, messageTemplateConfigPath).Rows;
        }

        public static ExcelPreviewReadResult ReadPreviewResult(
            string filePath,
            IReadOnlyCollection<DoNotContactEntry>? doNotContactEntries = null,
            string? messageTemplateConfigPath = null)
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

            Dictionary<string, int> headerIndexes = BuildHeaderIndexes(headerRow);
            Dictionary<int, MessageTemplate> templatesByNumber = LoadTemplatesByNumber(messageTemplateConfigPath);

            bool hasOptInColumn = headerIndexes.ContainsKey(OptInHeader);
            bool hasOptInSourceColumn = headerIndexes.ContainsKey(OptInSourceHeader);
            bool hasOptInAtColumn = headerIndexes.ContainsKey(OptInAtHeader);

            foreach (var row in worksheet.RowsUsed().Where(currentRow => currentRow.RowNumber() > headerRow.RowNumber()))
            {
                string countryCode = GetCellString(row, headerIndexes, CountryCodeHeader);
                string phone = GetCellString(row, headerIndexes, PhoneHeader).Replace(" ", "");
                string legacyMessage = GetCellString(row, headerIndexes, MessageHeader);
                string bank = GetCellString(row, headerIndexes, BankHeader);
                string debtorName = GetCellString(row, headerIndexes, DebtorNameHeader);
                string normalizedPhone = DoNotContactEntry.NormalizePhone(countryCode, phone);

                var validationIssues = new List<string>();

                if (string.IsNullOrWhiteSpace(phone))
                {
                    validationIssues.Add("Falta teléfono");
                }

                bool toAudio = false;
                if (!TryParseOptionalBooleanLike(GetCell(row, headerIndexes, ToAudioHeader), out toAudio))
                {
                    validationIssues.Add(InvalidToAudioMessage);
                }

                bool? optIn = null;
                if (hasOptInColumn && !TryParseOptIn(GetCell(row, headerIndexes, OptInHeader), out optIn))
                {
                    validationIssues.Add(InvalidOptInMessage);
                }

                string optInSource = GetCellString(row, headerIndexes, OptInSourceHeader);
                if (optIn == true)
                {
                    if (!hasOptInSourceColumn)
                    {
                        validationIssues.Add("Falta columna optInSource");
                    }
                    else if (string.IsNullOrWhiteSpace(optInSource))
                    {
                        validationIssues.Add(MissingOptInSourceMessage);
                    }
                }

                DateTime? optInAt = null;
                if (!TryParseOptionalDate(GetCell(row, headerIndexes, OptInAtHeader), out optInAt))
                {
                    validationIssues.Add(InvalidOptInAtMessage);
                }

                int? templateNumber = null;
                string resolvedMessage = legacyMessage;
                string templateValue = GetCellString(row, headerIndexes, TemplateHeader);

                if (!string.IsNullOrWhiteSpace(templateValue))
                {
                    if (!TryParseTemplateNumber(templateValue, out templateNumber))
                    {
                        validationIssues.Add(InvalidTemplateNumberMessage);
                        resolvedMessage = string.Empty;
                    }
                    else if (!templatesByNumber.TryGetValue(templateNumber.Value, out var template))
                    {
                        validationIssues.Add($"La plantilla {templateNumber.Value} no existe o está deshabilitada.");
                        resolvedMessage = string.Empty;
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(bank))
                        {
                            validationIssues.Add("Falta Banco");
                        }

                        if (string.IsNullOrWhiteSpace(debtorName))
                        {
                            validationIssues.Add("Falta Nombre deudor");
                        }

                        resolvedMessage = MessageTemplateRenderer.Render(template.Body, bank, debtorName);
                    }
                }
                else if (string.IsNullOrWhiteSpace(legacyMessage))
                {
                    validationIssues.Add("Falta mensaje");
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
                    Message = resolvedMessage,
                    ToAudio = toAudio,
                    OptIn = optIn,
                    OptInSource = optInSource,
                    OptInAt = optInAt,
                    TemplateNumber = templateNumber,
                    Bank = bank,
                    DebtorName = debtorName,
                    ResolvedMessage = resolvedMessage,
                    HasInvalidData = hasInvalidData,
                    IsBlockedByDoNotContact = isBlockedByDoNotContact,
                    LacksExplicitOptIn = lacksExplicitOptIn,
                    IsSendable = isSendable,
                    ValidationStatus = hasInvalidData
                        ? string.Join(", ", validationIssues)
                        : isBlockedByDoNotContact
                            ? BlockedByDoNotContactMessage
                            : GetConsentStatus(hasOptInColumn, optIn)
                });
            }

            return new ExcelPreviewReadResult
            {
                Rows = rows,
                MissingConsentColumns = GetMissingConsentColumns(
                    hasOptInColumn,
                    hasOptInSourceColumn,
                    hasOptInAtColumn)
            };
        }

        private static Dictionary<string, int> BuildHeaderIndexes(IXLRow headerRow)
        {
            var headerIndexes = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var cell in headerRow.CellsUsed())
            {
                string normalizedHeader = NormalizeHeader(cell.GetString());
                if (string.IsNullOrWhiteSpace(normalizedHeader) || headerIndexes.ContainsKey(normalizedHeader))
                {
                    continue;
                }

                headerIndexes[normalizedHeader] = cell.Address.ColumnNumber;
            }

            return headerIndexes;
        }

        private static Dictionary<int, MessageTemplate> LoadTemplatesByNumber(string? messageTemplateConfigPath)
        {
            return MessageTemplateStore.LoadOrDefault(messageTemplateConfigPath)
                .Where(template => template.Enabled)
                .GroupBy(template => template.Number)
                .ToDictionary(group => group.Key, group => group.Last());
        }

        private static IXLCell? GetCell(
            IXLRow row,
            IReadOnlyDictionary<string, int> headerIndexes,
            string normalizedHeader)
        {
            return headerIndexes.TryGetValue(normalizedHeader, out int columnNumber)
                ? row.Cell(columnNumber)
                : null;
        }

        private static string GetCellString(
            IXLRow row,
            IReadOnlyDictionary<string, int> headerIndexes,
            string normalizedHeader)
        {
            IXLCell? cell = GetCell(row, headerIndexes, normalizedHeader);
            return cell == null ? string.Empty : cell.GetString().Trim();
        }

        private static bool TryParseOptionalBooleanLike(IXLCell? cell, out bool value)
        {
            value = false;

            if (cell == null || cell.IsEmpty())
            {
                return true;
            }

            if (cell.DataType == XLDataType.Boolean)
            {
                value = cell.GetBoolean();
                return true;
            }

            string rawValue = cell.GetString().Trim();
            if (bool.TryParse(rawValue, out bool parsedBoolean))
            {
                value = parsedBoolean;
                return true;
            }

            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numericValue))
            {
                if (numericValue is 0 or 1)
                {
                    value = numericValue == 1;
                    return true;
                }

                return false;
            }

            return false;
        }

        private static bool TryParseOptIn(IXLCell? cell, out bool? optIn)
        {
            optIn = null;

            if (cell == null || cell.IsEmpty())
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
                && numericValue is 0 or 1)
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

        private static bool TryParseTemplateNumber(string rawValue, out int? templateNumber)
        {
            templateNumber = null;

            if (!int.TryParse(rawValue.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedTemplateNumber))
            {
                return false;
            }

            templateNumber = parsedTemplateNumber;
            return true;
        }

        private static IReadOnlyList<string> GetMissingConsentColumns(
            bool hasOptInColumn,
            bool hasOptInSourceColumn,
            bool hasOptInAtColumn)
        {
            var missingColumns = new List<string>();

            if (!hasOptInColumn)
            {
                missingColumns.Add("optIn");
            }

            if (!hasOptInSourceColumn)
            {
                missingColumns.Add("optInSource");
            }

            if (!hasOptInAtColumn)
            {
                missingColumns.Add("optInAt");
            }

            return missingColumns;
        }

        private static string GetConsentStatus(bool hasOptInColumn, bool? optIn)
        {
            if (optIn == true)
            {
                return ValidRowStatus;
            }

            if (!hasOptInColumn)
            {
                return "Sin opt-in explícito (falta columna optIn)";
            }

            return optIn == false
                ? "Sin opt-in explícito"
                : "Falta opt-in explícito";
        }

        private static string NormalizeHeader(string? header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return string.Empty;
            }

            string normalized = header.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (char character in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString();
        }
    }
}
