namespace Kindi.API.Application.Common.Interfaces;

using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;

/// <summary>Cấu hình chung của hệ thống.</summary>
public interface ISystemSettingService
{
    /// <summary>Đọc cấu hình chung (trả giá trị mặc định khi chưa cấu hình lần nào).</summary>
    Task<SystemSettingResponse> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Đọc phần cấu hình công khai cho giao diện người dùng.</summary>
    Task<PublicSystemSettingResponse> GetPublicAsync(CancellationToken cancellationToken = default);

    /// <summary>Cập nhật cấu hình chung.</summary>
    Task<SystemSettingResponse> SaveAsync(SaveSystemSettingRequest request, CancellationToken cancellationToken = default);
}
