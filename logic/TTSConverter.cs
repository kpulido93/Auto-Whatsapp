using System.Text;

namespace Automate_Whatsapp.Logic
{
    public class ElevenLabsTts
    {
        private readonly string apiKey;
        private readonly string voiceId;

        public ElevenLabsTts()
        {
            apiKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY")
                ?? throw new InvalidOperationException("Define la variable de entorno ELEVENLABS_API_KEY.");
            voiceId = Environment.GetEnvironmentVariable("ELEVENLABS_VOICE_ID")
                ?? throw new InvalidOperationException("Define la variable de entorno ELEVENLABS_VOICE_ID.");
        }

        public async Task<string?> ConvertToOggAsync(string text, string outputPath)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("xi-api-key", apiKey);

                var payload = new
                {
                    text = text,
                    model_id = "eleven_multilingual_v2",
                    voice_settings = new
                    {
                        stability = 0.75,
                        similarity_boost = 0.75
                    }
                };

                string json = System.Text.Json.JsonSerializer.Serialize(payload);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}?output_format=opus_48000_96", content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error HTTP: {response.StatusCode}");
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Detalles: {error}");
                    return null;
                }

                byte[] audioBytes = await response.Content.ReadAsByteArrayAsync();

                string? dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllBytes(outputPath, audioBytes);

                
                Console.WriteLine("✅ Audio generado correctamente: " + outputPath);
                return outputPath;
            }
        }
    }
}
