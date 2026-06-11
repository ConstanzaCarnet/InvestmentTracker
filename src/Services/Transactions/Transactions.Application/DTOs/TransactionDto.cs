namespace Transactions.Application.DTOs;

//ojo aqui es record, ya que no queremos modificar el historico de transacciones
/*public record TransactionDto
{
    public Guid Id;
    public string Ticker;
    public decimal Quantity;
    public decimal Price;
    public decimal TotalAmount;
    public DateTime TransactionDate;
    public string Type; // "Buy" o "Sell"
}
*/
public record TransactionDto
{
    public Guid Id { get; init; }
    public string Ticker { get; init; }
    public decimal Quantity { get; init; }
    public decimal Price { get; init; }
    public decimal ExchangeRate { get; init; }
    public decimal Ratio { get; init; }
    public string Currency { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime TransactionDate { get; init; }
    public string Type { get; init; }

    public TransactionDto(
        Guid id,
        string ticker,
        decimal quantity,
        decimal price,
        decimal exchangeRate,
        decimal ratio,
        string currency,
        DateTime transactionDate,
        string type)
    {
        Id = id;
        Ticker = ticker;
        Quantity = quantity;
        Price = price;
        ExchangeRate = exchangeRate;
        Ratio = ratio;
        Currency = currency;
        TotalAmount = quantity * price * exchangeRate;
        TransactionDate = transactionDate;
        Type = type;
    }
}

