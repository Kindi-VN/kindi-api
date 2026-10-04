namespace Kindi.API.WebApi.Authorization;

using Kindi.API.Domain.Enums;
using Kindi.API.Shared.Constants;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Chặn endpoint theo quyền P###. Truyền nhiều mã nghĩa là "có MỘT trong các quyền đó" là được.
/// SuperAdmin luôn qua (handler bypass). Mọi endpoint không AllowAnonymous phải có quyền — có test
/// tự động quét lại (tests/Kindi.API.ArchitectureTests).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(params PermissionCode[] permissions)
        : base(PolicyConstants.PermissionPrefix + string.Join(",", permissions.Select(p => p.ToCode())))
    {
        PermissionCodes = permissions.Select(p => p.ToCode()).ToList();
    }

    /// <summary>Các mã P### mà endpoint chấp nhận; có một mã là qua.</summary>
    public IReadOnlyList<string> PermissionCodes { get; }
}
