using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[ApiController]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/system")]
public sealed class SystemInfoController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("version")]
    public IActionResult GetVersion() => Ok(new
    {
        apiVersion = "v2",
        name = "DMS API",
        status = "Ready"
    });
}
