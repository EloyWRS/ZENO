using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using ZENO_API_II.Data;
using ZENO_API_II.DTOs.Message;
using ZENO_API_II.Models;
using ZENO_API_II.Services.Interfaces;

namespace ZENO_API_II.Controllers;

    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly ZenoDbContext _db;
        private readonly IConfiguration _config;

        public MessagesController(ZenoDbContext db, IConfiguration configuration )
        {
            _db = db;
            _config = configuration;
    }

        [HttpGet("api/threads/{threadId}/messages")]
        public async Task<ActionResult<IEnumerable<MessageReadDto>>> GetMessagesByThread(Guid threadId)
        {
            var thread = await _db.Threads.FindAsync(threadId);

            if (thread == null)
                return NotFound("Thread não encontrada.");

            var messages = await _db.Messages
                .Where(m => m.ThreadId == threadId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new MessageReadDto
                {
                    Id = m.Id,
                    ThreadId = m.ThreadId,
                    Role = m.Role,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(messages);
        }


    // GET /api/messages/{id}
    [HttpGet("api/messages/{id}")]
        public async Task<ActionResult<MessageReadDto>> GetMessageById(Guid id)
        {
            var message = await _db.Messages.FindAsync(id);

            if (message == null)
                return NotFound("Mensagem não encontrada.");

            var result = new MessageReadDto
            {
                Id = message.Id,
                ThreadId = message.ThreadId,
                Role = message.Role,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            };

            return Ok(result);
        }


    [HttpPost("api/conversations/{threadId}/messages")]
    public async Task<ActionResult<MessageReadDto>> CreateMessage(
    Guid threadId,
    [FromBody] MessageCreateDto dto,
    [FromServices] ITokenEstimatorService tokenEstimator,
    [FromServices] ICreditService creditService)
    {
        // 1. Obter thread e assistente
        var thread = await _db.Threads
            .Include(t => t.Assistant)
            .FirstOrDefaultAsync(t => t.Id == threadId);

        if (thread == null)
            return NotFound("Conversa não encontrada.");

        if (string.IsNullOrWhiteSpace(thread.OpenAI_ThreadId))
            return BadRequest("Thread da OpenAI não está configurada.");

        var assistant = thread.Assistant;
        if (assistant == null || string.IsNullOrWhiteSpace(assistant.OpenAI_Id))
            return BadRequest("Assistente inválido ou não ligado à OpenAI.");

        var userId = assistant.UserLocalId;

        // 2. Estimar tokens e custo
        var promptTokens = tokenEstimator.CountTokens(dto.Content);
        var estimatedCompletionTokens = 300;
        var estimatedCostUSD = tokenEstimator.EstimateCost(promptTokens, estimatedCompletionTokens, "gpt-4o");
        var estimatedCredits = (int)Math.Ceiling(estimatedCostUSD * 1000 * 10); // 10x custo da OpenAI

        var hasCredits = await creditService.HasEnoughCredits(userId, estimatedCredits);
        if (!hasCredits)
            return BadRequest("Créditos insuficientes.");

        // 3. Enviar mensagem para OpenAI
        var apiKey = _config["OpenAI:ApiKey"];
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

        var openaiThreadId = thread.OpenAI_ThreadId;
        var assistantId = assistant.OpenAI_Id;

        var msgBody = new { role = "user", content = dto.Content };
        var msgContent = new StringContent(JsonConvert.SerializeObject(msgBody), Encoding.UTF8, "application/json");

        var msgResponse = await httpClient.PostAsync($"https://api.openai.com/v1/threads/{openaiThreadId}/messages", msgContent);
        if (!msgResponse.IsSuccessStatusCode)
            return StatusCode((int)msgResponse.StatusCode, await msgResponse.Content.ReadAsStringAsync());

        // 4. Iniciar execução
        var runBody = new { assistant_id = assistantId };
        var runContent = new StringContent(JsonConvert.SerializeObject(runBody), Encoding.UTF8, "application/json");

        var runRes = await httpClient.PostAsync($"https://api.openai.com/v1/threads/{openaiThreadId}/runs", runContent);
        var runJson = await runRes.Content.ReadAsStringAsync();
        if (!runRes.IsSuccessStatusCode)
            return StatusCode((int)runRes.StatusCode, runJson);

        string runId;
        try
        {
            runId = JsonConvert.DeserializeObject<dynamic>(runJson).id;
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao obter runId: {ex.Message} \nConteúdo: {runJson}");
        }

        // 5. Polling até estar concluído
        string status = "in_progress";
        while (status == "queued" || status == "in_progress")
        {
            await Task.Delay(1000);
            var checkRes = await httpClient.GetAsync($"https://api.openai.com/v1/threads/{openaiThreadId}/runs/{runId}");
            var checkJson = await checkRes.Content.ReadAsStringAsync();

            if (!checkRes.IsSuccessStatusCode)
                return StatusCode((int)checkRes.StatusCode, checkJson);

            try
            {
                status = JsonConvert.DeserializeObject<dynamic>(checkJson).status;
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro ao verificar status: {ex.Message}\nConteúdo: {checkJson}");
            }
        }

        // 6. Obter resposta final
        var replyRes = await httpClient.GetAsync($"https://api.openai.com/v1/threads/{openaiThreadId}/messages");
        var replyJson = await replyRes.Content.ReadAsStringAsync();

        if (!replyRes.IsSuccessStatusCode)
            return StatusCode((int)replyRes.StatusCode, replyJson);

        string finalReply;
        try
        {
            finalReply = JsonConvert.DeserializeObject<dynamic>(replyJson).data[0].content[0].text.value;
        }
        catch (Exception ex)
        {
            return BadRequest($"Erro ao obter resposta do assistente: {ex.Message}\nConteúdo: {replyJson}");
        }

        // 7. Guardar ambas as mensagens (user + assistant)
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

        // 8. Consumir créditos
        await creditService.ConsumeCredits(userId, estimatedCredits,
            $"Mensagem enviada (tokens estimados: {promptTokens + estimatedCompletionTokens})");

        // 9. Resposta final
        var result = new MessageReadDto
        {
            Id = assistantMessage.Id,
            ThreadId = thread.Id,
            Role = assistantMessage.Role,
            Content = assistantMessage.Content,
            CreatedAt = assistantMessage.CreatedAt
        };

        return CreatedAtAction(nameof(GetMessageById), new { id = result.Id }, result);
    }


}


