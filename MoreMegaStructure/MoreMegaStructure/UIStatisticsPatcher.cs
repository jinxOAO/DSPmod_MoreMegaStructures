using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace MoreMegaStructure
{
    public class UIStatisticsPatcher
    {
        public static bool active = true;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "RefreshAstroBox")]
        public static bool RefreshAstroBoxPostPatch(ref UIStatisticsWindow __instance)
        {
            if (__instance == null)
            {
                return true;
            }

            var _this = __instance;

            // 原方法逻辑：看但凡有在GameData.factories这个列表里的工厂，但是又没在 this.astroBox.Items;和this.astroBox.ItemsData;里面的，将其planetId插入到正确位置（如果是整个星系的第一个插入的工厂，则额外先pushback一个（注意星系在这个ItemsData列表里是不按id排序的，而是按加入顺序排序的，所以说pushback，不是插入）该星系的总统计(starId*100)，然后再add该行星工厂的planetId）。
            // 为了增加巨构独有的production统计，用每个星系第planetCount+1号不存在planet的id存储巨构的统计
            if (_this.isStatisticsTab)
            {
                List<string> items = _this.astroBox.Items;
                List<int> itemsData = _this.astroBox.ItemsData;
                items.Clear();
                itemsData.Clear();
                items.Add("统计全星系".Translate());
                itemsData.Add(-1);
                if (!_this.isDysonTab && _this.gameData.localPlanet != null)
                {
                    items.Add("统计当前星球".Translate());
                    itemsData.Add(0);
                }
                if (_this.isKillTab)
                {
                    items.Add("统计玩家".Translate());
                    itemsData.Add(-2);
                }
                int factoryCount = _this.gameData.factoryCount;
                for (int i = 0; i < factoryCount; i++)
                {
                    int planetId = _this.gameData.factories[i].planetId;
                    int num = planetId / 100;
                    int num2 = planetId % 100;
                    bool flag = false;
                    bool flag2 = false;
                    int j;
                    for (j = 0; j < itemsData.Count; j++)
                    {
                        if (flag)
                        {
                            int num3 = itemsData[j] % 100;
                            if (num3 >= num2 || num3 == 0)
                            {
                                if (num3 == num2)
                                {
                                    flag2 = true;
                                    break;
                                }

                                break;
                            }
                        }
                        else
                        {
                            flag = (num == itemsData[j] / 100);
                        }
                    }

                    if (!flag2)
                    {
                        PlanetData planet = _this.gameData.factories[i].planet;
                        string displayName = planet.displayName;
                        if (flag)
                        {
                            if (!_this.isDysonTab)
                            {
                                itemsData.Insert(j, planetId);
                                items.Insert(j, displayName);
                            }
                        }
                        else
                        {
                            string item = planet.star.displayName + "空格行星系".Translate();
                            itemsData.Add(num * 100);
                            items.Add(item);
                            if (!_this.isDysonTab)
                            {
                                itemsData.Add(planetId);
                                items.Add(displayName);
                            }
                        }
                    }
                }

                // 新增部分
                int oldStarId = -1;
                for (int i = 0; i < itemsData.Count; i++)
                {
                    int nowStarId = itemsData[i] / 100;
                    if (oldStarId != nowStarId && oldStarId != -1 && oldStarId <= 1000 && oldStarId > 0)
                    {
                        if (MoreMegaStructure.StarMegaStructureType[oldStarId - 1] == 4)
                        {
                            itemsData.Insert(i, oldStarId * 100 + GameMain.galaxy.StarById(oldStarId).planetCount + 1);
                            items.Insert(
                                i, GameMain.galaxy.PlanetById(oldStarId * 100 + GameMain.galaxy.StarById(oldStarId).planetCount + 1).displayName);
                            i++;
                        }
                    }

                    if (i == itemsData.Count - 1)
                    {
                        oldStarId = nowStarId;
                        if (oldStarId > 0 && oldStarId <= 1000 && MoreMegaStructure.StarMegaStructureType[oldStarId - 1] == 4)
                        {
                            itemsData.Add(oldStarId * 100 + GameMain.galaxy.StarById(oldStarId).planetCount + 1);
                            items.Add(GameMain.galaxy.PlanetById(oldStarId * 100 + GameMain.galaxy.StarById(oldStarId).planetCount + 1).displayName);
                            break;
                        }
                    }

                    oldStarId = nowStarId;
                }

                // 这一行使原本的
                _this.ValueToAstroBox();
            }

            return false;
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
                GameMain.data.statistics.production.factoryStatPool
                    = new FactoryProductionStat[GameMain.data.factories.Length + GameMain.galaxy.starCount];

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
            if (__result == null)
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
                    for (int i = num * 100 + 1; i < planetId; i++) // 这里是为了，bottleneck会用工厂信息，随便找一个不是null的factory搪塞一下，防止他报错。反正后面大概也许把需要处理的数据还得都覆盖一遍
                    {
                        PlanetData pd = GameMain.galaxy.PlanetById(i);
                        if (pd != null && pd.factory != null)
                        {
                            planet.factory = pd.factory;
                            break;
                        }
                    }

                    planet.factoryIndex = GameMain.data.factories.Length + GameMain.galaxy.starCount - num - 1;
                    planet.overrideName = Utils.MegaNameByType(MoreMegaStructure.StarMegaStructureType[num]) +
                                          " " +
                                          GameMain.galaxy.StarById(num + 1).displayName;
                    __result = planet;
                    return;
                }
            }
        }

        /// <summary>
        /// 完全拦截，需要__instance.astroFilter %100==0、==-1时候加入巨构的统计
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "ComputeDisplayProductEntries")]
        public static bool ComputeDisplayEntriesPrePatch(ref UIStatisticsWindow __instance)
        {
            __instance.productEntryList.PrepareDisplayEntries(__instance.lastListCursor);
            __instance.factoryStatGroupCursor = 0;
            if (__instance.astroFilter == -1)
            {
                int factoryCount = __instance.gameData.factoryCount;
                for (int i = 0; i < factoryCount; i++)
                {
                    __instance.AddFactoryStatGroup(i);
                }

                for (int i = GameMain.data.factories.Length; i < GameMain.data.factories.Length + GameMain.galaxy.starCount; i++) // 加入巨构统计
                {
                    int idx = GameMain.data.factories.Length + GameMain.galaxy.starCount - i - 1;
                    if (idx < 1000 && MoreMegaStructure.StarMegaStructureType[idx] == 4) __instance.AddFactoryStatGroup(i);
                }
            }
            else if (__instance.gameData.localPlanet != null && __instance.astroFilter == 0)
            {
                __instance.AddFactoryStatGroup(__instance.gameData.localPlanet.factoryIndex);
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
                            __instance.AddFactoryStatGroup(starData.planets[j].factoryIndex);
                        }
                    }

                    __instance.AddFactoryStatGroup(GameMain.data.factories.Length + GameMain.galaxy.starCount - starId); // 加入巨构统计
                }
                else
                {
                    int factoryCount2 = __instance.gameData.factoryCount;
                    for (int k = 0; k < factoryCount2; k++)
                    {
                        __instance.AddFactoryStatGroup(k);
                    }

                    for (int k = GameMain.data.factories.Length; k < GameMain.data.factories.Length + GameMain.galaxy.starCount; k++) // 加入巨构统计
                    {
                        int idx = GameMain.data.factories.Length + GameMain.galaxy.starCount - k - 1;
                        if (idx < 1000 && MoreMegaStructure.StarMegaStructureType[idx] == 4) __instance.AddFactoryStatGroup(k);
                    }
                }
            }
            else
            {
                __instance.AddFactoryStatGroup(__instance.gameData.galaxy.PlanetById(__instance.astroFilter).factoryIndex);
            }

            int num = __instance.timeLevel + 1;
            int num2 = num + 7;
            bool flag = __instance.isProductionTab || __instance.isDysonTab;
            for (int l = 0; l < __instance.factoryStatGroupCursor; l++)
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
                            __instance.productEntryList.Add(num4, total[num], total[num2]);
                        }
                        else
                        {
                            __instance.productEntryList.Add(num4);
                        }
                    }
                    else
                    {
                        ProductStat productStat = productPool[m];
                        int num4 = productStat.itemId;
                        long[] total = productStat.total;
                        __instance.productEntryList.Add(num4, total[num], total[num2]);
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

            __instance.lastListCursor = __instance.productEntryList.entryDatasCursor;
            if (__instance.isProductionTab)
            {
                __instance.productEntryList.SetEntriesFavorite(__instance.productionStat.favoriteIds);
                __instance.productEntryList.FilterEntries(__instance.favoriteMask);
                int max = __instance.productEntryList.entryDatasCursor - 1;
                if (__instance.sortMethod == 0)
                {
                    __instance.productEntryList.QuickSortByItemIndex(__instance.productEntryList.entryDatas, 0, max);
                }
                else if (__instance.sortMethod == 1)
                {
                    __instance.productEntryList.QuickSortByProductionDesc(__instance.productEntryList.entryDatas, 0, max);
                }
                else
                {
                    __instance.productEntryList.QuickSortByProductionAsc(__instance.productEntryList.entryDatas, 0, max);
                }
            }

            __instance.productEntryList.RefreshDatasIndices(__instance.lastListCursor);
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

        /// <summary>
        /// 下面这个防止选中巨构时点击能量面板报错（factoryIndex越界或者是找回的）
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="powerPool"></param>
        /// <param name="energyConsumption"></param>
        /// <param name="factoryIndex"></param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "ComputePowerTab")]
        public static bool ComputePowerTabPrePatch(
            ref UIStatisticsWindow __instance,
            PowerStat[] powerPool,
            long energyConsumption,
            long factoryIndex)
        {
            if (factoryIndex >= GameMain.data.factories.Length)
            {
                long num = __instance.ComputePower(powerPool[0]);
                __instance.productEntryList.Add(1, num, 0L, energyConsumption);
                num = __instance.ComputePower(powerPool[1]);
                __instance.productEntryList.Add(1, 0L, num);
                num = __instance.ComputePower(powerPool[3]);
                long num2 = 0L;
                __instance.productEntryList.Add(2, num, 0L, num2);
                num = __instance.ComputePower(powerPool[2]);
                __instance.productEntryList.Add(2, 0L, num);
                return false;
            }

            return true;
        }

        public static void Export(BinaryWriter w)
        {
            w.Write(0);
            w.Write(GameMain.galaxy.starCount);
            for (int i = GameMain.data.factories.Length; i < GameMain.data.factories.Length + GameMain.galaxy.starCount; i++)
            {
                GameMain.statistics.production.factoryStatPool[i].Export(w.BaseStream, w);
            }
        }

        public static void Import(BinaryReader r)
        {
            RearrangeStatisticLists();
            if (MoreMegaStructure.savedModVersion >= 119)
            {
                r.ReadInt32();
                int num = r.ReadInt32();
                Debug.Log($"datafactorylenis {GameMain.data.factories.Length}");
                for (int i = GameMain.data.factories.Length; i < GameMain.data.factories.Length + num; i++)
                {
                    GameMain.statistics.production.factoryStatPool[i].Import(r.BaseStream, r);
                }
            }
        }

        public static void IntoOtherSave()
        {
            RearrangeStatisticLists();
        }
    }
}
