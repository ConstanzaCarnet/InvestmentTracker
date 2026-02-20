using System;

namespace EventBus.Messages.Events;

//Esto es lo que viaja por RabbitMQ
public record UserCreatedEvent
{
    //aqui ya usamos init que es inmutable despues de la creacion
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}