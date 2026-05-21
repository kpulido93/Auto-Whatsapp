using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class AudioAttachDiagnosticsTests
{
    [Fact]
    public void BuildAttachmentMenuAudioDiagnostics_ReportsRejectedCandidateOutsideAttachmentMenu()
    {
        var rejectedCandidate = new WhatsAppSender.AudioOptionCandidateDiagnosticItem
        {
            Origin = "button-li-tabindex-texto-audio",
            Snapshot = new WhatsAppSender.AudioOptionCandidateSnapshot
            {
                TagName = "div",
                Role = "gridcell",
                TabIndex = "0",
                Text = "+1 (829) 733-3636 viernes Audio",
                HasExactAudioText = true,
                Width = 555,
                Height = 72
            },
            Evaluation = new WhatsAppSender.AudioOptionCandidateEvaluation
            {
                Accepted = false,
                Reason = "fuera del menu de adjuntos"
            }
        };

        string diagnostic = WhatsAppSender.BuildAttachmentMenuAudioDiagnostics(
            menuContainerFound: true,
            fileInputs: [],
            acceptedCandidates: [],
            rejectedCandidates: [rejectedCandidate]);

        Assert.Contains("ignorado fuera del menu de adjuntos", diagnostic, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("role=gridcell", diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildFileInputsDiagnosticSummary_WhenOnlyImageInput_ReportsImageOnly()
    {
        string diagnostic = WhatsAppSender.BuildFileInputsDiagnosticSummary(
        [
            new WhatsAppSender.FileInputDiagnosticItem
            {
                Accept = "image/*",
                IsAudio = false,
                IsDisplayed = true,
                IsEnabled = true,
                Name = "image",
                AriaLabel = "image"
            }
        ]);

        Assert.Contains("solo input de imagen detectado", diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildRejectedAudioOptionCandidatesDiagnostics_LimitsCandidatesAndTextPreview()
    {
        string longText = "Audio " + new string('x', 120) + " tail";
        var rejectedCandidates = Enumerable.Range(1, 6)
            .Select(index => new WhatsAppSender.AudioOptionCandidateDiagnosticItem
            {
                Origin = $"origin-{index}",
                Snapshot = new WhatsAppSender.AudioOptionCandidateSnapshot
                {
                    TagName = "div",
                    Role = "gridcell",
                    TabIndex = "0",
                    Text = longText,
                    HasExactAudioText = true,
                    Width = 555,
                    Height = 72
                },
                Evaluation = new WhatsAppSender.AudioOptionCandidateEvaluation
                {
                    Accepted = false,
                    Reason = "fuera del menu de adjuntos"
                }
            })
            .ToList();

        string diagnostic = WhatsAppSender.BuildRejectedAudioOptionCandidatesDiagnostics(rejectedCandidates);

        Assert.Contains("+1 candidatos mas", diagnostic, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tail", diagnostic, StringComparison.OrdinalIgnoreCase);
    }
}
