namespace Automate_Whatsapp.Logic;

public sealed record WhatsAppMessageResult
{
    public WhatsAppMessageResult(
        string fullPhone,
        bool success,
        bool toAudio,
        WhatsAppHealthStatus status,
        string message,
        string lineName)
        : this(
            fullPhone,
            success,
            toAudio,
            status,
            message,
            lineName,
            "",
            BuildMessageState(success, status, message),
            BuildHasWhatsApp(success, status))
    {
    }

    public WhatsAppMessageResult(
        string fullPhone,
        bool success,
        bool toAudio,
        WhatsAppHealthStatus status,
        string message,
        string lineName,
        string name,
        string messageState,
        string hasWhatsApp)
    {
        FullPhone = fullPhone ?? string.Empty;
        Success = success;
        ToAudio = toAudio;
        Status = status;
        Message = message ?? string.Empty;
        LineName = lineName ?? string.Empty;
        Name = name ?? string.Empty;
        MessageState = string.IsNullOrWhiteSpace(messageState)
            ? BuildMessageState(success, status, Message)
            : messageState;
        HasWhatsApp = string.IsNullOrWhiteSpace(hasWhatsApp)
            ? BuildHasWhatsApp(success, status)
            : hasWhatsApp;
    }

    public string FullPhone { get; init; }

    public bool Success { get; init; }

    public bool ToAudio { get; init; }

    public WhatsAppHealthStatus Status { get; init; }

    public string Message { get; init; }

    public string LineName { get; init; }

    public string Number => FullPhone;

    public string Name { get; init; }

    public string LineaWP => LineName;

    public string MessageState { get; init; }

    public string HasWhatsApp { get; init; }

    public static WhatsAppMessageResult Create(
        OutboundMessage outboundMessage,
        bool success,
        WhatsAppSendResult result,
        string lineName)
    {
        ArgumentNullException.ThrowIfNull(outboundMessage);
        ArgumentNullException.ThrowIfNull(result);

        return new WhatsAppMessageResult(
            outboundMessage.FullPhone,
            success,
            outboundMessage.ToAudio,
            result.Status,
            result.Message,
            lineName,
            ResolveName(outboundMessage),
            BuildMessageState(success, result.Status, result.Message),
            BuildHasWhatsApp(success, result.Status));
    }

    private static string ResolveName(OutboundMessage outboundMessage)
    {
        return string.IsNullOrWhiteSpace(outboundMessage.Name)
            ? string.Empty
            : outboundMessage.Name.Trim();
    }

    private static string BuildMessageState(
        bool success,
        WhatsAppHealthStatus status,
        string message)
    {
        if (success)
        {
            return "Enviado";
        }

        if (status == WhatsAppHealthStatus.InvalidDestinationNumber)
        {
            return "No enviado - número inválido";
        }

        return $"No enviado - {status}: {message}";
    }

    private static string BuildHasWhatsApp(bool success, WhatsAppHealthStatus status)
    {
        if (success)
        {
            return "Sí";
        }

        if (status == WhatsAppHealthStatus.InvalidDestinationNumber)
        {
            return "No";
        }

        return "Desconocido";
    }
}
