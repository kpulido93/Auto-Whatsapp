using System.Text.Json;

namespace Automate_Whatsapp.Logic;

public static class DoNotContactStore
{
    public const string ConfigFileName = "do-not-contact.json";

    private const string AppFolderName = "AutoWhatsApp";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public static IReadOnlyList<DoNotContactEntry> LoadOrEmpty(string? configPath = null)
    {
        return TryLoad(out var entries, configPath)
            ? entries
            : Array.Empty<DoNotContactEntry>();
    }

    public static bool TryLoad(out IReadOnlyList<DoNotContactEntry> entries, string? configPath = null)
    {
        entries = Array.Empty<DoNotContactEntry>();

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

            using var document = JsonDocument.Parse(json);
            entries = document.RootElement.ValueKind switch
            {
                JsonValueKind.Array => DeserializeEntries(document.RootElement),
                JsonValueKind.Object => DeserializeEntriesFromObject(document.RootElement),
                _ => Array.Empty<DoNotContactEntry>()
            };

            return entries.Count > 0 || document.RootElement.ValueKind is JsonValueKind.Array or JsonValueKind.Object;
        }
        catch (JsonException)
        {
            entries = Array.Empty<DoNotContactEntry>();
            return false;
        }
        catch (IOException)
        {
            entries = Array.Empty<DoNotContactEntry>();
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            entries = Array.Empty<DoNotContactEntry>();
            return false;
        }
    }

    public static void Save(IEnumerable<DoNotContactEntry> entries, string? configPath = null)
    {
        ArgumentNullException.ThrowIfNull(entries);

        string resolvedConfigPath = configPath ?? GetConfigPath();
        string configDirectory = Path.GetDirectoryName(resolvedConfigPath)!;
        Directory.CreateDirectory(configDirectory);

        var payload = new StoredDoNotContactEntries
        {
            Entries = entries
                .Where(entry => entry != null)
                .Select(entry => new DoNotContactEntry(
                    entry.CountryCode.Trim(),
                    entry.Phone.Trim(),
                    entry.Reason.Trim(),
                    entry.CreatedAt))
                .ToList()
        };

        string json = JsonSerializer.Serialize(payload, JsonOptions);
        File.WriteAllText(resolvedConfigPath, json);
    }

    public static bool IsBlocked(
        IEnumerable<DoNotContactEntry>? entries,
        string? countryCode,
        string? phone)
    {
        string normalizedPhone = DoNotContactEntry.NormalizePhone(countryCode, phone);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return false;
        }

        return CreateNormalizedPhoneSet(entries).Contains(normalizedPhone);
    }

    public static HashSet<string> CreateNormalizedPhoneSet(IEnumerable<DoNotContactEntry>? entries)
    {
        var blockedPhones = new HashSet<string>(StringComparer.Ordinal);

        if (entries == null)
        {
            return blockedPhones;
        }

        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.NormalizedPhone))
            {
                continue;
            }

            blockedPhones.Add(entry.NormalizedPhone);
        }

        return blockedPhones;
    }

    public static string GetConfigPath()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, AppFolderName, ConfigFileName);
    }

    private static IReadOnlyList<DoNotContactEntry> DeserializeEntries(JsonElement element)
    {
        List<DoNotContactEntry>? entries = element.Deserialize<List<DoNotContactEntry>>(JsonOptions);
        return entries ?? (IReadOnlyList<DoNotContactEntry>)Array.Empty<DoNotContactEntry>();
    }

    private static IReadOnlyList<DoNotContactEntry> DeserializeEntriesFromObject(JsonElement element)
    {
        if (!TryGetEntriesProperty(element, out var entriesElement))
        {
            return Array.Empty<DoNotContactEntry>();
        }

        return DeserializeEntries(entriesElement);
    }

    private static bool TryGetEntriesProperty(JsonElement element, out JsonElement entriesElement)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, "entries", StringComparison.OrdinalIgnoreCase))
            {
                entriesElement = property.Value;
                return true;
            }
        }

        entriesElement = default;
        return false;
    }

    private sealed class StoredDoNotContactEntries
    {
        public List<DoNotContactEntry> Entries { get; init; } = new();
    }
}
