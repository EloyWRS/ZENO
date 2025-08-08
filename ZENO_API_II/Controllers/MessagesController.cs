using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using ZENO_API_II.Data;
using ZENO_API_II.DTOs.Audio;
using ZENO_API_II.DTOs.Message;
using ZENO_API_II.Models;
using ZENO_API_II.Services.Implementations;
using ZENO_API_II.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ZENO_API_II.Exceptions;
using ZENO_API_II.Constants;

namespace ZENO_API_II.Controllers;

    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly ZenoDbContext _db;
        private readonly IConfiguration _config;
        private readonly IOpenAITextToSpeechService _tts;
        private readonly IAssistantMessageService _assistantMessageService;
        private readonly IAudioTranscriptionService _audioTranscriptionService;
        private readonly IJwtService _jwtService;

    public MessagesController(
        ZenoDbContext db, 
        IConfiguration configuration, 
        IOpenAITextToSpeechService tts, 
        IAssistantMessageService assistantMessageService, 
        IAudioTranscriptionService audioTranscriptionService,
        IJwtService jwtService)
    {
        _db = db;
        _config = configuration;
        _tts = tts;
        _assistantMessageService = assistantMessageService;
        _audioTranscriptionService = audioTranscriptionService;
        _jwtService = jwtService;
    }

    // Helper method to get authenticated user from JWT token
    private async Task<UserLocal?> GetAuthenticatedUser()
    {
        try
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            var token = authHeader.Substring("Bearer ".Length);
            return await _jwtService.GetUserFromTokenAsync(token);
        }
        catch
        {
            return null;
        }
    }

    // Helper method to check if user can access a thread
    private async Task<bool> CanAccessThread(Guid threadId, UserLocal user)
    {
        try
        {
            var thread = await _db.Threads
                .Include(t => t.Assistant)
                .FirstOrDefaultAsync(t => t.Id == threadId);

            if (thread == null)
                return false;

            // Check if the thread belongs to the user's assistant
            return thread.Assistant?.UserLocalId == user.Id;
        }
        catch
        {
            return false;
        }
    }

    [HttpGet("api/threads/{threadId}/messages")]
        public async Task<ActionResult<IEnumerable<MessageReadDto>>> GetMessagesByThread(Guid threadId)
        {
            var authenticatedUser = await GetAuthenticatedUser();
            if (authenticatedUser == null)
                return Unauthorized(new { message = "Invalid token" });

            if (!await CanAccessThread(threadId, authenticatedUser))
                return Forbid();

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
            var authenticatedUser = await GetAuthenticatedUser();
            if (authenticatedUser == null)
                return Unauthorized(new { message = "Invalid token" });

            var message = await _db.Messages
                .Include(m => m.Thread)
                .ThenInclude(t => t.Assistant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null)
                return NotFound("Mensagem não encontrada.");

            // Check if user can access this message's thread
            if (message.Thread?.Assistant?.UserLocalId != authenticatedUser.Id)
                return Forbid();

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
        var authenticatedUser = await GetAuthenticatedUser();
        if (authenticatedUser == null)
            return Unauthorized(new { message = "Invalid token" });

        if (!await CanAccessThread(threadId, authenticatedUser))
            return Forbid();

        try
        {
            var result = await _assistantMessageService.CreateMessageAsync(threadId, dto);
            return CreatedAtAction(nameof(GetMessageById), new { id = result.Id }, result);
        }
        catch (BusinessException ex)
        {
            return StatusCode(ex.StatusCode, new
            {
                error = new
                {
                    message = ex.Message,
                    code = ex.ErrorCode,
                    statusCode = ex.StatusCode
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = new
                {
                    message = "Erro interno do servidor",
                    code = ErrorCodes.INTERNAL_ERROR,
                    statusCode = 500
                }
            });
        }
    }


    //AUDIO
    [HttpGet("api/messages/{id}/audio")]
    public async Task<IActionResult> GetMessageAudio(Guid id)
    {
        var authenticatedUser = await GetAuthenticatedUser();
        if (authenticatedUser == null)
            return Unauthorized(new { message = "Invalid token" });

        var message = await _db.Messages
            .Include(m => m.Thread)
            .ThenInclude(t => t.Assistant)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (message == null) 
            return NotFound();

        // Check if user can access this message's thread
        if (message.Thread?.Assistant?.UserLocalId != authenticatedUser.Id)
            return Forbid();

        try
        {
            // Select TTS voice based on user's language
            string baseLang = (authenticatedUser.Language ?? "en").Split('-')[0].ToLowerInvariant();
            string voice = baseLang switch
            {
                "pt" => "nova",
                "en" => "alloy",
                "es" => "alloy",
                "fr" => "alloy",
                "de" => "alloy",
                _ => "alloy"
            };

            var audioBytes = await _tts.GenerateSpeechAsync(message.Content, voice);
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
        var authenticatedUser = await GetAuthenticatedUser();
        if (authenticatedUser == null)
            return Unauthorized(new { message = "Invalid token" });

        if (!await CanAccessThread(input.ThreadId, authenticatedUser))
            return Forbid();

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

        // Auto-detect language from audio; do not force a language
        string transcribedText = await _audioTranscriptionService.TranscribeAudioAsync(input.AudioFile);

        var dto = new MessageCreateDto { Role = "user", Content = transcribedText };
        return await CreateMessage(input.ThreadId, dto);
    }
}


