using SalaryCalculatorApp.Models;

namespace SalaryCalculatorApp.Services;

public class DialogService : IDialogService
{
    private readonly Func<DailyRecord, OvertimeInputWindow> _overtimeInputWindowFactory;

    public DialogService(Func<DailyRecord, OvertimeInputWindow> overtimeInputWindowFactory)
    {
        _overtimeInputWindowFactory = overtimeInputWindowFactory;
    }

    public bool? ShowOvertimeDialog(DailyRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var dialog = _overtimeInputWindowFactory(record);
        return dialog.ShowDialog();
    }
}
