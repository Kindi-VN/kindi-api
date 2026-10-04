namespace Kindi.API.UnitTests;

using FluentAssertions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Xunit;

/// <summary>
/// Chốt hành vi tham số <c>searchField</c> của danh sách mua chung: chỉ định một trường thì CHỈ khớp
/// từ khoá nằm ở đúng trường đó (không OR lan sang cột khác), bỏ trống thì giữ nguyên hành vi cũ là
/// khớp trên mọi trường — mã yêu cầu, tên sản phẩm, mã người giới thiệu, họ tên/SĐT/email người mở nhóm.
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
    public void Chon_mot_truong_thi_khong_khop_tu_khoa_nam_o_truong_khac()
    {
        // "AAA" chỉ nằm ở RecordReferrerCode; chọn trường khác thì không được khớp.
        RequestSearchFilters.ApplyGroupBuying(Requests(), "AAA", RequestSearchField.ProductName).Should().BeEmpty();
        RequestSearchFilters.ApplyGroupBuying(Requests(), "AAA", RequestSearchField.Code).Should().BeEmpty();
        RequestSearchFilters.ApplyGroupBuying(Requests(), "AAA", RequestSearchField.CustomerName).Should().BeEmpty();

        // Chọn đúng trường chứa từ khoá thì mới khớp.
        RequestSearchFilters.ApplyGroupBuying(Requests(), "AAA", RequestSearchField.RecordReferrerCode)
            .Select(x => x.GroupBuyingRequestCode).Should().Equal("GBR-002");
    }

    [Fact]
    public void Chon_truong_nguoi_mo_nhom_thi_khong_lay_tu_khoa_o_truong_san_pham()
    {
        // "Bàn" chỉ nằm ở ProductName; chọn CustomerName/CustomerPhone thì không khớp.
        RequestSearchFilters.ApplyGroupBuying(Requests(), "Bàn", RequestSearchField.CustomerName).Should().BeEmpty();
        RequestSearchFilters.ApplyGroupBuying(Requests(), "Bàn", RequestSearchField.CustomerPhone).Should().BeEmpty();
        RequestSearchFilters.ApplyGroupBuying(Requests(), "Bàn", RequestSearchField.ProductName)
            .Select(x => x.GroupBuyingRequestCode).Should().Equal("GBR-002");
    }

    [Fact]
    public void Khong_truyen_truong_thi_tim_tren_moi_truong_nhu_cu()
    {
        // Bỏ trống searchField = giữ nguyên tập trường tìm kiếm cũ của danh sách mua chung.
        RequestSearchFilters.ApplyGroupBuying(Requests(), "Cường", null)
            .Select(x => x.GroupBuyingRequestCode).Should().Equal("GBR-002");
        RequestSearchFilters.ApplyGroupBuying(Requests(), "GBR-001", null)
            .Select(x => x.GroupBuyingRequestCode).Should().Equal("GBR-001");
        RequestSearchFilters.ApplyGroupBuying(Requests(), "0900000001", null)
            .Select(x => x.GroupBuyingRequestCode).Should().Equal("GBR-001");

        // Mua chung dò cả mã người giới thiệu ở chế độ mặc định nên từ khoá chỉ có ở trường đó vẫn khớp.
        RequestSearchFilters.ApplyGroupBuying(Requests(), "AAA", null)
            .Select(x => x.GroupBuyingRequestCode).Should().Equal("GBR-002");
    }

    [Fact]
    public void Tu_khoa_rong_thi_khong_loc_gi()
    {
        RequestSearchFilters.ApplyGroupBuying(Requests(), null, RequestSearchField.ProductName)
            .Should().HaveCount(2);
        RequestSearchFilters.ApplyGroupBuying(Requests(), "", RequestSearchField.CustomerEmail)
            .Should().HaveCount(2);
    }
}
