namespace ZENO_API_II.DTOs.Assistant;

    public class AssistantUpdateDto
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        // Optional: override full instructions of the assistant at OpenAI
        public string? Instructions { get; set; }
    }

