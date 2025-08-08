using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using ZENO_API_II.Data;
using ZENO_API_II.DTOs.Assistant;
using ZENO_API_II.Models;
using ZENO_API_II.Services.Interfaces;

namespace ZENO_API_II.Services.Implementations;

public class AssistantService : IAssistantService
{
    private readonly ZenoDbContext _db;
    private readonly IConfiguration _config;
    private readonly IUserSetupService _userSetupService;

    public AssistantService(ZenoDbContext db, IConfiguration config, IUserSetupService userSetupService)
    {
        _db = db;
        _config = config;
        _userSetupService = userSetupService;
    }

    private HttpClient CreateOpenAiClient()
    {
        var apiKey = _config["OpenAI:ApiKey"];
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
        return httpClient;
    }

    public async Task<AssistantReadDto> GetAssistantAsync(Guid userId)
    {
        var assistant = await _db.Assistants.FirstOrDefaultAsync(a => a.UserLocalId == userId);
        if (assistant == null) throw new Exception("Assistant not found");
        if (string.IsNullOrEmpty(assistant.OpenAI_Id)) throw new Exception("Assistant not linked to OpenAI");

        using var httpClient = CreateOpenAiClient();
        var response = await httpClient.GetAsync($"https://api.openai.com/v1/assistants/{assistant.OpenAI_Id}");
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new Exception(json);

        dynamic result = JsonConvert.DeserializeObject(json);
        return new AssistantReadDto
        {
            Id = assistant.Id,
            Name = result.name,
            Description = result.instructions,
            CreatedAt = assistant.CreatedAt,
            UserId = assistant.UserLocalId
        };
    }

        public async Task<string> GetAssistantInstructionsAsync(Guid userId)
        {
            var assistant = await _db.Assistants.FirstOrDefaultAsync(a => a.UserLocalId == userId);
            if (assistant == null) throw new Exception("Assistant not found");
            if (string.IsNullOrEmpty(assistant.OpenAI_Id)) throw new Exception("Assistant not linked to OpenAI");

            using var httpClient = CreateOpenAiClient();
            var response = await httpClient.GetAsync($"https://api.openai.com/v1/assistants/{assistant.OpenAI_Id}");
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new Exception(json);

            dynamic result = JsonConvert.DeserializeObject(json);
            return (string)result.instructions;
        }

    public async Task<AssistantReadDto> CreateAssistantAsync(Guid userId, AssistantCreateDto dto)
    {
        var user = await _db.Users.Include(u => u.Assistant).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) throw new Exception("User not found");
        if (user.Assistant != null) throw new InvalidOperationException("User already has an assistant");

        using var httpClient = CreateOpenAiClient();

        var body = new
        {
            name = dto.Name,
            instructions = dto.Description ?? "",
            model = "gpt-4o",
            temperature = 0.7
        };

        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://api.openai.com/v1/assistants", content);
        var responseJson = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new Exception(responseJson);

        dynamic result = JsonConvert.DeserializeObject(responseJson);
        string openaiId = result.id;

        var assistant = new AssistantLocal
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow,
            UserLocalId = userId,
            OpenAI_Id = openaiId
        };

        _db.Assistants.Add(assistant);
        await _db.SaveChangesAsync();

        return new AssistantReadDto
        {
            Id = assistant.Id,
            Name = assistant.Name,
            Description = assistant.Description,
            CreatedAt = assistant.CreatedAt,
            UserId = assistant.UserLocalId
        };
    }

        public async Task UpdateAssistantAsync(Guid userId, AssistantUpdateDto dto)
    {
        var assistant = await _db.Assistants.FirstOrDefaultAsync(a => a.UserLocalId == userId);
        if (assistant == null) throw new Exception("Assistant not found");
        if (string.IsNullOrEmpty(assistant.OpenAI_Id)) throw new Exception("Assistant not linked to OpenAI");

        using var httpClient = CreateOpenAiClient();
        var body = new
        {
            name = dto.Name ?? assistant.Name,
            instructions = dto.Instructions ?? dto.Description ?? assistant.Description
        };
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"https://api.openai.com/v1/assistants/{assistant.OpenAI_Id}", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new Exception(responseBody);

        assistant.Name = dto.Name ?? assistant.Name;
        assistant.Description = dto.Description ?? assistant.Description;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAssistantInstructionsAsync(Guid userId, string instructions)
    {
        var assistant = await _db.Assistants.FirstOrDefaultAsync(a => a.UserLocalId == userId);
        if (assistant == null) throw new Exception("Assistant not found");
        if (string.IsNullOrEmpty(assistant.OpenAI_Id)) throw new Exception("Assistant not linked to OpenAI");

        using var httpClient = CreateOpenAiClient();
        var body = new { instructions };
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"https://api.openai.com/v1/assistants/{assistant.OpenAI_Id}", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new Exception(responseBody);

        assistant.Description = instructions;
        await _db.SaveChangesAsync();
    }

        public async Task DeleteAssistantInstructionsAsync(Guid userId)
        {
            var assistant = await _db.Assistants.FirstOrDefaultAsync(a => a.UserLocalId == userId);
            if (assistant == null) throw new Exception("Assistant not found");
            if (string.IsNullOrEmpty(assistant.OpenAI_Id)) throw new Exception("Assistant not linked to OpenAI");

            using var httpClient = CreateOpenAiClient();
            var body = new { instructions = "" };
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"https://api.openai.com/v1/assistants/{assistant.OpenAI_Id}", content);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new Exception(responseBody);

            assistant.Description = string.Empty;
            await _db.SaveChangesAsync();
        }

    public async Task<object> GetOrCreateAssistantAndThreadAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) throw new Exception("User not found");

        await _userSetupService.SetupNewUserAsync(user);

        var assistant = await _db.Assistants.Include(a => a.Threads).FirstOrDefaultAsync(a => a.UserLocalId == userId);
        if (assistant == null) throw new Exception("Assistant not found");
        var latestThread = assistant.Threads.OrderByDescending(t => t.CreatedAt).FirstOrDefault();
        if (latestThread == null) throw new Exception("Thread not found");

        return new
        {
            assistant = new
            {
                id = assistant.Id,
                name = assistant.Name,
                description = assistant.Description,
                createdAt = assistant.CreatedAt,
                userId = assistant.UserLocalId
            },
            thread = new
            {
                id = latestThread.Id,
                title = latestThread.Title,
                createdAt = latestThread.CreatedAt,
                assistantId = latestThread.AssistantId,
                openaiThreadId = latestThread.OpenAI_ThreadId
            }
        };
    }
}


