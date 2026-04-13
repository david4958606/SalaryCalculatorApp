using SalaryCalculatorApp.Models;
using Xunit;

namespace SalaryCalculatorApp.Tests;

public class SalaryCalculatorTests
{
    [Theory]
    [InlineData(2025, 2, 28)]
    [InlineData(2024, 2, 29)]
    [InlineData(2025, 4, 30)]
    [InlineData(2025, 7, 31)]
    public void CreateMonthlyRecords_GeneratesExpectedDaysAndDates(int year, int month, int expectedDays)
    {
        var records = SalaryCalculator.CreateMonthlyRecords(year, month);

        Assert.Equal(expectedDays, records.Count);
        Assert.Equal(new DateTime(year, month, 1), records[0].Date);
        Assert.Equal(new DateTime(year, month, expectedDays), records[^1].Date);
        Assert.All(records, record =>
        {
            Assert.Equal(0m, record.OvertimeHours);
            Assert.Equal(0m, record.ProjectHours);
        });
    }

    [Theory]
    [InlineData(12000, 0.105, 35, 1295)]
    [InlineData(8500, 0.08, 0, 680)]
    public void CalculateInsurance_ReturnsBaseTimesRatePlusAddon(
        decimal insuranceBase,
        decimal insuranceRate,
        decimal insuranceAddon,
        decimal expected)
    {
        var actual = SalaryCalculator.CalculateInsurance(insuranceBase, insuranceRate, insuranceAddon);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CalculateTax_UsesMonthlyTaxBrackets_WhenGrandTotalDisabled()
    {
        const decimal taxableIncome = 10000m;

        var actual = SalaryCalculator.CalculateTax(taxableIncome, grandTotalEnabled: false);

        Assert.Equal(790m, actual);
    }

    [Fact]
    public void CalculateTax_UsesGrandTotalTaxBrackets_WhenGrandTotalEnabled()
    {
        const decimal currentTaxableIncome = 20000m;
        const decimal grandTotalPrePayTax = 20000m;

        var actual = SalaryCalculator.CalculateTax(currentTaxableIncome, grandTotalPrePayTax, true);

        Assert.Equal(1480m, actual);
    }
}
