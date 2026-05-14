using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Automate_Whatsapp.Logic;

public static class ElevenLabsSettingsStore
{
    public const string ConfigFileName = "elevenlabs-settings.json";

    private const string AppFolderName = "AutoWhatsApp";

    private static readonly byte[] EncryptionEntropy = Encoding.UTF8.GetBytes("AutoWhatsApp.ElevenLabsSettings.v1");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public static ElevenLabsSettings LoadOrEnvironment(string? configPath = null)
    {
        return TryLoad(out var settings, configPath)
            ? settings
            : ElevenLabsSettings.FromEnvironment();
    }

    public static bool TryLoad(out ElevenLabsSettings settings, string? configPath = null)
    {
        settings = ElevenLabsSettings.FromEnvironment();

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

            var storedSettings = JsonSerializer.Deserialize<StoredElevenLabsSettings>(json, JsonOptions);
            if (storedSettings == null)
            {
                return false;
            }

            settings = new ElevenLabsSettings(
                Unprotect(storedSettings.ApiKeyProtected),
                storedSettings.VoiceId,
                storedSettings.ModelId,
                storedSettings.OutputFormat,
                storedSettings.Stability ?? ElevenLabsSettings.DefaultStability,
                storedSettings.SimilarityBoost ?? ElevenLabsSettings.DefaultSimilarityBoost);

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
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static void Save(ElevenLabsSettings settings, string? configPath = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string resolvedConfigPath = configPath ?? GetConfigPath();
        string configDirectory = Path.GetDirectoryName(resolvedConfigPath)!;
        Directory.CreateDirectory(configDirectory);

        var storedSettings = new StoredElevenLabsSettings
        {
            ApiKeyProtected = Protect(settings.ApiKey),
            VoiceId = settings.VoiceId,
            ModelId = settings.ModelId,
            OutputFormat = settings.OutputFormat,
            Stability = settings.Stability,
            SimilarityBoost = settings.SimilarityBoost
        };

        string json = JsonSerializer.Serialize(storedSettings, JsonOptions);
        File.WriteAllText(resolvedConfigPath, json);
    }

    public static string GetConfigPath()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, AppFolderName, ConfigFileName);
    }

    private static string Protect(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        byte[] plainBytes = Encoding.UTF8.GetBytes(value);
        byte[] protectedBytes = ProtectedData.Protect(
            plainBytes,
            EncryptionEntropy,
            DataProtectionScope.CurrentUser);

        return Convert.ToBase64String(protectedBytes);
    }

    private static string Unprotect(string? protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            return string.Empty;
        }

        byte[] protectedBytes = Convert.FromBase64String(protectedValue);
        byte[] plainBytes = ProtectedData.Unprotect(
            protectedBytes,
            EncryptionEntropy,
            DataProtectionScope.CurrentUser);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private sealed class StoredElevenLabsSettings
    {
        public string ApiKeyProtected { get; init; } = "";

        public string? VoiceId { get; init; }

        public string? ModelId { get; init; }

        public string? OutputFormat { get; init; }

        public double? Stability { get; init; }

        public double? SimilarityBoost { get; init; }
    }
}
