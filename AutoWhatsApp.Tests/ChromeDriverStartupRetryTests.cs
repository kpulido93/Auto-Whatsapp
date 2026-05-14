using Automate_Whatsapp.Logic;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class ChromeDriverStartupRetryTests
{
    [Fact]
    public void CreateChromeDriverWithRetry_WhenTransientStartupFailsTwice_SucceedsOnThirdAttempt()
    {
        var line = CreateLine();
        var logs = new List<string>();
        var delays = new List<TimeSpan>();
        int attempts = 0;
        ChromeOptions? lastOptions = null;
        object expectedDriver = new();

        try
        {
            object driver = WhatsAppSender.CreateChromeDriverWithRetry(
                line,
                logs.Add,
                (_, options) =>
                {
                    attempts++;
                    lastOptions = options;

                    if (attempts <= 2)
                    {
                        throw new WebDriverException("session not created: Chrome failed to start: crashed. DevToolsActivePort file doesn't exist");
                    }

                    return expectedDriver;
                },
                delays.Add);

            Assert.Same(expectedDriver, driver);
            Assert.Equal(3, attempts);
            Assert.Equal(2, delays.Count);
            Assert.Contains(logs, log => log.Contains("Intento 1/3", StringComparison.Ordinal) && log.Contains("user-data-dir", StringComparison.Ordinal));
            Assert.Contains(logs, log => log.Contains("Fallo transitorio al abrir Chrome", StringComparison.Ordinal));
            Assert.Contains(logs, log => log.Contains("Reintentando apertura de Chrome", StringComparison.Ordinal));
            Assert.Contains(logs, log => log.Contains("Intento 3/3", StringComparison.Ordinal));
            Assert.NotNull(lastOptions);
            Assert.Contains("--start-maximized", lastOptions.Arguments);
            Assert.Contains("--no-first-run", lastOptions.Arguments);
            Assert.Contains("--no-default-browser-check", lastOptions.Arguments);
            Assert.Contains("--disable-popup-blocking", lastOptions.Arguments);
            Assert.Contains("--remote-allow-origins=*", lastOptions.Arguments);
            Assert.Contains(lastOptions.Arguments, argument => argument == "--profile-directory=Default");
            Assert.Contains(lastOptions.Arguments, argument => argument.StartsWith("--user-data-dir=", StringComparison.Ordinal));
        }
        finally
        {
            DeleteSessionDirectory(line.SessionPath);
        }
    }

    [Fact]
    public void CreateChromeDriverWithRetry_WhenTransientStartupAlwaysFails_ThrowsActionableMessage()
    {
        var line = CreateLine();
        var logs = new List<string>();
        int attempts = 0;

        try
        {
            var exception = Assert.Throws<WebDriverException>(() =>
                WhatsAppSender.CreateChromeDriverWithRetry<object>(
                    line,
                    logs.Add,
                    (_, _) =>
                    {
                        attempts++;
                        throw new WebDriverException("user data directory is already in use");
                    },
                    _ => { }));

            Assert.Equal(3, attempts);
            Assert.Contains("Cierra ventanas de Chrome abiertas con esta línea o cambia la ruta de sesión", exception.Message, StringComparison.Ordinal);
            Assert.Contains(Path.GetFullPath(line.SessionPath), exception.Message, StringComparison.Ordinal);
            Assert.Contains("profile-directory: Default", exception.Message, StringComparison.Ordinal);
            Assert.Contains(logs, log => log.Contains("Cierra ventanas de Chrome abiertas con esta línea o cambia la ruta de sesión", StringComparison.Ordinal));
        }
        finally
        {
            DeleteSessionDirectory(line.SessionPath);
        }
    }

    private static WhatsAppLine CreateLine()
    {
        return new WhatsAppLine
        {
            Id = "line-1",
            DisplayName = "Principal",
            SessionPath = Path.Combine(Path.GetTempPath(), "AutoWhatsAppTests", Guid.NewGuid().ToString("N")),
            ProfileDirectory = "Default",
            OperationalState = WhatsAppLineOperationalState.Ready
        };
    }

    private static void DeleteSessionDirectory(string sessionPath)
    {
        if (Directory.Exists(sessionPath))
        {
            Directory.Delete(sessionPath, recursive: true);
        }
    }
}
