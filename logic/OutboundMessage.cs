namespace Automate_Whatsapp.Logic;

public sealed record OutboundMessage
{
    public OutboundMessage(
        string countryCode,
        string phone,
        string message,
        bool toAudio)
        : this(countryCode, phone, message, toAudio, false, "", null, "", null, "")
    {
    }

    public OutboundMessage(
        string countryCode,
        string phone,
        string message,
        bool toAudio,
        bool hasExplicitOptIn,
        string? optInSource,
        DateTime? optInAt)
        : this(countryCode, phone, message, toAudio, hasExplicitOptIn, optInSource, optInAt, "", null, "")
    {
    }

    public OutboundMessage(
        string countryCode,
        string phone,
        string message,
        bool toAudio,
        string? name,
        int? templateNumber,
        string? bank)
        : this(countryCode, phone, message, toAudio, false, "", null, name, templateNumber, bank)
    {
    }

    public OutboundMessage(
        string countryCode,
        string phone,
        string message,
        bool toAudio,
        bool hasExplicitOptIn,
        string? optInSource,
        DateTime? optInAt,
        string? name,
        int? templateNumber,
        string? bank)
    {
        CountryCode = countryCode;
        Phone = phone;
        Message = message;
        ToAudio = toAudio;
        HasExplicitOptIn = hasExplicitOptIn;
        OptInSource = optInSource ?? string.Empty;
        OptInAt = optInAt;
        Name = name ?? string.Empty;
        TemplateNumber = templateNumber;
        Bank = bank ?? string.Empty;
    }

    public string CountryCode { get; init; }

    public string Phone { get; init; }

    public string Message { get; init; }

    public bool ToAudio { get; init; }

    public bool HasExplicitOptIn { get; init; }

    public string OptInSource { get; init; }

    public DateTime? OptInAt { get; init; }

    public string Name { get; init; }

    public int? TemplateNumber { get; init; }

    public string Bank { get; init; }

    public string FullPhone => string.Concat(CountryCode, Phone);
}
