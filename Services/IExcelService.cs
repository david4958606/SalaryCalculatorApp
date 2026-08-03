using SalaryCalculatorApp.Models;

namespace SalaryCalculatorApp.Services;

public interface IExcelService
{
    Task<byte[]> ExportToExcelAsync(
        List<DetailLine> breakdown,
        string title,
        IReadOnlyCollection<DailyRecord>? dailyRecords = null);

    Task<IReadOnlyList<DailyRecord>> ImportOvertimeAsync(byte[] workbookBytes, DateTime targetMonth);
}
