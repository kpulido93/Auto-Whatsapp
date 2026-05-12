namespace Automate_Whatsapp.Logic;

public sealed record WhatsAppSendProgress(
    int Processed,
    int Total,
    int Successes,
    int Errors,
    int Skipped);
