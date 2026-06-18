using DMS.Domain.Common;

namespace DMS.Domain.Entities;

public sealed class Inventory : Entity
{
    public long SiteId { get; set; }
    public long ItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }

    public Site Site { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
