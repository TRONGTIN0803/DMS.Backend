using DMS.Domain.Common;

namespace DMS.Domain.Entities;

public sealed class SalesPerson : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;

    public Company Company { get; set; } = null!;
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}

