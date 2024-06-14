using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace MoreMegaStructure
{
    public enum ECpuWorkEntryExtended
    {
        MoreMegaStructure=44,
        MainLogic,
        Receiver,
        StarAssembly,
        StarCannon,
        EnemySearch,
        Statistics,
        Patch
    }

    public class PerformanceMonitorPatcher
    {
        public static int cpuExtendedLength = 44; // 游戏原本的数组长度是43，但是ECpuWorkEntry枚举具有值=43的Max项，因此，跳过43号，所有新增的从44号开始，因此增加后的数组长度从45开始

        public static List<string> cpuWorkNamesAppend = new List<string>();
        public static List<int> cpuWorkLevelsAppend = new List<int>();
        public static List<int> cpuWorkParentsAppend = new List<int>();

        

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PerformanceMonitor), "Awake")]
        public static void Init()
        {
            Utils.Log("Editing Additional PerfromanceMonitor Fields.");
            PerformanceMonitor.timeCostsAve = new double[cpuExtendedLength];
            PerformanceMonitor.timeCostsShowing = new double[cpuExtendedLength];
            PerformanceMonitor.timeCostsFrame = new double[cpuExtendedLength];
            PerformanceMonitor.clocks = new HighStopwatch[cpuExtendedLength];
            for (int i = 0; i < cpuExtendedLength; i++)
            {
                PerformanceMonitor.clocks[i] = new HighStopwatch();
            }

            // 以下处理新增的sample项
            string[] appendedNames = new string[cpuExtendedLength];
            int[] appendedLevels = new int[cpuExtendedLength];
            ECpuWorkEntry[] appendedParents = new ECpuWorkEntry[cpuExtendedLength];
            Array.Copy(PerformanceMonitor.cpuWorkNames, appendedNames, 43);
            Array.Copy(PerformanceMonitor.cpuWorkLevels, appendedLevels, 43);
            Array.Copy(PerformanceMonitor.cpuWorkParents, appendedParents, 43);
            appendedNames[43] = "";
            appendedLevels[43] = 0;
            appendedParents[43] = ECpuWorkEntry.Null;
            for (int i = 44; i < cpuExtendedLength; i++)
            {
                appendedNames[i] = cpuWorkNamesAppend[i - 44];
                appendedLevels[i] = cpuWorkLevelsAppend[i - 44];
                appendedParents[i] = (ECpuWorkEntry)cpuWorkParentsAppend[i - 44];
            }
            PerformanceMonitor.cpuWorkNames = appendedNames;
            PerformanceMonitor.cpuWorkLevels = appendedLevels;
            PerformanceMonitor.cpuWorkParents = appendedParents;
        }

        // 在mod的Awake调用此项添加一个CPUSample项，返回其logic序号，从44开始，对于name，需要调用者自行处理本地化翻译
        // Use this in Mod's Awake() phase
        public static int AddCpuSampleLogic(string name, int level = 0, int parentIndex = 0)
        {
            cpuExtendedLength++;
            cpuWorkParentsAppend.Add(parentIndex);
            cpuWorkNamesAppend.Add(name);
            cpuWorkLevelsAppend.Add(level);
            return cpuExtendedLength - 1;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(PerformanceMonitor), "BeginLogicFrame")]
        public static void BeginLogicFramePostPatch()
        {
            int num = cpuExtendedLength - 43;
            Array.Clear(PerformanceMonitor.timeCostsFrame, 43, num);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(PerformanceMonitor), "ResetStatistics")]
        public static void ResetPostPatch()
        {
            int num = cpuExtendedLength - 43;
            Array.Clear(PerformanceMonitor.timeCostsShowing, 43, num);
            Array.Clear(PerformanceMonitor.timeCostsAve, 43, num);
            Array.Clear(PerformanceMonitor.timeCostsFrame, 43, num);
            Utils.Log($"cleared, and num is {num}, after, timeShowing is {PerformanceMonitor.timeCostsShowing[45]} / {PerformanceMonitor.timeCostsAve[45]} / {PerformanceMonitor.timeCostsFrame[45]}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PerformanceMonitor), "EndLogicFrame")]
        public static void EndLogicPostPatch()
        {

            if (PerformanceMonitor.CpuProfilerOn)
            {
                double num = (double)(Time.fixedDeltaTime * 2f);
                if (num > 1.0)
                {
                    num = 1.0;
                }
                else if (num > 0.2)
                {
                    num = 0.2;
                }
                for (int i = 43; i < cpuExtendedLength; i++)
                {
                    double num3 = (PerformanceMonitor.timeCostsAve[i] == 0.0) ? 1.0 : num;
                    PerformanceMonitor.timeCostsAve[i] = PerformanceMonitor.timeCostsAve[i] * (1.0 - num3) + PerformanceMonitor.timeCostsFrame[i] * num3;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DysonSphere), "RocketGameTick", new Type[] {typeof(int), typeof(int), typeof(int)})]
        public static void RocketMultiThreadPostPatch()
        {
            Utils.Log( Thread.CurrentThread.ManagedThreadId.ToString());
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorkerThreadExecutor), "ComputerThread")]
        public static void CTest(WorkerThreadExecutor __instance)
        {
            Utils.Log($"multi Thread enabled with {__instance.threadMissionOrders}");
        }
    }

    public class MMSCPU
    {
        public static void BeginSample(int logic)
        {
            if (PerformanceMonitor.CpuProfilerOn)
            {
                PerformanceMonitor.clocks[logic].Begin();
            }
            PerformanceMonitor.cpuCounter++;
        }

        public static void EndSample(int logic)
        {
            if (PerformanceMonitor.CpuProfilerOn)
            {
                PerformanceMonitor.timeCostsFrame[logic] += PerformanceMonitor.clocks[logic].duration;
            }
            PerformanceMonitor.cpuCounter++;
        }

        public static void BeginSampleSafe(int logic, int threadIndex)
        {

        }

        public static void EndSampleSafe(int logic, int threadIndex)
        {

        }

        public static void BeginSample(ECpuWorkEntryExtended logic)
        {
            if (PerformanceMonitor.CpuProfilerOn)
            {
                PerformanceMonitor.clocks[(int)logic].Begin();
            }
            PerformanceMonitor.cpuCounter++;
        }

        public static void EndSample(ECpuWorkEntryExtended logic)
        {
            if (PerformanceMonitor.CpuProfilerOn)
            {
                PerformanceMonitor.timeCostsFrame[(int)logic] += PerformanceMonitor.clocks[(int)logic].duration;
            }
            PerformanceMonitor.cpuCounter++;
        }
    }
    
    public class MMSPF
    {
        public static void AddMMSSamples()
        {
            PerformanceMonitorPatcher.AddCpuSampleLogic("PF巨构", 1, 2);
            int mainLogic = PerformanceMonitorPatcher.AddCpuSampleLogic("PF巨构主要逻辑", 2);
            PerformanceMonitorPatcher.AddCpuSampleLogic("PF接收器", -1, mainLogic);
            PerformanceMonitorPatcher.AddCpuSampleLogic("PF星际组装厂", -1, mainLogic);
            PerformanceMonitorPatcher.AddCpuSampleLogic("PF恒星炮", -1, mainLogic);
            PerformanceMonitorPatcher.AddCpuSampleLogic("PF寻敌逻辑", -1, mainLogic);
            PerformanceMonitorPatcher.AddCpuSampleLogic("PF巨构数据统计", 2);
            PerformanceMonitorPatcher.AddCpuSampleLogic("PFOtherPatches", 2);

        }
    }
}
