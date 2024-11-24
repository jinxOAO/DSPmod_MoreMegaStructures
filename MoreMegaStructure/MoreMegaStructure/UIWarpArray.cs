using HarmonyLib;
using NGPT;
using rail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MoreMegaStructure
{
    public class UIWarpArray
    {
        public static GameObject oriCircle = null;
        public static List<GameObject> arrayCircles;
        public static Transform circleParent = null;
        public static UIStarmap UIStarmap = null;
        public static float circleColorR = 0.399f;
        public static float circleColorG = 0.902f;
        public static float circleColorB = 0.555f;
        public static float circleColorA = 0.315f;
        public static Color warpCircleColor = new Color(circleColorR, circleColorG, circleColorB, circleColorA);

        public static GameObject showHideButtonObj = null;
        public static UIButton showHideUIBtn;
        public static bool showWarpFieldCircle = true;

        public static void InitAll()
        {
            if (oriCircle == null)
            {
                GameObject oriOriCircle = GameObject.Find("UI Root/Overlay Canvas/In Game/Starmap UIs/starmap-screen-ui/cursor-view/functions/func-deco-mask/func-deco-1");
                oriCircle = GameObject.Instantiate(oriOriCircle);
                oriCircle.GetComponent<Image>().color = warpCircleColor;
                GameObject.Destroy(oriCircle.GetComponent<TweenEulerAngles>());
                GameObject starmapUIObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Starmap UIs");
                GameObject warpCirclesObj = new GameObject();
                warpCirclesObj.name = "warp-field-circles";
                warpCirclesObj.transform.SetParent(starmapUIObj.transform);
                warpCirclesObj.transform.localScale = Vector3.one;
                warpCirclesObj.transform.localPosition = Vector3.zero;
                warpCirclesObj.SetActive(true);
                // circleParent = starmapUIObj.transform;
                circleParent = warpCirclesObj.transform;
            }
            if (showHideButtonObj == null)
            {
                GameObject oriButtonObj1 = GameObject.Find("UI Root/Overlay Canvas/In Game/Starmap UIs/starmap-screen-ui/btn-1");
                GameObject oriButtonObj2 = GameObject.Find("UI Root/Overlay Canvas/In Game/Starmap UIs/starmap-screen-ui/btn-2");
                GameObject oriButtonObj3 = GameObject.Find("UI Root/Overlay Canvas/In Game/Starmap UIs/starmap-screen-ui/btn-3");
                GameObject parentObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Starmap UIs/starmap-screen-ui");
                if (oriButtonObj2 != null && oriButtonObj3 != null && parentObj != null)
                {
                    showHideButtonObj = GameObject.Instantiate(oriButtonObj3, parentObj.transform);
                    showHideButtonObj.name = "btn-4";
                    float x = oriButtonObj3.transform.localPosition.x;
                    float y = oriButtonObj3.transform.localPosition.y + (oriButtonObj3.transform.localPosition.y - oriButtonObj2.transform.localPosition.y);
                    showHideButtonObj.transform.localPosition = new Vector3(x, y, 0);
                    showHideButtonObj.transform.localScale = Vector3.one;
                    GameObject.DestroyImmediate(showHideButtonObj.GetComponent<UIButton>());
                    GameObject.DestroyImmediate(showHideButtonObj.GetComponent<Button>());
                    showHideButtonObj.AddComponent<Button>();
                    showHideUIBtn = showHideButtonObj.AddComponent<UIButton>();
                    showHideUIBtn.tips.tipTitle = "折跃场范围显示".Translate();
                    showHideUIBtn.tips.tipText = "折跃场范围显示描述".Translate();
                    showHideUIBtn.tips.corner = oriButtonObj3.GetComponent<UIButton>().tips.corner;
                    showHideUIBtn.tips.offset = oriButtonObj3.GetComponent<UIButton>().tips.offset;
                    showHideUIBtn.tips.delay = oriButtonObj3.GetComponent<UIButton>().tips.delay;
                    showHideUIBtn.tips.width = oriButtonObj3.GetComponent<UIButton>().tips.width;
                    UIButton oriUIBtn = oriButtonObj1.GetComponent<UIButton>();
                    showHideUIBtn.transitions = new UIButton.Transition[oriUIBtn.transitions.Length];
                    for (int i = 0; i < showHideUIBtn.transitions.Length; i++)
                    {
                        showHideUIBtn.transitions[i] = new UIButton.Transition();
                        showHideUIBtn.transitions[i].damp = oriUIBtn.transitions[i].damp;
                        showHideUIBtn.transitions[i].mouseoverSize = oriUIBtn.transitions[i].mouseoverSize;
                        showHideUIBtn.transitions[i].pressedSize = oriUIBtn.transitions[i].pressedSize;
                        showHideUIBtn.transitions[i].normalColor = oriUIBtn.transitions[i].normalColor;
                        showHideUIBtn.transitions[i].mouseoverColor = oriUIBtn.transitions[i].mouseoverColor;
                        showHideUIBtn.transitions[i].pressedColor = oriUIBtn.transitions[i].pressedColor;
                        showHideUIBtn.transitions[i].disabledColor = oriUIBtn.transitions[i].disabledColor;
                        showHideUIBtn.transitions[i].alphaOnly = oriUIBtn.transitions[i].alphaOnly;
                        showHideUIBtn.transitions[i].highlightSizeMultiplier = oriUIBtn.transitions[i].highlightSizeMultiplier;
                        showHideUIBtn.transitions[i].highlightColorMultiplier = oriUIBtn.transitions[i].highlightColorMultiplier;
                        showHideUIBtn.transitions[i].highlightAlphaMultiplier = oriUIBtn.transitions[i].highlightAlphaMultiplier;
                        showHideUIBtn.transitions[i].highlightColorOverride = oriUIBtn.transitions[i].highlightColorOverride;
                        if (i == 0)
                            showHideUIBtn.transitions[i].target = showHideButtonObj.GetComponent<Graphic>();
                        else if (i == 1)
                            showHideUIBtn.transitions[i].target = showHideButtonObj.transform.Find("icon").GetComponent<Graphic>();
                        Debug.Log($"----\n\n\n\n\n{showHideButtonObj.transform.Find("icon").GetComponent<Graphic>() != null}");
                    }
                    showHideButtonObj.GetComponent<Button>().onClick.RemoveAllListeners();
                    showHideButtonObj.GetComponent<Button>().onClick.AddListener(() => { ShowHideWarpFieldCircle(); });



                    showHideButtonObj.transform.Find("icon").GetComponent<Image>().sprite = Resources.Load<Sprite>("ui/textures/sprites/starmap/target");
                }
            }
            if (arrayCircles == null)
            {
                arrayCircles = new List<GameObject>();
            }
        }



        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStarmap), "_OnOpen")]
        public static void RefreshAndShowAll(ref UIStarmap __instance)
        {
            UIStarmap = __instance;

            int warpArrayCount = WarpArray.arrays.Count;
            int circleCount = arrayCircles.Count;
            for (int i = 0; i < warpArrayCount && i < circleCount; i++)
            {
                GameObject circle = GameObject.Instantiate(oriCircle);
                circle.SetActive(showWarpFieldCircle);
            }
            if(warpArrayCount > circleCount)
            {
                for (int i = 0;i < warpArrayCount - circleCount; i++)
                {
                    GameObject circle = GameObject.Instantiate(oriCircle);
                    circle.name = "warp-field-circle";
                    circle.transform.localScale = Vector3.one;
                    circle.transform.SetParent(circleParent);
                    circle.SetActive(showWarpFieldCircle);
                    arrayCircles.Add(circle);
                }
            }
            else if (warpArrayCount < circleCount)
            {
                for (int i = warpArrayCount; i < circleCount; i++)
                {
                    arrayCircles[i].SetActive(false);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStarmap), "_OnClose")]
        public static void HideAll()
        {

        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStarmap), "_OnUpdate")]
        public static void RefreshUI(ref UIStarmap __instance)
        {
            showHideUIBtn.highlighted = showWarpFieldCircle;

            if (!showWarpFieldCircle)
            {
                return;
            }

            Camera camera = __instance.screenCamera;
            for (int i = 0; i < WarpArray.arrays.Count; i++)
            {
                if (i < arrayCircles.Count)
                {
                    GameObject circle = arrayCircles[i];
                    int starIndex = WarpArray.arrays[i].starIndex;
                    if(__instance.starUIs.Length > starIndex)
                    {
                        Vector2 screenPos;
                        __instance.WorldPointIntoScreen(__instance.starUIs[starIndex].starObject.vpos, out screenPos);
                        // circle.GetComponent<RectTransform>().anchoredPosition = screenPos;
                        circle.transform.localPosition = screenPos;
                        //circle.transform.localScale = Vector3.one;

                        // 算圈圈大小
                        // 获取一个与摄像机方向垂直的向量，长度为折跃场半径（换算到屏幕的星球距离还要乘0.00025），然后转换为屏幕坐标
                        double radius = WarpArray.arrays[i].radius;
                        Vector3 radiusPos = Utils.GetVertical(camera.transform.forward) * (float)radius * 0.00025f + __instance.starUIs[starIndex].starObject.vpos;
                        Vector2 edgeScreenPos;
                        __instance.WorldPointIntoScreen(radiusPos, out edgeScreenPos);

                        // 该坐标在屏幕上距离中心的距离就是下面，也就是宇宙中这个半径在星图屏幕中的像素长度
                        float radiusInScreen = (edgeScreenPos - screenPos).magnitude;
                        // 设置图片scale（图片半径）
                        float sizeOri = circle.GetComponent<RectTransform>().sizeDelta.x / 2 / 1.0185f;
                        if (sizeOri == 0)
                            sizeOri = 1;
                        float scale = radiusInScreen / sizeOri;
                        circle.transform.localScale = new Vector3 (scale, scale, scale);
                        float rotateSpeed = 2 / ((float)radius / 40000 / 60);
                        if (rotateSpeed < 0.1f)
                            rotateSpeed = 0.1f;
                        if(rotateSpeed > 2)
                            rotateSpeed = 2;
                        circle.transform.localRotation = circle.transform.localRotation * Quaternion.AngleAxis(rotateSpeed, new Vector3(0, 0, 1));

                        // 船通过变亮效果，已弃用
                        //float lighterFactor = 20.0f * WarpArray.arrays[i].activeCountThisFrame / (WarpArray.arrays[i].activeCountThisFrame + 10) + 1.0f;
                        //circle.GetComponent<Image>().color = new Color(circleColorR, circleColorG, circleColorB, circleColorA * lighterFactor);
                    }
                }
            }

            // WarpArray.UpdateStarmapActiveCount();
        }

        /*  笔记
         *  Vector3 vector2 = (this.hive.sector.astros[this.hive.hiveAstroId - 1000000].uPos - this.starmap.viewTargetUPos) * 0.00025; 注意距离乘0.00025
	        Vector2 zero = Vector2.zero;
	        bool flag = !UIStarmap.isChangingToMilkyWay;
	        if (flag)
	        {
		        flag = this.starmap.WorldPointIntoScreen(vector2, out zero);
		        this.projectedCoord = new Vector2(Mathf.Round(zero.x), Mathf.Round(zero.y));
	        }

        此外，圆圈图片的边缘达到1080像素时，实际的sizedelta达到1100（是1.0185倍）
         */

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStarmap), "UpdateCursorView")]
        public static void UIStarmapUpdateCursorViewPatch(ref UIStarmap __instance)
        {
            int curStarIndex = -1;
            if (__instance.mouseHoverPlanet != null)
            {
                curStarIndex = __instance.mouseHoverPlanet.planet.star.index;
            }
            if (__instance.mouseHoverStar != null)
            {
                curStarIndex = __instance.mouseHoverStar.star.index;
            }
            if (__instance.focusPlanet != null)
            {
                curStarIndex = __instance.focusPlanet.planet.star.index;
            }
            if (__instance.focusStar != null)
            {
                curStarIndex = __instance.focusStar.star.index;
            }

            if (curStarIndex < 0)
                return;

            if(curStarIndex >= 0 && curStarIndex < WarpArray.starIsInWhichWarpArray.Length)
            {
                if (__instance.cursorViewText.text.Length > 5 && __instance.cursorViewText.text[__instance.cursorViewText.text.Length - 1] != ' ')
                {
                    if (WarpArray.starIsInWhichWarpArray[curStarIndex] >= 0)
                    {
                        int listIndex = WarpArray.starIsInWhichWarpArray[curStarIndex];
                        int warpFieldStarIndex = WarpArray.arrays[WarpArray.starIsInWhichWarpArray[curStarIndex]].starIndex;
                        double energyConsumptionReduction = 1.0 - WarpArray.tripEnergyCostRatioByStarIndex[curStarIndex];
                        __instance.cursorViewText.text += "\n" + string.Format("折跃场已覆盖信息".Translate(), GameMain.galaxy.StarById(warpFieldStarIndex + 1).displayName, energyConsumptionReduction * 100);
                    }
                    else
                    {
                        __instance.cursorViewText.text += "\n" + string.Format("折跃场未覆盖信息".Translate());
                    }
                }
            }

            __instance.cursorViewTrans.sizeDelta = new Vector2(__instance.cursorViewText.preferredWidth * 0.5f + 44f, __instance.cursorViewText.preferredHeight * 0.5f + 14f);
            __instance.cursorRightDeco.sizeDelta = new Vector2(__instance.cursorViewTrans.sizeDelta.y - 12f, 5f);

        }

        public static void ShowHideWarpFieldCircle()
        {
            showWarpFieldCircle = !showWarpFieldCircle;
            if(showWarpFieldCircle)
            {
                int warpArrayCount = WarpArray.arrays.Count;
                for (int i = 0; i < warpArrayCount && i < arrayCircles.Count ; i++)
                {
                    arrayCircles[i].SetActive(true);
                }
            }
            else
            {
                for (int i = 0;i < arrayCircles.Count; i++)
                {
                    arrayCircles[i].SetActive(false);
                }

            }
        }


        public static void Import(BinaryReader r)
        {
            InitAll();
        }

        public static void Export(BinaryWriter w)
        {

        }

        public static void IntoOtherSave()
        {
            InitAll();

        }
    }
}
