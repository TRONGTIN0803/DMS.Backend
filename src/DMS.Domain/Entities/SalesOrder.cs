using DMS.Domain.Common;
using DMS.Domain.Enums;

namespace DMS.Domain.Entities;

public sealed class SalesOrder : AuditableEntity
{
    public string OrderNo { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public long CustomerId { get; set; }
    public long? SalesPersonId { get; set; }
    public long SiteId { get; set; }
    public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;
    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;
    public decimal SubTotal { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal GrandTotal { get; private set; }
    public string? Note { get; set; }

    public Company Company { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public SalesPerson? SalesPerson { get; set; }
    public Site Site { get; set; } = null!;
    public ICollection<SalesOrderDetail> Details { get; set; } = new List<SalesOrderDetail>();
    public Invoice? Invoice { get; set; }

    public void RecalculateTotals()
    {
        foreach (var detail in Details)
        {
            detail.RecalculateAmounts();
        }

        SubTotal = Details.Sum(x => x.LineAmount);
        VatAmount = Details.Sum(x => x.LineVatAmount);
        GrandTotal = SubTotal + VatAmount;
    }

    public void Submit()
    {
        if (Status != SalesOrderStatus.Draft)
        {
            throw new InvalidOperationException("Only draft sales orders can be submitted.");
        }

        if (Details.Count == 0)
        {
            throw new InvalidOperationException("Sales order must contain at least one line.");
        }

        if (Details.Any(x => x.Quantity <= 0))
        {
            throw new InvalidOperationException("Sales order line quantities must be greater than zero.");
        }

        Status = SalesOrderStatus.Submitted;
    }

    public void Approve()
    {
        if (Status != SalesOrderStatus.Submitted)
        {
            throw new InvalidOperationException("Only submitted sales orders can be approved.");
        }

        Status = SalesOrderStatus.Approved;
    }

    public void Cancel()
    {
        if (Status is not (SalesOrderStatus.Draft or SalesOrderStatus.Submitted))
        {
            throw new InvalidOperationException("Only draft or submitted sales orders can be cancelled.");
        }

        Status = SalesOrderStatus.Cancelled;
    }
}
