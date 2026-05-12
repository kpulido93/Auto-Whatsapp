namespace Automate_Whatsapp.Logic;

public sealed record WhatsAppSendResult(
    bool Success,
    WhatsAppHealthStatus Status,
    string Message,
    bool IsGlobalFailure,
    Exception? Exception = null)
{
    public static WhatsAppSendResult Ok(string message = "Mensaje enviado correctamente.") =>
        new(true, WhatsAppHealthStatus.Ready, message, false);

    public static WhatsAppSendResult Failure(
        WhatsAppHealthStatus status,
        string message,
        bool isGlobalFailure,
        Exception? exception = null) =>
        new(false, status, message, isGlobalFailure, exception);

    public static WhatsAppSendResult FromIssue(WhatsAppHealthIssue issue, Exception? exception = null) =>
        Failure(issue.Status, issue.Message, issue.IsGlobalFailure, exception ?? issue.Exception);
}
