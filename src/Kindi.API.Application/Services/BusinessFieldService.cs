using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.Requests;
using Kindi.API.Application.DTOs.Responses;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Kindi.API.Shared.Exceptions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Kindi.API.Application.Services;

public class BusinessFieldService : IBusinessFieldService
{
    private readonly IRepository<BusinessField> _repository;
    private readonly IRepository<Company> _companyRepository;
    private readonly IRepository<Collaborator> _collaboratorRepository;
    private readonly IRepository<Partner> _partnerRepository;
    private readonly IQueryService _queryService;
    private readonly IMapper _mapper;

    public BusinessFieldService(
        IRepository<BusinessField> repository,
        IRepository<Company> companyRepository,
        IRepository<Collaborator> collaboratorRepository,
        IRepository<Partner> partnerRepository,
        IQueryService queryService,
        IMapper mapper)
    {
        _repository = repository;
        _companyRepository = companyRepository;
        _collaboratorRepository = collaboratorRepository;
        _partnerRepository = partnerRepository;
        _queryService = queryService;
        _mapper = mapper;
    }

    public async Task<Guid> GetOrCreateBusinessFieldAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BadRequestException("Tên lĩnh vực không được để trống");

        var normalized = name.Trim().ToUpperInvariant();

        // 1. Tìm theo NormalizedName
        var existing = await _repository.GetFirstAsync(f => f.NormalizedName == normalized && !f.IsDeleted);
        if (existing != null) return existing.Id;

        // 2. Tìm theo Alias (nếu có)
        var allFields = await _repository.FindAsync(f => !f.IsDeleted);
        var matchedByAlias = allFields.FirstOrDefault(f =>
            !string.IsNullOrEmpty(f.Aliases) &&
            f.Aliases.Contains(normalized, StringComparison.OrdinalIgnoreCase));
        if (matchedByAlias != null) return matchedByAlias.Id;

        // 3. Tạo mới
        var field = new BusinessField
        {
            BusinessFieldCode = CodeGenerator.Generate("BSF"),
            Name = name.Trim(),
            NormalizedName = normalized,
            IsActive = true
        };

        await _repository.AddAsync(field);
        await _repository.SaveChangesAsync();

        return field.Id;
    }

    public async Task<List<BusinessFieldDto>> GetActiveFieldsAsync()
    {
        var fields = await _repository.FindAsync(f => f.IsActive && !f.IsDeleted);
        return _mapper.Map<List<BusinessFieldDto>>(fields);
    }

    public async Task<List<BusinessFieldAdminDto>> GetAllForAdminAsync()
    {
        var fields = (await _repository.FindAsync(f => !f.IsDeleted))
            .OrderBy(f => f.Name)
            .ToList();
        var companies = (await _companyRepository.FindAsync(c => !c.IsDeleted && c.BusinessFieldId != null)).ToList();
        var collaborators = (await _collaboratorRepository.FindAsync(c => !c.IsDeleted && c.BusinessFieldId != null)).ToList();
        var partners = (await _partnerRepository.FindAsync(p => !p.IsDeleted && p.BusinessFieldId != null)).ToList();

        return fields.Select(f => new BusinessFieldAdminDto
        {
            Id = f.Id,
            BusinessFieldCode = f.BusinessFieldCode,
            Name = f.Name,
            Aliases = f.Aliases,
            IsActive = f.IsActive,
            CompanyCount = companies.Count(c => c.BusinessFieldId == f.Id),
            UserCount = collaborators.Count(c => c.BusinessFieldId == f.Id) + partners.Count(p => p.BusinessFieldId == f.Id)
        }).ToList();
    }

    public async Task<BusinessFieldRelatedDto> GetRelatedAsync(Guid id)
    {
        var field = await _repository.GetFirstAsync(f => f.Id == id && !f.IsDeleted)
            ?? throw new NotFoundException("Không tìm thấy lĩnh vực");

        var companies = (await _companyRepository.FindAsync(c => !c.IsDeleted && c.BusinessFieldId == id))
            .OrderBy(c => c.Name)
            .ToList();
        // Phải Include User và Company: thiếu Include thì navigation null nên UserCode/FullName/Phone/CompanyName trả về null.
        var collaborators = await _collaboratorRepository.GetListWithIncludesAsync(
            query => query.Include(c => c.User).Include(c => c.Company),
            c => !c.IsDeleted && c.BusinessFieldId == id);
        var partners = await _partnerRepository.GetListWithIncludesAsync(
            query => query.Include(p => p.User).Include(p => p.Company),
            p => !p.IsDeleted && p.BusinessFieldId == id);

        var users = new List<BusinessFieldUserDto>();
        users.AddRange(collaborators.Select(c => new BusinessFieldUserDto
        {
            UserId = c.UserId,
            UserCode = c.User?.UserCode,
            FullName = c.User?.FullName,
            Phone = c.User?.Phone,
            Role = "Collaborator",
            CompanyId = c.CompanyId,
            CompanyName = c.Company?.Name
        }));
        users.AddRange(partners.Select(p => new BusinessFieldUserDto
        {
            UserId = p.UserId,
            UserCode = p.User?.UserCode,
            FullName = p.User?.FullName,
            Phone = p.User?.Phone,
            Role = "Partner",
            CompanyId = p.CompanyId,
            CompanyName = p.Company?.Name
        }));

        return new BusinessFieldRelatedDto
        {
            Id = field.Id,
            BusinessFieldCode = field.BusinessFieldCode,
            Name = field.Name,
            IsActive = field.IsActive,
            Companies = companies.Select(c => new BusinessFieldCompanyDto
            {
                Id = c.Id,
                CompanyCode = c.CompanyCode,
                Name = c.Name,
                TaxCode = c.TaxCode,
                Address = c.Address,
                CollaboratorCount = collaborators.Count(x => x.CompanyId == c.Id),
                PartnerCount = partners.Count(x => x.CompanyId == c.Id)
            }).ToList(),
            Users = users
        };
    }

    /// <summary>
    /// Công ty thuộc một lĩnh vực, phân trang phía server (màn chi tiết lĩnh vực).
    /// search lọc theo mã công ty, tên, mã số thuế.
    /// </summary>
    public async Task<PagedList<BusinessFieldCompanyDto>> GetCompaniesPagedAsync(Guid id, int page, int pageSize, string? search)
    {
        if (!await _repository.AnyAsync(f => f.Id == id && !f.IsDeleted))
            throw new NotFoundException("Không tìm thấy lĩnh vực");

        (page, pageSize) = NormalizePaging(page, pageSize);

        // Từ khoá đã bỏ dấu tiếng Việt + escape ký tự LIKE; ILIKE nên tìm không phân biệt hoa/thường.
        var searchTerm = search.RemoveVietnameseSign().ToLikeEscaped();
        var pattern = "%" + searchTerm + "%";

        var query = _queryService.GetAllNoTracking<Company>()
            .Where(c => !c.IsDeleted && c.BusinessFieldId == id);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(c =>
                (c.CompanyCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.CompanyCode), pattern, "\\")) ||
                EF.Functions.ILike(KindiDbFunctions.Unaccent(c.Name), pattern, "\\") ||
                (c.TaxCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.TaxCode), pattern, "\\")));
        }

        var paged = await query
            .OrderBy(c => c.Name)
            .Select(c => new BusinessFieldCompanyDto
            {
                Id = c.Id,
                CompanyCode = c.CompanyCode,
                Name = c.Name,
                TaxCode = c.TaxCode,
                Address = c.Address
            })
            .ToPagedListAsync(page, pageSize);

        // Đếm hồ sơ CTV/đối tác của ĐÚNG trang hiện tại bằng 2 query group-by rồi ghép trong bộ nhớ,
        // tránh N+1 (không đếm lần lượt từng công ty).
        var companyIds = paged.Items.Select(c => c.Id).ToList();
        if (companyIds.Count > 0)
        {
            var collaboratorCounts = await _queryService.GetAllNoTracking<Collaborator>()
                .Where(c => !c.IsDeleted && c.CompanyId != null && companyIds.Contains(c.CompanyId.Value))
                .GroupBy(c => c.CompanyId)
                .Select(g => new { CompanyId = g.Key, Count = g.Count() })
                .ToListAsync();

            var partnerCounts = await _queryService.GetAllNoTracking<Partner>()
                .Where(p => !p.IsDeleted && p.CompanyId != null && companyIds.Contains(p.CompanyId.Value))
                .GroupBy(p => p.CompanyId)
                .Select(g => new { CompanyId = g.Key, Count = g.Count() })
                .ToListAsync();

            var collaboratorLookup = collaboratorCounts.ToDictionary(x => x.CompanyId!.Value, x => x.Count);
            var partnerLookup = partnerCounts.ToDictionary(x => x.CompanyId!.Value, x => x.Count);

            foreach (var item in paged.Items)
            {
                item.CollaboratorCount = collaboratorLookup.TryGetValue(item.Id, out var cc) ? cc : 0;
                item.PartnerCount = partnerLookup.TryGetValue(item.Id, out var pc) ? pc : 0;
            }
        }

        return paged;
    }

    /// <summary>
    /// Tài khoản thuộc một lĩnh vực (gồm cả hồ sơ CTV lẫn đối tác), phân trang phía server.
    /// search lọc theo mã tài khoản, họ tên, SĐT, tên công ty; role lọc theo "Collaborator"/"Partner".
    /// </summary>
    public async Task<PagedList<BusinessFieldUserDto>> GetUsersPagedAsync(Guid id, int page, int pageSize, string? search, string? role)
    {
        if (!await _repository.AnyAsync(f => f.Id == id && !f.IsDeleted))
            throw new NotFoundException("Không tìm thấy lĩnh vực");

        (page, pageSize) = NormalizePaging(page, pageSize);

        var searchTerm = search.RemoveVietnameseSign().ToLikeEscaped();
        var pattern = "%" + searchTerm + "%";
        var hasSearch = !string.IsNullOrEmpty(searchTerm);

        // role chỉ nhận Collaborator/Partner; bỏ trống hoặc giá trị khác thì lấy cả hai loại hồ sơ.
        var roleFilter = role?.Trim();
        var includeCollaborator = !roleFilter.EqualsIgnoreCase("Partner");
        var includePartner = !roleFilter.EqualsIgnoreCase("Collaborator");

        IQueryable<BusinessFieldUserDto>? query = null;

        if (includeCollaborator)
        {
            var collaboratorQuery = _queryService.GetAllNoTracking<Collaborator>()
                .Where(c => !c.IsDeleted && c.BusinessFieldId == id);

            if (hasSearch)
            {
                collaboratorQuery = collaboratorQuery.Where(c =>
                    (c.User.UserCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.User.UserCode), pattern, "\\")) ||
                    EF.Functions.ILike(KindiDbFunctions.Unaccent(c.User.FullName), pattern, "\\") ||
                    (c.User.Phone != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.User.Phone), pattern, "\\")) ||
                    (c.Company != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(c.Company.Name), pattern, "\\")));
            }

            // Chiếu thẳng trong câu truy vấn: EF tự JOIN Users/Companies nên UserCode/FullName/Phone/CompanyName
            // luôn có giá trị (tương đương Include ở GetRelatedAsync, nhưng lọc + phân trang chạy dưới DB).
            query = collaboratorQuery.Select(c => new BusinessFieldUserDto
            {
                UserId = c.UserId,
                UserCode = c.User.UserCode,
                FullName = c.User.FullName,
                Phone = c.User.Phone,
                Role = "Collaborator",
                CompanyId = c.CompanyId,
                CompanyName = c.Company != null ? c.Company.Name : null
            });
        }

        if (includePartner)
        {
            var partnerQuery = _queryService.GetAllNoTracking<Partner>()
                .Where(p => !p.IsDeleted && p.BusinessFieldId == id);

            if (hasSearch)
            {
                partnerQuery = partnerQuery.Where(p =>
                    (p.User.UserCode != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(p.User.UserCode), pattern, "\\")) ||
                    EF.Functions.ILike(KindiDbFunctions.Unaccent(p.User.FullName), pattern, "\\") ||
                    (p.User.Phone != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(p.User.Phone), pattern, "\\")) ||
                    (p.Company != null && EF.Functions.ILike(KindiDbFunctions.Unaccent(p.Company.Name), pattern, "\\")));
            }

            var partnerProjection = partnerQuery.Select(p => new BusinessFieldUserDto
            {
                UserId = p.UserId,
                UserCode = p.User.UserCode,
                FullName = p.User.FullName,
                Phone = p.User.Phone,
                Role = "Partner",
                CompanyId = p.CompanyId,
                CompanyName = p.Company != null ? p.Company.Name : null
            });

            // Gộp 2 nguồn thành MỘT truy vấn (UNION ALL) để phân trang đúng trên toàn bộ danh sách.
            query = query == null ? partnerProjection : query.Concat(partnerProjection);
        }

        // Không có nguồn nào được chọn (không xảy ra với role hợp lệ) — trả trang rỗng an toàn.
        if (query == null)
            return new PagedList<BusinessFieldUserDto>(new List<BusinessFieldUserDto>(), 0, page, pageSize);

        return await query
            .OrderBy(x => x.FullName)
            .ThenBy(x => x.UserId)
            .ToPagedListAsync(page, pageSize);
    }

    /// <summary>Chuẩn hoá tham số phân trang: page >= 1, pageSize mặc định 10 và tối đa 100 (như các list khác).</summary>
    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;
        return (page, pageSize);
    }

    public async Task<BusinessFieldAdminDto> CreateAsync(CreateBusinessFieldRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new BadRequestException("Tên lĩnh vực không được để trống");

        var normalized = name.ToUpperInvariant();
        if (await _repository.AnyAsync(f => f.NormalizedName == normalized && !f.IsDeleted))
            throw new BadRequestException("Lĩnh vực này đã tồn tại");

        var field = new BusinessField
        {
            BusinessFieldCode = CodeGenerator.Generate("BSF"),
            Name = name,
            NormalizedName = normalized,
            Aliases = string.IsNullOrWhiteSpace(request.Aliases) ? null : request.Aliases.Trim(),
            IsActive = true
        };

        await _repository.AddAsync(field);
        await _repository.SaveChangesAsync();

        return new BusinessFieldAdminDto
        {
            Id = field.Id,
            BusinessFieldCode = field.BusinessFieldCode,
            Name = field.Name,
            Aliases = field.Aliases,
            IsActive = field.IsActive
        };
    }

    public async Task<BusinessFieldAdminDto> UpdateAsync(Guid id, UpdateBusinessFieldRequest request)
    {
        var field = await _repository.GetFirstAsync(f => f.Id == id && !f.IsDeleted)
            ?? throw new NotFoundException("Không tìm thấy lĩnh vực");

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new BadRequestException("Tên lĩnh vực không được để trống");

        var normalized = name.ToUpperInvariant();
        if (await _repository.AnyAsync(f => f.Id != id && f.NormalizedName == normalized && !f.IsDeleted))
            throw new BadRequestException("Lĩnh vực này đã tồn tại");

        field.Name = name;
        field.NormalizedName = normalized;
        field.Aliases = string.IsNullOrWhiteSpace(request.Aliases) ? null : request.Aliases.Trim();
        field.IsActive = request.IsActive;

        _repository.Update(field);
        await _repository.SaveChangesAsync();

        return new BusinessFieldAdminDto
        {
            Id = field.Id,
            BusinessFieldCode = field.BusinessFieldCode,
            Name = field.Name,
            Aliases = field.Aliases,
            IsActive = field.IsActive
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var field = await _repository.GetFirstAsync(f => f.Id == id && !f.IsDeleted)
            ?? throw new NotFoundException("Không tìm thấy lĩnh vực");

        // Giữ toàn vẹn dữ liệu: chỉ xoá khi không còn công ty/hồ sơ nào dùng lĩnh vực này.
        var companyCount = await _companyRepository.CountAsync(c => !c.IsDeleted && c.BusinessFieldId == id);
        var collaboratorCount = await _collaboratorRepository.CountAsync(c => !c.IsDeleted && c.BusinessFieldId == id);
        var partnerCount = await _partnerRepository.CountAsync(p => !p.IsDeleted && p.BusinessFieldId == id);
        var total = companyCount + collaboratorCount + partnerCount;
        if (total > 0)
            throw new BadRequestException(
                $"Không thể xoá: lĩnh vực đang được {companyCount} công ty, {collaboratorCount} hồ sơ cộng tác viên và {partnerCount} hồ sơ đối tác sử dụng");

        _repository.Delete(field);
        await _repository.SaveChangesAsync();
    }
}
