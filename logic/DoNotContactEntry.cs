namespace Automate_Whatsapp.Logic;

public sealed record DoNotContactEntry(
    string CountryCode,
    string Phone,
    string Reason = "",
    DateTime? CreatedAt = null)
{
    public string NormalizedPhone => NormalizePhone(CountryCode, Phone);

    public bool Matches(string? countryCode, string? phone)
    {
        string candidate = NormalizePhone(countryCode, phone);
        return !string.IsNullOrWhiteSpace(candidate)
            && string.Equals(NormalizedPhone, candidate, StringComparison.Ordinal);
    }

    public static string NormalizePhone(string? countryCode, string? phone)
    {
        return string.Concat(
            ExtractDigits(countryCode),
            ExtractDigits(phone));
    }

    private static string ExtractDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        return string.Concat(value.Where(char.IsDigit));
    }
}
