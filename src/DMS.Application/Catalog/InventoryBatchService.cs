using DMS.Application.Abstractions;
using DMS.Domain.Entities;
using DMS.Domain.Enums;
using DMS.Shared;
using Microsoft.EntityFrameworkCore;

namespace DMS.Application.Catalog;

public sealed class InventoryBatchService(
    IRepository<Batch> batchesRepository,
    IRepository<Inventory> inventoriesRepository,
    IRepository<Item> itemsRepository,
    IRepository<Site> sitesRepository,
    IRepository<StockTransaction> stockTransactionsRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService) : IInventoryBatchService
{
    public async Task<Result<InventoryBatchResponse>> CreateInboundBatchAsync(CreateInventoryBatchRequest request, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var site = await sitesRepository.Query()
            .FirstOrDefaultAsync(x => x.Id == request.SiteId, cancellationToken);

        if (site is null)
        {
            return Result<InventoryBatchResponse>.Failure(new Error("Site.NotFound", "Site does not exist."));
        }

        var requestedLines = request.Lines
            .GroupBy(x => x.ItemId)
            .Select(x => new
            {
                ItemId = x.Key,
                Quantity = x.Sum(line => line.Quantity)
            })
            .OrderBy(x => x.ItemId)
            .ToList();

        var itemIds = requestedLines.Select(x => x.ItemId).ToArray();
        var items = await itemsRepository.Query()
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (items.Count != itemIds.Length)
        {
            return Result<InventoryBatchResponse>.Failure(new Error("Item.NotFound", "One or more items do not exist."));
        }

        var inventories = await inventoriesRepository.Query()
            .Where(x => x.SiteId == request.SiteId && itemIds.Contains(x.ItemId))
            .ToDictionaryAsync(x => x.ItemId, cancellationToken);

        var batch = new Batch
        {
            BatchNo = CreateInboundBatchNo(),
            Type = BatchType.In,
            SiteId = request.SiteId,
            RefType = "Manual",
            RefId = 0
        };

        foreach (var line in requestedLines)
        {
            if (!inventories.TryGetValue(line.ItemId, out var inventory))
            {
                inventory = new Inventory
                {
                    SiteId = request.SiteId,
                    ItemId = line.ItemId,
                    Quantity = 0m,
                    ReservedQuantity = 0m
                };

                inventoriesRepository.Add(inventory);
                inventories[line.ItemId] = inventory;
            }

            inventory.Quantity += line.Quantity;

            batch.Details.Add(new BatchDetail
            {
                ItemId = line.ItemId,
                Quantity = line.Quantity
            });
        }

        batch.Approve();
        batchesRepository.Add(batch);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var line in requestedLines)
        {
            var inventory = inventories[line.ItemId];
            stockTransactionsRepository.Add(new StockTransaction
            {
                SiteId = request.SiteId,
                ItemId = line.ItemId,
                TransType = StockTransactionType.In,
                Quantity = line.Quantity,
                BalanceAfter = inventory.Quantity,
                RefType = "Batch",
                RefId = batch.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = currentUserService.UserId
            });
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<InventoryBatchResponse>.Success(ToResponse(batch, site, items));
    }

    private static string CreateInboundBatchNo() =>
        $"IN-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..30].ToUpperInvariant();

    private static InventoryBatchResponse ToResponse(Batch batch, Site site, IReadOnlyDictionary<long, Item> items) =>
        new(
            batch.Id,
            batch.BatchNo,
            batch.Type,
            batch.SiteId,
            site.Code,
            batch.Status,
            batch.RefType,
            batch.RefId,
            batch.Details
                .OrderBy(x => x.Id)
                .Select(x =>
                {
                    var item = items[x.ItemId];
                    return new InventoryBatchLineResponse(
                        x.Id,
                        x.ItemId,
                        item.Code,
                        item.Name,
                        x.Quantity);
                })
                .ToList());
}
