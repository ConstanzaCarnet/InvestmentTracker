using EventBus.Messages.Common;

namespace EventBus.Messages.Events;

public record TransactionCreatedIntegrationEvent : IntegrationEvent
{
    public TransactionBaseEvent Data { get; init; } = default!;
}
