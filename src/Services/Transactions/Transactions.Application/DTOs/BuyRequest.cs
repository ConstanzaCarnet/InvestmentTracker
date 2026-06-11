using System.ComponentModel.DataAnnotations;
using Transactions.Domain.Enums;

namespace Transactions.Application.DTOs;

public record BuyRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Ticker is required.")]
    [MaxLength(20, ErrorMessage = "Ticker must be 20 characters or fewer.")]
    public string Ticker { get; set; } = string.Empty;

    [Required]
    [Range(typeof(decimal), "0.0001", "1000000000", ErrorMessage = "Quantity must be greater than 0.")]
    public decimal Quantity { get; set; }

    [Required]
    [Range(typeof(decimal), "0.0001", "1000000000", ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [Required]
    [Range(typeof(decimal), "0.0001", "1000000000", ErrorMessage = "ExchangeRate must be greater than 0.")]
    public decimal ExchangeRate { get; set; }

    [Required]
    public Currency Currency { get; set; }
}
