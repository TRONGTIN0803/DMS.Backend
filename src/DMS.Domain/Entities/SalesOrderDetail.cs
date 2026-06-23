using DMS.Domain.Common;

namespace DMS.Domain.Entities;

public sealed class SalesOrderDetail : Entity
{
    public long SalesOrderId { get; set; }
    public long ItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal LineAmount { get; private set; }
    public decimal LineVatAmount { get; private set; }

    public SalesOrder SalesOrder { get; set; } = null!;
    public Item Item { get; set; } = null!;

    public void RecalculateAmounts()
    {
        LineAmount = decimal.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
        LineVatAmount = decimal.Round(LineAmount * VatRate / 100m, 2, MidpointRounding.AwayFromZero);
    }
}
