using DMS.Domain.Common;
using DMS.Domain.Enums;

namespace DMS.Domain.Entities;

public sealed class Batch : AuditableEntity
{
    public string BatchNo { get; set; } = string.Empty;
    public BatchType Type { get; set; }
    public long SiteId { get; set; }
    public BatchStatus Status { get; set; } = BatchStatus.Draft;
    public string RefType { get; set; } = string.Empty;
    public long RefId { get; set; }

    public Site Site { get; set; } = null!;
    public ICollection<BatchDetail> Details { get; set; } = new List<BatchDetail>();

    public void Approve()
    {
        if (Status != BatchStatus.Draft)
        {
            throw new InvalidOperationException("Only draft batches can be approved.");
        }

        Status = BatchStatus.Approved;
    }
}
