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


        public static void InitAll()
        {
            if(oriCircle == null)
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
            if (arrayCircles == null)
            {
                arrayCircles = new List<GameObject>();
            }
            else
            {
                for (int i = 0; i < arrayCircles.Count; i++)
                {
                    if (arrayCircles[i] != null)
                        GameObject.Destroy(arrayCircles[i]);
                }
                arrayCircles.Clear();
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
                circle.SetActive(true);
            }
            if(warpArrayCount > circleCount)
            {
                for (int i = 0;i < warpArrayCount - circleCount; i++)
                {
                    GameObject circle = GameObject.Instantiate(oriCircle);
                    circle.name = "warp-field-circle";
                    circle.transform.localScale = Vector3.one;
                    circle.transform.SetParent(circleParent);
                    circle.SetActive(true);
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

                        circle.transform.localRotation = circle.transform.localRotation * Quaternion.AngleAxis(2.0f / ((float)radius / 40000 / 60), new Vector3(0, 0, 1));

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
