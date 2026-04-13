// MainWindow.xaml.cs
// 关联于 MainWindow.xaml 的后台代码

using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using SalaryCalculatorApp.ViewModels;

namespace SalaryCalculatorApp;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        var version = Assembly.GetEntryAssembly()!
            .GetName()
            .Version!
            .ToString();
        Title = $"工资计算器 - 版本 {version}";
    }

    private void Calendar_GotMouseCapture(object sender, MouseEventArgs e)
    {
        if (e.OriginalSource is UIElement originalElement &&
            (originalElement is CalendarDayButton || originalElement is CalendarItem))
        {
            originalElement.ReleaseMouseCapture();
        }
    }
}
