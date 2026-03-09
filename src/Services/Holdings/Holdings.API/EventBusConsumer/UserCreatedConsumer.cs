using EventBus.Messages.Events;
using Holdings.Application.Interfaces;
using Holdings.Domain.Entities;
using Holdings.Infrastructure.Data;
using Holdings.Application.Helpers;
using MassTransit;

namespace Holdings.API.EventBusConsumer;

public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    private readonly HoldingsDbContext _context;

    public UserCreatedConsumer(HoldingsDbContext context)
    {
        _context = context;
    }

    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var message = context.Message;

        var account = new Account(message.UserId);

        _context.Accounts.Add(account);

        await _context.SaveChangesAsync();
    }
}