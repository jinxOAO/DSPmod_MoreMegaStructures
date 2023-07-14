using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using Unity;
using UnityEngine;

namespace MoreMegaStructure
{
    public class UIStatisticsPatcher
    {
        public static bool active = true;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "RefreshAstroBox")]
        public static void RefreshAstroBoxPostPatch(ref UIStatisticsWindow __instance)
        {
            if (__instance == null)
            {
                return;
            }
            var _this = __instance;
            // 原方法逻辑：看但凡有在ameData.factories这个列表里的工厂，但是又没在 this.astroBox.Items;和this.astroBox.ItemsData;里面的，将其planetId插入到正确位置（如果是整个星系的第一个插入的工厂，则额外先pushback一个（注意星系在这个ItemsData列表里是不按id排序的，而是按加入顺序排序的，所以说pushback，不是插入）该星系的总统计(starId*100)，然后再add该行星工厂的planetId）。

            // 为了增加巨构独有的production统计，用每个星系第planetCount+1号不存在planet的id存储巨构的统计
            if (__instance.astroBox == null)
            {
                return;
            }
            List<string> items = _this.astroBox.Items;
            List<int> itemsData = _this.astroBox.ItemsData;
            int oldStarId = -1;
            for (int i = 0; i < itemsData.Count; i++)
            {
                int nowStarId = itemsData[i] / 100;
                if (oldStarId != nowStarId && oldStarId != -1 && oldStarId <= 1000 && oldStarId > 0)
                {
                    if (MoreMegaStructure.StarMegaStructureType[oldStarId - 1] == 4)
                    {
                        itemsData.Insert(i, oldStarId * 100 + GameMain.galaxy.StarById(oldStarId).planetCount + 1);
                        items.Insert(i, Utils.MegaNameByType(MoreMegaStructure.StarMegaStructureType[oldStarId - 1]) + " " + GameMain.galaxy.StarById(oldStarId).displayName);
                        i++;
                    }
                }
                if (i == itemsData.Count - 1) 
                {
                    oldStarId = nowStarId; 
                    if (oldStarId<=1000 && MoreMegaStructure.StarMegaStructureType[oldStarId - 1] == 4)
                    {
                        itemsData.Add(oldStarId * 100 + GameMain.galaxy.StarById(oldStarId).planetCount + 1);
                        items.Add(Utils.MegaNameByType(MoreMegaStructure.StarMegaStructureType[oldStarId - 1]) + " " + GameMain.galaxy.StarById(oldStarId).displayName);
                        break;
                    }
                }
                oldStarId = nowStarId;
            }
            _this.ValueToAstroBox();
        }


        /// <summary>
        /// 将巨构独有的production统计倒序存储在GameMain.data.statistics.production.factoryStatPool超过GameMain.data.factories.Length的长度后。即其[length-1]存储starindex=0的巨构，[length-2]存储StarIndex=1的巨构
        /// </summary>

        public static void RearrangeStatisticLists()
        {
            if (!active) return;
            if (GameMain.data.statistics.production.factoryStatPool.Length <= GameMain.data.factories.Length) // 说明是游戏初始状态
            {
                FactoryProductionStat[] old = GameMain.data.statistics.production.factoryStatPool;
                GameMain.data.statistics.production.factoryStatPool = new FactoryProductionStat[GameMain.data.factories.Length + GameMain.galaxy.starCount];

                int i = 0;
                for (i = 0; i < old.Length; i++)
                {
                    GameMain.data.statistics.production.factoryStatPool[i] = old[i];
                }
                for (; i < GameMain.data.statistics.production.factoryStatPool.Length; i++)
                {
                    GameMain.data.statistics.production.factoryStatPool[i] = new FactoryProductionStat();
                    GameMain.data.statistics.production.factoryStatPool[i].Init();

                }
            }
        }


        // 需要在调用PlanetId=指示巨构生产统计的虚空行星的时候返回一个factoryIndex正确的planetData，而非null
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GalaxyData), "PlanetById")]
        public static void PlanetByIdPostPatch(GalaxyData __instance, int planetId, ref PlanetData __result)
        { 
            if(__result == null)
            {
                int num = planetId / 100 - 1;
                int num2 = planetId % 100 - 1;
                if (num < 0 || num >= __instance.stars.Length)
                {
                    return;
                }
                if (__instance.stars[num] == null)
                {
                    return;
                }
                if (num2 >= __instance.stars[num].planets.Length)
                {
                    PlanetData planet = new PlanetData();
                    for (int i = num*100+1; i < planetId; i++)
                    {
                        PlanetData pd = GameMain.galaxy.PlanetById(i);
                        if (pd != null && pd.factory!=null)
                        {
                            planet.factory = pd.factory;
                            break;
                        }
                    }
                    planet.factoryIndex = GameMain.data.factories.Length + GameMain.galaxy.starCount - num - 1;
                    __result = planet;
                    return;
                }
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "ComputeDisplayEntries")]
        public static bool ComputeDisplayEntriesPrePatch(ref UIStatisticsWindow __instance)
        {
            __instance.entryList.PrepareDisplayEntries(__instance.lastListCursor);
            __instance.statGroupCursor = 0;
            if (__instance.astroFilter == -1)
            {
                int factoryCount = __instance.gameData.factoryCount;
                for (int i = 0; i < factoryCount; i++)
                {
                    __instance.AddStatGroup(i);
                }
                for (int i = GameMain.data.factories.Length; i < GameMain.data.factories.Length + GameMain.galaxy.starCount; i++) // 加入巨构统计
                {
                    int idx = GameMain.data.factories.Length + GameMain.galaxy.starCount - i - 1;
                    if (idx < 1000 && MoreMegaStructure.StarMegaStructureType[idx] == 4)
                        __instance.AddStatGroup(i);
                }
            }
            else if (__instance.gameData.localPlanet != null && __instance.astroFilter == 0)
            {
                __instance.AddStatGroup(__instance.gameData.localPlanet.factoryIndex);
            }
            else if (__instance.astroFilter % 100 == 0)
            {
                int starId = __instance.astroFilter / 100;
                StarData starData = (__instance.astroFilter != 0) ? __instance.gameData.galaxy.StarById(starId) : __instance.gameData.localStar;
                if (starData != null)
                {
                    for (int j = 0; j < starData.planetCount; j++)
                    {
                        if (starData.planets[j].factory != null)
                        {
                            __instance.AddStatGroup(starData.planets[j].factoryIndex);
                        }
                    }

                    __instance.AddStatGroup(GameMain.data.factories.Length + GameMain.galaxy.starCount - starId); // 加入巨构统计
                }
                else
                {
                    int factoryCount2 = __instance.gameData.factoryCount;
                    for (int k = 0; k < factoryCount2; k++)
                    {
                        __instance.AddStatGroup(k);
                    }
                    for (int k = GameMain.data.factories.Length; k < GameMain.data.factories.Length + GameMain.galaxy.starCount; k++) // 加入巨构统计
                    {
                        int idx = GameMain.data.factories.Length + GameMain.galaxy.starCount - k - 1;
                        if (idx<1000 && MoreMegaStructure.StarMegaStructureType[idx] == 4)
                            __instance.AddStatGroup(k);
                    }
                }
            }
            else
            {
                __instance.AddStatGroup(__instance.gameData.galaxy.PlanetById(__instance.astroFilter).factoryIndex);
            }
            int num = __instance.timeLevel + 1;
            int num2 = num + 7;
            bool flag = __instance.isProductionTab || __instance.isDysonTab;
            for (int l = 0; l < __instance.statGroupCursor; l++)
            {
                UIStatisticsWindow.UIFactoryStatGroup uifactoryStatGroup = __instance.factoryStatGroup[l];
                ProductStat[] productPool = uifactoryStatGroup.productPool;
                PowerStat[] powerPool = uifactoryStatGroup.powerPool;
                int num3 = flag ? (__instance.isDysonTab ? 4 : uifactoryStatGroup.productCursor) : 1;
                int[] productIds = uifactoryStatGroup.productIds;
                for (int m = 1; m < num3; m++)
                {
                    if (__instance.isDysonTab)
                    {
                        int num4 = (m < 3) ? ((m == 1) ? 11901 : 11902) : 11903;
                        int num5 = productIds[num4];
                        if (num5 > 0)
                        {
                            long[] total = productPool[num5].total;
                            __instance.entryList.Add(num4, total[num], total[num2]);
                        }
                        else
                        {
                            __instance.entryList.Add(num4);
                        }
                    }
                    else
                    {
                        ProductStat productStat = productPool[m];
                        int num4 = productStat.itemId;
                        long[] total = productStat.total;
                        __instance.entryList.Add(num4, total[num], total[num2]);
                    }
                }
                if (__instance.isPowerTab)
                {
                    __instance.ComputePowerTab(powerPool, uifactoryStatGroup.energyConsumption, (long)uifactoryStatGroup.factoryIndex);
                }
                else if (__instance.isResearchTab)
                {
                    __instance.ComputeResearchTab(powerPool, num);
                }
            }
            __instance.lastListCursor = __instance.entryList.entryDatasCursor;
            if (__instance.isProductionTab)
            {
                __instance.entryList.SetEntriesFavorite(__instance.productionStat.favoriteIds);
                __instance.entryList.FilterEntries(__instance.favoriteMask);
                int max = __instance.entryList.entryDatasCursor - 1;
                if (__instance.sortMethod == 0)
                {
                    __instance.entryList.QuickSortByItemIndex(__instance.entryList.entryDatas, 0, max);
                }
                else if (__instance.sortMethod == 1)
                {
                    __instance.entryList.QuickSortByProductionDesc(__instance.entryList.entryDatas, 0, max);
                }
                else
                {
                    __instance.entryList.QuickSortByProductionAsc(__instance.entryList.entryDatas, 0, max);
                }
            }
            __instance.entryList.RefreshDatasIndices(__instance.lastListCursor);
            return false;
        }


        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(UIStatisticsWindow), "AddStatGroup")]
        //public static bool AddStatGroupTestPrePatch(ref UIStatisticsWindow __instance, int _factoryIndex)
        //{
        //    if (_factoryIndex < 0)
        //    {
        //        return true;
        //    }
        //    FactoryProductionStat factoryProductionStat = __instance.productionStat.factoryStatPool[_factoryIndex];
        //    __instance.factoryStatGroup[__instance.statGroupCursor].factoryIndex = _factoryIndex;
        //    __instance.factoryStatGroup[__instance.statGroupCursor].productPool = factoryProductionStat.productPool;
        //    __instance.factoryStatGroup[__instance.statGroupCursor].powerPool = factoryProductionStat.powerPool;
        //    __instance.factoryStatGroup[__instance.statGroupCursor].productIds = factoryProductionStat.productIndices;
        //    __instance.factoryStatGroup[__instance.statGroupCursor].productCursor = factoryProductionStat.productCursor;
        //    __instance.factoryStatGroup[__instance.statGroupCursor].energyConsumption = factoryProductionStat.energyConsumption;
        //    __instance.statGroupCursor++;
        //    return false;
        //}


        /// <summary>
        /// 下面两个让新增的production进行正常统计计算
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProductionStatistics), "PrepareTick")]
        public static void ProductionStatisticsPrepareTickPostPatch(ref ProductionStatistics __instance)
        {
            for (int i = __instance.gameData.factories.Length; i < __instance.gameData.factories.Length + GameMain.galaxy.starCount; i++)
            {
                __instance.factoryStatPool[i].PrepareTick();
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProductionStatistics), "GameTick")]
        public static void ProductionStatisticsGameTickPostPatch(ref ProductionStatistics __instance, long time)
        {
            bool flag = true;
            for (int i = __instance.gameData.factories.Length; i < __instance.gameData.factories.Length + GameMain.galaxy.starCount; i++)
            {
                __instance.factoryStatPool[i].GameTick(time);
                if (__instance.factoryStatPool[i].itemChanged && flag)
                {
                    try
                    {
                        UIRoot.instance.uiGame.statWindow.OnItemChange();
                    }
                    catch (Exception message)
                    {
                        Debug.LogError(message);
                    }
                    flag = false;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "ComputePowerTab")]
        public static bool ComputePowerTabPrePatch(ref UIStatisticsWindow __instance, PowerStat[] powerPool, long energyConsumption, long factoryIndex)
        {
            if (factoryIndex >= GameMain.data.factories.Length)
            {
                long num = __instance.ComputePower(powerPool[0]);
                __instance.entryList.Add(1, num, 0L, energyConsumption);
                num = __instance.ComputePower(powerPool[1]);
                __instance.entryList.Add(1, 0L, num);
                num = __instance.ComputePower(powerPool[3]);
                long num2 = 0L;
                __instance.entryList.Add(2, num, 0L, num2);
                num = __instance.ComputePower(powerPool[2]);
                __instance.entryList.Add(2, 0L, num);
                return false;
            }
            return true;
        }
    }
}
