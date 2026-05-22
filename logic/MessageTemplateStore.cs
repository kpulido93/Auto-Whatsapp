using System.Text.Json;

namespace Automate_Whatsapp.Logic;

public static class MessageTemplateStore
{
    public const string ConfigFileName = "message-templates.json";

    private const string AppFolderName = "AutoWhatsApp";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public static IReadOnlyList<MessageTemplate> LoadOrDefault(string? configPath = null)
    {
        return TryLoad(out var templates, configPath)
            ? templates
            : CreateDefaultTemplates();
    }

    public static bool TryLoad(out IReadOnlyList<MessageTemplate> templates, string? configPath = null)
    {
        templates = CreateDefaultTemplates();

        string resolvedConfigPath = configPath ?? GetConfigPath();
        if (!File.Exists(resolvedConfigPath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(resolvedConfigPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            MessageTemplate[]? storedTemplates = JsonSerializer.Deserialize<MessageTemplate[]>(json, JsonOptions);
            if (storedTemplates == null)
            {
                return false;
            }

            templates = storedTemplates;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static void Save(IEnumerable<MessageTemplate> templates, string? configPath = null)
    {
        ArgumentNullException.ThrowIfNull(templates);

        try
        {
            string resolvedConfigPath = configPath ?? GetConfigPath();
            string configDirectory = Path.GetDirectoryName(resolvedConfigPath)!;
            Directory.CreateDirectory(configDirectory);

            MessageTemplate[] storedTemplates = templates.ToArray();
            string json = JsonSerializer.Serialize(storedTemplates, JsonOptions);
            File.WriteAllText(resolvedConfigPath, json);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static string GetConfigPath()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, AppFolderName, ConfigFileName);
    }

    private static MessageTemplate[] CreateDefaultTemplates()
    {
        return
        [
            new MessageTemplate(
                number: 1,
                name: "Plantilla 1",
                body: "Hola {NombreDeudor}, te contactamos de {Banco}.",
                enabled: true),
            new MessageTemplate(
                number: 2,
                name: "Plantilla 2",
                body: "Señor(a) {NombreDeudor}, tenemos información importante de {Banco}.",
                enabled: true),
            new MessageTemplate(
                number: 3,
                name: "Plantilla 3",
                body: "Buen día {NombreDeudor}, por favor comunícate con {Banco}.",
                enabled: true)
        ];
    }
}
