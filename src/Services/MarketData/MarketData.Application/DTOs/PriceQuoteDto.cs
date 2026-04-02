namespace MarketData.Application.DTOs;

public record PriceQuoteDto(
    string Ticker,
    decimal Price,
    string Currency,
    DateTime Timestamp
);