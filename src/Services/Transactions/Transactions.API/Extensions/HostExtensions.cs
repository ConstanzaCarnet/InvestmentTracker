using Microsoft.EntityFrameworkCore;
using Transactions.Infrastructure.Data;

namespace Transactions.API.Extensions;

public static class HostExtensions
{
    //IHost es la interfaz que representa la aplicación host en ASP.NET Core
    public static IHost MigrateDatabase(this IHost host)
    {
        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<TransactionDbContext>>();
            var context = services.GetService<TransactionDbContext>();

            try
            {
                logger.LogInformation("Migrando base de datos de Transactions...");
                context.Database.Migrate();
                logger.LogInformation("Migración completada con éxito.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ocurrió un error al migrar la base de datos.");
            }
        }
        return host;
    }
}