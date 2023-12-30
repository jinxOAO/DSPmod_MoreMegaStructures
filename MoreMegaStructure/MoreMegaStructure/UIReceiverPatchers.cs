using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once InconsistentNaming

namespace MoreMegaStructure
{
    public class UIReceiverPatchers
    {
        private static Text _megaStatusLabelText;

        internal static void InitAll()
        {
            if (_megaStatusLabelText != null) return;

            GameObject megaStatusLabelTextObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/label-2");
            _megaStatusLabelText = megaStatusLabelTextObj.GetComponent<Text>();
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

                if (productId > 0 && ReceiverPatchers.ProductId2MegaType[productId] == megaType) return;

                if (_megaStatusLabelText != null) _megaStatusLabelText.text = "巨构状态".Translate();

                int protoProductId = LDB.items.Select(protoId).prefabDesc.powerProductId; // 设置为物质生成模式后的生成物Id

                bool b = productId == 0 && ReceiverPatchers.ProductId2MegaType[protoProductId] == megaType; // 说明建筑是相符的，但是没设置对模式

                string content = window.noPowerColorPrefix + (b ? "模式错误" : "巨构类型不符").Translate() + window.colorPostfix;

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
    }
}
