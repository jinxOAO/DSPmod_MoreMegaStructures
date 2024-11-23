using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using BepInEx;
using BepInEx.Configuration;
//using BuildMenuTool;
using CommonAPI;
using CommonAPI.Systems;
using CommonAPI.Systems.ModLocalization;
using crecheng.DSPModSave;
using HarmonyLib;
using MonoMod.Cil;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using xiaoye97;
using static UnityEngine.EventSystems.EventTrigger;

namespace MoreMegaStructure
{
    [BepInDependency(LDBToolPlugin.MODGUID)]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [BepInDependency(DSPModSavePlugin.MODGUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(TabSystem), nameof(LocalizationModule))]
    [BepInPlugin("Gnimaerd.DSP.plugin.MoreMegaStructure", "MoreMegaStructure", "1.7.0")]
    public class MoreMegaStructure : BaseUnityPlugin, IModCanSave
    {
        /// <summary>
        /// mod版本会进行存档
        /// </summary>
        public static int modVersion = 160;

        public static int savedModVersion = 160;

        public static bool CompatibilityPatchUnlocked = false;

        public static bool GenesisCompatibility;
        public static bool isBattleActive;
        public static bool TCFVCompatibility = false;

        public static int megaNum = 7; // 巨构类型的数量

        public static bool developerMode = false; // 发布前修改！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！

        //private static Sprite iconAntiInject;
        public static List<int> RelatedGammas;
        public static string GUID = "Gnimaerd.DSP.plugin.MoreMegaStructure";
        public static string MODID_tab = "MegaStructures";
        public static int pagenum = 3;
        public static int battlePagenum = 3;

        public static long
            HashGenDivisor = 40000000L; //巨构能量转换为哈希点数的除数，每帧hash = 巨构每帧能量 / 此值 * (HashBaseSpeedScale + 1)，每级研究速度科技则等于巨构每帧能量 / 此值 * HashBonusPerLevel

        public static int HashBasicSpeedScale = 99;
        public static int HashBonusPerLevel = 1;
        public static long WarpAccDivisor = 10000000L; //计算曲速倍率加成时，每帧巨构能量先除以这个数。如果是10^6，代表每60MW提供100%曲速。此值越大，每MW提供的加速效果越少。
        public static int WarpAccMax = 5000;           //巨构提供的最大曲速倍数

        public static long multifunctionComponentHeat = 4500000000; //接收多功能组件的比例数值

        //public static Color defaultType = new Color(0.566f, 0.915f, 1f, 0.07f); //原本是按钮的颜色，后来因为UIButton的鼠标移入移出事件我不会改，那个会扰乱按钮颜色的设定，所以不再用按钮颜色，改用文字颜色
        //public static Color currentType = new Color(0.95f, 0.68f, 0.5f, 0.15f);
        //public static Color disableType = new Color(0.4f, 0.4f, 0.4f, 0.85f); 
        public static Color normalTextColor = new Color(1f, 1f, 1f, 1f);
        public static Color currentTextColor = new Color(1f, 0.75f, 0.1f, 1f);
        public static Color disableTextColor = new Color(0.75f, 0.75f, 0.75f, 1f);

        public static bool isRemoteReceiveingGear;

        public static ConfigEntry<bool> NoUIAnimation;
        public static ConfigEntry<double> IASpdFactor;
        public static ConfigEntry<bool> NonlinearEnergy;
        public static ConfigEntry<bool> Support1000Stars;
        public static ConfigEntry<bool> NoWasteResources;
        public static ConfigEntry<bool> ReverseStarCannonShellAlignDirection;
        public static ConfigEntry<bool> ShowIAUIWhenOpenDE;
        public static ConfigEntry<bool> HideWarpFieldUI;
        public static bool resolutionLower1080 = false;

        public static ResourceData resources;
        public static ResourceData iconResources;
        public static Sprite iconRocketMattD;
        public static Sprite iconRocketScieN;
        public static Sprite iconRocketWarpG;
        public static Sprite iconRocketMegaA;
        public static Sprite iconRocketCrysM;
        public static Sprite iconGravityGen;
        public static Sprite iconConstrainRing;
        public static Sprite iconGravityDrill;
        public static Sprite iconIACompo;
        public static Sprite iconInterCompo;
        public static Sprite iconPhotonProbe;
        public static Sprite iconQuanComp;
        public static Sprite iconResDisc;
        public static Sprite iconTunnExciter;
        public static Sprite iconQuickAssembly;
        public static Sprite iconQuickBelt;
        public static Sprite iconQuickChemical;
        public static Sprite iconQuickCollider;
        public static Sprite iconQuickLab;
        public static Sprite iconQuickPLog;
        public static Sprite iconQuickILog;
        public static Sprite iconQuickPower;
        public static Sprite iconQuickReactor;
        public static Sprite iconQuickRefinery;
        public static Sprite iconQuickSmelter;
        public static Sprite iconQuickSorter;
        public static Sprite iconHashes;
        public static Sprite iconWarpBroadcast;
        public static Sprite iconReceiverIron;
        public static Sprite iconReceiverCopper;
        public static Sprite iconReceiverTitanium;
        public static Sprite iconReceiverSilicon;
        public static Sprite iconReceiverMagore;
        public static Sprite iconReceiverCasimir;
        public static Sprite iconReceiverIC;
        public static Sprite iconReceiverScience;
        public static Sprite iconReceiverWarp;
        public static Sprite iconReceiverCoal;
        public static Sprite iconReceiverGrating;

        public static Text LeftDysonCloudTitle;
        public static Text LeftDysonCloudBluePrintText;
        public static Text LeftDysonShellTitle;
        public static Text LeftDysonShellBluePrintText;
        public static Text LeftDysonShellOrbitTitleText;
        public static Text RightDysonTitle;
        public static Text RightStarPowRatioText;
        public static Text RightMaxPowGenText;
        public static Text RightMaxPowGenValueText;
        public static Text RightDysonBluePrintText;
        public static Text SpSailAmountText;
        public static Text SpNodeAmountText;
        public static Text SpSailLifeTimeText;
        public static Text SpSailLifeTimeBarText;
        public static Text SpConsumePowText;
        public static Text SpSwarmPowGenText;
        public static Text SpShellPowGenText;
        public static Text SpEnergySatisfiedLabelText;
        public static Text SoSailAmountText;
        public static Text SoSailPowGenText;
        public static Text SoSailLifeTimeText;
        public static Text SoSailLifeTimeBarText;
        public static Text LyPowGenText;
        public static Text NdPowGenText;
        public static Text FrPowGenText;
        public static Text ShPowGenText;
        public static Text ReceiverUIButton2Text; //接收器UI的模式2按钮的文本

        public static GameObject setMegaGroupObj;
        public static GameObject set2DysonButtonObj;
        public static GameObject set2MatDecomButtonObj;
        public static GameObject set2SciNexusButtonObj;
        public static GameObject set2WarpFieldGenButtonObj;
        public static GameObject set2MegaAssemButtonObj;
        public static GameObject set2CrystalMinerButtonObj;
        public static GameObject set2StarCannonButtonObj;
        public static GameObject rightBarObj;
        public static GameObject LeftMegaBuildWarning;
        public static GameObject DysonEditorPowerDescLabel4BarObj;
        public static GameObject selectAutoReceiveGearLimitObj;
        public static GameObject selectAutoReceiveGearLimitLabelObj;
        public static GameObject selectAutoReceiveGearLimitComboBoxObj;
        public static Button set2DysonButton;
        public static Button set2MatDecomButton;
        public static Button set2SciNexusButton;
        public static Button set2WarpFieldGenButton;
        public static Button set2MegaAssemButton;
        public static Button set2CrystalMinerButton;
        public static Button set2StarCannonButton;
        public static Transform set2DysonButtonTextTrans;
        public static Transform set2MatDecomButtonTextTrans;
        public static Transform set2SciNexusButtonTextTrans;
        public static Transform set2WarpFieldGenButtonTextTrans;
        public static Transform set2MegaAssemButtonTextTrans;
        public static Transform set2CrystalMinerButtonTextTrans;
        public static Transform set2StarCannonButtonTextTrans;
        public static UIComboBox selectAutoReceiveGearLimitComboBox;
        public static Material setButtonTextNormMat;
        public static Material setButtonTextAlphaMat;

        public static Text SetMegaStructureLabelText;
        public static Text SetMegaStructureWarningText;

        //public static bool UIDysonEditorIsOn = false; //戴森球编辑界面是否处于打开状态

        public static StarData curStar;           //编辑巨构建筑页面当前显示的恒星的数据
        public static DysonSphere curDysonSphere; //戴森球编辑界面正在浏览的戴森球
        public static int WarpBuiltStarIndex;     //折跃场已经在该地址的恒星上建造过了
        public static int CannonBuiltStarIndex;   //恒星炮已经在该地址的恒星上建造过了
        public static long hashGenByAllSN;        //每帧计算，所有科学枢纽生成的hash总和，用于提供元数据
        public static int resolutionY = 1080;
        //public static bool inLogged = false;
        public static System.Random randSeed = new System.Random();

        /// <summary>
        /// 下面的数据为游戏运行时的关键数据，且会进行存档
        /// </summary>
        public static int[] StarMegaStructureType = new int[1000]; //用于存储每个恒星所构建的巨构建筑类型，默认为0则为戴森球，1物质解压器，2科学枢纽，3折跃场，4星际组装厂，5晶体重构器，6恒星炮

        public static int maxAutoReceiveGear = 1000;
        public static long autoReceiveGearProgress;

        public static int pilerLvl = 1;

        // 测试用
        public static bool KeyNPressTime = false;
        public static int testHitIndex = 0;

        public void Awake()
        {
            try
            {
                using (ProtoRegistry.StartModLoad(GUID))
                {
                    //Initilize new instance of ResourceData class.
                    string pluginfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    resources = new ResourceData(GUID, "MegaStructureTab",
                                                 pluginfolder); // Make sure that the keyword you are using is not used by other mod authors.
                    resources.LoadAssetBundle("mmstabicon");    // Load asset bundle located near your assembly
                    //resources.ResolveVertaFolder(); // Call this to resolver verta folder. You don't need to call this if you are not using .verta files 
                    ProtoRegistry.AddResource(resources); // Add your ResourceData to global list of resources
                    pagenum = TabSystem.RegisterTab($"{MODID_tab}:{MODID_tab}Tab",
                                                    new TabData("MegaStructures", "Assets/MegaStructureTab/megaStructureTabIcon"));
                }
            }
            catch (Exception)
            {
                pagenum = TabSystem.RegisterTab($"{MODID_tab}:{MODID_tab}Tab",
                                                new TabData("MegaStructures", "Assets/MegaStructureTab/megaStructureTabIcon"));
            }

            battlePagenum = pagenum; //深空来敌mod开启后将使用battlePagenum
            NoUIAnimation = Config.Bind("config", "NoUIAnimation", false,
                                              "Turn this to true if your want to show and hide buttons without animations. 如果你想让按钮的出现和隐藏没有动画立即完成，将此项设置为true。");
            IASpdFactor = Config.Bind("config", "InterstellarAssemblySpeedFactor", 0.2,
                                              "Higher will make the interstellar assembly work faster with the same energy. 在同样的能量水平下，此项越高，星际组装厂的工作速度越快。可以是小数。");
            var NonlinearEnergyOld = Config.Bind("config", "NonlinearEnergyAssignmentAdjust", true, "This config is no longer working. 此项已弃用。");
            NonlinearEnergy = Config.Bind("config", "NonlinearEnergyAssignmentAdjust2", true,
                                                "Turn this to true will let you adjust the energy allocation of the Interstellar Assembly more finely within the range of lower value. 将此项设置为true能够使你在调整星际组装厂配方的能量分配时，在较低分配比例的区间内更加精细地调整。");
            Support1000Stars = Config.Bind("config", "Support1000Stars", false,
                                                 "Turn this to true will let the Interstellar Assemblies support upto 1000 stars (default is 100), but this might slow down your game or your save/load speed. 将此项设置为true能够使星际组装厂支持最多1000个星系（默认只支持100以下），但这可能使你的游戏速度或存读档速度被拖慢。");
            NoWasteResources = Config.Bind("config", "NoWasteResources", true,
                                                 "Turn this to false might slightly increase the game speed. But this will cause: if one of the various materials required by a recipe in Interstellar Assembly is insufficient, (its supply cannot meet the speed of full-speed production). Although the actual output will slow down, other sufficient materials may still be consumed at full speed, which means that they may be wasted.  将此项设置为false可能会轻微提升游戏速度，但这会导致：当星际组装厂中的部分原材料不支持满速消耗时，虽然产出速度按照最低供应原材料的速度为准，但其他充足供应的原材料仍被满速消耗而产生浪费。");
            ReverseStarCannonShellAlignDirection = Config.Bind("config", "ReverseStarCannonShellAlignDirection", false, "Turn this to true will reverse the align direction of all the shell of star cannon when firing, which means the south pole (of the shells) will point to the target star rather than the north pole.  将此项设置为true会反转恒星炮开火时壳层的对齐方向，这意味着所有壳层的南极将指向目标恒星开火（而非默认的北极）。如果你的炮口造反了，可以尝试更改此项设置。");
            ShowIAUIWhenOpenDE = Config.Bind("config", "AutoShowDEUI", true, "Set this to true will force to show the Interstellar Assembly's UI when opening/switching its Megastructure Editor Panel. Set to false will maintain the IA UI's last state. 将此项设置为true将在每次打开星际组装厂的巨构编辑器面板时，强制显示UI。设置为false则会维持上次的状态。");

            HideWarpFieldUI = Config.Bind("config", "HideWarpFieldUI", false, "Hide the warp field area in starmap UI. 是否隐藏星图界面的折跃场范围显示。");


            //var ab = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("MoreMegaStructure.megastructureicons"));
            iconRocketMattD = Resources.Load<Sprite>("Assets/MegaStructureTab/rocketMatter");
            iconRocketScieN = Resources.Load<Sprite>("Assets/MegaStructureTab/rocketScience");
            iconRocketWarpG = Resources.Load<Sprite>("Assets/MegaStructureTab/rocketWarp");
            iconRocketMegaA = Resources.Load<Sprite>("Assets/MegaStructureTab/rocketAssembly");
            iconRocketCrysM = Resources.Load<Sprite>("Assets/MegaStructureTab/rocketCrystal");
            iconGravityGen = Resources.Load<Sprite>("Assets/MegaStructureTab/gravitygenerator");
            iconConstrainRing = Resources.Load<Sprite>("Assets/MegaStructureTab/constrainring");
            iconGravityDrill = Resources.Load<Sprite>("Assets/MegaStructureTab/gravitydrill2");
            iconIACompo = Resources.Load<Sprite>("Assets/MegaStructureTab/iacomponent");
            iconInterCompo = Resources.Load<Sprite>("Assets/MegaStructureTab/integratedcomponents");
            iconPhotonProbe = Resources.Load<Sprite>("Assets/MegaStructureTab/photonprobeflipsmall");
            iconQuanComp = Resources.Load<Sprite>("Assets/MegaStructureTab/quantumcomputer3");
            iconResDisc = Resources.Load<Sprite>("Assets/MegaStructureTab/resonancedisc");
            iconTunnExciter = Resources.Load<Sprite>("Assets/MegaStructureTab/tunnelingexciter2");
            iconQuickAssembly = Resources.Load<Sprite>("Assets/MegaStructureTab/Rassembly");
            iconQuickBelt = Resources.Load<Sprite>("Assets/MegaStructureTab/Rbelt");
            iconQuickChemical = Resources.Load<Sprite>("Assets/MegaStructureTab/Rchemical2");
            iconQuickCollider = Resources.Load<Sprite>("Assets/MegaStructureTab/Rcollider");
            iconQuickLab = Resources.Load<Sprite>("Assets/MegaStructureTab/Rlab");
            iconQuickPLog = Resources.Load<Sprite>("Assets/MegaStructureTab/Rlogistic");
            iconQuickILog = Resources.Load<Sprite>("Assets/MegaStructureTab/Rstellalogistic");
            iconQuickPower = Resources.Load<Sprite>("Assets/MegaStructureTab/Rpower");
            iconQuickReactor = Resources.Load<Sprite>("Assets/MegaStructureTab/Rreactor");
            iconQuickRefinery = Resources.Load<Sprite>("Assets/MegaStructureTab/Rrefinery");
            iconQuickSmelter = Resources.Load<Sprite>("Assets/MegaStructureTab/Rsmelter");
            iconQuickSorter = Resources.Load<Sprite>("Assets/MegaStructureTab/Rsorter");
            iconHashes = Resources.Load<Sprite>("Assets/MegaStructureTab/HashPoints");
            iconWarpBroadcast = Resources.Load<Sprite>("Assets/MegaStructureTab/WarpFieldBroadcast");
            iconReceiverIron = Resources.Load<Sprite>("Assets/MegaStructureTab/Ciron");
            iconReceiverCopper = Resources.Load<Sprite>("Assets/MegaStructureTab/Ccopper");
            iconReceiverTitanium = Resources.Load<Sprite>("Assets/MegaStructureTab/Ctitanium");
            iconReceiverSilicon = Resources.Load<Sprite>("Assets/MegaStructureTab/Csilicon");
            iconReceiverMagore = Resources.Load<Sprite>("Assets/MegaStructureTab/Cmagore");
            iconReceiverCasimir = Resources.Load<Sprite>("Assets/MegaStructureTab/Ccrystal");
            iconReceiverIC = Resources.Load<Sprite>("Assets/MegaStructureTab/Cic");
            iconReceiverScience = Resources.Load<Sprite>("Assets/MegaStructureTab/Cscience");
            iconReceiverWarp = Resources.Load<Sprite>("Assets/MegaStructureTab/Cwarp");
            iconReceiverCoal = Resources.Load<Sprite>("Assets/MegaStructureTab/Ccoal");
            iconReceiverGrating = Resources.Load<Sprite>("Assets/MegaStructureTab/Cgrating");

            Harmony.CreateAndPatchAll(typeof(MoreMegaStructure));
            Harmony.CreateAndPatchAll(typeof(StarCannon));
            //Harmony.CreateAndPatchAll(typeof(RendererSphere));
            //Harmony.CreateAndPatchAll(typeof(EffectRenderer));
            Harmony.CreateAndPatchAll(typeof(ReceiverPatchers));
            Harmony.CreateAndPatchAll(typeof(StarAssembly));
            Harmony.CreateAndPatchAll(typeof(UIReceiverPatchers));
            if (UIStatisticsPatcher.enabled) Harmony.CreateAndPatchAll(typeof(UIStatisticsPatcher));
            if (UIBuildMenuPatcher.enabled) Harmony.CreateAndPatchAll(typeof(UIBuildMenuPatcher));
            Harmony.CreateAndPatchAll(typeof(UIStarCannon));
            Harmony.CreateAndPatchAll(typeof(UIMechaWindowPatcher));
            Harmony.CreateAndPatchAll(typeof(PerformanceMonitorPatcher));
            Harmony.CreateAndPatchAll(typeof(UIPerformancePanelPatcher));
            Harmony.CreateAndPatchAll(typeof(UIDialogPatch));
            Harmony.CreateAndPatchAll(typeof(UIStationWindowPatcher));
            Harmony.CreateAndPatchAll(typeof(WarpArray));
            Harmony.CreateAndPatchAll(typeof(UIWarpArray));

            MMSProtos.ChangeReceiverRelatedStringProto();
            MMSProtos.AddTranslateUILabel();
            MMSProtos.AddTranslateStructureName();
            MMSProtos.AddTranslateProtoNames1();
            MMSProtos.AddTranslateProtoNames2();
            MMSProtos.AddTranslateProtoNames3();
            MMSProtos.AddTranslateProtoNames4();

            LDBTool.PreAddDataAction += MMSProtos.AddNewItems;
            LDBTool.PreAddDataAction += MMSProtos.AddNewItems2;
            LDBTool.PostAddDataAction += MMSProtos.AddGenesisRecipes;
            LDBTool.PostAddDataAction += MMSProtos.AddReceivers;
            LDBTool.PostAddDataAction += MMSProtos.RefreshInitAll;
            LDBTool.EditDataAction += MMSProtos.EditOriRR;

            if (CompatibilityPatchUnlocked)
            {
                AccessTools.StaticFieldRefAccess<ConfigFile>(typeof(LDBTool), "CustomID").Clear();
                AccessTools.StaticFieldRefAccess<ConfigFile>(typeof(LDBTool), "CustomGridIndex").Clear();
                AccessTools.StaticFieldRefAccess<ConfigFile>(typeof(LDBTool), "CustomStringZHCN").Clear();
                AccessTools.StaticFieldRefAccess<ConfigFile>(typeof(LDBTool), "CustomStringENUS").Clear();
                AccessTools.StaticFieldRefAccess<ConfigFile>(typeof(LDBTool), "CustomStringFRFR").Clear();
            }

            MMSPF.AddMMSSamples();
        }

        public void Start()
        {
            GetVanillaUITexts();
            InitMegaSetUI();
            LateInitOtherUI();
            StarAssembly.InitAll();
            ReceiverPatchers.InitRawData();
            UIReceiverPatchers.InitAll();
            if(UIBuildMenuPatcher.enabled) UIBuildMenuPatcher.InitAll();
            UIStarCannon.InitAll();
            UIStationWindowPatcher.InitAll();

            if (isBattleActive)
            {
                Harmony.CreateAndPatchAll(typeof(StarCannon));
            }
        }

        public void Update()
        {
            Vector3 mouseUIPos = Input.mousePosition;
            int deltaY = Math.Max(0, resolutionY - 1080);
            if (mouseUIPos.x <= MegaButtonGroupBehaviour.currentX + 280 && mouseUIPos.y <= 270 + deltaY)
            {
                MegaButtonGroupBehaviour.ShowSetMegaGroup();
            }
            else
            {
                MegaButtonGroupBehaviour.HideSetMegaGroup();
            }

            MegaButtonGroupBehaviour.SetMegaGroupMove();
            try
            {
                pilerLvl = GameMain.history.stationPilerLevel;
            }
            catch (Exception)
            {
                pilerLvl = 1;
            }

            if (Input.GetKeyDown(KeyCode.R) && UIRoot.instance.uiGame.starmap.active)
            {
                StarCannon.OnFireButtonClick();
            }

            // test
            if (developerMode)
            {
                if(Input.GetKeyDown(KeyCode.N))
                {
                    Utils.Log(DSPGame.globalOption.languageLCID.ToString());
                }
            }
        }

        public static void GetVanillaUITexts()
        {
            try
            {
                GameObject LeftCloud
                    = GameObject.Find(
                        "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy/swarm/title-text");
                GameObject LeftCloudBluePrint
                    = GameObject.Find(
                        "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy/swarm/blueprint/text");
                LeftDysonCloudTitle = LeftCloud.GetComponent<Text>();
                LeftDysonCloudBluePrintText = LeftCloudBluePrint.GetComponent<Text>();
                //戴森云 戴森云蓝图
            }
            catch (Exception)
            {
                //Console.WriteLine("GO Find Group 1 ERROR");
            }

            try
            {
                GameObject LeftShell
                    = GameObject.Find(
                        "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy/layers/title-text");
                GameObject LeftShellBluePrint
                    = GameObject.Find(
                        "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy/layers/blueprint-group/blueprint-text");
                GameObject LeftShellOrbitTitle
                    = GameObject.Find(
                        "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy/layers/buttons-group/orbit-title-text");
                LeftDysonShellTitle = LeftShell.GetComponent<Text>();
                LeftDysonShellBluePrintText = LeftShellBluePrint.GetComponent<Text>();
                LeftDysonShellOrbitTitleText = LeftShellOrbitTitle.GetComponent<Text>();
                //戴森壳 戴森壳蓝图
            }
            catch (Exception)
            {
                //Console.WriteLine("GO Find Group 2 ERROR");
            }

            try
            {
                RightDysonTitle = GameObject
                                 .Find(
                                      "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/overview-group/name-text")
                                 .GetComponent<Text>();
                RightStarPowRatioText = GameObject
                                       .Find(
                                            "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/overview-group/star-label")
                                       .GetComponent<Text>();
                RightMaxPowGenText = GameObject
                                    .Find(
                                         "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/overview-group/gen-label")
                                    .GetComponent<Text>();
                RightMaxPowGenValueText = GameObject
                                         .Find(
                                              "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/overview-group/gen-value")
                                         .GetComponent<Text>();
                RightDysonBluePrintText = GameObject
                                         .Find(
                                              "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/overview-group/blueprint")
                                         .GetComponent<Text>();
                //戴森球+星系名称  恒星光度系数  最大发电性能 最大发电性能的值 戴森球蓝图  
            }
            catch (Exception)
            {
                //Console.WriteLine("GO Find Group 3 ERROR");
            }

            try
            {
                SpSailAmountText = GameObject
                                  .Find(
                                       "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/sphere-group/prop-label-0")
                                  .GetComponent<Text>();
                SpNodeAmountText = GameObject
                                  .Find(
                                       "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/sphere-group/prop-label-1")
                                  .GetComponent<Text>();
                SpSailLifeTimeText = GameObject
                                    .Find(
                                         "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/sphere-group/sail-stat/sail-histogram/title")
                                    .GetComponent<Text>();
                SpSailLifeTimeBarText = GameObject
                                       .Find(
                                            "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/sphere-group/sail-stat/bar-group/title")
                                       .GetComponent<Text>();
                //太阳帆总数 节点总数（已规划） 太阳帆寿命分布 太阳帆状态统计
                SpConsumePowText = GameObject
                                  .Find(
                                       "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/sphere-group/power-desc/label-2")
                                  .GetComponent<Text>();
                SpSwarmPowGenText = GameObject
                                   .Find(
                                        "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/sphere-group/power-desc/label-3")
                                   .GetComponent<Text>();
                SpShellPowGenText = GameObject
                                   .Find(
                                        "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/sphere-group/power-desc/label-4")
                                   .GetComponent<Text>();
                //请求功率、戴森云发电性能 戴森壳发电性能 
                SpEnergySatisfiedLabelText = GameObject
                                            .Find(
                                                 "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/sphere-group/power-desc/Graph/cons-circle/label-c")
                                            .GetComponent<Text>();
                //供电率 
            }
            catch (Exception)
            {
                //Console.WriteLine("GO Find Group 4 ERROR");
            }

            try
            {
                SoSailAmountText = GameObject
                                  .Find(
                                       "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/swarm-orbit-group/self-info/prop-label-0")
                                  .GetComponent<Text>();
                SoSailPowGenText = GameObject
                                  .Find(
                                       "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/swarm-orbit-group/self-info/prop-label-1")
                                  .GetComponent<Text>();
                SoSailLifeTimeText = GameObject
                                    .Find(
                                         "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/swarm-orbit-group/sail-stat/sail-histogram/title")
                                    .GetComponent<Text>();
                SoSailLifeTimeBarText = GameObject
                                       .Find(
                                            "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/swarm-orbit-group/sail-stat/bar-group/title")
                                       .GetComponent<Text>();
                //轨道的 太阳帆数量 发电性能 太阳帆寿命分布 太阳帆状态统计    
                LyPowGenText = GameObject
                              .Find(
                                   "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/layer-group/self-info/prop-label-4")
                              .GetComponent<Text>();
                //层的 发电性能
                NdPowGenText = GameObject
                              .Find(
                                   "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/node-group/self-info/prop-label-5")
                              .GetComponent<Text>();
                //节点的 发电性能
                DysonEditorPowerDescLabel4BarObj
                    = GameObject.Find(
                        "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/sphere-group/power-desc/label-4/legend-4");
                //右边圆圈，三个圆圈颜色示例的短横线，的第三个短横线
            }
            catch (Exception)
            {
                //Console.WriteLine("GO Find Group 5 ERROR");
            }

            try
            {
                FrPowGenText = GameObject
                              .Find(
                                   "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/frame-group/self-info/prop-label-0")
                              .GetComponent<Text>();
                //框架的 发电性能
                ShPowGenText = GameObject
                              .Find(
                                   "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/shell-group/self-info/prop-label-0")
                              .GetComponent<Text>();
                //壳的 发电性能
            }
            catch (Exception)
            {
                //Console.WriteLine("GO Find Group 6 ERROR");
            }

            try
            {
                ReceiverUIButton2Text = GameObject
                                       .Find("UI Root/Overlay Canvas/In Game/Windows/Power Generator Window/ray-receiver/switch-button-2/button-text")
                                       .GetComponent<Text>();
                ReceiverUIButton2Text.text = "物质合成".Translate();
            }
            catch (Exception)
            {
                //Console.WriteLine("GO Find Group 7 ERROR");
            }
        }

        public static void InitMegaSetUI()
        {
            try
            {
                //由于游戏改版，原有位置即使在高分辨率下也被占用了。所以更改位置
                //int biasX = 0;
                //int biasY = 0; // 原来是0, 0
                //resolutionLower1080 = DSPGame.globalOption.resolution.height < 1080 ? true : false;
                //if (LowResolutionMode.Value || resolutionLower1080)
                //{
                //    biasX = 300;
                //    biasY = 780; //原来是800
                //}
                //resolutionY = DSPGame.globalOption.resolution.height;
                //biasY = (int)(-800.0 * resolutionY / 1080) + 800;
                //if (resolutionY > 1090) biasY = -30;
                float ParentUIHeight = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel")
                                                 .GetComponent<RectTransform>().rect.height;
                int groupPosY = (int)(270 - ParentUIHeight);
                int warnTxtPosY = Math.Min((int)(-940 * ParentUIHeight / 1080), -835);

                //主要标签提示文字等
                GameObject DysonUILeft
                    = GameObject.Find(
                        "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy"); //戴森球编辑器UI的左边 作为Parent
                setMegaGroupObj = new GameObject("mega-buttons");
                setMegaGroupObj.transform.SetParent(DysonUILeft.transform);
                /*
                setMegaGroupObj = GameObject.Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/sphere-group/sail-stat/bar-group/bar-blue"), DysonUILeft.transform);
                setMegaGroupObj.GetComponent<Image>().color = new Color(0, 0, 0, 1);
                setMegaGroupObj.GetComponent<Image>().fillAmount = 1;
                setMegaGroupObj.AddComponent<Button>();
                */
                setMegaGroupObj.transform.localPosition = new Vector3(0, groupPosY, 0);
                setMegaGroupObj.transform.localScale = new Vector3(1, 1, 1);
                //setMegaGroupObj.AddComponent<RectTransform>();
                //setMegaGroupObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 500);
                //setMegaGroupObj.AddComponent<MegaButtonBehaviour>();


                GameObject LeftShellLabel2
                    = GameObject.Find(
                        "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy/layers/title-text");
                LeftMegaBuildWarning = Instantiate(LeftShellLabel2);
                LeftMegaBuildWarning.name = "settype-warning";
                LeftMegaBuildWarning.transform.SetParent(DysonUILeft.transform, false);
                LeftMegaBuildWarning.transform.localPosition = new Vector3(280, warnTxtPosY, 0);
                LeftMegaBuildWarning.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
                LeftMegaBuildWarning.GetComponent<RectTransform>().anchorMin = new Vector2(1, 1);
                LeftMegaBuildWarning.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
                LeftMegaBuildWarning.SetActive(true);
                SetMegaStructureWarningText = LeftMegaBuildWarning.GetComponent<Text>();
                SetMegaStructureWarningText.GetComponent<RectTransform>().sizeDelta = new Vector2(270, 100); //大小
                SetMegaStructureWarningText.text = "鼠标触碰左侧黄条以规划巨构".Translate();
                SetMegaStructureWarningText.alignment = TextAnchor.MiddleCenter;
                SetMegaStructureWarningText.fontSize = 16;
                SetMegaStructureWarningText.color = new Color(1f, 1f, 0.57f, 1f);

                rightBarObj
                    = Instantiate(
                        GameObject.Find(
                            "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/inspector/sphere-group/sail-stat/bar-group/bar-orange"),
                        setMegaGroupObj.transform);
                rightBarObj.name = "right-bar";
                rightBarObj.transform.localPosition = new Vector3(270, 0, 0);
                rightBarObj.transform.localScale = new Vector3(1, 1, 1);
                rightBarObj.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                rightBarObj.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
                rightBarObj.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0);
                rightBarObj.GetComponent<Image>().fillAmount = 1;
                rightBarObj.GetComponent<RectTransform>().sizeDelta = new Vector2(8, 243);

                GameObject LeftMegaStructrueTypeLabel = Instantiate(LeftShellLabel2);
                LeftMegaStructrueTypeLabel.name = "title3";
                LeftMegaStructrueTypeLabel.transform.SetParent(setMegaGroupObj.transform, false);
                LeftMegaStructrueTypeLabel.transform.localPosition = new Vector3(22, 0, 0);
                SetMegaStructureLabelText = LeftMegaStructrueTypeLabel.GetComponent<Text>();
                SetMegaStructureLabelText.text = "规划巨构建筑类型";


                //按钮
                GameObject addNewLayerButton
                    = GameObject.Find(
                        "UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel/hierarchy/layers/buttons-group/buttons/add-button");

                set2DysonButtonObj = Instantiate(addNewLayerButton);
                set2DysonButtonObj.SetActive(true);
                set2DysonButtonObj.name = "set-mega-0"; //名字
                set2DysonButtonObj.transform.SetParent(setMegaGroupObj.transform, false);
                set2DysonButtonObj.transform.localPosition = new Vector3(30, -35, 0);              //位置
                set2DysonButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 28); //按钮大小
                set2DysonButtonTextTrans = set2DysonButtonObj.transform.Find("Text");
                set2DysonButton = set2DysonButtonObj.GetComponent<Button>();
                set2DysonButton.interactable = true;

                set2MatDecomButtonObj = Instantiate(addNewLayerButton);
                set2MatDecomButtonObj.SetActive(true);
                set2MatDecomButtonObj.name = "set-mega-1"; //名字
                set2MatDecomButtonObj.transform.SetParent(setMegaGroupObj.transform, false);
                set2MatDecomButtonObj.transform.localPosition = new Vector3(30, -65, 0);              //位置
                set2MatDecomButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 28); //按钮大小
                set2MatDecomButtonTextTrans = set2MatDecomButtonObj.transform.Find("Text");
                set2MatDecomButton = set2MatDecomButtonObj.GetComponent<Button>();
                set2MatDecomButton.interactable = true;
                set2MatDecomButtonObj.GetComponent<UIButton>().tips.tipTitle = "功能说明题目".Translate();
                set2MatDecomButtonObj.GetComponent<UIButton>().tips.tipText = "物质解压器功能文本".Translate();
                set2MatDecomButtonObj.GetComponent<UIButton>().tips.delay = 0.3f;
                set2MatDecomButtonObj.GetComponent<UIButton>().tips.width = 280;
                set2MatDecomButtonObj.GetComponent<UIButton>().tips.offset = new Vector2(140, 50);

                set2SciNexusButtonObj = Instantiate(addNewLayerButton);
                set2SciNexusButtonObj.SetActive(true);
                set2SciNexusButtonObj.name = "set-mega-2"; //名字
                set2SciNexusButtonObj.transform.SetParent(setMegaGroupObj.transform, false);
                set2SciNexusButtonObj.transform.localPosition = new Vector3(30, -95, 0);              //位置
                set2SciNexusButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 28); //按钮大小
                set2SciNexusButtonTextTrans = set2SciNexusButtonObj.transform.Find("Text");
                set2SciNexusButton = set2SciNexusButtonObj.GetComponent<Button>();
                set2SciNexusButton.interactable = true;
                set2SciNexusButtonObj.GetComponent<UIButton>().tips.tipTitle = "功能说明题目".Translate();
                set2SciNexusButtonObj.GetComponent<UIButton>().tips.tipText = "科学枢纽功能文本".Translate();
                set2SciNexusButtonObj.GetComponent<UIButton>().tips.delay = 0.3f;
                set2SciNexusButtonObj.GetComponent<UIButton>().tips.width = 260;
                set2SciNexusButtonObj.GetComponent<UIButton>().tips.offset = new Vector2(130, 50);

                set2WarpFieldGenButtonObj = Instantiate(addNewLayerButton);
                set2WarpFieldGenButtonObj.SetActive(true);
                set2WarpFieldGenButtonObj.name = "set-mega-3"; //名字
                set2WarpFieldGenButtonObj.transform.SetParent(setMegaGroupObj.transform, false);
                set2WarpFieldGenButtonObj.transform.localPosition = new Vector3(30, -125, 0);             //位置
                set2WarpFieldGenButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 28); //按钮大小
                set2WarpFieldGenButtonTextTrans = set2WarpFieldGenButtonObj.transform.Find("Text");
                set2WarpFieldGenButton = set2WarpFieldGenButtonObj.GetComponent<Button>();
                set2WarpFieldGenButton.interactable = true;
                set2WarpFieldGenButtonObj.GetComponent<UIButton>().tips.tipTitle = "功能说明题目".Translate();
                set2WarpFieldGenButtonObj.GetComponent<UIButton>().tips.tipText = "折跃场广播阵列功能文本".Translate();
                set2WarpFieldGenButtonObj.GetComponent<UIButton>().tips.delay = 0.3f;
                set2WarpFieldGenButtonObj.GetComponent<UIButton>().tips.width = 260;
                set2WarpFieldGenButtonObj.GetComponent<UIButton>().tips.offset = new Vector2(130, 50);

                set2MegaAssemButtonObj = Instantiate(addNewLayerButton);
                set2MegaAssemButtonObj.SetActive(true);
                set2MegaAssemButtonObj.name = "set-mega-4"; //名字
                set2MegaAssemButtonObj.transform.SetParent(setMegaGroupObj.transform, false);
                set2MegaAssemButtonObj.transform.localPosition = new Vector3(30, -155, 0);             //位置
                set2MegaAssemButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 28); //按钮大小
                set2MegaAssemButtonTextTrans = set2MegaAssemButtonObj.transform.Find("Text");
                set2MegaAssemButton = set2MegaAssemButtonObj.GetComponent<Button>();
                set2MegaAssemButton.interactable = true;
                set2MegaAssemButtonObj.GetComponent<UIButton>().tips.tipTitle = "功能说明题目".Translate();
                set2MegaAssemButtonObj.GetComponent<UIButton>().tips.tipText = "星际组装厂功能文本".Translate();
                set2MegaAssemButtonObj.GetComponent<UIButton>().tips.delay = 0.3f;
                set2MegaAssemButtonObj.GetComponent<UIButton>().tips.width = 260;
                set2MegaAssemButtonObj.GetComponent<UIButton>().tips.offset = new Vector2(130, 50);

                set2CrystalMinerButtonObj = Instantiate(addNewLayerButton);
                set2CrystalMinerButtonObj.SetActive(true);
                set2CrystalMinerButtonObj.name = "set-mega-5"; //名字
                set2CrystalMinerButtonObj.transform.SetParent(setMegaGroupObj.transform, false);
                set2CrystalMinerButtonObj.transform.localPosition = new Vector3(30, -185, 0);             //位置
                set2CrystalMinerButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 28); //按钮大小
                set2CrystalMinerButtonTextTrans = set2CrystalMinerButtonObj.transform.Find("Text");
                set2CrystalMinerButton = set2CrystalMinerButtonObj.GetComponent<Button>();
                set2CrystalMinerButton.interactable = true;
                set2CrystalMinerButtonObj.GetComponent<UIButton>().tips.tipTitle = "功能说明题目".Translate();
                set2CrystalMinerButtonObj.GetComponent<UIButton>().tips.tipText = "晶体重构器功能文本".Translate();
                set2CrystalMinerButtonObj.GetComponent<UIButton>().tips.delay = 0.3f;
                set2CrystalMinerButtonObj.GetComponent<UIButton>().tips.width = 260;
                set2CrystalMinerButtonObj.GetComponent<UIButton>().tips.offset = new Vector2(130, 50);

                set2StarCannonButtonObj = Instantiate(addNewLayerButton);
                set2StarCannonButtonObj.SetActive(true);
                set2StarCannonButtonObj.name = "set-mega-6"; //名字
                set2StarCannonButtonObj.transform.SetParent(setMegaGroupObj.transform, false);
                set2StarCannonButtonObj.transform.localPosition = new Vector3(30, -215, 0);             //位置
                set2StarCannonButtonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 28); //按钮大小
                set2StarCannonButtonTextTrans = set2StarCannonButtonObj.transform.Find("Text");
                set2StarCannonButton = set2StarCannonButtonObj.GetComponent<Button>();
                set2StarCannonButton.interactable = true;
                set2StarCannonButtonObj.GetComponent<UIButton>().tips.tipTitle = "恒星炮设计说明题目".Translate();
                set2StarCannonButtonObj.GetComponent<UIButton>().tips.tipText = "恒星炮设计说明文本".Translate();
                set2StarCannonButtonObj.GetComponent<UIButton>().tips.delay = 0.3f;
                set2StarCannonButtonObj.GetComponent<UIButton>().tips.width = 520;
                set2StarCannonButtonObj.GetComponent<UIButton>().tips.offset = new Vector2(260, 100);

                set2DysonButton.onClick.RemoveAllListeners();
                set2DysonButton.onClick.AddListener(() => { SetMegaStructure(0); }); //按下按钮，设置巨构类型
                set2MatDecomButton.onClick.RemoveAllListeners();
                set2MatDecomButton.onClick.AddListener(() => { SetMegaStructure(1); });
                set2SciNexusButton.onClick.RemoveAllListeners();
                set2SciNexusButton.onClick.AddListener(() => { SetMegaStructure(2); });
                set2WarpFieldGenButton.onClick.RemoveAllListeners();
                set2WarpFieldGenButton.onClick.AddListener(() => { SetMegaStructure(3); });
                set2MegaAssemButton.onClick.RemoveAllListeners();
                set2MegaAssemButton.onClick.AddListener(() => { SetMegaStructure(4); });
                set2CrystalMinerButton.onClick.RemoveAllListeners();
                set2CrystalMinerButton.onClick.AddListener(() => { SetMegaStructure(5); });
                set2StarCannonButton.onClick.RemoveAllListeners();
                set2StarCannonButton.onClick.AddListener(() => { SetMegaStructure(6); });
            }
            catch (Exception)
            {
                //Debug.LogWarning("ButtonInit ERROR");
            }
        }

        public static void LateInitOtherUI()
        {
            if (selectAutoReceiveGearLimitObj != null) return;
            selectAutoReceiveGearLimitObj 
                = Instantiate(
                    GameObject.Find(
                        "UI Root/Overlay Canvas/In Game/Windows/Mecha Window/information/movement-panel"));
            selectAutoReceiveGearLimitObj.name = "gear-max-num";
            selectAutoReceiveGearLimitObj.transform.SetParent(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Mecha Window/information").transform);
            selectAutoReceiveGearLimitObj.transform.localScale = new Vector3(1, 1, 1);
            selectAutoReceiveGearLimitObj.transform.localPosition = new Vector3(-240, -390, 0);
            selectAutoReceiveGearLimitObj.SetActive(true);
            GameObject.Destroy(selectAutoReceiveGearLimitObj.transform.Find("title").gameObject);
            GameObject.Destroy(selectAutoReceiveGearLimitObj.transform.Find("value-1").gameObject);
            GameObject.Destroy(selectAutoReceiveGearLimitObj.transform.Find("label-2").gameObject);

            selectAutoReceiveGearLimitLabelObj
                = Instantiate(
                    GameObject.Find(
                        "UI Root/Overlay Canvas/In Game/Windows/Statistics Window/performance-bg/cpu-panel/Scroll View/Viewport/Content/label"),
                    selectAutoReceiveGearLimitObj.transform);
            Text oriNarrowText = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Mecha Window/information/construction-panel/label-1")
                                           .GetComponent<Text>();
            Text oriAlphaText = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Mecha Window/information/construction-panel/title")
                                           .GetComponent<Text>();

            selectAutoReceiveGearLimitObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 63);
            DestroyImmediate(selectAutoReceiveGearLimitObj.GetComponent<UnityEngine.EventSystems.EventTrigger>()); // 这个是为了阻止：因为鼠标悬停此Object，导致原本的（作为instantiate的源）Object里面的问号标志被触发显示。

            selectAutoReceiveGearLimitLabelObj.name = "title";
            selectAutoReceiveGearLimitLabelObj.SetActive(true);
            selectAutoReceiveGearLimitLabelObj.transform.localPosition = new Vector3(10, -6, 0);
            selectAutoReceiveGearLimitLabelObj.GetComponent<Text>().font = oriNarrowText.font;
            selectAutoReceiveGearLimitLabelObj.GetComponent<Text>().fontSize = 13;
            selectAutoReceiveGearLimitLabelObj.GetComponent<Text>().text = "远程折跃多功能组件限制".Translate();
            selectAutoReceiveGearLimitLabelObj.GetComponent<Text>().material = oriAlphaText.material;
            selectAutoReceiveGearLimitLabelObj.GetComponent<Text>().color = oriAlphaText.color;

            selectAutoReceiveGearLimitComboBoxObj
                = Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Statistics Window/dyson-bg/top/TimeComboBox"),
                                         selectAutoReceiveGearLimitObj.transform);
            selectAutoReceiveGearLimitComboBoxObj.name = "combo-box";
            selectAutoReceiveGearLimitComboBoxObj.SetActive(true);
            selectAutoReceiveGearLimitComboBoxObj.transform.localPosition = new Vector3(130, -30, 0);
            selectAutoReceiveGearLimitComboBoxObj.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            selectAutoReceiveGearLimitComboBox = selectAutoReceiveGearLimitComboBoxObj.GetComponent<UIComboBox>();
            selectAutoReceiveGearLimitComboBox.onItemIndexChange.RemoveAllListeners();
            selectAutoReceiveGearLimitComboBox.Items = new List<string> { "远程接收关闭gm".Translate(), "1000", "2000", "组件无限制".Translate() };
            selectAutoReceiveGearLimitComboBox.itemIndex = 1;
            selectAutoReceiveGearLimitComboBox.text = selectAutoReceiveGearLimitComboBox.Items[selectAutoReceiveGearLimitComboBox.itemIndex];
            selectAutoReceiveGearLimitComboBox.onItemIndexChange.AddListener(OnGearLimitChange);
            selectAutoReceiveGearLimitComboBox.enabled = true;
        }

        public static void RefreshUIWhenLoad()
        {
            if (selectAutoReceiveGearLimitComboBox != null && selectAutoReceiveGearLimitLabelObj != null)
            {
                selectAutoReceiveGearLimitLabelObj.GetComponent<Text>().text = "远程折跃多功能组件限制".Translate();
                selectAutoReceiveGearLimitComboBox.Items = new List<string> { "远程接收关闭gm".Translate(), "1000", "2000", "组件无限制".Translate() };
                selectAutoReceiveGearLimitComboBox.text = selectAutoReceiveGearLimitComboBox.Items[selectAutoReceiveGearLimitComboBox.itemIndex];
            }
        }

        public static void InitResolutionWhenLoad()
        {
            resolutionY = DSPGame.globalOption.resolution.height;
        }

        public static void OnGearLimitChange()
        {
            int index = selectAutoReceiveGearLimitComboBox.itemIndex;
            maxAutoReceiveGear = index * 1000;
        }

        /// <summary>
        /// 下面的patch用于：让其他巨构建筑只计算壳面的“发电量”，不计算漂浮的“戴森云/太阳帆”的“发电量”
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DysonSphere), "BeforeGameTick")]
        public static void BeforeGameTickPostPatch(ref DysonSphere __instance)
        {
            if (StarMegaStructureType[__instance.starData.id - 1] == 0) //如果是戴森球，则不进行修改
            {
                return;
            }

            //否则，只计算壳面的效果，忽略游戏本体所谓戴森云的效果（也就是发电量）
            // __instance.energyGenOriginalCurrentTick -= __instance.swarm.energyGenCurrentTick; // 防止显示的黑雾截取能量不正常（指算上截取了全部的戴森云能量），但是这样更改会影响EnemyDFHiveSystem.DecisionAI，所以转而PostPatch UIDEPowerDesc的Update，见下
            //__instance.energyGenCurrentTick -= (long)((double)__instance.swarm.energyGenCurrentTick * __instance.energyDFHivesDebuffCoef);
            __instance.energyGenCurrentTick = (long)((double)(__instance.energyGenOriginalCurrentTick - __instance.energyGenCurrentTick_Swarm) * __instance.energyDFHivesDebuffCoef);
        }

        /// <summary>
        /// 用于修复非戴森球 黑屋窃取能量显示不正常的bug
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDEPowerDesc), "UpdateUI")]
        public static void UIDEPowerDescUpdateUIPostPatch(ref UIDEPowerDesc __instance)
        {
            int StarIndex = __instance.dysonSphere.starData.index;
            if (StarIndex < 0 || StarIndex >= 1000)
                return;
            if (StarMegaStructureType[StarIndex] == 0)
                return;

            var _this = __instance;
            long num5 = (_this.dysonSphere.energyGenOriginalCurrentTick - _this.dysonSphere.energyGenCurrentTick_Swarm - _this.dysonSphere.energyGenCurrentTick) * 60L;
            StringBuilderUtility.WriteKMG(_this.sb, 8, -num5, true);
            _this.valueText4.text = _this.sb.ToString();
        }


        /// <summary>
        /// 接收器每帧函数的patch，用于设定各种新接收器的行为。同时，让物品输出自动叠加（针对物质解压器接收速度过快），叠加数量取决于当前物流站自动叠加的科技等级。
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="useIon"></param>
        /// <param name="useCata"></param>
        /// <param name="factory"></param>
        /// <param name="productRegister"></param>
        /// <param name="consumeRegister"></param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerGeneratorComponent), "GameTick_Gamma")]
        public static bool GameTick_GammaPatch(
            ref PowerGeneratorComponent __instance,
            bool useIon,
            bool useCata,
            PlanetFactory factory,
            int[] productRegister,
            int[] consumeRegister)
        {
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MainLogic);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.Receiver);
            int idx = factory.planet.star.id - 1;
            bool postWork = false;
            if (idx < 0 || idx > 999)
            {
                //Debug.LogWarning("GameTick_GammaPatch index out of range. Now return true.");
                postWork = true;
            }

            int megaType = StarMegaStructureType[idx];
            int protoId = factory.entityPool[__instance.entityId].protoId; //接收器的建筑的原型ID
            if (megaType == 0 && protoId == 2208)
            {
                postWork = true;
            }
            else if (megaType == 1) //物质解压器
            {
                postWork = true;
            }
            else if (megaType == 4 && protoId == 9499) //星际组装厂
            {
                postWork = false; // 不再允许用射线接受器接收组件
            }
            else if (megaType == 5) //晶体重构器
            {
                postWork = true;
            }

            int corresMegaType = -1;
            if (ReceiverPatchers.ProductId2MegaType.TryGetValue(__instance.productId, out corresMegaType))
            {
                if (corresMegaType != megaType)
                    postWork = false;
            }
            else
            {
                postWork = false;
            }
            

            //其他情况不允许接收器生成或输出物质
            if (postWork)
            {
                if (__instance.catalystPoint > 0)
                {
                    int num = __instance.catalystPoint / 3600;
                    if (useCata)
                    {
                        int num2 = __instance.catalystIncPoint / __instance.catalystPoint;
                        __instance.catalystPoint--;
                        __instance.catalystIncPoint -= num2;
                        if (__instance.catalystIncPoint < 0 || __instance.catalystPoint <= 0)
                        {
                            __instance.catalystIncPoint = 0;
                        }
                    }

                    int num3 = __instance.catalystPoint / 3600;
                    int[] obj = consumeRegister;
                    lock (obj)
                    {
                        consumeRegister[__instance.catalystId] += num - num3;
                    }
                }

                if (__instance.productId > 0 && __instance.productCount < 20f)
                {
                    int num4 = (int)__instance.productCount;
                    __instance.productCount += (float)(__instance.capacityCurrentTick / (double)__instance.productHeat);
                    int num5 = (int)__instance.productCount;
                    int[] obj = productRegister;
                    lock (obj)
                    {
                        productRegister[__instance.productId] += num5 - num4;
                    }

                    if (__instance.productCount > 20f)
                    {
                        __instance.productCount = 20f;
                    }
                }

                __instance.warmup += __instance.warmupSpeed;
                __instance.warmup = ((__instance.warmup > 1f) ? 1f : ((__instance.warmup < 0f) ? 0f : __instance.warmup));
                bool flag2 = __instance.productId > 0 && __instance.productCount >= pilerLvl;
                bool flag3 = useIon && __instance.catalystPoint < 72000f;
                if (flag2 || flag3)
                {
                    bool flag4;
                    int num6;
                    int num7;
                    factory.ReadObjectConn(__instance.entityId, 0, out flag4, out num6, out num7);
                    bool flag5;
                    int num8;
                    int num9;
                    factory.ReadObjectConn(__instance.entityId, 1, out flag5, out num8, out num9);
                    bool flag6;
                    bool flag7;
                    if (num6 <= 0)
                    {
                        flag6 = false;
                        flag7 = false;
                        num6 = 0;
                    }
                    else
                    {
                        flag6 = flag4;
                        flag7 = !flag4;
                    }

                    bool flag8;
                    bool flag9;
                    if (num8 <= 0)
                    {
                        flag8 = false;
                        flag9 = false;
                        num8 = 0;
                    }
                    else
                    {
                        flag8 = flag5;
                        flag9 = !flag5;
                    }

                    byte b = 0;
                    if (flag2)
                    {
                        if (flag6 && flag8)
                        {
                            if (__instance.fuelHeat == 0L)
                            {
                                if (factory.InsertInto(num6, 0, __instance.productId, (byte)pilerLvl, 0, out b) == pilerLvl)
                                {
                                    __instance.productCount -= pilerLvl;
                                    __instance.fuelHeat = 1L;
                                }
                                else if (factory.InsertInto(num8, 0, __instance.productId, (byte)pilerLvl, 0, out b) == pilerLvl)
                                {
                                    __instance.productCount -= pilerLvl;
                                    __instance.fuelHeat = 0L;
                                }
                            }
                            else if (factory.InsertInto(num8, 0, __instance.productId, (byte)pilerLvl, 0, out b) == pilerLvl)
                            {
                                __instance.productCount -= pilerLvl;
                                __instance.fuelHeat = 0L;
                            }
                            else if (factory.InsertInto(num6, 0, __instance.productId, (byte)pilerLvl, 0, out b) == pilerLvl)
                            {
                                __instance.productCount -= pilerLvl;
                                __instance.fuelHeat = 1L;
                            }
                        }
                        else if (flag6)
                        {
                            if (factory.InsertInto(num6, 0, __instance.productId, (byte)pilerLvl, 0, out b) == pilerLvl)
                            {
                                __instance.productCount -= pilerLvl;
                                __instance.fuelHeat = 1L;
                            }
                        }
                        else if (flag8 && factory.InsertInto(num8, 0, __instance.productId, (byte)pilerLvl, 0, out b) == pilerLvl)
                        {
                            __instance.productCount -= pilerLvl;
                            __instance.fuelHeat = 0L;
                        }
                    }

                    if (flag3)
                    {
                        byte b2;
                        byte b3;
                        if (flag7 && factory.PickFrom(num6, 0, __instance.catalystId, null, out b2, out b3) == __instance.catalystId)
                        {
                            __instance.catalystPoint += 3600 * b2;
                            __instance.catalystIncPoint += 3600 * b3;
                        }

                        if (flag9 && factory.PickFrom(num8, 0, __instance.catalystId, null, out b2, out b3) == __instance.catalystId)
                        {
                            __instance.catalystPoint += 3600 * b2;
                            __instance.catalystIncPoint += 3600 * b3;
                        }
                    }
                }
            }

            MMSCPU.EndSample(ECpuWorkEntryExtended.Receiver);
            MMSCPU.EndSample(ECpuWorkEntryExtended.MainLogic);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            return false;
        }

        /// <summary>
        /// 游戏每帧判断一下玩家背包里的多功能组件是否低于目标，如果低于则开启自动接收
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), "GameTick")]
        public static void GameTickPostPatch(long time)
        {
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MainLogic);
            isRemoteReceiveingGear = false;
            if ((GameMain.mainPlayer.package.GetItemCount(9500) < maxAutoReceiveGear || maxAutoReceiveGear >= 3000)) isRemoteReceiveingGear = true;

            if (true)
            {
                hashGenByAllSN *= 60;
                int propertyGen = (int)(Math.Pow(hashGenByAllSN, 0.65) + 0.001 * hashGenByAllSN * GameMain.data.gameDesc.propertyMultiplier);
                

                PropertyLogic p = GameMain.gameScenario?.propertyLogic;
                if (p != null)
                {
                    long clusterSeedKey = p.gameData.GetClusterSeedKey();
                    ClusterPropertyData clusterData = p.propertySystem.GetClusterData(clusterSeedKey);
                    ClusterPropertyData propertyData = p.gameData.history.propertyData;
                    foreach (int num in PropertySystem.productIds)
                    {
                        int itemProduction = propertyData.GetItemProduction(num);
                        int itemProduction2 = clusterData.GetItemProduction(num);

                        if (propertyGen > itemProduction)
                        {
                            propertyData.SetItemProduction(num, propertyGen);
                        }

                        if (propertyGen > itemProduction2)
                        {
                            clusterData.SetItemProduction(num, propertyGen);
                        }
                    }
                }
            }
            hashGenByAllSN = 0;
            MMSCPU.BeginSample(ECpuWorkEntryExtended.StarAssembly);
            StarAssembly.UIFrameUpdate(time);
            MMSCPU.EndSample(ECpuWorkEntryExtended.StarAssembly);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.StarCannon);
            StarCannon.RefreshStarCannonProperties();
            MMSCPU.EndSample(ECpuWorkEntryExtended.StarCannon);
            MMSCPU.EndSample(ECpuWorkEntryExtended.MainLogic);
            MMSCPU.EndSample(ECpuWorkEntryExtended.MoreMegaStructure);
            //UIStatisticsPatcher.UpdateLag();
        }

        /// <summary>
        /// 折跃场广播阵列巨构 以及 科学枢纽 的效果，将在戴森球本身的gametick里完成，而不需要接收器。新增的多功能组件的远程折跃到背包功能也在此进行。
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DysonSphere), "GameTick")]
        public static void DysonSphereGameTickPostPatch(ref DysonSphere __instance)
        {
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MainLogic);
            int idx = __instance.starData.id - 1;
            if (idx < 0 || idx > 999)
            {
                return;
            }

            //Utils.Log("postPatching dysonSphere GameTick");
            if (StarMegaStructureType[idx] == 2) //如果是科学枢纽
            {
                //HighStopwatch timetest = new HighStopwatch(); //
                //timetest.Begin(); //
                //double last = 0; //

                GameHistoryData history = GameMain.history;
                GameStatData statistics = GameMain.statistics;

                int num = history.currentTech;
                if (num <= 0)
                {
                    return;
                }

                TechProto techProto = LDB.techs.Select(num);
                TechState ts = default(TechState);
                for (int i = 0; i < techProto.Items.Length; i++) // 科学枢纽不支持研究黑雾矩阵相关科技
                {
                    if (techProto.Items[i] == 5201)
                        return;
                }

                FactoryProductionStat factoryProductionStat = null;
                if (__instance.starData.planets.Length > 0)
                {
                    try
                    {
                        //PlanetFactory factory = __instance.starData.planets[__instance.starData.planets.Length - 1].factory;//科学枢纽的科研点产出算在0号星球上，目前测试0号是最内轨行星，如果是气态怎么办呢？
                        //factoryProductionStat = statistics.production.factoryStatPool[factory.index];
                        factoryProductionStat = statistics.production.factoryStatPool[0]; //这是母星的统计面板
                    }
                    catch (Exception)
                    {
                        factoryProductionStat = null;
                        //Debug.LogWarning("factoryProductionStat didn't find.");
                    }
                }

                int techHashedThisFrame = statistics.techHashedThisFrame;
                long universeMatrixPointUploaded = history.universeMatrixPointUploaded;

                if (num > 0 && techProto != null && techProto.IsLabTech && GameMain.history.techStates.ContainsKey(num))
                {
                    ts = history.techStates[num];
                }

                int researchTechId = num;
                long HashP = (__instance.energyGenCurrentTick - __instance.energyReqCurrentTick) *
                             (HashBasicSpeedScale + HashBonusPerLevel * history.techSpeed) /
                             HashGenDivisor;
                history.AddTechHash(HashP);
                hashGenByAllSN += HashP;

                //如果找到了工厂，就记录数据面板研究点数
                if (factoryProductionStat != null)
                {
                    long hashRegister = factoryProductionStat.hashRegister;
                    hashRegister += HashP;
                    factoryProductionStat.hashRegister = hashRegister;
                }
            }
            else if (StarMegaStructureType[idx] == 3) //如果是折跃场广播阵列
            {
                //GameHistoryData history = GameMain.history;

                //int curTechLevel = 2;

                ////TechProto techProto = LDB.techs.Select(3407);
                //try
                //{
                //    TechState ts = history.techStates[3407];
                //    curTechLevel = ts.curLevel > 2 ? ts.curLevel : 2;
                //}
                //catch (Exception)
                //{
                //    //Debug.LogWarning("No history techStates of 3407.");
                //}

                //try
                //{
                //    if (__instance != null)
                //    {
                //        long DysonEnergy
                //            = (__instance.energyGenCurrentTick - __instance.energyReqCurrentTick) /
                //              WarpAccDivisor; //根据巨构的能量减去需求量，除以1000000后，如果再乘60，单位就是MW。现在除10^7也就是每60MW提供10%的额外曲速速度
                //        DysonEnergy = DysonEnergy > WarpAccMax ? WarpAccMax : DysonEnergy; //3TW为上限加成，即+250ly/s
                //        if (DysonEnergy <= 0) // 原来有|| __instance.energyGenCurrentTick_Layers <= 0，但是不需要了因为energyGenCurrentTick的计算方式已被我改了
                //        {
                //            history.logisticShipSpeedScale = 1f + (curTechLevel - 2) * 0.5f;
                //        }
                //        else
                //        {
                //            history.logisticShipSpeedScale = 1f + (curTechLevel - 2) * 0.5f + DysonEnergy;
                //        }
                //    }
                //    else
                //    {
                //        history.logisticShipSpeedScale = 1f + (curTechLevel - 2) * 0.5f;
                //    }
                //}
                //catch (Exception)
                //{
                //    //Debug.LogWarning("Error on RefreshShipSpeedScale");
                //}
            }
            else if (StarMegaStructureType[idx] == 4) //如果是星际组装厂
            {
                MMSCPU.BeginSample(ECpuWorkEntryExtended.StarAssembly);
                StarAssembly.InternalUpdate(__instance);
                StarAssembly.TryUseRocketInStorageToBuildIA(__instance);
                /*
                autoReceiveGearProgress += __instance.energyGenCurrentTick;
                int productCnt = (int)(autoReceiveGearProgress / (multifunctionComponentHeat * 10));
                autoReceiveGearProgress = autoReceiveGearProgress % (multifunctionComponentHeat * 10);
                if(productCnt>0)
                {
                    try
                    {
                        GameMain.mainPlayer.TryAddItemToPackage(9500, productCnt, 0, true);
                        if(!VFInput.inFullscreenGUI)
                            Utils.UIItemUp(9500, productCnt, 240);
                    }
                    catch (Exception)
                    {
                    }
                }
                */
                MMSCPU.EndSample(ECpuWorkEntryExtended.StarAssembly);
            }
            MMSCPU.EndSample(ECpuWorkEntryExtended.MainLogic);
            MMSCPU.EndSample(ECpuWorkEntryExtended.MoreMegaStructure);
        }

        /// <summary>
        /// 火箭发射器所需火箭修正，注意如果更改了巨构类型，而发射器内还存有不相符的火箭，该火箭将直接消失（为了防止用廉价火箭白嫖高价火箭）
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SiloComponent), "InternalUpdate")]
        public static void SiloUpdatePatch(ref SiloComponent __instance)
        {
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.Patch);
            int planetId = __instance.planetId;
            int starIndex = planetId / 100 - 1;
            PlanetFactory factory = GameMain.galaxy.stars[starIndex].planets[planetId % 100 - 1].factory;
            int gmProtoId = factory.entityPool[__instance.entityId].protoId;
            if (gmProtoId != 2312) return; //只修改原始火箭发射器

            if (starIndex < 0 || starIndex > 999)
            {
                //Debug.LogWarning("SiloInternalUpdate Patch Error because starIndex out of range.");
                return;
            }

            int bulletIdExpected = 1503;
            int megaType = StarMegaStructureType[starIndex];
            switch (megaType)
            {
                case 0:
                    bulletIdExpected = 1503;
                    break;

                case 1:
                    bulletIdExpected = 9488;
                    break;

                case 2:
                    bulletIdExpected = 9489;
                    break;

                case 3:
                    bulletIdExpected = 9490;
                    break;

                case 4:
                    bulletIdExpected = 9491;
                    break;

                case 5:
                    bulletIdExpected = 9492;
                    break;

                case 6:
                    bulletIdExpected = 9510;
                    break;
            }

            bool knownId = false; // 此处是为了适配深空来敌mod，有其他的火箭需要借用游戏本体的silo发射，因此只有已知的id会进行转化，位置的id交由深空来敌mod进行处理
            switch (__instance.bulletId)
            {
                case 1503:
                case 9488:
                case 9489:
                case 9490:
                case 9491:
                case 9492:
                case 9510:
                    knownId = true;
                    break;
            }

            if (__instance.bulletId != bulletIdExpected && knownId)
            {
                __instance.bulletCount = 0;
                __instance.bulletInc = 0;
                __instance.bulletId = bulletIdExpected;
            }
            MMSCPU.EndSample(ECpuWorkEntryExtended.Patch);
            MMSCPU.EndSample(ECpuWorkEntryExtended.MoreMegaStructure);
        }

        /// <summary>
        /// 弹射器所需发射物修正，类似上面的发射井
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EjectorComponent), "InternalUpdate")]
        public static void EjectorUpdatePatch(ref EjectorComponent __instance)
        {
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.Patch);
            if (!GenesisCompatibility) return; // 目前只对创世之书生效
            int planetId = __instance.planetId;
            int starIndex = planetId / 100 - 1;
            PlanetFactory factory = GameMain.galaxy.stars[starIndex].planets[planetId % 100 - 1].factory;
            int gmProtoId = factory.entityPool[__instance.entityId].protoId;
            if (gmProtoId != 2311) return; //只修改原始弹射器

            if (starIndex < 0 || starIndex > 999)
            {
                return;
            }

            int megaType = StarMegaStructureType[starIndex];
            if (megaType == 2)
            {
                if (__instance.bulletId == 1501)
                {
                    __instance.bulletCount = 0;
                    __instance.bulletInc = 0;
                    __instance.bulletId = 6006;
                }
            }
            else
            {
                if (__instance.bulletId == 6006)
                {
                    __instance.bulletCount = 0;
                    __instance.bulletInc = 0;
                    __instance.bulletId = 1501;
                }
            }
            MMSCPU.EndSample(ECpuWorkEntryExtended.Patch);
            MMSCPU.EndSample(ECpuWorkEntryExtended.MoreMegaStructure);
        }

        /// <summary>
        /// 下面三个是在戴森球界面进行操作时需要重置UI文本、按钮等操作，貌似改游戏本身的stringproto也可以，但是没改
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDysonEditor), "_OnOpen")]
        public static void SetTextOnOpen(UIDysonEditor __instance)
        {
            StarAssembly.ForceResetIncDataCache(); // 为了打开/切换星际组装厂页面时不错误地显示增产效果
            if (__instance.selection.viewDysonSphere != null)
            {
                curDysonSphere = __instance.selection.viewDysonSphere;
            }

            if (__instance.selection.viewStar != null)
            {
                RefreshUILabels(__instance.selection.viewStar, ShowIAUIWhenOpenDE.Value);
            }
            else
            {
                RefreshUILabels(__instance.gameData.localStar, ShowIAUIWhenOpenDE.Value);
            }

            try
            {
                SetMegaStructureWarningText.text = "鼠标触碰左侧黄条以规划巨构".Translate();
            }
            catch (Exception) { }

            RefreshButtonPos();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDysonEditor), "OnViewStarChange")]
        public static void SetTextOnViewStarChange(UIDysonEditor __instance)
        {
            StarAssembly.ForceResetIncDataCache(); // 为了打开/切换星际组装厂页面时不错误地显示增产效果
            if (__instance.selection.viewDysonSphere != null)
            {
                curDysonSphere = __instance.selection.viewDysonSphere;
            }

            if (__instance.selection.viewStar != null)
            {
                RefreshUILabels(__instance.selection.viewStar, ShowIAUIWhenOpenDE.Value);
            }
            else
            {
                RefreshUILabels(__instance.gameData.localStar, ShowIAUIWhenOpenDE.Value);
            }

            try
            {
                SetMegaStructureWarningText.text = "鼠标触碰左侧黄条以规划巨构".Translate();
            }
            catch (Exception) { }

            RefreshButtonPos();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDysonEditor), "OnSelectionChange")]
        public static void SetTextOnSelectionChange(UIDysonEditor __instance)
        {
            try
            {
                SetMegaStructureWarningText.text = "鼠标触碰左侧黄条以规划巨构".Translate();
            }
            catch (Exception) { }

            if (__instance.selection.viewDysonSphere != null)
            {
                curDysonSphere = __instance.selection.viewDysonSphere;
            }

            if (__instance.selection.viewStar != null)
            {
                RefreshUILabels(__instance.selection.viewStar);
            }
            else
            {
                RefreshUILabels(__instance.gameData.localStar);
            }
        }

        public static void RefreshButtonPos()
        {
            float ParentUIHeight = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Dyson Sphere Editor/Dyson Editor Control Panel")
                                             .GetComponent<RectTransform>().rect.height;
            int groupPosY = (int)(270 - ParentUIHeight);
            int warnTxtPosY = Math.Min((int)(-940 * ParentUIHeight / 1080), -835);
            if (setMegaGroupObj != null) setMegaGroupObj.transform.localPosition = new Vector3(MegaButtonGroupBehaviour.currentX, groupPosY, 0);
            if (LeftMegaBuildWarning != null) LeftMegaBuildWarning.transform.localPosition = new Vector3(280, warnTxtPosY, 0);
        }

        public static void RefreshUILabels(StarData star)
        {
            RefreshUILabels(star, false);
        }

        public static void RefreshUILabels(StarData star, bool forceShowUI) //改变UI中显示的文本，不能再叫戴森球了。另外改变新增的设置巨构建筑类型的按钮的状态
        {
            try
            {
                if (star == null) return;
                curStar = star;
                int idx = star.id - 1;
                idx = idx < 0 ? 0 : (idx > 999 ? 999 : idx);

                StarAssembly.RefreshUI(forceShowUI);

                //Console.WriteLine($"Refreshing phase. ori_idx={star.id -1} and finding idx the name is {GameMain.galaxy.stars[idx].displayName}, while cur name is {star.displayName}");

                SetMegaStructureLabelText.text = "规划巨构建筑类型".Translate();

                LeftDysonCloudTitle.text = "自由组件云".Translate();
                LeftDysonCloudBluePrintText.text = "组件云蓝图".Translate();
                LeftDysonShellTitle.text = "锚定结构".Translate();
                LeftDysonShellOrbitTitleText.text = "结构层级".Translate();
                LeftDysonShellBluePrintText.text = "锚定结构蓝图".Translate();
                RightStarPowRatioText.text = "恒星功效系数".Translate();
                RightMaxPowGenText.text = "最大工作效率".Translate();
                RightDysonBluePrintText.text = "巨构建筑蓝图".Translate();
                SpSailAmountText.text = "自由组件数量".Translate();
                SpSailLifeTimeText.text = "自由组件寿命分布".Translate();
                SpSailLifeTimeBarText.text = "自由组件状态统计".Translate();
                SpSwarmPowGenText.text = "自由组件工作效率".Translate();
                SpShellPowGenText.text = "锚定结构工作效率".Translate();
                SoSailAmountText.text = "自由组件数量".Translate();
                SoSailPowGenText.text = "工作效率".Translate();
                SoSailLifeTimeText.text = "自由组件寿命分布".Translate();
                SoSailLifeTimeBarText.text = "自由组件状态统计".Translate();
                LyPowGenText.text = "工作效率".Translate();
                NdPowGenText.text = "工作效率".Translate();
                FrPowGenText.text = "工作效率".Translate();
                ShPowGenText.text = "工作效率".Translate();


                if (curDysonSphere != null && StarMegaStructureType[curStar.index] == 6)
                {
                    SpSailAmountText.text = "连续开火次数".Translate();
                    SpNodeAmountText.text = "伤害削减".Translate();
                    SpConsumePowText.text = "当前能量水平".Translate();
                    SpSwarmPowGenText.text = "下一阶段所需能量水平".Translate();
                    SpShellPowGenText.text = "冷却及充能时间".Translate();

                    SpEnergySatisfiedLabelText.text = "修建进度".Translate();
                    SpEnergySatisfiedLabelText.lineSpacing = 0.65f;

                    if (StarCannon.GetStarCannonProperties(curDysonSphere)[0] >= 5)
                    {
                        SpEnergySatisfiedLabelText.text = "最终阶段".Translate();
                    }

                    DysonEditorPowerDescLabel4BarObj.SetActive(false);
                }
                else if (curDysonSphere != null && StarMegaStructureType[curStar.index] == 3)
                {
                    SpSailAmountText.text = "折跃场内曲速速率".Translate();
                    SpNodeAmountText.text = "折跃场内能量消耗".Translate();
                }
                else
                {
                    SpEnergySatisfiedLabelText.text = "供电率".Translate();
                    SpNodeAmountText.text = "节点总数（已规划）gm".Translate();
                    SpConsumePowText.text = "请求功率gm".Translate();
                    DysonEditorPowerDescLabel4BarObj.SetActive(true);
                }

                //初始化按钮等显示
                set2DysonButtonTextTrans.GetComponent<Text>().text = "规划".Translate() + "戴森球jinx".Translate();
                set2MatDecomButtonTextTrans.GetComponent<Text>().text = "规划".Translate() + "物质解压器".Translate();
                set2SciNexusButtonTextTrans.GetComponent<Text>().text = "规划".Translate() + "科学枢纽".Translate();
                set2WarpFieldGenButtonTextTrans.GetComponent<Text>().text = "规划".Translate() + "折跃场广播阵列".Translate(); //WarpFieldBroadcastArray
                set2MegaAssemButtonTextTrans.GetComponent<Text>().text = "规划".Translate() + "星际组装厂".Translate();      //生产多功能预制件
                set2CrystalMinerButtonTextTrans.GetComponent<Text>().text = "规划".Translate() + "晶体重构器".Translate();
                set2StarCannonButtonTextTrans.GetComponent<Text>().text = "规划".Translate() + "恒星炮".Translate();
                set2StarCannonButtonObj.SetActive(GameMain.data.history.TechState(MMSProtos.StarCannonTechId).unlocked);
                rightBarObj.GetComponent<RectTransform>().sizeDelta = new Vector2(8, GameMain.data.history.TechState(MMSProtos.StarCannonTechId).unlocked ? 243 : 213);

                set2DysonButtonTextTrans.GetComponent<Text>().color = normalTextColor;
                set2MatDecomButtonTextTrans.GetComponent<Text>().color = normalTextColor;
                set2SciNexusButtonTextTrans.GetComponent<Text>().color = normalTextColor;
                set2WarpFieldGenButtonTextTrans.GetComponent<Text>().color = normalTextColor;
                set2MegaAssemButtonTextTrans.GetComponent<Text>().color = normalTextColor;
                set2CrystalMinerButtonTextTrans.GetComponent<Text>().color = normalTextColor;
                set2StarCannonButtonTextTrans.GetComponent<Text>().color = normalTextColor;

                //根据当前恒星和巨构的状态修正显示
                int curtype = StarMegaStructureType[idx];
                RightDysonTitle.text = "巨构建筑".Translate() + " " + star.displayName;

                if (star.type != EStarType.BlackHole || (isBattleActive && !GameMain.history.TechUnlocked(1920)))
                {
                    set2MatDecomButtonTextTrans.GetComponent<Text>().color = disableTextColor;
                }

                if ((star.type != EStarType.NeutronStar && star.type != EStarType.WhiteDwarf) ||
                    (isBattleActive && !GameMain.history.TechUnlocked(1923)))
                {
                    set2CrystalMinerButtonTextTrans.GetComponent<Text>().color = disableTextColor;
                }

                if (isBattleActive && !GameMain.history.TechUnlocked(1924))
                {
                    set2SciNexusButtonTextTrans.GetComponent<Text>().color = disableTextColor;
                }

                if (isBattleActive && !GameMain.history.TechUnlocked(1922))
                {
                    set2MegaAssemButtonTextTrans.GetComponent<Text>().color = disableTextColor;
                }

                WarpBuiltStarIndex = CheckWarpArrayBuilt();
                CannonBuiltStarIndex = GetStarCannonBuiltIndex();
                if (WarpBuiltStarIndex >= 0 || (isBattleActive && !GameMain.history.TechUnlocked(1921)))
                {
                    set2WarpFieldGenButtonTextTrans.GetComponent<Text>().color = disableTextColor;
                }

                if (CannonBuiltStarIndex >= 0 || (isBattleActive && !GameMain.history.TechUnlocked(1918)))
                {
                    set2StarCannonButtonTextTrans.GetComponent<Text>().color = disableTextColor;
                }

                switch (curtype)
                {
                    case 0:
                        RightDysonTitle.text = "戴森球jinx".Translate() + " " + star.displayName;
                        set2DysonButtonTextTrans.GetComponent<Text>().color = currentTextColor;
                        set2DysonButtonTextTrans.GetComponent<Text>().text = "当前".Translate() + " " + "戴森球jinx".Translate();
                        break;

                    case 1:
                        RightDysonTitle.text = "物质解压器".Translate() + " " + star.displayName;
                        set2MatDecomButtonTextTrans.GetComponent<Text>().color = currentTextColor;
                        set2MatDecomButtonTextTrans.GetComponent<Text>().text = "当前".Translate() + " " + "物质解压器".Translate();
                        break;

                    case 2:
                        RightDysonTitle.text = "科学枢纽".Translate() + " " + star.displayName;
                        set2SciNexusButtonTextTrans.GetComponent<Text>().color = currentTextColor;
                        set2SciNexusButtonTextTrans.GetComponent<Text>().text = "当前".Translate() + " " + "科学枢纽".Translate();
                        RightMaxPowGenText.text = "研究效率".Translate();

                        break;

                    case 3:
                        RightDysonTitle.text = "折跃场广播阵列".Translate() + " " + star.displayName;
                        set2WarpFieldGenButtonTextTrans.GetComponent<Text>().color = currentTextColor;
                        set2WarpFieldGenButtonTextTrans.GetComponent<Text>().text = "当前".Translate() + " " + "折跃场广播阵列".Translate();
                        RightMaxPowGenText.text = "折跃场半径".Translate();
                        break;

                    case 4:
                        RightDysonTitle.text = "星际组装厂".Translate() + " " + star.displayName;
                        set2MegaAssemButtonTextTrans.GetComponent<Text>().color = currentTextColor;
                        set2MegaAssemButtonTextTrans.GetComponent<Text>().text = "当前".Translate() + " " + "星际组装厂".Translate();
                        RightMaxPowGenText.text = "最大生产速度gm".Translate();
                        break;

                    case 5:
                        RightDysonTitle.text = "晶体重构器".Translate() + " " + star.displayName;
                        set2CrystalMinerButtonTextTrans.GetComponent<Text>().color = currentTextColor;
                        set2CrystalMinerButtonTextTrans.GetComponent<Text>().text = "当前".Translate() + " " + "晶体重构器".Translate();
                        break;

                    case 6:
                        RightDysonTitle.text = "恒星炮".Translate() +
                                               " - " +
                                               "阶段" +
                                               StarCannon.GetStarCannonProperties(curDysonSphere)[0] +
                                               " " +
                                               star.displayName;
                        set2StarCannonButtonTextTrans.GetComponent<Text>().color = currentTextColor;
                        set2StarCannonButtonTextTrans.GetComponent<Text>().text = "当前".Translate() + " " + "恒星炮".Translate();
                        RightMaxPowGenText.text = "每秒伤害gm".Translate();
                        break;
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 尝试设置或改变巨构建筑的类型
        /// </summary>
        /// <param name="type"></param>
        public static void SetMegaStructure(int type)
        {
            try
            {
                int idx = curStar.id - 1;
                if (idx > 999)
                {
                    UIRealtimeTip.Popup("警告巨构不支持恒星系数量大于1000个".Translate());
                    return;
                }

                //没改变类型，无效操作
                if (type == StarMegaStructureType[idx])
                {
                    return;
                }

                //战斗mod需要解锁科技才可以改变巨构类型
                if (isBattleActive)
                {
                    if ((type == 1 && !GameMain.history.TechUnlocked(1920)) ||
                        (type == 2 && !GameMain.history.TechUnlocked(1924)) ||
                        (type == 3 && !GameMain.history.TechUnlocked(1921)) ||
                        (type == 4 && !GameMain.history.TechUnlocked(1922)) ||
                        (type == 5 && !GameMain.history.TechUnlocked(1923)) ||
                        (type == 6 && !GameMain.history.TechUnlocked(1918)))
                    {
                        UIRealtimeTip.Popup("警告巨构科技未解锁".Translate());
                        return;
                    }
                }

                //各种不满足条件不能修改巨构类型的情况
                if (type == 1 && curStar.type != EStarType.BlackHole)
                {
                    //SetMegaStructureWarningText.text = "警告仅黑洞".Translate();
                    UIRealtimeTip.Popup("警告仅黑洞".Translate());
                    return;
                }

                //if (type == 3 && WarpBuiltStarIndex >= 0)
                //{
                //    string systemName = GameMain.galaxy.stars[WarpBuiltStarIndex].displayName;
                //    UIRealtimeTip.Popup("警告最多一个".Translate() + " " + systemName);
                //    return;
                //}

                if (type == 5 && curStar.type != EStarType.NeutronStar && curStar.type != EStarType.WhiteDwarf)
                {
                    //SetMegaStructureWarningText.text = "警告仅中子星白矮星".Translate();
                    UIRealtimeTip.Popup("警告仅中子星白矮星".Translate());
                    return;
                }

                if (type == 6 && CannonBuiltStarIndex >= 0)
                {
                    string systemName = GameMain.galaxy.stars[CannonBuiltStarIndex].displayName;
                    //SetMegaStructureWarningText.text = "警告最多一个".Translate() + " " + systemName;
                    UIRealtimeTip.Popup("警告最多一个恒星炮".Translate() + " " + systemName);
                    return;
                }
                else if(type == 6 && !GameMain.data.history.TechState(MMSProtos.StarCannonTechId).unlocked)
                {
                    UIRealtimeTip.Popup("先解锁恒星炮科技警告".Translate());
                    return;
                }

                //根据是否有现存框架，是否允许改变巨构类型
                if (curDysonSphere != null)
                {
                    if (curDysonSphere.totalNodeCount > 0 && !developerMode && !GameMain.data.gameDesc.isSandboxMode) //如果有框架，则不允许修改巨构类型，在后续的UI刷新时对应修改按钮状态和文本
                    {
                        UIRealtimeTip.Popup("警告先拆除".Translate());
                        return;
                    }
                }

                //条件满足
                StarMegaStructureType[idx] = type;
                OnMegaTypeChanged();

                if (type == 4)
                {
                    StarAssembly.ResetArchiveDataByStarIndex(idx);
                    StarAssembly.CalcInGameDataByStarIndex(idx);
                    RefreshUILabels(curStar, true);
                }
                else
                {
                    RefreshUILabels(curStar);
                }

                if (type == 2 && GenesisCompatibility) // 改成科学枢纽后删除所有太阳帆，目前只对创世之书生效
                    curDysonSphere.swarm.RemoveSailsByOrbit(-1);

            }
            catch (Exception)
            {
                //SetMegaStructureWarningText.text = "警告未知错误".Translate();
                UIRealtimeTip.Popup("警告未知错误".Translate());
                return;
            }
        }

        public static void OnMegaTypeChanged()
        {
            WarpArray.CheckSectorWarpArrays();
        }

        //每秒刷新巨构UI的总Capacity数值的显示，主要用于科学枢纽和广播阵列（这两个不需要接收器）显示其效率
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDEOverview), "_OnUpdate")]
        public static void UIValueUpdate()
        {
            try
            {
                if (StarMegaStructureType[curDysonSphere.starData.id - 1] == 2) //如果是科学枢纽
                {
                    //long baseSpeed = (curDysonSphere.energyGenCurrentTick - curDysonSphere.energyReqCurrentTick) / HashGenDivisor * 60L;
                    long HashP = (curDysonSphere.energyGenCurrentTick - curDysonSphere.energyReqCurrentTick) *
                                 (HashBasicSpeedScale + HashBonusPerLevel * GameMain.history.techSpeed) /
                                 HashGenDivisor;
                    RightMaxPowGenValueText.text = Capacity2Str(HashP * 60) + "H/s";
                }
                else if (StarMegaStructureType[curDysonSphere.starData.id - 1] == 3) //如果是折跃场广播阵列
                {
                    long DysonEnergy = (curDysonSphere.energyGenCurrentTick - curDysonSphere.energyReqCurrentTick);
                    RightMaxPowGenValueText.text = (WarpArray.GetRadiusByEnergyPerFrame(DysonEnergy)/60/40000).ToString("N1") + " ly";
                }
                else if (StarMegaStructureType[curDysonSphere.starData.id - 1] == 4) //如果是星际组装厂
                {
                    long DysonEnergy = (curDysonSphere.energyGenCurrentTick - curDysonSphere.energyReqCurrentTick) * 60;
                    RightMaxPowGenValueText.text = Capacity2Str(DysonEnergy / 60.0 / StarAssembly.tickEnergyForFullSpeed) + "x";
                }
                else if (StarMegaStructureType[curDysonSphere.starData.id - 1] == 6) //如果是恒星炮
                {
                    RightDysonTitle.text = "恒星炮".Translate() +
                                           " - " +
                                           "阶段" +
                                           StarCannon.GetStarCannonProperties(curDysonSphere)[0] +
                                           " " +
                                           curStar.displayName; //因为阶段可能会变，巨构的标题里面有stage阶段，因此也会变
                    RightMaxPowGenValueText.text = (StarCannon.GetStarCannonProperties(curDysonSphere)[1] * 60 / 100).ToString();
                }
            }
            catch (Exception)
            {
                //Debug.LogWarning("Unable to edit the DysonUI's PowerGen Value.");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIDESphereInfo), "_OnUpdate")]
        public static void UIValueUpdate2(ref UIDESphereInfo __instance)
        {
            if (curDysonSphere != null)
            {
                if (StarMegaStructureType[curDysonSphere.starData.index] == 3)
                {
                    __instance.sailCntText.text = (WarpArray.warpSpeedInWarpField / 60 / 40000).ToString() + " ly/s";
                    __instance.nodeCntText.text = "-" + ((1 - WarpArray.GetTripEnergyCostRatioByEnergyPerFrame(curDysonSphere.energyGenCurrentTick)) * 100).ToString("N0") + " %";
                }
                else if (StarMegaStructureType[curDysonSphere.starData.index] == 6)
                {
                    int[] curData = StarCannon.GetStarCannonProperties(curDysonSphere);
                    __instance.sailCntText.text = curData[2] < 9000 ? curData[2].ToString() : "无限制gm".Translate();
                    __instance.nodeCntText.text = "-" + curData[5].ToString() + "% / ly";
                }
            }
        }

        //查看折跃场广播阵列是否达到建造上限
        public static int CheckWarpArrayBuilt()
        {
            return -1;
        }

        public static int GetStarCannonBuiltIndex()
        {
            for (int i = 0; i < 1000; i++)
            {
                if (StarMegaStructureType[i] == 6) return i;
            }

            return -1;
        }

        //下面两个prefix+postfix联合作用。由于新版游戏实际执行的能量消耗、采集速率等属性都使用映射到的modelProto的prefabDesc中的数值，而不是itemProto的PrefabDesc，而修改/新增modelProto我还不会改，会报错（貌似是和模型读取不到有关）
        //因此，提前修改设定建筑信息时读取的PrefabDesc的信息，在存储建筑属性前先修改一下（改成itemProto的PrefabDesc中对应的某些值），建造建筑设定完成后再改回去
        //并且，原始item和model执向的貌似是同一个PrefabDesc，所以不能直接改model的，然后再还原成oriItem的prefabDesc，因为改了model的oriItem的也变了，还原不回去了。所以得Copy一个出来改。
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetFactory), "AddEntityDataWithComponents")]
        public static bool AddEntityDataPrePatch(EntityData entity, ref PrefabDesc __state)
        {
            int gmProtoId = entity.protoId;
            if ((gmProtoId < 9493 || gmProtoId > 9499) && gmProtoId != 2208 && gmProtoId != 9501 && gmProtoId != 9502)
            {
                __state = null;
                return true; //不相关建筑直接返回
            }

            ItemProto itemProto = LDB.items.Select(entity.protoId);
            if (itemProto == null || !itemProto.IsEntity)
            {
                __state = null;
                return true;
            }

            ModelProto modelProto = LDB.models.Select(entity.modelIndex);
            __state = modelProto.prefabDesc;
            modelProto.prefabDesc = __state.Copy();
            modelProto.prefabDesc.powerProductId = itemProto.prefabDesc.powerProductId;
            modelProto.prefabDesc.powerProductHeat = itemProto.prefabDesc.powerProductHeat;
            //modelProto.prefabDesc.ejectorBulletId = itemProto.prefabDesc.ejectorBulletId;
            //modelProto.prefabDesc.siloBulletId = itemProto.prefabDesc.siloBulletId;
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetFactory), "AddEntityDataWithComponents")]
        public static void AddEntityDataPostPatch(EntityData entity, ref PrefabDesc __state)
        {
            if (__state == null)
            {
                return;
            }

            int gmProtoId = entity.protoId;
            if ((gmProtoId < 9493 || gmProtoId > 9499) && gmProtoId != 2208 && gmProtoId != 9501 && gmProtoId != 9502)
            {
                return; //不相关
            }

            ModelProto modelProto = LDB.models.Select(entity.modelIndex);
            modelProto.prefabDesc = __state; //还原
            return;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UITechNode), "UpdateLayoutDynamic")]
        public static void UITechNode_UpdateLayoutDynamic(ref UITechNode __instance, bool forceUpdate = false, bool forceReset = false)
        {
            float num4 = Mathf.Max(__instance.unlockText.preferredWidth - 40f + __instance.unlockTextTrans.anchoredPosition.x,
                                   Math.Min(__instance.techProto.unlockRecipeArray.Length, 3) * 46) +
                         __instance.baseWidth;
            if (num4 < __instance.minWidth)
            {
                num4 = __instance.minWidth;
            }

            if (num4 > __instance.maxWidth) num4 = __instance.maxWidth;

            if (__instance.focusState < 1f)
            {
                __instance.panelRect.sizeDelta
                    = new Vector2(Mathf.Lerp(__instance.minWidth, num4, __instance.focusState), __instance.panelRect.sizeDelta.y);
            }
            else
            {
                __instance.panelRect.sizeDelta = new Vector2(Mathf.Lerp(num4, __instance.maxWidth, __instance.focusState - 1f),
                                                             __instance.panelRect.sizeDelta.y);
            }

            __instance.titleText.rectTransform.sizeDelta
                = new Vector2(__instance.panelRect.sizeDelta.x - ((GameMain.history.TechState(__instance.techProto.ID).curLevel > 0) ? 65 : 25), 24f);
        }

        /// <summary>
        /// Save and Load
        /// </summary>
        /// <param name="r"></param>
        public void Import(BinaryReader r)
        {
            curStar = null;
            curDysonSphere = null;
            savedModVersion = r.ReadInt32();
            for (int i = 0; i < 1000; i++)
            {
                StarMegaStructureType[i] = r.ReadInt32();
            }

            if (savedModVersion >= 101)
            {
                maxAutoReceiveGear = r.ReadInt32();
                autoReceiveGearProgress = r.ReadInt64();
            }
            else
            {
                maxAutoReceiveGear = 1000;
                autoReceiveGearProgress = 0;
            }

            selectAutoReceiveGearLimitComboBox.itemIndex = maxAutoReceiveGear / 1000;

            hashGenByAllSN = 0;

            RefreshUIWhenLoad();
            InitResolutionWhenLoad();
            //EffectRenderer.InitAll();

            if (savedModVersion >= 110)
            {
                StarAssembly.Import(r);
                StarAssembly.InitInGameData();
                //StarAssembly.ResetUIBtnTransitions();
            }
            else
            {
                StarAssembly.ResetAndInitArchiveData();
                StarAssembly.InitInGameData();
                //StarAssembly.ResetUIBtnTransitions();
            }
            
            UIBuildMenuPatcher.InitDataWhenLoad();
            if (savedModVersion >= 130)
            {
                StarCannon.Import(r);
            }
            else
            {
                StarCannon.IntoOtherSave();
            }
            WarpArray.Import(r);
            UIWarpArray.Import(r);

            // 放在最后
            UIStatisticsPatcher.Import(r);
            if (GameMain.galaxy.starCount >= 100 && !Support1000Stars.Value)
            {
                UIMessageBox.Show("警告mms".Translate(), "警告未开启大于1000星系支持".Translate(), "我明白".Translate(), 1);
            }
            if(savedModVersion < 160)
            {
                UIMessageBox.Show("警告mms".Translate(), "星际组装厂逻辑更新警告".Translate(), "我明白".Translate(), 1);
            }
        }

        public void Export(BinaryWriter w)
        {
            w.Write(modVersion);
            for (int i = 0; i < 1000; i++)
            {
                w.Write(StarMegaStructureType[i]);
            }

            w.Write(maxAutoReceiveGear);
            w.Write(autoReceiveGearProgress);

            StarAssembly.Export(w);
            StarCannon.Export(w);
            WarpArray.Export(w);
            UIWarpArray.Export(w);

            // 放在最后
            UIStatisticsPatcher.Export(w);
        }

        public void IntoOtherSave()
        {
            for (int i = 0; i < 1000; i++)
            {
                StarMegaStructureType[i] = 0;
            }

            maxAutoReceiveGear = 1000;
            autoReceiveGearProgress = 0;
            selectAutoReceiveGearLimitComboBox.itemIndex = maxAutoReceiveGear / 1000;
            if (isBattleActive) { }

            hashGenByAllSN = 0;

            RefreshUIWhenLoad();
            InitResolutionWhenLoad();
            //EffectRenderer.InitAll();
            UIBuildMenuPatcher.InitDataWhenLoad();
            StarCannon.IntoOtherSave();
            WarpArray.IntoOtherSave();
            UIWarpArray.IntoOtherSave();

            // 放在最后
            UIStatisticsPatcher.IntoOtherSave();
        }

        private static string Capacity2Str(double capacityPerSecond)
        {
            if (capacityPerSecond >= 100 || capacityPerSecond == 0)
                return Capacity2Str((long)capacityPerSecond);
            if (capacityPerSecond >= 10)
            {
                return capacityPerSecond.ToString("F1") + " ";
            }

            if (capacityPerSecond >= 1)
            {
                return capacityPerSecond.ToString("F2") + " ";
            }

            return capacityPerSecond.ToString("F3") + " ";
        }

        internal static string Capacity2Str(long capacityPerSecond)
        {
            long midValue;
            string unitStr = "";
            if (capacityPerSecond >= 1000000000000000000)
            {
                midValue = capacityPerSecond / 1000000000000000;
                //return (midValue / 1000.0).ToString("G3") + " E";
                unitStr = " E";
            }
            else if (capacityPerSecond >= 1000000000000000)
            {
                midValue = capacityPerSecond / 1000000000000;
                //return (midValue / 1000.0).ToString("G3") + " P";
                unitStr = " P";
            }
            else if (capacityPerSecond >= 1000000000000)
            {
                midValue = capacityPerSecond / 1000000000;
                //return (midValue / 1000.0).ToString("G3") + " T";
                unitStr = " T";
            }
            else if (capacityPerSecond >= 1000000000)
            {
                midValue = capacityPerSecond / 1000000;
                //return (midValue / 1000.0).ToString("G3") + " G";
                unitStr = " G";
            }
            else if (capacityPerSecond >= 1000000)
            {
                midValue = capacityPerSecond / 1000;
                //return (midValue / 1000.0).ToString("G3") + " M";
                unitStr = " M";
            }
            else if (capacityPerSecond >= 1000)
            {
                midValue = capacityPerSecond;
                //return (midValue / 1000.0).ToString("G3") + " k";
                unitStr = " k";
            }
            else
            {
                return (capacityPerSecond + " ");
            }

            if (midValue >= 100000)
            {
                return (midValue / 1000) + unitStr;
            }

            if (midValue >= 10000)
            {
                return ((midValue / 100) / 10.0) + unitStr;
            }

            return ((midValue / 10) / 100.0) + unitStr;
        }

        public static string Capacity2SpeedAcc(int ratio)
        {
            return (ratio * 0.05).ToString("G3") + " ";
        }
    }

    /// 以下为创世之书特别适配用
    [BepInDependency("org.LoShin.GenesisBook")]
    [BepInPlugin("Gnimaerd.DSP.plugin.MMSGCPatch", "MMSGCPatch", "1.0")]
    public class GenesisCompatibilityPatch : BaseUnityPlugin
    {
        void Awake()
        {
            if (MoreMegaStructure.isBattleActive) return;
            
            MoreMegaStructure.GenesisCompatibility = true;
        }
    }

    //[BepInDependency("com.ckcz123.DSP_Battle")]
    //[BepInPlugin("Gnimaerd.DSP.plugin.MMSBattle", "MMSBattle", "1.0")]
    //public class DSPBattleCompatibilityPatch : BaseUnityPlugin
    //{
    //    void Awake()
    //    {
    //        try
    //        {
    //            MoreMegaStructure.TCFVCompatibility = true;
    //        }
    //        catch
    //        {
    //            // ignore
    //        }
    //    }
    //}


    //[BepInDependency("com.menglei.dsp.FractionateEverything")]
    //[BepInPlugin("Gnimaerd.DSP.plugin.MMSFEPatch", "MMSFEPatch", "1.0")]
    //public class FractionateEverythingCompatibilityPatch : BaseUnityPlugin
    //{
    //    void Awake()
    //    {
    //        try
    //        {
    //            //MoreMegaStructure.FECompatibility = true;
    //        }
    //        catch
    //        {
    //        }
    //    }
    //}
}
