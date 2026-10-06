namespace Kindi.API.UnitTests;

using FluentAssertions;
using Kindi.API.Application.Common.Extensions;
using Kindi.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Chốt câu SQL sinh ra cho <c>x.Field.Like(keyword)</c> và <c>x.Code.EqualsCode(keyword)</c>:
/// tìm theo tên thì phải bỏ dấu tiếng Việt (unaccent) và không phân biệt hoa/thường (ILIKE); tìm theo mã
/// thì so bằng <c>=</c> (KHÔNG LIKE) để không phá index. Kiểm bằng <c>ToQueryString</c> nên không cần DB thật.
/// </summary>
public class StringQueryExtensionsSqlTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    public StringQueryExtensionsSqlTests(Xunit.Abstractions.ITestOutputHelper output) => _output = output;

    [Fact]
    public void Like_sinh_ra_unaccent_va_ilike()
    {
        using var context = CreatePostgresContext();

        var sql = context.Users.Where(u => u.FullName.Like("sữa")).ToQueryString();

        _output.WriteLine(sql);

        sql.Should().Contain("unaccent(", "tìm 'sua bot' phải ra 'Sữa bột'");
        sql.Should().Contain("lower(", "tìm tên không phân biệt hoa/thường");
        sql.Should().Contain("strpos(", "tìm chứa: strpos(...) > 0");
    }

    [Fact]
    public void EqualsCode_so_bang_dung_va_khong_dung_like()
    {
        using var context = CreatePostgresContext();

        var sql = context.Users.Where(u => u.UserCode.EqualsCode("ctv-1")).ToQueryString();

        _output.WriteLine(sql);

        sql.Should().NotContain("LIKE", "mã phải so bằng = để còn dùng được index");
        sql.Should().Contain("= upper(trim(", "cột giữ nguyên, chỉ tham số được chuẩn hoá");
    }

    [Fact]
    public void Like_bo_dau_va_khong_phan_biet_hoa_thuong_ngoai_truy_van()
    {
        // Bản chạy trong bộ nhớ (dùng cho test) phải cùng hành vi: bỏ dấu + không phân biệt hoa/thường.
        "Sữa bột".Like("sua").Should().BeTrue();
        "Sua bot".Like("SỮA").Should().BeTrue();
        "Sữa bột".Like("sữa bột ngũ cốc").Should().BeFalse();
        ((string?)null).Like("sua").Should().BeFalse();
        "Sữa bột".Like(null).Should().BeTrue("không có từ khoá thì không lọc gì");
    }

    [Fact]
    public void EqualsCode_dung_ma_thi_khop_khac_hoa_thuong()
    {
        "CTV-1".EqualsCode("ctv-1").Should().BeTrue();
        "CTV-1".EqualsCode("CTV-").Should().BeFalse("mã so khớp đúng, không tìm một phần");
        "CTV-1".EqualsCode(null).Should().BeFalse();
    }

    private static ApplicationDbContext CreatePostgresContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=kindi;Username=kindi;Password=kindi")
            .Options;

        return new ApplicationDbContext(options, new StubCurrentUserService());
    }
}
