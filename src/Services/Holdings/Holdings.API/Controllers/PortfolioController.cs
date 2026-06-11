using Microsoft.AspNetCore.Mvc;
using Holdings.Application.Interfaces;
using Holdings.Application.DTOs;

namespace Holdings.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(
        IPortfolioService portfolioService,
        ILogger<PortfolioController> logger)
    {
        _portfolioService = portfolioService;
        _logger = logger;
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<PortfolioDto>> GetPortfolio(Guid userId)
    {
        try
        {
            var portfolio = await _portfolioService.GetPortfolioAsync(userId);
            return Ok(portfolio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching portfolio for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

}