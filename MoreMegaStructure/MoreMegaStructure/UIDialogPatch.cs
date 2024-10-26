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
    public class UIDialogPatch
    {
        private struct Data
        {
            public int _techId;
            public string _title;
            public string _conclusion;
            public ItemProto[] _items;
            public Sprite _sprite;
            public int _type;
            public Data(int techId, string title, string conclusion, int type, Sprite sprite, ItemProto[] items)
            {
                _techId = techId;
                _title = title;
                _conclusion = conclusion;
                _items = items;
                _sprite = sprite;
                _type = type;
            }
        }

        private static Dictionary<int, Data> dictionary = new Dictionary<int, Data>();
        private static int _nextId = -1000;
        private static string _originTitleText = null;

        public static void ShowUIDialog(string title, string conclusion, int type = 0, Sprite sprite = null, ItemProto[] items = null)
        {
            if (_originTitleText == null)
            {
                _originTitleText = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Research Result Window/content/title-text")?.GetComponent<Text>()?.text;
            }

            while (dictionary.ContainsKey(_nextId)) _nextId--;
            dictionary.Add(_nextId, new Data(_nextId, title, conclusion, type, sprite, items));

            UIRoot.instance.uiGame.researchResultTip.SetTechId(_nextId);
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIResearchResultWindow), "SetTechId")]
        public static bool UIResearchResultWindow_SetTechId(ref UIResearchResultWindow __instance, int _techId)
        {
            if (_techId >= 0 || !dictionary.ContainsKey(_techId))
            {
                if (_originTitleText != null)
                    GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Research Result Window/content/title-text").GetComponent<Text>().text = _originTitleText;
                _originTitleText = null;
                return true;
            }

            AccessTools.Field(typeof(UIResearchResultWindow), "techId").SetValue(__instance, _techId);

            Data data = dictionary[_techId];

            for (int i = 0; i < __instance.itemIcons.Length; i++)
            {
                if (data._type == 1)
                {
                    __instance.itemIcons[i].gameObject.SetActive(true);
                    __instance.itemIcons[i].sprite = data._sprite;
                    __instance.itemButtons[i].tips.itemId = 0;
                    //__instance.itemButtons[i].tips.tipTitle = "";
                    __instance.itemIcons[i].rectTransform.anchoredPosition = new Vector2(0f, 0f);
                }
                else if (data._items == null || i >= data._items.Length)
                {
                    __instance.itemIcons[i].gameObject.SetActive(false);
                    __instance.itemIcons[i].sprite = null;
                    __instance.itemButtons[i].tips.itemId = 0;

                }
                else
                {
                    __instance.itemIcons[i].gameObject.SetActive(true);
                    __instance.itemIcons[i].sprite = data._items[i].iconSprite;
                    __instance.itemButtons[i].tips.itemId = data._items[i].ID;
                    float x = ((float)data._items.Length - 1f) * -45f + 90f * (float)i;
                    __instance.itemIcons[i].rectTransform.anchoredPosition = new Vector2(x, 0f);
                }
            }
            int num2 = (data._items != null && data._items.Length != 0 || data._type == 1) ? 90 : 0;
            __instance.functionText.text = data._type == 0 ? data._title : "";
            __instance.functionText.rectTransform.anchoredPosition = new Vector2(0f, (float)(-(float)num2));

            if (!string.IsNullOrEmpty(data._title) && data._type == 0)
            {
                num2 += (int)__instance.functionText.preferredHeight;
                num2 += 5;
            }

            __instance.conclusionText.text = data._conclusion;
            __instance.conclusionText.rectTransform.anchoredPosition = new Vector2(0f, (float)(-(float)num2));
            if (!string.IsNullOrEmpty(data._conclusion))
            {
                num2 += (int)__instance.conclusionText.preferredHeight;
                num2 += 5;
            }
            num2 += 110;
            AccessTools.Field(typeof(UIResearchResultWindow), "windowHeight").SetValue(__instance, (float)num2);
            __instance.windowTrans.sizeDelta = new Vector2(400f, 0f);
            __instance._Open();
            string dialogTitle = "游戏提示gm".Translate();
            if (data._type == 1)
                dialogTitle = data._title;
            GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Research Result Window/content/title-text").GetComponent<Text>().text = dialogTitle;

            return false;
        }
    }
}
