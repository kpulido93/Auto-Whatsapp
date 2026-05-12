using System.Text.Json;

namespace Automate_Whatsapp.Logic;

public static class WhatsAppLineManager
{
    public const string ConfigFileName = "whatsapp-lines.json";
    private const string AppFolderName = "AutoWhatsApp";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public static IReadOnlyList<WhatsAppLine> LoadEnabledLines()
    {
        return GetEnabledLines();
    }

    public static IReadOnlyList<WhatsAppLine> GetEnabledLines()
    {
        var enabledLines = LoadLines()
            .Where(line => line.Enabled)
            .OrderByPriority()
            .ToList();

        return enabledLines.Count > 0 ? enabledLines : new[] { CreateDefaultLine() };
    }

    public static IReadOnlyList<WhatsAppLine> GetFallbackCandidates(
        IEnumerable<WhatsAppLine> lines,
        WhatsAppLine? currentLine,
        ISet<string> unhealthyLineIds,
        bool requireReady = false)
    {
        string? currentLineId = currentLine?.Id;

        return lines
            .Where(line => line.Enabled)
            .Where(line => !IsSameLine(line.Id, currentLineId))
            .Where(line => !unhealthyLineIds.Contains(line.Id))
            .Where(line => !requireReady || line.OperationalState == WhatsAppLineOperationalState.Ready)
            .OrderByPriority()
            .ToList();
    }

    public static IReadOnlyList<WhatsAppLine> LoadLines()
    {
        string resolvedPath = GetConfigPath();
        try
        {
            if (!File.Exists(resolvedPath))
            {
                return CreateDefaultLinesAndTrySave();
            }

            string json = File.ReadAllText(resolvedPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return CreateDefaultLinesAndTrySave();
            }

            var lines = JsonSerializer.Deserialize<List<WhatsAppLine>>(json, JsonOptions) ?? new List<WhatsAppLine>();
            var validLines = lines
                .Where(IsValid)
                .Select(Normalize)
                .ToList();

            if (validLines.Count > 0)
            {
                return validLines.OrderByPriority().ToList();
            }
        }
        catch (JsonException)
        {
            return CreateDefaultLinesAndTrySave();
        }
        catch (IOException)
        {
            return CreateDefaultLinesAndTrySave();
        }
        catch (UnauthorizedAccessException)
        {
            return new[] { CreateDefaultLine() };
        }

        return CreateDefaultLinesAndTrySave();
    }

    public static void SaveLines(IEnumerable<WhatsAppLine> lines)
    {
        var normalizedLines = lines
            .Where(IsValid)
            .Select(Normalize)
            .OrderByPriority()
            .ToList();

        if (normalizedLines.Count == 0)
        {
            normalizedLines.Add(CreateDefaultLine());
        }

        string configPath = GetConfigPath();
        string configDirectory = Path.GetDirectoryName(configPath)!;
        Directory.CreateDirectory(configDirectory);

        string json = JsonSerializer.Serialize(normalizedLines, JsonOptions);
        File.WriteAllText(configPath, json);
    }

    public static WhatsAppLine CreateDefaultLine()
    {
        return new WhatsAppLine
        {
            Id = "principal",
            DisplayName = "Principal",
            SessionPath = GetDefaultSessionPath("principal"),
            ProfileDirectory = "Default",
            Enabled = true,
            Priority = 1
        };
    }

    public static WhatsAppLine DefaultLine => CreateDefaultLine();

    public static string GetConfigPath()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, AppFolderName, ConfigFileName);
    }

    public static string GetDefaultSessionPath(string id)
    {
        return $"ChromeUserData/{id}";
    }

    private static IReadOnlyList<WhatsAppLine> CreateDefaultLinesAndTrySave()
    {
        var fallbackLines = new[] { CreateDefaultLine() };

        try
        {
            SaveLines(fallbackLines);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return fallbackLines;
    }

    private static bool IsValid(WhatsAppLine? line)
    {
        return line != null
            && !string.IsNullOrWhiteSpace(line.Id)
            && !string.IsNullOrWhiteSpace(line.DisplayName)
            && !string.IsNullOrWhiteSpace(line.SessionPath);
    }

    private static WhatsAppLine Normalize(WhatsAppLine line)
    {
        return line with
        {
            Id = line.Id.Trim(),
            DisplayName = line.DisplayName.Trim(),
            SessionPath = line.SessionPath.Trim(),
            ProfileDirectory = string.IsNullOrWhiteSpace(line.ProfileDirectory)
                ? "Default"
                : line.ProfileDirectory.Trim(),
            OperationalState = WhatsAppLineOperationalState.Unknown
        };
    }

    private static IOrderedEnumerable<WhatsAppLine> OrderByPriority(this IEnumerable<WhatsAppLine> lines)
    {
        return lines
            .OrderBy(line => line.Priority)
            .ThenBy(line => line.DisplayName, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsSameLine(string lineId, string? otherLineId)
    {
        return string.Equals(lineId, otherLineId, StringComparison.OrdinalIgnoreCase);
    }
}
