using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using ZENO_API_II.Data;
using ZENO_API_II.DTOs.Message;
using ZENO_API_II.Models;
using ZENO_API_II.Services.Interfaces;
using ZENO_API_II.Exceptions;
using ZENO_API_II.Constants;

namespace ZENO_API_II.Services.Implementations
{
    public class AssistantMessageService : IAssistantMessageService
    {
        private readonly ZenoDbContext _db;
        private readonly IConfiguration _config;
        private readonly ITokenEstimatorService _tokenEstimator;
        private readonly ICreditService _creditService;

        public AssistantMessageService(
            ZenoDbContext db,
            IConfiguration config,
            ITokenEstimatorService tokenEstimator,
            ICreditService creditService)
        {
            _db = db;
            _config = config;
            _tokenEstimator = tokenEstimator;
            _creditService = creditService;
        }

        public async Task<MessageReadDto> CreateMessageAsync(Guid threadId, MessageCreateDto dto)
        {
            var thread = await _db.Threads.Include(t => t.Assistant).FirstOrDefaultAsync(t => t.Id == threadId);
            if (thread == null) 
                throw new BusinessException("Thread não encontrada.", ErrorCodes.THREAD_NOT_FOUND, 404);

            if (string.IsNullOrWhiteSpace(thread.OpenAI_ThreadId))
                throw new BusinessException("Thread da OpenAI não está configurada.", ErrorCodes.THREAD_NOT_FOUND, 400);

            var assistant = thread.Assistant;
            if (assistant == null || string.IsNullOrWhiteSpace(assistant.OpenAI_Id))
                throw new BusinessException("Assistente inválido ou não ligado à OpenAI.", ErrorCodes.ASSISTANT_NOT_FOUND, 400);

            var userId = assistant.UserLocalId;
            var promptTokens = _tokenEstimator.CountTokens(dto.Content);
            var estimatedCompletionTokens = 300;
            var estimatedCostUSD = _tokenEstimator.EstimateCost(promptTokens, estimatedCompletionTokens, "gpt-4o");
            var estimatedCredits = (int)Math.Ceiling(estimatedCostUSD * 1000 * 10);

            var hasCredits = await _creditService.HasEnoughCredits(userId, estimatedCredits);
            if (!hasCredits) 
                throw new BusinessException("Créditos insuficientes.", ErrorCodes.INSUFFICIENT_CREDITS, 402);

            var apiKey = _config["OpenAI:ApiKey"];
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

            var msgBody = new { role = "user", content = dto.Content };
            var msgContent = new StringContent(JsonConvert.SerializeObject(msgBody), Encoding.UTF8, "application/json");
            var openaiThreadId = thread.OpenAI_ThreadId;

            var msgResponse = await httpClient.PostAsync($"https://api.openai.com/v1/threads/{openaiThreadId}/messages", msgContent);
            if (!msgResponse.IsSuccessStatusCode)
                throw new BusinessException(await msgResponse.Content.ReadAsStringAsync(), ErrorCodes.OPENAI_ERROR, (int)msgResponse.StatusCode);

            var runBody = new { assistant_id = assistant.OpenAI_Id };
            var runContent = new StringContent(JsonConvert.SerializeObject(runBody), Encoding.UTF8, "application/json");
            var runRes = await httpClient.PostAsync($"https://api.openai.com/v1/threads/{openaiThreadId}/runs", runContent);
            var runJson = await runRes.Content.ReadAsStringAsync();
            if (!runRes.IsSuccessStatusCode) 
                throw new BusinessException(runJson, ErrorCodes.OPENAI_ERROR, (int)runRes.StatusCode);

            string runId = JsonConvert.DeserializeObject<dynamic>(runJson).id;
            string status = "in_progress";

            while (status == "queued" || status == "in_progress")
            {
                await Task.Delay(1000);
                var checkRes = await httpClient.GetAsync($"https://api.openai.com/v1/threads/{openaiThreadId}/runs/{runId}");
                var checkJson = await checkRes.Content.ReadAsStringAsync();
                if (!checkRes.IsSuccessStatusCode) 
                    throw new BusinessException(checkJson, ErrorCodes.OPENAI_ERROR, (int)checkRes.StatusCode);
                status = JsonConvert.DeserializeObject<dynamic>(checkJson).status;
            }

            var replyRes = await httpClient.GetAsync($"https://api.openai.com/v1/threads/{openaiThreadId}/messages");
            var replyJson = await replyRes.Content.ReadAsStringAsync();
            if (!replyRes.IsSuccessStatusCode) 
                throw new BusinessException(replyJson, ErrorCodes.OPENAI_ERROR, (int)replyRes.StatusCode);
            string finalReply = JsonConvert.DeserializeObject<dynamic>(replyJson).data[0].content[0].text.value;

            var now = DateTime.UtcNow;

            var userMessage = new Message
            {
                Id = Guid.NewGuid(),
                ThreadId = thread.Id,
                Role = "user",
                Content = dto.Content,
                CreatedAt = now
            };

            var assistantMessage = new Message
            {
                Id = Guid.NewGuid(),
                ThreadId = thread.Id,
                Role = "assistant",
                Content = finalReply,
                CreatedAt = now
            };

            _db.Messages.AddRange(userMessage, assistantMessage);
            await _db.SaveChangesAsync();

            await _creditService.ConsumeCredits(userId, estimatedCredits, $"Mensagem enviada (tokens estimados: {promptTokens + estimatedCompletionTokens})");

            return new MessageReadDto
            {
                Id = assistantMessage.Id,
                ThreadId = assistantMessage.ThreadId,
                Role = assistantMessage.Role,
                Content = assistantMessage.Content,
                CreatedAt = assistantMessage.CreatedAt
            };
        }
    }
}
