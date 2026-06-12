using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Common.Authentication;
using Holdings.Application.Interfaces;
using Holdings.Application.DTOs;

namespace Holdings.API.Controllers;

[ApiController]
[Authorize]
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

    /// <summary>Returns the portfolio of the authenticated user (from the JWT, not the route).</summary>
    [HttpGet("me")]
    public async Task<ActionResult<PortfolioDto>> GetMyPortfolio()
    {
        var userId = User.GetUserId();
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