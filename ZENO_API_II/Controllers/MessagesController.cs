using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using ZENO_API_II.Data;
using ZENO_API_II.DTOs.Audio;
using ZENO_API_II.DTOs.Message;
using ZENO_API_II.Models;
using ZENO_API_II.Services.Interfaces;

namespace ZENO_API_II.Controllers;

    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly ZenoDbContext _db;
        private readonly IConfiguration _config;
        private readonly IOpenAITextToSpeechService _tts;
        private readonly IAssistantMessageService _assistantMessageService;
    public MessagesController(ZenoDbContext db, IConfiguration configuration, IOpenAITextToSpeechService tts, IAssistantMessageService assistantMessageService)
    {
        _db = db;
        _config = configuration;
        _tts = tts;
        _assistantMessageService = assistantMessageService;
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


    [HttpPost("api/threads/{threadId}/messages")]
    public async Task<ActionResult<MessageReadDto>> CreateMessage(
    Guid threadId,
    [FromBody] MessageCreateDto dto)
    {
        try
        {
            var result = await _assistantMessageService.CreateMessageAsync(threadId, dto);
            return CreatedAtAction(nameof(GetMessageById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }


    //AUDIO
    [HttpGet("api/messages/{id}/audio")]
    public async Task<IActionResult> GetMessageAudio(Guid id)
    {
        var message = await _db.Messages.FindAsync(id);
        if (message == null) return NotFound();

        try
        {
            var audioBytes = await _tts.GenerateSpeechAsync(message.Content);
            return File(audioBytes, "audio/mpeg", $"{id}.mp3");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
    [HttpPost("api/messages/audio")]
    public async Task<ActionResult<MessageReadDto>> CreateMessageFromAudio([FromForm] AudioUploadDto input)
    {
        if (input.AudioFile == null || input.AudioFile.Length == 0)
            return BadRequest("Ficheiro de áudio não foi fornecido.");

        var thread = await _db.Threads
            .Include(t => t.Assistant)
            .FirstOrDefaultAsync(t => t.Id == input.ThreadId);

        if (thread == null)
            return NotFound("Thread não encontrada.");

        var assistant = thread.Assistant;
        if (string.IsNullOrEmpty(assistant.OpenAI_Id))
            return BadRequest("Assistente não está ligado à OpenAI.");

        var apiKey = _config["OpenAI:ApiKey"];
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // Enviar áudio para Whisper API
        using var form = new MultipartFormDataContent();
        using var stream = input.AudioFile.OpenReadStream();
        form.Add(new StreamContent(stream), "file", input.AudioFile.FileName);
        form.Add(new StringContent("whisper-1"), "model");

        var response = await httpClient.PostAsync("https://api.openai.com/v1/audio/transcriptions", form);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, responseBody);

        dynamic result = JsonConvert.DeserializeObject(responseBody);
        string transcribedText = result.text;

        var dto = new MessageCreateDto { Role = "user", Content = transcribedText };
        return await CreateMessage(input.ThreadId, dto);
    }
}


