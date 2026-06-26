using DMS.Api.Auth;
using DMS.Application.Catalog;
using DMS.Application.Abstractions;
using DMS.Domain.Entities;
using DMS.Shared;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace DMS.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.MasterDataRead)]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/customers")]
public sealed class CustomersController(
    IRepository<Customer> customersRepository,
    IRepository<Company> companiesRepository,
    IRepository<SalesPerson> salesPeopleRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateCustomerRequest> createValidator,
    IValidator<UpdateCustomerRequest> updateValidator,
    IDistributedCache cache) : ControllerBase
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerResponse>>> GetCustomers([FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        var page = query.SafePage;
        var pageSize = query.SafePageSize;
        IQueryable<Customer> customersQuery = customersRepository.Query().AsNoTracking().Include(x => x.Company).Include(x => x.SalesPerson);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            customersQuery = customersQuery.Where(x => x.Code.ToLower().Contains(keyword) || x.Name.ToLower().Contains(keyword));
        }

        customersQuery = query.Sort?.ToLowerInvariant() switch
        {
            "code_desc" => customersQuery.OrderByDescending(x => x.Code),
            "name" => customersQuery.OrderBy(x => x.Name),
            "name_desc" => customersQuery.OrderByDescending(x => x.Name),
            _ => customersQuery.OrderBy(x => x.Code)
        };

        var totalCount = await customersQuery.CountAsync(cancellationToken);
        var customers = await customersQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CustomerResponse(
                x.Id,
                x.Code,
                x.Name,
                x.CompanyId,
                x.Company.Name,
                x.SalesPersonId,
                x.SalesPerson == null ? null : x.SalesPerson.Name,
                x.Address,
                x.Phone,
                x.CustomerType,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<CustomerResponse>(customers, page, pageSize, totalCount));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CustomerResponse>> GetCustomer(long id, CancellationToken cancellationToken)
    {
        var cacheKey = MasterDataCacheKeys.Customer(id);
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Ok(JsonSerializer.Deserialize<CustomerResponse>(cached, SerializerOptions));
        }

        var customer = await customersRepository.Query()
            .AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.SalesPerson)
            .Where(x => x.Id == id)
            .Select(x => new CustomerResponse(
                x.Id,
                x.Code,
                x.Name,
                x.CompanyId,
                x.Company.Name,
                x.SalesPersonId,
                x.SalesPerson == null ? null : x.SalesPerson.Name,
                x.Address,
                x.Phone,
                x.CustomerType,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        if (customer is null)
        {
            return NotFound();
        }

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(customer, SerializerOptions), CacheOptions, cancellationToken);
        return Ok(customer);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var relationshipError = await ValidateRelationshipsAsync(request.CompanyId, request.SalesPersonId, cancellationToken);
        if (relationshipError is not null)
        {
            ModelState.AddModelError(relationshipError.Value.Field, relationshipError.Value.Message);
            return ValidationProblem(ModelState);
        }

        if (await customersRepository.Query().AnyAsync(x => x.Code == request.Code, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.Code), "Customer code already exists.");
            return ValidationProblem(ModelState);
        }

        var customer = new Customer
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            CompanyId = request.CompanyId,
            SalesPersonId = request.SalesPersonId,
            Address = request.Address?.Trim(),
            Phone = request.Phone?.Trim(),
            CustomerType = request.CustomerType
        };

        customersRepository.Add(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(MasterDataCacheKeys.Customer(customer.Id), cancellationToken);
        var response = await GetCustomer(customer.Id, cancellationToken);
        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, response.Value);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<ActionResult<CustomerResponse>> UpdateCustomer(long id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var customer = await customersRepository.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        var relationshipError = await ValidateRelationshipsAsync(request.CompanyId, request.SalesPersonId, cancellationToken);
        if (relationshipError is not null)
        {
            ModelState.AddModelError(relationshipError.Value.Field, relationshipError.Value.Message);
            return ValidationProblem(ModelState);
        }

        if (await customersRepository.Query().AnyAsync(x => x.Id != id && x.Code == request.Code, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.Code), "Customer code already exists.");
            return ValidationProblem(ModelState);
        }

        customer.Code = request.Code.Trim();
        customer.Name = request.Name.Trim();
        customer.CompanyId = request.CompanyId;
        customer.SalesPersonId = request.SalesPersonId;
        customer.Address = request.Address?.Trim();
        customer.Phone = request.Phone?.Trim();
        customer.CustomerType = request.CustomerType;
        customer.IsActive = request.IsActive;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(MasterDataCacheKeys.Customer(id), cancellationToken);
        var response = await GetCustomer(customer.Id, cancellationToken);
        return Ok(response.Value);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<IActionResult> DeleteCustomer(long id, CancellationToken cancellationToken)
    {
        var customer = await customersRepository.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        customer.IsDeleted = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(MasterDataCacheKeys.Customer(id), cancellationToken);
        return NoContent();
    }

    private async Task<(string Field, string Message)?> ValidateRelationshipsAsync(long companyId, long? salesPersonId, CancellationToken cancellationToken)
    {
        if (!await companiesRepository.Query().AnyAsync(x => x.Id == companyId, cancellationToken))
        {
            return (nameof(companyId), "Company does not exist.");
        }

        if (salesPersonId is not null && !await salesPeopleRepository.Query().AnyAsync(x => x.Id == salesPersonId, cancellationToken))
        {
            return (nameof(salesPersonId), "Sales person does not exist.");
        }

        return null;
    }
}
