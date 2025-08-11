using System.Windows;

namespace SalaryCalculatorApp;

public class Utilities
{
    public static decimal ParseDecimal(string s) =>
        decimal.TryParse(s, out var v) ? v : 0.0m;

    public static decimal ParseRate(string s) =>
        decimal.TryParse(s.TrimEnd('%'), out var v) ? v / 100.0m : 0.0m;

    public static int ParseInt(string s) =>
        int.TryParse(s, out var v) ? v : 0;

    //弹出警告框
    public static void ShowWarning(string message)
    {
        MessageBox.Show(message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}