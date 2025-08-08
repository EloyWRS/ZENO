using ZENO_API_II.DTOs.Assistant;

namespace ZENO_API_II.Services.Interfaces;

public interface IAssistantService
{
    Task<AssistantReadDto> GetAssistantAsync(Guid userId);
    Task<AssistantReadDto> CreateAssistantAsync(Guid userId, AssistantCreateDto dto);
    Task UpdateAssistantAsync(Guid userId, AssistantUpdateDto dto);
    Task UpdateAssistantInstructionsAsync(Guid userId, string instructions);
     Task<string> GetAssistantInstructionsAsync(Guid userId);
    Task DeleteAssistantInstructionsAsync(Guid userId);
    Task<object> GetOrCreateAssistantAndThreadAsync(Guid userId);
}


