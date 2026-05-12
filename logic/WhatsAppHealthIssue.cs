namespace Automate_Whatsapp.Logic;

public sealed record WhatsAppHealthIssue(
    WhatsAppHealthStatus Status,
    string Message,
    bool IsGlobalFailure,
    Exception? Exception = null)
{
    public bool IsReady => Status == WhatsAppHealthStatus.Ready;
}
