// SalaryCalculator.cs
// 核心税前工资计算逻辑

using SalaryCalculatorApp.Models;

namespace SalaryCalculatorApp
{
    public class SalaryCalculator
    {
        /// <summary>
        /// 生成指定年月的 DailyRecord 列表，初始化时长为 0
        /// </summary>
        public static List<DailyRecord> CreateMonthlyRecords(int year, int month)
        {
            int days = DateTime.DaysInMonth(year, month);
            var list = new List<DailyRecord>(days);
            for (int d = 1; d <= days; d++)
                list.Add(new DailyRecord { Date = new DateTime(year, month, d) });
            return list;
        }

        /// <summary>
        /// 仅计算税前（应发）部分，不含社保/个税等扣款
        /// </summary>
        public static SalaryResult CalculateResult(DateTime month,
            decimal baseSalary,
            decimal perfSalary,
            bool isProbation,
            bool fullMonth,
            bool fullQuarter,
            decimal transportationSubsidy,
            decimal otherSubsidy,
            decimal projectBonusCoefficient,
            decimal insuranceBase,
            decimal insuranceRate,
            decimal insuranceAddon,
            decimal grandTotalPrePayTax,
            bool grandTotalEnabled,
            decimal otherWaiver,
            List<DailyRecord> daily,
            decimal performanceReward = 0m,
            decimal preTaxAdjustment = 0m,
            decimal postTaxAdjustment = 0m)
        {
            var res = new SalaryResult();

            //-------------------------
            // 1. 基础 + 绩效
            //-------------------------
            var factor = isProbation ? 0.8m : 1.0m;
            var basePart = baseSalary * factor;
            var perfPart = perfSalary * factor;
            res.GrossIncome += basePart + perfPart;
            res.Deductions = 0; // 初始化扣款为 0
            res.Breakdown.Add(new DetailLine { Label = "基础工资", Amount = basePart });
            res.Breakdown.Add(new DetailLine { Label = "绩效工资", Amount = perfPart });
            var hourly = basePart / 21.75m / 8.0m; // 基础时薪
            res.Breakdown.Add(new DetailLine { Label = "基础时薪", Amount = hourly });
            //-------------------------
            // 2. 补贴 & 全勤
            //-------------------------
            if (!isProbation)
            {
                // 获取交通补贴
                res.GrossIncome += transportationSubsidy;
                res.Breakdown.Add(new DetailLine { Label = "交通补贴", Amount = transportationSubsidy });
                // 获取其他补贴
                res.GrossIncome += otherSubsidy;
                res.Breakdown.Add(new DetailLine { Label = "其他补贴", Amount = otherSubsidy });
            }

            if (fullMonth)
            {
                res.GrossIncome += 300; // 月度全勤
                res.Breakdown.Add(new DetailLine { Label = "当月全勤奖", Amount = 300 });
            }

            // 季度全勤：1、4、7、10 月发放
            if (fullQuarter && (month.Month % 3 == 0))
            {
                res.GrossIncome += 1000;
                res.Breakdown.Add(new DetailLine { Label = "季度全勤奖", Amount = 1000 });
            }

            //-------------------------
            // 3. 加班费 & 项目奖
            //-------------------------
            if (daily.Count > 0)
            {
                decimal totalProjectPay = 0;
                decimal totalOvertimePay = 0;
                decimal totalOvertimeHours = 0;
                foreach (var rec in daily.Where(r => r.OvertimeHours > 0 || r.ProjectHours > 0))
                {
                    totalOvertimeHours += rec.OvertimeHours;
                    if (totalOvertimeHours > 36)
                    {
                        res.Warnings.Add("加班时长超过 36 小时，请检查记录！");
                        break;
                    }

                    if (rec.OvertimeHours < 0 || rec.ProjectHours < 0)
                    {
                        res.Warnings.Add("加班或项目奖时长不能为负，请检查记录！");
                        continue; // 跳过无效记录
                    }

                    if (rec.OvertimeHours > 24 || rec.ProjectHours > 24)
                    {
                        res.Warnings.Add("单日加班或项目奖时长不能超过 24 小时，请检查记录！");
                        continue; // 跳过无效记录
                    }

                    var isWeekend = rec.IsWeekend || (rec.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
                    var isHoliday = rec.IsHoliday;

                    if (rec.IsWorkday)
                    {
                        isWeekend = false;
                        isHoliday = false;
                    }

                    var overtimeMultiplier = isHoliday ? 3.0m : (isWeekend ? 2.0m : 1.5m);
                    var projectMultiplier = overtimeMultiplier * projectBonusCoefficient;

                    var overtimePay = rec.OvertimeHours * hourly * overtimeMultiplier;
                    var projectPay = rec.ProjectHours * hourly * projectMultiplier;

                    if (overtimePay > 0)
                    {
                        res.GrossIncome += overtimePay;
                        res.Breakdown.Add(new DetailLine
                        {
                            Label =
                                $"{rec.Date:MM-dd} 加班({(isHoliday ? "假日" : (isWeekend ? "周末" : "工作日"))}) {rec.OvertimeHours} 小时",
                            Amount = overtimePay
                        });
                    }

                    if (projectPay > 0)
                    {
                        res.GrossIncome += projectPay;
                        res.Breakdown.Add(new DetailLine
                        {
                            Label =
                                $"{rec.Date:MM-dd} 项目奖({(isHoliday ? "假日" : (isWeekend ? "周末" : "工作日"))}) {rec.ProjectHours} 小时",
                            Amount = projectPay
                        });
                    }

                    totalProjectPay += projectPay;
                    totalOvertimePay += overtimePay;
                }

                res.Breakdown.Add(new DetailLine { Label = "加班费", Amount = totalOvertimePay });
                res.Breakdown.Add(new DetailLine { Label = "项目奖", Amount = totalProjectPay });
            }

            //-------------------------
            // 3.5 绩效奖励 & 税前加/扣款（计入税前总额，不受试用期影响）
            //-------------------------
            res.GrossIncome += performanceReward;
            res.Breakdown.Add(new DetailLine { Label = "绩效奖励", Amount = performanceReward });

            res.GrossIncome += preTaxAdjustment; // 负数则自动从税前总额中扣除
            res.Breakdown.Add(new DetailLine { Label = "税前加/扣款", Amount = preTaxAdjustment });

            res.Breakdown.Add(new DetailLine { Label = "应发工资", Amount = res.GrossIncome });
            //-------------------------
            // 4. 扣除五险二金
            //-------------------------
            if (insuranceBase <= 0)
            {
                res.Warnings.Add("社保基数必须大于 0，请检查设置！");
                insuranceBase = baseSalary + perfSalary; // 默认社保基数为工资总额
            }

            // if (insuranceRate <= 0)
            // {
            //     Utilities.ShowWarning("五险二金比例必须大于 0，请检查设置！");
            // }

            if (isProbation)
            {
                insuranceRate *= 0.8m; // 试用期五险二金打八折
            }

            var insurance = CalculateInsurance(insuranceBase, insuranceRate, insuranceAddon);
            res.Deductions += insurance;
            res.Breakdown.Add(new DetailLine
            {
                Label = "五险二金",
                Amount = -insurance
            });
            //-------------------------
            // 5. 计算个税
            //-------------------------
            res.Breakdown.Add(new DetailLine
            {
                Label = "专项附加扣除",
                Amount = -otherWaiver
            });
            const decimal startPoint = 5000m;
            var taxableIncome = res.GrossIncome - insurance;
            if (grandTotalEnabled)
            {
                var taxAlreadyPaid = CalculateTax(grandTotalPrePayTax, 0m, true);
                taxableIncome = taxableIncome - (startPoint + otherWaiver);
                res.Breakdown.Add(new DetailLine
                {
                    Label = "累计预扣预缴应纳税所得额",
                    Amount = grandTotalPrePayTax + taxableIncome
                });
                res.Breakdown.Add(new DetailLine
                {
                    Label = "累计预扣预缴个税",
                    Amount = taxAlreadyPaid
                });
                var tax = CalculateTax(taxableIncome, grandTotalPrePayTax, true);
                tax -= taxAlreadyPaid;
                res.Deductions += tax; // 减去已缴税款
                res.Breakdown.Add(new DetailLine
                {
                    Label = "个税",
                    Amount = -tax
                });
            }
            else
            {
                taxableIncome = taxableIncome - otherWaiver - startPoint;
                res.Breakdown.Add(new DetailLine
                {
                    Label = "当月应纳税所得额",
                    Amount = taxableIncome
                });
                var tax = CalculateTax(taxableIncome, grandTotalPrePayTax, grandTotalEnabled);
                res.Deductions += tax;
                res.Breakdown.Add(new DetailLine
                {
                    Label = "个税",
                    Amount = -tax
                });
            }

            //-------------------------
            // 6. 税后加/扣款（直接计入税后实发工资，负数则自动扣除）
            //-------------------------
            res.Deductions -= postTaxAdjustment; // 减少扣款 = 增加实发；负数则增加扣款
            res.Breakdown.Add(new DetailLine { Label = "税后加/扣款", Amount = postTaxAdjustment });

            //-------------------------
            // 返回结果
            //-------------------------
            res.Breakdown.Add(new DetailLine { Label = "实发工资", Amount = res.NetIncome });
            return res;
        }

        /// <summary>
        /// 计算五险二金
        /// </summary>
        /// <param name="insuranceBase"></param>
        /// <param name="insuranceRate"></param>
        /// <param name="insuranceAddon"></param>
        /// <returns></returns>
        public static decimal CalculateInsurance(decimal insuranceBase, decimal insuranceRate, decimal insuranceAddon)
        {
            // 计算五险二金
            return insuranceBase * insuranceRate + insuranceAddon;
        }

        /// <summary>
        /// 计算当月个税
        /// </summary>
        /// <param name="taxableIncome">应纳税所得额</param>
        /// <param name="grandTotalPrePayTax">累计预扣预缴应纳税所得额</param>
        /// <param name="grandTotalEnabled"></param>
        /// <returns></returns>
        public static decimal CalculateTax(decimal taxableIncome,
            decimal grandTotalPrePayTax = 0m,
            bool grandTotalEnabled = false)
        {
            if (taxableIncome <= 0)
            {
                return 0; // 没有收入则不需要缴税
            }

            if (grandTotalEnabled)
            {
                taxableIncome = grandTotalPrePayTax + taxableIncome;
                var totalTax = taxableIncome switch
                {
                    <= 36000 => taxableIncome * 0.03m,
                    <= 144000 => taxableIncome * 0.1m - 2520,
                    <= 300000 => taxableIncome * 0.2m - 16920,
                    <= 420000 => taxableIncome * 0.25m - 31920,
                    <= 660000 => taxableIncome * 0.3m - 52920,
                    <= 960000 => taxableIncome * 0.35m - 85920,
                    _ => taxableIncome * 0.45m - 181920
                };
                return totalTax; // 减去已缴税款
            }

            return taxableIncome switch
            {
                <= 3000 => taxableIncome * 0.03m,
                <= 12000 => taxableIncome * 0.1m - 210,
                <= 25000 => taxableIncome * 0.2m - 1410,
                <= 35000 => taxableIncome * 0.25m - 2660,
                <= 55000 => taxableIncome * 0.3m - 4410,
                <= 80000 => taxableIncome * 0.35m - 7160,
                _ => taxableIncome * 0.45m - 15160
            };
        }
    }
}
