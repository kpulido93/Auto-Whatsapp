using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class MessageTemplateRendererTests
{
    [Fact]
    public void Render_ReplacesSupportedPlaceholdersIgnoringCase()
    {
        const string templateBody = "Hola {NOMBREDEUDOR}, te contactamos de {banco}.";

        string rendered = MessageTemplateRenderer.Render(templateBody, "Banco Uno", "Ana Perez");

        Assert.Equal("Hola Ana Perez, te contactamos de Banco Uno.", rendered);
    }

    [Fact]
    public void Render_WhenValuesAreNullOrEmpty_ReplacesSupportedPlaceholdersWithEmptyString()
    {
        const string templateBody = "Hola {NombreDeudor}, te contactamos de {Banco}.";

        string rendered = MessageTemplateRenderer.Render(templateBody, banco: "", nombreDeudor: null);

        Assert.Equal("Hola , te contactamos de .", rendered);
    }

    [Fact]
    public void Render_DoesNotModifyUnsupportedPlaceholders()
    {
        const string templateBody = "Hola {Nombre}, te contactamos de {Banco}.";

        string rendered = MessageTemplateRenderer.Render(templateBody, "Banco Uno", "Ana Perez");

        Assert.Equal("Hola {Nombre}, te contactamos de Banco Uno.", rendered);
    }
}
