using SalaryCalculatorApp.Models;
using SalaryCalculatorApp.Services;
using SalaryCalculatorApp.ViewModels;
using Xunit;

namespace SalaryCalculatorApp.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void CalculateCommand_WithValidInputs_PopulatesResultAndBreakdown()
    {
        var viewModel = CreateViewModel();
        viewModel.BaseSalaryText = "10000";
        viewModel.PerformanceSalaryText = "2000";
        viewModel.InsuranceBaseText = "12000";
        viewModel.FullMonth = true;
        viewModel.GrandTotalEnabled = true;
        viewModel.GrandTotalPrePayTaxText = "30000";

        viewModel.CalculateCommand.Execute(null);

        Assert.Contains("应发：", viewModel.ResultText);
        Assert.NotEmpty(viewModel.DetailLines);
        Assert.Contains(viewModel.DetailLines, item => item.Label == "基础工资");
        Assert.Contains(viewModel.DetailLines, item => item.Label == "实发工资");
    }

    [Fact]
    public void DisplayMonth_UpdatesQuarterAvailability_AndClearsFullQuarterWhenDisabled()
    {
        var viewModel = CreateViewModel();
        viewModel.DisplayMonth = new DateTime(2025, 3, 1);
        viewModel.FullQuarter = true;

        viewModel.DisplayMonth = new DateTime(2025, 5, 1);

        Assert.False(viewModel.IsQuarterMonth);
        Assert.False(viewModel.FullQuarter);
    }

    [Fact]
    public void EditSelectedDateCommand_WithoutSelection_ShowsInfoMessage()
    {
        var messageService = new FakeMessageService();
        var viewModel = CreateViewModel(messageService: messageService);
        viewModel.SelectedDate = null;

        viewModel.EditSelectedDateCommand.Execute(null);

        Assert.Contains("请先选择一个日期", messageService.InfoMessages);
    }

    [Fact]
    public async Task ExportExcelCommand_WithComputedDetails_SavesFileAndShowsSuccessMessage()
    {
        var excelService = new FakeExcelService();
        var fileDialogService = new FakeFileDialogService();
        var messageService = new FakeMessageService();
        var viewModel = CreateViewModel(excelService, fileDialogService, messageService);
        viewModel.BaseSalaryText = "10000";
        viewModel.PerformanceSalaryText = "2000";
        viewModel.InsuranceBaseText = "12000";
        viewModel.CalculateCommand.Execute(null);

        await viewModel.ExportExcelCommand.ExecuteAsync(null);

        Assert.True(File.Exists(fileDialogService.FilePath));
        Assert.NotNull(excelService.LastBreakdown);
        Assert.Contains("导出成功！", messageService.InfoMessages);
        File.Delete(fileDialogService.FilePath);
    }

    [Fact]
    public async Task ImportOvertimeCommand_LoadsRecordsAndRefreshesResult()
    {
        var excelService = new FakeExcelService
        {
            ImportedRecords =
            [
                new DailyRecord
                {
                    Date = new DateTime(2025, 5, 2),
                    OvertimeHours = 3m,
                    ProjectHours = 1m
                }
            ]
        };
        var fileDialogService = new FakeFileDialogService();
        await File.WriteAllBytesAsync(fileDialogService.FilePath, [1, 2, 3]);
        var messageService = new FakeMessageService();
        var viewModel = CreateViewModel(excelService, fileDialogService, messageService);
        viewModel.DisplayMonth = new DateTime(2025, 5, 1);
        viewModel.BaseSalaryText = "10000";
        viewModel.PerformanceSalaryText = "2000";
        viewModel.InsuranceBaseText = "12000";

        await viewModel.ImportOvertimeCommand.ExecuteAsync(null);

        Assert.Contains(viewModel.DetailLines, item => item.Label.Contains("05-02 加班") && item.Label.Contains("3 小时"));
        Assert.Contains(viewModel.DetailLines, item => item.Label.Contains("05-02 项目奖") && item.Label.Contains("1 小时"));
        Assert.Contains("成功导入 1 天的加班记录！", messageService.InfoMessages);
        File.Delete(fileDialogService.FilePath);
    }

    [Fact]
    public void CalculateCommand_WhenCalculatorReturnsWarnings_ShowsThemThroughMessageService()
    {
        var messageService = new FakeMessageService();
        var viewModel = CreateViewModel(messageService: messageService);
        viewModel.BaseSalaryText = "10000";
        viewModel.PerformanceSalaryText = "2000";
        viewModel.InsuranceBaseText = "0";

        viewModel.DisplayMonth = new DateTime(2025, 5, 1);
        viewModel.SelectedDate = new DateTime(2025, 5, 1);

        viewModel.EditSelectedDateCommand.Execute(null);
        viewModel.SelectedDate = new DateTime(2025, 5, 2);
        viewModel.EditSelectedDateCommand.Execute(null);

        viewModel.CalculateCommand.Execute(null);

        Assert.Contains("加班时长超过 36 小时，请检查记录！", messageService.WarningMessages);
        Assert.Contains("社保基数必须大于 0，请检查设置！", messageService.WarningMessages);
        Assert.Contains("应发：", viewModel.ResultText);
    }

    private static MainWindowViewModel CreateViewModel(
        FakeExcelService? excelService = null,
        FakeFileDialogService? fileDialogService = null,
        FakeMessageService? messageService = null)
    {
        return new MainWindowViewModel(
            new FakeDialogService(),
            excelService ?? new FakeExcelService(),
            fileDialogService ?? new FakeFileDialogService(),
            messageService ?? new FakeMessageService());
    }

    private sealed class FakeDialogService : IDialogService
    {
        public bool? ShowOvertimeDialog(DailyRecord record)
        {
            record.OvertimeHours = 20m;
            return true;
        }
    }

    private sealed class FakeExcelService : IExcelService
    {
        public List<DetailLine>? LastBreakdown { get; private set; }
        public IReadOnlyList<DailyRecord> ImportedRecords { get; init; } = [];

        public Task<byte[]> ExportToExcelAsync(
            List<DetailLine> breakdown,
            string title,
            IReadOnlyCollection<DailyRecord>? dailyRecords = null)
        {
            LastBreakdown = breakdown;
            return Task.FromResult(new byte[] { 1, 2, 3 });
        }

        public Task<IReadOnlyList<DailyRecord>> ImportOvertimeAsync(byte[] workbookBytes, DateTime targetMonth)
        {
            return Task.FromResult(ImportedRecords);
        }
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public string FilePath { get; } = Path.Combine(Path.GetTempPath(), $"salary-test-{Guid.NewGuid():N}.xlsx");

        public Task<string?> ShowSaveFileDialogAsync(string filter, string defaultExt, string? fileName)
        {
            return Task.FromResult<string?>(FilePath);
        }

        public Task<string?> ShowOpenFileDialogAsync(string filter, string defaultExt)
        {
            return Task.FromResult<string?>(FilePath);
        }
    }

    private sealed class FakeMessageService : IMessageService
    {
        public List<string> InfoMessages { get; } = new();
        public List<string> WarningMessages { get; } = new();
        public List<string> ErrorMessages { get; } = new();

        public void ShowInfo(string message)
        {
            InfoMessages.Add(message);
        }

        public void ShowWarning(string message)
        {
            WarningMessages.Add(message);
        }

        public void ShowError(string message)
        {
            ErrorMessages.Add(message);
        }
    }
}
