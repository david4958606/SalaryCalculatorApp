// Models.cs
// 放在 SalaryCalculatorApp 项目中的 Models 文件夹

using System;
using System.Collections.Generic;

namespace SalaryCalculatorApp.Models;

/// <summary>
/// 记录单日的加班/项目奖时长
/// </summary>
public class DailyRecord
{
    public DateTime Date { get; set; }

    /// <summary>
    /// 加班时长（小时）
    /// </summary>
    public decimal OvertimeHours { get; set; }

    /// <summary>
    /// 项目奖计时（小时）
    /// </summary>
    public decimal ProjectHours { get; set; }

    public bool IsWeekend { get; set; } // 是否周末
    public bool IsHoliday { get; set; } // 是否节假日
    public bool IsWorkday { get; set; }
}

/// <summary>
/// 详细条目，用于 "详细结果" DataGrid 绑定
/// </summary>
public class DetailLine
{
    public string Label { get; set; } = string.Empty; // eg. "基础工资" 或 "2025‑07‑05 加班(工作日)"
    public decimal Amount { get; set; } // 正值收入 / 负值扣款
}

/// <summary>
/// 当月工资结果（总额 + 明细）
/// </summary>
public class SalaryResult
{
    public decimal GrossIncome { get; set; } // 应发合计
    public decimal Deductions { get; set; } // 扣款合计（社保、公积金、个税…）
    public decimal NetIncome => GrossIncome - Deductions; // 实发

    public List<DetailLine> Breakdown { get; } = new(); // 供 DataGrid 绑定

    public override string ToString()
    {
        return $"应发：{GrossIncome:F2} 元\n扣款：{Deductions:F2} 元\n——\n实发：{NetIncome:F2} 元";
    }
}