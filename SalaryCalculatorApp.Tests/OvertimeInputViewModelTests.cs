using SalaryCalculatorApp.Models;
using SalaryCalculatorApp.Services;
using SalaryCalculatorApp.ViewModels;
using Xunit;

namespace SalaryCalculatorApp.Tests;

public class OvertimeInputViewModelTests
{
    [Fact]
    public async Task OkCommand_AppliesChangesToOriginalRecord_AndCompletesWithTrue()
    {
        var record = new DailyRecord
        {
            Date = new DateTime(2025, 4, 12),
            OvertimeHours = 1m,
            ProjectHours = 2m,
            IsWeekend = false,
            IsHoliday = false,
            IsWorkday = true
        };
        var messageService = new FakeMessageService();
        var viewModel = new OvertimeInputViewModel(record, messageService)
        {
            OvertimeHoursText = "3.5",
            ProjectHoursText = "4",
            IsWeekend = true,
            IsHoliday = true,
            IsWorkday = false
        };

        bool? closeResult = null;
        viewModel.RequestClose += (_, result) => closeResult = result;

        viewModel.OkCommand.Execute(null);

        Assert.Equal(3.5m, record.OvertimeHours);
        Assert.Equal(4m, record.ProjectHours);
        Assert.True(record.IsWeekend);
        Assert.True(record.IsHoliday);
        Assert.False(record.IsWorkday);
        Assert.True(viewModel.Completion.IsCompletedSuccessfully);
        Assert.True(await viewModel.Completion);
        Assert.True(closeResult);
        Assert.Empty(messageService.Messages);
    }

    [Fact]
    public async Task CancelCommand_DoesNotApplyChangesToOriginalRecord_AndCompletesWithFalse()
    {
        var record = new DailyRecord
        {
            Date = new DateTime(2025, 4, 12),
            OvertimeHours = 1m,
            ProjectHours = 2m,
            IsWeekend = false,
            IsHoliday = false,
            IsWorkday = true
        };
        var viewModel = new OvertimeInputViewModel(record, new FakeMessageService())
        {
            OvertimeHoursText = "8",
            ProjectHoursText = "6",
            IsWeekend = true,
            IsHoliday = true,
            IsWorkday = false
        };

        bool? closeResult = null;
        viewModel.RequestClose += (_, result) => closeResult = result;

        viewModel.CancelCommand.Execute(null);

        Assert.Equal(1m, record.OvertimeHours);
        Assert.Equal(2m, record.ProjectHours);
        Assert.False(record.IsWeekend);
        Assert.False(record.IsHoliday);
        Assert.True(record.IsWorkday);
        Assert.True(viewModel.Completion.IsCompletedSuccessfully);
        Assert.False(await viewModel.Completion);
        Assert.False(closeResult);
    }

    [Fact]
    public void OkCommand_WhenInputIsInvalid_ShowsWarningAndDoesNotApplyChanges()
    {
        var record = new DailyRecord
        {
            Date = new DateTime(2025, 4, 12),
            OvertimeHours = 1m,
            ProjectHours = 2m
        };
        var messageService = new FakeMessageService();
        var viewModel = new OvertimeInputViewModel(record, messageService)
        {
            OvertimeHoursText = "abc",
            ProjectHoursText = "2"
        };

        viewModel.OkCommand.Execute(null);

        Assert.Equal(1m, record.OvertimeHours);
        Assert.Equal(2m, record.ProjectHours);
        Assert.False(viewModel.Completion.IsCompleted);
        Assert.Single(messageService.Messages);
        Assert.Equal("请输入有效的加班时长！", messageService.Messages[0]);
    }

    private sealed class FakeMessageService : IMessageService
    {
        public List<string> Messages { get; } = new();

        public void ShowInfo(string message)
        {
            Messages.Add(message);
        }

        public void ShowWarning(string message)
        {
            Messages.Add(message);
        }

        public void ShowError(string message)
        {
            Messages.Add(message);
        }
    }
}
