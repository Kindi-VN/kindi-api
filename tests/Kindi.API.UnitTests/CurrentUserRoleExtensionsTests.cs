namespace Kindi.API.UnitTests;

using FluentAssertions;
using Kindi.API.Application.Common.Extensions;
using Kindi.API.Domain.Enums;
using Kindi.API.Shared.Common.Interfaces;
using Xunit;

/// <summary>
/// Chốt cách kiểm vai trò của người gọi: claim vai trò là SỐ nên mỗi vai trò phải so đúng mã của nó —
/// thiếu nhánh SuperAdmin thì lời gọi hỏi SuperAdmin bị hiểu nhầm thành hỏi User, làm cờ bảo vệ
/// tài khoản quản trị tối cao không chạy.
/// </summary>
public class CurrentUserRoleExtensionsTests
{
    [Theory]
    [InlineData(UserRole.User, "1")]
    [InlineData(UserRole.Partner, "2")]
    [InlineData(UserRole.Admin, "3")]
    [InlineData(UserRole.SuperAdmin, "4")]
    public void IsInRole_so_dung_ma_cua_vai_tro(UserRole role, string expectedClaim)
    {
        var currentUser = new StubCurrentUserService(expectedClaim);

        currentUser.IsInRole(role).Should().BeTrue();
        currentUser.LastCheckedRole.Should().Be(expectedClaim);
    }

    [Fact]
    public void Super_admin_khong_bi_hieu_nham_thanh_user()
    {
        var currentUser = new StubCurrentUserService("1");

        currentUser.IsInRole(UserRole.SuperAdmin).Should().BeFalse("người gọi chỉ có role User");
        currentUser.LastCheckedRole.Should().Be("4");
    }

    [Fact]
    public void Super_admin_co_role_3_van_la_admin_nhung_hoi_super_admin_thi_dung()
    {
        // Token của SuperAdmin mang cả role 3 (kế thừa quyền admin) — hai câu hỏi phải cho hai kết quả khác nhau.
        var currentUser = new StubCurrentUserService("3", "4");

        currentUser.IsInRole(UserRole.Admin).Should().BeTrue();
        currentUser.IsInRole(UserRole.SuperAdmin).Should().BeTrue();
    }

    private sealed class StubCurrentUserService(params string[] roles) : ICurrentUserService
    {
        public string? LastCheckedRole { get; private set; }
        public string? UserId => Guid.NewGuid().ToString();
        public string? UserName => null;
        public bool IsAuthenticated => true;

        public bool IsInRole(string role)
        {
            LastCheckedRole = role;
            return roles.Contains(role);
        }

        public string? IpAddress => null;
        public string? UserAgent => null;
    }
}
