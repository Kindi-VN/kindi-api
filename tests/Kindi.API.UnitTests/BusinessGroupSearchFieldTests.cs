namespace Kindi.API.UnitTests;

using System.Text.Json;
using FluentAssertions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Xunit;

/// <summary>
/// Chốt danh sách trường hợp lệ và hành vi rỗng của tham số <c>searchField</c> cho danh sách nhóm:
/// danh sách tài liệu phải khớp tên enum dạng camelCase (UI gửi lên đúng chuỗi này), và không chỉ định
/// trường / từ khoá rỗng thì helper không lọc gì (việc OR mặc định do từng danh sách tự áp).
/// </summary>
public class BusinessGroupSearchFieldTests
{
    [Fact]
    public void Danh_sach_truong_hop_le_khop_hop_dong_ui_va_ten_enum()
    {
        BusinessGroupSearchFilters.ValidFieldNames.Should().Equal(
            "name", "description", "topic", "businessFieldName", "code");

        var fromEnum = Enum.GetNames<BusinessGroupSearchField>()
            .Select(JsonNamingPolicy.CamelCase.ConvertName);
        BusinessGroupSearchFilters.ValidFieldNames.Should().BeEquivalentTo(fromEnum);
    }

    [Fact]
    public void Khong_chi_dinh_truong_hoac_tu_khoa_rong_thi_tra_nguyen_trang()
    {
        var groups = new List<BusinessGroup>
        {
            new() { Name = "Nhóm A" },
            new() { Name = "Nhóm B" }
        }.AsQueryable();

        BusinessGroupSearchFilters.ApplyField(groups, null, BusinessGroupSearchField.Name).Should().HaveCount(2);
        BusinessGroupSearchFilters.ApplyField(groups, "", BusinessGroupSearchField.Description).Should().HaveCount(2);
        BusinessGroupSearchFilters.ApplyField(groups, "abc", null).Should().HaveCount(2);
    }
}
