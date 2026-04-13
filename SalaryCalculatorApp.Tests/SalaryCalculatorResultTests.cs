using SalaryCalculatorApp.Models;
using Xunit;

namespace SalaryCalculatorApp.Tests;

public class SalaryCalculatorResultTests
{
    [Fact]
    public void CalculateResult_NonProbationWithGrandTotalTax_KeepsCurrentBehaviorAndStructure()
    {
        var month = new DateTime(2025, 3, 1);
        var daily = new List<DailyRecord>
        {
            new() { Date = new DateTime(2025, 3, 3), OvertimeHours = 2m, ProjectHours = 1m },
            new() { Date = new DateTime(2025, 3, 8), OvertimeHours = 4m, ProjectHours = 2m }
        };

        var result = SalaryCalculator.CalculateResult(
            month,
            baseSalary: 10000m,
            perfSalary: 2000m,
            isProbation: false,
            fullMonth: true,
            fullQuarter: true,
            transportationSubsidy: 200m,
            otherSubsidy: 100m,
            projectBonusCoefficient: 1.5m,
            insuranceBase: 12000m,
            insuranceRate: 0.1m,
            insuranceAddon: 50m,
            grandTotalPrePayTax: 30000m,
            grandTotalEnabled: true,
            otherWaiver: 1000m,
            daily: daily);

        var hourly = 10000m / 21.75m / 8m;
        var overtimePay = (2m * hourly * 1.5m) + (4m * hourly * 2m);
        var projectPay = (1m * hourly * 2.25m) + (2m * hourly * 3m);
        var expectedGross = 10000m + 2000m + 200m + 100m + 300m + 1000m + overtimePay + projectPay;
        var expectedInsurance = (12000m * 0.1m) + 50m;
        var currentTaxableIncome = expectedGross - expectedInsurance - 5000m - 1000m;
        var taxAlreadyPaid = 30000m * 0.03m;
        var totalCumulativeTax = ((30000m + currentTaxableIncome) * 0.1m) - 2520m;
        var expectedTax = totalCumulativeTax - taxAlreadyPaid;
        var expectedDeductions = expectedInsurance + expectedTax;

        Assert.Equal(expectedGross, result.GrossIncome);
        Assert.Equal(expectedDeductions, result.Deductions);
        Assert.Equal(expectedGross - expectedDeductions, result.NetIncome);

        Assert.Collection(
            result.Breakdown.Take(7),
            item => Assert.Equal("基础工资", item.Label),
            item => Assert.Equal("绩效工资", item.Label),
            item => Assert.Equal("基础时薪", item.Label),
            item => Assert.Equal("交通补贴", item.Label),
            item => Assert.Equal("其他补贴", item.Label),
            item => Assert.Equal("当月全勤奖", item.Label),
            item => Assert.Equal("季度全勤奖", item.Label));

        Assert.Contains(result.Breakdown, item => item.Label == "03-03 加班(工作日) 2 小时");
        Assert.Contains(result.Breakdown, item => item.Label == "03-03 项目奖(工作日) 1 小时");
        Assert.Contains(result.Breakdown, item => item.Label == "03-08 加班(周末) 4 小时");
        Assert.Contains(result.Breakdown, item => item.Label == "03-08 项目奖(周末) 2 小时");
        Assert.Contains(result.Breakdown, item => item.Label == "加班费" && item.Amount == overtimePay);
        Assert.Contains(result.Breakdown, item => item.Label == "项目奖" && item.Amount == projectPay);
        Assert.Contains(result.Breakdown, item => item.Label == "累计预扣预缴应纳税所得额" && item.Amount == 30000m + currentTaxableIncome);
        Assert.Contains(result.Breakdown, item => item.Label == "累计预扣预缴个税" && item.Amount == taxAlreadyPaid);
        Assert.Contains(result.Breakdown, item => item.Label == "个税" && item.Amount == -expectedTax);
        Assert.Equal("实发工资", result.Breakdown[^1].Label);
        Assert.Equal(result.NetIncome, result.Breakdown[^1].Amount);
    }

    [Fact]
    public void CalculateResult_ProbationWithoutGrandTotalTax_KeepsCurrentBehaviorAndStructure()
    {
        var month = new DateTime(2024, 2, 1);
        var daily = new List<DailyRecord>
        {
            new() { Date = new DateTime(2024, 2, 10), OvertimeHours = 3m, ProjectHours = 1m, IsHoliday = true }
        };

        var result = SalaryCalculator.CalculateResult(
            month,
            baseSalary: 8000m,
            perfSalary: 1000m,
            isProbation: true,
            fullMonth: false,
            fullQuarter: false,
            transportationSubsidy: 500m,
            otherSubsidy: 200m,
            projectBonusCoefficient: 2m,
            insuranceBase: 9000m,
            insuranceRate: 0.12m,
            insuranceAddon: 0m,
            grandTotalPrePayTax: 0m,
            grandTotalEnabled: false,
            otherWaiver: 500m,
            daily: daily);

        var hourly = 6400m / 21.75m / 8m;
        var overtimePay = 3m * hourly * 3m;
        var projectPay = 1m * hourly * 6m;
        var expectedGross = 6400m + 800m + overtimePay + projectPay;
        var expectedInsurance = 9000m * (0.12m * 0.8m);
        var currentTaxableIncome = expectedGross - expectedInsurance - 500m - 5000m;
        var expectedTax = currentTaxableIncome * 0.03m;
        var expectedDeductions = expectedInsurance + expectedTax;

        Assert.Equal(expectedGross, result.GrossIncome);
        Assert.Equal(expectedDeductions, result.Deductions);
        Assert.Equal(expectedGross - expectedDeductions, result.NetIncome);

        Assert.DoesNotContain(result.Breakdown, item => item.Label == "交通补贴");
        Assert.DoesNotContain(result.Breakdown, item => item.Label == "其他补贴");
        Assert.Contains(result.Breakdown, item => item.Label == "02-10 加班(假日) 3 小时" && item.Amount == overtimePay);
        Assert.Contains(result.Breakdown, item => item.Label == "02-10 项目奖(假日) 1 小时" && item.Amount == projectPay);
        Assert.Contains(result.Breakdown, item => item.Label == "五险二金" && item.Amount == -expectedInsurance);
        Assert.Contains(result.Breakdown, item => item.Label == "专项附加扣除" && item.Amount == -500m);
        Assert.Contains(result.Breakdown, item => item.Label == "当月应纳税所得额" && item.Amount == currentTaxableIncome);
        Assert.Contains(result.Breakdown, item => item.Label == "个税" && item.Amount == -expectedTax);
        Assert.Equal("实发工资", result.Breakdown[^1].Label);
        Assert.Equal(result.NetIncome, result.Breakdown[^1].Amount);
    }

    [Fact]
    public void CalculateResult_WhenInputTriggersWarnings_ReturnsWarningsInsteadOfShowingUi()
    {
        var month = new DateTime(2025, 5, 1);
        var daily = new List<DailyRecord>
        {
            new() { Date = new DateTime(2025, 5, 1), OvertimeHours = 20m, ProjectHours = 0m },
            new() { Date = new DateTime(2025, 5, 2), OvertimeHours = 20m, ProjectHours = 0m },
            new() { Date = new DateTime(2025, 5, 3), OvertimeHours = 25m, ProjectHours = 0m }
        };

        var result = SalaryCalculator.CalculateResult(
            month,
            baseSalary: 10000m,
            perfSalary: 2000m,
            isProbation: false,
            fullMonth: false,
            fullQuarter: false,
            transportationSubsidy: 100m,
            otherSubsidy: 50m,
            projectBonusCoefficient: 1m,
            insuranceBase: 0m,
            insuranceRate: 0.1m,
            insuranceAddon: 0m,
            grandTotalPrePayTax: 0m,
            grandTotalEnabled: false,
            otherWaiver: 0m,
            daily: daily);

        Assert.Equal(2, result.Warnings.Count);
        Assert.Equal("加班时长超过 36 小时，请检查记录！", result.Warnings[0]);
        Assert.Equal("社保基数必须大于 0，请检查设置！", result.Warnings[1]);
    }
}
