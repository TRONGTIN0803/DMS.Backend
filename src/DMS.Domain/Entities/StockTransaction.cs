using DMS.Domain.Common;
using DMS.Domain.Enums;

namespace DMS.Domain.Entities;

public sealed class StockTransaction : Entity
{
    public long SiteId { get; set; }
    public long ItemId { get; set; }
    public StockTransactionType TransType { get; set; }
    public decimal Quantity { get; set; }
    public decimal BalanceAfter { get; set; }
    public string RefType { get; set; } = string.Empty;
    public long RefId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }

    public Site Site { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
