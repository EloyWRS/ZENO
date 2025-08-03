using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Assistants;
using System.Net.Http.Headers;
using System.Text;
using ZENO_API_II.Data;
using ZENO_API_II.DTOs.Assistant;
using ZENO_API_II.Models;
using ZENO_API_II.Services.Interfaces;

namespace ZENO_API_II.Controllers;

[ApiController]
[Route("api/users/{userId}/[controller]")]
public class AssistantController : ControllerBase
{
    private readonly ZenoDbContext _db;
    private readonly IConfiguration _config;
    private readonly IJwtService _jwtService;
    private readonly IUserSetupService _userSetupService;

    public AssistantController(ZenoDbContext db, IConfiguration config, IJwtService jwtService, IUserSetupService userSetupService)
    {
        _db = db;
        _config = config;
        _jwtService = jwtService;
        _userSetupService = userSetupService;
    }

    private async Task<UserLocal?> GetAuthenticatedUser()
    {
        try
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return null;
            }

            var token = authHeader.Substring("Bearer ".Length);
            return await _jwtService.GetUserFromTokenAsync(token);
        }
        catch
        {
            return null;
        }
    }

    // GET /api/users/{userId}/assistant
    [HttpGet]
    public async Task<ActionResult<AssistantReadDto>> GetAssistant(Guid userId)
    {
        var authenticatedUser = await GetAuthenticatedUser();
        if (authenticatedUser == null)
            return Unauthorized(new { message = "Invalid token" });

        if (authenticatedUser.Id != userId)
            return Forbid();

        var assistant = await _db.Assistants
            .FirstOrDefaultAsync(a => a.UserLocalId == userId);

        if (assistant == null)
            return NotFound();

        if (string.IsNullOrEmpty(assistant.OpenAI_Id))
            return BadRequest("Assistente não está ligado à OpenAI.");

        var apiKey = _config["OpenAI:ApiKey"];
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

        var response = await httpClient.GetAsync($"https://api.openai.com/v1/assistants/{assistant.OpenAI_Id}");
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, json);

        dynamic result = JsonConvert.DeserializeObject(json);

        var dto = new AssistantReadDto
        {
            Id = assistant.Id,
            Name = result.name,
            Description = result.instructions, // ou mapeia como preferires
            CreatedAt = assistant.CreatedAt,
            UserId = assistant.UserLocalId
        };

        return Ok(dto);
    }

    // POST /api/users/{userId}/assistant
    [HttpPost]
    public async Task<ActionResult<AssistantReadDto>> CreateAssistant(Guid userId, [FromBody] AssistantCreateDto dto)
    {
        var authenticatedUser = await GetAuthenticatedUser();
        if (authenticatedUser == null)
            return Unauthorized(new { message = "Invalid token" });

        if (authenticatedUser.Id != userId)
            return Forbid();

        var user = await _db.Users
            .Include(u => u.Assistant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound("Utilizador não encontrado.");

        if (user.Assistant != null)
            return Conflict("O utilizador já tem um assistente.");

        // Criação do Assistant na OpenAI
        var apiKey = _config["OpenAI:ApiKey"];

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");


        var body = new
        {
            name = dto.Name,
            instructions = "És um assistente pessoal chamado ZENO. Sê informal, leal e com memória.",
            model = "gpt-4o",
            temperature = 0.7
        };

        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("https://api.openai.com/v1/assistants", content);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, responseJson);

        dynamic result = JsonConvert.DeserializeObject(responseJson);
        string openaiId = result.id;

        // Criação local do Assistant
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

        var output = new AssistantReadDto
        {
            Id = assistant.Id,
            Name = assistant.Name,
            Description = assistant.Description,
            CreatedAt = assistant.CreatedAt,
            UserId = assistant.UserLocalId
        };

        return CreatedAtAction(nameof(GetAssistant), new { userId }, output);
    }

    // PATCH /api/users/{userId}/assistant
    [HttpPatch]
    public async Task<IActionResult> UpdateAssistant(Guid userId, [FromBody] AssistantUpdateDto dto)
    {
        var authenticatedUser = await GetAuthenticatedUser();
        if (authenticatedUser == null)
            return Unauthorized(new { message = "Invalid token" });

        if (authenticatedUser.Id != userId)
            return Forbid();

        var assistant = await _db.Assistants
            .FirstOrDefaultAsync(a => a.UserLocalId == userId);

        if (assistant == null)
            return NotFound();

        if (string.IsNullOrEmpty(assistant.OpenAI_Id))
            return BadRequest("Assistente não está ligado à OpenAI.");

        // Atualizar na OpenAI
        var apiKey = _config["OpenAI:ApiKey"];
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var body = new
        {
            name = dto.Name ?? assistant.Name,
            instructions = dto.Description ?? assistant.Description
        };

        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var response = await httpClient.PatchAsync($"https://api.openai.com/v1/assistants/{assistant.OpenAI_Id}", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, responseBody);

        // Atualizar localmente
        assistant.Name = dto.Name ?? assistant.Name;
        assistant.Description = dto.Description ?? assistant.Description;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // GET /api/users/{userId}/assistant/thread
    [HttpGet("thread")]
    public async Task<ActionResult<object>> GetOrCreateAssistantAndThread(Guid userId)
    {
        var authenticatedUser = await GetAuthenticatedUser();
        if (authenticatedUser == null)
            return Unauthorized(new { message = "Invalid token" });

        if (authenticatedUser.Id != userId)
            return Forbid();

        // Get user
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound("User not found");

        // Use UserSetupService to ensure assistant and thread exist
        await _userSetupService.SetupNewUserAsync(user);

        // Get the assistant and latest thread
        var assistant = await _db.Assistants
            .Include(a => a.Threads)
            .FirstOrDefaultAsync(a => a.UserLocalId == userId);

        if (assistant == null)
            return NotFound("Assistant not found");

        var latestThread = assistant.Threads
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefault();

        if (latestThread == null)
            return NotFound("Thread not found");

        return Ok(new
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
        });
    }

}


