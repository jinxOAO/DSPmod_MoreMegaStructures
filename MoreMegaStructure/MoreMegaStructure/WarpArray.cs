using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreMegaStructure
{
    public class WarpArrayData
    {
        public int starIndex;
        public VectorLF3 uPos;
        public double radius;
        public double squaredRadius;
        public double tripEnergyCostRatio;
        // public int activeCountThisFrame;
    }

    public class WarpArray
    {
        // 配置项常数
        public static bool enabled = true;
        public static double radiusEnergyPowIndex = 0.666666667;
        public static int radiusEnergyDivisor = 200000;
        public static long tripCostEnergyDivisor = 2000000000;
        public const float warpSpeedInWarpField = 250 * 60 * 40000;
        public const float sailSpeedInWarpField = 400 * 500;
        public const float fixedSailSpeedInOtherMods = 400 * 100;

        // 运行时更新系数 无需存档
        public static List<WarpArrayData> arrays;
        public static int[] starIsInWhichWarpArray;
        public static double[] tripEnergyCostRatioByStarIndex; // 根据星系是否处在折跃场范围内，降低该星系出发的飞船的能量消耗

        public static void InitBeforeLoad()
        {
            arrays = new List<WarpArrayData>();
            tripEnergyCostRatioByStarIndex = new double[1024];
            starIsInWhichWarpArray = new int[1024];
            for (int i = 0; i < tripEnergyCostRatioByStarIndex.Length; i++)
            {
                tripEnergyCostRatioByStarIndex[i] = 1;
                starIsInWhichWarpArray[i] = -1;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), "GameTick")]
        public static void WarpArrayLogicUpdate(long time)
        {
            UpdateSectorTripCostRatio(time); // 每帧更新一部分星系，一秒全部更新一次

            if (time % 60 == 44)
                UpdateWarpArrayDatas();
            if (time % 3600 == 45)
                SortWarpArray();

        }

        public static void CheckSectorWarpArrays()
        {
            List<WarpArrayData> obj = arrays;
            lock (obj)
            {
                arrays.Clear();
                for (int starIndex = 0; starIndex < GameMain.galaxy.starCount && starIndex < 1000; starIndex++)
                {
                    if (MoreMegaStructure.StarMegaStructureType[starIndex] == 3)
                    {
                        WarpArrayData data = new WarpArrayData();
                        data.starIndex = starIndex;
                        data.uPos = GameMain.galaxy.StarById(starIndex + 1).uPosition;
                        if (GameMain.data.dysonSpheres[starIndex] != null)
                        {
                            data.radius = GetRadiusByEnergyPerFrame(GameMain.data.dysonSpheres[starIndex].energyGenCurrentTick);
                            data.tripEnergyCostRatio = GetTripEnergyCostRatioByEnergyPerFrame(GameMain.data.dysonSpheres[starIndex].energyGenCurrentTick);
                        }
                        else
                            continue;
                        //data.radius = 5 * 60 * 40000;
                        data.squaredRadius = data.radius * data.radius;
                        arrays.Add(data);
                    }
                }
            }
        }


        public static void UpdateWarpArrayDatas()
        {
            if (arrays != null)
            {
                for (int i = 0; i < arrays.Count; i++)
                {
                    int starIndex = arrays[i].starIndex;
                    if (starIndex >= 0 && starIndex < 1000 && MoreMegaStructure.StarMegaStructureType[starIndex] == 3)
                    {
                        if (GameMain.data.dysonSpheres[starIndex] != null)
                        {
                            arrays[i].uPos = GameMain.galaxy.StarById(starIndex + 1).uPosition;
                            arrays[i].radius = GetRadiusByEnergyPerFrame(GameMain.data.dysonSpheres[starIndex].energyGenCurrentTick);
                            arrays[i].tripEnergyCostRatio = GetTripEnergyCostRatioByEnergyPerFrame(GameMain.data.dysonSpheres[starIndex].energyGenCurrentTick);
                            arrays[i].squaredRadius = arrays[i].radius * arrays[i].radius;
                        }
                    }
                    else
                    {
                        CheckSectorWarpArrays();
                        return;
                    }
                }
            }
        }

        public static void SortWarpArray()
        {
            if(arrays != null && arrays.Count > 1)
            {
                arrays = arrays.OrderByDescending(a => a.radius).ToList();
            }
        }

        public static void UpdateSectorTripCostRatio(long time)
        {
            int beginStarIndex = GameMain.galaxy.starCount / 60 * (int)(time % 60);
            int endStarIndex = GameMain.galaxy.starCount / 60 * ((int)(time % 60) + 1);
            if (time % 60 == 59)
                endStarIndex = GameMain.galaxy.starCount;
            for (int starIndex = beginStarIndex; starIndex < endStarIndex; starIndex++)
            {
                tripEnergyCostRatioByStarIndex[starIndex] = 1;
                starIsInWhichWarpArray[starIndex] = -1;
                VectorLF3 starUPos = GameMain.galaxy.StarById(starIndex + 1).uPosition;
                for (int i = 0; i < arrays.Count; i++) // 因为列表是有序的，所以最先找到的是能量最高的折跃场，因此是降低能量效果最好的，因此找到了就可以break
                {
                    Vector3 disVector = starUPos - arrays[i].uPos;
                    if(disVector.x * disVector.x + disVector.y * disVector.y + disVector.z * disVector.z < arrays[i].squaredRadius)
                    {
                        tripEnergyCostRatioByStarIndex[starIndex] = arrays[i].tripEnergyCostRatio;
                        starIsInWhichWarpArray[starIndex] = i;
                        break;
                    }
                }
            }
        }


        //public static void UpdateStarmapActiveCount()
        //{
        //    if(UIGame.viewMode == EViewMode.Starmap)
        //    {
        //        for (int i = 0; i < arrays.Count; i++)
        //        {
        //            if(arrays[i].activeCountThisFrame > 0)
        //            {
        //                arrays[i].activeCountThisFrame = (int)(arrays[i].activeCountThisFrame * 0.8);
        //                if (arrays[i].activeCountThisFrame < 0)
        //                    arrays[i].activeCountThisFrame = 0;
        //            }
        //        }
        //    }
        //}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), "InternalTickRemote")]
        public static bool StationShipUpdatesPrePatch(ref StationComponent __instance, PlanetFactory factory, int timeGene, float shipSailSpeed, float shipWarpSpeed, int shipCarries, StationComponent[] gStationPool, AstroData[] astroPoses, ref VectorLF3 relativePos, ref Quaternion relativeRot, bool starmap, int[] consumeRegister)
        {
            //------------------------------------------------------------------------------
            float oriShipWarpSpeed = shipWarpSpeed;
            if(CustomCreateBirthStarCompat.enabled || GalacticScaleCompat.enabled)
            {
                shipSailSpeed = fixedSailSpeedInOtherMods > shipSailSpeed ? fixedSailSpeedInOtherMods : shipSailSpeed;
            }
            if (factory.planet.star.index < tripEnergyCostRatioByStarIndex.Length && tripEnergyCostRatioByStarIndex[factory.planet.star.index] < 1) // 代表它处在折跃场中
                shipSailSpeed = sailSpeedInWarpField > shipSailSpeed ? sailSpeedInWarpField : shipSailSpeed;
            // 注意这里不能像曲速速度修改一样在后面的每个船处修改，因为在那里修改之前会根据shipSailSpeed计算很多的中间变量，这影响启用曲速的时机。
            // 如果前面这些中间变量还是慢的sailSpeed计算出来的，然后在后面改了SailSpeed，会导致船迟迟不进入曲速状态
            //------------------------------------------------------------------------------

            bool flag = shipWarpSpeed > shipSailSpeed + 1f;
            __instance.warperFree = DSPGame.IsMenuDemo;
            if (__instance.warperCount < __instance.warperMaxCount)
            {
                StationStore[] obj = __instance.storage;
                lock (obj)
                {
                    for (int i = 0; i < __instance.storage.Length; i++)
                    {
                        if (__instance.storage[i].itemId == 1210 && __instance.storage[i].count > 0)
                        {
                            __instance.warperCount++;
                            int num = __instance.storage[i].inc / __instance.storage[i].count;
                            StationStore[] array = __instance.storage;
                            int num2 = i;
                            array[num2].count = array[num2].count - 1;
                            StationStore[] array2 = __instance.storage;
                            int num3 = i;
                            array2[num3].inc = array2[num3].inc - num;
                            break;
                        }
                    }
                }
            }
            int num4 = 0;
            int num5 = 0;
            int itemId = 0;
            int num6 = 0;
            int num7 = 0;
            int num8 = 0;
            int num9 = 0;
            float sailSpdPer0_1Frame = shipSailSpeed / 600f;
            float num11 = Mathf.Pow(sailSpdPer0_1Frame, 0.4f);
            float factorFromSailSpd = num11;
            if (factorFromSailSpd > 1f)
            {
                factorFromSailSpd = Mathf.Log(factorFromSailSpd) + 1f;
            }
            if (sailSpdPer0_1Frame > 500f)
            {
                sailSpdPer0_1Frame = 500f;
            }
            ref AstroData beginPlanetAstroData = ref astroPoses[__instance.planetId];
            float sailSpeed003 = shipSailSpeed * 0.03f;
            float sailSpeed012 = shipSailSpeed * 0.12f * factorFromSailSpd;
            float sailSpeed04 = shipSailSpeed * 0.4f * sailSpdPer0_1Frame;
            float num16 = num11 * 0.006f + 1E-05f;
            Vector3 vector = new Vector3(0f, 0f, 0f);
            VectorLF3 totalVector = new VectorLF3(0f, 0f, 0f);
            double totalDistance = 0.0;
            Quaternion uRot = new Quaternion(0f, 0f, 0f, 1f);
            int j = 0;
            while (j < __instance.workShipCount)
            {

                ref ShipData ship = ref __instance.workShipDatas[j];
                // ---------------------------------------------------------------------------------
                shipWarpSpeed = oriShipWarpSpeed;
                int enteredWarpArrayListIndex = -1; // 确认进入的折跃场的列表中的地址
                if (enabled)
                {
                    for (int wa = 0; wa < arrays.Count; wa++)
                    {
                        WarpArrayData data = arrays[wa];
                        Vector3 disVector = ship.uPos - data.uPos;
                        if (shipWarpSpeed < warpSpeedInWarpField && disVector.x * disVector.x + disVector.y * disVector.y + disVector.z * disVector.z <= data.squaredRadius)
                        {
                            enteredWarpArrayListIndex = wa;
                            shipWarpSpeed = warpSpeedInWarpField;
                            //// 进入折跃场，星图界面让折跃场更亮的特效的计数。如果起飞星球就在折跃场内，则不计数
                            //if (UIGame.viewMode == EViewMode.Starmap && enteredWarpArrayListIndex >= 0 && ship.warpState > 0 && starIsInWhichWarpArray[ship.planetA / 100 - 1] != wa && starIsInWhichWarpArray[ship.planetB / 100 - 1] != wa)
                            //{
                            //    List<WarpArrayData> obj = arrays;
                            //    lock (obj)
                            //    {
                            //        arrays[enteredWarpArrayListIndex].activeCountThisFrame += 1;
                            //    }
                            //}
                            break;
                        }
                    }
                }
                // ---------------------------------------------------------------------------------
                ref ShipRenderingData ptr3 = ref __instance.shipRenderers[ship.shipIndex];
                bool flag3 = false;
                uRot.x = (uRot.y = (uRot.z = 0f));
                uRot.w = 1f;
                ref AstroData targetPlanetAstroData = ref astroPoses[ship.planetB];
                totalVector.x = beginPlanetAstroData.uPos.x - targetPlanetAstroData.uPos.x;
                totalVector.y = beginPlanetAstroData.uPos.y - targetPlanetAstroData.uPos.y;
                totalVector.z = beginPlanetAstroData.uPos.z - targetPlanetAstroData.uPos.z;
                totalDistance = Math.Sqrt(totalVector.x * totalVector.x + totalVector.y * totalVector.y + totalVector.z * totalVector.z);
                if (ship.otherGId <= 0)
                {
                    ship.direction = -1;
                    if (ship.stage > 0)
                    {
                        ship.stage = 0;
                    }
                }
                if (ship.stage < -1)
                {
                    if (ship.direction > 0)
                    {
                        ship.t += 0.03335f;
                        if (ship.t > 1f)
                        {
                            ship.t = 0f;
                            ship.stage = -1;
                        }
                    }
                    else
                    {
                        ship.t -= 0.03335f;
                        if (ship.t < 0f)
                        {
                            ship.t = 0f;
                            __instance.AddItem(ship.itemId, ship.itemCount, ship.inc);
                            factory.NotifyShipDelivery(ship.planetB, gStationPool[ship.otherGId], ship.planetA, __instance, ship.itemId, ship.itemCount);
                            if (__instance.workShipOrders[j].itemId > 0)
                            {
                                StationStore[] obj = __instance.storage;
                                lock (obj)
                                {
                                    if (__instance.storage[__instance.workShipOrders[j].thisIndex].itemId == __instance.workShipOrders[j].itemId)
                                    {
                                        StationStore[] array3 = __instance.storage;
                                        int thisIndex = __instance.workShipOrders[j].thisIndex;
                                        array3[thisIndex].remoteOrder = array3[thisIndex].remoteOrder - __instance.workShipOrders[j].thisOrdered;
                                    }
                                }
                                __instance.workShipOrders[j].ClearThis();
                            }
                            int shipIndex = ship.shipIndex;
                            Array.Copy(__instance.workShipDatas, j + 1, __instance.workShipDatas, j, __instance.workShipDatas.Length - j - 1);
                            Array.Copy(__instance.workShipOrders, j + 1, __instance.workShipOrders, j, __instance.workShipOrders.Length - j - 1);
                            __instance.workShipCount--;
                            __instance.idleShipCount++;
                            __instance.WorkShipBackToIdle(shipIndex);
                            Array.Clear(__instance.workShipDatas, __instance.workShipCount, __instance.workShipDatas.Length - __instance.workShipCount);
                            Array.Clear(__instance.workShipOrders, __instance.workShipCount, __instance.workShipOrders.Length - __instance.workShipCount);
                            j--;
                            goto IL_2EB3;
                        }
                    }
                    ship.uPos = beginPlanetAstroData.uPos + Maths.QRotateLF(beginPlanetAstroData.uRot, __instance.shipDiskPos[ship.shipIndex]);
                    ship.uVel.x = 0f;
                    ship.uVel.y = 0f;
                    ship.uVel.z = 0f;
                    ship.uSpeed = 0f;
                    ship.uRot = beginPlanetAstroData.uRot * __instance.shipDiskRot[ship.shipIndex];
                    ship.uAngularVel.x = 0f;
                    ship.uAngularVel.y = 0f;
                    ship.uAngularVel.z = 0f;
                    ship.uAngularSpeed = 0f;
                    ship.pPosTemp = new VectorLF3(0f, 0f, 0f);
                    ship.pRotTemp = new Quaternion(0f, 0f, 0f, 1f);
                    ptr3.anim.z = 0f;
                    goto IL_2D53;
                }
                if (ship.stage == -1)
                {
                    if (ship.direction > 0)
                    {
                        ship.t += num16;
                        float num18 = ship.t;
                        if (ship.t > 1f)
                        {
                            ship.t = 1f;
                            num18 = 1f;
                            ship.stage = 0;
                        }
                        ptr3.anim.z = num18;
                        num18 = (3f - num18 - num18) * num18 * num18;
                        ship.uPos = beginPlanetAstroData.uPos + Maths.QRotateLF(beginPlanetAstroData.uRot, __instance.shipDiskPos[ship.shipIndex] + __instance.shipDiskPos[ship.shipIndex].normalized * (25f * num18));
                        ship.uRot = beginPlanetAstroData.uRot * __instance.shipDiskRot[ship.shipIndex];
                    }
                    else
                    {
                        ship.t -= num16 * 0.6666667f;
                        float num18 = ship.t;
                        if (ship.t < 0f)
                        {
                            ship.t = 1f;
                            num18 = 0f;
                            ship.stage = -2;
                        }
                        ptr3.anim.z = num18;
                        num18 = (3f - num18 - num18) * num18 * num18;
                        VectorLF3 lhs = beginPlanetAstroData.uPos + Maths.QRotateLF(beginPlanetAstroData.uRot, __instance.shipDiskPos[ship.shipIndex]);
                        VectorLF3 lhs2 = beginPlanetAstroData.uPos + Maths.QRotateLF(beginPlanetAstroData.uRot, ship.pPosTemp);
                        ship.uPos = lhs * (double)(1f - num18) + lhs2 * (double)num18;
                        ship.uRot = beginPlanetAstroData.uRot * Quaternion.Slerp(__instance.shipDiskRot[ship.shipIndex], ship.pRotTemp, num18 * 2f - 1f);
                    }
                    ship.uVel.x = 0f;
                    ship.uVel.y = 0f;
                    ship.uVel.z = 0f;
                    ship.uSpeed = 0f;
                    ship.uAngularVel.x = 0f;
                    ship.uAngularVel.y = 0f;
                    ship.uAngularVel.z = 0f;
                    ship.uAngularSpeed = 0f;
                    goto IL_2D53;
                }
                if (ship.stage == 0)
                {
                    VectorLF3 vectorLF2;
                    if (ship.direction > 0)
                    {
                        vector = gStationPool[ship.otherGId].shipDockPos;
                        float num19 = (float)Math.Sqrt((double)(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z));
                        num19 = 1f + 25f / num19;
                        vector.x *= num19;
                        vector.y *= num19;
                        vector.z *= num19;
                        StationComponent.lpos2upos_out(ref targetPlanetAstroData.uPos, ref targetPlanetAstroData.uRot, ref vector, out vectorLF2);
                    }
                    else
                    {
                        vector = __instance.shipDiskPos[ship.shipIndex];
                        float num20 = (float)Math.Sqrt((double)(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z));
                        num20 = 1f + 25f / num20;
                        vector.x *= num20;
                        vector.y *= num20;
                        vector.z *= num20;
                        StationComponent.lpos2upos_out(ref beginPlanetAstroData.uPos, ref beginPlanetAstroData.uRot, ref vector, out vectorLF2);
                    }
                    VectorLF3 vectorLF3;
                    vectorLF3.x = vectorLF2.x - ship.uPos.x;
                    vectorLF3.y = vectorLF2.y - ship.uPos.y;
                    vectorLF3.z = vectorLF2.z - ship.uPos.z;
                    double distFromStation = Math.Sqrt(vectorLF3.x * vectorLF3.x + vectorLF3.y * vectorLF3.y + vectorLF3.z * vectorLF3.z);
                    VectorLF3 vectorFromBeginPlanet;
                    if (ship.direction > 0)
                    {
                        vectorFromBeginPlanet.x = beginPlanetAstroData.uPos.x - ship.uPos.x;
                        vectorFromBeginPlanet.y = beginPlanetAstroData.uPos.y - ship.uPos.y;
                        vectorFromBeginPlanet.z = beginPlanetAstroData.uPos.z - ship.uPos.z;
                    }
                    else
                    {
                        vectorFromBeginPlanet.x = targetPlanetAstroData.uPos.x - ship.uPos.x;
                        vectorFromBeginPlanet.y = targetPlanetAstroData.uPos.y - ship.uPos.y;
                        vectorFromBeginPlanet.z = targetPlanetAstroData.uPos.z - ship.uPos.z;
                    }
                    double dist2FromBeginPlanet = vectorFromBeginPlanet.x * vectorFromBeginPlanet.x + vectorFromBeginPlanet.y * vectorFromBeginPlanet.y + vectorFromBeginPlanet.z * vectorFromBeginPlanet.z;
                    bool flag4 = dist2FromBeginPlanet <= (double)(beginPlanetAstroData.uRadius * beginPlanetAstroData.uRadius) * 2.25;
                    bool flag5 = false;
                    if (distFromStation < (double)(6f * factorFromSailSpd))
                    {
                        ship.t = 1f;
                        ship.stage = ship.direction;
                        flag5 = true;
                    }
                    int farFromAstroCheckStep = 1; // 1 is near
                    float positiveCorrWarpStateSpd = 0f;
                    if (flag)
                    {
                        double totalDistanceDoubled = totalDistance * 2.0;
                        double warpSpeedThreshold = ((double)shipWarpSpeed < totalDistanceDoubled) ? ((double)shipWarpSpeed) : totalDistanceDoubled;
                        double halfWarpEnableDist = __instance.warpEnableDist * 0.5;
                        if (ship.warpState <= 0f)
                        {
                            ship.warpState = 0f;
                            if (dist2FromBeginPlanet > 25000000.0 && distFromStation > halfWarpEnableDist && ship.uSpeed >= shipSailSpeed && (ship.warperCnt > 0 || __instance.warperFree))
                            {
                                ship.warperCnt--;
                                ship.warpState += 0.016666668f;
                            }
                        }
                        else
                        {
                            positiveCorrWarpStateSpd = (float)(warpSpeedThreshold * ((Math.Pow(1001.0, (double)ship.warpState) - 1.0) / 1000.0));
                            double num28 = (double)positiveCorrWarpStateSpd * 0.0449 + 5000.0 + (double)shipSailSpeed * 0.25;
                            double num29 = distFromStation - num28;
                            if (num29 < 0.0)
                            {
                                num29 = 0.0;
                            }
                            if (distFromStation < num28)
                            {
                                ship.warpState -= 0.06666667f;
                            }
                            else
                            {
                                ship.warpState += 0.016666668f;
                            }
                            if (ship.warpState < 0f)
                            {
                                ship.warpState = 0f;
                            }
                            else if (ship.warpState > 1f)
                            {
                                ship.warpState = 1f;
                            }
                            if (ship.warpState > 0f)
                            {
                                positiveCorrWarpStateSpd = (float)(warpSpeedThreshold * ((Math.Pow(1001.0, (double)ship.warpState) - 1.0) / 1000.0));
                                if ((double)positiveCorrWarpStateSpd * 0.016666666666666666 > num29)
                                {
                                    positiveCorrWarpStateSpd = (float)(num29 / 0.016666666666666666 * 1.01);
                                }
                            }
                        }
                    }
                    if (dist2FromBeginPlanet > 1000000000000.0 && dist2FromBeginPlanet > (double)(shipSailSpeed * shipSailSpeed * 4900f))
                    {
                        if (distFromStation > 1000000.0 + (double)positiveCorrWarpStateSpd * 0.55)
                        {
                            farFromAstroCheckStep = 30;
                        }
                        else if (distFromStation > 1000000.0 + (double)positiveCorrWarpStateSpd * 0.2)
                        {
                            farFromAstroCheckStep = 10;
                        }
                    }
                    //---------------------------------------------
                    if (enabled && ship.warpState > 0f) // 判断速度必须更频繁判断
                    {
                        ship.uSpeed = shipSailSpeed + positiveCorrWarpStateSpd;
                    }
                    //---------------------------------------------
                    VectorLF3 vectorLF5 = new VectorLF3(0f, 0f, 0f);
                    if (farFromAstroCheckStep == 1 || (__instance.gene + j + timeGene) % farFromAstroCheckStep == 0)
                    {
                        double etaTimeX0_3 = distFromStation / ((double)ship.uSpeed + 0.1) * 0.382;
                        float num31;
                        if (ship.warpState > 0f)
                        {
                            num31 = (ship.uSpeed = shipSailSpeed + positiveCorrWarpStateSpd);
                            if (num31 > shipSailSpeed)
                            {
                                num31 = shipSailSpeed;
                            }
                        }
                        else
                        {
                            float num32 = (float)((double)ship.uSpeed * etaTimeX0_3 * (double)factorFromSailSpd) + 6f * num11 + 0.15f * sailSpdPer0_1Frame;
                            if (num32 > shipSailSpeed)
                            {
                                num32 = shipSailSpeed;
                            }
                            float num33 = 0.016666668f * (flag4 ? sailSpeed003 : sailSpeed012);
                            if (ship.uSpeed < num32 - num33)
                            {
                                ship.uSpeed += num33;
                            }
                            else if (ship.uSpeed > num32 + sailSpeed04)
                            {
                                ship.uSpeed -= sailSpeed04;
                            }
                            else
                            {
                                ship.uSpeed = num32;
                            }
                            num31 = ship.uSpeed;
                        }
                        int num34 = -1;
                        double rhs = 0.0;
                        double num35 = 40000000000.0;
                        if (farFromAstroCheckStep == 1)
                        {
                            int num36 = ship.planetA / 100 * 100;
                            int num37 = ship.planetB / 100 * 100;
                            float num38 = 5000f + num31;
                            VectorLF3 vectorLF6;
                            for (int k = num36; k < num36 + 10; k++)
                            {
                                float uRadius = astroPoses[k].uRadius;
                                if (uRadius >= 1f)
                                {
                                    ref VectorLF3 ptr5 = ref astroPoses[k].uPos;
                                    float num39 = uRadius + num38;
                                    vectorLF6.x = ship.uPos.x - ptr5.x;
                                    vectorLF6.y = ship.uPos.y - ptr5.y;
                                    vectorLF6.z = ship.uPos.z - ptr5.z;
                                    double num40 = vectorLF6.x * vectorLF6.x + vectorLF6.y * vectorLF6.y + vectorLF6.z * vectorLF6.z;
                                    double num41 = -((double)ship.uVel.x * vectorLF6.x + (double)ship.uVel.y * vectorLF6.y + (double)ship.uVel.z * vectorLF6.z);
                                    if ((num41 > 0.0 || num40 < (double)(uRadius * uRadius * 7f)) && num40 < num35 && num40 < (double)(num39 * num39))
                                    {
                                        rhs = ((num41 < 0.0) ? 0.0 : num41);
                                        num34 = k;
                                        num35 = num40;
                                    }
                                }
                            }
                            if (num37 != num36)
                            {
                                for (int l = num37; l < num37 + 10; l++)
                                {
                                    float uRadius2 = astroPoses[l].uRadius;
                                    if (uRadius2 >= 1f)
                                    {
                                        ref VectorLF3 ptr6 = ref astroPoses[l].uPos;
                                        float num42 = uRadius2 + num38;
                                        vectorLF6.x = ship.uPos.x - ptr6.x;
                                        vectorLF6.y = ship.uPos.y - ptr6.y;
                                        vectorLF6.z = ship.uPos.z - ptr6.z;
                                        double num43 = vectorLF6.x * vectorLF6.x + vectorLF6.y * vectorLF6.y + vectorLF6.z * vectorLF6.z;
                                        double num44 = -((double)ship.uVel.x * vectorLF6.x + (double)ship.uVel.y * vectorLF6.y + (double)ship.uVel.z * vectorLF6.z);
                                        if ((num44 > 0.0 || num43 < (double)(uRadius2 * uRadius2 * 7f)) && num43 < num35 && num43 < (double)(num42 * num42))
                                        {
                                            rhs = ((num44 < 0.0) ? 0.0 : num44);
                                            num34 = l;
                                            num35 = num43;
                                        }
                                    }
                                }
                            }
                        }
                        Vector3 vector2 = new VectorLF3(0f, 0f, 0f);
                        Vector3 vector3 = new VectorLF3(0f, 0f, 0f);
                        float num45 = 0f;
                        if (num34 > 0)
                        {
                            ref AstroData ptr7 = ref astroPoses[num34];
                            float num46 = ptr7.uRadius;
                            if (num34 % 100 == 0)
                            {
                                num46 *= 2.5f;
                            }
                            double num47 = Math.Max(1.0, ((ptr7.uPosNext - ptr7.uPos).magnitude - 0.5) * 0.6);
                            double num48 = 1.0 + 1600.0 / (double)num46;
                            double num49 = 1.0 + 250.0 / (double)num46;
                            num48 *= num47 * num47;
                            double num50 = (double)((num34 == ship.planetA || num34 == ship.planetB) ? 1.25f : 1.5f);
                            double num51 = Math.Sqrt(num35);
                            double num52 = (double)num46 / num51 * 1.6 - 0.1;
                            if (num52 > 1.0)
                            {
                                num52 = 1.0;
                            }
                            else if (num52 < 0.0)
                            {
                                num52 = 0.0;
                            }
                            double num53 = num51 - (double)num46 * 0.82;
                            if (num53 < 1.0)
                            {
                                num53 = 1.0;
                            }
                            double num54 = (double)(num31 - 6f) / (num53 * (double)factorFromSailSpd) * 0.6 - 0.01;
                            if (num54 > 1.5)
                            {
                                num54 = 1.5;
                            }
                            else if (num54 < 0.0)
                            {
                                num54 = 0.0;
                            }
                            VectorLF3 vectorLF7 = ship.uPos + ((VectorLF3)ship.uVel * rhs) - ptr7.uPos;
                            double num55 = vectorLF7.magnitude / (double)num46;
                            if (num55 < num50)
                            {
                                double num56 = (num55 - 1.0) / (num50 - 1.0);
                                if (num56 < 0.0)
                                {
                                    num56 = 0.0;
                                }
                                num56 = 1.0 - num56 * num56;
                                vector3 = vectorLF7.normalized * (num54 * num54 * num56 * 2.0 * (double)(1f - ship.warpState));
                            }
                            VectorLF3 vectorLF8;
                            vectorLF8.x = ship.uPos.x - ptr7.uPos.x;
                            vectorLF8.y = ship.uPos.y - ptr7.uPos.y;
                            vectorLF8.z = ship.uPos.z - ptr7.uPos.z;
                            double num57 = num52 / num51;
                            vector2.x = (float)(vectorLF8.x * num57);
                            vector2.y = (float)(vectorLF8.y * num57);
                            vector2.z = (float)(vectorLF8.z * num57);
                            num45 = (float)num52;
                            double num58 = num51 / (double)num46;
                            num58 *= num58;
                            num58 = (num48 - num58) / (num48 - num49);
                            if (num58 > 1.0)
                            {
                                num58 = 1.0;
                            }
                            else if (num58 < 0.0)
                            {
                                num58 = 0.0;
                            }
                            if (num58 > 0.0)
                            {
                                VectorLF3 vectorLF9;
                                Maths.QInvRotateLF_refout(ref ptr7.uRot, ref vectorLF8, out vectorLF9);
                                VectorLF3 vectorLF10;
                                StationComponent.lpos2upos_out(ref ptr7.uPosNext, ref ptr7.uRotNext, ref vectorLF9, out vectorLF10);
                                num58 = (3.0 - num58 - num58) * num58 * num58;
                                vectorLF5.x = (vectorLF10.x - ship.uPos.x) * num58;
                                vectorLF5.y = (vectorLF10.y - ship.uPos.y) * num58;
                                vectorLF5.z = (vectorLF10.z - ship.uPos.z) * num58;
                            }
                        }
                        Vector3 vector4;
                        ship.uRot.ForwardUp(out ship.uVel, out vector4);
                        float num59 = 1f - num45;
                        Vector3 vector5;
                        vector5.x = vector4.x * num59 + vector2.x * num45;
                        vector5.y = vector4.y * num59 + vector2.y * num45;
                        vector5.z = vector4.z * num59 + vector2.z * num45;
                        float num60 = vector5.x * ship.uVel.x + vector5.y * ship.uVel.y + vector5.z * ship.uVel.z;
                        vector5.x -= num60 * ship.uVel.x;
                        vector5.y -= num60 * ship.uVel.y;
                        vector5.z -= num60 * ship.uVel.z;
                        float num61 = (float)Math.Sqrt((double)(vector5.x * vector5.x + vector5.y * vector5.y + vector5.z * vector5.z));
                        if (num61 > 0f)
                        {
                            vector5.x /= num61;
                            vector5.y /= num61;
                            vector5.z /= num61;
                        }
                        Vector3 vector6;
                        vector6.x = vector3.x;
                        vector6.y = vector3.y;
                        vector6.z = vector3.z;
                        if (distFromStation > 0.0)
                        {
                            vector6.x += (float)(vectorLF3.x / distFromStation);
                            vector6.y += (float)(vectorLF3.y / distFromStation);
                            vector6.z += (float)(vectorLF3.z / distFromStation);
                        }
                        Vector3 vector7;
                        StationComponent.Vector3Cross_ref(ref ship.uVel, ref vector6, out vector7);
                        float num62 = ship.uVel.x * vector6.x + ship.uVel.y * vector6.y + ship.uVel.z * vector6.z;
                        Vector3 vector8;
                        StationComponent.Vector3Cross_ref(ref vector4, ref vector5, out vector8);
                        float num63 = vector4.x * vector5.x + vector4.y * vector5.y + vector4.z * vector5.z;
                        if (num62 < 0f)
                        {
                            float num64 = (float)Math.Sqrt((double)(vector7.x * vector7.x + vector7.y * vector7.y + vector7.z * vector7.z));
                            if (num64 > 0f)
                            {
                                vector7.x /= num64;
                                vector7.y /= num64;
                                vector7.z /= num64;
                            }
                        }
                        if (num63 < 0f)
                        {
                            float num65 = (float)Math.Sqrt((double)(vector8.x * vector8.x + vector8.y * vector8.y + vector8.z * vector8.z));
                            if (num65 > 0f)
                            {
                                vector8.x /= num65;
                                vector8.y /= num65;
                                vector8.z /= num65;
                            }
                        }
                        float d = (etaTimeX0_3 < 3.0) ? ((3.25f - (float)etaTimeX0_3) * 4f) : (num31 / shipSailSpeed * (flag4 ? 0.2f : 1f));
                        vector7 = vector7 * d + vector8 * 2f;
                        Vector3 vector9;
                        vector9.x = vector7.x - ship.uAngularVel.x;
                        vector9.y = vector7.y - ship.uAngularVel.y;
                        vector9.z = vector7.z - ship.uAngularVel.z;
                        float num66 = (vector9.x * vector9.x + vector9.y * vector9.y + vector9.z * vector9.z < 0.1f) ? 1f : (0.05f * factorFromSailSpd);
                        if (num66 > 1f)
                        {
                            num66 = 1f;
                        }
                        ship.uAngularVel.x = ship.uAngularVel.x + vector9.x * num66;
                        ship.uAngularVel.y = ship.uAngularVel.y + vector9.y * num66;
                        ship.uAngularVel.z = ship.uAngularVel.z + vector9.z * num66;
                        float num67 = (float)Math.Sqrt((double)(ship.uAngularVel.x * ship.uAngularVel.x + ship.uAngularVel.y * ship.uAngularVel.y + ship.uAngularVel.z * ship.uAngularVel.z));
                        if (num67 > 0f)
                        {
                            double num68 = (double)num67 * 0.016666666666666666 * (double)farFromAstroCheckStep * 0.5;
                            float w = (float)Math.Cos(num68);
                            float num69 = (float)Math.Sin(num68) / num67;
                            Quaternion lhs3 = new Quaternion(ship.uAngularVel.x * num69, ship.uAngularVel.y * num69, ship.uAngularVel.z * num69, w);
                            ship.uRot = lhs3 * ship.uRot;
                        }
                        if (ship.warpState > 0f)
                        {
                            float num70 = ship.warpState * ship.warpState * ship.warpState;
                            ship.uRot = Quaternion.Slerp(ship.uRot, Quaternion.LookRotation(vector6, vector5), num70);
                            ship.uAngularVel *= 1f - num70;
                        }
                        if (distFromStation < 100.0)
                        {
                            float num71 = 1f - (float)distFromStation / 100f;
                            num71 = (3f - num71 - num71) * num71 * num71;
                            num71 *= num71;
                            if (ship.direction > 0)
                            {
                                uRot = Quaternion.Slerp(ship.uRot, targetPlanetAstroData.uRot * (gStationPool[ship.otherGId].shipDockRot * new Quaternion(0.70710677f, 0f, 0f, -0.70710677f)), num71);
                            }
                            else
                            {
                                Vector3 vector10 = (ship.uPos - beginPlanetAstroData.uPos).normalized;
                                Vector3 normalized = (ship.uVel - Vector3.Dot(ship.uVel, vector10) * vector10).normalized;
                                uRot = Quaternion.Slerp(ship.uRot, Quaternion.LookRotation(normalized, vector10), num71);
                            }
                            flag3 = true;
                        }
                    }
                    double num72 = (double)ship.uSpeed * 0.016666666666666666;
                    //--------------------------------------------------------------------------
                    double oriX = ship.uPos.x;
                    double oriY = ship.uPos.y;
                    double oriZ = ship.uPos.z;
                    //--------------------------------------------------------------------------
                    ship.uPos.x = ship.uPos.x + (double)ship.uVel.x * num72 + vectorLF5.x;
                    ship.uPos.y = ship.uPos.y + (double)ship.uVel.y * num72 + vectorLF5.y;
                    ship.uPos.z = ship.uPos.z + (double)ship.uVel.z * num72 + vectorLF5.z;

                    //-------------------------------------------------------------------------- 这里是为了超出折跃场时，别冲出去太多
                    if (enabled && enteredWarpArrayListIndex >= 0)
                    {
                        VectorLF3 realDisVector = ship.uPos - arrays[enteredWarpArrayListIndex].uPos;
                        double squaredRealDis = realDisVector.x * realDisVector.x + realDisVector.y * realDisVector.y + realDisVector.z * realDisVector.z;
                        if (squaredRealDis > arrays[enteredWarpArrayListIndex].squaredRadius)
                        {
                            double backwardDist = Math.Sqrt(squaredRealDis) - arrays[enteredWarpArrayListIndex].radius;
                            double shouldSpeed = num72 - backwardDist;
                            ship.uPos.x = oriX + (double)ship.uVel.x * shouldSpeed + vectorLF5.x;
                            ship.uPos.y = oriY + (double)ship.uVel.y * shouldSpeed + vectorLF5.y;
                            ship.uPos.z = oriZ + (double)ship.uVel.z * shouldSpeed + vectorLF5.z;
                            ship.uSpeed *= (float)(shouldSpeed / num72);
                        }
                    }
                    //--------------------------------------------------------------------------
                    if (flag5)
                    {
                        ship.uRot = uRot;
                        if (ship.direction > 0)
                        {
                            ship.pPosTemp = Maths.QInvRotateLF(targetPlanetAstroData.uRot, ship.uPos - targetPlanetAstroData.uPos);
                            ship.pRotTemp = Quaternion.Inverse(targetPlanetAstroData.uRot) * ship.uRot;
                        }
                        else
                        {
                            ship.pPosTemp = Maths.QInvRotateLF(beginPlanetAstroData.uRot, ship.uPos - beginPlanetAstroData.uPos);
                            ship.pRotTemp = Quaternion.Inverse(beginPlanetAstroData.uRot) * ship.uRot;
                        }
                        uRot.x = (uRot.y = (uRot.z = 0f));
                        uRot.w = 1f;
                        flag3 = false;
                    }
                    if (ptr3.anim.z > 1f)
                    {
                        ref ShipRenderingData ptr8 = ref ptr3;
                        ptr8.anim.z = ptr8.anim.z - 0.0050000004f;
                    }
                    else
                    {
                        ptr3.anim.z = 1f;
                    }
                    ptr3.anim.w = ship.warpState;
                    goto IL_2D53;
                }
                if (ship.stage == 1)
                {
                    float num73;
                    if (ship.direction > 0)
                    {
                        ship.t -= num16 * 0.6666667f;
                        num73 = ship.t;
                        if (ship.t < 0f)
                        {
                            ship.t = 1f;
                            num73 = 0f;
                            ship.stage = 2;
                        }
                        num73 = (3f - num73 - num73) * num73 * num73;
                        float num74 = num73 * 2f;
                        float num75 = num73 * 2f - 1f;
                        VectorLF3 lhs4 = targetPlanetAstroData.uPos + Maths.QRotateLF(targetPlanetAstroData.uRot, gStationPool[ship.otherGId].shipDockPos + gStationPool[ship.otherGId].shipDockPos.normalized * 7.2700005f);
                        if (num73 > 0.5f)
                        {
                            VectorLF3 lhs5 = targetPlanetAstroData.uPos + Maths.QRotateLF(targetPlanetAstroData.uRot, ship.pPosTemp);
                            ship.uPos = lhs4 * (double)(1f - num75) + lhs5 * (double)num75;
                            ship.uRot = targetPlanetAstroData.uRot * Quaternion.Slerp(gStationPool[ship.otherGId].shipDockRot * new Quaternion(0.70710677f, 0f, 0f, -0.70710677f), ship.pRotTemp, num75 * 1.5f - 0.5f);
                        }
                        else
                        {
                            VectorLF3 lhs6 = targetPlanetAstroData.uPos + Maths.QRotateLF(targetPlanetAstroData.uRot, gStationPool[ship.otherGId].shipDockPos + gStationPool[ship.otherGId].shipDockPos.normalized * -14.4f);
                            ship.uPos = lhs6 * (double)(1f - num74) + lhs4 * (double)num74;
                            ship.uRot = targetPlanetAstroData.uRot * (gStationPool[ship.otherGId].shipDockRot * new Quaternion(0.70710677f, 0f, 0f, -0.70710677f));
                        }
                    }
                    else
                    {
                        ship.t += num16;
                        num73 = ship.t;
                        if (ship.t > 1f)
                        {
                            ship.t = 1f;
                            num73 = 1f;
                            ship.stage = 0;
                        }
                        num73 = (3f - num73 - num73) * num73 * num73;
                        ship.uPos = targetPlanetAstroData.uPos + Maths.QRotateLF(targetPlanetAstroData.uRot, gStationPool[ship.otherGId].shipDockPos + gStationPool[ship.otherGId].shipDockPos.normalized * (-14.4f + 39.4f * num73));
                        ship.uRot = targetPlanetAstroData.uRot * (gStationPool[ship.otherGId].shipDockRot * new Quaternion(0.70710677f, 0f, 0f, -0.70710677f));
                    }
                    ship.uVel.x = 0f;
                    ship.uVel.y = 0f;
                    ship.uVel.z = 0f;
                    ship.uSpeed = 0f;
                    ship.uAngularVel.x = 0f;
                    ship.uAngularVel.y = 0f;
                    ship.uAngularVel.z = 0f;
                    ship.uAngularSpeed = 0f;
                    ptr3.anim.z = num73 * 1.7f - 0.7f;
                    goto IL_2D53;
                }
                if (ship.direction > 0)
                {
                    ship.t -= 0.0334f;
                    if (ship.t < 0f)
                    {
                        ship.t = 0f;
                        StationComponent stationComponent = gStationPool[ship.otherGId];
                        StationStore[] array4 = stationComponent.storage;
                        if (totalDistance > __instance.warpEnableDist && ship.warperCnt == 0 && stationComponent.warperCount > 0)
                        {
                            lock (consumeRegister)
                            {
                                ship.warperCnt++;
                                stationComponent.warperCount--;
                                consumeRegister[1210]++;
                            }
                        }
                        if (ship.itemCount > 0)
                        {
                            stationComponent.AddItem(ship.itemId, ship.itemCount, ship.inc);
                            factory.NotifyShipDelivery(ship.planetA, __instance, ship.planetB, stationComponent, ship.itemId, ship.itemCount);
                            ship.itemCount = 0;
                            ship.inc = 0;
                            if (__instance.workShipOrders[j].otherStationGId > 0)
                            {
                                StationStore[] obj = array4;
                                lock (obj)
                                {
                                    if (array4[__instance.workShipOrders[j].otherIndex].itemId == __instance.workShipOrders[j].itemId)
                                    {
                                        StationStore[] array5 = array4;
                                        int otherIndex = __instance.workShipOrders[j].otherIndex;
                                        array5[otherIndex].remoteOrder = array5[otherIndex].remoteOrder - __instance.workShipOrders[j].otherOrdered;
                                    }
                                }
                                __instance.workShipOrders[j].ClearOther();
                            }
                            if (__instance.remotePairOffsets != null && __instance.remotePairOffsets[6] > 0)
                            {
                                int num76;
                                int num77;
                                if (__instance.routePriority == ERemoteRoutePriority.Prioritize)
                                {
                                    num76 = 1;
                                    num77 = 5;
                                }
                                else if (__instance.routePriority == ERemoteRoutePriority.Only || __instance.routePriority == ERemoteRoutePriority.Designated)
                                {
                                    num76 = 1;
                                    num77 = 4;
                                }
                                else
                                {
                                    num76 = 0;
                                    num77 = 0;
                                }
                                bool flag6 = true;
                                for (int m = num76; m <= num77; m++)
                                {
                                    int num78 = __instance.remotePairOffsets[m + 1] - __instance.remotePairOffsets[m];
                                    if (num78 > 0)
                                    {
                                        int num79 = __instance.remotePairOffsets[m];
                                        __instance.remotePairProcesses[m] = __instance.remotePairProcesses[m] % num78;
                                        int num80 = __instance.remotePairProcesses[m];
                                        int num81 = __instance.remotePairProcesses[m];
                                        StationStore[] obj;
                                        SupplyDemandPair supplyDemandPair;
                                        for (; ; )
                                        {
                                            supplyDemandPair = __instance.remotePairs[num81 + num79];
                                            if (supplyDemandPair.demandId != __instance.gid || supplyDemandPair.supplyId != stationComponent.gid)
                                            {
                                                goto IL_2757;
                                            }
                                            if ((int)__instance.priorityLocks[supplyDemandPair.demandIndex].priorityIndex < m && __instance.priorityLocks[supplyDemandPair.demandIndex].lockTick > 0)
                                            {
                                                num81++;
                                                num81 %= num78;
                                            }
                                            else
                                            {
                                                if ((int)stationComponent.priorityLocks[supplyDemandPair.supplyIndex].priorityIndex >= m || stationComponent.priorityLocks[supplyDemandPair.supplyIndex].lockTick <= 0)
                                                {
                                                    obj = __instance.storage;
                                                    lock (obj)
                                                    {
                                                        num4 = __instance.storage[supplyDemandPair.demandIndex].remoteDemandCount;
                                                        num5 = __instance.storage[supplyDemandPair.demandIndex].totalDemandCount;
                                                        itemId = __instance.storage[supplyDemandPair.demandIndex].itemId;
                                                    }
                                                    goto IL_2757;
                                                }
                                                num81++;
                                                num81 %= num78;
                                            }
                                        IL_29F4:
                                            if (num80 == num81)
                                            {
                                                break;
                                            }
                                            continue;
                                        IL_2757:
                                            if (supplyDemandPair.demandId == __instance.gid && supplyDemandPair.supplyId == stationComponent.gid)
                                            {
                                                obj = array4;
                                                lock (obj)
                                                {
                                                    num6 = array4[supplyDemandPair.supplyIndex].count;
                                                    num7 = array4[supplyDemandPair.supplyIndex].inc;
                                                    num8 = array4[supplyDemandPair.supplyIndex].remoteSupplyCount;
                                                    num9 = array4[supplyDemandPair.supplyIndex].totalSupplyCount;
                                                }
                                            }
                                            if (supplyDemandPair.demandId == __instance.gid && supplyDemandPair.supplyId == stationComponent.gid)
                                            {
                                                if (num4 > 0 && num5 > 0)
                                                {
                                                    if (num6 >= shipCarries && num8 >= shipCarries && num9 >= shipCarries)
                                                    {
                                                        goto Block_124;
                                                    }
                                                    stationComponent.SetPriorityLock(supplyDemandPair.supplyIndex, m);
                                                }
                                                else if (num6 <= shipCarries || num8 <= shipCarries || num9 <= shipCarries)
                                                {
                                                    stationComponent.SetPriorityLock(supplyDemandPair.supplyIndex, m);
                                                }
                                            }
                                            num81++;
                                            num81 %= num78;
                                            goto IL_29F4;
                                        }
                                    IL_29FD:
                                        if (flag6)
                                        {
                                            goto IL_2A04;
                                        }
                                        break;
                                    Block_124:
                                        int num82 = (shipCarries < num6) ? shipCarries : num6;
                                        int num83 = num6;
                                        int num84 = num7;
                                        int num85 = __instance.split_inc(ref num83, ref num84, num82);
                                        ship.itemId = (__instance.workShipOrders[j].itemId = itemId);
                                        ship.itemCount = num82;
                                        ship.inc = num85;
                                        obj = array4;
                                        lock (obj)
                                        {
                                            StationStore[] array6 = array4;
                                            int supplyIndex = supplyDemandPair.supplyIndex;
                                            array6[supplyIndex].count = array6[supplyIndex].count - num82;
                                            StationStore[] array7 = array4;
                                            int supplyIndex2 = supplyDemandPair.supplyIndex;
                                            array7[supplyIndex2].inc = array7[supplyIndex2].inc - num85;
                                        }
                                        __instance.workShipOrders[j].otherStationGId = stationComponent.gid;
                                        __instance.workShipOrders[j].thisIndex = supplyDemandPair.demandIndex;
                                        __instance.workShipOrders[j].otherIndex = supplyDemandPair.supplyIndex;
                                        __instance.workShipOrders[j].thisOrdered = num82;
                                        __instance.workShipOrders[j].otherOrdered = 0;
                                        obj = __instance.storage;
                                        lock (obj)
                                        {
                                            StationStore[] array8 = __instance.storage;
                                            int demandIndex = supplyDemandPair.demandIndex;
                                            array8[demandIndex].remoteOrder = array8[demandIndex].remoteOrder + num82;
                                        }
                                        __instance.SetPriorityLock(supplyDemandPair.demandIndex, m);
                                        stationComponent.SetPriorityLock(supplyDemandPair.supplyIndex, m);
                                        flag6 = false;
                                        goto IL_29FD;
                                    }
                                IL_2A04:;
                                }
                            }
                        }
                        else
                        {
                            int itemId2 = ship.itemId;
                            int num86 = shipCarries;
                            int inc;
                            stationComponent.TakeItem(ref itemId2, ref num86, out inc);
                            ship.itemCount = num86;
                            ship.inc = inc;
                            StationStore[] obj;
                            if (__instance.workShipOrders[j].otherStationGId > 0)
                            {
                                obj = array4;
                                lock (obj)
                                {
                                    if (array4[__instance.workShipOrders[j].otherIndex].itemId == __instance.workShipOrders[j].itemId)
                                    {
                                        StationStore[] array9 = array4;
                                        int otherIndex2 = __instance.workShipOrders[j].otherIndex;
                                        array9[otherIndex2].remoteOrder = array9[otherIndex2].remoteOrder - __instance.workShipOrders[j].otherOrdered;
                                    }
                                }
                                __instance.workShipOrders[j].ClearOther();
                            }
                            obj = __instance.storage;
                            lock (obj)
                            {
                                if (__instance.storage[__instance.workShipOrders[j].thisIndex].itemId == __instance.workShipOrders[j].itemId && __instance.workShipOrders[j].thisOrdered != num86)
                                {
                                    int num87 = num86 - __instance.workShipOrders[j].thisOrdered;
                                    StationStore[] array10 = __instance.storage;
                                    int thisIndex2 = __instance.workShipOrders[j].thisIndex;
                                    array10[thisIndex2].remoteOrder = array10[thisIndex2].remoteOrder + num87;
                                    RemoteLogisticOrder[] array11 = __instance.workShipOrders;
                                    int num88 = j;
                                    array11[num88].thisOrdered = array11[num88].thisOrdered + num87;
                                }
                            }
                        }
                        ship.direction = -1;
                    }
                }
                else
                {
                    ship.t += 0.0334f;
                    if (ship.t > 1f)
                    {
                        ship.t = 0f;
                        ship.stage = 1;
                    }
                }
                ship.uPos = targetPlanetAstroData.uPos + Maths.QRotateLF(targetPlanetAstroData.uRot, gStationPool[ship.otherGId].shipDockPos + gStationPool[ship.otherGId].shipDockPos.normalized * -14.4f);
                ship.uVel.x = 0f;
                ship.uVel.y = 0f;
                ship.uVel.z = 0f;
                ship.uSpeed = 0f;
                ship.uRot = targetPlanetAstroData.uRot * (gStationPool[ship.otherGId].shipDockRot * new Quaternion(0.70710677f, 0f, 0f, -0.70710677f));
                ship.uAngularVel.x = 0f;
                ship.uAngularVel.y = 0f;
                ship.uAngularVel.z = 0f;
                ship.uAngularSpeed = 0f;
                ship.pPosTemp = new VectorLF3(0f, 0f, 0f);
                ship.pRotTemp = new Quaternion(0f, 0f, 0f, 1f);
                ptr3.anim.z = 0f;
                goto IL_2D53;
            IL_2EB3:
                j++;
                continue;
            IL_2D53:
                Vector3 vector11;
                vector11.x = ship.uVel.x * ship.uSpeed;
                vector11.y = ship.uVel.y * ship.uSpeed;
                vector11.z = ship.uVel.z * ship.uSpeed;
                if (flag3)
                {
                    ptr3.SetPose(ref ship.uPos, ref uRot, ref relativePos, ref relativeRot, ref vector11, (ship.itemCount > 0) ? ship.itemId : 0);
                    if (starmap)
                    {
                        __instance.shipUIRenderers[ship.shipIndex].SetPose(ref ship.uPos, ref uRot, (float)totalDistance, ship.uSpeed, (ship.itemCount > 0) ? ship.itemId : 0);
                    }
                }
                else
                {
                    ptr3.SetPose(ref ship.uPos, ref ship.uRot, ref relativePos, ref relativeRot, ref vector11, (ship.itemCount > 0) ? ship.itemId : 0);
                    if (starmap)
                    {
                        __instance.shipUIRenderers[ship.shipIndex].SetPose(ref ship.uPos, ref ship.uRot, (float)totalDistance, ship.uSpeed, (ship.itemCount > 0) ? ship.itemId : 0);
                    }
                }
                if (ptr3.anim.z < 0f)
                {
                    ptr3.anim.z = 0f;
                    goto IL_2EB3;
                }
                goto IL_2EB3;
            }
            __instance.ShipRenderersOnTick(astroPoses, ref relativePos, ref relativeRot);
            for (int n = 0; n < __instance.priorityLocks.Length; n++)
            {
                if (__instance.priorityLocks[n].priorityIndex >= 0)
                {
                    if (__instance.priorityLocks[n].lockTick > 0)
                    {
                        StationPriorityLock[] array12 = __instance.priorityLocks;
                        int num89 = n;
                        array12[num89].lockTick = (byte)(array12[num89].lockTick - 1);
                    }
                    else
                    {
                        __instance.priorityLocks[n].lockTick = 0;
                        __instance.priorityLocks[n].priorityIndex = 0;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 修成物流船能量消耗比例
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), "CalcTripEnergyCost")]
        public static void TripCostPatcher(ref StationComponent __instance, ref long __result)
        {
            int starIndex = __instance.planetId / 100 - 1;
            if (starIndex >= 0 && starIndex < tripEnergyCostRatioByStarIndex.Length)
            {
                if (tripEnergyCostRatioByStarIndex[starIndex] < 1)
                {
                    __result = (long)(__result * tripEnergyCostRatioByStarIndex[starIndex]);
                }
            }
        }


        public static double GetRadiusByEnergyPerFrame(long energyPerFrame)
        {
            double radiusLightYear = Math.Pow(1.0 * energyPerFrame, radiusEnergyPowIndex) / radiusEnergyDivisor;
            if (radiusLightYear > 300)
                radiusLightYear = 300;
            return radiusLightYear * 60 * 40000;
        }

        public static double GetTripEnergyCostRatioByEnergyPerFrame(long energyPerFrame)
        {
            return 1.0 * tripCostEnergyDivisor / (tripCostEnergyDivisor + energyPerFrame);
        }

        public static void Import(BinaryReader r)
        {
            InitBeforeLoad();
            CheckSectorWarpArrays();
        }

        public static void Export(BinaryWriter w)
        {

        }

        public static void IntoOtherSave()
        {
            InitBeforeLoad();
        }
    }
}
