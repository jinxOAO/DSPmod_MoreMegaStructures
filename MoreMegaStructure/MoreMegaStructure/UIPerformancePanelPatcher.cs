using System.Text;
using HarmonyLib;
using UnityEngine;

namespace MoreMegaStructure
{
    /// <summary>
    /// 在普通性能统计面板的 CPU 列表末尾追加本模组的自定义层级统计。
    /// 不涉及深度性能分析面板，也不修改游戏的任何数组，仅在 UIPerformancePanel.RefreshCpuStatTexts
    /// 执行完毕后把额外的行拼接到已有的文本上，并重算内容高度。
    /// </summary>
    public class UIPerformancePanelPatcher
    {
        /// <summary>
        /// 在面板初始化最前（_OnInit 内 cpuGraph._Init 读取 timeCostsShowing 之前）把自定义项并入
        /// 饼图所需的原生数组尾部。这样扇形图会为本模组的每一项建立对应扇区，并按 level/parent 正确嵌套。
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIPerformancePanel), "_OnInit")]
        public static void BeforeInit()
        {
            MMSCPU.RebuildChartArrays();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIPerformancePanel), "RefreshCpuStatTexts")]
        public static void Refresh(UIPerformancePanel __instance)
        {
            if (__instance == null || __instance.cpuLabelText == null)
            {
                return;
            }
            if (MMSCPU.Count == 0)
            {
                return;
            }
            // 与游戏本体保持一致：预热未完成（aveFrame <= -7）时数据尚未就绪，不显示自定义项。
            if (PerformanceMonitor.aveFrame <= -7L)
            {
                return;
            }

            StringBuilder label = new StringBuilder(__instance.cpuLabelText.text ?? string.Empty, 1024);
            StringBuilder value1 = new StringBuilder(__instance.cpuValueText1.text ?? string.Empty, 1024);
            StringBuilder value2 = new StringBuilder(__instance.cpuValueText2.text ?? string.Empty, 1024);

            // 分隔空行，使自定义区块与原生统计之间留出呼吸空间。
            label.AppendLine();
            value1.AppendLine();
            value2.AppendLine();

            double total = PerformanceMonitor.showCompleteEngineCost
                ? PerformanceMonitor.totalTimeCostsShowingComplete
                : PerformanceMonitor.totalTimeCostsShowing;

            AppendCustomEntries(label, value1, value2, total);

            __instance.cpuLabelText.text = label.ToString();
            __instance.cpuValueText1.text = value1.ToString();
            __instance.cpuValueText2.text = value2.ToString();
            __instance.cpuContentTrans.sizeDelta = new Vector2(
                __instance.cpuContentTrans.sizeDelta.x,
                __instance.cpuLabelText.preferredHeight - 10f);
        }

        /// <summary>
        /// 逐条追加自定义统计项。缩进风格与游戏原生 AppendCpuStatEntry 对齐：
        /// level 0 顶格，level 1 用 "  · "，level 2 用 "    - "，更深一级多两个空格。
        /// 每个统计项恰好向三列各写入一行，保证列之间行数一致。
        /// 绝不与主线程数值相加 —— 并发线程的耗时相加得到的是 CPU 时间总和，不是墙上占用。
        /// </summary>
        private static void AppendCustomEntries(StringBuilder label, StringBuilder value1, StringBuilder value2, double total)
        {
            int count = MMSCPU.Count;
            for (int i = 0; i < count; i++)
            {
                int level = MMSCPU.GetLevel(i);
                label.Append(GetIndent(level));
                label.Append(MMSCPU.GetName(i));

                double seconds = MMSCPU.GetShowingSeconds(i);
                double worker = MMSCPU.GetWorkerSeconds(i);
                label.AppendLine();

                if (seconds >= 2.5E-07)
                {
                    value1.AppendFormat("{0:0.000} ms\r\n", seconds * 1000.0);
                }
                else
                {
                    value1.AppendLine("-");
                }

                if (seconds < 2.5E-07 || total < 1E-08)
                {
                    value2.AppendLine("");
                }
                else
                {
                    value2.AppendFormat("{0:0.00} %\r\n", seconds / total * 100.0);
                }
            }

        }

        private static string GetIndent(int level)
        {
            switch (level)
            {
                case 1:
                    return "  · ";
                case 2:
                    return "    - ";
                case 3:
                    return "      - ";
                default:
                    return level > 3 ? "        - " : string.Empty;
            }
        }

        // 以下 UISectorGraph 相关补丁为历史遗留，当前版本不需要，保留注释以备查阅。
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(UISectorGraph), "_OnOpen")]
        //public static bool UISectorGraphOnOpenPrePatch(ref UISectorGraph __instance)
        //{
        //    return true;
        //}
    }
}
