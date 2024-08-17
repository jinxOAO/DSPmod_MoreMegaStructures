using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using System.Collections.Concurrent;
using UnityEngine.UI;
using System.IO;
using System.Security.Cryptography;
using System;

namespace MoreMegaStructure
{
    public enum EStarCannonState : int
    {
        Standby = 0,
        Align = 1,
        Heat = 2,
        Switch = 3, // 已弃用。只有一帧，告诉update需要停火当前目标，不管是否已消灭，接下来进入Aim瞄准目标的阶段
        Aim = 4, // 已弃用。匀速同步旋转每层，瞄准目标
        Fire = 5, // 开火中
        Cooldown = -2,
        Recharge = -1
    }
    public class StarCannon
    {
		public static List<long> energyPerTickRequiredByLevel = new List<long> { 0, 100000000, 500000000, 2000000000, 4000000000, 9000000000, 9000000000 };
		public static List<int> basicDamagePerTickByLevel = new List<int> { 0, 5000, 10000, 15000, 20000, 30000 }; //五级前，伤害是固定的，五级后，伤害是基础伤害+bonus，此外游戏UI显示的HP是实际游戏内计算HP和伤害数值的0.01倍，因此显示在游戏内的秒伤数值需要*60后/100
		// 修改测试用伤害！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！
        public static float bonusDpsPerMW = 0.2f; //5级后，每1MW的能量提供这么多的秒伤。每tick提供的tick伤害也是这个比值
		public static List<int> maxAimCountByLevel = new List<int> { 0, 3, 5, 10, 15, 30 }; //同时瞄准的目标上限
		public static List<int> chargeTickByLevel = new List<int> { 0, 108000, 72000, 54000, 36000, 18000 }; //充能时间，tick
		public static List<int> fireRangeByLevel = new List<int> { 0, 9999, 9999, 9999, 9999, 9999 }; //射程，以光年计
		public static List<int> damageReductionPerLyByLevel = new List<int> { 3, 3, 2, 1, 0, 0 }; //每光年伤害衰减的百分比
        public static List<int> maxFireDurationByLevel = new List<int> { 0, 7200, 7200, 10800, 18000, 60000 }; // 一次开火最大持续时间
        public static ConcurrentDictionary<int, int> noExplodeBullets = new ConcurrentDictionary<int, int>();
        public static List<VectorLF3> laserThickerPosDelta = new List<VectorLF3>();
        public static int damageSlice = 7; // 一个特殊的数值用于标明伤害来自于恒星炮，此外这个数值影响护甲的实际计算比例

        // 参数
        public static int laserBulletNum = 100;
        public static int laserBulletPosDelta = 500; // 主激光发射处随机偏移（激光半径）
        public static int laserBulletEndPosDelta = 75; // 主激光命中处随机偏移（激光命中处半径）
        //public static int laserAttackPosDelta = 1000; // 次级攻击激光发射处随机偏移
        public static int maxSideLaserIntensity = 5; //12束集束激光子弹数
        public static float sideLaserBulletLifeTime = 0.1f;
        public static VectorLF3 normDirection = new VectorLF3(0, 1, 0);
        public static int reverseDirection = 1; //只能是1或者-1，1是北极为炮口，-1则是南极。相当于设计恒星炮时所有层级南北极互换
        public static int renderLevel = 2; // 默认是2，更小可以减少恒星炮渲染的光束数量

        public static GameObject fireButtonObj = null;
        public static Button fireButton = null;
        public static Text fireButtonText = null;
        public static Image fireButtonImage = null;

        // 需要存档的参数
        public static EStarCannonState state = EStarCannonState.Standby; //恒星炮开火阶段。1=瞄准；2=预热旋转且瞄准锁定；3=开火；4=刚消灭一个目标、准备继续连续瞄准（此阶段只有一帧）；5=连续开火的正在瞄准新虫洞；-2=将各层角度还原到随机的其他角度并减慢旋转速度，冷却中；-1=重新充能中；0=充能完毕、待命、可开火。
        public static int priorTargetHiveOriAstroId = -1; // 黑雾巢穴的id = 1000000 + dfHivesByAstro的index
        public static int currentTargetStarIndex = 0;
        public static int time = 0; //恒星炮工作流程计时，均以tick记。负值代表冷却/充能过程
        public static int endAimTime = 999; //最慢的轨道所需的瞄准时间，也就是阶段1的总时间
        public static float rotateSpeedScale = 1;
        public static List<int> currentTargetIds;

        //下面属性可能根据戴森球等级有变化，但并不需要存档        
        public static int starCannonLevel = 1; //恒星炮建造的所属阶段（等级），即完成度
        public static int damagePerTick = 4000; //每tick伤害
        public static int damageReductionPerLy = 0; // 每光年损失的伤害百分比
        public static double maxRange = 100.0; //恒星炮最大开火距离，以光年计，1ly = 60AU = 60 * 40000m。
        public static int warmTimeNeed = 240; //阶段2预热加速旋转需要的tick时间
        public static int cooldownTimeNeed = 600; //阶段5冷却需要的tick时间
        public static int chargingTimeNeed = 45 * 3600; //阶段-1的重新充能需要的tick时间
        public static float reAimAngularSpeed = 10f; //连续瞄准时，所有层以同一个速度旋转瞄准到下一个虫洞
        public static int maxAimCount = 3; //连续瞄准次数上限，新版本改成了同时射击目标上限
        public static List<double> layerRotateSpeed; //不需要存档，每次随机生成即可
        public static int maxFireDuration = 6000; // 持续开火最大时间

        // 其他运行时参数
        public static SpaceSector spaceSector; // 每次读档更新
        public static bool needReAlign = false; // 每次读档置为true，因为发现了瞄准过程中读档会读不到各个壳层的正在旋转的数据，所以如果读档时发现state为align则进行一次新的对齐操作
        public static float chargeSpeedFactorByTCFV = 1; // 深空来敌元驱动聚能环加速充能速度
        public static float damageFactorByTCFV = 1; // 深空来敌增伤

        //每帧更新不需要存档
        public static int starCannonStarIndex = -1; //恒星炮所在恒星的index，每帧更新

        public static void InitBeforeLoad()
        {
            noExplodeBullets = new ConcurrentDictionary<int, int>();
            layerRotateSpeed = new List<double>();
            for (int i = 0; i < 22; i++)
            {
                double speed = Utils.RandF() - 0.5;
                if (speed < 0.2 && speed > -0.2)
                    speed *= 2;
                layerRotateSpeed.Add(speed);
            }
            starCannonStarIndex = -1;
            starCannonLevel = 0;
            time = 0;
            endAimTime = 0;
            state = EStarCannonState.Standby;
            UIStarCannon.InitWhenLoad();
            int ratio = renderLevel > 2 ? 2 : renderLevel;
            laserBulletNum = (int)(49.5 * renderLevel + 1);
            laserBulletPosDelta = 500 * ratio;
            laserBulletEndPosDelta = 75 * ratio;
            maxSideLaserIntensity = 2 * ratio + 1;
            sideLaserBulletLifeTime = 0.016f * ratio + 0.003f;
            spaceSector = GameMain.data.spaceSector;
            priorTargetHiveOriAstroId = -1;
            laserThickerPosDelta = new List<VectorLF3>();
            currentTargetIds = new List<int>();
            int x=-1, y=-1, z=-1;
            for (int i = 0; i < 27; i++)
            {
                laserThickerPosDelta.Add(new VectorLF3(x, y, z));
                z++;
                if(z > 1)
                {
                    z = -1;
                    y++;
                }
                if (y > 1)
                {
                    y = -1;
                    x++;
                }
            }
            reverseDirection = MoreMegaStructure.ReverseStarCannonShellAlignDirection.Value ? -1 : 1;
            //RefreshStarCannonProperties();
        }

        public static void InitAfterLoad()
        {
            if(state == EStarCannonState.Align)
            {
                needReAlign = true;
            }
        }

        /// <summary>
        /// 重新计算恒星炮当前属性
        /// </summary>
        public static void RefreshStarCannonProperties()
        {
            if (starCannonStarIndex >= 0 && starCannonStarIndex < 1000 && MoreMegaStructure.StarMegaStructureType[starCannonStarIndex] == 6 && starCannonStarIndex < GameMain.galaxy.starCount)
            {
                int[] res = GetStarCannonProperties(GameMain.data.dysonSpheres[starCannonStarIndex]);
                starCannonLevel = res[0];
                damagePerTick = res[1];
                maxAimCount = res[2];
                maxRange = res[4];
                damageReductionPerLy = res[5];
                maxFireDuration = res[6];
                if(chargingTimeNeed != res[3]) // 正在冷却中/充能中的炮，按剩余时间比例修改充能时间（因为正在充能的时候阶段改变了，充能时间也改变了）
                {
                    if (state == EStarCannonState.Cooldown)
                    {
                        int coolingTimeLeft = -time - chargingTimeNeed;
                        chargingTimeNeed = res[3];
                        time = -coolingTimeLeft - chargingTimeNeed;
                    }
                    else if (state == EStarCannonState.Recharge)
                    {
                        time = (int)(time * 1.0 / chargingTimeNeed * res[3]);
                        chargingTimeNeed = res[3];
                    }
                    else
                    {
                        chargingTimeNeed = res[3];
                    }
                }
            }
        }

        /// <summary>
        /// 返回恒星炮当前属性
        /// </summary>
        /// <param name="sphere"></param>
        /// <returns></returns>
        public static int[] GetStarCannonProperties(DysonSphere sphere)
        {
            if (sphere == null)
                return new int[] { -1, -1, -1, -1, -1, -1 };

			long cannonEnergy = sphere.energyGenCurrentTick - sphere.energyReqCurrentTick;
			if (cannonEnergy < 0) cannonEnergy = 0;
			int level = 0;
            for (int i = 0; i < 6; i++)
            {
				if (cannonEnergy >= energyPerTickRequiredByLevel[i])
					level = i;
				else
					break;
            }
			int damagePerTick = basicDamagePerTickByLevel[level];
			if(level >= 5)
            {
				damagePerTick += (int)((cannonEnergy - energyPerTickRequiredByLevel[5]) * 1.0 / 1000000.0 * bonusDpsPerMW);
            }
            if (damageFactorByTCFV != 1)
                damagePerTick = (int)(damagePerTick * damageFactorByTCFV);
            int realChargeTick = chargeTickByLevel[level];
            if (chargeSpeedFactorByTCFV > 0)
                realChargeTick = (int)(realChargeTick / chargeSpeedFactorByTCFV);
            return new int[] { level, damagePerTick, maxAimCountByLevel[level], realChargeTick, fireRangeByLevel[level], damageReductionPerLyByLevel[level], maxFireDurationByLevel[level] };
        }

        public static void OnFireButtonClick()
        {
            starCannonStarIndex = MoreMegaStructure.GetStarCannonBuiltIndex();
            RefreshStarCannonProperties();
            if(starCannonStarIndex < 0)
            {
                UIRealtimeTip.Popup("没有规划的恒星炮！".Translate());
                return;
            }
            if (starCannonLevel <= 0)
            {
                UIRealtimeTip.Popup("恒星炮修建中警告".Translate());
                return;
            }
            UIStarmap starmap = UIRoot.instance.uiGame.starmap;
            if (starmap == null)
                return;
            if (state == EStarCannonState.Standby) // 准备开火
            {
                bool canFire = false;

                if (starmap.focusPlanet != null || starmap.focusStar != null)
                {
                    StarData star = starmap.focusPlanet?.planet?.star;
                    if (star == null)
                        star = starmap.focusStar.star;
                    currentTargetStarIndex = star.index;
                    if (currentTargetStarIndex == starCannonStarIndex)
                    {
                        UIRealtimeTip.Popup("恒星炮不能向自身所在星系开火！".Translate());
                        currentTargetStarIndex = 0;
                        return;
                    }
                    SkillTarget currentTarget = SearchNextTarget();
                    priorTargetHiveOriAstroId = currentTarget.astroId;
                    currentTargetIds = new List<int>();
                    
                    if(CheckAndSearchAllTargets())
                        canFire = true;
                    else
                        UIRealtimeTip.Popup("目标无法定位警告".Translate());
                }
                else if(starmap.focusHive != null && starmap.focusHive.hive.starData.index >= 0)
                {
                    currentTargetStarIndex = starmap.focusHive.hive.starData.index;
                    if (currentTargetStarIndex == starCannonStarIndex)
                    {
                        UIRealtimeTip.Popup("恒星炮不能向自身所在星系开火！".Translate());
                        currentTargetStarIndex = 0;
                        return;
                    }
                    priorTargetHiveOriAstroId = starmap.focusHive.hive.hiveAstroId;
                    SkillTarget currentTarget = SearchNextTarget();
                    currentTargetIds = new List<int>();
                    if (CheckAndSearchAllTargets())
                        canFire = true;
                    else
                        UIRealtimeTip.Popup("目标无法定位警告".Translate());
                }
                else if(starmap.mouseHoverPlanet != null || starmap.mouseHoverStar != null)
                {
                    StarData star = starmap.mouseHoverPlanet?.planet?.star;
                    if (star == null)
                        star = starmap.mouseHoverStar.star;
                    currentTargetStarIndex = star.index;
                    if (currentTargetStarIndex == starCannonStarIndex)
                    {
                        UIRealtimeTip.Popup("恒星炮不能向自身所在星系开火！".Translate());
                        currentTargetStarIndex = 0;
                        return;
                    }
                    SkillTarget currentTarget = SearchNextTarget();
                    priorTargetHiveOriAstroId = currentTarget.astroId;
                    currentTargetIds = new List<int>();

                    if (CheckAndSearchAllTargets())
                        canFire = true;
                    else
                        UIRealtimeTip.Popup("目标无法定位警告".Translate());
                }
                else if (starmap.mouseHoverHive != null)
                {
                    currentTargetStarIndex = starmap.mouseHoverHive.hive.starData.index;
                    if (currentTargetStarIndex == starCannonStarIndex)
                    {
                        UIRealtimeTip.Popup("恒星炮不能向自身所在星系开火！".Translate());
                        currentTargetStarIndex = 0;
                        return;
                    }
                    priorTargetHiveOriAstroId = starmap.mouseHoverHive.hive.hiveAstroId;
                    currentTargetIds = new List<int>();
                    if (CheckAndSearchAllTargets())
                        canFire = true;
                    else
                        UIRealtimeTip.Popup("目标无法定位警告".Translate());
                }
                if (canFire)
                {
                    StartAiming();
                }
            }
            else if (state == EStarCannonState.Cooldown)
            {
                UIRealtimeTip.Popup("恒星炮冷却中警告".Translate());
                return;
            }
            else if (state == EStarCannonState.Recharge)
            {
                UIRealtimeTip.Popup("恒星炮充能中警告".Translate());
                return;
            }
            else // 开火过程中允许进行同星系内的目标切换
            {
                if (starmap.focusPlanet != null && starmap.focusPlanet.planet.star.index >= 0 || starmap.focusStar != null && starmap.focusStar.star.index >= 0)
                {
                    StarData star = starmap.focusPlanet?.planet?.star;
                    if (star == null)
                        star = starmap.focusStar.star;
                    if(currentTargetStarIndex != star.index)
                        UIRealtimeTip.Popup("无法更改目标星系警告".Translate());
                    else
                        UIRealtimeTip.Popup("恒星炮已经启动警告".Translate());
                }
                else if (starmap.focusHive != null && starmap.focusHive.hive.starData.index >= 0)
                {
                    int wantTargetStarIndex = starmap.focusHive.hive.starData.index;
                    if (wantTargetStarIndex != currentTargetStarIndex)
                    {
                        UIRealtimeTip.Popup("无法更改目标星系警告".Translate());
                    }
                    else
                    {
                        priorTargetHiveOriAstroId = starmap.focusHive.hive.hiveAstroId;
                        currentTargetIds = new List<int>();
                        if (!CheckAndSearchAllTargets()) // 可能是玩家置顶的黑雾巢穴没有存活的目标，反正就是没找到属于这个巢穴的开火目标
                        {
                            priorTargetHiveOriAstroId = -1; // 还原没有prior的状态
                            UIRealtimeTip.Popup("无法优先射击该巢穴警告".Translate());
                        }
                        else
                        {
                            //state = EStarCannonState.Switch;
                        }
                    }                    
                }
                else if(starmap.mouseHoverHive != null)
                {
                    StarData star = starmap.mouseHoverHive?.hive?.starData;
                    if (currentTargetStarIndex != star?.index)
                    {
                        UIRealtimeTip.Popup("无法更改目标星系警告".Translate());
                    }
                    else
                    {
                        priorTargetHiveOriAstroId = starmap.mouseHoverHive.hive.hiveAstroId;
                        currentTargetIds = new List<int>();
                        if (!CheckAndSearchAllTargets()) // 可能是玩家置顶的黑雾巢穴没有存活的目标，反正就是没找到属于这个巢穴的开火目标
                        {
                            priorTargetHiveOriAstroId = -1; // 还原没有prior的状态
                            UIRealtimeTip.Popup("无法优先射击该巢穴警告".Translate());
                        }
                    }
                }
            }
            //UIRoot.instance.uiGame.starmap.
        }

        public static void AddNewLaser(VectorLF3 uBegin, VectorLF3 uEnd, int targetId, int damage, int life)
        {
            if (targetId == 0) 
                return;
            if (targetId >= spaceSector.enemyPool.Length || targetId <= 0)
                return;
            ref EnemyData ptr0 = ref spaceSector.enemyPool[targetId];
            if (ptr0.id <= 0) 
                return;
             
            ref SpaceLaserOneShot ptr = ref spaceSector.skillSystem.dfsTowerLasers.Add();
            ptr.astroId = starCannonStarIndex * 100;
            ptr.hitIndex = 32;
            ptr.target.type = ETargetType.Enemy;
            ptr.target.id = ptr0.id;
            ptr.target.astroId = ptr0.originAstroId;
            ptr.caster.type = ETargetType.Craft;
            ptr.caster.id = 1;
            ptr.beginPosU = uBegin;
            ptr.endPosU = uEnd;
            ptr.endVelU = Vector3.zero;
            ptr.muzzleOffset = Vector3.zero;
            ptr.damage = damage;
            ptr.life = life;
            ptr.extendedDistWhenMiss = 0;
            ptr.mask = ETargetTypeMask.Enemy;
        }

        /// <summary>
        /// 检查id号目标是否合法且存活
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool CheckTarget(int id)
        {
            if (spaceSector == null || spaceSector.enemyPool == null)
                return false;

            if (id >= spaceSector.enemyPool.Length || id <= 0)
                return false;

            ref EnemyData ptr0 = ref spaceSector.enemyPool[id];
            if (ptr0.id <= 0) // 目标已不存在
                return false;

            return true;
        }


        public static bool CheckAndSearchAllTargets()
        {
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.EnemySearch);
            if (maxAimCount <= 0)
                return false;
            int count = Mathf.Min(currentTargetIds.Count, maxAimCount);

            while(currentTargetIds.Count > maxAimCount && maxAimCount >= 0)
            {
                currentTargetIds.RemoveAt(currentTargetIds.Count - 1);
            }
            for (int i = 0; i < count; )
            {
                // 此处不对优先的巢穴id做特殊判断，当玩家锁定优先巢穴id时，清空currentTargetIds重新寻找目标
                if (!CheckTarget(currentTargetIds[i]))
                { 
                    currentTargetIds.RemoveAt(i);
                    count--;
                }
                else
                {
                    i++;
                }
            }
            int left = maxAimCount - currentTargetIds.Count;
            if (left < 0)
            {
                return currentTargetIds.Count > 0;
            }
            for (int i = 0; i < left; i++)
            {
                SkillTarget ptr = SearchNextTarget();
                if (ptr.id > 0) 
                {
                    currentTargetIds.Add(ptr.id);
                }
                else
                {
                    break;
                }
            }
            MMSCPU.EndSample(ECpuWorkEntryExtended.EnemySearch);
            MMSCPU.EndSample(ECpuWorkEntryExtended.MoreMegaStructure);
            return currentTargetIds.Count > 0;
        }


        /// <summary>
        /// 根据当前瞄准的星系和是否有优先瞄准的黑雾巢穴的设定，返回下一个合法目标
        /// </summary>
        /// <returns></returns>
        public static SkillTarget SearchNextTarget()
        {
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.EnemySearch);
            SkillTarget skillTarget = default(SkillTarget);
            if (currentTargetStarIndex < 0 || currentTargetStarIndex >= GameMain.galaxy.starCount)
                return skillTarget;
            skillTarget.type = ETargetType.Enemy;
            EnemyData[] enemyPool = spaceSector.enemyPool;
            int enemyCursor = spaceSector.enemyCursor;
            EnemyDFHiveSystem[] dfHivesByAstro = spaceSector.dfHivesByAstro;
            AstroData[] galaxyAstros = spaceSector.galaxyAstros;
            Vector3 vector2 = GameMain.galaxy.stars[currentTargetStarIndex].uPosition;
            VectorLF3 zero = VectorLF3.zero;
            
            for (int m = 0; m < enemyCursor; m++)
            {
                ref EnemyData ptr3 = ref enemyPool[m];
                if (ptr3.id != 0)
                {
                    if (ptr3.dfTinderId != 0) // 不会锁定火种
                        continue;
                    EnemyDFHiveSystem enemyDFHiveSystem = dfHivesByAstro[ptr3.originAstroId - 1000000];
                    if (enemyDFHiveSystem == null || enemyDFHiveSystem.starData.index != currentTargetStarIndex) // 不是目标星系的黑雾巢穴
                        continue;
                    if (priorTargetHiveOriAstroId >= 0 && ptr3.originAstroId != priorTargetHiveOriAstroId)// 玩家要求优先攻击特定的黑雾巢穴，因此不属于特定黑雾巢穴的敌人被跳过
                        continue;
                    if (ptr3.dfSCoreId + ptr3.dfSNodeId + ptr3.dfSConnectorId + ptr3.dfSGammaId + ptr3.dfSTurretId + ptr3.dfSReplicatorId + ptr3.dfRelayId <= 0 && priorTargetHiveOriAstroId > -2) // 不是黑雾巢穴的建筑本体，跳过。恒星炮不会攻击敌舰，除非已经找不到hive作为目标，priorTargetHiveOriAstroId变为-2
                        continue;
                    if (false)
                    {
                        Debug.Log($"dfsNode yes: {ptr3.dfSNodeId} {ptr3.dfSCoreId} {ptr3.dfSConnectorId} {ptr3.dfSGammaId} {ptr3.dfSTurretId} {ptr3.dfSReplicatorId} in astro{ptr3.astroId} and oriAstro {ptr3.originAstroId} and relayId {ptr3.dfRelayId}");
                    }
                    spaceSector.TransformFromAstro_ref(ptr3.astroId, out zero, ref ptr3.pos);
                    // 原逻辑判断是否在开火距离内 恒星炮不判断
                    float num28 = (float)(zero.x - (double)vector2.x);
                    float num29 = num28 * num28;
                    float num30 = (float)(zero.y - (double)vector2.y);
                    float num31 = num30 * num30;
                    float num32 = (float)(zero.z - (double)vector2.z);
                    float num33 = num32 * num32;
                    float num34 = num29 + num31 + num33;
                    bool flag = false;
                    if (ptr3.astroId >= 100 && ptr3.astroId <= 204899)
                    {
                        int num35 = ptr3.astroId / 100 * 100;
                        for (int n = num35 + 1; n < num35 + 8; n++) // 这里貌似是判断不和星体相交，我把初始循环+1是为了排除恒星本体的防相交判断？
                        {
                            AstroData astroData = galaxyAstros[n];
                            float uRadius = astroData.uRadius;
                            if (uRadius >= 1f)
                            {
                                float num36 = (float)astroData.uPos.x - vector2.x;
                                float num37 = (float)astroData.uPos.y - vector2.y;
                                float num38 = (float)astroData.uPos.z - vector2.z;
                                float num39 = num36 * num28 + num37 * num30 + num38 * num32;
                                if (num39 > 0f)
                                {
                                    float num40 = num36 * num36 + num37 * num37 + num38 * num38;
                                    float num41 = num39 * num39 / num34;
                                    flag = (num40 - num41 < uRadius * uRadius && num34 > num41);
                                    flag = false; // 跳过判定
                                }
                            }
                        }
                    }
                    if (!flag) // 攻击第一个找到的
                    {
                        if (!currentTargetIds.Contains(ptr3.id))
                        {
                            skillTarget.id = ptr3.id;
                            skillTarget.astroId = ptr3.originAstroId;
                            return skillTarget;
                        }
                    }

                }
            }
            // 如果找不到合法目标，则改为无优先选中的hive然后再搜索一次，如果已经为无优先hive的情况，priorTargetHiveOriAstroId变为-2，代表允许搜索敌舰
            if (skillTarget.id == 0 && priorTargetHiveOriAstroId >= -1)
            {
                if (priorTargetHiveOriAstroId >= 0)
                    priorTargetHiveOriAstroId = -1; 
                else
                    priorTargetHiveOriAstroId = -2;
                skillTarget = SearchNextTarget();
            }

            MMSCPU.EndSample(ECpuWorkEntryExtended.EnemySearch);
            MMSCPU.EndSample(ECpuWorkEntryExtended.MoreMegaStructure);
            return skillTarget;
        }

        /// <summary>
        /// 处理开始开火的信息
        /// </summary>
        public static void StartAiming()
        {
            time = 0;
            endAimTime = 0;
            state = EStarCannonState.Align;
            rotateSpeedScale = 0.1f;
            layerRotateSpeed = new List<double>();
            for (int i = 0; i<22; i++)
            {
                double speed = Utils.RandF() - 1;
                if (speed < 0.2 && speed > -0.2)
                    speed *= 2;
                layerRotateSpeed.Add(speed);
            }

            UIRealtimeTip.Popup("恒星级武器检测警告".Translate());
        }

        public static void StopFiring()
        {
            if (GameMain.data.dysonSpheres == null)
                return;
            if (GameMain.data.dysonSpheres.Length <= starCannonStarIndex)
                return;
            DysonSphere sphere = GameMain.data.dysonSpheres[starCannonStarIndex];
            //每层旋转到随机位置
            for (int i = 0; i < sphere.layersIdBased.Length; i++)
            {
                if (sphere.layersIdBased[i] != null)
                {
                    DysonSphereLayer layer = sphere.layersIdBased[i];
                    Quaternion randRotation = Quaternion.LookRotation(new VectorLF3(Utils.RandF() - 0.5f, Utils.RandF() - 0.5f, Utils.RandF() - 0.5f));
                    layer.orbitAngularSpeed *= 5.0f; //加快轨道旋转速度，只在下面计算时用到一次，之后可以立刻还原
                    layer.InitOrbitRotation(layer.orbitRotation, randRotation); //每个戴森壳层开始随机旋转到任意方向，这个随机也不是平均分布的……
                    layer.orbitAngularSpeed /= 5.0f; //轨道旋转速度还原
                }
            }

            if (state == EStarCannonState.Align) //代表刚瞄准还没预热就停止“开火”，因此不需要重新充能
            {
                state = EStarCannonState.Standby;
                return;
            }

            //如果至少进入过预热阶段（fireStage=2的阶段），则必须经过完整的冷却和再充能过程，才能再次瞄准、开火
            state = EStarCannonState.Cooldown;
            float rechargeSpeed = 1;
            //if (Relic.HaveRelic(2, 9)) rechargeSpeed += 0.5f;
            //if (Relic.HaveRelic(3, 13)) rechargeSpeed += 1.75f;
            time = -cooldownTimeNeed - (int)(chargingTimeNeed * 1.0f / rechargeSpeed);
            noExplodeBullets.Clear();
            if(MoreMegaStructure.developerMode)
            {
                state = EStarCannonState.Standby;
                time = 0;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DysonSphere), "GameTick")]
        public static void StarCannonUpdate(ref DysonSphere __instance, long gameTick)
        {
            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.StarCannon);
            if (__instance == null || __instance.layersIdBased == null)
                return;

            int starIndex = __instance.starData.index;
            if (starIndex >= 1000)
                return;

            if (MoreMegaStructure.StarMegaStructureType[starIndex] != 6 )
                return;

            if (__instance.energyGenCurrentTick < 10000)
                return;

            RefreshStarCannonProperties();
            starCannonStarIndex = starIndex;

            //bool targetChanged = false;
            if (state == EStarCannonState.Standby)
                return;

            if(state != EStarCannonState.Recharge)
            {
                if(state == EStarCannonState.Align || state == EStarCannonState.Heat)
                {
                    if (!CheckAndSearchAllTargets())
                    {
                        UIRealtimeTip.Popup("目标无法定位警告".Translate());
                        StopFiring();
                    }                    
                }
                else if(state == EStarCannonState.Fire || state == EStarCannonState.Aim || state == EStarCannonState.Switch)
                {
                    if (!CheckAndSearchAllTargets())
                        StopFiring();                    
                }

                // 根据目标恒星位置计算锁定方向信息
                VectorLF3 targetUPos;
                if(currentTargetStarIndex < 0 || currentTargetStarIndex >= GameMain.galaxy.starCount)
                {
                    StopFiring();
                    return;
                }
                targetUPos = GameMain.galaxy.stars[currentTargetStarIndex].uPosition;

                VectorLF3 direction = (targetUPos - __instance.starData.uPosition) * reverseDirection;
                VectorLF3 vert = new VectorLF3(0, 0, 1);
                if (direction.z != 0)
                    vert = new VectorLF3(1, 1, (-direction.x - direction.y) / direction.z);
                Quaternion final = Quaternion.LookRotation(vert, direction);

                // 操作每一个壳层的旋转、瞄准等
                for (int i = 0; i < __instance.layersIdBased.Length; i++)
                {
                    if (__instance.layersIdBased[i] != null)
                    {
                        DysonSphereLayer layer = __instance.layersIdBased[i];

                        if (state == EStarCannonState.Align && (time <= 1 || needReAlign)) //原本第二个条件是是time==0，但可能会出现不进行瞄准动画的问题，因此改成了<=1，大不了瞄准两次
                        {
                            float originAngularSpeed = layer.orbitAngularSpeed;
                            layer.orbitAngularSpeed = reAimAngularSpeed;
                            layer.InitOrbitRotation(layer.orbitRotation, final); //每个戴森壳层开始轨道旋转、对齐瞄准
                            float aimTimeNeed = Quaternion.Angle(layer.orbitRotation, final) / layer.orbitAngularSpeed * 60f;
                            endAimTime = Mathf.Max(time + endAimTime, time + (int)aimTimeNeed); //保存瞄准完成所需的最大时间
                            layer.orbitAngularSpeed = originAngularSpeed;
                        }

                        //旋转
                        if ((int)state >= 2 || state == EStarCannonState.Cooldown)
                        {
                            layer.currentAngle += rotateSpeedScale * (float)layerRotateSpeed[i];
                        }

                        //目标锁定和旋转速度设置
                        if (state == EStarCannonState.Fire) //如果不是连续开火的瞄准阶段，也不是停火后的冷却阶段
                        {
                            layer.orbitRotation = final; //瞄准方向锁定在目标上
                        }

                        if(state == EStarCannonState.Fire) // 开火中，锁定炮口方向指向目标
                        {
                            layer.orbitRotation = final;
                        }

                        //预热时就开始的集束激光效果
                        if ((int)state >= 2)
                        {
                            int laserIntensity = (int)((time - endAimTime) * 1.0f / warmTimeNeed * maxSideLaserIntensity); //决定激光强度，这个逻辑是预热时周围集束激光效果随时间增强
                            if (laserIntensity > maxSideLaserIntensity || (int)state >= 3)
                            {
                                laserIntensity = maxSideLaserIntensity;
                            }
                            LaserEffect3(__instance, gameTick, laserIntensity, targetUPos);
                            LaserEffect2(__instance, gameTick, targetUPos);
                        }
                    }
                }
                if ((int)state >= 2) // 预热阶段，壳层逐渐加速旋转
                {
                    if (rotateSpeedScale < 3f)
                        rotateSpeedScale += 0.01f;
                }
                else if ((int)state < 0) // 冷却、停止开火阶段，壳层减速旋转
                {
                    if (rotateSpeedScale > 0.05f)
                        rotateSpeedScale -= 0.01f;
                }
                needReAlign = false;

                // 开火等特效和伤害逻辑处理
                if ((int)state >= 3)
                {
                    // 转火过程中保持开炮状态且方向与当前旋转进度同步
                    //VectorLF3 targetUPosFar = VectorLF3.zero;
                    //DysonSphereLayer layer = null;


                    // 游戏内的激光特效及伤害
                    Vector3 centerStarUPos = Vector3.zero;
                    if (currentTargetStarIndex >= 0 && currentTargetStarIndex < GameMain.galaxy.starCount)
                        centerStarUPos = GameMain.galaxy.StarById(currentTargetStarIndex + 1).uPosition;
                    int count = Mathf.Min(maxAimCount, currentTargetIds.Count);
                    float ratio = 1.0f;
                    if(damageReductionPerLy > 0)
                    {
                        float reduce = (float)(__instance.starData.uPosition - (VectorLF3)centerStarUPos).magnitude / 40000 / 60 * damageReductionPerLy / 100;
                        if (reduce > 0.8f)
                            reduce = 0.8f;
                        ratio = 1.0f - reduce;
                    }
                    for (int i = 0; i < count; i++)
                    {
                        ref EnemyData ptr0 = ref spaceSector.enemyPool[currentTargetIds[i]];
                        SkillTarget target = default(SkillTarget);
                        target.id = ptr0.id;
                        target.type = ETargetType.Enemy;
                        target.astroId = ptr0.originAstroId;
                        SkillTarget caster = default(SkillTarget);
                        caster.id = 1;
                        caster.type = ETargetType.Craft;
                        caster.astroId = 0;
                        VectorLF3 enemyUPos;
                        Vector3 vec;
                        spaceSector.skillSystem.GetObjectUPositionAndVelocity(ref target, out enemyUPos, out vec);
                        enemyUPos += (VectorLF3)vec * 0.016666667f;
                        AddNewLaser(centerStarUPos, enemyUPos, target.id, 1, 30);
                        spaceSector.skillSystem.DamageObject((int)(damagePerTick * ratio), damageSlice, ref target, ref caster);
                        if (maxAimCount - count > 0 && i == count - 1) // 如果目标过少，少于可以同时射击的最大数量，溢出的可射击的激光将同时射击最后一个合法目标
                        {
                            for (int j = 0; j < maxAimCount - count; j++)
                            {
                                AddNewLaser(centerStarUPos, enemyUPos, target.id, 1, 10);
                                spaceSector.skillSystem.DamageObject((int)(damagePerTick * ratio), damageSlice, ref target, ref caster);
                            }
                        }
                        // 为了让星图模式也显示恒星到敌人的攻击的激光，当玩家查看星图时，设置激光
                        if (UIGame.viewMode == EViewMode.Starmap && UIRoot.instance.uiGame.starmap.viewStarSystem == GameMain.galaxy.StarById(currentTargetStarIndex + 1)) 
                        {
                            DysonSwarm swarm = GameMain.data.dysonSpheres[currentTargetStarIndex]?.swarm;
                            if(swarm != null)
                            {
                                int bulletIndex = swarm.AddBullet(new SailBullet
                                {
                                    maxt = 0.3f,
                                    lBegin = Vector3.zero,
                                    uEndVel = ((Vector3)enemyUPos - centerStarUPos)*3,
                                    uBegin = centerStarUPos,
                                    uEnd = enemyUPos,
                                }, 0);
                                swarm.bulletPool[bulletIndex].state = 0;
                            }
                        }
                    }

                    // 为了让星图模式也显示恒星到恒星的激光激光，用深空来敌老套路创建太阳帆发射轨迹
                    StarData starData = __instance.starData;
                    int nearPoint = 400000;
                    if (__instance.starData.type == EStarType.GiantStar)
                        nearPoint = 1000000;
                    if (currentTargetStarIndex < GameMain.data.dysonSpheres.Length)
                    {
                        DysonSwarm targetSwarm = GameMain.data.dysonSpheres[currentTargetStarIndex]?.swarm;
                        DysonSwarm casterSwarm = __instance.swarm;
                        int lessBulletRatio = 1;
                        if (casterSwarm != null)
                        {
                            //VectorLF3 direction = targetUPos - starData.uPosition;
                            for (int i = 0; i < laserBulletNum / lessBulletRatio || i == 0; i++)
                            {
                                int bulletIndex = casterSwarm.AddBullet(new SailBullet
                                {
                                    maxt = 0.3f,
                                    lBegin = __instance.starData.uPosition,
                                    uEndVel = targetUPos,
                                    uBegin = __instance.starData.uPosition + Utils.RandPosDelta() * laserBulletPosDelta,
                                    uEnd = ((i == 0) ? (targetUPos + Utils.RandPosDelta() * laserBulletEndPosDelta) : ((targetUPos - __instance.starData.uPosition).normalized * nearPoint * (1+ Utils.RandF() * 3)) + __instance.starData.uPosition),
                                }, 0);
                                casterSwarm.bulletPool[bulletIndex].state = 0;
                                if (i >= 1)
                                {
                                    //noExplodeBullets.AddOrUpdate(bulletIndex, 1, (x, y) => 1);
                                }
                            }
                        }

                        //不在同星系，本星系内光会很细，增加一段短光
                        int dec = 5;
                        if (renderLevel >= 3) dec = 2;
                        if (casterSwarm != null)
                        {
                            for (int i = 0; i < laserBulletNum / dec || i == 0; i++)
                            {
                                int bulletIndex = __instance.swarm.AddBullet(new SailBullet
                                {
                                    maxt = 0.3f,
                                    lBegin = __instance.starData.uPosition,
                                    uEndVel = targetUPos,
                                    uBegin = __instance.starData.uPosition + Utils.RandPosDelta() * laserBulletEndPosDelta / dec,
                                    uEnd = (targetUPos - __instance.starData.uPosition).normalized * nearPoint + __instance.starData.uPosition + Utils.RandPosDelta() * laserBulletEndPosDelta / dec
                                }, 0);
                                __instance.swarm.bulletPool[bulletIndex].state = 0;

                                //noExplodeBullets.AddOrUpdate(bulletIndex, 1, (x, y) => 1);
                            }
                        }

                        //如果不在同星系，接收星系需要同样生成光束（由于发射星系的光束不会在观察目标星系时渲染），此部分是必须的
                        //但是减小了光线粗细和粒子数量
                        if (targetSwarm != null && targetSwarm.starData.index != __instance.starData.index)
                        {
                            //无需改变生成点和终点
                            for (int i = 0; i < laserBulletNum / dec || i == 0; i++)
                            {
                                int bulletIndex = targetSwarm.AddBullet(new SailBullet
                                {
                                    maxt = 0.3f,
                                    lBegin = __instance.starData.uPosition,
                                    uEndVel = targetUPos,
                                    uBegin = __instance.starData.uPosition + Utils.RandPosDelta() * (laserBulletPosDelta / dec),
                                    uEnd = targetUPos + Utils.RandPosDelta() * (laserBulletEndPosDelta / dec * 2)
                                }, 0);
                                targetSwarm.bulletPool[bulletIndex].state = 0;
                                if (i >= 1)
                                {
                                    //noExplodeBullets.AddOrUpdate(bulletIndex, 1, (x, y) => 1);
                                }
                            }
                        }

                    }
                }
            }

            // 状态计时结算与转换
            time += 1;
            if (state == EStarCannonState.Align && time >= endAimTime * 0.98f)
            {
                state = EStarCannonState.Heat;
            }
            else if (state == EStarCannonState.Heat && time >= endAimTime + warmTimeNeed)
            {
                state = EStarCannonState.Fire;
            }
            else if (state == EStarCannonState.Fire && time >= endAimTime + warmTimeNeed + maxFireDuration)
            {
                StopFiring();
            }
            else if (state == EStarCannonState.Cooldown && time >= -chargingTimeNeed)
            {
                state = EStarCannonState.Recharge;
            }
            else if (state == EStarCannonState.Recharge && time >= 0)
            {
                state = EStarCannonState.Standby;
                time = 0;
            }
            MMSCPU.EndSample(ECpuWorkEntryExtended.StarCannon);
            MMSCPU.EndSample(ECpuWorkEntryExtended.MoreMegaStructure);
        }

        public static void LaserEffect2(DysonSphere sphere, long gameTick, VectorLF3 targetUPos)
        {
            return;
            var __instance = sphere;
            VectorLF3 targetDirection = targetUPos - __instance.starData.uPosition;
            VectorLF3 vertDirection = new VectorLF3(0, 0, 1);
            //float minRadius = 99999999;
            float maxRadius = 0;
            int maxRadiusLayerId = 2;
            for (int i = 2; i < 5; i++) //寻找除了第一层之外最大的壳层的id
            {
                if (__instance.layersIdBased[i] != null)
                {
                    if (__instance.layersIdBased[i].orbitRadius > maxRadius)
                    {
                        maxRadius = __instance.layersIdBased[i].orbitRadius;
                        maxRadiusLayerId = i;
                    }
                }
            }
            VectorLF3 beginPointInStar = __instance.starData.uPosition;
            for (int i = 1; i < 5; i++)
            {
                if (__instance.layersIdBased[i] != null && renderLevel >= 2 && (i == 1 || i == maxRadiusLayerId || renderLevel >= 3)) //renderLevel = 2的时候只有第1层和半径最大层有光效。renderLevel=3则前五层都有。
                {
                    DysonSphereLayer layer = __instance.layersIdBased[i];
                    for (int j = 0; j < layer.nodeCursor; j++)
                    {
                        if (layer.nodePool[j] == null)
                            continue;
                        VectorLF3 endPByNode = layer.NodeUPos(layer.nodePool[j]);
                        int bulletIndex = __instance.swarm.AddBullet(new SailBullet
                        {
                            maxt = 0.01f,
                            lBegin = __instance.starData.uPosition,
                            uEndVel = targetUPos,
                            uBegin = beginPointInStar,
                            uEnd = endPByNode
                        }, 0);
                        __instance.swarm.bulletPool[bulletIndex].state = 0;
                        //noExplodeBullets.AddOrUpdate(bulletIndex, 1, (x, y) => 1);
                    }
                }
            }

        }

        //其他效果3，集束激光效果，类似1，但是起点是层1的随机12个node，因此推荐层1只造12个node。而终点在炮口前方。
        public static void LaserEffect3(DysonSphere sphere, long gameTick, int laserIntensity, VectorLF3 targetUPos)
        {
            var __instance = sphere;
            VectorLF3 targetDirection = targetUPos - __instance.starData.uPosition;
            double distance = targetDirection.magnitude;
            VectorLF3 vertDirection = new VectorLF3(0, 0, 1);
            float focusDistance = 0;
            for (int i = 0; i < __instance.layersIdBased.Length; i++)
            {
                if (__instance.layersIdBased[i] != null)
                {
                    focusDistance = Mathf.Max(focusDistance, __instance.layersIdBased[i].orbitRadius);
                }
            }

            if (focusDistance * 1.2 > distance)
                focusDistance = (float)distance * 0.7f;

            float initRot = gameTick % 60;

            if (targetDirection.z != 0)
            {
                vertDirection = new VectorLF3(1, 1, (-targetDirection.x - targetDirection.y) / targetDirection.z);
            }
            VectorLF3 eff3EndPos = __instance.starData.uPosition + targetDirection.normalized * focusDistance * 1.05;


            int activeFrameNum = 0;
            int maxBarNum = 12;
            DysonSphereLayer layer = __instance.layersIdBased[1];
            if (layer == null) return;
            eff3EndPos = layer.starData.uPosition + (VectorLF3)Maths.QRotate(layer.currentRotation, normDirection * focusDistance * 0.95 * reverseDirection);
            for (int i = 0; i < layer.nodeCursor; i++)
            {
                if (activeFrameNum >= maxBarNum)
                    break;
                if (layer.nodePool[i] == null)
                    continue;
                for (int j = 0; j < laserIntensity; j++)
                {
                    int bulletIndex = __instance.swarm.AddBullet(new SailBullet
                    {
                        maxt = sideLaserBulletLifeTime,
                        lBegin = __instance.starData.uPosition,
                        uEndVel = targetUPos,
                        uBegin = layer.NodeUPos(layer.nodePool[i]),
                        uEnd = eff3EndPos
                    }, 0);
                    __instance.swarm.bulletPool[bulletIndex].state = 0;
                    if (j >= 0)
                    {
                        //noExplodeBullets.AddOrUpdate(bulletIndex, 1, (x, y) => 1);
                    }
                }
                activeFrameNum += 1;
                //AddNewLaser(layer.NodeUPos(layer.nodePool[i]), eff3EndPos, 0, laserIntensity);
            }
            for (; activeFrameNum < maxBarNum; activeFrameNum++)
            {
                //AddNewLaser(layer.starData.uPosition, layer.starData.uPosition, 0, 0);
            }
            //Compensate12LaserDamage();
        }

        /// <summary>
        /// 阻止子弹粒子的爆炸特效，提高帧率
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), "GameTick")]
        public static void PreventBulletExplodeEffect()
        {
            return;
            if ((int)state < 3 && (int)state > -2) return;
            DysonSwarm swarm = GameMain.data.dysonSpheres[starCannonStarIndex]?.swarm;
            if (swarm == null) return;
            if (swarm.bulletPool == null) return;
            foreach (var item in noExplodeBullets.Keys)
            {
                if (noExplodeBullets[item] > 0 && swarm.bulletPool.Length > item)
                {
                    if (swarm.bulletPool[item].id != 0 && swarm.bulletPool[item].t >= swarm.bulletPool[item].maxt)
                    {
                        swarm.RemoveBullet(item);
                        int rm;
                        noExplodeBullets.TryRemove(item, out rm);
                    }
                }
            }
        }



        public static void Import(BinaryReader r)
        {
            InitBeforeLoad();
            state = (EStarCannonState)r.ReadInt32();
            priorTargetHiveOriAstroId = r.ReadInt32();
            currentTargetStarIndex = r.ReadInt32();
            time = r.ReadInt32();
            endAimTime = r.ReadInt32();
            rotateSpeedScale = (float)r.ReadDouble();
            int c = r.ReadInt32();
            for (int i = 0; i < c; i++)
            {
                currentTargetIds.Add(r.ReadInt32());
            }
            InitAfterLoad();
        }

        public static void Export(BinaryWriter w)
        {
            w.Write((int)state);
            w.Write(priorTargetHiveOriAstroId);
            w.Write(currentTargetStarIndex);
            w.Write(time);
            w.Write(endAimTime);
            w.Write((double)rotateSpeedScale);
            int c = currentTargetIds.Count;
            w.Write(c);
            for (int i = 0; i < c; i++)
            {
                w.Write(currentTargetIds[i]);
            }
        }

        public static void IntoOtherSave()
        {
            InitBeforeLoad();
            InitAfterLoad();
        }
    }
}
