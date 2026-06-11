using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Transactions.Infrastructure.Data;
using Transactions.Infrastructure.Outbox;
using EventBus.Messages.Events;

namespace Transactions.API.BackgroundServices;

public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                //usamos scopeFactory porque el BackgrounService es un Singlenton, por ende debemos crear un request falso
                // y obtener el DbContext y el IPublishEndpoint desde el scope
                using var scope = _scopeFactory.CreateScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                var messages = await dbContext.OutboxMessages
                    .Where(x => x.ProcessedOnUtc == null)
                    .AsTracking()
                    .OrderBy(x => x.OccurredOnUtc)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        var type = ResolveType(message.Type);
                        if (type == null)
                        {
                            _logger.LogError("Tipo de evento no encontrado: {Type}", message.Type);
                            continue;
                        }

                        var deserialized = JsonSerializer.Deserialize(message.Content, type);

                        if (deserialized == null)
                        {
                            _logger.LogError("No se pudo deserializar el mensaje {Id}", message.Id);
                            continue;
                        }

                        await publisher.Publish(deserialized, stoppingToken);

                        message.ProcessedOnUtc = DateTime.UtcNow;
                        //en este caso guardo por cada mensaje procesado, as� evito problemas de concurrencia
                        await dbContext.SaveChangesAsync(stoppingToken);
                        dbContext.ChangeTracker.Clear();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error publicando OutboxMessage {Id}", message.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general en OutboxProcessor");
            }

            // espera 5 segundos
            await Task.Delay(5000, stoppingToken);
        }
    }

    // Mapa explícito para evitar problemas de lazy loading de assemblies.
    private static readonly Dictionary<string, Type> _eventTypes = new()
    {
        [nameof(TransactionCreatedEvent)] = typeof(TransactionCreatedEvent),
        [nameof(TransactionUpdatedEvent)] = typeof(TransactionUpdatedEvent),
        [nameof(TransactionDeletedEvent)] = typeof(TransactionDeletedEvent),
    };

    private static Type? ResolveType(string typeName) =>
        _eventTypes.GetValueOrDefault(typeName);

}
