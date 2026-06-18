using DMS.Domain.Common;

namespace DMS.Domain.Entities;

public sealed class Site : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public Company Company { get; set; } = null!;
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}

