using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using ZENO_API_II.Data;
using ZENO_API_II.Models;
using ZENO_API_II.Services.Interfaces;

namespace ZENO_API_II.Services.Implementations
{
    public class UserSetupService : IUserSetupService
    {
        private readonly ZenoDbContext _db;
        private readonly IConfiguration _config;

        public UserSetupService(ZenoDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task SetupNewUserAsync(UserLocal user)
        {
            // Create assistant for the user
            var assistant = await CreateAssistantForUserAsync(user);
            
            // Create initial thread for the assistant
            await CreateInitialThreadForAssistantAsync(assistant);
        }

        private async Task<AssistantLocal> CreateAssistantForUserAsync(UserLocal user)
        {
            // Check if user already has an assistant
            var existingAssistant = await _db.Assistants
                .FirstOrDefaultAsync(a => a.UserLocalId == user.Id);

            if (existingAssistant != null)
                return existingAssistant;

            // Create assistant in OpenAI
            var apiKey = _config["OpenAI:ApiKey"];
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

            var body = new
            {
                name = "Zeno",
                instructions = "És um assistente pessoal chamado ZENO. Sê informal, leal e com memória.",
                model = "gpt-4o",
                temperature = 0.7
            };

            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.openai.com/v1/assistants", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to create OpenAI assistant: {responseJson}");

            dynamic result = JsonConvert.DeserializeObject(responseJson);
            string openaiId = result.id;

            // Create local assistant
            var assistant = new AssistantLocal
            {
                Id = Guid.NewGuid(),
                Name = "Zeno",
                Description = "Personal assistant",
                CreatedAt = DateTime.UtcNow,
                UserLocalId = user.Id,
                OpenAI_Id = openaiId
            };

            _db.Assistants.Add(assistant);
            await _db.SaveChangesAsync();

            return assistant;
        }

        private async Task<ChatThread> CreateInitialThreadForAssistantAsync(AssistantLocal assistant)
        {
            // Check if assistant already has threads
            var existingThreads = await _db.Threads
                .Where(t => t.AssistantId == assistant.Id)
                .ToListAsync();

            if (existingThreads.Any())
                return existingThreads.OrderByDescending(t => t.CreatedAt).First();

            // Create thread in OpenAI
            var apiKey = _config["OpenAI:ApiKey"];
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

            var response = await httpClient.PostAsync("https://api.openai.com/v1/threads", null);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to create OpenAI thread: {responseBody}");

            dynamic result = JsonConvert.DeserializeObject(responseBody);
            string openaiThreadId = result.id;

            // Create local thread
            var thread = new ChatThread
            {
                Id = Guid.NewGuid(),
                Title = "Nova Thread",
                CreatedAt = DateTime.UtcNow,
                AssistantId = assistant.Id,
                OpenAI_ThreadId = openaiThreadId
            };

            _db.Threads.Add(thread);
            await _db.SaveChangesAsync();

            return thread;
        }
    }
} 