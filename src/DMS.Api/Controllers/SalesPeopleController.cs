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
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/sales-people")]
public sealed class SalesPeopleController(
    IRepository<SalesPerson> salesPeopleRepository,
    IRepository<Company> companiesRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateSalesPersonRequest> createValidator,
    IValidator<UpdateSalesPersonRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SalesPersonResponse>>> GetSalesPeople([FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        var page = query.SafePage;
        var pageSize = query.SafePageSize;
        IQueryable<SalesPerson> salesPeopleQuery = salesPeopleRepository.Query().AsNoTracking().Include(x => x.Company);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            salesPeopleQuery = salesPeopleQuery.Where(x => x.Code.ToLower().Contains(keyword) || x.Name.ToLower().Contains(keyword));
        }

        salesPeopleQuery = query.Sort?.ToLowerInvariant() switch
        {
            "code_desc" => salesPeopleQuery.OrderByDescending(x => x.Code),
            "name" => salesPeopleQuery.OrderBy(x => x.Name),
            "name_desc" => salesPeopleQuery.OrderByDescending(x => x.Name),
            _ => salesPeopleQuery.OrderBy(x => x.Code)
        };

        var totalCount = await salesPeopleQuery.CountAsync(cancellationToken);
        var salesPeople = await salesPeopleQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SalesPersonResponse(x.Id, x.Code, x.Name, x.CompanyId, x.Company.Name, x.Phone, x.Email, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<SalesPersonResponse>(salesPeople, page, pageSize, totalCount));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<SalesPersonResponse>> GetSalesPerson(long id, CancellationToken cancellationToken)
    {
        var salesPerson = await salesPeopleRepository.Query()
            .AsNoTracking()
            .Include(x => x.Company)
            .Where(x => x.Id == id)
            .Select(x => new SalesPersonResponse(x.Id, x.Code, x.Name, x.CompanyId, x.Company.Name, x.Phone, x.Email, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return salesPerson is null ? NotFound() : Ok(salesPerson);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<ActionResult<SalesPersonResponse>> CreateSalesPerson(CreateSalesPersonRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        if (!await companiesRepository.Query().AnyAsync(x => x.Id == request.CompanyId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.CompanyId), "Company does not exist.");
            return ValidationProblem(ModelState);
        }

        if (await salesPeopleRepository.Query().AnyAsync(x => x.Code == request.Code, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.Code), "Sales person code already exists.");
            return ValidationProblem(ModelState);
        }

        var salesPerson = new SalesPerson
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            CompanyId = request.CompanyId,
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim()
        };

        salesPeopleRepository.Add(salesPerson);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await GetSalesPerson(salesPerson.Id, cancellationToken);
        return CreatedAtAction(nameof(GetSalesPerson), new { id = salesPerson.Id }, response.Value);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<ActionResult<SalesPersonResponse>> UpdateSalesPerson(long id, UpdateSalesPersonRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var salesPerson = await salesPeopleRepository.GetByIdAsync(id, cancellationToken);
        if (salesPerson is null)
        {
            return NotFound();
        }

        if (!await companiesRepository.Query().AnyAsync(x => x.Id == request.CompanyId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.CompanyId), "Company does not exist.");
            return ValidationProblem(ModelState);
        }

        if (await salesPeopleRepository.Query().AnyAsync(x => x.Id != id && x.Code == request.Code, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.Code), "Sales person code already exists.");
            return ValidationProblem(ModelState);
        }

        salesPerson.Code = request.Code.Trim();
        salesPerson.Name = request.Name.Trim();
        salesPerson.CompanyId = request.CompanyId;
        salesPerson.Phone = request.Phone?.Trim();
        salesPerson.Email = request.Email?.Trim();
        salesPerson.IsActive = request.IsActive;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await GetSalesPerson(salesPerson.Id, cancellationToken);
        return Ok(response.Value);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<IActionResult> DeleteSalesPerson(long id, CancellationToken cancellationToken)
    {
        var salesPerson = await salesPeopleRepository.GetByIdAsync(id, cancellationToken);
        if (salesPerson is null)
        {
            return NotFound();
        }

        salesPerson.IsDeleted = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
