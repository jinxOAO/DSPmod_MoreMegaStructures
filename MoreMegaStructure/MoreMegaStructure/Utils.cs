using HarmonyLib;
using NGPT;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoreMegaStructure
{
    public static class Utils
    {
        private static readonly System.Random RandSeed = new System.Random();
        public static string _textColorVanillaBlue = "<color=\"#61D8FFc0\">{1}</color>";
        public static string TextColorVanillaOrange = "<color=\"#FD965ECC\">{1}</color>";

        public static VectorLF3 RandPosDelta()
        {
            return new VectorLF3(RandSeed.NextDouble() - 0.5, RandSeed.NextDouble() - 0.5, RandSeed.NextDouble() - 0.5);
        }

        public static VectorLF3 RandVerticalPosDelta(VectorLF3 normal)
        {
            //if (normal.x == 0 && normal.y == 0)
            //    return new VectorLF3(RandSeed.NextDouble() - 0.5, RandSeed.NextDouble() - 0.5, 0);
            //else if (normal.y == 0 && normal.z == 0)
            //    return new VectorLF3(0, RandSeed.NextDouble() - 0.5, RandSeed.NextDouble() - 0.5);
            //else if (normal.x == 0 && normal.z == 0)
            //    return new VectorLF3(RandSeed.NextDouble() - 0.5, 0, RandSeed.NextDouble() - 0.5);

            try
            {
                return VectorLF3.Cross(normal, RandPosDelta()).normalized * (RandSeed.NextDouble() - 0.5);
            }
            catch (System.Exception)
            {
                return VectorLF3.zero;
            }
        }

        public static int RandInt(int min, int max)
        {
            return RandSeed.Next(min, max);
        }

        public static float RandF()
        {
            return (float)RandSeed.NextDouble();
        }

        public static void Check(int num = 0, string str = "Check ")
        {
            Debug.Log(str + num.ToString());
        }

        public static void Log(string text)
        {
            Debug.Log(text);
        }

        public static void UIItemUp(int itemId, int upCount, float forceWidth = 300)
        {
            if (GameMain.mainPlayer == null)
            {
                return;
            }

            if (UIRoot.instance == null)
            {
                return;
            }

            if (upCount <= 0)
            {
                return;
            }

            UIItemup itemupTips = UIRoot.instance.uiGame.itemupTips;
            int itemCount = GameMain.mainPlayer.package.GetItemCount(itemId);
            bool flag;
            UIItemupNode uiitemupNode = itemupTips.CreateNode(itemId, out flag);
            SetItemupNodeData(ref uiitemupNode, itemId, upCount, itemCount - upCount, itemCount, forceWidth);
            //uiitemupNode.SetData(itemId, upCount, itemCount - upCount, itemCount);
            if (!flag)
            {
                itemupTips.itemups.Insert(0, uiitemupNode);
                UIRoot.instance.uiGame.itemupTips.transform.SetAsLastSibling();
            }

            if (!itemupTips.active)
            {
                itemupTips._Open();
            }
        }

        public static void SetItemupNodeData(
            ref UIItemupNode _this,
            int _itemId,
            int _getCnt,
            int _prevCnt,
            int _totalCnt,
            float forceWidth)
        {
            if (_this.fadeOutCalled)
            {
                Assert.CannotBeReached();
                return;
            }

            bool flag = _itemId != _this.itemId;
            _this.itemId = _itemId;
            _this.getCount += _getCnt;
            _this.prevCount = _prevCnt;
            _this.totalCount = _totalCnt;
            _this.displayCount = _this.prevCount;
            ItemProto itemProto = LDB.items.Select(_itemId);

            if (itemProto == null)
            {
                _this._Close();
                return;
            }

            _this.itemIconImage.sprite = itemProto.iconSprite;
            _this.getNumText.text = "+ " + _this.getCount.ToString();
            _this.itemNameText.text = itemProto.name;
            _this.totalNumText.text = _this.displayCount > 0f ? _this.displayCount.ToString("#,##0") : "0";

            _this.sizeTween.to = new Vector2(forceWidth, _this.sizeTween.to.y);

            if (flag)
            {
                foreach (Tweener t in _this.tweeners) t.Play0To1();
            }
            else
            {
                _this.tweeners[1].Play0To1();
                _this.tweeners[1].normalizedTime = 0.3f;
                _this.tweeners[2].Play0To1();
                _this.tweeners[2].normalizedTime = 0.5f;
            }

            _this.time = (flag ? 0f : 0.3f);
        }

        public static string MegaNameByType(int type)
        {
            switch (type)
            {
                case 1:
                    return "物质解压器".Translate();

                case 2:
                    return "科学枢纽".Translate();

                case 3:
                    return "折跃场广播阵列".Translate();

                case 4:
                    return "星际组装厂".Translate();

                case 5:
                    return "晶体重构器".Translate();

                case 6:
                    return "恒星炮".Translate();

                default:
                    return "戴森球jinx".Translate();
            }
        }

        public static int Limit(int ori, int min, int max)
        {
            if (ori < min)
                return min;
            else if (ori > max)
                return max;
            else return ori;
        }
        //public static bool RebindButtonOnClick(this GameObject go, UnityEngine.Events.UnityAction call)
        //{
        //    if (go == null)
        //        return false;
        //    Button button = go.GetComponent<Button>();
        //    if (button == null)
        //        return false;
        //    GameObject.DestroyImmediate(button);

        //    Button button2 = go.AddComponent<Button>();
        //    button2.onClick.AddListener(call);
        //    go.GetComponent<UIButton>().button = button2;
        //    return true;
        //}


    }

}
