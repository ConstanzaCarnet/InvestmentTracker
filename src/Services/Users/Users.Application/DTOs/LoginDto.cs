using System.ComponentModel.DataAnnotations;

namespace Users.Application.DTOs;

// Mismo patrón que BuyRequest/SellRequest: record con propiedades (no posicional),
// para que las DataAnnotations vivan en la propiedad y [ApiController] las valide.
public record LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
