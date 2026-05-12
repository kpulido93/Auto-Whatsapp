namespace Automate_Whatsapp.Logic;

public interface IWhatsAppSender
{
    WhatsAppLine Line { get; }

    WhatsAppHealthIssue WaitForReady(TimeSpan timeout);

    WhatsAppLineOperationalState GetOperationalState(TimeSpan timeout, out WhatsAppHealthIssue issue);

    WhatsAppSendResult OpenChat(string phone);

    WhatsAppSendResult SendMessage(string phone, string message, bool isAudio, string audioPath = "");

    WhatsAppSendResult SendAudio(string audioPath);

    void Close();
}
