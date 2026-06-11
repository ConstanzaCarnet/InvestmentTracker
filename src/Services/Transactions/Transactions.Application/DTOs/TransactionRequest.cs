using System.ComponentModel.DataAnnotations;
using Transactions.Domain.Enums;

namespace Transactions.Application.DTOs;

public record TransactionRequest
{
    [Required]
    public Guid UserId { get; init; }

    [Required]
    [MinLength(1)]
    [MaxLength(20)]
    public string Ticker { get; init; } = string.Empty;

    [Required]
    [Range(typeof(decimal), "0.0001", "1000000000", ErrorMessage = "Quantity must be greater than 0.")]
    public decimal Quantity { get; init; }

    [Required]
    [Range(typeof(decimal), "0.0001", "1000000000", ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; init; }

    [Required]
    [Range(typeof(decimal), "0.0001", "1000000000", ErrorMessage = "ExchangeRate must be greater than 0.")]
    public decimal ExchangeRate { get; init; }

    [Required]
    public Currency Currency { get; init; }

    [Required]
    public DateTime Date { get; init; }

    [Required]
    public TransactionType Type { get; init; }
}
