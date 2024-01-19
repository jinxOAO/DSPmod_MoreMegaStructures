using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MoreMegaStructure
{
    public static class UIStarCannon
    {
        public static GameObject fastTravelButtonObj = null;
        public static GameObject FireButton1Obj;
        public static GameObject FireButton2Obj;

        public static Button FireButton1;
        public static Button FireButton2;

        public static Text FireButton1Text;
        public static Text FireButton2Text;

        public static RectTransform cursorViewRect;
        public static RectTransform bgRect;

        public static void InitAll()
        {
            InitUI();
            InitWhenLoad();
        }

        public static void InitUI()
        {
            if (fastTravelButtonObj != null) return;

            fastTravelButtonObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Starmap UIs/starmap-screen-ui/cursor-view/bg/fasttravel-btn");
            Transform parent1 = GameObject.Find("UI Root/Overlay Canvas/In Game/Starmap UIs/starmap-screen-ui/cursor-view/bg").transform;
            Transform parent2 = GameObject.Find("UI Root/Overlay Canvas/In Game/Planet & Star Details/star-detail-ui").transform;

            cursorViewRect = GameObject.Find("UI Root/Overlay Canvas/In Game/Starmap UIs/starmap-screen-ui/cursor-view").GetComponent<RectTransform>();
            bgRect = GameObject.Find("UI Root/Overlay Canvas/In Game/Starmap UIs/starmap-screen-ui/cursor-view/bg").GetComponent <RectTransform>();

            FireButton1Obj = GameObject.Instantiate(fastTravelButtonObj, parent1);
            FireButton1Obj.SetActive(true);
            FireButton1Obj.name = "fire-star-cannon"; //名字
            FireButton1Obj.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
            //FireButton1Obj.transform.localPosition = new Vector3(30, -95, 0); // 位置
            //FireButton1Obj.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 28); // 按钮大小
            FireButton1Text = FireButton1Obj.transform.Find("button-text").GetComponent<Text>();
            FireButton1Text.text = "恒星炮开火".Translate();
            GameObject.DestroyImmediate(FireButton1Obj.GetComponent<Button>());
            FireButton1 = FireButton1Obj.AddComponent<Button>();
            FireButton1.interactable = true;
            FireButton1.onClick.AddListener(() => { StarCannon.OnFireButtonClick(); });
            FireButton1Obj.GetComponent<UIButton>().tips.tipTitle = "恒星炮开火标题".Translate();
            FireButton1Obj.GetComponent<UIButton>().tips.tipText = "恒星炮开火描述".Translate();
            FireButton1Obj.GetComponent<UIButton>().tips.delay = 0.3f;
            FireButton1Obj.GetComponent<UIButton>().tips.width = 260;
            FireButton1Obj.GetComponent<UIButton>().tips.offset = new Vector2(130, 50);

        }

        public static void InitWhenLoad()
        { 
            
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStarmap), "_OnUpdate")]
        public static void UIStarmapOnUpdatePostPatch(ref UIStarmap __instance)
        {
            if(fastTravelButtonObj.activeSelf)
            {
                FireButton1Obj.transform.localPosition = new Vector3(1, (cursorViewRect.sizeDelta.y + bgRect.sizeDelta.y - 2) * (-0.5f) - 26, 0);
            }
            else
            {
                FireButton1Obj.transform.localPosition = new Vector3(1, (cursorViewRect.sizeDelta.y + bgRect.sizeDelta.y - 2) * (-0.5f), 0);
            }


            FireButton1Text.text = "恒星炮开火".Translate();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDEPowerDesc), "_OnUpdate")]
        public static void UIDEPowerDescUpdateUIPatch(ref UIDEPowerDesc __instance)
        {
            if (MoreMegaStructure.curDysonSphere == null)
                return;

            if (MoreMegaStructure.StarMegaStructureType[MoreMegaStructure.curDysonSphere.starData.index] != 6)
                return;

            int[] curDatas = StarCannon.GetStarCannonProperties(MoreMegaStructure.curDysonSphere);

            //请求、自由组件和锚定结构 -> 当前效率、下一阶段效率需求、充能耗时
            DysonSphere sphere = MoreMegaStructure.curDysonSphere;
            int curDPS = curDatas[1] * 60;
            long curEnergyPerSec = (sphere.energyGenCurrentTick - sphere.energyReqCurrentTick) * 60L;
            if (curEnergyPerSec < 0)
            {
                curEnergyPerSec = 0;
                MoreMegaStructure.SpConsumePowText.text = "请拆除接收站".Translate();
            }
            long nextLevelEnergyRequire =  StarCannon.energyPerTickRequiredByLevel[curDatas[0] + 1] * 60L;
            int chargingTimeNeed = curDatas[3] / 3600; //以分钟计

            __instance.valueText1.text = MoreMegaStructure.Capacity2Str(curEnergyPerSec) + "W";
            __instance.valueText2.text = MoreMegaStructure.Capacity2Str(nextLevelEnergyRequire) + "W";
            __instance.valueText3.text = chargingTimeNeed.ToString() + " min";
            double processToNextLevel = (double)curEnergyPerSec / ((double)nextLevelEnergyRequire + 0.01f);
            if (processToNextLevel <= 1)
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
            if (MoreMegaStructure.curDysonSphere == null)
            {
                //MoreMegaStructure.curDysonSphere = (DysonSphere)Traverse.Create(__instance).Field("dysonSphere").GetValue();
                //if (MoreMegaStructure.curDysonSphere != null)
                //	MoreMegaStructure.curStar = MoreMegaStructure.curDysonSphere.starData;
            }
            if (MoreMegaStructure.curDysonSphere != null && MoreMegaStructure.StarMegaStructureType[MoreMegaStructure.curDysonSphere.starData.index] == 6)
            {
                int[] curData = StarCannon.GetStarCannonProperties(MoreMegaStructure.curDysonSphere);
                __instance.sailCntText.text = curData[2] < 9000 ? curData[2].ToString() : "无限制gm".Translate();
                __instance.nodeCntText.text = "-" + curData[5].ToString() + "% / ly";
            }
        }

    }
}
