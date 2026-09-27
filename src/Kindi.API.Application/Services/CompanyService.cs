using AutoMapper;
using Kindi.API.Application.Common.Extensions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.Requests;
using Kindi.API.Application.DTOs.Responses;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Kindi.API.Application.Services;

public class CompanyService : ICompanyService
{
    private readonly IRepository<Company> _companyRepo;
    private readonly IRepository<BusinessField> _businessFieldRepo;
    private readonly IMapper _mapper;

    public CompanyService(
        IRepository<Company> companyRepo,
        IRepository<BusinessField> businessFieldRepo,
        IMapper mapper)
    {
        _companyRepo = companyRepo;
        _businessFieldRepo = businessFieldRepo;
        _mapper = mapper;
    }

    public async Task<Company?> AddOrUpdateFromLegacyAsync(
        string? name,
        string? taxCode,
        string? address,
        string? website,
        Guid? businessFieldId = null,
        BusinessType? businessType = null,
        CompanySize? companySize = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var validBusinessFieldId = await ResolveBusinessFieldIdAsync(businessFieldId);

        var nameTrim = name!.Trim();
        Company? existing = null;

        if (!string.IsNullOrWhiteSpace(taxCode))
        {
            existing = await _companyRepo.GetFirstAsync(c => c.TaxCode == taxCode && !c.IsDeleted);
        }

        if (existing == null)
        {
            // So khớp tên công ty không phân biệt hoa/thường bằng ILIKE: mẫu là tên đã escape,
            // không thêm % nên chỉ khớp khi bằng nhau toàn bộ.
            var nameLike = nameTrim.RemoveVietnameseSign().ToLikeEscaped();
            existing = await _companyRepo.GetFirstAsync(c => EF.Functions.ILike(KindiDbFunctions.Unaccent(c.Name), nameLike, "\\") && !c.IsDeleted);
        }

        if (existing != null)
        {
            var updated = false;
            if (!string.IsNullOrWhiteSpace(address) && existing.Address != address)
            {
                existing.Address = address;
                updated = true;
            }
            if (!string.IsNullOrWhiteSpace(website) && existing.Website != website)
            {
                existing.Website = website;
                updated = true;
            }
            if (!string.IsNullOrWhiteSpace(taxCode) && existing.TaxCode != taxCode)
            {
                existing.TaxCode = taxCode;
                updated = true;
            }
            if (businessFieldId.HasValue && existing.BusinessFieldId != validBusinessFieldId)
            {
                existing.BusinessFieldId = validBusinessFieldId;
                updated = true;
            }
            if (businessType.HasValue && existing.BusinessType != businessType)
            {
                existing.BusinessType = businessType;
                updated = true;
            }
            if (companySize.HasValue && existing.CompanySize != companySize)
            {
                existing.CompanySize = companySize;
                updated = true;
            }

            if (updated)
            {
                _companyRepo.Update(existing);
                await _companyRepo.SaveChangesAsync();
            }

            return existing;
        }

        var company = new Company
        {
            CompanyCode = CodeGenerator.Generate("CMP"),
            Name = nameTrim,
            TaxCode = string.IsNullOrWhiteSpace(taxCode) ? null : taxCode,
            Address = address,
            Website = website,
            BusinessFieldId = validBusinessFieldId,
            BusinessType = businessType,
            CompanySize = companySize
        };

        await _companyRepo.AddAsync(company);
        await _companyRepo.SaveChangesAsync();

        return company;
    }

    public async Task<CompanyResponseDto> CreateAsync(CreateCompanyDto request)
    {
        var validBusinessFieldId = await ResolveBusinessFieldIdAsync(request.BusinessFieldId);
        var company = new Company
        {
            CompanyCode = CodeGenerator.Generate("CMP"),
            Name = request.Name.Trim(),
            TaxCode = request.TaxCode,
            Address = request.Address,
            Website = request.Website,
            BusinessFieldId = validBusinessFieldId,
            BusinessType = request.BusinessType,
            CompanySize = request.CompanySize
        };

        await _companyRepo.AddAsync(company);
        await _companyRepo.SaveChangesAsync();

        return _mapper.Map<CompanyResponseDto>(company);
    }

    public async Task<CompanyResponseDto> UpdateAsync(Guid id, UpdateCompanyDto request)
    {
        var validBusinessFieldId = await ResolveBusinessFieldIdAsync(request.BusinessFieldId);

        var company = await _companyRepo.GetByIdAsync(id);
        if (company == null)
            throw new KeyNotFoundException("Company not found");

        if (!string.IsNullOrWhiteSpace(request.Name)) company.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.TaxCode)) company.TaxCode = request.TaxCode;
        if (request.Address != null) company.Address = request.Address;
        if (request.Website != null) company.Website = request.Website;
        if (request.BusinessFieldId.HasValue) company.BusinessFieldId = validBusinessFieldId;
        if (request.BusinessType.HasValue) company.BusinessType = request.BusinessType;
        if (request.CompanySize.HasValue) company.CompanySize = request.CompanySize;

        _companyRepo.Update(company);
        await _companyRepo.SaveChangesAsync();

        return _mapper.Map<CompanyResponseDto>(company);
    }

    public async Task<PagedList<CompanyResponseDto>> GetPagedAsync(int pageNumber, int pageSize, string? search = null)
    {
        var q = await _companyRepo.GetQueryableAsync();
        var queryable = q.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            queryable = queryable.Where(c => c.Name.Contains(s) || (c.TaxCode != null && c.TaxCode.Contains(s)));
        }

        var paged = await _companyRepo.GetPagedWithIncludesAsync(pageNumber, pageSize, includes: null, predicate: null);
        return _mapper.Map<PagedList<CompanyResponseDto>>(paged);
    }

    public async Task<CompanyResponseDto?> GetByIdAsync(Guid id)
    {
        var company = await _companyRepo.GetByIdAsync(id);
        if (company == null) return null;
        return _mapper.Map<CompanyResponseDto>(company);
    }

    /// <summary>
    /// Kiểm tra BusinessFieldId có tồn tại (chưa xóa mềm) không.
    /// Trả về Id hợp lệ, hoặc null nếu không tồn tại.
    /// </summary>
    private async Task<Guid?> ResolveBusinessFieldIdAsync(Guid? businessFieldId)
    {
        if (!businessFieldId.HasValue)
            return null;

        var exists = await _businessFieldRepo.AnyAsync(
            b => b.Id == businessFieldId.Value && !b.IsDeleted);

        return exists ? businessFieldId.Value : null;
    }
}
