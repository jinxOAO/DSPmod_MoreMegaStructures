using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace MoreMegaStructure
{
    public class UIPerformancePanelPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIPerformancePanel), "RefreshCpuStatTexts")]
        public static void Refresh(ref UIPerformancePanel __instance)
        {
            StringBuilder stringBuilder = new StringBuilder(__instance.cpuLabelText.text, 600 + PerformanceMonitorPatcher.cpuExtendedLength * 10);
            StringBuilder stringBuilder2 = new StringBuilder(__instance.cpuValueText1.text, 600 + PerformanceMonitorPatcher.cpuExtendedLength * 10);
            StringBuilder stringBuilder3 = new StringBuilder(__instance.cpuValueText2.text, 600 + PerformanceMonitorPatcher.cpuExtendedLength * 10);

            //AppendCpuStatEntry(ECpuWorkEntryExtended.MoreMegaStructure, stringBuilder, stringBuilder2, stringBuilder3);
            //AppendCpuStatEntry(ECpuWorkEntryExtended.MainLogic, stringBuilder, stringBuilder2, stringBuilder3);
            //AppendCpuStatEntry(ECpuWorkEntryExtended.Receiver, stringBuilder, stringBuilder2, stringBuilder3);
            //AppendCpuStatEntry(ECpuWorkEntryExtended.StarAssembly, stringBuilder, stringBuilder2, stringBuilder3);
            //AppendCpuStatEntry(ECpuWorkEntryExtended.StarCannon, stringBuilder, stringBuilder2, stringBuilder3);
            //AppendCpuStatEntry(ECpuWorkEntryExtended.EnemySearch, stringBuilder, stringBuilder2, stringBuilder3);
            //AppendCpuStatEntry(ECpuWorkEntryExtended.Statistics, stringBuilder, stringBuilder2, stringBuilder3);
            //AppendCpuStatEntry(ECpuWorkEntryExtended.Patch, stringBuilder, stringBuilder2, stringBuilder3);

            for (int i = 44; i < PerformanceMonitorPatcher.cpuExtendedLength; i++)
            {
                AppendCpuStatEntry(i, stringBuilder, stringBuilder2, stringBuilder3);
            }

            __instance.cpuLabelText.text = stringBuilder.ToString();
            __instance.cpuValueText1.text = stringBuilder2.ToString();
            __instance.cpuValueText2.text = stringBuilder3.ToString();
            __instance.cpuContentTrans.sizeDelta = new Vector2(__instance.cpuContentTrans.sizeDelta.x, __instance.cpuLabelText.preferredHeight - 10f);
        }


        public static void AppendCpuStatEntry(int entry, StringBuilder label, StringBuilder value1, StringBuilder value2)
        {
            double num = PerformanceMonitor.timeCostsShowing[(int)entry];
            if (num > -1.0)
            {
                int num2 = PerformanceMonitor.cpuWorkLevels[(int)entry];
                if (num2 < 0)
                {
                    int num3 = (int)PerformanceMonitor.cpuWorkParents[entry];
                    if (num3 > 0)
                    {
                        num2 = PerformanceMonitor.cpuWorkLevels[num3] + 1;
                    }
                }
                if (num2 != 2)
                {
                    if (num2 != 3)
                    {
                        label.AppendLine(PerformanceMonitor.cpuWorkNames[(int)entry].Translate());
                    }
                    else
                    {
                        label.AppendLine("    - " + PerformanceMonitor.cpuWorkNames[(int)entry].Translate());
                    }
                }
                else
                {
                    label.AppendLine("  > " + PerformanceMonitor.cpuWorkNames[(int)entry].Translate());
                }
                double num4 = PerformanceMonitor.timeCostsShowing[1];
                value1.AppendFormat("{0:0.000} ms\r\n", num * 1000.0);
                if (num4 < 1E-08)
                {
                    value2.AppendLine("0.0 %");
                    return;
                }
                value2.AppendFormat("{0:0.0} %\r\n", num / num4 * 100.0);
            }
        }


        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(UISectorGraph), "_OnOpen")]
        //public static bool UISectorGraphOnOpenPrePatch(ref UISectorGraph __instance)
        //{
        //    for (int i = 0; i < __instance.fanCount; i++)
        //    {
        //        if (__instance.fanDatas[i].level == -1)
        //        {
        //            if (__instance.fans[i] != null)
        //            {
        //                __instance.fans[i].gameObject.SetActive(false);
        //            }
        //        }
        //        else
        //        {
        //            UISectorFan uisectorFan = __instance.fans[i];
        //            if (uisectorFan == null)
        //            {
        //                uisectorFan = UnityEngine.Object.Instantiate<UISectorFan>(__instance.fanPrefab, __instance.levelGroups[__instance.fanDatas[i].level]);
        //            }
        //            uisectorFan.fanImage.sprite = __instance.levelSprites[__instance.fanDatas[i].level];
        //            uisectorFan.fanImage.color = __instance.colors[i % __instance.colors.Length];
        //            uisectorFan.gameObject.SetActive(true);
        //            __instance.fans[i] = uisectorFan;
        //        }
        //    }
        //    return false;
        //}
    }
}
