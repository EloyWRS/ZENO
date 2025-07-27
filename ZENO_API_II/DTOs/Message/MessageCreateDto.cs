using System.ComponentModel.DataAnnotations;

namespace ZENO_API_II.DTOs.Message;

public class MessageCreateDto
{
    [Required]
    public string Role { get; set; } // "user" ou "assistant"

    [Required]
    public string Content { get; set; }
}

