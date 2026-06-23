namespace DMS.Domain.Enums;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Sales = "Sales";
    public const string Warehouse = "Warehouse";

    public const string All = $"{Admin},{Sales},{Warehouse}";
}
