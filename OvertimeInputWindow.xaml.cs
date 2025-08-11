using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SalaryCalculatorApp.Models;

namespace SalaryCalculatorApp;

/// <summary>
/// OvertimeInputWindow.xaml 的交互逻辑
/// </summary>
public partial class OvertimeInputWindow : Window
{
    private readonly DailyRecord _record;

    public OvertimeInputWindow(DailyRecord record)
    {
        InitializeComponent();
        _record = record;

        // 将 DailyRecord 作为 DataContext，直接双向绑定
        DataContext = _record;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        // 这里可以再做一次格式/数值校验
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}