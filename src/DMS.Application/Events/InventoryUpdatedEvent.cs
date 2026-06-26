namespace DMS.Application.Events;

public sealed record InventoryUpdatedEvent(
    Guid MessageId,
    long SiteId,
    long ItemId,
    decimal Quantity,
    decimal BalanceAfter,
    string RefType,
    long RefId,
    DateTimeOffset UpdatedAt);
