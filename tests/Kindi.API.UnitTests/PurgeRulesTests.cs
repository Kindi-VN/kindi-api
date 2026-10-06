namespace Kindi.API.UnitTests;

using FluentAssertions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Domain.Entities;
using Kindi.API.Shared.Errors;
using Xunit;

/// <summary>
/// Chốt quy tắc xoá VĨNH VIỄN ở màn "Đã xoá": bắt buộc có điều kiện (chọn dòng hoặc khoảng ngày), chọn
/// dòng thì ưu tiên hơn khoảng ngày, và CHỈ xoá được bản ghi ĐANG xoá mềm — bản ghi còn sống không bao
/// giờ bị đụng tới (màn thường chỉ được xoá mềm).
/// </summary>
public class PurgeRulesTests
{
    private static Partner DeletedPartner(string name, DateTime deletedAt)
        => new() { CompanyName = name, IsDeleted = true, UpdatedAt = deletedAt };

    private static List<Partner> Seed() => new()
    {
        DeletedPartner("Xoá mềm 05/10", new DateTime(2026, 10, 5, 3, 0, 0, DateTimeKind.Utc)),
        DeletedPartner("Xoá mềm 06/10", new DateTime(2026, 10, 6, 9, 7, 28, DateTimeKind.Utc)),
        new() { CompanyName = "Đang hoạt động", IsDeleted = false, UpdatedAt = new DateTime(2026, 10, 6, 9, 7, 28, DateTimeKind.Utc) }
    };

    [Fact]
    public void Thieu_dieu_kien_thi_chan_khong_cho_xoa()
    {
        var action = () => PurgeRules.Resolve(null, null, null);

        action.Should().Throw<AppException>()
            .Which.Error.MessageKey.Should().Be("Purge_CriteriaRequired");
    }

    [Fact]
    public void Tu_ngay_lon_hon_den_ngay_thi_bao_loi()
    {
        var action = () => PurgeRules.Resolve(
            null,
            new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));

        action.Should().Throw<AppException>()
            .Which.Error.MessageKey.Should().Be("Purge_RangeInvalid");
    }

    [Fact]
    public void Chon_qua_nhieu_dong_thi_chan()
    {
        var ids = Enumerable.Range(0, PurgeRules.MaxIdsPerRequest + 1).Select(_ => Guid.NewGuid()).ToList();

        var action = () => PurgeRules.Resolve(ids, null, null);

        action.Should().Throw<AppException>()
            .Which.Error.MessageKey.Should().Be("Purge_TooManyIds");
    }

    [Fact]
    public void Chon_dong_thi_bo_qua_khoang_ngay_va_bo_id_rong_trung_lap()
    {
        var id = Guid.NewGuid();
        var scope = PurgeRules.Resolve(
            new List<Guid> { id, id, Guid.Empty },
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        scope.IsBySelection.Should().BeTrue();
        scope.Ids.Should().Equal(id);
        scope.FromDate.Should().BeNull();
        scope.ToDate.Should().BeNull();
    }

    [Fact]
    public void Chi_lay_ban_ghi_da_xoa_mem_khi_chon_dong()
    {
        var partners = Seed();
        var scope = PurgeRules.Resolve(partners.Select(p => p.Id).ToList(), null, null);

        var targets = PurgeRules.Apply(partners.AsQueryable(), scope).ToList();

        targets.Should().HaveCount(2);
        targets.Should().OnlyContain(p => p.IsDeleted);
        targets.Should().NotContain(p => p.CompanyName == "Đang hoạt động");
    }

    [Fact]
    public void Loc_theo_khoang_ngay_xoa_mem()
    {
        var partners = Seed();
        var scope = PurgeRules.Resolve(
            null,
            new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc));

        var targets = PurgeRules.Apply(partners.AsQueryable(), scope).ToList();

        targets.Should().HaveCount(1);
        targets.Single().CompanyName.Should().Be("Xoá mềm 06/10");
    }

    [Fact]
    public void Chi_gui_ngay_ket_thuc_thi_phai_phu_het_ngay_do()
    {
        // Xoá lúc 09:07 ngày 06/10 mà lọc "đến 06/10" vẫn phải thấy (lỗi cũ: lấy 00:00 nên sót).
        var partners = Seed();
        var scope = PurgeRules.Resolve(null, null, new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc));

        var targets = PurgeRules.Apply(partners.AsQueryable(), scope).ToList();

        targets.Should().HaveCount(2);
        targets.Should().NotContain(p => p.CompanyName == "Đang hoạt động");
    }

    [Fact]
    public void Ban_ghi_da_xoa_ma_thieu_UpdatedAt_thi_lay_theo_CreatedAt()
    {
        var legacy = new Partner { CompanyName = "Cũ", IsDeleted = true, UpdatedAt = null, CreatedAt = new DateTime(2026, 9, 30, 5, 0, 0, DateTimeKind.Utc) };
        var partners = new List<Partner> { legacy };

        var inside = PurgeRules.Resolve(null, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc));
        var outside = PurgeRules.Resolve(null, new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), null);

        PurgeRules.Apply(partners.AsQueryable(), inside).Should().HaveCount(1);
        PurgeRules.Apply(partners.AsQueryable(), outside).Should().BeEmpty();
    }
}
