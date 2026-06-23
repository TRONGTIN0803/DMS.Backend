using DMS.Application.Abstractions;
using DMS.Domain.Entities;
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
    IOrderNumberGenerator orderNumberGenerator,
    IUnitOfWork unitOfWork) : ISalesOrderService
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
