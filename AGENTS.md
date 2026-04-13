# PROJECT KNOWLEDGE BASE

**Generated:** 2026-04-13
**Commit:** 3d4b0c9
**Branch:** main

## OVERVIEW
Chinese salary calculator WPF desktop app (.NET 8.0-windows). Calculates gross salary, overtime, project bonuses, insurance deductions, and personal income tax with Excel export.

## STRUCTURE
```
.
├── App.xaml              # Application entry + merges Styles.xaml
├── MainWindow.xaml       # Main UI (3 tabs: calculate, settings, details)
├── OvertimeInputWindow.xaml  # Dialog for daily overtime/project input
├── SalaryCalculator.cs   # Core calculation logic
├── Utilities.cs          # Parsing helpers + ShowWarning
├── Styles.xaml           # Shared WPF styles (Button, TextBox, TabItem, DataGrid)
└── Models/
    └── Models.cs         # DailyRecord, DetailLine, SalaryResult
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Add UI | `MainWindow.xaml` / `OvertimeInputWindow.xaml` | Tabs: 工资计算 / 系数设置 / 详细结果 |
| Change salary rules | `SalaryCalculator.cs` | `CalculateResult` and `CalculateTax` |
| Change data models | `Models/Models.cs` | All entities in one file |
| Change styling | `Styles.xaml` | Merged in `App.xaml` |
| Excel export logic | `MainWindow.xaml.cs` | EPPlus `ExcelPackage` |

## CONVENTIONS
- File-scoped namespaces used (modern C# style)
- `ImplicitUsings` enabled; no redundant `using System;`
- `Nullable` enabled
- UI text and comments are in Chinese
- EPPlus license set in `MainWindow` constructor: `ExcelPackage.License.SetNonCommercialPersonal("David Wang")`

## ANTI-PATTERNS (THIS PROJECT)
- Do NOT split models into multiple files unless the folder grows significantly; current convention keeps all models in `Models/Models.cs`
- Do NOT remove `UpdateSourceTrigger=LostFocus` bindings in `OvertimeInputWindow.xaml` without verifying two-way binding behavior
- Do NOT change `CalculateTax` brackets without confirming against current Chinese IIT law

## COMMANDS
```bash
# Build
dotnet build

# Run
dotnet run

# Publish (self-contained implied by csproj settings)
dotnet publish -c Release
```

## NOTES
- No test projects present
- No CI/build scripts present
- `IncludeNativeLibrariesForSelfExtract` is enabled for single-file publish
- `AssemblyName` is `工资计算器v$(FileVersion)`
