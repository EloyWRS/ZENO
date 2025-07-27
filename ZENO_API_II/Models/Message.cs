using System.ComponentModel.DataAnnotations;

namespace ZENO_API_II.Models
{
    public class Message
    {
        public Guid Id { get; set; }

        public Guid ThreadId { get; set; }

        public ChatThread Thread { get; set; }

        [Required]
        public string Role { get; set; } // "user" ou "assistant"

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
