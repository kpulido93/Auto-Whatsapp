namespace Automate_Whatsapp.Logic;

public sealed record AppConfigurationState(
    string? SelectedWhatsAppLineId,
    bool AutoFallbackEnabled,
    IReadOnlyCollection<string> SelectedLineIdsForRun,
    int DelayBetweenMessagesMinutes,
    string ElevenLabsStatusText);
