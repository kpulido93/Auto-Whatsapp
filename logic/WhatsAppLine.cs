using System.Text.Json.Serialization;

namespace Automate_Whatsapp.Logic;

public enum WhatsAppLineOperationalState
{
    Unknown,
    Ready,
    RequiresManualAuth,
    NotAvailable
}

public sealed record WhatsAppLine
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string SessionPath { get; init; } = "";
    public string ProfileDirectory { get; init; } = "Default";
    public bool Enabled { get; init; } = true;
    public int Priority { get; init; } = 1;

    [JsonIgnore]
    public WhatsAppLineOperationalState OperationalState { get; init; } = WhatsAppLineOperationalState.Unknown;

    [JsonIgnore]
    public string OperationalStateText => OperationalState switch
    {
        WhatsAppLineOperationalState.Ready => "Lista",
        WhatsAppLineOperationalState.RequiresManualAuth => "Requiere QR",
        WhatsAppLineOperationalState.NotAvailable => "No disponible",
        _ => "Sin verificar"
    };

    [JsonIgnore]
    public string DisplayNameWithState => $"{ToString()} - {OperationalStateText}";

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(DisplayName) ? Id : DisplayName;
    }
}
