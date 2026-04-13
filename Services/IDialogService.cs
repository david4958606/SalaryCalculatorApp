using SalaryCalculatorApp.Models;

namespace SalaryCalculatorApp.Services;

public interface IDialogService
{
    bool? ShowOvertimeDialog(DailyRecord record);
}
