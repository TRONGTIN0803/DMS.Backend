namespace DMS.Application.Catalog;

public static class MasterDataCacheKeys
{
    public static string Item(long id) => $"item:{id}";
    public static string Customer(long id) => $"customer:{id}";
    public static string Company(long id) => $"company:{id}";
}
