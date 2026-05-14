using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class AudioSendDiagnosticsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SendAsync_WhenTtsReturnsNoPath_LogsElevenLabsFailureAndDoesNotAttach(string? ttsPath)
    {
        var tts = new FakeTts(ttsPath);
        var sender = new FakeWhatsAppSender();
        var context = CreateContext(sender, tts);

        var summary = await context.Orchestrator.SendAsync(
            new[] { new OutboundMessage("57", "3001112233", "audio", true) },
            new WhatsAppSendOptions(context.Line, AutoFallbackEnabled: false, SkippedCount: 0));

        Assert.Equal(1, summary.Failed);
        Assert.Equal(0, sender.SendAudioCalls);
        Assert.Equal(1, tts.ConvertCalls);
        Assert.Single(context.MessageResults);
        Assert.Equal(WhatsAppHealthStatus.TextToSpeechFailed, context.MessageResults[0].Status);
        Assert.Contains(context.Logs, log => log.Contains("Generando audio para 573001112233", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(context.Logs, log => log.Contains("ElevenLabs no devolvio una ruta de audio", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SendAsync_WhenTtsReturnsMissingFile_ReportsGeneratedAudioFileFailure()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ogg");
        var tts = new FakeTts(missingPath);
        var sender = new FakeWhatsAppSender();
        var context = CreateContext(sender, tts);

        var summary = await context.Orchestrator.SendAsync(
            new[] { new OutboundMessage("57", "3001112233", "audio", true) },
            new WhatsAppSendOptions(context.Line, AutoFallbackEnabled: false, SkippedCount: 0));

        Assert.Equal(1, summary.Failed);
        Assert.Equal(0, sender.SendAudioCalls);
        Assert.Single(context.MessageResults);
        Assert.Equal(WhatsAppHealthStatus.AudioFileInvalid, context.MessageResults[0].Status);
        Assert.Contains(context.Logs, log => log.Contains("archivo no existe", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SendAsync_WhenTtsReturnsEmptyFile_ReportsGeneratedAudioFileFailure()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string emptyFile = Path.Combine(tempDir, "empty.ogg");
        File.WriteAllBytes(emptyFile, Array.Empty<byte>());

        try
        {
            var tts = new FakeTts(emptyFile);
            var sender = new FakeWhatsAppSender();
            var context = CreateContext(sender, tts);

            var summary = await context.Orchestrator.SendAsync(
                new[] { new OutboundMessage("57", "3001112233", "audio", true) },
                new WhatsAppSendOptions(context.Line, AutoFallbackEnabled: false, SkippedCount: 0));

            Assert.Equal(1, summary.Failed);
            Assert.Equal(0, sender.SendAudioCalls);
            Assert.Single(context.MessageResults);
            Assert.Equal(WhatsAppHealthStatus.AudioFileInvalid, context.MessageResults[0].Status);
            Assert.Contains(context.Logs, log => log.Contains("esta vacio", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task SendAsync_WhenMessageIsText_SendsTextWithoutInvokingTts()
    {
        var tts = new FakeTts(null);
        var sender = new FakeWhatsAppSender();
        var context = CreateContext(sender, tts);

        var summary = await context.Orchestrator.SendAsync(
            new[] { new OutboundMessage("57", "3001112233", "texto", false) },
            new WhatsAppSendOptions(context.Line, AutoFallbackEnabled: false, SkippedCount: 0));

        Assert.Equal(1, summary.Sent);
        Assert.Equal(1, sender.SendMessageCalls);
        Assert.Equal(0, sender.SendAudioCalls);
        Assert.Equal(0, tts.ConvertCalls);
    }

    [Fact]
    public async Task SendAsync_WhenAudioAndElevenLabsSettingsInvalid_StopsBeforeOpeningSenderOrConvertingTts()
    {
        var tts = new FakeTts(null, new ElevenLabsSettings("", ""));
        var sender = new FakeWhatsAppSender();
        int senderFactoryCalls = 0;
        var context = CreateContext(sender, tts, onSenderFactoryInvoked: () => senderFactoryCalls++);

        var summary = await context.Orchestrator.SendAsync(
            new[] { new OutboundMessage("57", "3001112233", "audio", true) },
            new WhatsAppSendOptions(context.Line, AutoFallbackEnabled: false, SkippedCount: 0));

        Assert.Equal(0, senderFactoryCalls);
        Assert.Equal(0, tts.ConvertCalls);
        Assert.Equal(0, sender.OpenChatCalls);
        Assert.Equal(0, summary.Processed);
        Assert.Equal(0, summary.Sent);
        Assert.Equal(1, summary.Pending);
        Assert.True(summary.StoppedByGlobalFailure);
        Assert.Empty(context.MessageResults);
        Assert.Contains(context.Logs, log => log.Contains("ElevenLabs no está configurado para enviar audios. Guarda API key y Voice ID antes de iniciar.", StringComparison.Ordinal));
        Assert.DoesNotContain(context.Logs, log => log.Contains("Abriendo Chrome", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(context.Logs, log => log.Contains("ElevenLabs no devolvio una ruta", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SendAsync_WhenTextAndElevenLabsSettingsInvalid_SendsTextWithoutValidatingTts()
    {
        var tts = new FakeTts(null, new ElevenLabsSettings("", ""));
        var sender = new FakeWhatsAppSender();
        int ttsFactoryCalls = 0;
        var context = CreateContext(sender, tts, onTtsFactoryInvoked: () => ttsFactoryCalls++);

        var summary = await context.Orchestrator.SendAsync(
            new[] { new OutboundMessage("57", "3001112233", "texto", false) },
            new WhatsAppSendOptions(context.Line, AutoFallbackEnabled: false, SkippedCount: 0));

        Assert.Equal(1, summary.Sent);
        Assert.Equal(1, sender.SendMessageCalls);
        Assert.Equal(0, sender.SendAudioCalls);
        Assert.Equal(0, ttsFactoryCalls);
        Assert.Equal(0, tts.ConvertCalls);
    }

    private static TestContext CreateContext(
        FakeWhatsAppSender sender,
        FakeTts tts,
        Action? onSenderFactoryInvoked = null,
        Action? onTtsFactoryInvoked = null)
    {
        var line = new WhatsAppLine
        {
            Id = "line-1",
            DisplayName = "Linea prueba",
            SessionPath = Path.Combine(Path.GetTempPath(), "AutoWhatsAppTests"),
            OperationalState = WhatsAppLineOperationalState.Ready
        };

        var logs = new List<string>();
        var messageResults = new List<WhatsAppMessageResult>();
        var orchestrator = new WhatsAppSendOrchestrator(
            new[] { line },
            _ =>
            {
                onSenderFactoryInvoked?.Invoke();
                return sender;
            },
            () =>
            {
                onTtsFactoryInvoked?.Invoke();
                return tts;
            });

        orchestrator.Log += logs.Add;
        orchestrator.MessageResult += messageResults.Add;

        return new TestContext(orchestrator, line, logs, messageResults);
    }

    private sealed record TestContext(
        WhatsAppSendOrchestrator Orchestrator,
        WhatsAppLine Line,
        List<string> Logs,
        List<WhatsAppMessageResult> MessageResults);

    private sealed class FakeTts : ElevenLabsTts
    {
        private readonly string? resultPath;

        public FakeTts(string? resultPath, ElevenLabsSettings? settings = null)
            : base(settings ?? new ElevenLabsSettings("api-key", "voice-id"), _ => { })
        {
            this.resultPath = resultPath;
        }

        public int ConvertCalls { get; private set; }

        public override Task<string> ConvertToOggAsync(string text, string outputPath)
        {
            ConvertCalls++;
            return Task.FromResult(resultPath!);
        }
    }

    private sealed class FakeWhatsAppSender : IWhatsAppSender
    {
        public WhatsAppLine Line { get; } = new()
        {
            Id = "line-1",
            DisplayName = "Linea prueba",
            SessionPath = Path.Combine(Path.GetTempPath(), "AutoWhatsAppTests"),
            OperationalState = WhatsAppLineOperationalState.Ready
        };

        public int SendMessageCalls { get; private set; }

        public int SendAudioCalls { get; private set; }

        public int OpenChatCalls { get; private set; }

        public WhatsAppHealthIssue WaitForReady(TimeSpan timeout)
        {
            return ReadyIssue();
        }

        public WhatsAppLineOperationalState GetOperationalState(TimeSpan timeout, out WhatsAppHealthIssue issue)
        {
            issue = ReadyIssue();
            return WhatsAppLineOperationalState.Ready;
        }

        public WhatsAppSendResult OpenChat(string phone)
        {
            OpenChatCalls++;
            return WhatsAppSendResult.Ok("Chat listo para enviar.");
        }

        public WhatsAppSendResult SendMessage(string phone, string message, bool isAudio, string audioPath = "")
        {
            SendMessageCalls++;
            return WhatsAppSendResult.Ok($"Mensaje enviado a {phone}.");
        }

        public WhatsAppSendResult SendAudio(string audioPath)
        {
            SendAudioCalls++;
            return WhatsAppSendResult.Ok("Audio enviado correctamente.");
        }

        public void Close()
        {
        }

        private static WhatsAppHealthIssue ReadyIssue()
        {
            return new WhatsAppHealthIssue(WhatsAppHealthStatus.Ready, "Línea lista.", false);
        }
    }
}
