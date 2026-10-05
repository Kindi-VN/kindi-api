using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.Requests;
using Kindi.API.Application.DTOs.Responses;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Shared.Exceptions;
using AutoMapper;

namespace Kindi.API.Application.Services;

public class BusinessFieldService : IBusinessFieldService
{
    private readonly IRepository<BusinessField> _repository;
    private readonly IRepository<Company> _companyRepository;
    private readonly IRepository<Collaborator> _collaboratorRepository;
    private readonly IRepository<Partner> _partnerRepository;
    private readonly IMapper _mapper;

    public BusinessFieldService(
        IRepository<BusinessField> repository,
        IRepository<Company> companyRepository,
        IRepository<Collaborator> collaboratorRepository,
        IRepository<Partner> partnerRepository,
        IMapper mapper)
    {
        _repository = repository;
        _companyRepository = companyRepository;
        _collaboratorRepository = collaboratorRepository;
        _partnerRepository = partnerRepository;
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
        var collaborators = await _collaboratorRepository.FindAsync(c => !c.IsDeleted && c.BusinessFieldId == id);
        var partners = await _partnerRepository.FindAsync(p => !p.IsDeleted && p.BusinessFieldId == id);

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
