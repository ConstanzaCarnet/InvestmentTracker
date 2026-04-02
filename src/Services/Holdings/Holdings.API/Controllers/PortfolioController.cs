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
    public async Task<ActionResult<PortfolioDto>> GetPortfolio()
    {
        try
        {
            //Por ahora mockeamos el userId
            var userId = GetUserId();

            var portfolio = await _portfolioService.GetPortfolioAsync(userId);

            return Ok(portfolio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching portfolio");

            return StatusCode(500, "Internal server error");
        }
    }

    private Guid GetUserId()
    {
        // 🔴 TEMPORAL
        // después viene de JWT / Identity
        return Guid.Parse("11111111-1111-1111-1111-111111111111");
    }

}