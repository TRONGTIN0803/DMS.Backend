using DMS.Domain.Common;

namespace DMS.Domain.Entities;

public sealed class Company : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SalesPerson> SalesPeople { get; set; } = new List<SalesPerson>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<Site> Sites { get; set; } = new List<Site>();
}

