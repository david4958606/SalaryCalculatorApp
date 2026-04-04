// MainWindow.xaml.cs
// 关联于 MainWindow.xaml 的后台代码

using Microsoft.Win32;
using OfficeOpenXml;
using SalaryCalculatorApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SalaryCalculatorApp;

public partial class MainWindow : Window
{
    // 月度每日记录表 —— 解决“无法解析 _dailyRecords”
    private List<DailyRecord> _dailyRecords;
    private readonly SalaryCalculator _calculator = new();

    public MainWindow()
    {
        InitializeComponent();
        var version = Assembly.GetEntryAssembly()!
            .GetName()
            .Version!
            .ToString();
        Title = $"工资计算器 - 版本 {version}";
        // 设置 ExcelPackage 的 LicenseContext
        ExcelPackage.License.SetNonCommercialPersonal("David Wang");
        UpdateQuarterCheckEnabled();
        // 初始化当前月记录
        var d = DateTime.Today;
        _dailyRecords = SalaryCalculator.CreateMonthlyRecords(d.Year, d.Month);
    }

    private void UpdateQuarterCheckEnabled()
    {
        bool isQuarterMonth = WorkCalendar.DisplayDate.Month % 3 == 0; // 1、4、7、10
        FullQuarterCheck.IsEnabled = isQuarterMonth;
        if (!isQuarterMonth)
            FullQuarterCheck.IsChecked = false; // 禁用时顺带取消勾选，避免误选
    }

    #region 日历交互

    private void Calendar_DisplayDateChanged(object? sender, CalendarDateChangedEventArgs e)
    {
        // 当用户切换月份时，刷新日列表
        var y = WorkCalendar.DisplayDate.Year;
        var m = WorkCalendar.DisplayDate.Month;
        _dailyRecords = SalaryCalculator.CreateMonthlyRecords(y, m);
        UpdateQuarterCheckEnabled();
    }

    private void Calendar_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (WorkCalendar.SelectedDate is not DateTime day) return;
        var record = _dailyRecords.First(r => r.Date == day);
        var dlg = new OvertimeInputWindow(record) { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshResult();
    }

    private void Calendar_GotMouseCapture(object sender, MouseEventArgs e)
    {
        UIElement originalElement = e.OriginalSource as UIElement;
        if (originalElement is CalendarDayButton || originalElement is CalendarItem)
        {
            originalElement.ReleaseMouseCapture();
        }
    }

    #endregion

    #region 计算与导出

    private void OnCalculateClick(object sender, RoutedEventArgs e) => RefreshResult();

    private void RefreshResult()
    {
        if (!decimal.TryParse(BaseSalaryBox.Text, out var baseSalary) ||
            !decimal.TryParse(PerformanceSalaryBox.Text, out var perfSalary))
        {
            Utilities.ShowWarning("请输入有效的工资数值！");
            return;
        }

        if (!decimal.TryParse(TransportationSubsidyBox.Text, out var transportationSubsidy) ||
            !decimal.TryParse(OtherSubsidyBox.Text, out var otherSubsidy))
        {
            Utilities.ShowWarning("请输入有效的补贴数值！");
            return;
        }

        if (!decimal.TryParse(InsuranceBaseBox.Text, out var insuranceBase))
        {
            insuranceBase = baseSalary + perfSalary; // 默认社保基数为工资总额
            InsuranceBaseBox.Text = insuranceBase.ToString("F2");
        }

        // 读取更多设置（此处仅演示关键字段）
        var isProbation = IsProbationCheck.IsChecked == true;
        var fullMonth = FullMonthCheck.IsChecked == true;
        var fullQuarter = FullQuarterCheck.IsChecked == true;
        var month = WorkCalendar.DisplayDate;

        decimal insuranceRate = 0;
        insuranceRate += Utilities.ParseRate(PensionInsuranceRateBox.Text); // 养老保险
        insuranceRate += Utilities.ParseRate(MedicalInsuranceRateBox.Text); // 医疗保险
        insuranceRate += Utilities.ParseRate(UnemploymentInsuranceRateBox.Text); // 失业保险
        insuranceRate += Utilities.ParseRate(WorkInjuryInsuranceRateBox.Text); // 工伤保险
        insuranceRate += Utilities.ParseRate(MaternityInsuranceRateBox.Text); // 生育保险
        insuranceRate += Utilities.ParseRate(OtherInsuranceRateBox.Text); // 其他保险
        insuranceRate += Utilities.ParseRate(HousingFundRateBox.Text); // 住房公积金
        insuranceRate += Utilities.ParseRate(CorporatePensionRateBox.Text); // 企业年金

        var insuranceAddon = Utilities.ParseDecimal(InsuranceAddonBox.Text);

        var grandTotalPrePayTax = Utilities.ParseDecimal(GrandTotalPrePayTaxBox.Text);
        var grandTotalEnabled = GrandTotalCheck.IsChecked == true;

        var otherWaiver = Utilities.ParseDecimal(SpecialAdditionalReductionBox.Text);

        var result = SalaryCalculator.CalculateResult(month,
            baseSalary,
            perfSalary,
            isProbation,
            fullMonth,
            fullQuarter,
            transportationSubsidy,
            otherSubsidy,
            insuranceBase,
            insuranceRate,
            insuranceAddon,
            grandTotalPrePayTax,
            grandTotalEnabled,
            otherWaiver,
            _dailyRecords);


        ResultBox.Text = result.ToString();
        if (!grandTotalEnabled)
        {
            ResultBox.Text += "\n\n由于未采用累计计算个税，实际工资可能高于计算值。";
        }

        DetailGrid.ItemsSource = result.Breakdown;
    }

    private void OnExportExcelClick(object sender, RoutedEventArgs e)
    {
        if (DetailGrid.ItemsSource is not List<DetailLine> result)
        {
            Utilities.ShowWarning("请先计算结果后再导出！");
            return;
        }

        var dlg = new SaveFileDialog { Filter = "Excel 文件 (*.xlsx)|*.xlsx" };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                using var package = new ExcelPackage();
                package.Workbook.Properties.Title = "工资明细";
                package.Workbook.Properties.Author = "SalaryCalculatorApp by David Wang";
                package.Workbook.Properties.Created = DateTime.Now;
                package.Workbook.Properties.Comments = "导出自 SalaryCalculatorApp";
                var worksheet = package.Workbook.Worksheets.Add("工资明细");
                // 设置字体为微软雅黑
                worksheet.Cells.Style.Font.Name = "Microsoft YaHei";
                worksheet.Cells[1, 1].Value = "项目";
                worksheet.Cells[1, 2].Value = "金额";
                for (int i = 0; i < result.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = result[i].Label;
                    worksheet.Cells[i + 2, 2].Value = result[i].Amount;
                }

                // 设置货币格式
                worksheet.Cells[2, 2, result.Count + 1, 2].Style.Numberformat.Format = "\"￥\"#,##0.00";

                worksheet.Cells.AutoFitColumns(); // 自动调整列宽
                package.SaveAs(new System.IO.FileInfo(dlg.FileName));
                MessageBox.Show("导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && !button.IsFocused)
        {
            button.Focus();
        }

        if (WorkCalendar.SelectedDate is not DateTime day)
        {
            MessageBox.Show("请先选择一个日期", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var record = _dailyRecords.FirstOrDefault(r => r.Date.Date == day.Date);
        if (record == null)
        {
            Utilities.ShowWarning($"未找到 {day:yyyy-MM-dd} 的记录数据，请确认日期是否在当前月份内。");
            return;
        }

        var dlg = new OvertimeInputWindow(record) { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshResult();
    }
}