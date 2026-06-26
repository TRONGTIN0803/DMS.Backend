using DMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMS.Infrastructure.Jobs;

public sealed class InventoryReconciliationJob(
    ApplicationDbContext dbContext,
    ILogger<InventoryReconciliationJob> logger)
{
    public async Task<int> RecalculateInventoryAsync(CancellationToken cancellationToken = default)
    {
        var ledgerQuantities = await dbContext.StockTransactions
            .AsNoTracking()
            .GroupBy(x => new { x.SiteId, x.ItemId })
            .Select(x => new
            {
                x.Key.SiteId,
                x.Key.ItemId,
                Quantity = x.Sum(transaction => transaction.Quantity)
            })
            .ToDictionaryAsync(x => (x.SiteId, x.ItemId), cancellationToken);

        var mismatches = 0;
        var inventories = await dbContext.Inventories
            .AsNoTracking()
            .Include(x => x.Site)
            .Include(x => x.Item)
            .ToListAsync(cancellationToken);

        foreach (var inventory in inventories)
        {
            var ledgerQuantity = ledgerQuantities.TryGetValue((inventory.SiteId, inventory.ItemId), out var value) ? value.Quantity : 0m;
            if (inventory.Quantity == ledgerQuantity)
            {
                continue;
            }

            mismatches++;
            logger.LogWarning(
                "Inventory mismatch for site {SiteCode}, item {ItemCode}: inventory={InventoryQuantity}, ledger={LedgerQuantity}",
                inventory.Site.Code,
                inventory.Item.Code,
                inventory.Quantity,
                ledgerQuantity);
        }

        return mismatches;
    }
}
