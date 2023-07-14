using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

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
        public static List<int> targetSpecType = new List<int>(); // 目标特化类型

        // 以下为不需要存档的数据，在载入时重置或者重新计算
        public static Dictionary<int, List<List<int>>> items = new Dictionary<int, List<List<int>>>(); // 存储recipe的原材料的Id
        public static Dictionary<int, List<List<int>>> products = new Dictionary<int, List<List<int>>>(); // 存储recipe的产物的Id
        public static Dictionary<int, List<List<int>>> itemCounts = new Dictionary<int, List<List<int>>>(); // 存储recipe的原材料的需求数量
        public static Dictionary<int, List<List<int>>> productCounts = new Dictionary<int, List<List<int>>>(); // 存储recipe的产物的产出数量
        public static Dictionary<int, List<int>> timeSpend = new Dictionary<int, List<int>>(); // 存储recipe的所需时间
        public static Dictionary<int, Dictionary<int, int>> productStorage = new Dictionary<int, Dictionary<int, int>>(); // 存储产物已暂时堆积在巨构中的数量（可供相同星际组装厂的其他需要此产物作为原材料的配方取用），不区分slot只按照产物Id存储。不进行存档，读档后重置。
        // 上述productStorage项会存在：如果反复疯狂更换recipe会一直增加字典项，可能拖慢速度，但是重进游戏后冗余key会自动清除，因此暂时不做游戏内清理

        public static List<int> currentStarIncs = new List<int>();

        //public static int currentStarIndex = 0; // 弃用，使用MoreMegaStructure.curStar.index
        public static int currentRecipeSlot = 0; // 选择recipe时暂存所选定的是第几个recipe栏位

        public static int slotCount = 16;

        public static GameObject GigaFactoryUIObj = null;
        public static GameObject showHideButtonObj = null;
        public static Text showHideBtnText;

        public static List<Image> recipeIcons = new List<Image>();
        public static List<Image> incIcons = new List<Image>();
        public static List<GameObject> recipeSelectTips = new List<GameObject>();
        public static List<Text> produceSpeedTxts = new List<Text>();
        public static List<GameObject> sliderObjs = new List<GameObject>();
        public static List<GameObject> speedTextObjs = new List<GameObject>();
        public static List<GameObject> removeRecipeBtnObjs = new List<GameObject>();
        public static List<Slider> sliders = new List<Slider>();
        public static List<Text> weightTxts = new List<Text>();
        public static List<Text> storageTxts = new List<Text>();
        public static List<Text> recipePickerTxts = new List<Text>();
        public static Sprite noRecipeSelectedSprit = null;

        public static bool lockSliderListener = false;

        // 以下是固定参数
        public static int tickEnergyForFullSpeed = 20000; // 每相当于1.0倍速的产量需要的tick能量
        public static int matrixTimeSpendRatio = 100; // 用星际组装厂生产矩阵的速度修正倍率，这是为了不让该巨构部分替代科学枢纽
        public static int recipeType1213TimeSpendRatio = 20; // 用星际组装厂生产创世之书特定配方的速度修正
        public static List<long> speedNeededToUnlockSlot = new List<long> { 0, 0, 0, 0, 0, 10, 20, 30, 50, 100, 200, 500, 1000, 5000, 10000, 100000 }; // 星际组装厂解锁对应slot所需速度倍率

        public static void InitAll()
        {
            InitUI();
            ResetAndInitArchiveData();
        }

        public static void ResetAndInitArchiveData()
        {
            recipeIds = new List<List<int>>();
            weights = new List<List<double>>();
            progress = new List<List<double>>();
            incProgress = new List<List<double>>();
            for (int starIndex = 0; starIndex < 1000; starIndex++)
            {
                recipeIds.Add(new List<int> { 0 });
                weights.Add(new List<double> { 1 });
                progress.Add(new List<double> { 0 });
                incProgress.Add(new List<double> { 0 });
                for (int i = 1; i < slotCount; i++)
                {
                    recipeIds[starIndex].Add(0);
                    weights[starIndex].Add(0);
                    progress[starIndex].Add(0);
                    incProgress[starIndex].Add(0);
                }
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
                }
            }
        }

        public static void InitInGameData()
        {
            int maxStarIndex = MoreMegaStructure.Support1000Stars.Value ? 1000 : 100;
            for (int starIndex = 0; starIndex  < maxStarIndex; starIndex ++)
            {
                if (MoreMegaStructure.StarMegaStructureType[starIndex] == 4)
                {
                    items[starIndex] = new List<List<int>>();
                    products[starIndex] = new List<List<int>>();
                    itemCounts[starIndex] = new List<List<int>>();
                    productCounts[starIndex] = new List<List<int>>();
                    timeSpend[starIndex] = new List<int>();
                    productStorage[starIndex] = new Dictionary<int, int>();
                    productStorage[starIndex][9500] = 0;
                    for (int s = 0; s < slotCount; s++)
                    {
                        items[starIndex].Add(new List<int>());
                        itemCounts[starIndex].Add(new List<int>());
                        products[starIndex].Add(new List<int>());
                        productCounts[starIndex].Add(new List<int>());
                        timeSpend[starIndex].Add(1);
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
        }

        public static void ForceResetIncDataCache()
        {
            for (int i = 0; i < slotCount; i++)
            {
                currentStarIncs[i] = 0;
            }
        }

        public static void ResetInGameDataByStarIndex(int starIndex)
        {
            items[starIndex] = new List<List<int>>();
            products[starIndex] = new List<List<int>>();
            itemCounts[starIndex] = new List<List<int>>();
            productCounts[starIndex] = new List<List<int>>();
            timeSpend[starIndex] = new List<int>();
            productStorage[starIndex] = new Dictionary<int, int>();
            productStorage[starIndex][9500] = 0;
            for (int s = 0; s < slotCount; s++)
            {
                items[starIndex].Add(new List<int>());
                itemCounts[starIndex].Add(new List<int>());
                products[starIndex].Add(new List<int>());
                productCounts[starIndex].Add(new List<int>());
                timeSpend[starIndex].Add(1);
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
        }

        public static void InitUI()
        {
            if (GigaFactoryUIObj == null)
            {
                Transform parentTrans = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy").transform;
                GigaFactoryUIObj = new GameObject("GigaFactory");
                GigaFactoryUIObj.transform.SetParent(parentTrans);
                GigaFactoryUIObj.transform.localScale = new Vector3(1, 1, 1);
                GigaFactoryUIObj.transform.localPosition = new Vector3(300, -DSPGame.globalOption.uiLayoutHeight + 180, 0);
                GigaFactoryUIObj.SetActive(true);

                // 显示/隐藏按钮
                GameObject addNewLayerButton = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy/layers/buttons-group/buttons/add-button");
                showHideButtonObj = GameObject.Instantiate(addNewLayerButton, parentTrans);
                showHideButtonObj.SetActive(true);
                showHideButtonObj.name = "show-hide"; //名字
                showHideButtonObj.transform.localPosition = new Vector3(340, -DSPGame.globalOption.uiLayoutHeight + 40, 0); //位置
                showHideButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 24); //按钮大小
                showHideBtnText = showHideButtonObj.transform.Find("Text").gameObject.GetComponent<Text>();
                showHideBtnText.text = "显示/隐藏星际组装厂配置".Translate();
                Button showHideButton = showHideButtonObj.GetComponent<Button>();
                showHideButton.interactable = true;
                showHideButton.onClick.RemoveAllListeners();
                showHideButton.onClick.AddListener(() => { ShowHideUI(); });

                GameObject oriSelectObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/offwork");
                GameObject oriSliderObj = GameObject.Find("UI Root/Overlay Canvas/In Game/FPS Stats/priority-bar/slider-2");
                GameObject oriIncIconObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Station Window/storage-box-0(Clone)/storage-icon/inc-3");
                GameObject oriRemoveRecipeObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/circle-back/stop-btn");
                GameObject oriRemoveRecipeXObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/circle-back/stop-btn/x");

                for (int i = 0; i < slotCount; i++)
                {
                    GameObject slotObj = new GameObject("slot" + i.ToString());
                    slotObj.transform.SetParent(GigaFactoryUIObj.transform);
                    slotObj.transform.localScale = new Vector3(1, 1, 1);
                    slotObj.transform.localPosition = new Vector3((i/8)*500, (i%8) * 80, 0);

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
                    speedTextObjs.Add(produceSpeedTxtObj);

                    GameObject incIconObj = GameObject.Instantiate(oriIncIconObj,slotObj.transform);
                    incIconObj.transform.localPosition = new Vector3(60,-65);
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

                    GameObject storageTxtObj = GameObject.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/circle-back/cnt-text"), sliderObj.transform);
                    storageTxtObj.transform.localPosition = new Vector3(180, -12, 0);
                    storageTxts.Add(storageTxtObj.GetComponent<Text>());
                    storageTxtObj.GetComponent<Text>().fontSize = 16;
                    storageTxtObj.GetComponent<Text>().lineSpacing = 0.95f;
                    storageTxtObj.GetComponent<Text>().text = "";
                    storageTxtObj.GetComponent<Text>().alignment = TextAnchor.LowerLeft;

                    // 这里不能直接传入i，否则会导致所有的参数都变成了5，所以我该怎么写啊啊啊啊啊啊好麻烦
                    if (i == 0)
                    {
                        // circleButton.onClick.AddListener(() => { OnRecipeSelectClick(0); });
                        // slider.onValueChanged.AddListener((x) => { OnSliderValueChange(0, x); });
                        slider.interactable = false;
                    }
                    else if (i == 1)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(1); });
                        removeButton.onClick.AddListener(()=> { OnRecipeRemoveClick(1); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(1, x); });
                    }
                    else if (i == 2)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(2); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(2); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(2, x); });
                    }
                    else if (i == 3)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(3); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(3); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(3, x); });
                    }
                    else if (i == 4)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(4); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(4); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(4, x); });
                    }
                    else if (i == 5)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(5); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(5); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(5, x); });
                    }
                    else if (i == 6)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(6); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(6); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(6, x); });
                    }
                    else if (i == 7)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(7); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(7); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(7, x); });
                    }
                    else if (i == 8) 
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(8); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(8); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(8, x); });
                    }
                    else if (i == 9)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(9); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(9); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(9, x); });
                    }
                    else if (i == 10)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(10); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(10); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(10, x); });
                    }
                    else if (i == 11)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(11); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(11); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(11, x); });
                    }
                    else if (i == 12)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(12); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(12); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(12, x); });
                    }
                    else if (i == 13)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(13); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(13); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(13, x); });
                    }
                    else if (i == 14)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(14); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(14); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(14, x); });
                    }
                    else if (i == 15)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(15); });
                        removeButton.onClick.AddListener(() => { OnRecipeRemoveClick(15); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(15, x); });
                    }
                }

            }
        }


        public static void InternalUpdate(DysonSphere __instance)
        {
            //Utils.Log("internal updating " + __instance.starData.displayName);
            int starIndex = __instance.starData.index;
            long energy = __instance.energyGenCurrentTick - __instance.energyReqCurrentTick;
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
                        if (GameMain.statistics.production.factoryStatPool.Length > factory.index)
                        {
                            //FactoryProductionStat factoryProductionStat = GameMain.statistics.production.factoryStatPool[factory.index];
                            FactoryProductionStat factoryProductionStat = GameMain.statistics.production.factoryStatPool[GameMain.data.factories.Length + GameMain.galaxy.starCount - starIndex - 1];
                            
                            productRegister = factoryProductionStat.productRegister;
                            consumeRegister = factoryProductionStat.consumeRegister;
                            break;
                        }
                    }
                }
            }

            // 生产进度计算
            for (int i = 1; i < maxSlotCount; i++)
            {
                if (recipeIds[starIndex][i] > 0)
                {
                    bool flag = false; // 是否能够继续生产，只有在所有产物都堆满的情况下停止生产
                    for (int pd = 0; pd < products[starIndex][i].Count; pd++)
                    {
                        int productId = products[starIndex][i][pd];
                        if (!productStorage[starIndex].ContainsKey(productId))
                        {
                            flag = true;
                            break;
                        }
                        else if (productStorage[starIndex][productId] < 10000)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (flag && timeSpend[starIndex][i] > 0)
                    {
                        double prog = energy * weights[starIndex][i] / tickEnergyForFullSpeed / timeSpend[starIndex][i];
                        progress[starIndex][i] += prog;
                    }
                }
            }
            //Utils.Log("compo progress " +  progress[starIndex][0].ToString() + " + " + (energy * weights[starIndex][0] / MoreMegaStructure.multifunctionComponentHeat * (MoreMegaStructure.isRemoteReceiveingGear ? 0.1 : 1.0)).ToString());
            progress[starIndex][0] += energy * weights[starIndex][0] / MoreMegaStructure.multifunctionComponentHeat * (MoreMegaStructure.isRemoteReceiveingGear ? 0.1 : 1.0);

            // 生产进度填满，则立刻消耗原材料并根据消耗产出产物，存入巨构的产物暂存storage内
            for (int i = 1; i < maxSlotCount; i++)
            {
                if (progress[starIndex][i] >= 1)
                {
                    if (recipeIds[starIndex][i] > 0)
                    {
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
                                productStorage[starIndex][products[starIndex][i][j]] += minSatisfied * productCounts[starIndex][i][j];
                                if (productStorage[starIndex][products[starIndex][i][j]] > 10000) productStorage[starIndex][products[starIndex][i][j]] = 10000;
                                if (productRegister != null)
                                {
                                    int[] obj = productRegister;
                                    lock (obj)
                                    {
                                        productRegister[products[starIndex][i][j]] += minSatisfied * productCounts[starIndex][i][j];
                                    }
                                }
                            }
                            // 增产效果
                            if (incProgress[starIndex][i] >= 0) // 负数则视为是无法增产的配方
                            {
                                incProgress[starIndex][i] += minSatisfied * Cargo.incTableMilli[minInc];
                                if (MoreMegaStructure.curStar != null && starIndex == MoreMegaStructure.curStar.index)
                                    currentStarIncs[i] = minInc; // 将这一帧的增产效果暂存，方便刷新

                                // 增产点数满了之后
                                if (incProgress[starIndex][i] >= 1)
                                {
                                    int bonusProduct = (int)incProgress[starIndex][i];
                                    for (int j = 0; j < products[starIndex][i].Count; j++)
                                    {
                                        productStorage[starIndex][products[starIndex][i][j]] += bonusProduct * productCounts[starIndex][i][j];
                                        if (productStorage[starIndex][products[starIndex][i][j]] > 10000) productStorage[starIndex][products[starIndex][i][j]] = 10000;
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
                    if (!VFInput.inFullscreenGUI)
                        Utils.UIItemUp(9500, ICCount, 240);
                }
                catch (Exception)
                {
                }
            }

            SendProductToGround(starIndex);
            
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
                                        //if (productId == 9500)
                                        //{
                                        //    Utils.Log("Sending compo");
                                        //}
                                        //else
                                        //{
                                        //    Utils.Log("Sending others");
                                        //}
                                        int waitingToSend = productStorage[starIndex][productId];
                                        if (stationComponent.storage[k].max - stationComponent.storage[k].count >= waitingToSend)
                                        {
                                            stationComponent.storage[k].count += waitingToSend;
                                            productStorage[starIndex][productId] = 0;
                                        }
                                        else if(stationComponent.storage[k].max > stationComponent.storage[k].count)
                                        {
                                            int sended = stationComponent.storage[k].max - stationComponent.storage[k].count;
                                            stationComponent.storage[k].count += sended;
                                            productStorage[starIndex][productId] -= sended;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void RefreshUI(bool forceShowUI = false)
        {
            if (MoreMegaStructure.curStar == null) return;
            int starIndex = MoreMegaStructure.curStar.index;
            if (MoreMegaStructure.StarMegaStructureType[starIndex] == 4)
            {
                if (forceShowUI)
                    GigaFactoryUIObj.SetActive(true);
                showHideButtonObj.SetActive(true);
                showHideBtnText.text = "显示/隐藏星际组装厂配置".Translate();
                lockSliderListener = true;
                int maxSlotIndex = CalcMaxSlotIndex(GameMain.data.dysonSpheres[MoreMegaStructure.curStar.index]);
                for (int i = 1; i < slotCount; i++)
                {
                    if (recipeIds[starIndex][i] > 0 && maxSlotIndex >= i)
                    {
                        recipeIcons[i].sprite = LDB.recipes.Select(recipeIds[starIndex][i]).iconSprite;
                        recipeSelectTips[i].SetActive(false);
                        sliderObjs[i].SetActive(true);
                        speedTextObjs[i].SetActive(true);
                        sliders[i].value = W2S(weights[starIndex][i]);
                        removeRecipeBtnObjs[i].SetActive(true);
                    }
                    else
                    {
                        recipeIcons[i].sprite = noRecipeSelectedSprit;
                        recipeSelectTips[i].SetActive(true);
                        sliderObjs[i].SetActive(false);
                        speedTextObjs[i].SetActive(false);
                        removeRecipeBtnObjs[i].SetActive(false);
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
                lockSliderListener = false;
            }
            else
            {
                GigaFactoryUIObj.SetActive(false);
                showHideButtonObj.SetActive(false);
            }
        }

        public static void UIFrameUpdate(long timei)
        {
            RefreshStorageText();
            if (timei % 60 == 0)
            {
                RefreshProduceSpeedContent();
                RefreshUI();
            }
        }

        public static void RefreshProduceSpeedContent()
        {
            if (MoreMegaStructure.curStar == null) return;
            int starIndex = MoreMegaStructure.curStar.index;
            int maxSlotIndex = CalcMaxSlotIndex(GameMain.data.dysonSpheres[starIndex]);
            if (MoreMegaStructure.StarMegaStructureType[starIndex] != 4) return;
            for (int i = 1; i < slotCount; i++)
            {
                if (recipeIds[starIndex][i] > 0)
                {
                    double PPM = (GameMain.data.dysonSpheres[starIndex].energyGenCurrentTick - GameMain.data.dysonSpheres[starIndex].energyReqCurrentTick) * weights[starIndex][i] / tickEnergyForFullSpeed / timeSpend[starIndex][i] * 3600 * productCounts[starIndex][i][0];
                    string value = PPM > 10 ? PPM.ToString("N0") : (PPM > 1 ? PPM.ToString("N1") : (PPM > 0 ? PPM.ToString("N2") : "0.00"));
                    string incStr = "";
                    if (currentStarIncs[i] > 0)
                    {
                        int inc = currentStarIncs[i] > 10?10: currentStarIncs[i];
                        incStr = $"<color=#FD965EE0>  +{Cargo.incTableMilli[inc] * 100} %</color>";
                    }
                    produceSpeedTxts[i].text = "理论最大速度".Translate() + " " +  value + "/min" + incStr;
                    weightTxts[i].text = "能量分配".Translate() + " " + ((weights[starIndex][i] * 100)).ToString() + "%";
                }
                else
                {
                    produceSpeedTxts[i].text = "";
                }
            }
            double PPM2 = (GameMain.data.dysonSpheres[starIndex].energyGenCurrentTick - GameMain.data.dysonSpheres[starIndex].energyReqCurrentTick) * weights[starIndex][0] / MoreMegaStructure.multifunctionComponentHeat * 3600;
            string value2 = PPM2 > 10 ? PPM2.ToString("N0") : (PPM2 > 1 ? PPM2.ToString("N1") : (PPM2 > 0 ? PPM2.ToString("N2") : "0.00"));
            string remoteTransportingStr = "";
            if (MoreMegaStructure.isRemoteReceiveingGear)
                remoteTransportingStr = "已开启优先传输至机甲".Translate();
            produceSpeedTxts[0].text = "理论最大速度".Translate() + " " + value2 + "/min   " + remoteTransportingStr;
            weightTxts[0].text = "剩余能量".Translate() + " " + ((weights[starIndex][0] * 100)).ToString() + "%";

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
            currentStarIncs[slotIndex] = 0;
            double total = 1;
            for (int i = 1; i < slotCount; i++)
            {
                total -= weights[starIndex][i];
            }
            weights[starIndex][0] = total;
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

            RefreshUI();
        }

        /// <summary>
        /// 调整能量分配滑动条
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <param name="sv"></param>
        public static void OnSliderValueChange(int slotIndex, float sv)
        {
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
            if (GigaFactoryUIObj.activeSelf)
                GigaFactoryUIObj.SetActive(false);
            else
            {
                GigaFactoryUIObj.SetActive(true);
                RefreshUI();
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
                if (MoreMegaStructure.Support1000Stars.Value && support1000 > 0) // 如果记录了，且设置中也打开了1000星系支持，则读取后续数据
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
