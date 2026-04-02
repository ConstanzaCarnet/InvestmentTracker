namespace Transactions.Domain.Entities;

public class Instrument
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Exchange { get; set; } = null!;
    // Cambiado a decimal para manejar ratios como 0.5 (1:2)
    public decimal ConversionRatio { get; set; } = 1;
    // Útil para reporting y segmentación de cartera
    public string? Sector { get; set; }
    public string? Region { get; set; }

    // Para lógica de negocio: ¿Es CEDEAR, Acción local, Bono?
    public string AssetType { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}