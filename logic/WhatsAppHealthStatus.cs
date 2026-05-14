namespace Automate_Whatsapp.Logic;

public enum WhatsAppHealthStatus
{
    Ready,
    InvalidDestinationNumber,
    ChatOpenFailed,
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
