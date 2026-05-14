using Automate_Whatsapp.Logic;

namespace Automate_Whatsapp;

internal sealed record ConfigurationDialogActions(
    Func<IWin32Window, string?, IReadOnlyList<WhatsAppLine>?> ConfigureLines,
    Func<string, IReadOnlyList<WhatsAppLine>> PrepareLine,
    Func<IReadOnlyList<WhatsAppLine>> PrepareAllLines,
    Func<ElevenLabsSettings, ElevenLabsDialogState> SaveElevenLabsSettings,
    Func<ElevenLabsSettings, ElevenLabsDialogState> TestElevenLabsSettings);

internal sealed record ElevenLabsDialogState(
    ElevenLabsSettings Settings,
    string StatusText,
    Color StatusColor,
    string ToolTipText);
