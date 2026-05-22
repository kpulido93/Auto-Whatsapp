using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class MessageTemplateValidatorTests
{
    [Fact]
    public void Validate_WhenTemplatesAreValid_ReturnsSuccess()
    {
        MessageTemplate[] templates =
        [
            new MessageTemplate(1, "Plantilla 1", "Hola {NombreDeudor}", true),
            new MessageTemplate(2, "Plantilla 2", "Hola {Banco}", true),
            new MessageTemplate(3, "Plantilla 3", "", false)
        ];

        MessageTemplateValidationResult result = MessageTemplateValidator.Validate(templates);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenThereAreDuplicateNumbers_ReturnsError()
    {
        MessageTemplate[] templates =
        [
            new MessageTemplate(1, "Plantilla 1", "Hola", true),
            new MessageTemplate(1, "Plantilla repetida", "Hola otra vez", true),
            new MessageTemplate(3, "Plantilla 3", "Texto", true)
        ];

        MessageTemplateValidationResult result = MessageTemplateValidator.Validate(templates);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("duplicados", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WhenEnabledTemplateHasEmptyBody_ReturnsError()
    {
        MessageTemplate[] templates =
        [
            new MessageTemplate(1, "Plantilla 1", "Hola", true),
            new MessageTemplate(2, "Plantilla 2", "   ", true),
            new MessageTemplate(3, "Plantilla 3", "Texto", false)
        ];

        MessageTemplateValidationResult result = MessageTemplateValidator.Validate(templates);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("habilitada", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WhenTemplateNumberIsZero_ReturnsError()
    {
        MessageTemplate[] templates =
        [
            new MessageTemplate(0, "Plantilla 1", "Hola", true),
            new MessageTemplate(2, "Plantilla 2", "Texto", true),
            new MessageTemplate(3, "Plantilla 3", "Texto", true)
        ];

        MessageTemplateValidationResult result = MessageTemplateValidator.Validate(templates);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("mayor a 0", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WhenThereAreLessThanThreeTemplates_ReturnsError()
    {
        MessageTemplate[] templates =
        [
            new MessageTemplate(1, "Plantilla 1", "Hola", true),
            new MessageTemplate(2, "Plantilla 2", "Texto", true)
        ];

        MessageTemplateValidationResult result = MessageTemplateValidator.Validate(templates);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("al menos 3", StringComparison.OrdinalIgnoreCase));
    }
}
