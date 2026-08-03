using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SalaryCalculatorApp.Models;
using SalaryCalculatorApp.Services;

namespace SalaryCalculatorApp.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly IExcelService _excelService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IMessageService _messageService;

    private List<DailyRecord> _dailyRecords;

    [ObservableProperty]
    private string baseSalaryText = string.Empty;

    [ObservableProperty]
    private string performanceSalaryText = string.Empty;

    [ObservableProperty]
    private string insuranceBaseText = string.Empty;

    [ObservableProperty]
    private string grandTotalPrePayTaxText = "0";

    [ObservableProperty]
    private bool isProbation;

    [ObservableProperty]
    private bool fullMonth;

    [ObservableProperty]
    private bool fullQuarter;

    [ObservableProperty]
    private bool grandTotalEnabled;

    [ObservableProperty]
    private string pensionInsuranceRateText = "8%";

    [ObservableProperty]
    private string medicalInsuranceRateText = "2%";

    [ObservableProperty]
    private string unemploymentInsuranceRateText = "0.5%";

    [ObservableProperty]
    private string workInjuryInsuranceRateText = "0%";

    [ObservableProperty]
    private string maternityInsuranceRateText = "0%";

    [ObservableProperty]
    private string otherInsuranceRateText = "0%";

    [ObservableProperty]
    private string housingFundRateText = "12%";

    [ObservableProperty]
    private string corporatePensionRateText = "0%";

    [ObservableProperty]
    private string insuranceAddonText = "3";

    [ObservableProperty]
    private string transportationSubsidyText = "600";

    [ObservableProperty]
    private string otherSubsidyText = "0";

    [ObservableProperty]
    private string projectBonusCoefficientText = "0.7";

    [ObservableProperty]
    private string performanceRewardText = "0";

    [ObservableProperty]
    private string preTaxAdjustmentText = "0";

    [ObservableProperty]
    private string postTaxAdjustmentText = "0";

    [ObservableProperty]
    private string specialAdditionalReductionText = "0";

    [ObservableProperty]
    private string resultText = string.Empty;

    [ObservableProperty]
    private DateTime displayMonth;

    [ObservableProperty]
    private DateTime? selectedDate;

    [ObservableProperty]
    private bool isQuarterMonth;

    public ObservableCollection<DetailLine> DetailLines { get; } = new();

    public MainWindowViewModel(
        IDialogService dialogService,
        IExcelService excelService,
        IFileDialogService fileDialogService,
        IMessageService messageService)
    {
        _dialogService = dialogService;
        _excelService = excelService;
        _fileDialogService = fileDialogService;
        _messageService = messageService;

        DisplayMonth = DateTime.Today;
        SelectedDate = DateTime.Today;
        _dailyRecords = SalaryCalculator.CreateMonthlyRecords(DisplayMonth.Year, DisplayMonth.Month);
        UpdateQuarterState(DisplayMonth);
    }

    partial void OnDisplayMonthChanged(DateTime value)
    {
        _dailyRecords = SalaryCalculator.CreateMonthlyRecords(value.Year, value.Month);
        UpdateQuarterState(value);

        if (SelectedDate is DateTime selected &&
            (selected.Year != value.Year || selected.Month != value.Month))
        {
            SelectedDate = null;
        }
    }

    [RelayCommand]
    private void Calculate()
    {
        RefreshResult();
    }

    [RelayCommand]
    private void EditSelectedDate()
    {
        if (SelectedDate is not DateTime day)
        {
            _messageService.ShowInfo("请先选择一个日期");
            return;
        }

        var record = _dailyRecords.FirstOrDefault(r => r.Date.Date == day.Date);
        if (record == null)
        {
            _messageService.ShowWarning($"未找到 {day:yyyy-MM-dd} 的记录数据，请确认日期是否在当前月份内。");
            return;
        }

        if (_dialogService.ShowOvertimeDialog(record) == true)
        {
            RefreshResult();
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (DetailLines.Count == 0)
        {
            _messageService.ShowWarning("请先计算结果后再导出！");
            return;
        }

        var filePath = await _fileDialogService.ShowSaveFileDialogAsync(
            "Excel 文件 (*.xlsx)|*.xlsx|Excel 备用扩展名 (*.xlsx1)|*.xlsx1",
            ".xlsx",
            $"工资明细-{DisplayMonth:yyyy-MM}");

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            var bytes = await _excelService.ExportToExcelAsync(DetailLines.ToList(), "工资明细", _dailyRecords);
            await File.WriteAllBytesAsync(filePath, bytes);
            _messageService.ShowInfo("导出成功！");
        }
        catch (Exception ex)
        {
            _messageService.ShowError($"导出失败：{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ImportOvertimeAsync()
    {
        var filePath = await _fileDialogService.ShowOpenFileDialogAsync(
            "Excel 文件 (*.xlsx;*.xlsx1)|*.xlsx;*.xlsx1|标准 Excel (*.xlsx)|*.xlsx|Excel 备用扩展名 (*.xlsx1)|*.xlsx1",
            ".xlsx");
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            var bytes = await File.ReadAllBytesAsync(filePath);
            var importedRecords = await _excelService.ImportOvertimeAsync(bytes, DisplayMonth);
            foreach (var imported in importedRecords)
            {
                var target = _dailyRecords.First(record => record.Date.Date == imported.Date.Date);
                target.OvertimeHours = imported.OvertimeHours;
                target.ProjectHours = imported.ProjectHours;
                target.IsHoliday = imported.IsHoliday;
                target.IsWorkday = imported.IsWorkday;
                target.IsWeekend = imported.IsWeekend;
            }

            RefreshResult();
            _messageService.ShowInfo($"成功导入 {importedRecords.Count} 天的加班记录！");
        }
        catch (Exception ex)
        {
            _messageService.ShowError($"导入失败：{ex.Message}");
        }
    }

    private void RefreshResult()
    {
        // 工资数值留空自动视为 0，仅在输入了无法解析的内容时报错
        if (!Utilities.TryParseAllowEmpty(BaseSalaryText, out var baseSalary) ||
            !Utilities.TryParseAllowEmpty(PerformanceSalaryText, out var perfSalary))
        {
            _messageService.ShowWarning("请输入有效的工资数值！");
            return;
        }

        // 补贴留空自动视为 0，仅在输入了无法解析的内容时报错
        if (!Utilities.TryParseAllowEmpty(TransportationSubsidyText, out var transportationSubsidy) ||
            !Utilities.TryParseAllowEmpty(OtherSubsidyText, out var otherSubsidy))
        {
            _messageService.ShowWarning("请输入有效的补贴数值！");
            return;
        }

        if (!decimal.TryParse(ProjectBonusCoefficientText, out var projectBonusCoefficient) ||
            projectBonusCoefficient <= 0)
        {
            _messageService.ShowWarning("请输入有效的项目奖系数！");
            return;
        }

        if (!decimal.TryParse(InsuranceBaseText, out var insuranceBase))
        {
            insuranceBase = baseSalary + perfSalary;
            InsuranceBaseText = insuranceBase.ToString("F2");
        }

        decimal insuranceRate = 0;
        insuranceRate += Utilities.ParseRate(PensionInsuranceRateText);
        insuranceRate += Utilities.ParseRate(MedicalInsuranceRateText);
        insuranceRate += Utilities.ParseRate(UnemploymentInsuranceRateText);
        insuranceRate += Utilities.ParseRate(WorkInjuryInsuranceRateText);
        insuranceRate += Utilities.ParseRate(MaternityInsuranceRateText);
        insuranceRate += Utilities.ParseRate(OtherInsuranceRateText);
        insuranceRate += Utilities.ParseRate(HousingFundRateText);
        insuranceRate += Utilities.ParseRate(CorporatePensionRateText);

        var result = SalaryCalculator.CalculateResult(
            DisplayMonth,
            baseSalary,
            perfSalary,
            IsProbation,
            FullMonth,
            FullQuarter,
            transportationSubsidy,
            otherSubsidy,
            projectBonusCoefficient,
            insuranceBase,
            insuranceRate,
            Utilities.ParseDecimal(InsuranceAddonText),
            Utilities.ParseDecimal(GrandTotalPrePayTaxText),
            GrandTotalEnabled,
            Utilities.ParseDecimal(SpecialAdditionalReductionText),
            _dailyRecords,
            Utilities.ParseDecimal(PerformanceRewardText),
            Utilities.ParseDecimal(PreTaxAdjustmentText),
            Utilities.ParseDecimal(PostTaxAdjustmentText));

        foreach (var warning in result.Warnings)
        {
            _messageService.ShowWarning(warning);
        }

        ResultText = result.ToString();
        if (!GrandTotalEnabled)
        {
            ResultText += "\n\n由于未采用累计计算个税，实际工资可能高于计算值。";
        }

        DetailLines.Clear();
        foreach (var item in result.Breakdown)
        {
            DetailLines.Add(item);
        }
    }

    private void UpdateQuarterState(DateTime month)
    {
        IsQuarterMonth = month.Month % 3 == 0;
        if (!IsQuarterMonth)
        {
            FullQuarter = false;
        }
    }
}
