using System.ComponentModel.DataAnnotations;

namespace ZENO_API_II.Models;

public class ChatThread
{
    public Guid Id { get; set; }
    [MaxLength(200)]
    public string Title { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid AssistantId { get; set; }  // FK GUID
    public AssistantLocal Assistant { get; set; }

    public string? OpenAI_ThreadId { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}


