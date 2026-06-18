using DMS.Application.Catalog;
using DMS.Domain.Entities;
using DMS.Infrastructure.Persistence;
using DMS.Shared;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
public sealed class CustomersController(
    ApplicationDbContext dbContext,
    IValidator<CreateCustomerRequest> createValidator,
    IValidator<UpdateCustomerRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerResponse>>> GetCustomers([FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        var page = query.SafePage;
        var pageSize = query.SafePageSize;
        IQueryable<Customer> customersQuery = dbContext.Customers.AsNoTracking().Include(x => x.Company).Include(x => x.SalesPerson);

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
        var customer = await dbContext.Customers
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

        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
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

        if (await dbContext.Customers.AnyAsync(x => x.Code == request.Code, cancellationToken))
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

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetCustomer(customer.Id, cancellationToken);
        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, response.Value);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CustomerResponse>> UpdateCustomer(long id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var customer = await dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
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

        if (await dbContext.Customers.AnyAsync(x => x.Id != id && x.Code == request.Code, cancellationToken))
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

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetCustomer(customer.Id, cancellationToken);
        return Ok(response.Value);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteCustomer(long id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        customer.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<(string Field, string Message)?> ValidateRelationshipsAsync(long companyId, long? salesPersonId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Companies.AnyAsync(x => x.Id == companyId, cancellationToken))
        {
            return (nameof(companyId), "Company does not exist.");
        }

        if (salesPersonId is not null && !await dbContext.SalesPeople.AnyAsync(x => x.Id == salesPersonId, cancellationToken))
        {
            return (nameof(salesPersonId), "Sales person does not exist.");
        }

        return null;
    }
}
