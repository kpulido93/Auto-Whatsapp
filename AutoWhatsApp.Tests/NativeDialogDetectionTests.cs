using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class NativeDialogDetectionTests
{
    [Theory]
    [InlineData("chrome", true)]
    [InlineData("chrome.exe", true)]
    [InlineData("chromedriver", true)]
    [InlineData("msedge", true)]
    [InlineData("dbeaver", false)]
    [InlineData("mychromehelper", false)]
    public void IsBrowserNativeDialogProcess_RequiresExactWhitelistedProcess(string processName, bool expected)
    {
        bool actual = WhatsAppSender.IsBrowserNativeDialogProcess(processName);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BuildNativeFileDialogDiagnostic_SeparatesBrowserAndExternalDialogs()
    {
        var candidates = new[]
        {
            new WhatsAppSender.NativeDialogWindowInfo
            {
                Handle = (IntPtr)1,
                Title = "Open",
                ClassName = "#32770",
                ProcessId = 10,
                ProcessName = "chrome",
                NormalizedProcessName = "chrome",
                IsBrowserProcess = true,
                HasStrictTitle = true
            },
            new WhatsAppSender.NativeDialogWindowInfo
            {
                Handle = (IntPtr)2,
                Title = "Conectar a base de datos",
                ClassName = "#32770",
                ProcessId = 20,
                ProcessName = "dbeaver",
                NormalizedProcessName = "dbeaver",
                IsBrowserProcess = false,
                HasPossibleTitle = false
            }
        };

        string diagnostic = WhatsAppSender.BuildNativeFileDialogDiagnostic(candidates, null);

        Assert.Contains("dialogos navegador considerados: 1", diagnostic, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dialogos externos ignorados: 1", diagnostic, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("estricto=si", diagnostic, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("procesoNormalizado=dbeaver", diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildNativeFileDialogDiagnostic_WithOnlyExternalDialog_DoesNotReportPossibleDialog()
    {
        var candidates = new[]
        {
            new WhatsAppSender.NativeDialogWindowInfo
            {
                Handle = (IntPtr)3,
                Title = "Conectar a base de datos",
                ClassName = "#32770",
                ProcessId = 30,
                ProcessName = "dbeaver",
                NormalizedProcessName = "dbeaver",
                IsBrowserProcess = false,
                AppearedAfterReference = true
            }
        };

        string diagnostic = WhatsAppSender.BuildNativeFileDialogDiagnostic(candidates, new HashSet<IntPtr>());

        Assert.Contains("posible=no", diagnostic, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dialogos externos ignorados: 1", diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClassifyForegroundWindow_RestorePagesIsBrowserPopupNotFileSelector()
    {
        string classification = WhatsAppSender.ClassifyForegroundWindow(
            "¿Quieres restaurar las páginas?",
            "Chrome_WidgetWin_1",
            "chrome");

        Assert.Equal("popup de navegador no selector de archivo", classification);
    }
}
