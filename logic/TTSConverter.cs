using System.Text;

namespace Automate_Whatsapp.Logic
{
    public class ElevenLabsTts
    {
        private readonly ElevenLabsSettings settings;
        private readonly Action<string> log;

        public ElevenLabsTts()
            : this(ElevenLabsSettingsStore.LoadOrEnvironment(), Console.WriteLine)
        {
        }

        public ElevenLabsTts(Action<string> log)
            : this(ElevenLabsSettingsStore.LoadOrEnvironment(), log)
        {
        }

        public ElevenLabsTts(ElevenLabsSettings settings, Action<string>? log = null)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.log = log ?? Console.WriteLine;
        }

        public virtual async Task<string> ConvertToOggAsync(string text, string outputPath)
        {
            IReadOnlyList<string> configurationErrors = settings.Validate();
            if (configurationErrors.Count > 0)
            {
                log("Configuracion ElevenLabs incompleta o invalida. " + string.Join(" ", configurationErrors));
                return string.Empty;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("xi-api-key", settings.ApiKey);

                var payload = new
                {
                    text = text,
                    model_id = settings.ModelId,
                    voice_settings = new
                    {
                        stability = settings.Stability,
                        similarity_boost = settings.SimilarityBoost
                    }
                };

                string json = System.Text.Json.JsonSerializer.Serialize(payload);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                string requestUrl = "https://api.elevenlabs.io/v1/text-to-speech/"
                    + Uri.EscapeDataString(settings.VoiceId)
                    + "?output_format="
                    + Uri.EscapeDataString(settings.OutputFormat);

                var response = await client.PostAsync(requestUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    log($"Error HTTP de ElevenLabs: {response.StatusCode}");
                    var error = await response.Content.ReadAsStringAsync();
                    log($"Detalles: {error}");
                    return string.Empty;
                }

                byte[] audioBytes = await response.Content.ReadAsByteArrayAsync();

                string? dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                await File.WriteAllBytesAsync(outputPath, audioBytes);

                log("Audio generado correctamente: " + outputPath);
                return outputPath;
            }
        }
    }
}
