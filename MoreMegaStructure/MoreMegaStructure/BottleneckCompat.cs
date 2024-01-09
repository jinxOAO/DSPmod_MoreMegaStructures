﻿using System.Collections.Generic;
using BepInEx;
using Bottleneck;
using Bottleneck.Stats;
using HarmonyLib;

namespace MoreMegaStructure
{
    [BepInDependency("Bottleneck")]
    [BepInPlugin("Gnimaerd.DSP.plugin.MMSBottleneckCompat", "MMSBottleneckCompat", "1.0")]
    public class MMSBottleneckCompat : BaseUnityPlugin
    {
        private static readonly List<int> StarAssemblyIndex = new List<int>();

        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(MMSBottleneckCompat));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "AstroBoxToValue")]
        public static bool AstroBoxToValuePrePatch(ref UIStatisticsWindow __instance)
        {
            if (!__instance.isStatisticsTab) return true;

            __instance.lastAstroFilter = __instance.astroFilter;

            if (__instance.astroBox.ItemsData.Count <= __instance.astroBox.itemIndex)
            {
                __instance.RefreshAll();
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BottleneckPlugin), "ProcessDeficitTask")]
        public static void BottleneckPostPatch(ref BottleneckPlugin __instance)
        {
            if (GameMain.instance.timei % 60 == 0) RefreshStarAssemblyIndex();

            // ReSharper disable Unity.NoNullPropagation
            UIStatisticsWindow uiStatisticsWindow = UIRoot.instance?.uiGame?.statWindow;
            if (uiStatisticsWindow == null) return;

            bool calcAll = false;
            bool calc = false;
            bool isExactlyStarAssembly = false; // 就只展示星际组装厂的产出，那么bottleneck的统计就不是+=而是赋值
            int curStarIndex = -1;

            int planetId = uiStatisticsWindow.astroFilter;
            if (planetId == -1 || planetId == 0 && GameMain.data.localStar == null)
            {
                calcAll = true;
                calc = true;
            }
            else if (planetId % 100 == 0)
            {
                int starIndex = planetId / 100 - 1;
                if (starIndex >= 0 && starIndex < 1000 && MoreMegaStructure.StarMegaStructureType[starIndex] == 4)
                {
                    calc = true;
                    curStarIndex = starIndex;
                }
            }
            else
            {
                int starIndex = planetId / 100 - 1;
                int planetIndex = planetId % 100 - 1;
                if (starIndex >= 0 && starIndex < 1000 && planetIndex == GameMain.galaxy.stars[starIndex].planetCount) // 说明是巨构
                {
                    calc = true;
                    curStarIndex = starIndex;
                    isExactlyStarAssembly = true;
                }
            }

            if (!calc) return;

            foreach (int starIndex in StarAssemblyIndex)
            {
                if (starIndex != curStarIndex && !calcAll) continue;
                if (GameMain.data == null ||
                    GameMain.data.dysonSpheres == null ||
                    GameMain.data.dysonSpheres.Length <= starIndex ||
                    GameMain.data?.dysonSpheres[starIndex] == null)
                    break;

                int maxSlot = StarAssembly.CalcMaxSlotIndex(GameMain.data?.dysonSpheres[starIndex]);
                Dictionary<int, int> consumeItems = new Dictionary<int, int>();
                Dictionary<int, int> produceItems = new Dictionary<int, int>();
                int calcIncLevel = 0;
                calcIncLevel = ResearchTechHelper.GetMaxIncIndex();
                for (int s = 1; s < maxSlot; s++)
                {
                    double cRatio = StarAssembly.GetConsumeProduceSpeedRatio(starIndex, s);
                    double pRatio; // 这个对于每个物品在显示时可能是不同的，因为玩家可能选择不计算增产模式或者计算增产加速模式
                    for (int i = 0; i < StarAssembly.items[starIndex][s].Count; i++)
                    {
                        int itemId = StarAssembly.items[starIndex][s][i];
                        consumeItems[itemId] = 1;
                        if (BetterStats.counter.ContainsKey(itemId))
                        {
                            if (isExactlyStarAssembly)
                                BetterStats.counter[itemId].consumption = (float)cRatio * StarAssembly.itemCounts[starIndex][s][i] * 3600;
                            else
                                BetterStats.counter[itemId].consumption += (float)cRatio * StarAssembly.itemCounts[starIndex][s][i] * 3600;
                        }
                        else
                        {
                            BetterStats.counter.Add(itemId, new BetterStats.ProductMetrics());
                            BetterStats.counter[itemId].consumption = (float)cRatio * StarAssembly.itemCounts[starIndex][s][i] * 3600;
                        }
                    }

                    for (int i = 0; i < StarAssembly.products[starIndex][s].Count; i++)
                    {
                        int itemId = StarAssembly.products[starIndex][s][i];
                        produceItems[itemId] = 1;

                        ItemCalculationRuntimeSetting itemCalculationRuntimeSetting = PluginConfig.disableProliferatorCalc.Value
                            ? ItemCalculationRuntimeSetting.None
                            : ItemCalculationRuntimeSetting.ForItemId(itemId);
                        int incLevel = itemCalculationRuntimeSetting.Mode == ItemCalculationMode.None || !itemCalculationRuntimeSetting.Enabled
                            ? 0
                            : ResearchTechHelper.GetMaxIncIndex();
                        // 由于星际组装厂只支持增产模式，所以只能按增产计算。incProgress<0在星际组装厂中作为recipe不可增产的标志。但是如果是特化，并且能够享受特化buff，则还能允许叠加增产剂增产！
                        if (StarAssembly.incProgress[starIndex][s] < 0)
                        {
                            if (StarAssembly.specBuffLevel.ContainsKey(starIndex) && StarAssembly.specBuffLevel[starIndex][s] > 0)
                                incLevel = ResearchTechHelper.GetMaxIncIndex();
                            else
                                incLevel = 0;
                        }

                        pRatio = StarAssembly.GetConsumeProduceSpeedRatio(starIndex, s);
                        pRatio = pRatio + StarAssembly.GetIncProduceSpeedRatio(pRatio, starIndex, s, incLevel);

                        if (BetterStats.counter.ContainsKey(itemId))
                        {
                            if (isExactlyStarAssembly)
                                BetterStats.counter[itemId].production = (float)(pRatio * StarAssembly.productCounts[starIndex][s][i] * 3600);
                            else
                                BetterStats.counter[itemId].production += (float)(pRatio * StarAssembly.productCounts[starIndex][s][i] * 3600);
                        }
                        else
                        {
                            BetterStats.counter.Add(itemId, new BetterStats.ProductMetrics());
                            BetterStats.counter[itemId].production = (float)(pRatio * StarAssembly.productCounts[starIndex][s][i] * 3600);
                        }
                    }
                }

                // 特别地，集成组件
                double icRatio = StarAssembly.GetConsumeProduceSpeedRatio(starIndex, 0);
                if (BetterStats.counter.ContainsKey(9500))
                {
                    if (isExactlyStarAssembly)
                        BetterStats.counter[9500].production = (float)(icRatio * 3600);
                    else
                        BetterStats.counter[9500].production += (float)(icRatio * 3600);
                }
                else
                {
                    BetterStats.counter.Add(9500, new BetterStats.ProductMetrics());
                    BetterStats.counter[9500].production = (float)(icRatio * 3600);
                }

                if (icRatio > 0)
                {
                    produceItems[9500] = 1;
                }

                foreach (var consumeItem in consumeItems)
                {
                    int itemId = consumeItem.Key;
                    if (BetterStats.counter.ContainsKey(itemId))
                    {
                        if (isExactlyStarAssembly)
                            BetterStats.counter[itemId].consumers = 1;
                        else
                            BetterStats.counter[itemId].consumers += 1;
                    }
                    else
                    {
                        BetterStats.counter.Add(itemId, new BetterStats.ProductMetrics());
                        BetterStats.counter[itemId].consumers = 1;
                    }

                    __instance.AddPlanetaryUsage(
                        itemId, GameMain.galaxy.PlanetById((starIndex + 1) * 100 + GameMain.galaxy.stars[starIndex].planetCount + 1), 0,
                        true); // 解决筛选报key不存在的错误
                }

                foreach (var produceItem in produceItems)
                {
                    int itemId = produceItem.Key;
                    if (BetterStats.counter.ContainsKey(itemId))
                    {
                        if (isExactlyStarAssembly)
                            BetterStats.counter[itemId].producers = 1;
                        else
                            BetterStats.counter[itemId].producers += 1;
                    }
                    else
                    {
                        BetterStats.counter.Add(itemId, new BetterStats.ProductMetrics());
                        BetterStats.counter[itemId].producers = 1;
                    }

                    __instance.AddPlanetaryUsage(
                        itemId, GameMain.galaxy.PlanetById((starIndex + 1) * 100 + GameMain.galaxy.stars[starIndex].planetCount + 1), 0);
                }
            }
        }

        public static void RefreshStarAssemblyIndex()
        {
            StarAssemblyIndex.Clear();
            for (int i = 0; i < 1000; i++)
            {
                if (MoreMegaStructure.StarMegaStructureType[i] == 4) StarAssemblyIndex.Add(i);
            }
        }
    }
}
