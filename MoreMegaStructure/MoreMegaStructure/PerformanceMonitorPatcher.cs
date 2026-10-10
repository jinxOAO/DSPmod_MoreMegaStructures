using System;
using System.Collections.Generic;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace MoreMegaStructure
{
    /// <summary>
    /// 自定义性能统计项的标识。这些值仅作为本模组内部的采样点编号使用，
    /// 与游戏的 <see cref="ECpuWorkEntry"/> 无关，也不会写入游戏的任何数组，
    /// 因此游戏更新调整原生数组长度时不会与本模组冲突。
    /// </summary>
    public enum ECpuWorkEntryExtended
    {
        MoreMegaStructure = 44,
        Receiver,
        StarAssembly,
        //CargoProduce,
        CargoTransport,
        StarCannon,
        EnemySearch,
        AttackLogic,
        WarpField,
        Statistics,
        Patch
    }

    /// <summary>
    /// 在 PerformanceMonitor 的生命周期上挂接本模组的自定义统计：
    /// 逐帧滑动平均以及统计复位。这里刻意只挂钩游戏当前版本仍然存在的成员，
    /// 不再触碰（也不再重设）游戏自身的任何静态数组。
    /// </summary>
    public class PerformanceMonitorPatcher
    {
        // 游戏的 SlideAverage 只在 CPU 分析开启且本帧存在逻辑帧采样时被调用，
        // 其执行节奏与游戏自身的统计项完全一致，因此把我们的滑动平均挂在其后最稳妥。
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PerformanceMonitor), "SlideAverage")]
        public static void SlideAveragePostfix()
        {
            MMSCPU.OnFrameAverage();
        }

        // 面板打开/关闭时游戏会调用 ResetStatistics 清零自身数据，这里同步清零本模组数据。
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PerformanceMonitor), "ResetStatistics")]
        public static void ResetStatisticsPostfix()
        {
            MMSCPU.Reset();
        }
    }

    /// <summary>
    /// 巨构（MoreMegaStructure）自定义 CPU 时间采样器。
    ///
    /// 设计要点：
    /// - 完全自持有数据，不读写 PerformanceMonitor 的任何数组，避免与游戏更新冲突。
    /// - Begin/End 通过 [ThreadStatic] 的起始时间戳 + Interlocked 累计计数实现线程安全，
    ///   因此既可以在主线程（如 ThreadManager.ProcessFrame 后置）使用，
    ///   也可以在工厂逻辑的工作线程（如 PowerGeneratorComponent.GameTick_Gamma 前置）使用。
    /// - 只有当游戏 CPU 分析处于开启状态时才真正采样，关闭时 Begin/End 近乎零开销。
    /// - 允许 Begin/End 因提前 return 而不配对，也不会造成数据损坏或异常。
    /// </summary>
    public static class MMSCPU
    {
        private sealed class CpuEntry
        {
            public string Name;
            public int Level;   // 显示缩进层级：0 顶层，1 一级，2 二级
            public long AccumTicks; // 本帧累计的高精度计时刻度（跨线程 Interlocked 累加）
            public double Average;  // 滑动平均，单位：秒
            public double Showing;  // 供面板显示的数值，单位：秒
        }

        private static readonly List<CpuEntry> entries = new List<CpuEntry>();

        // ECpuWorkEntryExtended -> 内部索引 的映射表。注册时按枚举顺序填充。
        private static readonly Dictionary<ECpuWorkEntryExtended, int> enumMap =
            new Dictionary<ECpuWorkEntryExtended, int>();

        // 每个线程独立保存各采样点的起始刻度；0 表示尚未 Begin。
        [ThreadStatic]
        private static long[] threadStartTicks;

        private static long frequency = 0L;

        /// <summary>
        /// 注册一个自定义统计项，返回其内部索引。请在 Awake 阶段（游戏逻辑开始前）调用。
        /// </summary>
        /// <param name="name">显示名称（原样显示，不做本地化）。</param>
        /// <param name="level">显示缩进层级：0 顶层、1 一级、2 二级。</param>
        public static int AddEntry(string name, int level)
        {
            lock (entries)
            {
                entries.Add(new CpuEntry { Name = name, Level = level });
                EnsureFrequency();
                return entries.Count - 1;
            }
        }

        /// <summary>
        /// 将一个枚举标识绑定到某个已注册的采样点索引，方便用枚举进行 Begin/End。
        /// </summary>
        public static void MapEnum(ECpuWorkEntryExtended entry, int index)
        {
            lock (entries)
            {
                enumMap[entry] = index;
            }
        }

        private static void EnsureFrequency()
        {
            if (frequency <= 0L)
            {
                try
                {
                    frequency = PerformanceCounter.Frequency;
                }
                catch
                {
                    frequency = 0L;
                }
                if (frequency <= 0L)
                {
                    frequency = 10000000L; // 兜底：10MHz，正常不会走到
                }
            }
        }

        /// <summary>可供显示的有效采样项数量。</summary>
        public static int Count
        {
            get { return entries.Count; }
        }

        public static string GetName(int index)
        {
            CpuEntry e = GetEntry(index);
            return e != null ? e.Name : "";
        }

        public static int GetLevel(int index)
        {
            CpuEntry e = GetEntry(index);
            return e != null ? e.Level : 0;
        }

        /// <summary>该采样项当前用于显示的耗时（秒，滑动平均）。</summary>
        public static double GetShowingSeconds(int index)
        {
            CpuEntry e = GetEntry(index);
            return e != null ? e.Showing : 0.0;
        }

        private static CpuEntry GetEntry(int index)
        {
            if (index < 0 || index >= entries.Count)
            {
                return null;
            }
            return entries[index];
        }

        // ---------------- 采样 API（保持原有调用方式） ----------------

        public static void BeginSample(ECpuWorkEntryExtended logic)
        {
            int index;
            if (enumMap.TryGetValue(logic, out index))
            {
                BeginSample(index);
            }
        }

        public static void EndSample(ECpuWorkEntryExtended logic)
        {
            int index;
            if (enumMap.TryGetValue(logic, out index))
            {
                EndSample(index);
            }
        }

        public static void BeginSample(int logic)
        {
            // 关闭 CPU 分析时几乎零开销：仅一次布尔判断即返回。
            if (!PerformanceMonitor.CpuProfilerOn)
            {
                return;
            }
            if (logic < 0 || logic >= entries.Count)
            {
                return;
            }
            long[] starts = threadStartTicks;
            if (starts == null || starts.Length <= logic)
            {
                starts = ResizeThreadStarts(starts);
            }
            starts[logic] = PerformanceCounter.Counter;
        }

        public static void EndSample(int logic)
        {
            if (!PerformanceMonitor.CpuProfilerOn)
            {
                return;
            }
            if (logic < 0 || logic >= entries.Count)
            {
                return;
            }
            long[] starts = threadStartTicks;
            if (starts == null || starts.Length <= logic)
            {
                return;
            }
            long now = PerformanceCounter.Counter;
            long start = starts[logic];
            // 记录完毕后复位，避免同一线程跨帧残留。
            starts[logic] = 0L;
            if (start == 0L)
            {
                // 对应的 Begin 被跳过（分析当时处于关闭状态等），忽略。
                return;
            }
            long delta = now - start;
            // 过滤掉不合理的跨度（例如异常导致 Begin/End 严重错配，超过 1 秒）。
            if (delta > 0L && delta < frequency)
            {
                Interlocked.Add(ref entries[logic].AccumTicks, delta);
            }
        }

        /// <summary>
        /// 线程安全版本的 Begin。本模组的 BeginSample/EndSample 本身即线程安全，
        /// 这里的 threadIndex 参数仅为兼容旧调用而保留，不再需要。
        /// </summary>
        public static void BeginSampleSafe(int logic, int threadIndex)
        {
            BeginSample(logic);
        }

        public static void EndSampleSafe(int logic, int threadIndex)
        {
            EndSample(logic);
        }

        private static long[] ResizeThreadStarts(long[] old)
        {
            // 快照当前项数量（注册主要在 Awake 阶段完成，运行期数量基本稳定）。
            int count;
            lock (entries)
            {
                count = entries.Count;
            }
            long[] arr = new long[count < 16 ? 16 : count];
            if (old != null && old.Length <= arr.Length)
            {
                Array.Copy(old, arr, old.Length);
            }
            threadStartTicks = arr;
            return arr;
        }

        // ---------------- 逐帧维护 ----------------

        /// <summary>
        /// 每逻辑帧调用一次：把本帧累计的刻度换算为秒并做滑动平均，然后清零帧累加。
        /// 平滑系数与游戏 SlideAverage 采用的窗口保持一致，观感与原生统计项同步。
        /// </summary>
        public static void OnFrameAverage()
        {
            if (entries.Count == 0)
            {
                return;
            }
            EnsureFrequency();

            long aveFrame = PerformanceMonitor.aveFrame;
            double factor = aveFrame > 1L ? 1.0 / aveFrame : 1.0;
            double freqInv = 1.0 / frequency;

            for (int i = 0; i < entries.Count; i++)
            {
                CpuEntry e = entries[i];
                long ticks = Interlocked.Exchange(ref e.AccumTicks, 0L);
                double frameSeconds = ticks > 0L ? ticks * freqInv : 0.0;
                e.Average = e.Average * (1.0 - factor) + frameSeconds * factor;
                e.Showing = e.Average;
            }
        }

        /// <summary>清空所有累计与平均数据（面板打开/关闭时随游戏一同复位）。</summary>
        public static void Reset()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                CpuEntry e = entries[i];
                Interlocked.Exchange(ref e.AccumTicks, 0L);
                e.Average = 0.0;
                e.Showing = 0.0;
            }
        }
    }

    /// <summary>
    /// 巨构性能统计项的注册入口。为便于在需要的地方新增/调整统计项，
    /// 这里集中声明层级与名称，并保持与 <see cref="ECpuWorkEntryExtended"/> 一致的顺序。
    /// </summary>
    public static class MMSPF
    {
        public static void AddMMSSamples()
        {
            // 顶层：所有巨构相关逻辑的合计。
            int root = MMSCPU.AddEntry("PF巨构总览".Translate(), 0);
            MMSCPU.MapEnum(ECpuWorkEntryExtended.MoreMegaStructure, root);

            // 主要子项
            //MMSCPU.MapEnum(ECpuWorkEntryExtended.MainLogic, MMSCPU.AddEntry("PF主要逻辑", 1));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.Receiver, MMSCPU.AddEntry("PF接收器".Translate(), 1));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.StarAssembly, MMSCPU.AddEntry("PF星际组装厂".Translate(), 1));
            //MMSCPU.MapEnum(ECpuWorkEntryExtended.CargoProduce, MMSCPU.AddEntry("PF货物生产".Translate(), 2));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.CargoTransport, MMSCPU.AddEntry("PF货物输送".Translate(), 2));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.StarCannon, MMSCPU.AddEntry("PF恒星炮".Translate(), 1));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.AttackLogic, MMSCPU.AddEntry("PF攻击逻辑".Translate(), 2));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.EnemySearch, MMSCPU.AddEntry("PF寻敌逻辑".Translate(), 2));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.WarpField, MMSCPU.AddEntry("PF折跃场".Translate(), 1));

            // 其他统计项
            MMSCPU.MapEnum(ECpuWorkEntryExtended.Statistics, MMSCPU.AddEntry("PF巨构数据统计".Translate(), 1));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.Patch, MMSCPU.AddEntry("PF其他补丁".Translate(), 1));
        }
    }
}
