namespace ZENO_API_II.Services.Interfaces
{
    public interface IOpenAITextToSpeechService
    {
        Task<byte[]> GenerateSpeechAsync(string text, string voice = "nova", string format = "mp3", string model = "tts-1");
    }
}
