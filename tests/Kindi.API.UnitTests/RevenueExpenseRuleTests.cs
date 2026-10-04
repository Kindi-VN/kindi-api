namespace Kindi.API.UnitTests;

using FluentAssertions;
using Kindi.API.Application.Common.Helpers;
using Kindi.API.Application.DTOs.requests;
using Kindi.API.Domain.Entities;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Rules;
using Xunit;

/// <summary>
/// Chốt luật chọn loại chi phí theo loại giao dịch: có loại gắn riêng cho giao dịch thì chỉ dùng nhóm
/// gắn riêng, không có thì mới rơi về nhóm mặc định; thứ tự trong nhóm theo SortOrder rồi tên. Sai luật
/// này là màn khai doanh thu hiện sai danh sách chi phí.
/// </summary>
public class RevenueExpenseRuleTests
{
    private static RevenueExpenseType Type(string name, int sortOrder, params TransactionType[] scopes)
    {
        var type = new RevenueExpenseType { Name = name, SortOrder = sortOrder };
        foreach (var scope in scopes)
            type.Scopes.Add(new RevenueExpenseTypeScope { RevenueExpenseTypeId = type.Id, TransactionType = scope });

        return type;
    }

    [Fact]
    public void Co_loai_gan_dung_giao_dich_thi_lay_nhom_gan_rieng_va_bo_qua_mac_dinh()
    {
        var all = new[]
        {
            Type("Mặc định A", 0),
            Type("Riêng mua hàng", 1, TransactionType.PurchaseRequest),
            Type("Riêng mua chung", 2, TransactionType.GroupBuyingRequest)
        };

        var result = RevenueExpenseRule.Resolve(all, TransactionType.PurchaseRequest);

        result.Select(t => t.Name).Should().Equal("Riêng mua hàng");
    }

    [Fact]
    public void Khong_co_loai_gan_rieng_thi_lay_nhom_mac_dinh()
    {
        var all = new[]
        {
            Type("Mặc định A", 0),
            Type("Mặc định B", 1),
            Type("Riêng mua hàng", 2, TransactionType.PurchaseRequest)
        };

        var result = RevenueExpenseRule.Resolve(all, TransactionType.GroupBuyingRequest);

        result.Select(t => t.Name).Should().Equal("Mặc định A", "Mặc định B");
    }

    [Fact]
    public void Sap_theo_thu_tu_roi_ten_trong_nhom()
    {
        var all = new[]
        {
            Type("Zeta", 1, TransactionType.PurchaseRequest),
            Type("Alpha", 1, TransactionType.PurchaseRequest),
            Type("Beta", 0, TransactionType.PurchaseRequest)
        };

        var result = RevenueExpenseRule.Resolve(all, TransactionType.PurchaseRequest);

        result.Select(t => t.Name).Should().Equal("Beta", "Alpha", "Zeta");
    }

    [Fact]
    public void Bo_qua_loai_da_xoa_mem()
    {
        var deleted = Type("Đã xoá", 0, TransactionType.PurchaseRequest);
        deleted.IsDeleted = true;
        var all = new[] { deleted, Type("Còn dùng", 1, TransactionType.PurchaseRequest) };

        var result = RevenueExpenseRule.Resolve(all, TransactionType.PurchaseRequest);

        result.Select(t => t.Name).Should().Equal("Còn dùng");
    }

    [Fact]
    public void Scope_da_xoa_mem_khong_tinh_la_gan_rieng()
    {
        var type = Type("Mặc định", 0);
        type.Scopes.Add(new RevenueExpenseTypeScope
        {
            RevenueExpenseTypeId = type.Id,
            TransactionType = TransactionType.PurchaseRequest,
            IsDeleted = true
        });

        var result = RevenueExpenseRule.Resolve(new[] { type }, TransactionType.PurchaseRequest);

        result.Select(t => t.Name).Should().Equal("Mặc định");
    }
}

/// <summary>
/// Chốt chi phí phát sinh của bản khai bằng tổng các dòng: đây là số bị trừ khi ra doanh thu thực nhận.
/// </summary>
public class RevenueExpenseTotalTests
{
    [Fact]
    public void Chi_phi_bang_tong_cac_dong()
    {
        var lines = new List<TransactionExpenseRequest>
        {
            new() { Name = "Vận chuyển", Amount = 100_000m },
            new() { Name = "Lưu kho", Amount = 250_000m }
        };

        RevenueExpenseHelper.Total(lines).Should().Be(350_000m);
    }

    [Fact]
    public void Khong_co_dong_nao_thi_chi_phi_bang_khong()
    {
        RevenueExpenseHelper.Total(new List<TransactionExpenseRequest>()).Should().Be(0m);
        RevenueExpenseHelper.Total(null).Should().Be(0m);
    }

    [Fact]
    public void Chi_phi_cong_dung_vao_so_thuc_nhan()
    {
        var lines = new List<TransactionExpenseRequest>
        {
            new() { Name = "A", Amount = 200_000m },
            new() { Name = "B", Amount = 300_000m }
        };

        var breakdown = RevenueCalculator.Calculate(10_000_000m, taxIncluded: false, taxPercent: 0m,
            commissions: null, extraCost: RevenueExpenseHelper.Total(lines));

        breakdown.ExtraCost.Should().Be(500_000m);
        breakdown.ActualRevenue.Should().Be(9_500_000m);
    }
}
