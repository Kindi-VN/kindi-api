using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;

namespace Kindi.API.Application.Common.Interfaces;

public interface IAuditLogQueryService
{
    /// <summary>Log thay đổi entity. <paramref name="includeSuperAdminActors"/> = false thì ẩn hành động của SuperAdmin.</summary>
    Task<PagedList<AuditLogDto>> GetEntityLogsAsync(AuditLogQueryDto query, bool includeSuperAdminActors = false, CancellationToken cancellationToken = default);

    Task<AuditLogDto?> GetEntityLogByIdAsync(Guid id, bool includeSuperAdminActors = false, CancellationToken cancellationToken = default);

    /// <summary>Log đăng nhập/đăng ký. <paramref name="includeSuperAdminActors"/> = false thì ẩn hành động của SuperAdmin.</summary>
    Task<PagedList<AuthAuditLogDto>> GetAuthLogsAsync(AuthAuditLogQueryDto query, bool includeSuperAdminActors = false, CancellationToken cancellationToken = default);

    Task<AuthAuditLogDto?> GetAuthLogByIdAsync(Guid id, bool includeSuperAdminActors = false, CancellationToken cancellationToken = default);
}