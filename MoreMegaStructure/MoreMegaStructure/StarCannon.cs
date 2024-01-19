using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using System.Collections.Concurrent;
using UnityEngine.UI;
using DSP_Battle;

namespace MoreMegaStructure
{
    public enum EStarCannonState : int
    {
        Standby = 0,
        Aim = 1,
        Heat = 2,
        Fire = 3,
        Switch = 4,
        Retarget = 5,
        Cooldown = -2,
        Recharge = -1
    }
    public class StarCannon
    {
		public static List<long> energyPerTickRequiredByLevel = new List<long> { 0, 100000000, 500000000, 2000000000, 4000000000, 9000000000, 9000000000 };
		public static List<int> basicDamagePerTickByLevel = new List<int> { 0, 500, 1000, 1500, 2000, 700 }; //五级前，伤害是固定的，五级后，伤害是基础伤害+bonus
		public static float bonusDpsPerMW = 0.2f; //5级后，每1MW的能量提供这么多的秒伤。每tick提供的tick伤害也是这个比值
		public static List<int> maxAimCountByLevel = new List<int> { 0, 3, 5, 8, 15, 9999 }; //连续瞄准次数上限
		public static List<int> chargeTickByLevel = new List<int> { 0, 270000, 162000, 72000, 36000, 18000 }; //充能时间，tick
		public static List<int> fireRangeByLevel = new List<int> { 0, 9999, 9999, 9999, 9999, 9999 }; //射程，以光年计
		public static List<int> damageReducePerLy = new List<int> { 3, 3, 2, 1, 0, 0 }; //每光年伤害衰减的百分比
        public static ConcurrentDictionary<int, int> noExplodeBullets = new ConcurrentDictionary<int, int>();

        // 参数
        public static int laserBulletNum = 30;
        public static int laserBulletPosDelta = 200; // 主激光发射处随机偏移（激光半径）
        public static int laserBulletEndPosDelta = 0; // 主激光命中处随机偏移（激光命中处半径）
        public static int maxSideLaserIntensity = 5; //12束集束激光子弹数
        public static float sideLaserBulletLifeTime = 0.035f;
        public static VectorLF3 normDirection = new VectorLF3(0, 1, 0);
        public static int reverseDirection = -1; //只能是1或者-1，1是北极为炮口，-1则是南极。相当于设计恒星炮时所有层级南北极互换
        public static int renderLevel = 2; // 默认是2，更小可以减少恒星炮渲染的光束数量

        public static GameObject fireButtonObj = null;
        public static Button fireButton = null;
        public static Text fireButtonText = null;
        public static Image fireButtonImage = null;

        static Color cannonDisableColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        static Color cannonChargingColor = new Color(0.42f, 0.2f, 0.2f, 1f);
        static Color cannonReadyColor = new Color(0f, 0.499f, 0.824f, 1f);
        static Color cannonAimingColor = new Color(0.973f, 0.359f, 0.170f, 1f);
        static Color cannonFiringColor = new Color(1f, 0.16f, 0.16f, 1f);

        // 需要存档的参数
        public static EStarCannonState state = EStarCannonState.Standby; //恒星炮开火阶段。1=瞄准；2=预热旋转且瞄准锁定；3=开火；4=刚消灭一个目标、准备继续连续瞄准（此阶段只有一帧）；5=连续开火的正在瞄准新虫洞；-2=将各层角度还原到随机的其他角度并减慢旋转速度，冷却中；-1=重新充能中；0=充能完毕、待命、可开火。
        public static int currentTargetId = 0;
        public static int currentTargetStarIndex = 0;
        public static int retargetTimeLeft = 0;
        public static int time = 0; //恒星炮工作流程计时，均以tick记。负值代表冷却/充能过程
        public static int endAimTime = 999; //最慢的轨道所需的瞄准时间，也就是阶段1的总时间
        public static VectorLF3 targetUPos = new VectorLF3(30000, 40000, -50000);
        public static float rotateSpeedScale = 1;

        //下面属性可能根据戴森球等级有变化，但并不需要存档        
        public static int starCannonLevel = 1; //恒星炮建造的所属阶段（等级），即完成度
        public static int damagePerTick = 4000; //每tick伤害
        public static int damageReduction = 0;
        public static double maxRange = 10.0; //恒星炮最大开火距离，以光年计，1ly = 60AU = 60 * 40000m。
        public static int warmTimeNeed = 240; //阶段2预热加速旋转需要的tick时间
        public static int cooldownTimeNeed = 600; //阶段5冷却需要的tick时间
        public static int chargingTimeNeed = 75 * 3600; //阶段-1的重新充能需要的tick时间
        public static float reAimAngularSpeed = 30f; //连续瞄准时，所有层以同一个速度旋转瞄准到下一个虫洞
        public static int maxAimCount = 100; //连续瞄准次数上限
        public static List<double> layerRotateSpeed; //不需要存档，每次随机生成即可

        //每帧更新不需要存档
        public static DysonSwarm targetSwarm = null; //除了要在恒星炮的星系上发射“太阳帆束”来体现动画效果，还要在受攻击恒星上发射，使在观看目标点时也能够渲染，所以需要受击目标所在恒星系的index
        public static int starCannonStarIndex = -1; //恒星炮所在恒星的index，每帧更新

        public static void InitWhenLoad()
        {
            noExplodeBullets = new ConcurrentDictionary<int, int>();
            layerRotateSpeed = new List<double>();
            for (int i = 0; i < 22; i++)
            {
                double speed = DspBattlePlugin.randSeed.NextDouble() - 0.5;
                if (speed < 0.2 && speed > -0.2)
                    speed *= 2;
                layerRotateSpeed.Add(speed);
            }
            targetUPos = new VectorLF3(0, 0, 0);
            targetSwarm = null;
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

            maxAimCountByLevel = new List<int> { 0, 6, 12, 18, 30, 99999 };
        }


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
				damagePerTick += (int)(cannonEnergy * 1.0 / 1000000.0 * bonusDpsPerMW);
            }
			return new int[] { level, damagePerTick, maxAimCountByLevel[level], chargeTickByLevel[level], fireRangeByLevel[level], damageReducePerLy[level] };
        }

        public static void OnFireButtonClick()
        { 
        
        }

		public static void TestShootLaser(bool showInfo = false)
		{
			SkillTarget target = SearchTarget(false);
            if(target.id == 0) { return; }
			var _this = GameMain.data.mainPlayer.controller.actionCombat;
            ref EnemyData ptr0 = ref _this.spaceSector.enemyPool[target.id];
			//GameMain.mainPlayer.controller.actionCombat.Shoot_Laser_Space(ref ptr0, 1000);
			if (true)
			{
                ref SpaceLaserOneShot ptr = ref _this.skillSystem.spaceLaserOneShots.Add();
                ptr.astroId = ((_this.localStar != null) ? _this.localStar.astroId : 0);
                ptr.hitIndex = 1;
                ptr.target.type = ETargetType.Enemy;
                ptr.target.id = ptr0.id;
                ptr.target.astroId = ptr0.originAstroId;
                ptr.caster.type = ETargetType.TurretPlasmas;
                ptr.caster.id = 1;
                ptr.beginPosU = _this.spaceSector.dfHivesByAstro[ptr0.originAstroId - 1000000].starData.uPosition;

                StarData starData = _this.spaceSector.dfHivesByAstro[ptr0.originAstroId - 1000000].starData;
                Vector3 vec;
                //_this.skillSystem.GetObjectUPosition(ref ptr.target, out ptr.endPosU);
                _this.spaceSector.skillSystem.GetObjectUPositionAndVelocity(ref ptr.target, out ptr.endPosU, out vec);
                //ptr.endPosU += (VectorLF3)(0.016666666666666666f * vec);
                ptr.endPosU += (VectorLF3)(0.3f * vec);
                //ptr.muzzleOffset = Maths.QInvRotateLF(_this.player.uRotation, _this.mecha.skillCastRightU - _this.mecha.skillTargetUCenter);
                ptr.endVelU = Vector3.zero;
                ptr.muzzleOffset = Vector3.zero;
                ptr.damage = 1000;
                ptr.life = 0;
                ptr.extendedDistWhenMiss = 0;
                ptr.mask = ETargetTypeMask.Enemy;
                //_this.skillSystem.audio.AddPlayerAudio(312, 1.2f, _this.player.position);

                // 为了让星图模式也显示激光，用深空来敌老套路创建太阳帆发射轨迹
                int starIndex = starData.index;
                DysonSwarm swarm = GameMain.data.dysonSpheres[starIndex]?.swarm;
                if (swarm == null)
                {
                    return;
                }
                int lessBulletRatio = 1;
                if (swarm != null)
                {
                    VectorLF3 normDirection = (ptr.endPosU - starData.uPosition) / 12;
                    for (int i = 0; i < laserBulletNum / lessBulletRatio || i == 0; i++)
                    {
                        int bulletIndex = swarm.AddBullet(new SailBullet
                        {
                            maxt = 0.2f,
                            lBegin = starData.uPosition,
                            uEndVel = ptr.endPosU,
                            uBegin = starData.uPosition + Utils.RandVerticalPosDelta(normDirection) * laserBulletPosDelta / lessBulletRatio,
                            uEnd = ptr.endPosU + Utils.RandVerticalPosDelta(normDirection) * laserBulletEndPosDelta / lessBulletRatio
                        }, 0);
                        swarm.bulletPool[bulletIndex].state = 0;
                        if (i >= 1)
                        {
                            noExplodeBullets.AddOrUpdate(bulletIndex, 1, (x, y) => 1);
                        }
                    }
                }
            }
            if (ptr0.originAstroId > 0)
            {
                EnemyDFHiveSystem hiveByAstroId = _this.spaceSector.GetHiveByAstroId(ptr0.originAstroId);
                if (hiveByAstroId != null && hiveByAstroId.hiveAstroId == ptr0.originAstroId)
                {
                    _this.skillSystem.AddSpaceEnemyHatred(hiveByAstroId, ref ptr0, ETargetType.Player, _this.localAstroId, 1);
                }
            }
			
			StarData star = GameMain.data.localStar;
			Player player = GameMain.data.mainPlayer;
			if(star == null) { return; }
       //     for (int i = 0; i < star.planets.Length; i++)
       //     {
       //         PlanetData planetData = star.planets[i];
       //         PlanetFactory planetFactory = (planetData != null) ? planetData.factory : null;
       //         if (planetFactory != null)
       //         {
       //             for (int j = 1; j < planetFactory.entityCursor; j++)
       //             {
       //                 ref EntityData ptr = ref planetFactory.entityPool[j];
       //                 if (ptr.id == j)
       //                 {
       //                     ref LocalLaserOneShot ptr2 = ref GameMain.data.spaceSector.skillSystem.localLaserOneShots.Add();
       //                     ptr2.astroId = planetFactory.planetId;
       //                     ptr2.hitIndex = 1;
       //                     ptr2.beginPos = player.position + player.position.normalized * 1.5f;
       //                     ptr2.endPos = ptr.pos + ptr.pos.normalized * SkillSystem.RoughHeightByModelIndex[(int)ptr.modelIndex] * 0.5f;
       //                     ptr2.target.type = ETargetType.None;
       //                     ptr2.target.id = ptr.id;
       //                     ptr2.damage = 1000;
       //                     ptr2.life = 10;
       //                     ptr2.mask = ETargetTypeMask.NotPlayer;
							//break;
       //                 }
       //             }
       //         }
       //     }
        }

        public static SkillTarget SearchTargetNoLimit()
        {
            SkillTarget skillTarget = default(SkillTarget);
            skillTarget.type = ETargetType.Enemy;
            SpaceSector spaceSector = GameMain.data.spaceSector;
            EnemyData[] enemyPool = spaceSector.enemyPool;
            int enemyCursor = spaceSector.enemyCursor;
            for (int i = 0; i < enemyCursor; i++)
            {
                ref EnemyData ptr = ref enemyPool[i];
                if (ptr.id != 0 && ptr.dfRelayId != 0)
                {
                    skillTarget.id = ptr.id;
                    skillTarget.astroId = ptr.originAstroId;
                    return skillTarget;
                }
            }
            return skillTarget;
        }

		public static SkillTarget SearchTarget(bool showSearchInfo = false)
		{
			SkillTarget skillTarget = default(SkillTarget);
			skillTarget.type = ETargetType.Enemy;
			float num2 = 100000; // ammo range
			float num3 = 1E+12f;
			var _this = GameMain.mainPlayer.controller.actionCombat;
			if (_this != null)
			{
			}
			EnemyData[] enemyPool2 = _this.spaceSector.enemyPool;
			int enemyCursor = _this.spaceSector.enemyCursor;
			EnemyDFHiveSystem[] dfHivesByAstro = _this.spaceSector.dfHivesByAstro;
			AstroData[] galaxyAstros = _this.spaceSector.galaxyAstros;
			Vector3 vector2 = _this.mecha.skillTargetUCenter;
			float num26 = num2;
			float num27 = num26 * num26;
			VectorLF3 zero = VectorLF3.zero;
			for (int m = 0; m < enemyCursor; m++)
			{
                ref EnemyData ptr3 = ref enemyPool2[m]; 
				if (ptr3.id != 0 && ptr3.dfRelayId == 0)
                {
					if (ptr3.dfTinderId != 0)
						continue;
					EnemyDFHiveSystem enemyDFHiveSystem = dfHivesByAstro[ptr3.originAstroId - 1000000];
					if (enemyDFHiveSystem != null && enemyDFHiveSystem.starData.index != 0 && false) // 修改用于判断星系
						continue;
					if (showSearchInfo)
					{
						//Debug.Log($"dfsNode yes: {ptr3.dfSNodeId} {ptr3.dfSCoreId} {ptr3.dfSConnectorId} {ptr3.dfSGammaId} {ptr3.dfSTurretId} {ptr3.dfSReplicatorId} in astro{ptr3.astroId} and oriAstro {ptr3.originAstroId} and relayId {ptr3.dfRelayId}");
                        //Debug.Log($"spacesector is the same? {GameMain.data.spaceSector == _this.spaceSector}");
					}
                    _this.spaceSector.TransformFromAstro_ref(ptr3.astroId, out zero, ref ptr3.pos);
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
                                }
                            }
                        }
                    }
                    if (!flag && num34 < num3) // 攻击最近的
                    {
                        num3 = num34;
                        skillTarget.id = ptr3.id;
                        skillTarget.astroId = ptr3.originAstroId;
                    }

                }
            }
			return skillTarget;
        }




        /// <summary>
        /// 阻止子弹粒子的爆炸特效，提高帧率
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), "GameTick")]
        public static void PreventBulletExplodeEffect()
        {
            //if (starCannonStarIndex < 0) return;
            DysonSwarm swarm = GameMain.data.dysonSpheres[GameMain.data.localStar.index]?.swarm;
            if (swarm == null) return;
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
    }
}
