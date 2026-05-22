namespace Automate_Whatsapp.Logic;

public sealed record OutboundMessage(
    string CountryCode,
    string Phone,
    string Message,
    bool ToAudio,
    bool HasExplicitOptIn = false,
    string OptInSource = "",
    DateTime? OptInAt = null)
{
    public string FullPhone => string.Concat(CountryCode, Phone);
}
