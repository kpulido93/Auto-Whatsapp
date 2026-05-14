namespace Automate_Whatsapp.Logic;

public enum WhatsAppHealthStatus
{
    Ready,
    InvalidDestinationNumber,
    LoginRequired,
    PhoneDisconnected,
    SenderAccountBlockedOrRestricted,
    WhatsAppNotReady,
    BrowserUnavailable,
    TextToSpeechFailed,
    AudioFileInvalid,
    WhatsAppAttachmentFailed,
    UnknownError
}
