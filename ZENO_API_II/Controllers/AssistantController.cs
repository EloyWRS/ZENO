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
    private readonly IAssistantService _assistantService;

    public AssistantController(ZenoDbContext db, IConfiguration config, IJwtService jwtService, IUserSetupService userSetupService, IAssistantService assistantService)
    {
        _db = db;
        _config = config;
        _jwtService = jwtService;
        _userSetupService = userSetupService;
        _assistantService = assistantService;
    }

    // GET /api/users/{userId}/assistant/instructions
    [HttpGet("instructions")]
    public async Task<IActionResult> GetAssistantInstructions(Guid userId)
    {
        var authenticatedUser = await GetAuthenticatedUser();
        if (authenticatedUser == null)
            return Unauthorized(new { message = "Invalid token" });

        if (authenticatedUser.Id != userId)
            return Forbid();

        try
        {
            var instructions = await _assistantService.GetAssistantInstructionsAsync(userId);
            return Ok(new { instructions });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // PATCH /api/users/{userId}/assistant/instructions
    [HttpPatch("instructions")]
    public async Task<IActionResult> UpdateAssistantInstructions(Guid userId, [FromBody] AssistantInstructionsUpdateDto dto)
    {
        var authenticatedUser = await GetAuthenticatedUser();
        if (authenticatedUser == null)
            return Unauthorized(new { message = "Invalid token" });

        if (authenticatedUser.Id != userId)
            return Forbid();

        if (dto == null || string.IsNullOrWhiteSpace(dto.Instructions))
            return BadRequest(new { message = "'instructions' is required" });

        try
        {
            await _assistantService.UpdateAssistantInstructionsAsync(userId, dto.Instructions);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // DELETE /api/users/{userId}/assistant/instructions
    [HttpDelete("instructions")]
    public async Task<IActionResult> DeleteAssistantInstructions(Guid userId)
    {
        var authenticatedUser = await GetAuthenticatedUser();
        if (authenticatedUser == null)
            return Unauthorized(new { message = "Invalid token" });

        if (authenticatedUser.Id != userId)
            return Forbid();

        try
        {
            await _assistantService.DeleteAssistantInstructionsAsync(userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
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

        try
        {
            var dto = await _assistantService.GetAssistantAsync(userId);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
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

        try
        {
            var output = await _assistantService.CreateAssistantAsync(userId, dto);
            return CreatedAtAction(nameof(GetAssistant), new { userId }, output);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
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

        try
        {
            await _assistantService.UpdateAssistantAsync(userId, dto);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
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

        try
        {
            var result = await _assistantService.GetOrCreateAssistantAndThreadAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

}


