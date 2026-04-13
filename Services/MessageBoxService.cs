using System.Windows;

namespace SalaryCalculatorApp.Services;

public class MessageBoxService : IMessageService
{
    public void ShowInfo(string message)
    {
        MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowWarning(string message)
    {
        Utilities.ShowWarning(message);
    }

    public void ShowError(string message)
    {
        MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
