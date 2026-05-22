using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class AutoWhatsAppSettingsStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsDelayWithoutSecrets()
    {
        string tempDir = CreateTempDirectory();
        string configPath = Path.Combine(tempDir, AutoWhatsAppSettingsStore.ConfigFileName);

        try
        {
            var settings = new AutoWhatsAppSettings(DelayBetweenMessagesMinutes: 5);

            AutoWhatsAppSettingsStore.Save(settings, configPath);

            string storedJson = File.ReadAllText(configPath);
            Assert.Contains("DelayBetweenMessagesMinutes", storedJson, StringComparison.Ordinal);
            Assert.DoesNotContain("ApiKey", storedJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("VoiceId", storedJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Credential", storedJson, StringComparison.OrdinalIgnoreCase);
            Assert.True(AutoWhatsAppSettingsStore.TryLoad(out var loadedSettings, configPath));
            Assert.Equal(5, loadedSettings.DelayBetweenMessagesMinutes);

            var loadedOrDefaultSettings = AutoWhatsAppSettingsStore.LoadOrDefault(configPath);
            Assert.Equal(5, loadedOrDefaultSettings.DelayBetweenMessagesMinutes);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void LoadOrDefault_WhenConfigFileDoesNotExist_ReturnsDefaults()
    {
        string configPath = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"),
            AutoWhatsAppSettingsStore.ConfigFileName);

        var settings = AutoWhatsAppSettingsStore.LoadOrDefault(configPath);

        Assert.Equal(1, settings.DelayBetweenMessagesMinutes);
        Assert.False(AutoWhatsAppSettingsStore.TryLoad(out _, configPath));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ invalid json")]
    public void LoadOrDefault_WhenConfigFileIsEmptyOrInvalid_ReturnsDefaults(string json)
    {
        string tempDir = CreateTempDirectory();
        string configPath = Path.Combine(tempDir, AutoWhatsAppSettingsStore.ConfigFileName);

        try
        {
            File.WriteAllText(configPath, json);

            var settings = AutoWhatsAppSettingsStore.LoadOrDefault(configPath);

            Assert.Equal(1, settings.DelayBetweenMessagesMinutes);
            Assert.False(AutoWhatsAppSettingsStore.TryLoad(out _, configPath));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 1)]
    [InlineData(999, 120)]
    public void LoadOrDefault_WhenDelayIsOutOfRange_NormalizesValue(int storedDelay, int expectedDelay)
    {
        string tempDir = CreateTempDirectory();
        string configPath = Path.Combine(tempDir, AutoWhatsAppSettingsStore.ConfigFileName);

        try
        {
            File.WriteAllText(
                configPath,
                $$"""
                {
                  "DelayBetweenMessagesMinutes": {{storedDelay}}
                }
                """);

            var settings = AutoWhatsAppSettingsStore.LoadOrDefault(configPath);

            Assert.Equal(expectedDelay, settings.DelayBetweenMessagesMinutes);
            Assert.True(AutoWhatsAppSettingsStore.TryLoad(out var tryLoadSettings, configPath));
            Assert.Equal(expectedDelay, tryLoadSettings.DelayBetweenMessagesMinutes);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Save_WhenDelayIsOutOfRange_WritesNormalizedValue()
    {
        string tempDir = CreateTempDirectory();
        string configPath = Path.Combine(tempDir, AutoWhatsAppSettingsStore.ConfigFileName);

        try
        {
            AutoWhatsAppSettingsStore.Save(
                new AutoWhatsAppSettings(DelayBetweenMessagesMinutes: 999),
                configPath);

            var settings = AutoWhatsAppSettingsStore.LoadOrDefault(configPath);

            Assert.Equal(120, settings.DelayBetweenMessagesMinutes);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }
}
