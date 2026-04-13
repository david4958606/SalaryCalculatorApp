using SalaryCalculatorApp.Models;
using System.Windows;

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
        var mainWindow = Application.Current?.MainWindow;
        if (mainWindow != null && !ReferenceEquals(mainWindow, dialog))
        {
            dialog.Owner = mainWindow;
        }

        return dialog.ShowDialog();
    }
}
