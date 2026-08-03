using OfficeOpenXml;
using SalaryCalculatorApp.Models;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace SalaryCalculatorApp.Services;

public class ExcelService : IExcelService
{
    private const string OvertimeSheetName = "加班记录";

    public Task<byte[]> ExportToExcelAsync(
        List<DetailLine> breakdown,
        string title,
        IReadOnlyCollection<DailyRecord>? dailyRecords = null)
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

        if (dailyRecords is not null)
        {
            var recordsWorksheet = package.Workbook.Worksheets.Add(OvertimeSheetName);
            recordsWorksheet.Cells.Style.Font.Name = "Microsoft YaHei";
            recordsWorksheet.Cells[1, 1].Value = "日期";
            recordsWorksheet.Cells[1, 2].Value = "加班时长";
            recordsWorksheet.Cells[1, 3].Value = "项目奖时长";
            recordsWorksheet.Cells[1, 4].Value = "日期类型";

            var exportedRecords = dailyRecords
                .Where(record => record.OvertimeHours != 0 || record.ProjectHours != 0)
                .OrderBy(record => record.Date)
                .ToList();
            for (var i = 0; i < exportedRecords.Count; i++)
            {
                var record = exportedRecords[i];
                recordsWorksheet.Cells[i + 2, 1].Value = record.Date;
                recordsWorksheet.Cells[i + 2, 1].Style.Numberformat.Format = "yyyy-mm-dd";
                recordsWorksheet.Cells[i + 2, 2].Value = record.OvertimeHours;
                recordsWorksheet.Cells[i + 2, 3].Value = record.ProjectHours;
                recordsWorksheet.Cells[i + 2, 4].Value = GetDayType(record);
            }

            recordsWorksheet.Cells.AutoFitColumns();
        }

        return Task.FromResult(package.GetAsByteArray());
    }

    public Task<IReadOnlyList<DailyRecord>> ImportOvertimeAsync(byte[] workbookBytes, DateTime targetMonth)
    {
        ArgumentNullException.ThrowIfNull(workbookBytes);
        if (workbookBytes.Length == 0)
        {
            throw new InvalidDataException("Excel 文件内容为空。");
        }

        using var stream = new MemoryStream(workbookBytes);
        using var package = new ExcelPackage(stream);
        var recordsWorksheet = package.Workbook.Worksheets[OvertimeSheetName];
        var records = recordsWorksheet is null
            ? ImportLegacyBreakdown(package, targetMonth)
            : ImportStructuredRecords(recordsWorksheet, targetMonth);

        // 7.2.0 导出的结构化工作表尚无“日期类型”列，尝试从工资明细文字补回。
        if (recordsWorksheet is not null && recordsWorksheet.Cells[1, 4].Text.Trim() != "日期类型")
        {
            ApplyLegacyDayTypes(package, targetMonth, records);
        }

        return Task.FromResult<IReadOnlyList<DailyRecord>>(records);
    }

    private static List<DailyRecord> ImportStructuredRecords(ExcelWorksheet worksheet, DateTime targetMonth)
    {
        if (worksheet.Dimension is null ||
            worksheet.Cells[1, 1].Text.Trim() != "日期" ||
            worksheet.Cells[1, 2].Text.Trim() != "加班时长")
        {
            throw new InvalidDataException("“加班记录”工作表格式不正确。");
        }

        var records = new List<DailyRecord>();
        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            if (string.IsNullOrWhiteSpace(worksheet.Cells[row, 1].Text))
            {
                continue;
            }

            if (!TryReadDate(worksheet.Cells[row, 1], out var date) ||
                !TryReadDecimal(worksheet.Cells[row, 2], out var overtimeHours) ||
                !TryReadDecimal(worksheet.Cells[row, 3], out var projectHours))
            {
                throw new InvalidDataException($"“加班记录”第 {row} 行包含无效数据。");
            }

            ValidateRecord(date, overtimeHours, projectHours, targetMonth, row);
            var record = CreateRecord(date, overtimeHours, projectHours);
            if (worksheet.Cells[1, 4].Text.Trim() == "日期类型")
            {
                ApplyDayType(record, worksheet.Cells[row, 4].Text.Trim(), row);
            }

            records.Add(record);
        }

        return records;
    }

    private static List<DailyRecord> ImportLegacyBreakdown(ExcelPackage package, DateTime targetMonth)
    {
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet?.Dimension is null)
        {
            throw new InvalidDataException("Excel 文件中没有可导入的工作表。");
        }

        var pattern = CreateLegacyPattern();
        var byDate = new Dictionary<DateTime, DailyRecord>();
        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var match = pattern.Match(worksheet.Cells[row, 1].Text.Trim());
            if (!match.Success)
            {
                continue;
            }

            var date = new DateTime(
                targetMonth.Year,
                int.Parse(match.Groups["month"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["day"].Value, CultureInfo.InvariantCulture));
            var hours = decimal.Parse(match.Groups["hours"].Value, CultureInfo.InvariantCulture);
            ValidateRecord(date, hours, 0, targetMonth, row);

            if (!byDate.TryGetValue(date, out var record))
            {
                record = CreateRecord(date, 0, 0);
                byDate.Add(date, record);
            }

            ApplyDayType(record, match.Groups["dayType"].Value, row);

            if (match.Groups["type"].Value == "加班")
                record.OvertimeHours = hours;
            else
                record.ProjectHours = hours;
        }

        if (byDate.Count == 0)
        {
            throw new InvalidDataException("未找到可导入的加班记录。请使用本应用导出的 Excel 文件。");
        }

        return byDate.Values.OrderBy(record => record.Date).ToList();
    }

    private static void ApplyLegacyDayTypes(
        ExcelPackage package,
        DateTime targetMonth,
        IReadOnlyCollection<DailyRecord> records)
    {
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet?.Dimension is null)
        {
            return;
        }

        var recordsByDate = records.ToDictionary(record => record.Date.Date);
        var pattern = CreateLegacyPattern();
        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var match = pattern.Match(worksheet.Cells[row, 1].Text.Trim());
            if (!match.Success)
            {
                continue;
            }

            var date = new DateTime(
                targetMonth.Year,
                int.Parse(match.Groups["month"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["day"].Value, CultureInfo.InvariantCulture));
            if (recordsByDate.TryGetValue(date, out var record))
            {
                ApplyDayType(record, match.Groups["dayType"].Value, row);
            }
        }
    }

    private static Regex CreateLegacyPattern() => new(
        @"^(?<month>\d{1,2})-(?<day>\d{1,2})\s+(?<type>加班|项目奖)\((?<dayType>假日|周末|工作日)\)\s+(?<hours>\d+(?:\.\d+)?)\s*小时$",
        RegexOptions.CultureInvariant);

    private static string GetDayType(DailyRecord record)
    {
        if (record.IsWorkday)
            return "工作日";
        if (record.IsHoliday)
            return "假日";
        return record.IsWeekend || record.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            ? "周末"
            : "工作日";
    }

    private static void ApplyDayType(DailyRecord record, string dayType, int row)
    {
        record.IsHoliday = false;
        record.IsWorkday = false;
        record.IsWeekend = record.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        switch (dayType)
        {
            case "假日":
                record.IsHoliday = true;
                break;
            case "周末":
                record.IsWeekend = true;
                break;
            case "工作日":
                if (record.IsWeekend)
                    record.IsWorkday = true;
                break;
            case "":
                break;
            default:
                throw new InvalidDataException($"第 {row} 行日期类型“{dayType}”无效。");
        }
    }

    private static bool TryReadDate(ExcelRange cell, out DateTime value)
    {
        if (cell.Value is DateTime date)
        {
            value = date.Date;
            return true;
        }

        if (cell.Value is double serialDate)
        {
            try
            {
                value = DateTime.FromOADate(serialDate).Date;
                return true;
            }
            catch (ArgumentException)
            {
                // 继续尝试按显示文本解析，以便给调用方统一返回格式错误。
            }
        }

        return DateTime.TryParse(cell.Text, CultureInfo.CurrentCulture, DateTimeStyles.None, out value) ||
               DateTime.TryParseExact(cell.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    private static bool TryReadDecimal(ExcelRange cell, out decimal value)
    {
        if (string.IsNullOrWhiteSpace(cell.Text))
        {
            value = 0;
            return true;
        }

        return decimal.TryParse(cell.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) ||
               decimal.TryParse(cell.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static void ValidateRecord(DateTime date, decimal overtimeHours, decimal projectHours, DateTime targetMonth, int row)
    {
        if (date.Year != targetMonth.Year || date.Month != targetMonth.Month)
            throw new InvalidDataException($"第 {row} 行日期 {date:yyyy-MM-dd} 不属于当前月份 {targetMonth:yyyy-MM}。");
        if (overtimeHours < 0 || projectHours < 0 || overtimeHours > 24 || projectHours > 24)
            throw new InvalidDataException($"第 {row} 行时长必须在 0 到 24 小时之间。");
    }

    private static DailyRecord CreateRecord(DateTime date, decimal overtimeHours, decimal projectHours) => new()
    {
        Date = date.Date,
        OvertimeHours = overtimeHours,
        ProjectHours = projectHours,
        IsWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
    };
}
