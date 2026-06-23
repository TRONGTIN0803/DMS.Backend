using DMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace DMS.Api.Auth;

public static class AuthorizationPolicies
{
    public const string MasterDataRead = nameof(MasterDataRead);
    public const string MasterDataWrite = nameof(MasterDataWrite);
    public const string InventoryWrite = nameof(InventoryWrite);

    public static void AddDmsPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(MasterDataRead, policy =>
            policy.RequireRole(UserRoles.Admin, UserRoles.Sales, UserRoles.Warehouse));

        options.AddPolicy(MasterDataWrite, policy =>
            policy.RequireRole(UserRoles.Admin));

        options.AddPolicy(InventoryWrite, policy =>
            policy.RequireRole(UserRoles.Admin, UserRoles.Warehouse));
    }
}
