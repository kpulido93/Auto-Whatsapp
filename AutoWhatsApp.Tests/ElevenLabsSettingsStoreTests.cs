using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class ElevenLabsSettingsStoreTests
{
    [Fact]
    public void SaveAndTryLoad_RoundTripsSettingsWithoutPlainTextApiKey()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string configPath = Path.Combine(tempDir, ElevenLabsSettingsStore.ConfigFileName);

        try
        {
            var settings = new ElevenLabsSettings(
                "secret-api-key",
                "voice-id",
                "eleven_multilingual_v2",
                "opus_48000_96",
                0.6,
                0.7);

            ElevenLabsSettingsStore.Save(settings, configPath);

            string storedJson = File.ReadAllText(configPath);
            Assert.DoesNotContain("secret-api-key", storedJson, StringComparison.Ordinal);
            Assert.True(ElevenLabsSettingsStore.TryLoad(out var loadedSettings, configPath));
            Assert.Equal(settings.ApiKey, loadedSettings.ApiKey);
            Assert.Equal(settings.VoiceId, loadedSettings.VoiceId);
            Assert.Equal(settings.ModelId, loadedSettings.ModelId);
            Assert.Equal(settings.OutputFormat, loadedSettings.OutputFormat);
            Assert.Equal(settings.Stability, loadedSettings.Stability);
            Assert.Equal(settings.SimilarityBoost, loadedSettings.SimilarityBoost);

            ElevenLabsSettings loadedOrEnvironmentSettings = ElevenLabsSettingsStore.LoadOrEnvironment(configPath);
            Assert.Equal(settings.ApiKey, loadedOrEnvironmentSettings.ApiKey);
            Assert.Equal(settings.VoiceId, loadedOrEnvironmentSettings.VoiceId);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryLoad_WhenConfigFileDoesNotExist_ReturnsFalse()
    {
        string configPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), ElevenLabsSettingsStore.ConfigFileName);

        bool loaded = ElevenLabsSettingsStore.TryLoad(out _, configPath);

        Assert.False(loaded);
    }
}
