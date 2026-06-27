using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SalaryCalculatorApp.Models;
using SalaryCalculatorApp.Services;

namespace SalaryCalculatorApp.ViewModels;

public partial class OvertimeInputViewModel : ObservableObject
{
    private readonly DailyRecord _record;
    private readonly IMessageService _messageService;
    private readonly TaskCompletionSource<bool?> _completionSource = new();

    [ObservableProperty]
    private DateTime date;

    [ObservableProperty]
    private string overtimeHoursText = string.Empty;

    [ObservableProperty]
    private string projectHoursText = string.Empty;

    [ObservableProperty]
    private bool isWeekend;

    [ObservableProperty]
    private bool isHoliday;

    [ObservableProperty]
    private bool isWorkday;

    public OvertimeInputViewModel(DailyRecord record, IMessageService messageService)
    {
        _record = record;
        _messageService = messageService;

        Date = record.Date;
        OvertimeHoursText = record.OvertimeHours.ToString();
        ProjectHoursText = record.ProjectHours.ToString();
        IsWeekend = record.IsWeekend;
        IsHoliday = record.IsHoliday;
        IsWorkday = record.IsWorkday;
    }

    public Task<bool?> Completion => _completionSource.Task;

    public event EventHandler<bool?>? RequestClose;

    [RelayCommand]
    private void Ok()
    {
        if (!TryValidateHours(OvertimeHoursText, "加班时长", out var overtimeHours) ||
            !TryValidateHours(ProjectHoursText, "项目奖时长", out var projectHours))
        {
            return;
        }

        _record.OvertimeHours = overtimeHours;
        _record.ProjectHours = projectHours;
        _record.IsWeekend = IsWeekend;
        _record.IsHoliday = IsHoliday;
        _record.IsWorkday = IsWorkday;

        Complete(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        Complete(false);
    }

    private bool TryValidateHours(string input, string fieldName, out decimal value)
    {
        // 留空自动视为 0，不报错
        if (!Utilities.TryParseAllowEmpty(input, out value))
        {
            _messageService.ShowWarning($"请输入有效的{fieldName}！");
            return false;
        }

        if (value < 0)
        {
            _messageService.ShowWarning($"{fieldName}不能为负，请检查输入！");
            return false;
        }

        if (value > 24)
        {
            _messageService.ShowWarning("单日加班或项目奖时长不能超过 24 小时，请检查记录！");
            return false;
        }

        return true;
    }

    private void Complete(bool? result)
    {
        if (_completionSource.Task.IsCompleted)
        {
            return;
        }

        _completionSource.SetResult(result);
        RequestClose?.Invoke(this, result);
    }
}
