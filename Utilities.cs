using System.Windows;

namespace SalaryCalculatorApp;

public class Utilities
{
    public static decimal ParseDecimal(string s) =>
        decimal.TryParse(s, out var v) ? v : 0.0m;

    /// <summary>
    /// 空字符串视为 0（合法）；非空但无法解析时返回 false。
    /// </summary>
    public static bool TryParseAllowEmpty(string s, out decimal value)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            value = 0.0m;
            return true;
        }

        return decimal.TryParse(s, out value);
    }

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