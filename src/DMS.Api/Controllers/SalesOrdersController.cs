using DMS.Api.Auth;
using DMS.Api.Jobs;
using DMS.Application.Orders;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.MasterDataRead)]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/sales-orders")]
public sealed class SalesOrdersController(ISalesOrderService salesOrderService, IServiceProvider serviceProvider) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<ActionResult<SalesOrderResponse>> CreateDraft(CreateSalesOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await salesOrderService.CreateDraftAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            ModelState.AddModelError(result.Error!.Code, result.Error.Message);
            return ValidationProblem(ModelState);
        }

        return Created($"/api/v1/sales-orders/{result.Value!.Id}", result.Value);
    }

    [HttpPost("{id:long}/submit")]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<ActionResult<SalesOrderResponse>> Submit(long id, CancellationToken cancellationToken)
    {
        var result = await salesOrderService.SubmitAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            ModelState.AddModelError(result.Error!.Code, result.Error.Message);
            return ValidationProblem(ModelState);
        }

        var backgroundJobClient = serviceProvider.GetService<IBackgroundJobClient>();
        backgroundJobClient?.Enqueue<OrderEmailJob>(job => job.SendOrderApprovedEmailAsync(result.Value!.Id, CancellationToken.None));

        return Ok(result.Value);
    }

    [HttpPost("{id:long}/approve")]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<ActionResult<SalesOrderResponse>> Approve(long id, CancellationToken cancellationToken)
    {
        var result = await salesOrderService.ApproveAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            ModelState.AddModelError(result.Error!.Code, result.Error.Message);
            return ValidationProblem(ModelState);
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:long}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.MasterDataWrite)]
    public async Task<ActionResult<SalesOrderResponse>> Cancel(long id, CancellationToken cancellationToken)
    {
        var result = await salesOrderService.CancelAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            ModelState.AddModelError(result.Error!.Code, result.Error.Message);
            return ValidationProblem(ModelState);
        }

        return Ok(result.Value);
    }
}
