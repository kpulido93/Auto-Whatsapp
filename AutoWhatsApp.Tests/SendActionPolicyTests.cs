using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class SendActionPolicyTests
{
    private static readonly DateTime Now = new(2026, 5, 14, 12, 0, 0);

    [Fact]
    public void Evaluate_WhenExcelIsMissing_BlocksWithClearReason()
    {
        var input = ValidInput() with { HasExcel = false };

        var result = SendActionPolicy.Evaluate(input);

        Assert.False(result.CanProceed);
        Assert.Equal(SendActionBlockReason.MissingExcel, result.Reason);
        Assert.Contains("Excel", result.UserMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no hay archivo Excel", result.LogMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_WhenExcelIsLoading_BlocksUntilAnalysisFinishes()
    {
        var input = ValidInput() with { IsLoadingExcelPreview = true };

        var result = SendActionPolicy.Evaluate(input);

        Assert.False(result.CanProceed);
        Assert.Equal(SendActionBlockReason.ExcelLoading, result.Reason);
        Assert.Contains("análisis", result.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_WhenExcelHasNoValidRows_BlocksWithExcelReason()
    {
        var input = ValidInput() with { ValidRowCount = 0 };

        var result = SendActionPolicy.Evaluate(input);

        Assert.False(result.CanProceed);
        Assert.Equal(SendActionBlockReason.NoValidRows, result.Reason);
        Assert.Contains("filas válidas", result.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_WhenNoRunLinesAreSelected_BlocksWithLineSelectionReason()
    {
        var input = ValidInput() with { SelectedRunLineCount = 0 };

        var result = SendActionPolicy.Evaluate(input);

        Assert.False(result.CanProceed);
        Assert.Equal(SendActionBlockReason.NoSelectedRunLines, result.Reason);
        Assert.Contains("Selecciona al menos una línea", result.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_WhenSendIsAlreadyRunning_BlocksAndButtonsCannotRequestSend()
    {
        var input = ValidInput() with { IsSending = true };

        var result = SendActionPolicy.Evaluate(input);
        var canRequestSend = SendActionPolicy.CanRequestSend(
            new SendActionButtonState(IsSending: true, IsScheduled: false, IsPreparingLines: false));

        Assert.False(result.CanProceed);
        Assert.Equal(SendActionBlockReason.OperationInProgress, result.Reason);
        Assert.False(canRequestSend);
    }

    [Fact]
    public void Evaluate_WhenScheduleIsAlreadyActive_BlocksNewSchedule()
    {
        var input = ValidInput(SendActionKind.Schedule) with { IsScheduled = true };

        var result = SendActionPolicy.Evaluate(input);

        Assert.False(result.CanProceed);
        Assert.Equal(SendActionBlockReason.ScheduleAlreadyActive, result.Reason);
        Assert.Contains("programado", result.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_WhenScheduleTimeIsInThePast_BlocksSchedule()
    {
        var input = ValidInput(SendActionKind.Schedule) with { ScheduledTime = Now.AddMinutes(-1) };

        var result = SendActionPolicy.Evaluate(input);

        Assert.False(result.CanProceed);
        Assert.Equal(SendActionBlockReason.InvalidScheduleTime, result.Reason);
        Assert.Contains("futura", result.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_WhenSendNowInputsAreValid_AllowsSend()
    {
        var input = ValidInput(SendActionKind.SendNow);

        var result = SendActionPolicy.Evaluate(input);
        var canRequestSend = SendActionPolicy.CanRequestSend(
            new SendActionButtonState(IsSending: false, IsScheduled: false, IsPreparingLines: false));

        Assert.True(result.CanProceed);
        Assert.Equal(SendActionBlockReason.None, result.Reason);
        Assert.True(canRequestSend);
    }

    [Fact]
    public void Evaluate_WhenScheduleInputsAreValid_AllowsSchedule()
    {
        var input = ValidInput(SendActionKind.Schedule);

        var result = SendActionPolicy.Evaluate(input);

        Assert.True(result.CanProceed);
        Assert.Equal(SendActionBlockReason.None, result.Reason);
    }

    [Fact]
    public void Evaluate_WhenLineRequiresQr_BlocksWithPreparationGuidance()
    {
        var input = ValidInput() with
        {
            SelectedLineState = WhatsAppLineOperationalState.RequiresManualAuth,
            SelectedLineName = "Principal"
        };

        var result = SendActionPolicy.Evaluate(input);

        Assert.False(result.CanProceed);
        Assert.Equal(SendActionBlockReason.SelectedLineNotReady, result.Reason);
        Assert.Contains("QR", result.UserMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Preparar línea", result.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static SendActionPolicyInput ValidInput(SendActionKind action = SendActionKind.SendNow)
    {
        return new SendActionPolicyInput
        {
            Action = action,
            FailureContext = action == SendActionKind.Schedule
                ? "No se programó el envío"
                : "No se inició el envío inmediato",
            HasExcel = true,
            ValidRowCount = 3,
            AvailableLineCount = 1,
            HasSelectedLine = true,
            SelectedRunLineCount = 1,
            EnabledSelectedRunLineCount = 1,
            SelectedLineState = WhatsAppLineOperationalState.Ready,
            SelectedLineName = "Principal",
            IsSending = false,
            IsScheduled = false,
            IsPreparingLines = false,
            IsLoadingExcelPreview = false,
            ScheduledTime = action == SendActionKind.Schedule ? Now.AddMinutes(5) : null,
            Now = Now
        };
    }
}
