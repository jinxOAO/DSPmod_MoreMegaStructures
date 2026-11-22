using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

namespace MoreMegaStructure
{
    public static class UIStarAssemblyTodoList
    {
        public static GameObject todoListObj;
        public static GameObject todoListInputObj;
        public static InputField todoListInput;
        public static RectTransform contentRT;
        public static RectTransform inputRT;
        public static Text todoListText;

        /// <summary>克隆体自带的原生宿主；点亮它即可复用调色板/图标/复选框/高度自适应全套逻辑</summary>
        private static UISmartInputField host;

        public const float todoListOriHeight = 324;
        public const float todoListX = 1140;
        public const float todoListY = 300;
        public const float todoListXLowRes = 340;

        /// <summary>
        /// 一个星系的备忘录：纯文本 + 与文本逐字符平行的笔刷索引数组。
        /// 索引语义（对齐原生 UISmartInputField / InputField）：
        ///   &lt; 0        无色，不写富文本标签，由 Text 组件的基础色决定观感
        ///   色号 * 4 + alpha档   指向 Configs.builtin.colorPaletteText（16 色 × 4 档 = 64）
        /// 富文本标签只在渲染时由 InputField.CalculateColorText 临时展开，绝不进入 content。
        /// </summary>
        public struct TodoEntry
        {
            public string content;
            public short[] colors;
        }

        /// <summary>key = 星系 index（StarData.index），每个星系一份，无更细颗粒度</summary>
        public static Dictionary<int, TodoEntry> todos = new Dictionary<int, TodoEntry>();

        private static int _loadingStarIndex = -1;   // 正在编辑的文本实际属于哪个星系（不依赖实时 curStar）


        public static void InitData()
        {
            todos = new Dictionary<int, TodoEntry>();
            _loadingStarIndex = -1;
        }

        public static void InitUI()
        {
            Transform parentTrans = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy").transform;
            GameObject oriTodoList = GameObject.Find("UI Root/Overlay Canvas/In Game/Planet & Star Details/planet-detail-ui/memo-group/to-do-list");
            if(oriTodoList != null)
            {
                todoListObj = GameObject.Instantiate(oriTodoList);
                todoListObj.transform.SetParent(parentTrans);
                todoListObj.transform.localScale = Vector3.one;
                todoListObj.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
                todoListObj.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0);
                todoListObj.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
                todoListObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(todoListX, todoListY, 0);
                todoListObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 358);
                if (StarAssembly.lowUIResolution)
                    todoListObj.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(todoListXLowRes, todoListY, 0);

                contentRT = todoListObj.transform.Find("editor/scroll-view/viewport/content").GetComponent<RectTransform>();
                todoListInputObj = todoListObj.transform.Find("editor/scroll-view/viewport/content/InputField").gameObject;
                todoListInputObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, todoListOriHeight);
                inputRT = todoListInputObj.GetComponent<RectTransform>();
                todoListInput = todoListInputObj.GetComponent<InputField>();
                todoListText = todoListObj.transform.Find("editor/scroll-view/viewport/content/InputField/Text (Legacy)").GetComponent<Text>();

                host = todoListObj.GetComponent<UISmartInputField>();

                // Instantiate 会把源对象运行期的 ManualBehaviour 标志位（created/inited/active 等实例自动属性）
                // 一起复制过来。若源对象已被宿主 _Create 过，克隆体的 created == true，
                // 而 _Create() 的守卫是 if(!created && !destroyed)，会静默跳过、_OnCreate 永不执行。
                // 这些属性的 setter 是 private，只能靠 _Close/_Free 复位 inited/active，created 用反射兜底。
                ResetHostFlags(host);

                // 颜色链的前提：m_CharColorsMapping 是 InputField 上的 public 字段。
                // 原生由 UISmartInputField._OnCreate 赋值，这里显式兜一份，不依赖 _OnCreate 是否跑到。
                if (todoListInput != null)
                    todoListInput.m_CharColorsMapping = Configs.builtin.colorPaletteText;

                if (host != null)
                {
                    // 高度上下限交给宿主：HandleRectSize 用 textMinHeight/textMaxHeight 夹 InputField 的可见高度，
                    // content 则用不夹的全文高度（CalculateSize 的 z 分量），滚动条就是这么来的。
                    // prefab 默认是 60/60，不抬高的话正文会被夹死只显示两三行。
                    host.textMinHeight = (int)todoListOriHeight;
                    host.textMaxHeight = (int)todoListOriHeight;
                    host._Create();           // _Create 内部会把 gameObject 置 inactive，随后由 _Open 打开
                    host._Init(null);         // _OnInit() 恒返回 true，因此一定会走到 _OnRegEvent

                    // 弹窗位置只通过原生留的公开委托钩子定制，不去抢宿主 _OnRegEvent 注册的 onClick。
                    // 图标窗同理：host.calcIconPickerPopupPos = CalcIconPickerPos;
                    host.calcColorPickerPopupPos = CalcColorPickerPos;
                    host.calcIconPickerPopupPos = CalcIconPickerPos;
                }

                // 注意：这里绝不能 RemoveAllListeners()。
                // UnityEvent 分两份：m_PersistentCalls（Inspector 配置，Instantiate 会复制）与
                // m_Calls（运行时 AddListener，Instantiate 不会复制）。宿主 _Init -> _OnRegEvent 注册的
                // onEndEdit 属于后者，克隆体上原本没有；而克隆带过来的持久监听正是原生宿主的
                // OnInputFieldEndEdit（负责关调色板/图标浮动窗），清掉它就等于砍掉写回与收尾逻辑。
                if (todoListInput != null)
                    todoListInput.onEndEdit.AddListener(OnEndEdit);

                if (host != null) host._Open();
                else if (todoListObj != null) todoListObj.SetActive(true);

                if (todoListInput != null && !todoListInput.useColorText)
                    Debug.LogWarning("[MMS] todoList InputField.useColorText == false");

                todoListObj.SetActive(false);

                // 立刻灌一次内容：既保证初始显示正确，也让 _loadingStarIndex 有值，
                // 否则第一次编辑时 SaveCurrent 找不到归属星系。
                RefreshTodoListUI();
            }
        }


        /// <summary>
        /// 把克隆体上的原生宿主拨回"未初始化"状态，好让我们自己走一遍 _Create/_Init/_Open。
        /// inited/active 由 _Free/_Close 复位（顺带清掉从源对象复制来的运行时监听器）；
        /// created/destroyed 没有对外入口，只能反射写 private setter，失败也不致命——
        /// 那时 _Create 会被守卫跳过，但 m_CharColorsMapping 等关键项我们已在外部显式补齐。
        /// </summary>
        private static void ResetHostFlags(UISmartInputField h)
        {
            if (h == null) return;
            try
            {
                if (h.active) h._Close();
                if (h.inited) h._Free();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MMS] todoList host _Free failed: " + e.Message);
            }
            Type t = typeof(ManualBehaviour);
            while (t != null)
            {
                PropertyInfo pi = t.GetProperty("created", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi != null && pi.CanWrite) { pi.SetValue(h, false, null); break; }
                t = t.BaseType;
            }
        }

        /// <summary>
        /// 把输入框当前内容写回字典。文本走 onEndEdit 回调参数（而不是实时读 InputField.text），
        /// 星系归属走 _loadingStarIndex（而不是实时读 curStar）：
        /// ESC 取消时 Unity 会先把 m_Text 回滚成 m_OriginalText 再发 onEndEdit，
        /// 而切星系时 curStar 可能已经变了，两个实时值都会把内容写错地方。
        /// 颜色必须在 CalculateColorText 之后取，否则 m_CharColors 与文本不同步。
        /// </summary>
        public static void SaveCurrent(string text)
        {
            if (todoListInput == null) return;
            // 正常路径下 _loadingStarIndex 由 LoadIntoEditor 赋值；
            // 若面板打开后从未刷新过（第一次编辑、或走的是不触发刷新的路径），退回实时 curStar，
            // 宁可退化到旧行为也不能把玩家的输入丢掉。
            int starIndex = _loadingStarIndex;
            if (starIndex < 0)
                starIndex = (MoreMegaStructure.curStar != null) ? MoreMegaStructure.curStar.index : -1;
            if (starIndex < 0) return;

            TodoEntry e;
            todos.TryGetValue(starIndex, out e);
            e.content = (text ?? string.Empty);
            e.colors = null;
            todoListInput.CalculateColorText(false);
            todoListInput.GetCharColorValues(ref e.colors);
            if (e.colors != null && e.colors.Length > e.content.Length)
                e.colors = TrimColors(e.colors, e.content.Length);   // m_CharColors 是 2 的幂，尾部对渲染无意义
            todos[starIndex] = e;
        }

        public static void OnEndEdit(string text)
        {
            SaveCurrent(text);
        }

        /// <summary>把某个星系的备忘录（含颜色）灌进输入框。顺序不可颠倒，见方法内注释。</summary>
        public static void LoadIntoEditor(int starIndex)
        {
            if (todoListInput == null) return;
            _loadingStarIndex = starIndex;

            TodoEntry e;
            string content = todos.TryGetValue(starIndex, out e) ? (e.content ?? string.Empty) : string.Empty;
            short[] colors = (e.colors != null && e.colors.Length > content.Length)
                ? TrimColors(e.colors, content.Length) : e.colors;

            // 必须先 SetCharColorValues 再 SetTextWithoutNotify：
            // SetText 内部会调 CalculateColorText，此时读到的才是新数组；
            // 反过来则 m_Text 相等时 SetText 会整段早退，颜色改完没人重算，显示会残留上一次的。
            todoListInput.SetCharColorValues(colors);
            todoListInput.SetTextWithoutNotify(content);
            host.HandleRectSize();
            todoListInput.CalculateColorText(false);
            todoListInput.ForceLabelUpdate();
        }

        public static void RefreshTodoListUI()
        {
            if (MoreMegaStructure.curStar == null) return;
            // 正在编辑且切到了别的星系：先把旧内容落到它自己的槽位，不能被新星系内容覆盖掉
            if (todoListInput != null && todoListInput.isFocused && _loadingStarIndex != MoreMegaStructure.curStar.index)
                SaveCurrent(todoListInput.text);

            LoadIntoEditor(MoreMegaStructure.curStar.index);
        }

        public static void LateUpdate()
        {
            if (host == null) return;
            // ManualBehaviour._Update/_LateUpdate 自带 active 与 activeInHierarchy 双重守卫，直接调即可。
            // 宿主的 _OnUpdate -> UpdateInputField -> HandleRectSize 负责高度与滚动，
            // _OnLateUpdate 负责 delayDeselectInputField（Ctrl+点图标后的延迟失焦）。
            host._Update();
            host._LateUpdate();
        }


        
        private static Vector2 CalcColorPickerPos()
        {
            float uiWidth = DSPGame.globalOption.resolution.width / DSPGame.globalOption.resolution.height * DSPGame.globalOption.uiLayoutHeight;
            float uiHeight = DSPGame.globalOption.uiLayoutHeight;
            // 拿不到引用就退回原生 fallback，绝不能返回垃圾值把窗甩出可视区
            float fbX = -uiWidth / 2 + todoListX - 430; 
            float fbY = -uiHeight / 2 + todoListY + 112;
            if(StarAssembly.lowUIResolution)
                fbX = -uiWidth / 2 + todoListXLowRes - 400;

            return new Vector2(fbX, fbY);
        }

        private static Vector2 CalcIconPickerPos()
        {
            float uiWidth = DSPGame.globalOption.resolution.width / DSPGame.globalOption.resolution.height * DSPGame.globalOption.uiLayoutHeight;
            float uiHeight = DSPGame.globalOption.uiLayoutHeight;
            // 拿不到引用就退回原生 fallback，绝不能返回垃圾值把窗甩出可视区
            float fbX = -uiWidth / 2 + todoListX - 630;
            float fbY = -uiHeight / 2 + todoListY + 372;
            if (StarAssembly.lowUIResolution)
                fbX = -uiWidth / 2 + todoListXLowRes + 125;

            return new Vector2(fbX, fbY);
        }


        /// <summary>复刻 UISmartInputField.GetBrushColor（私有），逻辑：有选区 -2，否则取光标前一个字符的索引</summary>
        private static short GetBrushColor()
        {
            if (todoListInput == null) return -1;
            if (todoListInput.selectionAnchorPosition != todoListInput.selectionFocusPosition)
                return -2;
            if (todoListInput.m_BrushColor >= -1)
                return todoListInput.m_BrushColor;
            short[] cc = todoListInput.m_CharColors;
            int caret = todoListInput.caretPosition;
            if (cc != null && caret > 0 && caret <= cc.Length)
            {
                short v = cc[caret - 1];
                return (v < -1) ? (short)-1 : v;
            }
            return -1;
        }


        private static short[] TrimColors(short[] src, int len)
        {
            if (src == null || src.Length <= len) return src;
            short[] dst = new short[Math.Max(1, len)];
            Array.Copy(src, dst, len);
            return dst;
        }

        private static short[] ReadShortArray(BinaryReader r)
        {
            int n = r.ReadInt32();
            if (n <= 0) return null;
            if (n > 65536) n = 65536;              // 坏档兜底：宁可信其小，避免按垃圾长度分配
            short[] a = new short[n];
            for (int i = 0; i < n; i++) a[i] = r.ReadInt16();
            return a;
        }

        private static void WriteShortArray(BinaryWriter w, short[] a)
        {
            int n = (a == null) ? 0 : a.Length;
            w.Write(n);
            for (int i = 0; i < n; i++) w.Write(a[i]);
        }

        public static void Import(BinaryReader r)
        {
            InitData();
            if (MoreMegaStructure.savedModVersion >= 204)
            {
                int totalLen = r.ReadInt32();
                if (totalLen < 0) totalLen = 0;
                if (totalLen > GameMain.galaxy.starCount) totalLen = GameMain.galaxy.starCount;   // 坏档兜底，绝不按垃圾长度狂读
                for (int i = 0; i < totalLen; i++)
                {
                    int starIndex = r.ReadInt32();
                    string str = r.ReadString();
                    short[] colors = ReadShortArray(r);
                    if (starIndex >= 0 && starIndex < GameMain.galaxy.starCount)
                    {
                        TodoEntry e = new TodoEntry();
                        e.content = str;
                        e.colors = colors;
                        todos[starIndex] = e;
                    }
                }
            }
        }

        public static void Export(BinaryWriter w)
        {
            // 玩家改完颜色还没失焦就存档：onEndEdit 不会触发，这里主动把输入框内容落到它自己的星系槽位
            if (todoListInput != null && todoListInput.isFocused && _loadingStarIndex >= 0)
                SaveCurrent(todoListInput.text);

            int totalLen = 0;
            foreach (var todo in todos)
            {
                if (todo.Value.content != null && todo.Value.content.Length > 0)
                    totalLen++;
            }
            w.Write(totalLen);
            foreach (var todo in todos)
            {
                if (todo.Value.content != null && todo.Value.content.Length > 0)
                {
                    w.Write(todo.Key);
                    w.Write(todo.Value.content);
                    WriteShortArray(w, todo.Value.colors);
                }
            }
        }

        public static void IntoOtherSave()
        {
            InitData();
        }
    }
}
