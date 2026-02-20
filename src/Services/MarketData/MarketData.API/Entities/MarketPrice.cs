namespace MarketData.API.Entities;

public class MarketPrice
{
    public string Ticker { get; set; } = string.Empty;//Ej: AAPL, AL30
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty; //USD, ARS, EUR
    public DateTime DateTime { get; set; } // En lugar de LastUpdated, usamos el punto exacto en el tiempo
    public decimal? OpenPrice { get; set; }  // Opcional: para saber si el activo subió o bajó hoy

    public MarketPrice(string ticker, decimal price, string currency, DateTime dateTime, decimal? openPrice = null)
    {
        Ticker = ticker;
        Price = price;
        Currency = currency;
        DateTime = dateTime;
        OpenPrice = openPrice;
    }
    //Constructor vacío para serialización/deserialización
    public MarketPrice(){}
}