namespace ZENO_API_II.DTOs.Assistant;

    public class AssistantReadDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public Guid UserId { get; set; }
    }

