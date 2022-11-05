﻿using System;
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

        // 以下为不需要存档的数据，在载入时重置或者重新计算

        public static Dictionary<int, List<List<int>>> items = new Dictionary<int, List<List<int>>>(); // 存储recipe的原材料的Id
        public static Dictionary<int, List<List<int>>> products = new Dictionary<int, List<List<int>>>(); // 存储recipe的产物的Id
        public static Dictionary<int, List<List<int>>> itemCounts = new Dictionary<int, List<List<int>>>(); // 存储recipe的原材料的需求数量
        public static Dictionary<int, List<List<int>>> productCounts = new Dictionary<int, List<List<int>>>(); // 存储recipe的产物的产出数量
        public static Dictionary<int, List<int>> timeSpend = new Dictionary<int, List<int>>(); // 存储recipe的所需时间
        public static Dictionary<int, Dictionary<int, int>> productStorage = new Dictionary<int, Dictionary<int, int>>(); // 存储产物已暂时堆积在巨构中的数量（可供相同星际组装厂的其他需要此产物作为原材料的配方取用），不区分slot只按照产物Id存储。不进行存档，读档后重置。
        // 上述productStorage项会存在：如果反复疯狂更换recipe会一直增加字典项，可能拖慢速度，但是重进游戏后冗余key会自动清除，因此暂时不做游戏内清理

        public static int currentStarId = 0; // 选择recipe时暂存所选定的是哪一个star的组装厂
        public static int currentRecipeSlot = 0; // 选择recipe时暂存所选定的是第几个recipe栏位

        public static int slotCount = 5;

        public static GameObject GigaFactoryUIObj = null;

        public static List<Image> recipeIcons = new List<Image>();
        public static List<GameObject> recipeSelectTips = new List<GameObject>();
        public static List<Text> produceSpeedTxts = new List<Text>();
        public static List<GameObject> sliderObjs = new List<GameObject>();
        public static List<Slider> sliders = new List<Slider>();
        public static List<Text> weightTxts = new List<Text>();
        public static Sprite noRecipeSelectedSprit = null;

        public static bool lockSliderListener = false;

        // 以下是固定参数
        public static int tickEnergyForFullSpeed = 20000; // 每相当于1.0倍速的产量需要的tick能量
        public static double matrixSpeedRatio = 0.01; // 用星际组装厂生产矩阵的速度修正倍率，这是为了不让该巨构部分替代科学枢纽

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
            for (int starIndex = 0; starIndex < 100; starIndex++)
            {
                recipeIds.Add(new List<int> { 0, 0, 0, 0, 0 });
                weights.Add(new List<double> { 1, 0, 0, 0, 0 });
                progress.Add(new List<double> { 0, 0, 0, 0, 0 });
                incProgress.Add(new List<double> { 0, 0, 0, 0, 0 });
            }
        }

        public static void InitInGameData()
        {
            for (int starIndex = 0; starIndex  < 100; starIndex ++)
            {
                if (MoreMegaStructure.StarMegaStructureType[starIndex] == 4)
                {
                    items[starIndex] = new List<List<int>>();
                    products[starIndex] = new List<List<int>>();
                    itemCounts[starIndex] = new List<List<int>>();
                    productCounts[starIndex] = new List<List<int>>();
                    timeSpend[starIndex] = new List<int> { 0, 0, 0, 0, 0 };
                    productStorage[starIndex] = new Dictionary<int, int>();
                    productStorage[starIndex][9500] = 0;
                    for (int s = 0; s < 5; s++)
                    {
                        items[starIndex].Add(new List<int>());
                        itemCounts[starIndex].Add(new List<int>());
                        products[starIndex].Add(new List<int>());
                        productCounts[starIndex].Add(new List<int>());
                    }

                    for (int i = 0; i < 5; i++)
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
                        }
                    }
                }
            }
            currentStarId = 0;
            currentRecipeSlot = 0;
            lockSliderListener = false;
        }

        public static void ResetInGameDataByStarIndex(int starIndex)
        {
            items[starIndex] = new List<List<int>>();
            products[starIndex] = new List<List<int>>();
            itemCounts[starIndex] = new List<List<int>>();
            productCounts[starIndex] = new List<List<int>>();
            timeSpend[starIndex] = new List<int> { 0, 0, 0, 0, 0 };
            productStorage[starIndex] = new Dictionary<int, int>();
            productStorage[starIndex][9500] = 0;
            for (int s = 0; s < 5; s++)
            {
                items[starIndex].Add(new List<int>());
                itemCounts[starIndex].Add(new List<int>());
                products[starIndex].Add(new List<int>());
                productCounts[starIndex].Add(new List<int>());
            }

            for (int i = 0; i < 5; i++)
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

                GameObject oriSelectObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/offwork");
                GameObject oriSliderObj = GameObject.Find("UI Root/Overlay Canvas/In Game/FPS Stats/priority-bar/slider-2");

                for (int i = 0; i < 5; i++)
                {
                    GameObject slotObj = new GameObject("slot" + i.ToString());
                    slotObj.transform.SetParent(GigaFactoryUIObj.transform);
                    slotObj.transform.localScale = new Vector3(1, 1, 1);
                    slotObj.transform.localPosition = new Vector3(0, i * 80, 0);

                    GameObject recipeSelectionObj = GameObject.Instantiate(oriSelectObj, slotObj.transform);
                    recipeSelectionObj.SetActive(true);
                    recipeSelectionObj.transform.Find("tip-group/paste-button").gameObject.SetActive(false);

                    GameObject circleButtonObj = recipeSelectionObj.transform.Find("circle").gameObject;
                    recipeIcons.Add(circleButtonObj.GetComponent<Image>());
                    noRecipeSelectedSprit = circleButtonObj.GetComponent<Image>().sprite;
                    recipeSelectTips.Add(recipeSelectionObj.transform.Find("tip-group").gameObject);
                    Button circleButton = circleButtonObj.GetComponent<Button>();
                    circleButton.onClick.RemoveAllListeners();
                    

                    GameObject produceSpeedTxtObj = GameObject.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/circle-back/cnt-text"), slotObj.transform);
                    produceSpeedTxtObj.transform.localPosition = new Vector3(110, -136);
                    produceSpeedTxts.Add(produceSpeedTxtObj.GetComponent<Text>());
                    produceSpeedTxtObj.GetComponent<Text>().text = "理论最大速度".Translate() + " --/min";
                    produceSpeedTxtObj.GetComponent<Text>().alignment = TextAnchor.LowerLeft;

                    GameObject sliderObj = GameObject.Instantiate(oriSliderObj, slotObj.transform);
                    sliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 20);
                    sliderObj.transform.localPosition = new Vector3(150, -100, 0);
                    sliderObj.SetActive(false);
                    sliderObjs.Add(sliderObj);
                    Slider slider = sliderObj.GetComponent<Slider>();
                    slider.minValue = 0;
                    slider.maxValue = 100;
                    slider.value = 0;
                    slider.onValueChanged.RemoveAllListeners();
                    sliders.Add(slider);

                    GameObject weightTxtObj = GameObject.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Assembler Window/produce/circle-back/cnt-text"), sliderObj.transform);
                    weightTxtObj.transform.localPosition = new Vector3(200, -12, 0);
                    weightTxts.Add(weightTxtObj.GetComponent<Text>());
                    weightTxtObj.GetComponent<Text>().fontSize = 16;
                    weightTxtObj.GetComponent<Text>().text = "";
                    weightTxtObj.GetComponent<Text>().alignment = TextAnchor.LowerLeft;

                    // 这里不能直接传入i，否则会导致所有的参数都变成了5
                    if (i == 0) 
                    {
                        // circleButton.onClick.AddListener(() => { OnRecipeSelectClick(0); });
                        // slider.onValueChanged.AddListener((x) => { OnSliderValueChange(0, x); });
                        slider.interactable = false;
                    }
                    else if (i == 1)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(1); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(1, x); });
                    }
                    else if (i == 2)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(2); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(2, x); });
                    }
                    else if (i == 3)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(3); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(3, x); });
                    }
                    else if (i == 4)
                    {
                        circleButton.onClick.AddListener(() => { OnRecipeSelectClick(4); });
                        slider.onValueChanged.AddListener((x) => { OnSliderValueChange(4, x); });
                    }
                }

            }
        }

        public static void InternalUpdate(DysonSphere __instance)
        {
            int starIndex = __instance.starData.index;
            long energy = __instance.energyGenCurrentTick - __instance.energyReqCurrentTick;

            // 生产进度计算
            for (int i = 1; i < 5; i++)
            {
                if (recipeIds[starIndex][i] > 0)
                {
                    double prog = energy * weights[starIndex][i] / tickEnergyForFullSpeed / (60 * timeSpend[starIndex][i]);
                    progress[starIndex][i] += prog;
                }
            }
            progress[starIndex][0] += energy * weights[starIndex][0] / MoreMegaStructure.multifunctionComponentHeat * (MoreMegaStructure.isRemoteReceiveingGear ? 0.1 : 1.0);
            
            // 生产进度填满，则立刻消耗原材料并根据消耗产出产物，存入巨构的产物暂存storage内
            for (int i = 1; i < 5; i++)
            {
                if (progress[starIndex][i] >= 1)
                {
                    if (recipeIds[starIndex][i] > 0)
                    {
                        int minSatisfied = (int)progress[starIndex][i]; // 用于指示获取到的原材料是否满足，如果不满足总是取最低的作为最终产出的参照
                        int minInc = 10; // 指示获取的增产等级，总是以最低的为准

                        for (int j = 0; j < items[starIndex][i].Count; j++) // 开始尝试取得每个原材料
                        {
                            int gotInc = 0;

                            int gotItem = TakeItemForFactory(starIndex, items[starIndex][i][j], minSatisfied * itemCounts[starIndex][i][j], out gotInc);
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
                            }
                            // 增产效果
                            incProgress[starIndex][i] += minSatisfied * Cargo.incTableMilli[minInc];
                            if (incProgress[starIndex][i] >= 1)
                            {
                                int bonusProduct = (int)incProgress[starIndex][i];
                                for (int j = 0; j < products[starIndex][i].Count; j++)
                                {
                                    productStorage[starIndex][products[starIndex][i][j]] += bonusProduct * productCounts[starIndex][i][j];
                                    if (productStorage[starIndex][products[starIndex][i][j]] > 10000) productStorage[starIndex][products[starIndex][i][j]] = 10000;
                                }
                                incProgress[starIndex][i] -= (int)incProgress[starIndex][i];
                            }
                        }

                        progress[starIndex][i] -= (int)progress[starIndex][i];
                    }
                    else
                    {
                        progress[starIndex][i] = 0;
                    }
                }
            }
            if (progress[starIndex][0] >= 1)
            {
                if (productStorage[starIndex].ContainsKey(9500))
                    productStorage[starIndex][9500] = Math.Min(productStorage[starIndex][9500] + (int)progress[starIndex][0], 10000);
                else
                    productStorage[starIndex][9500] = Math.Min((int)progress[starIndex][0], 10000);

                progress[starIndex][0] -= (int)progress[starIndex][0];
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

        public static int TakeItemForFactory(int starIndex, int itemId, int itemCount, out int inc)
        {
            int result = 0;
            inc = 0;
            // 先从productStorage里面拿，从此处拿的视为最高默认增产等级
            if (productStorage[starIndex].ContainsKey(itemId))
            {
                result = Math.Min(itemCount, productStorage[starIndex][itemId]);
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
                                int protoId = factory.entityPool[stationComponent.entityId].protoId;
                                if (protoId != 9512) continue; // 只有物资交换建筑

                                for (int k = 0; k < 5; k++)
                                {
                                    if (stationComponent.storage[k].itemId == itemId && stationComponent.storage[k].count>0)
                                    {
                                        int oriInc = stationComponent.storage[k].inc / stationComponent.storage[k].count;
                                        int need = itemCount - result;
                                        if (stationComponent.storage[k].count > need)
                                        {
                                            stationComponent.storage[k].count -= need;
                                            stationComponent.storage[k].inc -= oriInc * need;
                                            result += need;
                                            inc += oriInc * need;
                                            return result;
                                        }
                                        else
                                        {
                                            result += stationComponent.storage[k].count;
                                            inc += stationComponent.storage[k].inc;
                                            stationComponent.storage[k].count = 0;
                                            stationComponent.storage[k].inc = 0;
                                        }

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

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
                            int protoId = factory.entityPool[stationComponent.entityId].protoId;
                            if (protoId != 9512) continue; // 只有物资交换建筑

                            for (int k = 0; k < 5; k++)
                            {
                                int productId = stationComponent.storage[k].itemId;
                                if (productStorage[starIndex].ContainsKey(productId) && productStorage[starIndex][productId]>0)
                                {
                                    int waitingToSend = productStorage[starIndex][productId];
                                    if (stationComponent.storage[k].max - stationComponent.storage[k].count >= waitingToSend)
                                    {
                                        stationComponent.storage[k].count += waitingToSend;
                                        productStorage[starIndex][productId] = 0;
                                    }
                                    else
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

        public static void RefreshUI()
        {
            if (MoreMegaStructure.curStar == null) return;
            int starIndex = MoreMegaStructure.curStar.index;
            if (MoreMegaStructure.StarMegaStructureType[starIndex] == 4)
            {
                GigaFactoryUIObj.SetActive(true);
                for (int i = 1; i < 5; i++)
                {
                    if (recipeIds[starIndex][i] > 0)
                    {
                        recipeIcons[i].sprite = LDB.recipes.Select(recipeIds[starIndex][i]).iconSprite;
                        recipeSelectTips[i].SetActive(false);
                        sliderObjs[i].SetActive(true);
                        sliders[i].value = (float)weights[starIndex][i]*100;
                    }
                    else
                    {
                        recipeIcons[i].sprite = noRecipeSelectedSprit;
                        recipeSelectTips[i].SetActive(true);
                        sliderObjs[i].SetActive(false);
                    }
                }
                sliderObjs[0].SetActive(true);
                recipeSelectTips[0].SetActive(false);
                recipeIcons[0].sprite = MoreMegaStructure.iconInterCompo;
                sliders[0].value = (float)weights[starIndex][0] * 100;

                RefreshProduceSpeedText();
            }
            else
            {
                GigaFactoryUIObj.SetActive(false);
            }
        }

        public static void RefreshProduceSpeedText()
        {
            if (MoreMegaStructure.curStar == null) return;
            int starIndex = MoreMegaStructure.curStar.index;
            for (int i = 1; i < 5; i++)
            {
                if (recipeIds[starIndex][i] > 0)
                {
                    produceSpeedTxts[i].text = "理论最大速度".Translate() + " 999/min"; // 需要修改！！
                    weightTxts[i].text = "能量分配".Translate() + " " + ((int)(weights[starIndex][i] * 100)).ToString() + "%";
                }
                else
                {
                    produceSpeedTxts[i].text = "";
                }
            }
            produceSpeedTxts[0].text = "理论最大速度".Translate() + " 75/min"; // 需要修改！！
            weightTxts[0].text = "剩余能量".Translate() + " " + ((int)(weights[starIndex][0] * 100)).ToString() + "%";
        }

        public static void OnRecipeSelectClick(int slotIndex)
        {
            currentRecipeSlot = slotIndex;
            UIRecipePicker.Popup(new Vector2(0f, 0f), new Action<RecipeProto>(OnRecipePickerReturn));
        }

        /// <summary>
        /// 选择好了配方后该窗口将调用此函数
        /// </summary>
        /// <param name="recipe"></param>
        public static void OnRecipePickerReturn(RecipeProto recipe)
        {
            int starIndex = MoreMegaStructure.curStar.index;
            if (starIndex > 99)
            {
                UIRealtimeTip.Popup("警告巨构不支持恒星系数量大于100个".Translate());
                return;
            }
            Utils.Log("starIndex = " + starIndex + " and curSlot = " + currentRecipeSlot);
            recipeIds[starIndex][currentRecipeSlot] = recipe.ID;
            progress[starIndex][currentRecipeSlot] = 0;
            if (!items.ContainsKey(starIndex))
            {
                items.Add(starIndex, new List<List<int>>());
                products[starIndex] = new List<List<int>>();
                itemCounts[starIndex] = new List<List<int>>();
                productCounts[starIndex] = new List<List<int>>();
                timeSpend[starIndex] = new List<int> { 0, 0, 0, 0, 0 };
                productStorage[starIndex] = new Dictionary<int, int>();
                productStorage[starIndex][9500] = 0;
                for (int s = 0; s < 5; s++)
                {
                    items[starIndex].Add(new List<int>());
                    itemCounts[starIndex].Add(new List<int>());
                    products[starIndex].Add(new List<int>());
                    productCounts[starIndex].Add(new List<int>());
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

            RefreshUI();
        }

        public static void OnSliderValueChange(int slotIndex, float sv)
        {
            if (MoreMegaStructure.curStar == null || lockSliderListener) return;
            int starIndex = MoreMegaStructure.curStar.index;
            int use = 0;
            for (int i = 1; i < 5; i++)
            {
                use += (int)sliders[i].value;
            }
            if (use <= 100) // 若总分配不足100%，未使用的分配将给0号slot，即多功能集成组件
            {
                sliders[0].value = 100 - use;
                for (int i = 0; i < 5; i++)
                {
                    weights[starIndex][i] = sliders[i].value / 100f;
                }
            }
            else // 若总分配超过100%，除了保证当前的分配不变之外，其他的按比例缩减直到总分配==100%
            {
                lockSliderListener = true;
                sliders[0].value = 0;
                weights[starIndex][0] = 0;
                int otherUse = 0;
                for (int i = 1; i < 5; i++)
                {
                    if (i != slotIndex)
                    {
                        otherUse += (int)sliders[i].value;
                    }
                }
                float ratio = (100f - sliders[slotIndex].value) / otherUse;
                for (int i = 1; i < 5; i++)
                {
                    if (i != slotIndex)
                    {
                        sliders[i].value = sliders[i].value * ratio;
                    }
                    weights[starIndex][i] = sliders[i].value / 100f;
                }
            }

            RefreshProduceSpeedText();
            lockSliderListener = false;
        }

        public static void Import(BinaryReader r)
        {
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    recipeIds[i][j] = r.ReadInt32();
                    weights[i][j] = r.ReadDouble();
                    progress[i][j] = r.ReadDouble();
                    incProgress[i][j] = r.ReadDouble();
                }
            }
        }

        public static void Export(BinaryWriter w)
        {
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    w.Write(recipeIds[i][j]);
                    w.Write(weights[i][j]);
                    w.Write(progress[i][j]);
                    w.Write(incProgress[i][j]);
                }
            }
        }

        public static void IntoOtherSave()
        {
            ResetAndInitArchiveData();
            InitInGameData();
        }
    }
}