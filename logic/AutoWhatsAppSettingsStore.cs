using System.Text.Json;

namespace Automate_Whatsapp.Logic;

public static class AutoWhatsAppSettingsStore
{
    public const string ConfigFileName = "app-settings.json";

    private const string AppFolderName = "AutoWhatsApp";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public static AutoWhatsAppSettings LoadOrDefault(string? configPath = null)
    {
        return TryLoad(out var settings, configPath)
            ? settings
            : AutoWhatsAppSettings.Default;
    }

    public static bool TryLoad(out AutoWhatsAppSettings settings, string? configPath = null)
    {
        settings = AutoWhatsAppSettings.Default;

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

            var storedSettings = JsonSerializer.Deserialize<StoredAutoWhatsAppSettings>(json, JsonOptions);
            if (storedSettings == null)
            {
                return false;
            }

            settings = new AutoWhatsAppSettings(
                storedSettings.DelayBetweenMessagesMinutes
                    ?? AutoWhatsAppSettings.DefaultDelayBetweenMessagesMinutes)
                .Normalize();

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

    public static void Save(AutoWhatsAppSettings settings, string? configPath = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            string resolvedConfigPath = configPath ?? GetConfigPath();
            string configDirectory = Path.GetDirectoryName(resolvedConfigPath)!;
            Directory.CreateDirectory(configDirectory);

            var normalizedSettings = settings.Normalize();
            var storedSettings = new StoredAutoWhatsAppSettings
            {
                DelayBetweenMessagesMinutes = normalizedSettings.DelayBetweenMessagesMinutes
            };

            string json = JsonSerializer.Serialize(storedSettings, JsonOptions);
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

    private sealed class StoredAutoWhatsAppSettings
    {
        public int? DelayBetweenMessagesMinutes { get; init; }
    }
}
