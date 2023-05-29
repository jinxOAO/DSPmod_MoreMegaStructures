using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace MoreMegaStructure
{
    public class ReceiverPatchers
    {
        public static Dictionary<int,int> productId2MegaType = new Dictionary<int,int>();

        public static void InitRawData()
        {
            productId2MegaType.Add(0, 0);
            productId2MegaType.Add(1208, 0);

            productId2MegaType.Add(1101, 1);
            productId2MegaType.Add(1104, 1);
            productId2MegaType.Add(1105, 1);
            productId2MegaType.Add(1106, 1);
            productId2MegaType.Add(1109, 1);
            productId2MegaType.Add(1016, 1);

            productId2MegaType.Add(1014, 5);
            productId2MegaType.Add(1126, 5);
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
                    else if (!productId2MegaType.ContainsKey(productId) || productId2MegaType[productId] != megaType) // 都是0则代表戴森球下直接发电。否则如果是物质生成模式且和巨构不符
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


        // 这里不用写Patch了，因为RequestDysonSpherePowerPrePatch这个patch在发现锅盖和巨构种类不符时，设置了其capacityCurrentTick为0，而后续的发电和物质生成的量都取决于capacityCurrentTick，自然地就是无法工作的状态
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(PowerGeneratorComponent), "EnergyCap_Gamma")]
        //public static void EnergyCap_Gamma_PostPatch(ref PowerGeneratorComponent __instance, ref long __result)
        //{

        //}

    }
}
