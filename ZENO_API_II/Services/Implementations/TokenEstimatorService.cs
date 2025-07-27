using SharpToken;
using ZENO_API_II.Services.Interfaces;

namespace ZENO_API_II.Services.Implementations;

public class TokenEstimatorService : ITokenEstimatorService
{
    public int CountTokens(string input, string model = "gpt-4o")
    {
        var encoding = GptEncoding.GetEncodingForModel(model);
        return encoding.Encode(input).Count;
    }

    public double EstimateCost(int promptTokens, int completionTokens, string model = "gpt-4o")
    {
        return model switch
        {
            "gpt-4o" => (promptTokens / 1000.0) * 0.005 + (completionTokens / 1000.0) * 0.015,
            "gpt-3.5-turbo" => (promptTokens / 1000.0) * 0.0005 + (completionTokens / 1000.0) * 0.0015,
            _ => throw new NotImplementedException($"Modelo {model} não suportado.")
        };
    }
}