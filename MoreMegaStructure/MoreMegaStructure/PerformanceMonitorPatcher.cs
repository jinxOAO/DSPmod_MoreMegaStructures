using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace MoreMegaStructure
{
    /// <summary>
    /// 自定义性能统计项的标识。这些值仅作为本模组内部的采样点编号使用，
    /// 与游戏的 <see cref="ECpuWorkEntry"/> 的取值无关。文本列表按注册顺序显示；
    /// 饼图在面板初始化时会把这些项并入游戏原生数组尾部（详见 MMSCPU.RebuildChartArrays）。
    /// 新增项请追加到末尾，避免影响第三方对本枚举数值的既有理解。
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
        Patch,
        // 追加项：恒星炮的渲染期与特效结算开销（OnRenderObject / LateUpdate / 弹头特效抑制）
        StarCannonRender
    }

    /// <summary>
    /// 一次 BeginSample 的结果凭证。
    /// 语义与游戏原版 DeepProfiler 的"同类嵌套只算最外层"一致：只有最外层（IsOwner=true）
    /// 的 End 才会真正结算耗时，内层 End 只退栈、不会覆盖或清零外层的起始刻度。
    /// Index = -1 表示本次没有占用槽位（CPU 分析关闭、索引非法等），对应的 End 直接跳过。
    /// </summary>
    public struct CpuHandle
    {
        /// <summary>采样点内部索引；-1 表示未占用槽位。</summary>
        public int Index;

        /// <summary>本层是否是该采样点在本线程上的最外层括号。</summary>
        public bool IsOwner;

        public CpuHandle(int index, bool isOwner)
        {
            Index = index;
            IsOwner = isOwner;
        }

        /// <summary>未占用槽位时的凭证（End 会安全跳过）。</summary>
        public static CpuHandle Invalid
        {
            get { return new CpuHandle(-1, false); }
        }
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
    /// 设计要点（与游戏原版 DeepProfiler 的三条纪律对齐）：
    /// - 完全自持有数据，不读写 PerformanceMonitor 的任何数组，避免与游戏更新冲突。
    /// - 重入安全：每个采样点在每条线程上维护"深度 + 起始刻度 + 所属纪元"三份线程局部状态。
    ///   同一 index 嵌套时，内层 Begin 不覆盖外层的起始刻度，内层 End 也只退栈不清零，
    ///   因此绝不会出现在"子项拿到全跨度、父项整段计时被吞掉"的情形
    ///   （旧实现只有一格时间戳，内层 End 清零会让外层 End 静默丢弃全部耗时）。
    /// - 并发安全：线程局部状态按线程各持一份，因此两条线程同时进入同一 index 不会被误判为嵌套，
    ///   各自独立结算并累加。累加用 Interlocked，因此既可在主线程使用，也可在工厂逻辑的工作线程使用。
    /// - 泄漏自愈：提前 return 造成的未配对括号，其残留会在"落后当前纪元 2 帧以上"时被作废重开，
    ///   因此不会像纯深度计数那样让某个统计项永久变黑；容忍 1 帧是为了不误伤工作线程上跨纪元的合法长括号。
    /// - 父子聚合：显示值 = max(自测括号耗时, Σ 子项显示值)。自测括号保证不会低报"未归入任何子项"
    ///   的那段时间，Σ 子项则保证当某子项的括号超出父项覆盖范围时（例如渲染期挂点）父项仍包得住它。
    ///   被 Σ 抬高的差额记录在 GetOverflowSeconds 中，用于暴露"层级声明与实际包裹不符"的挂点。
    /// - 口径（重要）：显示数值只取主线程墙钟耗时，绝不把多条线程的耗时相加。
    ///   同一统计项若在工作线程上执行，其耗时单独记入 GetWorkerSeconds 供面板另列展示，
    ///   不参与显示数值与父子聚合——多线程并发时相加得到的是 CPU 时间总和而非墙上占用，
    ///   会让数值虚高甚至超过帧时间，游戏原版同样是把并行段单列成 Parallel-LogicTick 等条目。
    /// - 只有当游戏 CPU 分析处于开启状态时才真正采样，关闭时 Begin/End 近乎零开销。
    /// - 饼图集成：在面板初始化时把各项并入游戏原生数组尾部（RebuildChartArrays），
    ///   父子关系由 level + 注册顺序自动推导；level 0 项挂在饼图根(Total)下，因此不会虚增总计。
    /// </summary>
    public static class MMSCPU
    {
        private sealed class CpuEntry
        {
            public string Name;
            public int Level;       // 显示缩进层级：0 顶层，1 一级，2 二级
            public int Parent = -1; // 聚合用的父项内部索引；-1 表示自身即顶层
            public long AccumTicksMain;   // 本帧主线程累计的高精度计时刻度（墙钟）
            public long AccumTicksWorker; // 本帧工作线程累计的高精度计时刻度（单独列示，不相加）
            public double OwnAverage;     // 仅主线程自测括号的滑动平均，单位：秒
            public double AverageMain;    // 主线程分量（即参与显示与聚合的墙钟量），单位：秒
            public double AverageWorker;  // 工作线程分量，单位：秒；仅供另列展示与诊断
            public double Average;        // 参与聚合后的最终显示值（主线程墙钟，含子项兜底），单位：秒
            public double Showing;        // 供面板与饼图显示的数值（= Average），单位：秒
            public double Overflow;       // 本帧被子项抬高出的差额（父项括号没罩住子项的部分）
        }

        private static readonly List<CpuEntry> entries = new List<CpuEntry>();

        // ECpuWorkEntryExtended -> 内部索引 的映射表。注册时按枚举顺序填充。
        private static readonly Dictionary<ECpuWorkEntryExtended, int> enumMap =
            new Dictionary<ECpuWorkEntryExtended, int>();

        // ------- 线程局部括号状态（每条线程各一份，因此并发不会被当成嵌套） -------
        // 起始刻度；0 表示当前深度为 0（无活跃括号）。
        [ThreadStatic]
        private static long[] threadStartTicks;

        // 该采样点在本线程上最外层括号的起始所属纪元（每次 OnFrameAverage 递增一次）。
        [ThreadStatic]
        private static long[] threadStampEpoch;

        // 该采样点在本线程上当前打开的括号层数（含内层）。
        [ThreadStatic]
        private static int[] threadDepth;

        // 帧纪元：由结算线程每帧推进一次，用于识别"上一帧残留的未配对括号"。
        private static long frameEpoch;

        // 残留判定容忍的帧数：只有落后 >= 该值才作废重开，避免误伤跨纪元仍在进行的长括号。
        private const long StaleEpochGap = 2L;

        private static long frequency = 0L;

        // 主线程的托管线程 id，在 Awake 注册阶段捕获；用于拆分主线程/工作线程分量。
        private static int mainThreadId = -1;

        // ------- 诊断（验证层级问题用，定稿后可把 VerifyMode 置 false） -------
        /// <summary>为 true 时定期把重入/配对/层级异常计数打到游戏日志，供验证完毕后关闭。</summary>
        public static bool VerifyMode = true;

        /// <summary>内层 Begin 次数（同线程同 index 重入，说明存在真实嵌套）。</summary>
        public static long NestedBeginCount;

        /// <summary>End 时发现无对应 Begin 的次数（例如 CPU 分析中途开启，或 Begin/End 写反）。</summary>
        public static long OrphanEndCount;

        /// <summary>未配对括号残留被作废重开的次数（说明存在提前 return 泄漏）。</summary>
        public static long StaleResetCount;

        private static double[] childSumBuffer = new double[16];

        // ------- 饼图集成状态（仅在主线程、面板初始化时写入） -------
        // 自定义项被并入原生数组尾部后，第 i 个自定义项对应的绝对下标为 ChartBase + i。
        // ChartBase < 0 表示当前尚未并入（例如面板从未打开过）。
        private static int ChartBase = -1;
        private static int ChartCount = -1;

        // 原生（未被本模组扩展过的）数组快照，首次扩展时捕获，之后每次重建都以此为基准，
        // 避免把上一轮自己写入的尾部误当成原生数据而不断累加。
        private static string[] pristineNames;
        private static int[] pristineLevels;
        private static ECpuWorkEntry[] pristineParents;

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
                RecomputeParents();
                return entries.Count - 1;
            }
        }

        /// <summary>
        /// 供依赖本模组的第三方 mod 注册自定义性能统计项。
        /// 若性能面板的饼图已经初始化过（ChartBase 已建立），会在主线程安全地重建尾部数组，
        /// 保证迟到的注册项也能获得对应扇区；未重建过则仅登记，等面板打开时自然并入。
        /// 返回值配合 BeginSample/EndSample 使用；重复调用本方法注册"同名项"会产生新条目，
        /// 调用方应自行保证每项只注册一次（如放在自己插件的 Awake 中）。
        /// </summary>
        public static int RegisterEntry(string localizationKey, int level)
        {
            lock (entries)
            {
                entries.Add(new CpuEntry { Name = localizationKey, Level = level });
                EnsureFrequency();
                RecomputeParents();
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
            // 注册发生在 Awake 阶段的主线程上，在此捕获主线程 id 最稳妥。
            Interlocked.CompareExchange(ref mainThreadId, Thread.CurrentThread.ManagedThreadId, -1);
        }

        /// <summary>显式声明"当前线程就是主线程"，供非 Awake 路径的初始化调用。</summary>
        public static void MarkMainThread()
        {
            Interlocked.CompareExchange(ref mainThreadId, Thread.CurrentThread.ManagedThreadId, -1);
        }

        /// <summary>
        /// 按 level + 注册顺序推导每个项的父项内部索引（父项必须注册在其子项之前）。
        /// - level 0：无父项（自身即顶层，饼图上挂在 Total 下）。
        /// - level 1：父项为最近一个 level 0 项。
        /// - level >= 2：父项为最近的 level 1 项，不存在则退到最近的 level 0 项。
        /// </summary>
        private static void RecomputeParents()
        {
            int lastLevel0 = -1;
            int lastLevel1 = -1;
            for (int i = 0; i < entries.Count; i++)
            {
                CpuEntry e = entries[i];
                int lvl = e.Level;
                if (lvl <= 0)
                {
                    e.Parent = -1;
                    lastLevel0 = i;
                    lastLevel1 = -1;
                }
                else if (lvl == 1)
                {
                    e.Parent = lastLevel0;
                    lastLevel1 = i;
                }
                else
                {
                    e.Parent = lastLevel1 >= 0 ? lastLevel1 : lastLevel0;
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
            return e != null ? e.Name.Translate() : "";
        }

        public static int GetLevel(int index)
        {
            CpuEntry e = GetEntry(index);
            return e != null ? e.Level : 0;
        }

        /// <summary>该采样点当前用于显示的耗时（秒）。口径为主线程墙钟，已含子项兜底。</summary>
        public static double GetShowingSeconds(int index)
        {
            CpuEntry e = GetEntry(index);
            return e != null ? e.Showing : 0.0;
        }

        /// <summary>仅主线程自测括号得到的耗时（秒），未经子项兜底抬高。</summary>
        public static double GetOwnSeconds(int index)
        {
            CpuEntry e = GetEntry(index);
            return e != null ? e.OwnAverage : 0.0;
        }

        /// <summary>主线程分量（秒），即参与显示与聚合的墙钟量。</summary>
        public static double GetMainSeconds(int index)
        {
            CpuEntry e = GetEntry(index);
            return e != null ? e.AverageMain : 0.0;
        }

        /// <summary>
        /// 工作线程分量（秒）。<b>不计入显示数值，也不参与父子聚合</b>，
        /// 只为避免把并发线程的 CPU 时间加成并不存在的墙钟占用；需要时单独列示。
        /// </summary>
        public static double GetWorkerSeconds(int index)
        {
            CpuEntry e = GetEntry(index);
            return e != null ? e.AverageWorker : 0.0;
        }

        /// <summary>
        /// 该项被子项抬高出的差额（秒）。非 0 说明有子项的采样区间超出了本项自己的括号范围，
        /// 应当在子项处补上本项的父括号，否则自测值会系统性低报。
        /// </summary>
        public static double GetOverflowSeconds(int index)
        {
            CpuEntry e = GetEntry(index);
            return e != null ? e.Overflow : 0.0;
        }

        /// <summary>该索引对应枚举是否已注册。</summary>
        public static bool IsMapped(ECpuWorkEntryExtended logic)
        {
            return enumMap.ContainsKey(logic);
        }

        private static CpuEntry GetEntry(int index)
        {
            if (index < 0 || index >= entries.Count)
            {
                return null;
            }
            return entries[index];
        }

        // ---------------- 采样 API ----------------
        // 主 API 是 BeginSampleSafe / EndSampleSafe：Begin 返回 CpuHandle 凭证，End 接住它即可，
        // 便于 try/finally 配对，也不会出现"忘了 End / 多写 End"。
        // 不带 Safe 的 void 版本（BeginSample/EndSample）继续保留：既有调用点忽略返回值同样正确，
        // 因为 EndSample(int) 走 threadDepth 退栈，与 Begin 的加深度严格配对。

        public static CpuHandle BeginSampleSafe(ECpuWorkEntryExtended logic)
        {
            int index;
            if (enumMap.TryGetValue(logic, out index))
            {
                return BeginSampleSafe(index);
            }
            return CpuHandle.Invalid;
        }

        public static void EndSampleSafe(ECpuWorkEntryExtended logic)
        {
            int index;
            if (enumMap.TryGetValue(logic, out index))
            {
                EndSample(index);
            }
        }

        public static CpuHandle BeginSampleSafe(int logic)
        {
            // 关闭 CPU 分析时几乎零开销：仅一次布尔判断即返回。
            if (!PerformanceMonitor.CpuProfilerOn)
            {
                return CpuHandle.Invalid;
            }
            if (logic < 0 || logic >= entries.Count)
            {
                return CpuHandle.Invalid;
            }
            if (threadStartTicks == null || threadStartTicks.Length <= logic)
            {
                ResizeThreadState(threadStartTicks);
            }

            long epoch = Volatile.Read(ref frameEpoch);
            // 槽位上已有活跃括号：区分"真嵌套"与"上一帧泄漏的残留"。
            if (threadDepth[logic] > 0)
            {
                if (epoch - threadStampEpoch[logic] < StaleEpochGap)
                {
                    // 本纪元（或紧邻的上一纪元，可能是工作线程上的长括号）内确实还在计数中：
                    // 只加深层次，绝不覆盖外层的起始刻度，也不改动纪元。
                    threadDepth[logic]++;
                    Interlocked.Increment(ref NestedBeginCount);
                    return new CpuHandle(logic, false);
                }
                // 残留：上一帧某处的提前 return 或异常让括号没有关闭。直接作废重开，
                // 否则该 index 在这条线程上会永远停在"计数中"，此后每次 Begin 都退化为内层而永久变黑。
                Interlocked.Increment(ref StaleResetCount);
            }

            threadDepth[logic] = 1;
            threadStartTicks[logic] = PerformanceCounter.Counter;
            threadStampEpoch[logic] = epoch;
            return new CpuHandle(logic, true);
        }

        /// <summary>按 Begin 返回的凭证结束一次采样。未占用槽位时（Index &lt; 0）安全跳过。</summary>
        public static void EndSampleSafe(CpuHandle handle)
        {
            if (handle.Index >= 0)
            {
                EndSample(handle.Index);
            }
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
                // 本线程从未 Begin 过（例如 CPU 分析是在本段代码中间才开启的）。
                Interlocked.Increment(ref OrphanEndCount);
                return;
            }
            int depth = threadDepth[logic];
            if (depth <= 0)
            {
                // 没有配对的 Begin（同上原因），忽略，绝不能把外层括号的槽位清零。
                Interlocked.Increment(ref OrphanEndCount);
                return;
            }
            depth--;
            threadDepth[logic] = depth;
            if (depth > 0)
            {
                // 内层退出：外层仍在计数中，保留起始刻度，由外层的 End 结算全跨度。
                return;
            }

            long start = starts[logic];
            starts[logic] = 0L;
            long delta = PerformanceCounter.Counter - start;
            // 过滤掉不合理的跨度（例如异常导致 Begin/End 严重错配，超过 1 秒）。
            if (delta > 0L && delta < frequency)
            {
                CpuEntry e = entries[logic];
                if (Thread.CurrentThread.ManagedThreadId == mainThreadId)
                {
                    Interlocked.Add(ref e.AccumTicksMain, delta);
                }
                else
                {
                    Interlocked.Add(ref e.AccumTicksWorker, delta);
                }
            }
        }

        public static void BeginSample(int logic)
        {
            BeginSampleSafe(logic);
        }

        // ---- 以下 void 重载是旧版公开 API，为第三方 mod 兼容而保留 ----
        // 依赖本模组的 mod 可能按 (ECpuWorkEntryExtended) 签名编译过 Begin/End，
        // 若这两个方法消失，它们会在运行时抛 MissingMethodException。行为与 Safe 版一致，
        // 只是丢弃返回的凭证：内层退栈与外层结算仍由 threadDepth 严格配对，不会错账。
        public static void BeginSample(ECpuWorkEntryExtended logic)
        {
            BeginSampleSafe(logic);
        }

        public static void EndSample(ECpuWorkEntryExtended logic)
        {
            EndSampleSafe(logic);
        }

        private static void ResizeThreadState(long[] old)
        {
            // 快照当前项数量（注册主要在 Awake 阶段完成，运行期数量基本稳定）。
            int count;
            lock (entries)
            {
                count = entries.Count;
            }
            int len = count < 16 ? 16 : count;

            long[] starts = new long[len];
            long[] epochs = new long[len];
            int[] depths = new int[len];
            if (old != null)
            {
                // 容量只会增长，且必须一次把三份状态都复制过去，否则旧的活跃括号会被误判为残留。
                int n = Math.Min(old.Length, len);
                long[] oldEpochs = threadStampEpoch;
                int[] oldDepths = threadDepth;
                Array.Copy(old, starts, n);
                if (oldEpochs != null)
                {
                    Array.Copy(oldEpochs, epochs, Math.Min(oldEpochs.Length, len));
                }
                if (oldDepths != null)
                {
                    Array.Copy(oldDepths, depths, Math.Min(oldDepths.Length, len));
                }
            }
            threadStartTicks = starts;
            threadStampEpoch = epochs;
            threadDepth = depths;
        }

        // ---------------- 饼图集成 ----------------

        /// <summary>
        /// 在性能面板初始化（_OnInit）之前于主线程调用：把本模组的自定义项并入饼图所需的
        /// 原生数组尾部（cpuWorkNames / cpuWorkLevels / cpuWorkParents / timeCostsAve / timeCostsShowing）。
        /// 之后饼图 _Init 读取到更长的 timeCostsShowing，便会为每一项建立对应扇区。
        ///
        /// 父子关系由 RecomputeParents 按 level + 注册顺序推导，无需显式指定 parent，
        /// 因此只要在 MMSPF.AddMMSSamples 里按"父项在前、子项紧随其后"的顺序注册即可。
        /// level 0 项挂在饼图根 Total 之下，与游戏原生顶层项并列（不计入总计，避免虚增）。
        /// 重复调用是安全的：以首次捕获的原生快照为基准重建，不会累加。
        /// </summary>
        public static void RebuildChartArrays()
        {
            string[] names = PerformanceMonitor.cpuWorkNames;
            int[] levels = PerformanceMonitor.cpuWorkLevels;
            ECpuWorkEntry[] parents = PerformanceMonitor.cpuWorkParents;
            if (names == null || levels == null || parents == null)
            {
                return;
            }

            // 首次（尚未扩展过时）捕获原生快照作为恒定基准。
            if (pristineNames == null)
            {
                if (ChartBase >= 0)
                {
                    // 已扩展过却丢失快照，属异常状态，保守放弃重建。
                    return;
                }
                pristineNames = (string[])names.Clone();
                pristineLevels = (int[])levels.Clone();
                pristineParents = (ECpuWorkEntry[])parents.Clone();
            }

            int baseLen = pristineNames.Length;
            int n = entries.Count;
            if (n == 0)
            {
                ChartBase = baseLen;
                ChartCount = 0;
                return;
            }
            int totalLen = baseLen + n;
            ChartBase = baseLen;
            ChartCount = n;

            string[] newNames = new string[totalLen];
            int[] newLevels = new int[totalLen];
            ECpuWorkEntry[] newParents = new ECpuWorkEntry[totalLen];
            Array.Copy(pristineNames, 0, newNames, 0, baseLen);
            Array.Copy(pristineLevels, 0, newLevels, 0, baseLen);
            Array.Copy(pristineParents, 0, newParents, 0, baseLen);

            int totalIndex = (int)ECpuWorkEntry.Total;
            // 环数由游戏预制体决定（levelSprites/levelGroups 的数量）。取原生最大层级为上限，
            // 避免自定义项指定了不存在的环层导致越界崩溃。
            int maxLevel = 0;
            for (int i = 0; i < baseLen; i++)
            {
                if (pristineLevels[i] > maxLevel)
                {
                    maxLevel = pristineLevels[i];
                }
            }

            RecomputeParents();
            for (int i = 0; i < n; i++)
            {
                CpuEntry e = entries[i];
                int idx = baseLen + i;
                newNames[idx] = e.Name;
                newLevels[idx] = e.Level < 0 ? 0 : (e.Level > maxLevel ? maxLevel : e.Level);
                // 内部父索引 -> 尾部数组的绝对下标；-1（顶层）挂在 Total 下。
                newParents[idx] = (ECpuWorkEntry)(e.Parent >= 0 ? baseLen + e.Parent : totalIndex);
            }

            PerformanceMonitor.cpuWorkNames = newNames;
            PerformanceMonitor.cpuWorkLevels = newLevels;
            PerformanceMonitor.cpuWorkParents = newParents;

            GrowTimeArray(ref PerformanceMonitor.timeCostsAve, baseLen, totalLen);
            GrowTimeArray(ref PerformanceMonitor.timeCostsShowing, baseLen, totalLen);
            // 注意：timeCostsFrame 为 private 且游戏只在 [0,48) 内使用，无需（也无法）扩展。
        }

        private static void GrowTimeArray(ref double[] arr, int baseLen, int totalLen)
        {
            double[] na = new double[totalLen];
            if (arr != null)
            {
                int c = Math.Min(arr.Length, baseLen);
                if (c > 0)
                {
                    Array.Copy(arr, 0, na, 0, c);
                }
            }
            arr = na;
        }

        // ---------------- 逐帧维护 ----------------

        /// <summary>
        /// 每帧调用一次：把本帧累计的刻度换算为秒并做滑动平均，随后自底向上做父子聚合，
        /// 最后清零帧累加并推进纪元。
        /// 平滑系数与游戏 SlideAverage 采用的窗口保持一致，观感与原生统计项同步。
        /// </summary>
        public static void OnFrameAverage()
        {
            // 纪元必须先推进再结算：本帧 Begin 打上的是推进前的纪元，
            // 因此下一帧 Begin 时"落后 1 帧"仍是合法在飞的括号，"落后 2 帧"才是泄漏残留。
            Interlocked.Increment(ref frameEpoch);

            int n = entries.Count;
            if (n == 0)
            {
                return;
            }
            EnsureFrequency();

            long aveFrame = PerformanceMonitor.aveFrame;
            double factor = aveFrame > 1L ? 1.0 / aveFrame : 1.0;
            double freqInv = 1.0 / frequency;

            // 若饼图已并入（面板打开过），把自定义值同步写回原生数组尾部供扇区读取。
            // 逐项再做下标界检查：这样即使有第三方在本面板初始化之后才注册出新的项，
            // 也只会丢掉那几项的扇区，而不会让整个自定义区块一起停止更新。
            double[] showArr = PerformanceMonitor.timeCostsShowing;
            double[] aveArr = PerformanceMonitor.timeCostsAve;
            bool pushToChart = ChartBase >= 0;

            // 第一趟：本帧刻度换算 + 滑动平均，得到各项"主线程自测括号"的耗时。
            // 工作线程分量各自平滑后单独保存，绝不并入 own —— 并发相加得到的是 CPU 时间总和，
            // 不是墙上占用，会把数值抬到超过帧时间。
            for (int i = 0; i < n; i++)
            {
                CpuEntry e = entries[i];
                long ticksMain = Interlocked.Exchange(ref e.AccumTicksMain, 0L);
                long ticksWorker = Interlocked.Exchange(ref e.AccumTicksWorker, 0L);
                double secMain = ticksMain > 0L ? ticksMain * freqInv : 0.0;
                double secWorker = ticksWorker > 0L ? ticksWorker * freqInv : 0.0;
                e.AverageMain = e.AverageMain * (1.0 - factor) + secMain * factor;
                e.AverageWorker = e.AverageWorker * (1.0 - factor) + secWorker * factor;
                e.OwnAverage = e.OwnAverage * (1.0 - factor) + secMain * factor;
            }

            // 第二趟：自底向上聚合（全部为主线程墙钟口径，因此同一条线程上的子项相加才有意义）。
            // 注册顺序保证父项索引恒小于子项索引，反向遍历一趟即可让每个子项先于父项完成结算。
            // 显示值 = max(主线程自测括号, Σ 子项显示值)：前者保证不低报未归入子项的代码，
            // 后者保证"子项区间超出父括号"时父项仍包得住（差额同时记入 Overflow 供诊断）。
            if (childSumBuffer.Length < n)
            {
                childSumBuffer = new double[Math.Max(n, childSumBuffer.Length * 2)];
            }
            Array.Clear(childSumBuffer, 0, n);
            for (int i = n - 1; i >= 0; i--)
            {
                CpuEntry e = entries[i];
                double own = e.OwnAverage;
                double sum = childSumBuffer[i];
                e.Overflow = sum > own ? sum - own : 0.0;
                double v = own > sum ? own : sum;
                e.Average = v;
                e.Showing = v;
                int p = e.Parent;
                if (p >= 0 && p < n)
                {
                    childSumBuffer[p] += v;
                }

                if (pushToChart)
                {
                    int ci = ChartBase + i;
                    if (showArr != null && ci < showArr.Length)
                    {
                        showArr[ci] = v;
                    }
                    if (aveArr != null && ci < aveArr.Length)
                    {
                        aveArr[ci] = v;
                    }
                }
            }

            if (VerifyMode && (frameEpoch & 511L) == 0L)
            {
                LogDiagnostics();
            }
        }

        /// <summary>
        /// 验证期诊断输出：重入/孤儿 End/残留自愈的累计次数，层级没罩住的项，
        /// 以及"只在 work 线程上跑、因而显示值为 0"的项（这类项的真实开销按口径不进显示数值，必须能看见）。
        /// 健康状态下（计数没有增长、也没有上述两种情形）完全不打印，因此不会污染游戏日志；
        /// 定稿后把 <see cref="VerifyMode"/> 置 false 可彻底移除这段逻辑。
        /// </summary>
        private static void LogDiagnostics()
        {
            try
            {
                long nested = NestedBeginCount;
                long orphan = OrphanEndCount;
                long stale = StaleResetCount;
                bool countersChanged = nested != lastDiagNested || orphan != lastDiagOrphan || stale != lastDiagStale;
                lastDiagNested = nested;
                lastDiagOrphan = orphan;
                lastDiagStale = stale;
        
                StringBuilder sb = null;
                StringBuilder mtOnly = null;
                for (int i = 0; i < entries.Count; i++)
                {
                    CpuEntry e = entries[i];
                    // 只在差额具备可感知量级时报告，避免正常抖动刷屏。
                    if (e.Overflow > 1.0E-05)
                    {
                        if (sb == null)
                        {
                            sb = new StringBuilder(256);
                            sb.Append("[MMSPerfDiag] epoch=").Append(frameEpoch)
                                .Append(" nested=").Append(nested)
                                .Append(" orphanEnd=").Append(orphan)
                                .Append(" staleReset=").Append(stale)
                                .Append(" overflow:");
                        }
                        sb.Append(' ').Append(e.Name).Append('=')
                            .Append((e.Overflow * 1000.0).ToString("0.000")).Append("ms")
                            .Append("(own=").Append((e.OwnAverage * 1000.0).ToString("0.000"))
                            .Append(",worker=").Append((e.AverageWorker * 1000.0).ToString("0.000")).Append(')');
                    }
                    // 工作线程有量、主线程却几乎为 0：说明该项完全跑在并行段，显示值按墙钟口径为 0。
                    if (e.AverageWorker > 1.0E-05 && e.AverageMain < 1.0E-06)
                    {
                        if (mtOnly == null)
                        {
                            mtOnly = new StringBuilder(128);
                            mtOnly.Append("[MMSPerfDiag] 纯并行项(显示值只算主线程墙钟，故为 0)：");
                        }
                        else
                        {
                            mtOnly.Append('、');
                        }
                        mtOnly.Append(e.Name).Append('=')
                            .Append((e.AverageWorker * 1000.0).ToString("0.000")).Append("ms");
                    }
                }
        
                if (sb != null)
                {
                    Utils.Log(sb.ToString());
                }
                else if (countersChanged)
                {
                    // 没有层级问题，但出现过重入/泄漏/残留：只报计数，说明机制在正常工作。
                    Utils.Log("[MMSPerfDiag] nested=" + nested + " orphanEnd=" + orphan + " staleReset=" + stale);
                }
                if (mtOnly != null)
                {
                    Utils.Log(mtOnly.ToString());
                }
            }
            catch
            {
                // 诊断本身绝不允许影响统计与游戏逻辑。
            }
        }
        
        private static long lastDiagNested;
        private static long lastDiagOrphan;
        private static long lastDiagStale;

        /// <summary>清空所有累计与平均数据（面板打开/关闭时随游戏一同复位）。</summary>
        public static void Reset()
        {
            double[] showArr = PerformanceMonitor.timeCostsShowing;
            double[] aveArr = PerformanceMonitor.timeCostsAve;
            bool pushToChart = ChartBase >= 0;

            for (int i = 0; i < entries.Count; i++)
            {
                CpuEntry e = entries[i];
                Interlocked.Exchange(ref e.AccumTicksMain, 0L);
                Interlocked.Exchange(ref e.AccumTicksWorker, 0L);
                e.AverageMain = 0.0;
                e.AverageWorker = 0.0;
                e.OwnAverage = 0.0;
                e.Average = 0.0;
                e.Showing = 0.0;
                e.Overflow = 0.0;

                if (pushToChart)
                {
                    int ci = ChartBase + i;
                    if (showArr != null && ci < showArr.Length)
                    {
                        showArr[ci] = 0.0;
                    }
                    if (aveArr != null && ci < aveArr.Length)
                    {
                        aveArr[ci] = 0.0;
                    }
                }
            }
            // 纪元不重置：线程局部的起始刻度带着旧纪元，若把纪元归零会让差值变负，
            // 反而把在飞的合法括号误判为嵌套。
        }
    }

    /// <summary>
    /// 巨构性能统计项的注册入口。为便于在需要的地方新增/调整统计项，
    /// 这里集中声明层级与名称，并保持与 <see cref="ECpuWorkEntryExtended"/> 一致的顺序。
    /// 必须遵循"父项在前、子项紧随其后"，父子关系由此推导（见 MMSCPU.RecomputeParents）。
    /// </summary>
    public static class MMSPF
    {
        public static void AddMMSSamples()
        {
            MMSCPU.MarkMainThread();

            // 顶层：所有巨构相关逻辑的合计。
            int root = MMSCPU.AddEntry("PF巨构总览", 0);
            MMSCPU.MapEnum(ECpuWorkEntryExtended.MoreMegaStructure, root);

            // 主要子项
            //MMSCPU.MapEnum(ECpuWorkEntryExtended.MainLogic, MMSCPU.AddEntry("PF主要逻辑", 1));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.Receiver, MMSCPU.AddEntry("PF接收器", 1));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.StarAssembly, MMSCPU.AddEntry("PF星际组装厂", 1));
            //MMSCPU.MapEnum(ECpuWorkEntryExtended.CargoProduce, MMSCPU.AddEntry("PF货物生产", 2));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.CargoTransport, MMSCPU.AddEntry("PF货物输送", 2));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.StarCannon, MMSCPU.AddEntry("PF恒星炮", 1));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.AttackLogic, MMSCPU.AddEntry("PF攻击逻辑", 2));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.EnemySearch, MMSCPU.AddEntry("PF寻敌逻辑", 2));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.StarCannonRender, MMSCPU.AddEntry("PF绘制调用", 2));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.WarpField, MMSCPU.AddEntry("PF折跃场", 1));

            // 其他统计项
            MMSCPU.MapEnum(ECpuWorkEntryExtended.Statistics, MMSCPU.AddEntry("PF巨构数据统计", 1));
            MMSCPU.MapEnum(ECpuWorkEntryExtended.Patch, MMSCPU.AddEntry("PF其他补丁", 1));
        }
    }
}
