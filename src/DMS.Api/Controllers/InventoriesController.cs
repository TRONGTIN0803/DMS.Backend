using DMS.Application.Catalog;
using DMS.Domain.Entities;
using DMS.Infrastructure.Persistence;
using DMS.Shared;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/v1/inventories")]
public sealed class InventoriesController(
    ApplicationDbContext dbContext,
    IValidator<CreateInventoryRequest> createValidator,
    IValidator<UpdateInventoryRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryResponse>>> GetInventories([FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        var page = query.SafePage;
        var pageSize = query.SafePageSize;
        IQueryable<Inventory> inventoriesQuery = dbContext.Inventories.AsNoTracking().Include(x => x.Site).Include(x => x.Item);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            inventoriesQuery = inventoriesQuery.Where(x =>
                x.Site.Code.ToLower().Contains(keyword) ||
                x.Site.Name.ToLower().Contains(keyword) ||
                x.Item.Code.ToLower().Contains(keyword) ||
                x.Item.Name.ToLower().Contains(keyword));
        }

        inventoriesQuery = query.Sort?.ToLowerInvariant() switch
        {
            "quantity_desc" => inventoriesQuery.OrderByDescending(x => x.Quantity),
            "available" => inventoriesQuery.OrderBy(x => x.Quantity - x.ReservedQuantity),
            "available_desc" => inventoriesQuery.OrderByDescending(x => x.Quantity - x.ReservedQuantity),
            "item" => inventoriesQuery.OrderBy(x => x.Item.Code),
            "site" => inventoriesQuery.OrderBy(x => x.Site.Code),
            _ => inventoriesQuery.OrderBy(x => x.Site.Code).ThenBy(x => x.Item.Code)
        };

        var totalCount = await inventoriesQuery.CountAsync(cancellationToken);
        var inventories = await inventoriesQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new InventoryResponse(
                x.Id,
                x.SiteId,
                x.Site.Code,
                x.Site.Name,
                x.ItemId,
                x.Item.Code,
                x.Item.Name,
                x.Quantity,
                x.ReservedQuantity,
                x.Quantity - x.ReservedQuantity))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<InventoryResponse>(inventories, page, pageSize, totalCount));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<InventoryResponse>> GetInventory(long id, CancellationToken cancellationToken)
    {
        var inventory = await dbContext.Inventories
            .AsNoTracking()
            .Include(x => x.Site)
            .Include(x => x.Item)
            .Where(x => x.Id == id)
            .Select(x => new InventoryResponse(
                x.Id,
                x.SiteId,
                x.Site.Code,
                x.Site.Name,
                x.ItemId,
                x.Item.Code,
                x.Item.Name,
                x.Quantity,
                x.ReservedQuantity,
                x.Quantity - x.ReservedQuantity))
            .FirstOrDefaultAsync(cancellationToken);

        return inventory is null ? NotFound() : Ok(inventory);
    }

    [HttpGet("by-site-item")]
    public async Task<ActionResult<InventoryResponse>> GetInventoryBySiteItem([FromQuery] long siteId, [FromQuery] long itemId, CancellationToken cancellationToken)
    {
        var inventory = await dbContext.Inventories
            .AsNoTracking()
            .Include(x => x.Site)
            .Include(x => x.Item)
            .Where(x => x.SiteId == siteId && x.ItemId == itemId)
            .Select(x => new InventoryResponse(
                x.Id,
                x.SiteId,
                x.Site.Code,
                x.Site.Name,
                x.ItemId,
                x.Item.Code,
                x.Item.Name,
                x.Quantity,
                x.ReservedQuantity,
                x.Quantity - x.ReservedQuantity))
            .FirstOrDefaultAsync(cancellationToken);

        return inventory is null ? NotFound() : Ok(inventory);
    }

    [HttpPost]
    public async Task<ActionResult<InventoryResponse>> CreateInventory(CreateInventoryRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var relationshipError = await ValidateRelationshipsAsync(request.SiteId, request.ItemId, cancellationToken);
        if (relationshipError is not null)
        {
            ModelState.AddModelError(relationshipError.Value.Field, relationshipError.Value.Message);
            return ValidationProblem(ModelState);
        }

        if (await dbContext.Inventories.AnyAsync(x => x.SiteId == request.SiteId && x.ItemId == request.ItemId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.ItemId), "Inventory already exists for this site and item.");
            return ValidationProblem(ModelState);
        }

        var inventory = new Inventory
        {
            SiteId = request.SiteId,
            ItemId = request.ItemId,
            Quantity = request.Quantity,
            ReservedQuantity = request.ReservedQuantity
        };

        dbContext.Inventories.Add(inventory);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetInventory(inventory.Id, cancellationToken);
        return CreatedAtAction(nameof(GetInventory), new { id = inventory.Id }, response.Value);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<InventoryResponse>> UpdateInventory(long id, UpdateInventoryRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var inventory = await dbContext.Inventories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (inventory is null)
        {
            return NotFound();
        }

        inventory.Quantity = request.Quantity;
        inventory.ReservedQuantity = request.ReservedQuantity;

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetInventory(inventory.Id, cancellationToken);
        return Ok(response.Value);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteInventory(long id, CancellationToken cancellationToken)
    {
        var inventory = await dbContext.Inventories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (inventory is null)
        {
            return NotFound();
        }

        dbContext.Inventories.Remove(inventory);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<(string Field, string Message)?> ValidateRelationshipsAsync(long siteId, long itemId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Sites.AnyAsync(x => x.Id == siteId, cancellationToken))
        {
            return (nameof(siteId), "Site does not exist.");
        }

        if (!await dbContext.Items.AnyAsync(x => x.Id == itemId, cancellationToken))
        {
            return (nameof(itemId), "Item does not exist.");
        }

        return null;
    }
}
