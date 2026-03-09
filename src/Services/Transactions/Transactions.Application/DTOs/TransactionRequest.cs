using Transactions.Domain.Enums;
using System.ComponentModel.DataAnnotations;


namespace Transactions.Application.DTOs;

public record TransactionRequest
(
    [Required]
    Guid UserId,
    
    [Required]
    string Ticker,// simbolo de la accion

    [Required]
    decimal Quantity,
    
    [Required]
    decimal Price,

    [Required]
    decimal ExchangeRate,
    [Required]
    Currency Currency,
    
    [Required]
    DateTime Date,

    [Required]
    TransactionType Type // Buy o Sell
);