using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Zeno_API_II.DTOs.Thread;
using ZENO_API_II.Data;
using ZENO_API_II.DTOs.Thread;
using ZENO_API_II.Models;

namespace ZENO_API_II.Controllers
{
    [ApiController]
    [Route("api")]
    public class ThreadController : ControllerBase
    {
        private readonly ZenoDbContext _db;
        private readonly IConfiguration _config;

        public ThreadController(ZenoDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // GET /api/assistants/{assistantId}/threads
        [HttpGet("assistants/{assistantId}/threads")]
        public async Task<ActionResult<IEnumerable<ChatThreadReadDto>>> GetThreads(Guid assistantId)
        {
            var threads = await _db.Threads
                .Where(t => t.AssistantId == assistantId)
                .Select(t => new ChatThreadReadDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    CreatedAt = t.CreatedAt,
                    AssistantId = t.AssistantId
                })
                .ToListAsync();

            return Ok(threads);
        }

        // GET /api/threads/{id}
        [HttpGet("threads/{id}")]
        public async Task<ActionResult<ChatThreadReadDto>> GetThreadById(Guid id)
        {
            var thread = await _db.Threads.FindAsync(id);
            if (thread == null) return NotFound();

            var dto = new ChatThreadReadDto
            {
                Id = thread.Id,
                Title = thread.Title,
                CreatedAt = thread.CreatedAt,
                AssistantId = thread.AssistantId
            };

            return Ok(dto);
        }

        [HttpPost("assistants/{assistantId}/threads")]
        public async Task<ActionResult<ChatThreadReadDto>> CreateThread(Guid assistantId, [FromBody] ChatThreadCreateDto dto)
        {
            var assistant = await _db.Assistants.FindAsync(assistantId);
            if (assistant == null) return NotFound("Assistente não encontrado.");

            // Criar thread na OpenAI
            var apiKey = _config["OpenAI:ApiKey"];
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

            var response = await httpClient.PostAsync("https://api.openai.com/v1/threads", null);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, responseBody);

            dynamic result = JsonConvert.DeserializeObject(responseBody);
            string openaiThreadId = result.id;

            var thread = new ChatThread
            {
                Id = Guid.NewGuid(),
                Title = dto.Title ?? "Nova Thread",
                CreatedAt = DateTime.UtcNow,
                AssistantId = assistant.Id,
                OpenAI_ThreadId = openaiThreadId
            };

            _db.Threads.Add(thread);
            await _db.SaveChangesAsync();

            var output = new ChatThreadReadDto
            {
                Id = thread.Id,
                Title = thread.Title,
                CreatedAt = thread.CreatedAt,
                AssistantId = assistant.Id,
                OpenAI_ThreadId = thread.OpenAI_ThreadId
            };

            return CreatedAtAction(nameof(GetThreadById), new { id = thread.Id }, output);
        }

        // DELETE /api/threads/{id}
        [HttpDelete("threads/{id}")]
        public async Task<IActionResult> DeleteThread(Guid id)
        {
            var thread = await _db.Threads.FindAsync(id);
            if (thread == null) return NotFound();

            _db.Threads.Remove(thread);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
