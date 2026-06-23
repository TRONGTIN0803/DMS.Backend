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
[Route("api/v1/companies")]
public sealed class CompaniesController(
    IRepository<Company> companiesRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateCompanyRequest> createValidator,
    IValidator<UpdateCompanyRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CompanyResponse>>> GetCompanies([FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        var page = query.SafePage;
        var pageSize = query.SafePageSize;
        var companiesQuery = companiesRepository.Query().AsNoTracking();

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
        var company = await companiesRepository.Query()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CompanyResponse(x.Id, x.Code, x.Name, x.TaxCode, x.Address, x.Phone, x.Email, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
        return company is null ? NotFound() : Ok(company);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<ActionResult<CompanyResponse>> CreateCompany(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        if (await companiesRepository.Query().AnyAsync(x => x.Code == request.Code, cancellationToken))
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

        companiesRepository.Add(company);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, ToResponse(company));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<ActionResult<CompanyResponse>> UpdateCompany(long id, UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var company = await companiesRepository.GetByIdAsync(id, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        if (await companiesRepository.Query().AnyAsync(x => x.Id != id && x.Code == request.Code, cancellationToken))
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

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(company));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<IActionResult> DeleteCompany(long id, CancellationToken cancellationToken)
    {
        var company = await companiesRepository.GetByIdAsync(id, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        company.IsDeleted = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static CompanyResponse ToResponse(Company company) =>
        new(company.Id, company.Code, company.Name, company.TaxCode, company.Address, company.Phone, company.Email, company.IsActive);
}
