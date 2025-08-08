namespace ZENO_API_II.Services.Interfaces;

public interface IAudioTranscriptionService
{
    Task<string> TranscribeAudioAsync(IFormFile audioFile, string? language = null);
}
