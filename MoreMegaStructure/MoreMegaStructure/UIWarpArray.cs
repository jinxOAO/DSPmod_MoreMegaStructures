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

namespace MoreMegaStructure
{
    public class UIWarpArray
    {
        public static GameObject oriCircle = null;
        public static List<GameObject> arrayCircles;
        public static Transform circleParent = null;
        public static UIStarmap UIStarmap = null;

        public static void InitAll()
        {
            if(oriCircle == null)
            {
                GameObject oriOriCircle = GameObject.Find("UI Root/Overlay Canvas/In Game/Starmap UIs/starmap-screen-ui/cursor-view/functions/func-deco-mask/func-deco-1");
                oriCircle = GameObject.Instantiate(oriOriCircle);
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
            Camera camera = __instance.screenCamera;
            if(camera == null) { Debug.Log("null camera"); }
            for (int i = 0; i < arrayCircles.Count; i++)
            {
                if (arrayCircles[i] != null)
                    GameObject.Destroy(arrayCircles[i]);
            }
            arrayCircles.Clear();
            for (int i = 0; i < WarpArray.warpArrays.Count; i++)
            {
                GameObject circle = GameObject.Instantiate(oriCircle);
                circle.name = "warp-field-circle";
                circle.transform.localScale = Vector3.one;
                circle.transform.SetParent(circleParent);
                circle.SetActive(true);
                arrayCircles.Add(circle);
            }
            UIStarmap = __instance;
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
            for (int i = 0; i < WarpArray.warpArrays.Count; i++)
            {
                if (i < arrayCircles.Count)
                {
                    GameObject circle = arrayCircles[i];
                    int starIndex = WarpArray.warpArrays[i].starIndex;
                    if(__instance.starUIs.Length > starIndex)
                    {
                        Vector2 screenPos;
                        __instance.WorldPointIntoScreen(__instance.starUIs[starIndex].starObject.vpos, out screenPos);
                        // circle.GetComponent<RectTransform>().anchoredPosition = screenPos;
                        circle.transform.localPosition = screenPos;
                        //circle.transform.localScale = Vector3.one;

                        // 算圈圈大小
                        // 获取一个与摄像机方向垂直的向量，长度为折跃场半径（换算到屏幕的星球距离还要乘0.00025），然后转换为屏幕坐标
                        double radius = WarpArray.warpArrays[i].radius;
                        Vector3 radiusPos = Utils.GetVertical(camera.transform.forward) * (float)radius * 0.00025f;
                        Vector2 radiusV2;
                        __instance.WorldPointIntoScreen(radiusPos, out radiusV2);

                        // 该坐标在屏幕上距离中心的距离就是下面，也就是宇宙中这个半径在星图屏幕中的像素长度
                        float radiusInScreen = radiusV2.magnitude;
                        // 设置图片scale（图片半径）
                        float sizeOri = circle.GetComponent<RectTransform>().sizeDelta.x / 2 / 1.0185f;
                        if (sizeOri == 0)
                            sizeOri = 1;
                        float scale = radiusInScreen / sizeOri * 1.15f; // 再变大一点，更符合船的曲速启停位置
                        circle.transform.localScale = new Vector3 (scale, scale, scale);
                    }
                }
            }
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
