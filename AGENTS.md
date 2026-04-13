# PROJECT KNOWLEDGE BASE

**Updated:** 2026-04-14  
**Branch:** mvvm

## OVERVIEW
工资计算器（WPF / .NET 8），当前主交互已迁移为 MVVM：
- `MainWindow` 通过 `MainWindowViewModel` 驱动
- `OvertimeInputWindow` 通过 `OvertimeInputViewModel` 驱动
- 业务计算在 `SalaryCalculator`，UI 提示由 `IMessageService` 负责

## STRUCTURE
```
.
├── App.xaml / App.xaml.cs            # 应用入口 + DI 组合根
├── MainWindow.xaml / .cs             # 主窗口（绑定驱动，code-behind 仅视图细节）
├── OvertimeInputWindow.xaml / .cs    # 加班录入对话框（MVVM）
├── SalaryCalculator.cs               # 业务计算核心（无直接弹窗）
├── Utilities.cs                      # 数值解析等通用辅助
├── Models/Models.cs                  # DailyRecord / DetailLine / SalaryResult
├── ViewModels/
│   ├── MainWindowViewModel.cs
│   └── OvertimeInputViewModel.cs
├── Services/
│   ├── IDialogService.cs / DialogService.cs
│   ├── IMessageService.cs / MessageBoxService.cs
│   ├── IExcelService.cs / ExcelService.cs
│   └── IFileDialogService.cs / FileDialogService.cs
└── SalaryCalculatorApp.Tests/         # xUnit 回归与 MVVM 测试
```

## WHERE TO LOOK
| 任务 | 位置 | 备注 |
|---|---|---|
| 改主界面交互 | `MainWindow.xaml` + `ViewModels/MainWindowViewModel.cs` | 优先改 VM，避免把逻辑放回 code-behind |
| 改加班弹窗交互 | `OvertimeInputWindow.xaml` + `ViewModels/OvertimeInputViewModel.cs` | 保持命令绑定；窗口关闭通过 `RequestClose` |
| 改工资计算规则 | `SalaryCalculator.cs` | 通过 `SalaryResult` 返回数据与 warnings |
| 改弹窗/消息行为 | `Services/IMessageService.cs` + `Services/MessageBoxService.cs` | VM 只依赖接口 |
| 改导出 Excel | `Services/ExcelService.cs` | 与 UI 解耦 |
| 改弹窗定位与所有权 | `Services/DialogService.cs` | `Owner` 在这里设置，保持 MVVM |

## CURRENT MVVM BOUNDARIES
- `MainWindow.xaml.cs` 仅做：`DataContext` 注入、标题显示、日历鼠标捕获 workaround。
- `DialogService` 负责创建并展示 `OvertimeInputWindow`，并设置 `Owner = Application.Current.MainWindow`，配合 `WindowStartupLocation="CenterOwner"` 实现相对主窗口居中。
- `SalaryCalculator` 不直接调用 `MessageBox`；告警通过 `SalaryResult.Warnings` 返回，由 VM 调用消息服务展示。

## CONVENTIONS
- Nullable 开启，避免可空警告忽略。
- UI 字段/提示语以中文为主。
- View 与 ViewModel 命名保持一一对应。
- 新增服务优先“接口 + 实现”成对落地，并在 `App.xaml.cs` 注册。

## PUBLISH
`WithRuntime.pubxml`（自包含单文件）位于：
`Properties/PublishProfiles/WithRuntime.pubxml`

关键设置：
- `SelfContained=true`
- `PublishSingleFile=true`
- `RuntimeIdentifier=win-x64`

常用命令：
```bash
dotnet build
dotnet test SalaryCalculatorApp.Tests/SalaryCalculatorApp.Tests.csproj
dotnet publish /p:PublishProfile=Properties/PublishProfiles/WithRuntime.pubxml
```

## TESTING NOTES
- 重点测试：
  - `MainWindowViewModelTests`
  - `OvertimeInputViewModelTests`
  - `SalaryCalculatorResultTests`
  - `SalaryCalculatorTests`
- 手动烟测建议：`dotnet run --no-build`，确认主窗口可启动、加班弹窗可从主窗口居中弹出。
