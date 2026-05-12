namespace Automate_Whatsapp.Logic;

public sealed record WhatsAppSendOptions(
    WhatsAppLine? SelectedLine,
    bool AutoFallbackEnabled,
    int SkippedCount,
    IReadOnlyCollection<string>? SelectedLineIdsForRun = null);
