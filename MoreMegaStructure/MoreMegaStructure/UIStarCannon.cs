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
        // 开火按钮相关
        public static GameObject fastTravelButtonObj = null;
        public static GameObject FireButton1Obj;
        public static GameObject FireButton2Obj;

        public static Button FireButton1;
        public static Button FireButton2;

        public static UIButton FireButton1UIButton;
        public static UIButton FireButton2UIButton;

        public static Text FireButton1Text;
        public static Text FireButton2Text;

        public static RectTransform cursorViewRect;
        public static RectTransform bgRect;

        // 巨构编辑面板顶部信息栏
        public static GameObject StarCannonStateUIObj;
        public static Text StarCannonStateText;
        //// 星图界面底部信息栏
        //public static GameObject StarCannonStateStarmapUIObj;
        //public static Text StarCannonStateStarmapText;


        static Color cannonDisableNormalColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        static Color cannonDisableOverColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        static Color cannonDisablePressedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        static Color cannonChargingNormalColor = new Color(0.42f, 0.2f, 0.2f, 1f);
        static Color cannonChargingOverColor = new Color(0.52f, 0.25f, 0.25f, 1f);
        static Color cannonChargingPressedColor = new Color(0.42f, 0.2f, 0.2f, 0.8f);
        static Color cannonReadyNormalColor = new Color(0f, 0.499f, 0.824f, 0.7f);
        static Color cannonReadyOverColor = new Color(0f, 0.499f, 0.824f, 1f);
        static Color cannonReadyPressedColor = new Color(0f, 0.499f, 0.824f, 0.5f);
        static Color cannonAimingNormalColor = new Color(0.973f, 0.359f, 0.170f, 1f);
        static Color cannonAimingOverColor = new Color(0.973f, 0.459f, 0.170f, 1f);
        static Color cannonAimingPressedColor = new Color(0.973f, 0.359f, 0.170f, 0.8f);
        static Color cannonFiringNormalColor = new Color(1f, 0.16f, 0.16f, 0.8f);
        static Color cannonFiringOverColor = new Color(1f, 0.16f, 0.16f, 1f);
        static Color cannonFiringPressedColor = new Color(1f, 0.16f, 0.16f, 0.5f);

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
            FireButton1Text.text = "恒星炮开火按钮文本".Translate();
            GameObject.DestroyImmediate(FireButton1Obj.GetComponent<Button>());
            FireButton1 = FireButton1Obj.AddComponent<Button>();
            FireButton1.interactable = true;
            FireButton1.onClick.AddListener(() => { StarCannon.OnFireButtonClick(); });

            FireButton1UIButton = FireButton1Obj.GetComponent<UIButton>();
            FireButton1UIButton.tips.tipTitle = "恒星炮开火标题".Translate();
            FireButton1UIButton.tips.tipText = "恒星炮开火描述".Translate();
            FireButton1UIButton.tips.delay = 0.1f;
            FireButton1UIButton.tips.width = 260;
            FireButton1UIButton.tips.offset = new Vector2(230, 0);

            
            GameObject oriTitleObj = GameObject.Find("UI Root/Overlay Canvas/Milky Way UI/milky-way-screen-ui/top-title");
            Transform parent3 = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/screen").transform;
            StarCannonStateUIObj = GameObject.Instantiate(oriTitleObj, parent3);
            StarCannonStateUIObj.name = "star-cannon-state-text";
            StarCannonStateUIObj.transform.localPosition = new Vector3(0, DSPGame.globalOption.uiLayoutHeight / 2 - 140, 0);

            StarCannonStateText = StarCannonStateUIObj.GetComponent<Text>();
            StarCannonStateText.supportRichText = true;
            GameObject oriStateTextObj = GameObject.Find("UI Root/Overlay Canvas/Milky Way UI/milky-way-screen-ui/statistics/desc-mask/desc/dyson-cnt-text");
            StarCannonStateText.material = oriStateTextObj.GetComponent<Text>().material;
            StarCannonStateText.color = new Color(1, 1, 1, 0.7f);
            StarCannonStateUIObj.SetActive(true);

            //Transform parent4 = GameObject.Find("UI Root/Overlay Canvas/In Game/Starmap UIs").transform;
            //StarCannonStateStarmapUIObj = GameObject.Instantiate(oriTitleObj, parent4);
            //StarCannonStateStarmapUIObj.name = "star-cannon-state-text";
            //StarCannonStateStarmapUIObj.transform.localPosition = new Vector3(0, 200, 0);

            //StarCannonStateStarmapText = StarCannonStateUIObj.GetComponent<Text>();
            //StarCannonStateStarmapText.supportRichText = true;
            //StarCannonStateStarmapText.material = oriStateTextObj.GetComponent<Text>().material;
            //StarCannonStateStarmapText.color = new Color(1, 1, 1, 0.7f);
            //StarCannonStateStarmapUIObj.SetActive(true);
        }

        public static void InitWhenLoad()
        { 
            
        }

        /// <summary>
        /// 打开星图模式时重新计算一次恒星炮的属性
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStarmap), "_OnOpen")]
        [HarmonyPatch(typeof(UIDysonEditor), "_OnOpen")]
        public static void UIStarmapOnOpenPostPatch()
        {
            StarCannon.starCannonStarIndex = MoreMegaStructure.GetStarCannonBuiltIndex();
            StarCannon.RefreshStarCannonProperties();
            //UIStarmapOnUpdatePostPatch_RefreshFireButton(ref __instance);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStarmap), "_OnUpdate")]
        public static void UIStarmapOnUpdatePostPatch_RefreshFireButton(ref UIStarmap __instance)
        {
            if(fastTravelButtonObj.activeSelf)
            {
                FireButton1Obj.transform.localPosition = new Vector3(1, (cursorViewRect.sizeDelta.y + bgRect.sizeDelta.y - 2) * (-0.5f) - 26, 0);
            }
            else
            {
                FireButton1Obj.transform.localPosition = new Vector3(1, (cursorViewRect.sizeDelta.y + bgRect.sizeDelta.y - 2) * (-0.5f), 0);
            }

            if (__instance.focusPlanet != null || __instance.focusStar != null)
            {
                FireButton1UIButton.tips.tipTitle = "恒星炮开火标题".Translate();
                FireButton1UIButton.tips.tipText = "恒星炮开火描述".Translate();
            }
            else if(__instance.focusHive != null || __instance.mouseHoverHive != null)
            {
                if ((int)StarCannon.state <= 0)
                {
                    FireButton1UIButton.tips.tipTitle = "恒星炮开火标题".Translate();
                    FireButton1UIButton.tips.tipText = "选中黑雾巢穴时的恒星炮开火描述".Translate();
                }
                else
                {
                    FireButton1UIButton.tips.tipTitle = "优先射击标题".Translate();
                    FireButton1UIButton.tips.tipText = "优先射击描述".Translate();
                }
            }
            else
            {
                FireButton1UIButton.tips.tipTitle = "恒星炮开火标题".Translate();
                FireButton1UIButton.tips.tipText = "恒星炮开火描述".Translate();
            }

            if (StarCannon.starCannonStarIndex < 0)
            {
                FireButton1Text.text = "恒星炮未规划按钮文本".Translate();
                FireButton1Obj.GetComponent<Image>().color = cannonDisableNormalColor;
                if (FireButton1UIButton.transitions?.Length > 0)
                {
                    for (int i = 0; i < 1; i++)
                    {
                        if (FireButton1UIButton.transitions[i] == null) continue;
                        FireButton1UIButton.transitions[i].normalColor = cannonDisableNormalColor;
                        FireButton1UIButton.transitions[i].mouseoverColor = cannonDisableOverColor;
                        FireButton1UIButton.transitions[i].pressedColor = cannonDisablePressedColor;
                    }
                }
            }
            else if(StarCannon.starCannonLevel <= 0)
            {
                FireButton1Text.text = "恒星炮建设中按钮文本".Translate();
                FireButton1Obj.GetComponent<Image>().color = cannonDisableNormalColor;
                if (FireButton1UIButton.transitions?.Length > 0)
                {
                    for (int i = 0; i < 1; i++)
                    {
                        if (FireButton1UIButton.transitions[i] == null) continue;
                        FireButton1UIButton.transitions[i].normalColor = cannonDisableNormalColor;
                        FireButton1UIButton.transitions[i].mouseoverColor = cannonDisableOverColor;
                        FireButton1UIButton.transitions[i].pressedColor = cannonDisablePressedColor;
                    }
                }
            }
            else if (StarCannon.state == EStarCannonState.Standby)
            {
                FireButton1Text.text = "恒星炮开火按钮文本".Translate();
                FireButton1Obj.GetComponent<Image>().color = cannonReadyNormalColor;
                if (FireButton1UIButton.transitions?.Length > 0)
                {
                    for (int i = 0; i < 1; i++)
                    {
                        if (FireButton1UIButton.transitions[i] == null) continue;
                        FireButton1UIButton.transitions[i].normalColor = cannonReadyNormalColor;
                        FireButton1UIButton.transitions[i].mouseoverColor = cannonReadyOverColor;
                        FireButton1UIButton.transitions[i].pressedColor = cannonReadyPressedColor;
                    }
                }
            }
            else if (StarCannon.state == EStarCannonState.Align)
            {
                FireButton1Text.text = "恒星炮正在瞄准按钮文本".Translate();
                FireButton1Obj.GetComponent<Image>().color = cannonAimingNormalColor;
                if (FireButton1UIButton.transitions?.Length > 0)
                {
                    for (int i = 0; i < 1; i++)
                    {
                        if (FireButton1UIButton.transitions[i] == null) continue;
                        FireButton1UIButton.transitions[i].normalColor = cannonAimingNormalColor;
                        FireButton1UIButton.transitions[i].mouseoverColor = cannonAimingOverColor;
                        FireButton1UIButton.transitions[i].pressedColor = cannonAimingPressedColor;
                    }
                }
            }
            else if (StarCannon.state == EStarCannonState.Heat)
            {
                FireButton1Text.text = "恒星炮预热中按钮文本".Translate();
                FireButton1Obj.GetComponent<Image>().color = cannonAimingNormalColor;
                if (FireButton1UIButton.transitions?.Length > 0)
                {
                    for (int i = 0; i < 1; i++)
                    {
                        if (FireButton1UIButton.transitions[i] == null) continue;
                        FireButton1UIButton.transitions[i].normalColor = cannonAimingNormalColor;
                        FireButton1UIButton.transitions[i].mouseoverColor = cannonAimingOverColor;
                        FireButton1UIButton.transitions[i].pressedColor = cannonAimingPressedColor;
                    }
                }
            }
            else if (StarCannon.state == EStarCannonState.Fire)
            {
                int timeLeft = StarCannon.maxFireDuration - (StarCannon.time - StarCannon.endAimTime - StarCannon.warmTimeNeed);
                FireButton1Text.text = "恒星炮开火中按钮文本".Translate() + String.Format(" {0:D2} : {1:D2}", timeLeft / 3600, timeLeft / 60 % 60);
                if (__instance.focusHive != null || __instance.mouseHoverHive != null && __instance.focusPlanet == null && __instance.focusStar == null)
                {
                    FireButton1Text.text = "优先射击按钮文本".Translate();
                }
                FireButton1Obj.GetComponent<Image>().color = cannonFiringNormalColor;
                if (FireButton1UIButton.transitions?.Length > 0)
                {
                    for (int i = 0; i < 1; i++)
                    {
                        if (FireButton1UIButton.transitions[i] == null) continue;
                        FireButton1UIButton.transitions[i].normalColor = cannonFiringNormalColor;
                        FireButton1UIButton.transitions[i].mouseoverColor = cannonFiringOverColor;
                        FireButton1UIButton.transitions[i].pressedColor = cannonFiringPressedColor;
                    }
                }
            }
            else if (StarCannon.state == EStarCannonState.Cooldown)
            {
                int timeLeft = -StarCannon.time - StarCannon.chargingTimeNeed;
                FireButton1Text.text = "恒星炮冷却中按钮文本".Translate() + String.Format(" {0:D2} : {1:D2}", timeLeft / 3600, timeLeft / 60 % 60);
                FireButton1Obj.GetComponent<Image>().color = cannonChargingNormalColor;
                if (FireButton1UIButton.transitions?.Length > 0)
                {
                    for (int i = 0; i < 1; i++)
                    {
                        if (FireButton1UIButton.transitions[i] == null) continue;
                        FireButton1UIButton.transitions[i].normalColor = cannonChargingNormalColor;
                        FireButton1UIButton.transitions[i].mouseoverColor = cannonChargingOverColor;
                        FireButton1UIButton.transitions[i].pressedColor = cannonChargingPressedColor;
                    }
                }
            }
            else if (StarCannon.state == EStarCannonState.Recharge)
            {
                int timeLeft = -StarCannon.time;
                FireButton1Text.text = "恒星炮充能中按钮文本".Translate() + String.Format(" {0:D2} : {1:D2}", timeLeft / 3600, timeLeft / 60 % 60);
                FireButton1Obj.GetComponent<Image>().color = cannonChargingNormalColor;
                if (FireButton1UIButton.transitions?.Length > 0)
                {
                    for (int i = 0; i < 1; i++)
                    {
                        if (FireButton1UIButton.transitions[i] == null) continue;
                        FireButton1UIButton.transitions[i].normalColor = cannonChargingNormalColor;
                        FireButton1UIButton.transitions[i].mouseoverColor = cannonChargingOverColor;
                        FireButton1UIButton.transitions[i].pressedColor = cannonChargingPressedColor;
                    }
                }
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStarmap), "UpdateCursorView")]
        public static void LimitCursorViewWidth(ref UIStarmap __instance)
        {
            int minWidth = StarCannon.starCannonStarIndex >= 0 && StarCannon.starCannonLevel < 1 && DSPGame.globalOption.languageLCID != 2052 ? 225 : 150; // 225是因为恒星炮正在建造中的英文过长，按钮需要更长才能放下文本。2052为中文LCID，不需要这么长。1033为英文。
            float width = Math.Max(__instance.cursorViewTrans.sizeDelta.x, minWidth);
            float height = __instance.cursorViewTrans.sizeDelta.y;
            __instance.cursorViewTrans.sizeDelta = new Vector2(width, height);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDysonEditor), "_OnUpdate")]
        [HarmonyPatch(typeof(UIDysonEditor), "_OnOpen")]
        public static void UIDysonEditorOnUpdatePostPatch_RefreshUI(ref UIDysonEditor __instance)
        {
            if (MoreMegaStructure.curDysonSphere!=null)
            {
                int starIndex = MoreMegaStructure.curDysonSphere.starData.index;
                if (starIndex > 1000)
                {
                    StarCannonStateUIObj.SetActive(false);
                    return;
                }
                if (MoreMegaStructure.StarMegaStructureType[starIndex] == 6)
                {
                    StarCannonStateUIObj.SetActive(true);
                    if(StarCannon.starCannonLevel <= 0)
                    {
                        StarCannonStateText.text = "恒星炮建设中按钮文本".Translate();
                        StarCannonStateText.color = new Color(0.7f, 0.7f, 0.7f, 0.84f);
                    }
                    else if (StarCannon.state == EStarCannonState.Standby)
                    {
                        StarCannonStateText.text = "恒星炮已就绪".Translate();
                        StarCannonStateText.color = new Color(0.233f, 0.78f, 1f, 0.754f);
                    }
                    else if (StarCannon.state == EStarCannonState.Align)
                    {
                        StarCannonStateText.text = "恒星炮正在瞄准".Translate();
                        StarCannonStateText.color = cannonAimingPressedColor;
                    }
                    else if (StarCannon.state == EStarCannonState.Heat)
                    {
                        StarCannonStateText.text = "恒星炮预热中".Translate();
                        StarCannonStateText.color = cannonAimingPressedColor;
                    }
                    else if (StarCannon.state == EStarCannonState.Fire)
                    {
                        int timeLeft = StarCannon.maxFireDuration - (StarCannon.time - StarCannon.endAimTime - StarCannon.warmTimeNeed);
                        StarCannonStateText.text = "恒星炮开火中".Translate() + String.Format("  {0:D2} : {1:D2}", timeLeft / 3600, timeLeft / 60 % 60);
                        StarCannonStateText.color = cannonFiringNormalColor;
                    }
                    else if (StarCannon.state == EStarCannonState.Cooldown)
                    {
                        int timeLeft = -StarCannon.time - StarCannon.chargingTimeNeed;
                        StarCannonStateText.text = "恒星炮冷却中".Translate() + String.Format("  {0:D2} : {1:D2}", timeLeft / 3600, timeLeft / 60 % 60);
                        StarCannonStateText.color = new Color(0.6f, 0.6f, 0.9f, 0.9f);
                    }
                    else if (StarCannon.state == EStarCannonState.Recharge)
                    {
                        int timeLeft = -StarCannon.time;
                        StarCannonStateText.text = "恒星炮充能中".Translate() + String.Format("  {0:D2} : {1:D2}", timeLeft / 3600, timeLeft / 60 % 60);
                        StarCannonStateText.color = new Color(0.6f, 0.6f, 0.9f, 0.9f);
                    }
                }
                else
                {
                    StarCannonStateUIObj.SetActive(false);
                }
            }
            else
            {
                StarCannonStateUIObj.SetActive(false);
            }
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

        

        /// <summary>
        /// 为了星图模式下底部字幕显示恒星炮状态
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIGeneralTips), "_OnUpdate")]
        public static void UIGeneralTipsOnUpdatePostPatch(ref UIGeneralTips __instance)
        {
            var _this = __instance;
            if (UIGame.viewMode == EViewMode.Starmap && UIGame.viewModeReady)
            {
                _this.modeText.gameObject.SetActive(true);
                _this.modeText.rectTransform.anchoredPosition = new Vector2(0f, 0f);
                if (StarCannon.starCannonStarIndex < 0)
                    _this.modeText.text = "星图".Translate() + "\n" + "恒星炮尚未规划".Translate();
                else if (StarCannon.starCannonLevel < 1)
                    _this.modeText.text = "星图".Translate() + "\n" + "恒星炮建设中按钮文本".Translate();
                else if (StarCannon.state == EStarCannonState.Standby)
                    _this.modeText.text = "星图".Translate() + "\n" + "恒星炮已就绪".Translate();
                else if (StarCannon.state == EStarCannonState.Align)
                    _this.modeText.text = "星图".Translate() + "\n" + "恒星炮正在瞄准".Translate();
                else if (StarCannon.state == EStarCannonState.Heat)
                    _this.modeText.text = "星图".Translate() + "\n" + "恒星炮预热中".Translate();
                else if (StarCannon.state == EStarCannonState.Fire)
                {
                    int timeLeft = StarCannon.maxFireDuration - (StarCannon.time - StarCannon.endAimTime - StarCannon.warmTimeNeed);
                    _this.modeText.text = "星图".Translate() + "\n" + "恒星炮开火中".Translate() + String.Format("  {0:D2} : {1:D2}", timeLeft / 3600, timeLeft / 60 % 60);
                }
                else if (StarCannon.state == EStarCannonState.Cooldown)
                {
                    int timeLeft = -StarCannon.time - StarCannon.chargingTimeNeed;
                    _this.modeText.text = "星图".Translate() + "\n" + "恒星炮冷却中".Translate() + String.Format("  {0:D2} : {1:D2}", timeLeft / 3600, timeLeft / 60 % 60);
                }
                else if (StarCannon.state == EStarCannonState.Recharge)
                {
                    int timeLeft = -StarCannon.time;
                    _this.modeText.text = "星图".Translate() + "\n" + "恒星炮充能中".Translate() + String.Format("  {0:D2} : {1:D2}", timeLeft / 3600, timeLeft / 60 % 60);
                }
            }
        }

    }
}
