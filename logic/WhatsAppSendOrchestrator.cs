namespace Automate_Whatsapp.Logic;

public sealed class WhatsAppSendOrchestrator
{
    private readonly Func<WhatsAppLine, IWhatsAppSender> senderFactory;
    private readonly Func<ElevenLabsTts> ttsFactory;

    private List<WhatsAppLine> whatsAppLines;
    private IWhatsAppSender? whatsSender;
    private WhatsAppLine? currentLine;
    private bool isPaused;
    private bool isCancellationRequested;
    private bool isRunning;

    public WhatsAppSendOrchestrator(
        IEnumerable<WhatsAppLine> whatsAppLines,
        Func<WhatsAppLine, IWhatsAppSender>? senderFactory = null,
        Func<ElevenLabsTts>? ttsFactory = null)
    {
        this.whatsAppLines = whatsAppLines.ToList();
        this.senderFactory = senderFactory ?? (line => new WhatsAppSender(line));
        this.ttsFactory = ttsFactory ?? (() => new ElevenLabsTts());
    }

    public event Action<string>? Log;
    public event Action<WhatsAppSendProgress>? ProgressChanged;
    public event Action<WhatsAppSendOrchestrationState>? StateChanged;
    public event Action<WhatsAppMessageResult>? MessageResult;
    public event Action<WhatsAppLine>? QrInstructionRequired;
    public event Action<WhatsAppLine, string>? SenderOpenFailed;
    public event Action<WhatsAppLine>? LineChanged;

    public bool IsRunning => isRunning;
    public bool IsPaused => isPaused;
    public WhatsAppLine? CurrentLine => currentLine;

    public void UpdateLines(IEnumerable<WhatsAppLine> lines)
    {
        whatsAppLines = lines.ToList();

        if (currentLine != null)
        {
            currentLine = whatsAppLines.FirstOrDefault(line =>
                string.Equals(line.Id, currentLine.Id, StringComparison.OrdinalIgnoreCase)) ?? currentLine;
        }
    }

    public void SetSelectedLine(WhatsAppLine? selectedLine)
    {
        if (selectedLine == null || currentLine?.Id == selectedLine.Id)
        {
            return;
        }

        bool hadActiveSender = whatsSender != null;
        CloseCurrentSender();

        EmitLog(hadActiveSender
            ? $"Línea cambiada a {selectedLine.DisplayName}. Se cerró el navegador anterior."
            : $"Línea seleccionada: {selectedLine.DisplayName}");
    }

    public void Pause()
    {
        if (!isRunning || isPaused)
        {
            return;
        }

        isPaused = true;
        EmitState(WhatsAppSendOrchestrationState.Paused);
        EmitLog("Temporizador pausado.");
    }

    public void Resume()
    {
        if (!isRunning || !isPaused)
        {
            return;
        }

        isPaused = false;
        EmitState(WhatsAppSendOrchestrationState.Sending);
        EmitLog("Temporizador reanudado.");
    }

    public void Cancel()
    {
        if (!isRunning)
        {
            return;
        }

        isCancellationRequested = true;
        isPaused = false;
        EmitState(WhatsAppSendOrchestrationState.Cancelled);
        EmitLog("Envío cancelado.");
    }

    public void CloseCurrentSender()
    {
        if (whatsSender == null)
        {
            return;
        }

        whatsSender.Close();
        whatsSender = null;
        currentLine = null;
    }

    public WhatsAppLinePreparationResult PrepareLine(WhatsAppLine line, TimeSpan timeout)
    {
        EmitLog($"Preparando línea {line.DisplayName}.");

        var sender = OpenSenderForLine(
            line,
            showQrInstruction: false,
            updateSelection: true,
            showOpenError: false,
            reuseCurrent: true);

        if (sender == null)
        {
            return new WhatsAppLinePreparationResult(
                line,
                WhatsAppLineOperationalState.NotAvailable,
                WhatsAppHealthStatus.BrowserUnavailable,
                "No se pudo abrir Chrome para la línea.");
        }

        var state = sender.GetOperationalState(timeout, out var issue);
        LogDetectedLineState(line, state, issue);

        return new WhatsAppLinePreparationResult(line, state, issue.Status, issue.Message);
    }

    public async Task<WhatsAppSendRunSummary> SendAsync(
        IReadOnlyList<OutboundMessage> messages,
        WhatsAppSendOptions options)
    {
        isRunning = true;
        isPaused = false;
        isCancellationRequested = false;

        int skipped = options.SkippedCount;
        int processed = 0;
        int sent = 0;
        int invalid = 0;
        int failed = 0;
        int lineChanges = 0;
        bool stoppedByGlobalFailure = false;
        bool cancelledByUser = false;
        string globalFailureReason = "";
        string lastLineName = currentLine?.DisplayName ?? "Ninguna";
        var unhealthyLineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var selectedLineIdsForRun = new HashSet<string>(
            options.SelectedLineIdsForRun ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        EmitState(WhatsAppSendOrchestrationState.Sending);
        EmitLog("🚀 Iniciando envío de mensajes...");
        EmitLog($"Mensajes recibidos: {messages.Count}");
        EmitProgress();

        string renamed = "Resources/audio_temp.ogg";
        string audioPath = Path.Combine(Path.GetDirectoryName(renamed)!, $"ptt_{Guid.NewGuid()}.ogg");
        var tts = ttsFactory();
        IWhatsAppSender? sender = GetOrCreateWhatsSender(options.SelectedLine);

        if (sender == null)
        {
            isRunning = false;
            EmitState(WhatsAppSendOrchestrationState.Cancelled);
            return BuildSummary(cancelled: true);
        }

        void EmitProgress()
        {
            ProgressChanged?.Invoke(new WhatsAppSendProgress(
                processed,
                messages.Count,
                sent,
                failed + invalid,
                skipped));
        }

        void MarkProcessed()
        {
            processed++;
            EmitProgress();
        }

        void StopByGlobalFailure(WhatsAppSendResult result, string? extraReason = null)
        {
            if (stoppedByGlobalFailure)
            {
                return;
            }

            stoppedByGlobalFailure = true;
            globalFailureReason = string.IsNullOrWhiteSpace(extraReason)
                ? $"{result.Status}: {result.Message}"
                : $"{result.Status}: {result.Message} {extraReason}";
            isCancellationRequested = true;
        }

        async Task<WhatsAppSendResult> SendWithCurrentLineAsync(string fullPhone, string message, bool toAudio)
        {
            if (sender == null)
            {
                return WhatsAppSendResult.Failure(
                    WhatsAppHealthStatus.BrowserUnavailable,
                    "No hay una línea activa para enviar.",
                    true);
            }

            if (!toAudio)
            {
                return sender.SendMessage(fullPhone, message, false);
            }

            var openChatResult = sender.OpenChat(fullPhone);
            if (!openChatResult.Success)
            {
                return openChatResult;
            }

            var audioPathMsg = Path.Combine(
                Path.GetDirectoryName(audioPath)!,
                $"ptt_{Guid.NewGuid()}.ogg"
            );

            string resultPath = await tts.ConvertToOggAsync(message, audioPathMsg);

            return string.IsNullOrEmpty(resultPath)
                ? WhatsAppSendResult.Failure(
                    WhatsAppHealthStatus.UnknownError,
                    $"No se pudo generar el audio para {fullPhone}.",
                    false)
                : sender.SendAudio(resultPath);
        }

        WhatsAppSendResult CheckCurrentLineHealth()
        {
            if (sender == null)
            {
                return WhatsAppSendResult.Failure(
                    WhatsAppHealthStatus.BrowserUnavailable,
                    "No hay una línea activa para verificar.",
                    true);
            }

            var health = sender.WaitForReady(TimeSpan.FromSeconds(8));
            return health.IsReady
                ? WhatsAppSendResult.Ok("Línea lista.")
                : WhatsAppSendResult.FromIssue(health);
        }

        IWhatsAppSender? TrySwitchToFallbackLine()
        {
            if (!options.AutoFallbackEnabled)
            {
                return null;
            }

            var failedLine = currentLine;
            var candidates = WhatsAppLineManager.GetFallbackCandidates(
                whatsAppLines,
                failedLine,
                unhealthyLineIds,
                requireReady: true)
                .Where(line => selectedLineIdsForRun.Count == 0 || selectedLineIdsForRun.Contains(line.Id))
                .ToList();

            foreach (var candidate in candidates)
            {
                EmitLog($"🔁 Probando cambio automático a la línea {candidate.DisplayName}...");

                var candidateSender = OpenSenderForLine(
                    candidate,
                    showQrInstruction: false,
                    updateSelection: true,
                    showOpenError: false);

                if (candidateSender == null)
                {
                    unhealthyLineIds.Add(candidate.Id);
                    EmitLog($"⚠️ Línea {candidate.DisplayName} no disponible: no se pudo abrir Chrome.");
                    continue;
                }

                var health = candidateSender.WaitForReady(TimeSpan.FromSeconds(12));
                if (health.IsReady)
                {
                    lineChanges++;
                    lastLineName = candidate.DisplayName;
                    EmitLog($"✅ Cambio automático activo. Nueva línea: {candidate.DisplayName}.");
                    return candidateSender;
                }

                unhealthyLineIds.Add(candidate.Id);
                if (health.Status == WhatsAppHealthStatus.LoginRequired)
                {
                    EmitLog($"⚠️ Línea {candidate.DisplayName} requiere preparación manual. No se esperará escaneo QR durante el envío.");
                }
                else
                {
                    EmitLog($"⚠️ Línea {candidate.DisplayName} no disponible para fallback: {health.Status} - {health.Message}");
                }

                CloseCurrentSender();
            }

            return null;
        }

        bool TryHandleGlobalFailure(WhatsAppSendResult result, string fullPhone)
        {
            var failedLine = currentLine;
            string failedLineName = failedLine?.DisplayName ?? "Sin línea activa";
            lastLineName = failedLineName;

            if (failedLine != null)
            {
                unhealthyLineIds.Add(failedLine.Id);
            }

            EmitLog($"⛔ Fallo global en línea {failedLineName}: {result.Status} - {result.Message}");
            if (result.Status == WhatsAppHealthStatus.LoginRequired)
            {
                EmitLog("La línea requiere escaneo manual. El envío automático no esperará autenticación QR.");
            }

            CloseCurrentSender();

            var fallbackSender = TrySwitchToFallbackLine();
            if (fallbackSender == null)
            {
                StopByGlobalFailure(
                    result,
                    options.AutoFallbackEnabled
                        ? $"No hay más líneas saludables disponibles. Contacto pendiente: {fullPhone}."
                        : $"El canal actual no está disponible para continuar. Contacto pendiente: {fullPhone}.");
                return false;
            }

            sender = fallbackSender;
            EmitLog($"🔁 Reintentando {fullPhone} con línea {currentLine?.DisplayName}.");
            return true;
        }

        WhatsAppSendRunSummary BuildSummary(bool cancelled)
        {
            string finalLineName = currentLine?.DisplayName ?? lastLineName;
            return new WhatsAppSendRunSummary(
                processed,
                messages.Count,
                sent,
                invalid,
                failed,
                skipped,
                lineChanges,
                finalLineName,
                cancelled,
                stoppedByGlobalFailure,
                globalFailureReason);
        }

        try
        {
            foreach (var messageItem in messages)
            {
                if (isCancellationRequested)
                {
                    cancelledByUser = true;
                    EmitLog("⚠️ Envío cancelado antes de procesar el mensaje.");
                    break;
                }

                while (isPaused)
                {
                    await Task.Delay(500);
                    if (isCancellationRequested)
                    {
                        cancelledByUser = true;
                        EmitLog("⚠️ Envío cancelado durante la pausa.");
                        break;
                    }
                }

                if (cancelledByUser)
                {
                    break;
                }

                EmitState(WhatsAppSendOrchestrationState.Sending);

                string fullPhone = messageItem.FullPhone;
                bool messageFinished = false;

                while (!messageFinished)
                {
                    var healthResult = CheckCurrentLineHealth();
                    if (!healthResult.Success)
                    {
                        if (healthResult.IsGlobalFailure)
                        {
                            if (!TryHandleGlobalFailure(healthResult, fullPhone))
                            {
                                break;
                            }

                            continue;
                        }

                        failed++;
                        MarkProcessed();
                        EmitLog($"❌ No se pudo enviar a {fullPhone}: {healthResult.Message}");
                        EmitMessageResult(fullPhone, false, messageItem.ToAudio, healthResult, currentLine?.DisplayName ?? lastLineName);
                        messageFinished = true;
                        continue;
                    }

                    EmitLog($"🚀 Enviando mensaje a {fullPhone}");
                    WhatsAppSendResult sendResult = await SendWithCurrentLineAsync(fullPhone, messageItem.Message, messageItem.ToAudio);

                    if (sendResult.Success)
                    {
                        sent++;
                        lastLineName = currentLine?.DisplayName ?? lastLineName;
                        MarkProcessed();
                        EmitLog(messageItem.ToAudio
                            ? $"✅ Mensaje de audio enviado a {fullPhone} con línea {currentLine?.DisplayName}"
                            : $"✅ Mensaje de texto enviado a {fullPhone} con línea {currentLine?.DisplayName}");
                        EmitMessageResult(fullPhone, true, messageItem.ToAudio, sendResult, currentLine?.DisplayName ?? lastLineName);
                        messageFinished = true;
                    }
                    else if (sendResult.IsGlobalFailure)
                    {
                        if (!TryHandleGlobalFailure(sendResult, fullPhone))
                        {
                            break;
                        }
                    }
                    else
                    {
                        lastLineName = currentLine?.DisplayName ?? lastLineName;
                        if (sendResult.Status == WhatsAppHealthStatus.InvalidDestinationNumber)
                        {
                            invalid++;
                        }
                        else
                        {
                            failed++;
                        }

                        MarkProcessed();
                        EmitLog($"❌ No se pudo enviar a {fullPhone} con línea {currentLine?.DisplayName}: {sendResult.Message}");
                        EmitMessageResult(fullPhone, false, messageItem.ToAudio, sendResult, currentLine?.DisplayName ?? lastLineName);
                        messageFinished = true;
                    }
                }

                if (stoppedByGlobalFailure)
                {
                    break;
                }

                await Task.Delay(2000);
            }
        }
        catch (Exception ex)
        {
            stoppedByGlobalFailure = true;
            globalFailureReason = $"UnknownError: {ex.Message}";
            failed++;
            EmitProgress();
            EmitLog($"❌ Error inesperado durante el envío: {ex.Message}");
        }
        finally
        {
            isRunning = false;
            isPaused = false;
            isCancellationRequested = false;
        }

        var summary = BuildSummary(cancelledByUser);
        EmitLog($"Resumen de envío. {summary.DisplaySummary}");

        if (summary.LineChanges > 0)
        {
            EmitLog($"Cambios automáticos de línea durante la corrida: {summary.LineChanges}");
        }

        if (summary.StoppedByGlobalFailure)
        {
            EmitLog($"⛔ Envío detenido. Procesados: {summary.Processed}. Pendientes: {summary.Pending}. Motivo: {summary.GlobalFailureReason}");
        }

        EmitProgress();
        EmitState(summary.Cancelled || summary.StoppedByGlobalFailure
            ? WhatsAppSendOrchestrationState.Cancelled
            : WhatsAppSendOrchestrationState.Finished);
        return summary;
    }

    private IWhatsAppSender? GetOrCreateWhatsSender(WhatsAppLine? selectedLine)
    {
        if (selectedLine == null)
        {
            EmitLog("Selecciona una línea de WhatsApp antes de enviar.");
            return null;
        }

        if (selectedLine.OperationalState != WhatsAppLineOperationalState.Ready)
        {
            EmitLog($"La línea {selectedLine.DisplayName} no está preparada para enviar. Estado: {selectedLine.OperationalState}.");
            return null;
        }

        if (whatsSender != null && currentLine?.Id == selectedLine.Id)
        {
            return whatsSender;
        }

        return OpenSenderForLine(selectedLine, showQrInstruction: false, updateSelection: false, showOpenError: true);
    }

    private IWhatsAppSender? OpenSenderForLine(
        WhatsAppLine line,
        bool showQrInstruction,
        bool updateSelection,
        bool showOpenError,
        bool reuseCurrent = false)
    {
        if (reuseCurrent && whatsSender != null && currentLine?.Id == line.Id)
        {
            EmitLog($"Usando Chrome ya abierto para la línea {line.DisplayName}.");
            return whatsSender;
        }

        CloseCurrentSender();

        try
        {
            EmitLog($"Abriendo Chrome para la línea {line.DisplayName}...");
            whatsSender = senderFactory(line);
            currentLine = line;

            if (updateSelection)
            {
                LineChanged?.Invoke(line);
            }

            if (showQrInstruction)
            {
                EmitLog("Escanea el QR en la ventana de Chrome y luego continúa.");
                QrInstructionRequired?.Invoke(line);
            }

            return whatsSender;
        }
        catch (Exception ex)
        {
            EmitLog($"❌ No se pudo abrir Chrome para {line.DisplayName}: {ex.Message}");
            if (showOpenError)
            {
                SenderOpenFailed?.Invoke(line, ex.Message);
            }

            whatsSender = null;
            currentLine = null;
            return null;
        }
    }

    private void LogDetectedLineState(
        WhatsAppLine line,
        WhatsAppLineOperationalState state,
        WhatsAppHealthIssue issue)
    {
        EmitLog($"Estado detectado para {line.DisplayName}: {state} ({issue.Status}) - {issue.Message}");

        switch (state)
        {
            case WhatsAppLineOperationalState.Ready:
                EmitLog($"✅ Línea {line.DisplayName} lista para operar.");
                break;
            case WhatsAppLineOperationalState.RequiresManualAuth:
                EmitLog($"⚠️ Línea {line.DisplayName} requiere escaneo manual de QR.");
                break;
            case WhatsAppLineOperationalState.NotAvailable:
                EmitLog($"⚠️ Línea {line.DisplayName} no disponible: {issue.Message}");
                break;
            default:
                EmitLog($"ℹ️ Línea {line.DisplayName} queda sin verificar.");
                break;
        }
    }

    private void EmitLog(string message)
    {
        Log?.Invoke(message);
    }

    private void EmitState(WhatsAppSendOrchestrationState state)
    {
        StateChanged?.Invoke(state);
    }

    private void EmitMessageResult(
        string fullPhone,
        bool success,
        bool toAudio,
        WhatsAppSendResult result,
        string lineName)
    {
        MessageResult?.Invoke(new WhatsAppMessageResult(
            fullPhone,
            success,
            toAudio,
            result.Status,
            result.Message,
            lineName));
    }
}
