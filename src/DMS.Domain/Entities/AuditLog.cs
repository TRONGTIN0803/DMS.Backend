namespace DMS.Domain.Entities;

public sealed class AuditLog
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public DateTimeOffset OccurredOn { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
