using System;

namespace EventBus.Messages.Events;

public record UserDeletedEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
}