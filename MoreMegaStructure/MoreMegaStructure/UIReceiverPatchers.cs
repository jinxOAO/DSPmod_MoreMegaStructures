using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MoreMegaStructure
{
    public class UIReceiverPatchers
    {
        public static Text megaStatusLabelText = null;

        public static void InitAll()
        {
            if (megaStatusLabelText == null)
            {
                GameObject megaStatusLabelTextObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/label-2");
                megaStatusLabelText = megaStatusLabelTextObj.GetComponent<Text>();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIPowerGeneratorWindow), "_OnUpdate")]
        public static void UIOnUpdatePostPatch(ref UIPowerGeneratorWindow __instance)
        {
            var _this = __instance;
            PowerGeneratorComponent powerGeneratorComponent = _this.powerSystem.genPool[_this.generatorId];
            if (powerGeneratorComponent.gamma)
            {
                PlanetFactory factory = _this.factory;
                int megaType = MoreMegaStructure.StarMegaStructureType[factory.planetId / 100 - 1];
                int protoId = factory.entityPool[powerGeneratorComponent.entityId].protoId;//接收器的建筑的原型ID
                int productId = powerGeneratorComponent.productId;

                if (protoId == 2208 && megaType == 0 && productId == 0)
                {
                    return;
                }
                if (!ReceiverPatchers.productId2MegaType.ContainsKey(productId)) // 未知的生成物，可能被其他mod修改过
                {
                    return;
                }
                if (productId > 0 && ReceiverPatchers.productId2MegaType[productId] == megaType)
                {
                    return;
                }
                if (megaStatusLabelText != null)
                {
                    megaStatusLabelText.text = "巨构状态".Translate();
                }
                int protoProductId = LDB.items.Select(protoId).prefabDesc.powerProductId; // 设置为物质生成模式后的生成物Id
                if (productId == 0 && ReceiverPatchers.productId2MegaType[protoProductId] == megaType) // 说明建筑是相符的，但是没设置对模式
                {
                    _this.gammaOutputIncreaseText.text = "";
                    _this.gammaOutputText.supportRichText = true;
                    _this.gammaEtaText.supportRichText = true;
                    _this.gammaDysonText.supportRichText = true;
                    _this.gammaOutputText.text = _this.noPowerColorPrefix + "模式错误".Translate() + _this.colorPostfix;
                    _this.gammaEtaText.text = _this.noPowerColorPrefix + "模式错误".Translate() + _this.colorPostfix;
                    _this.gammaReqText.text = _this.noPowerColorPrefix + "模式错误".Translate() + _this.colorPostfix;
                    _this.gammaDysonText.text = _this.noPowerColorPrefix + "模式错误".Translate() + _this.colorPostfix;
                }
                else
                {
                    _this.gammaOutputIncreaseText.text = "";
                    _this.gammaOutputText.supportRichText = true;
                    _this.gammaEtaText.supportRichText = true;
                    _this.gammaDysonText.supportRichText = true;
                    _this.gammaOutputText.text = _this.noPowerColorPrefix + "巨构类型不符".Translate() + _this.colorPostfix;
                    _this.gammaEtaText.text = _this.noPowerColorPrefix + "巨构类型不符".Translate() + _this.colorPostfix;
                    _this.gammaReqText.text = _this.noPowerColorPrefix + "巨构类型不符".Translate() + _this.colorPostfix;
                    _this.gammaDysonText.text = _this.noPowerColorPrefix + "巨构类型不符".Translate() + _this.colorPostfix;
                }

            }
        }

    }
}
