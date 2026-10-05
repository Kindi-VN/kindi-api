using Kindi.API.Application.DTOs.Requests;
using Kindi.API.Application.DTOs.Responses;
using Kindi.API.Domain.Models;

namespace Kindi.API.Application.Common.Interfaces;

public interface IBusinessFieldService
{
    Task<Guid> GetOrCreateBusinessFieldAsync(string name);
    Task<List<BusinessFieldDto>> GetActiveFieldsAsync();

    /// <summary>Toàn bộ lĩnh vực (kể cả đang tắt) kèm số công ty/tài khoản — màn quản trị.</summary>
    Task<List<BusinessFieldAdminDto>> GetAllForAdminAsync();

    /// <summary>Công ty và tài khoản thuộc một lĩnh vực.</summary>
    Task<BusinessFieldRelatedDto> GetRelatedAsync(Guid id);

    /// <summary>Công ty thuộc một lĩnh vực — phân trang phía server (search theo mã công ty/tên/mã số thuế).</summary>
    Task<PagedList<BusinessFieldCompanyDto>> GetCompaniesPagedAsync(Guid id, int page, int pageSize, string? search);

    /// <summary>Tài khoản thuộc một lĩnh vực — phân trang phía server (search theo mã TK/họ tên/SĐT/tên công ty), role lọc "Collaborator"/"Partner".</summary>
    Task<PagedList<BusinessFieldUserDto>> GetUsersPagedAsync(Guid id, int page, int pageSize, string? search, string? role);

    Task<BusinessFieldAdminDto> CreateAsync(CreateBusinessFieldRequest request);

    Task<BusinessFieldAdminDto> UpdateAsync(Guid id, UpdateBusinessFieldRequest request);

    /// <summary>Xoá mềm. Chặn nếu lĩnh vực đang được công ty/hồ sơ sử dụng.</summary>
    Task DeleteAsync(Guid id);
}