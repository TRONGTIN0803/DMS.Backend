namespace DMS.Application.Events;

public sealed record SalesOrderApprovedEvent(
    Guid MessageId,
    long SalesOrderId,
    string OrderNo,
    long CustomerId,
    long SiteId,
    decimal GrandTotal,
    DateTimeOffset ApprovedAt);
