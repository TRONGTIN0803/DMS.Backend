using DMS.Domain.Common;

namespace DMS.Domain.Entities;

public sealed class Item : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal Price { get; set; }
    public decimal VatRate { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}

