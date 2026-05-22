namespace Automate_Whatsapp.Logic;

public sealed record AutoWhatsAppSettings(
    int DelayBetweenMessagesMinutes = AutoWhatsAppSettings.DefaultDelayBetweenMessagesMinutes)
{
    public const int DefaultDelayBetweenMessagesMinutes = 1;
    public const int MinDelayBetweenMessagesMinutes = 1;
    public const int MaxDelayBetweenMessagesMinutes = 120;

    public static AutoWhatsAppSettings Default => new();

    public AutoWhatsAppSettings Normalize()
    {
        return this with
        {
            DelayBetweenMessagesMinutes = Math.Clamp(
                DelayBetweenMessagesMinutes,
                MinDelayBetweenMessagesMinutes,
                MaxDelayBetweenMessagesMinutes)
        };
    }
}
