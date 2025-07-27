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

namespace ZENO_API_II.Controllers;

[ApiController]
[Route("api/users/{userId}/[controller]")]
public class AssistantController : ControllerBase
{
    private readonly ZenoDbContext _db;
    private readonly IConfiguration _config;

    public AssistantController(ZenoDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // GET /api/users/{userId}/assistant
    [HttpGet]
    public async Task<ActionResult<AssistantReadDto>> GetAssistant(Guid userId)
    {
        var assistant = await _db.Assistants
            .FirstOrDefaultAsync(a => a.UserLocalId == userId);

        if (assistant == null)
            return NotFound();

        if (string.IsNullOrEmpty(assistant.OpenAI_Id))
            return BadRequest("Assistente não está ligado à OpenAI.");

        var apiKey = _config["OpenAI:ApiKey"];
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

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

}


