using System.Windows;
using SalaryCalculatorApp.ViewModels;

namespace SalaryCalculatorApp;

public partial class OvertimeInputWindow : Window
{
    private readonly OvertimeInputViewModel _viewModel;

    public OvertimeInputWindow(OvertimeInputViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.RequestClose += OnRequestClose;
        Closed += OnClosed;
    }

    private void OnRequestClose(object? sender, bool? dialogResult)
    {
        DialogResult = dialogResult;
    }

    private void OnClosed(object? sender, System.EventArgs e)
    {
        _viewModel.RequestClose -= OnRequestClose;
        Closed -= OnClosed;
    }
}
