using Transactions.Domain.Enums;
namespace Transactions.Application.DTOs;

public record SellRequest
{
    public Guid UserId { get; set; } // Identificador del usuario que vende
    public string Ticker { get; set; } = string.Empty; // Símbolo del activo a vender (Ej: "AAPL", "AL30")
    public decimal Quantity { get; set; } // Cantidad a vender
    public decimal Price { get; set; } // Precio unitario de venta
    public decimal ExchangeRate { get; set; } // Tipo de cambio para convertir a la moneda base
    public Currency Currency { get; set; } // Moneda en la que se realiza la venta
    public AssetType AssetType;
}

public enum AssetType
{
    Stock,
    Bond,
    ETF,
    Crypto
}