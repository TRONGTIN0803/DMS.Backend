using DMS.Api.Auth;
using DMS.Application.Catalog;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.MasterDataRead)]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/inventory-batches")]
public sealed class InventoryBatchesController(
    IInventoryBatchService inventoryBatchService,
    IValidator<CreateInventoryBatchRequest> createInboundValidator) : ControllerBase
{
    [HttpPost("inbound")]
    [Authorize(Policy = AuthorizationPolicies.InventoryWrite)]
    public async Task<ActionResult<InventoryBatchResponse>> CreateInboundBatch(CreateInventoryBatchRequest request, CancellationToken cancellationToken)
    {
        var validation = await createInboundValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await inventoryBatchService.CreateInboundBatchAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            ModelState.AddModelError(result.Error!.Code, result.Error.Message);
            return ValidationProblem(ModelState);
        }

        return Created($"/api/v1/inventory-batches/{result.Value!.Id}", result.Value);
    }
}
