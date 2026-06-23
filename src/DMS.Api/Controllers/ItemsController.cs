using DMS.Api.Auth;
using DMS.Application.Catalog;
using DMS.Application.Abstractions;
using DMS.Domain.Entities;
using DMS.Shared;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DMS.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.MasterDataRead)]
[Route("api/v1/items")]
public sealed class ItemsController(
    IRepository<Item> itemsRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateItemRequest> createValidator,
    IValidator<UpdateItemRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ItemResponse>>> GetItems([FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        var page = query.SafePage;
        var pageSize = query.SafePageSize;

        var itemsQuery = itemsRepository.Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            itemsQuery = itemsQuery.Where(x =>
                x.Code.ToLower().Contains(keyword) ||
                x.Name.ToLower().Contains(keyword));
        }

        itemsQuery = query.Sort?.ToLowerInvariant() switch
        {
            "code_desc" => itemsQuery.OrderByDescending(x => x.Code),
            "name" => itemsQuery.OrderBy(x => x.Name),
            "name_desc" => itemsQuery.OrderByDescending(x => x.Name),
            _ => itemsQuery.OrderBy(x => x.Code)
        };

        var totalCount = await itemsQuery.CountAsync(cancellationToken);
        var items = await itemsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ItemResponse(x.Id, x.Code, x.Name, x.Unit, x.Barcode, x.Price, x.VatRate, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<ItemResponse>(items, page, pageSize, totalCount));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ItemResponse>> GetItem(long id, CancellationToken cancellationToken)
    {
        var item = await itemsRepository.Query()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ItemResponse(x.Id, x.Code, x.Name, x.Unit, x.Barcode, x.Price, x.VatRate, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<ActionResult<ItemResponse>> CreateItem(CreateItemRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var codeExists = await itemsRepository.Query().AnyAsync(x => x.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            ModelState.AddModelError(nameof(request.Code), "Item code already exists.");
            return ValidationProblem(ModelState);
        }

        var item = new Item
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Unit = request.Unit.Trim(),
            Barcode = request.Barcode?.Trim(),
            Price = request.Price,
            VatRate = request.VatRate
        };

        itemsRepository.Add(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new ItemResponse(item.Id, item.Code, item.Name, item.Unit, item.Barcode, item.Price, item.VatRate, item.IsActive);
        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, response);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<ActionResult<ItemResponse>> UpdateItem(long id, UpdateItemRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var item = await itemsRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var codeExists = await itemsRepository.Query().AnyAsync(x => x.Id != id && x.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            ModelState.AddModelError(nameof(request.Code), "Item code already exists.");
            return ValidationProblem(ModelState);
        }

        item.Code = request.Code.Trim();
        item.Name = request.Name.Trim();
        item.Unit = request.Unit.Trim();
        item.Barcode = request.Barcode?.Trim();
        item.Price = request.Price;
        item.VatRate = request.VatRate;
        item.IsActive = request.IsActive;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new ItemResponse(item.Id, item.Code, item.Name, item.Unit, item.Barcode, item.Price, item.VatRate, item.IsActive));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<IActionResult> DeleteItem(long id, CancellationToken cancellationToken)
    {
        var item = await itemsRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.IsDeleted = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
