using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMegaStructure
{
    /// <summary>
    /// 恒星炮激光的独立渲染器。
    ///
    /// 主路径（自定义 shader，需把 StarCannonLaser.shader 打进 mmstabicon 包）：
    /// - 每条光束 = 一根“相机朝向 ribbon”quad，begin->end 全程等宽、不移动、精确连到终点，
    ///   宽度 = max(世界半宽, 屏幕像素下限 * 距离 / _ProjFactor) => 近看粗、远看也不消失。
    /// - 每实例带 intensity，用于“聚能阶段逐渐变亮”与停火淡出。
    /// - 完全脱离 DysonSwarm：自己维护光束列表 + 自己的 ComputeBuffer，在主视图/星图/戴森球编辑器
    ///   三个相机各按 renderPlace 做视图空间换算后 DrawProceduralNow。关闭戴森云渲染仍然可见。
    ///
    /// 回退路径（bundle 里还没有 laser 材质时自动启用）：
    /// - 复用游戏现成 sailBulletMaterial 的“太阳帆子弹”画法（旧逻辑）。它是运动粒子，
    ///   会有抖动/尾端渐细的缺点，但保证在未重打 bundle 前激光不消失。
    /// </summary>
    public static class StarCannonLaserRenderer
    {
        // 一条光束的“外形配方”。光束本身是无状态的，每帧由配方生成绘制数据。
        public struct BeamProfile
        {
            public VectorLF3 begin;      // 世界(u)坐标 起点
            public VectorLF3 end;        // 世界(u)坐标 终点
            public float muzzleRadius;   // 起点端半径(世界单位)——shader 路径下作为整根光束的统一半宽
            public float impactRadius;   // 终点端半径(世界单位)——仅回退(子弹)路径使用
            public int bulletCount;      // 回退路径的叠加子弹数
            public int seed;             // 回退路径的稳定伪随机种子
            public Color color;          // 颜色
            public float flowLife;       // 回退路径流动周期(秒)
            public float intensity;      // 亮度 0..1：聚能阶段渐亮 / 停火淡出（主路径使用）
            public int strandCount;      // 起点环绕的螺旋能量丝条数(0=无)，仅主视图生效
            public float strandTurns;    // 螺旋在丝长内的缠绕圈数
            public float strandSpeed;    // 螺旋旋转/流动速度
            public float expireAt;       // 光束存活截止(Time.unscaledTime)
            public int beginStarIdx;     // begin 端点所属恒星的 StarData.index(-1=未知)。用于现实视角叠加层按"当前星系"直接比对, 不用距离阈值
            public int endStarIdx;       // end 端点所属恒星的 StarData.index(-1=未知)
            public float beginShellLen;  // 主光束: begin 端恒星的"近端渲染长度"L(=2×最大戴森壳半径=dysonRadius*40000*4); 玩家在 begin 系时只渲染 begin 起算 L 的这段(<=0=不裁剪)
            public float endShellLen;    // 主光束: end 端恒星的近端渲染长度 L2; 玩家在 end 系时只渲染 end 回算 L2 的这段
        }

        // 默认外观参数
        public static Color DefaultColor = new Color(0.25f, 0.75f, 1.0f, 1.0f); // 蓝色
        public static int baseBulletCount = 36;     // 回退路径：主光束默认叠加子弹数
        public static float defaultMuzzleRadius = 420f;
        public static float defaultImpactRadius = 90f;

        // 主路径(shader)可调外观
        public static float defaultMinPixelsCore = 6f;    // 核心光束的屏幕最小半宽(像素)——“远了也粗也可见”的关键，调大即整体变粗
        public static float haloWidthScale = 2.8f;        // 外辉半宽 = 核心半宽 * 此系数
        public static float haloIntensity = 0.28f;        // 外辉亮度 = 亮度 * 此系数
        // 子光束/集束侧光(非主光束)的防糊团控制：大量子光束重叠会糊成白球，这里变细+去辉+距离衰减
        public static float subWidthScale = 0.5f;         // 子/侧光束相对其粗细档的额外变细(1=不额外变细)
        public static float subIntensityScale = 0.35f;    // 子/侧光束亮度整体缩放(<1 让主光束压过成团的子光束)
        public static bool  subNoHalo = true;             // 子/侧光束不画外辉(消除大量重叠糊成白球)
        public static float focusMuzzleMax = 100f;        // muzzleRadius<=此值视为“预热/集束充能光束”：保留渐亮与光晕，不做糊团压制
        public static float focusIntensityScale = 0.6f;   // 预热/集束光束亮度缩放(叠在渐亮之上)：让其最大亮度也明显低于主光束
        public static float haloEndFade = 0.06f;          // 外辉首尾渐变长度(占全长比例): 消除抵达星球处的整齐切口; 核心不渐隐. 传给 shader _HaloEndFade
        public static float haloFadeFrac = 0.15f;          // 外辉"相机深度"远端渐隐起点(占本视图最远光束终点距离, 0..1): 从该比例处开始淡, 到光束终点淡到0; 越小淡得越早越长。核心不淡。注意 _ClipFar 已改为光束实际终点距离(非远裁剪面), 故此参数现在真正生效
        public static int ztestMode = 4;                 // 近景分组(壳内)深度测试: 4=LEqual(真实前后遮挡, 默认)。远景分组(壳外)固定用 8=Always 保证永不消失, 不受此值影响。
        // 汇聚置顶(segsFront, ZTest Always): 盖过戴森壳(无玻璃遮挡), 但会连星球也盖住 => 用 CPU 射线-球 手动剔除"被载入星系某星球真正挡在前方"的那段光束。只跳过绘制不写深度 => 无黑边风险。
        public static bool  focusRespectPlanets = true;  // true=置顶汇聚光束仍被星球遮挡(仅跳过被挡段); false=纯 Always 盖一切
        public static float planetOccludeScale = 1.15f;  // 星球遮挡半径系数(略放大以含大气层边缘, 1.0=按 realRadius)
        public static float clampFarScale = 0.9f;         // 原始(永不消失)方案生效系数: 端点以相机为球心钳制半径 farR = farClipPlane * 此值。只缩短不丢弃 => 光束在所有视角可见(遮挡可能偏保守)。
        public static float farKeep = 0.98f;              // 尊重深度方案的裁剪系数: 把光束线段沿相机前向深度裁到 [nearClipPlane, farClipPlane*farKeep] 之间, 保留真实位置/真实深度参与遮挡, 丢弃视锥外部分。因 D<=far 宽度不再爆炸(无玻璃), 且不伪钉深度(消除"远端被钳到固定深度->连后壳也误挡")。越接近1保留越长。
        public static float distAttenStrength = 0.85f;    // 子/侧光束距离衰减强度(0=不衰减,1=最强)
        public static float distAttenFloor = 0.1f;       // 衰减到底时保留的亮度/粗细(>0 保证仍隐约可见)
        public static float distAttenRef = 0f;            // >0=用固定参考距离(视图单位); 0=自动用相机远裁剪面归一
        public static int ribbonSubdiv = 48;              // 主光束(跨星系超长)细分段数：每段独立算 sideV，修复长 quad 把亮芯挤到一侧
        // 螺旋能量丝(仅主视图、主光束起点附近)
        public static float strandLenFactor = 3.0f;       // 丝长 = muzzleRadius * 此系数(下限 1200m)
        public static float strandRadiusFactor = 0.9f;    // 起始环绕半径 = muzzleRadius * 此系数
        public static int strandSegs = 14;                // 每条丝的分段数(越大越平滑)
        public static float strandMinPx = 2.0f;           // 丝的屏幕最小半宽(像素)
        public static float strandWidthWorld = 30f;       // 丝的世界半宽(米)
        // 炮口 Flare(起点相机朝向光斑，仅主视图主光束)
        public static float flareSizeFactor = 2.2f;       // Flare 半径 = muzzleRadius * 此系数(世界)
        public static float flareIntensity = 0.9f;        // Flare 亮度 = intensity * 此系数
        // 诊断/功能开关(位掩码)：bit0=核心 bit1=外辉 bit2=螺旋丝 bit3=炮口Flare。
        // 当前默认 3 = 核心+外辉（拿回光晕）。前提：bundle 里必须是 v5 及以后(外辉已 saturate 修复)，否则会重现 pow(负)->NaN 黑边。
        // 丝/Flare 仍关(bit2/bit3=0)；若未重打包 v5 先回到 1 只画核心。只改此值需重编 DLL。
        public static int debugKinds = 3;
        // 诊断日志总开关: 关闭后不再打印周期性/信息性诊断日志(遮挡诊断/叠加诊断/shader 信息); 需要排查时置 true。材质初始化失败等一次性错误日志不受影响仍会打印。
        public static bool diagLog = false;
        // 诊断开关: false=只显示现实视角叠加的原生 sailBullet 层(单独验证叠加); true=同时渲染我们自研的 ribbon shader 激光。现已恢复 ribbon 以看整体效果
        public static bool debugShowRibbon = true;
        // ===== 叠加层(sailBullet)外观调参 =====
        // 教训: 每颗 SailBullet 是"跨自身 rBegin->rEnd 的精灵"。沿长度分段 => 每颗变短 => 短精灵按固定宽度显得比光束粗, 且相位恒定 => 冻在原地 = 一段一段静止的集束;
        //       汇聚/攻击的"静止小子弹条"同理是恒定相位(不流动)的整跨短股。现方案: 不分段, 多条整跨平行股 + 相位随时间流动(head=now/flowLife+phase)。
        public static float overlaySizeMain   = 4.0f;     // 主光束 _Size 倍数(实测材质 _Size 对宽度无效, 保留无害; 粗细靠半径倍数+股数)
        // 现实视角"叠加的原生 sailBullet 主激光"开关: 用户反馈效果一般 => 暂时永久隐藏(false), 主激光改由 ribbon 单独呈现。攻击叠加不受影响。
        public static bool  overlayShowMain   = false;
        public static float overlaySizeFocus  = 2.5f;     // 汇聚 _Size 倍数
        public static float overlaySizeSub    = 1.6f;     // 攻击 _Size 倍数
        // 粗细 = 横截面圆盘铺开半径(muzzle/impact, 主光束再 ×overlayMainRadiusMul) × 股数密度
        public static float overlayMainRadiusMul = 1.2f;  // 主光束整体粗细倍数(原 2.0 收到 ≈60% => 1.2; 粗细靠它, 密度/亮度靠股数)
        public static int   overlayStrandsMain  = 60;     // 主光束整跨流动股数(粗细收缩后加股数, 强化密度与亮度)
        public static int   overlayStrandsFocus = 5;      // 汇聚流动股数(整跨不冻头 => 不再静止短条; 汇聚半径本就小, 数条自动并成束)
        public static int   overlayStrandsSub   = 5;      // 攻击流动股数
        // bundle / 材质名匹配（打进 mmstabicon 后按此名查找）
        public static string bundleHint = "mmstabicon";
        public static string matHint = "laser";

        private const int STRIDE = 112;          // = SailBullet.dataLen（回退路径）
        private const int VERTS_PER_BULLET = 8;  // 与游戏一致：MeshTopology.Quads, bulletCursor*8

        // 主路径的实例数据（与 shader 里的 LaserSeg 内存布局严格一致，64 字节）
        private struct LaserSeg
        {
            public Vector3 beginPos;   // 12
            public float intensity;    // 4
            public Vector3 endPos;     // 12
            public float halfWidth;    // 4
            public Vector4 color;      // 16
            public float minPx;        // 4
            public float capStart;     // 4
            public float capEnd;       // 4
            public float kind;         // 4  0=核心 1=外辉 2=螺旋丝 3=炮口Flare
        }

        private const int SEG_STRIDE = 64;
        private const int VERTS_PER_SEG = 4;

        private static readonly Dictionary<int, BeamProfile> beams = new Dictionary<int, BeamProfile>();
        private static readonly List<int> keyScratch = new List<int>();

        // 回退路径(子弹)
        private static readonly List<SailBullet> pool = new List<SailBullet>(1024);
        // 现实视角叠加层: 按角色分桶的 beam key, 各桶用不同亮度分别绘制
        private static readonly List<int> mainKeys = new List<int>();
        private static readonly List<int> focusKeys = new List<int>();
        private static readonly List<int> blobKeys = new List<int>();
        private static ComputeBuffer buffer;
        private static int capacity;
        private static Material mat;
        private static bool matFailed;
        private static bool matHasSize;         // 原生 bullet 材质是否有 _Size 属性(用于增粗)
        private static float matSizeDefault = 1f;

        // 主路径(shader)
        private static readonly List<LaserSeg> segs = new List<LaserSeg>(256);
        // 远景分组(壳外/跨星系): 与近景分开渲染。近景用真实深度 + ZTest LEqual => 与戴森壳正确前后遮挡; 远景钳制到相机远球 + ZTest Always => 永不消失(壳外本无遮挡物)。
        private static readonly List<LaserSeg> segsFar = new List<LaserSeg>(256);
        // 置顶分组(仅汇聚 ribbon): ZTest Always => 覆盖所有戴森壳, 维持"汇聚无玻璃遮挡"特性; 主光束 ribbon 仍走 segs(LEqual)被壳遮挡。
        private static readonly List<LaserSeg> segsFront = new List<LaserSeg>(256);
        // 星球遮挡体(视图空间, xyz=球心 w=半径): 每帧由 BuildRibbonSegs 用载入星系行星重建, 供置顶汇聚段做 CPU 遮挡剔除
        private static readonly List<Vector4> occluders = new List<Vector4>(32);
        private static Vector3 occCam;
        // 逐帧路由参数(BuildRibbonSegs 设置, RouteQuad 使用)
        private static Vector3 s_camPos;   // 相机(相对帧)位置
        private static float s_nearR;      // 近景球半径 = 载入星系最大戴森壳半径(dysonRadius*40000) * 冗余; 壳心 = 相对帧原点 = 本星系恒星(发射或接收端被载入者)
        private static float s_clampR;     // 远景钳制半径 = farClipPlane * clampFarScale
        private static bool s_universe;    // 是否宇宙(主)视图; 星图/编辑器保持旧的"钳制 + LEqual"单分组
        private static float nearRadiusRedundancy = 1.2f; // 近景球相对最大壳半径的冗余系数(用户建议 ~1.2)
        private static ComputeBuffer segBuffer;
        private static int segCapacity;
        private static Material laserMat;
        private static bool laserMatFailed;

        private static StarCannonLaserHost host;

        /// <summary>登记 / 刷新一条光束。用相同的 key 每帧重复调用可维持其存活；停止调用后到期自动消失。</summary>
        public static void AddBeam(int key, VectorLF3 begin, VectorLF3 end, Color? color = null,
            int bulletCount = 0, float muzzleRadius = -1f, float impactRadius = -1f, float holdSeconds = 0.35f,
            float intensity = 1f, int strandCount = 0, float strandTurns = 3.5f, float strandSpeed = 1.4f,
            int beginStarIdx = -1, int endStarIdx = -1, float beginShellLen = 0f, float endShellLen = 0f)
        {
            if (begin == end) return; // 退化光束
            if (muzzleRadius < 0f) muzzleRadius = defaultMuzzleRadius;
            if (impactRadius < 0f) impactRadius = defaultImpactRadius;
            if (intensity < 0f) intensity = 0f;
            else if (intensity > 1f) intensity = 1f;

            BeamProfile p;
            if (beams.TryGetValue(key, out p))
            {
                p.begin = begin;
                p.end = end;
                p.intensity = intensity;
                p.beginStarIdx = beginStarIdx;
                p.endStarIdx = endStarIdx;
                p.beginShellLen = beginShellLen;
                p.endShellLen = endShellLen;
                p.expireAt = Time.unscaledTime + holdSeconds;
                beams[key] = p;
            }
            else
            {
                if (bulletCount <= 0) bulletCount = baseBulletCount;
                p = new BeamProfile
                {
                    begin = begin,
                    end = end,
                    muzzleRadius = muzzleRadius,
                    impactRadius = impactRadius,
                    bulletCount = bulletCount,
                    seed = unchecked((int)((uint)key * 701932u + 0x9e3779b9u)),
                    color = color ?? DefaultColor,
                    flowLife = 0.28f,
                    intensity = intensity,
                    strandCount = strandCount,
                    strandTurns = strandTurns,
                    strandSpeed = strandSpeed,
                    expireAt = Time.unscaledTime + holdSeconds,
                    beginStarIdx = beginStarIdx,
                    endStarIdx = endStarIdx,
                    beginShellLen = beginShellLen,
                    endShellLen = endShellLen,
                };
                beams[key] = p;
            }
        }

        /// <summary>立即清空所有光束（停火 / 读档 / 切存档时调用）。</summary>
        public static void Clear()
        {
            beams.Clear();
        }

        // 惰性创建宿主 GameObject（其 OnRenderObject 会被每个相机各调一次）
        public static void EnsureHost()
        {
            if (host != null) return;
            if (beams.Count == 0) return;
            var go = new GameObject("MMS_StarCannonLaserRenderer");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            host = go.AddComponent<StarCannonLaserHost>();
            // 说明(读源码确证): 戴森壳框体经 GameMain.logic.Draw → DysonSphere.DrawModel → Graphics.DrawMesh
            // 在正常不透明通道就写入深度, 早于 OnRenderObject; 故 OnRenderObject 里 ZTest LEqual 能读到壳真实深度,
            // 无需挪到 onPostRender(那样反而因相机投影状态不同导致屏幕边缘消失)。遮挡真凶是"伪钳/伪裁到假深度", 用视锥裁剪解决。
        }

        // 每帧在 LateUpdate 调用：清理过期光束；无光束时销毁宿主以零残留
        public static void TickLifetime()
        {
            float now = Time.unscaledTime;
            if (beams.Count == 0)
            {
                if (host != null)
                {
                    UnityEngine.Object.Destroy(host.gameObject);
                    host = null;
                }
                return;
            }
            keyScratch.Clear();
            foreach (var kv in beams)
                if (kv.Value.expireAt < now) keyScratch.Add(kv.Key);
            for (int i = 0; i < keyScratch.Count; i++) beams.Remove(keyScratch[i]);
        }

        // 每个相机的 OnRenderObject 都会调用到这里；只让“当前活动视图对应的相机”真正绘制，避免重复绘制。
        public static void Render()
        {
            if (beams.Count == 0) return;
            if (DSPGame.IsMenuDemo) return;

            Camera cur = Camera.current;
            if (cur == null) return;

            GameData gd = GameMain.data;
            if (gd == null) return;

            StarData viewStar = null;
            switch (DysonSphere.renderPlace)
            {
                case ERenderPlace.Universe:
                    viewStar = gd.localStar;
                    if (cur != Camera.main) return;
                    break;
                case ERenderPlace.Starmap:
                    if (UIRoot.instance == null || UIRoot.instance.uiGame == null) return;
                    var starmap = UIRoot.instance.uiGame.starmap;
                    if (starmap == null) return;
                    viewStar = starmap.viewStarSystem;
                    if (cur != starmap.screenCamera) return;
                    break;
                case ERenderPlace.Dysonmap:
                    if (UIRoot.instance == null || UIRoot.instance.uiGame == null) return;
                    var editor = UIRoot.instance.uiGame.dysonEditor;
                    if (editor == null) return;
                    if (cur != editor.screenCamera) return;
                    viewStar = editor.selection != null ? editor.selection.viewStar : null;
                    break;
            }
            if (viewStar == null) return;

            // 主路径：自定义 shader ribbon
            if (TryInitLaserMaterial(cur))
            {
                if (debugShowRibbon)
                {
                    BuildRibbonSegs(gd, viewStar, cur);
                    UploadAndDrawRibbon();
                }

                // 额外(仅现实视角): 用游戏【原生太阳帆 bullet 通道】再叠一层同源激光。该通道深度/遮挡关系正确,
                // 覆盖到本应被戴森壳部分遮挡的那段上, 提供正确的"前后遮挡"观感; ribbon 继续负责"永不消失"。
                // 门控: 主光束(1)=发射系或目标系; 汇聚(>=2000)=发射系; 攻击子激光(1000..1999)=目标系; 其它星系不叠。
                // 并按角色分桶绘制, 让叠加层的亮度也匹配 ribbon(主=满亮度, 汇聚=随预热渐亮, 攻击=压暗), 粗细本已随 muzzleRadius 对齐。
                if (DysonSphere.renderPlace == ERenderPlace.Universe)
                {
                    DrawOverlayBullets(gd, viewStar);
                }
                return;
            }
            
            // 回退路径：sailBullet 粒子（bundle 未重打包时保证激光不消失）
            if (!TryInitMaterial()) return;
            BuildBulletPool(gd, viewStar);
            UploadAndDraw(DefaultColor);
        }

        // ---------- 主路径 (shader) ----------

        private static bool TryInitLaserMaterial(Camera cur)
        {
            if (laserMat != null) return true;
            if (laserMatFailed) return false;
            try
            {
                AssetBundle bundle = FindLaserBundle();
                if (bundle == null)
                {
                    // bundle 还没加载（可能尚未进游戏主菜单），暂不置 failed，下次再试
                    return false;
                }
                string[] names = bundle.GetAllAssetNames();
                foreach (string n in names)
                {
                    if (n.IndexOf(matHint, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    Material m = bundle.LoadAsset<Material>(n);
                    if (m != null && m.shader != null)
                    {
                        laserMat = UnityEngine.Object.Instantiate(m);
                        laserMat.hideFlags = HideFlags.HideAndDontSave;
                        LogShaderDiag("加载材质", n, laserMat);
                        return true;
                    }
                    Shader sh = bundle.LoadAsset<Shader>(n);
                    if (sh != null)
                    {
                        laserMat = new Material(sh);
                        laserMat.hideFlags = HideFlags.HideAndDontSave;
                        LogShaderDiag("用shader创建材质", n, laserMat);
                        return true;
                    }
                }
                // bundle 已加载但没有 laser 资产 => 需要重打 bundle 并重启游戏；置 failed，本次及后续回退到子弹路径
                laserMatFailed = true;
                Utils.Log("[StarCannonLaser] 在 bundle 中未找到激光材质/shader(名含 '" + matHint +
                          "')，使用回退渲染。请确认已把 StarCannonLaser.shader 打进 " + bundleHint + " 并重启游戏。");
            }
            catch (Exception e)
            {
                laserMatFailed = true;
                Utils.Log("[StarCannonLaser] 初始化自定义激光材质失败: " + e.Message);
            }
            return false;
        }

        // 诊断日志：打印实际加载到的 shader 版本特征，用于确认 bundle 里的 shader 是不是最新的双 Pass 版。
        // 期望：passCount=2 且 _PulseParams/_FlowRand/_FlareTex 均为 True；否则说明游戏加载的是旧 shader(重打包未生效)。
        private static void LogShaderDiag(string how, string n, Material m)
        {
            try
            {
                Shader s = m != null ? m.shader : null;
                string sname = s != null ? s.name : "(null)";
                int pc = s != null ? s.passCount : -1;
                bool pulse = m != null && m.HasProperty("_PulseParams");
                bool flow = m != null && m.HasProperty("_FlowRand");
                bool flare = m != null && m.HasProperty("_FlareTex");
                bool laserTex = m != null && m.HasProperty("_LaserTex");
                bool hasVer = m != null && m.HasProperty("_MMSVersion");
                float ver = hasVer ? m.GetFloat("_MMSVersion") : -1f;
                if (diagLog)
                Utils.Log("[StarCannonLaser] " + how + ": " + n + " | shader='" + sname + "' passCount=" + pc +
                          " _MMSVersion=" + ver + (hasVer ? "" : "(无此属性=旧shader!)") +
                          " _PulseParams=" + pulse + " _FlowRand=" + flow + " _FlareTex=" + flare + " _LaserTex=" + laserTex +
                          "  (期望 _MMSVersion=2 且 passCount=2；_MMSVersion缺失/=-1 => bundle 里仍是旧 shader，重打包未生效)");
            }
            catch { }
        }

        // 复用已经被 CommonAPI 加载的 mmstabicon bundle 实例（不能对同一文件二次 LoadFromFile）
        private static AssetBundle FindLaserBundle()
        {
            foreach (AssetBundle ab in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (ab == null) continue;
                string nm = ab.name;
                if (nm != null && nm.IndexOf(bundleHint, StringComparison.OrdinalIgnoreCase) >= 0)
                    return ab;
            }
            return null;
        }

        private static void BuildRibbonSegs(GameData gd, StarData viewStar, Camera cur)
        {
            segs.Clear();
            segsFar.Clear();
            segsFront.Clear();
            cur_MaxViewDist = 0f;   // 逐帧重算本视图最远可绘制端点(用作外辉深度渐隐参考)
            // 缓存当前相机投影参数，供 UploadAndDrawRibbon 计算屏幕像素下限 -> 世界宽度
            cur_Ortho = cur.orthographic;
            cur_PixelHeight = cur.pixelHeight;
            cur_Fov = cur.fieldOfView;
            cur_FarClip = cur.farClipPlane;
            VectorLF3 relativePos = gd.relativePos;
            Quaternion relativeRot = gd.relativeRot;
            VectorLF3 viewTarget = UIStarmap.viewTargetStatic;
            VectorLF3 starPos = viewStar.uPosition;

            // 置顶汇聚光束用 ZTest Always 会盖住星球, 这里重建"载入星系行星"的视图空间球体列表, 供被星球真挡在前方时跳过该段(不写深度=>无黑边)。非宇宙视图不剔除。
            occluders.Clear();
            occCam = cur.transform.position;
            if (DysonSphere.renderPlace == ERenderPlace.Universe && focusRespectPlanets && viewStar != null)
            {
                PlanetData[] planets = viewStar.planets;
                int planetCount = viewStar.planetCount;
                if (planets != null)
                {
                    for (int i = 0; i < planetCount && i < planets.Length; i++)
                    {
                        PlanetData pd = planets[i];
                        if (pd == null) continue;                     // 只处理玩家当前(载入)星系的行星
                        double rr = pd.realRadius * (double)planetOccludeScale;
                        if (rr <= 1e-3) continue;
                        Vector3 c = (Vector3)Maths.QInvRotateLF(relativeRot, pd.uPosition - relativePos); // 与 vb/ve 同一视图坐标系
                        occluders.Add(new Vector4(c.x, c.y, c.z, (float)rr));
                    }
                }
            }

            // 分开渲染(用户方案): 以"载入星系最大戴森壳半径"为界。壳内(近景)用真实深度 + ZTest LEqual => 与壳正确前后遮挡;
            // 壳外(远景, 多为跨星系段)钳制到相机远球 + ZTest Always => 永不消失(壳外本无遮挡物)。
            // 相对帧原点 = 载入星系的恒星(玩家所在; 无论它是发射端还是接收端), 壳心即原点, 故近景判据用 |p| <= nearR。
            Vector3 camPos = cur.transform.position;   // Unity 世界(=相对渲染帧)相机位置, 与 vb/ve 同一坐标系
            bool universe = DysonSphere.renderPlace == ERenderPlace.Universe;
            s_camPos = camPos;
            s_universe = universe;
            s_clampR = cur.farClipPlane * clampFarScale;                    // 远景钳制半径(< far, 宽度不炸)
            // 近景球半径 = 该恒星最大戴森壳半径(dysonRadius*40000, 与 vb/ve 同为 uPosition 单位) * 冗余。非宇宙视图: MaxValue => 全部走"钳制+LEqual"单分组(星图/编辑器保持旧的正常表现)。
            s_nearR = universe ? Mathf.Max(viewStar.dysonRadius * 40000f * nearRadiusRedundancy, 1f) : float.MaxValue;

            foreach (var kv in beams)
            {
                BeamProfile p = kv.Value;
                Vector3 vb, ve;
                if (!TryViewPoints(p.begin, p.end, relativePos, relativeRot, viewTarget, starPos, out vb, out ve))
                    continue;
                // 稳定基线方案(按用户要求回退): 整条光束端点【以相机为球心】钳制到 s_clampR(方向不变, 只缩短) => 光束在所有视角都可见、不消失、无玻璃;
                // 配合单分组 + ZTest LEqual(4) => 遮挡偏保守(通常被戴森壳挡住)。这是"只有遮挡错误、其余都稳定"的那个版本。
                vb = CamClamp(vb, camPos, s_clampR);
                ve = CamClamp(ve, camPos, s_clampR);
                // 记录本视图端点的【眼深度】(到相机距离, 与 shader 的 D 一致)，作为外辉深度渐隐 _ClipFar：终点 D≈此值 -> dfade→0
                float dNear = (vb - camPos).magnitude, dFarE = (ve - camPos).magnitude;
                float endD = Mathf.Min(Mathf.Max(dNear, dFarE), s_clampR);   // 实际可绘制最远距离(远景被钳到 s_clampR)
                if (endD > cur_MaxViewDist) cur_MaxViewDist = endD;

                // 世界半宽只在主视图给(近处更粗)；星图/编辑器下坐标被 *0.00025 缩放，世界宽会给 0，纯用屏幕像素宽避免变成巨大宽面

                // 按 muzzleRadius 归一的粗细档 + 角色分类
                float widthScale = p.muzzleRadius / defaultMuzzleRadius;
                bool isMain = widthScale >= 0.999f;                         // 主光束(星→星)
                bool isFocus = !isMain && p.muzzleRadius <= focusMuzzleMax; // 预热/集束充能光束(近炮口、数量少、需要看到渐亮)
                bool isBlob = !isMain && !isFocus;                          // 目标侧子光束(数量多、会糊团、需压制)

                // 距离衰减：只压目标侧成团子光束；主光束与预热光束不衰减(预热光束要保留由暗到亮的过程)
                float atten = 1f;
                if (isBlob)
                {
                    float D = (vb + ve).magnitude * 0.5f;
                    float refD = distAttenRef > 0f ? distAttenRef : Mathf.Max(1f, cur.farClipPlane);
                    float rel = Mathf.Clamp01(D / refD);
                    atten = Mathf.Lerp(1f, Mathf.Max(1f - rel, distAttenFloor), distAttenStrength);
                }

                float roleScale = isMain ? 1f : (isFocus ? focusIntensityScale : subIntensityScale);
                float inten = p.intensity * atten * roleScale;
                if (inten <= 0f) continue;
                float hw = universe ? Mathf.Max(1f, p.muzzleRadius) * atten : 0f;
                float pxScale = Mathf.Max(widthScale, 0.3f) * (isBlob ? subWidthScale : 1f);
                // 核心像素下限(屏幕恒定 ~minPx 宽)：minPx*D/proj 换算成世界宽, 屏幕宽恒为 minPx 不随 D 爆炸; 光束已裁进 far, 远处靠此下限保持可见。
                float coreMinPx = Mathf.Max(defaultMinPixelsCore * pxScale * atten, 1.2f);
                Vector4 c = p.color;

                // 主光束跨星系超长：细分成多段 ribbon，每段用各自中点算 sideV，避免单条超长 quad 把亮芯挤到一侧
                int n = isMain ? Mathf.Max(1, ribbonSubdiv) : 1;
                // 汇聚 ribbon 置顶(用户要求): 仅【现实视角(Universe)】把 isFocus 段进 segsFront(ZTest Always, 覆盖所有戴森壳, 无玻璃遮挡);
                // 星图/戴森壳地图不改(仍进 segs 走常规 ztestMode), 保持原有表现。主/子光束任何视图都进 segs(被壳遮挡)。
                List<LaserSeg> grp = (isFocus && universe) ? segsFront : segs;
                // 核心光柱（子/侧光束更细更暗，远处再衰减，避免大量重叠糊成白球）
                if ((debugKinds & 1) != 0)
                    AddRibbon(vb, ve, inten, hw, c, coreMinPx, 0f, n, grp);
                // 外辉——仅主视图；主光束与预热光束都给外辉(预热要看到聚能发光)，目标侧子光束默认不给(subNoHalo)以消除重叠白球
                if (universe && (debugKinds & 2) != 0 && (isMain || isFocus || !subNoHalo))
                    AddRibbon(vb, ve, inten * haloIntensity, hw * haloWidthScale, c, coreMinPx * haloWidthScale, 1f, n, grp);

                // 起点环绕的螺旋能量丝 + 炮口 Flare（仅主视图、主光束）
                if (universe && p.strandCount > 0)
                {
                    if ((debugKinds & 4) != 0)
                        GenStrands(p, relativePos, relativeRot, viewTarget, starPos, inten, c);
                    if ((debugKinds & 8) != 0)
                    {
                        Vector3 fa = vb, fb = vb;
                        RouteQuad(ref fa, ref fb, segs).Add(new LaserSeg
                        {
                            beginPos = fa,
                            intensity = inten * flareIntensity,
                            endPos = fb,
                            halfWidth = p.muzzleRadius * flareSizeFactor,
                            color = c,
                            minPx = 0f,
                            capStart = 0f,
                            capEnd = 0f,
                            kind = 3f,
                        });
                    }
                }
            }

            // [遮挡诊断] 已作为不必要调试输出移除。
        }

        // 把一条 vb->ve 的光束细分成 n 段 ribbon quad；capStart/capEnd 复用为“全局沿长”(0..1)，供 shader 连续流动
        private static void AddRibbon(Vector3 vb, Vector3 ve, float inten, float hw, Vector4 c, float minPx, float kind, int n, List<LaserSeg> group)
        {
            if (n < 1) n = 1;
            for (int i = 0; i < n; i++)
            {
                float t0 = (float)i / n;
                float t1 = (float)(i + 1) / n;
                Vector3 q0 = Vector3.Lerp(vb, ve, t0);
                Vector3 q1 = Vector3.Lerp(vb, ve, t1);
                // 置顶汇聚组: ZTest Always 会盖住星球, 故对该段中点做星球遮挡剔除(被挡则整段跳过, 只跳过绘制不写深度 => 无黑边)
                if (group == segsFront && focusRespectPlanets && IsOccludedByPlanet((q0 + q1) * 0.5f))
                    continue;
                RouteQuad(ref q0, ref q1, group).Add(new LaserSeg
                {
                    beginPos = q0,
                    endPos = q1,
                    intensity = inten,
                    halfWidth = hw,
                    color = c,
                    minPx = minPx,
                    capStart = t0,
                    capEnd = t1,
                    kind = kind,
                });
            }
        }

        // 视图空间点 mid 是否被某颗星球挡在相机前方(相机->mid 射线命中星球球体, 且命中点比 mid 更近)。mid 与各 occluder 同处视图坐标系(相对渲染帧), 单位一致。
        private static bool IsOccludedByPlanet(Vector3 mid)
        {
            if (occluders.Count == 0) return false;
            for (int i = 0; i < occluders.Count; i++)
            {
                Vector4 o = occluders[i];
                Vector3 to = new Vector3(o.x, o.y, o.z) - occCam;   // 相机->球心
                Vector3 dir = mid - occCam;                          // 相机->光束点
                float L = dir.magnitude;
                if (L < 1e-3f) continue;
                dir /= L;
                float tca = Vector3.Dot(to, dir);
                if (tca <= 0f) continue;                             // 球在相机背后
                float r = o.w;
                float d2 = to.sqrMagnitude - tca * tca;              // 射线到球心垂直距离^2
                if (d2 > r * r) continue;                            // 射线错过球
                float thc = Mathf.Sqrt(r * r - d2);
                float t0 = tca - thc;                                // 首次命中距离
                if (t0 > 0f && t0 < L) return true;                  // 球在光束点之前 => 遮挡
            }
            return false;
        }

        // 稳定基线方案(按用户要求回退): 暂不做近/远分组。每段仅以相机为球心钳制到 s_clampR, 统一进调用方指定分组(主光束=segs; 汇聚=segsFront 置顶), 由对应 ztestMode 绘制。
        private static List<LaserSeg> RouteQuad(ref Vector3 q0, ref Vector3 q1, List<LaserSeg> group)
        {
            q0 = CamClamp(q0, s_camPos, s_clampR);
            q1 = CamClamp(q1, s_camPos, s_clampR);
            return group;
        }

        // 生成起点附近环绕主光束旋转/收敛的螺旋能量丝
        private static void GenStrands(in BeamProfile p, VectorLF3 relativePos, Quaternion relativeRot,
            VectorLF3 viewTarget, VectorLF3 starPos, float inten, Vector4 c)
        {
            VectorLF3 axis = p.end - p.begin;
            double len = axis.magnitude;
            if (len < 1e-3) return;
            VectorLF3 dir = axis / len;

            VectorLF3 up = new VectorLF3(0, 0, 1);
            if (Math.Abs(VectorLF3.Dot(dir, up)) > 0.98) up = new VectorLF3(0, 1, 0);
            VectorLF3 perp1 = VectorLF3.Cross(dir, up).normalized;
            VectorLF3 perp2 = VectorLF3.Cross(dir, perp1).normalized;

            float strandLen = Mathf.Max(1200f, p.muzzleRadius * strandLenFactor);
            if (strandLen > len * 0.5) strandLen = (float)(len * 0.5);
            float radius0 = p.muzzleRadius * strandRadiusFactor;
            float time = Time.unscaledTime;
            int n = Mathf.Max(4, strandSegs);
            float twoPi = (float)Math.PI * 2f;

            for (int k = 0; k < p.strandCount; k++)
            {
                float phase = k * twoPi / p.strandCount;
                for (int i = 0; i < n; i++)
                {
                    float u0 = (float)i / n;
                    float u1 = (float)(i + 1) / n;
                    VectorLF3 w0 = HelixPoint(p.begin, dir, perp1, perp2, strandLen, radius0, phase, time, p.strandTurns, p.strandSpeed, u0);
                    VectorLF3 w1 = HelixPoint(p.begin, dir, perp1, perp2, strandLen, radius0, phase, time, p.strandTurns, p.strandSpeed, u1);
                    Vector3 a, b;
                    if (!TryViewPoints(w0, w1, relativePos, relativeRot, viewTarget, starPos, out a, out b)) continue;

                    float flow = 0.5f + 0.5f * Mathf.Sin(u0 * p.strandTurns * twoPi - time * p.strandSpeed * 2f);
                    float si = inten * (0.35f + 0.65f * flow) * (1f - u0 * 0.6f);
                    if (si <= 0f) continue;

                    RouteQuad(ref a, ref b, segs).Add(new LaserSeg
                    {
                        beginPos = a,
                        intensity = si,
                        endPos = b,
                        halfWidth = strandWidthWorld,
                        color = c,
                        minPx = strandMinPx,
                        capStart = i == 0 ? 1f : 0f,
                        capEnd = i == n - 1 ? 1f : 0f,
                        kind = 2f,
                    });
                }
            }
        }

        // 螺旋线上的一点：沿轴前进 base，横向按角度旋转、随 u 收敛到轴，半径带小幅拖动(不稳定感)
        private static VectorLF3 HelixPoint(VectorLF3 begin, VectorLF3 dir, VectorLF3 perp1, VectorLF3 perp2,
            float strandLen, float radius0, float phase, float time, float turns, float speed, float u)
        {
            VectorLF3 bpos = begin + dir * (u * strandLen);
            float ang = phase + time * speed + u * turns * (float)Math.PI * 2f;
            float conv = 1f - u; // 收敛到轴
            float wob = 0.75f + 0.25f * Mathf.Sin(u * 12f + time * 3f + phase);
            float radius = radius0 * conv * wob;
            VectorLF3 off = perp1 * Mathf.Cos(ang) * radius + perp2 * Mathf.Sin(ang) * radius;
            return bpos + off;
        }

        // 视图空间换算（与 DysonSwarm.BulletGameTick 的公式一致），返回视图空间的起终点
        private static bool TryViewPoints(VectorLF3 begin, VectorLF3 end, VectorLF3 relativePos,
            Quaternion relativeRot, VectorLF3 viewTarget, VectorLF3 starPos, out Vector3 vb, out Vector3 ve)
        {
            vb = Vector3.zero; ve = Vector3.zero;
            switch (DysonSphere.renderPlace)
            {
                case ERenderPlace.Universe:
                    vb = (Vector3)Maths.QInvRotateLF(relativeRot, begin - relativePos);
                    ve = (Vector3)Maths.QInvRotateLF(relativeRot, end - relativePos);
                    break;
                case ERenderPlace.Starmap:
                    vb = (Vector3)((begin - viewTarget) * 0.00025);
                    ve = (Vector3)((end - viewTarget) * 0.00025);
                    break;
                case ERenderPlace.Dysonmap:
                    vb = (Vector3)((begin - starPos) * 0.00025);
                    ve = (Vector3)((end - starPos) * 0.00025);
                    break;
                default:
                    return false;
            }
            return true;
        }

        // 将视图空间点沿其方向钳制到以相机为球心、半径 maxR 的球内(保持方向，只缩长度)
        private static Vector3 ClampMag(Vector3 v, float maxR)
        {
            float m = v.magnitude;
            if (m > maxR && m > 1e-5f) return v * (maxR / m);
            return v;
        }

        // 真正"以相机为球心"的钳制：把点 p 压到距相机 <= R 的球内(保持相机->p 方向, 只缩短)。
        // 深度/远裁剪是按相机眼深度算的, 钳制球心必须是相机而非 relativePos 原点, 否则原点内 farR 内的点眼深度仍可能超出远平面 -> z≈1 -> 被壳误挡。
        private static Vector3 CamClamp(Vector3 p, Vector3 cam, float R)
        {
            Vector3 d = p - cam;
            float m = d.magnitude;
            if (m > R && m > 1e-5f) return cam + d * (R / m);
            return p;
        }

        private static void UploadAndDrawRibbon()
        {
            // 分开渲染(用户方案): 近景(戴森壳内)真实深度 + ZTest LEqual => 与壳正确前后遮挡; 远景(壳外/跨星系)钳制 + ZTest Always => 永不消失。
            float projFactor;
            if (cur_Ortho) projFactor = 1e6f;
            else projFactor = cur_PixelHeight * 0.5f / Mathf.Max(1e-4f, Mathf.Tan(cur_Fov * 0.5f * Mathf.Deg2Rad));
            if (projFactor < 1f) projFactor = 1f;
            float clipFar = cur_MaxViewDist;
            if (clipFar <= 1f) clipFar = 1e9f;               // 没有有效光束端点 -> 关掉深度渐隐

            DrawRibbonGroup(segs, ztestMode, projFactor, clipFar);   // 近景分组: LEqual(默认4), 含主光束(维持被戴森壳遮挡)
            DrawRibbonGroup(segsFar, 8, projFactor, clipFar);        // 远景分组: Always(8), 保证壳外/跨星系段不被任何深度挡掉
            DrawRibbonGroup(segsFront, 8, projFactor, clipFar);      // 置顶分组: Always(8), 仅汇聚 ribbon => 覆盖所有戴森壳(无玻璃遮挡), 最后绘制确保在最前
        }

        // 上传并即时绘制一个分组。每组各自 SetBuffer+SetData+SetPass(0)+Draw(重设 buffer 绑定, 避免二次 Draw 读到脏 buffer)。
        private static void DrawRibbonGroup(List<LaserSeg> list, int zmode, float projFactor, float clipFar)
        {
            int count = list.Count;
            if (count == 0) return;

            if (segBuffer == null || segCapacity < count)
            {
                int newCap = segCapacity == 0 ? 256 : segCapacity;
                while (newCap < count) newCap *= 2;
                if (segBuffer != null) segBuffer.Release();
                segBuffer = new ComputeBuffer(newCap, SEG_STRIDE, ComputeBufferType.Default);
                segCapacity = newCap;
            }

            LaserSeg[] arr = new LaserSeg[count];
            list.CopyTo(0, arr, 0, count);
            segBuffer.SetData(arr);

            // 屏幕像素下限换算系数：像素 -> 世界尺寸 = px * 距离 / _ProjFactor
            laserMat.SetBuffer("_LaserBuffer", segBuffer);
            laserMat.SetFloat("_ProjFactor", projFactor);
            laserMat.SetFloat("_HaloEndFade", haloEndFade);   // 外辉首尾渐变(核心不受影响)
            laserMat.SetFloat("_ClipFar", clipFar);
            laserMat.SetFloat("_HaloFadeFrac", haloFadeFrac);
            laserMat.SetInt("_ZTestMode", zmode);
            laserMat.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Quads, count * VERTS_PER_SEG, 1);
        }

        // 缓存当前相机投影参数（在 BuildRibbonSegs 时设置，供 UploadAndDrawRibbon 使用）
        private static bool cur_Ortho;
        private static float cur_PixelHeight = 1080f;
        private static float cur_Fov = 60f;
        private static float cur_FarClip = 1e9f;
        // 本视图里最远的光束端点距离(视图空间, 近似到相机的最大 D)。用作外辉深度渐隐的 _ClipFar。
        // 关键: 必须用"光束实际终点距离", 不能用 farClip*0.5 —— 后者在远裁剪面很大(光束未被钳制)时远超终点, 使渐隐恒不触发。
        private static float cur_MaxViewDist = 1e9f;

        // ---------- 回退路径 (sailBullet) ----------

        private static bool TryInitMaterial()
        {
            if (mat != null) return true;
            if (matFailed) return false;
            try
            {
                if (Configs.builtin == null || Configs.builtin.sailBulletMaterial == null)
                    return false;
                mat = UnityEngine.Object.Instantiate(Configs.builtin.sailBulletMaterial);
                mat.hideFlags = HideFlags.HideAndDontSave;
                // 记录原生 bullet 材质的默认 _Size(宽度), 供叠加层按角色乘倍数增粗; 材质若无此属性则跳过
                matHasSize = mat.HasProperty("_Size");
                matSizeDefault = matHasSize ? mat.GetFloat("_Size") : 1f;
                if (matHasSize && matSizeDefault <= 0f) matSizeDefault = 1f;
            }
            catch (Exception e)
            {
                matFailed = true;
                Utils.Log("[StarCannonLaser] 初始化 sailBulletMaterial 失败: " + e.Message);
            }
            return mat != null;
        }

        private static void BuildBulletPool(GameData gd, StarData viewStar)
        {
            pool.Clear();
            VectorLF3 relativePos = gd.relativePos;
            Quaternion relativeRot = gd.relativeRot;
            VectorLF3 viewTarget = UIStarmap.viewTargetStatic;
            VectorLF3 starPos = viewStar.uPosition;

            foreach (var kv in beams)
            {
                BeamProfile p = kv.Value;
                GenBeam(p, relativePos, relativeRot, viewTarget, starPos);
            }
        }

        // 现实视角额外叠加: 仅收录"与玩家当前星系相关"的光束, 用原生 bullet 通道重画(遮挡正确)。
        // 身份门控(直接比对 viewStar.index, 不用距离): 主光束(1) 任一系; 汇聚(>=2000) 发射系; 攻击(1000..1999) 目标系。
        // 按角色分桶各画一次, 让叠加层亮度对齐 ribbon: 主=满亮度(粗细已随 muzzleRadius 与 ribbon 核心同宽),
        // 汇聚=随预热渐亮(p.intensity=focusIntensity 逐帧上升)* focusIntensityScale, 攻击= subIntensityScale 压暗防糊团。
        private static void DrawOverlayBullets(GameData gd, StarData viewStar)
        {
            if (viewStar == null) return;                 // 现实视角 viewStar = gd.localStar; 为空则不叠加, 避免报错
            if (!TryInitMaterial()) return;               // 原生 bullet 材质不可用则整体跳过叠加(ribbon 仍负责显示)
            int localIdx = viewStar.index;                // 玩家当前星系身份(直接比对, 不再算距离)
            VectorLF3 relativePos = gd.relativePos;
            Quaternion relativeRot = gd.relativeRot;
            VectorLF3 viewTarget = UIStarmap.viewTargetStatic;
            VectorLF3 starPos = viewStar.uPosition;

            mainKeys.Clear(); focusKeys.Clear(); blobKeys.Clear();
            float focusBright = 0f;                       // 汇聚桶代表亮度: 取组内 p.intensity 最大(同帧各汇聚束 focusIntensity 相同)
            foreach (var kv in beams)
            {
                BeamProfile p = kv.Value;
                bool beginLocal = p.beginStarIdx >= 0 && p.beginStarIdx == localIdx;
                bool endLocal = p.endStarIdx >= 0 && p.endStarIdx == localIdx;
                if (kv.Key == 1)
                {
                    if (beginLocal || endLocal) mainKeys.Add(kv.Key);
                }
                else if (kv.Key >= 1000 && kv.Key < 2000)
                {
                    if (endLocal) blobKeys.Add(kv.Key);   // 攻击子激光: 仅目标系
                }
                else if (kv.Key >= 2000)
                {
                    if (beginLocal || endLocal)           // 汇聚: 发射系(begin=end=发射星)
                    {
                        focusKeys.Add(kv.Key);
                        if (p.intensity > focusBright) focusBright = p.intensity;
                    }
                }
            }

            // 诊断(节流): 确认三种角色是否各自进入叠加桶, 尤其问题3"汇聚看不见"是门控没命中还是纯粗细/亮度问题
            if (diagLog && Time.frameCount % 45 == 0)
                Utils.Log("[MMS overlay] localIdx=" + localIdx + " beams=" + beams.Count +
                          " main=" + mainKeys.Count + " focus=" + focusKeys.Count + " blob=" + blobKeys.Count +
                          " focusBright=" + focusBright.ToString("F3") +
                          " sizeProp=" + matHasSize + " sizeDefault=" + matSizeDefault);

            // 主光束(反思修复): 只渲染"玩家所在那一端"的近端段 L=2×该端恒星最大戴森壳半径。
            //   之前整根 begin->end 都渲染, 玩家站在发射星时目标端离相机几百万单位→精灵只有几像素→"接收端还是很细"。
            //   现只画贴近相机的那一段(近=>粗且遮挡正确), 远端细线直接不渲染; 玩家在目标系则画目标端那一段。
            //   overlayShowMain=false(用户反馈叠加主激光效果一般) => 暂时永久隐藏叠加层主激光, 主激光只由 ribbon 呈现。
            if (overlayShowMain)
            {
                pool.Clear();
                foreach (int k in mainKeys)
                {
                    BeamProfile p;
                    if (beams.TryGetValue(k, out p)) BuildMainNearPool(p, localIdx, relativePos, relativeRot, viewTarget, starPos);
                }
                if (pool.Count > 0) UploadAndDraw(DefaultColor, overlaySizeMain);
            }
            // 汇聚叠加层: 用户要求"额外渲染的汇聚光线直接不要了" => 不再用 sailBullet 叠加汇聚(改由 ribbon 置顶通道负责, 见下)
            // 攻击子激光: 整跨流动股(不分段/不冻头), 压暗避免大量重叠糊成白球
            Color bc = DefaultColor; bc *= subIntensityScale;
            DrawOverlayGroup(blobKeys, relativePos, relativeRot, viewTarget, starPos, bc, overlaySizeSub, overlayStrandsSub);
        }

        // 主光束近端裁剪: 依玩家在 begin 端还是 end 端, 生成从该端恒星算起长度 L(=begin/endShellLen) 的那一段(沿原锥度取半径), 交给 GenBeamOverlay 铺实心股
        private static void BuildMainNearPool(in BeamProfile p, int localIdx, VectorLF3 relativePos, Quaternion relativeRot,
            VectorLF3 viewTarget, VectorLF3 starPos)
        {
            VectorLF3 axis = p.end - p.begin;
            double len = axis.magnitude;
            if (len < 1e-3) return;
            VectorLF3 dir = axis / len;
            bool beginLocal = p.beginStarIdx >= 0 && p.beginStarIdx == localIdx;
            bool endLocal = p.endStarIdx >= 0 && p.endStarIdx == localIdx;
            BeamProfile q = p;
            if (beginLocal)
            {
                double d = (p.beginShellLen > 0f) ? Math.Min((double)p.beginShellLen, len) : len;
                double frac = d / len;
                q.begin = p.begin;
                q.end = p.begin + dir * d;
                q.muzzleRadius = p.muzzleRadius;                                              // 恒星处=炮口半径(粗)
                q.impactRadius = (float)(p.muzzleRadius + (p.impactRadius - p.muzzleRadius) * frac); // 裁剪末端沿原锥度过渡
            }
            else if (endLocal)
            {
                double d = (p.endShellLen > 0f) ? Math.Min((double)p.endShellLen, len) : len;
                double frac0 = 1.0 - d / len;
                q.end = p.end;
                q.begin = p.end - dir * d;
                q.muzzleRadius = (float)(p.muzzleRadius + (p.impactRadius - p.muzzleRadius) * frac0);
                q.impactRadius = p.impactRadius;                                              // 目标恒星处=落点半径
            }
            else return;
            // 主光束加粗一倍(用户要求): 把圆盘铺开半径整体放大 overlayMainRadiusMul 倍, 实心束随之变粗
            q.muzzleRadius *= overlayMainRadiusMul;
            q.impactRadius *= overlayMainRadiusMul;
            GenBeamOverlay(q, relativePos, relativeRot, viewTarget, starPos, overlayStrandsMain);
        }

        // 收集指定角色的光束生成子弹(整跨流动股), 用给定颜色(含亮度)/_Size 倍数/股数一次性绘制该组
        private static void DrawOverlayGroup(List<int> keys, VectorLF3 relativePos, Quaternion relativeRot,
            VectorLF3 viewTarget, VectorLF3 starPos, Color color, float sizeMul, int strands)
        {
            if (keys.Count == 0) return;
            pool.Clear();
            foreach (int k in keys)
            {
                BeamProfile p;
                if (beams.TryGetValue(k, out p))
                    GenBeamOverlay(p, relativePos, relativeRot, viewTarget, starPos, strands);
            }
            if (pool.Count == 0) return;
            UploadAndDraw(color, sizeMul);
        }

        // 由一条光束配方生成其固定的一批子弹
        private static void GenBeam(in BeamProfile p, VectorLF3 relativePos, Quaternion relativeRot,
            VectorLF3 viewTarget, VectorLF3 starPos)
        {
            int n = p.bulletCount;
            VectorLF3 axis = p.end - p.begin;
            double len = axis.magnitude;
            if (len < 1e-3) return;
            VectorLF3 dir = axis / len;

            // 找两个与轴垂直的稳定方向
            VectorLF3 up = new VectorLF3(0, 0, 1);
            if (Math.Abs(VectorLF3.Dot(dir, up)) > 0.98) up = new VectorLF3(0, 1, 0);
            VectorLF3 perp1 = VectorLF3.Cross(dir, up).normalized;
            VectorLF3 perp2 = VectorLF3.Cross(dir, perp1).normalized;

            float now = Time.unscaledTime;

            // 中心核：一条最亮最细的实心光柱
            AddOne(p, VectorLF3.zero, VectorLF3.zero, 0.0f, relativePos, relativeRot, viewTarget, starPos);

            for (int i = 0; i < n; i++)
            {
                // 稳定伪随机：角度 + 半径系数只取决于 seed 与 i，帧间不变 => 横截面稳定不闪
                float ang = Hash01(p.seed, i) * (float)Math.PI * 2f;
                float rr = Mathf.Sqrt(Hash01(p.seed, i + 131071)); // 均匀分布在圆盘上
                float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                VectorLF3 offDir = perp1 * ca + perp2 * sa;

                VectorLF3 oBegin = offDir * (p.muzzleRadius * rr);
                VectorLF3 oEnd = offDir * (p.impactRadius * rr);

                // 沿轴流动：head 在 [0,1) 内随时间推进 => 光柱有“能量流动”的观感
                float head = (now / Mathf.Max(0.05f, p.flowLife) + Hash01(p.seed, i + 7)) % 1f;
                VectorLF3 jitter = oBegin + (oEnd - oBegin) * head;

                AddOne(p, jitter, offDir * 0f, head, relativePos, relativeRot, viewTarget, starPos);
            }
        }

        // 追加一颗子弹（全跨度 begin->end，带横截面偏移 offB/offE 与流动相位 head）
        private static void AddOne(in BeamProfile p, VectorLF3 offB, VectorLF3 offE, float head,
            VectorLF3 relativePos, Quaternion relativeRot, VectorLF3 viewTarget, VectorLF3 starPos)
        {
            AddSpan(p, p.begin, p.end, offB, offE, head, relativePos, relativeRot, viewTarget, starPos);
        }

        // 追加一颗"任意起终点"的子弹（用于把一条光束沿长度切成多段重叠绘制, 修复每段首尾彗尾淡出）
        private static void AddSpan(in BeamProfile p, VectorLF3 ubBegin, VectorLF3 ubEnd, VectorLF3 offB, VectorLF3 offE, float head,
            VectorLF3 relativePos, Quaternion relativeRot, VectorLF3 viewTarget, VectorLF3 starPos)
        {
            SailBullet b = new SailBullet();
            b.id = pool.Count + 1;
            b.maxt = 0.3f;
            b.t = head * 0.3f;               // 与 maxt 配合，驱动 shader 的流动/明暗
            b.state = 0;                     // 0 => 永不生成真正的太阳帆（关键：不污染戴森云）
            b.uBegin = ubBegin + offB;
            b.uEnd = ubEnd + offE;
            b.uEndVel = VectorLF3.zero;
            b.lBegin = (Vector3)p.begin;

            switch (DysonSphere.renderPlace)
            {
                case ERenderPlace.Universe:
                    b.rBegin = (Vector3)Maths.QInvRotateLF(relativeRot, b.uBegin - relativePos);
                    b.rEnd = (Vector3)Maths.QInvRotateLF(relativeRot, b.uEnd - relativePos);
                    break;
                case ERenderPlace.Starmap:
                    b.rBegin = (Vector3)((b.uBegin - viewTarget) * 0.00025);
                    b.rEnd = (Vector3)((b.uEnd - viewTarget) * 0.00025);
                    break;
                case ERenderPlace.Dysonmap:
                    b.rBegin = (Vector3)((b.uBegin - starPos) * 0.00025);
                    b.rEnd = (Vector3)((b.uEnd - starPos) * 0.00025);
                    break;
            }
            pool.Add(b);
        }

        // 叠加层专用生成器: 横截面圆盘铺 count 条"整跨 begin->end"的平行光股, 每条的明暗相位 head 随时间推进 => 连续流动的能量束。
        // 关键(反思): 不再沿长度分段——分段会让每颗精灵变短 => 短精灵按固定宽度显得比光束更粗, 且 head 恒定 => 每段一颗彗星冻在同一位置=一段一段静止的集束。
        // 厚度靠: (1)半径倍数(调用方对主光束已 ×overlayMainRadiusMul) 撑开圆盘铺开半径; (2)股数 count 的密度。_Size 实测无效, 不用它。
        private static void GenBeamOverlay(in BeamProfile p, VectorLF3 relativePos, Quaternion relativeRot,
            VectorLF3 viewTarget, VectorLF3 starPos, int count)
        {
            VectorLF3 axis = p.end - p.begin;
            double len = axis.magnitude;
            if (len < 1e-3) return;
            VectorLF3 dir = axis / len;

            VectorLF3 up = new VectorLF3(0, 0, 1);
            if (Math.Abs(VectorLF3.Dot(dir, up)) > 0.98) up = new VectorLF3(0, 1, 0);
            VectorLF3 perp1 = VectorLF3.Cross(dir, up).normalized;
            VectorLF3 perp2 = VectorLF3.Cross(dir, perp1).normalized;

            int NS = Mathf.Max(1, count);
            float now = Time.unscaledTime;
            const float golden = 2.39996323f;
            for (int s = 0; s < NS; s++)
            {
                float rf, ang;
                if (s == 0) { rf = 0f; ang = 0f; }                            // 中心股, 恒在轴上(最亮实芯)
                else { rf = Mathf.Sqrt((s + 0.5f) / NS); ang = s * golden; }  // 均匀铺满圆盘(低差异, 横截面稳定不闪)
                VectorLF3 radial = perp1 * Mathf.Cos(ang) + perp2 * Mathf.Sin(ang);
                VectorLF3 offB = radial * (p.muzzleRadius * rf);
                VectorLF3 offE = radial * (p.impactRadius * rf);
                // head 随时间流动(每股相位用种子打散) => 能量沿光束流动, 不再是冻在原地的静止团
                float head = (now / Mathf.Max(0.05f, p.flowLife) + Hash01(p.seed, s)) % 1f;
                AddSpan(p, p.begin, p.end, offB, offE, head, relativePos, relativeRot, viewTarget, starPos);
            }
        }

        private static void UploadAndDraw(Color color, float sizeMul = 1f)
        {
            int count = pool.Count;
            if (count == 0) return;

            if (buffer == null || capacity < count)
            {
                int newCap = capacity == 0 ? 1024 : capacity;
                while (newCap < count) newCap *= 2;
                if (buffer != null) buffer.Release();
                buffer = new ComputeBuffer(newCap, STRIDE, ComputeBufferType.Default);
                capacity = newCap;
            }

            SailBullet[] arr = new SailBullet[count];
            pool.CopyTo(0, arr, 0, count);
            buffer.SetData(arr);

            mat.SetColor("_Color0", color);
            if (matHasSize) mat.SetFloat("_Size", matSizeDefault * sizeMul);   // 问题1/3: 按角色增粗子弹精灵宽度(材质支持时)
            Shader.SetGlobalInt("_Global_DS_RenderPlace", (int)DysonSphere.renderPlace);

            mat.SetBuffer("_BulletBuffer", buffer);
            mat.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Quads, count * VERTS_PER_BULLET, 1);
        }

        // 稳定 0..1 伪随机（Integer hash，跨帧一致）
        private static float Hash01(int seed, int i)
        {
            unchecked
            {
                uint h = (uint)(seed * 374761393 + i * 668265263);
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xffffff) / (float)0x1000000;
            }
        }
    }

    // 宿主：OnRenderObject 由每个渲染相机各调一次；LateUpdate 做生命周期清理。
    public class StarCannonLaserHost : MonoBehaviour
    {
        private void OnRenderObject()
        {
            StarCannonLaserRenderer.Render();
        }

        private void LateUpdate()
        {
            StarCannonLaserRenderer.TickLifetime();
        }
    }
}
