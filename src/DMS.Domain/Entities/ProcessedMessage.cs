namespace DMS.Domain.Entities;

public sealed class ProcessedMessage
{
    public Guid Id { get; set; }
    public string Handler { get; set; } = string.Empty;
    public DateTimeOffset ProcessedOn { get; set; }
}
