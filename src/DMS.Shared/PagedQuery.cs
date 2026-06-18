namespace DMS.Shared;

public sealed record PagedQuery(int Page = 1, int PageSize = 20, string? Search = null, string? Sort = null)
{
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize switch
    {
        < 1 => 20,
        > 100 => 100,
        _ => PageSize
    };
}
