using DMS.Domain.Common;

namespace DMS.Domain.Entities;

public sealed class ApplicationUser : AuditableEntity
{
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
