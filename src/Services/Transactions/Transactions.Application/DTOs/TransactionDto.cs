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
    public Guid Id;
    public string Ticker;
    public decimal Quantity;
    public decimal Price;
    public decimal ExchangeRate;
    public string Currency;
    public decimal totalAmount;
    public DateTime TransactionDate;
    public string Type; // "Buy" o "Sell"

    //constructor para mapear desde la entidad Transaction
    public TransactionDto(Guid id, string ticker, decimal quantity, decimal price, decimal exchangeRate, string currency, DateTime transactionDate, string type)
    {
        Id = id;
        Ticker = ticker;
        Quantity = quantity;
        Price = price;
        ExchangeRate = exchangeRate;
        Currency = currency;
        totalAmount = quantity * price * exchangeRate; // calculamos el monto total considerando el tipo de cambio
        TransactionDate = transactionDate;
        Type = type;
    }
}

