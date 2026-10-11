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
        public static float haloFadeFracRealDepth = 0.92f; // [球形深度壁分支专用] 外辉渐隐起点比例。以前用 0.15 是因为尾端会被 far 硬切、需要先淡出掩饰;
        //   现在尾端不再被切(压到球壁), 而 _ClipFar = 本帧最远光束终点 => 0.15 意味着主光束外辉在剩下 85% 路程里一路淡到 0,
        //   用户看到的就是"还没到目标星系光晕就提前截断"。此分支里改成只在最后 8% 微淡; 1.0 = 完全不淡。
        public static int ztestMode = 4;                 // 近景分组(壳内)深度测试: 4=LEqual(真实前后遮挡, 默认)。远景分组(壳外)固定用 8=Always 保证永不消失, 不受此值影响。
        // 汇聚置顶(segsFront, ZTest Always): 盖过戴森壳(无玻璃遮挡), 但会连星球也盖住 => 用 CPU 射线-球 手动剔除"被载入星系某星球真正挡在前方"的那段光束。只跳过绘制不写深度 => 无黑边风险。
        public static bool  focusRespectPlanets = true;  // true=置顶汇聚光束仍被星球遮挡(仅跳过被挡段); false=纯 Always 盖一切
        public static float planetOccludeScale = 1.15f;  // 星球遮挡半径系数(略放大以含大气层边缘, 1.0=按 realRadius)
        // ★[已废弃 2026-10] CPU 判壳交叉遮挡方案: 实测正确性与性能不可兼得 —— nodecps/vertsqOffset 运行时状态不可靠(进度门全灭),
        // 壳内观战每帧上万次射线×壳球无效求交, 旋转/采样密度导致遮挡闪烁, 分组路由随观战位置失真。
        // 现形态 = 还原单纯置顶: 宇宙视角主/子光束 segs 组也用 Always, 永远覆盖戴森壳, 不做任何额外壳判定。
        // 星球遮挡(focusRespectPlanets)不属此方案, 保留(轻量 ray-sphere, 玩家已认可的行为)。
        // 若将来重启壳遮挡: 先解决 prog-miss 实目(nodecps 数据源)与恒星遮挡半径(viewRadius 非 physicsRadius)再评估。
        public static bool  shellOccludeEnable = false;  // false=简单置顶现行形态; true=废弃的 CPU 判壳实验(代码路径保留)
        public static float gpuDepthReliableFrac = 0.7f; // [废弃方案遗留] 深度可靠上限 = farClipPlane × 此值
        public static float shellOccProgress = 0.5f;     // 壳块节点建造进度 ≥ 此值才视为实心可挡光束
        public static float shellOccDim = 0.3f;          // 单端/中点被挡(遮挡边界段)时的强度压暗系数, 柔化硬切边
        public static bool  starOccEnable = true;        // 恒星本体球遮挡(光束绕到星背后应消失)
        public static float starOccScale = 1.0f;         // [废弃方案配套] 恒星遮挡球半径 = viewRadius × 此值(仅 shellOccludeEnable=true 时生效)
        // ★CPU遮挡诊断(废弃方案配套): 现行形态下保持 0 不刷屏; 仅 shellOccludeEnable=true 实验时置 1/2 抓数据。
        public static int   shellOccDiag = 0;
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
        // [本轮实验临时置 true] bullet 束模式需要 [MMS bulletBeam]/[MMS bulletMat] 日志判定子弹数量与 _Width/_Multiplier 默认值, 测完可改回 false。
        public static bool diagLog = true;
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
        // ===== [2026-10 新方案] 宇宙现实视角 = 自研 ribbon + 真实深度(ZTest LEqual) + 顶点深度压回远平面内侧 =====
        // 依据(原生 bullet 通道实测): "壳在前能挡住" 与 "跨星系远端不被 farClipPlane 切断" 可以同时成立，唯一自洽解释是
        //   图元照常生成、只把裁剪深度压到远平面内侧。本方案在自研 shader 里复刻同一步(见 StarCannonLaser.shader 的 FarBandZ/_ZCompress)，
        //   从而同时拿到: 连续可调粗细 + 无两端淡出 + 遮挡正确 + 无远端切口 + 每束 1 个 draw call 的量级。
        // 风险隔离: 仅作用于宇宙现实视角。星图/戴森球编辑器走原分支(_ZCompress 下发 0 + 仍按旧 ztestMode/旧端点钳制)，表现逐帧不变。
        // 前置条件: bundle 里的 shader 必须已重打包到 _MMSVersion>=15(含 _ZCompress); 未满足时自动回退下面的 bullet 通道, 不会"看不见激光"。
        public static bool  universeRibbonRealDepth = true;   // true=宇宙视角走 ribbon 真实深度压带方案; false=走原生 bullet 通道
        // ★压带目标不能选"硬件远平面内侧一点点"! near=0.2 时深度极非线性: d=1-eps 对应的等效视深 z = n*f/(f-d*(f-n)),
        //   取 eps=1e-7 得 z≈1.7e6(以 far=1e7 估) —— 比整个载入星系(~3e5) 还要远得多 => 连后半球壳层都能误挡住激光(用户实测),
        //   而飞到特别近时真实视深小于这条壁, 压缩不再触发, 于是"一段一段回到前面"。
        // ★壁应放在"后半球略内侧"而不是"整个星系背后": 硬件比较的是量化后的 device depth, 我们要 d_激光 <= d_后壳 才不会被后壳挡住;
        //   mul<=1 时 R 严格小于后半球视深, IEEE 取整单调 => d_壁 <= d_后壳 恒成立(相等时 LEqual 也通过) => 后壳不会误挡;
        //   而前半球比壁近约 2 个壳半径(几个 ulp), 仍会正常挡住激光。mul>1 就是上一版"后壳误挡"的错误方向。
        // ===== [2026-10 第三阶段] 球形深度壁(在 shader 里做): 把平面壁换成与戴森壳同形的"球壁" =====
        // 上一版残留缺陷(用户实测): 遮挡错误随目标在屏幕上的位置反向 —— 中心处激光盖住壳, 移到显示器边缘反而被壳误挡,
        // 边界是一条"移动的锯齿弧"。定量原因: 固定 eps 压出来的是**等视深平面**, 而壳是**球面**
        // (沿离轴角 θ 的壳面视深 = shellR*cosθ), 两者交线是半顶角 acos(wall/shellR) 的圆锥 => 两侧比较结果必然相反。
        // 做法: 把球半径下发给 shader 的 BandZ(), 由它在**顶点着色器里**把球外顶点沿自己的视线折到球面, 再走同一个投影矩阵取深度
        // (图元的形状/宽度/方向仍用原始坐标算 => 屏幕位置与粗细逐字不变, 而深度与游戏自己画的壳逐位可比)。
        // ★绝不在 CPU 改几何! 沿相机射线改顶点位置虽然不改单点投影, 但 ribbon 的 dir/sideV/宽度是由两端点差重新算的,
        //   长尾多段折到球面同一片区域后 quad 退化(shader 里 dl<1e-3 直接塌缩) => 主光束未到目标就被截断 + 光晕更早硬断(已实错)。
        //   投影走同一个 UNITY_MATRIX_VP => 深度与游戏自己画的戴森壳逐位同构。
        // ★★ [v18 修正] 触发球必须与壳同径, 靠"落点回退量"决定与后壳的间距, 而不是靠把壁推到壳外:
        //   v17 把折叠球取 shellR×1.25 (比壳大) => 光束穿出壳体那段的深度被钉到"后壳表面之后", 于是被**后方**壳层误挡
        //   (实测日志直接坐实: devWall=0.999997034 > devShell=0.999995960, 壁比壳远 9 个 ulp)。
        //   也不能反过来把球缩小到 shellR×0.95: 那会留下一环 ρ∈(0.95R, R) 的视线"打得着壳、打不着折叠球" => 那圈不折 =>
        //   被 far 兜底钉到最远 => 依旧误挡, 并多出一圈"光束沉进壳里"的弧带。两种都是几何错位, 调 mul 消不掉。
        //   正确做法: 触发球 = 壳半径(逐像素对齐遮挡体, 无环带), 落点 = 该射线与壳的远交点沿射线向相机回退一段。
        //   回退量就是"激光与后壳表面的确定间距", 要按 float32 ulp 留余量: device gap ≈ near*pull/z^2, 取 8~10 ulp 稳定。
        //   near=0.2/shellR=3.72e4 下: pullFrac=0.05 仅 ~2.4 ulp, 0.2 得 ~9.5 ulp; 而壳内行星轨道半径远小于 0.8R,
        //   所以回退 0.2 个壳半径不会让激光越过任何真实星体(它仍比前半球远约 2 个壳半径 => 前方遮挡逐字不变)。
        // ★★★ [v19 修正: 壳是分层的, 不能把"一层的壳"当成一个整体球壁] 用户指出激光总是从壳心(=恒星)打出/打入,
        //   所以前半球必然能遮挡激光、后半球必然被激光盖住; 而壳最多有 10 层, 每层自己的 orbitRadius 与旋转都不同
        //   (源码 DysonSphere.cs: minOrbitRadius=physicsRadius*1.5, maxOrbitRadius=defOrbitRadius*2)。
        //   v18 用单一半径(defOrbitRadius=dysonRadius*40000) 建模, 两个方向同时漏:
        //     比它外的层 => 视线打得着壳、打不着壁 => 那圈不折 => 被 far 兜底钉到最远 => 误挡(屏幕上是一圈圈"按壳层显露"的边界);
        //     比它内的层 => 落点在该层后表面之后 => 又被最内层的后壳误挡。这就是"逐渐显露"现象的真因。
        //   v19 改成逐视线(在 shader 里按视线到星心的垂距 ρ 计算): 落点 = -b + sqrt(max(rMin,ρ)^2 - ρ^2) - pull,
        //   门控 ρ < rMax(该视线上根本没有壳就不折)。性质: 对任意层 r>=rMin 其后侧壳面都在落点之后, 前侧壳面都在落点之前 => 无环带漏洞。
        public static float ribbonSysRefMul       = 1.0f;   // 落点参考半径 = 最内层壳半径×此值。1.0 = 贴在最小的壳上(推荐); <=0 = 关闭球形壁, 退回平面壁(A/B 对比用)
        public static float ribbonSysPullFrac     = 0.2f;   // 落点回退量 = 参考半径×此值(0=贴在参考面上=v17 行为, 会被后壳误挡)
        public static float ribbonZCapOverride    = 0f;     // [诊断] 直接用此值作落点参考半径(绕过逐层扫描与 refMul), 0=不覆盖。用来做"把落点推远/推近"的对照实验
        public static bool  ribbonZDebug          = false;  // [诊断] 把最终 device depth 编到激光颜色里(红=贴近平面以至永不遮挡, 绿=落在壁上, 蓝=正常真实深度)
        // ===== [第一阶段: 平面壁] 仅 ribbonSysRefMul<=0 或 bundle 里 shader 低于 v19 时生效 =====
        public static float ribbonDepthTargetMul  = 0.95f;   // 壁位置 = 相机到载入恒星距离 + 最大壳半径×此值(<=1 推荐); <=0 = 不自动算, 用下方固定 eps
        public static float ribbonZCompress       = 1e-7f;   // 远平面兜底 eps(始终生效): 把超出硬件 far 的部分压到 far 内侧贴边。
        //   必需原因: 相机飞离载入星系足够远(整个星系超出 far)时, 连近端起点自己的视深都 > far => 不兜底则近端也被切断。
        //   设 0 = 完全不压深度(仅用于对比"远端被 far 切断"的旧现象)。
        public static float ribbonFarEndScale       = 0f;     // 端点几何安全阀: 0=完全不钳制几何(推荐, 全靠 shader 压深度); >0=把超出 far*此值的端点落到远球面(宽度/精度异常时再开)
        // ===== [2026-10 模式] 宇宙现实视角 = 纯原生 sailBullet 光束束(本视角不再用自研 ribbon) =====
        // 走游戏自己的管线/材质/shader => 戴森壳/行星/星光的深度遮挡天然正确(这正是 ribbon 顶不出去的壳遮挡)。
        // 已知特性: 子弹精灵粗细靠股数密度 + 圆盘铺开倍数(_Size 无效——该 shader 根本没有此属性, 真正的宽度属性是 _Width, 见下方 bulletWidth*)。
        // 主要优点: 走游戏自身 shader 的深度/渲染阶段 => 与戴森壳、行星的前后关系不再靠我们手算(上一轮 ribbon 置顶方案的痛点)。
        public static bool  bulletBeamInUniverse = true;   // true=宇宙视角激光全部由 bullet 束渲染(星图/编辑器仍走 ribbon); false=回到 ribbon+叠加层旧形态
        public static int   bulletStrandsMain   = 90;      // 主激光整跨股数
        public static int   bulletStrandsFocus  = 6;       // 汇聚每股股数
        public static int   bulletStrandsSub    = 5;       // 攻击子激光股数
        public static float bulletMainWidthMul  = 1.6f;    // 主激光圆盘铺开倍数(视觉粗细)
        public static float bulletEndBundleFrac = 0.10f;   // 起/终点短束路径长占整束比例(修复整跨子弹首尾淡出导致的"两端太浅")
        public static int   bulletEndBundleStrands = 24;   // 主激光每端短束股数
        public static int   bulletEndBundleStrandsSide = 0; // 汇聚/攻击的端束股数: 0=不开(用户实测它们会变成"悬停的粗短小线段")。这两类束本就短(星系尺度), 首尾淡出比例小, 平行整束股已经够连续
        // ===== 端头补光(修"靠恒星一侧太浅"; 注意不要反过来造成"巨大型静止光斑") =====
        public static float bulletEndBundleCeiling = 200000f; // 端头补光窗口的硬上限(世界单位)。恒星系尺度 ~1.5e5, 超过就是没意义的巨型精灵
        public static int   bulletEndSegs       = 4;     // 主激光每端窗口内再切几颗"更短"的重叠子弹(精灵越短 => 自身首尾淡出区越短 => 端头越实)
        public static float bulletEndOverlap    = 0.3f;  // 相邻短段重叠比例(0=有接缝, 0.3=上段亮头盖住下段淡尾)
        public static int   bulletEndSegsSide   = 1;     // 汇聚/攻击的端束切段数(用户实测它们会变成"很多悬停的粗短小线段"; 它们本就短, 不切段反而干净)
        public static float bulletEndHead       = -1f;   // -1 = 端头也流动(默认); 0..1 = 锁定相位(旧实测 0.5 附近最亮, 但会变静态堆)
        public static float bulletEndPeriodMul  = 1f;    // 端头流动周期 = 整束周期 × 此值。切勿按段长缩放周期: 短段会跌到帧时间以下 => 混叠成静止
        public static float bulletLocalSideBoost = 2f;   // 玩家所在那一端的端束股数倍数(近端比远端多一倍)
        public static float bulletEndRadiusMul  = 0.55f;   // 短束股圆盘半径收敛倍数(聚成实心端头)
        // [实测修正] 用户反馈: 跨星系整束在原生通道里远端并没有被 farClipPlane 切断。查证: 游戏侧只有一个 bullet 渲染入口
        //   (DysonSwarm.DrawPost 里只 SetBuffer("_BulletBuffer") + SetPass, 逐帧不写任何其它参数), 所以"不切断"必然来自 shader 内部
        //   —— 很可能就是它自己的 _ZMin/_ZMax 把深度重映射到小波段(见下方 bulletZMaxAbs 实验)。因此本钳制开关默认关闭。
        // 0 = 不钳制(直接用真实坐标); >0 = 把超出 farClipPlane×此值的端点沿视线拉回远球面(旧 ribbon 做法, 会使那段深度不再真实)。
        public static float bulletFarClampFrac  = 0f;
        // 流动速度: head = now/(p.flowLife × 此值)。>1 变慢(能量脉冲拉长、不闪烁), <1 变快。
        // 之所以要这个旋钮: flowLife=0.28 意味着每 0.28s 扫完整束, 对跨星系长束而言偏快、看起来像呼吸;
        // 且若逐帧相位增量恰好等于股间距的整数分之一, 图案会看起来静止(车轮效应), 调这个值即可破掉。
        public static float bulletFlowScale     = 3f;
        // ★粗细并非完全不可改(2026-10 核实资源得出结论): 原生 bullet 材质是 solar-sail-bullet.mat, 其 shader
        //   "VF Shaders/Dyson Sphere/Sail Bullet Inst" 的 Properties 只有 _Multiplier/_MainTex/_MainTex2/_Width(0.4,
        //   材质里改成 2.0)/_UAnim/_ZMin/_ZMax/_Color0..3 —— 里面根本没有 _Size!
        //   之前 mat.SetFloat("_Size", x) "实测无效" 不是 shader 不听话, 而是写到了不存在的属性上(材质里那个 _Size:80
        //   是 standard shader 遗留字段)。故这里按真实属性名做倍率控制: 默认 1(=游戏原粗细, 只靠股数增粗),
        //   想验证是否真管粗细: 把 bulletWidthAbs 设成 20/50 试一次即可判定(>0 时优先于 Mul)。
        public static float bulletWidthMul      = 1f;      // _Width = 材质默认 × 此值
        public static float bulletWidthAbs      = 0f;      // >0 = 直接写此绝对值到 _Width(一次判定用)
        public static float bulletMultMul       = 1f;      // _Multiplier = 材质默认 × 此值(疑似整体倍增亮度/尺寸)
        // 宽度是否按角色分档: 把调用方的 sizeMul(主 4.0 / 汇聚 2.5 / 子激光 1.6)一并乘到 _Width 上。
        // 默认 true = 主激光开箱就比游戏子弹粗 4 倍, 一次测试就能看出 _Width 到底管不管用; 效果不对改回 false。
        public static bool  bulletWidthRoleScale = true;
        // 候选宽度机制 #2: 游戏给太阳帆 sprite 每帧算并写入 _DistScalePoint = sizeInMat × Screen.height × 0.4 / tan(fov/2)
        //   (DysonSwarm.DrawPost L1752-1758), 即"屏幕恒定尺寸"换算因子; 但它从未对 bulletMaterial 写过这个值, 所以子弹一直用材质里的 60000。
        //   若 _Width 调不动粗细, 下一个要试的就是它(调大 = 变粗)。
        public static float bulletDistScaleMul  = 1f;      // _DistScalePoint = 材质默认 × 此值
        public static float bulletDistScaleAbs  = 0f;      // >0 = 直接写绝对值(优先于 Mul)
        // 候选"远端不被切断"的真因: 本 shader 家族用 _ZMin/_ZMax 把深度重映射到一个小波段(sibling 的 Local Laser Continuous
        //   就是 _ZMinBeam=-0.3 / _ZMax=0 配合 ZTest Off 控制绘制序)。子弹材质里是 _ZMin=0 / _ZMax=6。
        //   实验: 把 bulletZMaxAbs 改成 100000(接近 farClip)——若远端开始出现切口/不再延伸到屏外, 就证明 _ZMax 就是那个"特殊放行"开关。
        public static float bulletZMinAbs       = -1f;     // >=0 才覆写 _ZMin; -1 = 不动(保持材质默认)
        public static float bulletZMaxAbs       = -1f;     // >=0 才覆写 _ZMax; -1 = 不动(保持材质默认 6)
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
        private static bool matHasSize;         // 原生 bullet 材质是否有 _Size 属性(实测该 shader 无此属性, 保留仅为兼容)
        private static float matSizeDefault = 1f;
        private static bool bulletFallbackLogged;   // 原生材质不可用而回退 ribbon 的提醒只打一次
        // 原生 bullet shader 真正的粗细/倍增属性(solar-sail-bullet: _Width=2.0, _Multiplier=3.0)
        private static bool matHasWidth;
        private static float matWidthDefault = 1f;
        private static bool matHasMult;
        private static float matMultDefault = 1f;
        // 另外三个候选旋钮的真实存在性与默认值(一次日志定论, 不再猜)
        private static bool matHasDistScale;
        private static float matDistScaleDefault = 60000f;
        private static bool matHasZMin;
        private static float matZMinDefault = 0f;
        private static bool matHasZMax;
        private static float matZMaxDefault = 1f;

        // 主路径(shader)
        private static readonly List<LaserSeg> segs = new List<LaserSeg>(256);
        // 远景分组(壳外/跨星系): 与近景分开渲染。近景用真实深度 + ZTest LEqual => 与戴森壳正确前后遮挡; 远景钳制到相机远球 + ZTest Always => 永不消失(壳外本无遮挡物)。
        private static readonly List<LaserSeg> segsFar = new List<LaserSeg>(256);
        // 置顶分组(仅汇聚 ribbon): ZTest Always => 覆盖所有戴森壳, 维持"汇聚无玻璃遮挡"特性; 主光束 ribbon 仍走 segs(LEqual)被壳遮挡。
        private static readonly List<LaserSeg> segsFront = new List<LaserSeg>(256);
        // ★持久 scratch 上传数组(避免每帧 new 大数组进 LOH 导致 GC 逐段累积、fps 随时间坠到个位):
        // 只在容量不足时扩容; 用 ComputeBuffer.SetData(arr,0,count) 长度重载只拷前 count 个。
        private static LaserSeg[] segScratch = new LaserSeg[256];
        private static SailBullet[] bulletScratch = new SailBullet[1024];
        // 星球遮挡体(视图空间, xyz=球心 w=半径): 每帧由 BuildRibbonSegs 用载入星系行星重建, 供置顶汇聚段做 CPU 遮挡剔除
        private static readonly List<Vector4> occluders = new List<Vector4>(32);
        private static Vector3 occCam;
        // ★戴森壳遮挡层数据(按层 id 1..10, 每帧重建): occLayer=通过游戏渲染条件(有壳+游戏内可见)的层;
        // occCenter=星心在该层视图系的投影(视图坐标系), occRot=层自转(壳局部→视图), occMaxR=该层最大壳球半径(射线粗判用)。
        private static readonly DysonSphereLayer[] occLayer = new DysonSphereLayer[11];
        private static readonly Vector3[] occCenter = new Vector3[11];
        private static readonly Quaternion[] occRot = new Quaternion[11];
        private static readonly float[] occMaxR = new float[11];
        private static bool occShellAny;          // 本帧至少有一层壳通过游戏渲染条件(参与 CPU 遮挡)
        private static bool occTopLogged;         // 部署自检只打一次
        private static readonly bool[] occLayerLogged = new bool[11];   // 逐层坐标实目诊断只打一次(换存档/重建后重置)
        // 诊断计数(每帧在 BuildRibbonSegs 开头清零): 各判定环节是否命中过
        private static int dbgOccCalls, dbgOccPlanet, dbgOccStar, dbgOccSphereHit, dbgOccPolyHit, dbgOccProgHit;
        private static int dbgRibbonSeen;    // CPU 判定分支实际进入次数(证明置顶模式代码路径生效)
        private static bool dbgBranchLogged;
        private static int dbgSegDrop, dbgSegDim;   // 本帧被 CPU 剔除/压暗的段数
        private static int dbgProgSamp;             // 本帧进度门实目日志条数(防刷, 每帧最多5条)
        // 逐帧路由参数(BuildRibbonSegs 设置, RouteQuad 使用)
        private static Vector3 s_camPos;   // 相机(相对帧)位置
        private static float s_nearR;      // 近景球半径 = 载入星系最大戴森壳半径(dysonRadius*40000) * 冗余; 壳心 = 相对帧原点 = 本星系恒星(发射或接收端被载入者)
        private static float s_nearR2;     // (旧判据, 仅保留供 deploy 日志参考) 之前按"距原点>nearR"分远/近: 会把玩家在自己壳内/中距观战的主束全赶进 CPU+Always => segs 恒 0, 旋转闪烁+远看无遮挡+无 early-Z 卡
        private static float s_reliableR;  // 相机深度可靠上限 = farClip * gpuDepthReliableFrac
        private static float s_fakeR;      // 伪钳分界 = clampR * 0.998: 超过即含被钳制(位置失真)的点
        private static float s_reliableFakeMin; // = min(s_reliableR, s_fakeR): 段两端相机距离超过它 → 深度不可靠, 走 Always+CPU
        private static float dbgMinCamD;   // 本帧非置顶段中点的相机距离最小值(路由诊断)
        private static float s_clampR;     // 远景钳制半径 = farClipPlane * clampFarScale
        private static bool s_universe;    // 是否宇宙(主)视图; 星图/编辑器保持旧的"钳制 + LEqual"单分组
        private static float nearRadiusRedundancy = 1.2f; // 近景球相对最大壳半径的冗余系数(用户建议 ~1.2)
        private static ComputeBuffer segBuffer;
        private static int segCapacity;
        private static Material laserMat;
        private static bool laserMatFailed;
        // ===== 真实深度+压带新分支状态 =====
        private static bool s_realDepth;          // 本帧宇宙现实视角是否走"真实深度+压带"(星图/编辑器恒 false)
        private static float s_drawR = float.MaxValue;  // 本帧端点/四边形钳制半径: 新分支不钳制(MaxValue=空操作), 旧分支=far*clampFarScale
        private static bool laserHasZCompress;    // bundle 里的 shader 是否已含 _ZCompress(>= v15)
        private static bool laserHasSysFold;      // bundle 里的 shader 是否已含 _SysFoldR(>= v16)
        private static bool laserHasVertexFold;   // bundle 里的 shader 是否已含 _ZDebug(>= v17, 顶点内折叠取深度; v16 的解析式上限不可靠)
        private static bool laserHasSysPull;      // bundle 里的 shader 是否已含 _SysFoldPull(>= v18, 落点回退)
        private static bool laserHasSysOuter;     // bundle 里的 shader 是否已含 _SysFoldOuterR(>= v19, 逐视线落点+多层壳门控; 球壁可用性的真正门控)
        private static float laserShaderVer;      // 实际加载到的 _MMSVersion(日志/回退判定用)
        private static bool realDepthFallbackLogged;
        // 自适应压带壁的仪表读数(仅供周期日志定位, 不参与渲染)
        private static float s_zCompress;         // 本帧下发给 shader 的 eps(球壁模式 = 远平面兜底; 平面壁模式 = 按壁位置反解)
        private static float s_zTargetR;          // 壁的目标视深(相机前方多远); 0 = 本帧未用平面壁
        private static float s_dbgNear, s_dbgFar, s_dbgCamStarD, s_dbgShellR;
        // 球形深度壁: 下发给 shader 的球半径(0 = 关闭), 以及本帧是否真的启用了球壁
        private static float s_sysFoldR;
        private static float s_sysFoldPull;       // 本帧下发给 shader 的落点回退量(视图单位)
        private static float s_sysFoldOuter;      // 本帧下发的门控半径 = 最外层壳半径(v19; 0 = 不门控)
        // 本颗恒星"真正会被游戏画出来的壳层"半径包络(逐层扫描, 见 ScanShellRadii)。找不到任何层时退到 dysonRadius*40000。
        private static float s_shellRMin, s_shellRMax;
        private static int   s_shellLayerN;
        private static bool  s_sphereWall;

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

            // [新方案 优先] 宇宙现实视角: 自研 ribbon + 真实深度(ZTest LEqual) + shader 把超远顶点的深度压回远平面内侧。
            // 目标 = 拿原生 bullet 通道实测到的同样两个行为(壳能挡住 + 远端无切口), 但保留 ribbon 的连续/粗细/无端头淡出。
            // 仅当 shader 已重打包到含 _ZCompress(v15+) 才启用; 否则自动落到下面已有的 bullet 通道, 避免出现"没有激光"。
            // 星图(Starmap)/戴森球编辑器(Dysonmap) 不进本分支, 保持原有完美呈现。
            if (DysonSphere.renderPlace == ERenderPlace.Universe && universeRibbonRealDepth)
            {
                if (TryInitLaserMaterial(cur) && laserHasZCompress)
                {
                    BuildRibbonSegs(gd, viewStar, cur);
                    UploadAndDrawRibbon();
                    return;
                }
                if (!realDepthFallbackLogged && (laserMatFailed || (laserMat != null && !laserHasZCompress)))
                {
                    // 只在"确实加载到了旧 shader"或"bundle 里没有激光资产"时报告一次;
                    // bundle 尚未加载(刚启动到主菜单)不属于异常, 不刷屏, 下一帧会自动切到新分支。
                    realDepthFallbackLogged = true;
                    Utils.Log("[MMS realDepth] 自研 shader 缺 _ZCompress(实际加载到 _MMSVersion=" + laserShaderVer.ToString("F0") +
                              "), 本视角暂时回退原生 bullet 通道; 请把新版 StarCannonLaser.shader 重打包进 mmstabicon bundle 后重启游戏。");
                }
            }

            // [bullet 束模式] 宇宙现实视角: 全部光束用原生 sailBullet 管线渲染(壳遮挡天然正确), 自研 ribbon 完全不参与本视角。
            // 不依赖 mmstabicon bundle => 现实视角激光不再受自定义 shader 缺失/旧版影响。
            // 原生材质拿不到时不能"静默无激光": 回退 ribbon 通道, 原因只提一次。
            if (DysonSphere.renderPlace == ERenderPlace.Universe && bulletBeamInUniverse && TryInitMaterial())
            {
                RenderUniverseBullets(gd, viewStar);
                return;
            }
            if (DysonSphere.renderPlace == ERenderPlace.Universe && bulletBeamInUniverse && !bulletFallbackLogged)
            {
                bulletFallbackLogged = true;
                Utils.Log("[MMS bulletBeam] 原生 sailBullet 材质不可用(Configs.builtin.sailBulletMaterial 为空?), 本视角临时回退 ribbon");
            }

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
                // 新分支能力探测: bundle 里的 shader 是否已含 _ZCompress(v15+)。缺它则宇宙视角自动回退 bullet 通道(而不是“激光消失”)。
                laserHasZCompress = m != null && m.HasProperty("_ZCompress");
                laserHasSysFold   = m != null && m.HasProperty("_SysFoldR");   // v16+ 才有
                laserHasVertexFold = m != null && m.HasProperty("_ZDebug");      // v17+ 才有: 顶点内折叠取深度
                laserHasSysPull    = m != null && m.HasProperty("_SysFoldPull");  // v18+ 才有: 落点回退
                laserHasSysOuter   = m != null && m.HasProperty("_SysFoldOuterR"); // v19+ 才有: 逐视线落点 + 多层壳门控, 球壁真正可用的前提
                laserShaderVer = ver;
                if (diagLog)
                Utils.Log("[StarCannonLaser] " + how + ": " + n + " | shader='" + sname + "' passCount=" + pc +
                          " _MMSVersion=" + ver + (hasVer ? "" : "(无此属性=旧shader!)") +
                          " _ZCompress=" + laserHasZCompress + " _SysFoldR=" + laserHasSysFold + " _ZDebug=" + laserHasVertexFold +
                          " _SysFoldPull=" + laserHasSysPull + " _SysFoldOuterR=" + laserHasSysOuter +
                          (_SysFoldRFmt()) +
                          " _PulseParams=" + pulse + " _FlowRand=" + flow + " _FlareTex=" + flare + " _LaserTex=" + laserTex +
                          "  (判定只看 _SysFoldOuterR=True(v19=逐视线落点: 最内层参考+最外层门控, 多层壳唯一可靠版) / 只有 _SysFoldPull=True 而无 _SysFoldOuterR=v18(单一壳半径建模, 内/外层仍会误挡) / _ZDebug=True(v17 折叠球比壳大, 被后壳误挡) / _SysFoldR=True(v16 解析式不可靠) / _ZCompress=True(v15平面壁) 且 passCount=1; _MMSVersion 是从 .mat 序列化值读出的旧数字, 不代表 shader 新旧, 可忽略)");
            }
            catch { }
        }
        // 诊断文案: bundle 里的 shader 不够新时自动降级, 并提醒重打包
        private static string _SysFoldRFmt()
        {
            if (!laserHasSysOuter)
            {
                if (laserHasSysPull)
                    return "  !!shader=v18(把一层的壳当单一半径球壁 => 比它外的层打得着壳打不着壁、比它内的层又落在落点之前, 仍会按层误挡), 自动退回平面壁: 需重打包到 v19!!";
                if (laserHasVertexFold)
                    return "  !!shader=v17(折叠球比壳大 => 激光被自己星系的后方壳层误挡), 自动退回平面壁: 需重打包到 v19!!";
                if (laserHasSysFold)
                    return "  !!shader=v16(解析式上限, 精度不够 => 激光不遮挡), 自动退回平面壁: 需重打包到 v19!!";
                return "  !!shader=v15(无球壁), 宇宙视角自动退回平面壁模式: 需将 StarCannonLaser.shader 重新打包进 bundle!!";
            }
            return "";
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

            // ★重建视图空间遮挡体(仅宇宙视角有意义): 行星(既有) + 恒星本体 + 戴森壳块层组。
            // 置顶模式(shellOccludeEnable)下主/子/汇聚段全走 CPU 判定; 否则仅保留汇聚置顶段的星球判定(旧行为)。
            occluders.Clear();
            for (int oi = 0; oi <= 10; oi++) { occLayer[oi] = null; occMaxR[oi] = 0f; }   // 每帧全量重置, 防上一帧脏层残留
            occCam = cur.transform.position;
            dbgOccCalls = dbgOccPlanet = dbgOccStar = dbgOccSphereHit = dbgOccPolyHit = dbgOccProgHit = 0;
            dbgProgSamp = 0;
            dbgRibbonSeen = dbgSegDrop = dbgSegDim = 0;   // ★诊断计数每帧清零(之前 seen 累加导致日志误导)
            occShellAny = false;
            // ★真实深度压带分支(s_realDepth)下一概不需要 CPU 遮挡体: 遮挡全交给硬件深度。这里直接跳过整块重建,
            //   否则每帧白白构建遮挡体 + 上万次射线求交(历史上就是这部分把帧率拉到 10fps)。
            if (DysonSphere.renderPlace == ERenderPlace.Universe && viewStar != null && !(universeRibbonRealDepth && laserHasZCompress)
                && (focusRespectPlanets || shellOccludeEnable))
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
                if (shellOccludeEnable && starOccEnable)
                {
                    // ★遮挡球用 viewRadius(视觉光球 radius×800), 不是 physicsRadius(radius×1200):
                    // 物理半径比视觉大 50%, 用它会把"恒星外一圈本不该挡的星空"判成星体背后, 近看激光在很多角度被永久挡死。
                    double rr = viewStar.viewRadius * (double)starOccScale;
                    if (rr > 1.0)
                    {
                        Vector3 c = (Vector3)Maths.QInvRotateLF(relativeRot, viewStar.uPosition - relativePos);
                        occluders.Add(new Vector4(c.x, c.y, c.z, (float)rr));
                    }
                }
                if (shellOccludeEnable) BuildShellOccluders(gd, viewStar, relativePos, relativeRot);
            }

            // 分开渲染(用户方案): 以"载入星系最大戴森壳半径"为界。壳内(近景)用真实深度 + ZTest LEqual => 与壳正确前后遮挡;
            // 壳外(远景, 多为跨星系段)钳制到相机远球 + ZTest Always => 永不消失(壳外本无遮挡物)。
            // 相对帧原点 = 载入星系的恒星(玩家所在; 无论它是发射端还是接收端), 壳心即原点, 故近景判据用 |p| <= nearR。
            Vector3 camPos = cur.transform.position;   // Unity 世界(=相对渲染帧)相机位置, 与 vb/ve 同一坐标系
            bool universe = DysonSphere.renderPlace == ERenderPlace.Universe;
            s_camPos = camPos;
            s_universe = universe;
            s_clampR = cur.farClipPlane * clampFarScale;                    // 远景钳制半径(< far, 宽度不炸)
            // [新分支] 端点不再被"伪钳": 历史上把两端一起钳到 0.9*far 会连近端真实深度一并毁掉 => 全遮挡真凶。
            // 新分支保持几何真实(不钳), 超远部分的"不被远平面切断"交给 shader 的 FarBandZ 压深度带完成。
            // s_drawR = MaxValue 时 CamClamp 自动变为空操作(m > MaxValue 永不成立); ribbonFarEndScale>0 为安全阀(宽度/精度异常时才开)。
            s_realDepth = universe && universeRibbonRealDepth && laserHasZCompress;
            s_drawR = s_realDepth
                ? (ribbonFarEndScale > 0f ? cur.farClipPlane * ribbonFarEndScale : float.MaxValue)
                : s_clampR;
            // 近景球半径 = 该恒星最大戴森壳半径(dysonRadius*40000, 与 vb/ve 同为 uPosition 单位) * 冗余。非宇宙视图: MaxValue => 全部走"钳制+LEqual"单分组(星图/编辑器保持旧的正常表现)。
            s_nearR = universe ? Mathf.Max(viewStar.dysonRadius * 40000f * nearRadiusRedundancy, 1f) : float.MaxValue;
            s_nearR2 = s_nearR * s_nearR;
            // ★自适应压带壁: 只把"比载入星系背面还远"的顶点压到一个固定视深, 而不是压到硬件远平面内侧(那会跑到整个星系之后 => 后壳误挡)。
            // ===== 球形深度壁(shader)与平面壁(上一版, A/B 回退用)的参数 =====
            s_dbgNear = cur.nearClipPlane; s_dbgFar = cur.farClipPlane;
            s_dbgCamStarD = camPos.magnitude;                       // 帧原点 = 载入恒星, 故相机到位移的长度就是到该恒星的距离
            s_dbgShellR = universe ? s_nearR / Mathf.Max(1f, nearRadiusRedundancy) : 0f;   // 还原出默认层半径(dysonRadius*40000, 仅作为找不到壳层时的基线)
            // ★v19: 逐层扫描真实的壳半径包络。一个恒星最多 10 层、各层 orbitRadius 独立且各自旋转,
            //   所以落点参考半径用最内层(保证激光在**每一层**的后半球面之前), 门控半径用最外层(那里根本没有壳就不用折)。
            float rMin = 0f, rMax = 0f; int layerN = 0;   // (显式初始化: && 短路时 out 参数不会被赋值)
            bool scanned = universe && ScanShellRadii(gd, viewStar, out rMin, out rMax, out layerN);
            if (scanned) { s_shellRMin = rMin; s_shellRMax = rMax; s_shellLayerN = layerN; }
            else { s_shellRMin = s_dbgShellR; s_shellRMax = s_dbgShellR; s_shellLayerN = 0; }   // 无壳/非宇宙视图: 退到旧基线(两者相等 = v18 行为)
            // 球壁只在"宇宙真实深度分支 + bundle 里 shader 已是 v19+(逐视线落点 + 多层壳门控)"时启用;
            // v15 不认 _SysFoldR, v16 用解析式上限(误差与信号同量级), v17 把折叠球放在壳外(被后壳误挡),
            // v18 把一层的壳当单一半径球壁(内/外层仍误挡) —— 四者都自动退回平面壁。
            s_sphereWall = s_realDepth && laserHasSysOuter && ribbonSysRefMul > 0f && s_shellRMax > 0f;
            // 落点参考半径 = 最内层壳半径×refMul(默认 1.0)。shader 里按视线垂距 ρ 取 rt = max(此值, ρ), 所以不存在"打空"环带。
            s_sysFoldR = s_sphereWall
                ? (ribbonZCapOverride > 0f ? Mathf.Max(1f, ribbonZCapOverride)
                                           : Mathf.Max(1f, s_shellRMin * Mathf.Max(0.01f, ribbonSysRefMul)))
                : 0f;
            // 门控半径 = 最外层壳半径: 视线到星心的垂距超过它 ⇒ 这条视线上没有任何壳 ⇒ 不折(保留真实深度, 交给 far 兜底)。
            s_sysFoldOuter = s_sphereWall ? Mathf.Max(s_sysFoldR, s_shellRMax) : 0f;
            // 落点回退量: 沿射线往相机方向退这么多, 确保激光严格在后半球面**之前**。按 float32 ulp 留余量。
            s_sysFoldPull = s_sphereWall
                ? Mathf.Min(s_sysFoldR * Mathf.Clamp(ribbonSysPullFrac, 0f, 0.9f), Mathf.Max(1f, s_sysFoldR - 1f))
                : 0f;
            if (s_sphereWall)
            {
                // eps 只做"超出硬件 far"的兜底(逐顶点上限由 shader 的球壁自己算)
                s_zTargetR = 0f;
                s_zCompress = ribbonZCompress;
            }
            else if (s_realDepth && ribbonDepthTargetMul > 0f)
            {
                // 平面壁(第一阶段形态): 壁放在后半球略内侧, eps 由 CPU 按投影公式反解。中心/边缘会反向, 仅用于对比。
                s_zTargetR = s_dbgCamStarD + s_dbgShellR * Mathf.Max(0.05f, ribbonDepthTargetMul);
                s_zCompress = EpsForViewDepth(s_zTargetR, s_dbgNear, s_dbgFar);
            }
            else
            {
                s_zTargetR = 0f;
                s_zCompress = ribbonZCompress;
            }
            // ★新路由阈值: 段是否交给 GPU 真实深度(LEqual)由"相机深度可靠性"决定, 而不是距星系原点距离(旧判据导致 segs 恒 0)。
            s_reliableR = cur.farClipPlane * Mathf.Clamp(gpuDepthReliableFrac, 0.1f, 0.99f);
            s_fakeR = s_clampR * 0.998f;
            s_reliableFakeMin = Mathf.Min(s_reliableR, s_fakeR);
            dbgMinCamD = float.MaxValue;

            foreach (var kv in beams)
            {
                BeamProfile p = kv.Value;
                Vector3 vb, ve;
                if (!TryViewPoints(p.begin, p.end, relativePos, relativeRot, viewTarget, starPos, out vb, out ve))
                    continue;
                // 稳定基线方案(按用户要求回退): 整条光束端点【以相机为球心】钳制到 s_clampR(方向不变, 只缩短) => 光束在所有视角都可见、不消失、无玻璃;
                // 配合单分组 + ZTest LEqual(4) => 遮挡偏保守(通常被戴森壳挡住)。这是"只有遮挡错误、其余都稳定"的那个版本。
                vb = CamClamp(vb, camPos, s_drawR);
                ve = CamClamp(ve, camPos, s_drawR);
                // 记录本视图端点的【眼深度】(到相机距离, 与 shader 的 D 一致)，作为外辉深度渐隐 _ClipFar：终点 D≈此值 -> dfade→0
                float dNear = (vb - camPos).magnitude, dFarE = (ve - camPos).magnitude;
                float endD = Mathf.Min(Mathf.Max(dNear, dFarE), s_drawR);   // 实际可绘制最远距离(旧分支被钳到 s_clampR; 新分支不钳)
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
                // 汇聚 ribbon 置顶(旧形态): 仅【现实视角(Universe)】把 isFocus 段进 segsFront(ZTest Always, 覆盖所有戴森壳);
                // 星图/戴森壳地图不改(仍进 segs 走常规 ztestMode), 保持原有表现。
                // [新分支] 真实深度下不再需要置顶分组: 所有光束一律进 segs, 靠 LEqual + 压深度带拿真实遮挡(壳/星球/恒星全部自动正确)。
                List<LaserSeg> grp = (isFocus && universe && !s_realDepth) ? segsFront : segs;
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

            // ★诊断汇总(挪到光束循环末尾): 之前打在循环前 calls 恒为刚清零的伪值。此处才是本帧真实判定链计数。
            if (shellOccDiag > 0 && universe)
            {
                if (!occTopLogged)
                {
                    occTopLogged = true;
                    Utils.Log("[MMS shellOcc] deploy-ok shellAny=" + occShellAny + " camDist=" + occCam.magnitude.ToString("F0") +
                              " localStar=" + (gd.localStar != null ? gd.localStar.index : -1) +
                              " spheres=" + (gd.dysonSpheres != null ? gd.dysonSpheres.Length : -1) +
                              " mask=" + (gd.localStar != null && gd.dysonSpheres != null && gd.localStar.index >= 0 && gd.localStar.index < gd.dysonSpheres.Length && gd.dysonSpheres[gd.localStar.index] != null ? gd.dysonSpheres[gd.localStar.index].inGameRenderMaskL : -999) +
                              " starDysonR=" + viewStar.dysonRadius + " nearR=" + s_nearR.ToString("F0") + " clampR=" + s_clampR.ToString("F0") +
                              " viewR=" + viewStar.viewRadius.ToString("F0") + " physR=" + viewStar.physicsRadius.ToString("F0") +
                              " relR=" + s_reliableR.ToString("F0") + " fakeR=" + s_fakeR.ToString("F0") +
                              " beams=" + beams.Count + " topMode=" + (s_universe && shellOccludeEnable));
                }
                if (Time.frameCount % 90 == 0)
                    Utils.Log("[MMS shellOcc] any=" + occShellAny + " occluders=" + occluders.Count +
                              " seen=" + dbgRibbonSeen + " calls=" + dbgOccCalls + " planet=" + dbgOccPlanet + " shell=" + dbgOccStar +
                              " sphereHit=" + dbgOccSphereHit + " poly=" + dbgOccPolyHit + " prog=" + dbgOccProgHit +
                              " drop=" + dbgSegDrop + " dim=" + dbgSegDim +
                              " segs=" + segs.Count + " far=" + segsFar.Count + " front=" + segsFront.Count +
                              " minD=" + (dbgMinCamD >= float.MaxValue ? -1f : dbgMinCamD).ToString("F0") + " beams=" + beams.Count);
            }
        }

        // 把一条 vb->ve 的光束细分成 n 段 ribbon quad；capStart/capEnd 复用为“全局沿长”(0..1)，供 shader 连续流动
        private static void AddRibbon(Vector3 vb, Vector3 ve, float inten, float hw, Vector4 c, float minPx, float kind, int n, List<LaserSeg> group)
        {
            if (n < 1) n = 1;
            bool front = group == segsFront;
            for (int i = 0; i < n; i++)
            {
                float t0 = (float)i / n;
                float t1 = (float)(i + 1) / n;
                Vector3 q0 = Vector3.Lerp(vb, ve, t0);
                Vector3 q1 = Vector3.Lerp(vb, ve, t1);
                Vector3 mid = (q0 + q1) * 0.5f;
                float segInten = inten;                 // ★每段独立强度: 修复"一段被压暗后同束后续段被连带乘暗"(原 inten 就地累乘 bug)
                List<LaserSeg> tgt = group;

                float midD = (mid - occCam).magnitude;
                if (midD < dbgMinCamD) dbgMinCamD = midD;
                // 用段两端的最大相机距离作判据: 只要有一个端点落入塌缩区/伪钳球, 整段深度都不可信
                float dSeg = Mathf.Max(midD, Mathf.Max((q0 - occCam).magnitude, (q1 - occCam).magnitude));
                if (front)
                {
                    // 汇聚置顶段: 维持"盖过戴森壳但尊重行星"旧特性, 仅星球中点剔除
                    if (focusRespectPlanets && IsOccludedByPlanet(mid)) continue;
                }
                else if (s_universe && shellOccludeEnable && dSeg > s_reliableFakeMin)
                {
                    // ★深度不可靠段(进入 far 塌缩区, 或含被钳制的伪深度点): segsFar(Always) + CPU 判壳。
                    dbgRibbonSeen++;
                    if (shellOccDiag > 1 && !dbgBranchLogged)
                    {
                        dbgBranchLogged = true;
                        Utils.Log("[MMS shellOcc] far-branch first: camD=" + dSeg.ToString("F0") + " relR=" + s_reliableR.ToString("F0") + " fakeR=" + s_fakeR.ToString("F0"));
                    }
                    // 沿段 5 点采样: 壳开火时转得快, 3 点在旋转边界上极易闪烁("有的层挡住有的没挡住")。
                    // 两端同挡 或 ≥3 点挡(段主体在阴影里) => 整段丢弃; 1~2 点挡(压在边界) => 压暗柔化。
                    bool o0 = IsOccludedAny(q0);
                    bool o1 = IsOccludedAny(q1);
                    bool om = IsOccludedAny(mid);
                    bool oa = IsOccludedAny(Vector3.Lerp(q0, q1, 0.25f));
                    bool ob = IsOccludedAny(Vector3.Lerp(q0, q1, 0.75f));
                    int hits = (o0 ? 1 : 0) + (o1 ? 1 : 0) + (om ? 1 : 0) + (oa ? 1 : 0) + (ob ? 1 : 0);
                    if ((o0 && o1) || hits >= 3) { dbgSegDrop++; continue; }
                    if (hits > 0) { segInten *= Mathf.Clamp01(shellOccDim); dbgSegDim++; }
                    tgt = segsFar;
                }
                else
                {
                    // [旧形态] 简单置顶: 不做壳判定; 但星球仍 CPU 遮(轻量 ray-sphere)。
                    // [新方案] s_realDepth 时不跑 CPU 星球剔除: 硬件深度已正确处理星球/壳/恒星遮挡,
                    //   再叠一层 CPU 判会"提前消失 + 星球边缘闪烁"。逻辑上重复且只能变差。
                    if (!s_realDepth && s_universe && focusRespectPlanets && IsOccludedByPlanet(mid)) continue;
                }
                List<LaserSeg> sink = RouteQuad(ref q0, ref q1, tgt);
                sink.Add(new LaserSeg
                {
                    beginPos = q0,
                    endPos = q1,
                    intensity = segInten,
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

        // ★逐层扫描本颗恒星"游戏真的会画出来的壳层"半径包络(仅取 layer.orbitRadius, O(10), 不遍历 shell)。
        // 合法性判据与 BuildShellOccluders / DysonSphereSegmentRenderer.DrawModels 一致: 层非空 && shellPool 非空 && shellCursor>1 && inGameRenderMaskL 该位为 1。
        // 为什么必需逐层而不是用 dysonRadius*40000: 一个恒星最多 10 层, 每层自己的 orbitRadius 不同
        //   (源码 DysonSphere.cs: minOrbitRadius=physicsRadius*1.5, maxOrbitRadius=defOrbitRadius*2), 且各层独立旋转。
        //   把"一层的壳"当成一个整体球壁是错的: 单一半径既不是最内也不是最外, 内外两侧都会漏。
        // 返回 false = 一个可见壳层也没找到(调用方退到 dysonRadius*40000 的旧基线)。
        private static bool ScanShellRadii(GameData gd, StarData viewStar, out float rMin, out float rMax, out int n)
        {
            rMin = float.MaxValue; rMax = 0f; n = 0;
            if (gd == null || viewStar == null) return false;
            DysonSphere sp = null;
            DysonSphere[] arr = gd.dysonSpheres;
            if (arr != null && viewStar.index >= 0 && viewStar.index < arr.Length) sp = arr[viewStar.index];
            if (sp == null && arr != null)                                 // 回退: 槽位与 starData.index 不吻合时全扫(老档/异常容错)
                for (int i = 0; i < arr.Length; i++) if (arr[i] != null && arr[i].starData != null && arr[i].starData == viewStar) { sp = arr[i]; break; }
            if (sp == null || sp.layersIdBased == null) return false;
            for (int lid = 1; lid <= 10; lid++)
            {
                DysonSphereLayer layer = lid < sp.layersIdBased.Length ? sp.layersIdBased[lid] : null;
                if (layer == null || layer.shellPool == null || layer.shellCursor <= 1) continue;
                if ((sp.inGameRenderMaskL & (1 << lid)) == 0) continue;    // 玩家在游戏内关了该层显示 => 它不画壳, 也就不参不遮挡
                float r = layer.orbitRadius;                              // = layer.grossRadius(壳绘制的实际球半径)
                if (!(r > 0f) || float.IsNaN(r) || float.IsInfinity(r)) continue;
                if (r < rMin) rMin = r;
                if (r > rMax) rMax = r;
                n++;
            }
            if (n == 0) { rMin = float.MaxValue; return false; }
            return true;
        }

        // ★按戴森壳"自己的渲染逻辑"重建遮挡层数据(仅宇宙视角被调用方启用时)。
        // 合法性来源与 DysonSphereSegmentRenderer.DrawModels 完全一致: 层非空 && shellCursor>1 && inGameRenderMaskL 该位为 1;
        // 壳非空 && id==槽位 && polygon 有顶点(=玩家自定义形状的覆盖区)。隐藏的层/空槽位不参与遮挡。
        private static void BuildShellOccluders(GameData gd, StarData viewStar, VectorLF3 relativePos, Quaternion relativeRot)
        {
            for (int i = 1; i <= 10; i++) { occLayer[i] = null; occMaxR[i] = 0f; }
            DysonSphere sp = null;
            DysonSphere[] arr = gd.dysonSpheres;
            if (arr != null && viewStar.index >= 0 && viewStar.index < arr.Length) sp = arr[viewStar.index];
            if (sp == null && arr != null)                                 // 回退: 数组槽位与 starData.index 不吻合时全扫(老档/异常容错)
                for (int i = 0; i < arr.Length; i++) if (arr[i] != null && arr[i].starData != null && arr[i].starData == viewStar) { sp = arr[i]; break; }
            if (sp == null || sp.layersIdBased == null)
            {
                if (shellOccDiag > 1 && Time.frameCount % 90 == 0)
                    Utils.Log("[MMS shellOcc] NO sphere: starIdx=" + viewStar.index + " arrLen=" + (arr != null ? arr.Length : -1));
                return;
            }
            // 星心在视图坐标系的位置(宇宙视角壳绘制公式: 视图平移 = starData.uPosition - relativePos 经 QInvRotateLF)
            Vector3 starC = (Vector3)Maths.QInvRotateLF(relativeRot, viewStar.uPosition - relativePos);
            for (int lid = 1; lid <= 10; lid++)
            {
                DysonSphereLayer layer = lid < sp.layersIdBased.Length ? sp.layersIdBased[lid] : null;
                if (layer == null || layer.shellPool == null || layer.shellCursor <= 1) continue;
                if ((sp.inGameRenderMaskL & (1 << lid)) == 0) continue;    // 游戏内该层不可见(玩家关闭显示) => 不遮挡
                float maxR = 0f;
                for (int k = 1; k < layer.shellCursor; k++)
                {
                    DysonShell sh = layer.shellPool[k];
                    if (sh == null || sh.id != k || sh.polygon == null || sh.polygon.Count == 0) continue;
                    float r = (float)sh.polygon[0].magnitude;
                    if (r > maxR) maxR = r;
                }
                if (maxR <= 0f) { occLayer[lid] = null; continue; }
                occLayer[lid] = layer;
                occCenter[lid] = starC;
                occRot[lid] = layer.currentRotation;
                occMaxR[lid] = maxR;
                occShellAny = true;
                // 坐标实目诊断(只打第一次激活): 星心视图坐标/相机/壳半径 —— 若 starC 量级不对一眼看穿
                if (shellOccDiag > 1 && !occLayerLogged[lid])
                {
                    occLayerLogged[lid] = true;
                    Utils.Log("[MMS shellOcc] L" + lid + " active: starC=" + starC + " |starC|=" + starC.magnitude.ToString("F0")
                              + " camDist=" + (starC - occCam).magnitude.ToString("F0") + " maxR=" + maxR.ToString("F0")
                              + " cursor=" + layer.shellCursor + " rot=" + layer.currentRotation);
                }
            }
        }

        // pt(视图系)是否被某个"戴森壳块"挡在相机前方。严格按壳自身渲染逻辑逐层筛(occLayer 里都是游戏真的会画出来的层):
        // 1) 射线-壳球粗判(球外没有壳, 壳不是整球而是 polygon 覆盖区);
        // 2) 命中点做层的逆变换(壳局部 = QInvRotate(currentRotation, hp - 星心)), 用游戏自己的 IsPointInShell 判多边形覆盖
        //    ——该判定只认方向(内部 normalize×polygon[0] 长度), 故对壳半径缩放不敏感;
        // 3) 覆盖区内还要求该处建造进度 ≥ shellOccProgress —— 没建/在建的格子的实体六边形板由 shader 按 _NodeProgressArr 才画,
        //    进度不足的"有框架无实体"区域不挡激光; 激光自己从壳后方穿过(命中球都在 pt 之后)也不算被挡(t 须 < L)。
        private static bool IsOccludedByShell(Vector3 pt)
        {
            Vector3 dir = pt - occCam;
            float L = dir.magnitude;
            if (L < 1e-3f) return false;
            dir /= L;
            for (int lid = 1; lid <= 10; lid++)
            {
                DysonSphereLayer layer = occLayer[lid];
                if (layer == null) continue;
                Vector3 oc = occCenter[lid] - occCam;
                float tca = Vector3.Dot(oc, dir);
                float oc2 = oc.sqrMagnitude;
                float mr = occMaxR[lid];
                if (tca <= 0f && oc2 > mr * mr) continue;                 // 层壳整体在相机背后且 pt 不在其球锥内的廉价排除
                float d2 = oc2 - tca * tca;
                if (d2 > mr * mr) continue;                               // 射线擦不中最大壳球 => 该层无遮挡
                float ptC2 = (pt - occCenter[lid]).sqrMagnitude;          // 目标点到壳心距²(与相机同在壳球内则无壳面可挡, 逐壳廉价排除)
                Quaternion inv = Quaternion.Inverse(occRot[lid]);
                DysonShell[] pool = layer.shellPool;
                for (int k = 1; k < layer.shellCursor && k < pool.Length; k++)
                {
                    DysonShell sh = pool[k];
                    if (sh == null || sh.id != k || sh.polygon == null || sh.polygon.Count == 0) continue;
                    float r = (float)sh.polygon[0].magnitude;
                    if (d2 > r * r) continue;
                    if (oc2 < r * r && ptC2 < r * r) continue;            // 相机与目标点同在壳球内: 两者之间无壳面(穿壳交点在段外), 跳过 —— 消除壳内观战时 sphereHit 上万/帧的无效功
                    float thc = Mathf.Sqrt(r * r - d2);
                    // 失败定位实目(diag>1, 只打前3次): 相机到壳心距 vs 壳半径 —— 球内/球外与交点范围一眼可见
                    if (shellOccDiag > 1 && dbgOccSphereHit < 3)
                        Utils.Log("[MMS shellOcc] test L" + lid + " k" + k + " r=" + r.ToString("F0") +
                                  " |oc|=" + Mathf.Sqrt(oc2).ToString("F0") + " tca=" + tca.ToString("F0") +
                                  " t1=" + (tca - thc).ToString("F0") + " t2=" + (tca + thc).ToString("F0") + " L=" + L.ToString("F0"));
                    for (int s = 0; s < 2; s++)
                    {
                        float t = (s == 0) ? tca - thc : tca + thc;       // 近/远两个球面交点都测: 后方壳(t>L)不挡, 前方壳(t在0..L)挡
                        if (t <= 0f || t >= L) continue;
                        dbgOccSphereHit++;
                        Vector3 hp = occCam + dir * t;
                        Vector3 local = inv * (hp - occCenter[lid]);
                        if (!sh.IsPointInShell(new VectorLF3(local.x, local.y, local.z))) continue;
                        dbgOccPolyHit++;
                        if (ShellNodeProgress(sh, local) >= shellOccProgress) { dbgOccProgHit++; return true; }
                    }
                }
            }
            return false;
        }

        // 壳块上 local(层局部坐标)处的建造进度: 取最近节点 idx, 进度 = nodecps[idx] / ((vertsqOffset[idx+1]-vertsqOffset[idx]) * cpPerVertex)
        // ——与游戏 Import 时计算逐节点进度(used for _NodeProgressArr)的公式一致。近似: 节点附近用该节点进度, 不做格内插值。
        private static float ShellNodeProgress(DysonShell sh, Vector3 local)
        {
            List<DysonNode> nodes = sh.nodes;
            int[] cps = sh.nodecps;
            int[] offs = sh.vertsqOffset;
            if (nodes == null || cps == null || offs == null) return 1f;  // 数据缺失(极少数老档) => 保守视为实心会挡
            int cnt = nodes.Count;
            if (cnt == 0 || cps.Length < cnt || offs.Length < cnt + 1) return 1f;
            int idx = 0;
            float best = float.MaxValue;
            for (int i = 0; i < cnt; i++)
            {
                DysonNode nd = nodes[i];
                if (nd == null) continue;
                float dx = nd.pos.x - local.x, dy = nd.pos.y - local.y, dz = nd.pos.z - local.z;
                float d2 = dx * dx + dy * dy + dz * dz;
                if (d2 < best) { best = d2; idx = i; }
            }
            int total = (offs[idx + 1] - offs[idx]) * sh.cpPerVertex;
            if (total <= 0) return 0f;
            float pv = Mathf.Clamp01((float)cps[idx] / total);
            // ★进度门实目: 多边形命中却进度不足时直接打印原始数据(与游戏 SetMaterialDynamicVars L1527 同式), 一眼看穿是索引错位还是数据真为0
            if (pv < shellOccProgress && shellOccDiag > 0 && dbgProgSamp < 5)
            {
                dbgProgSamp++;
                Utils.Log("[MMS shellOcc] prog-miss idx=" + idx + " cps=" + cps[idx] + "/" + total
                          + " (offs " + offs[idx] + "->" + offs[idx + 1] + " cpPerV=" + sh.cpPerVertex + ")"
                          + " nodes=" + cnt + " cpsLen=" + cps.Length + " offsLen=" + offs.Length
                          + " p=" + pv.ToString("F3") + " nearD2=" + best.ToString("F0") + " localR=" + local.magnitude.ToString("F0"));
            }
            return pv;
        }

        // 置顶模式统一遮挡判定入口: 星球/恒星(球) → 戴森壳块(多边形覆盖+进度+层可见)。
        private static bool IsOccludedAny(Vector3 pt)
        {
            dbgOccCalls++;
            if (IsOccludedByPlanet(pt)) { dbgOccPlanet++; return true; }
            if (IsOccludedByShell(pt)) { dbgOccStar++; return true; }   // 此处 star 语义: 壳链总命中(细分计数见 sphereHit/poly/prog)
            return false;
        }

        // 稳定基线方案(按用户要求回退): 暂不做近/远分组。每段仅以相机为球心钳制到 s_clampR, 统一进调用方指定分组(主光束=segs; 汇聚=segsFront 置顶), 由对应 ztestMode 绘制。
        private static List<LaserSeg> RouteQuad(ref Vector3 q0, ref Vector3 q1, List<LaserSeg> group)
        {
            q0 = CamClamp(q0, s_camPos, s_drawR);
            q1 = CamClamp(q1, s_camPos, s_drawR);
            // ★深度上限一律在 shader 里做(BandZ: 球形壁 + 远平面兜底), 这里绝不改几何:
            //   ribbon 的 dir/sideV/宽度都由这两端点差重新算, 改点位置会让长尾 quad 退化塌缩(= 截断)。
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

        // 非反转深度([0,1])下的投影映射: d = f(z-n)/(z(f-n))  =>  1-d = n(f-z)/(z(f-n))。给定目标视深 R, 反解 eps = 1-d。
        // 为何必须用完整式而不是拿 n/R 当 eps: 靠近 far 时 (f-z) 因子主导, 同一个 eps 对应的视深会差几个数量级(旧版 eps=1e-7 实际飞到 ~1.7e6, 把激光放到整个载入星系之后 => 后壳误挡)。
        private static float EpsForViewDepth(float R, float n, float f)
        {
            if (!(R > 0f) || !(f > n + 0.01f)) return 1e-7f;
            if (R >= f) return 1e-7f;                       // 壁已在远平面之外 => 贴远平面内侧即可(星系整体都在 far 内, 前后关系由真实深度决定)
            float eps = n * (f - R) / (R * (f - n));
            if (eps < 1e-7f) eps = 1e-7f;                   // 低于 1 个 ulp 会量化成 d=1.0; 背景清除值也是 1.0, LEqual 仍通过, 但留点余量更稳
            if (eps > 0.9f) eps = 0.9f;
            return eps;
        }
        // 上述换算的反函数(仪表日志用): eps 对应的等效视深 z = f*n / (f - (1-eps)(f-n))
        private static float ViewDepthForEps(float eps, float n, float f)
        {
            if (!(f > n + 0.01f) || eps <= 0f) return float.MaxValue;
            float den = f - (1f - eps) * (f - n);
            return den > 1e-9f ? f * n / den : float.MaxValue;
        }
        // 视深 z 对应的 device depth(非反转 [0,1] 约定, 越大越远)。用 double 算, 仅供日志报"壁与壳差几个 ulp"。
        // 壁必须比壳远(此值大于壳的), 否则激光落在壳前 => 不会被遮; 差值小于 1~2 个 float32 ulp 则比较结果会随机。
        private static double DevDepthOf(double z, double n, double f)
        {
            if (!(z > n) || !(f > n)) return 0.0;
            return f * (z - n) / (z * (f - n));
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

            // [简单置顶方案 / 旧形态] 宇宙视角: 主/子光束永远覆盖戴森壳(Always=8), 不做 CPU 壳判定; 星图/编辑器维持 ztestMode。
            // 下发 _ZCompress = 0 => shader 完全保持旧行为(不压深度), 星图/编辑器表现逐帧不变。
            // segsFar 在废弃方案后恒空, 保留其 Always 绘制以防实验回启。
            if (s_realDepth)
            {
                // [新方案] 真实深度: 三个分组统一走 LEqual + 压深度带 —— 超出远平面的顶点被压到"可表达的最远深度",
                // 于是: 戴森壳/星球/恒星在前时能真实挡住激光, 而跨星系远端不再被 farClipPlane 切成齐刷刷切口。
                // (置顶分组与 CPU 星球剔除在新方案下不再参与: 遮挡全部交给硬件深度。)
                int zm = ztestMode <= 0 ? 4 : ztestMode;              // 保证是深度比较而非 Disable
                float eps = s_zCompress;
                float fade = s_sphereWall ? Mathf.Clamp(haloFadeFracRealDepth, 0.05f, 1f) : haloFadeFrac;
                DrawRibbonGroup(segs, zm, projFactor, clipFar, eps, s_sysFoldR, fade);
                DrawRibbonGroup(segsFront, zm, projFactor, clipFar, eps, s_sysFoldR, fade);   // 新方案下通常为空
                DrawRibbonGroup(segsFar, zm, projFactor, clipFar, eps, s_sysFoldR, fade);
                if (diagLog && Time.frameCount % 60 == 0)
                {
                    // 仪表行 1: 分组数量 + 本帧到底走了哪条深度路径(sys=球壁半径 / plane=平面壁 / off=不压)
                    Utils.Log("[MMS realDepth] ztest=" + zm + " segs=" + segs.Count + " front=" + segsFront.Count +
                              " far=" + segsFar.Count + " beams=" + beams.Count +
                              " wall=" + (s_sphereWall ? "sphere" : (s_zTargetR > 0f ? "plane" : "off")) +
                              " fade=" + fade.ToString("F2") +
                              " drawR=" + (s_drawR >= float.MaxValue ? "none" : s_drawR.ToString("F0")) +
                              " ver=" + laserShaderVer.ToString("F0"));
                    // 仪表行 2: 壁参数。camD=相机到载入恒星, shellR=默认层半径(dysonRadius*40000), rMin/rMax=逐层扫描到的最内/最外壳半径,
                    // layers=可见壳层数, sysR=落点参考半径(下发 _SysFoldR), outerR=门控半径(下发 _SysFoldOuterR),
                    // wall=平面壁模式的目标视深, eps=远平面兜底, epsZ=eps 真正等效的视深, frontShellZ/rearShellZ=前/后壳层视深。
                    Utils.Log("[MMS zWall] near=" + s_dbgNear.ToString("E2") + " far=" + s_dbgFar.ToString("E2") +
                              " camD=" + s_dbgCamStarD.ToString("E2") + " shellR=" + s_dbgShellR.ToString("E2") +
                              " layers=" + s_shellLayerN + " rMin=" + s_shellRMin.ToString("E2") + " rMax=" + s_shellRMax.ToString("E2") +
                              " sysR=" + (s_sysFoldR > 0f ? s_sysFoldR.ToString("E2") : "off") +
                              " outerR=" + (s_sysFoldOuter > 0f ? s_sysFoldOuter.ToString("E2") : "off") +
                              " refMul=" + ribbonSysRefMul + " pullFrac=" + ribbonSysPullFrac + " pull=" + s_sysFoldPull.ToString("E2") +
                              " camInShell=" + (s_dbgCamStarD < s_shellRMax ? "yes" : "no") +
                              " mul=" + ribbonDepthTargetMul +
                              " planeWall=" + (s_zTargetR > 0f ? s_zTargetR.ToString("E2") : "-") +
                              " eps=" + eps.ToString("E2") + " epsZ=" + ViewDepthForEps(eps, s_dbgNear, s_dbgFar).ToString("E2") +
                              " maxD=" + cur_MaxViewDist.ToString("E2") +
                              " frontShellZ=" + Math.Max(1.0, s_dbgCamStarD - s_shellRMin).ToString("E2") +
                              " rearShellZ=" + (s_dbgCamStarD + s_shellRMin).ToString("E2"));
                    // 仪表行 3(关键证据): 落点与**最内层后半球面**在 device depth 上的间距。★必须为正: 激光落在它之前。
                    // 最内层是最严格的基准: 激光能在它之前(marginUlp>0), 更外层的后半球面比它更远, 自然也在激光之后; 而前半球面(近约 2 个壳半径)照样能挡住激光。
                    // (v17 及更早把壁放在壳外 => 这里是负值 => "激光被自己星系后方的壳层挡住", 与实际观察到的现象同根。)
                    if (s_sphereWall)
                    {
                        double nd = s_dbgNear, fd = s_dbgFar;
                        double devLaser = DevDepthOf((double)s_dbgCamStarD + s_sysFoldR - s_sysFoldPull, nd, fd);
                        double devShell = DevDepthOf((double)s_dbgCamStarD + s_shellRMin, nd, fd);
                        double devRearOuter = DevDepthOf((double)s_dbgCamStarD + s_shellRMax, nd, fd);
                        double devFront = DevDepthOf(Math.Max((double)s_dbgNear * 1.001, (double)s_dbgCamStarD - s_shellRMin), nd, fd);
                        double marginUlp = (devShell - devLaser) / 1.1920929e-7;
                        Utils.Log("[MMS zGap] devLaser=" + devLaser.ToString("F9") + " devShell=" + devShell.ToString("F9") +
                                  " marginUlp=" + marginUlp.ToString("F1") +
                                  (marginUlp < 0.0 ? "  !!负=激光在最内层后壳之后 => 必被后方壳层误挡 => 调大 ribbonSysPullFrac"
                                   : marginUlp < 2.0 ? "  !!小于 2 个 ulp, 次序会被舍入噪声主导 => 调大 ribbonSysPullFrac" : "") +
                                  " vsOuterUlp=" + ((devRearOuter - devLaser) / 1.1920929e-7).ToString("F0") +
                                  " frontMarginUlp=" + ((devLaser - devFront) / 1.1920929e-7).ToString("F0") +
                                  " pull=" + s_sysFoldPull.ToString("E2") +
                                  " dbg=" + (ribbonZDebug ? "ON" : "off") + " fold=per-ray-land(innerR)-pull(outerR=gate)");
                    }
                }
                return;
            }
            DrawRibbonGroup(segs, s_universe ? 8 : ztestMode, projFactor, clipFar, 0f, 0f, haloFadeFrac);
            DrawRibbonGroup(segsFar, 8, projFactor, clipFar, 0f, 0f, haloFadeFrac);        // 远景分组: Always(8) —— 现行形态下恒空, 仅 shellOccludeEnable 实验回启时复用
            DrawRibbonGroup(segsFront, 8, projFactor, clipFar, 0f, 0f, haloFadeFrac);      // 置顶分组: Always(8), 仅汇聚 ribbon => 覆盖所有戴森壳(无玻璃遮挡), 最后绘制确保在最前
        }

        // 上传并即时绘制一个分组。每组各自 SetBuffer+SetData+SetPass(0)+Draw(重设 buffer 绑定, 避免二次 Draw 读到脏 buffer)。
        // zCompress: 0 = 不压深度(shader 逐字保持旧行为); >0 = 把顶点裁剪深度压回远平面内侧。
        // sysFoldR: 球形深度壁半径(0 = 关闭); fadeFrac: 外辉深度渐隐起点比例。旧分支两者下发 0/默认 => 属性不存在也无害。
        private static void DrawRibbonGroup(List<LaserSeg> list, int zmode, float projFactor, float clipFar, float zCompress,
            float sysFoldR, float fadeFrac)
        {
            int count = list.Count;
            if (count == 0) return;

            // buffer 与 scratch 同容量增长; scratch 复用持久数组(不再每帧 new LaserSeg[count] 进 LOH)。
            // Unity 2018.4 的 SetData 无 (array, start, length) 重载 => 整块上传, 超出 count 的陈旧项不会被 DrawProceduralNow 读到。
            if (segBuffer == null || segScratch.Length < count)
            {
                int nc = segScratch.Length == 0 ? 256 : segScratch.Length;
                while (nc < count) nc *= 2;
                if (segBuffer != null) segBuffer.Release();
                segBuffer = new ComputeBuffer(nc, SEG_STRIDE, ComputeBufferType.Default);
                segCapacity = nc;
                System.Array.Resize(ref segScratch, nc);
            }
            list.CopyTo(0, segScratch, 0, count);
            segBuffer.SetData(segScratch);

            // 屏幕像素下限换算系数：像素 -> 世界尺寸 = px * 距离 / _ProjFactor
            laserMat.SetBuffer("_LaserBuffer", segBuffer);
            laserMat.SetFloat("_ProjFactor", projFactor);
            laserMat.SetFloat("_HaloEndFade", haloEndFade);   // 外辉首尾渐变(核心不受影响)
            laserMat.SetFloat("_ClipFar", clipFar);
            laserMat.SetFloat("_HaloFadeFrac", fadeFrac);
            laserMat.SetInt("_ZTestMode", zmode);
            laserMat.SetFloat("_ZCompress", zCompress);   // 旧分支下发 0 => 属性不存在也无害(SetFloat 到未声明属性是空操作)
            laserMat.SetFloat("_SysFoldR", sysFoldR);     // 落点参考半径 = 最内层壳半径(v16+); 旧分支与旧 shader 下同为空操作
            laserMat.SetFloat("_SysFoldOuterR", s_sysFoldOuter);   // 门控半径 = 最外层壳半径(v19+); 视线垂距超过它就不折(那里没壳)
            laserMat.SetFloat("_SysFoldPull", s_sysFoldPull);   // 落点沿射线回退量(v18+); 0 = 贴在参考面上(= v17 行为)
            laserMat.SetFloat("_ZDebug", ribbonZDebug ? 1f : 0f);   // [诊断] 把最终深度编进颜色; 0 = 正常着色
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
                // ★真正的粗细属性: 该 shader 无 _Size, 但有 _Width(与 _Multiplier) —— 逐个探测并记住默认值
                matHasWidth = mat.HasProperty("_Width");
                matWidthDefault = matHasWidth ? mat.GetFloat("_Width") : 1f;
                if (matHasWidth && matWidthDefault <= 0f) matWidthDefault = 1f;
                matHasMult = mat.HasProperty("_Multiplier");
                matMultDefault = matHasMult ? mat.GetFloat("_Multiplier") : 1f;
                if (matHasMult && matMultDefault <= 0f) matMultDefault = 1f;
                // 候选宽度机制 #2 与深度波段钳制开关: 同样逐个探测 + 记默认值
                matHasDistScale = mat.HasProperty("_DistScalePoint");
                matDistScaleDefault = matHasDistScale ? mat.GetFloat("_DistScalePoint") : 60000f;
                if (matHasDistScale && matDistScaleDefault <= 0f) matDistScaleDefault = 60000f;
                matHasZMin = mat.HasProperty("_ZMin");
                matZMinDefault = matHasZMin ? mat.GetFloat("_ZMin") : 0f;
                matHasZMax = mat.HasProperty("_ZMax");
                matZMaxDefault = matHasZMax ? mat.GetFloat("_ZMax") : 1f;
                if (diagLog)
                    Utils.Log("[MMS bulletMat] props: _Size=" + matHasSize + "/" + matSizeDefault +
                              " _Width=" + matHasWidth + "/" + matWidthDefault +
                              " _Multiplier=" + matHasMult + "/" + matMultDefault +
                              " _DistScalePoint=" + matHasDistScale + "/" + matDistScaleDefault +
                              " _ZMin=" + matHasZMin + "/" + matZMinDefault +
                              " _ZMax=" + matHasZMax + "/" + matZMaxDefault +
                              " shader=" + (mat.shader != null ? mat.shader.name : "null"));
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
            VectorLF3 relativePos, Quaternion relativeRot, VectorLF3 viewTarget, VectorLF3 starPos, int bulletId = 0)
        {
            SailBullet b = new SailBullet();
            b.id = bulletId != 0 ? bulletId : pool.Count + 1;   // 外部传入确定性 id 时不依赖池序(防光束增删导致整批 id 漂移闪烁)
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
                    if (bulletFarClampFrac > 0f)
                    {
                        Camera cam = Camera.current;   // OnRenderObject 内有效; 相机即相对帧原点
                        if (cam != null)
                        {
                            float R = cam.farClipPlane * Mathf.Clamp(bulletFarClampFrac, 0.1f, 0.999f);
                            ClampToSphere(ref b.rBegin, R);
                            ClampToSphere(ref b.rEnd, R);
                        }
                    }
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

        // ============ [bullet 束模式] 宇宙视角原生 sailBullet 主渲染器 ============
        // 历史坑逐一防护: ①静止/冻头: 每股径向位置只由 (keySeed, 股号) 决定, 跨帧不变但不会互相"换位置"
        //   不出现"前后两帧子弹位置刚好互换 => 看起来不流动"(车轮效应): head = (now/流动周期 + 股相位) % 1 由时间恒定推进
        //   且 bullet.id 用 StableId(确定性散列)不依赖池序 => 光束增删/池重排不会引起整批 id 漂移而看起来静止或闪烁。
        // ②起终点太浅: 两端各补一小段短路径束(沿轴 len*frac)补偿整跨精灵首尾淡出(最早直接加 bullet 的思路)。
        // ③粗细: 该 shader 无 _Size(之前 SetFloat 写在不存在属性上所以"无效"), 真正的宽度是 _Width, 已接 bulletWidth* 旋钮; 主靠股数密度 + 圆盘铺开倍数。
        // ④state=0(AddSpan 内)永不生成真太阳帆, 不污染戴森云。⑤LOH: UploadAndDraw 复用持久 scratch, 不每帧 new 大数组。
        private static void RenderUniverseBullets(GameData gd, StarData viewStar)
        {
            if (viewStar == null) return;
            if (!TryInitMaterial()) return;                    // 原生材质不可用 => 调用方(Render 入口)回退 ribbon
            int localIdx = viewStar.index;
            int nMain = -1, nFocus = -1, nSub = -1;             // 诊断: 各桶实际提交的子弹数
            VectorLF3 relativePos = gd.relativePos;
            Quaternion relativeRot = gd.relativeRot;
            VectorLF3 viewTarget = UIStarmap.viewTargetStatic;
            VectorLF3 starPos = viewStar.uPosition;

            mainKeys.Clear(); focusKeys.Clear(); blobKeys.Clear();
            float focusBright = 0f, mainBright = 0f;
            foreach (var kv in beams)
            {
                BeamProfile p = kv.Value;
                bool beginLocal = p.beginStarIdx >= 0 && p.beginStarIdx == localIdx;
                bool endLocal = p.endStarIdx >= 0 && p.endStarIdx == localIdx;
                if (kv.Key == 1)
                {
                    if (beginLocal || endLocal) { mainKeys.Add(kv.Key); if (p.intensity > mainBright) mainBright = p.intensity; }
                }
                else if (kv.Key >= 1000 && kv.Key < 2000)
                {
                    if (endLocal) blobKeys.Add(kv.Key);        // 攻击子激光: 仅目标系
                }
                else if (kv.Key >= 2000)
                {
                    if (beginLocal || endLocal) { focusKeys.Add(kv.Key); if (p.intensity > focusBright) focusBright = p.intensity; }
                }
            }

            // 主激光: 整跨股 + 两端短束; 亮度随充能/收尾(p.intensity)
            if (mainKeys.Count > 0 && mainBright > 0.01f)
            {
                pool.Clear();
                foreach (int k in mainKeys)
                {
                    BeamProfile p;
                    if (!beams.TryGetValue(k, out p)) continue;
                    BeamProfile q = p;
                    q.muzzleRadius *= bulletMainWidthMul;
                    q.impactRadius *= bulletMainWidthMul;
                    GenBeamBullets(q, relativePos, relativeRot, viewTarget, starPos, bulletStrandsMain, k * 7919 + 11, bulletEndBundleStrands,
                        SideMul(p.beginStarIdx, localIdx), SideMul(p.endStarIdx, localIdx), bulletEndSegs);
                }
                Color mc = DefaultColor;
                mc *= Mathf.Clamp01(mainBright);
                nMain = pool.Count;
                if (pool.Count > 0) UploadAndDraw(mc, overlaySizeMain);
            }
            // 主激光端束股数用 bulletEndBundleStrands(实芯端头), 汇聚/攻击用 side 档
            // 汇聚: 随预热渐亮(数十条汇聚本就会叠加增亮, 不再额外压暗)
            if (focusKeys.Count > 0 && focusBright > 0.01f)
            {
                pool.Clear();
                foreach (int k in focusKeys)
                {
                    BeamProfile p;
                    if (!beams.TryGetValue(k, out p)) continue;
                    GenBeamBullets(p, relativePos, relativeRot, viewTarget, starPos, bulletStrandsFocus, k * 7919 + 23, bulletEndBundleStrandsSide,
                        SideMul(p.beginStarIdx, localIdx), SideMul(p.endStarIdx, localIdx), bulletEndSegsSide);
                }
                Color fc = DefaultColor;
                fc *= Mathf.Clamp01(focusBright);
                nFocus = pool.Count;
                if (pool.Count > 0) UploadAndDraw(fc, overlaySizeFocus);
            }
            // 攻击子激光: 压暗防大量重叠糊团(与旧叠加层同策略)
            if (blobKeys.Count > 0)
            {
                pool.Clear();
                foreach (int k in blobKeys)
                {
                    BeamProfile p;
                    if (!beams.TryGetValue(k, out p)) continue;
                    GenBeamBullets(p, relativePos, relativeRot, viewTarget, starPos, bulletStrandsSub, k * 7919 + 37, bulletEndBundleStrandsSide,
                        SideMul(p.beginStarIdx, localIdx), SideMul(p.endStarIdx, localIdx), bulletEndSegsSide);
                }
                Color bc = DefaultColor;
                bc *= subIntensityScale;
                nSub = pool.Count;
                if (pool.Count > 0) UploadAndDraw(bc, overlaySizeSub);
            }

            // 诊断(节流): 确认三类光束是否真进了 bullet 通道 + 每帧子弹量(性能预算) + _Width/_Multiplier 默认值
            if (diagLog && Time.frameCount % 60 == 0)
                Utils.Log("[MMS bulletBeam] localIdx=" + localIdx + " beams=" + beams.Count +
                          " main=" + mainKeys.Count + "/" + nMain + " focus=" + focusKeys.Count + "/" + nFocus +
                          " sub=" + blobKeys.Count + "/" + nSub +
                          " mainBright=" + mainBright.ToString("F2") + " focusBright=" + focusBright.ToString("F2") +
                          " W=" + matHasWidth + "/" + matWidthDefault + " M=" + matHasMult + "/" + matMultDefault +
                          " DS=" + matHasDistScale + "/" + matDistScaleDefault + " Z=" + matHasZMin + "/" + matZMinDefault + "-" + matZMaxDefault + "/" + matHasZMax +
                          " cap=" + capacity);
        }

        // 玩家在哪个系, 那一端的端束股数加权(近端观战时恒星一侧不再显得浅)
        private static float SideMul(int starIdx, int localIdx)
        {
            return (starIdx >= 0 && starIdx == localIdx) ? Mathf.Max(0.05f, bulletLocalSideBoost) : 1f;
        }

        // 一条光束: count 股全长平行股 + 两端各一组短子弹补光(全部参数由 keySeed 确定性派生, 帧间稳定)
        // beginSideMul/endSideMul: 两端的股数权重(玩家在哪个系, 那一端 ×bulletLocalSideBoost)
        private static void GenBeamBullets(in BeamProfile p, VectorLF3 relativePos, Quaternion relativeRot,
            VectorLF3 viewTarget, VectorLF3 starPos, int count, int keySeed, int endStrands,
            float beginSideMul = 1f, float endSideMul = 1f, int endSegs = 0)
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
            float flow = Mathf.Max(0.05f, p.flowLife) * Mathf.Max(0.01f, bulletFlowScale);
            const float golden = 2.39996323f;

            for (int s = 0; s < NS; s++)
            {
                float rf, ang;
                if (s == 0) { rf = 0f; ang = 0f; }                            // 中心股恒在轴上(最亮实芯)
                else { rf = Mathf.Sqrt((s + 0.5f) / NS); ang = s * golden; }  // 低差异均匀铺盘, 横截面稳定不闪
                VectorLF3 radial = perp1 * Mathf.Cos(ang) + perp2 * Mathf.Sin(ang);
                VectorLF3 offB = radial * (p.muzzleRadius * rf);
                VectorLF3 offE = radial * (p.impactRadius * rf);
                float head = (now / flow + Hash01(keySeed, s)) % 1f;           // 恒由时间驱动 => 能量流动不冻头
                AddSpan(p, p.begin, p.end, offB, offE, head, relativePos, relativeRot, viewTarget, starPos, StableId(keySeed, s));
            }

            // ===== 起/终点补光 =====
            // ★上一版重大翻车记录(用户实测"大量大的倒水滴状静止粒子堆"): 窗口长度用了 len×frac —— 对跨星系长束而言
            //   frac=0.1 就是几千万米的"子弹", 而子弹精灵是固定长宽比的拖尾(越长越宽) => 在恒星旁堆出一摞巨大型水滴光斑;
            //   同时把流动周期按段长缩放(flow×sub/len) 使其跌到 ~0.03s, 远低于帧时间 => 逐帧相位跳 1.3 个整周期 = 完全混叠
            //   => 看起来悬停静止(正是最初那个"前后两帧恰好替掉"的坑, 但这次是我自己造成的); 又把相位锁定 0.5 => 直接静态。
            // 正解: (a)窗口长度取"绝对尺度"—— 优先用该端恒星的最大壳长 beginShellLen/endShellLen, 并硬上限 bulletEndBundleCeiling;
            //       (b)周期与整束股一致(flow × bulletEndPeriodMul), 不再按段长缩放, 保证相位逐帧只走百分之几 => 平滑流动不混叠;
            //       (c)默认 bulletEndHead = -1(全流动); 需要更实可改 0..1 锁定, 代价是静态感。
            if (endStrands <= 0) return;                       // <=0 显式关闭端束(不生成零长子弹)
            int NSeg = Mathf.Max(1, endSegs > 0 ? endSegs : bulletEndSegs);
            float ov = Mathf.Clamp(bulletEndOverlap, 0f, 0.6f);
            float period = Mathf.Max(0.1f, flow * Mathf.Max(0.05f, bulletEndPeriodMul));
            int NE = Mathf.Max(1, endStrands);
            int NB = Mathf.Max(1, Mathf.RoundToInt(NE * Mathf.Max(0.05f, beginSideMul)));
            int NX = Mathf.Max(1, Mathf.RoundToInt(NE * Mathf.Max(0.05f, endSideMul)));
            float rMul = Mathf.Clamp(bulletEndRadiusMul, 0.05f, 1f);
            double winB = EndWindow(len, p.beginShellLen);
            double winE = EndWindow(len, p.endShellLen);
            if (winB < 1.0) NB = 0;
            if (winE < 1.0) NX = 0;
            VectorLF3 vSubB = dir * (winB / NSeg * (1.0 + ov));
            VectorLF3 vSubE = dir * (winE / NSeg * (1.0 + ov));
            for (int s = 0; s < NB; s++)
            {
                float rf = (s == 0) ? 0f : Mathf.Sqrt((s + 0.5f) / NB);
                float ang = s * golden;
                VectorLF3 radial = perp1 * Mathf.Cos(ang) + perp2 * Mathf.Sin(ang);
                VectorLF3 oB = radial * (p.muzzleRadius * rMul * rf);
                for (int si = 0; si < NSeg; si++)
                {
                    VectorLF3 b0 = p.begin + dir * (winB * si / NSeg);
                    float h = bulletEndHead >= 0f ? Mathf.Clamp01(bulletEndHead)
                                                  : (now / period + Hash01(keySeed, 1024 + s * 64 + si)) % 1f;
                    AddSpan(p, b0, b0 + vSubB, oB, oB, h, relativePos, relativeRot, viewTarget, starPos, StableId(keySeed, 16384 + s * 64 + si));
                }
            }
            for (int s = 0; s < NX; s++)
            {
                float rf = (s == 0) ? 0f : Mathf.Sqrt((s + 0.5f) / NX);
                float ang = s * golden;
                VectorLF3 radial = perp1 * Mathf.Cos(ang) + perp2 * Mathf.Sin(ang);
                VectorLF3 oE = radial * (p.impactRadius * rMul * rf);
                for (int si = 0; si < NSeg; si++)
                {
                    VectorLF3 b1 = p.end - dir * (winE * si / NSeg);
                    float h = bulletEndHead >= 0f ? Mathf.Clamp01(bulletEndHead)
                                                  : (now / period + Hash01(keySeed, 2048 + s * 64 + si)) % 1f;
                    AddSpan(p, b1 - vSubE, b1, oE, oE, h, relativePos, relativeRot, viewTarget, starPos, StableId(keySeed, 32768 + s * 64 + si));
                }
            }
        }

        // 端头补光窗口长度: 绝对尺度而非全束比例。优先用该端恒星的"最大壳半径对应长度", 再被硬上限卡住;
        // 拿不到 shellLen 时才退回 len×frac(短束无所谓, 长束靠硬上限兼底)。
        private static double EndWindow(double len, double shellLen)
        {
            double w = len * Mathf.Clamp(bulletEndBundleFrac, 0.02f, 0.3f);
            if (shellLen > 1.0) w = Math.Min(w, shellLen);
            if (bulletEndBundleCeiling > 1.0) w = Math.Min(w, (double)bulletEndBundleCeiling);
            return Math.Min(w, len);        // 绝不超过整束长度本身
        }

        // 确定性子弹 id: 同一(seed,股号)永远同值, 与池序无关 => 光束增删不会整批 id 漂移而闪烁
        private static int StableId(int seed, int i)
        {
            unchecked { return (int)((((uint)seed * 2654435761u) ^ ((uint)i * 40503u)) & 0x3FFFFFFFu) + 1; }
        }

        // 把超出以原点为球心、半径 R 的球面的点沿视线拉回球面(bullet 空式钳制专用: 保留方向, 只截距离)
        private static void ClampToSphere(ref Vector3 v, float R)
        {
            float d = v.magnitude;
            if (d > R && d > 1e-4f) v *= (R / d);
        }

        private static void UploadAndDraw(Color color, float sizeMul = 1f)
        {
            int count = pool.Count;
            if (count == 0) return;

            // buffer 与 scratch 同容量增长; 复用持久 scratch(不再每帧 new SailBullet[count]; 开火时 count 上千×112B 属于 LOH, 每帧砸一块 => GC 逐段累积 => fps 随时间坠到 10)。
            // Unity 2018.4 的 SetData 无 (array, start, length) 重载 => 整块上传, 超出 count 的陈旧项不会被 DrawProceduralNow 读到。
            if (buffer == null || bulletScratch.Length < count)
            {
                int nc = bulletScratch.Length == 0 ? 1024 : bulletScratch.Length;
                while (nc < count) nc *= 2;
                if (buffer != null) buffer.Release();
                buffer = new ComputeBuffer(nc, STRIDE, ComputeBufferType.Default);
                capacity = nc;
                System.Array.Resize(ref bulletScratch, nc);
            }
            pool.CopyTo(0, bulletScratch, 0, count);
            buffer.SetData(bulletScratch);

            mat.SetColor("_Color0", color);
            if (matHasSize) mat.SetFloat("_Size", matSizeDefault * sizeMul);   // 旧写法: 该 shader 其实没有 _Size, 保留无害
            if (matHasWidth)                                                // ★真正的粗细入口(sail bullet inst shader 有 _Width)
            {
                float wRole = bulletWidthRoleScale ? Mathf.Max(0.01f, sizeMul) : 1f;
                mat.SetFloat("_Width", bulletWidthAbs > 0f ? bulletWidthAbs : matWidthDefault * bulletWidthMul * wRole);
            }
            if (matHasMult) mat.SetFloat("_Multiplier", matMultDefault * bulletMultMul);
            if (matHasDistScale)
                mat.SetFloat("_DistScalePoint", bulletDistScaleAbs > 0f ? bulletDistScaleAbs : matDistScaleDefault * Mathf.Max(0.01f, bulletDistScaleMul));
            if (matHasZMin && bulletZMinAbs >= 0f) mat.SetFloat("_ZMin", bulletZMinAbs);   // -1 = 保持游戏默认
            if (matHasZMax && bulletZMaxAbs >= 0f) mat.SetFloat("_ZMax", bulletZMaxAbs);   // -1 = 保持游戏默认(6)
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
