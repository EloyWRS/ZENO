namespace ZENO_API_II.Services.Interfaces;

public interface ITokenEstimatorService
{
    int CountTokens(string input, string model = "gpt-4o");
    double EstimateCost(int promptTokens, int completionTokens, string model = "gpt-4o");
}

