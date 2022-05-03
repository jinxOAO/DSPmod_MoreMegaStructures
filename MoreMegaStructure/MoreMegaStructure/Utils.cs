using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreMegaStructure
{
    class Utils
    {
        static System.Random randSeed = new System.Random();

        public static VectorLF3 RandPosDelta()
        {
            return new VectorLF3(randSeed.NextDouble() - 0.5, randSeed.NextDouble() - 0.5, randSeed.NextDouble() - 0.5);
        }

        public static int RandInt(int min, int max)
        {
            return randSeed.Next(min, max);
        }

        public static void Check(int num, string str = "Check ")
        {
            Debug.Log(str + num.ToString());
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

        public static void SetItemupNodeData(ref UIItemupNode _this, int _itemId, int _getCnt, int _prevCnt, int _totalCnt, float forceWidth)
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
            _this.displayCount = (float)_this.prevCount;
            ItemProto itemProto = LDB.items.Select(_itemId);
            if (itemProto == null)
            {
                _this._Close();
                return;
            }

            _this.itemIconImage.sprite = itemProto.iconSprite;
            _this.getNumText.text = "+ " + _this.getCount.ToString();
            _this.itemNameText.text = itemProto.name;
            if (_this.displayCount > 0f)
            {
                _this.totalNumText.text = _this.displayCount.ToString("#,##0");
            }
            else
            {
                _this.totalNumText.text = "0";
            }
            _this.sizeTween.to = new Vector2(forceWidth, _this.sizeTween.to.y);

            if (flag)
            {
                for (int i = 0; i < _this.tweeners.Length; i++)
                {
                    _this.tweeners[i].Play0To1();
                }
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

    }
}
