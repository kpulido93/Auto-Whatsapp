using System.Globalization;

namespace Automate_Whatsapp.Logic;

public sealed class ElevenLabsSettings
{
    public const string ApiKeyEnvironmentVariable = "ELEVENLABS_API_KEY";
    public const string VoiceIdEnvironmentVariable = "ELEVENLABS_VOICE_ID";
    public const string ModelIdEnvironmentVariable = "ELEVENLABS_MODEL_ID";
    public const string OutputFormatEnvironmentVariable = "ELEVENLABS_OUTPUT_FORMAT";
    public const string StabilityEnvironmentVariable = "ELEVENLABS_STABILITY";
    public const string SimilarityBoostEnvironmentVariable = "ELEVENLABS_SIMILARITY_BOOST";

    public const string DefaultModelId = "eleven_multilingual_v2";
    public const string DefaultOutputFormat = "opus_48000_96";
    public const double DefaultStability = 0.75;
    public const double DefaultSimilarityBoost = 0.75;

    public ElevenLabsSettings(
        string? apiKey,
        string? voiceId,
        string? modelId = null,
        string? outputFormat = null,
        double stability = DefaultStability,
        double similarityBoost = DefaultSimilarityBoost)
    {
        ApiKey = Normalize(apiKey);
        VoiceId = Normalize(voiceId);
        ModelId = string.IsNullOrWhiteSpace(modelId) ? DefaultModelId : modelId.Trim();
        OutputFormat = string.IsNullOrWhiteSpace(outputFormat) ? DefaultOutputFormat : outputFormat.Trim();
        Stability = stability;
        SimilarityBoost = similarityBoost;
    }

    public string ApiKey { get; }

    public string VoiceId { get; }

    public string ModelId { get; }

    public string OutputFormat { get; }

    public double Stability { get; }

    public double SimilarityBoost { get; }

    public static ElevenLabsSettings FromEnvironment()
    {
        return new ElevenLabsSettings(
            Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable),
            Environment.GetEnvironmentVariable(VoiceIdEnvironmentVariable),
            Environment.GetEnvironmentVariable(ModelIdEnvironmentVariable),
            Environment.GetEnvironmentVariable(OutputFormatEnvironmentVariable),
            ReadOptionalDouble(StabilityEnvironmentVariable, DefaultStability),
            ReadOptionalDouble(SimilarityBoostEnvironmentVariable, DefaultSimilarityBoost));
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            errors.Add($"Falta la variable obligatoria {ApiKeyEnvironmentVariable}.");
        }

        if (string.IsNullOrWhiteSpace(VoiceId))
        {
            errors.Add($"Falta la variable obligatoria {VoiceIdEnvironmentVariable}.");
        }

        ValidateRatio(Stability, StabilityEnvironmentVariable, errors);
        ValidateRatio(SimilarityBoost, SimilarityBoostEnvironmentVariable, errors);

        return errors;
    }

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static double ReadOptionalDouble(string variableName, double defaultValue)
    {
        string? rawValue = Environment.GetEnvironmentVariable(variableName);

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        return double.TryParse(
            rawValue,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double value)
            ? value
            : double.NaN;
    }

    private static void ValidateRatio(double value, string variableName, List<string> errors)
    {
        if (double.IsNaN(value) || value < 0 || value > 1)
        {
            errors.Add($"La variable {variableName} debe ser un numero entre 0 y 1.");
        }
    }
}
