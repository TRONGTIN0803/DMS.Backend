namespace DMS.Application.Orders;

public interface IOrderNumberGenerator
{
    Task<string> NextSalesOrderNoAsync(CancellationToken cancellationToken = default);
}
