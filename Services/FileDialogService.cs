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

    public Task<string?> ShowOpenFileDialogAsync(string filter, string defaultExt)
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            DefaultExt = defaultExt,
            CheckFileExists = true,
            Multiselect = false
        };

        var result = dialog.ShowDialog() == true ? dialog.FileName : null;
        return Task.FromResult<string?>(result);
    }
}
