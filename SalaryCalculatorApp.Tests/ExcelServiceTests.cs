using OfficeOpenXml;
using SalaryCalculatorApp.Models;
using SalaryCalculatorApp.Services;
using System.IO;
using Xunit;

namespace SalaryCalculatorApp.Tests;

public class ExcelServiceTests
{
    static ExcelServiceTests()
    {
        ExcelPackage.License.SetNonCommercialPersonal("SalaryCalculatorApp Tests");
    }

    [Fact]
    public async Task ExportThenImport_PreservesOvertimeAndProjectHours()
    {
        var service = new ExcelService();
        var records = new List<DailyRecord>
        {
            new() { Date = new DateTime(2025, 5, 2), OvertimeHours = 3.5m, ProjectHours = 1m },
            new() { Date = new DateTime(2025, 5, 3) }
        };

        var bytes = await service.ExportToExcelAsync([], "工资明细", records);
        var imported = await service.ImportOvertimeAsync(bytes, new DateTime(2025, 5, 1));

        var record = Assert.Single(imported);
        Assert.Equal(new DateTime(2025, 5, 2), record.Date);
        Assert.Equal(3.5m, record.OvertimeHours);
        Assert.Equal(1m, record.ProjectHours);
    }

    [Fact]
    public async Task ImportOvertimeAsync_SupportsLegacyExportFormat()
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("工资明细");
        sheet.Cells[1, 1].Value = "项目";
        sheet.Cells[1, 2].Value = "金额";
        sheet.Cells[2, 1].Value = "05-02 加班(工作日) 3.5 小时";
        sheet.Cells[3, 1].Value = "05-02 项目奖(工作日) 1 小时";

        var service = new ExcelService();
        var imported = await service.ImportOvertimeAsync(
            package.GetAsByteArray(),
            new DateTime(2025, 5, 1));

        var record = Assert.Single(imported);
        Assert.Equal(3.5m, record.OvertimeHours);
        Assert.Equal(1m, record.ProjectHours);
    }

    [Fact]
    public async Task ImportOvertimeAsync_RejectsRecordsFromAnotherMonth()
    {
        var service = new ExcelService();
        var bytes = await service.ExportToExcelAsync(
            [],
            "工资明细",
            [new DailyRecord { Date = new DateTime(2025, 6, 1), OvertimeHours = 2m }]);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            service.ImportOvertimeAsync(bytes, new DateTime(2025, 5, 1)));

        Assert.Contains("不属于当前月份", exception.Message);
    }
}
