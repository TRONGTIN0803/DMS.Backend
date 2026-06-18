using DMS.Domain.Common;
using DMS.Domain.Enums;

namespace DMS.Domain.Entities;

public sealed class Customer : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public long? SalesPersonId { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public CustomerType CustomerType { get; set; } = CustomerType.Retail;
    public bool IsActive { get; set; } = true;

    public Company Company { get; set; } = null!;
    public SalesPerson? SalesPerson { get; set; }
}

