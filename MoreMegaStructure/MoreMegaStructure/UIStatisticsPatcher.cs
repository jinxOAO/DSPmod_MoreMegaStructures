using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using Steamworks;
using System.Reflection;
using static UIReferenceSpeedTip;
using UnityEngine.Playables;
using System.Linq;

namespace MoreMegaStructure
{
    public class UIStatisticsPatcher
    {
        public const int starAssemblyRepresentativeItemId = 9493; // 用这个itemId代表巨构组装厂的生产机器itemId

        public static bool enabled = true;
        public static bool forceBanned = false;
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
            if (__instance == null)
            {
                return true;
            }
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.Statistics);
            var _this = __instance;

            // 原方法逻辑：看但凡有在GameData.factories这个列表里的工厂，但是又没在 this.astroBox.Items;和this.astroBox.ItemsData;里面的，将其planetId插入到正确位置（如果是整个星系的第一个插入的工厂，则额外先pushback一个（注意星系在这个ItemsData列表里是不按id排序的，而是按加入顺序排序的，所以说pushback，不是插入）该星系的总统计(starId*100)，然后再add该行星工厂的planetId）。
            // 为了增加巨构独有的production统计，用每个星系第planetCount+1号不存在planet的id存储巨构的统计，这也是astroFilter的值
            // 其他情况下，AstroFilter / 100 为StarId。
            // 组装厂的factoryIndex为GameMain.data.factories.Length + GameMain.galaxy.starCount - starId
            if (_this.isProductionTab) // 只有productionTab才能生成组装厂的astroBox，防止“非生产统计面板中能选到星际组装厂页面”这种情况发生，因为某些面板会在选到组装厂页面报错，并且还能加入仪表盘（也会报错）
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

                // 新增部分：在该星系组末尾注入巨构虚项（此处为星际组装厂 type 4）
                InjectMegaAstroEntriesIntoList(items, itemsData, 4);

                // 这一行使原本的
                _this.ValueToAstroBox();
                MMSCPU.EndSample(ECpuWorkEntryExtended.Statistics);
                MMSCPU.EndSample(ECpuWorkEntryExtended.MoreMegaStructure);
                return false;
            }
            MMSCPU.EndSample(ECpuWorkEntryExtended.Statistics);
            MMSCPU.EndSample(ECpuWorkEntryExtended.MoreMegaStructure);
            return true;
        }

        /// <summary>
        /// 在 astroBox 的下拉列表(按恒星分组：先恒星系总计 starId*100，再各行星 planetId)中，
        /// 于每个含指定巨构类型的星系组末尾插入该巨构的虚行星项(starId*100 + planetCount + 1)，
        /// 显示名取 MMSPlanetById(...).displayName。生产页以 megaType=4(星际组装厂)调用；
        /// 研究页以 megaType=2(科学枢纽)调用。每个星系至多一种巨构，故与另一类型不冲突。
        /// </summary>
        public static void InjectMegaAstroEntriesIntoList(List<string> items, List<int> itemsData, int megaType)
        {
            int oldStarId = -1;
            for (int i = 0; i < itemsData.Count; i++)
            {
                int nowStarId = itemsData[i] / 100;
                if (oldStarId != nowStarId && oldStarId != -1 && oldStarId <= MoreMegaStructure.MegaArrayLength && oldStarId > 0)
                {
                    if (MoreMegaStructure.StarMegaStructureType[oldStarId - 1] == megaType)
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
                    if (oldStarId > 0 && oldStarId <= MoreMegaStructure.MegaArrayLength && MoreMegaStructure.StarMegaStructureType[oldStarId - 1] == megaType)
                    {
                        itemsData.Add(oldStarId * 100 + GameMain.galaxy.StarById(oldStarId).planetCount + 1);
                        items.Add(MMSPlanetById(oldStarId * 100 + GameMain.galaxy.StarById(oldStarId).planetCount + 1).displayName);
                        break;
                    }
                }

                oldStarId = nowStarId;
            }
        }

        /// <summary>
        /// 研究标签页下拉注入科学枢纽项。生产页前缀对 isProductionTab 返回 false(原生被跳过)，
        /// 故本 postfix 不在生产页触发；研究/电力/戴森/击杀页前缀返回 true(原生执行)，postfix 触发后
        /// 仅当 isResearchTab 时以 megaType=2 注入科学枢纽虚项，其它页无操作。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "RefreshAstroBox")]
        public static void RefreshAstroBoxResearchPostPatch(ref UIStatisticsWindow __instance)
        {
            if (!enabled) return;
            if (__instance == null) return;
            if (!__instance.isResearchTab) return;
            if (__instance.astroBox == null) return;
            List<string> items = __instance.astroBox.Items;
            List<int> itemsData = __instance.astroBox.ItemsData;
            if (items == null || itemsData == null) return;
            try
            {
                InjectMegaAstroEntriesIntoList(items, itemsData, 2);
                __instance.ValueToAstroBox();
            }
            catch (Exception)
            {
            }
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
            __instance.factoryTrafficPool = new AstroTrafficStat[__instance.gameData.factories.Length + GameMain.galaxy.starCount];
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
                    if (idx < MoreMegaStructure.MegaArrayLength && (MoreMegaStructure.StarMegaStructureType[idx] == 4 || MoreMegaStructure.StarMegaStructureType[idx] == 2)) __instance.AddProductStatGroup(i);
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
                        if (idx < MoreMegaStructure.MegaArrayLength && (MoreMegaStructure.StarMegaStructureType[idx] == 4 || MoreMegaStructure.StarMegaStructureType[idx] == 2)) __instance.AddProductStatGroup(k);
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
                        if(uistatGroup.factoryIndex < GameMain.data.factories.Length)
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
                if(MoreMegaStructure.StarMegaStructureType.Length > starIndex && starIndex >= 0 && (MoreMegaStructure.StarMegaStructureType[starIndex] == 4 || MoreMegaStructure.StarMegaStructureType[starIndex] == 2))
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
                if (MoreMegaStructure.StarMegaStructureType.Length > starIndex && starIndex >= 0 && (MoreMegaStructure.StarMegaStructureType[starIndex] == 4 || MoreMegaStructure.StarMegaStructureType[starIndex] == 2))
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
        /// 多线程统计路径下游戏只会调用 PrepareTick_Parallel（其内部按 factoryCount 分段处理真实工厂，
        /// 永远不涉及借用给巨构的尾部槽位）。这里仅在 threadOrdinal==0 的工作线程上补齐巨构尾部槽位的
        /// PrepareTick，保证与串行路径行为一致且每 tick 恰好一次。巨构槽位与真实工厂头部槽位互不相交，
        /// 因此与其它线程的头部处理无数据竞争。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProductionStatistics), "PrepareTick_Parallel")]
        public static void ProductionStatisticsPrepareTickParallelPostPatch(ref ProductionStatistics __instance, int threadOrdinal)
        {
            if (threadOrdinal != 0) return;
            int maxLen = Math.Min(__instance.gameData.factories.Length + GameMain.galaxy.starCount, __instance.factoryStatPool.Length);
            int endIndex = __instance.gameData.factories.Length + GameMain.galaxy.starCount - 1;
            for (int i = __instance.gameData.factories.Length; i < maxLen; i++)
            {
                int starIndex = endIndex - i;
                if (MoreMegaStructure.StarMegaStructureType.Length > starIndex && starIndex >= 0 && (MoreMegaStructure.StarMegaStructureType[starIndex] == 4 || MoreMegaStructure.StarMegaStructureType[starIndex] == 2))
                    __instance.factoryStatPool[i].PrepareTick();
            }
        }

        /// <summary>
        /// 多线程统计路径下补齐巨构尾部槽位的 GameTick，使实际产出/消耗的累计量在默认多线程模式下也能统计。
        /// 仅在 threadOrdinal==0 工作线程执行一次；注意此处绝不调用 OnItemChange（工作线程访问 UI 会崩溃），
        /// 面板自身会按刷新周期轮询，参考速率则由主线程的 extraInfoCalculator.GameTick 驱动。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProductionStatistics), "GameTick_Parallel")]
        public static void ProductionStatisticsGameTickParallelPostPatch(ref ProductionStatistics __instance, long time, int threadOrdinal)
        {
            if (threadOrdinal != 0) return;
            int maxLen = Math.Min(__instance.gameData.factories.Length + GameMain.galaxy.starCount, __instance.factoryStatPool.Length);
            int endIndex = __instance.gameData.factories.Length + GameMain.galaxy.starCount - 1;
            for (int i = __instance.gameData.factories.Length; i < maxLen; i++)
            {
                int starIndex = endIndex - i;
                if (MoreMegaStructure.StarMegaStructureType.Length > starIndex && starIndex >= 0 && (MoreMegaStructure.StarMegaStructureType[starIndex] == 4 || MoreMegaStructure.StarMegaStructureType[starIndex] == 2))
                    __instance.factoryStatPool[i].GameTick(time);
            }
        }


        /// <summary>
        /// 面板每次按 astroFilter 刷新真实工厂仓储时(RefreshItemsStorageCount)，同步把星际组装厂内部仓储
        /// 写入其借用的尾部槽位，使仓储数量列显示巨构内部堆积值（方案第3条）。进出口、垃圾保持 0。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProductionStatistics), "RefreshItemsStorageCount")]
        public static void RefreshItemsStorageCountPostPatch(ref ProductionStatistics __instance, int astroFilter)
        {
            RefreshAllIAStorage();
        }

        /// <summary>
        /// 刷新所有星际组装厂借用槽的内部仓储(storageCount + detailedStorageCounts)。真实工厂的仓储分类由
        /// ProductionExtraInfoCalculator 周期刷新，但 IA 借用槽不在其处理范围内，且面板正常浏览星系/全星区时
        /// 并不会调用 RefreshItemsStorageCount(它主要服务于 GoalTools/成就)，所以星系/全星区的 IA 仓储需要由
        /// RefreshProductionExtraInfo 这条与真实工厂同频的刷新路径主动补齐。
        /// </summary>
        public static void RefreshAllIAStorage()
        {
            if (GameMain.galaxy == null) return;
            int maxStar = Math.Min(GameMain.galaxy.starCount, MoreMegaStructure.StarMegaStructureType.Length);
            for (int starIndex = 0; starIndex < maxStar; starIndex++)
            {
                if (MoreMegaStructure.StarMegaStructureType[starIndex] == 4)
                    FillIAStorage(starIndex);
            }
        }

        /// <summary>
        /// 把某个星系的星际组装厂内部仓储(StarAssembly.productStorage)写入其借用的 factoryStatPool 槽位。
        /// 采用 reset 后 add 的方式，保证多次刷新幂等、且全星区/星系求和时各巨构槽位数值都是最新的。
        /// </summary>
        public static void FillIAStorage(int starIndex)
        {
            if (starIndex < 0 || starIndex >= MoreMegaStructure.StarMegaStructureType.Length) return;
            if (MoreMegaStructure.StarMegaStructureType[starIndex] != 4) return;
            if (!StarAssembly.productStorage.ContainsKey(starIndex)) return;
            if (GameMain.data == null || GameMain.data.statistics == null || GameMain.data.statistics.production == null) return;
            FactoryProductionStat[] pool = GameMain.data.statistics.production.factoryStatPool;
            if (pool == null) return;
            int idx = StarIndex2FactoryIndex(starIndex);
            if (idx < GameMain.data.factories.Length || idx >= pool.Length) return;
            FactoryProductionStat fps = pool[idx];
            if (fps == null) return;
            fps.ResetStorageCount();
            fps.ResetDetailedStorageCount();
            foreach (KeyValuePair<int, int> kv in StarAssembly.productStorage[starIndex])
            {
                if (kv.Value > 0)
                {
                    fps.AddStorageCount(kv.Key, kv.Value);
                    // 与原生 ProductionExtraInfoCalculator 配对写法一致：巨构内部仓储归入"其他设施"子分类
                    // (EDetailedStorageCountType.Other)，供 UIStorageCountTip 悬停子标签页读取。
                    fps.AddDetailedStorageCount(kv.Key, EDetailedStorageCountType.Other, kv.Value);
                }
            }
        }

        /// <summary>
        /// 仓储数量悬停子标签页(UIStorageCountTip.Refresh)只会累加真实工厂 [0,factoryCount) 或本星系行星工厂的
        /// detailedStorageCounts，星际组装厂借用的尾部槽位不在这个遍历范围内，导致其内部仓储不会出现在任何子分类中。
        /// 这里在原生 Refresh 之后，按当前 astroFilter 把对应范围的巨构尾部槽 detailedStorageCounts(仅"其他设施"Other 会非零)
        /// 追加进 entryDatas，再复刻原生末尾的渲染段刷新各分类数值/显隐/总计，使 IA 内部仓储与真实工厂一样计入"其他设施"，
        /// 覆盖 全星区(astroFilter==0)、所在星系(%100==0)、IA 虚行星页 三种视图；只看单个真实行星时不会混入 IA。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStorageCountTip), "Refresh")]
        public static void UIStorageCountTipRefreshPostPatch(ref UIStorageCountTip __instance)
        {
            if (!enabled) return;
            if (__instance == null) return;
            GameData data = GameMain.data;
            if (data == null || data.statistics == null || data.statistics.production == null) return;
            if (__instance.entryDatas == null || __instance.entries == null || __instance.entries.Length == 0) return;
            FactoryProductionStat[] pool = data.statistics.production.factoryStatPool;
            if (pool == null) return;

            int astroFilter = __instance.astroFilter;
            int itemId = __instance.itemId;
            long added = 0L;
            int maxStar = Math.Min(data.galaxy.starCount, MoreMegaStructure.StarMegaStructureType.Length);
            for (int starIndex = 0; starIndex < maxStar; starIndex++)
            {
                if (MoreMegaStructure.StarMegaStructureType[starIndex] != 4) continue;
                bool include;
                if (astroFilter == 0) include = true; // 全星区
                else if (astroFilter % 100 == 0) include = (starIndex == astroFilter / 100 - 1); // 所在星系
                else include = IsStarAssemblyAstroFilter(astroFilter) && starIndex == astroFilter / 100 - 1; // IA 虚行星页
                if (!include) continue;

                int idx = StarIndex2FactoryIndex(starIndex);
                if (idx < data.factories.Length || idx >= pool.Length) continue;
                FactoryProductionStat fps = pool[idx];
                if (fps == null || fps.productIndices == null || fps.productPool == null) continue;
                if (itemId < 0 || itemId >= fps.productIndices.Length) continue;
                int num = fps.productIndices[itemId];
                if (num <= 0 || num >= fps.productPool.Length) continue;
                long[] detailed = fps.productPool[num].detailedStorageCounts;
                if (detailed == null) continue;
                for (int j = 0; j < __instance.entryDatas.Length && j < detailed.Length; j++)
                {
                    if (detailed[j] != 0L)
                    {
                        __instance.entryDatas[j] += detailed[j];
                        added += detailed[j];
                    }
                }
            }
            if (added == 0L) return; // 无巨构贡献则完全保持原生显示

            // 复刻原生 Refresh 末尾渲染段：按更新后的 entryDatas 重新计算显隐/数值/总计
            GameHistoryData history = data.history;
            long total = 0L;
            float y = __instance.entries[0].rectTrans.anchoredPosition.y;
            for (int k = 0; k < __instance.entries.Length; k++)
            {
                if ((k == 4 && __instance.entryDatas[k] == 0L) || (k == 1 && history != null && !history.logisticsSystemUnlocked && __instance.entryDatas[k] == 0L))
                {
                    __instance.entries[k].gameObject.SetActive(false);
                }
                else
                {
                    __instance.entries[k].Refresh(__instance.entryDatas[k], ref y);
                    total += __instance.entryDatas[k];
                }
            }
            __instance.rectTrans.sizeDelta = new Vector2(276f, -y + 12f);
            __instance.totalCountText.text = total.ToString("#,##0");
        }

        /// <summary>
        /// 游戏原生 RefreshProductionExtraInfo 的最后一条 else 分支会对 astroFilter 直接执行
        /// gameData.galaxy.PlanetById(astroFilter).factoryIndex。星际组装厂的虚行星 id（starId*100+planetCount+1）
        /// 既不是 -1、也不是 0、更不是 %100==0，会落入该 else 分支，而真实 PlanetById 返回 null 触发空引用崩溃。
        /// 这里拦截该虚行星场景：巨构的参考速率/仓储/进出口由本类其它补丁(MMSRefreshItemsCyclicRefSpeedWithFactory、
        /// FillIAStorage)单独维护，且方案要求进出口/垃圾恒为 0，因此无需进入 extraInfoCalculator（其内部还会用
        /// gameData.factories[num] 索引真实工厂，借用槽位越界会再次崩溃），直接跳过原方法即可。
        /// 其余（真实行星/星系/全星区/当前星球）一律交回原方法处理。
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStatisticsWindow), "RefreshProductionExtraInfo")]
        public static bool RefreshProductionExtraInfoPrePatch(ref UIStatisticsWindow __instance)
        {
            if (!enabled) return true;
            if (__instance == null) return true;
            if (!__instance.isProductionTab || __instance.astroFilter < -1) return true;
            int astroFilter = __instance.astroFilter;
            // 全星系(-1)/星系(%100==0)视图下，原生用 extraInfoCalculator 持续刷新真实工厂的仓储数量与分类，
            // 但 IA 借用槽不在其处理范围内；而面板正常浏览这些视图时并不会调用 RefreshItemsStorageCount，
            // 导致星系/全星区里来自星际组装厂的仓储数据不刷新(只有进入 IA 页面才更新)。这里顺带补齐所有 IA 的内部仓储。
            if (astroFilter == -1 || astroFilter % 100 == 0)
            {
                try { RefreshAllIAStorage(); }
                catch (Exception) { }
            }
            // 仅当原方法会走 else 分支（非 -1、非 %100==0）且真实行星查不到时，才认定为巨构虚行星并跳过。
            int iaFactoryIndex;
            if (astroFilter != -1 && astroFilter % 100 != 0 && TryGetIAFactoryIndex(astroFilter, out iaFactoryIndex))
            {
                // 原方法此处会执行 PlanetById(astroFilter).factoryIndex，对巨构虚行星返回 null 而崩溃；且绝不能把借用索引
                // 塞进 extraInfoCalculator（其 CalculateImmediately/GameTick 会用 gameData.factories[num] 索引，越界二次崩溃）。
                // 巨构的参考速率与内部仓储改由下面直接刷新（不再依赖 extraInfoCalculator 这条永远进不去、也从不触发的路径）。
                try
                {
                    RefreshIARefSpeedAndStorage(FactoryIndex2StarIndex(iaFactoryIndex));
                }
                catch (Exception)
                {
                }
                return false; // 跳过原方法，规避空引用
            }
            return true;
        }

        /// <summary>
        /// 取某星系星际组装厂借用的 FactoryProductionStat 槽位（含全部边界/空判断），取不到返回 null。
        /// </summary>
        public static FactoryProductionStat GetIAFactoryStat(int starIndex)
        {
            if (starIndex < 0 || starIndex >= MoreMegaStructure.StarMegaStructureType.Length) return null;
            if (GameMain.data == null || GameMain.data.statistics == null || GameMain.data.statistics.production == null) return null;
            FactoryProductionStat[] pool = GameMain.data.statistics.production.factoryStatPool;
            if (pool == null) return null;
            int idx = StarIndex2FactoryIndex(starIndex);
            if (idx < GameMain.data.factories.Length || idx >= pool.Length) return null;
            return pool[idx];
        }

        /// <summary>
        /// 直接刷新星际组装厂的期望生产/消耗参考速率与内部仓储。原本这一步只在
        /// ProductionExtraInfoCalculator.GameTick 处理借用索引时经 CalculateFactory_IA 触发，但由于原生
        /// RefreshProductionExtraInfo 对巨构虚行星在 AddFactory 之前就空引用崩溃、而拦截补丁又跳过原方法，
        /// 借用索引永远不会进入 extraInfoCalculator，导致参考速率恒为 0。改为在打开 IA 统计页时直接计算。
        /// </summary>
        public static void RefreshIARefSpeedAndStorage(int starIndex)
        {
            if (starIndex < 0 || starIndex >= MoreMegaStructure.StarMegaStructureType.Length) return;
            if (MoreMegaStructure.StarMegaStructureType[starIndex] != 4) return;
            if (GetIAFactoryStat(starIndex) == null) return;
            int maxInc = 0;
            try
            {
                GameHistoryData history = GameMain.data.history;
                ItemProto itemProto = LDB.items.Select(2313);
                int[] incItemIds = (itemProto != null) ? itemProto.prefabDesc.incItemId : null;
                if (incItemIds != null && history != null)
                {
                    for (int i = 0; i < incItemIds.Length; i++)
                    {
                        if (history.ItemUnlocked(incItemIds[i]))
                        {
                            ItemProto p = LDB.items.Select(incItemIds[i]);
                            if (p != null && p.Ability > maxInc) maxInc = p.Ability;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            MMSRefreshItemsCyclicRefSpeedWithFactory(starIndex, maxInc);
            FillIAStorage(starIndex);
        }



        /// <summary>
        /// 在游戏创建 ProductionStatistics 时立刻把 factoryStatPool 扩容为
        /// factories.Length + starCount，并为借用槽位初始化 FactoryProductionStat。
        /// 否则新开局（无 Import/IntoOtherSave 流程）池子仍是 factories.Length 长度，
        /// 后续所有 [factories.Length + starCount - 1 - starIndex] 的巨构槽位访问都会越界。
        /// 直接基于 __instance 操作而非 GameMain.data，避免加载期 GameMain.data 尚未指向本 gameData 的时序问题。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProductionStatistics), "Init")]
        public static void ProductionStatisticsInitPostPatch(ref ProductionStatistics __instance)
        {
            if (!enabled) return;
            if (__instance == null || __instance.gameData == null) return;
            int factoriesLen = __instance.gameData.factories.Length;
            int starCount = __instance.gameData.galaxy.starCount;
            if (__instance.factoryStatPool != null && __instance.factoryStatPool.Length > factoriesLen) return; // 已扩容
            FactoryProductionStat[] old = __instance.factoryStatPool;
            __instance.factoryStatPool = new FactoryProductionStat[factoriesLen + starCount];
            if (old != null)
            {
                for (int i = 0; i < old.Length && i < __instance.factoryStatPool.Length; i++)
                    __instance.factoryStatPool[i] = old[i];
            }
            for (int i = factoriesLen; i < __instance.factoryStatPool.Length; i++)
            {
                if (__instance.factoryStatPool[i] == null)
                {
                    __instance.factoryStatPool[i] = new FactoryProductionStat();
                    __instance.factoryStatPool[i].Init();
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
            int slotCount = StarAssembly.recipeIds[starIndex].Count;
            for (int slot = 0; slot < slotCount; slot++)
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
            bool flag = false;
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
            int slotCount = StarAssembly.recipeIds[starIndex].Count;
            for (int slot = 0; slot < slotCount; slot++)
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
                    // 全星区视图会枚举所有建了星际组装厂的恒星；仅当该组装厂对本次悬停物品确有贡献
                    // (子条目数据已按 astroId 填充)时才列出，否则会出现名称"???"、数量 x0、占比 0% 的空条目。
                    // 与下方原生对行星子条目的过滤条件(loadedSubTipDatas[astroId].astroId == astroId)保持一致。
                    int iaSubAstroId = DefineStarAssemblyAstroId(i);
                    bool flagIAHasSubData = __instance.loadedSubTipDatas != null && iaSubAstroId >= 0 && iaSubAstroId < __instance.loadedSubTipDatas.Length && __instance.loadedSubTipDatas[iaSubAstroId].astroId == iaSubAstroId;
                    if ( flagIsHoveringStarAssembly && flagStarIsStarAssembly && flagIAHasSubData && (flagFilterIsTotalGalaxyData || flagFilterIsThisStarSystem || flagFilterIsThisStarAssembly))
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
                    __instance.planetNameText.text = displayName;
                }
            }
        }


        /// <summary>
        /// 下面这个防止选中巨构时点击能量面板报错（factoryIndex越界或者是找回的），可能不再需要了
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="powerPool"></param>
        /// <param name="energyConsumption"></param>
        /// <param name="factoryIndex"></param>
        /// <returns></returns>
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(UIStatisticsWindow), "ComputePowerTab")]
        //public static bool ComputePowerTabPrePatch(
        //    ref UIStatisticsWindow __instance,
        //    PowerStat[] powerPool,
        //    long energyConsumption,
        //    long factoryIndex)
        //{
        //    MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
        //    MMSCPU.BeginSample(ECpuWorkEntryExtended.Statistics);
        //    if (factoryIndex >= GameMain.data.factories.Length)
        //    {
        //        long num = __instance.ComputePower(powerPool[0]);
        //        __instance.productEntryList.Add(1, num, 0L, energyConsumption);
        //        num = __instance.ComputePower(powerPool[1]);
        //        __instance.productEntryList.Add(1, 0L, num);
        //        num = __instance.ComputePower(powerPool[3]);
        //        long num2 = 0L;
        //        __instance.productEntryList.Add(2, num, 0L, num2);
        //        num = __instance.ComputePower(powerPool[2]);
        //        __instance.productEntryList.Add(2, 0L, num);
        //        return false;
        //    }
        //    MMSCPU.EndSample(ECpuWorkEntryExtended.Statistics);
        //    MMSCPU.EndSample(ECpuWorkEntryExtended.MoreMegaStructure);
        //    return true;
        //}



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


        // 以下为20250123晚更新后0.10.32.25496版本后的适配
        // 工具，判断是不是指向组装厂的astroFilter并且返回factoryIndex
        public static bool TryGetIAFactoryIndex(int astroFilter, out int factoryIndex)
        {
            if (astroFilter % 100 != 0)
            {
                int starId = astroFilter / 100;
                if (GameMain.galaxy.StarById(starId) != null)
                {
                    int planetOrder = astroFilter % 100;
                    if (planetOrder == GameMain.galaxy.StarById(starId).planetCount + 1) // 代表是星际组装厂的统计 
                    {
                        factoryIndex = GameMain.data.factories.Length + GameMain.galaxy.starCount - starId;
                        return true;
                    }
                }
            }
            factoryIndex = -1;
            return false;
        }

        // 以下的多个patch，其中一部分涉及巨构有关的统计数据的，会同时有prefix和postfix为一组，prefix用于防止planet为null报错，并加入组装厂数据。postfix用于统计整个星系或者全星区的时候，将组装厂数据加入
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AstroItemProductionStatPlan), "CalculateFactoryStatGroup")]
        public static bool CalculateFactoryStatGroupPrefix(ref AstroItemProductionStatPlan __instance)
        {
            int factoryIndex;
            if (TryGetIAFactoryIndex(__instance.astroFilter, out factoryIndex))
            {
                Array.Clear(__instance.statFactoryIndices, 0, __instance.statFactoriesCursor);
                __instance.statFactoriesCursor = 0;
                __instance.AddFactoryStatGroup(factoryIndex);
                return false;
            }
            return true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AstroItemProductionStatPlan), "CalculateFactoryStatGroup")]
        public static void CalculateFactoryStatGroupPostfix(ref AstroItemProductionStatPlan __instance)
        {
            if(__instance.astroFilter == 0)
            {
                if(__instance.gameData.localPlanet == null && __instance.gameData.localStar != null)
                {
                    int starIndex = __instance.gameData.localStar.index;
                    __instance.AddFactoryStatGroup(StarIndex2FactoryIndex(starIndex));
                }
                return;
            }
            else if (__instance.astroFilter % 100 == 0) // 星系统计（而非全星区）则加入组装厂数据
            {
                int starIndex = __instance.astroFilter / 100 - 1;                
                if (starIndex >= 0)
                {
                    __instance.AddFactoryStatGroup(StarIndex2FactoryIndex(starIndex));
                }
                return;
            }

            if (__instance.astroFilter == -1 || __instance.astroFilter == 0 && __instance.gameData.localStar == null && __instance.gameData.localPlanet == null) // 全星区统计加入所有星际组装厂
            {
                for (int starIndex = 0; starIndex < GameMain.galaxy.starCount && starIndex < MoreMegaStructure.StarMegaStructureType.Length; starIndex++)
                {
                    if (MoreMegaStructure.StarMegaStructureType[starIndex] == 4)
                        __instance.AddFactoryStatGroup(StarIndex2FactoryIndex(starIndex));
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AstroItemProductionStatPlan), "CalculateExtraInfo")]
        public static bool CalculateExtraInfoPrefix(ref AstroItemProductionStatPlan __instance)
        {
            int factoryIndex;
            if (TryGetIAFactoryIndex(__instance.astroFilter, out factoryIndex))
            {
                __instance.gameData.statistics.production.extraInfoCalculator.AddFactory(factoryIndex);
                return false;
            }
            return true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AstroItemProductionStatPlan), "CalculateExtraInfo")]
        public static void CalculateExtraInfoPostfix(ref AstroItemProductionStatPlan __instance)
        {
            if (__instance.astroFilter == 0)
            {
                if (__instance.gameData.localPlanet == null && __instance.gameData.localStar != null)
                {
                    int starIndex = __instance.gameData.localStar.index;
                    __instance.gameData.statistics.production.extraInfoCalculator.AddFactory(StarIndex2FactoryIndex(starIndex));
                }
                return;
            }
            else if (__instance.astroFilter % 100 == 0) // 星系统计（而非全星区）则加入组装厂数据
            {
                int starIndex = __instance.astroFilter / 100 - 1;
                if (starIndex >= 0)
                {
                    __instance.gameData.statistics.production.extraInfoCalculator.AddFactory(StarIndex2FactoryIndex(starIndex));
                }
                return;
            }

            if (__instance.astroFilter == -1 || __instance.astroFilter == 0 && __instance.gameData.localStar == null && __instance.gameData.localPlanet == null) // 全星区统计加入所有星际组装厂
            {
                for (int starIndex = 0; starIndex < GameMain.galaxy.starCount && starIndex < MoreMegaStructure.StarMegaStructureType.Length; starIndex++)
                {
                    if (MoreMegaStructure.StarMegaStructureType[starIndex] == 4)
                        __instance.gameData.statistics.production.extraInfoCalculator.AddFactory(StarIndex2FactoryIndex(starIndex));
                }
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AstroItemProductionStatPlan), "CalculateImportAndExport")]
        public static bool CalculateImportAndExportPrefix(ref AstroItemProductionStatPlan __instance, out long import, out long export)
        {
            import = 0;
            export = 0;
            int factoryIndex;
            if (TryGetIAFactoryIndex(__instance.astroFilter, out factoryIndex))
            {
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AstroItemProductionStatPlan), "CalculateTrafficDetail")]
        public static bool CalculateTrafficDetailPrefix(ref AstroItemProductionStatPlan __instance)
        {
            int factoryIndex;
            if (TryGetIAFactoryIndex(__instance.astroFilter, out factoryIndex))
            {
                Array.Clear(__instance.trafficDetail, 0, __instance.trafficDetail.Length);
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AstroItemProductionStatPlan), "GetGalacticTransportStorageCount")]
        public static bool GetGalacticTransportStorageCountPrefix(ref AstroItemProductionStatPlan __instance, out long importStorageCount, out long exportStorageCount)
        {
            importStorageCount = 0L;
            exportStorageCount = 0L;
            int factoryIndex;
            if (TryGetIAFactoryIndex(__instance.astroFilter, out factoryIndex))
            {
                FactoryProductionStat[] factoryStatPool = __instance.gameData.statistics.production.factoryStatPool;
                int num5 = factoryStatPool[factoryIndex].productIndices[__instance.itemId];
                if (num5 > 0)
                {
                    importStorageCount += factoryStatPool[factoryIndex].productPool[num5].importStorageCount;
                    exportStorageCount += factoryStatPool[factoryIndex].productPool[num5].exportStorageCount;
                }
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AstroItemProductionStatPlan), "GetItemsCyclicRefSpeed")]
        public static bool GetItemsCyclicRefSpeedPrefix(ref AstroItemProductionStatPlan __instance, out float productRefSpeed, out float consumeRefSpeed)
        {
            productRefSpeed = 0f;
            consumeRefSpeed = 0f;
            int factoryIndex;
            if (TryGetIAFactoryIndex(__instance.astroFilter, out factoryIndex))
            {
                FactoryProductionStat[] factoryStatPool = __instance.gameData.statistics.production.factoryStatPool;
                int num5 = factoryStatPool[factoryIndex].productIndices[__instance.itemId];
                if (num5 > 0)
                {
                    productRefSpeed += factoryStatPool[factoryIndex].productPool[num5].refProductSpeed;
                    consumeRefSpeed += factoryStatPool[factoryIndex].productPool[num5].refConsumeSpeed;
                }
                return false;
            }
            return true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AstroItemProductionStatPlan), "GetItemsCyclicRefSpeed")]
        public static void GetItemsCyclicRefSpeedPostfix(ref AstroItemProductionStatPlan __instance, ref float productRefSpeed, ref float consumeRefSpeed)
        {
            FactoryProductionStat[] factoryStatPool = __instance.gameData.statistics.production.factoryStatPool;
            if (__instance.astroFilter == 0)
            {
                if (__instance.gameData.localPlanet == null && __instance.gameData.localStar != null)
                {
                    int starIndex = __instance.gameData.localStar.index; 
                    int factoryIndex = StarIndex2FactoryIndex(starIndex);
                    int num5 = factoryStatPool[factoryIndex].productIndices[__instance.itemId];
                    if (num5 > 0)
                    {
                        productRefSpeed += factoryStatPool[factoryIndex].productPool[num5].refProductSpeed;
                        consumeRefSpeed += factoryStatPool[factoryIndex].productPool[num5].refConsumeSpeed;
                    }
                }
                return;
            }
            else if (__instance.astroFilter % 100 == 0) // 星系统计（而非全星区）则加入组装厂数据
            {
                int starIndex = __instance.astroFilter / 100 - 1;
                if (starIndex >= 0)
                {
                    int factoryIndex = StarIndex2FactoryIndex(starIndex);
                    int num5 = factoryStatPool[factoryIndex].productIndices[__instance.itemId];
                    if (num5 > 0)
                    {
                        productRefSpeed += factoryStatPool[factoryIndex].productPool[num5].refProductSpeed;
                        consumeRefSpeed += factoryStatPool[factoryIndex].productPool[num5].refConsumeSpeed;
                    }
                }
                return;
            }

            if (__instance.astroFilter == -1 || __instance.astroFilter == 0 && __instance.gameData.localStar == null && __instance.gameData.localPlanet == null) // 全星区统计加入所有星际组装厂
            {
                for (int starIndex = 0; starIndex < GameMain.galaxy.starCount && starIndex < MoreMegaStructure.StarMegaStructureType.Length; starIndex++)
                {
                    if (MoreMegaStructure.StarMegaStructureType[starIndex] == 4)
                    {
                        int factoryIndex = StarIndex2FactoryIndex(starIndex);
                        int num5 = factoryStatPool[factoryIndex].productIndices[__instance.itemId];
                        if (num5 > 0)
                        {
                            productRefSpeed += factoryStatPool[factoryIndex].productPool[num5].refProductSpeed;
                            consumeRefSpeed += factoryStatPool[factoryIndex].productPool[num5].refConsumeSpeed;
                        }
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AstroItemProductionStatPlan), "GetItemsStorageGroupCount")]
        public static bool GetItemsStorageGroupCountPrefix(ref AstroItemProductionStatPlan __instance, out long storageCount, out long trashCount)
        {
            storageCount = 0L;
            trashCount = 0L;
            int factoryIndex;
            if (TryGetIAFactoryIndex(__instance.astroFilter, out factoryIndex))
            {
                FactoryProductionStat[] factoryStatPool = __instance.gameData.statistics.production.factoryStatPool;
                int num5 = factoryStatPool[factoryIndex].productIndices[__instance.itemId];
                if (num5 > 0)
                {
                    storageCount += factoryStatPool[factoryIndex].productPool[num5].storageCount;
                    trashCount += factoryStatPool[factoryIndex].productPool[num5].trashCount;
                }
                bool flag = (__instance.gameData.localPlanet != null && !__instance.gameData.mainPlayer.sailing);
                if (flag)
                {
                    storageCount += __instance.gameData.statistics.production.playerStorageCounts[__instance.itemId];
                }
                return false;
            }
            return true;
        }

        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIChartAstroItemProduction), "SetTipFormatString")]
        public static bool SetTipFormatStringPrefix(ref UIChartAstroItemProduction __instance)
        {
            int factoryIndex;
            if (TryGetIAFactoryIndex(__instance.statPlan.astroFilter, out factoryIndex))
            {
                if(__instance.titleTip != null && __instance.statPlan != null)
                {
                    string text = "";

                    text = text + string.Format("#{0} ", __instance.statPlan.id) + __instance.statPlan.displayName + ";";
                    GameData gameData = __instance.statPlan.gameData;
                    int astroFilter = __instance.statPlan.astroFilter;
                    StarData starData = gameData.galaxy.StarById(astroFilter / 100);
                    text = text +  "星际组装厂".Translate() + starData.displayName + ";";
                    switch (__instance.statPlan.timeLevel)
                    {
                        case 0:
                            text += "统计1分钟".Translate();
                            break;
                        case 1:
                            text += "统计10分钟".Translate();
                            break;
                        case 2:
                            text += "统计1小时".Translate();
                            break;
                        case 3:
                            text += "统计10小时".Translate();
                            break;
                        case 4:
                            text += "统计100小时".Translate();
                            break;
                        case 5:
                            text += "统计总计".Translate();
                            break;
                    }
                    __instance.titleTip.tipTextFormatString = text;
                }

                return false;
            }
            return true;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(ProductionExtraInfoCalculator), "GameTick")]
        public static bool ProductionExtraInfoCalculator_GameTick_Prefix(ref ProductionExtraInfoCalculator __instance)
        {
            if (__instance.prepareCalcFactoryIndices.Count > 0 && __instance.prepareCalcFactoryIndices.ElementAt(0) >= __instance.productionStatistics.gameData.factories.Length) // 说明是星际组装厂
            {
                int factoryIndex = __instance.prepareCalcFactoryIndices.ElementAt(0);

                CalculateFactory_IA(ref __instance, __instance.productionStatistics.gameData, factoryIndex);

                __instance.prepareCalcFactoryIndices.Remove(factoryIndex);
                return false;
            }
            return true;
        }

        public static void CalculateFactory_IA(ref ProductionExtraInfoCalculator __instance, GameData gameData, int factoryIndex)
        {
            FactoryProductionStat factoryProductionStat = __instance.productionStatistics.factoryStatPool[factoryIndex];
            factoryProductionStat.ResetRefSpeed();
            factoryProductionStat.ResetStorageCount();
            factoryProductionStat.ResetDetailedStorageCount();
            factoryProductionStat.ResetTransportStorageCount();
            GameHistoryData history = gameData.history;
            ItemProto itemProto = LDB.items.Select(2313);
            int[] array = (itemProto != null) ? itemProto.prefabDesc.incItemId : null;
            int num = 0;
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (history.ItemUnlocked(array[i]))
                    {
                        ItemProto itemProto2 = LDB.items.Select(array[i]);
                        if (itemProto2 != null && itemProto2.Ability > num)
                        {
                            num = itemProto2.Ability;
                        }
                    }
                }
            }
            
            MMSRefreshItemsCyclicRefSpeedWithFactory(FactoryIndex2StarIndex(factoryIndex), num);
            FillIAStorage(FactoryIndex2StarIndex(factoryIndex));
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(AstroItemProductionStatPlan), "CalculateTrafficDetail")]
        public static void CalculateTrafficDetailPostfix(ref AstroItemProductionStatPlan __instance, ref long[] __result)
        {
            if(__result == null)
            {
                __result = new long[120];
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AstroStorageStatPlan), "CalculateExtraInfo")]
        public static bool CalculateExtraInfoPrefix(ref AstroStorageStatPlan __instance)
        {
            int factoryIndex;
            if (TryGetIAFactoryIndex(__instance.astroFilter, out factoryIndex))
            {
                int starIndex = __instance.astroFilter / 100 - 1;
                __instance.gameData.statistics.production.extraInfoCalculator.AddFactory(StarIndex2FactoryIndex(starIndex));
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIChartAstroStorage), "SetTipFormatString")]
        public static bool SetTipFormatString_Storage_Prefix(ref UIChartAstroStorage __instance)
        {
            int factoryIndex;
            if (TryGetIAFactoryIndex(__instance.statPlan.astroFilter, out factoryIndex))
            {
                if (__instance.titleTip != null && __instance.statPlan != null)
                {
                    string text = "";

                    text = text + string.Format("#{0} ", __instance.statPlan.id) + __instance.statPlan.displayName + ";";
                    GameData gameData = __instance.statPlan.gameData;
                    int astroFilter = __instance.statPlan.astroFilter;
                    StarData starData = gameData.galaxy.StarById(astroFilter / 100);
                    text = text + "星际组装厂".Translate() + starData.displayName + ";";
                    __instance.titleTip.tipTextFormatString = text;
                }

                return false;
            }
            return true;
        }


        /// <summary>
        /// 原封不动模仿游戏本体：本体对每个真实工厂调用 FactoryProductionStat.Export 存整套多时间窗滚动历史
        /// (powerPool 6x3600、每个 productPool 项 count[7200]、productIndices[12000] 等)。这里把借给巨构的
        /// 尾部槽位(factoryStatPool 中索引 &gt;= factories.Length 的部分)"一视同仁"地全部导出，不按
        /// StarMegaStructureType 过滤——正如本体对所有工厂一视同仁。这样当玩家中途切换某星系巨构类型
        /// (科学枢纽&lt;-&gt;星际组装厂等)，同一槽里研究历史/生产历史出现"有的全 0、有的混杂"时，也只是原样搬运
        /// 该槽字节，绝不会因类型判断而漏读/错位/报错。写入数量 = 尾部槽数，与 Import 端严格对称。
        /// </summary>
        public static void Export(BinaryWriter w)
        {
            RearrangeStatisticLists(); // 幂等：保证尾部槽已扩容且非 null
            FactoryProductionStat[] pool = GameMain.statistics.production.factoryStatPool;
            int start = GameMain.data.factories.Length;
            int tail = (pool != null) ? pool.Length - start : 0;
            if (tail < 0) tail = 0;
            w.Write(0);    // 结构版本标识 tag(当前为 0，供将来 blob 内部格式演进时区分)
            w.Write(tail); // 本次写入的巨构尾部槽数量
            for (int i = start; i < start + tail; i++)
            {
                FactoryProductionStat st = pool[i];
                if (st == null) // 防御性补建，确保写入槽数恒等于 tail，避免读档错位
                {
                    st = new FactoryProductionStat();
                    st.Init();
                    pool[i] = st;
                }
                st.Export(w.BaseStream, w);
            }
        }

        /// <summary>
        /// 读取由 Export 写入的巨构统计 blob。仅对 savedModVersion &gt;= 200 的存档读取：
        /// 200 是本 blob 正式启用读写、且由当前游戏版本 FactoryProductionStat 结构写出的起点。
        /// 200 之前(含历史曾写过但结构/字节布局存疑的 119-134、以及导出被注释的 135-199)一律不读取——
        /// 统计数据是按 tick 重建的可重算数据，老存档回 0 无副作用，绝不为赌老格式一致性而读出垃圾数值。
        /// 因本 blob 严格位于 mod 存档段最末且自描述，200 之前不读 = 尾部多余字节被存档框架按段长忽略，绝对安全。
        /// 夹取到当前池长度以兼容星系数量变化；全程 try/catch 兜底，任何异常都不影响读档流程本身。
        /// </summary>
        public static void Import(BinaryReader r)
        {
            RearrangeStatisticLists();
            if (MoreMegaStructure.savedModVersion < 200) return; // 200 之前(含 119-134/135-199)一律不读
            try
            {
                r.ReadInt32(); // 结构版本标识 tag
                int num = r.ReadInt32(); // 保存时的尾部槽数量
                FactoryProductionStat[] pool = GameMain.statistics.production.factoryStatPool;
                int start = GameMain.data.factories.Length;
                int count = Math.Min(pool.Length, start + num); // 夹取到当前池长度
                for (int i = start; i < count; i++)
                {
                    if (pool[i] == null) // 保证仍逐槽 Import，维持与写入端字节对齐
                    {
                        pool[i] = new FactoryProductionStat();
                        pool[i].Init();
                    }
                    pool[i].Import(r.BaseStream, r);
                }
            }
            catch (Exception)
            {
                // 读档安全兜底：任何异常都不让游戏崩溃；最坏情况统计历史回 0 并随 tick 自然重建。
            }
        }

        public static void IntoOtherSave()
        {
            RearrangeStatisticLists();
        }

        public static int FactoryIndex2StarIndex(int factoryIndex)
        {
            return GameMain.data.factories.Length + GameMain.galaxy.starCount - factoryIndex - 1;
        }

        public static int StarIndex2FactoryIndex(int starIndex)
        {
            return GameMain.data.factories.Length + GameMain.galaxy.starCount - starIndex - 1;
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
