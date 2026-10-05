using Kindi.API.Application.DTOs.Requests;
using Kindi.API.Application.DTOs.Responses;

namespace Kindi.API.Application.Common.Interfaces;

public interface IBusinessFieldService
{
    Task<Guid> GetOrCreateBusinessFieldAsync(string name);
    Task<List<BusinessFieldDto>> GetActiveFieldsAsync();

    /// <summary>Toàn bộ lĩnh vực (kể cả đang tắt) kèm số công ty/tài khoản — màn quản trị.</summary>
    Task<List<BusinessFieldAdminDto>> GetAllForAdminAsync();

    /// <summary>Công ty và tài khoản thuộc một lĩnh vực.</summary>
    Task<BusinessFieldRelatedDto> GetRelatedAsync(Guid id);

    Task<BusinessFieldAdminDto> CreateAsync(CreateBusinessFieldRequest request);

    Task<BusinessFieldAdminDto> UpdateAsync(Guid id, UpdateBusinessFieldRequest request);

    /// <summary>Xoá mềm. Chặn nếu lĩnh vực đang được công ty/hồ sơ sử dụng.</summary>
    Task DeleteAsync(Guid id);
}