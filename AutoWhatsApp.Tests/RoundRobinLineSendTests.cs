using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class RoundRobinLineSendTests
{
    [Fact]
    public async Task SendAsync_WithTwoReadyRunLines_AlternatesLineByLine()
    {
        var lines = new[]
        {
            CreateLine("line-2", "Línea 2", priority: 2),
            CreateLine("line-1", "Línea 1", priority: 1)
        };
        var context = CreateContext(lines);

        var summary = await context.Orchestrator.SendAsync(
            CreateMessages(4),
            CreateOptions(
                selectedLine: lines[0],
                selectedLineIdsForRun: new[] { lines[0].Id, lines[1].Id }));

        Assert.True(summary.CompletedSuccessfully);
        Assert.Equal(4, summary.Sent);
        Assert.Equal(
            new[] { "Línea 1", "Línea 2", "Línea 1", "Línea 2" },
            context.SendSequence);
        Assert.Contains(context.Logs, log => log == "Usando línea Línea 1 para mensaje 1");
        Assert.Contains(context.Logs, log => log == "Usando línea Línea 2 para mensaje 2");
    }

    [Fact]
    public async Task SendAsync_WithThreeReadyRunLines_UsesPriorityThenDisplayNameOrder()
    {
        var lines = new[]
        {
            CreateLine("line-c", "Línea C", priority: 2),
            CreateLine("line-a", "Línea A", priority: 1),
            CreateLine("line-b", "Línea B", priority: 2)
        };
        var context = CreateContext(lines);

        var summary = await context.Orchestrator.SendAsync(
            CreateMessages(5),
            CreateOptions(
                selectedLine: lines[0],
                selectedLineIdsForRun: lines.Select(line => line.Id).ToArray()));

        Assert.True(summary.CompletedSuccessfully);
        Assert.Equal(5, summary.Sent);
        Assert.Equal(
            new[] { "Línea A", "Línea B", "Línea C", "Línea A", "Línea B" },
            context.SendSequence);
    }

    [Fact]
    public async Task SendAsync_WithSingleReadyRunLine_KeepsSingleLineBehavior()
    {
        var lines = new[]
        {
            CreateLine("line-1", "Línea 1", priority: 1),
            CreateLine("line-2", "Línea 2", priority: 2)
        };
        var context = CreateContext(lines);

        var summary = await context.Orchestrator.SendAsync(
            CreateMessages(3),
            CreateOptions(
                selectedLine: lines[0],
                selectedLineIdsForRun: new[] { lines[0].Id }));

        Assert.True(summary.CompletedSuccessfully);
        Assert.Equal(3, summary.Sent);
        Assert.Equal(
            new[] { "Línea 1", "Línea 1", "Línea 1" },
            context.SendSequence);
        Assert.Equal(0, context.Senders[lines[1].Id].SendMessageCalls);
    }

    [Fact]
    public async Task SendAsync_WhenFirstRoundRobinLineFailsGlobally_RetriesSameMessageWithNextHealthyLine()
    {
        var lines = new[]
        {
            CreateLine("line-2", "Línea 2", priority: 2),
            CreateLine("line-1", "Línea 1", priority: 1)
        };
        var context = CreateContext(lines);
        context.Senders["line-1"].EnqueueSendResult(WhatsAppSendResult.Failure(
            WhatsAppHealthStatus.BrowserUnavailable,
            "Chrome no disponible.",
            isGlobalFailure: true));

        var summary = await context.Orchestrator.SendAsync(
            CreateMessages(2),
            CreateOptions(
                selectedLine: lines[0],
                selectedLineIdsForRun: new[] { lines[0].Id, lines[1].Id }));

        Assert.True(summary.CompletedSuccessfully);
        Assert.Equal(2, summary.Sent);
        Assert.Equal(
            new[] { "Línea 1", "Línea 2", "Línea 2" },
            context.SendSequence);
        Assert.Equal(1, context.Senders["line-1"].SendMessageCalls);
        Assert.Equal(2, context.Senders["line-2"].SendMessageCalls);
        Assert.Contains(context.Logs, log => log == "🔁 Reintentando 573001112201 con línea Línea 2.");
    }

    [Fact]
    public async Task SendAsync_WhenLineBecomesUnhealthy_ItIsNotUsedAgain()
    {
        var lines = new[]
        {
            CreateLine("line-c", "Línea C", priority: 2),
            CreateLine("line-a", "Línea A", priority: 1),
            CreateLine("line-b", "Línea B", priority: 2)
        };
        var context = CreateContext(lines);
        context.Senders["line-b"].EnqueueSendResult(WhatsAppSendResult.Failure(
            WhatsAppHealthStatus.BrowserUnavailable,
            "Chrome no disponible.",
            isGlobalFailure: true));

        var summary = await context.Orchestrator.SendAsync(
            CreateMessages(4),
            CreateOptions(
                selectedLine: lines[0],
                selectedLineIdsForRun: lines.Select(line => line.Id).ToArray()));

        Assert.True(summary.CompletedSuccessfully);
        Assert.Equal(4, summary.Sent);
        Assert.Equal(
            new[] { "Línea A", "Línea B", "Línea C", "Línea A", "Línea C" },
            context.SendSequence);
        Assert.Equal(1, context.Senders["line-b"].SendMessageCalls);
    }

    private static TestContext CreateContext(IReadOnlyList<WhatsAppLine> lines)
    {
        var logs = new List<string>();
        var sendSequence = new List<string>();
        var senders = lines.ToDictionary(
            line => line.Id,
            line => new FakeWhatsAppSender(line, sendSequence));

        var orchestrator = new WhatsAppSendOrchestrator(
            lines,
            line => senders[line.Id]);

        orchestrator.Log += logs.Add;

        return new TestContext(orchestrator, senders, logs, sendSequence);
    }

    private static WhatsAppLine CreateLine(string id, string displayName, int priority)
    {
        return new WhatsAppLine
        {
            Id = id,
            DisplayName = displayName,
            SessionPath = Path.Combine(Path.GetTempPath(), "AutoWhatsAppTests", id),
            Enabled = true,
            Priority = priority,
            OperationalState = WhatsAppLineOperationalState.Ready
        };
    }

    private static OutboundMessage[] CreateMessages(int count)
    {
        return Enumerable.Range(1, count)
            .Select(index => new OutboundMessage("57", $"30011122{index:00}", $"mensaje {index}", false))
            .ToArray();
    }

    private static WhatsAppSendOptions CreateOptions(
        WhatsAppLine selectedLine,
        IReadOnlyCollection<string> selectedLineIdsForRun)
    {
        return new WhatsAppSendOptions(
            selectedLine,
            AutoFallbackEnabled: false,
            SkippedCount: 0,
            SelectedLineIdsForRun: selectedLineIdsForRun,
            DelayBetweenMessagesMinutes: 0);
    }

    private sealed record TestContext(
        WhatsAppSendOrchestrator Orchestrator,
        IReadOnlyDictionary<string, FakeWhatsAppSender> Senders,
        List<string> Logs,
        List<string> SendSequence);

    private sealed class FakeWhatsAppSender : IWhatsAppSender
    {
        private readonly List<string> sendSequence;
        private readonly Queue<WhatsAppSendResult> sendMessageResults = new();

        public FakeWhatsAppSender(WhatsAppLine line, List<string> sendSequence)
        {
            Line = line;
            this.sendSequence = sendSequence;
        }

        public WhatsAppLine Line { get; }

        public int SendMessageCalls { get; private set; }

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
            SendMessageCalls++;
            sendSequence.Add(Line.DisplayName);

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
