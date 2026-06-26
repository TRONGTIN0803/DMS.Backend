using DMS.Application.Abstractions;
using DMS.Application.Events;
using DMS.Domain.Entities;
using DMS.Domain.Enums;
using DMS.Shared;
using Microsoft.EntityFrameworkCore;

namespace DMS.Application.Orders;

public sealed class SalesOrderService(
    IRepository<SalesOrder> salesOrdersRepository,
    IRepository<Company> companiesRepository,
    IRepository<Customer> customersRepository,
    IRepository<SalesPerson> salesPeopleRepository,
    IRepository<Site> sitesRepository,
    IRepository<Item> itemsRepository,
    IRepository<Inventory> inventoriesRepository,
    IRepository<Invoice> invoicesRepository,
    IRepository<Batch> batchesRepository,
    IRepository<StockTransaction> stockTransactionsRepository,
    IOrderNumberGenerator orderNumberGenerator,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IOutboxWriter outboxWriter) : ISalesOrderService
{
    public async Task<Result<SalesOrderResponse>> CreateDraftAsync(CreateSalesOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (!await companiesRepository.Query().AnyAsync(x => x.Id == request.CompanyId, cancellationToken))
        {
            return Result<SalesOrderResponse>.Failure(new Error("Company.NotFound", "Company does not exist."));
        }

        if (!await customersRepository.Query().AnyAsync(x => x.Id == request.CustomerId && x.CompanyId == request.CompanyId, cancellationToken))
        {
            return Result<SalesOrderResponse>.Failure(new Error("Customer.NotFound", "Customer does not exist for this company."));
        }

        if (request.SalesPersonId is not null &&
            !await salesPeopleRepository.Query().AnyAsync(x => x.Id == request.SalesPersonId && x.CompanyId == request.CompanyId, cancellationToken))
        {
            return Result<SalesOrderResponse>.Failure(new Error("SalesPerson.NotFound", "Sales person does not exist for this company."));
        }

        if (!await sitesRepository.Query().AnyAsync(x => x.Id == request.SiteId && x.CompanyId == request.CompanyId, cancellationToken))
        {
            return Result<SalesOrderResponse>.Failure(new Error("Site.NotFound", "Site does not exist for this company."));
        }

        var itemIds = request.Lines.Select(x => x.ItemId).Distinct().ToArray();
        var items = await itemsRepository.Query()
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (items.Count != itemIds.Length)
        {
            return Result<SalesOrderResponse>.Failure(new Error("Item.NotFound", "One or more items do not exist."));
        }

        var order = new SalesOrder
        {
            OrderNo = await orderNumberGenerator.NextSalesOrderNoAsync(cancellationToken),
            CompanyId = request.CompanyId,
            CustomerId = request.CustomerId,
            SalesPersonId = request.SalesPersonId,
            SiteId = request.SiteId,
            OrderDate = request.OrderDate ?? DateTimeOffset.UtcNow,
            Note = request.Note?.Trim()
        };

        foreach (var line in request.Lines)
        {
            var item = items[line.ItemId];
            order.Details.Add(new SalesOrderDetail
            {
                ItemId = item.Id,
                Quantity = line.Quantity,
                UnitPrice = item.Price,
                VatRate = item.VatRate
            });
        }

        order.RecalculateTotals();
        salesOrdersRepository.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var savedOrder = await salesOrdersRepository.Query()
            .AsNoTracking()
            .Include(x => x.Details)
            .ThenInclude(x => x.Item)
            .FirstAsync(x => x.Id == order.Id, cancellationToken);

        return Result<SalesOrderResponse>.Success(ToResponse(savedOrder));
    }

    public async Task<Result<SalesOrderResponse>> SubmitAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var order = await salesOrdersRepository.Query()
            .Include(x => x.Details)
            .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (order is null)
        {
            return Result<SalesOrderResponse>.Failure(new Error("SalesOrder.NotFound", "Sales order does not exist."));
        }

        if (order.Status == SalesOrderStatus.Submitted)
        {
            return Result<SalesOrderResponse>.Success(ToResponse(order));
        }

        try
        {
            order.Submit();
        }
        catch (InvalidOperationException ex)
        {
            return Result<SalesOrderResponse>.Failure(new Error("SalesOrder.InvalidState", ex.Message));
        }

        var itemIds = order.Details.Select(x => x.ItemId).Distinct().ToArray();
        var inventories = await inventoriesRepository.Query()
            .Where(x => x.SiteId == order.SiteId && itemIds.Contains(x.ItemId))
            .ToDictionaryAsync(x => x.ItemId, cancellationToken);

        foreach (var detailGroup in order.Details.GroupBy(x => x.ItemId).OrderBy(x => x.Key))
        {
            if (!inventories.TryGetValue(detailGroup.Key, out var inventory))
            {
                return Result<SalesOrderResponse>.Failure(new Error("Inventory.NotFound", $"Inventory does not exist for item {detailGroup.Key} at site {order.SiteId}."));
            }

            var requestedQuantity = detailGroup.Sum(x => x.Quantity);
            var availableQuantity = inventory.Quantity - inventory.ReservedQuantity;
            if (availableQuantity < requestedQuantity)
            {
                return Result<SalesOrderResponse>.Failure(new Error("Inventory.InsufficientStock", $"Insufficient stock for item {detailGroup.Key}."));
            }

            inventory.ReservedQuantity += requestedQuantity;
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<SalesOrderResponse>.Failure(new Error("Inventory.ConcurrencyConflict", "Inventory changed while submitting this order. Please retry."));
        }

        return Result<SalesOrderResponse>.Success(ToResponse(order));
    }

    public async Task<Result<SalesOrderResponse>> ApproveAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var order = await salesOrdersRepository.Query()
            .Include(x => x.Details)
            .ThenInclude(x => x.Item)
            .Include(x => x.Invoice)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (order is null)
        {
            return Result<SalesOrderResponse>.Failure(new Error("SalesOrder.NotFound", "Sales order does not exist."));
        }

        if (order.Status == SalesOrderStatus.Approved)
        {
            return Result<SalesOrderResponse>.Success(ToResponse(order));
        }

        try
        {
            order.Approve();
        }
        catch (InvalidOperationException ex)
        {
            return Result<SalesOrderResponse>.Failure(new Error("SalesOrder.InvalidState", ex.Message));
        }

        var itemIds = order.Details.Select(x => x.ItemId).Distinct().ToArray();
        var inventories = await inventoriesRepository.Query()
            .Where(x => x.SiteId == order.SiteId && itemIds.Contains(x.ItemId))
            .ToDictionaryAsync(x => x.ItemId, cancellationToken);

        var batch = new Batch
        {
            BatchNo = $"OM-{order.OrderNo}",
            Type = BatchType.Out,
            SiteId = order.SiteId,
            RefType = "SalesOrder",
            RefId = order.Id
        };

        foreach (var detail in order.Details.OrderBy(x => x.ItemId))
        {
            if (!inventories.TryGetValue(detail.ItemId, out var inventory))
            {
                return Result<SalesOrderResponse>.Failure(new Error("Inventory.NotFound", $"Inventory does not exist for item {detail.ItemId} at site {order.SiteId}."));
            }

            if (inventory.ReservedQuantity < detail.Quantity)
            {
                return Result<SalesOrderResponse>.Failure(new Error("Inventory.ReservationNotFound", $"Reserved stock is not available for item {detail.ItemId}."));
            }

            inventory.Quantity -= detail.Quantity;
            inventory.ReservedQuantity -= detail.Quantity;

            batch.Details.Add(new BatchDetail
            {
                ItemId = detail.ItemId,
                Quantity = detail.Quantity
            });
        }

        try
        {
            batch.Approve();
            batchesRepository.Add(batch);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var detail in order.Details.OrderBy(x => x.ItemId))
            {
                var inventory = inventories[detail.ItemId];
                stockTransactionsRepository.Add(new StockTransaction
                {
                    SiteId = order.SiteId,
                    ItemId = detail.ItemId,
                    TransType = StockTransactionType.Out,
                    Quantity = -detail.Quantity,
                    BalanceAfter = inventory.Quantity,
                    RefType = "Batch",
                    RefId = batch.Id,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = currentUserService.UserId
                });

                var inventoryMessageId = Guid.NewGuid();
                outboxWriter.Add(
                    new InventoryUpdatedEvent(
                        inventoryMessageId,
                        order.SiteId,
                        detail.ItemId,
                        -detail.Quantity,
                        inventory.Quantity,
                        "Batch",
                        batch.Id,
                        DateTimeOffset.UtcNow),
                    inventoryMessageId);
            }

            if (order.Invoice is null)
            {
                var invoice = new Invoice
                {
                    InvoiceNo = $"INV-{order.OrderNo}",
                    SalesOrderId = order.Id,
                    CustomerId = order.CustomerId,
                    InvoiceDate = DateTimeOffset.UtcNow,
                    SubTotal = order.SubTotal,
                    VatAmount = order.VatAmount,
                    GrandTotal = order.GrandTotal
                };

                invoicesRepository.Add(invoice);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                var invoiceMessageId = Guid.NewGuid();
                outboxWriter.Add(
                    new InvoiceCreatedEvent(
                        invoiceMessageId,
                        invoice.Id,
                        invoice.InvoiceNo,
                        order.Id,
                        order.CustomerId,
                        order.GrandTotal,
                        invoice.InvoiceDate),
                    invoiceMessageId);
            }

            var salesOrderMessageId = Guid.NewGuid();
            outboxWriter.Add(
                new SalesOrderApprovedEvent(
                    salesOrderMessageId,
                    order.Id,
                    order.OrderNo,
                    order.CustomerId,
                    order.SiteId,
                    order.GrandTotal,
                    DateTimeOffset.UtcNow),
                salesOrderMessageId);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<SalesOrderResponse>.Failure(new Error("Inventory.ConcurrencyConflict", "Inventory changed while approving this order. Please retry."));
        }

        return Result<SalesOrderResponse>.Success(ToResponse(order));
    }

    public async Task<Result<SalesOrderResponse>> CancelAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var order = await salesOrdersRepository.Query()
            .Include(x => x.Details)
            .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (order is null)
        {
            return Result<SalesOrderResponse>.Failure(new Error("SalesOrder.NotFound", "Sales order does not exist."));
        }

        if (order.Status == SalesOrderStatus.Cancelled)
        {
            return Result<SalesOrderResponse>.Success(ToResponse(order));
        }

        var shouldReleaseReservation = order.Status == SalesOrderStatus.Submitted;

        try
        {
            order.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return Result<SalesOrderResponse>.Failure(new Error("SalesOrder.InvalidState", ex.Message));
        }

        if (shouldReleaseReservation)
        {
            var itemIds = order.Details.Select(x => x.ItemId).Distinct().ToArray();
            var inventories = await inventoriesRepository.Query()
                .Where(x => x.SiteId == order.SiteId && itemIds.Contains(x.ItemId))
                .ToDictionaryAsync(x => x.ItemId, cancellationToken);

            foreach (var detailGroup in order.Details.GroupBy(x => x.ItemId).OrderBy(x => x.Key))
            {
                if (!inventories.TryGetValue(detailGroup.Key, out var inventory))
                {
                    return Result<SalesOrderResponse>.Failure(new Error("Inventory.NotFound", $"Inventory does not exist for item {detailGroup.Key} at site {order.SiteId}."));
                }

                var reservedQuantity = detailGroup.Sum(x => x.Quantity);
                if (inventory.ReservedQuantity < reservedQuantity)
                {
                    return Result<SalesOrderResponse>.Failure(new Error("Inventory.ReservationNotFound", $"Reserved stock is not available for item {detailGroup.Key}."));
                }

                inventory.ReservedQuantity -= reservedQuantity;
            }
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<SalesOrderResponse>.Failure(new Error("Inventory.ConcurrencyConflict", "Inventory changed while cancelling this order. Please retry."));
        }

        return Result<SalesOrderResponse>.Success(ToResponse(order));
    }

    private static SalesOrderResponse ToResponse(SalesOrder order) =>
        new(
            order.Id,
            order.OrderNo,
            order.CompanyId,
            order.CustomerId,
            order.SalesPersonId,
            order.SiteId,
            order.OrderDate,
            order.Status,
            order.SubTotal,
            order.VatAmount,
            order.GrandTotal,
            order.Note,
            order.Details
                .OrderBy(x => x.Id)
                .Select(x => new SalesOrderLineResponse(
                    x.Id,
                    x.ItemId,
                    x.Item.Code,
                    x.Item.Name,
                    x.Quantity,
                    x.UnitPrice,
                    x.VatRate,
                    x.LineAmount,
                    x.LineVatAmount))
                .ToList());
}
