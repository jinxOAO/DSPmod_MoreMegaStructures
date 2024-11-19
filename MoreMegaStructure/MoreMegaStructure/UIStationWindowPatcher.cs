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
    public class UIStationWindowPatcher
    {
        public static GameObject autoSprayActiveObj;
        public static Text autoSprayActiveText;

        public static void InitAll()
        {
            if(autoSprayActiveObj == null)
            {
                GameObject oriTextObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Station Window/storage-box-0(Clone)/bg/empty-tip");
                if (oriTextObj == null) return;
                GameObject parentObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Station Window");
                if(parentObj == null) return;
                autoSprayActiveObj = new GameObject("auto-spray-text");
                autoSprayActiveObj.transform.SetParent(parentObj.transform, false);
                autoSprayActiveObj.AddComponent<Text>();
                autoSprayActiveObj.SetActive(false);
                autoSprayActiveObj.transform.localScale = Vector3.one;
                autoSprayActiveObj.GetComponent<Graphic>().raycastTarget = false;
                autoSprayActiveObj.transform.localPosition = new Vector3(35, -429, 0);
                autoSprayActiveText = autoSprayActiveObj.GetComponent<Text>();
                autoSprayActiveText.text = "mms自动喷涂".Translate();
                autoSprayActiveText.color = new Color(0.4f, 0.4f, 0.4f, 0.84f);
                autoSprayActiveText.font = oriTextObj.GetComponent<Text>().font;
                autoSprayActiveText.fontSize = oriTextObj.GetComponent<Text>().fontSize;
                autoSprayActiveText.alignment = TextAnchor.MiddleCenter;
                autoSprayActiveText.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "_OnOpen")]
        public static void OnOpenPatch(ref UIStationWindow __instance)
        {
            autoSprayActiveObj.transform.localPosition = new Vector3(35, -429, 0);
            RefreshAutoSprayText(ref __instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "_OnUpdate")]
        public static void OnUpdatePatch(ref UIStationWindow __instance)
        {
            RefreshAutoSprayText(ref __instance);
        }


        public static void RefreshAutoSprayText(ref UIStationWindow __instance)
        {
            if (autoSprayActiveObj == null)
                return;
            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            if (stationComponent == null) return;
            int protoId = __instance.factory.entityPool[stationComponent.entityId].protoId;
            if (protoId != 9512) // 无论如何不显示自动喷涂的提示
            { 
                autoSprayActiveObj.SetActive(false); 
            }
            else
            {
                if (stationComponent.storage[4].itemId == 1143 || stationComponent.storage[4].itemId == 1142 || stationComponent.storage[4].itemId == 1141)
                {
                    autoSprayActiveObj.SetActive(true);
                    //autoSprayActiveText.text = "mms自动喷涂".Translate();
                }
                else
                {
                    autoSprayActiveObj.SetActive(false);
                }
            }
        }
    }
}
