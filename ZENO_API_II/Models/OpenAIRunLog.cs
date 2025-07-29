namespace ZENO_API_II.Models;

public class OpenAIRunLog
{
    public Guid Id { get; set; }

    public string RunId { get; set; }
    public Guid UserId { get; set; }
    public Guid AssistantId { get; set; }
    public Guid ThreadId { get; set; }

    public int PromptTokens { get; set; }
    public int EstimatedCompletionTokens { get; set; }
    public int CreditsCharged { get; set; }
    public double EstimatedCostUSD { get; set; }

    public string Status { get; set; } // "success", "failed", etc.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


