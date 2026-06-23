using DMS.Api.Auth;
using DMS.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.MasterDataRead)]
[Route("api/v1/sales-orders")]
public sealed class SalesOrdersController(ISalesOrderService salesOrderService) : ControllerBase
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
}
