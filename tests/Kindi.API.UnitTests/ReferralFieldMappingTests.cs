namespace Kindi.API.UnitTests;

using AutoMapper;
using FluentAssertions;
using Kindi.API.Application.DTOs.responses;
using Kindi.API.Application.Mappings;
using Kindi.API.Domain.Entities;
using Xunit;

/// <summary>
/// Chốt việc map ra DTO cho yêu cầu: mỗi cặp entity→DTO chỉ có MỘT định nghĩa map (khai trong DTO),
/// nên thông tin cá nhân (họ tên/SĐT/Zalo/Email) lẫn mã người giới thiệu của tài khoản phải ra đủ giá trị,
/// không bị định nghĩa map trùng trong MappingProfile ghi đè làm mất trường.
/// </summary>
public class ReferralFieldMappingTests
{
    private static IMapper CreateMapper()
        => new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    [Fact]
    public void Map_offer_lay_du_ma_nguoi_gioi_thieu_va_thong_tin_ca_nhan()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Nguyễn Văn A",
            Phone = "0901234567",
            Zalo = "0907654321",
            Email = "a@example.com",
            AccountReferrerCode = "CTV-ABC123"
        };
        var offer = new OfferRequest
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ProductName = "Sữa bột",
            Unit = "hộp",
            RecordReferrerCode = "CTV-RECORD"
        };

        var dto = CreateMapper().Map<OfferRequestResponseDto>(offer);

        dto.AccountReferrerCode.Should().Be("CTV-ABC123");
        dto.RecordReferrerCode.Should().Be("CTV-RECORD");
        dto.FullName.Should().Be("Nguyễn Văn A");
        dto.Phone.Should().Be("0901234567");
        dto.Zalo.Should().Be("0907654321");
        dto.Email.Should().Be("a@example.com");
    }

    [Fact]
    public void Map_offer_khong_co_zalo_thi_tra_chuoi_rong()
    {
        var user = new User { Id = Guid.NewGuid(), FullName = "A", Email = "a@x.com", Zalo = null };
        var offer = new OfferRequest { UserId = user.Id, User = user, ProductName = "P", Unit = "cái" };

        CreateMapper().Map<OfferRequestResponseDto>(offer).Zalo.Should().BeEmpty();
    }

    [Fact]
    public void Map_purchase_lay_du_ma_nguoi_gioi_thieu_va_thong_tin_ca_nhan()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Trần Thị B",
            Phone = "0909999999",
            Zalo = "0908888888",
            Email = "b@example.com",
            AccountReferrerCode = "CTV-XYZ789"
        };
        var purchase = new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ProductName = "Bàn làm việc",
            Unit = "cái",
            RecordReferrerCode = "CTV-RECORD"
        };

        var dto = CreateMapper().Map<PurchaseRequestResponseDto>(purchase);

        dto.AccountReferrerCode.Should().Be("CTV-XYZ789");
        dto.RecordReferrerCode.Should().Be("CTV-RECORD");
        dto.FullName.Should().Be("Trần Thị B");
        dto.Phone.Should().Be("0909999999");
        dto.Zalo.Should().Be("0908888888");
        dto.Email.Should().Be("b@example.com");
    }

    [Fact]
    public void Map_purchase_khong_co_zalo_thi_tra_null()
    {
        var user = new User { Id = Guid.NewGuid(), FullName = "B", Email = "b@x.com", Zalo = null };
        var purchase = new PurchaseRequest { UserId = user.Id, User = user, ProductName = "P", Unit = "cái" };

        CreateMapper().Map<PurchaseRequestResponseDto>(purchase).Zalo.Should().BeNull();
    }
}
