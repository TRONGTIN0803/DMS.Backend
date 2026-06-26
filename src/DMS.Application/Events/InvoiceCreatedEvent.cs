namespace DMS.Application.Events;

public sealed record InvoiceCreatedEvent(
    Guid MessageId,
    long InvoiceId,
    string InvoiceNo,
    long SalesOrderId,
    long CustomerId,
    decimal GrandTotal,
    DateTimeOffset CreatedAt);
