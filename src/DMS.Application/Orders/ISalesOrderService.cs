using DMS.Shared;

namespace DMS.Application.Orders;

public interface ISalesOrderService
{
    Task<Result<SalesOrderResponse>> CreateDraftAsync(CreateSalesOrderRequest request, CancellationToken cancellationToken = default);
    Task<Result<SalesOrderResponse>> SubmitAsync(long id, CancellationToken cancellationToken = default);
    Task<Result<SalesOrderResponse>> ApproveAsync(long id, CancellationToken cancellationToken = default);
    Task<Result<SalesOrderResponse>> CancelAsync(long id, CancellationToken cancellationToken = default);
}
