namespace MarketData.Domain.Entities;
//tipo de cambio entre dos monedas
public class FxRate
{
    public string FromCurrency { get; set; } = string.Empty;

    public string ToCurrency { get; set; } = string.Empty;

    public decimal Rate { get; set; }

    public DateTime Timestamp { get; set; }
}