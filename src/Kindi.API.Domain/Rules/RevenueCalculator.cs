namespace Kindi.API.Domain.Rules;

using Kindi.API.Domain.Enums;

/// <summary>
/// Phép tính doanh thu của một giao dịch: từ doanh thu gộp và tỷ lệ thuế ra doanh thu sau thuế, trừ tiếp
/// hoa hồng từng bên rồi trừ chi phí phát sinh để ra số thực nhận.
///
/// Tiền là đồng Việt Nam nên mỗi bước đều làm tròn tới đồng (nửa lên), giống cách máy chủ tính phí rút
/// hoa hồng. Hoa hồng luôn tính trên doanh thu SAU thuế, không tính trên số gộp.
/// </summary>
public static class RevenueCalculator
{
    /// <summary>Một bên nhận hoa hồng kèm tỷ lệ phần trăm tính trên doanh thu sau thuế.</summary>
    public readonly record struct CommissionRate(CommissionBeneficiary Beneficiary, decimal RatePercent);

    /// <summary>Kết quả bóc tách một lần khai doanh thu.</summary>
    public sealed record RevenueBreakdown(
        decimal GrossRevenue,
        decimal TaxAmount,
        decimal NetRevenue,
        IReadOnlyList<CommissionLine> Commissions,
        decimal TotalCommission,
        decimal ExtraCost,
        decimal ActualRevenue);

    /// <summary>Một dòng hoa hồng đã tính tiền.</summary>
    public sealed record CommissionLine(CommissionBeneficiary Beneficiary, decimal RatePercent, decimal Amount);

    /// <summary>
    /// Tính doanh thu sau thuế, hoa hồng từng bên và số thực nhận.
    /// </summary>
    /// <param name="grossRevenue">Doanh thu quản trị viên nhập.</param>
    /// <param name="taxIncluded">Số nhập đã bao gồm thuế hay chưa.</param>
    /// <param name="taxPercent">Tỷ lệ thuế (%), trong khoảng 0-100.</param>
    /// <param name="commissions">Tỷ lệ hoa hồng từng bên, tính trên doanh thu sau thuế.</param>
    /// <param name="extraCost">Chi phí phát sinh ngoài thuế và hoa hồng.</param>
    public static RevenueBreakdown Calculate(
        decimal grossRevenue,
        bool taxIncluded,
        decimal taxPercent,
        IEnumerable<CommissionRate>? commissions,
        decimal extraCost)
    {
        if (grossRevenue < 0)
            throw new ArgumentOutOfRangeException(nameof(grossRevenue), "Doanh thu không được âm.");
        if (taxPercent < 0 || taxPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(taxPercent), "Tỷ lệ thuế phải trong khoảng 0-100%.");
        if (extraCost < 0)
            throw new ArgumentOutOfRangeException(nameof(extraCost), "Chi phí phát sinh không được âm.");

        var gross = Round(grossRevenue);

        // Số nhập đã gồm thuế thì phải bóc ngược ra tiền thuế, nếu tính thẳng sẽ trừ thuế hai lần.
        var tax = taxIncluded
            ? Round(gross * taxPercent / (100 + taxPercent))
            : Round(gross * taxPercent / 100);
        var net = gross - tax;

        var lines = new List<CommissionLine>();
        var totalCommission = 0m;
        foreach (var item in commissions ?? Enumerable.Empty<CommissionRate>())
        {
            if (item.RatePercent < 0 || item.RatePercent > 100)
                throw new ArgumentOutOfRangeException(nameof(commissions), "Tỷ lệ hoa hồng phải trong khoảng 0-100%.");

            var amount = Round(net * item.RatePercent / 100);
            lines.Add(new CommissionLine(item.Beneficiary, item.RatePercent, amount));
            totalCommission += amount;
        }

        var cost = Round(extraCost);
        return new RevenueBreakdown(gross, tax, net, lines, totalCommission, cost, net - totalCommission - cost);
    }

    private static decimal Round(decimal value) => decimal.Round(value, 0, MidpointRounding.AwayFromZero);
}
