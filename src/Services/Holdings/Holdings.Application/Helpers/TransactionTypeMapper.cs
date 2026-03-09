namespace Holdings.Application.Helpers;

public static class TransactionTypeMapper
{
    public static Holdings.Domain.Enums.TransactionType ToDomain(
        EventBus.Messages.Events.TransactionType type)
    {
        return (Holdings.Domain.Enums.TransactionType)type;
    }
}