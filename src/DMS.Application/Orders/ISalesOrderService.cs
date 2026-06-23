using DMS.Shared;

namespace DMS.Application.Orders;

public interface ISalesOrderService
{
    Task<Result<SalesOrderResponse>> CreateDraftAsync(CreateSalesOrderRequest request, CancellationToken cancellationToken = default);
}
