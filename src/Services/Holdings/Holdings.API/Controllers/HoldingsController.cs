using Holdings.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Holdings.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HoldingsController : ControllerBase
{
    private readonly IHoldingsService _service;

    public HoldingsController(IHoldingsService service)
    {
        _service = service;
    }

    [HttpGet("{userId}/portfolio")]
    public async Task<IActionResult> GetPortfolio(Guid userId)
    {
        var portfolio = await _service.GetPortfolioAsync(userId);
        return Ok(portfolio);
    }
    
}