namespace Automate_Whatsapp.Logic;

public sealed record OutboundMessage(
    string CountryCode,
    string Phone,
    string Message,
    bool ToAudio)
{
    public string FullPhone => string.Concat(CountryCode, Phone);
}
