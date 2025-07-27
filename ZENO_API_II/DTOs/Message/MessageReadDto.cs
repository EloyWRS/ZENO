namespace ZENO_API_II.DTOs.Message;

    public class MessageReadDto
    {
        public Guid Id { get; set; }
        public Guid ThreadId { get; set; }
        public string Role { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }

