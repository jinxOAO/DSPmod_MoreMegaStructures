using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once InconsistentNaming

namespace MoreMegaStructure
{
    public class UIReceiverPatchers
    {
        private static Text _megaStatusLabelText;
        private static Text GammaMode1Text;
        private static Text GammaMode2Text;
        private static Text applyAllButtonText;
        private static Text applyAllButtonText0;

        public static GameObject oriMode1ButtonObj;
        public static GameObject oriMode2ButtonObj;
        public static GameObject productSetParentObj;
        public static GameObject applyAllObj;
        public static UIButton applyAllUIBtn;
        public static GameObject applyAllObj0;
        public static UIButton applyAllUIBtn0;
        public static List<GameObject> productSetButtonObjs;
        public static List<Image> productSetImages;
        public static List<UIButton> productSetUIBtns;
        public static PlanetFactory lastFactory = null;
        public static int lastId = -1;
        public static int lastMegaType = -1;

        public static PowerGeneratorComponent[] curPool = null;
        public static int curIndex = 0;

        public static List<int> buttonIndex2ItemId_1;
        public static List<int> buttonIndex2ItemId_5;
        //public static Dictionary<int, int> itemId2ButtonIndex;

        internal static void InitAll()
        {
            if (_megaStatusLabelText != null) return;

            GameObject megaStatusLabelTextObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/label-2");
            _megaStatusLabelText = megaStatusLabelTextObj.GetComponent<Text>();
            GammaMode1Text = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/switch-button-1/button-text").GetComponent<Text>();
            GammaMode2Text = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/switch-button-2/button-text").GetComponent<Text>();

            oriMode1ButtonObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/switch-button-1");
            oriMode2ButtonObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/switch-button-2");
            productSetButtonObjs = new List<GameObject>();
            productSetImages = new List<Image>();
            productSetUIBtns = new List<UIButton>();

            productSetParentObj = new GameObject();
            productSetParentObj.name = "sub-button-group";
            productSetParentObj.transform.SetParent(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver").transform);
            productSetParentObj.transform.localScale = Vector3.one;
            productSetParentObj.transform.localPosition = new Vector3(-165, 60, 0);
            for (int i = 0; i < 12; i++) 
            {
                GameObject go = GameObject.Instantiate(oriMode1ButtonObj, productSetParentObj.transform);
                go.name = "switch-button-" + i.ToString();
                GameObject.DestroyImmediate(go.GetComponent<Button>());
                GameObject.DestroyImmediate(go.transform.Find("button-text").gameObject);
                go.transform.localScale = Vector3.one;
                go.transform.localPosition = new Vector3(i * 28, 0);
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
                Button button = go.AddComponent<Button>();
                go.GetComponent<UIButton>().button = button;
                int j = i;
                button.onClick.AddListener(() => { OnProductSetButtonClick(j); });

                GameObject icon = new GameObject();
                icon.name = "icon";
                icon.transform.SetParent(go.transform);
                Image img = icon.AddComponent<Image>();
                img.sprite = LDB.items.Select(1101)._iconSprite;
                icon.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                icon.GetComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
                icon.transform.localScale = Vector3.one;
                icon.transform.localPosition = new Vector3(0, 0, 0);

                productSetButtonObjs.Add(go);
                productSetImages.Add(img);
                productSetUIBtns.Add(go.GetComponent<UIButton>());

            }

            // apply all button Dyson Sphere
            applyAllObj0 = GameObject.Instantiate(oriMode1ButtonObj);
            applyAllObj0.transform.SetParent(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver").transform);
            applyAllObj0.name = "apply-all-button";
            GameObject.DestroyImmediate(applyAllObj0.GetComponent<Button>());
            applyAllObj0.transform.localScale = Vector3.one;
            applyAllObj0.transform.localPosition = new Vector3(-155, 35);
            applyAllObj0.GetComponent<RectTransform>().sizeDelta = new Vector2(310, 25);
            Button applyAllButton0 = applyAllObj0.AddComponent<Button>();
            applyAllUIBtn0 = applyAllObj0.GetComponent<UIButton>();
            applyAllButton0.onClick.AddListener(() => { ApplyAll(); });
            applyAllUIBtn0.tips.tipTitle = "";
            applyAllButtonText0 = applyAllObj0.transform.Find("button-text").gameObject.GetComponent<Text>();
            applyAllButtonText0.fontSize = 16;
            oriMode1ButtonObj.transform.localPosition = new Vector3(-155, 65, 0);
            oriMode2ButtonObj.transform.localPosition = new Vector3(5, 65, 0);
            oriMode1ButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 25);
            oriMode2ButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 25);

            // apply all button MD/CR
            applyAllObj = GameObject.Instantiate(oriMode1ButtonObj, productSetParentObj.transform);
            applyAllObj.name = "apply-all-button";
            GameObject.DestroyImmediate(applyAllObj.GetComponent<Button>());
            applyAllObj.transform.localScale = Vector3.one;
            applyAllObj.transform.localPosition = new Vector3(84, -28);
            applyAllObj.GetComponent<RectTransform>().sizeDelta = new Vector2(164, 24);
            Button applyAllButton = applyAllObj.AddComponent<Button>();
            applyAllUIBtn = applyAllObj.GetComponent<UIButton>();
            applyAllButton.onClick.AddListener(() => { ApplyAll(); });
            applyAllUIBtn.tips.tipTitle = "";
            applyAllButtonText = applyAllObj.transform.Find("button-text").gameObject.GetComponent<Text>();
            applyAllButtonText.fontSize = 14;

            //GameObject oriButtonObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/switch-button-2");
            //GameObject RRUIRoot = GameObject.Find("\"UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver");

            //GameObject switchOreModeButtonObj = GameObject.Instantiate(oriButtonObj, RRUIRoot.transform);
            //switchOreModeButtonObj.transform.localPosition = new Vector3(-155, 155, 0);
            InitDict();
        }

        public static void InitDict()
        {
            buttonIndex2ItemId_1 = new List<int>
            {
                1001,
                1002,
                1003,
                1004,
                1006,
                1016,
                1101,
                1102,
                1104,
                1105,
                1106,
                1109
            };
            buttonIndex2ItemId_5 = new List<int>
            {
                1126,
                1014
            };
            //itemId2ButtonIndex = new Dictionary<int, int>();
            //for (int i = 0; i < buttonIndex2ItemId_1.Count; i++)
            //{
            //    itemId2ButtonIndex[buttonIndex2ItemId_1[i]] = i;
            //}
            //for (int i = 0;i < buttonIndex2ItemId_5.Count; i++)
            //{
            //    itemId2ButtonIndex[buttonIndex2ItemId_5[i]] = i;
            //}
        }

        [HarmonyPatch(typeof(UIPowerGeneratorWindow), "_OnUpdate")]
        [HarmonyPostfix]
        public static void UIOnUpdatePostPatch(ref UIPowerGeneratorWindow __instance)
        {
            UIPowerGeneratorWindow window = __instance;
            PowerGeneratorComponent powerGeneratorComponent = window.powerSystem.genPool[window.generatorId];
            if (powerGeneratorComponent.gamma)
            {
                PlanetFactory factory = window.factory;
                int megaType = MoreMegaStructure.StarMegaStructureType[factory.planetId / 100 - 1];
                int protoId = factory.entityPool[powerGeneratorComponent.entityId].protoId; //接收器的建筑的原型ID
                int productId = powerGeneratorComponent.productId;

                // if (protoId == 2208 && megaType == 0 && productId == 0) return;

                if (!ReceiverPatchers.ProductId2MegaType.ContainsKey(productId)) return; // 未知的生成物，可能被其他mod修改过

                int protoProductId = LDB.items.Select(protoId).prefabDesc.powerProductId; // 设置为物质生成模式后的生成物Id
                if (!ReceiverPatchers.ProductId2MegaType.ContainsKey(protoProductId)) return;
                int corresMegaType = ReceiverPatchers.ProductId2MegaType[productId]; // 此接收器对应的巨构类型

                bool isMode1 = false;
                bool isMode2 = false;
                if (productId == 0)
                    isMode1 = true;
                
                else if (corresMegaType == 1 && ReceiverPatchers.RRId2OreId.ContainsKey(protoId) && productId == ReceiverPatchers.RRId2OreId[protoId])
                    isMode1 = true;
                
                if(productId == LDB.items.Select(protoId)?.prefabDesc?.powerProductId)
                    isMode2 = true;

                GammaMode1Text.text = "直接发电".Translate();
                __instance.gammaMode1Button.tips.tipTitle = "直接发电";
                __instance.gammaMode1Button.tips.tipText = "直接发电提示";
                //if(corresMegaType == 1)
                //{
                //    GammaMode1Text.text = "原矿模式".Translate(); // 支持原矿模式的建筑，第一个按钮定为输出原矿的模式
                //    __instance.gammaMode1Button.tips.tipTitle = "原矿模式";
                //    __instance.gammaMode1Button.tips.tipText = "原矿模式提示";
                //}
                //else
                //{
                //}

                //GammaMode2Text.text = (corresMegaType == 1) ? "冶炼模式".Translate() : "物质合成".Translate();

               

                // 正确显示产物图标和tip
                ItemProto realProduct = LDB.items.Select(productId);
                __instance.productIcon.sprite = realProduct == null ? null : realProduct.iconSprite;
                __instance.productUIButton.tips.itemId = ((realProduct == null) ? 0 : realProduct.ID);

                // 如果产物设置和巨构不符，显示错误提示
                if (corresMegaType != megaType)
                {
                    if (_megaStatusLabelText != null) 
                        _megaStatusLabelText.text = "巨构状态".Translate();

                    //bool wrongMode = productId == 0 && corresMegaType == megaType; // 说明建筑是相符的，但是没设置对模式

                    //string content = window.noPowerColorPrefix + (wrongMode ? "模式错误" : "巨构类型不符").Translate() + window.colorPostfix;
                    string content = window.noPowerColorPrefix + "巨构类型不符".Translate() + window.colorPostfix;

                    window.gammaOutputIncreaseText.text = "";
                    window.gammaOutputText.supportRichText = true;
                    window.gammaEtaText.supportRichText = true;
                    window.gammaDysonText.supportRichText = true;
                    window.gammaOutputText.text = content;
                    window.gammaEtaText.text = content;
                    window.gammaReqText.text = content;
                    window.gammaDysonText.text = content;
                }

                List<int> buttonIndex2ItemId = null; 
                if (megaType == 1)
                {
                    buttonIndex2ItemId = buttonIndex2ItemId_1;
                }
                else if (megaType == 5)
                {
                    buttonIndex2ItemId = buttonIndex2ItemId_5;
                }

                // 正确设定按钮高亮状态
                __instance.gammaMode1Button.highlighted = isMode1;
                __instance.gammaMode2Button.highlighted = isMode2;
                applyAllUIBtn.highlighted = false;
                applyAllUIBtn0.highlighted = false;
                if (buttonIndex2ItemId != null)
                {
                    for (int i = 0; i < productSetUIBtns.Count; i++)
                    {
                        if (buttonIndex2ItemId.Count > i && buttonIndex2ItemId[i] == productId)
                        {
                            productSetUIBtns[i].highlighted = true;
                        }
                        else
                        {
                            productSetUIBtns[i].highlighted = false;
                        }
                    }
                }

                applyAllButtonText.text = "应用到全部".Translate();
                applyAllButtonText0.text = "应用到全部".Translate();

                // 如果窗口所对应的建筑更改、或者根据巨构类型后，刷新界面按钮。我猜也可以patch UIGame.OnPlayerInspecteeChange
                if (factory != lastFactory || window.generatorId != lastId || megaType != lastMegaType)
                {
                    lastFactory = factory;
                    lastId = window.generatorId;
                    lastMegaType = megaType;
                    if(megaType == 1)
                    {
                        oriMode1ButtonObj.SetActive(false);
                        oriMode2ButtonObj.SetActive(false);
                        applyAllObj0.SetActive(false);
                        productSetParentObj.SetActive(true);
                    }
                    else if (megaType == 5)
                    {
                        oriMode1ButtonObj.SetActive(false);
                        oriMode2ButtonObj.SetActive(false);
                        applyAllObj0.SetActive(false);
                        productSetParentObj.SetActive(true);
                    }
                    else
                    {
                        oriMode1ButtonObj.SetActive(true);
                        oriMode2ButtonObj.SetActive(true);
                        applyAllObj0.SetActive(true);
                        productSetParentObj.SetActive(false);
                    }
                    if(buttonIndex2ItemId != null)
                    {
                        int count = Math.Min(buttonIndex2ItemId.Count, productSetButtonObjs.Count);
                        for (int i = 0; i < count; i++)
                        {
                            productSetButtonObjs[i].SetActive(true);
                            productSetImages[i].sprite = LDB.items.Select(buttonIndex2ItemId[i]).iconSprite;
                            productSetUIBtns[i].tips.itemId = buttonIndex2ItemId[i];
                            productSetUIBtns[i].tips.tipTitle = "";
                        }
                        for (int j = count; j < productSetButtonObjs.Count; j++)
                        {
                            productSetButtonObjs[j].SetActive(false);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(UIPowerGeneratorWindow), "OnGammaMode1Click")]
        [HarmonyPrefix]
        public static bool OnGammaMode1ClickPrePatch(ref UIPowerGeneratorWindow __instance, int obj)
        {
            if (__instance.generatorId == 0 || __instance.factory == null || __instance.player == null)
            {
                return false;
            }
            PowerGeneratorComponent powerGeneratorComponent = __instance.powerSystem.genPool[__instance.generatorId];
            if (powerGeneratorComponent.id != __instance.generatorId)
            {
                return false;
            }

            int protoId = __instance.factory.entityPool[powerGeneratorComponent.entityId].protoId; //接收器的建筑的原型ID
            int protoProductId = LDB.items.Select(protoId).prefabDesc.powerProductId; // 设置为物质生成模式后的生成物Id
            if (!ReceiverPatchers.ProductId2MegaType.ContainsKey(protoProductId)) 
                return true;
            if (!ReceiverPatchers.RRId2OreId.ContainsKey(protoId))
                return true;
            int productId = powerGeneratorComponent.productId;
            int num = (int)powerGeneratorComponent.productCount;
            ItemProto itemProto = LDB.items.Select((int)__instance.factory.entityPool[powerGeneratorComponent.entityId].protoId);
            int targetId = ReceiverPatchers.RRId2OreId[protoId];
            if (productId == targetId) 
                return false;
            
            if (productId != 0 && num > 0)
            {
                int upCount = __instance.player.TryAddItemToPackage(productId, num, 0, true, 0, false);
                UIItemup.Up(productId, upCount);
            }
            __instance.powerSystem.genPool[__instance.generatorId].productId = targetId;
            __instance.powerSystem.genPool[__instance.generatorId].productCount = 0f;
            if(ReceiverPatchers.MDOreModeFactor.ContainsKey(targetId))
                __instance.powerSystem.genPool[__instance.generatorId].productHeat = itemProto.prefabDesc.powerProductHeat / ReceiverPatchers.MDOreModeFactor[targetId];
            return false;
        }

        [HarmonyPatch(typeof(UIPowerGeneratorWindow), "OnGammaMode2Click")]
        [HarmonyPrefix]
        public static bool OnGammaMode2ClickPrePatch(ref UIPowerGeneratorWindow __instance, int obj)
        {
            if (__instance.generatorId == 0 || __instance.factory == null || __instance.player == null)
            {
                return false;
            }
            PowerGeneratorComponent powerGeneratorComponent = __instance.powerSystem.genPool[__instance.generatorId];
            if (powerGeneratorComponent.id != __instance.generatorId)
            {
                return false;
            }

            int protoId = __instance.factory.entityPool[powerGeneratorComponent.entityId].protoId; //接收器的建筑的原型ID
            int protoProductId = LDB.items.Select(protoId).prefabDesc.powerProductId; // 设置为物质生成模式后的生成物Id
            if (!ReceiverPatchers.ProductId2MegaType.ContainsKey(protoProductId))
                return true;

            int productId = powerGeneratorComponent.productId;
            int num = (int)powerGeneratorComponent.productCount;
            ItemProto itemProto = LDB.items.Select((int)__instance.factory.entityPool[powerGeneratorComponent.entityId].protoId);
            GameHistoryData history = GameMain.history;
            if (LDB.items.Select(itemProto.prefabDesc.powerProductId) == null)
            {
                __instance.powerSystem.genPool[__instance.generatorId].productId = 0;
                return false;
            }
            int targetId = itemProto.prefabDesc.powerProductId;
            if (productId == targetId) 
                return false;

            if (productId != 0 && num > 0)
            {
                int upCount = __instance.player.TryAddItemToPackage(productId, num, 0, true, 0, false);
                UIItemup.Up(productId, upCount);
            }
            __instance.powerSystem.genPool[__instance.generatorId].productId = targetId;
            __instance.powerSystem.genPool[__instance.generatorId].productCount = 0f;
            __instance.powerSystem.genPool[__instance.generatorId].productHeat = itemProto.prefabDesc.powerProductHeat;
            return false;
        }

        public static void OnProductSetButtonClick(int index)
        {
            UIPowerGeneratorWindow window = UIRoot.instance.uiGame.generatorWindow;
            if (window != null)
            {
                PlanetFactory factory = window.factory;
                int megaType = MoreMegaStructure.StarMegaStructureType[factory.planetId / 100 - 1];
                List<int> itemMap = null;
                if (megaType == 1)
                    itemMap = buttonIndex2ItemId_1;
                else if (megaType == 5)
                    itemMap = buttonIndex2ItemId_5;
                if (itemMap != null)
                {

                    int productId = window.powerSystem.genPool[window.generatorId].productId;
                    int num = (int)window.powerSystem.genPool[window.generatorId].productCount;
                    GameHistoryData history = GameMain.history;
                    int targetId = itemMap[index];
                    if (LDB.items.Select(targetId) == null)
                    {
                        window.powerSystem.genPool[window.generatorId].productId = 0;
                        return;
                    }
                    if (productId == targetId)
                        return;

                    if (productId != 0 && num > 0)
                    {
                        int upCount = GameMain.mainPlayer.TryAddItemToPackage(productId, num, 0, true, 0, false);
                        UIItemup.Up(productId, upCount);
                    }
                    if (ReceiverPatchers.productHeat.ContainsKey(targetId))
                    {
                        window.powerSystem.genPool[window.generatorId].productId = targetId;
                        window.powerSystem.genPool[window.generatorId].productCount = 0f;
                        window.powerSystem.genPool[window.generatorId].productHeat = ReceiverPatchers.productHeat[targetId];
                    }
                }
            }
        }


        public static void ApplyAll()
        {
            UIPowerGeneratorWindow window = UIRoot.instance.uiGame.generatorWindow;
            PlanetFactory factory = window?.factory;
            if(factory != null)
            {
                int targetProductId = window.powerSystem.genPool[window.generatorId].productId;
                long productHeat = window.powerSystem.genPool[window.generatorId].productHeat;
                PowerSystem powerSystem = factory.powerSystem;
                int count = 0;
                for (int i = 1; i < factory.entityCursor; i++)
                {
                    if (factory.entityPool[i].id == i && factory.entityPool[i].powerGenId > 0)
                    {
                        int powerGenId = factory.entityPool[i].powerGenId;
                        int oriProductId = powerSystem.genPool[powerGenId].productId;
                        int oriProductCount = (int)powerSystem.genPool[powerGenId].productCount;
                        if (oriProductId != targetProductId)
                        {
                            if (oriProductId != 0 && oriProductCount > 0)
                            {
                                int upCount = GameMain.mainPlayer.TryAddItemToPackage(oriProductId, oriProductCount, 0, true, 0, false);
                                UIItemup.Up(oriProductId, upCount);
                            }
                            window.powerSystem.genPool[powerGenId].productId = targetProductId;
                            window.powerSystem.genPool[powerGenId].productCount = 0f;
                            window.powerSystem.genPool[powerGenId].productHeat = productHeat;
                        }
                        count++;
                    }
                }
                UIRealtimeTip.Popup(string.Format("成功应用数量提示".Translate(), count));
            }
        }
    }
}
