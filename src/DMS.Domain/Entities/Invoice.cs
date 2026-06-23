using DMS.Domain.Common;
using DMS.Domain.Enums;

namespace DMS.Domain.Entities;

public sealed class Invoice : AuditableEntity
{
    public string InvoiceNo { get; set; } = string.Empty;
    public long SalesOrderId { get; set; }
    public long CustomerId { get; set; }
    public DateTimeOffset InvoiceDate { get; set; } = DateTimeOffset.UtcNow;
    public decimal SubTotal { get; set; }
    public decimal VatAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;

    public SalesOrder SalesOrder { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}
