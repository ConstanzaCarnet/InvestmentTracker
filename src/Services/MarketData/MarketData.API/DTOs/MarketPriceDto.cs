namespace MarketData.API.DTOs;

public record MarketPriceDto(
    string Ticker,
    decimal Price,
    string Currency
);