using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class WhatsAppMessageResultTests
{
    [Fact]
    public async Task SendAsync_WhenMessageIsSent_EmitsExportableFields()
    {
        var line = CreateLine("line-1", "Línea 1");
        var sender = new FakeWhatsAppSender(line);
        var context = CreateContext(line, sender);
        var message = new OutboundMessage("57", "3001112233", "hola", false, "Ana Pérez", null, "Bancolombia");

        var summary = await context.Orchestrator.SendAsync(
            new[] { message },
            new WhatsAppSendOptions(line, AutoFallbackEnabled: false, SkippedCount: 0));

        Assert.Equal(1, summary.Sent);
        Assert.Single(context.MessageResults);

        WhatsAppMessageResult result = context.MessageResults[0];
        Assert.Equal(message.FullPhone, result.FullPhone);
        Assert.Equal(message.FullPhone, result.Number);
        Assert.Equal("Ana Pérez", result.Name);
        Assert.Equal("Línea 1", result.LineName);
        Assert.Equal("Línea 1", result.LineaWP);
        Assert.Equal("Enviado", result.MessageState);
        Assert.Equal("Sí", result.HasWhatsApp);
    }

    [Fact]
    public async Task SendAsync_WhenNumberIsInvalid_EmitsNoWhatsApp()
    {
        var line = CreateLine("line-1", "Línea 1");
        var sender = new FakeWhatsAppSender(line);
        sender.EnqueueSendResult(WhatsAppSendResult.Failure(
            WhatsAppHealthStatus.InvalidDestinationNumber,
            "El número no tiene WhatsApp.",
            isGlobalFailure: false));
        var context = CreateContext(line, sender);
        var message = new OutboundMessage("57", "3001112233", "hola", false, "Ana Pérez", null, "Bancolombia");

        var summary = await context.Orchestrator.SendAsync(
            new[] { message },
            new WhatsAppSendOptions(line, AutoFallbackEnabled: false, SkippedCount: 0));

        Assert.Equal(1, summary.Invalid);
        Assert.Single(context.MessageResults);

        WhatsAppMessageResult result = context.MessageResults[0];
        Assert.Equal(WhatsAppHealthStatus.InvalidDestinationNumber, result.Status);
        Assert.Equal("No enviado - número inválido", result.MessageState);
        Assert.Equal("No", result.HasWhatsApp);
        Assert.Equal("Ana Pérez", result.Name);
        Assert.Equal("Línea 1", result.LineaWP);
    }

    [Theory]
    [InlineData(WhatsAppHealthStatus.BrowserUnavailable, "Chrome no disponible.")]
    [InlineData(WhatsAppHealthStatus.LoginRequired, "Requiere QR.")]
    [InlineData(WhatsAppHealthStatus.AudioFileInvalid, "El audio generado es inválido.")]
    public void Create_WhenFailureIsInconclusive_UsesDesconocido(
        WhatsAppHealthStatus status,
        string message)
    {
        var outboundMessage = new OutboundMessage("57", "3001112233", "hola", true, "Ana Pérez", null, "Bancolombia");
        var result = WhatsAppMessageResult.Create(
            outboundMessage,
            success: false,
            WhatsAppSendResult.Failure(status, message, isGlobalFailure: status is WhatsAppHealthStatus.BrowserUnavailable or WhatsAppHealthStatus.LoginRequired),
            "Línea 1");

        Assert.Equal(outboundMessage.FullPhone, result.Number);
        Assert.Equal("Ana Pérez", result.Name);
        Assert.Equal("Línea 1", result.LineaWP);
        Assert.Equal($"No enviado - {status}: {message}", result.MessageState);
        Assert.Equal("Desconocido", result.HasWhatsApp);
    }

    private static TestContext CreateContext(WhatsAppLine line, FakeWhatsAppSender sender)
    {
        var messageResults = new List<WhatsAppMessageResult>();
        var orchestrator = new WhatsAppSendOrchestrator(
            new[] { line },
            _ => sender);

        orchestrator.MessageResult += messageResults.Add;

        return new TestContext(orchestrator, messageResults);
    }

    private static WhatsAppLine CreateLine(string id, string displayName)
    {
        return new WhatsAppLine
        {
            Id = id,
            DisplayName = displayName,
            SessionPath = Path.Combine(Path.GetTempPath(), "AutoWhatsAppTests", id),
            Enabled = true,
            Priority = 1,
            OperationalState = WhatsAppLineOperationalState.Ready
        };
    }

    private sealed record TestContext(
        WhatsAppSendOrchestrator Orchestrator,
        List<WhatsAppMessageResult> MessageResults);

    private sealed class FakeWhatsAppSender : IWhatsAppSender
    {
        private readonly Queue<WhatsAppSendResult> sendMessageResults = new();

        public FakeWhatsAppSender(WhatsAppLine line)
        {
            Line = line;
        }

        public WhatsAppLine Line { get; }

        public void EnqueueSendResult(WhatsAppSendResult result)
        {
            sendMessageResults.Enqueue(result);
        }

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
            return WhatsAppSendResult.Ok("Chat listo para enviar.");
        }

        public WhatsAppSendResult SendMessage(string phone, string message, bool isAudio, string audioPath = "")
        {
            return sendMessageResults.Count > 0
                ? sendMessageResults.Dequeue()
                : WhatsAppSendResult.Ok($"Mensaje enviado a {phone}.");
        }

        public WhatsAppSendResult SendAudio(string audioPath)
        {
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
