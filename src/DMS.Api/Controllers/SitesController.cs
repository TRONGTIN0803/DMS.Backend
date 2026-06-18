using DMS.Application.Catalog;
using DMS.Domain.Entities;
using DMS.Infrastructure.Persistence;
using DMS.Shared;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/v1/sites")]
public sealed class SitesController(
    ApplicationDbContext dbContext,
    IValidator<CreateSiteRequest> createValidator,
    IValidator<UpdateSiteRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SiteResponse>>> GetSites([FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        var page = query.SafePage;
        var pageSize = query.SafePageSize;
        IQueryable<Site> sitesQuery = dbContext.Sites.AsNoTracking().Include(x => x.Company);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            sitesQuery = sitesQuery.Where(x => x.Code.ToLower().Contains(keyword) || x.Name.ToLower().Contains(keyword));
        }

        sitesQuery = query.Sort?.ToLowerInvariant() switch
        {
            "code_desc" => sitesQuery.OrderByDescending(x => x.Code),
            "name" => sitesQuery.OrderBy(x => x.Name),
            "name_desc" => sitesQuery.OrderByDescending(x => x.Name),
            _ => sitesQuery.OrderBy(x => x.Code)
        };

        var totalCount = await sitesQuery.CountAsync(cancellationToken);
        var sites = await sitesQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SiteResponse(x.Id, x.Code, x.Name, x.CompanyId, x.Company.Name, x.Address, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<SiteResponse>(sites, page, pageSize, totalCount));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<SiteResponse>> GetSite(long id, CancellationToken cancellationToken)
    {
        var site = await dbContext.Sites
            .AsNoTracking()
            .Include(x => x.Company)
            .Where(x => x.Id == id)
            .Select(x => new SiteResponse(x.Id, x.Code, x.Name, x.CompanyId, x.Company.Name, x.Address, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return site is null ? NotFound() : Ok(site);
    }

    [HttpPost]
    public async Task<ActionResult<SiteResponse>> CreateSite(CreateSiteRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        if (!await dbContext.Companies.AnyAsync(x => x.Id == request.CompanyId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.CompanyId), "Company does not exist.");
            return ValidationProblem(ModelState);
        }

        if (await dbContext.Sites.AnyAsync(x => x.Code == request.Code, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.Code), "Site code already exists.");
            return ValidationProblem(ModelState);
        }

        var site = new Site
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            CompanyId = request.CompanyId,
            Address = request.Address?.Trim()
        };

        dbContext.Sites.Add(site);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetSite(site.Id, cancellationToken);
        return CreatedAtAction(nameof(GetSite), new { id = site.Id }, response.Value);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<SiteResponse>> UpdateSite(long id, UpdateSiteRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var site = await dbContext.Sites.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (site is null)
        {
            return NotFound();
        }

        if (!await dbContext.Companies.AnyAsync(x => x.Id == request.CompanyId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.CompanyId), "Company does not exist.");
            return ValidationProblem(ModelState);
        }

        if (await dbContext.Sites.AnyAsync(x => x.Id != id && x.Code == request.Code, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.Code), "Site code already exists.");
            return ValidationProblem(ModelState);
        }

        site.Code = request.Code.Trim();
        site.Name = request.Name.Trim();
        site.CompanyId = request.CompanyId;
        site.Address = request.Address?.Trim();
        site.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetSite(site.Id, cancellationToken);
        return Ok(response.Value);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteSite(long id, CancellationToken cancellationToken)
    {
        var site = await dbContext.Sites.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (site is null)
        {
            return NotFound();
        }

        site.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
