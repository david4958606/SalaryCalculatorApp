using Microsoft.Win32;

namespace SalaryCalculatorApp.Services;

public class FileDialogService : IFileDialogService
{
    public Task<string?> ShowSaveFileDialogAsync(string filter, string defaultExt, string? fileName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            DefaultExt = defaultExt,
            FileName = fileName ?? string.Empty
        };

        var result = dialog.ShowDialog() == true ? dialog.FileName : null;
        return Task.FromResult<string?>(result);
    }
}
