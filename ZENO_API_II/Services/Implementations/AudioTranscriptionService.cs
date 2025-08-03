using Newtonsoft.Json;
using System.Net.Http.Headers;
using ZENO_API_II.Services.Interfaces;

namespace ZENO_API_II.Services.Implementations;

public class AudioTranscriptionService : IAudioTranscriptionService
{
    private readonly IConfiguration _config;

    public AudioTranscriptionService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string> TranscribeAudioAsync(IFormFile audioFile)
    {
        var apiKey = _config["OpenAI:ApiKey"];
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var form = new MultipartFormDataContent();
        using var stream = audioFile.OpenReadStream();
        form.Add(new StreamContent(stream), "file", audioFile.FileName);
        form.Add(new StringContent("whisper-1"), "model");
        form.Add(new StringContent("pt"), "language");

        var response = await httpClient.PostAsync("https://api.openai.com/v1/audio/transcriptions", form);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception(responseBody);

        dynamic result = JsonConvert.DeserializeObject(responseBody);
        return result.text;
    }
}

