namespace Transactions.Application.DTOs;
public record BuyRequest
{
    public Guid UserId { get; set; } // Identificador del usuario que compra
    public string Ticker { get; set; } = string.Empty; // Símbolo del activo a comprar (Ej: "AAPL", "AL30")
    public decimal Quantity { get; set; } // Cantidad a comprar
    public decimal Price { get; set; } // Precio unitario de compra
}