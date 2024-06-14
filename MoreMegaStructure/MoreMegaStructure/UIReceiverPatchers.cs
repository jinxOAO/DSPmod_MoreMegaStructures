using HarmonyLib;
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

        internal static void InitAll()
        {
            if (_megaStatusLabelText != null) return;

            GameObject megaStatusLabelTextObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/label-2");
            _megaStatusLabelText = megaStatusLabelTextObj.GetComponent<Text>();
            GammaMode1Text = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/switch-button-1/button-text").GetComponent<Text>();
            GammaMode2Text = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/switch-button-2/button-text").GetComponent<Text>();

            //GameObject oriButtonObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/switch-button-2");
            //GameObject RRUIRoot = GameObject.Find("\"UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver");

            //GameObject switchOreModeButtonObj = GameObject.Instantiate(oriButtonObj, RRUIRoot.transform);
            //switchOreModeButtonObj.transform.localPosition = new Vector3(-155, 155, 0);
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

                if (protoId == 2208 && megaType == 0 && productId == 0) return;

                if (!ReceiverPatchers.ProductId2MegaType.ContainsKey(productId)) return; // 未知的生成物，可能被其他mod修改过

                int protoProductId = LDB.items.Select(protoId).prefabDesc.powerProductId; // 设置为物质生成模式后的生成物Id
                if (!ReceiverPatchers.ProductId2MegaType.ContainsKey(protoProductId)) return;
                int corresMegaType = ReceiverPatchers.ProductId2MegaType[protoProductId]; // 此接收器对应的巨构类型

                bool isMode1 = false;
                bool isMode2 = false;
                if (productId == 0 && !ReceiverPatchers.RRId2OreId.ContainsKey(protoId))
                    isMode1 = true;
                
                else if (corresMegaType == 1 && ReceiverPatchers.RRId2OreId.ContainsKey(protoId) && productId == ReceiverPatchers.RRId2OreId[protoId])
                    isMode1 = true;
                
                if(productId == LDB.items.Select(protoId)?.prefabDesc?.powerProductId)
                    isMode2 = true;

                if(corresMegaType == 1)
                {
                    GammaMode1Text.text = "原矿模式".Translate(); // 支持原矿模式的建筑，第一个按钮定为输出原矿的模式
                    __instance.gammaMode1Button.tips.tipTitle = "原矿模式";
                    __instance.gammaMode1Button.tips.tipText = "原矿模式提示";
                }
                else
                {
                    GammaMode1Text.text = "直接发电".Translate();
                    __instance.gammaMode1Button.tips.tipTitle = "直接发电";
                    __instance.gammaMode1Button.tips.tipText = "直接发电提示";
                }

                //GammaMode2Text.text = (corresMegaType == 1) ? "冶炼模式".Translate() : "物质合成".Translate();

                // 正确设定按钮高亮状态
                __instance.gammaMode1Button.highlighted = isMode1; 
                __instance.gammaMode2Button.highlighted = isMode2;

                // 正确显示产物图标和tip
                ItemProto realProduct = LDB.items.Select(productId);
                __instance.productIcon.sprite = realProduct == null ? null : realProduct.iconSprite;
                __instance.productUIButton.tips.itemId = ((realProduct == null) ? 0 : realProduct.ID);

                // 如果一切正常，则不显示错误提示
                if (productId > 0 && ReceiverPatchers.ProductId2MegaType[productId] == megaType) return;

                // 否则判断错误类型，并提示玩家
                if (_megaStatusLabelText != null) _megaStatusLabelText.text = "巨构状态".Translate();

                bool wrongMode = productId == 0 && corresMegaType == megaType; // 说明建筑是相符的，但是没设置对模式

                string content = window.noPowerColorPrefix + (wrongMode ? "模式错误" : "巨构类型不符").Translate() + window.colorPostfix;

                window.gammaOutputIncreaseText.text = "";
                window.gammaOutputText.supportRichText = true;
                window.gammaEtaText.supportRichText = true;
                window.gammaDysonText.supportRichText = true;
                window.gammaOutputText.text = content;
                window.gammaEtaText.text = content;
                window.gammaReqText.text = content;
                window.gammaDysonText.text = content;

                
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
            if (!ReceiverPatchers.RRId2OreId.ContainsKey(protoId))
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

        
    }
}
