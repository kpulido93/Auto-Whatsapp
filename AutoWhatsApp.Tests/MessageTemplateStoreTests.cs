using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class MessageTemplateStoreTests
{
    [Fact]
    public void LoadOrDefault_WhenConfigFileDoesNotExist_ReturnsDefaultTemplates()
    {
        string configPath = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"),
            MessageTemplateStore.ConfigFileName);
        MessageTemplate[] expectedTemplates =
        [
            new MessageTemplate(1, "Plantilla 1", "Hola {NombreDeudor}, te contactamos de {Banco}.", true),
            new MessageTemplate(2, "Plantilla 2", "Señor(a) {NombreDeudor}, tenemos información importante de {Banco}.", true),
            new MessageTemplate(3, "Plantilla 3", "Buen día {NombreDeudor}, por favor comunícate con {Banco}.", true)
        ];

        IReadOnlyList<MessageTemplate> templates = MessageTemplateStore.LoadOrDefault(configPath);

        Assert.Equal(expectedTemplates, templates);
        Assert.False(MessageTemplateStore.TryLoad(out _, configPath));
    }

    [Fact]
    public void SaveAndLoad_RoundTripsTemplatesAndWritesIndentedJson()
    {
        string tempDir = CreateTempDirectory();
        string configPath = Path.Combine(tempDir, MessageTemplateStore.ConfigFileName);

        try
        {
            MessageTemplate[] templates =
            [
                new MessageTemplate(4, "Cobro preventivo", "Hola {NombreDeudor}", true),
                new MessageTemplate(5, "Seguimiento", "Banco: {Banco}", false)
            ];

            MessageTemplateStore.Save(templates, configPath);

            string storedJson = File.ReadAllText(configPath);
            Assert.Contains("\n  {", storedJson, StringComparison.Ordinal);
            Assert.Contains("\n    \"Number\": 4", storedJson, StringComparison.Ordinal);
            Assert.True(MessageTemplateStore.TryLoad(out var loadedTemplates, configPath));
            Assert.Equal(templates, loadedTemplates);

            IReadOnlyList<MessageTemplate> loadedOrDefaultTemplates = MessageTemplateStore.LoadOrDefault(configPath);
            Assert.Equal(templates, loadedOrDefaultTemplates);
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
