namespace Holdings.Infrastructure.ExternalServices.Models;
//Cada microservicio define su propia representación de los datos externos. Esto es ACL(Anti-Corruption Layer) para evitar que cambios en el servicio externo afecten directamente a nuestro dominio.
public class MarketPriceDto
{
    public string Ticker { get; set; } = default!;
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
}