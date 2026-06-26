using DMS.Shared;

namespace DMS.Application.Catalog;

public interface IInventoryBatchService
{
    Task<Result<InventoryBatchResponse>> CreateInboundBatchAsync(CreateInventoryBatchRequest request, CancellationToken cancellationToken = default);
}
