using EventBus.Messages.Events;
using Holdings.Domain.Entities;
using Holdings.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Holdings.API.EventBusConsumer;

public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    private readonly HoldingsDbContext _context;
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(HoldingsDbContext context, ILogger<UserCreatedConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var message = context.Message;

        var exists = await _context.Accounts
            .AnyAsync(a => a.UserId == message.UserId, context.CancellationToken);

        if (exists)
        {
            _logger.LogInformation("Account already exists for UserId={UserId}, skipping.", message.UserId);
            return;
        }

        var account = new Account(message.UserId);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Account created for UserId={UserId} AccountNumber={Number}",
            message.UserId, account.AccountNumber);
    }
}
