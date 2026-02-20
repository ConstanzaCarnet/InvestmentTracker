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
public record TransactionDto(
    Guid Id,
    string Ticker,
    decimal Quantity,
    decimal Price,
    decimal TotalAmount,
    DateTime TransactionDate,
    string Type // "Buy" o "Sell"
);