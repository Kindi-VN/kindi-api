namespace Kindi.API.Application.Services;

using Kindi.API.Application.Common.Exceptions;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Domain.Rules;
using Kindi.API.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

/// <summary>
/// Cấu hình loại chi phí dùng chung khi khai doanh thu. Mỗi loại có thể gắn cho một số loại giao dịch;
/// loại không gắn gì là loại mặc định. Scope của một loại được thay nguyên cụm mỗi lần lưu nên không
/// bao giờ lộn giữa các loại giao dịch.
/// </summary>
public sealed class RevenueExpenseTypeService : IRevenueExpenseTypeService
{
    private readonly IRepository<RevenueExpenseType> _typeRepository;
    private readonly IRepository<RevenueExpenseTypeScope> _scopeRepository;
    private readonly IRepository<SystemSetting> _settingRepository;
    private readonly IQueryService _queryService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<RevenueExpenseTypeService> _logger;

    public RevenueExpenseTypeService(
        IRepository<RevenueExpenseType> typeRepository,
        IRepository<RevenueExpenseTypeScope> scopeRepository,
        IRepository<SystemSetting> settingRepository,
        IQueryService queryService,
        IStringLocalizer<SharedResource> localizer,
        ILogger<RevenueExpenseTypeService> logger)
    {
        _typeRepository = typeRepository;
        _scopeRepository = scopeRepository;
        _settingRepository = settingRepository;
        _queryService = queryService;
        _localizer = localizer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<RevenueExpenseTypeResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _queryService.GetAllNoTracking<RevenueExpenseType>()
            .Include(t => t.Scopes)
            .ToListAsync(cancellationToken);

        return items
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(Map)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<RevenueExpenseTypeResponse> CreateAsync(SaveRevenueExpenseTypeRequest request, CancellationToken cancellationToken = default)
    {
        var name = CleanName(request.Name);
        var desired = NormalizeTypes(request.TransactionTypes);

        var entity = new RevenueExpenseType { Name = name, SortOrder = request.SortOrder };
        foreach (var type in desired)
            entity.Scopes.Add(new RevenueExpenseTypeScope { RevenueExpenseTypeId = entity.Id, TransactionType = type });

        await _typeRepository.AddAsync(entity, cancellationToken);
        _logger.LogInformation("Đã thêm loại chi phí doanh thu {Name}", entity.Name);

        return await BuildAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RevenueExpenseTypeResponse> UpdateAsync(Guid id, SaveRevenueExpenseTypeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _typeRepository.GetFirstWithIncludesAsync(
            t => t.Id == id,
            q => q.Include(t => t.Scopes),
            cancellationToken) ?? throw new NotFoundException(_localizer["RevenueExpenseType_NotFound"]);

        var name = CleanName(request.Name);
        var desired = NormalizeTypes(request.TransactionTypes);

        entity.Name = name;
        entity.SortOrder = request.SortOrder;

        // Lấy cả scope đã xoá mềm để lần gán lại cùng loại giao dịch chỉ cần khôi phục, không tạo dòng trùng.
        var existing = await _scopeRepository.GetQueryable()
            .IgnoreQueryFilters()
            .Where(s => s.RevenueExpenseTypeId == id)
            .ToListAsync(cancellationToken);

        ReconcileScopes(entity, existing, desired);

        await _typeRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã cập nhật loại chi phí doanh thu {Name}", entity.Name);

        return await BuildAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _typeRepository.GetFirstWithIncludesAsync(
            t => t.Id == id,
            q => q.Include(t => t.Scopes),
            cancellationToken) ?? throw new NotFoundException(_localizer["RevenueExpenseType_NotFound"]);

        entity.IsDeleted = true;
        foreach (var scope in entity.Scopes)
            scope.IsDeleted = true;

        await _typeRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã xoá loại chi phí doanh thu {Name}", entity.Name);
    }

    /// <inheritdoc />
    public async Task AssignAsync(AssignRevenueExpenseTypeScopesRequest request, CancellationToken cancellationToken = default)
    {
        var ids = request.ExpenseTypeIds.Distinct().ToList();
        if (ids.Count == 0)
            throw new BusinessException(_localizer["RevenueExpenseType_NoTarget"]);

        var desired = NormalizeTypes(request.TransactionTypes);

        var types = await _typeRepository.GetQueryable()
            .Include(t => t.Scopes)
            .Where(t => ids.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (types.Count != ids.Count)
            throw new NotFoundException(_localizer["RevenueExpenseType_NotFound"]);

        var existing = await _scopeRepository.GetQueryable()
            .IgnoreQueryFilters()
            .Where(s => ids.Contains(s.RevenueExpenseTypeId))
            .ToListAsync(cancellationToken);

        foreach (var type in types)
            ReconcileScopes(type, existing.Where(s => s.RevenueExpenseTypeId == type.Id).ToList(), desired);

        await _typeRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã gán loại giao dịch cho {Count} loại chi phí doanh thu", types.Count);
    }

    /// <inheritdoc />
    public async Task<List<RevenueExpenseTypeResolveResponse>> ResolveAsync(TransactionType type, CancellationToken cancellationToken = default)
    {
        var all = await _queryService.GetAllNoTracking<RevenueExpenseType>()
            .Include(t => t.Scopes)
            .ToListAsync(cancellationToken);

        return RevenueExpenseRule.Resolve(all, type)
            .Select(t => new RevenueExpenseTypeResolveResponse { Id = t.Id, Name = t.Name })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<RevenueExpenseConfigResponse> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _settingRepository.GetFirstAsync(x => !x.IsDeleted, cancellationToken) ?? new SystemSetting();
        return new RevenueExpenseConfigResponse
        {
            RevenueTaxPercent = setting.RevenueTaxPercent,
            RevenueTaxIncluded = setting.RevenueTaxIncluded
        };
    }

    /// <inheritdoc />
    public async Task<RevenueExpenseConfigResponse> SaveConfigAsync(RevenueExpenseConfigRequest request, CancellationToken cancellationToken = default)
    {
        if (request.RevenueTaxPercent is < 0 or > 100)
            throw new BusinessException(_localizer["Revenue_RateInvalid"]);

        var setting = await _settingRepository.GetQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => !x.IsDeleted, cancellationToken);

        if (setting is null)
        {
            setting = new SystemSetting
            {
                RevenueTaxPercent = request.RevenueTaxPercent,
                RevenueTaxIncluded = request.RevenueTaxIncluded
            };
            await _settingRepository.AddAsync(setting, cancellationToken);
        }
        else
        {
            // Chỉ chạm hai cột cấu hình doanh thu, giữ nguyên phần còn lại của cài đặt chung.
            setting.RevenueTaxPercent = request.RevenueTaxPercent;
            setting.RevenueTaxIncluded = request.RevenueTaxIncluded;
            _settingRepository.Update(setting);
            await _settingRepository.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Đã cập nhật cấu hình thuế doanh thu: {Percent}%", request.RevenueTaxPercent);

        return new RevenueExpenseConfigResponse
        {
            RevenueTaxPercent = setting.RevenueTaxPercent,
            RevenueTaxIncluded = setting.RevenueTaxIncluded
        };
    }

    /// <summary>
    /// Đặt lại scope của một loại chi phí đúng bằng danh sách loại giao dịch mong muốn: scope đang dùng
    /// mà không còn trong danh sách thì ẩn đi, scope đã ẩn mà được chọn lại thì khôi phục, loại giao
    /// dịch mới thì thêm dòng.
    /// </summary>
    private static void ReconcileScopes(
        RevenueExpenseType entity,
        IReadOnlyCollection<RevenueExpenseTypeScope> existing,
        IReadOnlyCollection<TransactionType> desired)
    {
        var desiredSet = desired.ToHashSet();

        foreach (var scope in existing)
        {
            var keep = desiredSet.Contains(scope.TransactionType);
            if (scope.IsDeleted == keep)
                scope.IsDeleted = !keep;
        }

        foreach (var type in desired.Where(t => existing.All(s => s.TransactionType != t)))
            entity.Scopes.Add(new RevenueExpenseTypeScope { RevenueExpenseTypeId = entity.Id, TransactionType = type });
    }

    /// <summary>Chuẩn hoá danh sách loại giao dịch: bỏ trùng và chặn giá trị không nằm trong enum.</summary>
    private List<TransactionType> NormalizeTypes(IEnumerable<int>? values)
    {
        var result = new List<TransactionType>();
        foreach (var value in (values ?? Enumerable.Empty<int>()).Distinct())
        {
            if (!Enum.IsDefined(typeof(TransactionType), value))
                throw new BusinessException(_localizer["RevenueExpenseType_TypeInvalid"]);

            result.Add((TransactionType)value);
        }

        return result;
    }

    /// <summary>Kiểm tra tên loại chi phí: bắt buộc và tối đa 200 ký tự.</summary>
    private string CleanName(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new BusinessException(_localizer["RevenueExpenseType_NameRequired"]);
        if (trimmed.Length > 200)
            throw new BusinessException(_localizer["RevenueExpenseType_NameTooLong"]);

        return trimmed;
    }

    private static RevenueExpenseTypeResponse Map(RevenueExpenseType type) => new()
    {
        Id = type.Id,
        Name = type.Name,
        SortOrder = type.SortOrder,
        TransactionTypes = type.Scopes
            .Where(s => !s.IsDeleted)
            .Select(s => (int)s.TransactionType)
            .Distinct()
            .OrderBy(x => x)
            .ToList(),
        CreatedAt = type.CreatedAt,
        UpdatedAt = type.UpdatedAt
    };

    /// <summary>Dựng lại response từ scope đang dùng thật trong DB sau khi ghi.</summary>
    private async Task<RevenueExpenseTypeResponse> BuildAsync(RevenueExpenseType type, CancellationToken cancellationToken)
    {
        var types = await _scopeRepository.GetQueryable()
            .Where(s => s.RevenueExpenseTypeId == type.Id)
            .Select(s => (int)s.TransactionType)
            .ToListAsync(cancellationToken);

        return new RevenueExpenseTypeResponse
        {
            Id = type.Id,
            Name = type.Name,
            SortOrder = type.SortOrder,
            TransactionTypes = types.Distinct().OrderBy(x => x).ToList(),
            CreatedAt = type.CreatedAt,
            UpdatedAt = type.UpdatedAt
        };
    }
}
