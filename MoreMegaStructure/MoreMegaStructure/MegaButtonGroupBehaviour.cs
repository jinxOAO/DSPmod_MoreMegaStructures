using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MoreMegaStructure
{
    public class MegaButtonGroupBehaviour//: MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        //public void OnPointerEnter(PointerEventData eventData)
        //{
        //    Debug.Log("--");
        //}
        //public void OnPointerExit(PointerEventData eventData)
        //{
        //    Debug.Log("--");
        //}

        static bool cleared = false;
        static float targetX = 0;
        public static float currentX = 0;

        public static void ShowSetMegaGroup()
        {
            targetX = 0;
            MoreMegaStructure.LeftMegaBuildWarning.SetActive(false);
            if(UIRoot.instance?.uiGame?.dysonEditor?.selection!=null && !cleared)
            {
                try
                {
                    cleared = true;
                    UIRoot.instance.uiGame.dysonEditor.selection.ClearAllSelection();
                }
                catch (Exception)
                { }
            }
        }

        public static void HideSetMegaGroup()
        {
            targetX = -270;
            cleared = false;
        }

        public static void SetMegaGroupMove()
        {
            Transform trans = MoreMegaStructure.setMegaGroupObj.transform;
            Vector3 cur = trans.localPosition;
            if(targetX > cur.x + 20)
            {
                currentX = cur.x + 20;
                trans.localPosition = new Vector3(currentX, cur.y, cur.z);
            }
            else if(targetX < cur.x - 20)
            {
                currentX = cur.x - 20;
                trans.localPosition = new Vector3(currentX, cur.y, cur.z);
            }
            else if(targetX != cur.x)
            {
                currentX = targetX;
                trans.localPosition = new Vector3(currentX, cur.y, cur.z);
            }
            if(currentX<=-260)
            {
                MoreMegaStructure.LeftMegaBuildWarning.SetActive(true);
                MoreMegaStructure.SetMegaStructureWarningText.text = "鼠标触碰左侧黄条以规划巨构".Translate();
            }
        }
    }
}
