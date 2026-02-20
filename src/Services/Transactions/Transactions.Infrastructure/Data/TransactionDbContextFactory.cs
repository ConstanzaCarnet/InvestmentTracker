using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;


namespace Transactions.Infrastructure.Data;

public class TransactionDbContextFactory : IDesignTimeDbContextFactory<TransactionDbContext>
{
    public TransactionDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TransactionDbContext>();

        // Esta cadena de conexión es solo para que la migración sepa qué dialecto de SQL usar
        optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=TransactionDb;Trusted_Connection=True;TrustServerCertificate=True;");

        return new TransactionDbContext(optionsBuilder.Options);
    }
}