using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using System.Collections.Concurrent;
using UnityEngine.UI;
using System.IO;
using System.Security.Cryptography;
using System;
using static UnityEngine.EventSystems.EventTrigger;

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
        public static int focusBeamMaxBar = 36;      // 预热/集束汇聚激光最多发射的根数(受第1层已建 node 数限制; 可试 100)
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
            if (starCannonStarIndex >= 0 && starCannonStarIndex < MoreMegaStructure.MegaArrayLength && MoreMegaStructure.StarMegaStructureType[starCannonStarIndex] == 6 && starCannonStarIndex < GameMain.galaxy.starCount)
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
                return new int[] { -1, -1, -1, -1, -1, -1, -1};

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
                    ForceRealizeHivesInStar(currentTargetStarIndex); // 远程开火前强制实例化目标星系全部巢穴，必须在寻敌之前
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
                    ForceRealizeHivesInStar(currentTargetStarIndex); // 远程开火前强制实例化目标星系全部巢穴，必须在寻敌之前
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
                    ForceRealizeHivesInStar(currentTargetStarIndex); // 远程开火前强制实例化目标星系全部巢穴，必须在寻敌之前
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
                    ForceRealizeHivesInStar(currentTargetStarIndex); // 远程开火前强制实例化目标星系全部巢穴，必须在寻敌之前
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

            // 校验目标仍属于当前瞄准的星系, 防止 pool 槽位被其他星系的实体复用后误伤
            if (ptr0.originAstroId < 1000000)
                return false;
            int hiveIdx = ptr0.originAstroId - 1000000;
            if (spaceSector.dfHivesByAstro == null || hiveIdx >= spaceSector.dfHivesByAstro.Length)
                return false;
            EnemyDFHiveSystem hive = spaceSector.dfHivesByAstro[hiveIdx];
            if (hive == null || hive.starData == null || hive.starData.index != currentTargetStarIndex)
                return false;

            if (ptr0.dfRelayId > 0)
            {
                return CheckRelay(ref ptr0);
            }
        
            return true;
        }


        /// <summary>
        /// 强制实例化目标星系中所有尚未realized的黑雾巢穴，效果等同于玩家亲自进入过该星系（原版进入星系范围时就是全星系巢穴一起realize）。
        /// Realize()是同步的，调用当下就会通过InstantiateEnemies把巢穴建筑实体灌入enemyPool，因此必须在SearchNextTarget/CheckAndSearchAllTargets之前调用。
        /// 遍历整条nextSibling链而不是只realize被选中的巢穴，是为了维持原版"同星系全链同态"的隐含不变量，
        /// 避免UIStarmap.SetHivePreviewInStar的while(!realized)循环在混合链上提前终止，导致同星系其他巢穴的星图预览失灵。
        /// isEmpty的巢穴必须跳过：Realize()对空巢会把空槽位推进realized分支的AI逻辑，没有必要。
        /// </summary>
        /// <param name="starIndex">目标星系索引</param>
        public static void ForceRealizeHivesInStar(int starIndex)
        {
            if (spaceSector == null || spaceSector.dfHives == null)
                return;
            if (starIndex < 0 || starIndex >= GameMain.galaxy.starCount)
                return;
            EnemyDFHiveSystem hive = spaceSector.dfHives[starIndex];
            while (hive != null)
            {
                if (!hive.realized && !hive.isEmpty)
                    hive.Realize();
                hive = hive.nextSibling;
            }
        }

        /// <summary>
        /// 为避免击杀一个未实例化的行星上的中继站，导致行星Init一个Factory同时，错误地保持了planet.modData为null，从而使得存档时报错，因此阻止恒星炮锁定或攻击所在行星的factory为null的中继站
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public static bool CheckRelay(ref EnemyData ptr)
        {
            EnemyDFHiveSystem hive = GameMain.spaceSector.GetHiveByAstroId(ptr.originAstroId);
            DFRelayComponent relayComponent = hive.relays.buffer[ptr.dfRelayId];
            if (relayComponent != null && relayComponent.id == ptr.dfRelayId)
            {
                PlanetData planet = GameMain.spaceSector.galaxy.PlanetById(relayComponent.targetAstroId);
                if (planet != null && (planet.factory != null || planet.modData != null))
                    return true;
            }
            return false;
        }


        public static bool CheckAndSearchAllTargets()
        {
            return CheckAndSearchAllTargets(-1);
        }

        public static bool CheckAndSearchAllTargets(long gameTick = -1)
        {
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
            // 方案1: 由 GameTick 驱动且已处于预热/开火阶段时, 目标打不满/打光就尝试激活未激活敌舰(幂等, 无未激活舰则 no-op)
            if (gameTick >= 0 && (int)state >= 2)
            {
                TryActivateDormantShips(gameTick);
            }
            return currentTargetIds.Count > 0;
        }

        /// <summary>
        /// 方案1(激活未激活敌舰): 由 GameTick 驱动且已处于预热/开火阶段时, 若本星系"打不满/打光"合法目标,
        /// 用官方 EnemyDFHiveSystem.ActivateUnit 把巢穴"待命编队"里 units[port]==1 的悬停舰(星图中白色, 此时不在 enemyPool)实例化为真实敌舰, 并直接加入 currentTargetIds,
        /// 复用现有校验/伤害/渲染管线。全程只读 forms 槽位并调用官方激活接口, 不触碰 pbuilders/数组结构 => 存档安全; ActivateUnit 内部以 units[port]==1 为前置 => 不会重复激活同一槽。
        /// 触发: 目标数==0(即将停火, 无需tick门槛) 或 (目标数<上限 且 gameTick%30==19)。单次上限 n = maxAimCount*2, 且不超过剩余未激活舰数。
        /// 控制流(与需求一致): 先判情景B(有优先巢穴时优先激活"优先巢穴"的舰), 一旦成功激活(>=1)即置标志跳过情景A;
        /// B 未成功(条件不满足或优先巢穴无未激活舰)则继续走情景A(激活全星系的舰)。情景A 不以"没有优先巢穴"为前提。
        /// 决策"直接加入列表"而非"等下帧重寻": 现有 SearchNextTarget 仅在降级到 -2 时才搜索舰船, 有优先巢穴时新激活的舰不会被自动锁定,
        /// 故必须主动加入; 加入后下一帧仍会被 CheckTarget 校验并按 maxAimCount 裁剪, 安全。
        /// </summary>
        public static void TryActivateDormantShips(long gameTick)
        {
            int legalCount = currentTargetIds.Count;                 // 校验+补寻之后的合法目标数(含正在攻击的)
            bool noTargets = legalCount == 0;                        // 完全没目标, 即将停止开火
            bool lowTargets = legalCount < maxAimCount;              // 目标未满, 有激光空转/即将转火
            bool tickGate = gameTick % 30 == 19;                     // 低频批量激活时机
            bool trigger = noTargets || (lowTargets && tickGate);     // 情景A/B 共用的触发条件
            if (!trigger)
                return;
            if (maxAimCount <= 0 || spaceSector == null || spaceSector.dfHives == null)
                return;
            if (currentTargetStarIndex < 0 || currentTargetStarIndex >= GameMain.galaxy.starCount)
                return;

            int n = maxAimCount * 2;                                 // 本次尝试激活数量上限

            // 情景B: 有优先巢穴时, 先尝试只激活"优先巢穴"的未激活舰; 成功激活>=1 即置标志返回, 跳过情景A
            if (priorTargetHiveOriAstroId >= 0)
            {
                EnemyDFHiveSystem priorityHive = spaceSector.GetHiveByAstroId(priorTargetHiveOriAstroId);
                if (priorityHive != null && ActivateHiveShips(priorityHive, n, gameTick) > 0)
                    return;                                          // 标志: 情景B成功激活 => 跳过情景A
            }

            // 情景A(或情景B未成功时): 激活目标星系各巢穴的未激活舰, 直到达到 n 或全部激活; 情景A不限制有没有优先巢穴
            EnemyDFHiveSystem hive = spaceSector.dfHives[currentTargetStarIndex];
            int activated = 0;
            while (hive != null && activated < n)
            {
                activated += ActivateHiveShips(hive, n - activated, gameTick);
                hive = hive.nextSibling;
            }
        }

        /// <summary>
        /// 激活指定巢穴至多 quota 个"待命(未激活)敌舰": 遍历巢穴三个编队 hive.forms[formId](formId 0/1/2 对应 protoId 8113/8112/8111),
        /// 找 units[port]==1 的悬停槽(星图中白色、此刻尚未进入 enemyPool), 调官方 EnemyDFHiveSystem.ActivateUnit(formId, port, gameTick)
        /// 将其实例化为真实 localized 敌舰, 再把对应的 enemyId 去重加入 currentTargetIds。
        /// 只读 forms 槽位并调用官方激活接口, 不碰 pbuilders/数组结构 => 存档安全; ActivateUnit 以 units[port]==1 为前置, 已激活/空槽返回0 => 幂等。
        /// 返回实际激活数量。
        /// </summary>
        public static int ActivateHiveShips(EnemyDFHiveSystem hive, int quota, long gameTick)
        {
            if (hive == null || hive.forms == null || hive.units == null || hive.units.buffer == null || quota <= 0 || spaceSector == null || currentTargetIds == null)
                return 0;
            int done = 0;
            for (int formId = 0; formId < hive.forms.Length && done < quota; formId++)
            {
                EnemyFormation form = hive.forms[formId];
                if (form == null || form.units == null)
                    continue;
                for (int port = 1; port < form.units.Length && done < quota; port++)
                {
                    if (form.units[port] != 1)                     // 只激活"待命"槽: 0=空位, 2=激活中, 3=出击中; 1=待命
                        continue;
                    int unitId = hive.ActivateUnit(formId, port, gameTick);   // 官方实例化: 生成真实 localized 敌舰
                    if (unitId > 0 && unitId < hive.units.buffer.Length)
                    {
                        int enemyId = hive.units.buffer[unitId].enemyId;   // EnemyUnitComponent 是结构体, 直接取 enemyId(与官方 DeactivateUnit 一致)
                        if (enemyId > 0 && !currentTargetIds.Contains(enemyId))
                            currentTargetIds.Add(enemyId);
                        done++;
                    }
                }
            }
            return done;
        }

      

        /// <summary>
        /// 根据当前瞄准的星系和是否有优先瞄准的黑雾巢穴的设定，返回下一个合法目标
        /// </summary>
        /// <returns></returns>
        public static SkillTarget SearchNextTarget()
        {
            SkillTarget skillTarget = default(SkillTarget);
            if (currentTargetStarIndex < 0 || currentTargetStarIndex >= GameMain.galaxy.starCount)
                return skillTarget;
            skillTarget.type = ETargetType.Enemy;
            EnemyData[] enemyPool = spaceSector.enemyPool;
            if(enemyPool == null) //////////////////////// 怎么回事，这里不判断null会在后面读取到Pool的时候报错
            {
                Console.WriteLine("null pool");
                return skillTarget;
            }
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
                    { continue; }
                    EnemyDFHiveSystem enemyDFHiveSystem = dfHivesByAstro[ptr3.originAstroId - 1000000];
                    if (enemyDFHiveSystem == null || enemyDFHiveSystem.starData.index != currentTargetStarIndex) // 不是目标星系的黑雾巢穴
                    { continue; }
                    if (priorTargetHiveOriAstroId >= 0 && ptr3.originAstroId != priorTargetHiveOriAstroId)// 玩家要求优先攻击特定的黑雾巢穴，因此不属于特定黑雾巢穴的敌人被跳过
                    { continue; }
                    if (ptr3.dfSCoreId + ptr3.dfSNodeId + ptr3.dfSConnectorId + ptr3.dfSGammaId + ptr3.dfSTurretId + ptr3.dfSReplicatorId + ptr3.dfRelayId <= 0 && priorTargetHiveOriAstroId > -2) // 不是黑雾巢穴的建筑本体，跳过。恒星炮不会攻击敌舰，除非已经找不到hive作为目标，priorTargetHiveOriAstroId变为-2
                    { continue; }
                    if( ptr3.dfRelayId > 0)
                    {
                        if (!CheckRelay(ref ptr3))
                            continue;
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
            if (__instance == null || __instance.layersIdBased == null)
                return;

            int starIndex = __instance.starData.index;
            if (starIndex >= MoreMegaStructure.MegaArrayLength)
                return;

            if (MoreMegaStructure.StarMegaStructureType[starIndex] != 6 )
                return;

            if (__instance.energyGenCurrentTick < 10000)
                return;

            MMSCPU.BeginSample(ECpuWorkEntryExtended.MoreMegaStructure);
            MMSCPU.BeginSample(ECpuWorkEntryExtended.StarCannon);
            RefreshStarCannonProperties();
            starCannonStarIndex = starIndex;

            //bool targetChanged = false;
            if (state == EStarCannonState.Standby)
                return;

            if(state != EStarCannonState.Recharge)
            {
                MMSCPU.BeginSample(ECpuWorkEntryExtended.EnemySearch);
                if (state == EStarCannonState.Align || state == EStarCannonState.Heat)
                {
                    if (!CheckAndSearchAllTargets(gameTick))
                    {
                        UIRealtimeTip.Popup("目标无法定位警告".Translate());
                        StopFiring();
                    }                    
                }
                else if(state == EStarCannonState.Fire || state == EStarCannonState.Aim || state == EStarCannonState.Switch)
                {
                    if (!CheckAndSearchAllTargets(gameTick))
                        StopFiring();                    
                }
                MMSCPU.EndSample(ECpuWorkEntryExtended.EnemySearch);
                // 根据目标恒星位置计算锁定方向信息
                MMSCPU.BeginSample(ECpuWorkEntryExtended.AttackLogic);
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
                            endAimTime = Mathf.Max(endAimTime, time + (int)aimTimeNeed); //保存瞄准完成所需的最大时间
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
                    // 记录恒星身份 index, 交给独立渲染器在现实视角按"当前星系(localStar)"直接比对来叠加同源 bullet(遮挡正确); 全程空判断防报错
                    int fireStarIdx = (__instance.starData != null) ? __instance.starData.index : -1;
                    int targetStarIdx = (currentTargetStarIndex >= 0 && currentTargetStarIndex < GameMain.galaxy.starCount
                        && GameMain.galaxy.stars[currentTargetStarIndex] != null)
                        ? GameMain.galaxy.stars[currentTargetStarIndex].index : -1;
                    // 主光束近端渲染长度 L = 2×该端恒星"最大戴森壳半径"。源码最大壳半径 = dysonRadius*40000*2 (UIStatisticsDysonDetailPanel/DysonSphereStatPlan), 故 L = dysonRadius*160000。各星系各算各的, 交由渲染器在绘制前现场裁剪
                    float fireShellLen = (__instance.starData != null) ? __instance.starData.dysonRadius * 160000f : 0f;
                    float targetShellLen = (currentTargetStarIndex >= 0 && currentTargetStarIndex < GameMain.galaxy.starCount
                        && GameMain.galaxy.stars[currentTargetStarIndex] != null)
                        ? GameMain.galaxy.stars[currentTargetStarIndex].dysonRadius * 160000f : 0f;
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
                        //AddNewLaser(centerStarUPos, enemyUPos, target.id, 1, 30);
                        spaceSector.skillSystem.DamageObject((int)(damagePerTick * ratio), damageSlice, ref target, ref caster);
                        if (maxAimCount - count > 0 && i == count - 1) // 如果目标过少，少于可以同时射击的最大数量，溢出的可射击的激光将同时射击最后一个合法目标
                        {
                            for (int j = 0; j < maxAimCount - count; j++)
                            {
                                //AddNewLaser(centerStarUPos, enemyUPos, target.id, 1, 10);
                                spaceSector.skillSystem.DamageObject((int)(damagePerTick * ratio), damageSlice, ref target, ref caster);
                            }
                        }
                        // 恒星 -> 单个敌人 的攻击激光：交给独立渲染器，三种视图都能看到，且不经过 DysonSwarm（隐藏戴森云也可见）
                        StarCannonLaserRenderer.AddBeam(1000 + i, centerStarUPos, enemyUPos,
                            StarCannonLaserRenderer.DefaultColor,
                            StarCannonLaserRenderer.baseBulletCount,
                            StarCannonLaserRenderer.defaultMuzzleRadius * 0.5f,
                            StarCannonLaserRenderer.defaultImpactRadius * 0.5f,
                            beginStarIdx: targetStarIdx,
                            endStarIdx: targetStarIdx);
                    }

                    // 恒星 -> 恒星 主光束：交给独立渲染器（自带三视图坐标换算 + 自持 DrawProceduralNow，
                    // 不再往任何 DysonSwarm 灌太阳帆，因此隐藏戴森云渲染、或正在查看别的星系时都能显示，且大幅省性能）
                    StarCannonLaserRenderer.EnsureHost();
                    int beamLevel = Mathf.Max(1, renderLevel);
                    StarCannonLaserRenderer.AddBeam(1, __instance.starData.uPosition, targetUPos,
                        StarCannonLaserRenderer.DefaultColor,
                        StarCannonLaserRenderer.baseBulletCount * beamLevel,
                        StarCannonLaserRenderer.defaultMuzzleRadius * beamLevel,
                        StarCannonLaserRenderer.defaultImpactRadius * beamLevel,
                        0.35f, 1f, 3 + beamLevel,
                        beginStarIdx: fireStarIdx,
                        endStarIdx: targetStarIdx,
                        beginShellLen: fireShellLen,
                        endShellLen: targetShellLen);
                }
                MMSCPU.EndSample(ECpuWorkEntryExtended.AttackLogic);
            }

            // 状态计时结算与转换
            time += 1;
            if (time < -cooldownTimeNeed - chargingTimeNeed)
                time = -cooldownTimeNeed - chargingTimeNeed;
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
            int maxBarNum = focusBeamMaxBar;
            // 聚能阶段亮度：预热时 laserIntensity 从 0 涨到 maxSideLaserIntensity，归一化后驱动光束逐渐变亮
            // 从 0.15 起步(而非 0)：保证预热一开始就有“较暗但可见”的汇聚激光，随后每秒变亮直到开火
            float focusIntensity = maxSideLaserIntensity > 0
                ? Mathf.Clamp01(0.15f + 0.85f * (float)laserIntensity / (float)maxSideLaserIntensity)
                : 1f;
            DysonSphereLayer layer = __instance.layersIdBased[1];
            if (layer == null) return;
            eff3EndPos = layer.starData.uPosition + (VectorLF3)Maths.QRotate(layer.currentRotation, normDirection * focusDistance * 0.95 * reverseDirection);
            for (int i = 0; i < layer.nodeCursor; i++)
            {
                if (activeFrameNum >= maxBarNum)
                    break;
                if (layer.nodePool[i] == null)
                    continue;
                // 集束侧激光：从壳层 node 汇聚到炮口前方，交给独立渲染器（不再向 swarm 灌子弹）
                // beginStarIdx/endStarIdx = 发射恒星 index（汇聚束两段都在发射星壳层附近），供现实视角叠加层按当前星系身份门控
                StarCannonLaserRenderer.AddBeam(2000 + i, layer.NodeUPos(layer.nodePool[i]), eff3EndPos,
                    StarCannonLaserRenderer.DefaultColor,
                    2 + laserIntensity,
                    24f, 12f, 0.2f, focusIntensity,
                    beginStarIdx: (__instance.starData != null ? __instance.starData.index : -1),
                    endStarIdx: (__instance.starData != null ? __instance.starData.index : -1));
                activeFrameNum += 1;
            }
            for (; activeFrameNum < maxBarNum; activeFrameNum++)
            {
                //AddNewLaser(layer.starData.uPosition, layer.starData.uPosition, 0, 0);
            }
            // 预热阶段主光束还未出现，此处必须主动创建渲染宿主，否则汇聚光束只会写进字典而没有任何 OnRenderObject 来绘制，
            // 表现为“汇聚光束和主光束同时才出现”。EnsureHost 幂等，beams 为空时不会创建。
            StarCannonLaserRenderer.EnsureHost();
            //Compensate12LaserDamage();
        }

        /// <summary>
        /// 阻止子弹粒子的爆炸特效，提高帧率
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ThreadManager), "ProcessFrame")]
        public static void PreventBulletExplodeEffect()
        {
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


        ///// <summary>
        ///// 仅供测试 3901 3902
        ///// </summary>

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(PlanetFactory), "Init")]
        //public static bool PlanetFactoryInitPrefix(ref PlanetFactory __instance, GameData _gameData, PlanetData _planet, int _index)
        //{
        //    Utils.Log($"Init planet factory {__instance.planet == null} and {__instance.planet?.factory}, _planet{_planet.id}");

        //    Utils.Log($"Before data is {_planet?.modData?.Length}, is null?{_planet.modData == null}");
        //    return true;
        //}
        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(PlanetFactory), "Init")]
        //public static void PlanetFactoryInitPostfix(ref PlanetFactory __instance, GameData _gameData, PlanetData _planet, int _index)
        //{
        //    Utils.Log($"After data is {__instance.planet == null},{_planet?.modData?.Length}, and {__instance.planet?.modData?.Length}, is null ? {__instance.planet.modData == null}");
        //}
        ////[HarmonyPrefix]
        ////[HarmonyPatch(typeof(EnemyDFHiveSystem), "NotifyRelayKilled")]
        ////public static bool NotifyRelayKilledPrefix(ref EnemyDFHiveSystem __instance, ref EnemyData enemy)
        ////{
        ////    DFRelayComponent dfrelayComponent = __instance.relays.buffer[enemy.dfRelayId];
        ////    if (dfrelayComponent != null && dfrelayComponent.id == enemy.dfRelayId)
        ////    {
        ////        __instance.relayNeutralizedCounter++;
        ////        if (dfrelayComponent.baseState == 1 && dfrelayComponent.stage == 2)
        ////        {
        ////            PlanetData planetData = __instance.sector.galaxy.PlanetById(dfrelayComponent.targetAstroId);
        ////            Utils.Log($"planet data is null? {planetData == null}");
        ////            if(planetData != null )
        ////            {
        ////                Utils.Log($"factory is null? {planetData.factory == null}");
        ////                if (planetData.factory != null )
        ////                {
        ////                    Utils.Log($"its planet id is {planetData.factory.planet?.id}, while true id is {planetData.id}");
        ////                }
        ////            }
        ////            if (planetData != null)
        ////            {
        ////                PlanetFactory orCreateFactory = __instance.gameData.GetOrCreateFactory(planetData);
        ////                __instance.sector.galaxy.astrosFactory[dfrelayComponent.targetAstroId] = orCreateFactory;
        ////                if (orCreateFactory != null)
        ////                {
        ////                    dfrelayComponent.RealizePlanetBase(__instance.sector);
        ////                }
        ////            }
        ////        }
        ////    }
        ////    return false;
        ////}


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
