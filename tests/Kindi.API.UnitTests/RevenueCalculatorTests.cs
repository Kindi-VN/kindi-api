namespace Kindi.API.UnitTests;

using FluentAssertions;
using Kindi.API.Domain.Enums;
using Kindi.API.Domain.Rules;
using Xunit;

/// <summary>
/// Chốt phép tính doanh thu giao dịch: thứ tự thuế rồi mới tới hoa hồng, làm tròn tới đồng, và trường
/// hợp số nhập đã gồm thuế phải bóc ngược chứ không trừ thuế hai lần. Sai một đồng ở đây là sai tiền
/// thật nên mọi nhánh đều có test.
/// </summary>
public class RevenueCalculatorTests
{
    private static readonly RevenueCalculator.CommissionRate Referrer = new(CommissionBeneficiary.Referrer, 5m);
    private static readonly RevenueCalculator.CommissionRate Partner = new(CommissionBeneficiary.Partner, 2m);

    [Fact]
    public void Doanh_thu_chua_gom_thue_thi_thue_tinh_tren_so_nhap()
    {
        var result = RevenueCalculator.Calculate(10_000_000m, taxIncluded: false, taxPercent: 10m,
            new[] { Referrer }, extraCost: 0m);

        result.TaxAmount.Should().Be(1_000_000m);
        result.NetRevenue.Should().Be(9_000_000m);
        result.TotalCommission.Should().Be(450_000m, "hoa hồng 5% tính trên doanh thu sau thuế");
        result.ActualRevenue.Should().Be(8_550_000m);
    }

    [Fact]
    public void Doanh_thu_da_gom_thue_thi_boc_nguoc_ra_tien_thue()
    {
        var result = RevenueCalculator.Calculate(11_000_000m, taxIncluded: true, taxPercent: 10m,
            Array.Empty<RevenueCalculator.CommissionRate>(), extraCost: 0m);

        result.TaxAmount.Should().Be(1_000_000m, "11.000.000 gồm 10% thuế thì thuế là 1.000.000");
        result.NetRevenue.Should().Be(10_000_000m);
        result.ActualRevenue.Should().Be(10_000_000m);
    }

    [Fact]
    public void Hoa_hong_cua_nhieu_ben_cong_lai_va_tru_tiep_chi_phi()
    {
        var result = RevenueCalculator.Calculate(10_000_000m, taxIncluded: false, taxPercent: 10m,
            new[] { Referrer, Partner }, extraCost: 200_000m);

        result.Commissions.Should().HaveCount(2);
        result.Commissions[0].Amount.Should().Be(450_000m);
        result.Commissions[1].Amount.Should().Be(180_000m);
        result.TotalCommission.Should().Be(630_000m);
        result.ExtraCost.Should().Be(200_000m);
        result.ActualRevenue.Should().Be(8_170_000m);
    }

    [Fact]
    public void Khong_co_thue_thi_doanh_thu_sau_thue_bang_so_nhap()
    {
        var result = RevenueCalculator.Calculate(2_500_000m, taxIncluded: false, taxPercent: 0m,
            Array.Empty<RevenueCalculator.CommissionRate>(), extraCost: 0m);

        result.TaxAmount.Should().Be(0m);
        result.NetRevenue.Should().Be(2_500_000m);
    }

    [Theory]
    [InlineData(5, 10, 1, 4)]          // 0,5 đồng làm tròn nửa lên thành 1
    [InlineData(1000, 33.335, 333, 667)]
    [InlineData(999, 10, 100, 899)]
    public void Lam_tron_tung_buoc_toi_dong(decimal gross, decimal taxPercent, decimal tax, decimal net)
    {
        var result = RevenueCalculator.Calculate(gross, taxIncluded: false, taxPercent,
            Array.Empty<RevenueCalculator.CommissionRate>(), extraCost: 0m);

        result.TaxAmount.Should().Be(tax);
        result.NetRevenue.Should().Be(net);
    }

    [Fact]
    public void Chi_phi_lon_hon_phan_con_lai_thi_thuc_nhan_am_van_tinh_duoc()
    {
        var result = RevenueCalculator.Calculate(1_000_000m, taxIncluded: false, taxPercent: 0m,
            Array.Empty<RevenueCalculator.CommissionRate>(), extraCost: 1_500_000m);

        result.ActualRevenue.Should().Be(-500_000m);
    }

    [Fact]
    public void So_am_hoac_ty_le_sai_thi_chan_ngay()
    {
        var act1 = () => RevenueCalculator.Calculate(-1m, false, 0m, null, 0m);
        var act2 = () => RevenueCalculator.Calculate(1_000m, false, 120m, null, 0m);
        var act3 = () => RevenueCalculator.Calculate(1_000m, false, 10m,
            new[] { new RevenueCalculator.CommissionRate(CommissionBeneficiary.Partner, 150m) }, 0m);
        var act4 = () => RevenueCalculator.Calculate(1_000m, false, 10m, null, -5m);

        act1.Should().Throw<ArgumentOutOfRangeException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
        act3.Should().Throw<ArgumentOutOfRangeException>();
        act4.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Thue_bang_100_phan_tram_va_so_nhap_da_gom_thue_khong_chia_cho_khong()
    {
        var result = RevenueCalculator.Calculate(500_000m, taxIncluded: true, taxPercent: 100m,
            Array.Empty<RevenueCalculator.CommissionRate>(), extraCost: 0m);

        result.TaxAmount.Should().Be(250_000m);
        result.NetRevenue.Should().Be(250_000m);
    }
}
