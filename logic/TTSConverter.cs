using System.Text;

namespace Automate_Whatsapp.Logic
{
    public class ElevenLabsTts
    {
        private readonly string apiKey = "sk_4ebd263194ba8907d1f8228c27e1b0edf953c5ca5ab281e3";
        private readonly string voiceId = "IoWn77TsmQnza94sYlfg";

        public async Task<string> ConvertToOggAsync(string text, string outputPath)
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

                string dir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(dir))
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
