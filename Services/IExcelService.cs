using SalaryCalculatorApp.Models;

namespace SalaryCalculatorApp.Services;

public interface IExcelService
{
    Task<byte[]> ExportToExcelAsync(List<DetailLine> breakdown, string title);
}
