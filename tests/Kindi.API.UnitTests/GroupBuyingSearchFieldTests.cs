namespace Kindi.API.UnitTests;

using FluentAssertions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Xunit;

/// <summary>
/// Chốt hành vi tham số <c>searchField</c> của danh sách mua chung ở phần chạy được trong bộ nhớ:
/// từ khoá rỗng (hoặc <c>null</c>) thì helper không lọc gì, trả về nguyên tập bản ghi.
/// Các trường hợp so khớp từ khoá của mua chung dùng ILIKE + Unaccent nên chỉ chạy được dưới nhà cung cấp
/// EF (PostgreSQL), không kiểm chứng được in-memory.
/// </summary>
public class GroupBuyingSearchFieldTests
{
    private static IQueryable<GroupBuyingRequest> Requests() => new List<GroupBuyingRequest>
    {
        new()
        {
            Id = Guid.NewGuid(),
            GroupBuyingRequestCode = "GBR-001",
            ProductName = "Sản phẩm A",
            RecordReferrerCode = "CTV-ZZZ",
            User = new User { Id = Guid.NewGuid(), FullName = "Nguyễn Văn Bình", Phone = "0900000001", Email = "binh@example.com" }
        },
        new()
        {
            Id = Guid.NewGuid(),
            GroupBuyingRequestCode = "GBR-002",
            ProductName = "Bàn ghế",
            RecordReferrerCode = "CTV-AAA",
            User = new User { Id = Guid.NewGuid(), FullName = "Trần Văn Cường", Phone = "0900000002", Email = "cuong@example.com" }
        }
    }.AsQueryable();

    [Fact]
    public void Tu_khoa_rong_thi_khong_loc_gi()
    {
        RequestSearchFilters.ApplyGroupBuying(Requests(), null, RequestSearchField.ProductName)
            .Should().HaveCount(2);
        RequestSearchFilters.ApplyGroupBuying(Requests(), "", RequestSearchField.CustomerEmail)
            .Should().HaveCount(2);
    }

    [Fact]
    public void Ten_nguoi_gioi_thieu_ban_ghi_loc_theo_tap_ma_quy_doi()
    {
        // Nhánh tên người giới thiệu chỉ so tập mã (không dùng ILIKE) nên chạy được trong bộ nhớ.
        // Nhánh mặc định của mua chung trộn ILIKE + Unaccent nên chỉ kiểm chứng được dưới PostgreSQL.
        RequestSearchFilters.ApplyGroupBuying(Requests(), "luc8", RequestSearchField.RecordReferrerName, new[] { "CTV-AAA" })
            .Select(x => x.GroupBuyingRequestCode).Should().Equal("GBR-002");
        RequestSearchFilters.ApplyGroupBuying(Requests(), "luc8", RequestSearchField.RecordReferrerName, Array.Empty<string>())
            .Should().BeEmpty();
    }
}
