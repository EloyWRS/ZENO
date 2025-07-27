namespace ZENO_API_II.DTOs.Thread
{
    public class ChatThreadReadDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }

        public Guid AssistantId { get; set; }
        public string OpenAI_ThreadId { get; set; }
    }

}
