namespace Kindi.API.WebApi.Authorization;

using Kindi.API.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

/// <summary>
/// Sinh policy động cho mọi mã quyền (tên policy dạng <c>permission:P020</c> hoặc nhiều mã cách
/// nhau bằng dấu phẩy) — không phải khai báo từng policy một.
/// </summary>
public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var existing = await base.GetPolicyAsync(policyName);
        if (existing != null)
        {
            return existing;
        }

        if (policyName.StartsWith(PolicyConstants.PermissionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var codes = policyName[PolicyConstants.PermissionPrefix.Length..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (codes.Count > 0)
            {
                return new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(codes))
                    .Build();
            }
        }

        return null;
    }
}
