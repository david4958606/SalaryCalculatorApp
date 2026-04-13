namespace SalaryCalculatorApp.Services;

public interface IMessageService
{
    void ShowInfo(string message);
    void ShowWarning(string message);
    void ShowError(string message);
}
