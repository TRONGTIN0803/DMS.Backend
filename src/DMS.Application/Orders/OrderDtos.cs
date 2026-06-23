using DMS.Domain.Enums;

namespace DMS.Application.Orders;

public sealed record CreateSalesOrderRequest(
    long CompanyId,
    long CustomerId,
    long? SalesPersonId,
    long SiteId,
    DateTimeOffset? OrderDate,
    string? Note,
    IReadOnlyList<CreateSalesOrderLineRequest> Lines);

public sealed record CreateSalesOrderLineRequest(
    long ItemId,
    decimal Quantity);

public sealed record SalesOrderResponse(
    long Id,
    string OrderNo,
    long CompanyId,
    long CustomerId,
    long? SalesPersonId,
    long SiteId,
    DateTimeOffset OrderDate,
    SalesOrderStatus Status,
    decimal SubTotal,
    decimal VatAmount,
    decimal GrandTotal,
    string? Note,
    IReadOnlyList<SalesOrderLineResponse> Lines);

public sealed record SalesOrderLineResponse(
    long Id,
    long ItemId,
    string ItemCode,
    string ItemName,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineAmount,
    decimal LineVatAmount);
