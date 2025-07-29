using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using ZENO_API_II.Services.Interfaces;

namespace ZENO_API_II.Services.Implementations;

    public class OpenAITextToSpeechService : IOpenAITextToSpeechService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public OpenAITextToSpeechService(IConfiguration config)
        {
            _apiKey = config["OpenAI:ApiKey"];
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<byte[]> GenerateSpeechAsync(string text, string voice = "nova", string format = "mp3", string model = "tts-1")
        {
            var body = new
            {
                model = model,
                input = text,
                voice = voice,
                response_format = format
            };

            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/speech", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI TTS error: {error}");
            }

            return await response.Content.ReadAsByteArrayAsync();

        }


    }
