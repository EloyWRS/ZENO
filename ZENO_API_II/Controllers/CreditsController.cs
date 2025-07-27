using Microsoft.AspNetCore.Mvc;
using ZENO_API_II.DTOs.CreditTransaction;
using ZENO_API_II.Services.Interfaces;

namespace ZENO_API_II.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CreditsController : ControllerBase
{
    private readonly ICreditService _creditService;

    public CreditsController(ICreditService creditService)
    {
        _creditService = creditService;
    }

    // GET api/credits/{userId}
    [HttpGet("{userId}")]
    public async Task<ActionResult<UserCreditsDto>> GetUserCredits(Guid userId)
    {
        var result = await _creditService.GetUserCreditsAsync(userId);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // POST api/credits/add
    [HttpPost("add")]
    public async Task<IActionResult> AddCredits([FromBody] CreditTransactionCreateDto dto)
    {
        var success = await _creditService.AddCreditsAsync(dto);
        if (!success)
            return NotFound("Utilizador não encontrado.");

        return Ok("Créditos adicionados com sucesso.");
    }

    // POST api/credits/consume
    [HttpPost("consume")]
    public async Task<IActionResult> ConsumeCredits([FromBody] CreditTransactionCreateDto dto)
    {
        if (dto.Amount >= 0)
            return BadRequest("Para consumir créditos, o valor deve ser negativo.");

        var success = await _creditService.ConsumeCredits(dto.UserId, -dto.Amount, dto.Description);
        if (!success)
            return BadRequest("Créditos insuficientes ou utilizador inexistente.");

        return Ok("Créditos consumidos com sucesso.");
    }
}
