using System.Text.Json.Serialization;

namespace Automate_Whatsapp.Logic;

public sealed record MessageTemplate
{
    [JsonConstructor]
    public MessageTemplate(int number, string? name = null, string? body = null, bool enabled = true)
    {
        Number = number;
        Name = name ?? string.Empty;
        Body = body ?? string.Empty;
        Enabled = enabled;
    }

    public int Number { get; init; }

    public string Name { get; init; }

    public string Body { get; init; }

    public bool Enabled { get; init; }
}
