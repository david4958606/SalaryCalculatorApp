using OfficeOpenXml;
using SalaryCalculatorApp.Models;

namespace SalaryCalculatorApp.Services;

public class ExcelService : IExcelService
{
    public Task<byte[]> ExportToExcelAsync(List<DetailLine> breakdown, string title)
    {
        ArgumentNullException.ThrowIfNull(breakdown);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        using var package = new ExcelPackage();
        package.Workbook.Properties.Title = title;
        package.Workbook.Properties.Author = "SalaryCalculatorApp by David Wang";
        package.Workbook.Properties.Created = DateTime.Now;
        package.Workbook.Properties.Comments = "导出自 SalaryCalculatorApp";

        var worksheet = package.Workbook.Worksheets.Add(title);
        worksheet.Cells.Style.Font.Name = "Microsoft YaHei";

        worksheet.Cells[1, 1].Value = "项目";
        worksheet.Cells[1, 2].Value = "金额";

        for (var i = 0; i < breakdown.Count; i++)
        {
            worksheet.Cells[i + 2, 1].Value = breakdown[i].Label;
            worksheet.Cells[i + 2, 2].Value = breakdown[i].Amount;
        }

        if (breakdown.Count > 0)
        {
            worksheet.Cells[2, 2, breakdown.Count + 1, 2].Style.Numberformat.Format = "\"¥\"#,##0.00";
        }

        worksheet.Cells.AutoFitColumns();
        return Task.FromResult(package.GetAsByteArray());
    }
}
