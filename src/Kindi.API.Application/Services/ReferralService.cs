// src/Kindi.API.Application/Services/ReferralService.cs
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.Common.Interfaces;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Interfaces;
using Kindi.API.Shared.Common.Interfaces;

namespace Kindi.API.Application.Services;

/// <summary>
/// Mã chia sẻ riêng (refcode) của từng chủ thể — xem <see cref="IReferralService"/>.
/// CTV dùng mã riêng trên hồ sơ CTV; các tài khoản khác sinh mã riêng ở bảng Users.
/// </summary>
public class ReferralService : IReferralService
{
    private readonly IRepository<Collaborator> _collaboratorRepository;
    private readonly IRepository<User> _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IQueryService _queryService;

    public ReferralService(
        IRepository<Collaborator> collaboratorRepository,
        IRepository<User> userRepository,
        ICurrentUserService currentUserService,
        IQueryService queryService)
    {
        _collaboratorRepository = collaboratorRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _queryService = queryService;
    }

    public async Task<string?> GetSharerReferralCodeAsync()
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            return null;

        var collaborator = await _queryService.GetFirstOrDefaultAsync<Collaborator>(c => c.UserId == userId);
        if (collaborator != null)
        {
            // Hồ sơ CTV cũ chưa có mã chia sẻ riêng → lấy theo mã CTV đã in trên hồ sơ.
            if (!string.IsNullOrWhiteSpace(collaborator.ReferralCode))
                return collaborator.ReferralCode;

            collaborator.ReferralCode = collaborator.CollaboratorCode ?? await GenerateUniqueCodeAsync();
            _collaboratorRepository.Update(collaborator);
            await _collaboratorRepository.SaveChangesAsync();

            return collaborator.ReferralCode;
        }

        var user = await _queryService.GetByIdAsync<User>(userId);
        if (user == null)
            return null;

        if (!string.IsNullOrWhiteSpace(user.ReferralCode))
            return user.ReferralCode;

        user.ReferralCode = await GenerateUniqueCodeAsync();
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        return user.ReferralCode;
    }

    public async Task<string?> ResolveAsync(string? referralCode)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
            return null;

        var code = referralCode.Trim().ToUpperInvariant();

        var exists =
            await _queryService.AnyAsync<User>(u => u.ReferralCode == code) ||
            await _queryService.AnyAsync<Collaborator>(c => c.ReferralCode == code || c.CollaboratorCode == code);

        return exists ? code : null;
    }

    public async Task<Dictionary<string, string>> LoadNamesAsync(IEnumerable<string?> referralCodes)
    {
        var codes = referralCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (codes.Count == 0)
            return new Dictionary<string, string>();

        var users = await _queryService.GetListAsync<User>(
            u => u.ReferralCode != null && codes.Contains(u.ReferralCode));

        var collaborators = await _queryService.GetListAsync<Collaborator>(
            c => (c.ReferralCode != null && codes.Contains(c.ReferralCode))
                 || (c.CollaboratorCode != null && codes.Contains(c.CollaboratorCode)));

        var names = users
            .Where(u => u.ReferralCode != null)
            .GroupBy(u => u.ReferralCode!)
            .ToDictionary(g => g.Key, g => g.First().FullName);

        // Mã của CTV ưu tiên tên trên hồ sơ CTV (chủ thể chính của link chia sẻ).
        foreach (var group in collaborators.GroupBy(c => c.ReferralCode ?? c.CollaboratorCode!))
            names[group.Key] = group.First().FullName;

        return names;
    }

    /// <summary>Mã chia sẻ riêng dạng "CTV-XXXXXX" (như mã CTV) và không trùng ở bảng nào.</summary>
    private async Task<string> GenerateUniqueCodeAsync()
    {
        string code;
        bool exists;
        do
        {
            code = CodeGenerator.Generate("CTV");
            exists =
                await _queryService.AnyAsync<User>(u => u.ReferralCode == code) ||
                await _queryService.AnyAsync<Collaborator>(c => c.ReferralCode == code || c.CollaboratorCode == code);
        } while (exists);

        return code;
    }
}
