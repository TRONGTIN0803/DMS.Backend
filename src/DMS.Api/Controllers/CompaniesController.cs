using DMS.Application.Catalog;
using DMS.Domain.Entities;
using DMS.Infrastructure.Persistence;
using DMS.Shared;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/v1/companies")]
public sealed class CompaniesController(
    ApplicationDbContext dbContext,
    IValidator<CreateCompanyRequest> createValidator,
    IValidator<UpdateCompanyRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CompanyResponse>>> GetCompanies([FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        var page = query.SafePage;
        var pageSize = query.SafePageSize;
        var companiesQuery = dbContext.Companies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            companiesQuery = companiesQuery.Where(x => x.Code.ToLower().Contains(keyword) || x.Name.ToLower().Contains(keyword));
        }

        companiesQuery = query.Sort?.ToLowerInvariant() switch
        {
            "code_desc" => companiesQuery.OrderByDescending(x => x.Code),
            "name" => companiesQuery.OrderBy(x => x.Name),
            "name_desc" => companiesQuery.OrderByDescending(x => x.Name),
            _ => companiesQuery.OrderBy(x => x.Code)
        };

        var totalCount = await companiesQuery.CountAsync(cancellationToken);
        var companies = await companiesQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CompanyResponse(x.Id, x.Code, x.Name, x.TaxCode, x.Address, x.Phone, x.Email, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<CompanyResponse>(companies, page, pageSize, totalCount));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CompanyResponse>> GetCompany(long id, CancellationToken cancellationToken)
    {
        var company = await dbContext.Companies
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CompanyResponse(x.Id, x.Code, x.Name, x.TaxCode, x.Address, x.Phone, x.Email, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
        return company is null ? NotFound() : Ok(company);
    }

    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> CreateCompany(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        if (await dbContext.Companies.AnyAsync(x => x.Code == request.Code, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.Code), "Company code already exists.");
            return ValidationProblem(ModelState);
        }

        var company = new Company
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            TaxCode = request.TaxCode?.Trim(),
            Address = request.Address?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim()
        };

        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, ToResponse(company));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CompanyResponse>> UpdateCompany(long id, UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var company = await dbContext.Companies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        if (await dbContext.Companies.AnyAsync(x => x.Id != id && x.Code == request.Code, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.Code), "Company code already exists.");
            return ValidationProblem(ModelState);
        }

        company.Code = request.Code.Trim();
        company.Name = request.Name.Trim();
        company.TaxCode = request.TaxCode?.Trim();
        company.Address = request.Address?.Trim();
        company.Phone = request.Phone?.Trim();
        company.Email = request.Email?.Trim();
        company.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(company));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteCompany(long id, CancellationToken cancellationToken)
    {
        var company = await dbContext.Companies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        company.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static CompanyResponse ToResponse(Company company) =>
        new(company.Id, company.Code, company.Name, company.TaxCode, company.Address, company.Phone, company.Email, company.IsActive);
}
