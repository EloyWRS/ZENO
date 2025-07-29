using ZENO_API_II.DTOs.Message;

namespace ZENO_API_II.Services.Interfaces;

public interface IAssistantMessageService
{
    Task<MessageReadDto> CreateMessageAsync(Guid threadId, MessageCreateDto dto);
}