using Holdings.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Holdings.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HoldingsController : ControllerBase
{
    private readonly IHoldingRepository _repository;

    public HoldingsController(IHoldingRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetHoldings(Guid userId)
    {
        var account = await _repository.GetAccountByUserIdAsync(userId);

        if (account == null)
            return NotFound(new { Message = "El usuario no tiene una cuenta de inversiones registrada." });

        // Devolvemos la cuenta con sus posiciones
        return Ok(new
        {
            account.UserId,
            Positions = account.Positions.Select(p => new
            {
                p.Ticker,
                p.Quantity,
                p.AveragePurchasePrice,
                TotalInvested = p.Quantity * p.AveragePurchasePrice,
                p.LastTransactionDate
            })
        });
    }
}