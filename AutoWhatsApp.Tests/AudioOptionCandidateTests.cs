using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class AudioOptionCandidateTests
{
    [Fact]
    public void EvaluateAudioOptionCandidate_RejectsChatListGridCellOutsideAttachmentMenu()
    {
        var snapshot = CreateSnapshot(new WhatsAppSender.AudioOptionCandidateSnapshot
        {
            TagName = "div",
            Role = "gridcell",
            TabIndex = "0",
            Text = "+1 (829) 733-3636 viernes\r\nAudio",
            HasExactAudioText = true,
            Width = 555,
            Height = 72
        });

        WhatsAppSender.AudioOptionCandidateEvaluation evaluation = WhatsAppSender.EvaluateAudioOptionCandidate(snapshot);

        Assert.False(evaluation.Accepted);
        Assert.Equal("fuera del menu de adjuntos", evaluation.Reason);
    }

    [Theory]
    [InlineData("div", "menuitem", false)]
    [InlineData("li", "button", true)]
    public void EvaluateAudioOptionCandidate_AcceptsRealAttachmentMenuItem(
        string tagName,
        string role,
        bool hasHeadphonesIcon)
    {
        var snapshot = CreateSnapshot(new WhatsAppSender.AudioOptionCandidateSnapshot
        {
            TagName = tagName,
            Role = role,
            TabIndex = "0",
            Text = "Audio",
            HasExactAudioText = true,
            HasHeadphonesIcon = hasHeadphonesIcon,
            InAttachmentMenu = true
        });

        WhatsAppSender.AudioOptionCandidateEvaluation evaluation = WhatsAppSender.EvaluateAudioOptionCandidate(snapshot);

        Assert.True(evaluation.Accepted);
        Assert.Equal("aceptado", evaluation.Reason);
    }

    [Fact]
    public void EvaluateAudioOptionCandidate_RejectsTabIndexZeroOutsideAttachmentMenu()
    {
        var snapshot = CreateSnapshot(new WhatsAppSender.AudioOptionCandidateSnapshot
        {
            TagName = "div",
            Role = "button",
            TabIndex = "0",
            Text = "Audio",
            HasExactAudioText = true
        });

        WhatsAppSender.AudioOptionCandidateEvaluation evaluation = WhatsAppSender.EvaluateAudioOptionCandidate(snapshot);

        Assert.False(evaluation.Accepted);
        Assert.Equal("fuera del menu de adjuntos", evaluation.Reason);
    }

    private static WhatsAppSender.AudioOptionCandidateSnapshot CreateSnapshot(WhatsAppSender.AudioOptionCandidateSnapshot overrides)
    {
        return new WhatsAppSender.AudioOptionCandidateSnapshot
        {
            TagName = overrides.TagName,
            Role = overrides.Role,
            TabIndex = overrides.TabIndex,
            Text = overrides.Text,
            HasExactAudioText = overrides.HasExactAudioText,
            HasHeadphonesIcon = overrides.HasHeadphonesIcon,
            InAttachmentMenu = overrides.InAttachmentMenu,
            InSidePane = overrides.InSidePane,
            HasGridAncestor = overrides.HasGridAncestor,
            HasGridCellAncestor = overrides.HasGridCellAncestor,
            HasListItemAncestor = overrides.HasListItemAncestor,
            HasChildElements = overrides.HasChildElements || string.Equals(overrides.TagName, "div", StringComparison.OrdinalIgnoreCase),
            IsDisplayed = !overrides.Text.Equals("hidden", StringComparison.OrdinalIgnoreCase),
            IsEnabled = !overrides.Text.Equals("disabled", StringComparison.OrdinalIgnoreCase),
            AriaDisabled = overrides.AriaDisabled,
            IsInsideFooter = overrides.IsInsideFooter,
            Width = overrides.Width == 0 ? 120 : overrides.Width,
            Height = overrides.Height == 0 ? 48 : overrides.Height
        };
    }
}
