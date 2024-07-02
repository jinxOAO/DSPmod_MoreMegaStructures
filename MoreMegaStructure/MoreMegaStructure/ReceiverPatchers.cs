using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace MoreMegaStructure
{
    public static class ReceiverPatchers
    {
        internal static readonly Dictionary<int,int> ProductId2MegaType = new Dictionary<int,int>();
        public static Dictionary<int,int> RRId2OreId = new Dictionary<int,int>();
        public static Dictionary<int, int> MDOreModeFactor = new Dictionary<int,int>();
        public static Dictionary<int, long> productHeat = new Dictionary<int, long>();

        internal static void InitRawData()
        {
            ProductId2MegaType.Add(0, 0);
            ProductId2MegaType.Add(1208, 0);
            
            ProductId2MegaType.Add(1101, 1);
            ProductId2MegaType.Add(1102, 1);
            ProductId2MegaType.Add(1104, 1);
            ProductId2MegaType.Add(1105, 1);
            ProductId2MegaType.Add(1106, 1);
            ProductId2MegaType.Add(1109, 1);
            ProductId2MegaType.Add(1016, 1);

            ProductId2MegaType.Add(1014, 5);
            ProductId2MegaType.Add(1126, 5);

            ProductId2MegaType.Add(1001, 1);
            ProductId2MegaType.Add(1002, 1);
            ProductId2MegaType.Add(1003, 1);
            ProductId2MegaType.Add(1004, 1);
            ProductId2MegaType.Add(1006, 1);

            RRId2OreId.Add(9493, 1001);
            RRId2OreId.Add(9494, 1002);
            RRId2OreId.Add(9495, 1003);
            RRId2OreId.Add(9496, 1004);
            RRId2OreId.Add(9497, 1016);
            RRId2OreId.Add(9501, 1006);
            RRId2OreId.Add(2208, 0);

            MDOreModeFactor.Add(1001, 1);
            MDOreModeFactor.Add(1002, 1);
            MDOreModeFactor.Add(1003, 2);
            MDOreModeFactor.Add(1004, 2);
            MDOreModeFactor.Add(1006, 2);

            productHeat.Add(1101, 6000000);
            productHeat.Add(1102, 6000000);
            productHeat.Add(1104, 6000000);
            productHeat.Add(1105, 12000000);
            productHeat.Add(1106, 12000000);
            productHeat.Add(1109, 12000000);


            productHeat.Add(1001, 5000000);
            productHeat.Add(1002, 5000000);
            productHeat.Add(1003, 5000000);
            productHeat.Add(1004, 5000000);
            productHeat.Add(1006, 5000000);
            productHeat.Add(1016, 6000000); // 单极磁石

            productHeat.Add(1126, 120000000);
            productHeat.Add(1014, 12000000);
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), "RequestDysonSpherePower")]
        public static bool RequestDysonSpherePowerPrePatch(ref PowerSystem __instance)
        {
            var _this = __instance;
            PlanetFactory factory = _this.factory;
            int starIndex = factory.planetId / 100 - 1;
            int megaType = MoreMegaStructure.StarMegaStructureType[starIndex];

            _this.dysonSphere = _this.factory.gameData.dysonSpheres[_this.planet.star.index];
            float eta = 1f - GameMain.history.solarEnergyLossRate;
            float increase = (_this.dysonSphere != null) ? ((float)((double)_this.dysonSphere.grossRadius / ((double)_this.planet.sunDistance * 40000.0))) : 0f;
            Vector3 normalized = _this.planet.runtimeLocalSunDirection.normalized;
            long num = 0L;
            bool flag = false;
            for (int i = 1; i < _this.genCursor; i++)
            {
                if (_this.genPool[i].gamma)
                {
                    int productId = _this.genPool[i].productId;
                    int protoId = factory.entityPool[_this.genPool[i].entityId].protoId;
                    if (productId <= 0 && MoreMegaStructure.StarMegaStructureType[starIndex] != 0) // 如果不是戴森球且设置成了直接发电模式(productId为0)，则不请求能量
                    {
                        _this.genPool[i].capacityCurrentTick = 0;
                        continue;
                    }
                    else if (productId <= 0 && protoId != 2208) // 不是普通的接收器但是设置成了发电模式，不工作
                    {
                        _this.genPool[i].capacityCurrentTick = 0;
                        continue;
                    }
                    else if (!ProductId2MegaType.ContainsKey(productId) || ProductId2MegaType[productId] != megaType) // 都是0则代表戴森球下直接发电。否则如果是物质生成模式且和巨构不符
                    {
                        _this.genPool[i].capacityCurrentTick = 0;
                        continue;
                    }
                    num += _this.genPool[i].EnergyCap_Gamma_Req(normalized.x, normalized.y, normalized.z, increase, eta);
                    flag = true;
                }
            }
            if (_this.dysonSphere == null && flag)
            {
                _this.dysonSphere = _this.factory.CheckOrCreateDysonSphere();
            }
            if (_this.dysonSphere != null)
            {
                _this.dysonSphere.energyReqCurrentTick += num;
            }

            return false;
        }

        /// <summary>
        /// 使得复制粘贴可以应用于物质解压器的原矿模式
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="objectId"></param>
        /// <param name="factory"></param>
        [HarmonyPatch(typeof(BuildingParameters), "PasteToFactoryObject")]
        [HarmonyPostfix]
        public static void BuildingParametersPastePostPatch(ref BuildingParameters __instance, int objectId, PlanetFactory factory, ref bool __result)
        {
            int num = -objectId;
            Player mainPlayer = factory.gameData.mainPlayer;
            EntityData[] entityPool = factory.entityPool;
            PowerSystem powerSystem = factory.powerSystem;
            if (objectId > 0 && entityPool[objectId].id == objectId)
            {
                int powerGenId = entityPool[objectId].powerGenId;
                if (powerGenId != 0 && __instance.type == BuildingType.Gamma && powerSystem.genPool[powerGenId].gamma)
                {
                    ItemProto itemProto4 = LDB.items.Select((int)entityPool[objectId].protoId);
                    if (itemProto4 != null)
                    {
                        if (__instance.mode0 > 0 && __instance.mode0 != powerSystem.genPool[powerGenId].productId && __instance.mode0 != itemProto4.prefabDesc.powerProductId && productHeat.ContainsKey(__instance.mode0))
                        {
                            //if (ReceiverPatchers.RRId2OreId.ContainsKey(itemProto4.ID) && __instance.mode0 == ReceiverPatchers.RRId2OreId[itemProto4.ID])
                            //{
                            //    factory.TakeBackItemsInEntity(mainPlayer, objectId);
                            //    powerSystem.genPool[powerGenId].productId = __instance.mode0;
                            //    __result = true;
                            //    return;
                            //}
                            factory.TakeBackItemsInEntity(mainPlayer, objectId);
                            powerSystem.genPool[powerGenId].productId = __instance.mode0;
                            powerSystem.genPool[powerGenId].productHeat = productHeat[__instance.mode0];
                            __result = true;
                            return;
                        }
                        // 此处可以限制mode0==0禁止复制到非原始接收器上，但是不写了，保留这种操作，目前未发现会产生bug
                    }
                }
            }
        }

        /// <summary>
        /// 为了让直接建造建筑的时候可以复制到原矿模式。此外，除了游戏自带的射线接受器之外的接收器，现在建造时默认为物质生成模式
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="entityId"></param>
        /// <param name="recipeId"></param>
        /// <param name="filterId"></param>
        /// <param name="parameters"></param>
        /// <param name="factory"></param>
        [HarmonyPatch(typeof(BuildingParameters), "ApplyPrebuildParametersToEntity")]
        [HarmonyPostfix]
        public static void ApplyPrebuildParametersToEntityPostPatch(int entityId, int[] parameters, PlanetFactory factory) // 此处很奇怪，我要是加上ref __instance以及其他原本方法需要的参数，就会在patch的时候报错。
        {
            GameHistoryData history = factory.gameData.history;
            EntityData[] entityPool = factory.entityPool;
            PowerSystem powerSystem = factory.powerSystem;
            if (entityId > 0 && entityPool[entityId].id == entityId)
            {
                ItemProto itemProto = LDB.items.Select((int)entityPool[entityId].protoId);
                PrefabDesc prefabDesc = null;
                if (itemProto != null)
                {
                    prefabDesc = itemProto.prefabDesc;
                }
                if (prefabDesc == null)
                {
                    return;
                }
                int powerGenId = entityPool[entityId].powerGenId;
                if (powerGenId != 0)
                {
                    if (powerSystem.genPool[powerGenId].gamma && parameters != null && parameters.Length >= 1)
                    {
                        if (parameters[0] > 0 && parameters[0] != powerSystem.genPool[powerGenId].productId && history.ItemUnlocked(parameters[0]))
                        {
                            powerSystem.genPool[powerGenId].productId = parameters[0];
                            powerSystem.genPool[powerGenId].productHeat = productHeat[parameters[0]];
                        }
                        //else if (parameters[0] == 0 && parameters[0] != powerSystem.genPool[powerGenId].productId && itemProto.ID != 2208) // 不同的
                        //{
                        //    powerSystem.genPool[powerGenId].productId = prefabDesc.powerProductId;
                        //}
                    }
                    else if (powerSystem.genPool[powerGenId].gamma && parameters == null) // 不从任何现有建筑Shift过来建造的时候（即建造一个全新的建筑时），prarmeters是null，让其自动设置为物质合成模式。
                    {
                        // 因为将要将所有新的接收器删除，功能由游戏原本的接收器替代，因此已弃用。
                        //powerSystem.genPool[powerGenId].productId = prefabDesc.powerProductId;
                    }
                }
            }
        }
    }
}
