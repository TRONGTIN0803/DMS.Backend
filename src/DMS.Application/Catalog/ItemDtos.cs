namespace DMS.Application.Catalog;

public sealed record ItemResponse(
    long Id,
    string Code,
    string Name,
    string Unit,
    string? Barcode,
    decimal Price,
    decimal VatRate,
    bool IsActive);

public sealed record CreateItemRequest(
    string Code,
    string Name,
    string Unit,
    string? Barcode,
    decimal Price,
    decimal VatRate);

public sealed record UpdateItemRequest(
    string Code,
    string Name,
    string Unit,
    string? Barcode,
    decimal Price,
    decimal VatRate,
    bool IsActive);
