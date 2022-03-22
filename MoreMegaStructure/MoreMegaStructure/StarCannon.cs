using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using System.IO;
using UnityEngine.UI;
using CommonAPI.Systems;

namespace MoreMegaStructure
{
    class StarCannon
    {

		public static List<long> energyPerTickRequiredByLevel = new List<long> { 0, 100000000, 500000000, 1000000000, 2000000000, 3000000000, 3000000000 };
		public static List<int> basicDamagePerTickByLevel = new List<int> { 0, 500, 1000, 1500, 2000, 700 }; //五级前，伤害是固定的，五级后，伤害是基础伤害+bonus
		public static float bonusDpsPerMW = 0.6f; //5级后，每1MW的能量提供这么多的秒伤。每tick提供的tick伤害也是这个比值
		public static List<int> maxAimCountByLevel = new List<int> { 0, 2, 3, 5, 10, 9999 }; //连续瞄准次数上限
		public static List<int> chargeTickByLevel = new List<int> { 0, 270000, 162000, 72000, 36000, 18000 }; //充能时间，tick
		public static List<int> fireRangeByLevel = new List<int> { 0, 5, 10, 20, 9999, 9999 }; //射程，以光年计

        public static int[] GetStarCannonProperties(DysonSphere sphere)
        {
            if (sphere == null)
                return new int[] { -1, -1, -1, -1, -1, -1 };

			long cannonEnergy = sphere.energyGenCurrentTick - sphere.energyReqCurrentTick;

			int level = 0;
            for (int i = 0; i < 6; i++)
            {
				if (cannonEnergy >= energyPerTickRequiredByLevel[i])
					level = i;
				else
					break;
            }
			int damagePerTick = basicDamagePerTickByLevel[level];
			if(level >= 5)
            {
				damagePerTick += (int)(cannonEnergy * 1.0 / 1000000.0 * bonusDpsPerMW);
            }
			return new int[] { level, damagePerTick, maxAimCountByLevel[level], chargeTickByLevel[level], fireRangeByLevel[level] };
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDEPowerDesc), "_OnUpdate")]
        public static void UIDEPowerDescUpdateUIPatch(ref UIDEPowerDesc __instance)
        {
            if (MoreMegaStructure.curDysonSphere == null)
				return;

			if (MoreMegaStructure.StarMegaStructureType[MoreMegaStructure.curDysonSphere.starData.index] != 6)
				return;

			int[] curDatas = GetStarCannonProperties(MoreMegaStructure.curDysonSphere);

			//请求、自由组件和锚定结构 -> 当前效率、下一阶段效率需求、充能耗时
			DysonSphere sphere = MoreMegaStructure.curDysonSphere;
			int curDPS = curDatas[1] * 60;
			long curEnergyPerSec = (sphere.energyGenCurrentTick - sphere.energyReqCurrentTick) * 60L;
			long nextLevelEnergyRequire = energyPerTickRequiredByLevel[curDatas[0]+1] * 60L;
			int chargingTimeNeed = curDatas[3] / 3600; //以分钟计
			
			__instance.valueText1.text = MoreMegaStructure.Capacity2Str(curEnergyPerSec) + "W";
			__instance.valueText2.text = MoreMegaStructure.Capacity2Str(nextLevelEnergyRequire) + "W";
			__instance.valueText3.text = chargingTimeNeed.ToString() + " min";
			double processToNextLevel = (double)curEnergyPerSec / ((double)nextLevelEnergyRequire + 0.01f );
			if (processToNextLevel<=1)
			{
				__instance.consCircle.fillAmount = (float)processToNextLevel;
				__instance.swarmCircle.fillAmount = 1f;
				__instance.layerCircle.fillAmount = 0f;
				__instance.layerCircle.transform.eulerAngles = new Vector3(0f, 0f, -360.0f);
				__instance.supplyRatioText.text = (processToNextLevel * 100f).ToString("0.0") + " %";
				return;
			}
			if (processToNextLevel > 1)
			{
				__instance.consCircle.fillAmount = 1f;
				__instance.swarmCircle.fillAmount = 1f;
				__instance.layerCircle.fillAmount = 0f;
				__instance.layerCircle.transform.eulerAngles = new Vector3(0f, 0f, -360.0f);
				__instance.supplyRatioText.text = "100 %";
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIDESphereInfo), "_OnUpdate")]
		public static void UIDESphereInfoOnUpdatePatch(ref UIDESphereInfo __instance)
        {
			if(MoreMegaStructure.curDysonSphere == null)
			{
				//MoreMegaStructure.curDysonSphere = (DysonSphere)Traverse.Create(__instance).Field("dysonSphere").GetValue();
				//if (MoreMegaStructure.curDysonSphere != null)
				//	MoreMegaStructure.curStar = MoreMegaStructure.curDysonSphere.starData;
			}
			if (MoreMegaStructure.curDysonSphere!=null && MoreMegaStructure.StarMegaStructureType[MoreMegaStructure.curDysonSphere.starData.index] == 6)
            {
				int[] curData = GetStarCannonProperties(MoreMegaStructure.curDysonSphere);
				__instance.sailCntText.text = curData[2] < 9000 ? curData[2].ToString() : "无限制gm".Translate();
				__instance.nodeCntText.text = curData[4] < 9000 ? curData[4].ToString() + " ly" : "无限制gm".Translate();
            }
        }



	}
}
