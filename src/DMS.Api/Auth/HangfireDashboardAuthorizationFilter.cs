using DMS.Domain.Enums;
using Hangfire.Dashboard;

namespace DMS.Api.Auth;

public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true &&
            httpContext.User.IsInRole(UserRoles.Admin);
    }
}
