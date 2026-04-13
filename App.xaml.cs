using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using SalaryCalculatorApp.Models;
using SalaryCalculatorApp.Services;
using SalaryCalculatorApp.ViewModels;
using System.Windows;

namespace SalaryCalculatorApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        ExcelPackage.License.SetNonCommercialPersonal("David Wang");

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IMessageService, MessageBoxService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IExcelService, ExcelService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<OvertimeInputWindow>();
        services.AddTransient<Func<DailyRecord, OvertimeInputViewModel>>(serviceProvider =>
            record => ActivatorUtilities.CreateInstance<OvertimeInputViewModel>(serviceProvider, record));
        services.AddTransient<Func<DailyRecord, OvertimeInputWindow>>(serviceProvider =>
            record =>
            {
                var viewModelFactory = serviceProvider.GetRequiredService<Func<DailyRecord, OvertimeInputViewModel>>();
                return ActivatorUtilities.CreateInstance<OvertimeInputWindow>(serviceProvider, viewModelFactory(record));
            });
    }
}
