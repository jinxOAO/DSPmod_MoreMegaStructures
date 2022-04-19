using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xiaoye97;
using CommonAPI;
using CommonAPI.Systems;
using UnityEngine;

namespace MoreMegaStructure
{
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry))]
    class MMSProtos
    {

        public static void RefreshInitAll()
        {
            ItemProto.InitFluids();
            ItemProto.InitItemIds();
            ItemProto.InitFuelNeeds();
            ItemProto.InitItemIndices();
            ItemProto.InitMechaMaterials();
        }
        public static void ItemsDesc()
        {
            //简单地用临界光子轰击奇异物质即可激发引力波，但这也意味着只有在恒星附近才能使引力发生装置高效运行。
            //借助黑洞本身的引力，引力钻头能够将物质从黑洞中取出，这还包括吸积盘中大量的单极磁石。借助谐振盘，黑洞原质将能够被解压并在星系内输送。
            //将临界光子沿变频设备向中子星或白矮星发射，从而向地表引导规模庞大的微晶流。微晶流中的可控中子直接在合成器中部分地进行β衰变，即可直接形成稳定且完美的卡西米尔晶体。
            //位面约束环能够协同引力透镜引导并操纵引力，也是构建科学枢纽所需的恒星级粒子加速器的必要组件。
            //隧穿激发装置可以完美地掌控量子隧穿效应，常被用来强化量子芯片的处理能力和纠错能力。通过量子隧穿效应还能够轻易突破弯曲空间的能量势垒，使得在任意远的空间打开裂口成为可能。
            //谐振盘仅通过恒星级别的能量就可以产生跨越恒星系的空间波动能量束。如果将谐振盘组成阵列，理论上可以形成覆盖全宇宙的折跃能量场。
            //只要供给足够的能量，量子计算机的单线程运算时钟能够无限逼近普朗克时间，通过量子比特协同，其潜在的单线程运算速率还能突破物理极限，并可以无限提升。现在，限制其计算效率的将只有能量输入水平。
        }


        public static void AddNewItems()
        {
            int pagePlus = MoreMegaStructure.pagenum * 1000;
            int linePlus = 0;
            if(MoreMegaStructure.isBattleActive)
            {
                pagePlus = MoreMegaStructure.battlePagenum * 1000 + 100;
                linePlus = -100;
            }

            var oriRecipe = LDB.recipes.Select(51);
            var oriItem = LDB.items.Select(1303);
            int recipeIdBias = 0;
            if(MoreMegaStructure.GenesisCompatibility)
            {
                recipeIdBias = -200;
            }

            //引力发生装置
            var itemGravityGenRecipe = oriRecipe.Copy();
            var itemGravityGen = oriItem.Copy();
            itemGravityGenRecipe.ID = 530;
            itemGravityGenRecipe.Name = "引力发生装置";
            itemGravityGenRecipe.name = "引力发生装置".Translate();
            itemGravityGenRecipe.Description = "引力发生装置描述";
            itemGravityGenRecipe.description = "引力发生装置描述".Translate();
            itemGravityGenRecipe.Items = new int[] { 1107, 1127 };
            itemGravityGenRecipe.ItemCounts = new int[] { 1, 1 };
            itemGravityGenRecipe.Results = new int[] { 9480 };
            itemGravityGenRecipe.ResultCounts = new int[] { 1 };
            itemGravityGenRecipe.TimeSpend = 180;
            itemGravityGenRecipe.GridIndex = 101 + pagePlus + linePlus;
            itemGravityGenRecipe.preTech = LDB.techs.Select(1704);
            Traverse.Create(itemGravityGenRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconGravityGen);
            ProtoRegistry.RegisterItem(9480, "引力发生装置".Translate(), "引力发生装置描述".Translate(), "Assets/MegaStructureTab/gravitygenerator", 101 + pagePlus + linePlus, 100,
                EItemType.Component, ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.9f, 0.3f)));

            //位面约束环
            var itemConstrainRingRecipe = oriRecipe.Copy();
            var itemConstrainRing = oriItem.Copy();
            itemConstrainRingRecipe.ID = 531;
            itemConstrainRingRecipe.Name = "位面约束环";
            itemConstrainRingRecipe.name = "位面约束环".Translate();
            itemConstrainRingRecipe.Description = "位面约束环描述";
            itemConstrainRingRecipe.description = "位面约束环描述".Translate();
            itemConstrainRingRecipe.Items = new int[] { 1205, 1304 };
            itemConstrainRingRecipe.ItemCounts = new int[] { 2, 1 };
            if(MoreMegaStructure.GenesisCompatibility)
            {
                itemConstrainRingRecipe.Items = new int[] { 1205, 1119, 1126 };
                itemConstrainRingRecipe.ItemCounts = new int[] { 2, 1, 1 };

            }
            itemConstrainRingRecipe.Results = new int[] { 9481 };
            itemConstrainRingRecipe.ResultCounts = new int[] { 2 };
            itemConstrainRingRecipe.TimeSpend = 180;
            itemConstrainRingRecipe.GridIndex = 102 + pagePlus + linePlus;
            itemConstrainRingRecipe.preTech = LDB.techs.Select(1141);
            Traverse.Create(itemConstrainRingRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconConstrainRing);
            ProtoRegistry.RegisterItem(9481, "位面约束环".Translate(), "位面约束环描述".Translate(), "Assets/MegaStructureTab/constrainring", 102 + pagePlus + linePlus, 100,
                EItemType.Component, ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.4f,0.08f,0.4f)));

            
            //引力钻头
            var itemGravityDrillRecipe = oriRecipe.Copy();
            var itemGravityDrill = oriItem.Copy();
            itemGravityDrillRecipe.ID = 532;
            itemGravityDrillRecipe.Name = "引力钻头";
            itemGravityDrillRecipe.name = "引力钻头".Translate();
            itemGravityDrillRecipe.Description = "引力钻头描述";
            itemGravityDrillRecipe.description = "引力钻头描述".Translate();
            itemGravityDrillRecipe.Items = new int[] { 9480, 9481, 1209 };
            itemGravityDrillRecipe.ItemCounts = new int[] { 1, 1, 1 };
            itemGravityDrillRecipe.Results = new int[] { 9482 };
            itemGravityDrillRecipe.ResultCounts = new int[] { 1 };
            itemGravityDrillRecipe.TimeSpend = 180;
            itemGravityDrillRecipe.GridIndex = 103 + pagePlus + linePlus;
            itemGravityDrillRecipe.preTech = LDB.techs.Select(1704);
            Traverse.Create(itemGravityDrillRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconGravityDrill);
            ProtoRegistry.RegisterItem(9482, "引力钻头".Translate(), "引力钻头描述".Translate(), "Assets/MegaStructureTab/gravitydrill2", 103 + pagePlus + linePlus, 50,
                EItemType.Component, ProtoRegistry.GetDefaultIconDesc(Color.black, new Color(0.3f, 0.9f, 0.3f)));

            //隧穿激发装置
            var itemExciterRecipe = oriRecipe.Copy();
            var itemExciter = oriItem.Copy();
            itemExciterRecipe.ID = 533;
            itemExciterRecipe.Name = "隧穿激发装置";
            itemExciterRecipe.name = "隧穿激发装置".Translate();
            itemExciterRecipe.Description = "隧穿激发装置描述";
            itemExciterRecipe.description = "隧穿激发装置描述".Translate();
            itemExciterRecipe.Items = new int[] { 1206, 1404 };
            itemExciterRecipe.ItemCounts = new int[] { 1, 1 };
            if (MoreMegaStructure.GenesisCompatibility)
            {
                itemExciterRecipe.Items = new int[] { 1303, 1014 };
                itemExciterRecipe.ItemCounts = new int[] { 1, 2 };
            }
            itemExciterRecipe.Results = new int[] { 9483 };
            itemExciterRecipe.ResultCounts = new int[] { 6 };
            itemExciterRecipe.TimeSpend = 360;
            itemExciterRecipe.GridIndex = 104 + pagePlus + linePlus;
            itemExciterRecipe.preTech = LDB.techs.Select(1703);
            if (MoreMegaStructure.GenesisCompatibility)
            {
                itemExciterRecipe.preTech = LDB.techs.Select(1302);
            }
            Traverse.Create(itemExciterRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconTunnExciter);
            ProtoRegistry.RegisterItem(9483, "隧穿激发装置".Translate(), "隧穿激发装置描述".Translate(), "Assets/MegaStructureTab/tunnelingexciter2", 104 + pagePlus + linePlus, 200,
                EItemType.Component, ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.1f, 0.0f)));
            //谐振盘
            var itemDiscRecipe = oriRecipe.Copy();
            var itemDisc = oriItem.Copy();
            itemDiscRecipe.ID = 534;
            itemDiscRecipe.Name = "谐振盘";
            itemDiscRecipe.name = "谐振盘".Translate();
            itemDiscRecipe.Description = "谐振盘描述";
            itemDiscRecipe.description = "谐振盘描述".Translate();
            itemDiscRecipe.Items = new int[] { 9483, 1113, 1305 };
            itemDiscRecipe.ItemCounts = new int[] { 3, 2, 1 };
            itemDiscRecipe.Results = new int[] { 9484 };
            itemDiscRecipe.ResultCounts = new int[] { 1 };
            itemDiscRecipe.TimeSpend = 240;
            itemDiscRecipe.GridIndex = 105 + pagePlus + linePlus;
            itemDiscRecipe.preTech = LDB.techs.Select(1303);
            Traverse.Create(itemDiscRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconResDisc);
            ProtoRegistry.RegisterItem(9484, "谐振盘".Translate(), "谐振盘描述".Translate(), "Assets/MegaStructureTab/resonancedisc", 105 + pagePlus + linePlus, 200,
                EItemType.Component, ProtoRegistry.GetDefaultIconDesc(Color.gray, new Color(0.4f, 0.4f, 0.8f)));
            //光子探针
            var itemProbeRecipe = oriRecipe.Copy();
            var itemProbe = oriItem.Copy();
            itemProbeRecipe.ID = 535;
            itemProbeRecipe.Name = "光子探针";
            itemProbeRecipe.name = "光子探针".Translate();
            itemProbeRecipe.Description = "光子探针描述";
            itemProbeRecipe.description = "光子探针描述".Translate();
            itemProbeRecipe.Items = new int[] { 1404, 1208 };
            itemProbeRecipe.ItemCounts = new int[] { 2, 1 };
            if (MoreMegaStructure.GenesisCompatibility)
            {
                itemProbeRecipe.Items = new int[] { 1014, 1208 };
                itemProbeRecipe.ItemCounts = new int[] { 4, 1 };
            }
            itemProbeRecipe.Results = new int[] { 9485 };
            itemProbeRecipe.ResultCounts = new int[] { 1 };
            itemProbeRecipe.TimeSpend = 240;
            itemProbeRecipe.GridIndex = 106 + pagePlus + linePlus;
            itemProbeRecipe.preTech = LDB.techs.Select(1504); //射线接收站的科技
            Traverse.Create(itemProbeRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconPhotonProbe);
            var icondesc_alpha = ProtoRegistry.GetDefaultIconDesc(Color.gray, new Color(0.6f, 0.6f, 0.9f));
            icondesc_alpha.solidAlpha = 0.1f;
            ProtoRegistry.RegisterItem(9485, "光子探针".Translate(), "光子探针描述".Translate(), "Assets/MegaStructureTab/photonprobeflipsmall", 106 + pagePlus + linePlus, 200,
                EItemType.Component, icondesc_alpha);

            //量子计算机
            var itemQuanCompRecipe = oriRecipe.Copy();
            var itemQuanComp = oriItem.Copy();
            itemQuanCompRecipe.ID = 536;
            itemQuanCompRecipe.Name = "量子计算机";
            itemQuanCompRecipe.name = "量子计算机".Translate();
            itemQuanCompRecipe.Description = "量子计算机描述";
            itemQuanCompRecipe.description = "量子计算机描述".Translate();
            itemQuanCompRecipe.Items = new int[] { 1305, 1402, 9483 };
            itemQuanCompRecipe.ItemCounts = new int[] { 3, 2, 1 };
            itemQuanCompRecipe.Results = new int[] { 9486 };
            itemQuanCompRecipe.ResultCounts = new int[] { 1 };
            itemQuanCompRecipe.TimeSpend = 720;
            itemQuanCompRecipe.GridIndex = 107 + pagePlus + linePlus;
            itemQuanCompRecipe.preTech = LDB.techs.Select(1303);
            Traverse.Create(itemQuanCompRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconQuanComp);
            var icondesc_alpha2 = ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0f, 0.7f, 1f));
            icondesc_alpha2.solidAlpha = 0f;
            ProtoRegistry.RegisterItem(9486, "量子计算机".Translate(), "量子计算机描述".Translate(), "Assets/MegaStructureTab/quantumcomputer3", 107 + pagePlus + linePlus, 200,
                EItemType.Component, icondesc_alpha2);
            //星际组装厂组件
            var itemIACompoRecipe = oriRecipe.Copy();
            var itemIACompo = oriItem.Copy();
            itemIACompoRecipe.ID = 537;
            itemIACompoRecipe.Name = "星际组装厂组件";
            itemIACompoRecipe.name = "星际组装厂组件".Translate();
            itemIACompoRecipe.Description = "星际组装厂组件描述";
            itemIACompoRecipe.description = "星际组装厂组件描述".Translate();
            itemIACompoRecipe.Items = new int[] { 1125, 2305, 1143 };
            itemIACompoRecipe.ItemCounts = new int[] { 3, 3, 1 };
            itemIACompoRecipe.Results = new int[] { 9487 };
            itemIACompoRecipe.ResultCounts = new int[] { 1 };
            itemIACompoRecipe.TimeSpend = 480;
            itemIACompoRecipe.GridIndex = 108 + pagePlus + linePlus;
            itemIACompoRecipe.preTech = LDB.techs.Select(1303);
            if (MoreMegaStructure.isBattleActive) itemIACompoRecipe.preTech = LDB.techs.Select(1922);
            Traverse.Create(itemIACompoRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconIACompo);
            ProtoRegistry.RegisterItem(9487, "星际组装厂组件".Translate(), "星际组装厂组件描述".Translate(), "Assets/MegaStructureTab/iacomponent", 108 + pagePlus + linePlus, 200,
                EItemType.Component, ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.7f,0.2f,0.7f)));

            //下面是火箭
            var oriRecipe2 = LDB.recipes.Select(83);
            var oriItem2 = LDB.items.Select(1503);
            //物质解压器运载火箭
            var rocketMDRecipe = oriRecipe2.Copy();
            var rocketMD = oriItem2.Copy();
            rocketMDRecipe.ID = 538;
            rocketMDRecipe.Name = "物质解压器运载火箭";
            rocketMDRecipe.name = "物质解压器运载火箭".Translate();
            rocketMDRecipe.Description = "物质解压器运载火箭描述";
            rocketMDRecipe.description = "物质解压器运载火箭描述".Translate();
            rocketMDRecipe.Items = new int[] { 9482, 9484, 1802 };
            rocketMDRecipe.ItemCounts = new int[] { 2, 1, 2 };
            rocketMDRecipe.Results = new int[] { 9488 };
            rocketMDRecipe.ResultCounts = new int[] { 1 };
            rocketMDRecipe.TimeSpend = 480;
            rocketMDRecipe.GridIndex = 201 + pagePlus;
            rocketMDRecipe.preTech = LDB.techs.Select(1522); //垂直发射井科技
            if (MoreMegaStructure.isBattleActive) rocketMDRecipe.preTech = LDB.techs.Select(1920);
            Traverse.Create(rocketMDRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconRocketMattD);
            ProtoRegistry.RegisterItem(9488, "物质解压器运载火箭".Translate(), "物质解压器运载火箭描述".Translate(), "Assets/MegaStructureTab/rocketMatter", 201 + pagePlus, 20,
                EItemType.Product, ProtoRegistry.GetDefaultIconDesc(new Color(1f, 0.9f, 0.9f), new Color(0.7f, 0.2f, 0.2f)));
            //科学枢纽运载火箭
            var rocketSNRecipe = oriRecipe2.Copy();
            var rocketSN = oriItem2.Copy();
            rocketSNRecipe.ID = 539;
            rocketSNRecipe.Name = "科学枢纽运载火箭";
            rocketSNRecipe.name = "科学枢纽运载火箭".Translate();
            rocketSNRecipe.Description = "科学枢纽运载火箭描述";
            rocketSNRecipe.description = "科学枢纽运载火箭描述".Translate();
            rocketSNRecipe.Items = new int[] { 9481, 9486, 1802 };
            rocketSNRecipe.ItemCounts = new int[] { 3, 1, 2 };
            rocketSNRecipe.Results = new int[] { 9489 };
            rocketSNRecipe.ResultCounts = new int[] { 1 };
            rocketSNRecipe.TimeSpend = 480;
            rocketSNRecipe.GridIndex = 202 + pagePlus;
            rocketSNRecipe.preTech = LDB.techs.Select(1522); //垂直发射井科技
            if (MoreMegaStructure.isBattleActive) rocketSNRecipe.preTech = LDB.techs.Select(1924);
            Traverse.Create(rocketSNRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconRocketScieN);
            ProtoRegistry.RegisterItem(9489, "科学枢纽运载火箭".Translate(), "科学枢纽运载火箭描述".Translate(), "Assets/MegaStructureTab/rocketScience", 202 + pagePlus, 20,
                EItemType.Product, ProtoRegistry.GetDefaultIconDesc(new Color(1f, 1f, 0.9f), new Color(0.7f, 0.7f, 0.2f)));
            //谐振发射器运载火箭
            var rocketWBARecipe = oriRecipe2.Copy();
            var rocketWBA = oriItem2.Copy();
            rocketWBARecipe.ID = recipeIdBias + 540;
            rocketWBARecipe.Name = "谐振发射器运载火箭";
            rocketWBARecipe.name = "谐振发射器运载火箭".Translate();
            rocketWBARecipe.Description = "谐振发射器运载火箭描述";
            rocketWBARecipe.description = "谐振发射器运载火箭描述".Translate();
            rocketWBARecipe.Items = new int[] { 9480, 9484, 1802 };
            rocketWBARecipe.ItemCounts = new int[] { 1, 4, 2 };
            rocketWBARecipe.Results = new int[] { 9490 };
            rocketWBARecipe.ResultCounts = new int[] { 1 };
            rocketWBARecipe.TimeSpend = 480;
            rocketWBARecipe.GridIndex = 203 + pagePlus;
            rocketWBARecipe.preTech = LDB.techs.Select(1522); //垂直发射井科技
            if (MoreMegaStructure.isBattleActive) rocketWBARecipe.preTech = LDB.techs.Select(1921);
            Traverse.Create(rocketWBARecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconRocketWarpG);
            ProtoRegistry.RegisterItem(9490, "谐振发射器运载火箭".Translate(), "谐振发射器运载火箭描述".Translate(), "Assets/MegaStructureTab/rocketWarp", 203 + pagePlus, 20,
                EItemType.Product, ProtoRegistry.GetDefaultIconDesc(new Color(0.9f, 1f, 0.9f), new Color(0.2f, 0.7f, 0.2f)));
            //星际组装厂运载火箭
            var rocketIARecipe = oriRecipe2.Copy();
            var rocketIA = oriItem2.Copy();
            rocketIARecipe.ID = recipeIdBias + 541;
            rocketIARecipe.Name = "星际组装厂运载火箭";
            rocketIARecipe.name = "星际组装厂运载火箭".Translate();
            rocketIARecipe.Description = "星际组装厂运载火箭描述";
            rocketIARecipe.description = "星际组装厂运载火箭描述".Translate();
            rocketIARecipe.Items = new int[] { 9487, 1802 };
            rocketIARecipe.ItemCounts = new int[] { 2, 2 };
            rocketIARecipe.Results = new int[] { 9491 };
            rocketIARecipe.ResultCounts = new int[] { 1 };
            rocketIARecipe.TimeSpend = 480;
            rocketIARecipe.GridIndex = 204 + pagePlus;
            rocketIARecipe.preTech = LDB.techs.Select(1522); //垂直发射井科技
            if (MoreMegaStructure.isBattleActive) rocketIARecipe.preTech = LDB.techs.Select(1922);
            Traverse.Create(rocketIARecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconRocketMegaA);
            ProtoRegistry.RegisterItem(9491, "星际组装厂运载火箭".Translate(), "星际组装厂运载火箭描述".Translate(), "Assets/MegaStructureTab/rocketAssembly", 204 + pagePlus, 20,
                EItemType.Product, ProtoRegistry.GetDefaultIconDesc(new Color(0.9f, 0.9f, 1f), new Color(0.1f, 0.5f, 0.7f)));
            //晶体重构器运载火箭
            var rocketCRRecipe = oriRecipe2.Copy();
            var rocketCR = oriItem2.Copy();
            rocketCRRecipe.ID = recipeIdBias + 542;
            rocketCRRecipe.Name = "晶体重构器运载火箭";
            rocketCRRecipe.name = "晶体重构器运载火箭".Translate();
            rocketCRRecipe.Description = "晶体重构器运载火箭描述";
            rocketCRRecipe.description = "晶体重构器运载火箭描述".Translate();
            rocketCRRecipe.Items = new int[] { 9485, 1802, 1305 };
            rocketCRRecipe.ItemCounts = new int[] { 1, 2, 2 };
            rocketCRRecipe.Results = new int[] { 9492 };
            rocketCRRecipe.ResultCounts = new int[] { 1 };
            rocketCRRecipe.TimeSpend = 480;
            rocketCRRecipe.GridIndex = 205 + pagePlus;
            rocketCRRecipe.preTech = LDB.techs.Select(1522); //垂直发射井科技
            if (MoreMegaStructure.isBattleActive) rocketCRRecipe.preTech = LDB.techs.Select(1923);
            Traverse.Create(rocketCRRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconRocketCrysM);
            ProtoRegistry.RegisterItem(9492, "晶体重构器运载火箭".Translate(), "晶体重构器运载火箭描述".Translate(), "Assets/MegaStructureTab/rocketCrystal", 205 + pagePlus, 20,
                EItemType.Product, ProtoRegistry.GetDefaultIconDesc(new Color(1f, 0.9f, 1f), new Color(0.7f, 0.2f, 0.7f)));

            
            
            //多功能集成组件
            var oriItem4 = LDB.items.Select(1131); //地基
            var itemICRecipe = oriRecipe.Copy();
            var itemIC = oriItem4.Copy();
            itemICRecipe.ID = recipeIdBias + 550;
            itemICRecipe.Name = "多功能集成组件";
            itemICRecipe.name = "多功能集成组件".Translate();
            itemICRecipe.Description = "多功能集成组件描述";
            itemICRecipe.description = "多功能集成组件描述".Translate();
            itemICRecipe.Items = new int[] { 9500 };
            itemICRecipe.ItemCounts = new int[] { 1 };
            itemICRecipe.Results = new int[] { 9500 };
            itemICRecipe.ResultCounts = new int[] { 1 };
            itemICRecipe.GridIndex = 199 + pagePlus + linePlus;
            itemICRecipe.TimeSpend = 60;
            itemICRecipe.preTech = LDB.techs.Select(1203); //量子打印科技
            Traverse.Create(itemICRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconInterCompo);
            ProtoRegistry.RegisterItem(9500, "多功能集成组件".Translate(), "多功能集成组件描述".Translate(), "Assets/MegaStructureTab/integratedcomponents", 109 + pagePlus + linePlus, 1000,
                EItemType.Component, ProtoRegistry.GetDefaultIconDesc(Color.white, Color.white));

            //快速组装配方
            var oriRecipe5 = LDB.recipes.Select(47);
            //传送带 快速组装
            var quickBeltRecipe = oriRecipe5.Copy();
            quickBeltRecipe.ID = recipeIdBias + 551;
            quickBeltRecipe.Name = "传送带 快速组装";
            quickBeltRecipe.name = "传送带 快速组装".Translate();
            quickBeltRecipe.Description = "快速组装描述";
            quickBeltRecipe.description = "快速组装描述".Translate();
            quickBeltRecipe.Items = new int[] { 9500 };
            quickBeltRecipe.ItemCounts = new int[] { 1 };
            quickBeltRecipe.Results = new int[] { 2003 };
            quickBeltRecipe.ResultCounts = new int[] { 60 };
            quickBeltRecipe.GridIndex = 401 + pagePlus;
            quickBeltRecipe.TimeSpend = 6;
            Traverse.Create(quickBeltRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconQuickBelt);
            //分拣器 快速组装
            var quickSorterRecipe = oriRecipe5.Copy();
            quickSorterRecipe.ID = 552;
            quickSorterRecipe.Name = "分拣器 快速组装";
            quickSorterRecipe.name = "分拣器 快速组装".Translate();
            quickSorterRecipe.Description = "快速组装描述";
            quickSorterRecipe.description = "快速组装描述".Translate();
            quickSorterRecipe.Items = new int[] { 9500 };
            quickSorterRecipe.ItemCounts = new int[] { 1 };
            quickSorterRecipe.Results = new int[] { 2013 };
            quickSorterRecipe.ResultCounts = new int[] { 10 };
            quickSorterRecipe.GridIndex = 402 + pagePlus;
            quickSorterRecipe.TimeSpend = 6;
            Traverse.Create(quickSorterRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconQuickSorter);
            //配电站 快速组装
            var quickPowerRecipe = oriRecipe5.Copy();
            quickPowerRecipe.ID = 553;
            quickPowerRecipe.Name = "配电站 快速组装";
            quickPowerRecipe.name = "配电站 快速组装".Translate();
            quickPowerRecipe.Description = "快速组装描述";
            quickPowerRecipe.description = "快速组装描述".Translate();
            quickPowerRecipe.Items = new int[] { 9500 };
            quickPowerRecipe.ItemCounts = new int[] { 1 };
            quickPowerRecipe.Results = new int[] { 2212 };
            quickPowerRecipe.ResultCounts = new int[] { 5 };
            quickPowerRecipe.GridIndex = 403 + pagePlus;
            quickPowerRecipe.TimeSpend = 6;
            Traverse.Create(quickPowerRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconQuickPower);
            //制造台 快速组装
            var quickAssemblyRecipe = oriRecipe5.Copy();
            quickAssemblyRecipe.ID = 554;
            quickAssemblyRecipe.Name = "制造台 快速组装";
            quickAssemblyRecipe.name = "制造台 快速组装".Translate();
            quickAssemblyRecipe.Description = "快速组装描述";
            quickAssemblyRecipe.description = "快速组装描述".Translate();
            quickAssemblyRecipe.Items = new int[] { 9500 };
            quickAssemblyRecipe.ItemCounts = new int[] { 1 };
            quickAssemblyRecipe.Results = new int[] { 2305 };
            quickAssemblyRecipe.ResultCounts = new int[] { 1 };
            quickAssemblyRecipe.GridIndex = 404 + pagePlus;
            quickAssemblyRecipe.TimeSpend = 6;
            Traverse.Create(quickAssemblyRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconQuickAssembly);
            //位面熔炉 快速组装
            var quickSmelterRecipe = oriRecipe5.Copy();
            quickSmelterRecipe.ID = 555;
            quickSmelterRecipe.Name = "位面熔炉 快速组装";
            quickSmelterRecipe.name = "位面熔炉 快速组装".Translate();
            quickSmelterRecipe.Description = "快速组装描述";
            quickSmelterRecipe.description = "快速组装描述".Translate();
            quickSmelterRecipe.Items = new int[] { 9500 };
            quickSmelterRecipe.ItemCounts = new int[] { 1 };
            quickSmelterRecipe.Results = new int[] { 2315 };
            quickSmelterRecipe.ResultCounts = new int[] { 1 };
            quickSmelterRecipe.GridIndex = 405 + pagePlus;
            quickSmelterRecipe.TimeSpend = 6;
            Traverse.Create(quickSmelterRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconQuickSmelter);
            //化工厂 快速组装
            var quickChemicalRecipe = oriRecipe5.Copy();
            quickChemicalRecipe.ID = 556;
            quickChemicalRecipe.Name = "化工厂 快速组装";
            quickChemicalRecipe.name = "化工厂 快速组装".Translate();
            quickChemicalRecipe.Description = "快速组装描述";
            quickChemicalRecipe.description = "快速组装描述".Translate();
            quickChemicalRecipe.Items = new int[] { 9500 };
            quickChemicalRecipe.ItemCounts = new int[] { 1 };
            quickChemicalRecipe.Results = new int[] { 2309 };
            quickChemicalRecipe.ResultCounts = new int[] { 2 };
            quickChemicalRecipe.GridIndex = 406 + pagePlus;
            quickChemicalRecipe.TimeSpend = 6;
            Traverse.Create(quickChemicalRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconQuickChemical);
            //精炼厂 快速组装
            var quickRefineryRecipe = oriRecipe5.Copy();
            quickRefineryRecipe.ID = 557;
            quickRefineryRecipe.Name = "精炼厂 快速组装";
            quickRefineryRecipe.name = "精炼厂 快速组装".Translate();
            quickRefineryRecipe.Description = "快速组装描述";
            quickRefineryRecipe.description = "快速组装描述".Translate();
            quickRefineryRecipe.Items = new int[] { 9500 };
            quickRefineryRecipe.ItemCounts = new int[] { 1 };
            quickRefineryRecipe.Results = new int[] { 2308 };
            quickRefineryRecipe.ResultCounts = new int[] { 2 };
            quickRefineryRecipe.GridIndex = 407 + pagePlus;
            quickRefineryRecipe.TimeSpend = 6;
            Traverse.Create(quickRefineryRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconQuickRefinery);
            //对撞机 快速组装
            var quickColliderRecipe = oriRecipe5.Copy();
            quickColliderRecipe.ID = 558;
            quickColliderRecipe.Name = "对撞机 快速组装";
            quickColliderRecipe.name = "对撞机 快速组装".Translate();
            quickColliderRecipe.Description = "快速组装描述";
            quickColliderRecipe.description = "快速组装描述".Translate();
            quickColliderRecipe.Items = new int[] { 9500 };
            quickColliderRecipe.ItemCounts = new int[] { 2 };
            quickColliderRecipe.Results = new int[] { 2310 };
            quickColliderRecipe.ResultCounts = new int[] { 1 };
            quickColliderRecipe.GridIndex = 408 + pagePlus;
            quickColliderRecipe.TimeSpend = 6;
            Traverse.Create(quickColliderRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconQuickCollider);
            //研究站 快速组装
            var quickLabRecipe = oriRecipe5.Copy();
            quickLabRecipe.ID = 559;
            quickLabRecipe.Name = "研究站 快速组装";
            quickLabRecipe.name = "研究站 快速组装".Translate();
            quickLabRecipe.Description = "快速组装描述";
            quickLabRecipe.description = "快速组装描述".Translate();
            quickLabRecipe.Items = new int[] { 9500 };
            quickLabRecipe.ItemCounts = new int[] { 1 };
            quickLabRecipe.Results = new int[] { 2901 };
            quickLabRecipe.ResultCounts = new int[] { 2 };
            quickLabRecipe.GridIndex = 409 + pagePlus;
            quickLabRecipe.TimeSpend = 6;
            Traverse.Create(quickLabRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconQuickLab);
            //人造恒星 快速组装
            var quickReactorRecipe = oriRecipe5.Copy();
            quickReactorRecipe.ID = 560;
            quickReactorRecipe.Name = "人造恒星 快速组装";
            quickReactorRecipe.name = "人造恒星 快速组装".Translate();
            quickReactorRecipe.Description = "快速组装描述";
            quickReactorRecipe.description = "快速组装描述".Translate();
            quickReactorRecipe.Items = new int[] { 9500 };
            quickReactorRecipe.ItemCounts = new int[] { 5 };
            quickReactorRecipe.Results = new int[] { 2210 };
            quickReactorRecipe.ResultCounts = new int[] { 1 };
            quickReactorRecipe.GridIndex = 410 + pagePlus;
            quickReactorRecipe.TimeSpend = 6;
            quickReactorRecipe.preTech = LDB.techs.Select(1144);
            Traverse.Create(quickReactorRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconQuickReactor);
            //行星内物流 快速组装
            var quickPLogRecipe = oriRecipe5.Copy();
            quickPLogRecipe.ID = 561;
            quickPLogRecipe.Name = "行星内物流 快速组装";
            quickPLogRecipe.name = "行星内物流 快速组装".Translate();
            quickPLogRecipe.Description = "快速组装描述";
            quickPLogRecipe.description = "快速组装描述".Translate();
            quickPLogRecipe.Items = new int[] { 9500 };
            quickPLogRecipe.ItemCounts = new int[] { 3 };
            quickPLogRecipe.Results = new int[] { 2103, 5001 };
            quickPLogRecipe.ResultCounts = new int[] { 1, 30 };
            quickPLogRecipe.GridIndex = 411 + pagePlus;
            quickPLogRecipe.TimeSpend = 6;
            quickPLogRecipe.preTech = LDB.techs.Select(1604);
            Traverse.Create(quickPLogRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconQuickPLog);
            //星际物流 快速组装
            var quickILogRecipe = oriRecipe5.Copy();
            quickILogRecipe.ID = 562;
            quickILogRecipe.Name = "星际物流 快速组装";
            quickILogRecipe.name = "星际物流 快速组装".Translate();
            quickILogRecipe.Description = "快速组装描述";
            quickILogRecipe.description = "快速组装描述".Translate();
            quickILogRecipe.Items = new int[] { 9500 };
            quickILogRecipe.ItemCounts = new int[] { 5 };
            quickILogRecipe.Results = new int[] { 2104, 5002 };
            quickILogRecipe.ResultCounts = new int[] { 1, 10 };
            quickILogRecipe.GridIndex = 412 + pagePlus;
            quickILogRecipe.TimeSpend = 6;
            quickILogRecipe.preTech = LDB.techs.Select(1605);
            Traverse.Create(quickILogRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconQuickILog);

            //itemGravityGen.makes = new List<RecipeProto> { itemGravityDrillRecipe, rocketWBARecipe };
            //itemConstrainRing.makes = new List<RecipeProto> { itemGravityDrillRecipe, rocketSNRecipe};
            //itemGravityDrill.makes = new List<RecipeProto> {rocketMDRecipe };
            //itemExciter.makes = new List<RecipeProto> {itemDiscRecipe, itemQuanCompRecipe };
            //itemDisc.makes = new List<RecipeProto> {rocketMDRecipe, rocketWBARecipe };
            //itemProbe.makes = new List<RecipeProto> { rocketCRRecipe };
            //itemQuanComp.makes = new List<RecipeProto> { rocketSNRecipe };
            //itemIACompo.makes = new List<RecipeProto> { rocketIARecipe };
            //itemIC.makes = new List<RecipeProto> { quickAssemblyRecipe, quickBeltRecipe, quickChemicalRecipe, quickColliderRecipe, quickILogRecipe, quickLabRecipe, quickPLogRecipe, quickPowerRecipe, quickReactorRecipe, quickRefineryRecipe, quickSmelterRecipe, quickSorterRecipe };

            LDB.items.Select(2003).recipes.Add(quickBeltRecipe);
            LDB.items.Select(2013).recipes.Add(quickSorterRecipe);
            LDB.items.Select(2305).recipes.Add(quickAssemblyRecipe);
            LDB.items.Select(2315).recipes.Add(quickSmelterRecipe);
            LDB.items.Select(2309).recipes.Add(quickChemicalRecipe);
            LDB.items.Select(2212).recipes.Add(quickPowerRecipe);
            LDB.items.Select(2308).recipes.Add(quickRefineryRecipe);
            LDB.items.Select(2310).recipes.Add(quickColliderRecipe);
            LDB.items.Select(2901).recipes.Add(quickLabRecipe);
            LDB.items.Select(2210).recipes.Add(quickReactorRecipe);
            LDB.items.Select(2103).recipes.Add(quickPLogRecipe);
            LDB.items.Select(5001).recipes.Add(quickPLogRecipe);
            LDB.items.Select(2104).recipes.Add(quickILogRecipe);
            LDB.items.Select(5002).recipes.Add(quickILogRecipe);

            //LDBTool.PostAddProto(itemGravityGen);
            LDBTool.PreAddProto(itemGravityGenRecipe);
            //LDBTool.PreAddProto(itemConstrainRing);
            LDBTool.PreAddProto(itemConstrainRingRecipe);
            //LDBTool.PreAddProto(itemGravityDrill);
            LDBTool.PreAddProto(itemGravityDrillRecipe);
            //LDBTool.PreAddProto(itemExciter);
            LDBTool.PreAddProto(itemExciterRecipe);
            //LDBTool.PreAddProto(itemDisc);
            LDBTool.PreAddProto(itemDiscRecipe);
            //LDBTool.PreAddProto(itemProbe);
            LDBTool.PreAddProto(itemProbeRecipe);
            //LDBTool.PreAddProto(itemQuanComp);
            LDBTool.PreAddProto(itemQuanCompRecipe);
            //LDBTool.PreAddProto(itemIACompo);
            LDBTool.PreAddProto(itemIACompoRecipe);
            //LDBTool.PreAddProto(itemIC);
            LDBTool.PreAddProto(itemICRecipe);

            //LDBTool.PreAddProto(rocketMD);
            LDBTool.PreAddProto(rocketMDRecipe);
            //LDBTool.PreAddProto(rocketSN);
            LDBTool.PreAddProto(rocketSNRecipe);
            //LDBTool.PreAddProto(rocketWBA);
            LDBTool.PreAddProto(rocketWBARecipe);
            //LDBTool.PreAddProto(rocketIA);
            LDBTool.PreAddProto(rocketIARecipe);
            //LDBTool.PreAddProto(rocketCR);
            LDBTool.PreAddProto(rocketCRRecipe);

            //LDBTool.PostAddProto(ReceiverIron);
            //LDBTool.PostAddProto(ReceiverIronRecipe);
            //LDBTool.PostAddProto(ReceiverCopper);
            //LDBTool.PostAddProto(ReceiverCopperRecipe);
            //LDBTool.PostAddProto(ReceiverSilicon);
            //LDBTool.PostAddProto(ReceiverSiliconRecipe);
            //LDBTool.PostAddProto(ReceiverTitanium);
            //LDBTool.PostAddProto(ReceiverTitaniumRecipe);
            //LDBTool.PostAddProto(ReceiverMagore);
            //LDBTool.PostAddProto(ReceiverMagoreRecipe);
            //LDBTool.PostAddProto(ReceiverCasimir);
            //LDBTool.PostAddProto(ReceiverCasimirRecipe);
            //LDBTool.PostAddProto(ReceiverIC);
            //LDBTool.PostAddProto(ReceiverICRecipe);
            //LDBTool.PostAddProto(ReceiverCoal);
            //LDBTool.PostAddProto(ReceiverCoalRecipe);
            //LDBTool.PostAddProto(ReceiverGrating);
            //LDBTool.PostAddProto(ReceiverGratingRecipe);

            LDBTool.PostAddProto(quickAssemblyRecipe);
            LDBTool.PostAddProto(quickBeltRecipe);
            LDBTool.PostAddProto(quickChemicalRecipe);
            LDBTool.PostAddProto(quickColliderRecipe);
            LDBTool.PostAddProto(quickILogRecipe);
            LDBTool.PostAddProto(quickLabRecipe);
            LDBTool.PostAddProto(quickPLogRecipe);
            LDBTool.PostAddProto(quickPowerRecipe);
            LDBTool.PostAddProto(quickReactorRecipe);
            LDBTool.PostAddProto(quickRefineryRecipe);
            LDBTool.PostAddProto(quickSmelterRecipe);
            LDBTool.PostAddProto(quickSorterRecipe);

            if (true)
            {
                LDBTool.SetBuildBar(8, 5, 9493);
                LDBTool.SetBuildBar(8, 6, 9494);
                LDBTool.SetBuildBar(8, 7, 9495);
                LDBTool.SetBuildBar(8, 8, 9496);
                LDBTool.SetBuildBar(8, 9, 9497);
                LDBTool.SetBuildBar(8, 10, 9498);
                LDBTool.SetBuildBar(4, 4, 9499);
            }
        }

        public static void AddNewItems2()
        {
            if (!MoreMegaStructure.isBattleActive)
                return;

            int pagePlus = MoreMegaStructure.battlePagenum * 1000;
            //itemId 9503 available
            //recipeId 565 available

            ProtoRegistry.RegisterItem(9503, "力场发生器", "力场发生器描述", "Assets/MegaStructureTab/forceGen", 201 + pagePlus, 20, EItemType.Component,
                ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.7f, 0.7f)));
            ProtoRegistry.RegisterItem(9504, "复合态晶体", "复合态晶体描述", "Assets/MegaStructureTab/compoCrystal", 202 + pagePlus, 100, EItemType.Component,
                ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.7f, 0.7f)));
            ProtoRegistry.RegisterItem(9505, "电磁力抑制器", "电磁力抑制器描述", "Assets/MegaStructureTab/elemaginhibitor2", 203 + pagePlus, 50, EItemType.Component,
                ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.7f, 0.7f)));
            ProtoRegistry.RegisterItem(9506, "胶子发生器", "胶子发生器描述", "Assets/MegaStructureTab/gluonGen", 204 + pagePlus, 50, EItemType.Component,
                ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.7f, 0.7f)));
            ProtoRegistry.RegisterItem(9507, "强力过载装置", "强力过载装置描述", "Assets/MegaStructureTab/strIntOverloader", 205 + pagePlus, 20, EItemType.Component,
                ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.7f, 0.7f)));
            ProtoRegistry.RegisterItem(9508, "导流框架", "导流框架描述", "Assets/MegaStructureTab/starcannonframe", 206 + pagePlus, 20, EItemType.Component,
                ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.7f, 0.7f)));
            ProtoRegistry.RegisterItem(9509, "恒星炮组件", "恒星炮组件描述", "Assets/MegaStructureTab/starcannoncompo", 207 + pagePlus, 20, EItemType.Component,
                ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.3f, 0.3f, 0.9f)));
            ProtoRegistry.RegisterItem(9510, "恒星炮运载火箭", "恒星炮运载火箭描述", "Assets/MegaStructureTab/rocketStarcannon", 306 + pagePlus, 20, EItemType.Product,
                ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.3f, 0.9f, 0.9f)));

            ItemProto dropletItem = ProtoRegistry.RegisterItem(9511, "水滴gm", "水滴描述gm", "Assets/MegaStructureTab/drop1", 707 + pagePlus, 1, EItemType.Product,
                ProtoRegistry.GetDefaultIconDesc(Color.white, Color.white));
            dropletItem.DescFields = new int[] { 50, 51, 56, 54, 55, 1 };

            ProtoRegistry.RegisterRecipe(565, ERecipeType.Assemble, 240, new int[] { 9480, 9484 }, new int[] { 2, 2 }, new int[] { 9503 }, new int[] { 1 }, "力场发生器描述", 1916, 201 + pagePlus, "Assets/MegaStructureTab/forceGen");
            RecipeProto SIMRecipe = ProtoRegistry.RegisterRecipe(566, ERecipeType.Particle, 600, new int[] { 1014, 1126, 1124, 1118, 1120 }, new int[] { 1, 1, 1, 1, 1 }, new int[] { 9504 }, new int[] { 1 }, "复合态晶体描述", 1919, 202 + pagePlus, "Assets/MegaStructureTab/compoCrystal");
            ProtoRegistry.RegisterRecipe(567, ERecipeType.Assemble, 360, new int[] { 1305, 1205 }, new int[] { 1, 2 }, new int[] { 9505 }, new int[] { 1 }, "电磁力抑制器描述", 1919, 203 + pagePlus, "Assets/MegaStructureTab/elemaginhibitor2");
            RecipeProto gluonGenRecipe = ProtoRegistry.RegisterRecipe(568, ERecipeType.Assemble, 360, new int[] { 9483, 1402, 1122 }, new int[] { 1, 1, 2 }, new int[] { 9506 }, new int[] { 1 }, "胶子发生器描述", 1919, 204 + pagePlus, "Assets/MegaStructureTab/gluonGen");
            RecipeProto strIntOverloaderRecipe = ProtoRegistry.RegisterRecipe(569, ERecipeType.Assemble, 1200, new int[] { 9506, 9486 }, new int[] { 2, 2 }, new int[] { 9507 }, new int[] { 1 }, "强力过载装置描述", 1919, 205 + pagePlus, "Assets/MegaStructureTab/strIntOverloader");
            ProtoRegistry.RegisterRecipe(570, ERecipeType.Assemble, 180, new int[] { 1125, 9481 }, new int[] { 3, 2 }, new int[] { 9508 }, new int[] { 1 }, "导流框架描述", 1918, 206 + pagePlus, "Assets/MegaStructureTab/starcannonframe");
            ProtoRegistry.RegisterRecipe(571, ERecipeType.Assemble, 480, new int[] { 1209, 9508, 9484 }, new int[] { 3, 2, 1 }, new int[] { 9509 }, new int[] { 1 }, "恒星炮组件描述", 1918, 207 + pagePlus, "Assets/MegaStructureTab/starcannoncompo");
            ProtoRegistry.RegisterRecipe(572, ERecipeType.Assemble, 360, new int[] { 9509, 1802, 1305 }, new int[] { 2, 2, 2 }, new int[] { 9510 }, new int[] { 1 }, "恒星炮运载火箭描述", 1918, 306 + pagePlus, "Assets/MegaStructureTab/rocketStarcannon");
            RecipeProto dropRecipe = ProtoRegistry.RegisterRecipe(573, ERecipeType.Assemble, 3600, new int[] { 9505, 9507, 9504 }, new int[] { 20, 20, 100 }, new int[] { 9511 }, new int[] { 1 }, "水滴描述", 1919, 707 + pagePlus, "Assets/MegaStructureTab/drop1");
            SIMRecipe.Handcraft = false;
            gluonGenRecipe.Handcraft = false;
            strIntOverloaderRecipe.Handcraft = false;
            dropRecipe.Handcraft = false;
        }

        public static void AddReceivers()
        {
            int pagePlus = MoreMegaStructure.pagenum * 1000;
            if (MoreMegaStructure.isBattleActive)
            {
                pagePlus = MoreMegaStructure.battlePagenum * 1000 + 100;
            }
            int recipeIdBias = 0;
            if (MoreMegaStructure.GenesisCompatibility)
            {
                recipeIdBias = -200;
            }

            //下面是接收器
            var oriRecipe3 = LDB.recipes.Select(41);
            var oriItem3 = LDB.items.Select(2208);
            //Iron
            var ReceiverIronRecipe = oriRecipe3.Copy();
            var ReceiverIron = oriItem3.Copy();
            ReceiverIronRecipe.ID = recipeIdBias + 543;
            ReceiverIronRecipe.Name = "铁金属重构装置";
            ReceiverIronRecipe.name = "铁金属重构装置".Translate();
            ReceiverIronRecipe.Description = "接收重构装置描述";
            ReceiverIronRecipe.description = "接收重构装置描述".Translate();
            ReceiverIronRecipe.Items = new int[] { 1103, 1404, 1303, 9481 };
            ReceiverIronRecipe.ItemCounts = new int[] { 20, 10, 5, 2 };
            if(MoreMegaStructure.GenesisCompatibility)
            {
                ReceiverIronRecipe.Items = new int[] { 1103, 1014, 1303, 9481 };
                ReceiverIronRecipe.ItemCounts = new int[] { 20, 20, 5, 2 };
            }
            ReceiverIronRecipe.Results = new int[] { 9493 };
            ReceiverIronRecipe.ResultCounts = new int[] { 1 };
            ReceiverIronRecipe.GridIndex = 301 + pagePlus;
            ReceiverIronRecipe.TimeSpend = 480;
            Traverse.Create(ReceiverIronRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverIron);
            ReceiverIronRecipe.preTech = LDB.techs.Select(1504); //射线接收站科技
            ReceiverIron.ID = 9493;
            ReceiverIron.Name = "铁金属重构装置";
            ReceiverIron.name = "铁金属重构装置".Translate();
            ReceiverIron.Description = "接收重构装置描述";
            ReceiverIron.description = "接收重构装置描述".Translate();
            ReceiverIron.GridIndex = 301 + pagePlus;
            ReceiverIron.HeatValue = 0L;
            ReceiverIron.prefabDesc = oriItem3.prefabDesc.Copy();
            ReceiverIron.prefabDesc.powerProductHeat = 6000000;
            ReceiverIron.prefabDesc.powerProductId = 1101;
            ReceiverIron.handcraft = ReceiverIronRecipe;
            ReceiverIron.handcrafts = new List<RecipeProto> { ReceiverIronRecipe };
            ReceiverIron.maincraft = ReceiverIronRecipe;
            ReceiverIron.recipes = new List<RecipeProto> { ReceiverIronRecipe };
            //ReceiverIron.makes = new List<RecipeProto> { ReceiverIronRecipe };
            Traverse.Create(ReceiverIron).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverIron);
            //Copper
            var ReceiverCopperRecipe = oriRecipe3.Copy();
            var ReceiverCopper = oriItem3.Copy();
            ReceiverCopperRecipe.ID = recipeIdBias + 544;
            ReceiverCopperRecipe.Name = "铜金属重构装置";
            ReceiverCopperRecipe.name = "铜金属重构装置".Translate();
            ReceiverCopperRecipe.Description = "接收重构装置描述";
            ReceiverCopperRecipe.description = "接收重构装置描述".Translate();
            ReceiverCopperRecipe.Items = new int[] { 1103, 1404, 1303, 9481 };
            ReceiverCopperRecipe.ItemCounts = new int[] { 20, 10, 5, 2 };
            if (MoreMegaStructure.GenesisCompatibility)
            {
                ReceiverCopperRecipe.Items = new int[] { 1103, 1014, 1303, 9481 };
                ReceiverCopperRecipe.ItemCounts = new int[] { 20, 20, 5, 2 };
            }
            ReceiverCopperRecipe.Results = new int[] { 9494 };
            ReceiverCopperRecipe.ResultCounts = new int[] { 1 };
            ReceiverCopperRecipe.GridIndex = 302 + pagePlus;
            ReceiverCopperRecipe.TimeSpend = 480;
            Traverse.Create(ReceiverCopperRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverCopper);
            ReceiverCopperRecipe.preTech = LDB.techs.Select(1504); //射线接收站科技
            ReceiverCopper.ID = 9494;
            ReceiverCopper.Name = "铜金属重构装置";
            ReceiverCopper.name = "铜金属重构装置".Translate();
            ReceiverCopper.Description = "接收重构装置描述";
            ReceiverCopper.description = "接收重构装置描述".Translate();
            ReceiverCopper.GridIndex = 302 + pagePlus;
            ReceiverCopper.HeatValue = 0L;
            ReceiverCopper.prefabDesc = oriItem3.prefabDesc.Copy();
            ReceiverCopper.prefabDesc.powerProductHeat = 6000000;
            ReceiverCopper.prefabDesc.powerProductId = 1104;
            ReceiverCopper.handcraft = ReceiverCopperRecipe;
            ReceiverCopper.handcrafts = new List<RecipeProto> { ReceiverCopperRecipe };
            ReceiverCopper.maincraft = ReceiverCopperRecipe;
            ReceiverCopper.recipes = new List<RecipeProto> { ReceiverCopperRecipe };
            //ReceiverCopper.makes = new List<RecipeProto> { ReceiverCopperRecipe };
            Traverse.Create(ReceiverCopper).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverCopper);
            //Silicon
            var ReceiverSiliconRecipe = oriRecipe3.Copy();
            var ReceiverSilicon = oriItem3.Copy();
            ReceiverSiliconRecipe.ID = recipeIdBias + 545;
            ReceiverSiliconRecipe.Name = "高纯硅重构装置";
            ReceiverSiliconRecipe.name = "高纯硅重构装置".Translate();
            ReceiverSiliconRecipe.Description = "接收重构装置描述";
            ReceiverSiliconRecipe.description = "接收重构装置描述".Translate();
            ReceiverSiliconRecipe.Items = new int[] { 1103, 1404, 1303, 9481 };
            ReceiverSiliconRecipe.ItemCounts = new int[] { 20, 10, 5, 2 };
            if (MoreMegaStructure.GenesisCompatibility)
            {
                ReceiverSiliconRecipe.Items = new int[] { 1103, 1014, 1303, 9481 };
                ReceiverSiliconRecipe.ItemCounts = new int[] { 20, 20, 5, 2 };
            }
            ReceiverSiliconRecipe.Results = new int[] { 9495 };
            ReceiverSiliconRecipe.ResultCounts = new int[] { 1 };
            ReceiverSiliconRecipe.GridIndex = 303 + pagePlus;
            ReceiverSiliconRecipe.TimeSpend = 480;
            Traverse.Create(ReceiverSiliconRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverSilicon);
            ReceiverSiliconRecipe.preTech = LDB.techs.Select(1504); //射线接收站科技
            ReceiverSilicon.ID = 9495;
            ReceiverSilicon.Name = "高纯硅重构装置";
            ReceiverSilicon.name = "高纯硅重构装置".Translate();
            ReceiverSilicon.Description = "接收重构装置描述";
            ReceiverSilicon.description = "接收重构装置描述".Translate();
            ReceiverSilicon.GridIndex = 303 + pagePlus;
            ReceiverSilicon.HeatValue = 0L;
            ReceiverSilicon.prefabDesc = oriItem3.prefabDesc.Copy();
            ReceiverSilicon.prefabDesc.powerProductHeat = 6000000;
            ReceiverSilicon.prefabDesc.powerProductId = 1105;
            ReceiverSilicon.handcraft = ReceiverSiliconRecipe;
            ReceiverSilicon.handcrafts = new List<RecipeProto> { ReceiverSiliconRecipe };
            ReceiverSilicon.maincraft = ReceiverSiliconRecipe;
            ReceiverSilicon.recipes = new List<RecipeProto> { ReceiverSiliconRecipe };
            //ReceiverSilicon.makes = new List<RecipeProto> { ReceiverSiliconRecipe };
            Traverse.Create(ReceiverSilicon).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverSilicon);
            //Titanium
            var ReceiverTitaniumRecipe = oriRecipe3.Copy();
            var ReceiverTitanium = oriItem3.Copy();
            ReceiverTitaniumRecipe.ID = recipeIdBias + 546;
            ReceiverTitaniumRecipe.Name = "钛金属重构装置";
            ReceiverTitaniumRecipe.name = "钛金属重构装置".Translate();
            ReceiverTitaniumRecipe.Description = "接收重构装置描述";
            ReceiverTitaniumRecipe.description = "接收重构装置描述".Translate();
            ReceiverTitaniumRecipe.Items = new int[] { 1103, 1404, 1303, 9481 };
            ReceiverTitaniumRecipe.ItemCounts = new int[] { 20, 10, 5, 2 };
            if (MoreMegaStructure.GenesisCompatibility)
            {
                ReceiverTitaniumRecipe.Items = new int[] { 1103, 1014, 1303, 9481 };
                ReceiverTitaniumRecipe.ItemCounts = new int[] { 20, 20, 5, 2 };
            }
            ReceiverTitaniumRecipe.Results = new int[] { 9496 };
            ReceiverTitaniumRecipe.ResultCounts = new int[] { 1 };
            ReceiverTitaniumRecipe.GridIndex = 304 + pagePlus;
            ReceiverTitaniumRecipe.TimeSpend = 480;
            Traverse.Create(ReceiverTitaniumRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverTitanium);
            ReceiverTitaniumRecipe.preTech = LDB.techs.Select(1504); //射线接收站科技
            ReceiverTitanium.ID = 9496;
            ReceiverTitanium.Name = "钛金属重构装置";
            ReceiverTitanium.name = "钛金属重构装置".Translate();
            ReceiverTitanium.Description = "接收重构装置描述";
            ReceiverTitanium.description = "接收重构装置描述".Translate();
            ReceiverTitanium.GridIndex = 304 + pagePlus;
            ReceiverTitanium.HeatValue = 0L;
            ReceiverTitanium.prefabDesc = oriItem3.prefabDesc.Copy();
            ReceiverTitanium.prefabDesc.powerProductHeat = 6000000;
            ReceiverTitanium.prefabDesc.powerProductId = 1106;
            ReceiverTitanium.handcraft = ReceiverTitaniumRecipe;
            ReceiverTitanium.handcrafts = new List<RecipeProto> { ReceiverTitaniumRecipe };
            ReceiverTitanium.maincraft = ReceiverTitaniumRecipe;
            ReceiverTitanium.recipes = new List<RecipeProto> { ReceiverTitaniumRecipe };
            //ReceiverTitanium.makes = new List<RecipeProto> { ReceiverTitaniumRecipe };
            Traverse.Create(ReceiverTitanium).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverTitanium);
            //Magore
            var ReceiverMagoreRecipe = oriRecipe3.Copy();
            var ReceiverMagore = oriItem3.Copy();
            ReceiverMagoreRecipe.ID = recipeIdBias + 547;
            ReceiverMagoreRecipe.Name = "单极磁石重构装置";
            ReceiverMagoreRecipe.name = "单极磁石重构装置".Translate();
            ReceiverMagoreRecipe.Description = "接收重构装置描述";
            ReceiverMagoreRecipe.description = "接收重构装置描述".Translate();
            ReceiverMagoreRecipe.Items = new int[] { 1103, 1404, 1303, 9481 };
            ReceiverMagoreRecipe.ItemCounts = new int[] { 20, 10, 5, 2 };
            if (MoreMegaStructure.GenesisCompatibility)
            {
                ReceiverMagoreRecipe.Items = new int[] { 1103, 1014, 1303, 9481 };
                ReceiverMagoreRecipe.ItemCounts = new int[] { 20, 20, 5, 2 };
            }
            ReceiverMagoreRecipe.Results = new int[] { 9497 };
            ReceiverMagoreRecipe.ResultCounts = new int[] { 1 };
            ReceiverMagoreRecipe.GridIndex = 305 + pagePlus;
            ReceiverMagoreRecipe.TimeSpend = 480;
            Traverse.Create(ReceiverMagoreRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverMagore);
            ReceiverMagoreRecipe.preTech = LDB.techs.Select(1504); //射线接收站科技
            ReceiverMagore.ID = 9497;
            ReceiverMagore.Name = "单极磁石重构装置";
            ReceiverMagore.name = "单极磁石重构装置".Translate();
            ReceiverMagore.Description = "接收重构装置描述";
            ReceiverMagore.description = "接收重构装置描述".Translate();
            ReceiverMagore.GridIndex = 305 + pagePlus;
            ReceiverMagore.HeatValue = 0L;
            ReceiverMagore.prefabDesc = oriItem3.prefabDesc.Copy();
            ReceiverMagore.prefabDesc.powerProductHeat = 6000000;
            ReceiverMagore.prefabDesc.powerProductId = 1016;
            ReceiverMagore.handcraft = ReceiverMagoreRecipe;
            ReceiverMagore.handcrafts = new List<RecipeProto> { ReceiverMagoreRecipe };
            ReceiverMagore.maincraft = ReceiverMagoreRecipe;
            ReceiverMagore.recipes = new List<RecipeProto> { ReceiverMagoreRecipe };
            //ReceiverMagore.makes = new List<RecipeProto> { ReceiverMagoreRecipe };
            Traverse.Create(ReceiverMagore).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverMagore);
            //Coal
            var ReceiverCoalRecipe = oriRecipe3.Copy();
            var ReceiverCoal = oriItem3.Copy();
            ReceiverCoalRecipe.ID = recipeIdBias + 563;
            ReceiverCoalRecipe.Name = "石墨提炼装置";
            ReceiverCoalRecipe.name = "石墨提炼装置".Translate();
            ReceiverCoalRecipe.Description = "接收重构装置描述";
            ReceiverCoalRecipe.description = "接收重构装置描述".Translate();
            ReceiverCoalRecipe.Items = new int[] { 1103, 1404, 1303, 9481 };
            ReceiverCoalRecipe.ItemCounts = new int[] { 20, 10, 5, 2 };
            if (MoreMegaStructure.GenesisCompatibility)
            {
                ReceiverCoalRecipe.Items = new int[] { 1103, 1014, 1303, 9481 };
                ReceiverCoalRecipe.ItemCounts = new int[] { 20, 20, 5, 2 };
            }
            ReceiverCoalRecipe.Results = new int[] { 9501 };
            ReceiverCoalRecipe.ResultCounts = new int[] { 1 };
            ReceiverCoalRecipe.GridIndex = 306 + pagePlus;
            ReceiverCoalRecipe.TimeSpend = 480;
            Traverse.Create(ReceiverCoalRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverCoal);
            ReceiverCoalRecipe.preTech = LDB.techs.Select(1504); //射线接收站科技
            ReceiverCoal.ID = 9501;
            ReceiverCoal.Name = "石墨提炼装置";
            ReceiverCoal.name = "石墨提炼装置".Translate();
            ReceiverCoal.Description = "接收重构装置描述";
            ReceiverCoal.description = "接收重构装置描述".Translate();
            ReceiverCoal.GridIndex = 306 + pagePlus;
            ReceiverCoal.HeatValue = 0L;
            ReceiverCoal.prefabDesc = oriItem3.prefabDesc.Copy();
            ReceiverCoal.prefabDesc.powerProductHeat = 12000000;
            ReceiverCoal.prefabDesc.powerProductId = 1109;
            ReceiverCoal.handcraft = ReceiverCoalRecipe;
            ReceiverCoal.handcrafts = new List<RecipeProto> { ReceiverCoalRecipe };
            ReceiverCoal.maincraft = ReceiverCoalRecipe;
            ReceiverCoal.recipes = new List<RecipeProto> { ReceiverCoalRecipe };
            //ReceiverCoal.makes = new List<RecipeProto> { ReceiverCoalRecipe };
            Traverse.Create(ReceiverCoal).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverCoal);
            //Casimir
            var ReceiverCasimirRecipe = oriRecipe3.Copy();
            var ReceiverCasimir = oriItem3.Copy();
            ReceiverCasimirRecipe.ID = recipeIdBias + 548;
            ReceiverCasimirRecipe.Name = "晶体接收器";
            ReceiverCasimirRecipe.name = "晶体接收器".Translate();
            ReceiverCasimirRecipe.Description = "晶体接收器描述";
            ReceiverCasimirRecipe.description = "晶体接收器描述".Translate();
            ReceiverCasimirRecipe.Items = new int[] { 1107, 1404, 1303, 1206 };
            ReceiverCasimirRecipe.ItemCounts = new int[] { 10, 10, 5, 5 };
            if (MoreMegaStructure.GenesisCompatibility)
            {
                ReceiverCasimirRecipe.Items = new int[] { 1107, 1014, 1303 };
                ReceiverCasimirRecipe.ItemCounts = new int[] { 10, 20, 10 };
            }
            ReceiverCasimirRecipe.Results = new int[] { 9498 };
            ReceiverCasimirRecipe.ResultCounts = new int[] { 1 };
            ReceiverCasimirRecipe.GridIndex = 307 + pagePlus;
            ReceiverCasimirRecipe.TimeSpend = 480;
            Traverse.Create(ReceiverCasimirRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverCasimir);
            ReceiverCasimirRecipe.preTech = LDB.techs.Select(1504); //射线接收站科技
            ReceiverCasimir.ID = 9498;
            ReceiverCasimir.Name = "晶体接收器";
            ReceiverCasimir.name = "晶体接收器".Translate();
            ReceiverCasimir.Description = "晶体接收器描述";
            ReceiverCasimir.description = "晶体接收器描述".Translate();
            ReceiverCasimir.GridIndex = 307 + pagePlus;
            ReceiverCasimir.HeatValue = 0L;
            ReceiverCasimir.prefabDesc = oriItem3.prefabDesc.Copy();
            ReceiverCasimir.prefabDesc.powerProductHeat = 120000000;
            ReceiverCasimir.prefabDesc.powerProductId = 1126;
            ReceiverCasimir.handcraft = ReceiverCasimirRecipe;
            ReceiverCasimir.handcrafts = new List<RecipeProto> { ReceiverCasimirRecipe };
            ReceiverCasimir.maincraft = ReceiverCasimirRecipe;
            ReceiverCasimir.recipes = new List<RecipeProto> { ReceiverCasimirRecipe };
            //ReceiverCasimir.makes = new List<RecipeProto> { ReceiverCasimirRecipe };
            Traverse.Create(ReceiverCasimir).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverCasimir);
            //Grating
            var ReceiverGratingRecipe = oriRecipe3.Copy();
            var ReceiverGrating = oriItem3.Copy();
            ReceiverGratingRecipe.ID = recipeIdBias + 564;
            ReceiverGratingRecipe.Name = "光栅晶体接收器";
            ReceiverGratingRecipe.name = "光栅晶体接收器".Translate();
            ReceiverGratingRecipe.Description = "晶体接收器描述";
            ReceiverGratingRecipe.description = "晶体接收器描述".Translate();
            ReceiverGratingRecipe.Items = new int[] { 1107, 1404, 1303, 1206 };
            ReceiverGratingRecipe.ItemCounts = new int[] { 10, 10, 5, 5 };
            if (MoreMegaStructure.GenesisCompatibility)
            {
                ReceiverGratingRecipe.Items = new int[] { 1107, 1014, 1303 };
                ReceiverGratingRecipe.ItemCounts = new int[] { 10, 20, 10 };
            }
            ReceiverGratingRecipe.Results = new int[] { 9502 };
            ReceiverGratingRecipe.ResultCounts = new int[] { 1 };
            ReceiverGratingRecipe.GridIndex = 308 + pagePlus;
            ReceiverGratingRecipe.TimeSpend = 480;
            Traverse.Create(ReceiverGratingRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverGrating);
            ReceiverGratingRecipe.preTech = LDB.techs.Select(1504); //射线接收站科技
            ReceiverGrating.ID = 9502;
            ReceiverGrating.Name = "光栅晶体接收器";
            ReceiverGrating.name = "光栅晶体接收器".Translate();
            ReceiverGrating.Description = "晶体接收器描述";
            ReceiverGrating.description = "晶体接收器描述".Translate();
            ReceiverGrating.GridIndex = 308 + pagePlus;
            ReceiverGrating.HeatValue = 0L;
            ReceiverGrating.prefabDesc = oriItem3.prefabDesc.Copy();
            ReceiverGrating.prefabDesc.powerProductHeat = 12000000;
            ReceiverGrating.prefabDesc.powerProductId = 1014;
            ReceiverGrating.handcraft = ReceiverGratingRecipe;
            ReceiverGrating.handcrafts = new List<RecipeProto> { ReceiverGratingRecipe };
            ReceiverGrating.maincraft = ReceiverGratingRecipe;
            ReceiverGrating.recipes = new List<RecipeProto> { ReceiverGratingRecipe };
            //ReceiverGrating.makes = new List<RecipeProto> { ReceiverGratingRecipe };
            Traverse.Create(ReceiverGrating).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverGrating);

            //IC
            var ReceiverICRecipe = oriRecipe3.Copy();
            var ReceiverIC = oriItem3.Copy();
            ReceiverICRecipe.ID = recipeIdBias + 549;
            ReceiverICRecipe.Name = "组件集成装置";
            ReceiverICRecipe.name = "组件集成装置".Translate();
            ReceiverICRecipe.Description = "组件集成装置描述";
            ReceiverICRecipe.description = "组件集成装置描述".Translate();
            ReceiverICRecipe.Items = new int[] { 1125, 1404, 1303, 1206 };
            ReceiverICRecipe.ItemCounts = new int[] { 10, 10, 5, 5 };
            if (MoreMegaStructure.GenesisCompatibility)
            {
                ReceiverICRecipe.Items = new int[] { 1125, 1014, 1303 };
                ReceiverICRecipe.ItemCounts = new int[] { 10, 20, 10 };
            }
            ReceiverICRecipe.Results = new int[] { 9499 };
            ReceiverICRecipe.ResultCounts = new int[] { 1 };
            ReceiverICRecipe.GridIndex = 309 + pagePlus;
            ReceiverICRecipe.TimeSpend = 480;
            Traverse.Create(ReceiverICRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverIC);
            ReceiverICRecipe.preTech = LDB.techs.Select(1141); //射线接收站科技
            ReceiverIC.ID = 9499;
            ReceiverIC.Name = "组件集成装置";
            ReceiverIC.name = "组件集成装置".Translate();
            ReceiverIC.Description = "组件集成装置描述";
            ReceiverIC.description = "组件集成装置描述".Translate();
            ReceiverIC.GridIndex = 309 + pagePlus;
            ReceiverIC.HeatValue = 0L;
            ReceiverIC.prefabDesc = oriItem3.prefabDesc.Copy();
            ReceiverIC.prefabDesc.powerProductHeat = 4500000000;
            ReceiverIC.prefabDesc.powerProductId = 9500;
            ReceiverIC.handcraft = ReceiverICRecipe;
            ReceiverIC.handcrafts = new List<RecipeProto> { ReceiverICRecipe };
            ReceiverIC.maincraft = ReceiverICRecipe;
            ReceiverIC.recipes = new List<RecipeProto> { ReceiverICRecipe };
            //ReceiverIC.makes = new List<RecipeProto> { ReceiverICRecipe };
            Traverse.Create(ReceiverIC).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverIC);


            LDBTool.PostAddProto(ReceiverIron);
            LDBTool.PostAddProto(ReceiverIronRecipe);
            LDBTool.PostAddProto(ReceiverCopper);
            LDBTool.PostAddProto(ReceiverCopperRecipe);
            LDBTool.PostAddProto(ReceiverSilicon);
            LDBTool.PostAddProto(ReceiverSiliconRecipe);
            LDBTool.PostAddProto(ReceiverTitanium);
            LDBTool.PostAddProto(ReceiverTitaniumRecipe);
            LDBTool.PostAddProto(ReceiverMagore);
            LDBTool.PostAddProto(ReceiverMagoreRecipe);
            LDBTool.PostAddProto(ReceiverCasimir);
            LDBTool.PostAddProto(ReceiverCasimirRecipe);
            LDBTool.PostAddProto(ReceiverIC);
            LDBTool.PostAddProto(ReceiverICRecipe);
            LDBTool.PostAddProto(ReceiverCoal);
            LDBTool.PostAddProto(ReceiverCoalRecipe);
            LDBTool.PostAddProto(ReceiverGrating);
            LDBTool.PostAddProto(ReceiverGratingRecipe);

        }



        
        public static void AddTranslateUILabel()
        {
            StringProto tr1 = new StringProto();
            StringProto tr2 = new StringProto();
            StringProto tr3 = new StringProto();
            StringProto tr4 = new StringProto();
            StringProto tr5 = new StringProto();
            StringProto tr6 = new StringProto();
            StringProto tr7 = new StringProto();
            StringProto tr8 = new StringProto();
            StringProto tr9 = new StringProto();
            StringProto tr10 = new StringProto();
            StringProto tr11 = new StringProto();
            StringProto tr12 = new StringProto();
            StringProto tr13 = new StringProto();
            StringProto tr14 = new StringProto();
            StringProto tr15 = new StringProto();
            StringProto tr16 = new StringProto();
            StringProto tr17 = new StringProto();
            StringProto tr18 = new StringProto();
            StringProto tr19 = new StringProto();
            //StringProto tr20 = new StringProto();

            tr1.ID = 10610;
            tr1.Name = "巨构建筑";
            tr1.name = "巨构建筑";
            tr1.ZHCN = "巨构建筑";
            tr1.ENUS = "Megastructure";
            tr1.FRFR = "Megastructure";

            tr2.ID = 10611;
            tr2.Name = "规划巨构建筑类型";
            tr2.name = "规划巨构建筑类型";
            tr2.ZHCN = "规划巨构建筑类型";
            tr2.ENUS = "Plan Megastructure";
            tr2.FRFR = "Plan Megastructure";


            tr3.ID = 10612;
            tr3.Name = "自由组件";
            tr3.name = "自由组件";
            tr3.ZHCN = "自由组件";
            tr3.ENUS = "Floating components ";
            tr3.FRFR = "Floating components ";


            tr4.ID = 10613;
            tr4.Name = "工作效率";
            tr4.name = "工作效率";
            tr4.ZHCN = "工作效率";
            tr4.ENUS = "Capacity";
            tr4.FRFR = "Capacity";


            tr5.ID = 10614;
            tr5.Name = "自由组件数量";
            tr5.name = "自由组件数量";
            tr5.ZHCN = "自由组件数量";
            tr5.ENUS = "Floating components in total";
            tr5.FRFR = "Floating components in total";


            tr6.ID = 10615;
            tr6.Name = "自由组件云";
            tr6.name = "自由组件云";
            tr6.ZHCN = "自由组件云";
            tr6.ENUS = "Components Swarm";
            tr6.FRFR = "Components Swarm";

            tr7.ID = 10616;
            tr7.Name = "组件云蓝图";
            tr7.name = "组件云蓝图";
            tr7.ZHCN = "组件云蓝图";
            tr7.ENUS = "Swarm Blueprint";
            tr7.FRFR = "Swarm Blueprint";

            tr8.ID = 10617;
            tr8.Name = "锚定结构";
            tr8.name = "锚定结构";
            tr8.ZHCN = "锚定结构";
            tr8.ENUS = "Anchored Structure";
            tr8.FRFR = "Anchored Structure";

            tr9.ID = 10618;
            tr9.Name = "结构层级";
            tr9.name = "结构层级";
            tr9.ZHCN = "结构层级";
            tr9.ENUS = "Structure Layers";
            tr9.FRFR = "Structure Layers";

            tr10.ID = 10619;
            tr10.Name = "锚定结构蓝图";
            tr10.name = "锚定结构蓝图";
            tr10.ZHCN = "锚定结构蓝图";
            tr10.ENUS = "Anchored Structure Blueprint";
            tr10.FRFR = "Anchored Structure Blueprint";

            tr11.ID = 10620;
            tr11.Name = "恒星功效系数";
            tr11.name = "恒星功效系数";
            tr11.ZHCN = "恒星功效系数";
            tr11.ENUS = "Star Efficiency";
            tr11.FRFR = "Star Efficiency";

            tr12.ID = 10621;
            tr12.Name = "最大工作效率";
            tr12.name = "最大工作效率";
            tr12.ZHCN = "最大工作效率";
            tr12.ENUS = "Capacity";
            tr12.FRFR = "Capacity";

            tr13.ID = 10622;
            tr13.Name = "巨构建筑蓝图";
            tr13.name = "巨构建筑蓝图";
            tr13.ZHCN = "巨构建筑蓝图";
            tr13.ENUS = "Structure Blueprint";
            tr13.FRFR = "Structure Blueprint";

            tr14.ID = 10623;
            tr14.Name = "自由组件寿命分布";
            tr14.name = "自由组件寿命分布";
            tr14.ZHCN = "自由组件寿命分布";
            tr14.ENUS = "Life Distribution of Floating Components";
            tr14.FRFR = "Life Distribution of Floating Components";

            tr15.ID = 10624;
            tr15.Name = "自由组件状态统计";
            tr15.name = "自由组件状态统计";
            tr15.ZHCN = "自由组件状态统计";
            tr15.ENUS = "Floating Components Status Statistics";
            tr15.FRFR = "Floating Components Status Statistics";

            tr16.ID = 10625;
            tr16.Name = "自由组件工作效率";
            tr16.name = "自由组件工作效率";
            tr16.ZHCN = "自由组件工作效率";
            tr16.ENUS = "Generation of Floating Components";
            tr16.FRFR = "Generation of Floating Components";

            tr17.ID = 10626;
            tr17.Name = "锚定结构工作效率";
            tr17.name = "锚定结构工作效率";
            tr17.ZHCN = "锚定结构工作效率";
            tr17.ENUS = "Generation of Anchored Structure";
            tr17.FRFR = "Generation of Anchored Structure";

            tr18.ID = 10627;
            tr18.Name = "研究效率";
            tr18.name = "研究效率";
            tr18.ZHCN = "研究效率";
            tr18.ENUS = "Research Capacity";
            tr18.FRFR = "Research Capacity";

            tr19.ID = 10628;
            tr19.Name = "折跃场加速";
            tr19.name = "折跃场加速";
            tr19.ZHCN = "折跃场加速";
            tr19.ENUS = "Warp Acceleration";
            tr19.FRFR = "Warp Acceleration";

            //tr20.ID = 10629;
            //tr20.Name = "锚定结构工作效率";
            //tr20.name = "锚定结构工作效率";
            //tr20.ZHCN = "锚定结构工作效率";
            //tr20.ENUS = "Generation of Anchored Structure";
            //tr20.FRFR = "Generation of Anchored Structure";

            LDBTool.PreAddProto(tr1);
            LDBTool.PreAddProto(tr2);
            LDBTool.PreAddProto(tr3);
            LDBTool.PreAddProto(tr4);
            LDBTool.PreAddProto(tr5);
            LDBTool.PreAddProto(tr6);
            LDBTool.PreAddProto(tr7);
            LDBTool.PreAddProto(tr8);
            LDBTool.PreAddProto(tr9);
            LDBTool.PreAddProto(tr10);
            LDBTool.PreAddProto(tr11);
            LDBTool.PreAddProto(tr12);
            LDBTool.PreAddProto(tr13);
            LDBTool.PreAddProto(tr14);
            LDBTool.PreAddProto(tr15);
            LDBTool.PreAddProto(tr16);
            LDBTool.PreAddProto(tr17);
            LDBTool.PreAddProto(tr18);
            LDBTool.PreAddProto(tr19);
            //LDBTool.PreAddProto(tr20);
        }

        public static void AddTranslateStructureName()
        {
            StringProto tr1 = new StringProto();
            StringProto tr2 = new StringProto();
            StringProto tr3 = new StringProto();
            StringProto tr4 = new StringProto();
            StringProto tr5 = new StringProto();
            StringProto tr6 = new StringProto();
            StringProto tr7 = new StringProto();
            StringProto tr8 = new StringProto();
            StringProto tr9 = new StringProto();
            StringProto tr10 = new StringProto();
            StringProto tr11 = new StringProto();
            StringProto tr12 = new StringProto();
            StringProto tr13 = new StringProto();
            StringProto tr14 = new StringProto();
            StringProto tr15 = new StringProto();
            StringProto tr16 = new StringProto();
            StringProto tr17 = new StringProto();

            tr1.ID = 10630;
            tr1.Name = "规划";
            tr1.name = "规划";
            tr1.ZHCN = "规划";
            tr1.ENUS = "Plan ";
            tr1.FRFR = "Plan ";

            tr2.ID = 10631;
            tr2.Name = "戴森球jinx";
            tr2.name = "戴森球jinx";
            tr2.ZHCN = "戴森球";
            tr2.ENUS = "Dyson Sphere";
            tr2.FRFR = "Dyson Sphere";


            tr3.ID = 10632;
            tr3.Name = "物质解压器";
            tr3.name = "物质解压器";
            tr3.ZHCN = "物质解压器";
            tr3.ENUS = "Matter Decompressor";
            tr3.FRFR = "Matter Decompressor";


            tr4.ID = 10633;
            tr4.Name = "科学枢纽";
            tr4.name = "科学枢纽";
            tr4.ZHCN = "科学枢纽";
            tr4.ENUS = "Science Nexus";
            tr4.FRFR = "Science Nexus";


            tr5.ID = 10634;
            tr5.Name = "折跃场广播阵列";
            tr5.name = "折跃场广播阵列";
            tr5.ZHCN = "折跃场广播阵列";
            tr5.ENUS = "Warp Field Broadcast Array";
            tr5.FRFR = "Warp Field Broadcast Array";


            tr6.ID = 10635;
            tr6.Name = "星际组装厂";
            tr6.name = "星际组装厂";
            tr6.ZHCN = "星际组装厂";
            tr6.ENUS = "Interstellar Assembly";
            tr6.FRFR = "Interstellar Assembly";

            tr7.ID = 10636;
            tr7.Name = "晶体重构器";
            tr7.name = "晶体重构器";
            tr7.ZHCN = "晶体重构器";
            tr7.ENUS = "Crystal Reconstructor";
            tr7.FRFR = "Crystal Reconstructor";

            tr8.ID = 10637;
            tr8.Name = "警告最多一个";
            tr8.name = "警告最多一个";
            tr8.ZHCN = "折跃场广播阵列最多建造一个，请检查星系:";
            tr8.ENUS = "You can only build one Wrapfield broadcast array, please check:";
            tr8.FRFR = "You can only build one Wrapfield broadcast array, please check:";

            tr9.ID = 10638;
            tr9.Name = "警告先拆除";
            tr9.name = "警告先拆除";
            tr9.ZHCN = "你必须先拆除所有锚定结构（节点）再规划不同的巨构建筑。";
            tr9.ENUS = "You have to remove all anchor structures (nodes) before planning different megastructures.";
            tr9.FRFR = "You have to remove all anchor structures (nodes) before planning different megastructures.";

            tr10.ID = 10639;
            tr10.Name = "警告仅黑洞";
            tr10.name = "警告仅黑洞";
            tr10.ZHCN = "物质解压器只能在黑洞上建造。";
            tr10.ENUS = "Matter decompressors can only be built on black holes.";
            tr10.FRFR = "Matter decompressors can only be built on black holes.";

            tr11.ID = 10640;
            tr11.Name = "警告仅中子星白矮星";
            tr11.name = "警告仅中子星白矮星";
            tr11.ZHCN = "晶体重构器只能在中子星或白矮星上建造。";
            tr11.ENUS = "Crystal reconstructors can only be built on neutron stars or white drawf.";
            tr11.FRFR = "Crystal reconstructors can only be built on neutron stars or white drawf.";

            
            tr12.ID = 10641;
            tr12.Name = "当前";
            tr12.name = "当前";
            tr12.ZHCN = "当前";
            tr12.ENUS = "Currently";
            tr12.FRFR = "Currently";
            
            tr13.ID = 10642;
            tr13.Name = "警告未知错误";
            tr13.name = "警告未知错误";
            tr13.ZHCN = "设置异常失败，请像mod作者反馈该问题。";
            tr13.ENUS = "The setting fails abnormally, please report this problem.";
            tr13.FRFR = "The setting fails abnormally, please report this problem.";
            
            tr14.ID = 10643;
            tr14.Name = "MegaStructures";
            tr14.name = "MegaStructures";
            tr14.ZHCN = "巨构";
            tr14.ENUS = "Megastructures";
            tr14.FRFR = "Megastructures";
            
            tr15.ID = 10644;
            tr15.Name = "物质合成";
            tr15.name = "物质合成";
            tr15.ZHCN = "物质合成";
            tr15.ENUS = "Substance generation";
            tr15.FRFR = "Substance generation";
            
            tr16.ID = 10645;
            tr16.Name = "恒星炮";
            tr16.name = "恒星炮";
            tr16.ZHCN = "恒星炮";
            tr16.ENUS = "Star cannon";
            tr16.FRFR = "Star cannon";

            tr17.ID = 10646;
            tr17.Name = "警告最多一个恒星炮";
            tr17.name = "警告最多一个恒星炮";
            tr17.ZHCN = "恒星炮最多建造一个，请检查星系:";
            tr17.ENUS = "You can only build one Star cannon, please check:";
            tr17.FRFR = "You can only build one Star cannon, please check:";
            

            LDBTool.PreAddProto(tr1);
            LDBTool.PreAddProto(tr2);
            LDBTool.PreAddProto(tr3);
            LDBTool.PreAddProto(tr4);
            LDBTool.PreAddProto(tr5);
            LDBTool.PreAddProto(tr6);
            LDBTool.PreAddProto(tr7);
            LDBTool.PreAddProto(tr8);
            LDBTool.PreAddProto(tr9);
            LDBTool.PreAddProto(tr10);
            LDBTool.PreAddProto(tr11);
            LDBTool.PreAddProto(tr12);
            LDBTool.PreAddProto(tr13);
            LDBTool.PreAddProto(tr14);
            LDBTool.PreAddProto(tr15);
            LDBTool.PreAddProto(tr16);
            LDBTool.PreAddProto(tr17);
        }

        public static void AddTranslateProtoNames1()
        {
            int bias = 40;
            StringProto tr1 = new StringProto();
            StringProto tr2 = new StringProto();
            StringProto tr3 = new StringProto();
            StringProto tr4 = new StringProto();
            StringProto tr5 = new StringProto();
            StringProto tr6 = new StringProto();
            StringProto tr7 = new StringProto();
            StringProto tr8 = new StringProto();
            StringProto tr9 = new StringProto();
            StringProto tr10 = new StringProto();
            StringProto tr11 = new StringProto();
            StringProto tr12 = new StringProto();
            StringProto tr13 = new StringProto();
            StringProto tr14 = new StringProto();
            StringProto tr15 = new StringProto();
            StringProto tr16 = new StringProto();
            StringProto tr17 = new StringProto();
            StringProto tr18 = new StringProto();
            StringProto tr19 = new StringProto();
            StringProto tr20 = new StringProto();

            tr1.ID = 10610 + bias;
            tr1.Name = "引力发生装置";
            tr1.name = "引力发生装置";
            tr1.ZHCN = "引力发生装置";
            tr1.ENUS = "Gravity generator";
            tr1.FRFR = tr1.ENUS;

            tr2.ID = 10611 + bias;
            tr2.Name = "引力发生装置描述";
            tr2.name = "引力发生装置描述";
            tr2.ZHCN = "引导临界光子轰击奇异物质即可激发引力波。恒星附近能够获取大量的临界光子，从而能够使引力发生装置高效地运行。";
            tr2.ENUS = "Gravitational waves can be excited by directing critical photons to hit strange matter. A large number of critical photons can be obtained near the star, allowing the gravitational generator to operate efficiently.";
            tr2.FRFR = tr2.ENUS;


            tr3.ID = 10612 + bias;
            tr3.Name = "位面约束环";
            tr3.name = "位面约束环";
            tr3.ZHCN = "位面约束环";
            tr3.ENUS = "Plane constraint ring";
            tr3.FRFR = tr3.ENUS;


            tr4.ID = 10613 + bias;
            tr4.Name = "位面约束环描述";
            tr4.name = "位面约束环描述";
            tr4.ZHCN = "位面约束环能够协同引力透镜引导并操纵引力，也是构建科学枢纽所需的恒星级粒子加速器的必要组件。";
            tr4.ENUS = "Plane constraint ring can guide and manipulate gravity with graviton lens, and it is also an essential component of the stellar-scale particle accelerators which are needed to build science nexus.";
            tr4.FRFR = tr4.ENUS;


            tr5.ID = 10614 + bias;
            tr5.Name = "引力钻头";
            tr5.name = "引力钻头";
            tr5.ZHCN = "引力钻头";
            tr5.ENUS = "Graviton drill";
            tr5.FRFR = tr5.ENUS;


            tr6.ID = 10615 + bias;
            tr6.Name = "引力钻头描述";
            tr6.name = "引力钻头描述";
            tr6.ZHCN = "借助黑洞本身的引力，引力钻头能够将物质从黑洞中取出，这还包括吸积盘中大量的单极磁石。借助谐振盘，黑洞原质将能够被解压并在星系内输送。";
            tr6.ENUS = "The graviton drill can pull matter out of the black hole using the gravity of the black hole itself, which also includes the unipolar magnets in the accretion disk. With the help of the resonant disc, the matter from the black hole will be able to be decompressed and transported within the galaxy.";
            tr6.FRFR = tr6.ENUS;

            tr7.ID = 10616 + bias;
            tr7.Name = "隧穿激发装置";
            tr7.name = "隧穿激发装置";
            tr7.ZHCN = "隧穿激发装置";
            tr7.ENUS = "Tunneling exciter";
            tr7.FRFR = tr7.ENUS;

            tr8.ID = 10617 + bias;
            tr8.Name = "隧穿激发装置描述";
            tr8.name = "隧穿激发装置描述";
            tr8.ZHCN = "隧穿激发装置可以完美地掌控量子隧穿效应，常被用来强化量子芯片的处理能力和纠错能力。通过量子隧穿效应还能够轻易突破弯曲空间的能量势垒，使得在任意远的空间打开裂口成为可能。";
            tr8.ENUS = "Tunneling exciters can perfectly control the quantum tunneling effect, and are often used to enhance the processing and error correction capabilities of quantum chips. The quantum tunneling effect can also easily break through the energy barrier of the curved space, making it possible to open the warp crack in any space far away.";
            tr8.FRFR = tr8.ENUS;

            tr9.ID = 10618 + bias;
            tr9.Name = "谐振盘";
            tr9.name = "谐振盘";
            tr9.ZHCN = "谐振盘";
            tr9.ENUS = "Resonant disc";
            tr9.FRFR = tr9.ENUS;

            tr10.ID = 10619 + bias;
            tr10.Name = "谐振盘描述";
            tr10.name = "谐振盘描述";
            tr10.ZHCN = "谐振盘仅通过恒星级别的能量就可以产生跨越恒星系的空间波动能量束。如果将谐振盘组成阵列，理论上可以形成覆盖全宇宙的折跃能量场。";
            tr10.ENUS = "The resonant disc can generate interstellar-scale space-wave energy beams from only stellar-scale energy. If the resonant discs are formed into an array, a warp field covering the entire universe can theoretically be formed.";
            tr10.FRFR = tr10.ENUS;

            tr11.ID = 10620 + bias;
            tr11.Name = "光子探针";
            tr11.name = "光子探针";
            tr11.ZHCN = "光子探针";
            tr11.ENUS = "Photon probe";
            tr11.FRFR = tr11.ENUS;

            tr12.ID = 10621 + bias;
            tr12.Name = "光子探针描述";
            tr12.name = "光子探针描述";
            tr12.ZHCN = "将临界光子变频后发射并引导晶体重构，发射的光子还能被回收。";
            tr12.ENUS = "The critical photons are frequency-converted and emitted, thereby guiding the crystal reconstruction. The photons can also be recovered.";
            tr12.FRFR = tr12.ENUS;

            tr13.ID = 10622 + bias;
            tr13.Name = "量子计算机";
            tr13.name = "量子计算机";
            tr13.ZHCN = "量子计算机";
            tr13.ENUS = "Quantum computer";
            tr13.FRFR = tr13.ENUS;

            tr14.ID = 10623 + bias;
            tr14.Name = "量子计算机描述";
            tr14.name = "量子计算机描述";
            tr14.ZHCN = "只要供给足够的能量，量子计算机的运算时钟能够无限逼近普朗克时间。通过量子比特协同，其潜在的单线程运算速率还能突破物理极限，并可以无限提升。现在，限制其计算速度的将只有能量输入水平。";
            tr14.ENUS = "As long as enough energy is supplied, the computing clock of a quantum computer can approach Planck time indefinitely. Through the cooperation of qubits, its potential single-threaded operation rate can also break through the physical limit and can be infinitely improved. Now, it will only be the level of energy input that will limit its computational speed.";
            tr14.FRFR = tr14.ENUS;

            tr15.ID = 10624 + bias;
            tr15.Name = "星际组装厂组件";
            tr15.name = "星际组装厂组件";
            tr15.ZHCN = "星际组装厂组件";
            tr15.ENUS = "Interstellar assembly component";
            tr15.FRFR = tr15.ENUS;

            tr16.ID = 10625 + bias;
            tr16.Name = "星际组装厂组件描述";
            tr16.name = "星际组装厂组件描述";
            tr16.ZHCN = "使用微型火箭将组件运载到恒星附近并构建星际组装厂的节点和框架。";
            tr16.ENUS = "Use a small carrier rocket to the planned Interstellar assembly to form the nodes and frames of Interstellar assembly.";
            tr16.FRFR = tr16.ENUS;

            tr17.ID = 10626 + bias;
            tr17.Name = "物质解压器运载火箭";
            tr17.name = "物质解压器运载火箭";
            tr17.ZHCN = "物质解压器运载火箭";
            tr17.ENUS = "Matter decompressor carrier rocket";
            tr17.FRFR = tr17.ENUS;

            tr18.ID = 10627 + bias;
            tr18.Name = "物质解压器运载火箭描述";
            tr18.name = "物质解压器运载火箭描述";
            tr18.ZHCN = "物质解压器相关组件的运载工具。";
            tr18.ENUS = "The delivery vehicle for the components of the Matter decompressor.";
            tr18.FRFR = tr18.ENUS;

            tr19.ID = 10628 + bias;
            tr19.Name = "科学枢纽运载火箭";
            tr19.name = "科学枢纽运载火箭";
            tr19.ZHCN = "科学枢纽运载火箭";
            tr19.ENUS = "Science nexus carrier rocket";
            tr19.FRFR = tr19.ENUS;

            tr20.ID = 10629 + bias;
            tr20.Name = "科学枢纽运载火箭描述";
            tr20.name = "科学枢纽运载火箭描述";
            tr20.ZHCN = "科学枢纽相关组件的运载工具。";
            tr20.ENUS = "The delivery vehicle for the components of the Science nexus.";
            tr20.FRFR = tr20.ENUS;

            LDBTool.PreAddProto(tr1);
            LDBTool.PreAddProto(tr2);
            LDBTool.PreAddProto(tr3);
            LDBTool.PreAddProto(tr4);
            LDBTool.PreAddProto(tr5);
            LDBTool.PreAddProto(tr6);
            LDBTool.PreAddProto(tr7);
            LDBTool.PreAddProto(tr8);
            LDBTool.PreAddProto(tr9);
            LDBTool.PreAddProto(tr10);
            LDBTool.PreAddProto(tr11);
            LDBTool.PreAddProto(tr12);
            LDBTool.PreAddProto(tr13);
            LDBTool.PreAddProto(tr14);
            LDBTool.PreAddProto(tr15);
            LDBTool.PreAddProto(tr16);
            LDBTool.PreAddProto(tr17);
            LDBTool.PreAddProto(tr18);
            LDBTool.PreAddProto(tr19);
            LDBTool.PreAddProto(tr20);
        }

        public static void AddTranslateProtoNames2()
        {
            int bias = 60;
            StringProto tr1 = new StringProto();
            StringProto tr2 = new StringProto();
            StringProto tr3 = new StringProto();
            StringProto tr4 = new StringProto();
            StringProto tr5 = new StringProto();
            StringProto tr6 = new StringProto();
            StringProto tr7 = new StringProto();
            StringProto tr8 = new StringProto();
            StringProto tr9 = new StringProto();
            StringProto tr10 = new StringProto();
            StringProto tr11 = new StringProto();
            StringProto tr12 = new StringProto();
            StringProto tr13 = new StringProto();
            StringProto tr14 = new StringProto();
            StringProto tr15 = new StringProto();
            StringProto tr16 = new StringProto();
            StringProto tr17 = new StringProto();
            StringProto tr18 = new StringProto();
            StringProto tr19 = new StringProto();
            StringProto tr20 = new StringProto();

            tr1.ID = 10610 + bias;
            tr1.Name = "谐振发射器运载火箭";
            tr1.name = "谐振发射器运载火箭";
            tr1.ZHCN = "谐振发射器运载火箭";
            tr1.ENUS = "Resonant generator carrier rocket";
            tr1.FRFR = tr1.ENUS;

            tr2.ID = 10611 + bias;
            tr2.Name = "谐振发射器运载火箭描述";
            tr2.name = "谐振发射器运载火箭描述";
            tr2.ZHCN = "大量谐振发射器将组成阵列并向全星系广播折跃能量场。";
            tr2.ENUS = "A large number of resonant generators will form an array and broadcast the warp energy field to the entire galaxy.";
            tr2.FRFR = tr2.ENUS;


            tr3.ID = 10612 + bias;
            tr3.Name = "星际组装厂运载火箭";
            tr3.name = "星际组装厂运载火箭";
            tr3.ZHCN = "星际组装厂运载火箭";
            tr3.ENUS = "Interstellar assembly carrier rocket";
            tr3.FRFR = tr3.ENUS;


            tr4.ID = 10613 + bias;
            tr4.Name = "星际组装厂运载火箭描述";
            tr4.name = "星际组装厂运载火箭描述";
            tr4.ZHCN = "星际组装厂组件的运载工具。";
            tr4.ENUS = "The delivery vehicle of Interstellar assembly components.";
            tr4.FRFR = tr4.ENUS;


            tr5.ID = 10614 + bias;
            tr5.Name = "晶体重构器运载火箭";
            tr5.name = "晶体重构器运载火箭";
            tr5.ZHCN = "晶体重构器运载火箭";
            tr5.ENUS = "Crystal reconstructor carrier rocket";
            tr5.FRFR = tr5.ENUS;


            tr6.ID = 10615 + bias;
            tr6.Name = "晶体重构器运载火箭描述";
            tr6.name = "晶体重构器运载火箭描述";
            tr6.ZHCN = "晶体重构器相关组件的运载工具。";
            tr6.ENUS = "The delivery vehicle for the components of the Crystal reconstructor.";
            tr6.FRFR = tr6.ENUS;

            tr7.ID = 10616 + bias;
            tr7.Name = "铁金属重构装置";
            tr7.name = "铁金属重构装置";
            tr7.ZHCN = "铁金属重构装置";
            tr7.ENUS = "Iron reconstruct receiver";
            tr7.FRFR = tr7.ENUS;

            tr8.ID = 10617 + bias;
            tr8.Name = "铜金属重构装置";
            tr8.name = "铜金属重构装置";
            tr8.ZHCN = "铜金属重构装置";
            tr8.ENUS = "Copper reconstruct receiver";
            tr8.FRFR = tr8.ENUS;

            tr9.ID = 10618 + bias;
            tr9.Name = "高纯硅重构装置";
            tr9.name = "高纯硅重构装置";
            tr9.ZHCN = "高纯硅重构装置";
            tr9.ENUS = "Silicon reconstruct receiver";
            tr9.FRFR = tr9.ENUS;

            tr10.ID = 10619 + bias;
            tr10.Name = "钛金属重构装置";
            tr10.name = "钛金属重构装置";
            tr10.ZHCN = "钛金属重构装置";
            tr10.ENUS = "Titanium reconstruct receiver";
            tr10.FRFR = tr10.ENUS;

            tr11.ID = 10620 + bias;
            tr11.Name = "单极磁石重构装置";
            tr11.name = "单极磁石重构装置";
            tr11.ZHCN = "单极磁石重构装置";
            tr11.ENUS = "Unipolar magnet receiver";
            tr11.FRFR = tr11.ENUS;

            tr12.ID = 10621 + bias;
            tr12.Name = "接收重构装置描述";
            tr12.name = "接收重构装置描述";
            tr12.ZHCN = "从黑洞中解压出的亚稳态物质被接收后经过处理，重构为可直接使用的稳定材料。";
            tr12.ENUS = "The metastable matter decompressed from the black hole is received, processed, and reconstructed into stable material that can be used directly.";
            tr12.FRFR = tr12.ENUS;

            tr13.ID = 10622 + bias;
            tr13.Name = "晶体接收器";
            tr13.name = "晶体接收器";
            tr13.ZHCN = "晶体接收器";
            tr13.ENUS = "Crystal receiver";
            tr13.FRFR = tr13.ENUS;

            tr14.ID = 10623 + bias;
            tr14.Name = "晶体接收器描述";
            tr14.name = "晶体接收器描述";
            tr14.ZHCN = "从晶体重构器中合成的卡西米尔晶体前导微晶流将在此经过自发β衰变并形成完美的卡西米尔晶体。接收器也可以转而富集该过程的副产物——光栅石。";
            tr14.ENUS = "The Casimir crystal precursor crystallite flow synthesized from the Crystal reconstructor will undergo spontaneous β decay here and form perfect casimir crystals. The receivers can also in turn enrich for optical grating crystals, the by-product of the process.";
            tr14.FRFR = tr14.ENUS;

            tr15.ID = 10624 + bias;
            tr15.Name = "组件集成装置";
            tr15.name = "组件集成装置";
            tr15.ZHCN = "组件集成装置";
            tr15.ENUS = "Component integration station";
            tr15.FRFR = tr15.ENUS;

            tr16.ID = 10625 + bias;
            tr16.Name = "组件集成装置描述";
            tr16.name = "组件集成装置描述";
            tr16.ZHCN = "将星际组装厂的高集成配件进行预解压，形成可被快速组装的多功能集成组件。";
            tr16.ENUS = "Pre-decompress the high-integration parts from the Interstellar assembly, to form multi-functional integrated components that can be quickly assembled.";
            tr16.FRFR = tr16.ENUS;

            tr17.ID = 10626 + bias;
            tr17.Name = "多功能集成组件";
            tr17.name = "多功能集成组件";
            tr17.ZHCN = "多功能集成组件";
            tr17.ENUS = "Multi-functional integrated components";
            tr17.FRFR = tr17.ENUS;

            tr18.ID = 10627 + bias;
            tr18.Name = "多功能集成组件描述";
            tr18.name = "多功能集成组件描述";
            tr18.ZHCN = "超高集成度使其可以迅速地组装成多种生产建筑和物流组件，却仅占用极小的空间。";
            tr18.ENUS = "The high level of integration makes it possible to quickly assemble a variety of production building and logistics components, while occupying very little space.";
            tr18.FRFR = tr18.ENUS;

            tr19.ID = 10628 + bias;
            tr19.Name = "光栅晶体接收器";
            tr19.name = "光栅晶体接收器";
            tr19.ZHCN = "光栅晶体接收器";
            tr19.ENUS = "Optical crystal receiver";
            tr19.FRFR = tr19.ENUS;

            tr20.ID = 10629 + bias;
            tr20.Name = "石墨提炼装置";
            tr20.name = "石墨提炼装置";
            tr20.ZHCN = "石墨提炼装置";
            tr20.ENUS = "Graphite extraction receiver";
            tr20.FRFR = tr20.ENUS;

            LDBTool.PreAddProto(tr1);
            LDBTool.PreAddProto(tr2);
            LDBTool.PreAddProto(tr3);
            LDBTool.PreAddProto(tr4);
            LDBTool.PreAddProto(tr5);
            LDBTool.PreAddProto(tr6);
            LDBTool.PreAddProto(tr7);
            LDBTool.PreAddProto(tr8);
            LDBTool.PreAddProto(tr9);
            LDBTool.PreAddProto(tr10);
            LDBTool.PreAddProto(tr11);
            LDBTool.PreAddProto(tr12);
            LDBTool.PreAddProto(tr13);
            LDBTool.PreAddProto(tr14);
            LDBTool.PreAddProto(tr15);
            LDBTool.PreAddProto(tr16);
            LDBTool.PreAddProto(tr17);
            LDBTool.PreAddProto(tr18);
            LDBTool.PreAddProto(tr19);
            LDBTool.PreAddProto(tr20);
        }

        public static void AddTranslateProtoNames3()
        {
            int bias = 80;
            StringProto tr1 = new StringProto();
            StringProto tr2 = new StringProto();
            StringProto tr3 = new StringProto();
            StringProto tr4 = new StringProto();
            StringProto tr5 = new StringProto();
            StringProto tr6 = new StringProto();
            StringProto tr7 = new StringProto();
            StringProto tr8 = new StringProto();
            StringProto tr9 = new StringProto();
            StringProto tr10 = new StringProto();
            StringProto tr11 = new StringProto();
            StringProto tr12 = new StringProto();
            StringProto tr13 = new StringProto();
            //StringProto tr14 = new StringProto();
            //StringProto tr15 = new StringProto();
            //StringProto tr16 = new StringProto();
            //StringProto tr17 = new StringProto();
            //StringProto tr18 = new StringProto();
            //StringProto tr19 = new StringProto();
            //StringProto tr20 = new StringProto();

            tr1.ID = 10610 + bias;
            tr1.Name = "传送带 快速组装";
            tr1.name = "传送带 快速组装";
            tr1.ZHCN = "传送带 快速组装";
            tr1.ENUS = "Conveyor belt - quick assembly";
            tr1.FRFR = tr1.ENUS;

            tr2.ID = 10611 + bias;
            tr2.Name = "分拣器 快速组装";
            tr2.name = "分拣器 快速组装";
            tr2.ZHCN = "分拣器 快速组装";
            tr2.ENUS = "Sorter - quick assembly";
            tr2.FRFR = tr2.ENUS;


            tr3.ID = 10612 + bias;
            tr3.Name = "配电站 快速组装";
            tr3.name = "配电站 快速组装";
            tr3.ZHCN = "配电站 快速组装";
            tr3.ENUS = "Substation - quick assembly";
            tr3.FRFR = tr3.ENUS;


            tr4.ID = 10613 + bias;
            tr4.Name = "制造台 快速组装";
            tr4.name = "制造台 快速组装";
            tr4.ZHCN = "制造台 快速组装";
            tr4.ENUS = "Assembling machine - quick assembly";
            tr4.FRFR = tr4.ENUS;


            tr5.ID = 10614 + bias;
            tr5.Name = "位面熔炉 快速组装";
            tr5.name = "位面熔炉 快速组装";
            tr5.ZHCN = "位面熔炉 快速组装";
            tr5.ENUS = "Plane smelter - quick assembly";
            tr5.FRFR = tr5.ENUS;


            tr6.ID = 10615 + bias;
            tr6.Name = "化工厂 快速组装";
            tr6.name = "化工厂 快速组装";
            tr6.ZHCN = "化工厂 快速组装";
            tr6.ENUS = "Chemical plant - quick assembly";
            tr6.FRFR = tr6.ENUS;

            tr7.ID = 10616 + bias;
            tr7.Name = "精炼厂 快速组装";
            tr7.name = "精炼厂 快速组装";
            tr7.ZHCN = "精炼厂 快速组装";
            tr7.ENUS = "Refinery - quick assembly";
            tr7.FRFR = tr7.ENUS;

            tr8.ID = 10617 + bias;
            tr8.Name = "对撞机 快速组装";
            tr8.name = "对撞机 快速组装";
            tr8.ZHCN = "对撞机 快速组装";
            tr8.ENUS = "Collider - quick assembly";
            tr8.FRFR = tr8.ENUS;

            tr9.ID = 10618 + bias;
            tr9.Name = "研究站 快速组装";
            tr9.name = "研究站 快速组装";
            tr9.ZHCN = "研究站 快速组装";
            tr9.ENUS = "Lab - quick assembly";
            tr9.FRFR = tr9.ENUS;

            tr10.ID = 10619 + bias;
            tr10.Name = "人造恒星 快速组装";
            tr10.name = "人造恒星 快速组装";
            tr10.ZHCN = "人造恒星 快速组装";
            tr10.ENUS = "Artificial star - quick assembly";
            tr10.FRFR = tr10.ENUS;

            tr11.ID = 10620 + bias;
            tr11.Name = "行星内物流 快速组装";
            tr11.name = "行星内物流 快速组装";
            tr11.ZHCN = "行星内物流 快速组装";
            tr11.ENUS = "Planetary logistics - quick assembly";
            tr11.FRFR = tr11.ENUS;

            tr12.ID = 10621 + bias;
            tr12.Name = "星际物流 快速组装";
            tr12.name = "星际物流 快速组装";
            tr12.ZHCN = "星际物流 快速组装";
            tr12.ENUS = "Interstellar logistics - quick assembly";
            tr12.FRFR = tr12.ENUS;

            tr13.ID = 10622 + bias;
            tr13.Name = "快速组装描述";
            tr13.name = "快速组装描述";
            tr13.ZHCN = "使用多功能集成组件快速递组装成目标物品。";
            tr13.ENUS = "Quickly assemble target items using multi-functional integrated components.";
            tr13.FRFR = tr13.ENUS;

            //tr14.ID = 10623 + bias;
            //tr14.Name = "快速组装描述";
            //tr14.name = "快速组装描述";
            //tr14.ZHCN = "使用多功能集成组件快速递组装成目标物品。";
            //tr14.ENUS = "Quickly assemble target items using multi-functional integrated components.";
            //tr14.FRFR = tr14.ENUS;

            //tr15.ID = 10624 + bias;
            //tr15.Name = "组件集成装置";
            //tr15.name = "组件集成装置";
            //tr15.ZHCN = "组件集成装置";
            //tr15.ENUS = "Component integration station";
            //tr15.FRFR = tr15.ENUS;

            //tr16.ID = 10625 + bias;
            //tr16.Name = "组件集成装置描述";
            //tr16.name = "组件集成装置描述";
            //tr16.ZHCN = "将星际组装厂的高集成配件进行预解压，形成可被快速组装的多功能集成组件。";
            //tr16.ENUS = "Pre-decompress the high-integration parts from the Interstellar assembly, to form multi-functional integrated components that can be quickly assembled.";
            //tr16.FRFR = tr16.ENUS;

            //tr17.ID = 10626 + bias;
            //tr17.Name = "多功能集成组件";
            //tr17.name = "多功能集成组件";
            //tr17.ZHCN = "多功能集成组件";
            //tr17.ENUS = "Multi-functional integrated components";
            //tr17.FRFR = tr17.ENUS;

            //tr18.ID = 10627 + bias;
            //tr18.Name = "多功能集成组件描述";
            //tr18.name = "多功能集成组件描述";
            //tr18.ZHCN = "超高集成度使其可以迅速地组装成多种生产建筑和物流组件，却仅占用极小的空间。";
            //tr18.ENUS = "The high level of integration makes it possible to quickly assemble a variety of production building and logistics components, while occupying very little space.";
            //tr18.FRFR = tr18.ENUS;

            //tr19.ID = 10628 + bias;
            //tr19.Name = "科学枢纽运载火箭";
            //tr19.name = "科学枢纽运载火箭";
            //tr19.ZHCN = "科学枢纽运载火箭";
            //tr19.ENUS = "Science nexus carrier rocket";
            //tr19.FRFR = tr19.ENUS;

            //tr20.ID = 10629 + bias;
            //tr20.Name = "科学枢纽运载火箭描述";
            //tr20.name = "科学枢纽运载火箭描述";
            //tr20.ZHCN = "科学枢纽相关组件的运载工具。";
            //tr20.ENUS = "The delivery vehicle for the components of the Science nexus.";
            //tr20.FRFR = tr20.ENUS;

            LDBTool.PreAddProto(tr1);
            LDBTool.PreAddProto(tr2);
            LDBTool.PreAddProto(tr3);
            LDBTool.PreAddProto(tr4);
            LDBTool.PreAddProto(tr5);
            LDBTool.PreAddProto(tr6);
            LDBTool.PreAddProto(tr7);
            LDBTool.PreAddProto(tr8);
            LDBTool.PreAddProto(tr9);
            LDBTool.PreAddProto(tr10);
            LDBTool.PreAddProto(tr11);
            LDBTool.PreAddProto(tr12);
            LDBTool.PreAddProto(tr13);
            //LDBTool.PreAddProto(tr14);
            //LDBTool.PreAddProto(tr15);
            //LDBTool.PreAddProto(tr16);
            //LDBTool.PreAddProto(tr17);
            //LDBTool.PreAddProto(tr18);
            //LDBTool.PreAddProto(tr19);
            //LDBTool.PreAddProto(tr20);
        }


        public static void AddTranslateProtoNames4()
        {
            ProtoRegistry.RegisterString("每秒伤害gm", "Damage per second", "每秒伤害");
            ProtoRegistry.RegisterString("阶段", "stage", "阶段");
            ProtoRegistry.RegisterString("连续开火次数", "Maximum fire times per charging", "连续开火次数");
            ProtoRegistry.RegisterString("最大射程", "Maximum fire range", "最大射程");
            ProtoRegistry.RegisterString("伤害削减", "Damage reduction", "伤害削减");
            ProtoRegistry.RegisterString("当前能量水平", "Current capacity", "当前能量水平");
            ProtoRegistry.RegisterString("下一阶段所需能量水平", "Next stage required capacity", "下一阶段所需能量水平");
            ProtoRegistry.RegisterString("冷却及充能时间", "Cooldown & charge time", "冷却及充能时间");
            ProtoRegistry.RegisterString("修建进度", "\nProgress to\nnext stage", "修建进度");
            ProtoRegistry.RegisterString("最终阶段", "Final stage", "最终阶段");
            ProtoRegistry.RegisterString("节点总数（已规划）gm", "Nodes in total(Planned)", "节点总数（已规划）");
            ProtoRegistry.RegisterString("请求功率gm", "Requested power", "最终阶段");
            ProtoRegistry.RegisterString("无限制gm", "Infinite", "无限制");

            ProtoRegistry.RegisterString("力场发生器", "Force field generator", "力场发生器");
            ProtoRegistry.RegisterString("力场发生器描述", "With the help of the gravity generator, the force field generator can multiply the gravitational force in a fixed resonance field and give it a highly controllable directivity. If the energy supply is sufficient, the force field generator is able to deflects or even reverses the direction of the force field, making it possible to encode the resonant frequency of the force field.", 
                "借助引力发生装置，力场发生器可以将引力在固定的谐振场域成倍放大，并赋予其高度可控的指向性。如果能量供应足够，力场发生器快速偏折甚至掉转力场方向，这使得对力场谐振频率进行编码成为可能。");
            ProtoRegistry.RegisterString("复合态晶体", "Compound cyrstal", "复合态晶体");
            ProtoRegistry.RegisterString("复合态晶体描述", "This single-molecule crystal has a normal density like ordinary matter, but can be reshaped into a material with extremely high hardness under the constraints of strong interaction force, so this material is also called strong interaction material (SIM).", 
                "这种单分子晶体像普通物质一样拥有正常的密度，但能够在强相互作用力的束缚下，被重塑为硬度极高的物质，因此这种物质又被称为强相互作用力材料（SIM）。");
            ProtoRegistry.RegisterString("电磁力抑制器", "Electromagnetic force suppressor", "电磁力抑制器");
            ProtoRegistry.RegisterString("电磁力抑制器描述", "By eliminating the electromagnetic force between atomic nucleus, it allows the range of the strong interaction force to overflow the nucleus and expand to atom scope, providing the conditions for precise control of the strong interaction force. The suppressed electromagnetic force can also be redirected to create a vacuum Valsex field vortex ring.", 
                "通过消除原子核之间的电磁力，允许强相互作用力的范围溢出原子核，并扩展到原子大小，为精确控制强力提供了条件。被抑制的电磁力还可以被引导至特定方向用以产生真空瓦尔塞克斯电场涡环。");
            ProtoRegistry.RegisterString("胶子发生器", "Gluon generator", "胶子发生器");
            ProtoRegistry.RegisterString("胶子发生器描述", "Generate controllable gluons to limit or expand the strength and scope of the strong interaction. The gluon generator must be controlled by quantum computers, so as to precisely control the arrangement of atoms on the quantum scale.", 
                "产生可控胶子，以此限制或扩大强相互作用力的强度和作用范围。胶子发生器必须在量子计算机的协助下才能提高控制的精准程度，从而在量子尺度上精确控制原子排布。");
            ProtoRegistry.RegisterString("强力过载装置", "Strong interaction overload device", "强力过载装置");
            ProtoRegistry.RegisterString("强力过载装置描述", "The SIO device can make the repulsive and the attractive force peak to coincide precisely at a specific point, so that any deviation of the nucleus will be pulled back by the strong interaction force. If electromagnetic interference is removed, the nucleus will be fully anchored.", 
                "强力过载装置可以使强力的排斥力峰值和吸引力峰值在特定的点精准重合，因而原子核的任何偏离都会被强力拉回。如果剔除了电磁力干扰，原子核将被完全锚定。");
            ProtoRegistry.RegisterString("导流框架", "Flow guid frame", "导流框架");
            ProtoRegistry.RegisterString("导流框架描述", "Storing, directing the energy of stars in a specific direction, creating a very high power output.", "将恒星的能量存储并引导、集中至特定方向，创造极高功率的能量输出。"); 
            ProtoRegistry.RegisterString("恒星炮组件", "Star cannon component", "恒星炮组件");
            ProtoRegistry.RegisterString("恒星炮组件描述", "The star cannon can store the energy of the star and guide it to the front of the muzzle through the frame. Nicoll-Dyson beam will continuously destabilize the wormhole, eventually closing the high-dimensional channel.", "恒星炮能够储存恒星的能量，并通过框架引导至炮口前方。尼科尔-戴森光束可以持续破坏虫洞的稳定性，最终关闭高维通道。");
            ProtoRegistry.RegisterString("恒星炮运载火箭", "Star cannon carrier rocket", "恒星炮运载火箭");
            ProtoRegistry.RegisterString("恒星炮运载火箭描述", "The delivery vehicle for the components of the Star cannon.", "恒星炮相关组件的运载工具。"); 
            ProtoRegistry.RegisterString("水滴gm", "Droplet", "水滴");
            ProtoRegistry.RegisterString("水滴描述gm", "If the battle is in the current galaxy, the droplets will leave the mecha and hit the enemy's key structures with its extremely hard surface. Since precise control of the propulsion and steering of the droplets requires powerful remote computing power, and the launch of droplets and manipulation require the remote supply of mecha energy, there is an upper limit to the number of droplets that the mecha can control at one time.", 
                "如果战斗发生在当前星系，水滴将从机甲中离开，并使用极其坚硬的表面撞击敌人的关键结构。由于精确地控制水滴的推进和转向需要强大的远端运算能力，且发射水滴和操控均需要机甲能量的远程供给，机甲一次性能够操控的水滴数量是有上限的。");

            ProtoRegistry.RegisterString("恒星炮设计说明题目", "Design Instructions", "恒星炮设计说明");
            ProtoRegistry.RegisterString("恒星炮设计说明文本",
                "1. When the star cannon fires, the rotation axes of all layers will overlap, and the north pole will point to the target wormhole, so please design the north pole of each layer as the center of the muzzle;\n"
                + "2. When the star cannon fires, 12 random  nodes on the 1st layer will emit a laser to the muzzle. For aesthetic reasons, please try to make the first layer only contain up to 12 nodes, and try to make them symmetrical to each other"
                + "\n3. The construction of the star cannon needs to go through multiple stages. And at the same time, the star cannon can increase its damage, firing distance and charging speed several times. After reaching the final stage, continue to build shells will continuously increase the damage.",
                "1.恒星炮开火时所有层级的旋转轴将重叠，并且让北极指向目标开火，因此设计时请以各层的北极点为炮口中心；\n2.恒星炮开火时，第1层的随机12个节点将发射出激光指向炮口，为美观考虑，请尽量使得第1层只包含12个（或更少的）节点，并使其相互对称。\n3.恒星炮建造需要经过多个阶段，随着各建造阶段完成，恒星炮能数次提高伤害、开火距离和充能速度等属性。在达到最终阶段后，继续修建壳层可以不断提高伤害。");
            ProtoRegistry.RegisterString("功能说明题目","Function","功能");
            ProtoRegistry.RegisterString("物质解压器功能文本", "Produce unipolar magnet and some basic resources such as iron ingot, which can be received by corresponding receivers.", "产出单极磁石和一些基础资源（例如铁块），可被对应的物质重构器接收。");
            ProtoRegistry.RegisterString("科学枢纽功能文本", "Upload hash points for research without requiring any matrix.", "无需矩阵即可上传hash点数进行研究。");
            ProtoRegistry.RegisterString("折跃场广播阵列功能文本", "Increase the warp speed of logistics vessels.", "提高物流运输船的曲速速度。");
            ProtoRegistry.RegisterString("星际组装厂功能文本", "Produce Multi-functional integrated components.", "生产多功能集成组件。");
            ProtoRegistry.RegisterString("晶体重构器功能文本", "Produce Casimir crystals and optical grating crystals, which can be received by corresponding receivers.", "产出卡西米尔晶体和光栅石，可被对应的接收器接收。");
        }

        /// <summary>
        /// 用于在接收器面板显示的文本修正，因为并非总是生成光子
        /// </summary>
        /// <param name="proto"></param>
        public static void ChangeReceiverRelatedStringProto(Proto proto)
        {
            if (proto is StringProto) //光子生成
            {
                if (proto.ID == 1264)
                {
                    var item = proto as StringProto;
                    item.ZHCN = "物质合成";
                    item.ENUS = "Substance generation";
                    item.FRFR = "Substance generation";
                }

                else if (proto.ID == 1265 || proto.ID == 1369)
                {
                    var item = proto as StringProto;
                    item.ZHCN = "在物质合成模式下，接收站将允许接收巨构建筑的输出，并将其转换为对应物质。";
                    item.ENUS = "In Substance generation mode, the receiver will receive product delivered by the megastructure and convert it into the corresponding item.";
                    item.FRFR = "In Substance generation mode, the receiver will receive product delivered by the megastructure and convert it into the corresponding item.";
                }

                else if (proto.ID == 1187)
                {
                    var item = proto as StringProto;
                    item.ZHCN = "打开巨构建筑面板，制定巨构建筑的建造规划。";
                    item.ENUS = "Turn on the Megastructure editor, and make a build plan.";
                    item.FRFR = "Turn on the Megastructure editor, and make a build plan.";
                }

                else if (proto.ID == 1186) //左下角戴森球编辑器按钮
                {
                    var item = proto as StringProto;
                    item.ZHCN = "巨构建筑 (Y)";
                    item.ENUS = "Megastructure (Y)";
                    item.FRFR = "Megastructure (Y)";
                }

                else if (proto.ID == 1167) //戴森球计划 (8)   这是建造栏的标签
                {
                    var item = proto as StringProto;
                    item.ZHCN = "巨构建筑 (8)";
                    item.ENUS = "Megastructure (8)";
                    item.FRFR = "Megastructure (8)";
                }

                else if (proto.ID == 1416) //统计面板
                {
                    var item = proto as StringProto;
                    item.ZHCN = "巨构建筑";
                    item.ENUS = "Megastructure";
                    item.FRFR = "Megastructure";
                }

                else if (proto.ID == 7346) //修建时的tooltip
                {
                    var item = proto as StringProto;
                    item.ZHCN = "巨构节点";
                    item.ENUS = "Megastructure Node";
                    item.FRFR = "Megastructure Node";
                }

                else if (proto.ID == 7347) //修建时的tooltip
                {
                    var item = proto as StringProto;
                    item.ZHCN = "规划修建巨构建筑节点，点击该按钮可以选择节点的样式。";
                    item.ENUS = "Plan and build the Megastructure Node, click this button to choose the node style.";
                    item.FRFR = "Plan and build the Megastructure Node, click this button to choose the node style.";
                }

                else if (proto.ID == 7348) //修建时的tooltip
                {
                    var item = proto as StringProto;
                    item.ZHCN = "巨构壳面";
                    item.ENUS = "Megastructure Shell";
                    item.FRFR = "Megastructure Shell";
                }

                else if (proto.ID == 7349) //修建时的tooltip
                {
                    var item = proto as StringProto;
                    item.ZHCN = "若要规划一个巨构壳面，需要将相应的壳面用节点和框架规划成一个闭合的多边形。\n点击该按钮可以选择巨构壳面的样式进行修建规划。";
                    item.ENUS = "In order to Plan a Megastructure Shell, it is necessary to plan the corresponding shell as a closed polygon with nodes and frames.\nClick this button to choose the Megastructure Shell style.";
                    item.FRFR = item.ENUS;
                }

                else if (proto.ID == 394) //规划完在球上点击时的显示，由于太长把英文的Megastructure改成了Structure
                {
                    var item = proto as StringProto;
                    item.ZHCN = "巨构节点 # {0}-{1}";
                    item.ENUS = "Structure Node # {0}-{1}"; //Megastructure Node # {0}-{1}
                    item.FRFR = item.ENUS;
                }

                else if (proto.ID == 772) //
                {
                    var item = proto as StringProto;
                    item.ZHCN = "巨构框架 # {0}-{1}";
                    item.ENUS = "Structure Frame # {0}-{1}"; //Megastructure Frame # {0}-{1}
                    item.FRFR = item.ENUS;
                }

                else if (proto.ID == 773) //
                {
                    var item = proto as StringProto;
                    item.ZHCN = "巨构壳面 # {0}-{1}";
                    item.ENUS = "Structure Shell # {0}-{1}"; //Megastructure Shell # {0}-{1}
                    item.FRFR = item.ENUS;
                }


                /*
                if (((StringProto)proto).ZHCN.Length >= 5 && ((StringProto)proto).ZHCN.Substring(0,5) == "戴森球节点")
                {
                    Console.WriteLine($"戴森球节点id={proto.ID},name={proto.name}, eng = {((StringProto)proto).ENUS}");
                }
                */
                
                
            }

        }

    }
}
