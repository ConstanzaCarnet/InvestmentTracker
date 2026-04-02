using Microsoft.AspNetCore.Mvc;
using MarketData.Application.Interfaces;
using MarketData.Application.DTOs;

namespace MarketData.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PricesController : ControllerBase
{
    private readonly IPriceService _priceService;

    public PricesController(IPriceService priceService)
    {
        _priceService = priceService;
    }

    // POST api/v1/prices/batch
    [HttpPost("batch")]
    public async Task<IActionResult> GetBatchPrices([FromBody] BatchPriceRequest request)
    {
        var prices = await _priceService.GetPricesAsync(request.InstrumentIds);

        return Ok(new BatchPriceResponse
        {
            Prices = prices.Select(p => new PriceResponse
            {
                InstrumentId = p.InstrumentId,
                Price = p.Price,
                Timestamp = p.Timestamp
            }).ToList()
        });
    }
}