namespace Automate_Whatsapp.Logic;

public sealed record WhatsAppLinePreparationResult(
    WhatsAppLine Line,
    WhatsAppLineOperationalState State,
    WhatsAppHealthStatus HealthStatus,
    string Message);
