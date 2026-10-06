namespace Kindi.API.UnitTests;

using FluentAssertions;
using Kindi.API.Application.Common.Helpers;
using Xunit;

/// <summary>
/// Chốt cách chuẩn hoá khoảng ngày cho bộ lọc / thao tác xoá: mốc kết thúc gửi dạng NGÀY phải phủ hết
/// ngày đó, còn gửi kèm giờ thì giữ nguyên.
/// </summary>
public class DateRangeBoundsTests
{
    [Fact]
    public void Moc_bat_dau_giu_nguyen_thoi_diem()
    {
        var from = new DateTime(2026, 10, 1, 13, 45, 30, DateTimeKind.Utc);

        DateRangeBounds.NormalizeFrom(from).Should().Be(from);
    }

    [Fact]
    public void Moc_ket_thuc_chi_co_ngay_thi_lay_het_ngay_do()
    {
        var to = new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);

        var normalized = DateRangeBounds.NormalizeTo(to);

        normalized!.Value.Date.Should().Be(new DateTime(2026, 10, 6));
        normalized.Value.TimeOfDay.Should().Be(TimeSpan.FromTicks(TimeSpan.TicksPerDay - 1));
    }

    [Fact]
    public void Moc_ket_thuc_co_gio_thi_giu_nguyen()
    {
        var to = new DateTime(2026, 10, 6, 8, 30, 0, DateTimeKind.Utc);

        DateRangeBounds.NormalizeTo(to).Should().Be(to);
    }

    [Fact]
    public void Thieu_dau_vao_thi_tra_null()
    {
        DateRangeBounds.NormalizeFrom(null).Should().BeNull();
        DateRangeBounds.NormalizeTo(null).Should().BeNull();
    }

    [Fact]
    public void Moc_ket_thuc_ngay_cuoi_thang_van_dung()
    {
        var to = new DateTime(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc);

        var normalized = DateRangeBounds.NormalizeTo(to);

        normalized!.Value.Day.Should().Be(31);
        normalized.Value.Month.Should().Be(10);
    }
}
