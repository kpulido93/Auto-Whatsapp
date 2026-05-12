namespace Automate_Whatsapp.Logic;

public sealed record WhatsAppMessageResult(
    string FullPhone,
    bool Success,
    bool ToAudio,
    WhatsAppHealthStatus Status,
    string Message,
    string LineName);
