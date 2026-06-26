using DMS.Domain.Common;

namespace DMS.Domain.Entities;

public sealed class BatchDetail : Entity
{
    public long BatchId { get; set; }
    public long ItemId { get; set; }
    public decimal Quantity { get; set; }

    public Batch Batch { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
