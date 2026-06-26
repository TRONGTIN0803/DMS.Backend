using DMS.Domain.Enums;

namespace DMS.Application.Catalog;

public sealed record CompanyResponse(
    long Id,
    string Code,
    string Name,
    string? TaxCode,
    string? Address,
    string? Phone,
    string? Email,
    bool IsActive);

public sealed record CreateCompanyRequest(
    string Code,
    string Name,
    string? TaxCode,
    string? Address,
    string? Phone,
    string? Email);

public sealed record UpdateCompanyRequest(
    string Code,
    string Name,
    string? TaxCode,
    string? Address,
    string? Phone,
    string? Email,
    bool IsActive);

public sealed record SalesPersonResponse(
    long Id,
    string Code,
    string Name,
    long CompanyId,
    string CompanyName,
    string? Phone,
    string? Email,
    bool IsActive);

public sealed record CreateSalesPersonRequest(
    string Code,
    string Name,
    long CompanyId,
    string? Phone,
    string? Email);

public sealed record UpdateSalesPersonRequest(
    string Code,
    string Name,
    long CompanyId,
    string? Phone,
    string? Email,
    bool IsActive);

public sealed record CustomerResponse(
    long Id,
    string Code,
    string Name,
    long CompanyId,
    string CompanyName,
    long? SalesPersonId,
    string? SalesPersonName,
    string? Address,
    string? Phone,
    CustomerType CustomerType,
    bool IsActive);

public sealed record CreateCustomerRequest(
    string Code,
    string Name,
    long CompanyId,
    long? SalesPersonId,
    string? Address,
    string? Phone,
    CustomerType CustomerType);

public sealed record UpdateCustomerRequest(
    string Code,
    string Name,
    long CompanyId,
    long? SalesPersonId,
    string? Address,
    string? Phone,
    CustomerType CustomerType,
    bool IsActive);

public sealed record SiteResponse(
    long Id,
    string Code,
    string Name,
    long CompanyId,
    string CompanyName,
    string? Address,
    bool IsActive);

public sealed record CreateSiteRequest(
    string Code,
    string Name,
    long CompanyId,
    string? Address);

public sealed record UpdateSiteRequest(
    string Code,
    string Name,
    long CompanyId,
    string? Address,
    bool IsActive);

public sealed record InventoryResponse(
    long Id,
    long SiteId,
    string SiteCode,
    string SiteName,
    long ItemId,
    string ItemCode,
    string ItemName,
    decimal Quantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity);

public sealed record CreateInventoryRequest(
    long SiteId,
    long ItemId,
    decimal Quantity,
    decimal ReservedQuantity);

public sealed record UpdateInventoryRequest(
    decimal Quantity,
    decimal ReservedQuantity);

public sealed record CreateInventoryBatchRequest(
    long SiteId,
    IReadOnlyList<CreateInventoryBatchLineRequest> Lines);

public sealed record CreateInventoryBatchLineRequest(
    long ItemId,
    decimal Quantity);

public sealed record InventoryBatchResponse(
    long Id,
    string BatchNo,
    BatchType Type,
    long SiteId,
    string SiteCode,
    BatchStatus Status,
    string RefType,
    long RefId,
    IReadOnlyList<InventoryBatchLineResponse> Lines);

public sealed record InventoryBatchLineResponse(
    long Id,
    long ItemId,
    string ItemCode,
    string ItemName,
    decimal Quantity);

public sealed record StockTransactionResponse(
    long Id,
    long SiteId,
    string SiteCode,
    long ItemId,
    string ItemCode,
    StockTransactionType TransType,
    decimal Quantity,
    decimal BalanceAfter,
    string RefType,
    long RefId,
    DateTimeOffset CreatedAt,
    string? CreatedBy);

public sealed record InventoryReconciliationResponse(
    long SiteId,
    string SiteCode,
    long ItemId,
    string ItemCode,
    decimal InventoryQuantity,
    decimal LedgerQuantity,
    decimal Difference);
