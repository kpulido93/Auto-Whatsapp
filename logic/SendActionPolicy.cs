namespace Automate_Whatsapp.Logic;

public enum SendActionKind
{
    SendNow,
    Schedule
}

public enum SendActionBlockSeverity
{
    Information,
    Warning
}

public enum SendActionBlockReason
{
    None,
    OperationInProgress,
    LinePreparationInProgress,
    ScheduleAlreadyActive,
    InvalidScheduleTime,
    MissingExcel,
    ExcelLoading,
    NoValidRows,
    NoAvailableLines,
    MissingSelectedLine,
    NoSelectedRunLines,
    NoEnabledSelectedRunLines,
    SelectedLineNotReady
}

public sealed record SendActionButtonState(
    bool IsSending,
    bool IsScheduled,
    bool IsPreparingLines);

public sealed record SendActionPolicyInput
{
    public SendActionKind Action { get; init; }
    public string FailureContext { get; init; } = "";
    public bool HasExcel { get; init; }
    public int ValidRowCount { get; init; }
    public int AvailableLineCount { get; init; }
    public bool HasSelectedLine { get; init; }
    public int SelectedRunLineCount { get; init; }
    public int EnabledSelectedRunLineCount { get; init; }
    public WhatsAppLineOperationalState SelectedLineState { get; init; } = WhatsAppLineOperationalState.Unknown;
    public string SelectedLineName { get; init; } = "";
    public bool IsSending { get; init; }
    public bool IsScheduled { get; init; }
    public bool IsPreparingLines { get; init; }
    public bool IsLoadingExcelPreview { get; init; }
    public DateTime? ScheduledTime { get; init; }
    public DateTime Now { get; init; } = DateTime.Now;
}

public sealed record SendActionPolicyResult(
    SendActionBlockReason Reason,
    string Title,
    string UserMessage,
    string LogMessage,
    SendActionBlockSeverity Severity)
{
    public bool CanProceed => Reason == SendActionBlockReason.None;

    public static SendActionPolicyResult Ready { get; } = new(
        SendActionBlockReason.None,
        "",
        "",
        "",
        SendActionBlockSeverity.Information);
}

public static class SendActionPolicy
{
    public static bool CanRequestSend(SendActionButtonState state)
    {
        return !state.IsSending && !state.IsScheduled && !state.IsPreparingLines;
    }

    public static SendActionPolicyResult Evaluate(SendActionPolicyInput input)
    {
        string context = GetFailureContext(input);
        string actionTitle = input.Action == SendActionKind.Schedule
            ? "Programación no disponible"
            : "Enviar ahora";

        if (input.IsSending)
        {
            return Block(
                SendActionBlockReason.OperationInProgress,
                actionTitle,
                "Ya hay un envío en curso. Espera a que finalice o cancélalo antes de iniciar otro.",
                $"{context} porque ya hay un envío en curso.",
                SendActionBlockSeverity.Information);
        }

        if (input.IsPreparingLines)
        {
            return Block(
                SendActionBlockReason.LinePreparationInProgress,
                actionTitle,
                "Hay una preparación de líneas en curso. Espera a que termine antes de iniciar el envío.",
                $"{context} porque hay una preparación de líneas en curso.",
                SendActionBlockSeverity.Information);
        }

        if (input.IsScheduled)
        {
            string userMessage = input.Action == SendActionKind.Schedule
                ? "Ya hay un envío programado. Cancela la programación antes de crear otra."
                : "Ya hay un envío programado. Cancela la programación antes de iniciar un envío inmediato.";

            return Block(
                SendActionBlockReason.ScheduleAlreadyActive,
                actionTitle,
                userMessage,
                $"{context} porque hay una programación activa.",
                SendActionBlockSeverity.Information);
        }

        if (input.Action == SendActionKind.Schedule
            && (!input.ScheduledTime.HasValue || input.ScheduledTime.Value <= input.Now))
        {
            string scheduledText = input.ScheduledTime.HasValue
                ? input.ScheduledTime.Value.ToString("dd/MM/yyyy HH:mm")
                : "sin fecha";

            return Block(
                SendActionBlockReason.InvalidScheduleTime,
                "Hora no válida",
                "Selecciona una fecha y hora futura para programar el envío.",
                $"{context} porque la fecha seleccionada no es futura: {scheduledText}.",
                SendActionBlockSeverity.Warning);
        }

        if (!input.HasExcel)
        {
            return Block(
                SendActionBlockReason.MissingExcel,
                "Excel requerido",
                "Seleccione un archivo Excel primero.",
                $"{context}: no hay archivo Excel seleccionado.",
                SendActionBlockSeverity.Warning);
        }

        if (input.IsLoadingExcelPreview)
        {
            return Block(
                SendActionBlockReason.ExcelLoading,
                "Excel en análisis",
                "Espera a que termine el análisis del Excel antes de enviar.",
                $"{context}: el Excel todavía se está analizando.",
                SendActionBlockSeverity.Information);
        }

        if (input.ValidRowCount <= 0)
        {
            return Block(
                SendActionBlockReason.NoValidRows,
                "Excel sin filas válidas",
                "No hay filas válidas para enviar. Corrige teléfono y mensaje en el Excel.",
                $"{context}: no hay filas válidas en el Excel.",
                SendActionBlockSeverity.Warning);
        }

        if (input.AvailableLineCount <= 0)
        {
            return Block(
                SendActionBlockReason.NoAvailableLines,
                "Líneas no disponibles",
                "No hay líneas de WhatsApp disponibles. Configura al menos una línea antes de enviar.",
                $"{context}: no hay líneas de WhatsApp disponibles.",
                SendActionBlockSeverity.Warning);
        }

        if (!input.HasSelectedLine)
        {
            return Block(
                SendActionBlockReason.MissingSelectedLine,
                "Línea requerida",
                "Selecciona una línea de WhatsApp antes de enviar.",
                $"{context}: no hay una línea principal seleccionada.",
                SendActionBlockSeverity.Warning);
        }

        if (input.SelectedRunLineCount <= 0)
        {
            return Block(
                SendActionBlockReason.NoSelectedRunLines,
                "Líneas de corrida requeridas",
                "Selecciona al menos una línea para esta corrida antes de enviar.",
                $"{context}: no hay líneas seleccionadas para esta corrida.",
                SendActionBlockSeverity.Warning);
        }

        if (input.EnabledSelectedRunLineCount <= 0)
        {
            return Block(
                SendActionBlockReason.NoEnabledSelectedRunLines,
                "Líneas no disponibles",
                "Las líneas seleccionadas ya no están habilitadas. Selecciona al menos una línea habilitada para esta corrida.",
                $"{context}: las líneas seleccionadas no están habilitadas.",
                SendActionBlockSeverity.Warning);
        }

        if (input.SelectedLineState != WhatsAppLineOperationalState.Ready)
        {
            string lineName = GetLineName(input);
            string guidance = input.SelectedLineState switch
            {
                WhatsAppLineOperationalState.RequiresManualAuth =>
                    $"La línea {lineName} requiere iniciar sesión. Pulsa \"Preparar línea\", escanea el QR en Chrome y vuelve a intentar.",
                WhatsAppLineOperationalState.NotAvailable =>
                    $"La línea {lineName} no está disponible. Revisa Chrome/WhatsApp Web, pulsa \"Preparar línea\" y vuelve a intentar.",
                _ =>
                    $"La línea {lineName} aún no fue verificada. Pulsa \"Preparar línea\" antes de enviar."
            };

            string stateText = input.SelectedLineState switch
            {
                WhatsAppLineOperationalState.Ready => "Lista",
                WhatsAppLineOperationalState.RequiresManualAuth => "Requiere QR",
                WhatsAppLineOperationalState.NotAvailable => "No disponible",
                _ => "Sin verificar"
            };

            return Block(
                SendActionBlockReason.SelectedLineNotReady,
                "Línea no preparada",
                $"{guidance}\n\nEstado actual: {stateText}.",
                $"{context}: la línea {lineName} no está lista. Estado: {stateText}.",
                SendActionBlockSeverity.Warning);
        }

        return SendActionPolicyResult.Ready;
    }

    private static SendActionPolicyResult Block(
        SendActionBlockReason reason,
        string title,
        string userMessage,
        string logMessage,
        SendActionBlockSeverity severity)
    {
        return new SendActionPolicyResult(reason, title, userMessage, logMessage, severity);
    }

    private static string GetFailureContext(SendActionPolicyInput input)
    {
        if (!string.IsNullOrWhiteSpace(input.FailureContext))
        {
            return input.FailureContext.Trim();
        }

        return input.Action == SendActionKind.Schedule
            ? "No se programó el envío"
            : "No se inició el envío inmediato";
    }

    private static string GetLineName(SendActionPolicyInput input)
    {
        return string.IsNullOrWhiteSpace(input.SelectedLineName)
            ? "seleccionada"
            : input.SelectedLineName.Trim();
    }
}
