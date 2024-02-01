using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreMegaStructure
{
    public static class UIMechaWindowPatcher
    {
        /// <summary>
        /// 因为新增加了选择远程接收上限的信息，机甲面板收起来之后的高度不能太小，所以进行修改
        /// 此外，因为收起来的高度变高，最小显示的军队行数由2行改为3行
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIMechaWindow), "RefreshWindowComplete")]
        public static void UIMechaWindowRefreshWindowCompletePostPatch(ref UIMechaWindow __instance)
        {
            // 这些num参见游戏原始函数的代码
            var _this = __instance;
            bool highlighted = _this.collapseFleetButton.highlighted;
            int num7 = (_this.mecha.groundCombatModule.fleetCount > _this.mecha.spaceCombatModule.fleetCount) ? _this.mecha.groundCombatModule.fleetCount : _this.mecha.spaceCombatModule.fleetCount;
            int num8 = (num7 <= 2 || highlighted) ? 484 : ((num7 >= 4) ? ((num7 > 4 && _this.fleetSwitchGroup.activeSelf) ? 628 : 596) : 540);
            int num9 = (num7 <= 2 || highlighted) ? 152 : ((num7 >= 4) ? ((num7 > 4 && _this.fleetSwitchGroup.activeSelf) ? 296 : 264) : 208);
            if (num8 <= 540) 
                num8 = 540;
            if (num7 >= 3 && num9 < 208)
                num9 = 208;
            _this.rectTrans.sizeDelta = new Vector2(_this.rectTrans.sizeDelta.x, (float)num8);
            _this.fleetGroupRectTrans.sizeDelta = new Vector2(_this.fleetGroupRectTrans.sizeDelta.x, (float)num9);

            bool flag = false;
            for (int j = 0; j < ItemProto.kFighterIds.Length; j++)
            {
                if (_this.history.ItemUnlocked(ItemProto.kFighterIds[j]))
                {
                    flag = true;
                    break;
                }
            }
            if (flag) 
            {
                bool flag2 = _this.mecha.spaceCombatModule.fleetCount > 0;
                for (int m = 0; m < _this.gFleetEntries.Count; m++)
                {
                    UIMechaFleetEntry uimechaFleetEntry3 = _this.gFleetEntries[m];
                    if (_this.fleetTabIndex == 0 && uimechaFleetEntry3.fleetIndex / 4 == _this.fleetPageIndex && (!highlighted || uimechaFleetEntry3.fleetIndex % 4 < 3)) // 2改成了3，下同
                    {
                        uimechaFleetEntry3.SetTrans(flag2);
                        uimechaFleetEntry3._Open();
                        uimechaFleetEntry3._Update();
                    }
                    else
                    {
                        uimechaFleetEntry3._Close();
                    }
                }
                for (int n = 0; n < _this.sFleetEntries.Count; n++)
                {
                    UIMechaFleetEntry uimechaFleetEntry4 = _this.sFleetEntries[n];
                    if (uimechaFleetEntry4.fleetProtoId != _this.mecha.spaceCombatModule.moduleFleets[uimechaFleetEntry4.fleetIndex].protoId)
                    {
                        uimechaFleetEntry4.fleetProtoId = _this.mecha.spaceCombatModule.moduleFleets[uimechaFleetEntry4.fleetIndex].protoId;
                        uimechaFleetEntry4._Close();
                    }
                    if (_this.fleetTabIndex == 1 && uimechaFleetEntry4.fleetIndex / 4 == _this.fleetPageIndex && (!highlighted || uimechaFleetEntry4.fleetIndex % 4 < 3))
                    {
                        uimechaFleetEntry4.SetTrans(flag2);
                        uimechaFleetEntry4._Open();
                        uimechaFleetEntry4._Update();
                    }
                    else
                    {
                        uimechaFleetEntry4._Close();
                    }
                }
            }
        }
    }
}
