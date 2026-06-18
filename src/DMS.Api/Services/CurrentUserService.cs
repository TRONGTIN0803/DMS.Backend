using DMS.Application.Abstractions;
using System.Security.Claims;

namespace DMS.Api.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}

