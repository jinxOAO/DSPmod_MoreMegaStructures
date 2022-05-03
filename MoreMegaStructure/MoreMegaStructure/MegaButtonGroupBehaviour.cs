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
            //MoreMegaStructure.LeftMegaBuildWarning.SetActive(false);
            if (UIRoot.instance?.uiGame?.dysonEditor?.selection != null && !cleared)
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
            bool moved = false;
            Transform trans = MoreMegaStructure.setMegaGroupObj.transform;
            Vector3 cur = trans.localPosition;
            if (MoreMegaStructure.NoUIAnimation.Value)
            {
                if(cur.x!=targetX)
                {
                    currentX = targetX;
                    trans.localPosition = new Vector3(currentX, cur.y, cur.z);
                    moved = true;
                }
            }
            else
            {
                if (targetX > cur.x + 0.5f)
                {
                    //currentX = cur.x + 20;
                    float distance = targetX - cur.x;
                    float move = Math.Max(distance * 0.2f, 0.5f);
                    currentX = cur.x + move;
                    trans.localPosition = new Vector3(currentX, cur.y, cur.z);
                    moved = true;
                }
                else if (targetX < cur.x - 0.5f)
                {
                    //currentX = cur.x - 20;
                    float distance = targetX - cur.x;
                    float move = Math.Min(distance * 0.2f, -0.5f);
                    currentX = cur.x + move;
                    trans.localPosition = new Vector3(currentX, cur.y, cur.z);
                    moved = true;
                }
                else if (targetX != cur.x)
                {
                    currentX = targetX;
                    trans.localPosition = new Vector3(currentX, cur.y, cur.z);
                    moved = true;
                }
            }

            if (moved)
            {
                //MoreMegaStructure.LeftMegaBuildWarning.SetActive(true);
                MoreMegaStructure.SetMegaStructureWarningText.text = "鼠标触碰左侧黄条以规划巨构".Translate();
                float alpha = (-200 - currentX) * 1.0f / 70;
                MoreMegaStructure.SetMegaStructureWarningText.color = new Color(1f, 1f, 0.57f, alpha);
                float scale = (-currentX) * 1.0f / 270;
                if (scale > 1) scale = 1;
                MoreMegaStructure.LeftMegaBuildWarning.transform.localScale = new Vector3(scale, 1, 1);
            }
        }
    }
}
