namespace MarketData.Application.DTOs;

public record FxRateDto(
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateTime Timestamp
);