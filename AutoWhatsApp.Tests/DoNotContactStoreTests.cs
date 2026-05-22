using Automate_Whatsapp.Logic;
using Xunit;

namespace AutoWhatsApp.Tests;

public sealed class DoNotContactStoreTests
{
    [Fact]
    public void NormalizePhone_StripsFormattingAndCombinesCountryCodeWithPhone()
    {
        string normalizedPhone = DoNotContactEntry.NormalizePhone(" +57 ", "300 111-22(33)");

        Assert.Equal("573001112233", normalizedPhone);
    }

    [Fact]
    public void IsBlocked_WhenNormalizedPhoneMatchesExactly_ReturnsTrue()
    {
        var entries = new[]
        {
            new DoNotContactEntry("+57", "300 111 2233", "opt-out")
        };

        bool blocked = DoNotContactStore.IsBlocked(entries, "57", "(300)111-2233");
        bool otherNumberBlocked = DoNotContactStore.IsBlocked(entries, "57", "3001112244");

        Assert.True(blocked);
        Assert.False(otherNumberBlocked);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsEntries()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string configPath = Path.Combine(tempDir, DoNotContactStore.ConfigFileName);

        try
        {
            var entries = new[]
            {
                new DoNotContactEntry("57", "3001112233", "solicitud del usuario", new DateTime(2026, 5, 21))
            };

            DoNotContactStore.Save(entries, configPath);

            Assert.True(DoNotContactStore.TryLoad(out var loadedEntries, configPath));
            var loadedEntry = Assert.Single(loadedEntries);
            Assert.Equal("573001112233", loadedEntry.NormalizedPhone);
            Assert.Equal("solicitud del usuario", loadedEntry.Reason);
            Assert.Equal(new DateTime(2026, 5, 21), loadedEntry.CreatedAt);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
