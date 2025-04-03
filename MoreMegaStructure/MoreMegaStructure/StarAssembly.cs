using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using xiaoye97;
using static UnityEngine.EventSystems.EventTrigger;

namespace MoreMegaStructure
{
    public class StarAssembly
    {
        // 以下为需要存档的数据
        public static List<List<int>> recipeIds = new List<List<int>>(); // 存储所有star（后面省略）的所有recipes
        public static List<List<double>> weights = new List<List<double>>(); // 存储不同recipe的占有能量百分比
        public static List<List<double>> progress = new List<List<double>>(); // 存储不同recipe的生产进度
        public static List<List<double>> incProgress = new List<List<double>>(); // 存储不同recipe的增产生产进度
        public static List<int> specProgress = new List<int>(); // 存储组装厂特化进度
        public static List<int> curSpecType = new List<int>(); // 当前组装厂特化类型
        public static List<int> inProgressSpecType = new List<int>(); // 正处在特化进程中的类型
        public static List<int> satisfiedSpecType = new List<int>(); // 当前正满足要求的特化类型
        public static List<List<int>> productSpeedRequest = new List<List<int>>(); // 手动期望的（要求的）生产速率

        // 以下为不需要存档的数据，在载入时重置或者重新计算
        public static Dictionary<int, List<List<int>>> items = new Dictionary<int, List<List<int>>>(); // 存储recipe的原材料的Id
        public static Dictionary<int, List<List<int>>> products = new Dictionary<int, List<List<int>>>(); // 存储recipe的产物的Id
        public static Dictionary<int, List<List<int>>> itemCounts = new Dictionary<int, List<List<int>>>(); // 存储recipe的原材料的需求数量
        public static Dictionary<int, List<List<int>>> productCounts = new Dictionary<int, List<List<int>>>(); // 存储recipe的产物的产出数量
        public static Dictionary<int, List<int>> timeSpend = new Dictionary<int, List<int>>(); // 存储recipe的所需时间
        public static Dictionary<int, Dictionary<int, int>> productStorage = new Dictionary<int, Dictionary<int, int>>(); // 存储产物已暂时堆积在巨构中的数量（可供相同星际组装厂的其他需要此产物作为原材料的配方取用），不区分slot只按照产物Id存储。不进行存档，读档后重置。
        // 上述productStorage项会存在：如果反复疯狂更换recipe会一直增加字典项，可能拖慢速度，但是重进游戏后冗余key会自动清除，因此暂时不做游戏内清理
        public static Dictionary<int, Dictionary<int,int>> productStorageInc = new Dictionary<int, Dictionary<int, int>>(); // 存储产物的增产点数
        public static Dictionary<int, List<int>> specBuffLevel = new Dictionary<int, List<int>>(); // 星际组装厂特化后，配方能触发加成
        public static List<int> currentStarIncs = new List<int>();
        public static int blueBuffByTCFV = 0;
        public static int r002ByTCFV = 0;
        public static int r106ByTCFV = 0;
        public static int r208ByTCFV = 0; // 狄拉克只产出反物质

        //public static int currentStarIndex = 0; // 弃用，使用MoreMegaStructure.curStar.index
        public static int currentRecipeSlot = 0; // 选择recipe时暂存所选定的是第几个recipe栏位

        public static int slotCount = 16;

        public static GameObject GigaFactoryUIObj = null;
        public static GameObject specializeObj = null;
        public static GameObject showHideButtonObj = null;
        public static Text showHideBtnText;
        public static GameObject showHideLimitButtonObj = null;
        public static Text doubleAllBtnText;
        public static Text halveAllBtnText;

        public static List<Image> recipeIcons = new List<Image>();
        public static List<Image> incIcons = new List<Image>();
        public static List<GameObject> recipeSelectTips = new List<GameObject>();
        public static List<Text> produceSpeedTxts = new List<Text>();
        public static List<GameObject> sliderObjs = new List<GameObject>();
        public static List<GameObject> speedTextObjs = new List<GameObject>();
        public static List<GameObject> removeRecipeBtnObjs = new List<GameObject>();
        public static List<GameObject> setLimitObjs = new List<GameObject>();
        public static List<Slider> sliders = new List<Slider>();
        public static List<InputField> limitInputs = new List<InputField>();
        public static List<Text> weightTxts = new List<Text>();
        public static List<Text> storageTxts = new List<Text>();
        public static List<Text> recipePickerTxts = new List<Text>();
        public static List<Text> specializeTitleTxts = new List<Text>();
        public static List<Text> specializeStateTxts = new List<Text>();
        public static List<Text> tipButtonTxts = new List<Text>();
        public static List<UIButton> recipeCircleUIBtns = new List<UIButton>();
        public static Sprite noRecipeSelectedSprit = null;

        public static bool lowUIResolution = false;

        public static bool lockSliderListener = false;

        public static bool showingLimit = false;

        // 以下是固定参数
        public static int tickEnergyForFullSpeed = 20000; // 每相当于1.0倍速的产量需要的tick能量。注意这个会在进入游戏后受到MoreMegaStructure.IASpdFactor.Value作为反系数修改
        public static int matrixTimeSpendRatio = 100; // 用星际组装厂生产矩阵的速度修正倍率，这是为了不让该巨构部分替代科学枢纽
        public static int recipeType1213TimeSpendRatio = 20; // 用星际组装厂生产创世之书特定配方的速度修正
        public static List<long> speedNeededToUnlockSlot = new List<long> { 0, 0, 0, 0, 0, 10, 20, 30, 50, 100, 200, 500, 1000, 5000, 10000, 100000 }; // 星际组装厂解锁对应slot所需速度倍率
        public static Color UITextOrange = new Color(1f, 0.705f, 0f, 0.745f);
        public static Color UITextBlue = new Color(0.382f, 0.845f, 1f, 0.784f);
        public static Color UITextGreen = new Color(0.225f, 1f, 0.179f, 0.744f);
        public static Color UITextRed = new Color(1.0f, 0.12f, 0.12f, 0.744f);
        public static Color UITextGray = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        public static List<int> specializeRequirements = new List<int> { 0, 5, 3, 3, 5, 5 };
        public static int specializeTimeNeed = 36000; // 特化进程需要的基础时间，随着星际组装厂总能量水平的增加，特化完成需要的时间可能成倍增加 36000

        public static void InitAll()
        {
            InitUI();
            ResetAndInitArchiveData();
        }

        public static void InitUI()
        {
            if (GigaFactoryUIObj == null)
            {
                Transform parentTrans = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy").transform;
                GigaFactoryUIObj = new GameObject("GigaFactory");
                GigaFactoryUIObj.transform.SetParent(parentTrans);
                GigaFactoryUIObj.transform.localScale = new Vector3(1, 1, 1);
                GigaFactoryUIObj.transform.localPosition = new Vector3(300, -Utils.UIActualHeight + 190, 0);
                GigaFactoryUIObj.SetActive(false);

                // 显示/隐藏按钮
                GameObject addNewLayerButton = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy/layers/buttons-group/buttons/add-button");
                showHideButtonObj = GameObject.Instantiate(addNewLayerButton, parentTrans);
                showHideButtonObj.SetActive(true);
                showHideButtonObj.name = "show-hide"; //名字
                showHideButtonObj.transform.localPosition = new Vector3(320, -Utils.UIActualHeight + 40, 0); //位置
                showHideButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 24); //按钮大小
                showHideBtnText = showHideButtonObj.transform.Find("Text").gameObject.GetComponent<Text>();
                showHideBtnText.text = "显示/隐藏星际组装厂配置".Translate();
                Button showHideButton = showHideButtonObj.GetComponent<Button>();
                showHideButton.interactable = true;
                showHideButton.onClick.RemoveAllListeners();
                showHideButton.onClick.AddListener(() => { ShowHideUI(); });

                //GameObject showHideLimitButtonObj = GameObject.Instantiate(addNewLayerButton, GigaFactoryUIObj.transform);
                //showHideLimitButtonObj.SetActive(true);
                //showHideLimitButtonObj.name = "show-hide"; //名字
                //showHideLimitButtonObj.transform.localPosition = new Vector3(280, -150, 0); //位置
                //showHideLimitButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 24); //按钮大小
                //showHideLimitBtnText = showHideLimitButtonObj.transform.Find("Text").gameObject.GetComponent<Text>();
                //showHideLimitBtnText.text = "配置最大生产速度限制".Translate();
                //Button showHideLimitButton = showHideLimitButtonObj.GetComponent<Button>();
                //showHideLimitButton.interactable = true;
                //showHideLimitButton.onClick.RemoveAllListeners();
                //showHideLimitButton.onClick.AddListener(() => { RefreshLimitUI(); });

                // 加倍全部速度预期 按钮
                GameObject DoubleAllSpeedButtonObj = GameObject.Instantiate(addNewLayerButton, GigaFactoryUIObj.transform);
                DoubleAllSpeedButtonObj.SetActive(true);
                DoubleAllSpeedButtonObj.name = "double-all"; //名字
                DoubleAllSpeedButtonObj.transform.localPosition = new Vector3(240, -150, 0); //位置
                DoubleAllSpeedButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 24); //按钮大小
                doubleAllBtnText = DoubleAllSpeedButtonObj.transform.Find("Text").gameObject.GetComponent<Text>();
                doubleAllBtnText.text = "加倍所有速度设置".Translate();
                Button DoubleAllSpeedButton = DoubleAllSpeedButtonObj.GetComponent<Button>();
                DoubleAllSpeedButton.interactable = true;
                DoubleAllSpeedButton.onClick.RemoveAllListeners();
                DoubleAllSpeedButton.onClick.AddListener(() => { DoubleAllSpeedRequest(); });

                // 减半全部速度预期 按钮
                GameObject HalveAllSpeedButtonObj = GameObject.Instantiate(addNewLayerButton, GigaFactoryUIObj.transform);
                HalveAllSpeedButtonObj.SetActive(true);
                HalveAllSpeedButtonObj.name = "halve-all"; //名字
                HalveAllSpeedButtonObj.transform.localPosition = new Vector3(400, -150, 0); //位置
                HalveAllSpeedButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 24); //按钮大小
                halveAllBtnText = HalveAllSpeedButtonObj.transform.Find("Text").gameObject.GetComponent<Text>();
                halveAllBtnText.text = "减半所有速度设置".Translate();
                Button HalveAllSpeedButton = HalveAllSpeedButtonObj.GetComponent<Button>();
                HalveAllSpeedButton.interactable = true;
                HalveAllSpeedButton.onClick.RemoveAllListeners();
                HalveAllSpeedButton.onClick.AddListener(() => { HalveAllSpeedRequest(); });

                GameObject backObj = new GameObject("back");
                backObj.transform.parent = GigaFactoryUIObj.transform;
                backObj.transform.localScale = new Vector3(1, 1, 1);
                backObj.transform.localPosition = new Vector3(660, 180, 0);
                backObj.AddComponent<Image>();
                backObj.GetComponent<Image>().color = new Color(0.106f, 0.180f, 0.228f, 0.88f);
                backObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1280, 650);

                GameObject oriSelectObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/offwork");
                GameObject oriSliderObj = GameObject.Find("UI Root/Overlay Canvas/In Game/FPS Stats/priority-bar/slider-2");
                GameObject oriIncIconObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Station Window/storage-box-0(Clone)/storage-icon/inc-3");
                if (oriIncIconObj == null)
                    oriIncIconObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Station Window/Station-scroll(Clone)/Viewport/pane/storage-box-0(Clone)/storage-icon/inc-3");
                GameObject oriRemoveRecipeObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/circle-back/stop-btn");
                GameObject oriRemoveRecipeXObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/circle-back/stop-btn/x");
                GameObject oriInputFieldObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Blueprint Browser/inspector-group/Scroll View/Viewport/Content/group-1/input-short-text");
                if(oriInputFieldObj == null)
                    oriInputFieldObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Blueprint Browser/inspector-group/BP-panel-scroll(Clone)/Viewport/pane/group-1/input-short-text");
                if (oriInputFieldObj == null)
                    Utils.Log("Error when init oriInputField because some other mods has changed the Blueprint Browser UI. Please check if you've install the BluePrintTweaks and then contant jinxOAO.");

                for (int i = 0; i < slotCount; i++)
                {
                    GameObject slotObj = new GameObject("slot" + i.ToString());
                    slotObj.transform.SetParent(GigaFactoryUIObj.transform);
                    slotObj.transform.localScale = new Vector3(1, 1, 1);
                    slotObj.transform.localPosition = new Vector3((i / 8) * 400, (i % 8) * 80, 0);

                    GameObject recipeSelectionObj = GameObject.Instantiate(oriSelectObj, slotObj.transform);
                    recipeSelectionObj.SetActive(true);
                    recipeSelectionObj.transform.Find("tip-group/paste-button").gameObject.SetActive(false);
                    recipePickerTxts.Add(recipeSelectionObj.transform.Find("tip-group/tip-text").gameObject.GetComponent<Text>());

                    GameObject circleButtonObj = recipeSelectionObj.transform.Find("circle").gameObject;
                    recipeIcons.Add(circleButtonObj.GetComponent<Image>());
                    noRecipeSelectedSprit = circleButtonObj.GetComponent<Image>().sprite;
                    recipeSelectTips.Add(recipeSelectionObj.transform.Find("tip-group").gameObject);
                    Button circleButton = circleButtonObj.GetComponent<Button>();
                    circleButton.onClick.RemoveAllListeners();
                    UIButton recipeUIBtn = circleButtonObj.GetComponent<UIButton>();
                    recipeUIBtn.tips.corner = 3;
                    if (i == 0)
                    {
                        recipeUIBtn.tips.itemId = 9500;
                    }
                    recipeCircleUIBtns.Add(recipeUIBtn);

                    GameObject removeButtonObj = GameObject.Instantiate(circleButtonObj, recipeSelectionObj.transform);
                    removeButtonObj.name = "stop-btn";
                    removeButtonObj.SetActive(true);
                    removeButtonObj.transform.localPosition = new Vector3(78, -28, 0);
                    removeButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(18, 18);
                    removeButtonObj.GetComponent<Image>().sprite = oriRemoveRecipeObj.GetComponent<Image>().sprite;
                    removeButtonObj.GetComponent<Image>().material = oriRemoveRecipeObj.GetComponent<Image>().material;
                    removeButtonObj.GetComponent<Image>().color = new Color(0.988f, 0.588f, 0.368f, 0.141f);
                    GameObject removeButtonXObj = GameObject.Instantiate(oriRemoveRecipeXObj, removeButtonObj.transform);
                    Button removeButton = removeButtonObj.GetComponent<Button>();
                    removeButton.onClick.RemoveAllListeners();
                    removeRecipeBtnObjs.Add(removeButtonObj);
                    UIButton removeBtnUIBtn = removeButtonObj.GetComponent<UIButton>();
                    for (int t = 0; t < removeBtnUIBtn.transitions.Length; t++)
                    {
                        removeBtnUIBtn.transitions[t].normalColor = new Color(0.988f, 0.588f, 0.368f, 0.141f);
                        removeBtnUIBtn.transitions[t].pressedColor = new Color(0.988f, 0.588f, 0.368f, 0.041f);
                        removeBtnUIBtn.transitions[t].mouseoverColor = new Color(0.988f, 0.588f, 0.368f, 0.201f);
                        removeBtnUIBtn.transitions[t].mouseoverSize = 1.1f;
                    }

                    GameObject produceSpeedTxtObj = GameObject.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/circle-back/cnt-text"), slotObj.transform);
                    produceSpeedTxtObj.transform.localPosition = new Vector3(110, -136);
                    produceSpeedTxts.Add(produceSpeedTxtObj.GetComponent<Text>());
                    produceSpeedTxtObj.GetComponent<Text>().text = "理论最大速度".Translate() + " --/min";
                    produceSpeedTxtObj.GetComponent<Text>().alignment = TextAnchor.LowerLeft;
                    produceSpeedTxtObj.GetComponent<Text>().supportRichText = true;
                    produceSpeedTxtObj.GetComponent<Text>().color = new Color(0.992f, 0.588f, 0.3686f, 0.8384f);
                    produceSpeedTxtObj.GetComponent<Text>().lineSpacing = 0.7f;
                    speedTextObjs.Add(produceSpeedTxtObj);

                    GameObject incIconObj = GameObject.Instantiate(oriIncIconObj, slotObj.transform);
                    incIconObj.transform.localPosition = new Vector3(60, -65);
                    incIconObj.transform.localScale = new Vector3(1, 1, 1);
                    //incIconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
                    incIconObj.SetActive(false);
                    incIconObj.GetComponent<Image>().enabled = true;
                    incIcons.Add(incIconObj.GetComponent<Image>());

                    GameObject sliderObj = GameObject.Instantiate(oriSliderObj, slotObj.transform);
                    sliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 20);
                    sliderObj.transform.localPosition = new Vector3(150, -110, 0); // 150, 100, 0
                    sliderObj.SetActive(false);
                    sliderObjs.Add(sliderObj);
                    Slider slider = sliderObj.GetComponent<Slider>();
                    slider.minValue = 0;
                    slider.maxValue = 100;
                    slider.value = 0;
                    slider.onValueChanged.RemoveAllListeners();
                    sliders.Add(slider);

                    GameObject weightTxtObj = GameObject.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/circle-back/cnt-text"), sliderObj.transform);
                    //weightTxtObj.transform.localPosition = new Vector3(150, -12, 0);
                    weightTxtObj.transform.localPosition = new Vector3(30, 12, 0);
                    weightTxts.Add(weightTxtObj.GetComponent<Text>());
                    weightTxtObj.GetComponent<Text>().fontSize = 16;
                    weightTxtObj.GetComponent<Text>().text = "";
                    weightTxtObj.GetComponent<Text>().alignment = TextAnchor.LowerLeft;

                    GameObject storageTxtObj = GameObject.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/circle-back/cnt-text"), slotObj.transform);
                    storageTxtObj.transform.localPosition = new Vector3(330, -122, 0);
                    storageTxts.Add(storageTxtObj.GetComponent<Text>());
                    storageTxtObj.GetComponent<Text>().fontSize = 16;
                    storageTxtObj.GetComponent<Text>().lineSpacing = 0.95f;
                    storageTxtObj.GetComponent<Text>().text = "";
                    storageTxtObj.GetComponent<Text>().alignment = TextAnchor.LowerLeft;

                    GameObject spdLimitObj = new GameObject();
                    spdLimitObj.transform.SetParent(slotObj.transform);
                    spdLimitObj.name = "speed-limit";
                    spdLimitObj.transform.localPosition = new Vector3(120, -98, 0);
                    spdLimitObj.transform.localScale = new Vector3(1, 1, 1);
                    setLimitObjs.Add(spdLimitObj);
                    GameObject spdLimitTitleObj = GameObject.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/circle-back/cnt-text"), spdLimitObj.transform);
                    spdLimitTitleObj.name = "title";
                    spdLimitTitleObj.transform.localPosition = new Vector3(60, 0, 0);
                    spdLimitTitleObj.GetComponent<Text>().text = "最大生产速度限制".Translate();
                    spdLimitTitleObj.GetComponent<Text>().fontSize = 16;
                    spdLimitTitleObj.GetComponent<Text>().alignment = TextAnchor.LowerLeft;
                    
                    GameObject spdLimitInputObj = GameObject.Instantiate(oriInputFieldObj, spdLimitObj.transform);
                    spdLimitInputObj.name = "value-input";
                    spdLimitInputObj.transform.localPosition = new Vector3(30, 0, 0);
                    spdLimitInputObj.GetComponent<UIButton>().tips.tipTitle = "最大生产速度限制题目".Translate();
                    spdLimitInputObj.GetComponent<UIButton>().tips.tipText = "最大生产速度限制描述".Translate();
                    spdLimitInputObj.GetComponent<UIButton>().tips.width = 300;
                    spdLimitInputObj.GetComponent<UIButton>().tips.offset = new Vector2(450, 130);
                    spdLimitInputObj.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 20);
                    spdLimitInputObj.GetComponent<InputField>().contentType = InputField.ContentType.IntegerNumber;
                    spdLimitInputObj.GetComponent<InputField>().textComponent.fontSize = 12;
                    spdLimitInputObj.GetComponent<InputField>().characterLimit = 9;
                    spdLimitInputObj.GetComponent<InputField>().onEndEdit.RemoveAllListeners();
                    string istr = i.ToString();
                    spdLimitInputObj.GetComponent<InputField>().onEndEdit.AddListener((x) => { SetProductSpeedRequest(Convert.ToInt32(istr), x); });
                    limitInputs.Add(spdLimitInputObj.GetComponent<InputField>());
                    spdLimitInputObj.transform.Find("value-text").GetComponent<Text>().color = Color.white;
                    spdLimitObj.SetActive(false);

                    // 规范化速度设置按钮 

                    GameObject normalizeBtnObj1 = GameObject.Instantiate(circleButtonObj, spdLimitObj.transform);
                    normalizeBtnObj1.name = "normalizeSpeed1";
                    normalizeBtnObj1.transform.localPosition = new Vector3(119, -10, 0);
                    normalizeBtnObj1.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
                    normalizeBtnObj1.GetComponent<Image>().sprite = Resources.Load<Sprite>("ui/textures/sprites/icons/speed-icon");
                    normalizeBtnObj1.GetComponent<Button>().onClick.RemoveAllListeners();
                    int slotNum = i;
                    normalizeBtnObj1.GetComponent<Button>().onClick.AddListener(() => { NormalizeSetValue(slotNum, false); });
                    normalizeBtnObj1.GetComponent<UIButton>().transitions[0].mouseoverSize = 1;
                    normalizeBtnObj1.GetComponent<UIButton>().tips.tipTitle = "组装厂速度规范化1标题".Translate();
                    normalizeBtnObj1.GetComponent<UIButton>().tips.tipText = "组装厂速度规范化1描述".Translate();
                    normalizeBtnObj1.GetComponent<UIButton>().tips.corner = 9;
                    normalizeBtnObj1.GetComponent<UIButton>().tips.width = 355;

                    GameObject normalizeBtnObj2 = GameObject.Instantiate(circleButtonObj, spdLimitObj.transform);
                    normalizeBtnObj2.name = "normalizeSpeed2";
                    normalizeBtnObj2.transform.localPosition = new Vector3(145, -10, 0);
                    normalizeBtnObj2.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
                    normalizeBtnObj2.GetComponent<Image>().sprite = Resources.Load<Sprite>("ui/textures/sprites/icons/speed-icon");
                    normalizeBtnObj2.GetComponent<Button>().onClick.RemoveAllListeners();
                    normalizeBtnObj2.GetComponent<Button>().onClick.AddListener(() => { NormalizeSetValue(slotNum, true); });
                    normalizeBtnObj2.GetComponent<UIButton>().transitions[0].mouseoverSize = 1;
                    normalizeBtnObj2.GetComponent<UIButton>().tips.tipTitle = "组装厂速度规范化2标题".Translate();
                    normalizeBtnObj2.GetComponent<UIButton>().tips.tipText = "组装厂速度规范化2描述".Translate();
                    normalizeBtnObj2.GetComponent<UIButton>().tips.corner = 9;
                    normalizeBtnObj2.GetComponent<UIButton>().tips.width = 355;
                    GameObject normalizeBtn2IncObj = new GameObject("inc-icon");
                    normalizeBtn2IncObj.transform.SetParent(normalizeBtnObj2.transform, false);
                    normalizeBtn2IncObj.transform.localPosition = new Vector3(0, -9, 0);
                    normalizeBtn2IncObj.transform.localScale = Vector3.one;
                    Image normalizeBtn2IncImg = normalizeBtn2IncObj.AddComponent<Image>();
                    normalizeBtn2IncImg.sprite = Resources.Load<Sprite>("entities/models/cargo/inc-4");
                    normalizeBtn2IncObj.GetComponent<RectTransform>().sizeDelta = new Vector2(12, 12);
                    normalizeBtnObj2.GetComponent<UIButton>().transitions = normalizeBtnObj2.GetComponent<UIButton>().transitions.AddItem(normalizeBtnObj2.GetComponent<UIButton>().transitions[0].Copy()).ToArray();
                    normalizeBtnObj2.GetComponent<UIButton>().transitions[1].target = normalizeBtn2IncImg;

                    if (i == 0)
                    {
                        // circleButton.onClick.AddListener(() => { OnRecipeSelectClick(0); });
                        // slider.onValueChanged.AddListener((x) => { OnSliderValueChange(0, x); });
                        slider.interactable = false;
                    }
                    else if (i == 1)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(1); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(1); });
                        //slider.onValueChanged.AddListener((x) => { OnSliderValueChange(1, x); });
                    }
                    else if (i == 2)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(2); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(2); });
                        //slider.onValueChanged.AddListener((x) => { OnSliderValueChange(2, x); });
                    }
                    else if (i == 3)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(3); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(3); });
                        //slider.onValueChanged.AddListener((x) => { OnSliderValueChange(3, x); });
                    }
                    else if (i == 4)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(4); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(4); });
                        //slider.onValueChanged.AddListener((x) => { OnSliderValueChange(4, x); });
                    }
                    else if (i == 5)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(5); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(5); });
                        //slider.onValueChanged.AddListener((x) => { OnSliderValueChange(5, x); });
                    }
                    else if (i == 6)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(6); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(6); });
                        //slider.onValueChanged.AddListener((x) => { OnSliderValueChange(6, x); });
                    }
                    else if (i == 7)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(7); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(7); });
                        //slider.onValueChanged.AddListener((x) => { OnSliderValueChange(7, x); });
                    }
                    else if (i == 8)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(8); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(8); });
                        //slider.onValueChanged.AddListener((x) => { OnSliderValueChange(8, x); });
                    }
                    else if (i == 9)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(9); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(9); });
                        //slider.onValueChanged.AddListener((x) => { OnSliderValueChange(9, x); });
                    }
                    else if (i == 10)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(10); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(10); });
                        //slider.onValueChanged.AddListener((x) => { OnSliderValueChange(10, x); });
                    }
                    else if (i == 11)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(11); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(11); });
                        //slider.onValueChanged.AddListener((x) => { OnSliderValueChange(11, x); });
                    }
                    else if (i == 12)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(12); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(12); });
                        //slider.onValueChanged.AddListener((x) => { OnSliderValueChange(12, x); });
                    }
                    else if (i == 13)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(13); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(13); });
                        //slider.onValueChanged.AddListener((x) => { OnSliderValueChange(13, x); });
                    }
                    else if (i == 14)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(14); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(14); });
                        //slider.onValueChanged.AddListener((x) => { OnSliderValueChange(14, x); });
                    }
                    else if (i == 15)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(15); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(15); });
                        //slider.onValueChanged.AddListener((x) => { OnSliderValueChange(15, x); });
                    }
                }


                // 特化UI
                specializeObj = new GameObject("Specialize");
                specializeObj.transform.SetParent(parentTrans);
                specializeObj.transform.localScale = new Vector3(1, 1, 1);
                specializeObj.transform.localPosition = new Vector3(1200, -Utils.UIActualHeight + 190, 0);
                if (DSPGame.globalOption.resolution.width * Utils.UIActualHeight / DSPGame.globalOption.resolution.height < 1920)
                {
                    lowUIResolution = true;
                    specializeObj.transform.localPosition = new Vector3(400, -Utils.UIActualHeight + 190, 0);
                }

                specializeObj.SetActive(false);
                GameObject specMainTitleObj = GameObject.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/circle-back/cnt-text"), specializeObj.transform);
                specMainTitleObj.name = "mainTitle";
                specMainTitleObj.transform.localPosition = new Vector3(0, 40, 0);
                specMainTitleObj.GetComponent<Text>().fontSize = 20;
                specMainTitleObj.GetComponent<Text>().lineSpacing = 0.95f;
                specMainTitleObj.GetComponent<Text>().text = "星际组装厂特化".Translate();
                specMainTitleObj.GetComponent<Text>().alignment = TextAnchor.LowerLeft;
                specializeTitleTxts.Add(specMainTitleObj.GetComponent<Text>()); // 占位用
                specializeStateTxts.Add(specMainTitleObj.GetComponent<Text>()); // 占位用
                for (int i = 0; i <= 5; i++)
                {
                    if (i > 0)
                    {
                        GameObject specTitleObj = GameObject.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/circle-back/cnt-text"), specializeObj.transform);
                        specTitleObj.name = $"specTitle{i}";
                        specTitleObj.transform.localPosition = new Vector3(0, 30 - 30 * i, 0);
                        specTitleObj.GetComponent<Text>().fontSize = 16;
                        specTitleObj.GetComponent<Text>().lineSpacing = 0.95f;
                        specTitleObj.GetComponent<Text>().text = $"星际组装厂特化名称{i}".Translate();
                        specTitleObj.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
                        specTitleObj.GetComponent<Text>().color = new Color(1.0f, 0.705f, 0f, 0.765f);
                        specializeTitleTxts.Add(specTitleObj.GetComponent<Text>());

                        GameObject specStateObj = GameObject.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/circle-back/cnt-text"), specializeObj.transform);
                        specStateObj.name = $"specState{i}";
                        specStateObj.transform.localPosition = new Vector3(260, 30 -30 * i, 0);
                        specStateObj.GetComponent<Text>().fontSize = 16;
                        specStateObj.GetComponent<Text>().lineSpacing = 0.95f;
                        specStateObj.GetComponent<Text>().text = "";
                        specStateObj.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                        specializeStateTxts.Add(specStateObj.GetComponent<Text>());
                    }

                    // 用来显示说明提示的按钮
                    GameObject tipButtonObj = GameObject.Instantiate(addNewLayerButton, specializeObj.transform);
                    tipButtonObj.SetActive(true);
                    tipButtonObj.name = $"tip{i}"; //名字
                    tipButtonObj.transform.localPosition = new Vector3(-60, 50 - 30 * i, 0); //位置
                    if(i==0) // 特化标题的问号
                        tipButtonObj.transform.localPosition = new Vector3(-60, 65, 0);
                    tipButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20); //按钮大小
                    tipButtonTxts.Add(tipButtonObj.transform.Find("Text").gameObject.GetComponent<Text>());
                    tipButtonObj.transform.Find("Text").gameObject.GetComponent<Text>().text = "?";
                    Button tipButton = tipButtonObj.GetComponent<Button>();
                    tipButton.interactable = true;
                    tipButton.onClick.RemoveAllListeners();

                    tipButtonObj.GetComponent<UIButton>().tips.tipTitle = $"特化{i}介绍标题".Translate();
                    tipButtonObj.GetComponent<UIButton>().tips.tipText = $"特化{i}介绍内容".Translate();
                    tipButtonObj.GetComponent<UIButton>().tips.delay = 0.1f;
                    tipButtonObj.GetComponent<UIButton>().tips.width = i == 0 ? 300 : 280; 
                    tipButtonObj.GetComponent<UIButton>().tips.corner = 7;
                    //tipButtonObj.GetComponent<UIButton>().tips.offset = new Vector2(-180, 90);
                    //if(lowUIResolution)
                    //    tipButtonObj.GetComponent<UIButton>().tips.offset = new Vector2(150, 90);
                }

            }
        }


        // 初始化所有存档数据为默认0，只在加载游戏或游戏开始时使用
        public static void ResetAndInitArchiveData()
        {
            recipeIds = new List<List<int>>();
            weights = new List<List<double>>();
            progress = new List<List<double>>();
            incProgress = new List<List<double>>();
            productSpeedRequest = new List<List<int>>();
            specProgress = new List<int>();
            curSpecType = new List<int>();
            inProgressSpecType = new List<int>();
            satisfiedSpecType = new List<int>();
            for (int starIndex = 0; starIndex < 1000; starIndex++)
            {
                recipeIds.Add(new List<int> { 0 });
                weights.Add(new List<double> { 1 });
                progress.Add(new List<double> { 0 });
                incProgress.Add(new List<double> { 0 });
                productSpeedRequest.Add(new List<int> { 0 });
                for (int i = 1; i < slotCount; i++)
                {
                    recipeIds[starIndex].Add(0);
                    weights[starIndex].Add(0);
                    progress[starIndex].Add(0);
                    incProgress[starIndex].Add(0);
                    productSpeedRequest[starIndex].Add(0);
                }

                specProgress.Add(0);
                curSpecType.Add(0);
                inProgressSpecType.Add(0);
                satisfiedSpecType.Add(0);
            }
        }

        public static void ResetDataAfterStarIndex100()
        {
            for (int i = 100; i < 1000; i++)
            {
                for (int j = 0; j < slotCount; j++)
                {
                    recipeIds[i][j] = 0;
                    weights[i][j] = j == 0 ? 1 : 0;
                    progress[i][j] = 0;
                    incProgress[i][j] = 0;
                    productSpeedRequest[i][j] = 0;
                }
            }
        }

        public static void ResetArchiveDataByStarIndex(int starIndex)
        {
            recipeIds[starIndex] = new List<int> { 0 };
            weights[starIndex] = new List<double> { 1 };
            progress[starIndex] = new List<double> { 0 };
            incProgress[starIndex] = new List<double> { 0 };
            for (int i = 1; i < slotCount; i++)
            {
                recipeIds[starIndex].Add(0);
                weights[starIndex].Add(0);
                progress[starIndex].Add(0);
                incProgress[starIndex].Add(0);
            }

            specProgress[starIndex] = 0;
            curSpecType[starIndex] = 0;
            inProgressSpecType[starIndex] = 0;
            satisfiedSpecType[starIndex] = 0;
        }

        /// <summary>
        /// 初始化并计算游戏运行时数据
        /// </summary>
        public static void InitInGameData()
        {
            int maxStarIndex = MoreMegaStructure.Support1000Stars.Value ? 1000 : 100;
            for (int starIndex = 0; starIndex  < maxStarIndex; starIndex ++)
            {
                if (MoreMegaStructure.StarMegaStructureType[starIndex] == 4)
                {
                    CalcInGameDataByStarIndex(starIndex);
                }
            }
            //currentStarIndex = 0;
            currentRecipeSlot = 0;
            currentStarIncs = new List<int>();
            for (int i = 0; i < slotCount; i++)
            {
                currentStarIncs.Add(0);
            }
            lockSliderListener = false;
            blueBuffByTCFV = 0;
            r002ByTCFV = 0;
            r106ByTCFV = 0;
            r208ByTCFV = 0;
        }

        public static void ForceResetIncDataCache()
        {
            currentStarIncs.Clear();
            for (int i = 0; i < slotCount; i++)
            {
                currentStarIncs.Add(0);
            }
        }

        public static void ResetInGameDataByStarIndex(int starIndex)
        {
            CalcInGameDataByStarIndex(starIndex);
        }

        public static void CalcInGameDataByStarIndex(int starIndex)
        {
            items[starIndex] = new List<List<int>>();
            products[starIndex] = new List<List<int>>();
            itemCounts[starIndex] = new List<List<int>>();
            productCounts[starIndex] = new List<List<int>>();
            timeSpend[starIndex] = new List<int>();
            productStorage[starIndex] = new Dictionary<int, int>();
            productStorageInc[starIndex] = new Dictionary<int, int>();
            productStorage[starIndex][9500] = 0;
            specBuffLevel[starIndex] = new List<int>();
            for (int s = 0; s < slotCount; s++)
            {
                items[starIndex].Add(new List<int>());
                itemCounts[starIndex].Add(new List<int>());
                products[starIndex].Add(new List<int>());
                productCounts[starIndex].Add(new List<int>());
                timeSpend[starIndex].Add(1);
                specBuffLevel[starIndex].Add(0);
            }

            for (int i = 0; i < slotCount; i++)
            {
                if (recipeIds[starIndex][i] > 0)
                {
                    RecipeProto recipe = LDB.recipes.Select(recipeIds[starIndex][i]);

                    for (int j = 0; j < recipe.Items.Length; j++)
                    {
                        items[starIndex][i].Add(recipe.Items[j]);
                        itemCounts[starIndex][i].Add(recipe.ItemCounts[j]);
                    }
                    for (int j = 0; j < recipe.Results.Length; j++)
                    {
                        products[starIndex][i].Add(recipe.Results[j]);
                        productCounts[starIndex][i].Add(recipe.ResultCounts[j]);
                        if (!productStorage[starIndex].ContainsKey(recipe.Results[j]))
                            productStorage[starIndex].Add(recipe.Results[j], 0);
                    }
                    timeSpend[starIndex][i] = Math.Max(1, recipe.TimeSpend);
                    if (recipe.Results[0] >= 6001 && recipe.Results[0] <= 6006)
                        timeSpend[starIndex][i] *= matrixTimeSpendRatio;
                }
            }
            if (starIndex < GameMain.galaxy.starCount)
            {
                if (GameMain.data?.dysonSpheres[starIndex] != null)
                    CheckSpecializeState(GameMain.data.dysonSpheres[starIndex], true);
            }
        }


 
        public static void InternalUpdate(DysonSphere __instance)
        {
            if (GameMain.instance.timei % 60 == 0)
                CheckSpecializeState(__instance);

            int starIndex = __instance.starData.index;
            long energy = __instance.energyGenCurrentTick - __instance.energyReqCurrentTick;
            int div = (int)(Math.Log10(energy / tickEnergyForFullSpeed / 100) * 4); // 从100x开始，特化所需时间由10min开始增加，最多到100kx的规模需要2小时特化完成
            div = Utils.Limit(div, 1, 12);
            UpdateSpecializeState(__instance, div);
            //Utils.Log("internal updating " + __instance.starData.displayName);
            int[] productRegister = null;
            int[] consumeRegister = null;
            int maxSlotCount = CalcMaxSlotIndex(__instance) + 1;
            for (int i = 0; i < GameMain.galaxy.stars[starIndex].planetCount; i++)
            {
                PlanetData planet = GameMain.galaxy.stars[starIndex].planets[i];
                if (planet != null)
                {
                    PlanetFactory factory = planet.factory;
                    if (factory != null)
                    {
                        if (UIStatisticsPatcher.enabled)
                        {
                            if (GameMain.statistics.production.factoryStatPool.Length > factory.index
                                && GameMain.data.factories.Length + GameMain.galaxy.starCount - starIndex - 1 < GameMain.statistics.production.factoryStatPool.Length)
                            {
                                //FactoryProductionStat factoryProductionStat = GameMain.statistics.production.factoryStatPool[factory.index];
                                FactoryProductionStat factoryProductionStat = GameMain.statistics.production.factoryStatPool[GameMain.data.factories.Length + GameMain.galaxy.starCount - starIndex - 1];
                                productRegister = factoryProductionStat.productRegister;
                                consumeRegister = factoryProductionStat.consumeRegister;
                                break;
                            }
                        }
                        else
                        {
                            FactoryProductionStat factoryProductionStat = GameMain.statistics.production.factoryStatPool[factory.index];
                            productRegister = factoryProductionStat.productRegister;
                            consumeRegister = factoryProductionStat.consumeRegister;
                            if (productRegister != null && consumeRegister != null)
                                break;
                        }
                    }
                }
            }
            AutoSprayInStation(starIndex, ref consumeRegister);

            double totalRedundantEnergy = 0; // 多余的能量被用于生产组件
            bool allEnergyNotUsed = true; // 指示是否所有配方都没有分配速度，如果这样，redundantEnergy应该是组装厂的全部energy而不是0
            long tickEnergy = GameMain.data.dysonSpheres[starIndex].energyGenCurrentTick - GameMain.data.dysonSpheres[starIndex].energyReqCurrentTick;
            // 生产进度计算
            for (int i = 1; i < maxSlotCount; i++)
            {
                if (recipeIds[starIndex][i] > 0)
                {
                    if (allEnergyNotUsed && weights[starIndex][i] > 0) // 如果有一个配方分配了能量，就不是“所有配方都没有分配速度”
                        allEnergyNotUsed = false;
                    bool flag = false; // 是否能够继续生产，只有在所有产物都堆满的情况下停止生产
                    bool stopDueToFullStorage = false;
                    for (int pd = 0; pd < products[starIndex][i].Count; pd++)
                    {
                        int productId = products[starIndex][i][pd];
                        if (!productStorage[starIndex].ContainsKey(productId))
                        {
                            flag = true;
                            break;
                        }
                        else if (productStorage[starIndex][productId] < 10000 && productCounts[starIndex][i][pd] > 0) // 如果是写在产物里，但是实际产出是0的，则其没有满也不能作为可生产的依据（为了防止狄拉克元驱动）
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (timeSpend[starIndex][i] > 0)
                    {
                        if (flag)
                        {
                            double redundantEnergy;
                            double prog = GetConsumeProduceSpeedRatio(starIndex, i, out redundantEnergy);
                            totalRedundantEnergy += redundantEnergy;
                            progress[starIndex][i] += prog;
                        }
                        else
                        {
                            double redundantEnergy;
                            double prog = GetConsumeProduceSpeedRatio(starIndex, i, out redundantEnergy);
                            totalRedundantEnergy += redundantEnergy;
                            // totalRedundantEnergy += (tickEnergy) * weights[starIndex][i]; // 如果有这项，会导致：产物堵住时，能量会转移到生产组件上
                        }
                    }

                    
                }
            }
            //Utils.Log("compo progress " +  progress[starIndex][0].ToString() + " + " + (energy * weights[starIndex][0] / MoreMegaStructure.multifunctionComponentHeat * (MoreMegaStructure.isRemoteReceiveingGear ? 0.1 : 1.0)).ToString());
            if (allEnergyNotUsed)
                weights[starIndex][0] = 1;
            else if(tickEnergy > 0)
                weights[starIndex][0] = totalRedundantEnergy / tickEnergy;
            else
                weights[starIndex][0] = 0;
            progress[starIndex][0] += GetConsumeProduceSpeedRatio(starIndex, 0, out _);

            // 生产进度填满，则立刻消耗原材料并根据消耗产出产物，存入巨构的产物暂存storage内
            for (int i = 1; i < maxSlotCount; i++)
            {
                if (progress[starIndex][i] >= 1)
                {
                    if (recipeIds[starIndex][i] > 0)
                    {
                        int recipeId = recipeIds[starIndex][i];
                        int minSatisfied = (int)progress[starIndex][i]; // 用于指示获取到的原材料是否满足，如果不满足总是取最低的作为最终产出的参照
                        int minInc = 10; // 指示获取的增产等级，总是以最低的为准

                        if (MoreMegaStructure.NoWasteResources.Value) // 如果这个选项被开启，先执行一遍寻找最小满足比例的原材料，将其满足比例设定为最小满足比例
                        {
                            for (int j = 0; j < items[starIndex][i].Count; j++)
                            {
                                if (itemCounts[starIndex][i][j] <= 0) continue;
                                int gotInc = 0;
                                int gotItem = TakeItemForFactory(starIndex, items[starIndex][i][j], minSatisfied * itemCounts[starIndex][i][j], itemCounts[starIndex][i][j], out gotInc, false);
                                if (gotItem >= itemCounts[starIndex][i][j])
                                {
                                    minSatisfied = Math.Min(minSatisfied, gotItem / itemCounts[starIndex][i][j]);
                                    minInc = Math.Min(minInc, gotInc / gotItem);
                                }
                                else
                                {
                                    minSatisfied = 0;
                                    minInc = 0;
                                    break;
                                }
                            }
                        }

                        for (int j = 0; j < items[starIndex][i].Count; j++) // 开始尝试取得每个原材料
                        {
                            if (minSatisfied <= 0)
                                break;

                            if (itemCounts[starIndex][i][j] <= 0) continue;
                            
                            int gotInc = 0;

                            int gotItem = TakeItemForFactory(starIndex, items[starIndex][i][j], minSatisfied * itemCounts[starIndex][i][j], itemCounts[starIndex][i][j], out gotInc, true);
                            if (consumeRegister != null)
                            {
                                int[] obj = consumeRegister;
                                lock (obj)
                                {
                                    consumeRegister[items[starIndex][i][j]] += gotItem;
                                }
                            }
                            if (gotItem >= itemCounts[starIndex][i][j])
                            {
                                minSatisfied = Math.Min(minSatisfied, gotItem / itemCounts[starIndex][i][j]);
                                minInc = Math.Min(minInc, gotInc / gotItem);
                            }
                            else
                            {
                                minSatisfied = 0;
                                minInc = 0;
                                break;
                            }
                        }

                        if (minSatisfied > 0)
                        {
                            for (int j = 0; j < products[starIndex][i].Count; j++)
                            {
                                float productFactor = minSatisfied;
                                //深空狄拉克分解出反物质的buff
                                if (r208ByTCFV > 0 && recipeId == 74)
                                {
                                    if (products[starIndex][i][j] == 1120)
                                        productFactor = 0;
                                    else if (products[starIndex][i][j] == 1122)
                                        productFactor = 1.5f * minSatisfied;
                                }
                                productStorage[starIndex][products[starIndex][i][j]] += (int)(productFactor * productCounts[starIndex][i][j]);
                                if (productStorage[starIndex][products[starIndex][i][j]] > 99999) productStorage[starIndex][products[starIndex][i][j]] = 99999;
                                // 恒星反应釜产物的增产点数储存 以及深空来敌女神泪效果
                                if (curSpecType[starIndex] == 2 && specBuffLevel[starIndex][i] > 0 || r002ByTCFV > 0)
                                {
                                    if (!productStorageInc.ContainsKey(starIndex))
                                        productStorageInc.Add(starIndex, new Dictionary<int, int>());
                                    if (!productStorageInc[starIndex].ContainsKey(products[starIndex][i][j]))
                                        productStorageInc[starIndex].Add(products[starIndex][i][j], (int)(productFactor * productCounts[starIndex][i][j]) * 4);
                                    else
                                        productStorageInc[starIndex][products[starIndex][i][j]] += (int)(productFactor * productCounts[starIndex][i][j]) * 4;
                                    if (productStorageInc[starIndex][products[starIndex][i][j]] > 4 * productStorage[starIndex][products[starIndex][i][j]])
                                        productStorageInc[starIndex][products[starIndex][i][j]] = 4 * productStorage[starIndex][products[starIndex][i][j]];
                                }
                                if (productRegister != null)
                                {
                                    int[] obj = productRegister;
                                    lock (obj)
                                    {
                                        productRegister[products[starIndex][i][j]] += (int)(productFactor * productCounts[starIndex][i][j]);
                                    }
                                }
                            }

                            int actualProduct = minSatisfied;

                            // 增产效果
                            if (incProgress[starIndex][i] >= 0 || specBuffLevel[starIndex][i] > 0) // 负数则视为是无法增产的配方，但是如果能够享受特化Buff，那么可以无视recipe设定，强行允许增产！
                            {
                                incProgress[starIndex][i] += minSatisfied * GetFullIncMilli(starIndex, i, minInc);
                                if (MoreMegaStructure.curStar != null && starIndex == MoreMegaStructure.curStar.index)
                                {
                                    currentStarIncs[i] = minInc; // 将这一帧的增产效果暂存，方便刷新
                                    if (curSpecType[starIndex] == 2 && specBuffLevel[starIndex][i] > 0)
                                        currentStarIncs[i] = 4;
                                }

                                // 增产点数满了之后
                                if (incProgress[starIndex][i] >= 1)
                                {
                                    int bonusProduct = (int)incProgress[starIndex][i];
                                    for (int j = 0; j < products[starIndex][i].Count; j++)
                                    {
                                        float productFactor = bonusProduct;
                                        if (r208ByTCFV > 0 && recipeId == 74)
                                        {
                                            if (products[starIndex][i][j] == 1120)
                                                productFactor = 0;
                                            else if (products[starIndex][i][j] == 1122)
                                                productFactor = 1.5f * bonusProduct;
                                        }
                                        productStorage[starIndex][products[starIndex][i][j]] += (int)(productFactor * productCounts[starIndex][i][j]);
                                        if (productStorage[starIndex][products[starIndex][i][j]] > 99999) productStorage[starIndex][products[starIndex][i][j]] = 99999;
                                        if (curSpecType[starIndex] == 2 && specBuffLevel[starIndex][i] > 0 || r002ByTCFV > 0)
                                        {
                                            if (!productStorageInc.ContainsKey(starIndex))
                                                productStorageInc.Add(starIndex, new Dictionary<int, int>());
                                            if (!productStorageInc[starIndex].ContainsKey(products[starIndex][i][j]))
                                                productStorageInc[starIndex].Add(products[starIndex][i][j], (int)(productFactor * productCounts[starIndex][i][j]) * 4);
                                            else
                                                productStorageInc[starIndex][products[starIndex][i][j]] += (int)(productFactor * productCounts[starIndex][i][j]) * 4;
                                            if (productStorageInc[starIndex][products[starIndex][i][j]] > 4 * productStorage[starIndex][products[starIndex][i][j]])
                                                productStorageInc[starIndex][products[starIndex][i][j]] = 4 * productStorage[starIndex][products[starIndex][i][j]];
                                        }
                                        if (productRegister != null)
                                        {
                                            int[] obj = productRegister;
                                            lock (obj)
                                            {
                                                productRegister[products[starIndex][i][j]] += bonusProduct * productCounts[starIndex][i][j];
                                            }
                                        }
                                    }
                                    incProgress[starIndex][i] -= (int)incProgress[starIndex][i];
                                    if (incProgress[starIndex][i] < 0)
                                        incProgress[starIndex][i] = 0;

                                    actualProduct += bonusProduct; // 用于后续处理蓝buff等效果
                                }
                            }

                            // 深空来敌蓝buff效果
                            if (blueBuffByTCFV > 0 && itemCounts[starIndex][i].Count > 1 && products[starIndex][i][0] != 1803 && products[starIndex][i][0] != 6006)
                            {
                                int returnCount = actualProduct * productCounts[starIndex][i][0];
                                int realConsume = minSatisfied * itemCounts[starIndex][i][0];
                                int overReturned = returnCount - realConsume;
                                int returnItemId = items[starIndex][i][0];
                                if (!productStorage[starIndex].ContainsKey(returnItemId))
                                    productStorage[starIndex].Add(returnItemId, returnCount);
                                else
                                    productStorage[starIndex][returnItemId] += returnCount;
                                if (productStorage[starIndex][returnItemId] > 99999) productStorage[starIndex][returnItemId] = 99999;
                                if (minInc > 0) // 返还的输入原材料具有本次的增产等级
                                {
                                    if (!productStorageInc[starIndex].ContainsKey(returnItemId))
                                        productStorageInc[starIndex][returnItemId] = returnCount * minInc;
                                    else
                                        productStorageInc[starIndex][returnItemId] += returnCount * minInc;

                                    if (productStorageInc[starIndex][returnItemId] > 4 * productStorage[starIndex][returnItemId])
                                        productStorageInc[starIndex][returnItemId] = 4 * productStorage[starIndex][returnItemId];
                                }
                                if (overReturned <= 0 && consumeRegister != null)
                                {
                                    int[] obj = consumeRegister;
                                    lock (obj)
                                    {
                                        consumeRegister[returnItemId] -= returnCount;
                                    }
                                }
                                else if (consumeRegister != null && productRegister != null) // 返还的比消耗的还多，多出的视为生产
                                {
                                    int[] obj = consumeRegister;
                                    lock (obj)
                                    {
                                        consumeRegister[returnItemId] -= realConsume;
                                    }
                                    int[] obj2 = productRegister;
                                    lock (obj2)
                                    {
                                        productRegister[returnItemId] += overReturned;
                                    }
                                }
                            }

                            // 深空来敌能量迸发效果
                            if (r106ByTCFV > 0)
                            {
                                int rocketId = products[starIndex][i][0];
                                int rodNum = -1;
                                if (rocketId >= 9488 && rocketId <= 9490)
                                    rodNum = 2;
                                else if (rocketId == 9491 || rocketId == 9492 || rocketId == 9510 || rocketId == 1503)
                                    rodNum = 1;
                                if (rodNum > 0) // 判断原材料是否已满
                                {
                                    int returnItemId = 1802;
                                    int returnCount = actualProduct * 2;
                                    int overReturnCount = 0;
                                    if (!productStorage[starIndex].ContainsKey(returnItemId))
                                        productStorage[starIndex].Add(returnItemId, returnCount);
                                    else
                                        productStorage[starIndex][returnItemId] += returnCount;
                                    if (productStorage[starIndex][returnItemId] > 99999) productStorage[starIndex][returnItemId] = 99999;
                                    if (minInc > 0) // 返还的输入原材料具有本次的增产等级
                                    {
                                        if (!productStorageInc[starIndex].ContainsKey(returnItemId))
                                            productStorageInc[starIndex][returnItemId] = returnCount * minInc;
                                        else
                                            productStorageInc[starIndex][returnItemId] += returnCount * minInc;

                                        if (productStorageInc[starIndex][returnItemId] > 4 * productStorage[starIndex][returnItemId])
                                            productStorageInc[starIndex][returnItemId] = 4 * productStorage[starIndex][returnItemId];
                                    }
                                    // 如果返还的比消耗的还多（只会且必定会出现在增产情况下，因为所有火箭都是2）
                                    if(actualProduct > minSatisfied)
                                    {
                                        returnCount = minSatisfied * 2;
                                        overReturnCount = (actualProduct - minSatisfied) * 2;
                                        if(productRegister != null)
                                        {
                                            int[] obj = productRegister;
                                            lock(obj)
                                            {
                                                productRegister[returnItemId] += overReturnCount;
                                            }
                                        }
                                    }
                                    if (consumeRegister != null)
                                    {
                                        int[] obj = consumeRegister;
                                        lock (obj)
                                        {
                                            consumeRegister[returnItemId] -= returnCount;
                                        }
                                    }
                                }
                            }

                        }

                        progress[starIndex][i] -= (int)progress[starIndex][i];
                    }
                    else
                    {
                        progress[starIndex][i] = 0;
                    }
                }
                else if (progress[starIndex][i] < 0)
                {
                    progress[starIndex][i] = 0;
                }
            }

            if (progress[starIndex][0] >= 1)
            {
                //Utils.Log("gen" + ((int)progress[starIndex][0]).ToString());
                if (productStorage[starIndex].ContainsKey(9500))
                    productStorage[starIndex][9500] = Math.Min(productStorage[starIndex][9500] + (int)progress[starIndex][0], 10000);
                else
                    productStorage[starIndex][9500] = Math.Min((int)progress[starIndex][0], 10000);
                if (productRegister != null)
                {
                    int[] obj = productRegister;
                    lock (obj)
                    {
                        productRegister[9500] += (int)progress[starIndex][0];
                    }
                }
                progress[starIndex][0] -= (int)progress[starIndex][0];
            }
            else if (progress[starIndex][0] < 0)
            {
                progress[starIndex][0] = 0;
            }

            // 将暂存产物全部放入星系地表的交换站，如果有远程传送需求，优先传送
            if (MoreMegaStructure.isRemoteReceiveingGear && productStorage[starIndex][9500]>0)
            {
                try
                {
                    int ICCount = productStorage[starIndex][9500];
                    productStorage[starIndex][9500] = 0;
                    GameMain.mainPlayer.TryAddItemToPackage(9500, ICCount, 0, true);
                    if (!VFInput.inFullscreenGUI && MoreMegaStructure.maxAutoReceiveGear < 3000)
                        Utils.UIItemUp(9500, ICCount, 240);
                }
                catch (Exception)
                {
                }
            }

            SendProductToGround(starIndex);
            AutoSprayInStation(starIndex, ref consumeRegister);
        }


        public static void AutoSprayInStation(int starIndex, ref int[] consumeRegister)
        {
            int planetCount = GameMain.galaxy.stars[starIndex].planetCount;
            for (int i = 0; i < planetCount; i++)
            {
                PlanetFactory factory = GameMain.galaxy.stars[starIndex].planets[i].factory;
                if (factory != null)
                {
                    PlanetTransport transport = factory.transport;
                    if (transport != null)
                    {
                        for (int j = 1; j < transport.stationCursor; j++)
                        {
                            StationComponent stationComponent = transport.stationPool[j];
                            if (stationComponent == null) continue;
                            int protoId = factory.entityPool[stationComponent.entityId].protoId;
                            if (protoId != 9512) continue; // 只有物资交换建筑

                            bool autoSpray = false; // 如果最后一个格子是增产剂，则自动喷涂
                            int autoSprayLevel = 0; // 增产剂对应的增产登记
                            int eachSprayCount = 1; // 每个增产剂可以喷多少个物品
                            float consumeProb = 1; // 不足消耗一个增产剂时，喷涂每个物品消耗的几率
                            int proliferatorId = stationComponent.storage[4].itemId;
                            if (proliferatorId == 1141 || proliferatorId == 1142 || proliferatorId == 1143)
                            {
                                autoSpray = true;
                                eachSprayCount = proliferatorId == 1143 ? 60 : proliferatorId == 1142 ? 24 : 12;
                                autoSprayLevel = proliferatorId == 1143 ? 4 : proliferatorId == 1142 ? 2 : 1;
                                eachSprayCount = (int)(eachSprayCount * (Cargo.incTableMilli[4] + 1));
                                consumeProb = 1f / eachSprayCount;
                            }
                            for (int k = 0; k < 5; k++)
                            {
                                StationStore[] obj = stationComponent.storage;
                                lock (obj)
                                {
                                    // 如果第五个位置是增产剂，则自动喷涂前四个位置
                                    if (k != 4)
                                    {
                                        if (autoSpray && stationComponent.storage[4].count > 0)
                                        {
                                            int proliferatorCount = stationComponent.storage[4].count;
                                            int itemCount = stationComponent.storage[k].count;
                                            int alreadyHas = stationComponent.storage[k].inc;
                                            if (alreadyHas < itemCount * autoSprayLevel)
                                            {
                                                int needIncPoints = itemCount * autoSprayLevel - alreadyHas;
                                                int needProliferator = (int)(1.0f * needIncPoints / autoSprayLevel / eachSprayCount);
                                                int needPointsLeft = needIncPoints - needProliferator * autoSprayLevel * eachSprayCount;
                                                if (Utils.RandF() < (1f * needPointsLeft / autoSprayLevel) * consumeProb) // 不够整数消耗增产剂的部分按照几率消耗
                                                    needProliferator++;

                                                if (proliferatorCount >= needProliferator)
                                                {
                                                    stationComponent.storage[4].inc = (int)(1f * (proliferatorCount - needProliferator) / stationComponent.storage[4].count * stationComponent.storage[4].inc);
                                                    stationComponent.storage[4].count = proliferatorCount - needProliferator;
                                                    stationComponent.storage[k].inc = stationComponent.storage[k].inc + needIncPoints;
                                                    if (consumeRegister != null)
                                                    {
                                                        int[] registerObj = consumeRegister;
                                                        lock (registerObj)
                                                        {
                                                            consumeRegister[proliferatorId] += needProliferator;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    int realProliferatorConsume = stationComponent.storage[4].count;
                                                    stationComponent.storage[k].inc = stationComponent.storage[k].inc + realProliferatorConsume * eachSprayCount * autoSprayLevel;
                                                    stationComponent.storage[4].inc = 0;
                                                    stationComponent.storage[4].count = 0;
                                                    if (consumeRegister != null)
                                                    {
                                                        int[] registerObj = consumeRegister;
                                                        lock (registerObj)
                                                        {
                                                            consumeRegister[proliferatorId] += realProliferatorConsume;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 试图从巨构储藏空间和地表的交换塔里面取原材料
        /// </summary>
        /// <param name="starIndex"></param>
        /// <param name="itemId"></param>
        /// <param name="itemCount"></param>
        /// <param name="minCount"></param> 指单个配方一次产出需要的量，如果已有的少于这个就不取了，否则可能出现一直取但又不生产的情况
        /// <param name="inc"></param>
        /// <param name="consume"></param> 如果是false，则这次调用是为了检查具体数量的而暂时不消耗。true则是真正开始消耗
        /// <returns></returns>
        public static int TakeItemForFactory(int starIndex, int itemId, int itemCount, int minCount, out int inc, bool consume)
        {
            int result = 0;
            inc = 0;
            // 先从productStorage里面拿，从此处拿的视为最高默认增产等级
            if (productStorage[starIndex].ContainsKey(itemId) && productStorage[starIndex][itemId] >= minCount)
            {
                result = Math.Min(itemCount, productStorage[starIndex][itemId]);
                if(consume)
                    productStorage[starIndex][itemId] -= result;
                inc = 4 * result;
                // 如果内部仓储的该物品有增产点数，得拿掉增产点数。可能导致送回地面的就不带增产了，那得这样。
                if (productStorageInc.ContainsKey(starIndex) && productStorageInc[starIndex].ContainsKey(itemId))
                {
                    productStorageInc[starIndex][itemId] -= inc < productStorageInc[starIndex][itemId] ? inc : productStorageInc[starIndex][itemId];
                }
            }
            // 再从地表拿
            if (result < itemCount)
            {
                int planetCount = GameMain.galaxy.stars[starIndex].planetCount;
                for (int i = 0; i < planetCount; i++)
                {
                    PlanetFactory factory = GameMain.galaxy.stars[starIndex].planets[i].factory;
                    if (factory != null)
                    {
                        PlanetTransport transport = factory.transport;
                        if (transport != null)
                        {
                            for (int j = 1; j < transport.stationCursor; j++)
                            {
                                StationComponent stationComponent = transport.stationPool[j];
                                if (stationComponent == null) continue;
                                int protoId = factory.entityPool[stationComponent.entityId].protoId;
                                if (protoId != 9512) continue; // 只有物资交换建筑

                                for (int k = 0; k < 5; k++)
                                {
                                    StationStore[] obj = stationComponent.storage;
                                    lock (obj)
                                    {
                                        if (stationComponent.storage[k].itemId == itemId && stationComponent.storage[k].count >= minCount)
                                        {
                                            int oriInc = stationComponent.storage[k].inc / stationComponent.storage[k].count;
                                            int need = itemCount - result;
                                            if (stationComponent.storage[k].count > need)
                                            {
                                                result += need;
                                                inc += oriInc * need;
                                                if (consume)
                                                {
                                                    stationComponent.storage[k].count -= need;
                                                    stationComponent.storage[k].inc -= oriInc * need;
                                                }
                                                return result;
                                            }
                                            else
                                            {
                                                result += stationComponent.storage[k].count;
                                                inc += stationComponent.storage[k].inc;
                                                if (consume)
                                                {
                                                    stationComponent.storage[k].count = 0;
                                                    stationComponent.storage[k].inc = 0;
                                                }
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 将产物送回地表
        /// </summary>
        /// <param name="starIndex"></param>
        public static void SendProductToGround(int starIndex)
        {
            
            int planetCount = GameMain.galaxy.stars[starIndex].planetCount;
            for (int i = 0; i < planetCount; i++)
            {
                PlanetFactory factory = GameMain.galaxy.stars[starIndex].planets[i].factory;
                if (factory != null)
                {
                    PlanetTransport transport = factory.transport;
                    if (transport != null)
                    {
                        for (int j = 1; j < transport.stationCursor; j++)
                        {
                            StationComponent stationComponent = transport.stationPool[j];
                            if (stationComponent == null) continue;
                            int protoId = factory.entityPool[stationComponent.entityId].protoId;
                            if (protoId != 9512) continue; // 只有物资交换建筑

                            for (int k = 0; k < 5; k++)
                            {
                                StationStore[] obj = stationComponent.storage;
                                lock (obj)
                                {
                                    int productId = stationComponent.storage[k].itemId;
                                    if (productStorage[starIndex].ContainsKey(productId) && productStorage[starIndex][productId] > 0)
                                    {
                                        int waitingToSend = productStorage[starIndex][productId];
                                        if (stationComponent.storage[k].max - stationComponent.storage[k].count >= waitingToSend)
                                        {
                                            stationComponent.storage[k].count += waitingToSend;
                                            productStorage[starIndex][productId] = 0;
                                            if (productStorageInc.ContainsKey(starIndex) && productStorageInc[starIndex].ContainsKey(productId))
                                            {
                                                stationComponent.storage[k].inc += productStorageInc[starIndex][productId];
                                                productStorageInc[starIndex][productId] = 0;
                                            }
                                        }
                                        else if(stationComponent.storage[k].max > stationComponent.storage[k].count)
                                        {
                                            int sended = stationComponent.storage[k].max - stationComponent.storage[k].count;
                                            stationComponent.storage[k].count += sended;
                                            productStorage[starIndex][productId] -= sended; 
                                            if (productStorageInc.ContainsKey(starIndex) && productStorageInc[starIndex].ContainsKey(productId))
                                            {
                                                int sendedInc = Math.Min(productStorageInc[starIndex][productId], sended * 4);
                                                stationComponent.storage[k].inc += sendedInc;
                                                productStorageInc[starIndex][productId] -= sendedInc;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // 返回每帧理想的基础消耗产出系数：这是已被各种特化的加速效果加成过的结果，但还未被增产效果加成
        public static double GetConsumeProduceSpeedRatio(int starIndex, int slotNum, out double redundantEnergy)
        {
            long energy = GameMain.data.dysonSpheres[starIndex].energyGenCurrentTick - GameMain.data.dysonSpheres[starIndex].energyReqCurrentTick;
            double prog = energy * weights[starIndex][slotNum] / tickEnergyForFullSpeed / timeSpend[starIndex][slotNum];
            redundantEnergy = 0;
            if (slotNum != 0)
            {
                int mainProductCount = productCounts[starIndex][slotNum][0];
                if (mainProductCount == 0)
                    mainProductCount = 1;
                double speedLimitProg = 1.0 * productSpeedRequest[starIndex][slotNum] / mainProductCount / 3600;
                if (prog > speedLimitProg)
                {
                    redundantEnergy = energy * weights[starIndex][slotNum] * (prog - speedLimitProg) / prog; // 多余的能量
                    prog = speedLimitProg;
                }
            }
            // 特化速度加成或其他影响
            if (specBuffLevel[starIndex][slotNum] > 0)
            {
                if (curSpecType[starIndex] == 1)
                {
                    prog *= 3;
                }
                else if (curSpecType[starIndex] == 2)
                {
                    prog *= 2;
                }
            }
            else if (slotNum == 0) // 集成组件速率专门计算
            {
                if (curSpecType[starIndex] == 1)
                    return 0;
                else
                    return energy * weights[starIndex][0] / MoreMegaStructure.multifunctionComponentHeat * (MoreMegaStructure.isRemoteReceiveingGear ? 0.1 : 1.0);
            }
            return prog;
        }

        /// <summary>
        /// 返回能量和原材料足够时，首要产物的产出速度
        /// </summary>
        /// <param name="starIndex"></param>
        /// <param name="slotNum"></param>
        /// <param name="forceIncLevel">如果为负则使用当前值</param>
        /// <returns></returns>
        public static double GetFirstProductFullySpeed(int starIndex, int slotNum, int forceIncLevel = -1)
        {
            if (slotNum <= 0)
                return 0;
            
            int mainProductCount = productCounts[starIndex][slotNum][0];
            if (mainProductCount == 0)
                mainProductCount = 1;
            double finalSpeed = 1.0 * productSpeedRequest[starIndex][slotNum];
            if (specBuffLevel[starIndex][slotNum] > 0)
            {
                if (curSpecType[starIndex] == 1)
                {
                    finalSpeed *= 3;
                }
                else if (curSpecType[starIndex] == 2)
                {
                    finalSpeed *= 2;
                }

            }
            if (forceIncLevel < 0)
                forceIncLevel = currentStarIncs[slotNum];
            float incFactor = GetFullIncMilli(starIndex, slotNum, forceIncLevel);
            return finalSpeed * (1 + incFactor);

        }

        // 返回被特化和增产剂增产效果加成后的每帧产出倍率
        public static double GetIncProduceSpeedRatio(double satisfiedRatio, int starIndex, int slotNum, int incLevel)
        {
            return satisfiedRatio * GetFullIncMilli(starIndex, slotNum, incLevel);
        }

        // 返回特化和增产剂联合作用下的增产系数
        public static float GetFullIncMilli(int starIndex, int slotNum, int incLevel)
        {
            incLevel = incLevel >= 10 ? 10 : incLevel;
            incLevel = incLevel < 0 ? 0 : incLevel;
            double incByProliferator = Cargo.incTableMilli[incLevel];
            double incBySpecialization = 0;
            if (specBuffLevel[starIndex][slotNum] > 0)
            {
                if (curSpecType[starIndex] == 2)
                {
                    incByProliferator = Math.Max(incByProliferator, Cargo.incTableMilli.Length > 4 ? Cargo.incTableMilli[4] : 0.25);
                }
                else if (curSpecType[starIndex] == 3)
                {
                    incBySpecialization = 0.25;
                }
                else if (curSpecType[starIndex] == 4)
                {
                    incBySpecialization = 0.25 * specBuffLevel[starIndex][slotNum];
                }
                else if (curSpecType[starIndex] == 5)
                {
                    incBySpecialization = 0.25 * specBuffLevel[starIndex][slotNum];
                }
            }
            else if (incProgress[starIndex][slotNum] < 0) // 说明是常规条件下无法增产的配方，且没吃到特化加成，这种情况下不允许增产
            {
                incByProliferator = 0;
            }
            return (float)(incByProliferator + incBySpecialization);
        }

        /// <summary>
        /// 检查是否触发或改变特化进程，并检查每个配方是否触发特化的buff要求
        /// </summary>
        /// <param name="sphere"></param>
        public static void CheckSpecializeState(DysonSphere sphere, bool forceCheckAllSlot = false)
        {
            int starIndex = sphere.starData.index;
            List<int> specTypeFlag = new List<int> { 0, 0, 0, 0, 0, 0 };
            int maxSlotIndex = forceCheckAllSlot ? slotCount-1 : CalcMaxSlotIndex(sphere);
            for (int slot = 1; slot <= maxSlotIndex; slot++)
            {
                if (recipeIds[starIndex][slot] == 0)
                    continue;

                int slotSpecBuffLvl = 0;
                ERecipeType recipeType = LDB.recipes.Select(recipeIds[starIndex][slot]).Type;

                // 1
                if (recipeType == ERecipeType.Smelt)
                {
                    specTypeFlag[1] += 1;
                    if (curSpecType[starIndex] == 1)
                        slotSpecBuffLvl = 1;
                }
                else
                    specTypeFlag[1] = -9999;

                // 2
                if (recipeType == ERecipeType.Chemical || recipeType == ERecipeType.Refine || (int)recipeType == 16 || products[starIndex][slot].Contains(1141) || products[starIndex][slot].Contains(1142) || products[starIndex][slot].Contains(1143))
                {
                    specTypeFlag[2] += 1;
                    if (curSpecType[starIndex] == 2)
                        slotSpecBuffLvl = 1;
                }
                else
                    specTypeFlag[2] = -9999;

                // 3
                bool flag3 = false;
                for (int i = 0; i < items[starIndex][slot].Count; i++)
                {
                    if (items[starIndex][slot][i] == 1121 || items[starIndex][slot][i] == 1122)
                    {
                        flag3 = true;
                        break;
                    }
                }
                if (!flag3)
                {
                    for (int i = 0; i < products[starIndex][slot].Count; i++)
                    {
                        if (products[starIndex][slot][i] == 1121 || products[starIndex][slot][i] == 1122)
                        {
                            flag3 = true;
                            break;
                        }
                    }
                }
                if (flag3)
                {
                    specTypeFlag[3] += 1;
                    if (curSpecType[starIndex] == 3)
                        slotSpecBuffLvl = 1;
                }
                else
                {
                    specTypeFlag[3] = -9999;
                }

                // 4
                int flag4Lvl = 0;
                for (int i = 0; i < products[starIndex][slot].Count; i++)
                {
                    if (products[starIndex][slot][i] == 1303 || products[starIndex][slot][i] == 1305 || products[starIndex][slot][i] == 9486)
                    {
                        flag4Lvl = 2;
                        break;
                    }
                }
                if (flag4Lvl<=0)
                {
                    for (int i = 0; i < items[starIndex][slot].Count; i++)
                    {
                        if (items[starIndex][slot][i] == 1303 || items[starIndex][slot][i] == 1305 || items[starIndex][slot][i] == 9486)
                        {
                            flag4Lvl = 1;
                            break;
                        }
                    }
                }
                if (flag4Lvl > 0)
                {
                    specTypeFlag[4] += 1;
                    if (curSpecType[starIndex] == 4)
                        slotSpecBuffLvl = flag4Lvl;
                }
                else
                    specTypeFlag[4] = -9999;

                // 5
                int flag5Lvl = 0;
                for (int i = 0; i < products[starIndex][slot].Count; i++) // 遍历产物 是否有弹药或防御、舰队等
                {
                    int pId = products[starIndex][slot][i];
                    ItemProto pItem = LDB.items.Select(pId);
                    if (pItem.isAmmo) // 要加||isBomb? 没发现有符合的，可能后续会需要改动
                    {
                        flag5Lvl = 4;
                        break;
                    }
                    else if (pItem.Type == EItemType.Defense || pItem.Type == EItemType.Turret || pItem.isCraft) // isFighter等是冗余的？ 类似上面可能后续需要改动
                    {
                        flag5Lvl = 2; // 不要break是万一产物既有弹药又有防御，按高的算（这合理吗？）反正目前没有这种recipe无所谓啦
                    }
                }
                if (flag5Lvl > 0)
                {
                    specTypeFlag[5] += 1;
                    if (curSpecType[starIndex] == 5)
                        slotSpecBuffLvl = flag5Lvl;
                }
                else
                    specTypeFlag[5] = -9999;
                

                // 存储该slot是否受到加成影响，且设定影响等级
                specBuffLevel[starIndex][slot] = slotSpecBuffLvl;
                if (slotSpecBuffLvl == 0) // 未能享受特化加成的配方，若不允许其增产，必须还原其禁止增产的标志，即incProgress为负数
                {
                    RecipeProto rp = LDB.recipes.Select(recipeIds[starIndex][slot]);
                    if (rp != null && !rp.productive)
                        incProgress[starIndex][slot] = -1;
                }
            }

            // 根据结果设定已满足要求的特化类型，之后交由UpdateSpecializeState处理特化进程
            bool noSatisfied = true;
            for (int sType = 5; sType >= 1; sType--) // 从5到1是为了让5的优先级比4高。
            {
                if (specTypeFlag[sType] >= specializeRequirements[sType])
                {
                    satisfiedSpecType[starIndex] = sType;
                    noSatisfied = false;
                    break;
                }
            }
            if (noSatisfied)
                satisfiedSpecType[starIndex] = 0;
        }

        /// <summary>
        /// 计算特化进程，div正向取决于巨构能量水平，只有在tick整除div时才修改进度点
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="div"></param>
        public static void UpdateSpecializeState(DysonSphere sphere, int div)
        {
            int starIndex = sphere.starData.index;
            if (starIndex >= 1000) return;
            bool sandBoxMode = GameMain.data.gameDesc.isSandboxMode;
            if (sandBoxMode)
                div = 1;
            if (GameMain.instance.timei % div == 0) // 受div影响，决定特化速度
            {
                if (specProgress[starIndex] <= 0)
                {
                    if (satisfiedSpecType[starIndex] > 0 && satisfiedSpecType[starIndex] != curSpecType[starIndex])
                    {
                        inProgressSpecType[starIndex] = satisfiedSpecType[starIndex];
                        specProgress[starIndex]++;
                        if(sandBoxMode)
                            specProgress[starIndex]+= 300;
                    }
                }
                else
                {
                    if (satisfiedSpecType[starIndex] == inProgressSpecType[starIndex])
                    {
                        specProgress[starIndex]++;
                        if (sandBoxMode)
                            specProgress[starIndex] += 300;
                    }
                    else
                    {
                        specProgress[starIndex]--;
                        if (sandBoxMode)
                            specProgress[starIndex] -= 300;
                    }
                }

                if (specProgress[starIndex] >= specializeTimeNeed)
                {
                    specProgress[starIndex] = 0;
                    curSpecType[starIndex] = inProgressSpecType[starIndex];
                }
                else if (specProgress[starIndex] <= 0)
                {
                    inProgressSpecType[starIndex] = 0;
                    specProgress[starIndex] = 0;
                }
            }
        }

        // 刷新全部UI
        public static void RefreshUI(bool forceShowUI = false)
        {
            if (MoreMegaStructure.curStar == null) return;
            showingLimit = true;
            int starIndex = MoreMegaStructure.curStar.index;
            if (starIndex < 1000 && MoreMegaStructure.StarMegaStructureType[starIndex] == 4)
            {
                if (forceShowUI)
                {
                    showingLimit = false; // 强制打开时，默认不显示生产速率限制的编辑栏
                    GigaFactoryUIObj.SetActive(true);
                    if(lowUIResolution)
                        specializeObj.SetActive(false);
                    else
                        specializeObj.SetActive(true);
                }
                showHideButtonObj.SetActive(true);
                showHideBtnText.text = "显示/隐藏星际组装厂配置".Translate();
                doubleAllBtnText.text = "加倍所有速度设置".Translate();
                halveAllBtnText.text = "减半所有速度设置".Translate();
                lockSliderListener = true;
                int maxSlotIndex = CalcMaxSlotIndex(GameMain.data.dysonSpheres[MoreMegaStructure.curStar.index]);
                for (int i = 1; i < slotCount; i++)
                {
                    if (recipeIds[starIndex][i] > 0 && maxSlotIndex >= i)
                    {
                        recipeIcons[i].sprite = LDB.recipes.Select(recipeIds[starIndex][i]).iconSprite;
                        recipeSelectTips[i].SetActive(false);
                        sliderObjs[i].SetActive(!showingLimit);
                        speedTextObjs[i].SetActive(true);
                        setLimitObjs[i].SetActive(showingLimit);
                        sliders[i].value = W2S(weights[starIndex][i]);
                        removeRecipeBtnObjs[i].SetActive(true);
                        recipeCircleUIBtns[i].tips.itemId = products[starIndex][i][0];
                        // recipeCircleUIBtns[i].tips.type = UIButton.ItemTipType.Recipe; // 没效果
                        if (LDB.recipes.Select(recipeIds[starIndex][i]) != null && LDB.recipes.Select(recipeIds[starIndex][i]).Explicit)
                        {
                            recipeCircleUIBtns[i].tips.itemId = -recipeIds[starIndex][i];
                        }
                    }
                    else
                    {
                        recipeIcons[i].sprite = noRecipeSelectedSprit;
                        recipeSelectTips[i].SetActive(true);
                        sliderObjs[i].SetActive(false);
                        speedTextObjs[i].SetActive(false);
                        setLimitObjs[i].SetActive(false);
                        removeRecipeBtnObjs[i].SetActive(false);
                        recipeCircleUIBtns[i].tips.itemId = 0;
                        if (maxSlotIndex < i)
                            recipePickerTxts[i].text = String.Format("组装厂槽位解锁于".Translate(), MoreMegaStructure.Capacity2Str(speedNeededToUnlockSlot[i]));
                        else
                            recipePickerTxts[i].text = "指定配方".Translate();
                    }
                }
                sliderObjs[0].SetActive(true);
                recipeSelectTips[0].SetActive(false);
                removeRecipeBtnObjs[0].SetActive(false);
                recipeIcons[0].sprite = MoreMegaStructure.iconInterCompo;
                sliders[0].value = (float)weights[starIndex][0] * 100;

                RefreshProduceSpeedContent();
                RefreshStorageText();
                RefreshSpecializeUI(); 
                RefreshLimitInputUI();
                lockSliderListener = false; 

            }
            else
            {
                GigaFactoryUIObj.SetActive(false);
                specializeObj.SetActive(false);
                showHideButtonObj.SetActive(false);
            }
        }

        public static void UIFrameUpdate(long timei)
        {
            RefreshStorageText();
            if (timei % 10 == 0)
            {
                RefreshProduceSpeedContent();
                RefreshUI();
            }
        }

        public static void RefreshProduceSpeedContent()
        {
            if (UIRoot.instance.uiMainMenu.gameObject.activeSelf) return;
            if (MoreMegaStructure.curStar == null) return;
            int starIndex = MoreMegaStructure.curStar.index;
            if (GameMain.data.dysonSpheres?.Length <= starIndex) return;
            if (!GigaFactoryUIObj.activeSelf) return;
            int maxSlotIndex = CalcMaxSlotIndex(GameMain.data.dysonSpheres[starIndex]);
            if (MoreMegaStructure.StarMegaStructureType[starIndex] != 4) return;
            for (int i = 1; i < slotCount; i++)
            {
                if (recipeIds[starIndex][i] > 0 && i<= maxSlotIndex)
                {
                    double PPM = GetConsumeProduceSpeedRatio(starIndex, i, out _) * 3600 * productCounts[starIndex][i][0];
                    string incStr = "";
                    double extraProductRatio = GetFullIncMilli(starIndex, i, currentStarIncs[i] > 10 ? 10 : currentStarIncs[i]);
                    if (extraProductRatio > 0)
                    {
                        incStr = string.Format("<color=#61D8FFC8>+{0:N2} %</color>\n", extraProductRatio * 100);
                    }
                    PPM = PPM * (1 + extraProductRatio);
                    string value = PPM > 10 ? PPM.ToString("N0") : (PPM > 1 ? PPM.ToString("N1") : (PPM > 0 ? PPM.ToString("N2") : "0.00"));
                    double idealPM = GetFirstProductFullySpeed(starIndex, i);
                    //string idealValue = idealPM > 10 ? idealPM.ToString("N0") : (idealPM > 1 ? idealPM.ToString("N1") : (idealPM > 0 ? idealPM.ToString("N2") : "0.00"));
                    
                    if(idealPM > PPM + 0.001f)
                    {
                        produceSpeedTxts[i].text = incStr + "可承载速度".Translate() + " " + value + "/min";
                    }
                    else
                    {
                        produceSpeedTxts[i].text = incStr + "理论最大速度".Translate() + " " + value + "/min";
                    }

                    weightTxts[i].text = "能量分配".Translate() + " " + ((weights[starIndex][i] * 100)).ToString() + "%";
                }
                else
                {
                    produceSpeedTxts[i].text = "";
                }
            }
            double PPM2 = GetConsumeProduceSpeedRatio(starIndex, 0, out _) * 3600;
            string value2 = PPM2 > 10 ? PPM2.ToString("N0") : (PPM2 > 1 ? PPM2.ToString("N1") : (PPM2 > 0 ? PPM2.ToString("N2") : "0.00"));
            string remoteTransportingStr = "";
            if (MoreMegaStructure.isRemoteReceiveingGear)
                remoteTransportingStr = "已开启优先传输至机甲".Translate();
            produceSpeedTxts[0].text = "理论最大速度".Translate() + " " + value2 + "/min   " + remoteTransportingStr;
            weightTxts[0].text = "剩余能量".Translate() + " " + ((weights[starIndex][0] * 100)).ToString("N0") + "%";

            // 增产图标刷新
            for (int i = 1; i <slotCount; i++)
            {
                int minInc = currentStarIncs[i];
                if (minInc > 0 && i <= maxSlotIndex)
                {
                    int iconIdx = minInc;
                    if (iconIdx == 3) iconIdx = 2;
                    else if (iconIdx > 4) iconIdx = 4;
                    incIcons[i].sprite = Resources.Load<Sprite>($"entities/models/cargo/inc-{iconIdx}");
                    incIcons[i].gameObject.SetActive(true);
                }
                else
                {
                    incIcons[i].gameObject.SetActive(false);
                }
            }
        }

        public static void RefreshStorageText()
        {
            if (MoreMegaStructure.curStar == null) return; 
            int starIndex = MoreMegaStructure.curStar.index;
            if (MoreMegaStructure.StarMegaStructureType[starIndex] != 4) return;
            for (int i = 1; i <slotCount; i++)
            {
                if (recipeIds[starIndex][i] > 0)
                {
                    int itemId = products[starIndex][i][0];
                    int firstProductCount = productStorage[starIndex].ContainsKey(itemId) ? productStorage[starIndex][itemId] : 0;
                    int recipeId = recipeIds[starIndex][i];
                    if (productCounts[starIndex][i].Count>1)
                        storageTxts[i].text = "主产物巨构内部仓储".Translate() + "\n" + firstProductCount.ToString() + "/10000";
                    else
                        storageTxts[i].text = "巨构内部仓储".Translate() + "\n" + firstProductCount.ToString() + "/10000";
                }
                else
                {
                    storageTxts[i].text = "";
                }
                
            }
            int MCProductCount = productStorage[starIndex].ContainsKey(9500) ? productStorage[starIndex][9500] : 0;
            storageTxts[0].text = "巨构内部仓储".Translate() + "\n" + MCProductCount.ToString() + "/10000";
        }

        public static void RefreshSpecializeUI()
        {
            for (int i = 0; i < tipButtonTxts.Count; i++)
            {
                if (tipButtonTxts[i] != null)
                {
                    tipButtonTxts[i].text = "?";
                }
            }
            for (int i = 0; i < specializeTitleTxts.Count; i++)
            {
                specializeTitleTxts[i].text = $"星际组装厂特化名称{i}".Translate();
            }

            for (int i = 1; i < specializeTitleTxts.Count; i++)
            {
                int starIndex = MoreMegaStructure.curStar.index;
                if (curSpecType[starIndex] == i)
                {
                    specializeTitleTxts[i].color = UITextBlue;
                    if (specProgress[starIndex] > 0 && satisfiedSpecType[starIndex] > 0 && satisfiedSpecType[starIndex] != i)
                    {
                        specializeStateTxts[i].color = UITextOrange;
                        specializeStateTxts[i].text = "特化即将被取代".Translate();
                    }
                    else
                    {
                        specializeStateTxts[i].color = UITextBlue;
                        specializeStateTxts[i].text = "特化已激活".Translate();
                    }
                }
                else if (inProgressSpecType[starIndex] == i)
                {
                    if (satisfiedSpecType[starIndex] == i)
                    {
                        specializeTitleTxts[i].color = UITextGreen;
                        specializeStateTxts[i].color = UITextGreen;
                        specializeStateTxts[i].text = string.Format("特化进程".Translate(), (int)(100.0 * specProgress[starIndex] / specializeTimeNeed));
                    }
                    else
                    {
                        specializeTitleTxts[i].color = UITextOrange;
                        specializeStateTxts[i].color = UITextOrange;
                        specializeStateTxts[i].text = string.Format("特化进程消退中".Translate(), (int)(100.0 * specProgress[starIndex] / specializeTimeNeed));
                    }
                }
                else if (satisfiedSpecType[starIndex] == i)
                {
                    specializeTitleTxts[i].color = UITextGreen;
                    specializeStateTxts[i].color = UITextGreen;
                    specializeStateTxts[i].text = "等待其他特化进程消退".Translate();
                }
                else
                {
                    specializeTitleTxts[i].color = UITextGray;
                    specializeStateTxts[i].color = UITextGray;
                    specializeStateTxts[i].text = "特化条件未满足".Translate();
                }
            }
        }

        public static void OnRecipeSelectClick(int slotIndex)
        {
            if (MoreMegaStructure.curStar == null) return;
            int maxSlotIndex = CalcMaxSlotIndex(GameMain.data.dysonSpheres[MoreMegaStructure.curStar.index]);
            if (slotIndex > maxSlotIndex)
            {
                UIRealtimeTip.Popup("星际组装厂槽位未解锁警告".Translate());
                return;
            }
            currentRecipeSlot = slotIndex;
            UIRecipePicker.Popup(new Vector2(0f, 0f), new Action<RecipeProto>(OnRecipePickerReturn));
        }

        public static void OnRecipeRemoveClick(int slotIndex)
        {
            if (MoreMegaStructure.curStar == null) return;
            int starIndex = MoreMegaStructure.curStar.index;
            recipeIds[starIndex][slotIndex] = 0;
            incProgress[starIndex][slotIndex] = 0;
            progress[starIndex][slotIndex] = 0;
            weights[starIndex][slotIndex] = 0;
            productSpeedRequest[starIndex][slotIndex] = 0;
            currentStarIncs[slotIndex] = 0;
            double total = 1;
            SetProductSpeedRequest(slotIndex, "0");
            RefreshUI();
        }

        /// <summary>
        /// 选择好了配方后该窗口将调用此函数
        /// </summary>
        /// <param name="recipe"></param>
        public static void OnRecipePickerReturn(RecipeProto recipe)
        {
            int starIndex = MoreMegaStructure.curStar.index;
            if(MoreMegaStructure.Support1000Stars.Value)
            {
                if (starIndex > 999)
                {
                    UIRealtimeTip.Popup("警告巨构不支持恒星系数量大于1000个".Translate());
                    return;
                }
            }
            else if (starIndex > 99)
            {
                UIRealtimeTip.Popup("警告巨构不支持恒星系数量大于100个".Translate());
                return;
            }
            int recipeId = recipe.ID;
            for (int s = 1; s < slotCount; s++)
            {
                if (recipeIds[starIndex][s] == recipeId) 
                {
                    if (s > CalcMaxSlotIndex(GameMain.data.dysonSpheres[starIndex])) // 配方重复的时候，如果已该配方是被一个还未解锁的槽位占用的，那么则将此配方抢夺过来
                    {
                        recipeIds[starIndex][s] = 0; // 不break
                    }
                    else // 代表重复发生的位置是已解锁的槽位，那么就阻止重复配方
                    {
                        UIRealtimeTip.Popup("警告选择了重复的配方".Translate());
                        return;
                    }
                }
            }
            if (MoreMegaStructure.GenesisCompatibility)
            {
                if (recipe.Type == (ERecipeType)14)
                {
                    UIRealtimeTip.Popup("警告巨构不支持此类配方".Translate());
                    return;
                }
            }
            recipeIds[starIndex][currentRecipeSlot] = recipe.ID;
            incProgress[starIndex][currentRecipeSlot] = 0;
            progress[starIndex][currentRecipeSlot] = 0;
            currentStarIncs[currentRecipeSlot] = 0;
            productSpeedRequest[starIndex][currentRecipeSlot] = 0;
            if (!recipe.productive) // 不能增产的配方，其incProgress标记为负数
                incProgress[starIndex][currentRecipeSlot] = -1;
            if (!items.ContainsKey(starIndex))
            {
                items.Add(starIndex, new List<List<int>>());
                products[starIndex] = new List<List<int>>();
                itemCounts[starIndex] = new List<List<int>>();
                productCounts[starIndex] = new List<List<int>>();
                timeSpend[starIndex] = new List<int>();
                productStorage[starIndex] = new Dictionary<int, int>();
                productStorageInc[starIndex] = new Dictionary<int, int>();
                productStorage[starIndex][9500] = 0;
                for (int s = 0; s < slotCount; s++)
                {
                    items[starIndex].Add(new List<int>());
                    itemCounts[starIndex].Add(new List<int>());
                    products[starIndex].Add(new List<int>());
                    productCounts[starIndex].Add(new List<int>());
                    timeSpend[starIndex].Add(1);
                }
            }
            int i = currentRecipeSlot;
            items[starIndex][i].Clear();
            itemCounts[starIndex][i].Clear();
            products[starIndex][i].Clear();
            productCounts[starIndex][i].Clear();
            for (int j = 0; j < recipe.Items.Length; j++)
            {
                items[starIndex][i].Add(recipe.Items[j]);
                itemCounts[starIndex][i].Add(recipe.ItemCounts[j]);
            }
            for (int j = 0; j < recipe.Results.Length; j++)
            {
                products[starIndex][i].Add(recipe.Results[j]);
                productCounts[starIndex][i].Add(recipe.ResultCounts[j]);
                if (!productStorage[starIndex].ContainsKey(recipe.Results[j]))
                    productStorage[starIndex].Add(recipe.Results[j], 0);
            }
            timeSpend[starIndex][i] = Math.Max(1, recipe.TimeSpend);
            if (recipe.Results[0] >= 6001 && recipe.Results[0] <= 6006)
                timeSpend[starIndex][i] *= matrixTimeSpendRatio;
            if (MoreMegaStructure.GenesisCompatibility)
            {
                if (recipe.Type == (ERecipeType)12 || recipe.Type == (ERecipeType)13)
                {
                    timeSpend[starIndex][i] *= recipeType1213TimeSpendRatio;
                }
            }
            SetProductSpeedRequest(currentRecipeSlot, "0");
            CheckSpecializeState(GameMain.data.dysonSpheres[starIndex]);
            RefreshUI();
        }

        /// <summary>
        /// 调整能量分配滑动条，已弃用
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <param name="sv"></param>
        public static void OnSliderValueChange(int slotIndex, float sv)
        {
            return;
            if (MoreMegaStructure.curStar == null || lockSliderListener) return;
            int starIndex = MoreMegaStructure.curStar.index;
            weights[starIndex][slotIndex] = S2W(sliders[slotIndex].value);

            double use = 0;
            for (int i = 1; i < slotCount; i++)
            {
                use += weights[starIndex][i] * 100;
            }
            if (use <= 100) // 若总分配不足100%，未使用的分配将给0号slot，即多功能集成组件
            {
                sliders[0].value = (float)(100 - use);
                weights[starIndex][0] = sliders[0].value / 100f;
            }
            else // 若总分配超过100%，除了保证当前的分配不变之外，其他的按比例缩减直到总分配==100%
            {
                lockSliderListener = true;
                sliders[0].value = 0;
                weights[starIndex][0] = 0;
                double otherUse = 0;
                for (int i = 1; i < slotCount; i++)
                {
                    if (i != slotIndex)
                    {
                        otherUse += weights[starIndex][i] * 100;
                    }
                }
                double ratio = (100f - S2W(sliders[slotIndex].value)*100.0) / otherUse;
                for (int i = 1; i < slotCount; i++)
                {
                    if (i != slotIndex)
                    {
                        sliders[i].value = (float)(W2S(weights[starIndex][i]) * ratio);
                    }
                    weights[starIndex][i] = S2W(sliders[i].value);
                }
            }

            RefreshProduceSpeedContent();
            lockSliderListener = false;
        }

        public static void NormalizeSetValue(int index, bool forceFullInc)
        {
            int starIndex = MoreMegaStructure.curStar.index;
            int targetSpeed = productSpeedRequest[starIndex][index];
            double realSpeed = GetFirstProductFullySpeed(starIndex, index, forceFullInc ? 4 : -1);
            int needInput = (int)Math.Ceiling(targetSpeed * 1.0 / realSpeed * targetSpeed);
            SetProductSpeedRequest(index, needInput.ToString());
        }


        public static void TryUseRocketInStorageToBuildIA(DysonSphere __instance)
        {
            if (productStorage[__instance.starData.index].ContainsKey(9491))
            {
                if (productStorage[__instance.starData.index][9491] > 0)
                {
                    productStorage[__instance.starData.index][9491] = AutoBuildInterstellarAssembly(__instance.starData.index, productStorage[__instance.starData.index][9491]);
                }
            }
        }

        /// <summary>
        /// 使用星际组装厂内部存储的组装厂火箭直接建造节点
        /// </summary>
        /// <param name="starIndex"></param>
        /// <param name="amount"></param>
        public static int AutoBuildInterstellarAssembly(int starIndex = -1, int amount = 1)
        {
            if (starIndex < 0)
            {
                return amount;
            }
            if (starIndex >= 0 && starIndex < GameMain.data.dysonSpheres.Length)
            {
                DysonSphere sphere = GameMain.data.dysonSpheres[starIndex];
                if (sphere != null)
                {
                    for (int i = 0; i < sphere.layersIdBased.Length; i++)
                    {
                        DysonSphereLayer dysonSphereLayer = sphere.layersIdBased[i];
                        if (dysonSphereLayer != null)
                        {
                            int num = dysonSphereLayer.nodePool.Length;
                            for (int j = 0; j < num; j++)
                            {
                                DysonNode dysonNode = dysonSphereLayer.nodePool[j];
                                if (dysonNode != null)
                                {
                                    for (int k = 0; k < Math.Min(6, amount); k++)
                                    {
                                        if (dysonNode.spReqOrder > 0)
                                        {
                                            sphere.OrderConstructSp(dysonNode);
                                            sphere.ConstructSp(dysonNode);
                                            amount -= 1;
                                        }
                                    }
                                }
                                if (amount <= 0) return amount;
                            }
                        }
                    }
                }
            }
            return amount;
        }

        public static void ShowHideUI()
        {
            if (GigaFactoryUIObj == null) return;
            showingLimit = false;
            if (GigaFactoryUIObj.activeSelf)
            {
                GigaFactoryUIObj.SetActive(false);
                if (lowUIResolution) // lowresolution情况下两个ui占用重叠的位置，所以交替显示。正常1080p以上分辨率的情况下两个ui不重叠，且一起显示或隐藏
                {
                    specializeObj.SetActive(true);
                    RefreshSpecializeUI();
                }
                else
                    specializeObj.SetActive(false);
            }
            else
            {
                GigaFactoryUIObj.SetActive(true);
                if (lowUIResolution)
                    specializeObj.SetActive(false);
                else
                {
                    specializeObj.SetActive(true);
                    RefreshSpecializeUI();
                }

                RefreshUI();
            }
        }

        public static void RefreshLimitInputUI()
        {
            if (GigaFactoryUIObj == null) return;
            int starIndex = MoreMegaStructure.curStar.index;
            for (int i = 1; i < slotCount; i++)
            {
                if (!limitInputs[i].isFocused)
                    limitInputs[i].text = productSpeedRequest[starIndex][i].ToString();
            }

        }

        /// <summary>
        /// 返回当前星际组装厂能量下可支持的最大配方数
        /// </summary>
        /// <param name="sphere"></param>
        /// <returns></returns>
        public static int CalcMaxSlotIndex(DysonSphere sphere)
        {
            if (sphere == null)
                return 4;
            long energy = sphere.energyGenCurrentTick - sphere.energyReqCurrentTick;
            double spd = 1.0 * energy / tickEnergyForFullSpeed;
            for (int n = slotCount - 1; n > 4; n--)
            {
                if (speedNeededToUnlockSlot[n] <= spd)
                    return n;
            }
            return 4;
        }

        public static void SetProductSpeedRequest(int index, string text)
        {
            if (MoreMegaStructure.curStar == null)
                return;
            int starIndex = MoreMegaStructure.curStar.index;
            int value = 0;
            if (text.Length == 0 || text == "")
                text = "0";
            if (index > 0)
            {
                try
                {
                    value = Convert.ToInt32(text);
                }
                catch (Exception)
                {
                    value = 0;
                }
                if (value < 0)
                    value = 0;
                productSpeedRequest[starIndex][index] = value;
                limitInputs[index].text = value.ToString();
            }
            double sum = 0;
            for (int i = 1; i < slotCount; i++)
            {
                if (recipeIds[starIndex][i] > 0 && productCounts[starIndex][i][0] > 0)
                {
                    sum += 1.0 * productSpeedRequest[starIndex][i] / productCounts[starIndex][i][0] * timeSpend[starIndex][i];
                }
            }
            for (int i = 1; i < slotCount; i++)
            {
                if (recipeIds[starIndex][i] > 0 && productCounts[starIndex][i][0] > 0 && sum > 0.000000001)
                {
                    weights[starIndex][i] = 1.0 * productSpeedRequest[starIndex][i] / productCounts[starIndex][i][0] * timeSpend[starIndex][i] / sum;
                }
                else
                {
                    weights[starIndex][i] = 0;
                }
            }
            RefreshLimitInputUI();
            RefreshProduceSpeedContent();
        }

        public static void DoubleAllSpeedRequest()
        {
            int starIndex = MoreMegaStructure.curStar.index;
            for (int i = 1; i < slotCount; i++)
            {
                if (recipeIds[starIndex][i] > 0)
                {
                    if (productSpeedRequest[starIndex][i] >= 499999999)
                    {
                        UIRealtimeTip.Popup(string.Format(("有过大数值警告".Translate()), LDB.recipes.Select(recipeIds[starIndex][i]).Name.Translate()));
                        return;
                    }
                }
            }
            for (int i = 1; i < slotCount; i++)
            {
                if (recipeIds[starIndex][i] > 0)
                {
                    productSpeedRequest[starIndex][i] = productSpeedRequest[starIndex][i] * 2 < 0 ? 0 : productSpeedRequest[starIndex][i] * 2;
                }
            }
            SetProductSpeedRequest(0, "");
            RefreshUI();
        }

        public static void HalveAllSpeedRequest()
        {
            int starIndex = MoreMegaStructure.curStar.index;
            for (int i = 1; i < slotCount; i++)
            {
                productSpeedRequest[starIndex][i] = productSpeedRequest[starIndex][i] / 2;
            }
            SetProductSpeedRequest(0, "");
            RefreshUI();
        }


        /// <summary>
        /// 根据是否线性调整能量分配比例，将SliderValue转变为Weights的数值
        /// </summary>
        /// <param name="sliderValue"></param>
        /// <returns></returns>
        public static double S2W(double sliderValue)
        {
            if (MoreMegaStructure.NonlinearEnergy.Value)
            {
                return Math.Pow(sliderValue / 100.0, 2);
            }
            else
            {
                return sliderValue / 100.0;
            }
        }

        public static float W2S(double weight)
        {
            if (MoreMegaStructure.NonlinearEnergy.Value)
            {
                return (float)(Math.Sqrt(weight)*100);
            }
            else
            {
                return (float)(weight*100);
            }
        }

        public static void Import(BinaryReader r)
        {
            int slotCountInSave = 5;
            if (MoreMegaStructure.savedModVersion >= 119)
            {
                slotCountInSave = slotCount;
            }
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < slotCountInSave; j++)
                {
                    recipeIds[i][j] = r.ReadInt32();
                    weights[i][j] = r.ReadDouble();
                    progress[i][j] = r.ReadDouble();
                    incProgress[i][j] = r.ReadDouble();
                }
                for (int j = slotCountInSave; j < slotCount; j++)
                {
                    recipeIds[i][j] = 0;
                    weights[i][j] = 0;
                    progress[i][j] = 0;
                    incProgress[i][j] = 0;
                }
            }
            if (MoreMegaStructure.savedModVersion >= 116)
            {
                int support1000 = r.ReadInt32(); // 读取是否后续记录了101~1000个星系的数据
                if (support1000 > 0) // 如果记录了，则读取后续数据
                {
                    for (int i = 100; i < 1000; i++)
                    {
                        for (int j = 0; j < slotCountInSave; j++)
                        {
                            recipeIds[i][j] = r.ReadInt32();
                            weights[i][j] = r.ReadDouble();
                            progress[i][j] = r.ReadDouble();
                            incProgress[i][j] = r.ReadDouble();
                        }
                        for (int j = slotCountInSave; j < slotCount; j++)
                        {
                            recipeIds[i][j] = 0;
                            weights[i][j] = 0;
                            progress[i][j] = 0;
                            incProgress[i][j] = 0;
                        }
                    }
                }
                else
                {
                    ResetDataAfterStarIndex100();
                }
            }
            else 
            {
                ResetDataAfterStarIndex100();
            }
            if (MoreMegaStructure.savedModVersion >= 120)
            {
                for (int i = 0; i < 1000; i++)
                {
                    specProgress[i] = r.ReadInt32();
                    curSpecType[i] = r.ReadInt32();
                    inProgressSpecType[i] = r.ReadInt32();
                    satisfiedSpecType[i] = r.ReadInt32();
                }
            }
            if(MoreMegaStructure.savedModVersion >= 130)
            {
                for (int i = 0; i < 1000; i++)
                {
                    for (int j = 0; j < slotCountInSave; j++)
                    {
                        productSpeedRequest[i][j] = r.ReadInt32();
                    }
                }
            }

            tickEnergyForFullSpeed = (int)(20000.0 / MoreMegaStructure.IASpdFactor.Value);
            if (tickEnergyForFullSpeed <= 0) tickEnergyForFullSpeed = 100000;
        }

        public static void Export(BinaryWriter w)
        {
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < slotCount; j++)
                {
                    w.Write(recipeIds[i][j]);
                    w.Write(weights[i][j]);
                    w.Write(progress[i][j]);
                    w.Write(incProgress[i][j]);
                }
            }
            bool support1000 = MoreMegaStructure.Support1000Stars.Value;
            w.Write(support1000 ? 1 : 0); // 在100组数据后写入1或0，记录是否后续还有最多1000个星系的数据
            if (support1000) // 如果设置支持了1000星系，则将101~1000星系的数据写入存档
            {
                for (int i = 100; i < 1000; i++)
                {
                    for (int j = 0; j < slotCount; j++)
                    {
                        w.Write(recipeIds[i][j]);
                        w.Write(weights[i][j]);
                        w.Write(progress[i][j]);
                        w.Write(incProgress[i][j]);
                    }
                }
            }
            for (int i = 0; i <1000;  i++) 
            {
                w.Write(specProgress[i]);
                w.Write(curSpecType[i]);
                w.Write(inProgressSpecType[i]);
                w.Write(satisfiedSpecType[i]);
            }
            for (int i = 0; i < 1000; i++)
            {
                for (int j = 0; j < slotCount; j++)
                {
                    w.Write(productSpeedRequest[i][j]);
                }
            }
        }

        public static void IntoOtherSave()
        {
            ResetAndInitArchiveData();
            InitInGameData();
            tickEnergyForFullSpeed = (int)(20000.0 / MoreMegaStructure.IASpdFactor.Value);
            if (tickEnergyForFullSpeed <= 0) tickEnergyForFullSpeed = 100000;
        }
    }
}
