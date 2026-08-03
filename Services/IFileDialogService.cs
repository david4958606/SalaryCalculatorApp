namespace SalaryCalculatorApp.Services;

public interface IFileDialogService
{
    Task<string?> ShowSaveFileDialogAsync(string filter, string defaultExt, string? fileName);
    Task<string?> ShowOpenFileDialogAsync(string filter, string defaultExt);
}
