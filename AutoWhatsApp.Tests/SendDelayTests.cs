using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class SendDelayTests
{
    [Fact]
    public async Task SendAsync_WhenDelayMinutesIsZero_DoesNotInvokeConfigurableWait()
    {
        var delay = new FakeDelay();
        var context = CreateContext(delay);

        var summary = await context.Orchestrator.SendAsync(
            new[] { CreateMessage(1), CreateMessage(2) },
            new WhatsAppSendOptions(
                context.Line,
                AutoFallbackEnabled: false,
                SkippedCount: 0,
                DelayBetweenMessagesMinutes: 0));

        Assert.Equal(2, summary.Sent);
        Assert.Equal(2, context.Sender.SendMessageCalls);
        Assert.Equal(0, delay.Calls);
        Assert.DoesNotContain(context.Logs, log => log.Contains("Esperando", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SendAsync_WhenDelayIsConfigured_WaitsOnlyBetweenMessages()
    {
        var delay = new FakeDelay();
        var context = CreateContext(delay);

        var summary = await context.Orchestrator.SendAsync(
            new[] { CreateMessage(1), CreateMessage(2), CreateMessage(3) },
            new WhatsAppSendOptions(
                context.Line,
                AutoFallbackEnabled: false,
                SkippedCount: 0,
                DelayBetweenMessagesMinutes: 2));

        var waitLogs = context.Logs
            .Where(log => log == "Esperando 2 minuto(s) antes del siguiente mensaje...")
            .ToList();

        Assert.Equal(3, summary.Sent);
        Assert.Equal(3, context.Sender.SendMessageCalls);
        Assert.Equal(2, waitLogs.Count);
        Assert.Equal(TimeSpan.FromMinutes(4), delay.TotalRequestedDelay);
    }

    [Fact]
    public async Task SendAsync_WhenCancelledDuringDelay_StopsBeforeProcessingNextMessage()
    {
        var delay = new FakeDelay();
        var context = CreateContext(delay);
        delay.OnDelay = (_, _) => context.Orchestrator.Cancel();

        var summary = await context.Orchestrator.SendAsync(
            new[] { CreateMessage(1), CreateMessage(2) },
            new WhatsAppSendOptions(
                context.Line,
                AutoFallbackEnabled: false,
                SkippedCount: 0,
                DelayBetweenMessagesMinutes: 1));

        Assert.True(summary.Cancelled);
        Assert.Equal(1, summary.Processed);
        Assert.Equal(1, summary.Sent);
        Assert.Equal(1, context.Sender.SendMessageCalls);
        Assert.DoesNotContain("573001112202", context.Sender.SentPhones);
        Assert.Contains(context.Logs, log => log == "Espera entre mensajes cancelada.");
    }

    [Fact]
    public async Task SendAsync_WhenPausedDuringDelay_ResumesAndProcessesNextMessage()
    {
        var delay = new FakeDelay();
        var context = CreateContext(delay);
        delay.OnDelay = (call, _) =>
        {
            if (call == 1)
            {
                context.Orchestrator.Pause();
            }
            else if (call == 2)
            {
                context.Orchestrator.Resume();
            }
        };

        var summary = await context.Orchestrator.SendAsync(
            new[] { CreateMessage(1), CreateMessage(2) },
            new WhatsAppSendOptions(
                context.Line,
                AutoFallbackEnabled: false,
                SkippedCount: 0,
                DelayBetweenMessagesMinutes: 1));

        Assert.True(summary.CompletedSuccessfully);
        Assert.Equal(2, summary.Sent);
        Assert.Equal(2, context.Sender.SendMessageCalls);
        Assert.Contains(context.Logs, log => log == "Temporizador pausado.");
        Assert.Contains(context.Logs, log => log == "Temporizador reanudado.");
    }

    private static TestContext CreateContext(FakeDelay delay)
    {
        var line = new WhatsAppLine
        {
            Id = "line-1",
            DisplayName = "Linea prueba",
            SessionPath = Path.Combine(Path.GetTempPath(), "AutoWhatsAppTests"),
            OperationalState = WhatsAppLineOperationalState.Ready
        };

        var sender = new FakeWhatsAppSender(line);
        var logs = new List<string>();
        var orchestrator = new WhatsAppSendOrchestrator(
            new[] { line },
            _ => sender,
            delayAsync: delay.DelayAsync);

        orchestrator.Log += logs.Add;

        return new TestContext(orchestrator, line, sender, logs);
    }

    private static OutboundMessage CreateMessage(int index)
    {
        return new OutboundMessage("57", $"30011122{index:00}", $"mensaje {index}", false);
    }

    private sealed record TestContext(
        WhatsAppSendOrchestrator Orchestrator,
        WhatsAppLine Line,
        FakeWhatsAppSender Sender,
        List<string> Logs);

    private sealed class FakeDelay
    {
        public int Calls { get; private set; }

        public TimeSpan TotalRequestedDelay { get; private set; }

        public Action<int, TimeSpan>? OnDelay { get; set; }

        public Task DelayAsync(TimeSpan delay)
        {
            Calls++;
            TotalRequestedDelay += delay;
            OnDelay?.Invoke(Calls, delay);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWhatsAppSender : IWhatsAppSender
    {
        public FakeWhatsAppSender(WhatsAppLine line)
        {
            Line = line;
        }

        public WhatsAppLine Line { get; }

        public int SendMessageCalls { get; private set; }

        public List<string> SentPhones { get; } = new();

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
            SentPhones.Add(phone);
            return WhatsAppSendResult.Ok($"Mensaje enviado a {phone}.");
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
