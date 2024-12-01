using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using Steamworks;
using System.Reflection;
using static UIReferenceSpeedTip;

namespace MoreMegaStructure
{
    public class UIStatisticsPatcher
    {
        public const int starAssemblyRepresentativeItemId = 9493; // 用这个itemId代表巨构组装厂的生产机器itemId

        public static bool enabled = true;
        public static long time0 = 0;
        public static long time1 = 0;
        public static long time2 = 0;
        public static long time3 = 0;
        public static long time4 = 0;
        public static long time5 = 0;
        // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- //
        // 注意！！！！！！！！！！！！！factoryCount和GameMain.data.factories.Length不一样！！！！！！！前者是已经实例化的工厂的数量，后者是所有行星数量（可最终实例化的全部工厂数）！！！！！！！！！！！！//
        // 注意！！！！！！！！！！！！！factoryCount和GameMain.data.factories.Length不一样！！！！！！！前者是已经实例化的工厂的数量，后者是所有行星数量（可最终实例化的全部工厂数）！！！！！！！！！！！！//
        // 注意！！！！！！！！！！！！！factoryCount和GameMain.data.factories.Length不一样！！！！！！！前者是已经实例化的工厂的数量，后者是所有行星数量（可最终实例化的全部工厂数）！！！！！！！！！！！！//
        // 注意！！！！！！！！！！！！！factoryCount和GameMain.data.factories.Length不一样！！！！！！！前者是已经实例化的工厂的数量，后者是所有行星数量（可最终实例化的全部工厂数）！！！！！！！！！！！！//
        // 注意！！！！！！！！！！！！！factoryCount和GameMain.data.factories.Length不一样！！！！！！！前者是已经实例化的工厂的数量，后者是所有行星数量（可最终实例化的全部工厂数）！！！！！！！！！！！！//
        // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- //

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "RefreshAstroBox")]
        public static bool RefreshAstroBoxPostPatch(ref UIStatisticsWindow __instance)
        {
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.Statistics);
            if (__instance == null)
            {
                return true;
            }
            var _this = __instance;

            // 原方法逻辑：看但凡有在GameData.factories这个列表里的工厂，但是又没在 this.astroBox.Items;和this.astroBox.ItemsData;里面的，将其planetId插入到正确位置（如果是整个星系的第一个插入的工厂，则额外先pushback一个（注意星系在这个ItemsData列表里是不按id排序的，而是按加入顺序排序的，所以说pushback，不是插入）该星系的总统计(starId*100)，然后再add该行星工厂的planetId）。
            // 为了增加巨构独有的production统计，用每个星系第planetCount+1号不存在planet的id存储巨构的统计，这也是astroFilter的值
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
                                i, MMSPlanetById(oldStarId * 100 + GameMain.galaxy.StarById(oldStarId).planetCount + 1).displayName);
                            i++;
                        }
                    }

                    if (i == itemsData.Count - 1)
                    {
                        oldStarId = nowStarId;
                        if (oldStarId > 0 && oldStarId <= 1000 && MoreMegaStructure.StarMegaStructureType[oldStarId - 1] == 4)
                        {
                            itemsData.Add(oldStarId * 100 + GameMain.galaxy.StarById(oldStarId).planetCount + 1);
                            items.Add(MMSPlanetById(oldStarId * 100 + GameMain.galaxy.StarById(oldStarId).planetCount + 1).displayName);
                            break;
                        }
                    }

                    oldStarId = nowStarId;
                }

                // 这一行使原本的
                _this.ValueToAstroBox();
            }
            MMSCPU.EndSample(ECpuWorkEntryExtended.Statistics);
            MMSCPU.EndSample(ECpuWorkEntryExtended.MoreMegaStructure);
            return false;
        }

        /// <summary>
        /// 将巨构独有的production统计倒序存储在GameMain.data.statistics.production.factoryStatPool超过GameMain.data.factories.Length的长度后。即其[length-1]存储starindex=0的巨构，[length-2]存储StarIndex=1的巨构
        /// </summary>
        public static void RearrangeStatisticLists()
        {
            if (!enabled) return;
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
            else // 修复了加载旧存档时ProductionStatisticsPrepareTickPostPatch报null的错误（虽然我不知道为啥null，大概吧）
            {
                for (int i = GameMain.data.factories.Length; i < GameMain.data.statistics.production.factoryStatPool.Length; i++)
                {
                    if (GameMain.data.statistics.production.factoryStatPool[i] == null)
                    {
                        GameMain.data.statistics.production.factoryStatPool[i] = new FactoryProductionStat();
                        GameMain.data.statistics.production.factoryStatPool[i].Init();
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TrafficStatistics), "Init")]
        public static void TrafficStatisticsInitPostPatch(ref TrafficStatistics __instance)
        {
            __instance.factoryTrafficPool = new AstroTrafficStat[__instance.gameData.factories.Length + +GameMain.galaxy.starCount];
        }


        public static PlanetData MMSPlanetById(int planetId)
        {
            GalaxyData __instance = GameMain.galaxy;
            PlanetData planet = GameMain.galaxy.PlanetById(planetId);
            if(planet == null)
            {
                try
                {
                    int num = planetId / 100 - 1; // starIndex
                    if (num < 0 || num >= __instance.stars.Length)
                    {
                        return null;
                    }
                    int num2 = planetId % 100 - 1; // planet index in star system
                    if (num2 >= __instance.stars[num].planets.Length)
                    {
                        if (__instance.stars[num] == null)
                        {
                            return null;
                        }

                        if (num2 >= __instance.stars[num].planets.Length)
                        {
                            planet = new PlanetData();
                            planet.factory = null;
                            for (int i = num * 100 + 1; i < planetId; i++) // 这里是为了，bottleneck会用工厂信息，随便找一个不是null的factory搪塞一下，防止他报错。反正后面大概也许把需要处理的数据还得都覆盖一遍
                            {
                                int index = i - num * 100 - 1;
                                if (index >= 0 && index < __instance.stars[num].planets.Length)
                                {
                                    PlanetData pd = __instance.stars[num].planets[index];
                                    if (pd != null && pd.factory != null)
                                    {
                                        planet.factory = pd.factory;
                                        break;
                                    }
                                }
                                if (i == planetId - 1)
                                {
                                    Utils.Log("planetById Postfix found no factory to replace null, now returning new PlanetData() as result");
                                }
                            }
                            if (planet.factory == null)
                            {
                                for (int i = 0; i < GameMain.data.factories.Length && i < GameMain.data.factoryCount; i++)
                                {
                                    if (GameMain.data.factories[i] != null)
                                        planet.factory = GameMain.data.factories[i];
                                }
                            }
                            planet.factoryIndex = GameMain.data.factories.Length + GameMain.galaxy.starCount - num - 1;
                            planet.overrideName = Utils.MegaNameByType(MoreMegaStructure.StarMegaStructureType[num]) +
                                                  " " +
                                                  GameMain.galaxy.StarById(num + 1).displayName;
                            return planet;
                        }
                    }
                }
                catch (Exception)
                {
                    Utils.Log("Error in MoreMegaStructure PalnetByIdPostPatch.");
                }

            }
            return planet;
        }

        /// <summary>
        /// 完全拦截，需要__instance.astroFilter %100==0、==-1时候加入巨构的统计
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "DetermineProductEntryList")]
        public static bool ComputeDisplayEntriesPrePatch(ref UIStatisticsWindow __instance)
        {
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.Statistics);
            __instance.productEntryList.PrepareDisplayEntries(__instance.lastListCursor);
            Array.Clear(__instance.statGroup, 0, __instance.statGroupCursor);
            __instance.statGroupCursor = 0;
            if (__instance.astroFilter == -1)
            {
                int factoryCount = __instance.gameData.factoryCount;
                for (int i = 0; i < factoryCount; i++)
                {
                    __instance.AddProductStatGroup(i);
                }

                for (int i = GameMain.data.factories.Length; i < GameMain.data.factories.Length + GameMain.galaxy.starCount; i++) // 加入巨构统计
                {
                    int idx = GameMain.data.factories.Length + GameMain.galaxy.starCount - i - 1;
                    if (idx < 1000 && MoreMegaStructure.StarMegaStructureType[idx] == 4) __instance.AddProductStatGroup(i);
                }
            }
            else if (__instance.gameData.localPlanet != null && __instance.astroFilter == 0)
            {
                __instance.AddProductStatGroup(__instance.gameData.localPlanet.factoryIndex);
                __instance.AddTrafficFactoryStatGroup(__instance.gameData.localPlanet.factoryIndex);
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
                            __instance.AddProductStatGroup(starData.planets[j].factoryIndex);
                        }
                    }
                    __instance.AddProductStatGroup(GameMain.data.factories.Length + GameMain.galaxy.starCount - starId); // 加入巨构统计

                    __instance.AddTrafficStarStatGroup(starId);
                }
                else
                {
                    int factoryCount2 = __instance.gameData.factoryCount;
                    for (int k = 0; k < factoryCount2; k++)
                    {
                        __instance.AddProductStatGroup(k);
                    }

                    for (int k = GameMain.data.factories.Length; k < GameMain.data.factories.Length + GameMain.galaxy.starCount; k++) // 加入巨构统计
                    {
                        int idx = GameMain.data.factories.Length + GameMain.galaxy.starCount - k - 1;
                        if (idx < 1000 && MoreMegaStructure.StarMegaStructureType[idx] == 4) __instance.AddProductStatGroup(k);
                    }

                    int starCount = __instance.gameData.galaxy.starCount;
                    for (int l = 1; l <= starCount; l++)
                    {
                        __instance.AddTrafficStarStatGroup(l);
                    }
                }
            }
            else
            {
                int factoryIndex = MMSPlanetById(__instance.astroFilter).factoryIndex;
                __instance.AddProductStatGroup(factoryIndex);
                __instance.AddTrafficFactoryStatGroup(factoryIndex);
            }

            int num = __instance.timeLevel + 1;
            int num2 = num + 7;
            int num3 = num;
            int num4 = num2;
            bool flag = __instance.isProductionTab || __instance.isDysonTab;
            for (int m = 0; m < __instance.statGroupCursor; m++)
            {
                UIStatisticsWindow.UIStatGroup uistatGroup = __instance.statGroup[m];
                ProductStat[] productPool = uistatGroup.productPool;
                PowerStat[] powerPool = uistatGroup.powerPool;
                TrafficStat[] trafficPool = uistatGroup.trafficPool;
                int num5 = flag ? (__instance.isDysonTab ? 4 : uistatGroup.poolCursor) : 1;
                int[] itemIndices = uistatGroup.itemIndices;
                for (int n = 1; n < num5; n++)
                {
                    if (__instance.isDysonTab)
                    {
                        int num6 = (n < 3) ? ((n == 1) ? 11901 : 11902) : 11903;
                        int num7 = itemIndices[num6];
                        if (num7 > 0)
                        {
                            long[] total = productPool[num7].total;
                            __instance.productEntryList.Add(num6, total[num], total[num2]);
                        }
                        else
                        {
                            __instance.productEntryList.Add(num6);
                        }
                    }
                    else
                    {
                        if (productPool != null)
                        {
                            ProductStat productStat = productPool[n];
                            int num6 = productStat.itemId;
                            long[] total = productStat.total;
                            __instance.productEntryList.Add(num6, total[num], total[num2]);
                        }
                        if (trafficPool != null)
                        {
                            TrafficStat trafficStat = trafficPool[n];
                            int num6 = trafficStat.itemId;
                            long[] total = trafficStat.total;
                            __instance.productEntryList.AddImportAndExport(num6, total[num3], total[num4]);
                        }
                    }
                }
                if (powerPool != null)
                {
                    if (__instance.isPowerTab)
                    {
                        __instance.ComputePowerTab(powerPool, uistatGroup.energyConsumption, (long)uistatGroup.factoryIndex);
                    }
                    else if (__instance.isResearchTab)
                    {
                        __instance.ComputeResearchTab(powerPool, num);
                    }
                }
            }
            __instance.lastListCursor = __instance.productEntryList.entryDatasCursor;
            if (__instance.isProductionTab)
            {
                __instance.productEntryList.SetEntriesFavorite(__instance.productionStat.favoriteIds);
                if (__instance.rawMaterialFilter > 0 || __instance.endProductFilter > 0)
                {
                    bool flag2 = __instance.rawMaterialFilter == 0;
                    bool flag3 = __instance.endProductFilter == 0;
                    UIProductEntryData[] entryDatas = __instance.productEntryList.entryDatas;
                    for (int num8 = 0; num8 < entryDatas.Length; num8++)
                    {
                        if (__instance.rawMaterialFilter == entryDatas[num8].itemId)
                        {
                            flag2 = true;
                        }
                        if (__instance.endProductFilter == entryDatas[num8].itemId)
                        {
                            flag3 = true;
                        }
                        if (flag2 && flag3)
                        {
                            break;
                        }
                    }
                    if (!flag2)
                    {
                        __instance.rawMaterialFilter = 0;
                        __instance.ComputeDetailNextTick();
                    }
                    if (!flag3)
                    {
                        __instance.endProductFilter = 0;
                        __instance.ComputeDetailNextTick();
                    }
                }
                __instance.productEntryList.FilterEntries(__instance.favoriteMask, __instance.rawMaterialFilter, __instance.endProductFilter, ref __instance.nameFilter);
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

            MMSCPU.EndSample(ECpuWorkEntryExtended.Statistics);
            MMSCPU.EndSample(ECpuWorkEntryExtended.MoreMegaStructure);
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
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.Statistics);
            int maxLen = __instance.gameData.factories.Length + GameMain.galaxy.starCount;
            maxLen = maxLen < __instance.factoryStatPool.Length ? maxLen : __instance.factoryStatPool.Length;
            int endIndex = GameMain.data.factories.Length + GameMain.galaxy.starCount - 1;
            for (int i = __instance.gameData.factories.Length; i < maxLen; i++)
            {
                int starIndex =  endIndex - i;
                if(MoreMegaStructure.StarMegaStructureType.Length > starIndex && starIndex >= 0 && MoreMegaStructure.StarMegaStructureType[starIndex] == 4)
                    __instance.factoryStatPool[i].PrepareTick();
            }
            MMSCPU.EndSample(ECpuWorkEntryExtended.Statistics);
            MMSCPU.EndSample(ECpuWorkEntryExtended.MoreMegaStructure);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProductionStatistics), "GameTick")]
        public static void ProductionStatisticsGameTickPostPatch(ref ProductionStatistics __instance, long time)
        {
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.Statistics);
            bool flag = true;
            int maxLen = Math.Min(__instance.gameData.factories.Length + GameMain.galaxy.starCount, __instance.factoryStatPool.Length);
            int endIndex = GameMain.data.factories.Length + GameMain.galaxy.starCount - 1;
            for (int i = __instance.gameData.factories.Length; i < maxLen; i++)
            {
                int starIndex = endIndex - i;
                if (MoreMegaStructure.StarMegaStructureType.Length > starIndex && starIndex >= 0 && MoreMegaStructure.StarMegaStructureType[starIndex] == 4)
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
                        }

                        flag = false;
                    }
                }
            }
            MMSCPU.EndSample(ECpuWorkEntryExtended.Statistics);
            MMSCPU.EndSample(ECpuWorkEntryExtended.MoreMegaStructure);
        }



        /// <summary>
        /// 下面这个是写入星际组装厂的 预期理想产出和消耗数据
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="astroFilter"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProductionStatistics), "RefreshItemsCyclicRefSpeed")]
        public static void RefreshItemsCyclicRefSpeedPostPatch(ref ProductionStatistics __instance, int astroFilter)
        {
            // 原本的方法会读取增产剂解锁级别。
            GameHistoryData history = __instance.gameData.history;
            ItemProto itemProto = LDB.items.Select(2313);
            int[] array = (itemProto != null) ? itemProto.prefabDesc.incItemId : null;
            int maxIncLevelByProliferatorUnlocked = 0;
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (history.ItemUnlocked(array[i]))
                    {
                        ItemProto itemProto2 = LDB.items.Select(array[i]);
                        if (itemProto2 != null && itemProto2.Ability > maxIncLevelByProliferatorUnlocked)
                        {
                            maxIncLevelByProliferatorUnlocked = itemProto2.Ability;
                        }
                    }
                }
            }

            if (astroFilter == 0) // 把所有组装厂数据都写入
            {
                int minStarCount = GameMain.galaxy.starCount;
                if (minStarCount > 1000)
                    minStarCount = 1000;
                for (int starIndex = 0; starIndex < minStarCount; starIndex++)
                {
                    if (MoreMegaStructure.StarMegaStructureType[starIndex] == (int)EMegaType.InterstellarAssembly)
                    {
                        // 写入星际组装厂ref速度数据
                        MMSRefreshItemsCyclicRefSpeedWithFactory(starIndex, maxIncLevelByProliferatorUnlocked);
                    }
                }
            }
            else if (astroFilter % 100 == 0)
            {
                StarData starData = __instance.gameData.galaxy.StarById(astroFilter / 100);
                if (starData != null)
                {
                    int starIndex = starData.index;
                    if (starIndex < MoreMegaStructure.StarMegaStructureType.Length && MoreMegaStructure.StarMegaStructureType[starIndex] == (int)EMegaType.InterstellarAssembly)
                    {
                        // 写入星际组装厂ref速度数据
                        MMSRefreshItemsCyclicRefSpeedWithFactory(starIndex, maxIncLevelByProliferatorUnlocked);
                    }
                }
            }
            else
            {
                PlanetData planetData = __instance.gameData.galaxy.PlanetById(astroFilter);
                if(planetData == null) // 基本上意味着astroFilter代表着这个星系行星数+1（超出planets长度的那个）的那个行星，也就是星际组装厂的替代占位index
                {
                    int starIndex = astroFilter / 100 - 1;
                    if(starIndex >= 0 && starIndex < GameMain.galaxy.stars.Length && starIndex < MoreMegaStructure.StarMegaStructureType.Length && MoreMegaStructure.StarMegaStructureType[starIndex] == (int)EMegaType.InterstellarAssembly)
                    {
                        int planetIndex = astroFilter % 100 - 1;
                        
                        // 如果是那个astroFilter代表着这个星系行星数+1（超出planets长度的那个）的那个行星，也就是星际组装厂的替代占位index
                        if (planetIndex>=0 && planetIndex == GameMain.galaxy.stars[starIndex].planets.Length) 
                        {
                            // 写入星际组装厂ref速度数据
                            MMSRefreshItemsCyclicRefSpeedWithFactory(starIndex, maxIncLevelByProliferatorUnlocked);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 刷新星际组装厂的预期生产消耗速率
        /// </summary>
        /// <param name="starIndex"></param>
        /// <param name="maxIncLevelByProliferatorUnlocked"></param>
        public static void MMSRefreshItemsCyclicRefSpeedWithFactory(int starIndex, int maxIncLevelByProliferatorUnlocked)
        {
            int borrowedFactoryIndex = GameMain.data.factories.Length + GameMain.galaxy.starCount - starIndex - 1;
            FactoryProductionStat factoryProductionStat = GameMain.data.statistics.production.factoryStatPool[borrowedFactoryIndex];
            factoryProductionStat.ResetRefSpeed();
            for (int slot = 0; slot < 15; slot++)
            {
                if (slot == 0)
                {
                    GameMain.data.statistics.production.factoryStatPool[borrowedFactoryIndex].AddRefProductSpeed(9500, (float)StarAssembly.GetConsumeProduceSpeedRatio(starIndex, 0, out _) * 3600);
                }
                else if (StarAssembly.recipeIds[starIndex][slot] > 0)
                {
                    float speedRatio = (float)(StarAssembly.GetConsumeProduceSpeedRatio(starIndex, slot, out _) * 3600);

                    int consumLen = StarAssembly.items[starIndex][slot].Count;
                    for (int itemIndex = 0; itemIndex < consumLen; itemIndex++)
                    {
                        int itemId = StarAssembly.items[starIndex][slot][itemIndex];
                        GameMain.data.statistics.production.factoryStatPool[borrowedFactoryIndex].AddRefConsumeSpeed(itemId, speedRatio * StarAssembly.itemCounts[starIndex][slot][itemIndex]);
                    }
                    int productLen = StarAssembly.products[starIndex][slot].Count;
                    float incedSpeedRatio = speedRatio * (1f + StarAssembly.GetFullIncMilli(starIndex, slot, maxIncLevelByProliferatorUnlocked));
                    for (int productIndex = 0; productIndex < productLen; productIndex++)
                    {
                        int itemId = StarAssembly.products[starIndex][slot][productIndex];
                        GameMain.data.statistics.production.factoryStatPool[borrowedFactoryIndex].AddRefProductSpeed(itemId, incedSpeedRatio * StarAssembly.productCounts[starIndex][slot][productIndex]);
                    }
                }
            }
        }


        /// <summary>
        /// 这个是为了能让上面加入的预期产出，被算进来，能显示。否则光加入数据，数据位置超出了factoryCount导致显示的时候没被计入
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIProductEntry), "UpdateExtraProductTexts")]
        public static bool UpdateExtraProductTextsPostPatch(ref UIProductEntry __instance)
        {

            if (!__instance.productionStatWindow.isProductionTab)
            {
                return false;
            }
            float num = 0f;
            float num2 = 0f;
            long num3 = 0L;
            long num4 = 0L;
            long num5 = 0L;
            long num6 = 0L;
            int itemId = __instance.entryData.itemId;
            int astroFilter = __instance.productionStatWindow.astroFilter;
            FactoryProductionStat[] factoryStatPool = __instance.productionStatWindow.productionStat.factoryStatPool;
            if (astroFilter == -1)
            {
                int factoryCount = __instance.gameData.factoryCount;
                for (int i = 0; i < factoryCount; i++)
                {
                    int num7 = factoryStatPool[i].productIndices[itemId];
                    if (num7 > 0)
                    {
                        num += factoryStatPool[i].productPool[num7].refProductSpeed;
                        num2 += factoryStatPool[i].productPool[num7].refConsumeSpeed;
                        num3 += factoryStatPool[i].productPool[num7].storageCount;
                        num4 += factoryStatPool[i].productPool[num7].trashCount;
                        num5 += factoryStatPool[i].productPool[num7].importStorageCount;
                        num6 += factoryStatPool[i].productPool[num7].exportStorageCount;
                    }
                }
                // 然后加入末尾部分巨构借用的factoryStatPool里面的数值
                for (int i = GameMain.data.factories.Length; i < factoryStatPool.Length; i++)
                {
                    int num7 = factoryStatPool[i].productIndices[itemId];
                    if (num7 > 0)
                    {
                        num += factoryStatPool[i].productPool[num7].refProductSpeed;
                        num2 += factoryStatPool[i].productPool[num7].refConsumeSpeed;
                        num3 += factoryStatPool[i].productPool[num7].storageCount;
                        num4 += factoryStatPool[i].productPool[num7].trashCount;
                        num5 += factoryStatPool[i].productPool[num7].importStorageCount;
                        num6 += factoryStatPool[i].productPool[num7].exportStorageCount;
                    }
                }
            }
            else if (__instance.gameData.localPlanet != null && astroFilter == 0)
            {
                int factoryIndex = __instance.gameData.localPlanet.factoryIndex;
                int num8 = factoryStatPool[factoryIndex].productIndices[itemId];
                if (num8 > 0)
                {
                    num += factoryStatPool[factoryIndex].productPool[num8].refProductSpeed;
                    num2 += factoryStatPool[factoryIndex].productPool[num8].refConsumeSpeed;
                    num3 += factoryStatPool[factoryIndex].productPool[num8].storageCount;
                    num4 += factoryStatPool[factoryIndex].productPool[num8].trashCount;
                    num5 += factoryStatPool[factoryIndex].productPool[num8].importStorageCount;
                    num6 += factoryStatPool[factoryIndex].productPool[num8].exportStorageCount;
                }
            }
            else if (astroFilter % 100 == 0)
            {
                int starId = astroFilter / 100;
                StarData starData = (astroFilter != 0) ? __instance.gameData.galaxy.StarById(starId) : __instance.gameData.localStar;
                if (starData != null)
                {
                    for (int j = 0; j < starData.planetCount; j++)
                    {
                        if (starData.planets[j].factory != null)
                        {
                            int factoryIndex2 = starData.planets[j].factoryIndex;
                            int num9 = factoryStatPool[factoryIndex2].productIndices[itemId];
                            if (num9 > 0)
                            {
                                num += factoryStatPool[factoryIndex2].productPool[num9].refProductSpeed;
                                num2 += factoryStatPool[factoryIndex2].productPool[num9].refConsumeSpeed;
                                num3 += factoryStatPool[factoryIndex2].productPool[num9].storageCount;
                                num4 += factoryStatPool[factoryIndex2].productPool[num9].trashCount;
                                num5 += factoryStatPool[factoryIndex2].productPool[num9].importStorageCount;
                                num6 += factoryStatPool[factoryIndex2].productPool[num9].exportStorageCount;
                            }
                        }
                    }
                    // 加入星际组装厂数据
                    int borrowedFactoryIndex = GameMain.data.factories.Length + GameMain.galaxy.starCount - starData.id;
                    int indexInStarAssembly = factoryStatPool[borrowedFactoryIndex].productIndices[itemId];
                    if (indexInStarAssembly > 0)
                    {
                        num += factoryStatPool[borrowedFactoryIndex].productPool[indexInStarAssembly].refProductSpeed;
                        num2 += factoryStatPool[borrowedFactoryIndex].productPool[indexInStarAssembly].refConsumeSpeed;
                        num3 += factoryStatPool[borrowedFactoryIndex].productPool[indexInStarAssembly].storageCount;
                        num4 += factoryStatPool[borrowedFactoryIndex].productPool[indexInStarAssembly].trashCount;
                        num5 += factoryStatPool[borrowedFactoryIndex].productPool[indexInStarAssembly].importStorageCount;
                        num6 += factoryStatPool[borrowedFactoryIndex].productPool[indexInStarAssembly].exportStorageCount;
                    }
                }
                else
                {
                    int factoryCount2 = __instance.gameData.factoryCount;
                    for (int k = 0; k < factoryCount2; k++)
                    {
                        int num10 = factoryStatPool[k].productIndices[itemId];
                        if (num10 > 0)
                        {
                            num += factoryStatPool[k].productPool[num10].refProductSpeed;
                            num2 += factoryStatPool[k].productPool[num10].refConsumeSpeed;
                            num3 += factoryStatPool[k].productPool[num10].storageCount;
                            num4 += factoryStatPool[k].productPool[num10].trashCount;
                            num5 += factoryStatPool[k].productPool[num10].importStorageCount;
                            num6 += factoryStatPool[k].productPool[num10].exportStorageCount;
                        }
                    }

                    // 然后加入末尾部分巨构借用的factoryStatPool里面的数值
                    for (int i = GameMain.data.factories.Length; i < factoryStatPool.Length; i++)
                    {
                        int num7 = factoryStatPool[i].productIndices[itemId];
                        if (num7 > 0)
                        {
                            num += factoryStatPool[i].productPool[num7].refProductSpeed;
                            num2 += factoryStatPool[i].productPool[num7].refConsumeSpeed;
                            num3 += factoryStatPool[i].productPool[num7].storageCount;
                            num4 += factoryStatPool[i].productPool[num7].trashCount;
                            num5 += factoryStatPool[i].productPool[num7].importStorageCount;
                            num6 += factoryStatPool[i].productPool[num7].exportStorageCount;
                        }
                    }
                }
            }
            else
            {
                PlanetData planetData = __instance.gameData.galaxy.PlanetById(astroFilter);
                if (planetData != null && planetData.factory != null)
                {
                    int factoryIndex3 = planetData.factoryIndex;
                    int num11 = factoryStatPool[factoryIndex3].productIndices[itemId];
                    if (num11 > 0)
                    {
                        num += factoryStatPool[factoryIndex3].productPool[num11].refProductSpeed;
                        num2 += factoryStatPool[factoryIndex3].productPool[num11].refConsumeSpeed;
                        num3 += factoryStatPool[factoryIndex3].productPool[num11].storageCount;
                        num4 += factoryStatPool[factoryIndex3].productPool[num11].trashCount;
                        num5 += factoryStatPool[factoryIndex3].productPool[num11].importStorageCount;
                        num6 += factoryStatPool[factoryIndex3].productPool[num11].exportStorageCount;
                    }
                }
                else
                {
                    int starIndex = astroFilter / 100 - 1;
                    if (starIndex >= 0 && starIndex < GameMain.galaxy.stars.Length && starIndex < MoreMegaStructure.StarMegaStructureType.Length && MoreMegaStructure.StarMegaStructureType[starIndex] == (int)EMegaType.InterstellarAssembly)
                    {
                        int planetIndex = astroFilter % 100 - 1;

                        // 如果是那个astroFilter代表着这个星系行星数+1（超出planets长度的那个）的那个行星，也就是星际组装厂的替代占位index
                        if (planetIndex >= 0 && planetIndex == GameMain.galaxy.stars[starIndex].planets.Length)
                        {
                            int borrowedFactoryIndex = GameMain.data.factories.Length + GameMain.galaxy.starCount - starIndex - 1;
                            int indexInStarAssembly = factoryStatPool[borrowedFactoryIndex].productIndices[itemId];
                            if (indexInStarAssembly > 0)
                            {
                                num += factoryStatPool[borrowedFactoryIndex].productPool[indexInStarAssembly].refProductSpeed;
                                num2 += factoryStatPool[borrowedFactoryIndex].productPool[indexInStarAssembly].refConsumeSpeed;
                                num3 += factoryStatPool[borrowedFactoryIndex].productPool[indexInStarAssembly].storageCount;
                                num4 += factoryStatPool[borrowedFactoryIndex].productPool[indexInStarAssembly].trashCount;
                                num5 += factoryStatPool[borrowedFactoryIndex].productPool[indexInStarAssembly].importStorageCount;
                                num6 += factoryStatPool[borrowedFactoryIndex].productPool[indexInStarAssembly].exportStorageCount;
                            }
                        }
                    }
                }
            }
            StringBuilderUtility.WriteKMG(__instance.sb1, 8, (long)(num + 0.5f), true);
            StringBuilderUtility.WriteKMG(__instance.sb2, 8, (long)(num2 + 0.5f), true);
            __instance.productRefSpeedText.text = __instance.sb1.ToString();
            __instance.consumeRefSpeedText.text = __instance.sb2.ToString();
            bool flag = __instance.storageCountTip.thisTipRef != null && !__instance.storageCountTip.thisTipRef.isThreadEnded;
            __instance.storageCountText.text = (flag ? "正在计算中".Translate() : num3.ToString("#,##0"));
            __instance.storageCountText.fontSize = (flag ? 12 : 18);
            __instance.storageCountText.resizeTextForBestFit = !flag;
            __instance.trashCountText.text = num4.ToString("#,##0");
            __instance.importStorageCountText.text = num5.ToString("#,##0");
            __instance.exportStorageCountText.text = num6.ToString("#,##0");
            __instance.productRefSpeedText.color = (((long)(num + 0.5f) > 0L) ? __instance.productColor : __instance.zeroColor);
            __instance.consumeRefSpeedText.color = (((long)(num2 + 0.5f) > 0L) ? __instance.consumeColor : __instance.zeroColor);
            __instance.storageCountText.color = ((num3 > 0L) ? __instance.storageCountColor : __instance.zeroColor);
            __instance.trashCountText.color = ((num4 > 0L) ? __instance.storageCountColor : __instance.zeroColor);
            __instance.importStorageCountText.color = ((num5 > 0L) ? __instance.productColor : __instance.zeroColor);
            __instance.exportStorageCountText.color = ((num6 > 0L) ? __instance.consumeColor : __instance.zeroColor);
            return false;
        }






        /// <summary>
        /// 修正鼠标悬停的ref速度（预期速度）的悬停Tip数据显示
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIReferenceSpeedTip), "SetSubTip")]
        public static bool UIRefSpeedTipSetSubTipPrePatch(ref UIReferenceSpeedTip __instance, int _productionProtoId)
        {
            
            __instance.subTipRectTrans.gameObject.SetActive(true);
            __instance.productionProtoId = _productionProtoId;
            __instance.subTipPageIndex = 0;
            GameData data = GameMain.data;
            if (data == null)
            {
                return false;
            }
            Array.Clear(__instance.loadedEntryDatas, 0, __instance.loadedEntryDatas.Length);
            Array.Clear(__instance.loadedSubTipDatas, 0, __instance.loadedSubTipDatas.Length);
            GameHistoryData history = data.history;
            ItemProto itemProto = LDB.items.Select(2313);
            int[] array = (itemProto != null) ? itemProto.prefabDesc.incItemId : null;
            int maxIncLevelByProliferatorUnlocked = 0;
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (history.ItemUnlocked(array[i]))
                    {
                        ItemProto itemProto2 = LDB.items.Select(array[i]);
                        if (itemProto2 != null && itemProto2.Ability > maxIncLevelByProliferatorUnlocked)
                        {
                            maxIncLevelByProliferatorUnlocked = itemProto2.Ability;
                        }
                    }
                }
            }
            float incMulti = 1f + (float)Cargo.incTableMilli[maxIncLevelByProliferatorUnlocked];
            float accMulti = 1f + (float)Cargo.accTableMilli[maxIncLevelByProliferatorUnlocked];
            int num2 = 1;
            if (history.TechUnlocked(1607))
            {
                num2 = 4;
            }
            int inserterStackOutput = history.inserterStackOutput;
            int stationPilerLevel = history.stationPilerLevel;
            if (inserterStackOutput > num2)
            {
                num2 = inserterStackOutput;
            }
            if (inserterStackOutput > num2)
            {
                num2 = stationPilerLevel;
            }
            if (__instance.astroFilter == 0)
            {
                // 添加星际组装厂
                for (int starIndex = 0; starIndex < GameMain.galaxy.starCount && starIndex < MoreMegaStructure.StarMegaStructureType.Length; starIndex++)
                {
                    if (MoreMegaStructure.StarMegaStructureType[starIndex] == (int)EMegaType.InterstellarAssembly)
                    {
                        MMSAddRefSpeedTipEntryData(ref __instance, starIndex, maxIncLevelByProliferatorUnlocked, __instance.productionProtoId);
                    }
                }
                int factoryCount = data.factoryCount;
                for (int j = 0; j < factoryCount; j++)
                {
                    __instance.AddEntryDataWithFactory(data.factories[j], incMulti, accMulti, num2, __instance.productionProtoId);
                }
            }
            else if (__instance.astroFilter % 100 == 0)
            {
                StarData starData = data.galaxy.StarById(__instance.astroFilter / 100);
                if (starData != null)
                {
                    MMSAddRefSpeedTipEntryData(ref __instance, starData.index, maxIncLevelByProliferatorUnlocked, __instance.productionProtoId);
                    PlanetData[] planets = starData.planets;
                    for (int k = 0; k < starData.planetCount; k++)
                    {
                        PlanetData planetData = planets[k];
                        if (planetData != null && planetData.factory != null)
                        {
                            __instance.AddEntryDataWithFactory(data.factories[planetData.factoryIndex], incMulti, accMulti, num2, __instance.productionProtoId);
                        }
                    }
                }
                else
                {
                    for (int starIndex = 0; starIndex < GameMain.galaxy.starCount && starIndex < MoreMegaStructure.StarMegaStructureType.Length; starIndex++)
                    {
                        if (MoreMegaStructure.StarMegaStructureType[starIndex] == (int)EMegaType.InterstellarAssembly)
                        {
                            MMSAddRefSpeedTipEntryData(ref __instance, starIndex, maxIncLevelByProliferatorUnlocked, __instance.productionProtoId);
                        }
                    }
                    int factoryCount2 = data.factoryCount;
                    for (int l = 0; l < factoryCount2; l++)
                    {
                        __instance.AddEntryDataWithFactory(data.factories[l], incMulti, accMulti, num2, __instance.productionProtoId);
                    }
                }
            }
            else
            {
                PlanetData planetData2 = data.galaxy.PlanetById(__instance.astroFilter);
                if (planetData2 != null && planetData2.factory != null)
                {
                    __instance.AddEntryDataWithFactory(planetData2.factory, incMulti, accMulti, num2, __instance.productionProtoId);
                }
                else if(IsStarAssemblyAstroFilter(__instance.astroFilter))
                {
                    MMSAddRefSpeedTipEntryData(ref __instance, __instance.astroFilter / 100 - 1, maxIncLevelByProliferatorUnlocked, __instance.productionProtoId);
                }
            }
            __instance.RefreshSubEntries();
            __instance.subTipRectTrans.anchoredPosition = new Vector2(0f, 0f);
            __instance.subTipRectTrans.SetParent(UIRoot.instance.itemReferenceSpeedTipTransform, true);
            Rect rect = UIRoot.instance.itemReferenceSpeedTipTransform.rect;
            float num3 = (float)Mathf.RoundToInt(rect.width);
            float num4 = (float)Mathf.RoundToInt(rect.height);
            Vector2 anchoredPosition = __instance.subTipRectTrans.anchoredPosition;
            float num5 = __instance.subTipRectTrans.anchorMin.x * num3 + anchoredPosition.x;
            float num6 = __instance.subTipRectTrans.anchorMin.y * num4 + anchoredPosition.y;
            Rect rect2 = __instance.subTipRectTrans.rect;
            rect2.x += num5;
            rect2.y += num6;
            Vector2 zero = Vector2.zero;
            if (rect2.xMin < 0f)
            {
                zero.x -= rect2.xMin;
            }
            if (rect2.yMin < 0f)
            {
                zero.y -= rect2.yMin;
            }
            if (rect2.xMax > num3)
            {
                zero.x -= rect2.xMax - num3;
            }
            if (rect2.yMax > num4)
            {
                zero.y -= rect2.yMax - num4;
            }
            Vector2 vector = anchoredPosition + zero;
            vector = new Vector2(Mathf.Round(vector.x), Mathf.Round(vector.y));
            __instance.subTipRectTrans.anchoredPosition = vector;
            __instance.subTipRectTrans.SetParent(__instance.rectTrans, true);
            return false;
        }


        public static void MMSAddRefSpeedTipEntryData(ref UIReferenceSpeedTip _this, int starIndex, int maxIncLevelByProliferatorUnlocked, int productionIdMask = starAssemblyRepresentativeItemId)
        {
            int itemId = _this.itemId;
            ItemProto itemProto = LDB.items.Select(itemId);
            if (itemProto == null)
            {
                return;
            }
            int astroId = DefineStarAssemblyAstroId(starIndex); // 这里决定着UIRefSpeedTipRefreshSubEntriesPrePatch方法中的astroId的写法，到底是把星际组装厂的astroId写成恒星上，还是最大位置行星+1。两者写法要一致
            bool calcProduct = _this.itemCycle == UIReferenceSpeedTip.EItemCycle.Production;
            bool calcConsumption = _this.itemCycle == UIReferenceSpeedTip.EItemCycle.Consumption;
            FactoryProductionStat factoryProductionStat = GameMain.data.statistics.production.factoryStatPool[GameMain.data.factories.Length + GameMain.galaxy.starCount - 1 - starIndex];
            if(calcProduct)
                factoryProductionStat.ResetRefProductSpeed(itemId);
            if(calcConsumption)
                factoryProductionStat.ResetRefConsumeSpeed(itemId);
            for (int slot = 0; slot < 15; slot++)
            {
                if(slot == 0 && itemId == 9500)
                {
                    if(calcProduct)
                    {
                        float basicSpeed = (float)(StarAssembly.GetConsumeProduceSpeedRatio(starIndex, slot, out _) * 3600);
                        float actualSpeed = basicSpeed;
                        if (productionIdMask == starAssemblyRepresentativeItemId)
                            _this.AddSubTipEntryData(astroId, actualSpeed, false, false, false); // 这里加入的在_this.loadedSubTipDatas中加入
                        _this.AddEntryData(starAssemblyRepresentativeItemId, actualSpeed, false, false, false); 
                        factoryProductionStat.AddRefProductSpeed(itemId, actualSpeed);

                    }
                }
                else if (StarAssembly.recipeIds[starIndex][slot] > 0)
                {
                    float basicSpeed = (float)(StarAssembly.GetConsumeProduceSpeedRatio(starIndex, slot, out _) * 3600);
                    if(calcConsumption)
                    {
                        for (int i = 0; i < StarAssembly.items[starIndex][slot].Count; i++)
                        {
                            if (StarAssembly.items[starIndex][slot][i] == itemId)
                            {
                                float actualSpeed = basicSpeed * StarAssembly.itemCounts[starIndex][slot][i];
                                if (productionIdMask == starAssemblyRepresentativeItemId)
                                    _this.AddSubTipEntryData(astroId, actualSpeed, false, false, false);
                                _this.AddEntryData(starAssemblyRepresentativeItemId, actualSpeed, false, false, false);
                                factoryProductionStat.AddRefConsumeSpeed(itemId, actualSpeed);
                            }
                        }
                    }
                    else if (calcProduct)
                    {
                        for (int i = 0; i < StarAssembly.products[starIndex][slot].Count; i++)
                        {
                            if (StarAssembly.products[starIndex][slot][i] == itemId)
                            {
                                float actualSpeed = basicSpeed * (1 + StarAssembly.GetFullIncMilli(starIndex, slot, maxIncLevelByProliferatorUnlocked)) * StarAssembly.productCounts[starIndex][slot][i];
                                if (productionIdMask == starAssemblyRepresentativeItemId)
                                    _this.AddSubTipEntryData(astroId, actualSpeed, false, false, false);
                                _this.AddEntryData(starAssemblyRepresentativeItemId, actualSpeed, false, false, false);
                                factoryProductionStat.AddRefProductSpeed(itemId, actualSpeed);
                            }
                        }
                    }
                }
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIReferenceSpeedTip), "RefreshSubEntries")]
        public static bool UIRefSpeedTipRefreshSubEntriesPrePatch(ref UIReferenceSpeedTip __instance)
        {
            GameData data = GameMain.data;
            if (data == null)
            {
                return false;
            }
            ref RefSpeedTipEntryData ptr = ref __instance.loadedEntryDatas[__instance.productionProtoId];
            float entryTotalSpeed = ptr.useInc2IncSpeed + ptr.useInc2AccSpeed + ptr.normalSpeed;
            int num = 0;
            int minIndexInCurPage = __instance.subTipPageIndex * 8;
            int maxIndexInCurPage = (__instance.subTipPageIndex + 1) * 8 - 1;
            int idx = -1;
            GalaxyData galaxy = data.galaxy;
            StarData[] stars = galaxy.stars;
            int num5 = (int)__instance.subTipEntryPrefab.rectTrans.anchoredPosition.y;
            for (int i = 0; i < galaxy.starCount; i++)
            {
                if (stars[i] != null)
                {
                    // 加入星际组装厂数据
                    bool flagIsHoveringStarAssembly = __instance.productionProtoId == starAssemblyRepresentativeItemId;
                    bool flagFilterIsTotalGalaxyData = __instance.astroFilter == 0;
                    bool flagFilterIsThisStarSystem = __instance.astroFilter % 100 == 0 && i == __instance.astroFilter / 100 - 1;
                    bool flagFilterIsThisStarAssembly = IsStarAssemblyAstroFilter(__instance.astroFilter) && i == __instance.astroFilter / 100 - 1;
                    bool flagStarIsStarAssembly = i < MoreMegaStructure.StarMegaStructureType.Length && MoreMegaStructure.StarMegaStructureType[i] == (int)EMegaType.InterstellarAssembly;
                    if ( flagIsHoveringStarAssembly && flagStarIsStarAssembly && (flagFilterIsTotalGalaxyData || flagFilterIsThisStarSystem || flagFilterIsThisStarAssembly))
                    {
                        idx++;
                        if (idx >= minIndexInCurPage && idx <= maxIndexInCurPage)
                        {
                            int astroId = DefineStarAssemblyAstroId(i);
                            if (num >= __instance.subTipEntries.Count)
                            {
                                UIReferenceSpeedSubTipEntry item = GameObject.Instantiate<UIReferenceSpeedSubTipEntry>(__instance.subTipEntryPrefab, __instance.subTipEntryPrefab.rectTrans.parent);
                                __instance.subTipEntries.Add(item);
                            }
                            ref RefSpeedSubTipEntryData ptr2 = ref __instance.loadedSubTipDatas[astroId];
                            __instance.subTipEntries[num].gameObject.SetActive(true);
                            __instance.subTipEntries[num].entryData = ptr2;
                            __instance.subTipEntries[num].productionProtoId = __instance.productionProtoId;
                            __instance.subTipEntries[num].entryTotalSpeed = entryTotalSpeed;
                            __instance.subTipEntries[num].Refresh();
                            __instance.subTipEntries[num].rectTrans.anchoredPosition = new Vector2(__instance.subTipEntryPrefab.rectTrans.anchoredPosition.x, (float)num5);
                            num5 -= __instance.subTipEntries[num].entryHeight;
                            num++;
                        }
                    }



                    PlanetData[] planets = stars[i].planets;
                    for (int j = 0; j < stars[i].planetCount; j++)
                    {
                        PlanetData planetData = planets[j];
                        if (planetData != null && planetData.factory != null && __instance.loadedSubTipDatas[planetData.astroId].astroId == planetData.astroId)
                        {
                            idx++;
                            if (idx >= minIndexInCurPage && idx <= maxIndexInCurPage)
                            {
                                if (num >= __instance.subTipEntries.Count)
                                {
                                    UIReferenceSpeedSubTipEntry item = GameObject.Instantiate<UIReferenceSpeedSubTipEntry>(__instance.subTipEntryPrefab, __instance.subTipEntryPrefab.rectTrans.parent);
                                    __instance.subTipEntries.Add(item);
                                }
                                ref RefSpeedSubTipEntryData ptr2 = ref __instance.loadedSubTipDatas[planetData.astroId];
                                __instance.subTipEntries[num].gameObject.SetActive(true);
                                __instance.subTipEntries[num].entryData = ptr2;
                                __instance.subTipEntries[num].productionProtoId = __instance.productionProtoId;
                                __instance.subTipEntries[num].entryTotalSpeed = entryTotalSpeed;
                                __instance.subTipEntries[num].Refresh();
                                __instance.subTipEntries[num].rectTrans.anchoredPosition = new Vector2(__instance.subTipEntryPrefab.rectTrans.anchoredPosition.x, (float)num5);
                                num5 -= __instance.subTipEntries[num].entryHeight;
                                num++;
                            }
                        }
                    }
                }
            }
            for (int k = num; k < __instance.subTipEntries.Count; k++)
            {
                __instance.subTipEntries[k].gameObject.SetActive(false);
            }
            bool flag = idx + 1 > 8;
            if (flag)
            {
                __instance.previousPageBtn.gameObject.SetActive(true);
                __instance.nextPageBtn.gameObject.SetActive(true);
                __instance.previousPageBtn.button.interactable = (__instance.subTipPageIndex > 0);
                __instance.nextPageBtn.button.interactable = (__instance.subTipPageIndex < idx / 8);
            }
            else
            {
                __instance.previousPageBtn.gameObject.SetActive(false);
                __instance.nextPageBtn.gameObject.SetActive(false);
            }
            for (int l = 0; l < __instance.activeEntryCount; l++)
            {
                __instance.entries[l].entryData = __instance.loadedEntryDatas[__instance.entries[l].entryData.productionProtoId];
                __instance.entries[l].Refresh();
            }
            __instance.subTipRectTrans.sizeDelta = new Vector2(__instance.subTipRectTrans.sizeDelta.x, flag ? ((float)(-(float)num5) + 40f) : ((float)(-(float)num5) + 10f));
            return false;
        }



        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIReferenceSpeedTip), "SetTip")]
        public static bool UIReferenceSpeedTipSetTipPostPatch(ref UIReferenceSpeedTip __instance, int _itemId, int _astroFilter, UIReferenceSpeedTip.EItemCycle _itemCycle, Transform _parent)
        {
            if (__instance.itemId == _itemId && __instance.astroFilter == _astroFilter && __instance.itemCycle == _itemCycle)
            {
                return false;
            }
            __instance.itemId = _itemId;
            __instance.astroFilter = _astroFilter;
            __instance.itemCycle = _itemCycle;
            __instance.rectTrans.SetParent(_parent, true);
            RectTransform rectTransform = __instance.rectTrans;
            RectTransform rectTransform2 = __instance.rectTrans;
            Vector2 vector = new Vector2(1f, 0.5f);
            rectTransform2.anchorMin = vector;
            rectTransform.anchorMax = vector;
            __instance.rectTrans.pivot = new Vector2(0f, 0.5f);
            RectTransform rectTransform3 = __instance.rectTrans;
            vector = default(Vector2);
            rectTransform3.anchoredPosition = vector;
            GameData data = GameMain.data;
            if (data != null)
            {
                Array.Clear(__instance.loadedEntryDatas, 0, __instance.loadedEntryDatas.Length);
                GameHistoryData history = data.history;
                ItemProto itemProto = LDB.items.Select(2313);
                int[] array = (itemProto != null) ? itemProto.prefabDesc.incItemId : null;
                int maxIncLevelByProliferatorUnlocked = 0;
                if (array != null)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (history.ItemUnlocked(array[i]))
                        {
                            ItemProto itemProto2 = LDB.items.Select(array[i]);
                            if (itemProto2 != null && itemProto2.Ability > maxIncLevelByProliferatorUnlocked)
                            {
                                maxIncLevelByProliferatorUnlocked = itemProto2.Ability;
                            }
                        }
                    }
                }
                float incMulti = 1f + (float)Cargo.incTableMilli[maxIncLevelByProliferatorUnlocked];
                float accMulti = 1f + (float)Cargo.accTableMilli[maxIncLevelByProliferatorUnlocked];
                int num2 = 1;
                if (history.TechUnlocked(1607))
                {
                    num2 = 4;
                }
                int inserterStackOutput = history.inserterStackOutput;
                int stationPilerLevel = history.stationPilerLevel;
                if (inserterStackOutput > num2)
                {
                    num2 = inserterStackOutput;
                }
                if (inserterStackOutput > num2)
                {
                    num2 = stationPilerLevel;
                }
                if (__instance.astroFilter == 0)
                {
                    for (int starIndex = 0; starIndex < GameMain.galaxy.starCount && starIndex < MoreMegaStructure.StarMegaStructureType.Length; starIndex++)
                    {
                        if (MoreMegaStructure.StarMegaStructureType[starIndex] == (int)EMegaType.InterstellarAssembly)
                        {
                            MMSAddRefSpeedTipEntryData(ref __instance, starIndex, maxIncLevelByProliferatorUnlocked);
                        }
                    }
                    int factoryCount = data.factoryCount;
                    for (int j = 0; j < factoryCount; j++)
                    {
                        __instance.AddEntryDataWithFactory(data.factories[j], incMulti, accMulti, num2, 0);
                    }
                }
                else if (__instance.astroFilter % 100 == 0)
                {
                    StarData starData = data.galaxy.StarById(__instance.astroFilter / 100);
                    if (starData != null)
                    {
                        MMSAddRefSpeedTipEntryData(ref __instance, starData.index, maxIncLevelByProliferatorUnlocked);
                        PlanetData[] planets = starData.planets;
                        for (int k = 0; k < starData.planetCount; k++)
                        {
                            PlanetData planetData = planets[k];
                            if (planetData != null && planetData.factory != null)
                            {
                                __instance.AddEntryDataWithFactory(data.factories[planetData.factoryIndex], incMulti, accMulti, num2, 0);
                            }
                        }
                    }
                    else
                    {
                        for (int starIndex = 0; starIndex < GameMain.galaxy.starCount && starIndex < MoreMegaStructure.StarMegaStructureType.Length; starIndex++)
                        {
                            if (MoreMegaStructure.StarMegaStructureType[starIndex] == (int)EMegaType.InterstellarAssembly)
                            {
                                MMSAddRefSpeedTipEntryData(ref __instance, starIndex, maxIncLevelByProliferatorUnlocked);
                            }
                        }
                        int factoryCount2 = data.factoryCount;
                        for (int l = 0; l < factoryCount2; l++)
                        {
                            __instance.AddEntryDataWithFactory(data.factories[l], incMulti, accMulti, num2, 0);
                        }
                    }
                }
                else
                {
                    PlanetData planetData2 = data.galaxy.PlanetById(__instance.astroFilter);
                    if (planetData2 != null && planetData2.factory != null)
                    {
                        __instance.AddEntryDataWithFactory(planetData2.factory, incMulti, accMulti, num2, 0);
                    }
                    else if (IsStarAssemblyAstroFilter(__instance.astroFilter))
                    {
                        MMSAddRefSpeedTipEntryData(ref __instance, __instance.astroFilter / 100 - 1, maxIncLevelByProliferatorUnlocked);
                    }
                }
            }
            ItemProto[] dataArray = LDB.items.dataArray;
            __instance.activeEntryCount = 0;
            float num3 = 0f;
            int num4 = (int)__instance.entryPrefab.rectTrans.anchoredPosition.y;
            int num5 = (int)(__instance.entryPrefab.rectTrans.rect.width + 0.5f);
            for (int m = 0; m < dataArray.Length; m++)
            {
                if (__instance.loadedEntryDatas[dataArray[m].ID].productionProtoId != 0)
                {
                    if (__instance.activeEntryCount >= __instance.entries.Count)
                    {
                        UIReferenceSpeedTipEntry item = GameObject.Instantiate<UIReferenceSpeedTipEntry>(__instance.entryPrefab, __instance.entryPrefab.rectTrans.parent);
                        __instance.entries.Add(item);
                    }
                    ref RefSpeedTipEntryData ptr = ref __instance.loadedEntryDatas[dataArray[m].ID];
                    __instance.entries[__instance.activeEntryCount].gameObject.SetActive(true);
                    __instance.entries[__instance.activeEntryCount].entryData = ptr;
                    __instance.entries[__instance.activeEntryCount].Refresh();
                    __instance.entries[__instance.activeEntryCount].rectTrans.anchoredPosition = new Vector2(__instance.entryPrefab.rectTrans.anchoredPosition.x, (float)num4);
                    num4 -= __instance.entries[__instance.activeEntryCount].entryHeight;
                    if (num5 < __instance.entries[__instance.activeEntryCount].entryWidth)
                    {
                        num5 = __instance.entries[__instance.activeEntryCount].entryWidth;
                    }
                    num3 += ptr.normalSpeed + ptr.useInc2IncSpeed + ptr.useInc2AccSpeed + ptr.outNetworkSpeed;
                    __instance.activeEntryCount++;
                }
            }
            for (int n = __instance.activeEntryCount; n < __instance.entries.Count; n++)
            {
                __instance.entries[n].gameObject.SetActive(false);
            }
            if (__instance.activeEntryCount == 0)
            {
                __instance.zeroCountTipText.gameObject.SetActive(true);
                if (__instance.itemCycle == UIReferenceSpeedTip.EItemCycle.Production)
                {
                    __instance.zeroCountTipText.text = "参考速率无生产设施".Translate();
                }
                else if (__instance.itemCycle == UIReferenceSpeedTip.EItemCycle.Consumption)
                {
                    __instance.zeroCountTipText.text = "参考速率无消耗设施".Translate();
                }
                num4 -= (int)(__instance.zeroCountTipText.rectTransform.rect.height + 0.5f);
            }
            else
            {
                __instance.zeroCountTipText.gameObject.SetActive(false);
            }
            __instance.totalSpeedText.text = ((long)(num3 + 0.5f)).ToString("#,##0") + " / min";
            if (__instance.itemCycle == UIReferenceSpeedTip.EItemCycle.Production)
            {
                __instance.totalSpeedLabel.color = __instance.productColor;
                __instance.totalSpeedText.color = __instance.productColor;
            }
            else if (__instance.itemCycle == UIReferenceSpeedTip.EItemCycle.Consumption)
            {
                __instance.totalSpeedLabel.color = __instance.consumeColor;
                __instance.totalSpeedText.color = __instance.consumeColor;
            }
            else
            {
                __instance.totalSpeedLabel.color = Color.white;
                __instance.totalSpeedText.color = Color.white;
            }
            __instance.rectTrans.SetParent(UIRoot.instance.itemReferenceSpeedTipTransform, true);
            __instance.rectTrans.sizeDelta = new Vector2((float)num5 + 20f, (float)(-(float)num4) + 2f);
            Rect rect = UIRoot.instance.itemReferenceSpeedTipTransform.rect;
            float num6 = (float)Mathf.RoundToInt(rect.width);
            float num7 = (float)Mathf.RoundToInt(rect.height);
            Vector2 anchoredPosition = __instance.rectTrans.anchoredPosition;
            float num8 = __instance.rectTrans.anchorMin.x * num6 + anchoredPosition.x;
            float num9 = __instance.rectTrans.anchorMin.y * num7 + anchoredPosition.y;
            Rect rect2 = __instance.rectTrans.rect;
            rect2.x += num8;
            rect2.y += num9;
            Vector2 zero = Vector2.zero;
            if (rect2.xMin < 0f)
            {
                zero.x -= rect2.xMin;
            }
            if (rect2.yMin < 0f)
            {
                zero.y -= rect2.yMin;
            }
            if (rect2.xMax > num6)
            {
                zero.x -= rect2.xMax - num6;
            }
            if (rect2.yMax > num7)
            {
                zero.y -= rect2.yMax - num7;
            }
            Vector2 vector2 = anchoredPosition + zero;
            vector2 = new Vector2(Mathf.Round(vector2.x), Mathf.Round(vector2.y));
            __instance.rectTrans.anchoredPosition = vector2;
            return false;
        }


        /// <summary>
        /// 用于设置鼠标悬停时，显示的是星际组装厂的icon。看起来无效，没发现用处在哪儿。可能是我理解错位置了
        /// </summary>
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(UIReferenceSpeedTipEntry), "Refresh")]
        //public static void UIReferenceSpeedTipEntryIconPostPacth(ref UIReferenceSpeedTipEntry __instance)
        //{
        //}


        /// <summary>
        /// 用于设置鼠标悬停时，二级tip显示的是星际组装厂的icon以及星际组装厂的名称
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIReferenceSpeedSubTipEntry), "Refresh")]
        public static void UIReferenceSpeedSubTipEntryIconAndNamePostPacth(ref UIReferenceSpeedSubTipEntry __instance)
        {
            // Debug.Log($"id is {__instance.productionProtoId}, while astro id is {__instance.entryData.astroId}");
            if (__instance.productionProtoId == starAssemblyRepresentativeItemId)
            {
                //__instance.productionIcon.sprite = Resources.Load<Sprite>("ui/textures/sprites/icons/dsp-icon"); // icon请去对应的itemProto的sprite里面修改
                int starId = __instance.entryData.astroId / 100;
                if (GameMain.galaxy.StarById(starId) != null)
                {
                    string displayName = "星际组装厂".Translate() + " " + GameMain.galaxy.StarById(starId).displayName;
                    UITools.Utils.UITextTruncateShow(__instance.planetNameText, ref displayName, __instance.planetNameTextWidthLimit, ref __instance.planetNameTextSettings);
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
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.Statistics);
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
            MMSCPU.EndSample(ECpuWorkEntryExtended.Statistics);
            MMSCPU.EndSample(ECpuWorkEntryExtended.MoreMegaStructure);
            return true;
        }



        public static bool IsStarAssemblyAstroFilter(int astroFilter)
        {
            int starIndex = astroFilter / 100 - 1;
            if (starIndex >= 0 && starIndex < GameMain.galaxy.stars.Length && starIndex < MoreMegaStructure.StarMegaStructureType.Length && MoreMegaStructure.StarMegaStructureType[starIndex] == (int)EMegaType.InterstellarAssembly)
            {
                int planetIndex = astroFilter % 100 - 1;

                // 如果是那个astroFilter代表着这个星系行星数+1（超出planets长度的那个）的那个行星，也就是星际组装厂的替代占位index
                if (planetIndex >= 0 && planetIndex == GameMain.galaxy.stars[starIndex].planets.Length)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 独特的定义星际组装厂的astroId是多少
        /// </summary>
        /// <param name="starIndex"></param>
        /// <returns></returns>
        public static int DefineStarAssemblyAstroId(int starIndex)
        {
            if (starIndex < 0 || starIndex >= GameMain.galaxy.stars.Length || starIndex >= MoreMegaStructure.StarMegaStructureType.Length)
                return 100;
            else
                return (starIndex + 1) * 100 + GameMain.galaxy.stars[starIndex].planetCount + 1;
        }








        public static void Export(BinaryWriter w)
        {
            //w.Write(0);
            //w.Write(GameMain.statistics.production.factoryStatPool.Length - GameMain.data.factories.Length);
            //for (int i = GameMain.data.factories.Length; i < GameMain.statistics.production.factoryStatPool.Length; i++)
            //{
            //    GameMain.statistics.production.factoryStatPool[i].Export(w.BaseStream, w);
            //}
        }

        public static void Import(BinaryReader r)
        {
            RearrangeStatisticLists();
            if (MoreMegaStructure.savedModVersion >= 119 && MoreMegaStructure.savedModVersion<=134)
            {
                r.ReadInt32();
                int num = r.ReadInt32();
                int count = Math.Min(GameMain.statistics.production.factoryStatPool.Length, GameMain.data.factories.Length + num);
                for (int i = GameMain.data.factories.Length; i < count; i++)
                {
                    GameMain.statistics.production.factoryStatPool[i].Import(r.BaseStream, r);
                }
            }
        }

        public static void IntoOtherSave()
        {
            RearrangeStatisticLists();
        }


        // 以下为测试用
        /*

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "_OnCreate")]
        public static bool OnCreatePrePatch()
        {
            Utils.Log("before on create");
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "_OnCreate")]
        public static void OnCreatePostPatch()
        {
            Utils.Log("after on create");
        }




        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "RefreshAll")]
        public static bool RefreshAllPatch(ref UIStatisticsWindow __instance)
        {
            return true;
            var _this = __instance;
            _this.TabButtonHighlighted();
            _this.TabPanelActive();
            _this.FavoriteButtonHighlighted();
            _this.KillFavoriteButtonHighlighted();
            _this.ValueToSortBox();
            _this.ValueToTimeBox();
            _this.RefreshAstroBox();
            _this.ValueToAstroBox();
            _this.detailUpdateTick = 0L;
            _this.lastListCursor = 0;
            _this.lastBeginIndex = -1;
            _this.productEntryList.ResetListStatus();
            _this.killEntryList.ResetListStatus();
            return false;
        }



        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "_OnOpen")]
        public static bool OnOpenPrePatch(ref UIStatisticsWindow __instance)
        {
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "_OnOpen")]
        public static void OnOpenPostPatch()
        {
            Utils.Log("after on open");
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "_OnUpdate")]
        public static bool OnUpdatePrePatch()
        {
            Utils.Log("before on update");
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "_OnUpdate")]
        public static void OnUpdatePostPatch()
        {
            Utils.Log("after on update");
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "_OnInit")]
        public static bool OnInitPrePatch()
        {
            Utils.Log("before on init");
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "_OnInit")]
        public static void OnInitPostPatch()
        {
            Utils.Log("after on init");
        }

        // 需要在调用PlanetId=指示巨构生产统计的虚空行星的时候返回一个factoryIndex正确的planetData，而非null，由于和GS不兼容，已废弃
        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(GalaxyData), "PlanetById")]
        //public static void PlanetByIdPostPatch(GalaxyData __instance, int planetId, ref PlanetData __result)
        //{
        //    if (__result != null) // 已经找到的planetData不拦截
        //        return;
        //    try
        //    {
        //        int num = planetId / 100 - 1; // starIndex
        //        if (num < 0 || num >= __instance.stars.Length)
        //        {
        //            return;
        //        }
        //        int num2 = planetId % 100 - 1; // planet index in star system
        //        if (num2 >= __instance.stars[num].planets.Length)
        //        {
        //            if (__instance.stars[num] == null)
        //            {
        //                return;
        //            }

        //            if (num2 >= __instance.stars[num].planets.Length)
        //            {
        //                PlanetData planet = new PlanetData();
        //                planet.factory = null;
        //                for (int i = num * 100 + 1; i < planetId; i++) // 这里是为了，bottleneck会用工厂信息，随便找一个不是null的factory搪塞一下，防止他报错。反正后面大概也许把需要处理的数据还得都覆盖一遍
        //                {
        //                    int index = i - num * 100 - 1;
        //                    if (index >= 0 && index < __instance.stars[num].planets.Length)
        //                    {
        //                        PlanetData pd = __instance.stars[num].planets[index];
        //                        if (pd != null && pd.factory != null)
        //                        {
        //                            planet.factory = pd.factory;
        //                            break;
        //                        }
        //                    }
        //                    if (i == planetId - 1)
        //                    {
        //                        Utils.Log("planetById Postfix found no factory to replace null, now returning new PlanetData() as result");
        //                    }
        //                }
        //                if (planet.factory == null)
        //                {
        //                    for (int i = 0; i < GameMain.data.factories.Length && i < GameMain.data.factoryCount; i++)
        //                    {
        //                        if (GameMain.data.factories[i] != null)
        //                            planet.factory = GameMain.data.factories[i];
        //                    }
        //                }
        //                planet.factoryIndex = GameMain.data.factories.Length + GameMain.galaxy.starCount - num - 1;
        //                planet.overrideName = Utils.MegaNameByType(MoreMegaStructure.StarMegaStructureType[num]) +
        //                                      " " +
        //                                      GameMain.galaxy.StarById(num + 1).displayName;
        //                __result = planet;
        //                return;
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        Utils.Log("Error in MoreMegaStructure PalnetByIdPostPatch.");
        //    }
        //}
        */

    }
}
