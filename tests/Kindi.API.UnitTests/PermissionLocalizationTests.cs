namespace Kindi.API.UnitTests;

using System.Security.Claims;
using FluentAssertions;
using Kindi.API.Application.Resources;
using Kindi.API.Domain.Enums;
using Kindi.API.Shared.Common.Helpers;
using Kindi.API.Shared.Constants;
using Kindi.API.WebApi.Middlewares;
using Microsoft.AspNetCore.Http;
using Xunit;

/// <summary>
/// Tên quyền hiển thị ở UI do API dịch sẵn theo ngôn ngữ người dùng chọn lúc đăng nhập (claim trong token),
/// nên UI không phải giữ bản dịch riêng cho từng mã quyền — thêm quyền mới là tự có tên ở mọi ngôn ngữ.
/// Bộ test này chốt: mọi node quyền có bản dịch ở CẢ vi và en, cây quyền trả kèm metadata
/// (mã, loại hành động, module, route, endpoint), token mang ngôn ngữ và middleware dịch theo token.
/// </summary>
[Collection("permission-culture")]
public class PermissionLocalizationTests
{
    [Fact]
    public async Task Moi_node_quyen_deu_co_ten_hien_thi_o_ca_vi_va_en()
    {
        // Hành động: bản dịch nằm ở resx theo NameKey → phải có đủ cả 2 ngôn ngữ API hỗ trợ.
        // Localizer đọc ngôn ngữ tại thời điểm tạo nên phải tạo lại trong từng culture.
        var missing = new List<string>();

        using (new CultureScope("vi"))
        {
            var viLocalizer = TestLocalizer.Create();
            foreach (var action in PermissionCatalog.All)
            {
                if (viLocalizer[action.NameKey].ResourceNotFound)
                    missing.Add($"vi:{action.PermissionCode}");
            }
        }

        using (new CultureScope("en"))
        {
            var enLocalizer = TestLocalizer.Create();
            foreach (var action in PermissionCatalog.All)
            {
                if (enLocalizer[action.NameKey].ResourceNotFound)
                    missing.Add($"en:{action.PermissionCode}");
            }
        }

        missing.Should().BeEmpty("thiếu bản dịch thì màn phân quyền hiện mã thô thay vì tên quyền");

        // Nhóm/màn hình: tên 2 ngôn ngữ nằm trong danh mục của code → cây quyền phải trả tên đã chọn theo ngôn ngữ.
        await using var context = PermissionTestFixture.CreateContext();

        using (new CultureScope("vi"))
        {
            var service = PermissionTestFixture.CreateService(context);
            var viTree = await service.GetTreeAsync((int)UserRole.Admin);
            Flatten(viTree).Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.Name));
            viTree.First(x => x.Code == "ADMIN").Name
                .Should().Be(PermissionGroupCatalog.All.First(g => g.Code == "ADMIN").Name);
        }

        using (new CultureScope("en"))
        {
            var service = PermissionTestFixture.CreateService(context);
            var enTree = await service.GetTreeAsync((int)UserRole.Admin);
            Flatten(enTree).Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.Name));
            enTree.First(x => x.Code == "ADMIN").Name
                .Should().Be(PermissionGroupCatalog.All.First(g => g.Code == "ADMIN").NameEn);
        }
    }

    [Fact]
    public async Task Cay_quyen_tra_ten_da_dich_theo_ngon_gu_va_kem_metadata()
    {
        await using var context = PermissionTestFixture.CreateContext();

        using (new CultureScope("vi"))
        {
            var service = PermissionTestFixture.CreateService(context);
            var viNode = Flatten(await service.GetTreeAsync((int)UserRole.Admin)).First(x => x.Code == "P155");

            viNode.Name.Should().Be("Sửa thông tin người dùng");
            viNode.ActionKind.Should().Be("update");
            viNode.Module.Should().NotBeNullOrWhiteSpace();
            viNode.Endpoints.Should().NotBeNullOrWhiteSpace("màn phân quyền hiển thị endpoint để biết quyền bảo vệ API nào");
        }

        using (new CultureScope("en"))
        {
            var service = PermissionTestFixture.CreateService(context);
            Flatten(await service.GetTreeAsync((int)UserRole.Admin)).First(x => x.Code == "P155")
                .Name.Should().Be("Update user information");
        }
    }

    [Theory]
    [InlineData("vi", "vi")]
    [InlineData("VI-VN", "vi")]
    [InlineData("en-US", "en")]
    [InlineData("en_US", "en")]
    [InlineData("fr", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Chuan_hoa_ngon_ngu_ve_vi_hoac_en(string? input, string? expected)
        => LanguageHelper.Normalize(input).Should().Be(expected);

    [Fact]
    public async Task Token_mang_ngon_ngu_nguoi_dung_chon()
    {
        await using var context = PermissionTestFixture.CreateContext();
        var jwt = PermissionTestFixture.CreateJwtService(context);

        var withEnglish = jwt.GenerateToken("user-1", "user1", new[] { RoleConstants.Admin }, language: "en-US");
        var withVietnamese = jwt.GenerateToken("user-1", "user1", new[] { RoleConstants.Admin }, language: "vi");
        var withoutLanguage = jwt.GenerateToken("user-1", "user1", new[] { RoleConstants.Admin });

        LanguageOf(jwt, withEnglish).Should().Be("en");
        LanguageOf(jwt, withVietnamese).Should().Be("vi");
        LanguageOf(jwt, withoutLanguage).Should().BeNull("không chọn ngôn ngữ thì để API dùng ngôn ngữ mặc định");
    }

    [Fact]
    public async Task Middleware_dich_theo_ngon_ngu_trong_token()
    {
        await using var context = PermissionTestFixture.CreateContext();
        var jwt = PermissionTestFixture.CreateJwtService(context);
        var token = jwt.GenerateToken("user-1", "user1", new[] { RoleConstants.Admin }, language: "en");

        using var scope = new CultureScope("vi");
        var httpContext = new DefaultHttpContext { User = jwt.ValidateToken(token)! };

        // CultureInfo.CurrentUICulture là trạng thái theo luồng async: middleware đặt culture cho luồng chạy
        // TIẾP (middleware sau + MVC) nên phải quan sát TỪ BÊN TRONG delegate, không phải sau lời gọi.
        var observed = await ObserveCultureAsync(httpContext);

        observed.Should().Be("en");
    }

    [Fact]
    public async Task Middleware_giu_nguyen_ngon_ngu_khi_token_khong_co_claim()
    {
        await using var context = PermissionTestFixture.CreateContext();
        var jwt = PermissionTestFixture.CreateJwtService(context);
        var token = jwt.GenerateToken("user-1", "user1", new[] { RoleConstants.Admin });

        using var scope = new CultureScope("vi");
        var httpContext = new DefaultHttpContext { User = jwt.ValidateToken(token)! };

        var observed = await ObserveCultureAsync(httpContext);

        observed.Should().Be("vi");
    }

    /// <summary>Chạy middleware rồi trả về ngôn ngữ mà các bước xử lý phía sau nhìn thấy.</summary>
    private static async Task<string> ObserveCultureAsync(HttpContext httpContext)
    {
        string? observed = null;
        var middleware = new TokenCultureMiddleware(_ =>
        {
            observed = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(httpContext);
        return observed!;
    }

    private static string? LanguageOf(Kindi.API.Shared.Common.Interfaces.IJwtService jwt, string token)
        => jwt.ValidateToken(token)?.FindFirst(AuthClaimConstants.Language)?.Value;

    private static IEnumerable<Kindi.API.Application.DTOs.responses.PermissionTreeNodeResponse> Flatten(
        IEnumerable<Kindi.API.Application.DTOs.responses.PermissionTreeNodeResponse> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in Flatten(node.Children)) yield return child;
        }
    }
}
