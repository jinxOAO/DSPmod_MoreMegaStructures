using HarmonyLib;
using System.Collections.Generic;
using xiaoye97;
using CommonAPI.Systems;
using CommonAPI.Systems.ModLocalization;
using UnityEngine;

// ReSharper disable once InconsistentNaming

#pragma warning disable CS0618 // Type or member is obsolete

namespace MoreMegaStructure
{
    internal static class MMSProtos
    {
        public static int StarCannonTechId = 1918;

        internal static void RefreshInitAll()
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

        internal static void AddNewItems()
        {
            int pagePlus = MoreMegaStructure.pagenum * 1000;
            int linePlus = 0;
            if (true || MoreMegaStructure.isBattleActive)
            {
                pagePlus = MoreMegaStructure.pagenum * 1000 + 100;
                linePlus = -100;
            }

            var oriRecipe0 = LDB.recipes.Select(51);
            var oriRecipe = oriRecipe0.Copy();
            if (MoreMegaStructure.GenesisCompatibility)
            {
                oriRecipe.Type = (ERecipeType)10;
            }

            var oriItem = LDB.items.Select(1303);
            int recipeIdBias = 0;
            if (MoreMegaStructure.GenesisCompatibility)
            {
                recipeIdBias = -200;
            }

            //引力发生装置
            var itemGravityGenRecipe = oriRecipe.Copy();
            var itemGravityGen = oriItem.Copy();
            itemGravityGenRecipe.ID = recipeIdBias + 530;
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
            ProtoRegistry.RegisterItem(9480, "引力发生装置".Translate(), "引力发生装置描述".Translate(), "Assets/MegaStructureTab/gravitygenerator",
                                       101 + pagePlus + linePlus, 100, EItemType.Component,
                                       ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.9f, 0.3f)));

            //位面约束环
            var itemConstrainRingRecipe = oriRecipe.Copy();
            var itemConstrainRing = oriItem.Copy();
            itemConstrainRingRecipe.ID = recipeIdBias + 531;
            itemConstrainRingRecipe.Name = "位面约束环";
            itemConstrainRingRecipe.name = "位面约束环".Translate();
            itemConstrainRingRecipe.Description = "位面约束环描述";
            itemConstrainRingRecipe.description = "位面约束环描述".Translate();
            itemConstrainRingRecipe.Items = new int[] { 1205, 1304 };
            itemConstrainRingRecipe.ItemCounts = new int[] { 2, 1 };
            if (MoreMegaStructure.GenesisCompatibility)
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
            ProtoRegistry.RegisterItem(9481, "位面约束环".Translate(), "位面约束环描述".Translate(), "Assets/MegaStructureTab/constrainring",
                                       102 + pagePlus + linePlus, 100, EItemType.Component,
                                       ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.4f, 0.08f, 0.4f)));


            //引力钻头
            var itemGravityDrillRecipe = oriRecipe.Copy();
            var itemGravityDrill = oriItem.Copy();
            itemGravityDrillRecipe.ID = recipeIdBias + 532;
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
            ProtoRegistry.RegisterItem(9482, "引力钻头".Translate(), "引力钻头描述".Translate(), "Assets/MegaStructureTab/gravitydrill2",
                                       103 + pagePlus + linePlus, 50, EItemType.Component,
                                       ProtoRegistry.GetDefaultIconDesc(Color.black, new Color(0.3f, 0.9f, 0.3f)));

            //隧穿激发装置
            var itemExciterRecipe = oriRecipe.Copy();
            var itemExciter = oriItem.Copy();
            itemExciterRecipe.ID = recipeIdBias + 533;
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
            ProtoRegistry.RegisterItem(9483, "隧穿激发装置".Translate(), "隧穿激发装置描述".Translate(), "Assets/MegaStructureTab/tunnelingexciter2",
                                       104 + pagePlus + linePlus, 200, EItemType.Component,
                                       ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.1f, 0.0f)));
            //谐振盘
            var itemDiscRecipe = oriRecipe.Copy();
            var itemDisc = oriItem.Copy();
            itemDiscRecipe.ID = recipeIdBias + 534;
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
            ProtoRegistry.RegisterItem(9484, "谐振盘".Translate(), "谐振盘描述".Translate(), "Assets/MegaStructureTab/resonancedisc",
                                       105 + pagePlus + linePlus, 200, EItemType.Component,
                                       ProtoRegistry.GetDefaultIconDesc(Color.gray, new Color(0.4f, 0.4f, 0.8f)));
            //光子探针
            var itemProbeRecipe = oriRecipe.Copy();
            var itemProbe = oriItem.Copy();
            itemProbeRecipe.ID = recipeIdBias + 535;
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
            ProtoRegistry.RegisterItem(9485, "光子探针".Translate(), "光子探针描述".Translate(), "Assets/MegaStructureTab/photonprobeflipsmall",
                                       106 + pagePlus + linePlus, 200, EItemType.Component, icondesc_alpha);

            //量子计算机
            var itemQuanCompRecipe = oriRecipe.Copy();
            var itemQuanComp = oriItem.Copy();
            itemQuanCompRecipe.ID = recipeIdBias + 536;
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
            ProtoRegistry.RegisterItem(9486, "量子计算机".Translate(), "量子计算机描述".Translate(), "Assets/MegaStructureTab/quantumcomputer3",
                                       107 + pagePlus + linePlus, 200, EItemType.Component, icondesc_alpha2);
            //星际组装厂组件
            var itemIACompoRecipe = oriRecipe.Copy();
            var itemIACompo = oriItem.Copy();
            itemIACompoRecipe.ID = recipeIdBias + 537;
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
            ProtoRegistry.RegisterItem(9487, "星际组装厂组件".Translate(), "星际组装厂组件描述".Translate(), "Assets/MegaStructureTab/iacomponent",
                                       108 + pagePlus + linePlus, 200, EItemType.Component,
                                       ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.7f, 0.2f, 0.7f)));

            //下面是火箭
            var oriRecipe2 = LDB.recipes.Select(83);
            var oriItem2 = LDB.items.Select(1503);
            //物质解压器运载火箭
            var rocketMDRecipe = oriRecipe2.Copy();
            var rocketMD = oriItem2.Copy();
            rocketMDRecipe.ID = recipeIdBias + 538;
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
            ProtoRegistry.RegisterItem(9488, "物质解压器运载火箭".Translate(), "物质解压器运载火箭描述".Translate(), "Assets/MegaStructureTab/rocketMatter",
                                       201 + pagePlus, 20, EItemType.Product,
                                       ProtoRegistry.GetDefaultIconDesc(new Color(1f, 0.9f, 0.9f), new Color(0.7f, 0.2f, 0.2f)));
            //科学枢纽运载火箭
            var rocketSNRecipe = oriRecipe2.Copy();
            var rocketSN = oriItem2.Copy();
            rocketSNRecipe.ID = recipeIdBias + 539;
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
            rocketSNRecipe.preTech = LDB.techs.Select(1508); //最终胜利科技
            if (MoreMegaStructure.isBattleActive) rocketSNRecipe.preTech = LDB.techs.Select(1924);
            if (MoreMegaStructure.GenesisCompatibility) rocketSNRecipe.preTech = LDB.techs.Select(1508);
            Traverse.Create(rocketSNRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconRocketScieN);
            ProtoRegistry.RegisterItem(9489, "科学枢纽运载火箭".Translate(), "科学枢纽运载火箭描述".Translate(), "Assets/MegaStructureTab/rocketScience",
                                       202 + pagePlus, 20, EItemType.Product,
                                       ProtoRegistry.GetDefaultIconDesc(new Color(1f, 1f, 0.9f), new Color(0.7f, 0.7f, 0.2f)));
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
            ProtoRegistry.RegisterItem(9490, "谐振发射器运载火箭".Translate(), "谐振发射器运载火箭描述".Translate(), "Assets/MegaStructureTab/rocketWarp", 203 + pagePlus,
                                       20, EItemType.Product,
                                       ProtoRegistry.GetDefaultIconDesc(new Color(0.9f, 1f, 0.9f), new Color(0.2f, 0.7f, 0.2f)));
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
            ProtoRegistry.RegisterItem(9491, "星际组装厂运载火箭".Translate(), "星际组装厂运载火箭描述".Translate(), "Assets/MegaStructureTab/rocketAssembly",
                                       204 + pagePlus, 20, EItemType.Product,
                                       ProtoRegistry.GetDefaultIconDesc(new Color(0.9f, 0.9f, 1f), new Color(0.1f, 0.5f, 0.7f)));
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
            ProtoRegistry.RegisterItem(9492, "晶体重构器运载火箭".Translate(), "晶体重构器运载火箭描述".Translate(), "Assets/MegaStructureTab/rocketCrystal",
                                       205 + pagePlus, 20, EItemType.Product,
                                       ProtoRegistry.GetDefaultIconDesc(new Color(1f, 0.9f, 1f), new Color(0.7f, 0.2f, 0.7f)));


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
            ProtoRegistry.RegisterItem(9500, "多功能集成组件".Translate(), "多功能集成组件描述".Translate(), "Assets/MegaStructureTab/integratedcomponents",
                                       109 + pagePlus + linePlus, 1000, EItemType.Component,
                                       ProtoRegistry.GetDefaultIconDesc(Color.white, Color.white));

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
            quickChemicalRecipe.ItemCounts = new int[] { 2 };
            quickChemicalRecipe.Results = new int[] { 2317 };
            quickChemicalRecipe.ResultCounts = new int[] { 1 };
            quickChemicalRecipe.GridIndex = 406 + pagePlus;
            quickChemicalRecipe.TimeSpend = 6;
            quickChemicalRecipe.preTech = LDB.techs.Select(1305);
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


            if (MoreMegaStructure.GenesisCompatibility)
            {
                quickReactorRecipe.preTech = LDB.techs.Select(1153);
                quickPowerRecipe.GridIndex = 9999;
                quickSmelterRecipe.GridIndex = 9999;
                quickAssemblyRecipe.GridIndex = 9999;
                quickChemicalRecipe.GridIndex = 9999;
                quickRefineryRecipe.GridIndex = 9999;
                quickColliderRecipe.GridIndex = 9999;
            }

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
            LDB.items.Select(2317).recipes.Add(quickChemicalRecipe);
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
                //LDBTool.SetBuildBar(13, 1, 9493);
                //LDBTool.SetBuildBar(13, 2, 9494);
                //LDBTool.SetBuildBar(13, 3, 9495);
                //LDBTool.SetBuildBar(13, 4, 9496);
                //LDBTool.SetBuildBar(13, 5, 9497);
                //LDBTool.SetBuildBar(13, 6, 9498);
                LDBTool.SetBuildBar(6, 9, 9512);
                //LDBTool.SetBuildBar(4, 4, 9499);
            }
        }

        public static void AddNewItems2()
        {
            int pagePlus = MoreMegaStructure.pagenum * 1000;
            //itemId 9513 available
            //recipeId 576 available
            int recipeIdBias = 0;
            int techPosXBias = 0;
            if (MoreMegaStructure.GenesisCompatibility)
            {
                recipeIdBias = -200;
                techPosXBias = -8;
            }
            //if(MoreMegaStructure.FECompatibility)
            //{
            //    recipeIdBias = -200;
            //}

            ProtoRegistry.RegisterItem(9503, "力场发生器", "力场发生器描述", "Assets/MegaStructureTab/forceGen", 201 + pagePlus, 20, EItemType.Component,
                                       ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.7f, 0.7f)));
            ProtoRegistry.RegisterItem(9504, "复合态晶体", "复合态晶体描述", "Assets/MegaStructureTab/compoCrystal", 202 + pagePlus, 100, EItemType.Component,
                                       ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.7f, 0.7f)));
            ProtoRegistry.RegisterItem(9505, "电磁力抑制器", "电磁力抑制器描述", "Assets/MegaStructureTab/elemaginhibitor2", 203 + pagePlus, 50,
                                       EItemType.Component, ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.7f, 0.7f)));
            ProtoRegistry.RegisterItem(9506, "胶子发生器", "胶子发生器描述", "Assets/MegaStructureTab/gluonGen", 204 + pagePlus, 50, EItemType.Component,
                                       ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.7f, 0.7f)));
            ProtoRegistry.RegisterItem(9507, "强力过载装置", "强力过载装置描述", "Assets/MegaStructureTab/strIntOverloader", 205 + pagePlus, 20,
                                       EItemType.Component, ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.7f, 0.7f)));
            ProtoRegistry.RegisterItem(9508, "导流框架", "导流框架描述", "Assets/MegaStructureTab/starcannonframe", 206 + pagePlus, 20, EItemType.Component,
                                       ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.2f, 0.7f, 0.7f)));
            ProtoRegistry.RegisterItem(9509, "恒星炮组件", "恒星炮组件描述", "Assets/MegaStructureTab/starcannoncompo", 207 + pagePlus, 20, EItemType.Component,
                                       ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.3f, 0.3f, 0.9f)));
            ProtoRegistry.RegisterItem(9510, "恒星炮运载火箭", "恒星炮运载火箭描述", "Assets/MegaStructureTab/rocketStarcannon", 306 + pagePlus, 20,
                                       EItemType.Product, ProtoRegistry.GetDefaultIconDesc(Color.white, new Color(0.3f, 0.9f, 0.9f)));

            ItemProto dropletItem = ProtoRegistry.RegisterItem(9511, "水滴gm", "水滴描述gm", "Assets/MegaStructureTab/drop1", 707 + pagePlus, 10,
                                                               EItemType.Product, ProtoRegistry.GetDefaultIconDesc(Color.white, Color.white));
            // 下面这些由深空来敌自行完成，深空来敌必须new一个新的prefabDesc!!!否则会改了别的物品
            //dropletItem.DescFields = new int[] { 81, 82, 80, 59, 11, 1 };
            //dropletItem.AmmoType = EAmmoType.Bullet;
            //if (dropletItem.prefabDesc == null)
            //    dropletItem.prefabDesc = new PrefabDesc();
            //dropletItem.prefabDesc.isCraftUnit = true;
            //dropletItem.prefabDesc.craftUnitMaxMovementSpeed = 30000;
            //dropletItem.prefabDesc.workEnergyPerTick = 500000;

            ProtoRegistry.RegisterRecipe(565 + recipeIdBias, ERecipeType.Assemble, 240, new int[] { 9480, 9484 }, new int[] { 1, 2 }, new int[] { 9503 },
                                         new int[] { 1 }, "力场发生器描述", StarCannonTechId, 201 + pagePlus, "Assets/MegaStructureTab/forceGen");
            RecipeProto SIMRecipe = ProtoRegistry.RegisterRecipe(566 + recipeIdBias, ERecipeType.Particle, 600, new int[] { 5203, 1126, 1124, 1118, 1120 },
                                         new int[] { 1, 1, 1, 1, 1 }, new int[] { 9504 }, new int[] { 1 }, "复合态晶体描述", 1919, 202 + pagePlus, "Assets/MegaStructureTab/compoCrystal");
            ProtoRegistry.RegisterRecipe(567 + recipeIdBias, ERecipeType.Assemble, 360, new int[] { 1305, 1205 }, new int[] { 1, 2 }, new int[] { 9505 },
                                         new int[] { 1 }, "电磁力抑制器描述", 1919, 203 + pagePlus, "Assets/MegaStructureTab/elemaginhibitor2");
            RecipeProto gluonGenRecipe = ProtoRegistry.RegisterRecipe(568 + recipeIdBias, ERecipeType.Assemble, 360, new int[] { 9483, 1402, 1122 },
                                         new int[] { 1, 1, 2 }, new int[] { 9506 }, new int[] { 1 }, "胶子发生器描述", 1919, 204 + pagePlus, "Assets/MegaStructureTab/gluonGen");
            RecipeProto strIntOverloaderRecipe = ProtoRegistry.RegisterRecipe(569 + recipeIdBias, ERecipeType.Assemble, 1200, new int[] { 9506, 9486 },
                                         new int[] { 2, 2 }, new int[] { 9507 }, new int[] { 1 }, "强力过载装置描述", 1919, 205 + pagePlus, "Assets/MegaStructureTab/strIntOverloader");
            ProtoRegistry.RegisterRecipe(570 + recipeIdBias, ERecipeType.Assemble, 180, new int[] { 1125, 9481, 9503 }, new int[] { 3, 2, 3 }, new int[] { 9508 },
                                         new int[] { 1 }, "导流框架描述", StarCannonTechId, 206 + pagePlus, "Assets/MegaStructureTab/starcannonframe");
            ProtoRegistry.RegisterRecipe(571 + recipeIdBias, ERecipeType.Assemble, 480, new int[] { 1209, 9508 }, new int[] { 3, 2 }, new int[] { 9509 },
                                         new int[] { 1 }, "恒星炮组件描述", StarCannonTechId, 207 + pagePlus, "Assets/MegaStructureTab/starcannoncompo");
            ProtoRegistry.RegisterRecipe(572 + recipeIdBias, ERecipeType.Assemble, 360, new int[] { 9509, 1802, 1305 }, new int[] { 2, 2, 2 }, new int[] { 9510 },
                                         new int[] { 1 }, "恒星炮运载火箭描述", StarCannonTechId, 306 + pagePlus, "Assets/MegaStructureTab/rocketStarcannon");
            RecipeProto dropRecipe = ProtoRegistry.RegisterRecipe(573 + recipeIdBias, ERecipeType.Assemble, 3600, new int[] { 9505, 9507, 9504 },
                                                                  new int[] { 20, 20, 100 }, new int[] { 9511 }, new int[] { 1 }, "水滴描述gm", 1919,
                                                                  707 + pagePlus, "Assets/MegaStructureTab/drop1");
            //1918
            TechProto techStarCannon = ProtoRegistry.RegisterTech(StarCannonTechId, "尼科尔戴森光束", "尼科尔戴森光束描述", "尼科尔戴森光束结论", "Assets/MegaStructureTab/starcannontech", new int[] { }, new int[] { 5201 }, new int[] { 200 }, 36000, new int[] { 570 + recipeIdBias, 571 + recipeIdBias, 572 + recipeIdBias, 565 + recipeIdBias }, new Vector2(65 + techPosXBias, -3)); // 原本解锁有个823不记得是什么了
            techStarCannon.PreTechsImplicit = new int[] { 1522 };
            techStarCannon.IsHiddenTech = true;
            techStarCannon.PreItem = new int[] { 5201 };

            SIMRecipe.Handcraft = false;
            gluonGenRecipe.Handcraft = false;
            strIntOverloaderRecipe.Handcraft = false;
            dropRecipe.Handcraft = false;

        }

        public static void EditOriRR(Proto proto)
        {            
            if (proto is ItemProto && proto.ID == 2208)
            {
                var rr = proto as ItemProto;
                rr.Upgrades = new int[] { 2208, 9493, 9494, 9495, 9496, 9497, 9501, 9498, 9502 };
                rr.Grade = 1;
            }
        }

        public static void AddGenesisRecipes()
        {
            if (!MoreMegaStructure.GenesisCompatibility) return;

            int pagePlus = MoreMegaStructure.pagenum * 1000 + 100;
            var recipe376 = ProtoRegistry.RegisterRecipe(376, (ERecipeType)10, 6, new int[] { 9500 }, new int[] { 25 }, new int[] { 6257 },
                                                         new int[] { 1 }, "巨建快速组装描述", 1923, 403 + pagePlus, "Assets/MegaStructureTab/quick6257");
            var recipe377 = ProtoRegistry.RegisterRecipe(377, (ERecipeType)10, 6, new int[] { 9500 }, new int[] { 25 }, new int[] { 6258 },
                                                         new int[] { 1 }, "巨建快速组装描述", 1924, 404 + pagePlus, "Assets/MegaStructureTab/quick6258");
            var recipe378 = ProtoRegistry.RegisterRecipe(378, (ERecipeType)10, 6, new int[] { 9500 }, new int[] { 25 }, new int[] { 6259 },
                                                         new int[] { 1 }, "巨建快速组装描述", 1925, 405 + pagePlus, "Assets/MegaStructureTab/quick6259");
            var recipe379 = ProtoRegistry.RegisterRecipe(379, (ERecipeType)10, 6, new int[] { 9500 }, new int[] { 25 }, new int[] { 6260 },
                                                         new int[] { 1 }, "巨建快速组装描述", 1926, 406 + pagePlus, "Assets/MegaStructureTab/quick6260");
            var recipe380 = ProtoRegistry.RegisterRecipe(380, (ERecipeType)10, 6, new int[] { 9500 }, new int[] { 25 }, new int[] { 6264 },
                                                         new int[] { 1 }, "巨建快速组装描述", 1931, 407 + pagePlus, "Assets/MegaStructureTab/quick6264");
            var recipe381 = ProtoRegistry.RegisterRecipe(381, (ERecipeType)10, 6, new int[] { 9500 }, new int[] { 25 }, new int[] { 6265 },
                                                         new int[] { 1 }, "巨建快速组装描述", 1927, 408 + pagePlus, "Assets/MegaStructureTab/quick6265");

            recipe376._iconSprite = Resources.Load<Sprite>("Assets/MegaStructureTab/quick6257");
            recipe377._iconSprite = Resources.Load<Sprite>("Assets/MegaStructureTab/quick6258");
            recipe378._iconSprite = Resources.Load<Sprite>("Assets/MegaStructureTab/quick6259");
            recipe379._iconSprite = Resources.Load<Sprite>("Assets/MegaStructureTab/quick6260");
            recipe380._iconSprite = Resources.Load<Sprite>("Assets/MegaStructureTab/quick6264");
            recipe381._iconSprite = Resources.Load<Sprite>("Assets/MegaStructureTab/quick6265");

            LDBTool.PostAddProto(recipe376);
            LDBTool.PostAddProto(recipe377);
            LDBTool.PostAddProto(recipe378);
            LDBTool.PostAddProto(recipe379);
            LDBTool.PostAddProto(recipe380);
            LDBTool.PostAddProto(recipe381);
        }

        public static void AddReceivers()
        {
            int pagePlus = MoreMegaStructure.pagenum * 1000;
            if (true || MoreMegaStructure.isBattleActive)
            {
                pagePlus = MoreMegaStructure.battlePagenum * 1000 + 100;
            }

            int recipeIdBias = 0;
            if (MoreMegaStructure.GenesisCompatibility)
            {
                recipeIdBias = -200;
            }

            //下面是接收器 以及 新的物流塔
            var oriRecipe3 = LDB.recipes.Select(41);
            var oriItem3 = LDB.items.Select(2208);
            var oriLogisticStation = LDB.items.Select(2104);
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
            if (MoreMegaStructure.GenesisCompatibility)
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
            if (MoreMegaStructure.isBattleActive) ReceiverIronRecipe.preTech = LDB.techs.Select(1920);
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
            ReceiverIron.Grade = 2;
            ReceiverIron.Upgrades = new int[] { 2208, 9493, 9494, 9495, 9496, 9497, 9501, 9498, 9502 };
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
            if (MoreMegaStructure.isBattleActive) ReceiverCopperRecipe.preTech = LDB.techs.Select(1920);
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
            ReceiverCopper.Grade = 3;
            ReceiverCopper.Upgrades = new int[] { 2208, 9493, 9494, 9495, 9496, 9497, 9501, 9498, 9502 };
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
            if (MoreMegaStructure.isBattleActive) ReceiverSiliconRecipe.preTech = LDB.techs.Select(1920);
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
            ReceiverSilicon.Grade = 4;
            ReceiverSilicon.Upgrades = new int[] { 2208, 9493, 9494, 9495, 9496, 9497, 9501, 9498, 9502 };
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
            if (MoreMegaStructure.isBattleActive) ReceiverTitaniumRecipe.preTech = LDB.techs.Select(1920);
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
            ReceiverTitanium.Grade = 5;
            ReceiverTitanium.Upgrades = new int[] { 2208, 9493, 9494, 9495, 9496, 9497, 9501, 9498, 9502 };
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
            if (MoreMegaStructure.isBattleActive) ReceiverMagoreRecipe.preTech = LDB.techs.Select(1920);
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
            ReceiverMagore.Grade = 6;
            ReceiverMagore.Upgrades = new int[] { 2208, 9493, 9494, 9495, 9496, 9497, 9501, 9498, 9502 };
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
            if (MoreMegaStructure.isBattleActive) ReceiverCoalRecipe.preTech = LDB.techs.Select(1920);
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
            ReceiverCoal.Grade = 7;
            ReceiverCoal.Upgrades = new int[] { 2208, 9493, 9494, 9495, 9496, 9497, 9501, 9498, 9502 };
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
            if (MoreMegaStructure.isBattleActive) ReceiverCasimirRecipe.preTech = LDB.techs.Select(1923);
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
            ReceiverCasimir.Grade = 8;
            ReceiverCasimir.Upgrades = new int[] { 2208, 9493, 9494, 9495, 9496, 9497, 9501, 9498, 9502 };
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
            if (MoreMegaStructure.isBattleActive) ReceiverGratingRecipe.preTech = LDB.techs.Select(1923);
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
            ReceiverGrating.Grade = 9;
            ReceiverGrating.Upgrades = new int[] { 2208, 9493, 9494, 9495, 9496, 9497, 9501, 9498, 9502 };
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
            ReceiverICRecipe.GridIndex = 9902 + pagePlus;
            ReceiverICRecipe.TimeSpend = 480;
            Traverse.Create(ReceiverICRecipe).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverIC);
            ReceiverICRecipe.preTech = LDB.techs.Select(1504); //射线接收站科技 
            if (MoreMegaStructure.isBattleActive) ReceiverICRecipe.preTech = LDB.techs.Select(1922);
            if (MoreMegaStructure.GenesisCompatibility) ReceiverICRecipe.preTech = LDB.techs.Select(1504); //射线接收站科技 
            ReceiverIC.ID = 9499;
            ReceiverIC.Name = "组件集成装置";
            ReceiverIC.name = "组件集成装置".Translate();
            ReceiverIC.Description = "组件集成装置描述";
            ReceiverIC.description = "组件集成装置描述".Translate();
            ReceiverIC.GridIndex = 9902 + pagePlus;
            ReceiverIC.HeatValue = 0L;
            ReceiverIC.prefabDesc = oriItem3.prefabDesc.Copy();
            ReceiverIC.prefabDesc.powerProductHeat = MoreMegaStructure.multifunctionComponentHeat;
            ReceiverIC.prefabDesc.powerProductId = 9500;
            ReceiverIC.handcraft = ReceiverICRecipe;
            ReceiverIC.handcrafts = new List<RecipeProto> { ReceiverICRecipe };
            ReceiverIC.maincraft = ReceiverICRecipe;
            ReceiverIC.recipes = new List<RecipeProto> { ReceiverICRecipe };
            //ReceiverIC.makes = new List<RecipeProto> { ReceiverICRecipe };
            Traverse.Create(ReceiverIC).Field("_iconSprite").SetValue(MoreMegaStructure.iconReceiverIC);


            //Exchanger
            var ExchangerRecipe = oriRecipe3.Copy();
            var Exchanger = oriLogisticStation.Copy();
            ExchangerRecipe.ID = recipeIdBias + 575;
            ExchangerRecipe.Name = "物资交换器";
            ExchangerRecipe.name = "物资交换器".Translate();
            ExchangerRecipe.Description = "物资交换器描述";
            ExchangerRecipe.description = "物资交换器描述".Translate();
            ExchangerRecipe.Items = new int[] { 2104, 2208 };
            ExchangerRecipe.ItemCounts = new int[] { 1, 1 };
            ExchangerRecipe.Results = new int[] { 9512 };
            ExchangerRecipe.ResultCounts = new int[] { 1 };
            ExchangerRecipe.GridIndex = 309 + pagePlus;
            ExchangerRecipe.TimeSpend = 480;
            Traverse.Create(ExchangerRecipe).Field("_iconSprite").SetValue(Resources.Load<Sprite>("Assets/MegaStructureTab/exchangeLS"));
            ExchangerRecipe.preTech = LDB.techs.Select(1504); //射线接收站科技
            if (MoreMegaStructure.isBattleActive) ExchangerRecipe.preTech = LDB.techs.Select(1922);
            Exchanger.ID = 9512;
            Exchanger.Name = "物资交换器";
            Exchanger.name = "物资交换器".Translate();
            Exchanger.Description = "物资交换器描述";
            Exchanger.description = "物资交换器描述".Translate();
            Exchanger.GridIndex = 309 + pagePlus;
            Exchanger.HeatValue = 0L;
            Exchanger.prefabDesc = oriLogisticStation.prefabDesc.Copy();
            Exchanger.handcraft = ExchangerRecipe;
            Exchanger.handcrafts = new List<RecipeProto> { ExchangerRecipe };
            Exchanger.maincraft = ExchangerRecipe;
            Exchanger.recipes = new List<RecipeProto> { ExchangerRecipe };
            //Exchanger.makes = new List<RecipeProto> { ExchangerRecipe };
            Traverse.Create(Exchanger).Field("_iconSprite").SetValue(Resources.Load<Sprite>("Assets/MegaStructureTab/exchangeLS"));


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
            LDBTool.PostAddProto(Exchanger);
            LDBTool.PostAddProto(ExchangerRecipe);
        }

        public class StringProto
        {
            public string Name { get; set; }
            public string ZHCN { get; set; }
            public string ENUS { get; set; }

            public void RegisterTranslation() => LocalizationModule.RegisterTranslation(Name, ENUS, ZHCN, "");
        }

        public static void AddTranslateUILabel()
        {
            new StringProto { Name = "巨构建筑", ZHCN = "巨构建筑", ENUS = "Megastructure" }.RegisterTranslation();
            new StringProto { Name = "规划巨构建筑类型", ZHCN = "规划巨构建筑类型", ENUS = "Plan Megastructure" }.RegisterTranslation();
            new StringProto { Name = "自由组件", ZHCN = "自由组件", ENUS = "Floating components " }.RegisterTranslation();
            new StringProto { Name = "工作效率", ZHCN = "工作效率", ENUS = "Capacity" }.RegisterTranslation();
            new StringProto { Name = "自由组件数量", ZHCN = "自由组件数量", ENUS = "Floating components in total" }.RegisterTranslation();
            new StringProto { Name = "自由组件云", ZHCN = "自由组件云", ENUS = "Components Swarm" }.RegisterTranslation();
            new StringProto { Name = "组件云蓝图", ZHCN = "组件云蓝图", ENUS = "Swarm Blueprint" }.RegisterTranslation();
            new StringProto { Name = "锚定结构", ZHCN = "锚定结构", ENUS = "Anchored Structure" }.RegisterTranslation();
            new StringProto { Name = "结构层级", ZHCN = "结构层级", ENUS = "Structure Layers" }.RegisterTranslation();
            new StringProto { Name = "锚定结构蓝图", ZHCN = "锚定结构蓝图", ENUS = "Anchored Structure Blueprint" }.RegisterTranslation();
            new StringProto { Name = "恒星功效系数", ZHCN = "恒星功效系数", ENUS = "Star Efficiency" }.RegisterTranslation();
            new StringProto { Name = "最大工作效率", ZHCN = "最大工作效率", ENUS = "Capacity" }.RegisterTranslation();
            new StringProto { Name = "巨构建筑蓝图", ZHCN = "巨构建筑蓝图", ENUS = "Structure Blueprint" }.RegisterTranslation();
            new StringProto { Name = "自由组件寿命分布", ZHCN = "自由组件寿命分布", ENUS = "Life Distribution of Floating Components" }.RegisterTranslation();
            new StringProto { Name = "自由组件状态统计", ZHCN = "自由组件状态统计", ENUS = "Floating Components Status Statistics" }.RegisterTranslation();
            new StringProto { Name = "自由组件工作效率", ZHCN = "自由组件工作效率", ENUS = "Generation of Floating Components" }.RegisterTranslation();
            new StringProto { Name = "锚定结构工作效率", ZHCN = "锚定结构工作效率", ENUS = "Generation of Anchored Structure" }.RegisterTranslation();
            new StringProto { Name = "研究效率", ZHCN = "研究效率", ENUS = "Research Capacity" }.RegisterTranslation();
            new StringProto { Name = "折跃场加速", ZHCN = "折跃场加速", ENUS = "Warp Acceleration" }.RegisterTranslation();

            new StringProto { Name = "切换快捷键", ZHCN = "CapsLock\n↑快捷键切换↓", ENUS = "CapsLock\n↑ Hotkey Row ↓" }.RegisterTranslation();
        }

        public static void AddTranslateStructureName()
        {
            new StringProto { Name = "规划", ZHCN = "规划", ENUS = "Plan " }.RegisterTranslation();

            new StringProto { Name = "戴森球jinx", ZHCN = "戴森球", ENUS = "Dyson Sphere" }.RegisterTranslation();

            new StringProto { Name = "物质解压器", ZHCN = "物质解压器", ENUS = "Matter Decompressor" }.RegisterTranslation();


            new StringProto { Name = "科学枢纽", ZHCN = "科学枢纽", ENUS = "Science Nexus" }.RegisterTranslation();


            new StringProto { Name = "折跃场广播阵列", ZHCN = "折跃场广播阵列", ENUS = "Warp Field Broadcast Array" }.RegisterTranslation();


            new StringProto { Name = "星际组装厂", ZHCN = "星际组装厂", ENUS = "Interstellar Assembly" }.RegisterTranslation();

            new StringProto { Name = "晶体重构器", ZHCN = "晶体重构器", ENUS = "Crystal Reconstructor" }.RegisterTranslation();

            new StringProto
            {
                Name = "警告最多一个", ZHCN = "折跃场广播阵列最多建造一个，请检查星系:", ENUS = "You can only build one Wrapfield broadcast array, please check:"
            }.RegisterTranslation();

            new StringProto
            {
                Name = "警告先拆除",
                ZHCN = "你必须先拆除所有锚定结构（节点）再规划不同的巨构建筑。",
                ENUS = "You have to remove all anchor structures (nodes) before planning different megastructures."
            }.RegisterTranslation();

            new StringProto { Name = "警告仅黑洞", ZHCN = "物质解压器只能在黑洞上建造。", ENUS = "Matter decompressors can only be built on black holes." }
               .RegisterTranslation();

            new StringProto
            {
                Name = "警告仅中子星白矮星",
                ZHCN = "晶体重构器只能在中子星或白矮星上建造。",
                ENUS = "Crystal reconstructors can only be built on neutron stars or white drawf."
            }.RegisterTranslation();


            new StringProto { Name = "当前", ZHCN = "当前", ENUS = "Currently" }.RegisterTranslation();

            new StringProto { Name = "警告未知错误", ZHCN = "设置异常失败，请像mod作者反馈该问题。", ENUS = "The setting fails abnormally, please report this problem." }
               .RegisterTranslation();

            new StringProto { Name = "MegaStructures", ZHCN = "巨构", ENUS = "Megastructures" }.RegisterTranslation();

            new StringProto { Name = "物质合成", ZHCN = "物质合成", ENUS = "Substance generation" }.RegisterTranslation();

            new StringProto { Name = "恒星炮", ZHCN = "恒星炮", ENUS = "Star cannon" }.RegisterTranslation();

            new StringProto { Name = "警告最多一个恒星炮", ZHCN = "恒星炮最多建造一个，请检查星系:", ENUS = "You can only build one Star cannon, please check:" }
               .RegisterTranslation();
        }

        public static void AddTranslateProtoNames1()
        {
            new StringProto { Name = "引力发生装置", ZHCN = "引力发生装置", ENUS = "Gravity generator" }.RegisterTranslation();

            new StringProto
            {
                Name = "引力发生装置描述",
                ZHCN = "引导临界光子轰击奇异物质即可激发引力波。恒星附近能够获取大量的临界光子，从而能够使引力发生装置高效地运行。",
                ENUS
                    = "Gravitational waves can be excited by directing critical photons to hit strange matter. A large number of critical photons can be obtained near the star, allowing the gravitational generator to operate efficiently."
            }.RegisterTranslation();


            new StringProto { Name = "位面约束环", ZHCN = "位面约束环", ENUS = "Plane constraint ring" }.RegisterTranslation();


            new StringProto
            {
                Name = "位面约束环描述",
                ZHCN = "位面约束环能够协同引力透镜引导并操纵引力，也是构建科学枢纽所需的恒星级粒子加速器的必要组件。",
                ENUS
                    = "Plane constraint ring can guide and manipulate gravity with graviton lens, and it is also an essential component of the stellar-scale particle accelerators which are needed to build science nexus."
            }.RegisterTranslation();


            new StringProto { Name = "引力钻头", ZHCN = "引力钻头", ENUS = "Graviton drill" }.RegisterTranslation();


            new StringProto
            {
                Name = "引力钻头描述",
                ZHCN = "借助黑洞本身的引力，引力钻头能够将物质从黑洞中取出，这还包括吸积盘中大量的单极磁石。借助谐振盘，黑洞原质将能够被解压并在星系内输送。",
                ENUS
                    = "The graviton drill can pull matter out of the black hole using the gravity of the black hole itself, which also includes the unipolar magnets in the accretion disk. With the help of the resonant disc, the matter from the black hole will be able to be decompressed and transported within the galaxy."
            }.RegisterTranslation();

            new StringProto { Name = "隧穿激发装置", ZHCN = "隧穿激发装置", ENUS = "Tunneling exciter" }.RegisterTranslation();

            new StringProto
            {
                Name = "隧穿激发装置描述",
                ZHCN = "隧穿激发装置可以完美地掌控量子隧穿效应，常被用来强化量子芯片的处理能力和纠错能力。通过量子隧穿效应还能够轻易突破弯曲空间的能量势垒，使得在任意远的空间打开裂口成为可能。",
                ENUS
                    = "Tunneling exciters can perfectly control the quantum tunneling effect, and are often used to enhance the processing and error correction capabilities of quantum chips. The quantum tunneling effect can also easily break through the energy barrier of the curved space, making it possible to open the warp crack in any space far away."
            }.RegisterTranslation();

            new StringProto { Name = "谐振盘", ZHCN = "谐振盘", ENUS = "Resonant disc" }.RegisterTranslation();

            new StringProto
            {
                Name = "谐振盘描述",
                ZHCN = "谐振盘仅通过恒星级别的能量就可以产生跨越恒星系的空间波动能量束。如果将谐振盘组成阵列，理论上可以形成覆盖全宇宙的折跃能量场。",
                ENUS
                    = "The resonant disc can generate interstellar-scale space-wave energy beams from only stellar-scale energy. If the resonant discs are formed into an array, a warp field covering the entire universe can theoretically be formed."
            }.RegisterTranslation();

            new StringProto { Name = "光子探针", ZHCN = "光子探针", ENUS = "Photon probe" }.RegisterTranslation();

            new StringProto
            {
                Name = "光子探针描述",
                ZHCN = "将临界光子变频后发射并引导晶体重构，发射的光子还能被回收。",
                ENUS
                    = "The critical photons are frequency-converted and emitted, thereby guiding the crystal reconstruction. The photons can also be recovered."
            }.RegisterTranslation();

            new StringProto { Name = "量子计算机", ZHCN = "量子计算机", ENUS = "Quantum computer" }.RegisterTranslation();

            new StringProto
            {
                Name = "量子计算机描述",
                ZHCN = "只要供给足够的能量，量子计算机的运算时钟能够无限逼近普朗克时间。通过量子比特协同，其潜在的单线程运算速率还能突破物理极限，并可以无限提升。现在，限制其计算速度的将只有能量输入水平。",
                ENUS
                    = "As long as enough energy is supplied, the computing clock of a quantum computer can approach Planck time indefinitely. Through the cooperation of qubits, its potential single-threaded operation rate can also break through the physical limit and can be infinitely improved. Now, it will only be the level of energy input that will limit its computational speed."
            }.RegisterTranslation();

            new StringProto { Name = "星际组装厂组件", ZHCN = "星际组装厂组件", ENUS = "Interstellar assembly component" }.RegisterTranslation();

            new StringProto
            {
                Name = "星际组装厂组件描述",
                ZHCN = "使用微型火箭将组件运载到恒星附近并构建星际组装厂的节点和框架。",
                ENUS = "Use a small carrier rocket to the planned Interstellar assembly to form the nodes and frames of Interstellar assembly."
            }.RegisterTranslation();

            new StringProto { Name = "物质解压器运载火箭", ZHCN = "物质解压器运载火箭", ENUS = "Matter decompressor carrier rocket" }.RegisterTranslation();

            new StringProto
            {
                Name = "物质解压器运载火箭描述", ZHCN = "物质解压器相关组件的运载工具。", ENUS = "The delivery vehicle for the components of the Matter decompressor."
            }.RegisterTranslation();

            new StringProto { Name = "科学枢纽运载火箭", ZHCN = "科学枢纽运载火箭", ENUS = "Science nexus carrier rocket" }.RegisterTranslation();

            new StringProto { Name = "科学枢纽运载火箭描述", ZHCN = "科学枢纽相关组件的运载工具。", ENUS = "The delivery vehicle for the components of the Science nexus." }
               .RegisterTranslation();
        }

        public static void AddTranslateProtoNames2()
        {
            new StringProto { Name = "谐振发射器运载火箭", ZHCN = "谐振发射器运载火箭", ENUS = "Resonant generator carrier rocket" }.RegisterTranslation();

            new StringProto
            {
                Name = "谐振发射器运载火箭描述",
                ZHCN = "大量谐振发射器将组成阵列并向全星系广播折跃能量场。",
                ENUS = "A large number of resonant generators will form an array and broadcast the warp energy field to the entire galaxy."
            }.RegisterTranslation();


            new StringProto { Name = "星际组装厂运载火箭", ZHCN = "星际组装厂运载火箭", ENUS = "Interstellar assembly carrier rocket" }.RegisterTranslation();


            new StringProto { Name = "星际组装厂运载火箭描述", ZHCN = "星际组装厂组件的运载工具。", ENUS = "The delivery vehicle of Interstellar assembly components." }
               .RegisterTranslation();


            new StringProto { Name = "晶体重构器运载火箭", ZHCN = "晶体重构器运载火箭", ENUS = "Crystal reconstructor carrier rocket" }.RegisterTranslation();


            new StringProto
            {
                Name = "晶体重构器运载火箭描述", ZHCN = "晶体重构器相关组件的运载工具。", ENUS = "The delivery vehicle for the components of the Crystal reconstructor."
            }.RegisterTranslation();

            new StringProto { Name = "铁金属重构装置", ZHCN = "铁金属重构装置", ENUS = "Iron reconstruct receiver" }.RegisterTranslation();

            new StringProto { Name = "铜金属重构装置", ZHCN = "铜金属重构装置", ENUS = "Copper reconstruct receiver" }.RegisterTranslation();

            new StringProto { Name = "高纯硅重构装置", ZHCN = "高纯硅重构装置", ENUS = "Silicon reconstruct receiver" }.RegisterTranslation();

            new StringProto { Name = "钛金属重构装置", ZHCN = "钛金属重构装置", ENUS = "Titanium reconstruct receiver" }.RegisterTranslation();

            new StringProto { Name = "单极磁石重构装置", ZHCN = "单极磁石重构装置", ENUS = "Unipolar magnet receiver" }.RegisterTranslation();

            new StringProto
            {
                Name = "接收重构装置描述",
                ZHCN = "从黑洞中解压出的亚稳态物质被接收后经过处理，重构为可直接使用的稳定材料。",
                ENUS
                    = "The metastable matter decompressed from the black hole is received, processed, and reconstructed into stable material that can be used directly."
            }.RegisterTranslation();

            new StringProto { Name = "晶体接收器", ZHCN = "晶体接收器", ENUS = "Crystal receiver" }.RegisterTranslation();

            new StringProto
            {
                Name = "晶体接收器描述",
                ZHCN = "从晶体重构器中合成的卡西米尔晶体前导微晶流将在此经过自发β衰变并形成完美的卡西米尔晶体。接收器也可以转而富集该过程的副产物——光栅石。",
                ENUS
                    = "The Casimir crystal precursor crystallite flow synthesized from the Crystal reconstructor will undergo spontaneous β decay here and form perfect casimir crystals. The receivers can also in turn enrich for optical grating crystals, the by-product of the process."
            }.RegisterTranslation();

            new StringProto { Name = "组件集成装置", ZHCN = "组件集成装置", ENUS = "Component integration station" }.RegisterTranslation();

            new StringProto
            {
                Name = "组件集成装置描述",
                ZHCN = "将星际组装厂的高集成配件进行预解压，形成可被快速组装的多功能集成组件。",
                ENUS
                    = "Pre-decompress the high-integration parts from the Interstellar assembly, to form multi-functional integrated components that can be quickly assembled."
            }.RegisterTranslation();

            new StringProto { Name = "多功能集成组件", ZHCN = "多功能集成组件", ENUS = "Multi-functional integrated components" }.RegisterTranslation();

            new StringProto
            {
                Name = "多功能集成组件描述",
                ZHCN = "超高集成度使其可以迅速地组装成多种生产建筑和物流组件，却仅占用极小的空间。",
                ENUS
                    = "The high level of integration makes it possible to quickly assemble a variety of production building and logistics components, while occupying very little space."
            }.RegisterTranslation();

            new StringProto { Name = "光栅晶体接收器", ZHCN = "光栅晶体接收器", ENUS = "Optical crystal receiver" }.RegisterTranslation();

            new StringProto { Name = "石墨提炼装置", ZHCN = "石墨提炼装置", ENUS = "Graphite extraction receiver" }.RegisterTranslation();
        }

        public static void AddTranslateProtoNames3()
        {
            new StringProto { Name = "传送带 快速组装", ZHCN = "传送带 快速组装", ENUS = "Conveyor belt - quick assembly" }.RegisterTranslation();

            new StringProto { Name = "分拣器 快速组装", ZHCN = "分拣器 快速组装", ENUS = "Sorter - quick assembly" }.RegisterTranslation();


            new StringProto { Name = "配电站 快速组装", ZHCN = "配电站 快速组装", ENUS = "Substation - quick assembly" }.RegisterTranslation();


            new StringProto { Name = "制造台 快速组装", ZHCN = "制造台 快速组装", ENUS = "Assembling machine - quick assembly" }.RegisterTranslation();


            new StringProto { Name = "位面熔炉 快速组装", ZHCN = "位面熔炉 快速组装", ENUS = "Plane smelter - quick assembly" }.RegisterTranslation();


            new StringProto { Name = "化工厂 快速组装", ZHCN = "化工厂 快速组装", ENUS = "Chemical plant - quick assembly" }.RegisterTranslation();

            new StringProto { Name = "精炼厂 快速组装", ZHCN = "精炼厂 快速组装", ENUS = "Refinery - quick assembly" }.RegisterTranslation();

            new StringProto { Name = "对撞机 快速组装", ZHCN = "对撞机 快速组装", ENUS = "Collider - quick assembly" }.RegisterTranslation();

            new StringProto { Name = "研究站 快速组装", ZHCN = "研究站 快速组装", ENUS = "Lab - quick assembly" }.RegisterTranslation();

            new StringProto { Name = "人造恒星 快速组装", ZHCN = "人造恒星 快速组装", ENUS = "Artificial star - quick assembly" }.RegisterTranslation();

            new StringProto { Name = "行星内物流 快速组装", ZHCN = "行星内物流 快速组装", ENUS = "Planetary logistics - quick assembly" }.RegisterTranslation();

            new StringProto { Name = "星际物流 快速组装", ZHCN = "星际物流 快速组装", ENUS = "Interstellar logistics - quick assembly" }.RegisterTranslation();

            new StringProto
            {
                Name = "快速组装描述",
                ZHCN = "使用多功能集成组件快速递组装成目标物品。",
                ENUS = "Quickly assemble target items using multi-functional integrated components."
            }.RegisterTranslation();
        }

        public static void AddTranslateProtoNames4()
        {
            LocalizationModule.RegisterTranslation("每秒伤害gm", "Base Damage", "基础伤害", "");
            LocalizationModule.RegisterTranslation("最大生产速度gm", "Production speed", "生产速度", "");
            LocalizationModule.RegisterTranslation("阶段", "stage", "阶段", "");
            LocalizationModule.RegisterTranslation("连续开火次数", "Aim targets", "射击目标数", "");
            LocalizationModule.RegisterTranslation("最大射程", "Maximum fire range", "最大射程", "");
            LocalizationModule.RegisterTranslation("伤害削减", "Damage reduction", "伤害削减", "");
            LocalizationModule.RegisterTranslation("当前能量水平", "Current capacity", "当前能量水平", "");
            LocalizationModule.RegisterTranslation("请拆除接收站", "Please remove all receivers", "请拆除本星系的接收站", "");
            LocalizationModule.RegisterTranslation("下一阶段所需能量水平", "Next stage required capacity", "下一阶段所需能量水平", "");
            LocalizationModule.RegisterTranslation("冷却及充能时间", "Charge duration", "充能时间", "");
            LocalizationModule.RegisterTranslation("修建进度", "\nProgress to\nnext stage", "修建进度", "");
            LocalizationModule.RegisterTranslation("最终阶段", "Final stage", "最终阶段", "");
            LocalizationModule.RegisterTranslation("节点总数（已规划）gm", "Nodes in total(Planned)", "节点总数（已规划）", "");
            LocalizationModule.RegisterTranslation("请求功率gm", "Requested power", "请求功率", "");
            LocalizationModule.RegisterTranslation("无限制gm", "Infinite", "无限制", "");

            LocalizationModule.RegisterTranslation("警告巨构科技未解锁", "You must unlock the corresponding technology first", "你必须先解锁对应巨构的科技", "");

            LocalizationModule.RegisterTranslation("力场发生器", "Force field generator", "力场发生器", "");
            LocalizationModule.RegisterTranslation(
                "力场发生器描述",
                "With the help of the gravity generator, the force field generator can multiply the gravitational force in a fixed resonance field and give it a highly controllable directivity. If the energy supply is sufficient, the force field generator is able to deflects or even reverses the direction of the force field, making it possible to encode the resonant frequency of the force field.",
                "借助引力发生装置，力场发生器可以将引力在固定的谐振场域成倍放大，并赋予其高度可控的指向性。如果能量供应足够，力场发生器快速偏折甚至掉转力场方向，这使得对力场谐振频率进行编码成为可能。", "");
            LocalizationModule.RegisterTranslation("复合态晶体", "Compound cyrstal", "复合态晶体", "");
            LocalizationModule.RegisterTranslation(
                "复合态晶体描述",
                "This single-molecule crystal has a normal density like ordinary matter, but can be reshaped into a material with extremely high hardness under the constraints of strong interaction force, so this material is also called strong interaction material (SIM).",
                "这种单分子晶体像普通物质一样拥有正常的密度，但能够在强相互作用力的束缚下，被重塑为硬度极高的物质，因此这种物质又被称为强相互作用力材料（SIM）。", "");
            LocalizationModule.RegisterTranslation("电磁力抑制器", "Electromagnetic force suppressor", "电磁力抑制器", "");
            LocalizationModule.RegisterTranslation(
                "电磁力抑制器描述",
                "By eliminating the electromagnetic force between atomic nucleus, it allows the range of the strong interaction force to overflow the nucleus and expand to atom scope, providing the conditions for precise control of the strong interaction force. The suppressed electromagnetic force can also be redirected to create a vacuum Valsex field vortex ring.",
                "通过消除原子核之间的电磁力，允许强相互作用力的范围溢出原子核，并扩展到原子大小，为精确控制强力提供了条件。被抑制的电磁力还可以被引导至特定方向用以产生真空瓦尔塞克斯电场涡环。", "");
            LocalizationModule.RegisterTranslation("胶子发生器", "Gluon generator", "胶子发生器", "");
            LocalizationModule.RegisterTranslation(
                "胶子发生器描述",
                "Generate controllable gluons to limit or expand the strength and scope of the strong interaction. The gluon generator must be controlled by quantum computers, so as to precisely control the arrangement of atoms on the quantum scale.",
                "产生可控胶子，以此限制或扩大强相互作用力的强度和作用范围。胶子发生器必须在量子计算机的协助下才能提高控制的精准程度，从而在量子尺度上精确控制原子排布。", "");
            LocalizationModule.RegisterTranslation("强力过载装置", "Strong interaction overload device", "强力过载装置", "");
            LocalizationModule.RegisterTranslation(
                "强力过载装置描述",
                "The SIO device can make the repulsive and the attractive force peak to coincide precisely at a specific point, so that any deviation of the nucleus will be pulled back by the strong interaction force. If electromagnetic interference is removed, the nucleus will be fully anchored.",
                "强力过载装置可以使强力的排斥力峰值和吸引力峰值在特定的点精准重合，因而原子核的任何偏离都会被强力拉回。如果剔除了电磁力干扰，原子核将被完全锚定。", "");
            LocalizationModule.RegisterTranslation("导流框架", "Flow guide frame", "导流框架", "");
            LocalizationModule.RegisterTranslation(
                "导流框架描述", "Storing, directing and guiding the energy of stars in a specific direction, creating high power directional energy resonance.",
                "将恒星的能量存储并引导、集中至特定方向，创造极高功率的定向能量谐振。", "");
            LocalizationModule.RegisterTranslation("恒星炮组件", "Star cannon component", "恒星炮组件", "");
            LocalizationModule.RegisterTranslation(
                "恒星炮组件描述",
                "The star cannon can store the energy of the star to stimulate another star's energy surge, guiding its own energy to attack the dark fog. The time for the star cannon to fire is limited, and it needs to be recharged after firing.",
                "恒星炮能够储存恒星的能量，用以激发另一个恒星的能量涌动，引导其自身的能量攻击其所在星系的黑雾巢穴。恒星炮每次开火的时间有限，开火后需要重新充能。", "");
            LocalizationModule.RegisterTranslation("恒星炮运载火箭", "Star cannon carrier rocket", "恒星炮运载火箭", "");
            LocalizationModule.RegisterTranslation("恒星炮运载火箭描述", "The delivery vehicle for the components of the Star cannon.", "恒星炮相关组件的运载工具。", "");
            LocalizationModule.RegisterTranslation("水滴gm", "Droplet", "水滴", "");
            LocalizationModule.RegisterTranslation(
                "水滴描述gm",
                "Droplets can fight as Icarus' space fleet. It uses extremely solid surface to hit key structures. Precise control of the droplet's propulsion and steering requires powerful remote computing power, as well as a remote supply of Mecha energy to control it. Although the damage efficiency of tje droplet is limited by its attack mode, its structure of strongly interacting materials allows it to withstand unlimited damage. You neet to set the space fleet type to droplet type to use the droplet.",
                "水滴可以作为伊卡洛斯的空中舰队进行作战，并使用极其坚硬的表面撞击敌人的关键结构。精确地控制水滴的推进和转向需要强大的远端运算能力，且发射水滴和操控也需要机甲能量的远程供给。虽然水滴的输出效率受其攻击方式限制，但其由强相互作用力材料组成的结构可以使其承受无限的伤害。你需要将太空编队的类型设置为水滴编队，以此放入水滴。", "");

            LocalizationModule.RegisterTranslation("恒星炮设计说明题目", "Design Instructions", "恒星炮设计说明", "");
            LocalizationModule.RegisterTranslation("恒星炮设计说明文本",
                                                   "1. When the star cannon fires, the rotation axes of all layers will overlap, and the north pole will point to the target, so please design the north pole of each layer's grid as the center of the muzzle;\n" +
                                                   "2. When the star cannon fires, 12 random  nodes on the 1st layer will emit a laser to the muzzle. For aesthetic reasons, please try to make the first layer only contain up to 12 nodes, and try to make them symmetrical to each other;\n" +
                                                   "3. The construction of the star cannon needs to go through multiple stages. And at the same time, the star cannon can increase its damage, max fire target count, and charging speed several times. After reaching the final stage, continue to build shells will continuously increase the damage;\n" +
                                                   "4. After built the star cannon, select a star system or a space dark fog hive in starmap mode, then launch the star cannon. Star cannon can NOT fire at its own star system.",
                                                   "1.恒星炮开火时所有层级的旋转轴将重叠，并且让北极指向目标开火，因此设计时请以各层网格自身的北极点为炮口中心；\n2.恒星炮开火时，第1层的随机12个节点将发射出激光指向炮口，为美观考虑，请尽量使得第1层只包含12个（或更少的）节点，并使其相互对称。\n3.恒星炮建造需要经过多个阶段，随着各建造阶段完成，恒星炮能数次提高伤害、同时射击的目标数和充能速度等属性。在达到最终阶段后，继续修建壳层可以不断提高伤害。\n4.建造恒星炮后，在星图模式中选择一个恒星系或太空黑雾巢穴进行开火。恒星炮无法向自身所在星系开火。",
                                                   "");
            LocalizationModule.RegisterTranslation("恒星炮开火按钮文本", "Launch Star Cannon (R)", " 启动恒星炮 (R)", "");
            LocalizationModule.RegisterTranslation("恒星炮开火标题", "Launch Star Cannon", "启动恒星炮", "");
            LocalizationModule.RegisterTranslation("恒星炮开火描述", "Attack the space dark fog hive in the selected star system by the Star Cannon.","使用恒星炮射击该星系内的太空黑雾巢穴。", "");
            LocalizationModule.RegisterTranslation("选中黑雾巢穴时的恒星炮开火描述", "Attack the selected dark fog hive in the selected star system by the Star Cannon. Then attack other hives in that star system.", "使用恒星炮向该黑雾巢穴开火，而后继续射击该星系的其他太空黑雾巢穴。", "");
            LocalizationModule.RegisterTranslation("优先射击按钮文本", "Prioritize attack (R)", " 优先射击 (R)", "");
            LocalizationModule.RegisterTranslation("优先射击标题", "Prioritize attack", "优先射击", "");
            LocalizationModule.RegisterTranslation("优先射击描述", "Star Cannon will turn to attack structures of this selected hive immediately.", "恒星炮将立刻开始转而攻击选中的黑雾巢穴结构。", "");
            LocalizationModule.RegisterTranslation("恒星炮未规划按钮文本", "Star Cannon not planned", "恒星炮未规划", "");
            LocalizationModule.RegisterTranslation("恒星炮建设中按钮文本", "Star Cannon Construction in Progress", "恒星炮建设中", "");
            LocalizationModule.RegisterTranslation("恒星炮正在瞄准按钮文本", "Aiming", "瞄准中", "");
            LocalizationModule.RegisterTranslation("恒星炮预热中按钮文本", "Guiding", "引导中", "");
            LocalizationModule.RegisterTranslation("恒星炮开火中按钮文本", "Firing", "开火中", "");
            LocalizationModule.RegisterTranslation("恒星炮冷却中按钮文本", "Cooling down", "冷却中", "");
            LocalizationModule.RegisterTranslation("恒星炮充能中按钮文本", "Charging", "充能中", "");
            LocalizationModule.RegisterTranslation("恒星炮已就绪", "Star Cannon is Ready", "恒星炮已就绪", "");
            LocalizationModule.RegisterTranslation("恒星炮正在瞄准", "Star Cannon Aiming", "恒星炮正在瞄准", "");
            LocalizationModule.RegisterTranslation("恒星炮预热中", "Star Cannon Guiding", "恒星炮引导中", "");
            LocalizationModule.RegisterTranslation("恒星炮开火中", "Star Cannon Firing", "恒星炮开火中", "");
            LocalizationModule.RegisterTranslation("恒星炮冷却中", "Star Cannon Cooling Down", "恒星炮冷却中", "");
            LocalizationModule.RegisterTranslation("恒星炮充能中", "Star Cannon Charging", "恒星炮充能中", "");
            LocalizationModule.RegisterTranslation("没有规划的恒星炮！", "Star Cannon not planned!", "没有规划的恒星炮！", "");
            LocalizationModule.RegisterTranslation("恒星炮尚未规划", "Star Cannon Not Planned", "恒星炮尚未规划", "");
            LocalizationModule.RegisterTranslation("恒星炮修建中警告", "Star cannon needs to be built to at least the first stage before it can fire!", "恒星炮需要至少修建至第一阶段才能够开火！", "");
            LocalizationModule.RegisterTranslation("恒星炮冷却中警告", "Unable to fire because the star cannon is cooling down.", "恒星炮正在冷却中，无法开火！", "");
            LocalizationModule.RegisterTranslation("恒星炮充能中警告", "Unable to fire because the star cannon is charging.", "恒星炮正在充能中，无法开火！", "");
            LocalizationModule.RegisterTranslation("目标无法定位警告", "Unable to locate any targets! Please check if this star system has surviving DF hive.", "无法定位任何目标！请确认该星系有存活的黑雾巢穴。", "");
            LocalizationModule.RegisterTranslation("恒星炮不能向自身所在星系开火！", "Star cannon cannot fire at its own star system!", "恒星炮无法向自身所在星系开火！", "");
            LocalizationModule.RegisterTranslation("恒星级武器检测警告", "Star level weapon activation detected!", "检测到恒星级武器启动！", "");
            LocalizationModule.RegisterTranslation("尼科尔戴森光束", "Nicoll-Dyson beam", "尼科尔-戴森光束","");
            LocalizationModule.RegisterTranslation("尼科尔戴森光束描述", "Decoding a method to guide stellar energy from dark fog matrix, then use it to attack space dark fog hive.\nAfter built the star cannon, select a star system or a space dark fog hive in starmap mode, then launch the star cannon.\n<color=#FD965ECC>Warning:</color> This technology has been prohibited by the COSMO Technology Ethics Committee. <color=#FD965ECC>Please initiate such research manually.</color>", "从黑雾矩阵中解码引导恒星级能量的方法，并利用其攻击太空黑雾巢穴。\n建造恒星炮后，在星图模式中选择一个恒星系或太空黑雾巢穴进行开火。\n<color=#FD965ECC>警告：</color>该科技的相关技术已被COSMO技术伦理委员会禁用，<color=#FD965ECC>请手动研究。</color>","");
            LocalizationModule.RegisterTranslation("尼科尔戴森光束结论", "You have unlocked the star cannon.", "你解锁了建造恒星炮的能力。","");
            LocalizationModule.RegisterTranslation("先解锁恒星炮科技警告", "You have to unlocked the Nicoll-Dyson beam tech first.", "你需要先解锁尼科尔-戴森光束的科技。", "");
            LocalizationModule.RegisterTranslation("功能说明题目", "Function", "功能", "");
            LocalizationModule.RegisterTranslation(
                "物质解压器功能文本", "Produce unipolar magnet and some basic resources such as iron ingot, which can be received by corresponding receivers.",
                "产出单极磁石和一些基础资源（例如铁块），可被对应的物质重构器接收。", "");
            LocalizationModule.RegisterTranslation("科学枢纽功能文本", "Upload hash points for research without requiring any matrix.", "无需矩阵即可上传hash点数进行研究。",
                                                   "");
            LocalizationModule.RegisterTranslation("折跃场广播阵列功能文本", "Increase the warp speed of logistics vessels.", "提高物流运输船的曲速速度。", "");
            LocalizationModule.RegisterTranslation(
                "星际组装厂功能文本",
                "You can set up to 15 recipes, this megastructure will automatically obtain materials from the exchange logistic stations on the ground, produce products and transport them back to the ground. The production speed depends on the assignment of energy and the recipe's time cost. And you can use proliferator to get extra products without consuming extra energy, if the recipe allows extra production. In any cases, you can not choose the production speedup mode. Unused energy will automatically produce multi-functional integration components.\nIn addition to receive by the exchange logistic station, components can also be transmitted directly to the mecha (but only 10%efficiency). You can set this function in the mecha panel.",
                "可设定最多15个配方，从地表的物资交换站获取材料，生产产品并输送回物资交换站，生产速度取决于分配的能量水平。可以将原料喷涂增产剂来获取额外产出（如果配方允许），这不会占用额外能量，也不能切换为生产加速模式。未使用的能量会自动生产多功能集成组件。\n组件除了可以在物资交换站接收，还可以直接远程传输到机甲中（但只有10%效率）。你可以在机甲面板中设置此功能。",
                "");
            LocalizationModule.RegisterTranslation(
                "晶体重构器功能文本", "Produce Casimir crystals and optical grating crystals, which can be received by corresponding receivers.",
                "产出卡西米尔晶体和光栅石，可被对应的接收器接收。", "");

            LocalizationModule.RegisterTranslation("远程折跃多功能组件限制", "Remote receive [Multifunc-Compo] Upper Limit", "远程输送多功能组件限制", "");
            LocalizationModule.RegisterTranslation("远程接收关闭gm", "Off", "关闭", "");
            LocalizationModule.RegisterTranslation("上限1000", "Max 1000", "上限1000", "");
            LocalizationModule.RegisterTranslation("上限2000", "Max 2000", "上限2000", "");
            LocalizationModule.RegisterTranslation("组件无限制", "No limit", "无限制", "");
            LocalizationModule.RegisterTranslation("已开启优先传输至机甲", "Remote-Teleport To Mecha Enabled", "已开启优先输送至机甲", "");

            LocalizationModule.RegisterTranslation("鼠标触碰左侧黄条以规划巨构", "Touch the left bar to plan the megastructure", "鼠标触碰左侧线条以规划巨构", "");

            LocalizationModule.RegisterTranslation("物资交换器", "Exchange Logistic Station", "物资交换物流站", "");
            LocalizationModule.RegisterTranslation(
                "物资交换器描述",
                "Exchange Logistic Station can transport materials like Interstellar Logistics Station. In addition, the interstellar assembly also needs this building to obtain raw materials from the ground, or to transport the product to the ground.",
                "物资交换物流站可以像星际物流站一样运输物资，除此之外，星际组装厂还需要此建筑来从地面获取原材料，或将产物输送至地表。", "");
            LocalizationModule.RegisterTranslation("理论最大速度", "max", "最大", "");
            LocalizationModule.RegisterTranslation("受限理论最大速度", "<color=#ff1010c0>(limited)</color> max", "<color=#ff1010c0>(受限)</color>最大", "");
            LocalizationModule.RegisterTranslation("能量分配", "Energy Allocation", "能量分配", "");
            LocalizationModule.RegisterTranslation("剩余能量", "Residual Energy", "剩余能量", "");
            LocalizationModule.RegisterTranslation("警告巨构不支持恒星系数量大于100个",
                                                   "Warning! This MegaStructure does not support the galaxy with more than 100 star systems. Please find a star system with StarIndex lower than 100, or try to edit config file to remove this restriction.",
                                                   "警告！此巨构不支持恒星系数量大于100个！请寻找序号小于100的恒星系，或在config文件中修改设置来解除此限制。", "");
            LocalizationModule.RegisterTranslation("警告巨构不支持恒星系数量大于1000个",
                                                   "Warning! This MegaStructure does not support the galaxy with more than 1000 star systems.  Please find another star system with StarIndex lower than 1000",
                                                   "警告！此巨构不支持恒星系数量大于1000个！请寻找一个序号小于1000的恒星系。", "");
            LocalizationModule.RegisterTranslation("警告选择了重复的配方", "Please don't select repeated recipes.", "请不要选择重复的配方。", "");
            LocalizationModule.RegisterTranslation("主产物巨构内部仓储", "Internal Storage (Primary Product)", "主产物内部仓储", "");
            LocalizationModule.RegisterTranslation("巨构内部仓储", "Internal Storage", "内部仓储", "");
            LocalizationModule.RegisterTranslation("显示/隐藏星际组装厂配置", "Show/Hide Star Assembly Recipes", "显示/隐藏 星际组装厂配置", "");
            LocalizationModule.RegisterTranslation("配置最大生产速度限制", "Configure production speed limit", "配置生产速度限制", "");
            LocalizationModule.RegisterTranslation("最大生产速度限制", "Speed limit (/min)", "生产速度限制(/min)", "");
            LocalizationModule.RegisterTranslation("最大生产速度限制题目", "Production speed limit", "生产速度限制", "");
            LocalizationModule.RegisterTranslation("最大生产速度限制描述", "By changing this value, the maximum production speed of this recipe can be limited. Even if the allocated energy and raw material supply can meet higher production speed, the interstellar assembly will still consume raw materials for production according to this speed limit.\nCancel this limitation by set it to 0 (default).", "通过该数值，可以限制此配方在星际组装厂的最大生产速度，即使分配的能量和地面原材料的供给均可以满足更高的生产速度，星际组装厂也会按照此设置中限制的速度消耗原材料进行生产。\n设置为0（默认）则代表取消限制。", "");
            LocalizationModule.RegisterTranslation("组装厂槽位解锁于", "Locked. Unlock at {0}x speed", "已锁定，解锁于 {0}x 速度", "");
            LocalizationModule.RegisterTranslation("星际组装厂槽位未解锁警告", "This slot is locked!", "此栏位尚未解锁！", "");

            LocalizationModule.RegisterTranslation("警告巨构不支持此类配方", "Interstellar Assembly is not able to process this recipe.", "星际组装厂无法处理此配方。", "");
            LocalizationModule.RegisterTranslation("钨重构装置", "Tungsten Reconstructor", "钨重构装置", "");
            LocalizationModule.RegisterTranslation("巨建快速组装描述", "Quickly assemble constructions using multi-functional integrated components",
                                                   "使用多功能集成组件快速递组装成目标巨建。", "");


            LocalizationModule.RegisterTranslation("巨构状态", "Mega structure status", "巨构状态", "");
            LocalizationModule.RegisterTranslation("巨构类型不符", "type not match", "巨构不符", "");
            LocalizationModule.RegisterTranslation("模式错误", "wrong mode", "模式错误", "");

            // 特化
            LocalizationModule.RegisterTranslation("星际组装厂特化名称0", "Interstellar Assembly Specilization", "星际组装厂 - 特化", "");
            LocalizationModule.RegisterTranslation("星际组装厂特化名称1", "Stellar Forge", "恒星熔炉", "");
            LocalizationModule.RegisterTranslation("星际组装厂特化名称2", "Stellar reactor", "恒星反应釜", "");
            LocalizationModule.RegisterTranslation("星际组装厂特化名称3", "Ring-star Particle Accelerator", "星环粒子加速器", "");
            LocalizationModule.RegisterTranslation("星际组装厂特化名称4", "Hyper-precision Assembly", "超精密装配厂", "");
            LocalizationModule.RegisterTranslation("星际组装厂特化名称5", "Cybrex War Forge", "赛博勒克斯战争工厂", "");
            LocalizationModule.RegisterTranslation("特化0介绍标题", "Interstellar Assembly Specilization", "星际组装厂 - 特化", "");
            LocalizationModule.RegisterTranslation("特化1介绍标题", "Stellar Forge", "恒星熔炉", "");
            LocalizationModule.RegisterTranslation("特化2介绍标题", "Stellar Reactor", "恒星反应釜", "");
            LocalizationModule.RegisterTranslation("特化3介绍标题", "Ring-star Particle Accelerator", "星环粒子加速器", "");
            LocalizationModule.RegisterTranslation("特化4介绍标题", "Hyper-precision Assembly", "超精密装配厂", "");
            LocalizationModule.RegisterTranslation("特化5介绍标题", "Cybrex War Forge", "赛博勒克斯战争工厂", "");
            LocalizationModule.RegisterTranslation("特化0介绍内容", "\"Once certain specialization <color=#30bb30>requirements</color> are met, the specialization process of the Interstellar Assembly will begin. After <color=#30bb30>maintaining the requirements for 10-120 minutes</color>, the interstellar Assembly can be converted to this <color=#61d8ffc0>Specialization Mode</color>. The larger the current scale of the Interstellar Assembly, the longer the specialization process will take.\nDifferent specialization modes will provide different  <color=#61d8ffc0>bonus effects</color> on different recipes.\nOnce the specialization process is completed, you can <color=#30bb30>no longer maintain the specialization requirements</color>,<color=#30bb30><i>Unless you start meeting the requirements for a different specialization mode</i></color>, which will cause it to start converting to the new specialization.",
                                                   "一旦满足某种特化的<color=#30bb30>要求</color>，星际组装厂的特化进程将开始，<color=#30bb30>保持要求10-120分钟</color>后可以将星际组装厂转化为该<color=#61d8ffc0>特化模式</color>。星际组装厂的当前规模越大，转化过程所需时间越长。\n不同的特化模式将对不同的配方提供<color=#61d8ffc0>加成效果</color>。\n一旦特化进程完成，你可以<color=#30bb30>不再保持特化的要求</color>，<color=#30bb30><i>除非你转而开始满足另一种不同特化的转化要求</i></color>，这将使你的星际组装厂向新的特化开始转变。",
                                                   "");
            LocalizationModule.RegisterTranslation("特化1介绍内容", "Requirement: At least 5 <color=#30bb30>smelting</color> recipes are assigned, and no other types of recipes assigned. \n\nSpecialized effect: All <color=#30bb30>smelting</color> recipes have <color=#61d8ffc0>extra-output-enabled</color>, and <color=#61d8ffc0>production speed +200%</color>. But it cannot produce multi-functional integrated components. \n\n<color=#61d8ffc0>Extra-output-enabled</color>: After all raw materials of the recipe are sprayed with proliferators, this recipe will definitely gain the <color=#61d8ffc0>extra product</color> effect in the Interstellar Assembly, regardless of whether the recipe prohibits extra output effects. ",
                                                   "要求：分配了至少5个<color=#30bb30>冶炼</color>配方，且无其他类型的配方。\n\n特化效果：所有<color=#30bb30>冶炼</color>配方<color=#61d8ffc0>允许增产</color>，且<color=#61d8ffc0>速度+200%</color>。但无法产出多功能集成组件。\n\n<color=#61d8ffc0>允许增产</color>：配方的原料喷涂增产剂后，在星际组装厂中生产的产物一定可以获得<color=#61d8ffc0>额外产出</color>效果，无论该配方是否禁止额外产出效果。",
                                                   "");
            LocalizationModule.RegisterTranslation("特化2介绍内容", "Requirements: At least 3 <color=#30bb30>Chemical</color>, <color=#30bb30>Petroleum</color> or <color=#30bb30>Production Enhancer</color> recipes are assigned, and no other types of recipes assigned.\n\nSpecialized effect: raw materials and products for all <color=#30bb30>chemical</color>, <color=#30bb30>petroleum</color> and <color=#30bb30>proliferator</color> recipes will be automatically <color=#61d8ffc0>sprayed with proliferator for free</color>, and have <color=#61d8ffc0>extra-output-enabled</color>, besides, their <color=#61d8ffc0>production speed +100%</color>.\n\n<color=#61d8ffc0>Extra-output-enabled</color>: After all raw materials of the recipe are sprayed with proliferators, this recipe will definitely gain the <color=#61d8ffc0>extra product</color> effect in the Interstellar Assembly, regardless of whether the recipe prohibits extra output effects. ",
                                                   "要求：分配了至少3个<color=#30bb30>化工</color>、<color=#30bb30>石油</color>或<color=#30bb30>增产剂</color>配方，且无其他类型的配方。\n\n特化效果：为所有<color=#30bb30>化工</color>、<color=#30bb30>石油</color>和<color=#30bb30>增产剂</color>配方的原料和产物<color=#61d8ffc0>免费喷涂增产剂</color>，并<color=#61d8ffc0>允许增产</color>，且他们的<color=#61d8ffc0>速度+100%</color>。\n\n<color=#61d8ffc0>允许增产</color>：配方的原料喷涂增产剂后，在星际组装厂中生产的产物一定可以获得<color=#61d8ffc0>额外产出</color>效果，无论该配方是否禁止额外产出效果。",
                                                   "");
            LocalizationModule.RegisterTranslation("特化3介绍内容", "Requirements: At least 3 recipes <color=#30bb30><i>related to</i></color> <color=#30bb30>Antimatter</color> or <color=#30bb30>Deuterium</color> are assigned, and no other types of recipes assigned. \n\nSpecialization effects: All recipes <color=#30bb30><i>related to</i></color> <color=#30bb30>antimatter</color> or <color=#30bb30>deuterium</color> have <color=#61d8ffc0>extra-output-enabled</color> and gain <color=#61d8ffc0>+25% additional output</color>. \n\n<color=#30bb30><i>Related</i></color> : The raw materials or products of the recipe contain an item.\n\n<color=#61d8ffc0>Extra-output-enabled</color>: After all raw materials of the recipe are sprayed with proliferators, this recipe will definitely gain the <color=#61d8ffc0>extra product</color> effect in the Interstellar Assembly, regardless of whether the recipe prohibits extra output effects.",
                                                   "要求：分配了至少3个与<color=#30bb30>反物质</color>或<color=#30bb30>重氢</color><color=#30bb30><i>相关</i></color>的配方，且没有不<color=#30bb30><i>相关</i></color>的配方。\n\n特化效果：所有与<color=#30bb30>反物质</color>或<color=#30bb30>重氢</color><color=#30bb30><i>相关</i></color>的配方<color=#61d8ffc0>允许增产</color>，并获得<color=#61d8ffc0>+25%额外产出</color>。\n\n<color=#30bb30><i>相关</i></color>：配方的原材料或者产物中包含某个物品。\n\n<color=#61d8ffc0>允许增产</color>：配方的原料喷涂增产剂后，在星际组装厂中生产的产物一定可以获得<color=#61d8ffc0>额外产出</color>效果，无论该配方是否禁止额外产出效果。",
                                                   "");
            LocalizationModule.RegisterTranslation("特化4介绍内容", "Requirements: At least 5 recipes <color=#30bb30><i>related to</i></color> <color=#30bb30>processor</color>, <color=#30bb30>quantum chip</color> or <color=#30bb30>quantum computer</color> are assigned, and no other types of recipes assigned.\n\nSpecialized effects: All recipes that <color=#30bb30>use processors</color>, <color=#30bb30>quantum chips</color> or <color=#30bb30>quantum computers</color> as inputs have <color=#61d8ffc0>extra-output-enabled</color>, and gain <color=#61d8ffc0>+25% additional output</color>; \n<color=#30bb30>Production processor</color>, <color=#30bb30>Quantum chip</color> and <color=#30bb30>Quantum Computer</color>'s recipe itself will gain <color=#61d8ffc0>+50% additional output</color> instead.\n\n<color=#30bb30><i>Related</i></color> : The raw materials or products of the recipe contain an item.\n\n<color=#61d8ffc0>Extra-output-enabled</color>: After all raw materials of the recipe are sprayed with proliferators, this recipe will definitely gain the <color=#61d8ffc0>extra product</color> effect in the Interstellar Assembly, regardless of whether the recipe prohibits extra output effects.",
                                                   "要求：分配了至少5个与<color=#30bb30>处理器</color>、<color=#30bb30>量子芯片</color>、<color=#30bb30>量子计算机</color><color=#30bb30><i>相关</i></color>的配方，且没有不<color=#30bb30><i>相关</i></color>的配方\n\n特化效果：所有<color=#30bb30>使用处理器</color>、<color=#30bb30>量子芯片</color>、<color=#30bb30>量子计算机</color>作为输入的配方<color=#61d8ffc0>允许增产</color>，并获得<color=#61d8ffc0>+25%额外产出</color>；\n<color=#30bb30>生产处理器</color>、<color=#30bb30>量子芯片</color>、<color=#30bb30>量子计算机</color>的配方则转而<color=#61d8ffc0>+50%额外产出</color>。\n\n<color=#30bb30><i>相关</i></color>：配方的原材料或者产物中包含某个物品。\n\n<color=#61d8ffc0>允许增产</color>：配方的原料喷涂增产剂后，在星际组装厂中生产的产物一定可以获得<color=#61d8ffc0>额外产出</color>效果，无论该配方是否禁止额外产出效果。",
                                                   "");
            LocalizationModule.RegisterTranslation("特化5介绍内容", "Requirements: At least 5 recipes <color=#30bb30><i>related to</i></color> <color=#30bb30>combat drones</color>, <color=#30bb30>warships</color>, <color=#30bb30>ammo</color> or <color=#30bb30>defense facilities</color> are assigned, and no other types of recipes assigned.\n\nSpecialized effects: All recipes of <color=#30bb30>attack drones</color>, <color=#30bb30>warships</color> and <color=#30bb30>defense facilities</color> have <color=#61d8ffc0>extra-output-enabled</color> and gain <color=#61d8ffc0>+50% additional output</color>; the recipes of <color=#30bb30>ammo</color> gain <color=#61d8ffc0>+100% additional output</color> instead.\n\n<color=#61d8ffc0>Extra-output-enabled</color>: After all raw materials of the recipe are sprayed with proliferators, this recipe will definitely gain the <color=#61d8ffc0>extra product</color> effect in the Interstellar Assembly, regardless of whether the recipe prohibits extra output effects.",
                                                   "要求：分配了至少5个<color=#30bb30>攻击型无人机</color>、<color=#30bb30>战舰</color>、<color=#30bb30>弹药</color>或<color=#30bb30>防御设施</color>的配方，且无其他类型的配方。\n\n特化效果：各类<color=#30bb30>攻击型无人机</color>、<color=#30bb30>战舰</color>和<color=#30bb30>防御设施</color>的配方<color=#61d8ffc0>允许增产</color>，并获得<color=#61d8ffc0>+50%额外产出</color>；<color=#30bb30>弹药</color>的配方则转而获得<color=#61d8ffc0>+100%额外产出</color>。\n\n<color=#61d8ffc0>允许增产</color>：配方的原料喷涂增产剂后，在星际组装厂中生产的产物一定可以获得<color=#61d8ffc0>额外产出</color>效果，无论该配方是否禁止额外产出效果。",
                                                   "");

            LocalizationModule.RegisterTranslation("特化已激活", "Specialization Activated", "特化已激活", "");
            LocalizationModule.RegisterTranslation("特化即将被取代", "Specialization is about to be replaced", "特化即将被取代", "");
            LocalizationModule.RegisterTranslation("特化进程", "Specialize in progress {0}%", "特化进程 {0}%", "");
            LocalizationModule.RegisterTranslation("特化进程消退中", "Progress fading {0}%", "特化进程消退中 {0}%", "");
            LocalizationModule.RegisterTranslation("等待其他特化进程消退", "Stand by", "等待进程开始", "");
            LocalizationModule.RegisterTranslation("特化条件未满足", "Requirements not satisfied", "特化条件未满足", "");
        }

        /// <summary>
        /// 用于在接收器面板显示的文本修正，因为并非总是生成光子
        /// </summary>
        /// <param name="proto"></param>
        internal static void ChangeReceiverRelatedStringProto()
        {
            new StringProto { Name = "光子生成", ZHCN = "物质合成", ENUS = "Substance generation" }.RegisterTranslation();

            new StringProto
            {
                Name = "光子生成描述",
                ZHCN = "在物质合成模式下，接收站将允许接收巨构建筑的输出，并将其转换为对应物质。",
                ENUS
                    = "In Substance generation mode, the receiver will receive product delivered by the megastructure and convert it into the corresponding item."
            }.RegisterTranslation();


            new StringProto { Name = "戴森球统计页签", ZHCN = "巨构建筑", ENUS = "Megastructure" }.RegisterTranslation();
            new StringProto { Name = "戴森球类", ZHCN = "巨构建筑 (8)", ENUS = "Megastructure (8)" }.RegisterTranslation();
            new StringProto { Name = "戴森球面板提示", ZHCN = "打开巨构建筑面板，制定巨构建筑的建造规划。", ENUS = "Turn on the Megastructure editor, and make a build plan." }
               .RegisterTranslation();

            new StringProto { Name = "戴森球面板", ZHCN = "巨构建筑 (Y)", ENUS = "Megastructure (Y)" }.RegisterTranslation();
            new StringProto { Name = "修建节点标题", ZHCN = "巨构节点", ENUS = "Megastructure Node" }.RegisterTranslation();
            new StringProto
            {
                Name = "修建节点描述",
                ZHCN = "规划修建巨构建筑节点，点击该按钮可以选择节点的样式。",
                ENUS = "Plan and build the Megastructure Node, click this button to choose the node style."
            }.RegisterTranslation();
            new StringProto { Name = "修建壳面标题", ZHCN = "巨构壳面", ENUS = "Megastructure Shell" }.RegisterTranslation();
            new StringProto
            {
                Name = "修建壳面描述",
                ZHCN = "若要规划一个巨构壳面，需要将相应的壳面用节点和框架规划成一个闭合的多边形。\n点击该按钮可以选择巨构壳面的样式进行修建规划。",
                ENUS
                    = "In order to Plan a Megastructure Shell, it is necessary to plan the corresponding shell as a closed polygon with nodes and frames.\nClick this button to choose the Megastructure Shell style."
            }.RegisterTranslation();

            new StringProto
            {
                Name = "戴森球节点号", ZHCN = "巨构节点 # {0}-{1}", ENUS = "Structure Node # {0}-{1}" //Megastructure Node # {0}-{1}
            }.RegisterTranslation();

            new StringProto
            {
                Name = "戴森球框架号", ZHCN = "巨构框架 # {0}-{1}", ENUS = "Structure Frame # {0}-{1}" //Megastructure Frame # {0}-{1}
            }.RegisterTranslation();

            new StringProto
            {
                Name = "戴森球壳面号", ZHCN = "巨构壳面 # {0}-{1}", ENUS = "Structure Shell # {0}-{1}" //Megastructure Shell # {0}-{1}
            }.RegisterTranslation();
        }
    }
}
