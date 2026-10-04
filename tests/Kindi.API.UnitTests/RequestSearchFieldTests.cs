namespace Kindi.API.UnitTests;

using System.Text.Json;
using FluentAssertions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Xunit;

/// <summary>
/// Chốt hành vi tham số <c>searchField</c> của danh sách yêu cầu: chỉ định một trường thì CHỈ khớp
/// từ khoá nằm ở đúng trường đó (không OR lan sang cột khác), bỏ trống thì giữ nguyên hành vi cũ
/// là khớp trên mọi trường.
/// </summary>
public class RequestSearchFieldTests
{
    private static IQueryable<OfferRequest> Offers() => new List<OfferRequest>
    {
        new()
        {
            Id = Guid.NewGuid(),
            ProductName = "Sản phẩm A",
            Unit = "cái",
            OfferRequestCode = "OFR-001",
            RecordReferrerCode = "CTV-ZZZ",
            User = new User { Id = Guid.NewGuid(), FullName = "Nguyễn Văn Bình", Phone = "0900000001", Email = "binh@example.com" }
        },
        new()
        {
            Id = Guid.NewGuid(),
            ProductName = "Bàn ghế",
            Unit = "bộ",
            OfferRequestCode = "OFR-002",
            RecordReferrerCode = "CTV-AAA",
            User = new User { Id = Guid.NewGuid(), FullName = "Trần Văn Cường", Phone = "0900000002", Email = "cuong@example.com" }
        }
    }.AsQueryable();

    [Fact]
    public void Chon_mot_truong_thi_khong_khop_tu_khoa_nam_o_truong_khac()
    {
        // "AAA" chỉ nằm ở RecordReferrerCode; chọn trường khác thì không được khớp.
        RequestSearchFilters.ApplyOffer(Offers(), "AAA", RequestSearchField.ProductName).Should().BeEmpty();
        RequestSearchFilters.ApplyOffer(Offers(), "AAA", RequestSearchField.Code).Should().BeEmpty();
        RequestSearchFilters.ApplyOffer(Offers(), "AAA", RequestSearchField.CustomerName).Should().BeEmpty();

        // Chọn đúng trường chứa từ khoá thì mới khớp.
        RequestSearchFilters.ApplyOffer(Offers(), "AAA", RequestSearchField.RecordReferrerCode)
            .Select(x => x.OfferRequestCode).Should().Equal("OFR-002");
    }

    [Fact]
    public void Chon_truong_khach_hang_thi_khong_lay_tu_khoa_o_truong_san_pham()
    {
        // "Bàn" chỉ nằm ở ProductName; chọn CustomerName/CustomerPhone thì không khớp.
        RequestSearchFilters.ApplyOffer(Offers(), "Bàn", RequestSearchField.CustomerName).Should().BeEmpty();
        RequestSearchFilters.ApplyOffer(Offers(), "Bàn", RequestSearchField.CustomerPhone).Should().BeEmpty();
        RequestSearchFilters.ApplyOffer(Offers(), "Bàn", RequestSearchField.ProductName)
            .Select(x => x.OfferRequestCode).Should().Equal("OFR-002");
    }

    [Fact]
    public void Khong_truyen_truong_thi_tim_tren_moi_truong_nhu_cu()
    {
        // Bỏ trống searchField = giữ nguyên tập trường tìm kiếm cũ của danh sách offer
        // (sản phẩm, mã yêu cầu, họ tên/SĐT/email khách hàng) — khớp khi từ khoá nằm ở bất kỳ trường nào.
        RequestSearchFilters.ApplyOffer(Offers(), "Cường", null)
            .Select(x => x.OfferRequestCode).Should().Equal("OFR-002");
        RequestSearchFilters.ApplyOffer(Offers(), "OFR-001", null)
            .Select(x => x.OfferRequestCode).Should().Equal("OFR-001");
        RequestSearchFilters.ApplyOffer(Offers(), "0900000001", null)
            .Select(x => x.OfferRequestCode).Should().Equal("OFR-001");

        // Hành vi cũ của offer KHÔNG dò RecordReferrerCode: từ khoá chỉ có ở trường đó không khớp.
        RequestSearchFilters.ApplyOffer(Offers(), "AAA", null).Should().BeEmpty();
    }

    [Fact]
    public void Tu_khoa_rong_thi_khong_loc_gi()
    {
        RequestSearchFilters.ApplyOffer(Offers(), null, RequestSearchField.ProductName)
            .Should().HaveCount(2);
        RequestSearchFilters.ApplyOffer(Offers(), "", RequestSearchField.CustomerEmail)
            .Should().HaveCount(2);

        var purchases = new List<PurchaseRequest>
        {
            new() { ProductName = "P", Unit = "cái" }
        }.AsQueryable();
        RequestSearchFilters.ApplyPurchase(purchases, null, RequestSearchField.RecordReferrerCode)
            .Should().HaveCount(1);
    }

    [Fact]
    public void Danh_sach_truong_hop_le_khop_hop_dong_ui_va_ten_enum()
    {
        RequestSearchFilters.ValidFieldNames.Should().Equal(
            "productName", "code", "recordReferrerCode", "customerName", "customerPhone", "customerEmail");

        // Danh sách tài liệu phải khớp tên enum dạng camelCase (UI gửi lên đúng chuỗi này).
        var fromEnum = Enum.GetNames<RequestSearchField>()
            .Select(JsonNamingPolicy.CamelCase.ConvertName);
        RequestSearchFilters.ValidFieldNames.Should().BeEquivalentTo(fromEnum);
    }
}
