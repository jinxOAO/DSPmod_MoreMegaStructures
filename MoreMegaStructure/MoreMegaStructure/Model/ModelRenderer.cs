using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace MoreMegaStructure
{
    /// <summary>
    /// 折跃场（Warp Array）巨构的原生模型组装渲染器。
    ///
    /// 目标：不引入任何外部建模资源，完全用【游戏自己的模型与材质】在恒星周围组装出一座
    /// 固定的折跃场造型，并让它只在玩家所处的星系内、以与原版戴森壳完全相同的时序与深度关系呈现。
    ///
    /// ── 与原版戴森壳保持完全一致的部分（逐条对照反编译源码确证）──
    /// 1) 触发时机与门控：壳是在 GameLogic.Draw 内部这样画的（GameLogic.cs:1824-1832）
    ///        if (this.localStar != null && DysonSphere.renderPlace == ERenderPlace.Universe)
    ///            this.data.dysonSpheres[this.localStar.index].DrawModel();
    ///    本类挂在 GameLogic.Draw 的 postfix 上，使用同一条判据；因此星图 / 戴森球编辑器 / 菜单演示
    ///    一律不画，玩家离开该星系立即不画，与壳的表现严格同步。
    /// 2) 坐标配方：壳在 DysonSphereSegmentRenderer.DrawModels 里把恒星换算到"以行星为原点的
    ///    相机局部系"（DysonSphereSegmentRenderer.cs:355-373），再用
    ///    Matrix4x4.TRS(恒星局部位置, Conjugate(行星运行旋转) * 层旋转, 1) 提交（同文件 :477）。
    ///    本类逐字复刻：整体根矩阵 = TRS(starLocal, conjPlanet, one)，部件矩阵 = TRS(偏移, 姿态, 缩放)，
    ///    最终矩阵 = root * part。这样行星自转/公转带来的坐标系摆动被完全抵消，模型与壳"钉"在同一空间。
    /// 3) 渲染层与绘制调用：壳用 layer 16（Universe 与 Starmap 用 16/20，编辑器用 21，见同文件 :401-410），
    ///    camera 传 null 表示"交给所有按 cullingMask 认领该层的相机"。本类只用 16，即只在现实视角出现。
    ///    DrawMesh 重载与参数（submesh 索引、ShadowCastingMode.Off、receiveShadows:false）也与壳一致，
    ///    保证不透明队列、深度写入与原版戴森结构逐位同构 —— 这也是本项目激光深度方案已验证过的通路。
    ///
    /// ── 刻意与原版不同的一点 ──
    /// 不受玩家的戴森壳渲染开关影响：原版在 DrawModels 里用 gameMask/editorMask 过滤
    /// （DysonSphere.cs:2027 传入 inGameRenderMaskL，段内再按 1 << layerId 位与，见 :395/:456-460）。
    /// 本类完全不读取这些 mask，因此玩家关掉壳层渲染后折跃场模型依旧可见。
    ///
    /// ── 两层结构：结构层（原版 PBR 材质 + 覆写旋钮）+ 自发光能量骨架层 ──
    /// 结构层用游戏原生建筑网格与【原生 PBR 材质】，负责贴图细节与"不同面不同的光"；
    /// 发光层用 unlit 材质 + 我们生成的亮环/光核，负责"远看看得见 + 有颜色"。
    ///
    /// 结构层的上色不再靠"外面套一层纯色壳"（第四轮实测证明那件紧身衣会把 _MainTex/_NormalTex/_MS_Tex
    /// 全部遮住，反而造成"发光处一个色、不发光处看不出结构、黑与光直接交界"），而是用 MaterialPropertyBlock
    /// 逐次绘制覆写官方材质自己的旋钮（依据见 _dev/warpmodel §5.3）：
    ///   保险 A 受光通道：_AlbedoMultiplier 抬亮、_NormalMultiplier 放大法线贴图面差、
    ///                    _SmoothMultiplier/_SpecularColor 做出随视角变化的高光、_Color 只轻染不洗掉层次；
    ///   保险 B 自发光通道：两个门**语义不同**（第八/九轮 A/B 实测才分清，见 P21）——
    ///                    _EmissionUsePower = "发光要不要读供电数据"（按三档 EGateMode 给值，默认 IgnorePower 写 0：
    ///                    假建筑没有逐实例供电数组，任何"交给游戏状态决定"的取值都读到空 ⇒ 灭），
    ///                    _EmissionSwitch = "这个发光开关开不开"（用 StructureEmissionSwitchValue，默认 0）。
    ///                    第九轮实测：把后者交回材质原值 ⇒ 电塔红点、接收侧发光、主环颜色一起消失，
    ///                    所以结构层【必须写】它 —— 上一轮"永久不覆写"的结论已作废（P21）。
    ///                    判据只能问运行时 HasProperty：.mat 里的序列化字段可能是 shader 根本不认的遗留值。
    ///                    _EmissionMultiplier 取一档低于游戏默认 5.5 的值，
    ///                    _EmissionMask / _EmColor1 / _EmColor2 三个候选色属性一次铺满（三支 shader 各认其中一个）。
    /// 结构层写在 MPB 里 ⇒ 不改游戏材质、不改任何全局状态，对原版渲染零副作用；
    /// 唯一的例外是【特效层】：建筑是多 renderer 合并成一张网格的（P18/P22），主网格里那几个"非本体材质"的
    /// submesh 会被摘出来交给我们的克隆材质单独绘制 —— 这样特效才有独立抓手（门控/关键字/染色），
    /// 而游戏资产依旧一个字都没被改过。
    /// 饱和度不靠 _Color 洗色，而靠【冷暖分离】：受光面 = 恒星色温轻掺色板，背光/过渡面 = 重掺色板，
    /// 于是同一次绘制里两块面各持一种色相（P15）。
    ///
    /// 第三层是【建筑自带的原生特效件】（P16 / R20，归类判据第八轮由 P18 改判）：原版建筑的"接收光效"
    /// 是挂在 model prefab 里的子网格 + 特效材质（如 Unlit Additive Ray Receiver Gravity），不是粒子。
    /// 但对【多 renderer 的建筑】，`prefabDesc.mesh` 已把这些 renderer **合并成一张网格**、`materials` 扁平成
    /// 一个数组（实测 ray-receiver：3913+164 = 4077 顶点、材质 4 项含 eff）⇒ 特效其实已经由结构层那次绘制画过了；
    /// 于是判据必须是"这个 renderer 的材质在不在主网格材质数组里"，而**不能**是"网格是不是同一张"——
    /// 后者会把主体当特效件重画第二遍，那层不吃 MPB 的玻璃罩保持原版蓝，就是用户看到的"蓝色发光圆环"。
    /// 只有主网格没覆盖的材质（真·独立特效件）才被克隆出来补画，且只画对应的那几个 submesh。
    /// （电塔是另一头：`渲染件=1 粒子系统=0` ⇒ 该版本原版就没有特效子件，"电离特效"属于借用而非接回，见 P20 / E4。）
    /// 绘制时乘的是同一个部件矩阵，所以 P13 修好的同心关系不会被特效层再次带偏。
    ///
    /// 程序化几何有两处必须记住的规矩（都是实测踩出来的）：
    /// 1) 三角形绕序必须与朝外的顶点法线一致 —— (a,b,c)/(a,c,d) 是反的，会让 Cull Back 剔掉外壁、
    ///    管外看永远是个凹槽（相机进管内才看到完整管道），且内壁还拿着朝外的法线算光照 ⇒ 又暗又错乱（P12）。
    /// 2) 把部件网格几何中心对齐到摆位点时，位移要减【完整旋转】乘出来的中心偏移（含 FromToRotation 那一层），
    ///    少乘一层会让部件按朝向偏心，只有朝向恰为 +Y 的那个碰巧对齐 ⇒ 建筑和它的能量环不同心（P13）。
    /// 发光色有两个开关（EGlowColorMode）：随所在恒星变化，或与恒星无关的固定色板。
    /// 分两层不是我们的发明，而是原版戴森壳自己的做法：DysonShell.cs:27/33 加载的就是
    /// "Dyson Sphere/Materials/dyson-shell-unlit-N"，并在 DysonShell.cs:1516-1517 写
    /// _SunColor 与 _DysonEmission（发光色取自 DysonSphere.cs:405-411 的恒星调色板）。
    /// 骨架层必须存在的原因：游戏把恒星当"方向光"用（StarSimulator.cs:453-459），环绕恒星分布的部件
    /// 必然大面积背光，而建筑材质 albedo 本就是灰色金属 —— 靠光照只能得到"灰的明暗"，得不到颜色；
    /// 远景可见性同理：一个足够亮的点对亚像素尺寸最鲁棒，而暗灰色结构件在远处等于隐形。
    /// 实测已确认：远景可见 ✓ 有发光 ✓ 能被星球正确遮挡 ✓（故命中材质是正常深度测试的 unlit，
    /// 也因此它只读 _Color 不读顶点色 —— 色差必须靠"拆网格 + 拆材质实例"表达）。
    /// 【刻意不走半透明】建筑 shader 是 RenderType=Opaque、只有 _AlphaClip(cutout)，骨架命中的
    /// Unlit/Color 也没有 Blend 命令；真透明要换另一支 shader，而且透明队列不写深度会破坏上面那条遮挡。
    /// 查证与结论见 _dev/warpmodel §5.3 末尾 / P11。
    ///
    /// ── 尺寸取值 ──
    /// 基准半径直接采用游戏自己给每颗恒星算出的"最小合法壳层半径"：
    ///     minOrbitRadius = max(star.physicsRadius * 1.5, 4000)   （DysonSphere.cs:413-416）
    /// 这是原版允许戴森壳贴得最近的距离，所以模型既不会穿进星球，也会随恒星大小自适应。
    /// 若该恒星还没有戴森球对象（dysonSpheres[i] 为 null），就按同一公式现算，保证任何时候都有尺度锚点。
    ///
    /// ── 第十五轮 E14：本类由 `WarpArrayModelRenderer` 整体搬迁而来（R34）──
    /// 用户的 `Model/` 架构分工是：**Builder 只在首次读档跑一次**，把"往哪一层加了什么预制件"记成常量表；
    /// **本类承担从模型目录到提交绘制的全部渲染工作**（用户原话"modelRenderer 也可以承担渲染代码的全部工作"）。
    /// 于是 `Render()` 里原先那四段硬编码几何（三重进动环 / 6 座主发生器 / 塔位能量环 / 8 座接收器 + 其能量环）
    /// 改成"遍历 `WarpArrayModelBuilder.layersByLevel[level]`"：每个 `ModelLayer` 自带角速度，
    /// 每个 `ModelEntry` 自带局部位置/姿态/尺寸。其余机器（材质与 PBR 旋钮、MPB 动画喂数据、特效件摘取、
    /// 自研辉光资产、程序化环几何、日志）一行语义都没改 —— 依据与等价推导见
    /// `_dev/warpmodel/warp-array-model.md` P30（名义米 ⇄ ratio、矩阵等价、初始化时机、energyGenCurrentTick）。
    /// 本类是 `partial`：`Model/ModelCatalog.cs` 是它的另一半，负责"枚举 → 一整套模型+参数+动画+辉光"的目录。
    /// </summary>
    [HarmonyPatch(typeof(GameLogic), "Draw")]
    public static partial class ModelRenderer
    {
        // ---------------------------------------------------------------- 配置

        /// <summary>
        /// 演示阶段固定挂在哪颗恒星上（用户早期要求 starId = 0）。玩家不在该星系时不渲染。
        /// ⚠️ E19（R37）起这条**默认不生效**（被 <see cref="FollowLocalStar"/> 短路），保留它就是为了"一行回退到固定星系"。
        /// </summary>
        public const int TargetStarId = 0;

        /// <summary>
        /// E19（R37）裁决②的**临时档**：true ⇒ 画在"玩家当前所在的那个星系"（任何恒星都画，跟着 E19 的自适应倍率缩放）。
        /// 用 static readonly 而不是 const：const true 会让下面那条 `!FollowLocalStar && …` 的分支被判为不可达（CS0162）。
        /// ⚠️ 这不是终态 —— 终态由用户后续给的"判断方案"决定（原文见 <see cref="GameLogicDrawPostfix"/> 里那段注释）。
        /// </summary>
        private static readonly bool FollowLocalStar = true;

        /// <summary>现实视角戴森结构的 Unity 渲染层，与原版壳一致（DysonSphereSegmentRenderer.cs:402）。</summary>
        private const int DysonRenderLayer = 16;

        // ── E18（R37）之后这里**不再有"布点比例"** ──
        // 用户要求："所有 builder 里面使用的 addring/addmodel 之类的里面的坐标、半径等数值，都不要引用 ModelRenderer
        // 里面的固定值"⇒ 原来这 6 个 `Ring/Node/Sub*Ratio` 与下面那 7 个 `*Nominal*` 已全部搬进
        // `WarpArrayModelBuilder` 的"布点数值"一节并**就地删除**（`Node/Sub*Ratio` 四个失去读者，一并删）。
        // 留在渲染器里的这几个**不是布点数值**，而是"渲染器自己算矩阵时要用的关系常数"：
        //   · `NominalBaseRadius` = 名义米 → 世界米的换算常数（`GetAdaptiveScale` 的分母 + 目录里能量环的名义半径）；
        //   · `RingRadiusRatio`   = 发光环与结构环的**半径比基准**（`DrawRingEntry` 里 `GlowRingRadiusRatio / RingRadiusRatio`），
        //     写 1.00 的意思是"结构环就落在换算出来的那个世界半径上"，改它会连带改变发光环的相对位置；
        //   · `RingTubeRatio`     = **默认那一档管径比**，唯一用途是 `ModelCatalog.DefaultRingKey` ⇒ 命中它就复用
        //     重构前那同一张程序化环网格（R26"环逐像素不变"的落实点）。Builder 现在写的是 `AddRing(4000, 200)`，
        //     `200/4000 = 0.05` 恰好落在这个键上 ⇒ 画面不变；把管径改成别的数就会各造一张（本来也该长得不一样）。
        private const float RingRadiusRatio = 1.00f;   // 结构环半径 / 世界基准半径 R
        private const float RingTubeRatio = 0.05f;     // 默认档管径比（= 缓存键，见上面第三条）
        private const int RingMajorSeg = 144;          // 主环大圆分段（R=4000 时约 174 m/段，轮廓不再像多边形）
        private const int RingMinorSeg = 14;           // 主环管截面分段
        private const float RingUvTiles = 144f;        // 主环贴图重复数：2πR/144 ≈ 175 m 一个贴图模块（详见 P10）

        // ───────────────────────────── 第十五轮 E14（R34）：**名义米**尺度体系
        // 新架构里 Builder 只写"我以为这座巨构有多大"的米数（`AddRing(4000, 200)`、`AddModel(..., 800)`），
        // 渲染器负责把它换算成世界米：`world = 名义值 * assemblyScale`，`assemblyScale` 见 <see cref="GetAdaptiveScale"/>。
        // 锚点取 4000 有原文根据：`DysonSphere.cs:413-416` 给每颗恒星算的 minOrbitRadius = max(physicsRadius*1.5, 4000)，
        // 也就是"原版允许戴森壳贴得最近"的那个下限（`:425` 还会 Ceil 到整百米 ⇒ 恒 ≥4000）。
        public const float NominalBaseRadius = 4000f;                       // 名义米体系的"1.00 R"

        // 背光面的环境光强度（乘在恒星光色上）。太空中原版环境光被压到接近黑，
        // 直接用会让背光面死黑一片、看不出体积；这里给一档"被恒星照亮的环境"。
        // 取值来回过一次，留个准绳：S8 为"立体感"从 0.30 压到 0.24，结果第六轮实测反馈"整体还是暗"，
        // ⇒ 回抬到 0.34。对比感交给冷暖分离（见 SunlightTintStrength/AmbientTintStrength）去做，
        //   不要靠压暗环境换对比 —— 黑太空背景本身已经把对比度给够了。
        private const float AmbientGain = 0.34f;
        // 饱和度靠"受光面偏暖、背光面带色板青绿"的**分离**，而不是靠把 _Color 洗成单色（那会重犯 P9）。
        private const float SunlightTintStrength = 0.22f;   // 主光往色板偏移的量（小，保持该恒星的色温主导）
        private const float AmbientTintStrength = 0.62f;    // 暗部/过渡补光往色板偏移的量（大，负责"有颜色"）

        // ─────────────────────────────────────────── 自发光能量层（修 P4/P5/P8）
        // 结构层用游戏原生建筑材质负责近看细节；发光层用 unlit 材质负责"远看看得见 + 有颜色"。
        // 这不是我们发明的观感，而是原版戴森壳自己的做法：DysonShell.cs:27/33 加载的正是
        // "Dyson Sphere/Materials/dyson-shell-unlit-N"，并在 DysonShell.cs:1516-1517 写
        // _SunColor 与 _DysonEmission（发光色取自 DysonSphere.cs:405-411 的恒星调色板）。
        // 之所以必须这么做：建筑材质 albedo 本就是灰色金属，而游戏把恒星当方向光用
        // （StarSimulator.cs:453-459），环绕分布的部件必然大面积背光 —— 靠光照调不出颜色，实测即"还是灰的"。
        // 第三轮实测已确认这条路成立：远景可见 ✓ 有发光 ✓ 能被星球正确遮挡 ✓（方案按要求保留备用）。
        private static readonly bool showStructure = true;    // 关掉可单独评判发光层观感
        // R32（第十三轮）："暂时关闭我们自己的辉光特效，有点太亮纯色也不好看" ⇒ 自研的 unlit 发光环/光核/能量环整层关掉。
        // ⚠️ 这条不是免费改动：R8"远景必须可见"当初正是靠这层达成的（第三轮实测：只靠受光 ⇒ "离得远什么都看不到"）。
        //    所以 §7.10 第 4 条专门要求在远/中/近三档各看一眼；若远景失守，正解是抬官方 _EmissionMultiplier
        //    （见 NodeEmissionMulOverride），而不是把这层纯色辉光加回来。
        // ✅ 第十四轮实测：用户回报"远景目前没问题" ⇒ 这个风险**没有发生**，R8 在"只有官方自发光 + 环的 PBR 结构色"
        //    的条件下仍然成立；不要再为了"远景"把这层纯色辉光加回来（R32 已明确它太亮、纯色不好看）。
        private static readonly bool showGlow = false;
        // R32（第十三轮）："删掉所有的 rayreceiver 模型，不是很好，不要了" ⇒ 8 座次级接收器整层不画：
        // 本体、被摘成克隆材质的 ray-receiver-eff / 玻璃罩特效件、以及接收器位的能量环全部停掉。
        // ── 第十五轮 E14（R34）：这个开关**退役**了 ──
        // "画不画接收器"从此不归渲染器管，而归 **Builder**：它没往任何层里放 RayReceiver 条目，画面就没有接收器
        // （当前 `WarpArrayModelBuilder` 正是如此 ⇒ R32 的语义原样保持；想放回就在某个层里 `AddModel(RayReceiver, …)`）。
        // 渲染器只保留一个由层级反推出来的事实 `needReceiver`，用来决定"要不要费事去装载那只 prefab"。
        // §7.10 / §7.11 那张"一行回退表"里 `showSubStructure` 一项从此改读作"在 Builder 里加一层"。
        // 第四轮实测判定这是**回归**：外扩的纯色 unlit 壳从任何角度都完整包住本体，
        // 把 _MainTex/_NormalTex/_MS_Tex 全遮住 ⇒ "发光处一个色、本体看不出结构、黑与光直接交界"（P9）。
        // 代码留着只为 A/B 对比，默认关闭；正解见下面的 PBR 旋钮覆写。
        private static readonly bool showShellGlow = false;   // 建筑本体的发光外壳（P8 的老解法）；关掉即退回"只有环发光"

        // ─────────────────────────── 建筑原生特效件（修 P16 / 落地 R20：接收光效、电离线圈）
        // 原版建筑的"特效"不是粒子，而是挂在模型 prefab 里的【附加子网格 + 专用特效材质】：
        // 接收器的 ray-receiver-eff 用的是 "VF Shaders/Forward/Unlit Additive Ray Receiver Gravity"，
        // 电塔则带 meshdatas/tesla-coil 线圈网格。我们此前只取 prefabDesc.mesh 那一张主网格，
        // 这些子件一个都没进来 ⇒ "接收器没有接受光效、电塔没有电离特效"的真因是那层网格根本没画。
        private static readonly bool showNativeEffects = true;
        private static readonly float nativeEffectTintStrength = 0.55f;   // 特效色往色板偏移的量；0=完全原色，1=完全色板色
        // 特效材质也带 _ENABLE_VFINST（逐实例）。本体材质同样带着它而我们画得位置正确，
        // 所以默认不动它；万一特效件出现"跑位/暴涨"，把这行改 true（克隆体上 DisableKeyword）即可，不必改架构。
        private static readonly bool nativeEffectKillVfinst = false;

        // ─────────────────────────────────── 结构层的官方 PBR 旋钮覆写（修 P9 / 落地 R13）
        // 三支 shader 的属性并集/差集已逐个核对（_dev/warpmodel §5.3）：
        //   环   = VF Shaders/Forward/PBR Standard              有 _EmissionMask、_EmissionSwitch、_EmissionUsePower
        //   电塔 = VF Shaders/Forward/PBR Standard Black Vertex Rotate  有 _EmissionSwitch/_EmissionUsePower，无 _EmissionMask/_EmColor
        //   接收器= VF Shaders/Forward/PBR Ray Receiver          有 _EmColor1/_EmColor2，无 _EmissionMask/_EmissionSwitch/_EmissionUsePower
        // 三者共同声明：_Color/_SpecularColor/_AlbedoMultiplier/_NormalMultiplier/_SmoothMultiplier/_EmissionMultiplier。
        // 未声明的名字赋值是**静默空操作**（材质遗留字段的旧坑现场复现）⇒ 候选名一次全铺，并有探测日志自证。
        private static readonly bool useVanillaPbrKnobs = true;   // false ⇒ 退回 S2 的"只覆写光照"观感，便于隔离验证
        private const float StructureAlbedoMultiplier = 3.6f;     // 漫反射倍率：材质里存的是 1.3~1.5，官方机甲曲速敢用 4~8；实测"还是暗"故从 2.4 抬上来
        private const float StructureNormalMultiplier = 1.35f;    // 法线倍率：放大法线贴图的面差
        private const float StructureSmoothMultiplier = 1.6f;     // 高光倍率：随视角变化的反光 = 立体感
        private const float StructureSpecularGain = 1.2f;         // 高光色相对主光色的增益
        private const float StructureTintStrength = 0.38f;        // _Color 掺色比例；拉满会把贴图层次洗成单色（= P9 观感），只肯小幅抬
        private const float StructureEmissionMultiplier = 3.5f;   // 自发光倍率：游戏默认 5.5、材质存 10 ⇒ 仍低于默认，守住 R14"不滥用"
        private const float StructureEmissionTint = 0.70f;        // 自发光染色强度（发光纹路带多少色板色）

        // 自发光门控的取值（R18 / P14 / R22 / P19 / **P21 定案**）。这里有两个**语义不同**的开关：
        //   _EmissionUsePower = "发光要不要读供电数据"；_EmissionSwitch = "这个发光开关开不开"。
        // 判据只能问运行时（`.mat` 的序列化字段会骗人 —— 未声明的属性是材质里的遗留值，见 P19）：
        //   环 / 电塔 两个门都声明；接收器本体(PBR Ray Receiver) **两个都不声明**；
        //   additive 特效件(ray-receiver-eff) **只声明 _EmissionSwitch**。
        // P21 的两轮 A/B（同一份代码，只差 _EmissionSwitch 写不写）：
        //   写 0/0 ⇒ 电塔红点、接收侧发光、主环有颜色；不写 Switch ⇒ "红点蓝圈全没、暗暗的没颜色"。
        // ⇒ **结构层必须写 _EmissionSwitch，且写 0**（0 不是"没电"，是"不查游戏状态"⇒ 恒亮；
        //    我们画的是假建筑，没有 ObjectRenderer.cs:438 那块 `_InstBuffer` 逐实例供电数组，
        //    任何"交给游戏状态决定"的取值都读到空 ⇒ 灭）。上一轮"永久移出覆写名单"的结论作废。
        // 两个门现在**各自独立给值**（用户 R24："还能继续给别的数？"⇒ 能，见下面三行）。
        private enum EGateMode { Native, IgnorePower, FollowPower }
        private static readonly EGateMode emissionGateMode = EGateMode.IgnorePower;
        private const float GateValueIgnorePower = 0f;    // 写 0：不看供电 ⇒ 恒亮
        private const float GateValueFollowPower = 1f;    // 写 1：交供电数据决定（官方本体的配法，假建筑大概率不亮）
        // 结构层（游戏材质 + MPB）该给 _EmissionSwitch 的数。0 = 实测能亮的那一档；
        // 想验证 shader 内部是 ">0.5 判定"还是"乘法权重"，把它改成 0.5f 再看一次即可（P21 第 4 条 / §7.7）。
        private const float StructureEmissionSwitchValue = 0f;
        // 特效克隆材质（我们自己 new 出来的材质实例，不碰游戏资产）该给 _EmissionSwitch 的数。
        // 单独一行的意义：特效层从此有**只作用于它自己**的抓手，可以与结构层给不同值（P22）。
        private const float NativeEffectEmissionSwitchValue = 0f;

        // ─────────────────────────────────── 第十轮 E8：贴图/材质/渲染等级类数值（R27）
        // 这些名字全部来自 AssetRipper 导出的 Properties 原文（P24），不是猜的；**初值一律等于官方值**，
        // 而 ApplyPbrKnobs 只在"与官方值不同"时才写入 ⇒ 现在画面与第十轮那张"漂亮但不晃眼"完全一致，
        // 想动哪个就只动那一行（每个都注明官方值与往哪边调的效果）。
        private const float StructureMetallicMultiplier = 1f;     // 官方 1：金属反射强度。抬到 1.5~2 = 更"金属质感"、高光更硬；压到 0.5 = 更哑光
        private const float StructureEmissionMipBias = -2f;       // 官方 -2（仅环）：自发光贴图取样的 mip 偏移。往 0 走 = 远景取更平均的那级 mip ⇒ 光条更柔更连续、不闪；往 -3 走 = 更锐但远景易碎成噪点
        private const float StructureNormalMipBias = -1f;         // 官方 -1（仅环）：法线贴图 mip 偏移，远景各向异性感
        private const float StructureEmissionJitter = 0f;         // 官方 0（环与电塔）：自发光抖动倍率，>0 会让发光纹理随时间游走（配合 _EmissionJitterTex）
        private const float StructureRotateSpeed = 1f;            // 官方 1（仅电塔）：顶点/贴图旋转速度，电塔"活"起来的那一档
        // 用 static readonly 而非 const：const 会让下面的 `>= 0f` 被常量折叠成 false，编译器判"不可达代码"(CS0162)
        private static readonly float StructureCullMode = -1f;      // 官方 2 = 只剔背面；0 = 双面渲染（想让环管内壁也可见才用，代价是内壁拿朝外法线算光照）；-1 = 不覆写
        // 逐角色自发光增益（在 StructureEmissionMultiplier 之上再乘）：官方默认 5.5，我们压到 3.5 是刻意的（R14）。
        // 想让某一个部件更亮就只抬它那一档，别再全局加。
        private const float RingEmissionGain = 1f;
        private const float NodeEmissionGain = 1f;
        private const float SubEmissionGain = 1f;
        // 特效克隆材质（glass 罩 / additive 光效）的数值。增益是**乘在官方值上**（先 GetFloat 读官方，再乘），
        // 不是凭空赋数；1 = 与官方完全一致。
        private static readonly float NativeEffectGain = 1f;                 // 同时乘到 _Multiplier/_AlphaMultiplier/_AuroraMultiplier
        private static readonly Vector4 NativeEffectUvScroll = Vector4.zero; // 非零 ⇒ 覆写 _UVSpeed，让极光/折射贴图流动（x,y 速度）
        private static readonly float NativeEffectSideFade = -1f;                   // 官方范围 0~2 的"侧面消隐"；-1 = 不覆写（同样用 readonly 避开 CS0162）
        private static readonly Color NativeEffectTintColor = new Color(1f, 1f, 1f, 0f);   // a<=0 ⇒ 不覆写 glass 的 _TintColor；给了色就按它染那层罩

        // ─────────────────────────────────── 第十二轮 E9（R28 / R29）：无线输电塔 + 官方"有电→动画"数据
        // 这一组的每一行都注明它抄的是哪个原文（依据全在 §4 的 P25~P28），不再有我上一轮的推测性判读。
        // ⚠️ P28 第 3 条是**未证实项**（Graphics.DrawMesh 是否真的消费 MPB 里的 buffer；游戏全项目没有一处 MPB.SetBuffer 先例）
        //    ⇒ 所以本轮所有动画动作都收在 feedAnimBuffer 这一个总闸后面：实测异常只关它，不牵连换模型与颜色改动。
        private static readonly bool preferAnimatedNodeTower = true;    // false ⇒ 退回 tesla-tower-1（本版本它的名字是"电力感应塔"，P27）
        // P27：本地化原文（Locale\2052\base.txt:4115/4128、prototype.txt:433/624）证明"电力感应塔(短距离)"与
        // "无线输电塔(长距离+给机甲充电)"是**两个建筑**，而"特斯拉塔"这个词在当前本地化里零命中。
        // 游戏自己给每条 ModelProto 挂了 displayName（ModelProto.cs:10-20 ⇒ sid = SID.Translate()，Preload():70），
        // 所以**用游戏自己的名字去 LDB.models 里反查模型**，命中即打日志；查不到才退回 prefabPath 候选链。
        private const string NodeModelName = "无线输电塔";
        private static readonly bool feedAnimBuffer = true;             // false ⇒ 四个 buffer 名字一个都不绑
        // AnimData 数组的行数。官方靠 instanceID→_IdBuffer→objId 定位"自己这一栋"（P26④），我们没有这一环，
        // 于是把**每一行都填同一条数据** ⇒ 取到哪一行结果都一样，把"索引未知"变成"索引无关"。
        private const int AnimBufferRows = 8;
        private const float AnimPower = 1f;         // 原文 _power = networkServes[networkId]（0~1 供电满足率）；给 0 = "没电" ⇒ time 倒退归零（AnimData.cs:134-138）
        private const uint AnimTargetState = 2u;    // 原文 (requiredEnergy - idleEnergyPerTick > 0) ? 2u : 1u（PowerSystem.cs:2139/2146）
        private static readonly bool animUseStep2 = false;   // true ⇒ 改用 PowerSystem.cs:2169 那条**纯电力节点**公式（到 working_length 回绕 = 循环）；默认走 :2146 的 charger 公式（爬到顶就停末帧）
        // ── R30（第十三轮实测）：塔会"循环播放从有电到没电的收起动画"，要求改成**持续的展开状态停住** ──
        // 根因有原文（P29 第 1 条，AnimData.cs:117-130 全函数）：power=1 时 `time` 爬到 `working_length-0.001` 就 clamp 住不再变，
        // `state/power/working_length` 也全是常量 ⇒ 我们上传的 5 个 float 里**只有 `prepare_length` 带周期**
        // （charger 分支把它挪作"电量累加器"，每 tick 加 power*dt、超过 30 就减 30 ⇒ 周期正好 30 秒），
        // 而它在 AnimData 的通用语义里本来是**展开段长度**（StepPoweredClamped:94 与 Step2:175/183/195 都拿它当总长/分段边界）。
        // ⇒ 正解是把这个唯一的周期量**归零并保持**，而不是调步长或换公式：定格时 time 直接给到末帧，StepAnim 干脆不推进。
        // ✅ 第十四轮实测通过（"塔不动了，动作正确"+"塔的电流特效状态没错的，很好"）⇒ 本条排除法结论由实测确认，保持 true。
        private static readonly bool animHoldExpanded = true;
        private const float AnimStepDt = 1f / 60f;  // 原文 PowerSystem.cs:1550 `float num = 0.016666668f;` —— 游戏的动画步长是逻辑 tick，不是帧时间
        private const float AnimStepSpeed = 0.4f;   // 原文 Step2(…, 0.4f) 的 _speed（PowerSystem.cs:2169）
        // P25：同一支属性对不同建筑的作用是**相反**的 —— _Color 是电力感应塔唯一的颜色来源（只能轻掺），
        // _EmissionMask 是无线输电塔的颜色所在（官方青，最好一个字都不写）。环与接收器维持第十一轮的值 ⇒ R26 基准不动。
        private const float NodeColorTintStrength = -1f;      // 塔(_Color) 掺色强度；-1 ⇒ 不覆写。R31/P29 第 2 条：官方 _Color 就是**纯白 (1,1,1,1)**，我们上轮的 0.15 掺色本身就多写了
        private const float NodeEmissionMaskTint = -1f;       // 塔(_EmissionMask) 掺色强度；-1 ⇒ **保留官方青色**（space-2 为 (0.552,1,0.851)，其余塔 (0.596,0.957,0.933)）
        // ── R31（第十三轮实测）："电流特效有了，但贴图颜色还是没有" ──
        // P29 第 2 条把 `.mat` 原文与我们的覆写逐条对齐后，结论很难看：**上一轮只放开了 `_EmissionMask` 一个字，
        // 剩下四个乘数仍在全局覆写**，而它们恰好是把颜色洗掉的那四个（官方值 → 我们写的值）：
        //   _AlbedoMultiplier 1 → 3.6（漫反射过曝 ⇒ 洗白，**主因**）  _EmissionMultiplier 7 → 3.5（自发光压掉一半）
        //   _SmoothMultiplier 1 → 1.6   _NormalMultiplier 1 → 1.35   _Color (1,1,1) → 掺 0.15 色板
        // 所以修法是"**这一整包对塔都不写**"，让美术调过的序列化值原样生效，而不是我再猜一组更好看的数。
        // 位在这里而不是写死在 ApplyPbrKnobs 里：环与接收器仍走全覆写 ⇒ R26 定案的基准逐像素不变。
        // ⚠️ 第十四轮实测：**这一包交回官方后，塔的颜色仍然没有**（用户"能接受，留到后续再说"）
        //    ⇒ 上面那四个覆写是事实、但只是**必要不充分**，"恢复官方值"并不等于"颜色会出现"。
        //    这一位**保持 true**（它至少不再洗白，且回退成 false 只会更糟）；真正的根因待 E13 取证：
        //    ①实际参与绘制的是不是 lod0 这支材质 ②`_ENABLE_ANIM` keyword 运行时是否真开着（MPB 没有 EnableKeyword）
        //    ③官方 Switch/UsePower=1/1 那条"按电决定发光"路径（见 nodeUseOfficialEmissionGate）④与用户对齐"贴图颜色"的语义。
        //    未取证前不要再猜数值。依据与验收全文见 `_dev/warpmodel/warp-array-model.md` P29 第 2 条 / §7.11。
        private static readonly bool NodeKeepOfficialMaterialValues = true;   // false ⇒ 退回第十二轮那套"洗白"覆写（A/B 用）
        // 颜色交回官方后如果"对了但偏暗"（官方 albedo 只有 1，远低于我们原来的 3.6），**只动这一行**：
        // -1 ⇒ 连 `_EmissionMultiplier` 也不写（用官方 7 / ground 1.5）；给具体数字就只对塔写那一档，全局 3.5 不受影响。
        private static readonly float NodeEmissionMulOverride = -1f;
        // P21 实测过"交出 `_EmissionSwitch` 就整体变暗"（那是电塔+环那批材质），所以门控默认仍写 0/0。
        // 但这一档现在值得重测：官方 space 塔的 `_EmissionSwitch/_EmissionUsePower` 序列化是 **1/1**（ground 是 1/0），
        // 而我们这一轮已经真的把 `power=1` 喂进 `_AnimBuffer` 了 ⇒ "按电决定发光"那条官方路径第一次有数据源。
        private static readonly bool nodeUseOfficialEmissionGate = false;     // true ⇒ 塔也不写这两个门

        // 发光色来源开关（R12）：两个方案都保留，翻这一行即可切换。
        // 用 static readonly 而非 const：避免常量折叠把另一支判为不可达代码（CS0162）。
        private enum EGlowColorMode { StarTinted, FixedPalette }
        private static readonly EGlowColorMode glowColorMode = EGlowColorMode.FixedPalette;

        // 固定色板：偏绿的蓝绿（teal → aqua），与所在恒星无关。四个槽是"颜色组合"而非全场单色。
        private static readonly Color FixedRingColor = new Color(0.22f, 0.92f, 0.72f, 1f);    // 发光环体
        private static readonly Color FixedBulbColor = new Color(0.45f, 1.00f, 0.82f, 1f);   // 环上光核：更亮更偏绿
        private static readonly Color FixedHaloColor = new Color(0.16f, 0.82f, 0.66f, 1f);   // 能量环：压暗作底
        private static readonly Color FixedShellColor = new Color(0.30f, 0.88f, 0.76f, 1f);  // 建筑本体染色（S8 起驱动 PBR 覆写，不再是纯色外壳）

        private const float GlowRingRadiusRatio = 1.06f;      // 发光环半径 / R：略大于结构环，两层不互相吞
        // R14"收紧、不要滥用"：管径与光核都收小一档。光核仍是远景可见性的主力，所以收得最保守。
        private const float GlowTubeRatio = 0.013f;           // 发光环管径：细亮线，远景读作骨架
        private const float GlowBulbRatio = 0.038f;           // 环上光核半径 / 环半径 —— 远景可见性的主力
        private const int GlowBulbCount = 16;                 // 每条发光环上的光核数
        private const float GlowNodeHaloRatio = 0.30f;        // 主发生器能量环半径 / R（塔身 0.20R，环套在外面不遮挡）
        private const float GlowSubHaloRatio = 0.19f;         // 接收器能量环半径 / R（罩身 0.14R）
        private const float GlowHaloTubeRatio = 0.10f;        // 能量环管径 / 环半径
        // 发光色增益：第三轮实测"太亮了"，从 2.0 降到 1.0。降曝不伤远景可见性（2.0 已证可见），
        // 1.0 相对纯黑太空依旧对比拉满，只是不再糊成一片、也不再狂吃 bloom。仍嫌亮就继续往 0.7/0.5 试。
        private const float EmissiveGain = 1.0f;
        // 外壳相对建筑本体的外扩倍率：必须 >1 才能避免与本体 Z-fighting；
        // 抬太高会把机械细节糊成纯色剪影（观感判读见 _dev/warpmodel §7.2）。
        private const float ShellInflate = 1.012f;


        /// <summary>true 则改用游戏自带的 Resources/models/Torus 作为环几何（观感偏粗，留作对比开关）。
        /// 用 static readonly 而非 const：避免编译器常量折叠后把分支内代码判为不可达。</summary>
        private static readonly bool useVanillaTorus = false;

        // 角速度（弧度/秒）。这里**只剩渲染器自己那一个**：
        // · `RingTiltPrecessSpeed` 是"整层共用的极轴摆动"，它每帧前乘在层旋转之外（`:660` 那条 `frame`），
        //   不属于任何一层的 `rotateSpeed` ⇒ 既然不是 Builder 的输入，就继续留在渲染器（E18 / R37 的边界正是这条）。
        // · 原来同处的 `SpinSpeed / RingTiltBase / RingTiltStep / RingSpeedMul` 四个**已搬进 WarpArrayModelBuilder 并删除**，
        //   因为它们是"这座巨构转多快、三环各倾多少度"的**布点/布速事实**，不是渲染机器的一部分。
        private const float RingTiltPrecessSpeed = 0.0035f; // 整体极轴摆动（全层共用，仍由渲染器每帧算一次）

        // 用作"游戏内模型"来源的 prefab 资源名后缀（按 PrefabPath 尾部匹配，不受语言影响）。
        // 每个角色都给了候选链：主名是按 AssetRipper 导出的 Resources/entities/prefabs 文件名取的，
        // 万一该版本的 PrefabPath 尾部有出入，也会退到同类造型而不是"画不出东西"。
        private static readonly string[] RingMatKeys = { "hadron-collider", "fusion-reactor", "accumulator" }; // 对撞机=环状巨构，其材质最贴主环
        // 第九轮 R25/P23：用户指出"二级电塔游戏本身有模型（无线输电塔）"，查证成立 —— prefab 目录里有
        // ground-node-tower-1/-2 与 space-node-tower-1/-2，上一轮"没有第二档"的结论已撤回。
        // 第十二轮 E9（R29 / P25 / P27）换序：**node-tower 系提到 tesla-tower-1 之前**。
        //   理由是 P25 的原文对照：塔的彩色在 _EmissionMask（官方青），而 tesla-tower-1 那支 shader 压根不声明这个属性、
        //   它唯一的颜色就是 _Color=(1,0.429,0.429) —— 正是我们结构层在覆写的那一支 ⇒ "太空里没颜色"是我们自己洗掉的。
        //   注意 P27：tesla-tower-1 在当前本地化里叫**电力感应塔**（旧称特斯拉塔），与用户要的**无线输电塔**是两个建筑，
        //   所以首选其实是 ResolveAssets 里按 displayName 的反查（NodeModelName），本链只是本地化查不到时的兜底。
        private static readonly string[] NodeKeys = { "space-node-tower-2", "space-node-tower-1", "ground-node-tower-2", "ground-node-tower-1", "tesla-tower-1", "lighthouse" };   // 仅在按显示名反查失败时兜底；这些英文名与"无线输电塔"的对应关系**未证实**，建筑身份一律以 displayName 反查 + 日志为准（P27/R28）
        private static readonly string[] SubKeys = { "ray-receiver", "orbital-substation" };                    // 射线接收器：造型本就是"对着恒星取能"
        // 模型清单日志的筛选词（R25：上一轮漏了整族 node-tower，导致"游戏里没有"的误判）。
        // 宁可多打几十行也不要再出现"我的关键词没覆盖 ⇒ 误判游戏没有"这种错误。
        private static readonly string[] ModelDumpKeys = { "tesla", "node-tower", "node", "tower", "relay", "ray-receiver", "charging", "lighthouse", "hadron", "reactor", "accumulator", "orbital" };

        // ---------------------------------------------------------------- 状态

        private struct Part
        {
            public Mesh mesh;
            public Material[] mats;
            public Material[] drawMats;   // 结构层真正拿去画的数组：被摘给特效克隆体的那些位置是 null（P22）
            public Vector3 center;      // 网格几何中心，用于把部件摆正
            public float norm;          // 归一化尺度：模型最大外延
            public string hit;          // 实际命中的资源名，只为日志好排查
            public EffectPiece[] pieces;   // 该模型自带的原生特效子件（P16）
            public string pieceNote;       // 采集结论摘要，只为"组装完成"那行日志好判读（P18）
            // ── E9（P26 补强④）：顶点动画需要的两样东西，都在 PrefabDesc 上，官方也是从这两处取的 ──
            public VertaBuffer verta;      // = lodVertas[0]，与 lodMeshes[0] 同级配对（ObjectRenderer.cs:54 的 new BatchRenderer(lodMeshes[j], …, lodVertas[j], …)）；null = 该模型没有顶点动画数据
            public float animPrepare;      // desc.anim_prepare_length（PlanetFactory.cs:1369 建 AnimData 初值的来源；无 AnimDesc 组件时 PrefabDesc.cs:702-703 给 0/1）
            public float animWorking;      // desc.anim_working_length
            public string displayName;     // 游戏自己的模型名（ModelProto.displayName，P27）——日志用它对号，不靠我猜中文
        }

        /// <summary>
        /// 建筑原生特效子件（P16）：网格 + 我们自用的材质克隆 + 相对【主网格所在空间】的局部矩阵。
        /// 之所以记局部矩阵而不是简单复用部件矩阵，是因为特效件在 prefab 里是带自身偏移/旋转的子物体；
        /// 而部件矩阵已经把主网格摆在模型局部原点，所以必须以主网格的 transform 为基准求相对矩阵，
        /// 这样 piece.local 与我们的 partMatrix 天然同空间，不需要任何额外坐标换算。
        /// </summary>
        private sealed class EffectPiece
        {
            public Mesh mesh;
            public Material[] mats;     // 克隆出来的自用材质（绝不改游戏材质）
            public int[] subs;          // 与 mats 一一对应：该材质在原网格里的 submesh 索引（只画主网格没覆盖的那几项，P18）
            public Color[] baseColors;  // 各材质在原游戏里的 _Color，染色以此为基准而非白色
            public Matrix4x4 local;
            public string tag;          // 只为日志：网格名 / shader 实名 / 本体还是特效
        }

        private static bool resolved;
        private static bool resolveFailedLogged;
        private static bool wrongStarLogged;

        private static Part ringPart;       // 主环：默认程序化几何（可切到游戏自带 models/Torus）
        private static Material[] ringMats; // 环的材质取自对撞机，保证整组造型是"游戏原生皮肤"
        private static Part nodePart;
        private static Part subPart;
        /// <summary>E14（R34）：Builder 建好的层里**有没有**任何一条 RayReceiver 条目 ⇒ 决定要不要去装载那只 prefab。
        /// 取代旧的 `showSubStructure` 开关：画不画接收器现在是"模型创建"侧的事，不再藏在渲染器里。</summary>
        private static bool needReceiver;

        // ── E9：喂给 shader 的动画数据（P26 的②③段）。stride 直接用原文常量 AnimData.dataLen = 20，
        //    与 FactoryModel.cs:534 `new ComputeBuffer(num, 20, ComputeBufferType.Default)` 逐字节一致。
        private static ComputeBuffer animBuffer;
        private static AnimData[] animRows;         // AnimBufferRows 行**同值**（把"取哪一行"变成无关）
        private static VertaBuffer animVerta;       // 命中模型的 lodVertas[0]，null ⇒ 只喂 _AnimBuffer 不喂 _VertaBuffer
        private static bool animLogged;
        private static Mesh animSource;             // animRows 是为哪张网格建的：换塔（重新解析资产）时必须重建，否则 prepare/working 是旧模型的
        private static float animHoldTime;          // 展开定格写进 time 的那个值（末帧 = working_length - 0.001，原文 AnimData.cs:118），日志要自证用
        private static bool nameLogged;             // "按显示名命中/未命中"各说一次即可，避免每次换恒星刷屏

        // ── E9：逐角色的 PBR 覆写参数（P25 的结论：同一属性对不同建筑作用是反的）──
        //    环与接收器的三个值 = 第十一轮原值 ⇒ R26 定案的基准逐像素不变；只有塔走新档。
        private struct RoleKnobs
        {
            public float emissionGain;       // 乘在 StructureEmissionMultiplier 上的逐角色增益（R27）
            public float colorTint;          // _Color 掺色强度；<0 ⇒ 不覆写（保留建筑自己的 albedo 染色）
            public float emissionMaskTint;   // _EmissionMask 掺色强度；<0 ⇒ 不覆写（保留建筑官方叠底色）
            public bool bindAnim;            // 这一次绘制要不要绑 _AnimBuffer/_VertaBuffer（只有塔有动画数据可言）
            // E12②（R31）：true ⇒ **整个"上色/倍率包"都不写**，让 `.mat` 序列化值原样生效。
            // 依据是 P29 第 2 条那张对照表：官方 _AlbedoMultiplier/_EmissionMultiplier/_SmoothMultiplier/_NormalMultiplier/_Color
            // 分别是 1 / 7 / 1 / 1 / (1,1,1)，而我们全局写的是 3.6 / 3.5 / 1.6 / 1.35 / 掺 0.15 ⇒ 颜色被自己洗掉。
            // 受影响的属性：_AlbedoMultiplier、_NormalMultiplier、_SmoothMultiplier、_SpecularColor、_Color、
            //               _EmissionMultiplier、_EmissionMask、_EmColor1/_EmColor2。
            // 不受影响的：_Global_*（光照那组仍然要写，否则太空里背光面死黑 —— P1 的老结论）与 SetIfTuned 那几档（初值=官方，本来就不写）。
            public bool keepOfficial;
            public float emissionMulOverride;  // 仅当 keepOfficial 时生效：-1 ⇒ 连 _EmissionMultiplier 也不写；给了数就**只对这一个角色**写
            public bool keepOfficialGate;      // 仅当 keepOfficial 时生效：true ⇒ _EmissionSwitch/_EmissionUsePower 也交回官方（P21 那轮实测过风险，默认 false）
        }
        private static readonly RoleKnobs RingKnobs = new RoleKnobs { emissionGain = RingEmissionGain, colorTint = StructureTintStrength, emissionMaskTint = StructureEmissionTint, bindAnim = false, keepOfficial = false };
        private static readonly RoleKnobs NodeKnobs = new RoleKnobs { emissionGain = NodeEmissionGain, colorTint = NodeColorTintStrength, emissionMaskTint = NodeEmissionMaskTint, bindAnim = true, keepOfficial = NodeKeepOfficialMaterialValues, emissionMulOverride = NodeEmissionMulOverride, keepOfficialGate = nodeUseOfficialEmissionGate };
        private static readonly RoleKnobs SubKnobs = new RoleKnobs { emissionGain = SubEmissionGain, colorTint = StructureTintStrength, emissionMaskTint = StructureEmissionTint, bindAnim = false, keepOfficial = false };

        private static Mesh proceduralRing;         // 程序化环（兜底，静态持有避免被卸载）
        private static Quaternion ringBaseRot = Quaternion.identity; // 游戏 Torus 网格位于 XY 平面，需转 90°

        // ── 逐部件光照覆写 ──
        // 源码事实：游戏把恒星当"方向光"用 —— StarSimulator.LateUpdate:453-459 把 _Global_SunDir 设成
        // 恒星 transform.forward（一个固定方向），非白色模式下全程只有一个恒定光照方向。
        // 这对行星表面的建筑没问题（离恒星足够远，方向光近似成立），但我们的部件是【环绕恒星分布】的：
        // 方向光近似下，只有恰好朝向那个固定方向的一面亮，其余面全部背光 —— 这就是"纯黑灰、不反射恒星光"的成因。
        // 正解是把光还原成"从恒星中心射向该部件"：MaterialPropertyBlock 的属性优先级高于同名全局变量，
        // 因此可逐次 DrawMesh 覆写 _Global_SunDir，只影响我们的绘制，不动任何全局状态（零副作用）。
        private static readonly int SunDirId = Shader.PropertyToID("_Global_SunDir");
        private static readonly int SunLightColorId = Shader.PropertyToID("_Global_SunLightColor");
        private static readonly int SunsetColor0Id = Shader.PropertyToID("_Global_SunsetColor0");
        private static readonly int SunsetColor1Id = Shader.PropertyToID("_Global_SunsetColor1");
        private static readonly int SunsetColor2Id = Shader.PropertyToID("_Global_SunsetColor2");
        private static readonly int AmbientColor0Id = Shader.PropertyToID("_Global_AmbientColor0");
        private static readonly int AmbientColor1Id = Shader.PropertyToID("_Global_AmbientColor1");
        private static readonly int AmbientColor2Id = Shader.PropertyToID("_Global_AmbientColor2");

        // ── 官方 PBR / 自发光旋钮的属性名（三支 shader 各认其中一部分，未声明的赋值是空操作）──
        // 注意 _Color/_EmColor1/_EmColor2/_EmissionMask 在 Properties 里声明为 Vector 而非 Color；
        // 二者在 shader 属性块里都是 float4，SetColor 照样写进去，不必担心类型。
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SpecularColorId = Shader.PropertyToID("_SpecularColor");
        private static readonly int AlbedoMulId = Shader.PropertyToID("_AlbedoMultiplier");
        private static readonly int NormalMulId = Shader.PropertyToID("_NormalMultiplier");
        private static readonly int SmoothMulId = Shader.PropertyToID("_SmoothMultiplier");
        private static readonly int EmissionMulId = Shader.PropertyToID("_EmissionMultiplier");
        private static readonly int EmissionMaskId = Shader.PropertyToID("_EmissionMask");   // PBR Standard(环) 认这个
        private static readonly int EmColor1Id = Shader.PropertyToID("_EmColor1");           // PBR Ray Receiver(接收器) 认这两个
        private static readonly int EmColor2Id = Shader.PropertyToID("_EmColor2");
        private static readonly int EmissionUsePowerId = Shader.PropertyToID("_EmissionUsePower"); // "用供电数据决定发光"门控
        // "发光开关开不开"门控。P19 曾把它判成"绝对不许覆写"，第九轮 P21 用两轮 A/B 推翻：
        // 结构层不写它 ⇒ 自发光整体关闭（红点、蓝圈、环的颜色一起没）。现在它按**载体**分别给值：
        //   游戏材质（结构层 MPB）→ StructureEmissionSwitchValue；自用克隆材质 → NativeEffectEmissionSwitchValue。
        // 注意 MPB 对"材质未声明的属性"是静默空操作，所以这次重新写入依然伤不到接收器本体那支两个门都不声明的材质。
        private static readonly int EmissionSwitchId = Shader.PropertyToID("_EmissionSwitch");
        // ── 第十轮 E8：从 Properties 原文（P24）里补上的真实旋钮，此前探测名单太窄所以一直没看见 ──
        private static readonly int MetallicMulId = Shader.PropertyToID("_MetallicMultiplier");       // 金属倍率(三支本体都声明, 官方 1)
        private static readonly int EmissionMipBiasId = Shader.PropertyToID("_EmissionMipBias");       // 自发光Mip偏移(仅 PBR Standard, 官方 -2)
        private static readonly int NormalMipBiasId = Shader.PropertyToID("_NormalMipBias");           // 法线Mip偏移(仅 PBR Standard, 官方 -1)
        private static readonly int EmissionJitterId = Shader.PropertyToID("_EmissionJitter");         // 自发光抖动倍率(环与电塔, 官方 0)
        private static readonly int RotateSpeedId = Shader.PropertyToID("_RotateSpeed");               // 贴图旋转速度(仅电塔, 官方 1)
        private static readonly int CullModeId = Shader.PropertyToID("_CullMode");                     // 剔除模式(环与电塔, 官方 2=背面)
        // 特效层（glass / additive）自己的一套数值 —— 与本体那套完全不重叠，只能作用在克隆材质上
        private static readonly int MultiplierId = Shader.PropertyToID("_Multiplier");
        private static readonly int AlphaMulId = Shader.PropertyToID("_AlphaMultiplier");
        private static readonly int AuroraMulId = Shader.PropertyToID("_AuroraMultiplier");
        private static readonly int UVSpeedId = Shader.PropertyToID("_UVSpeed");
        private static readonly int SideFadeId = Shader.PropertyToID("_SideFade");
        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");

        // ── E9：顶点动画的绑定名。五个名字**逐个抄自源码原文**，不是猜的：
        //    VertaBuffer.cs:122-125 ⇒ _VertexSize / _VertexCount / _FrameCount / _VertaBuffer
        //    GPUInstancingManager.cs:690 ⇒ _AnimBuffer
        //    刻意**不绑** _InstBuffer / _IdBuffer：官方是在克隆材质上绑它们做逐实例位置（BatchRenderer.cs:114-115），
        //    而前十轮用原始资产材质（上面从来没绑过任何 buffer）画出的部件位置一直正确 ⇒ 现在去绑一个
        //    内容为 0 的 _InstBuffer 反而可能引入"四元数 (0,0,0,0)"这种新危险。索引链未知由 AnimBufferRows 同值对冲。
        private static readonly int AnimBufferId = Shader.PropertyToID("_AnimBuffer");
        private static readonly int VertaBufferId = Shader.PropertyToID("_VertaBuffer");
        private static readonly int VertexSizeId = Shader.PropertyToID("_VertexSize");
        private static readonly int VertexCountId = Shader.PropertyToID("_VertexCount");
        private static readonly int FrameCountId = Shader.PropertyToID("_FrameCount");

        /// <summary>结构层当前该掺的色（= 色板 shell 槽）。默认白：发光层整个关掉时染色也不该把建筑染黑。</summary>
        private static Color structureTint = Color.white;
        /// <summary>色板 ring 槽（骨架细线色），结构层用它做暗部补光色；同样默认白，避免"色板没算出来就染黑"。</summary>
        private static Color structureFill = Color.white;
        // 本帧两路光照色（P15 的"冷暖分离"）：受光面以恒星色温为主、背光/过渡面重掺色板。
        // 默认白：万一色板还没算出来，也只会退回"无色偏"，不会把补光乘成黑。
        private static Color keyLightTint = Color.white;
        private static Color ambientFillTint = Color.white;

        // 每个 DrawMesh 独占一块 block：同一帧内已提交的 block 不能再改，否则会串色，
        // 所以这里按"每帧每部件一个槽位"顺序取用，绝不复用同一槽位给不同部件。
        // 第十一轮起 SubmitEffects 每次也占一格 ⇒ 当前峰值 = 环 3 + 发生器 6×2 + 接收器 8×2 = 31 槽 / 帧，余量充足。
        private static readonly MaterialPropertyBlock[] pbPool = new MaterialPropertyBlock[64];
        private static int pbCursor;

        private static Color sunTint = Color.white;   // 本帧恒星光色（按光谱取，让巨构带上该恒星的色温）

        // ── 自发光层资产 ──
        // 四张网格/四块材质一一对应四个色槽：命中链上的 Unlit/Color 只读 _Color、不读顶点色，
        // 所以色差只能靠"分开网格 + 分开材质"表达（详见 _dev/warpmodel §5.2）。
        private static Mesh glowRingMesh;     // 发光环体
        private static Mesh glowBulbMesh;     // 环上光核：与环共用同一矩阵，但独立成网格才能单独上色
        private static Mesh glowHaloMesh;     // 能量环：套在塔/接收器外面的亮线圈，中间 hollow 故不遮挡结构层
        private static Material glowMatRing;
        private static Material glowMatBulb;
        private static Material glowMatHalo;
        private static Material glowMatShell; // 建筑本体外壳用的那块（P8）
        private static string glowSrc = "未解析";   // 实际命中的材质来源与 shader 名，日志用
        private static GlowPalette glowPal;
        private static float glowPalKey = float.MinValue;  // 脏标记：色模式 + 恒星色，换恒星才重写颜色
        private static bool diagLogged;
        private static bool propLogged;     // 材质属性探测只打一次（S8b）
        // ── E23（R41）远裁剪**探针**的状态：全部是"只读日志"用的累加器，不参与任何绘制决策 ──
        // 为什么不像上一版（E21）那样只留一个"打过了"的 flag：R41 实测证明"某一刻越界了"这件事已经不需要证明
        //（用户报的 3.多-4 AU 与 far=150000 已经对上），**还缺的是"far 自己会不会随距离变"** —— 那必须连续复采。
        private const string ProbeVersion = "E23";  // 探针版本号，读档时打成横幅 ⇒ 一眼判"这次跑的到底是哪版 DLL"
        private static int probeTick;               // 60 帧一采的节拍器
        private static float probeFirstFar = -1f;   // 本进程第一次采到的 far（"far 恒定/在变"的基准）
        private static bool probeFarChanged;        // 后续任何一次采样与基准差 >1 m 就置真
        private static float probeLoggedFar = -1f;  // 上次真打了日志时的 far ⇒ far 一变就再打一行
        private static int probeLoggedTier = -1;    // 上次打时的"越界几何占比"档位（10% 一档）
        private static float lastDrawDist;          // 最后一次成功绘制时"相机到恒星"的距离（星系边界那行用）
        private static bool boundaryLogged;         // 每"离开星系"一次只打一行；重新开画即复位

        /// <summary>一组发光色。拆四个槽是为了"颜色组合"，而不是全场单色。</summary>
        private struct GlowPalette
        {
            public Color ring;    // 发光环体
            public Color bulb;    // 环上光核
            public Color halo;    // 能量环
            public Color shell;   // 建筑本体：S6 时是纯色外壳色，S8 起改为"结构层染色"（_Color 轻染 + 自发光染色）
        }


        // ---------------------------------------------------------------- 入口

        [HarmonyPostfix]
        public static void GameLogicDrawPostfix(GameLogic __instance)
        {
            // 与原版戴森壳完全相同的门控（GameLogic.cs:1824）
            if (DSPGame.IsMenuDemo) return;
            StarData localStar = __instance.localStar;
            if (localStar == null)
            {
                // E23③（R41）：用户说"原版戴森壳不会消失，除非离开了星系"，而这条 `localStar == null`
                // 正是那句"离开星系"的**真身**（壳与我们用的是同一条判据 ⇒ 它量出来的边界对两者一样）。
                // 上一轮这条边界只有"好几十 AU"这种体感描述，所以在这里把它量成实数：
                // 取最后一次成功绘制时"相机到恒星"的距离，即"画还在、随后停画"的那个位置。
                // 每"离开一次"只打一行（`ProbeFar` 头部会在重新开画时复位 flag），不会刷屏。
                if (!boundaryLogged && lastDrawDist > 0f)
                {
                    boundaryLogged = true;
                    Utils.Log("[MMS farProbe] 星系边界 停画：最后一次绘制的 相机到恒星=" + lastDrawDist.ToString("F0") +
                              " m = " + (lastDrawDist / 40000f).ToString("F2") +
                              " AU（判据 localStar==null，与原版壳 GameLogic.cs:1824 同一条 ⇒ 这一界外的消失不是 far 墙）");
                }
                return;
            }
            if (DysonSphere.renderPlace != ERenderPlace.Universe) return;
            // ── E19（R37）裁决②：**这是临时档，不是终态** ──
            // 用户原话："当前先改成画用户所在星系。未来我会写一个判断方案来告诉你最终mod实装的逻辑应该是什么时候画，画多少。"
            // 所以下面那条 starId 判据先由 `FollowLocalStar` 短路掉（改回 false 即完全复原旧行为，别的什么都不用动）。
            // 为什么必须放开门：`TargetStarId = 0` 是出生星星系，而正常星图里出生星的 `radius` 只在 0.82~1.22 之间随机
            //（`StarGen.cs:343-345`，P32 第 5 条）⇒ 待在原星系**永远看不到自适应放大**，E19 没法实测。
            // ⚠️ 挂起项（等用户的判断方案）：最终"什么时候画、画多少"；届时这条门控应当换成那个方案，而不是删掉判据。
            // MMS 全局以 star.id 作星系序号；这里同时接受 index，避免出现"条件永不成立、画面什么都没有"这类难查问题
            if (!FollowLocalStar && localStar.id != TargetStarId && localStar.index != TargetStarId)
            {
                // 只提示一次：模型按需求固定画在 starId=0，玩家若在别的星系看不到属预期，但很容易误判为"渲染失败"
                if (!wrongStarLogged)
                {
                    wrongStarLogged = true;
                    Utils.Log("[MMS warpModel] 折跃场模型固定渲染在 starId=" + TargetStarId +
                              "，当前星系 starId=" + localStar.id + "，故不显示。");
                }
                return;
            }

            CpuHandle hMms = MMSCPU.BeginSampleSafe(ECpuWorkEntryExtended.MoreMegaStructure);
            CpuHandle hWarp = MMSCPU.BeginSampleSafe(ECpuWorkEntryExtended.WarpField);
            CpuHandle hModel = MMSCPU.BeginSampleSafe(ECpuWorkEntryExtended.WarpFieldModel);
            try
            {
                Render(localStar, __instance.data);
            }
            finally
            {
                MMSCPU.EndSampleSafe(hModel);
                MMSCPU.EndSampleSafe(hWarp);
                MMSCPU.EndSampleSafe(hMms);
            }
        }

        private static void Render(StarData star, GameData gd)
        {
            // E14（R34）：模型层由 Builder 在"首次读档"时建一次。主类在 Import / IntoOtherSave 末尾都会调
            // ModelHandler.InitWhenFirstLoad()，但"新开局必定走哪一条"我没取证（P30 第 1 条的未证实项），
            // 所以这里再兜一次幂等的 EnsureInit —— 万一两条回调都没来，第一帧也能自己把层建出来。
            ModelHandler.EnsureInit();
            if (!resolved) ResolveAssets();
            if (ringPart.mesh == null && nodePart.mesh == null && subPart.mesh == null) return;

            // ── 恒星位置换算到相机局部系：逐字复刻 DysonSphereSegmentRenderer.cs:355-373 ──
            PlanetData localPlanet = gd.localPlanet;
            Player mainPlayer = gd.mainPlayer;
            if (mainPlayer == null) return;

            Vector3 starLocal;
            Vector4 planetRotV4 = new Vector4(0f, 0f, 0f, 1f);
            if (localPlanet != null)
            {
                starLocal = Maths.QInvRotateLF(localPlanet.runtimeRotation, star.uPosition - localPlanet.uPosition);
                Quaternion rt = localPlanet.runtimeRotation;
                planetRotV4 = new Vector4(rt.x, rt.y, rt.z, rt.w);
            }
            else
            {
                starLocal = star.uPosition - mainPlayer.uPosition;
            }
            // 壳用的是 new Quaternion(_LocalRot.xyz, -_LocalRot.w)，即行星运行旋转的共轭
            Quaternion conjPlanet = new Quaternion(planetRotV4.x, planetRotV4.y, planetRotV4.z, -planetRotV4.w);
            Matrix4x4 root = Matrix4x4.TRS(starLocal, conjPlanet, Vector3.one);

            pbCursor = 0;
            // E9④/R30：默认走"展开定格"⇒ 这里其实什么都不做（StepAnim 第一行就返回），数据在 SetupAnim 里已写成末帧。
            // 只有 animHoldExpanded=false 才逐帧推进；那种模式下步长用逻辑 tick 1/60 而不是帧时间
            // —— 原文 PowerSystem.cs:1550 `float num = 0.016666668f;`。
            StepAnim();
            DysonSphere sphere = GetSphere(star, gd);
            sunTint = GetSunTint(star, sphere);
            float r = GetBaseRadius(star, sphere);
            // E19（R37）：整套巨构的世界缩放从今天的 `r / NominalBaseRadius` 换成**自适应倍率**
            //（= max(今天那条, star.radius / ScaleRefRadius) ⇒ 恒 ≥ 今天，M/K 型不缩、蓝巨星按比例放大）。
            // `r` 本身继续留着：远景诊断日志要报"该恒星的原版最小壳层半径"，那是与缩放无关的天文事实。
            float scale = GetAdaptiveScale(star, sphere);
            LogAdaptiveOnce(star, sphere, r, scale);
            float t = Time.realtimeSinceStartup;
            // E14：这里原本的 `float spinDeg = t * SpinSpeed * Mathf.Rad2Deg;` 已交给层级遍历逐层算
            //（每层的角速度现在是 `ModelLayer.rotateSpeed`，见 WarpArrayModelBuilder）。

            // 发光层的几何与材质各生成一次；色板只在换恒星或翻开关时才重写，不进每帧分配。
            // S8 起色板还驱动结构层的染色（_Color 轻染 + 自发光色），所以只要画结构层就得先算出来。
            if (showGlow || showShellGlow || (showStructure && useVanillaPbrKnobs))
            {
                // star.color 是光谱索引（StarData.cs:585 是 float），配上色模式就够当脏标记，无需逐帧比较浮点色值
                float key = (glowColorMode == EGlowColorMode.FixedPalette ? 4096f : 0f) + star.color;
                if (key != glowPalKey)
                {
                    glowPalKey = key;
                    glowPal = BuildPalette(star, sphere);
                    structureTint = glowPal.shell;
                    structureFill = glowPal.ring;
                    EnsureGlowAssets(glowPal);
                    // 换恒星/翻色模式时，建筑自带的特效件克隆材质也跟着色板走（它们是材质实例，不走 MPB）
                    PaintNativeEffects(glowPal);
                }
            }
            // 冷暖分离（P15）：受光面只轻掺色板、保住"这颗恒星是什么色温"，背光/过渡面重掺 ⇒
            // 两块面各持一种色相。饱和度来自这种**分离**，而不是把 _Color 洗成单色（那会重犯 P9 的蒙平错）。
            keyLightTint = Color.Lerp(sunTint, structureTint, SunlightTintStrength);
            ambientFillTint = Color.Lerp(sunTint, structureFill, AmbientTintStrength);
            LogDiagOnce(starLocal, r, scale, star, sphere);

            // 环的受光方向取"从恒星中心射向玩家/相机"。环整体包围恒星，任何单一方向都无法表达
            // "从内部被照亮"，取朝向相机的这条可获得最完整的受光面，远处一眼就能看到亮环。
            // starLocal 已处于行星对齐的相机局部系，也就是 shader 眼里的"世界"，无需再变换。
            Vector3 sunDirToView = starLocal.sqrMagnitude > 1e-6f ? -starLocal.normalized : Vector3.up;

            // 整体姿态：极轴极其缓慢地摆动，让巨构不至于像"焊死"在画面上。
            // 这一档是**全层共用**的（所有环/塔都在同一个摆动参考系里），所以前乘在"层旋转"外面，
            // 不进任何一层的 rotateSpeed —— 旧的三重进动环写法就是 frame · Euler(roll) · Euler(tilt)，
            // 现在 frame 仍在这里、Euler(roll) 交给层、Euler(tilt) 交给条目局部姿态（等价性见 P30 第 4 条）。
            Quaternion frame = Quaternion.Euler(t * RingTiltPrecessSpeed * Mathf.Rad2Deg, 0f, 0f);

            // ── 第十五轮 E14（R34）：原先这里的四段硬编码几何（三重进动环 / 6 座主发生器 / 塔位能量环 /
            //    8 座接收器 + 其能量环）整体换成"遍历 Builder 首次读档时建好的层"。
            //    环的倾角与快慢、塔的八面体布点、接收器的立方体布点，现在都是 WarpArrayModelBuilder 里的声明。
            RenderLayers(root, frame, conjPlanet, sphere, scale, t, sunDirToView);
        }

        /// <summary>
        /// E14（R34）的层级遍历：**这一帧该画的东西全部由 Builder 建好的 `layersByLevel[level]` 决定**，
        /// 渲染器自己不再藏任何几何布局。逐层取角速度算 `layerRot`，逐条目取局部位置/姿态/尺寸算部件矩阵，
        /// 提交仍然是旧那一条 `root · TRS(ctr − rot·(center·s), rot, s)`，所以与重构前**同一条式子**
        /// （名义米 → 世界米只引入这一次除法/倍率，浮点结合次序可能有 1 ulp 差别，画面不可见；见 P30 第 3/4 条）：
        /// · 名义米 → 世界米：`assemblyScale` = <see cref="GetAdaptiveScale"/>（E19 起不再是纯 `R / 4000`；
        ///   出生星上两者**同值** = 1.0 ⇒ P30 第 3 条那条等价性在本存档里原样成立，`AddRing(4000,200)` 仍是 1.00R + 0.05 管径比）；
        /// · 枢轴修正用**条目局部旋转**算（P13 那条教训不能由新架构再犯一次）；
        /// · 光照方向仍按角色的 `lightMode` 分两路（环 = 朝向相机；模型 = 从恒星中心沿半径射出，P30 第 4 条末项）；
        /// · 自研辉光（`showGlow` / `showShellGlow`，当前出厂档都是 false）与特效件绘制一并跟着层级走。
        /// </summary>
        private static void RenderLayers(Matrix4x4 root, Quaternion frame, Quaternion conjPlanet, DysonSphere sphere, float scale, float t, Vector3 sunDirToView)
        {
            List<ModelLayer> layers = ModelHandler.CurrentLayers(sphere);
            if (layers == null || layers.Count == 0) return;
            for (int i = 0; i < layers.Count; i++)
            {
                ModelLayer layer = layers[i];
                if (layer == null || layer.entries == null || layer.entries.Count == 0) continue;
                Quaternion layerRot = frame * Quaternion.Euler(0f, t * layer.rotateSpeed * Mathf.Rad2Deg, 0f);
                for (int k = 0; k < layer.entries.Count; k++)
                {
                    ModelEntry en = layer.entries[k];
                    if (en.kind == EModelEntryKind.Ring) DrawRingEntry(root, layerRot, scale, en, sunDirToView);
                    else DrawModelEntry(root, layerRot, conjPlanet, scale, en, sunDirToView);
                }
            }
        }

        /// <summary>
        /// 画一个"环"条目（E14）。它就是旧三重进动环那一段的通用化：
        /// `rot = layerRot · 条目倾角 · ringBaseRot`（`ringBaseRot` 只在改用游戏自带 Torus 时才非单位四元数），
        /// 缩放 `s = 世界大圆半径 / part.norm`，而 `part.norm = 1 + 管径比` —— 与旧式 `r * RingRadiusRatio / ringPart.norm` 同一条。
        /// 管径比不同的环会各自拿到自己的网格（见 ModelCatalog 的环网格缓存），**同管径比共用同一张网格**，
        /// 这正是"三环逐像素不变"的前提：Builder 里三环必须写同一个 thickness（当前都是 200 ⇒ 0.05 ⇒ 同一张）。
        /// </summary>
        private static void DrawRingEntry(Matrix4x4 root, Quaternion layerRot, float scale, ModelEntry en, Vector3 sunDirToView)
        {
            if (en.radius <= 0f) return;
            ModelSpec spec;
            // 环的"预制件"是程序化几何，管径由条目自己的 thickness 决定 ⇒ 目录按 tubeRatio 取（同管径共用同一张网格）
            if (!GetRingSpec(en.thickness / en.radius, out spec)) return;
            Part p = spec.part;
            if (p.mesh == null) return;
            float worldR = en.radius * scale;
            Quaternion rot = layerRot * en.rotation;
            if (showStructure)
            {
                float s = worldR / p.norm;
                Submit(root * Matrix4x4.TRS(Vector3.zero, rot * spec.meshRotFix, new Vector3(s, s, s)),
                    p.mesh, p.drawMats, sunDirToView, spec.knobs);
            }
            if (showGlow && spec.ringGlow && (glowRingMesh != null || glowBulbMesh != null))
            {
                // 发光环与光核都以大圆半径 1 构建，所以直接按名义半径乘 scale 就精确落在 1.06×结构环上。
                // 两者共用同一个矩阵：拆成两张网格纯粹是为了各吃一个色槽（命中材质只读 _Color），
                // 位置必须严格同步，否则光核会脱离环体飘走。
                // 倍率不写死：GlowRingRadiusRatio / RingRadiusRatio 就是今天那条"发光环 = 1.06R、结构环 = 1.00R"的关系，
                // 于是 Builder 里无论把环写成 4000 还是 15000，发光环都自动跟着结构环走。
                float sg = worldR * (GlowRingRadiusRatio / RingRadiusRatio);
                Matrix4x4 gm = root * Matrix4x4.TRS(Vector3.zero, rot, new Vector3(sg, sg, sg));
                SubmitGlow(gm, glowRingMesh, glowMatRing);
                SubmitGlow(gm, glowBulbMesh, glowMatBulb);
            }
        }

        /// <summary>
        /// 画一个"模型"条目（E14）。旧那段 `for (i &lt; OctaDirs.Length)` 的通用化，逐字保住三件事：
        /// 1) **摆位**：`ctr = layerRot · (局部位置 · scale)`，于是 `AddModel(dir·4000, …)` 精确落在 1.00× 基准半径上
        ///    （`4000` 现在是 Builder 自带的数字，E18 / R37；`scale` 自 E19 起是恒星自适应倍率）；
        /// 2) **枢轴**（P13）：位移要减掉【完整旋转】乘出来的中心偏移，而完整旋转 = `layerRot · 局部姿态`，
        ///    由 `T(R·a)·R(R₀) = R·T(a)·R(R₀)` 可先在世界系合成再走旧那条 TRS，结果与重构前是同一条式子；
        /// 3) **尺寸**：`s = 局部尺寸 · scale / part.norm`（`AddModel(…, 800)` ≡ 今天的 `R · 0.20`）。
        ///    ⚠️ 这一条也是 <see cref="ModelLayer.AddLaser"/> 的"粗细"来源，而**拉伸段解 k 时用的正是同一个 `s`**
        ///   （`ModelCatalog.BuildVariant` 第 2 步）⇒ 这里的式子一旦改动，那边必须同步，否则尖端就不再正好落到终点。
        /// 光照方向按 `en.lightMode` 分两路（见 P30 第 4 条末项）：径向 = `conjPlanet · (layerRot · dirLocal)`，
        /// 朝向相机 = `sunDirToView`。能量环/外壳这些附加层仍跟着同一条目矩阵走。
        /// </summary>
        private static void DrawModelEntry(Matrix4x4 root, Quaternion layerRot, Quaternion conjPlanet, float scale, ModelEntry en, Vector3 sunDirToView)
        {
            ModelSpec spec;
            // 条目里带了 crop 或拉伸时，这里返回的 part 是**网格副本**（见 ModelCatalog.ApplyVariant）：
            // 两种变体都**不改顶点数、不改索引** ⇒ 下面的枢轴/尺寸/动画全部沿用同一条式子
            //（裁剪只是少画些三角形；拉伸只改一部分顶点的 Y，而特效件根本没绑动画 buffer）。
            if (!TryGetSpec(en, out spec)) return;
            Part p = spec.part;
            if (p.mesh == null) return;
            if (!showStructure && !showShellGlow && !showGlow) return;   // 三条路都不画才提前退出（旧式里能量环只看 showGlow，别多砍一层）

            Vector3 localPos = en.position;             // VectorLF3 → Vector3 是隐式转换（VectorLF3.cs:207）
            Quaternion localRot = en.rotation;
            Quaternion rot = layerRot * localRot;
            Vector3 ctr = layerRot * (localPos * scale);
            Vector3 dir = localPos.sqrMagnitude > 1e-12f ? localPos.normalized : Vector3.up;
            Vector3 sunDir = en.lightMode == EModelLightMode.RadialFromStar ? conjPlanet * (layerRot * dir) : sunDirToView;
            // 名义尺寸 → 世界尺寸，再除掉网格的归一化外延。三个轴允许各给各的
            //（`AddModel(id, pos, rot, new VectorLF3(x, y, z))`），而当前出厂档是各向同性的 800
            // ⇒ 与旧式 `r * NodeSizeRatio / nodePart.norm` 算出来的是同一条数（P30 第 3 条）。
            Vector3 s = ((Vector3)en.scale * scale) / p.norm;
            Vector3 ss = s * ShellInflate;

            if (showStructure)
            {
                // 该部件被"从恒星中心沿它所在半径射出的光"照亮 —— 这才是环绕恒星分布该有的受光方式
                Matrix4x4 part = root * Matrix4x4.TRS(ctr - rot * Vector3.Scale(p.center, s), rot, s);
                // E17（R36）"只保留电流特效"的预制件：跳过这一句就够了。特效 submesh 早在 E5② 就被摘出交给
                // p.pieces 的自用克隆材质（drawMats 对应位置是 null），所以结构层与特效层是两次独立提交
                // （依据见文档 P31 第 1/2 条）；这里若改回 `showStructure=false` 会把下面那句一起关掉，故必须逐条目判。
                if (!spec.effectOnly) Submit(part, p.mesh, p.drawMats, sunDir, spec.knobs);
                // 电离/线圈这类原生特效件与本体共用部件矩阵（R20），不能各算各的姿态，否则会再次偏心
                SubmitEffects(part, p.pieces, sunDir);
            }
            // 外扩纯色外壳那条路已被实测证伪为回归（P9），默认关闭、只留作 A/B 对比；
            // 若真要开，注意它同样必须以部件几何中心为基准重算位移：只改缩放不改 pos 会把整个部件甩走。
            if (showShellGlow && !spec.effectOnly)
                SubmitGlow(root * Matrix4x4.TRS(ctr - rot * Vector3.Scale(p.center, ss), rot, ss),
                    p.mesh, glowMatShell);

            // 该模型位的能量环：半径比塔身大一档，且中间是空的 —— 远景是一团亮点，
            // 近景也不会把塔身整个吞掉（若换成实心发光球就会糊掉结构细节，两层反而互相抵消）。
            // 姿态必须写成 `FromToRotation(up, 世界 dir)`（与重构前那一条一字不差）：
            // 若写成 `layerRot · FromToRotation(up, 局部 dir)`，两者只差一个"绕自身轴拧一层 layerRot"的扭转，
            // 圆环看着无所谓，但 `frame` 里那档 X 轴摆动并不与能量环自身的轴重合，会给未来重开 showGlow 埋下偏移。
            // effectOnly 一并跳过：那条预制件的定义就是"画面里只有官方电流特效"，不配任何自研发光
            //（`TryGetSpec` 里也已经把它的 glowHaloNominal 置 0，这里是第二重保险）。
            if (showGlow && !spec.effectOnly && glowHaloMesh != null && spec.glowHaloNominal > 0f)
            {
                float sh = spec.glowHaloNominal * scale;
                Quaternion hrot = Quaternion.FromToRotation(Vector3.up, layerRot * dir);
                SubmitGlow(root * Matrix4x4.TRS(ctr, hrot, new Vector3(sh, sh, sh)), glowHaloMesh, glowMatHalo);
            }
        }

        /// <summary>
        /// 照抄壳的提交方式：layer 16、camera 传 null、阴影关、不接收投影；
        /// 外加逐部件光照覆写 + 官方 PBR/自发光旋钮覆写（结构层的上色就靠这两件事，见 ApplyPbrKnobs）。
        /// knobs 是逐角色的一包参数（E9）：其中 bindAnim 只对塔为 true ⇒ 同一次绘制里把动画数据也补上（见 BindAnim）。
        /// </summary>
        private static void Submit(Matrix4x4 matrix, Mesh mesh, Material[] mats, Vector3 sunDir, RoleKnobs knobs)
        {
            if (mats == null || mats.Length == 0) return;
            int subs = Mathf.Min(mesh.subMeshCount, mats.Length);
            if (subs <= 0) return;

            MaterialPropertyBlock pb = NextBlock();
            ApplyLighting(pb, sunDir);

            if (useVanillaPbrKnobs) ApplyPbrKnobs(pb, knobs);
            BindAnim(pb, knobs);

            for (int i = 0; i < subs; i++)
            {
                Material m = mats[i];
                if (m == null) continue;
                Graphics.DrawMesh(mesh, matrix, m, DysonRenderLayer, null, i, pb, UnityEngine.Rendering.ShadowCastingMode.Off, false);
            }
        }

        /// <summary>
        /// E9③：把 P26 查实的那条动画链**接到我们这次绘制上**。
        /// 官方链条是 `PowerSystem.cs:2127-2170` 推进 AnimData → `FactoryModel.cs:175` SetData 上传 →
        /// `BatchRenderer.cs:114-118` / `GPUInstancingManager.cs:686-690` 把 buffer **绑在克隆材质上** → DrawMeshInstancedIndirect。
        /// 第四段是我们缺的：我们用的是**原始资产材质**，而 P26 补强④ 证明那些材质上一个 buffer 都没绑过
        /// ⇒ shader 里 `_VertaBuffer`/`_AnimBuffer` 无从取数据 ⇒ 塔不可能动。这里用 MPB 逐次绘制补上（P28：MPB 有 SetBuffer，反射证实）。
        /// 只写这四个名字，不碰 _InstBuffer/_IdBuffer（理由见属性声明处的注释），且整体受 feedAnimBuffer 总闸管：
        /// "DrawMesh 到底认不认 MPB 里的 buffer"是 P28 第 3 条的未证实项，异常时关掉这一行即可完全回到上一轮。
        /// </summary>
        private static void BindAnim(MaterialPropertyBlock pb, RoleKnobs knobs)
        {
            if (!feedAnimBuffer || !knobs.bindAnim || pb == null) return;
            if (animBuffer != null) pb.SetBuffer(AnimBufferId, animBuffer);
            // 下面四行的名字与写法逐字抄 VertaBuffer.cs:122-125（SetToAnimMaterial 的原文），只是载体从 Material 换成 MPB
            if (animVerta != null && animVerta.cbuffer != null)
            {
                pb.SetInt(VertexSizeId, animVerta.vertexSize);
                pb.SetInt(VertexCountId, animVerta.vertexCount);
                pb.SetInt(FrameCountId, animVerta.frameCount);
                pb.SetBuffer(VertaBufferId, animVerta.cbuffer);
            }
        }

        /// <summary>
        /// E9③④：为塔准备动画数据。两条来源都是原文：
        /// stride 用 `AnimData.dataLen`（=20，与 `FactoryModel.cs:534` 的 ComputeBuffer 一致）；
        /// 初值 prepare/working 取自 `PrefabDesc.anim_prepare_length/anim_working_length`（即 `PlanetFactory.cs:1369-1373` 给真实建筑的同一对初值）。
        /// 行数 AnimBufferRows 全部填同一条 ⇒ 索引链未知也无所谓（P26 的那条对冲设计）。
        /// </summary>
        private static void SetupAnim(Part node)
        {
            if (!feedAnimBuffer) return;
            try
            {
                if (node.mesh == null) return;
                animVerta = node.verta;
                // VertaBuffer.Import 只填 CPU 数组，GPU buffer 是**懒创建**的：官方在 BatchRenderer.cs:102-105 调 SetBufferData()。
                // 我们的模型可能从来没被官方 BatchRenderer 建过批（假建筑不进那套 culling），所以自己补这一次上传。
                if (animVerta != null && animVerta.cbuffer == null && animVerta.data != null && animVerta.dataLength > 0)
                    animVerta.SetBufferData();

                // 换塔（重新解析资产）时旧行的 prepare/working 属于上一个模型 ⇒ 按"网格变了就重建"处理
                if (animRows == null || animRows.Length != AnimBufferRows || animSource != node.mesh)
                {
                    animSource = node.mesh;
                    animLogged = false;     // 重建过一次就允许再打一次日志，换模型后的实况必须说清楚
                    // R30：定格用的"末帧时刻"。working_length 必须 >0 —— 原文 `float num = this.working_length - 0.001f;`
                    // （AnimData.cs:118）若拿到 0（`PrefabDesc` 无 AnimDesc 时 anim_working_length 可能为 0）就会得到**负 time**，
                    // 交给 shader 属于未定义输入。这里按原文语义 clamp，不猜别的值。
                    float working = node.animWorking > 0f ? node.animWorking : 1f;
                    animHoldTime = animHoldExpanded ? working - 0.001f : 0f;
                    animRows = new AnimData[AnimBufferRows];
                    for (int i = 0; i < animRows.Length; i++)
                    {
                        animRows[i] = animHoldExpanded
                            // **展开定格**：prepare_length 归零（P29 第 1 条：它是那 5 个字段里唯一的周期量，
                            // 官方 charger 分支把它挪作电量累加器、>30 就减 30 ⇒ 用户看到的 30 秒"收起→弹回"循环就是它），
                            // time 直接给到末帧、state 给 tar_state、power 给常量 ⇒ 上传后不再有任何变化量
                            ? new AnimData
                            {
                                time = animHoldTime,
                                prepare_length = 0f,
                                working_length = working,
                                state = AnimTargetState,
                                power = AnimPower
                            }
                            // 第十二轮的老写法（逐帧推进）：初值与 PlanetFactory.cs:1369-1373 给真实建筑的那对初值一致
                            : new AnimData
                            {
                                time = 0f,
                                prepare_length = node.animPrepare,
                                working_length = working,
                                state = 0u,
                                power = 0f
                            };
                    }
                    if (animBuffer != null) { animBuffer.Release(); animBuffer = null; }
                    animBuffer = new ComputeBuffer(AnimBufferRows, AnimData.dataLen, ComputeBufferType.Default);
                    animBuffer.SetData(animRows);
                }
                if (!animLogged)
                {
                    animLogged = true;
                    string v = animVerta == null ? "无verta(lodVertas[0]为空 ⇒ 该模型官方就没有顶点动画数据)"
                        : "verta 顶点" + animVerta.vertexCount + " 帧" + animVerta.frameCount +
                          " 每点" + animVerta.vertexSize + "float cbuffer=" + (animVerta.cbuffer != null ? "已上传" : "**null**");
                    Utils.Log("[MMS warpModel] 动画喂数据 模式=" + (animHoldExpanded
                              ? ("展开定格(不推进, prepare=0 time=" + animHoldTime + " state=" + AnimTargetState + ")")
                              : ("逐帧推进 公式=" + (animUseStep2 ? "Step2(纯节点:2169 循环)" : "StepPoweredClamped2(charger:2146 爬升停末帧)"))) +
                              " 行数=" + AnimBufferRows + " stride=" + AnimData.dataLen +
                              " power=" + AnimPower + " tarState=" + AnimTargetState + " dt=" + AnimStepDt +
                              " 资产prepare/working=" + node.animPrepare + "/" + node.animWorking +
                              " | " + v +
                              " | 载体=MPB.SetBuffer（P28 第 3 条的未证实项已由第十三轮实测**证实**：电流特效出现 ⇒ DrawMesh 会消费 MPB 里的 buffer）");
                }
            }
            catch (Exception e)
            {
                Utils.Log("[MMS warpModel] 动画数据准备失败(不影响静态渲染): " + e.Message);
                animBuffer = null;
                animRows = null;
            }
        }

        /// <summary>
        /// 每帧推进一次动画数据 —— 调的是**游戏自己的方法**，不是我自己写的近似：
        ///   默认 `AnimData.StepPoweredClamped2(power, dt, tarState)` = `PowerSystem.cs:2146`（无线输电塔 coverRadius≥20 那一支）
        ///   对照 `AnimData.Step2(1u, dt, power*0.97+0.03*serve, 0.4f)` = `PowerSystem.cs:2169`（纯电力节点那一支）
        /// dt 用 1/60 固定步长（`PowerSystem.cs:1550` 原文），与游戏的逻辑 tick 一致而非帧时间。
        /// ⚠️ R30：`animHoldExpanded = true` 时本方法**直接返回**，一帧都不推进（hold 优先于 animUseStep2，
        ///    两个开关不互相打架：想看循环就先把 hold 改 false，再按需翻 animUseStep2）。
        ///    这不是"关掉动画"——绑定与数据都还在，只是不再产生任何变化量，塔停在展开完成的末帧。
        /// </summary>
        private static void StepAnim()
        {
            if (animRows == null || animBuffer == null) return;
            // 定格的依据见属性声明处（P29 第 1 条）：继续推进就会把 prepare_length 那个 30 秒锯齿重新引入，
            // 而那正是用户实测到的"循环播放从有电到没电的收起动画"的唯一周期源。
            if (animHoldExpanded) return;
            try
            {
                for (int i = 0; i < animRows.Length; i++)
                {
                    if (animUseStep2)
                    {
                        // 原文：anim.power*0.97 + 0.03*serve（serve 对我们恒为 AnimPower，因为假建筑没有电网）
                        float p = animRows[i].power * 0.97f + 0.03f * AnimPower;
                        animRows[i].Step2(1u, AnimStepDt, p, AnimStepSpeed);
                    }
                    else
                    {
                        animRows[i].StepPoweredClamped2(AnimPower, AnimStepDt, AnimTargetState);
                    }
                }
                animBuffer.SetData(animRows);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 逐部件的全局光照覆写（第十一轮从 Submit 里抽出，好让特效层也能共用）。
        /// 之所以结构层与特效层都要它：`_Global_*` 是这些 shader 实际读的一组全局量，
        /// 摘出件（尤其 `PBR Glass RayReceiver` 那层罩子）过去在结构层里是**吃到过**这套光照的，
        /// 摘出去若不带光照就等于顺手改了它的观感 —— 与"本轮不改观感"的约定冲突，所以原样补回。
        /// 对 unlit/additive/black mask 那些不声明这些量的材质，写入是静默空操作，无副作用。
        /// </summary>
        private static void ApplyLighting(MaterialPropertyBlock pb, Vector3 sunDir)
        {
            if (pb == null) return;
            // w = 0 与游戏下发放一致（StarSimulator.cs:459 new Vector4(x, y, z, 0f)）
            pb.SetVector(SunDirId, new Vector4(sunDir.x, sunDir.y, sunDir.z, 0f));
            pb.SetColor(SunLightColorId, keyLightTint);
            // 明暗过渡三档从"主光色"连续走到"补光色" ⇒ 交界带自带色相，这就是"不同面不同的光"（P15）。
            // 原版由 StarSimulator 按恒星混合三档暖色，这里刻意不让它等于主光色，否则又是单一色相。
            pb.SetColor(SunsetColor0Id, Color.Lerp(keyLightTint, ambientFillTint, 0.35f));
            pb.SetColor(SunsetColor1Id, Color.Lerp(keyLightTint, ambientFillTint, 0.60f));
            pb.SetColor(SunsetColor2Id, Color.Lerp(keyLightTint, ambientFillTint, 0.85f));
            // 抬环境光：太空中 _Global_AmbientColor* 被 PlanetEnvironment.cs:107 往黑插值，
            // 背光面因此死黑、丢形状。补光走"重掺色板"那一档，让暗部既保留体积又带上颜色。
            Color amb = ambientFillTint * AmbientGain;
            pb.SetColor(AmbientColor0Id, amb);
            pb.SetColor(AmbientColor1Id, amb * 0.72f);
            pb.SetColor(AmbientColor2Id, amb * 0.48f);
        }

        /// <summary>
        /// 结构层的"双保险"覆写（修 P9 / 落地 R13，依据逐条见 _dev/warpmodel §5.3）。
        /// 全部写进 MPB：不克隆游戏材质、不改全局状态，对本项目其它渲染零副作用。
        /// 保险 A 受光通道是**必然生效**的那半 —— 立体感来自法线贴图 × 方向光 × 高光，与自发光无关；
        /// 保险 B 自发光通道是**可能额外生效**的那半 —— 三支 shader 各认其中一部分属性名，
        /// 未声明的赋值是静默空操作，所以候选名一次铺满，谁被声明谁生效（探测日志见 LogMaterialPropsOnce）。
        /// </summary>
        private static void ApplyPbrKnobs(MaterialPropertyBlock pb, RoleKnobs knobs)
        {
            Color shell = structureTint;
            // E12②（R31）：这一位决定"上色/倍率"整包写不写。塔 = true（NodeKeepOfficialMaterialValues），环与接收器 = false。
            // 依据是 P29 第 2 条那张 `.mat` 原文对照表 —— 上一轮只放开了 `_EmissionMask`，剩下四个乘数仍在全局覆写，
            // 而这四个恰好就是把官方色相洗掉的元凶（albedo 1→3.6、emission 7→3.5、smooth 1→1.6、normal 1→1.35、_Color 掺 0.15）。
            // 注意这里**不**动 ApplyLighting 的 _Global_* 那一组：交回官方数值 ≠ 交回原版的光照条件，
            // 太空里没有环境光，那组不写会让背光面死黑（P1 的老账），所以照写。
            bool keep = knobs.keepOfficial;
            if (!keep)
            {
                // ── 保险 A：受光通道 ──
                // _AlbedoMultiplier 是官方"不受光也能整体提亮、且保留贴图相对明暗"的旋钮
                // （MechaArmorModel.cs:583 曲速时就把它设成 4f * 100^warpState）。
                pb.SetFloat(AlbedoMulId, StructureAlbedoMultiplier);
                pb.SetFloat(NormalMulId, StructureNormalMultiplier);   // 放大法线贴图 = 面与面差异更明显（官方塔是 1）
                pb.SetFloat(SmoothMulId, StructureSmoothMultiplier);  // 放大高光 = 转动时反光随视角走，立体感的关键（官方塔是 1）
                pb.SetColor(SpecularColorId, keyLightTint * StructureSpecularGain);   // 官方塔的 _SpecularColor 是中性灰 (0.706)
                // _Color 只掺一档色相：拉满会把 albedo 贴图的相对明暗洗成单色，那就又变回 P9 的观感。
                // E9 起逐角色给值（P25 第 1 条：对电力感应塔那种"black vertex rotate"材质，贴图近乎无彩、
                // 顶点色被当遮罩用，**_Color 就是它唯一的颜色来源**，按 0.38 掺色等于把它自己的 (1,0.429,0.429) 洗掉）；
                // E12 起塔直接 -1 不写（P29 第 2 条：无线输电塔的官方 _Color 就是纯白 (1,1,1,1)，它本来不染色）。
                if (knobs.colorTint >= 0f) pb.SetColor(ColorId, Color.Lerp(Color.white, shell, knobs.colorTint));
            }
            // ── 第十轮 E8（R27）：Properties 原文里一直没被我们看见的那几档（判据见 P24）──
            // 这四档**只在偏离官方值时才写**：材质自己序列化里的值可能不等于 shader 默认，
            // 无条件写"官方默认"反而会覆盖掉美术调过的数（例如某支建筑把金属度调低过）。
            SetIfTuned(pb, MetallicMulId, StructureMetallicMultiplier, 1f);       // 金属倍率：三支本体都声明
            SetIfTuned(pb, EmissionMipBiasId, StructureEmissionMipBias, -2f);     // 自发光 mip 偏移：往 0 走远景更柔更连续
            SetIfTuned(pb, NormalMipBiasId, StructureNormalMipBias, -1f);         // 法线 mip 偏移：只有环那支有
            SetIfTuned(pb, EmissionJitterId, StructureEmissionJitter, 0f);        // 自发光抖动：>0 发光纹理会自己游走
            SetIfTuned(pb, RotateSpeedId, StructureRotateSpeed, 1f);              // 贴图旋转速度：只有电塔那支有
            if (StructureCullMode >= 0f) pb.SetFloat(CullModeId, StructureCullMode);   // -1 = 明确表示"不覆写"（官方 2 = 剔背面）
            // ── 保险 B：官方自发光通道 ──
            // 两个门**分开给值**（P21）：_EmissionUsePower 按三档模式走，_EmissionSwitch 用独立常量。
            // 上一轮"Switch 交回材质原值"的实测结果是自发光整体关闭 ⇒ 这里必须写，默认 0（Properties 里它的
            // 显示名就是"是否使用游戏状态决定自发光"，0 = 不查游戏状态 ⇒ 我们这个假建筑也恒亮）。
            if (emissionGateMode != EGateMode.Native && !(keep && knobs.keepOfficialGate))
            {
                float gate = emissionGateMode == EGateMode.FollowPower ? GateValueFollowPower : GateValueIgnorePower;
                pb.SetFloat(EmissionUsePowerId, gate);
                pb.SetFloat(EmissionSwitchId, StructureEmissionSwitchValue);
            }
            // 逐角色增益（R27）：基调仍是"把官方 5.5 压到 3.5"，但允许环/电塔/接收器各走各的亮度
            if (!keep)
            {
                float gain = knobs.emissionGain;
                pb.SetFloat(EmissionMulId, gain == 1f ? StructureEmissionMultiplier : StructureEmissionMultiplier * gain);
            }
            else if (knobs.emissionMulOverride >= 0f)
            {
                // keep 档默认连这个字也不写（官方 space 塔 = 7、ground 塔 = 1.5，都比我们原来的 3.5 有出处）。
                // 只有 NodeEmissionMulOverride 给了数才单独写塔这一档 ⇒ "颜色对了但偏暗"时有抓手，且不动全局。
                pb.SetFloat(EmissionMulId, knobs.emissionMulOverride);
            }
            if (!keep)
            {
                // 染色属性名三支 shader 不统一：环认 _EmissionMask、接收器认 _EmColor1/2、电塔两个都没有
                // （电塔只能靠上面的 _Color 与 _EmissionMultiplier 调），所以一次全铺。
                Color em = Color.Lerp(Color.white, shell, StructureEmissionTint);
                // E9②：_EmissionMask 也必须逐角色给 —— P25 查实无线输电塔的官方 _EmissionMask 是**青色**
                // (space-2 为 (0.552,1,0.851)，其余塔 (0.596,0.957,0.933))，那正是用户说的"贴图有颜色"所在。
                // 上一轮无条件把它覆写成我们的掺色结果 ⇒ 等于亲手洗掉官方颜色（第二处自伤，与 _Color 同性质）。
                // knobs.emissionMaskTint < 0 ⇒ 一个字都不写，青色原样生效。
                if (knobs.emissionMaskTint >= 0f)
                    pb.SetColor(EmissionMaskId, Color.Lerp(Color.white, shell, knobs.emissionMaskTint));
                pb.SetColor(EmColor1Id, em);
                pb.SetColor(EmColor2Id, em);
            }
        }

        /// <summary>
        /// 只有当旋钮被改离官方值时才写入（第十轮 E8 的"零风险留手"约定）：
        /// 保持初值 = 官方值 ⇒ 不写 ⇒ 材质序列化里美术调过的数原样生效，画面与上一轮逐像素一致。
        /// </summary>
        private static void SetIfTuned(MaterialPropertyBlock pb, int id, float value, float official)
        {
            if (!Mathf.Approximately(value, official)) pb.SetFloat(id, value);
        }

        private static MaterialPropertyBlock NextBlock()
        {
            if (pbCursor >= pbPool.Length) pbCursor = 0;  // 实际不会触及：部件数远小于槽位数
            MaterialPropertyBlock pb = pbPool[pbCursor];
            if (pb == null) pb = pbPool[pbCursor] = new MaterialPropertyBlock();
            pbCursor++;
            return pb;
        }

        /// <summary>恒星光色：优先用该星戴森球已经算好的 sunColor，否则按同一张调色板现评。</summary>
        private static Color GetSunTint(StarData star, DysonSphere sphere)
        {
            if (sphere != null) return sphere.sunColor;
            try
            {
                Gradient g = Configs.builtin != null ? Configs.builtin.dysonSphereSunColors : null;
                if (g != null) return g.Evaluate(star.color);   // 与 DysonSphere.cs:405 同一条路径
            }
            catch (Exception) { }
            return Color.white;
        }

        private static DysonSphere GetSphere(StarData star, GameData gd)
        {
            if (gd.dysonSpheres == null || star.id < 0 || star.id >= gd.dysonSpheres.Length) return null;
            return gd.dysonSpheres[star.id];
        }

        /// <summary>基准半径 = 游戏给该恒星算的最小壳层半径；无戴森球对象时按同一公式现算。</summary>
        private static float GetBaseRadius(StarData star, DysonSphere sphere)
        {
            if (sphere != null && sphere.minOrbitRadius > 1f) return sphere.minOrbitRadius;
            float r = star.physicsRadius * 1.5f;
            return r < 4000f ? 4000f : r;   // DysonSphere.cs:413-416
        }

        /// <summary>
        /// E19（R37）**恒星半径自适应倍率**：这套巨构最终乘的世界缩放，取两条路里更大的那一个。
        /// 用户原话："以M型恒星的普遍大小为最低缩放倍率，即使再小也不会收缩，而更大半径（比如蓝巨星等）则会按比例放大最后的渲染结果"
        /// ⇒ 裁决①把"最低档"锚在 **1.0**（= 今天出生星那一档），所以本式的结果**恒 ≥ 今天的 `R/4000`**，不可能缩小。
        /// 第一项 = 今天那条链（`GetBaseRadius` 内含 `physicsRadius*1.5` 与 4000 下限）：它同时保证"巨构始终贴在
        /// 该恒星表面 1.5 倍半径之外"，不会被星体吞掉，也已经是"小星不缩"的那一半（radius=0.66 时仍是 4000/4000 = 1.0）。
        /// 第二项 = 新增的恒星半径线性项，以 <see cref="ScaleRefRadius"/> = 1.0（太阳型）为 1 倍基准：
        ///   M 型 0.59~0.78 ⇒ 不抬（仍取第一项）；出生星 0.82~1.22 ⇒ 通常仍取第一项（= 1.0，画面逐像素不变）；
        ///   O 型（蓝巨星）4.2~5.6 ⇒ 取第二项 ⇒ 整套放大 4~5.6 倍（今天只有 2.25）。
        /// 依据与取证过程（含"现锚拐点其实在 radius&gt;2.222"这条事实）全在文档 **P32**。
        /// ⚠️ 两项都只读 `StarData`／`DysonSphere` 的公开字段、不写任何东西 ⇒ 可以在每帧调用（下面 :682 那条就是每帧）。
        /// </summary>
        private static float GetAdaptiveScale(StarData star, DysonSphere sphere)
        {
            if (star == null) return 1f;
            float legacy = GetBaseRadius(star, sphere) / NominalBaseRadius;      // 今天那条（含 4000 下限 ⇒ 恒 ≥1）
            float byStar = star.radius / ScaleRefRadius;                          // 新增：恒星半径线性放大项
            return legacy > byStar ? legacy : byStar;
        }

        /// <summary>自适应倍率的"1 倍"对应多少恒星半径（太阳型）。改这一行就等于整体抬高/压低放大拐点（<see cref="GetAdaptiveScale"/>）。</summary>
        private const float ScaleRefRadius = 1.0f;

        private static int adaptiveLogStar = int.MinValue;
        /// <summary>
        /// **换一颗恒星就打一行**（同一颗星不刷屏）。这一行存在的理由是 E19 的验收没法靠眼睛判：
        /// 用户要的是"M 型不缩、蓝巨星按比例放大"，而那两档分别落在 `radius≈0.66` 与 `radius≈4.9`（P32 第 6/8 条），
        /// 只有把 `legacy`（今天那条）与 `byStar`（半径项）两个候选值并排打出来，才能确认"最终那个数是谁赢的"、
        /// 以及"放大到底是不是按比例的"。请实测时把这一行的 `scale=` 数值原样回报（§7.16）。
        /// </summary>
        private static void LogAdaptiveOnce(StarData star, DysonSphere sphere, float r, float scale)
        {
            if (star == null || star.id == adaptiveLogStar) return;
            adaptiveLogStar = star.id;
            float legacy = r / NominalBaseRadius;
            float byStar = star.radius / ScaleRefRadius;
            Utils.Log("[MMS warpModel] 恒星自适应：starId=" + star.id + " 类别=" + star.type + "/" + star.spectr +
                      " radius=" + N(star.radius) + "（光谱色键 star.color=" + N(star.color) + "）" +
                      " physicsRadius=" + N(star.physicsRadius) +
                      " 壳层基准R=" + N(r) + "（minOrbit=" + (sphere != null ? N(sphere.minOrbitRadius) : "无戴森球对象") + "）" +
                      " ⇒ 旧式倍率=" + N(legacy) + " 半径项=" + N(byStar) + " **最终 scale=" + N(scale) + "**" +
                      "（取两者较大 ⇒ 恒 ≥ 旧式，永不缩小；见 P32）");
        }

        // ---------------------------------------------------------------- 自发光能量层

        /// <summary>
        /// 按 R12 的开关组装一组发光色。
        /// StarTinted：走原版戴森壳的同一条配方（DysonSphere.cs:405-411），换恒星/中子星都会变，
        ///           先归一到满亮度再由它派生四槽（保留槽间明暗层次）；
        /// FixedPalette：与恒星无关的固定色组合（当前默认，偏绿的蓝绿），四槽值即最终色，不再二次归一。
        /// </summary>
        private static GlowPalette BuildPalette(StarData star, DysonSphere sphere)
        {
            GlowPalette p = new GlowPalette();
            if (glowColorMode == EGlowColorMode.FixedPalette)
            {
                // 固定色板按作者给的值原样用，不再逐槽归一 —— 逐槽归一会把"halo 压暗、bulb 提亮"
                // 这些刻意设置的明暗差全部抹平（每槽最大分量都变成 1），层次与降曝就都没了。
                p.ring = FixedRingColor;
                p.bulb = FixedBulbColor;
                p.halo = FixedHaloColor;
                p.shell = FixedShellColor;
                return p;
            }
            // 恒星分支反过来必须先归一：原版发光色本身可能偏暗，直接在黑太空背景下依然看不见。
            // 关键是"归一一次基准色、再由它派生各槽"，这样槽间明暗差得以保留。
            // 远景对比度不押在 bloom 上（PostEffectController.cs:486 玩家可整个关掉，阈值还在 0.13~1.2 浮动）。
            Color c = NormalizeBright(StarEmission(star, sphere));
            p.ring = c;
            p.bulb = Color.Lerp(c, Color.white, 0.32f);     // 光核更亮，远景才读得出"是一串点"
            p.halo = c * 0.85f;                             // 能量环压暗一档，避免近景盖过光核
            p.halo.a = 1f;                                  // 连乘会把 alpha 也压下去，这里显式补回不透明
            p.shell = Color.Lerp(c, Color.white, 0.18f);    // 外壳略提亮，建筑轮廓才从骨架里分得开
            return p;
        }

        /// <summary>该恒星的官方戴森壳发光色（优先用戴森球已经算好的 emissionColor）。</summary>
        private static Color StarEmission(StarData star, DysonSphere sphere)
        {
            Color c = Color.white;
            try
            {
                if (sphere != null) c = sphere.emissionColor;
                else if (star.type == EStarType.NeutronStar) c = Configs.builtin.dysonSphereNeutronEmissionColor;
                else if (Configs.builtin != null && Configs.builtin.dysonSphereEmissionColors != null)
                    c = Configs.builtin.dysonSphereEmissionColors.Evaluate(star.color);
            }
            catch (Exception) { }
            return c;
        }

        /// <summary>把最大分量归一到 1：色相不动，只把亮度拉满，实际亮度再交给 EmissiveGain。</summary>
        private static Color NormalizeBright(Color c)
        {
            float mx = Mathf.Max(0.05f, Mathf.Max(c.r, Mathf.Max(c.g, c.b)));
            Color o = c / mx;
            o.a = 1f;
            return o;
        }

        /// <summary>发光层资产：几何与材质只建一次；色板变了才重写（换恒星或翻色模式才会变）。</summary>
        private static void EnsureGlowAssets(GlowPalette pal)
        {
            // 只有真要画骨架时才建这几张网格：结构层的染色覆写不需要它们，别白建
            if (showGlow)
            {
                if (glowRingMesh == null) glowRingMesh = BuildGlowRing();
                if (glowBulbMesh == null) glowBulbMesh = BuildGlowBulbs();
                if (glowHaloMesh == null) glowHaloMesh = BuildGlowHalo();
            }
            if ((showGlow || showShellGlow) && glowMatRing == null) ResolveGlowMaterials();
            PaintGlow(pal);
        }

        private static void PaintGlow(GlowPalette pal)
        {
            // 主路径是材质 _Color：实测命中的 unlit 材质只读 _Color、不读顶点色（§5.2）。
            // 顶点色照样按槽位铺一遍当兜底 —— 将来若候选链改命中读顶点色的 shader 也不会错色。
            FillColors(glowRingMesh, ToColor32(pal.ring));
            FillColors(glowBulbMesh, ToColor32(pal.bulb));
            FillColors(glowHaloMesh, ToColor32(pal.halo));

            TintMaterial(glowMatRing, pal.ring);
            TintMaterial(glowMatBulb, pal.bulb);
            TintMaterial(glowMatHalo, pal.halo);
            TintMaterial(glowMatShell, pal.shell);
        }

        /// <summary>顶点色只能表达 0~1，这里把色槽值截到 0~1 再转 Color32（超出部分只能靠材质色表达）。</summary>
        private static Color32 ToColor32(Color c)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f));
        }

        private static void TintMaterial(Material m, Color baseColor)
        {
            if (m == null) return;
            Color tint = baseColor * EmissiveGain;
            m.color = tint;                                  // 内置约定名 _Color
            m.SetColor("_EmissionColor", tint);
            // 这两条与原版戴森壳完全同名（DysonShell.cs:1516-1517）；材质不吃就是空操作
            m.SetColor("_SunColor", sunTint);
            m.SetColor("_DysonEmission", tint);
        }

        private static void FillColors(Mesh m, Color32 c)
        {
            if (m == null) return;
            int n = m.vertexCount;
            if (n <= 0) return;
            Color32[] arr = new Color32[n];
            for (int i = 0; i < n; i++) arr[i] = c;
            m.colors32 = arr;   // 该 Unity 版本上 Mesh.colors 是 Color[]，Color32 要走 colors32
        }

        /// <summary>
        /// 候选链只解析一次，然后按色槽实例化四块。必须实例化：这些材质与游戏原版戴森壳/编辑器共用，
        /// 直接改色会污染原版渲染；而且四块共用同一块源材质才能各持一个 _Color。
        /// </summary>
        private static void ResolveGlowMaterials()
        {
            Material src = ResolveGlowSourceMaterial();
            if (src == null) return;   // 取不到源材质，日志已在里面打过
            glowMatRing = CloneGlowMaterial(src);
            glowMatBulb = CloneGlowMaterial(src);
            glowMatHalo = CloneGlowMaterial(src);
            glowMatShell = CloneGlowMaterial(src);
        }

        private static Material CloneGlowMaterial(Material src)
        {
            Material m = UnityEngine.Object.Instantiate(src);
            m.hideFlags = HideFlags.HideAndDontSave;
            return m;
        }

        /// <summary>
        /// 发光材质候选链。刻意全部避开 *-inst 系列（node inst / frame inst / sail bullet inst）：
        /// 那些 shader 走 GPU instancing、要从 _InstBuffer 取每实例数据，
        /// 用我们的 TRS 矩阵画会把模型甩到缓冲区里的位置 —— 与之前排除 GenericRenderer.instMats 同一个坑。
        /// </summary>
        private static Material ResolveGlowSourceMaterial()
        {
            Material m = null;
            string src = "无";
            try
            {
                Shader s = Shader.Find("Unlit/Color");
                // TransformGizmo.cs:171 就是在运行时 Find 这个内置 shader 的，说明它没被从发行包里剥掉
                if (s == null) s = Shader.Find("Unlit/GLLines");
                if (s != null) { m = new Material(s); src = "Shader:" + s.name; }
            }
            catch (Exception) { }
            if (m == null)
            {
                try
                {
                    m = Resources.Load<Material>("Dyson Sphere/Materials/gl-color");
                    if (m != null) src = "gl-color";
                }
                catch (Exception) { }
            }
            if (m == null)
            {
                try
                {
                    // 末位兜底：原版壳材质。unlit 至少不灰，但它本意服务于 Points 拓扑的壳网格，观感可能不对
                    m = Resources.Load<Material>("Dyson Sphere/Materials/dyson-shell-unlit-0");
                    if (m != null) src = "dyson-shell-unlit-0";
                }
                catch (Exception) { }
            }
            if (m == null)
            {
                glowSrc = "无(发光层不可用)";
                Utils.Log("[MMS warpModel] 发光材质候选链全部失败，远景将只剩结构层，达不到 R8。");
                return null;
            }
            glowSrc = src + "/" + (m.shader != null ? m.shader.name : "?");
            return m;
        }

        /// <summary>
        /// 发光层提交：与结构层同 layer、同深度策略，区别只在材质是 unlit —— 不受光，自己就是光源。
        /// 逐 submesh 提交：建筑网格有多个 submesh（对应多材质），只画第 0 段会漏掉大半几何；
        /// 而我们自生成的环/光核只有 1 段，循环自然只跑一次。
        /// </summary>
        private static void SubmitGlow(Matrix4x4 matrix, Mesh mesh, Material mat)
        {
            if (mat == null || mesh == null) return;
            int subs = Mathf.Max(1, mesh.subMeshCount);
            for (int i = 0; i < subs; i++)
            {
                Graphics.DrawMesh(mesh, matrix, mat, DysonRenderLayer, null, i, null,
                    UnityEngine.Rendering.ShadowCastingMode.Off, false);
            }
        }

        /// <summary>
        /// 远景诊断的**入口**：第一帧打一条横幅（含 near/far 快照），此后每帧交给 <see cref="ProbeFar"/> 连续复采。
        /// 为什么留一条 near/far 快照：`CameraPose.cs:263-267` 的 near/far 取自 prefab 序列化字段而非代码常量，
        /// 光读源码拿不到真值，只能运行时直读相机。R40/R41 把这条账收齐了：
        /// **far=150000 已实测**（与用户"3.多-4 AU 开始消失"数值对上，且 `UIZS_HUD.cs:293` 等五处写死
        /// `z &lt; 150000f` 互证）；**near 实测 0.20** 却与 `level0.unity` 里四个 `far: 150000` pose 的
        /// `near: 0.6` 矛盾，而全库除 `CameraPose.cs:263` 无人写主相机 near ⇒ 这条矛盾**尚未定性**，
        /// 交给 E23 的周期复采去判"是本机被别的 mod 改过，还是 pose 之外另有出处"。
        /// 判据从 R40 的"相机到恒星有没有越过 far"改成"**模型远侧沿**的视空间 z 有没有越过 far"——
        /// 前者会漏报（恒星心还在圈内、远侧沿早已出界），那正是这次现象的入口。
        /// </summary>
        private static void LogDiagOnce(Vector3 starLocal, float r, float scale, StarData star, DysonSphere sphere)
        {
            Camera cam = null;
            try { cam = GameCamera.main; } catch (Exception) { }
            if (cam == null)
            {
                if (!diagLogged) { diagLogged = true; Utils.Log("[MMS warpModel] 远景诊断：取不到 GameCamera.main"); }
                return;
            }
            float dist = (starLocal - cam.transform.position).magnitude;
            if (!diagLogged)
            {
                diagLogged = true;
                // E23（R41）横幅：把"探针版本号 + 这条 log 出自哪个星系"钉在诊断串最前面。
                // 两个动机都是上一轮欠的账：① 用户原话"我不确定这个 log 是在哪个星系里面打印出来的了"
                // —— 于是从现在起**每一行**探针都自带星系标识，不必再猜；② 上一版 DLL 里明明编进了
                // `远裁剪诊断` 这串（对部署 dll 做 UTF-16 直搜命中偏移 369013），日志里却没有 ⇒
                // 只能归因为"进程起得比 dll 新"或"后退时没再走到"，而版本号一打出来，这两种解释立刻可分。
                Utils.Log("[MMS warpModel] 探针版本=" + ProbeVersion +
                          "（E23：far 连续复采 + 官方壳实时包络 + 星系边界实测；渲染与相机一行未动） " + StarTag(star));
                Utils.Log("[MMS warpModel] 发光层=" + glowSrc + " 色模式=" + glowColorMode +
                          " 增益=" + EmissiveGain + " 结构层=" + showStructure +
                          " 发光层=" + showGlow + " 外壳=" + showShellGlow + " 外扩=" + ShellInflate);
                // 结构层现在是"覆写官方 PBR 旋钮"，这几个值决定观感，出问题时要能一眼对上是哪档调错
                Utils.Log("[MMS warpModel] 结构层PBR覆写=" + useVanillaPbrKnobs +
                          " albedo=" + StructureAlbedoMultiplier + " normal=" + StructureNormalMultiplier +
                          " smooth=" + StructureSmoothMultiplier + " spec=" + StructureSpecularGain +
                          " 掺色=" + StructureTintStrength + " emission=" + StructureEmissionMultiplier +
                          " 发光掺色=" + StructureEmissionTint + " 环境光=" + AmbientGain +
                          " 门控=" + emissionGateMode + (emissionGateMode == EGateMode.Native
                              ? "(两个门都不写)"
                              : "(本体写UsePower=" + (emissionGateMode == EGateMode.FollowPower ? 1 : 0) + ",Switch=" + StructureEmissionSwitchValue +
                          ";特效克隆写Switch=" + NativeEffectEmissionSwitchValue + ")") +
                          // E8 的新增数值一行打完：与官方同值的那一档就是"我没写、由材质序列化值决定"
                          " 逐角色emission增益 环" + RingEmissionGain + "/塔" + NodeEmissionGain + "/收" + SubEmissionGain +
                          " 附加旋钮 金属" + StructureMetallicMultiplier + "(官方1)" +
                          " 自发光mip" + StructureEmissionMipBias + "(官方-2)" +
                          " 抖动" + StructureEmissionJitter + "(官方0)" +
                          " 转速" + StructureRotateSpeed + "(官方1)" +
                          " 剔除" + StructureCullMode + "(-1=不覆写)" +
                          " 特效增益" + NativeEffectGain + "(1=不改官方值)" +
                          " 特效UV" + NativeEffectUvScroll + "(零=不覆写)" +
                          " 主光掺色=" + SunlightTintStrength + " 补光掺色=" + AmbientTintStrength +
                          " 特效件=" + showNativeEffects + " 特效掺色=" + nativeEffectTintStrength +
                          " 特效去VFINST=" + nativeEffectKillVfinst +
                          " 环UV重复=" + RingUvTiles + " 环分段=" + RingMajorSeg);
                // 四槽颜色一并打出来，出问题时能立刻区分"色板算错了"还是"材质没吃到色"
                Utils.Log("[MMS warpModel] 色板 ring=(" + C(glowPal.ring) + ") bulb=(" + C(glowPal.bulb) +
                          ") halo=(" + C(glowPal.halo) + ") shell=(" + C(glowPal.shell) + ")");
                Utils.Log("[MMS warpModel] 远景诊断 near=" + cam.nearClipPlane.ToString("F2") +
                          " far=" + cam.farClipPlane.ToString("F0") + " 相机到恒星=" + dist.ToString("F0") +
                          " 模型半径R=" + r.ToString("F0") + " 缩放=" + scale.ToString("F3") + " " + StarTag(star));
            }
            else
            {
                // E23（R41）：把上一版"每进程只走一次"的阶梯诊断换成**连续复采**的探针，理由见该方法注释
                ProbeFar(cam, star, dist, WarpArrayModelBuilder.GetNominalEnvelope() * scale, sphere);
            }
        }

        /// <summary>
        /// E23（R41）远裁剪**探针**：只在远端按节拍复采，把"far 现在是多少、越界到什么程度、官方壳此刻应该在哪"
        /// 打成一行可读回的行。**只读**相机与 `DysonSphere`，不写 far、不写材质、不参与任何提交决策。
        ///
        /// 为什么要替掉 E21 那条一次性诊断：R41 回来的三条实测里，"3.多-4 AU 开始消失"已经和 `far=150000`
        /// 数值对上了，所以**"有没有越界"不再是问题**；新的问题是"**far 自己会不会随距离变**"——
        /// 它决定 P35 那条正预测（原版壳应当在 `D > far + R壳` 处**整块**不见）成不成立：若 far 会随距离抬上去，
        /// 壳确实能一直看得见，那我上一轮的机制判断就得整个重查。一次性的行答不了这个，必须**一串**。
        ///
        /// 三条判据的形状（缺一不可）：
        /// ① `dist > 60000` 才采：近景根本没越界，占了日志反而把远端那几行淹掉；
        /// ② 每 60 帧一采：用户"一路后退"会拿到一串（正是要的形状），而正常飞行不会刷屏；
        /// ③ 只在 **far 变了** 或 **越界占比跨过新的 10% 档** 时才真打：让"far 恒定"这件事本身成为一条信息，
        ///    而不是让 60 帧一行重复 40 遍同样的数 ⇒ 于是回报里凡是 `far=` 相同、占比递增的行，
        ///    就是把 P34 第 4 节那张数线表**逐项实测**了一遍。
        /// </summary>
        private static void ProbeFar(Camera cam, StarData star, float dist, float env, DysonSphere sphere)
        {
            lastDrawDist = dist;      // 给"星系边界"那行留底
            boundaryLogged = false;   // 重新开画 ⇒ 下一次停画允许再打一行
            if (dist < 60000f) return;
            if (++probeTick % 60 != 0) return;

            float far = cam.farClipPlane;
            if (probeFirstFar < 0f) probeFirstFar = far;
            else if (Mathf.Abs(far - probeFirstFar) > 1f) probeFarChanged = true;

            float zNear = dist - env, zFar = dist + env;
            if (zFar <= far) return;                       // 未越界 ⇒ 不打（近处不值得占一条）
            float frac = (zFar - far) / (2f * env);        // 越界几何占比 0~1
            int tier = (int)(frac * 10f);                  // 10% 一档
            if (tier <= probeLoggedTier && Mathf.Abs(far - probeLoggedFar) < 1f) return;   // 没有新信息
            probeLoggedTier = tier;
            probeLoggedFar = far;

            // 视空间 z = 真实距离 × cos(偏轴角)，所以"能救回来"所需的角度是这个反余弦（远侧沿最苛刻）
            float need = Mathf.Acos(Mathf.Clamp(far / zFar, 0f, 1f)) * Mathf.Rad2Deg;
            // 这台相机给得到的最大偏轴角 = 画角（fieldOfView 是**垂直**方向，水平要乘 aspect）
            float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * cam.aspect;
            float corner = Mathf.Atan(Mathf.Sqrt(tanV * tanV + tanH * tanH)) * Mathf.Rad2Deg;

            // ── 官方壳的实时半径包络（P35 第 2 节那张表的"实测替身"）──
            // 只数**真的有内容**的层：`layersIdBased` 里空层的 `orbitRadius` 也有值（`DysonSphereLayer.Init()`
            // 在 :51 写死 10000f，注意不是构造函数 —— :40-45 只填引用），拿它当"用户的壳多大"会虚高 ⇒ 整块消失点算错。
            // 循环上界用 `DysonSphere.kMaxLayerCount`（= 10，`DysonSphere.cs:2081`）而不是硬编码：
            // 数组本身是 `[11]`（`:433`）且下标 0 不用 ⇒ 1..kMaxLayerCount **正好是全部层**，没有"漏数第 11 层"这种风险。
            float shellR = 0f;
            int shellLayers = 0;
            if (sphere != null && sphere.layersIdBased != null)
            {
                for (int i = 1; i <= DysonSphere.kMaxLayerCount && i < sphere.layersIdBased.Length; i++)
                {
                    DysonSphereLayer layer = sphere.layersIdBased[i];
                    if (layer == null) continue;
                    if (layer.nodeCount + layer.frameCount + layer.shellCount <= 0) continue;
                    if (layer.orbitRadius > shellR) shellR = layer.orbitRadius;
                    shellLayers++;
                }
            }
            string shellPart;
            if (shellLayers == 0)
            {
                shellPart = " 壳层=无内容";
            }
            else
            {
                float dieAt = far + shellR;                                  // D 超过它，整个壳都在 far 之外
                float t = far - dist;                                        // 裁剪面在壳心之前(+)/之后(-)多少米
                float rim = shellR;                                          // 此刻仍可见的外沿半径
                if (t <= -shellR) rim = 0f;
                else if (t < shellR) rim = Mathf.Sqrt(Mathf.Max(0f, shellR * shellR - t * t));
                shellPart = " 壳层R=" + shellR.ToString("F0") + "(" + (shellR / 40000f).ToString("F2") + "AU)" +
                            " 按far应整块不见于D>" + dieAt.ToString("F0") + "(" + (dieAt / 40000f).ToString("F2") + "AU)" +
                            " 此刻壳可见外沿=" + rim.ToString("F0") + "(" + (rim / shellR * 100f).ToString("F1") + "%)" +
                            (dist > dieAt ? "【现在早该看不见】" : "【现在应仍可见】");
            }

            Utils.Log("[MMS farProbe] far=" + far.ToString("F0") + (probeFarChanged ? "(在变)" : "(恒定)") +
                      " near=" + cam.nearClipPlane.ToString("F2") +
                      " 相机到恒星=" + dist.ToString("F0") + "(" + (dist / 40000f).ToString("F2") + "AU)" +
                      " 模型外沿=" + env.ToString("F0") +
                      " 视空间z区间=[" + zNear.ToString("F0") + "," + zFar.ToString("F0") + "]" +
                      " 越界占比=" + (frac * 100f).ToString("F0") + "% " +
                      "救回需偏轴角≥" + need.ToString("F1") + "° 而画角只有" + corner.ToString("F1") + "°(" +
                      cam.fieldOfView.ToString("F0") + "°×" + cam.aspect.ToString("F2") + ") ⇒ " +
                      (need <= corner
                          ? "【平面裁剪自洽：正视被裁、转到画面边缘应能救回，与 R41 描述一致】"
                          : "【连画面边缘也救不回：与平面裁剪不符，说明另有机制，我重查】") +
                      shellPart + " " + StarTag(star) + " 探针版本=" + ProbeVersion);
        }

        /// <summary>
        /// 日志用星系标识。E23 起**每一行**远端诊断都带上它，起因是 R41 那句
        /// "我不确定这个 log 是在哪个星系里面打印出来的了" —— 上一轮的量测因此无法定位到具体恒星。
        /// 而"在哪颗星上量的"直接决定尺度该怎么解读：巨星走 `StarGen.SetStarAge :1017-1025` 的
        /// `radius = num4 * num2`（代进去约 11~23），出生星只有 [0.82, 1.22]（`StarGen.cs:343-345`）
        /// ⇒ 自适应 scale 差一个数量级，把两类星的数据混在一起看必然得出错误结论。
        /// 三个字段刻意全打：`displayName` 给人看；`index` 是全宇宙恒星序号、`id` 是本存档的星系号
        ///（门控 `GameLogicDrawPostfix` 对两者都比，所以两个都得在日志里）；`typeString` 直接回答
        /// "这台是不是红巨星"（`StarData.cs:99-134`：巨星按光谱分红/黄/白/蓝四档，且走 `.Translate()` ⇒ 跟随游戏语言）。
        /// 三个都是只读属性（`StarData.cs:10-19`、`:99-134` 是纯字段读取与字符串拼接），不改任何状态。
        /// </summary>
        private static string StarTag(StarData star)
        {
            if (star == null) return "星系=未知";
            return "星系=" + star.displayName + "(index=" + star.index + ",id=" + star.id + "," + star.typeString + ")";
        }

        /// <summary>日志用：把颜色压成 r,g,b 两位小数，方便一眼比对四槽色差。</summary>
        private static string C(Color c)
        {
            return c.r.ToString("F2") + "," + c.g.ToString("F2") + "," + c.b.ToString("F2");
        }

        /// <summary>
        /// 一次性材质属性探测（S8b，第十轮 E8 大改）。动机有两层：
        /// ① 旧坑"材质序列化字段 ≠ shader 声明的属性，SetFloat 到未声明的名字上是**静默无操作**"，
        ///    所以只能运行时 HasProperty 逐个探（`ray-receiver.mat` 现场就带着这种遗留字段）。
        /// ② 第十轮暴露的**更严重的坑**：探测名单是我手写死的 13 项，于是日志里"可用旋钮: 一个都没有"
        ///    其实是"我一个都没探"⇒ 我曾据此把 glass/additive 判成无可调项（P24，三条结论作废）。
        ///    现在名单取自这五支 shader 的 Properties **原文并集**，并且**连当前值一起打**，
        ///    日志从此直接回答"还能拧什么、官方拧到哪"，不必再靠 A/B 反推。
        /// 顺带打出 shader 实名与 shaderKeywords，用来坐实 _ENABLE_VFINST 那条疑点（§5.3 推论 4）。
        /// </summary>
        private static void LogMaterialPropsOnce()
        {
            if (propLogged) return;
            propLogged = true;
            try
            {
                LogMatSet("环材质", ringMats);
                LogMatSet("发生器材质", nodePart.mats);
                LogMatSet("接收器材质", subPart.mats);
            }
            catch (Exception e) { Utils.Log("[MMS warpModel] 材质属性探测异常: " + e.Message); }
        }

        private static void LogMatSet(string tag, Material[] mats)
        {
            if (mats == null) return;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;   // 被摘给特效层的 submesh（drawMats 里的 null，见 P22）不必探测
                string[] kw = m.shaderKeywords;
                Utils.Log("[MMS warpModel] " + tag + "[" + i + "]=" + m.name + "/" +
                          (m.shader != null ? m.shader.name : "无shader") +
                          " kw=" + (kw == null || kw.Length == 0 ? "无" : string.Join(",", kw)));
                Utils.Log("[MMS warpModel] " + tag + "[" + i + "] 声明的旋钮与当前值: " + Probe(m, ProbeNames));
            }
        }

        // ── 探测名单 = 这五支 shader 的 Properties **并集**（原文来自 _ref/AssetRiperExport 的 vff standard /
        //    vff standard black vertex rotate / vff ray receiver / vff glass rayreceiver /
        //    vff unlit ray receiver gravity，整理见 §P24）。
        //    第十轮之前的名单只有 13 项，于是"可用旋钮: 一个都没有"其实是"我一个都没探"——
        //    那次的误判直接导致我把 glass/additive 判成"无可调项"，而现在证明它们各有 4 个和 18 个数值。
        //    ⇒ 名单必须宽过我们已经在写的，否则日志只会重复我已知的东西。
        private static readonly string[] ProbeNames = {
            // 本体三兄弟共用的 PBR 组
            "_Color", "_SpecularColor", "_EmissionMask", "_EmColor1", "_EmColor2",
            "_AlbedoMultiplier", "_NormalMultiplier", "_MetallicMultiplier", "_SmoothMultiplier",
            "_EmissionMultiplier", "_NormalMipBias", "_EmissionMipBias", "_EmissionJitter",
            "_EmissionSwitch", "_EmissionUsePower", "_AlphaClip", "_CullMode", "_RotateSpeed",
            // glass / additive 特效组的数值
            "_TintColor", "_Multiplier", "_AlphaMultiplier", "_AuroraMultiplier", "_PScle", "_WrpScale",
            "_SideFade", "_InvFade",
            "_UVSpeed", "_Beam", "_Aurora", "_AuroraMask", "_NoiseScale", "_Particle", "_Circle",
            "_AuroraColor", "_GravityLenColor", "_DarkFogLenColor", "_PolarColor", "_Pivot", "_TestDirection",
            // E9 补：塔那几支 shader 独有的标量（类型逐个从 AssetRipper 导出的 Properties 原文核对，不是猜的）
            //   vff space enemy triplanar building tower 2.shader:  _TriplanarTileSize Float=20 /
            //     _TriplanarNormalMultiplier Float=1 / _HDR_Fence Float=0.55 / _VertexAOUse Range(0,1)=1 /
            //     _VertexEmUse Range(0,2)=1
            //   vff standard dfground tower1.shader:                _LocalScale Float=1（显示名"整体缩放"）
            // 只探测、不覆写：这一轮先看运行时实况再决定要不要动它们（R28）。
            "_TriplanarTileSize", "_TriplanarNormalMultiplier", "_HDR_Fence", "_VertexAOUse", "_VertexEmUse", "_LocalScale",
        };
        private static readonly string[] ProbeVecNames = {
            "_UVSpeed", "_Beam", "_Aurora", "_AuroraMask", "_NoiseScale", "_Particle", "_Circle",
            "_Pivot", "_TestDirection",
            "_UGI_Strength",   // E9：Properties 原文是 Vector (0.2,0.02,1,0.45)，按 float 读会得到误导值 ⇒ 走向量分支
        };
        private static readonly string[] ProbeColorNames = {
            "_Color", "_SpecularColor", "_EmissionMask", "_EmColor1", "_EmColor2", "_TintColor",
            "_AuroraColor", "_GravityLenColor", "_DarkFogLenColor", "_PolarColor",
        };

        /// <summary>
        /// 把该材质**真正声明过**的属性挑出来，并连带打出当前值（HasProperty 读的是 shader 属性表，不是材质序列化字段）。
        /// 只读不写：GetFloat/GetColor/GetVector 都作用在游戏资产材质的序列值副本上，不改任何全局状态。
        /// 打出值的意义（E8①）：从此"还能拧什么"与"官方拧到哪"一次看清，不必再靠猜与 A/B 反推。
        /// </summary>
        private static string Probe(Material m, string[] names)
        {
            string s = "";
            for (int i = 0; i < names.Length; i++)
            {
                string n = names[i];
                try
                {
                    if (!m.HasProperty(n)) continue;
                    string v;
                    if (Array.IndexOf(ProbeVecNames, n) >= 0)
                    {
                        Vector4 vv = m.GetVector(n);
                        v = "(" + vv.x.ToString("0.###") + "," + vv.y.ToString("0.###") + "," +
                            vv.z.ToString("0.###") + "," + vv.w.ToString("0.###") + ")";
                    }
                    else if (Array.IndexOf(ProbeColorNames, n) >= 0)
                    {
                        Color cc = m.GetColor(n);
                        v = "(" + cc.r.ToString("0.##") + "," + cc.g.ToString("0.##") + "," +
                            cc.b.ToString("0.##") + "," + cc.a.ToString("0.##") + ")";
                    }
                    else v = m.GetFloat(n).ToString("0.###");
                    s += (s.Length == 0 ? "" : "  ") + n + "=" + v;
                }
                catch (Exception) { }
            }
            return s.Length == 0 ? "无（这支 shader 不在上面那份并集里，需补 Properties 原文）" : s;
        }

        /// <summary>
        /// 发光环体：只含细圆环。光核被拆到 BuildGlowBulbs 单独成网格 —— 命中链上的材质只读 _Color，
        /// 同一张网格只能吃一个颜色，想给光核单开一个色槽就必须分开画（两者共用同一矩阵，位置严格同步）。
        /// </summary>
        private static Mesh BuildGlowRing()
        {
            List<Vector3> vs = new List<Vector3>();
            List<Vector3> ns = new List<Vector3>();
            List<int> tris = new List<int>();
            AppendTorus(vs, ns, tris, GlowTubeRatio, 96, 8);
            return FinishGlowMesh(vs, ns, tris, "MMS_WarpGlowRing");
        }

        /// <summary>
        /// 环上光核：GlowBulbCount 个八面体烘进同一张网格，随环一起进动，所以一条环只 1 次 DrawMesh。
        /// 光核才是远景可见性的主力：一个亮点对亚像素尺寸最鲁棒，bloom 开启时还会被扩散成若干个像素。
        /// 网格以大圆半径 1 构建，与发光环共用同一套缩放。
        /// </summary>
        private static Mesh BuildGlowBulbs()
        {
            List<Vector3> vs = new List<Vector3>();
            List<Vector3> ns = new List<Vector3>();
            List<int> tris = new List<int>();
            for (int i = 0; i < GlowBulbCount; i++)
            {
                float a = i * Mathf.PI * 2f / GlowBulbCount;
                AppendOctahedron(vs, ns, tris, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), GlowBulbRatio);
            }
            return FinishGlowMesh(vs, ns, tris, "MMS_WarpGlowBulb");
        }

        /// <summary>能量环（套在塔/接收器外面的亮线圈）：中间是空的，所以近景不会把结构层整个吞掉。</summary>
        private static Mesh BuildGlowHalo()
        {
            List<Vector3> vs = new List<Vector3>();
            List<Vector3> ns = new List<Vector3>();
            List<int> tris = new List<int>();
            AppendTorus(vs, ns, tris, GlowHaloTubeRatio, 40, 8);
            return FinishGlowMesh(vs, ns, tris, "MMS_WarpGlowHalo");
        }

        /// <summary>圆环贴片：大圆半径固定为 1、躺在 XZ 平面（法线 +Y），与结构环用同一套姿态约定。</summary>
        private static void AppendTorus(List<Vector3> vs, List<Vector3> ns, List<int> tris, float tube, int major, int minor)
        {
            int base0 = vs.Count;
            for (int i = 0; i <= major; i++)
            {
                float u = i * Mathf.PI * 2f / major, cu = Mathf.Cos(u), su = Mathf.Sin(u);
                for (int j = 0; j <= minor; j++)
                {
                    float w = j * Mathf.PI * 2f / minor, cw = Mathf.Cos(w), sw = Mathf.Sin(w);
                    Vector3 n = new Vector3(cw * cu, sw, cw * su);
                    vs.Add(new Vector3(cu, 0f, su) + n * tube);
                    ns.Add(n);
                }
            }
            int ring = minor + 1;
            // 绕序必须是 (a,c,b)/(a,d,c)：按 (a,b,c) 连出来的几何法线指向环心，而顶点法线设的是朝外，
            // 两者差 180° ⇒ 在材质 _CullMode=2(Back) 下【外壁被当背面剔除】，管外看永远是凹槽、
            // 相机进管内才看得到完整管道（P12，用户实测"贴图好像朝里的"）。法线保持朝外不动，只翻索引。
            for (int i = 0; i < major; i++)
                for (int j = 0; j < minor; j++)
                {
                    int a = base0 + i * ring + j, b = a + ring, c = b + 1, d = a + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(a); tris.Add(d); tris.Add(c);
                }
        }

        private static void AppendOctahedron(List<Vector3> vs, List<Vector3> ns, List<int> tris, Vector3 ctr, float rad)
        {
            Vector3 px = ctr + new Vector3(rad, 0f, 0f), nx = ctr + new Vector3(-rad, 0f, 0f);
            Vector3 py = ctr + new Vector3(0f, rad, 0f), ny = ctr + new Vector3(0f, -rad, 0f);
            Vector3 pz = ctr + new Vector3(0f, 0f, rad), nz = ctr + new Vector3(0f, 0f, -rad);
            AppendTri(vs, ns, tris, ctr, px, py, pz);
            AppendTri(vs, ns, tris, ctr, py, nx, pz);
            AppendTri(vs, ns, tris, ctr, nx, ny, pz);
            AppendTri(vs, ns, tris, ctr, ny, px, pz);
            AppendTri(vs, ns, tris, ctr, py, px, nz);
            AppendTri(vs, ns, tris, ctr, nx, py, nz);
            AppendTri(vs, ns, tris, ctr, ny, nx, nz);
            AppendTri(vs, ns, tris, ctr, px, ny, nz);
        }

        /// <summary>三角面追加，并按"外法线"自动纠正绕序：绕序反了会被背面剔除吃掉半个模型，肉眼很难归因。</summary>
        private static void AppendTri(List<Vector3> vs, List<Vector3> ns, List<int> tris, Vector3 ctr, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(n, (a + b + c) / 3f - ctr) < 0f)
            {
                Vector3 swap = b; b = c; c = swap; n = -n;
            }
            n = n.normalized;
            int i = vs.Count;
            vs.Add(a); ns.Add(n);
            vs.Add(b); ns.Add(n);
            vs.Add(c); ns.Add(n);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        private static Mesh FinishGlowMesh(List<Vector3> vs, List<Vector3> ns, List<int> tris, string name)
        {
            Mesh m = new Mesh();
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            m.SetVertices(vs);
            m.SetNormals(ns);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            // 照抄原版做法（DysonShell.cs:1279）：巨构尺度下把包围盒放大，杜绝按包围盒的视锥误剔除。
            // 红线：只改我们自己生成的网格 —— 游戏共享资产的 bounds 绝对不能动，否则会破坏原版建筑的剔除。
            m.bounds = new Bounds(m.bounds.center, new Vector3(2000000f, 2000000f, 2000000f));
            m.name = name;
            m.hideFlags = HideFlags.HideAndDontSave;
            return m;
        }

        // E14（R34）：这里原先那两份"布点表"（OctaDirs = 正八面体 6 顶点、CubeDirs = 立方体 8 顶点）
        // 已经搬去 `WarpArrayModelBuilder` —— **往哪些位置放预制件**属于"模型创建"，不再藏在渲染器里。
        // 渲染器从此只知道"层 → 条目 → 提交"，不知道"六座塔"或"八座接收器"这种布局事实。

        // ---------------------------------------------------------------- 资源解析

        /// <summary>
        /// 从游戏已加载的资产里挑部件：LDB.models 里的建筑网格与材质都是原版运行时产物，
        /// 直接复用即可拿到与游戏建筑完全一致的光照/雾/着色表现，无需我们自己造材质。
        /// </summary>
        private static void ResolveAssets()
        {
            resolved = true;
            try
            {
                // 环几何：默认程序化生成。游戏自带的 Resources/models/Torus 管径约占外径 10%，
                // 放到 R≈4000 米的巨构尺度上会粗成"泳圈"，反而不像折跃骨架；自己生成可把管径压到 3.5%，
                // 观感接近原版戴森框架梁。材质仍然全部取自游戏原生建筑 —— 那才是决定"像不像游戏"的部分。
                // 想对比原生圆环观感时，把 useVanillaTorus 改成 true 即可。
                if (useVanillaTorus)
                {
                    GameObject torusGo = Resources.Load<GameObject>("models/Torus");
                    if (torusGo != null)
                    {
                        MeshFilter mf = torusGo.GetComponent<MeshFilter>();
                        Mesh m = mf != null ? mf.sharedMesh : null;
                        if (m != null && m.vertexCount > 0)
                        {
                            ringPart.mesh = m;
                            // 该网格躺在 XY 平面（其 prefab 自身带 -90° X 旋转），转到 XZ 平面以便与我们的倾角参数一致
                            ringBaseRot = Quaternion.Euler(90f, 0f, 0f);
                            ringPart.norm = Mathf.Max(0.01f, Mathf.Max(m.bounds.extents.x, m.bounds.extents.y));
                        }
                    }
                }
                if (ringPart.mesh == null)
                {
                    // E14：默认那一档的网格在这里建好并**静态持有**（proceduralRing），
                    // ModelCatalog 的环缓存命中同一 tubeRatio 时就直接复用它 ⇒ 三环仍是同一张网格、逐像素不变。
                    proceduralRing = BuildProceduralRing(RingMajorSeg, RingMinorSeg, RingTubeRatio);
                    ringPart.mesh = proceduralRing;
                    ringPart.norm = 1f + RingTubeRatio;   // 外延 = 大圆半径 1 + 管半径，配合 RingRadiusRatio 即得目标半径
                    ringBaseRot = Quaternion.identity;
                }

                // 材质与部件模型：各角色沿候选链取第一个可用的
                // 环只**借**对撞机的材质：我们的环是程序化几何，特效子件的坐标长在对撞机自己的形状上、
                // 映射不过来 ⇒ 明确不采集环的特效件（否则克隆一堆材质白挂着，日志还会误导"环有特效"）
                Part matSrc = FindPartFirst(RingMatKeys, false);
                ringMats = matSrc.mats;
                // E9①（P27）：塔**先按游戏自己的显示名反查**，命中即用；查不到才退回 prefabPath 候选链。
                // 顺序不能反过来 —— tesla-tower-1 这座建筑在本版本叫"电力感应塔"，不是用户要的"无线输电塔"，
                // 靠文件名候选链挑 = 靠我的猜测定建筑身份，正是 R28 要禁止的那类做法。
                nodePart = preferAnimatedNodeTower ? FindPartByModelName(NodeModelName) : default(Part);
                if (nodePart.mesh == null) nodePart = FindPartFirst(NodeKeys);
                // R32：接收器整层不要了 ⇒ 连资产解析也一起跳过（省一次 prefab 装载 + 一趟特效件采集）。
                // SubKeys 与绘制代码（现在是"层级遍历"里的通用一条）都留着：Builder 里放一条 RayReceiver 即完整复原。
                // E14（R34）改判：**"要不要这只 prefab"不再由渲染器开关决定，而是看 Builder 有没有任何层用到它**
                //（当前 WarpArrayModelBuilder 一个 RayReceiver 都没放 ⇒ needReceiver=false ⇒ 与 R32 行为一致）。
                needReceiver = ModelHandler.AnyLayerUses((int)ModularModels.RayReceiver);
                subPart = needReceiver ? FindPartFirst(SubKeys) : default(Part);
                // E9③：塔的动画数据在这里落地（verta 的 GPU buffer 是懒创建的，必须我们自己补那一次上传）
                SetupAnim(nodePart);
                // 特效件的克隆材质此刻才既齐备又可能有有效色板：首帧 ResolveAssets 早于色板块，
                // 拿不到色板时 PaintNativeEffects 内部会自己跳过，等色板脏标记那条路补涂。
                PaintNativeEffects(glowPal);

                // 环自身没有配套材质：退而借用同组其它原生建筑的材质，保持整组"游戏原生皮肤"的统一感
                if (ringMats == null || ringMats.Length == 0) ringMats = nodePart.mats;
                if (ringMats == null || ringMats.Length == 0) ringMats = subPart.mats;

                // R32：关闭的那一层不能算进"未取到"，否则日志会把"你不要它了"报成"资产缺失"，误导下一轮排查
                bool anyMissing = ringMats == null || nodePart.mesh == null || (needReceiver && subPart.mesh == null);
                if (anyMissing && !resolveFailedLogged)
                {
                    resolveFailedLogged = true;
                    Utils.Log("[MMS warpModel] 部分原生模型未取到(ringMat=" + (ringMats != null) +
                              ", node=" + (nodePart.mesh != null) + ", sub=" + (needReceiver ? (subPart.mesh != null ? "有" : "缺") : "Builder未用(R32)") +
                              ")，折跃场将按可得部件渲染。");
                }
                // 一次性摘要：出问题时第一眼就能判断是"没取到资产"还是"没进对星系"
                Utils.Log("[MMS warpModel] 组装完成 环=" + (ringPart.mesh != null ? ringPart.mesh.vertexCount + "顶点" : "无") +
                          " 材质(" + (matSrc.hit ?? "缺") + ")=" + (ringMats != null ? ringMats.Length + "项" : "缺") +
                          " 发生器(" + (nodePart.hit ?? "缺") + (string.IsNullOrEmpty(nodePart.displayName) ? "" : "|显示名:" + nodePart.displayName) + ")=" + (nodePart.mesh != null ? nodePart.mesh.vertexCount + "顶点" : "缺") +
                          " 接收器(" + (needReceiver ? (subPart.hit ?? "缺") : "未使用") + ")=" + (needReceiver ? (subPart.mesh != null ? subPart.mesh.vertexCount + "顶点" : "缺") : "-") +
                          " 特效件 发生器" + (nodePart.pieces == null ? 0 : nodePart.pieces.Length) +
                          "[" + (nodePart.pieceNote ?? "未采集") + "]" +
                          " / 接收器" + (needReceiver ? (subPart.pieces == null ? 0 : subPart.pieces.Length) + "[" + (subPart.pieceNote ?? "未采集") + "]" : "层级未使用") +
                          "（环是程序化几何，不套特效件；'主网格摘出N件'表示 N 个特效 submesh 已改由自用克隆材质绘制，见 P22）" +
                          (showNativeEffects ? "" : "【特效层已关】"));
                // 材质能不能吃我们想覆写的旋钮，只有运行时问 shader 才知道（依据见 LogMaterialPropsOnce 的注释）
                LogMaterialPropsOnce();
                // 模型清单：把 id + PrefabPath + **显示名 + verta 有无**一起打出来。
                // 上一轮这条清单只给英文路径名，导致我按文件名猜建筑身份、把"电力感应塔"当成"特斯拉塔"（P27 已定案是错的）
                // ⇒ 现在连游戏自己的显示名都打出来，"我们画的到底是哪座塔"变成日志里可直接核对的事实，不需要我再推测。
                LogModelNameMatches();
                // E15（R35）：切模型之前的**取证** —— 把主网格的 Y 分布、submesh 划分、verta 与顶点数的对应关系打成日志。
                // 一次性、只读、不进渲染路径；开关是 AnatomyDumpEnabled。
                DumpGeometryAnatomy();
            }
            catch (Exception e)
            {
                if (!resolveFailedLogged)
                {
                    resolveFailedLogged = true;
                    Utils.Log("[MMS warpModel] 解析原生模型异常: " + e.Message);
                }
            }
        }

        /// <summary>模型表读取：modelArray 按 ID 索引且可能有空洞(ModelProtoSet.OnAfterDeserialize)，dataArray 作兜底。</summary>
        private static ModelProto[] ModelArray()
        {
            if (LDB.models == null) return null;
            return LDB.models.modelArray ?? LDB.models.dataArray;
        }

        /// <summary>按 PrefabPath 尾部名在 LDB.models 里查部件，取 lod0 网格与配套材质。</summary>
        /// <param name="wantEffects">false 则不采集特效子件（环只借材质用，特效件位置长在原建筑形状上、映射不到自造圆环）</param>
        private static Part FindPartByPrefabKey(string key, bool wantEffects = true)
        {
            Part p = default(Part);
            ModelProto[] arr = ModelArray();
            if (arr == null) return p;
            string tail = "/" + key;
            for (int i = 0; i < arr.Length; i++)
            {
                ModelProto proto = arr[i];
                if (proto == null || string.IsNullOrEmpty(proto.PrefabPath)) continue;
                if (!proto.PrefabPath.EndsWith(tail, StringComparison.OrdinalIgnoreCase)) continue;
                p = BuildPart(proto, proto.prefabDesc, key, wantEffects);
                if (p.mesh != null) return p;
            }
            return p;
        }

        /// <summary>
        /// E9①：按**游戏自己的显示名**反查部件（依据 P27，不再用文件名猜建筑）。
        /// 本地化原文（Locale\2052\base.txt:4115/4128）证明"电力感应塔"与"无线输电塔"是**两个建筑**，
        /// 而 prefab 名 tesla-tower-1 对应的其实是电力感应塔 ⇒ 只按 prefabPath 候选链挑塔必然挑错。
        /// 名字来源是原文而非我的译法：ModelProto.displayName = OverrideName.Translate() 否则 base.sid
        /// （ModelProto.cs:10-20），sid 由 `base.sid = this.SID.Translate()` 在 Preload():70 赋值 ⇒ 运行时它就是个本地化串。
        /// 用"包含"而非相等：允许命中"高级无线输电塔"这类变体；命中的到底是哪条 prefabPath 一律打日志，
        /// 让我对建筑身份的判断在日志里可被推翻（R28）。查不到返回空 Part，调用方退回 prefabPath 候选链。
        /// </summary>
        private static Part FindPartByModelName(string name, bool wantEffects = true)
        {
            Part p = default(Part);
            if (string.IsNullOrEmpty(name)) return p;
            ModelProto[] arr = ModelArray();
            if (arr == null) return p;
            for (int i = 0; i < arr.Length; i++)
            {
                ModelProto proto = arr[i];
                if (proto == null) continue;
                string dn;
                try { dn = proto.displayName; } catch (Exception) { dn = null; }
                if (string.IsNullOrEmpty(dn) || dn.IndexOf(name, StringComparison.Ordinal) < 0) continue;
                p = BuildPart(proto, proto.prefabDesc, name, wantEffects);
                if (p.mesh == null) continue;
                if (!nameLogged)
                {
                    nameLogged = true;
                    Utils.Log("[MMS warpModel] 按显示名命中模型 \"" + dn + "\" 匹配词=" + name +
                              " path=" + proto.PrefabPath +
                              " verta=" + (p.verta != null ? "有(顶点" + p.verta.vertexCount + "帧" + p.verta.frameCount + ")" : "无") +
                              " anim prepare/working=" + p.animPrepare + "/" + p.animWorking +
                              "（P27：显示名是运行时本地化串，命中即证明这就是用户要的那座塔）");
                }
                return p;
            }
            if (!nameLogged)
            {
                nameLogged = true;
                Utils.Log("[MMS warpModel] 按显示名未命中 \"" + name + "\" ⇒ 退回 prefabPath 候选链。" +
                          "若下面'可选模型清单'里那座塔的显示名与本版本不同（语言/版本差异），就改 NodeModelName 这一行。");
            }
            return default(Part);
        }

        /// <summary>
        /// 从命中的 proto/desc 组装部件。两条查询路径（按 prefabPath 名、按显示名）共用这一份，
        /// 保证 E9 新增的动画字段不会因为走另一条路而漏填。
        /// verta 与 anim 初值的出处都是原文：
        ///   lodVertas[0] —— PrefabDesc.cs:269/296-298（逐 LOD 装载，**.verta 文件读不到就留 null** 并 LogError），
        ///     官方也是这么配给渲染器的：ObjectRenderer.cs:54 `new BatchRenderer(…, lodVertas[j], …)`；
        ///   anim_prepare_length / anim_working_length —— PrefabDesc.cs:695-704（无 AnimDesc 组件时 0/1），
        ///     与 PlanetFactory.cs:1369-1373 给真实建筑建 AnimData 初值用的是同一对字段。
        /// </summary>
        private static Part BuildPart(ModelProto proto, PrefabDesc pd, string hitName, bool wantEffects)
        {
            Part p = default(Part);
            if (pd == null) return p;

            Mesh mesh = pd.lodMeshes != null && pd.lodMeshes.Length > 0 ? pd.lodMeshes[0] : pd.mesh;
            Material[] mats = pd.lodMaterials != null && pd.lodMaterials.Length > 0 ? pd.lodMaterials[0] : pd.materials;
            if (mesh == null || mesh.vertexCount == 0) return p;

            p.mesh = mesh;
            p.mats = mats;
            p.drawMats = mats;
            p.center = mesh.bounds.center;
            p.hit = hitName;
            p.norm = Mathf.Max(0.01f, Mathf.Max(
                Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.y), mesh.bounds.extents.z));
            // E9：动画三件套。索引必须与上面取网格用的 LOD 对齐（PrefabDesc 里 lodMeshes/lodVertas 同索引配对）
            p.verta = pd.lodVertas != null && pd.lodVertas.Length > 0 ? pd.lodVertas[0] : null;
            p.animPrepare = pd.anim_prepare_length;
            p.animWorking = pd.anim_working_length;
            try { p.displayName = proto.displayName; } catch (Exception) { p.displayName = null; }
            // 特效子件必须**接着这次命中**取：同一个 proto 才有"主网格 transform"这个基准，
            // 换个 proto 去翻 prefab 就会把特效挂到错误的空间上。
            if (wantEffects)
            {
                string effNote;
                p.pieces = CollectEffectPieces(proto, mesh, mats, out effNote);
                // P22：合并网格 ⇒ 特效就是主网格的几个 submesh，"覆盖去重"把它们交回结构层，
                // 于是我们对特效**再无独立抓手**。这里把它们摘出来交给自用克隆材质：
                // 结构层数组(drawMats)对应位置置 null 跳过，克隆体单独吃门控/关键字/染色。
                Material[] drawMats;
                EffectPiece mainEff = SplitMainMeshEffects(mesh, mats, out drawMats);
                if (mainEff != null)
                {
                    p.drawMats = drawMats;
                    p.pieces = PrependPiece(mainEff, p.pieces);
                    effNote = (string.IsNullOrEmpty(effNote) ? "" : effNote + " + ") + mainEff.tag;
                }
                p.pieceNote = effNote;
            }
            return p;
        }

        /// <summary>沿候选链取第一个能命中的部件（网格或材质任一存在即算命中）。</summary>
        private static Part FindPartFirst(string[] keys, bool wantEffects = true)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                Part p = FindPartByPrefabKey(keys[i], wantEffects);
                if (p.mesh != null || p.mats != null) return p;
            }
            return default(Part);
        }

        /// <summary>
        /// 采集建筑模型自带的原生特效子件（修 P16；归类判据由 P18 在第八轮改判）。
        /// 依据：PrefabDesc 除 mesh/materials 外还带着整只 prefab（VFPreload.cs:361 会预载它），
        /// 而接收光效这类特效是挂在里面的【子网格 + 特效材质】。
        /// 【关键事实】对多 renderer 的建筑，`PrefabDesc.mesh` 是把所有 renderer **合并成的一张网格**，
        /// `materials` 是扁平化的材质数组（实测 ray-receiver：3913 + 164 = 4077 顶点、材质 4 项含 eff）
        /// ⇒ 特效件其实**已经被主网格画过了**，于是"网格 ≠ 主网格"这种引用判等必然失效，
        ///   把主体当特效件重画第二遍（那层玻璃罩不吃我们的 MPB，就留下用户看到的"蓝色发光圆环"）。
        /// 新判据与形态无关：**这个 renderer 的材质在不在主网格的材质数组里** —— 在 = 已画，跳过；
        /// 不在 = 主网格没覆盖它（真·独立特效件，如未合并的 *-eff / line 件）⇒ 只补画那一个 submesh。
        /// 于是单 renderer 建筑与合并网格建筑走的是同一套规则，既不会重画也不会漏画。
        /// 局部矩阵仍以主网格 transform 为基准，保证与我们的部件矩阵同空间；
        /// 材质一律 Instantiate 出克隆体自用：既不污染游戏资产，又能按色板染 _Color。
        /// </summary>
        private static EffectPiece[] CollectEffectPieces(ModelProto proto, Mesh mainMesh, Material[] mainMats, out string note)
        {
            note = "";
            if (!showNativeEffects) return null;
            try
            {
                PrefabDesc pd = proto.prefabDesc;
                GameObject pref = pd == null ? null : pd.prefab;
                if (pref == null)
                {
                    note = "无prefab";
                    Utils.Log("[MMS warpModel] 特效件采集跳过 " + proto.PrefabPath + "：prefabDesc.prefab 为空"
                        + "（该版本的特效件只能改走 Resources/AssetBundle 路线，见 §5.4 备注）");
                    return null;
                }
                LogPrefabTree(proto, pref);

                Renderer[] rs = pref.GetComponentsInChildren<Renderer>(true);
                if (rs == null || rs.Length <= 1)
                {
                    // 单 renderer 的建筑（实测 tesla-tower-1 就是）：整个 prefab 只有主网格，**原版就没有特效件**（P20）
                    note = "prefab仅主网格1件";
                    return null;
                }

                // 基准：主网格所在的那个 transform。找不到就退回 prefab 根（根一般就是模型局部原点）。
                Transform basis = pref.transform;
                for (int i = 0; i < rs.Length; i++)
                {
                    MeshFilter mf = rs[i] != null ? rs[i].GetComponent<MeshFilter>() : null;
                    if (mf != null && mf.sharedMesh != null && ReferenceEquals(mf.sharedMesh, mainMesh))
                    {
                        basis = rs[i].transform;
                        break;
                    }
                }
                Matrix4x4 basisInv = basis.worldToLocalMatrix;

                // 主网格材质数组 = Submit 那一路已经画过的东西（HashSet 用 UnityEngine.Object 的实例判等）
                HashSet<Material> covered = new HashSet<Material>();
                if (mainMats != null)
                {
                    for (int i = 0; i < mainMats.Length; i++) if (mainMats[i] != null) covered.Add(mainMats[i]);
                }

                List<EffectPiece> list = new List<EffectPiece>(4);
                int nCoveredSkip = 0, nSameMeshSkip = 0;
                for (int i = 0; i < rs.Length; i++)
                {
                    Renderer rd = rs[i];
                    MeshFilter mf = rd != null ? rd.GetComponent<MeshFilter>() : null;
                    if (mf == null) continue;                 // 粒子发射器等无网格件，本轮不处理（E2 已由日志结案）
                    Mesh gm = mf.sharedMesh;
                    if (gm == null || gm.vertexCount == 0) continue;
                    Material[] srcs = rd.sharedMaterials;
                    if (srcs == null || srcs.Length == 0) continue;

                    if (ReferenceEquals(gm, mainMesh))        // 单 renderer 建筑的老判据，仍然成立
                    {
                        nSameMeshSkip++;
                        continue;
                    }

                    // 只挑"主网格材质数组里没有"的那些 submesh —— 其余的 Submit 已经画过，重画就是那个蓝圈（P18）
                    List<int> want = new List<int>(srcs.Length);
                    for (int k = 0; k < srcs.Length; k++)
                    {
                        Material m = srcs[k];
                        if (m == null || covered.Contains(m)) continue;
                        if (k >= gm.subMeshCount) continue;   // 材质数与 submesh 数对不上时不能瞎索引（防御）
                        want.Add(k);
                    }
                    if (want.Count == 0)
                    {
                        nCoveredSkip++;
                        continue;
                    }

                    EffectPiece ep = new EffectPiece();
                    ep.mesh = gm;
                    ep.subs = want.ToArray();
                    ep.mats = new Material[want.Count];
                    ep.baseColors = new Color[want.Count];
                    ep.local = basisInv * rd.transform.localToWorldMatrix;
                    string shaders = "";
                    for (int j = 0; j < want.Count; j++)
                    {
                        Material src = srcs[want[j]];
                        Material own = UnityEngine.Object.Instantiate(src);   // 自用克隆，游戏材质一个字节都不改
                        own.name = src.name + "_mmsEff";
                        // 与发光层的克隆同一惯例：不让它进资源枚举/卸载流程，也不会被误当成场景资产保存
                        own.hideFlags = HideFlags.HideAndDontSave;
                        if (nativeEffectKillVfinst) own.DisableKeyword("_ENABLE_VFINST");
                        // 同一套数值抓手也发给这些"独立 prefab 特效件"的克隆体（E8）：读官方值 + 按增益缩放
                        string knob = BoostNativeEffect(own);
                        ep.mats[j] = own;
                        ep.baseColors[j] = own.HasProperty(ColorId) ? own.color : Color.white;
                        // 归类只写进日志（判据见 IsStructureMaterial）：绘制路径的取舍已经由"主网格材质数组"决定了
                        bool structMat = IsStructureMaterial(src);
                        shaders += (j > 0 ? " | " : "") + src.name + "/" +
                            (src.shader != null ? src.shader.name : "无shader") +
                            "[" + (structMat ? "本体材质" : "特效材质") + "]" +
                            (string.IsNullOrEmpty(knob) ? "" : "{" + knob + "}");
                    }
                    ep.tag = gm.name + "#" + string.Join(",", Array.ConvertAll(ep.subs, x => x.ToString())) +
                        " (" + shaders + ")";
                    list.Add(ep);
                }
                if (list.Count == 0)
                {
                    note = "补画0/覆盖跳过" + nCoveredSkip + "/同主网格" + nSameMeshSkip;
                    Utils.Log("[MMS warpModel] 特效件采集 " + proto.PrefabPath + "：不补画任何子件"
                        + "（同主网格 " + nSameMeshSkip + " 件 / 材质已被主网格覆盖 " + nCoveredSkip + " 件"
                        + " ⇒ 该建筑的特效已经在结构层那一次绘制里，只需确认门控没把它关掉，见 P19）"
                        + (nSameMeshSkip + nCoveredSkip > 0 ? "" : "；prefab 里没有可画的子网格"));
                    return null;
                }
                note = "补画" + list.Count + "/覆盖跳过" + nCoveredSkip + "/同主网格" + nSameMeshSkip;
                Utils.Log("[MMS warpModel] 特效件采集 " + proto.PrefabPath + " 补画 " + list.Count + " 件（同主网格跳过 "
                    + nSameMeshSkip + " 件，材质已被主网格覆盖跳过 " + nCoveredSkip + " 件）：" +
                    string.Join(" ; ", list.ConvertAll(x => x.tag).ToArray()));
                // 这里**不能**染色：采集发生在 FindPartByPrefabKey 内部，此时 nodePart/subPart 还没赋值完、
                // 色板 glowPal 也仍是默认值（全 0），涂上去会把 additive 材质乘成黑色。
                // 统一由 ResolveAssets 末尾与色板脏标记处调用 PaintNativeEffects（那里才保证色板有效）。
                return list.ToArray();
            }
            catch (Exception e)
            {
                note = "采集异常";
                Utils.Log("[MMS warpModel] 特效件采集异常(" + proto.PrefabPath + "): " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// 这支材质是不是"建筑本体"的 PBR 材质（只用于日志归类与判读，绘制路径的取舍靠主网格材质数组，见 P18）。
        /// 判据取运行时事实：本体那三支（PBR Standard / PBR Standard Black Vertex Rotate / PBR Ray Receiver）
        /// 都声明了 _AlbedoMultiplier，而 Black Mask、Unlit Additive … 这些特效/遮罩材质都不声明。
        /// 名字兜底判 PBR：个别特效 shader 也会带 PBR 字样，但它既然声明了 _AlbedoMultiplier 就归本体，无歧义。
        /// </summary>
        private static bool IsStructureMaterial(Material m)
        {
            if (m == null) return false;
            // 判据只看"有没有本体那套 PBR 旋钮"。Properties 原文（P24）已确认三支本体 shader
            // （PBR Standard / PBR Standard Black Vertex Rotate / PBR Ray Receiver）**都声明 _AlbedoMultiplier**，
            // 而 glass / black mask / additive 特效三支一个都不声明 ⇒ 只靠这一个探针就分得干干净净。
            // 原来那条"shader 名含 PBR 就算本体"的兜底**已删**：`PBR Glass RayReceiver` 名字带 PBR，
            // 但它的属性是 `_TintColor/_Multiplier/_AlphaMultiplier/_UVSpeed`，与本体那套毫无重叠 ——
            // 留在结构层里等于"我们对它毫无抓手"（所有写入都是空操作）。摘进特效层后行为完全不变
            // （它本来就没吃到任何结构层覆写），却第一次让我们能调它的亮度/透明度/流动。
            try { return m.HasProperty(AlbedoMulId); } catch (Exception) { }
            return false;
        }

        /// <summary>
        /// 把主网格里**非本体材质**的那些 submesh 摘出来，做成一个用自用克隆材质绘制的特效件（P22 / E5②）。
        /// 为什么必须摘：建筑是多 renderer 合并成一张网格（P18），所以 additive 的接收光效、Black Mask 这些
        /// 特效 submesh 就躺在 `PrefabDesc.materials` 里，只能由结构层用**游戏材质**画一遍 —— 于是
        ///   · `nativeEffectKillVfinst` 即使打开也无效（不能在游戏资产上 DisableKeyword）；
        ///   · 特效与本体被迫共用同一套门控取值；
        ///   · 结构层的 _Color / _AlbedoMultiplier / 冷暖分离全糊在特效上。
        /// 摘出后：`drawMats[那些位置] = null` ⇒ `Submit` 天然跳过（它本来就跳过 null 材质，零额外分支），
        /// 克隆体则由我们独占：可以设两个门、关关键字、按色板染色，且**不影响戴森壳/机甲/玩家建筑**。
        /// 第八轮那个"蓝色发光圆环"恰好证明了一条事实：这些材质**不吃我们的 MPB**时是发光的 ⇒
        /// 摘出来的克隆件仍走 `SubmitEffects` 的"不传 MPB"路径，是最接近那次实测观感的画法。
        /// </summary>
        private static EffectPiece SplitMainMeshEffects(Mesh mesh, Material[] mats, out Material[] drawMats)
        {
            drawMats = mats;
            if (!showNativeEffects) return null;
            if (mesh == null || mats == null || mats.Length == 0) return null;
            try
            {
                int subs = Mathf.Min(mesh.subMeshCount, mats.Length);
                List<int> want = new List<int>(4);
                for (int i = 0; i < subs; i++)
                {
                    Material m = mats[i];
                    if (m == null) continue;
                    if (IsStructureMaterial(m)) continue;    // 本体材质留在结构层，该吃的光照与染色都得吃
                    want.Add(i);
                }
                if (want.Count == 0) return null;

                Material[] dm = new Material[mats.Length];
                Array.Copy(mats, dm, mats.Length);

                EffectPiece ep = new EffectPiece();
                ep.mesh = mesh;
                ep.local = Matrix4x4.identity;               // 就在主网格自己的空间里，不需要任何换算
                ep.subs = new int[want.Count];
                ep.mats = new Material[want.Count];
                ep.baseColors = new Color[want.Count];
                string tags = "";
                for (int k = 0; k < want.Count; k++)
                {
                    int si = want[k];
                    Material src = mats[si];
                    string knob = "";                         // 该克隆件被读到/写到的数值摘要（E8）
                    dm[si] = null;                            // 结构层不再画这一层（避免画两遍）
                    Material own = new Material(src);         // 克隆：绝不修改游戏资产
                    own.hideFlags = HideFlags.HideAndDontSave;
                    try
                    {
                        // 自用克隆体 ⇒ 两个门都能写（P19 的"不许覆写"只约束游戏资产材质）。
                        // Properties 原文（P24）：additive 那支确实声明 _EmissionSwitch（默认 0），
                        // 而 glass 那支连门控都不声明，它认的是 _TintColor/_Multiplier/_AlphaMultiplier/_UVSpeed。
                        if (own.HasProperty(EmissionSwitchId)) own.SetFloat(EmissionSwitchId, NativeEffectEmissionSwitchValue);
                        if (own.HasProperty(EmissionUsePowerId)) own.SetFloat(EmissionUsePowerId, GateValueIgnorePower);
                        if (nativeEffectKillVfinst) own.DisableKeyword("_ENABLE_VFINST");
                        knob = BoostNativeEffect(own);        // 第十轮 E8：这一层从此有真正的数值抓手
                    }
                    catch (Exception) { }
                    ep.subs[k] = si;
                    ep.mats[k] = own;
                    ep.baseColors[k] = own.HasProperty(ColorId) ? own.color : Color.white;
                    string sh = null;
                    try { sh = own.shader != null ? own.shader.name : null; } catch (Exception) { }
                    tags += (k == 0 ? "" : ",") + (src.name ?? "?") + "@" + si + (sh == null ? "" : "(" + sh + ")") +
                            (string.IsNullOrEmpty(knob) ? "" : "[" + knob + "]");
                }
                ep.tag = "主网格摘出" + want.Count + "件:" + tags;
                drawMats = dm;
                Utils.Log("[MMS warpModel] 特效层摘取 " + ep.tag +
                          " ⇒ 这些 submesh 不再由结构层用游戏材质绘制，改由自用克隆材质（门控=" +
                          NativeEffectEmissionSwitchValue + " 去VFINST=" + nativeEffectKillVfinst +
                          " 增益=" + NativeEffectGain + "），见 P22；方括号里是它们各自的官方数值");
                return ep;
            }
            catch (Exception e)
            {
                Utils.Log("[MMS warpModel] 特效层摘取异常: " + e.Message);
                drawMats = mats;
                return null;
            }
        }

        /// <summary>
        /// 特效克隆材质的数值处理（第十轮 E8，回答 R27"还有别的数值设置吗"）。
        /// 关键做法：**先 GetFloat 读官方值，再乘增益** —— 不是凭空赋一个数。
        /// 于是 NativeEffectGain = 1 时画面与上一轮完全一致，但日志已经把每支特效件的官方数值打出来了，
        /// 下一次要加亮/调透明/让贴图流动，改一行就够，而且知道自己在改什么。
        /// 这些属性 glass 与 additive 两支各有子集（Properties 原文见 P24）：写之前一律 HasProperty。
        /// 返回一句摘要给日志（`_Mul=1 _Alpha=0.6 …`，被覆写的那项额外标 `→新值`）。
        /// </summary>
        private static string BoostNativeEffect(Material own)
        {
            string note = "";
            try
            {
                note = ScaleKnob(own, MultiplierId, note, "_Mul", NativeEffectGain);
                note = ScaleKnob(own, AlphaMulId, note, "_Alpha", NativeEffectGain);
                note = ScaleKnob(own, AuroraMulId, note, "_AuroraMul", NativeEffectGain);
                if (NativeEffectUvScroll.sqrMagnitude > 0f && own.HasProperty(UVSpeedId))
                {
                    own.SetVector(UVSpeedId, NativeEffectUvScroll);
                    note += (note.Length == 0 ? "" : " ") + "_UVSpeed→" + NativeEffectUvScroll;
                }
                if (NativeEffectSideFade >= 0f && own.HasProperty(SideFadeId))
                {
                    own.SetFloat(SideFadeId, NativeEffectSideFade);
                    note += (note.Length == 0 ? "" : " ") + "_SideFade→" + NativeEffectSideFade;
                }
                if (NativeEffectTintColor.a > 0f && own.HasProperty(TintColorId))
                {
                    own.SetColor(TintColorId, NativeEffectTintColor);
                    note += (note.Length == 0 ? "" : " ") + "_Tint→" + NativeEffectTintColor;
                }
            }
            catch (Exception e) { note += " (数值处理异常:" + e.Message + ")"; }
            return note;
        }

        /// <summary>读官方浮点值 → 按需乘增益 → 拼日志摘要。材质没声明这个属性就直接跳过（不拼进摘要）。</summary>
        private static string ScaleKnob(Material m, int id, string note, string label, float gain)
        {
            if (!m.HasProperty(id)) return note;
            float v = m.GetFloat(id);
            string txt = label + "=" + v.ToString("0.###");
            if (gain != 1f)
            {
                m.SetFloat(id, v * gain);
                txt += "→" + (v * gain).ToString("0.###");
            }
            return note + (note.Length == 0 ? "" : " ") + txt;
        }

        /// <summary>把"主网格摘出件"排在采集件之前（只为日志与绘制顺序稳定，不改语义）。</summary>
        private static EffectPiece[] PrependPiece(EffectPiece first, EffectPiece[] rest)
        {
            if (rest == null || rest.Length == 0) return new EffectPiece[] { first };
            EffectPiece[] all = new EffectPiece[rest.Length + 1];
            all[0] = first;
            Array.Copy(rest, 0, all, 1, rest.Length);
            return all;
        }

        /// <summary>把特效件的 _Color 按色板重染一遍（以游戏原色为基准，不是从白色起步，保留美术原意）。</summary>
        private static void PaintNativeEffects(GlowPalette pal)
        {
            // 色板未初始化时（默认 struct 全是 0）绝不能涂：Lerp 到 (0,0,0,0) 等于把特效材质染黑，
            // additive 材质一黑就再也看不见，会被误判成"特效件没生效"。
            if (pal.ring.a <= 0f) return;
            Color tint = pal.ring;
            PaintOneSet(nodePart.pieces, tint);
            PaintOneSet(subPart.pieces, tint);
        }

        private static void PaintOneSet(EffectPiece[] pieces, Color tint)
        {
            if (pieces == null) return;
            for (int i = 0; i < pieces.Length; i++)
            {
                EffectPiece ep = pieces[i];
                if (ep == null || ep.mats == null) continue;
                for (int k = 0; k < ep.mats.Length; k++)
                {
                    Material m = ep.mats[k];
                    if (m == null || !m.HasProperty(ColorId)) continue;
                    Color src = ep.baseColors != null && ep.baseColors.Length > k ? ep.baseColors[k] : Color.white;
                    Color c = Color.Lerp(src, tint, nativeEffectTintStrength);
                    c.a = src.a;   // alpha 一律保留原值：这些材质的 alpha 常兼作混合权重，不能拿来染色
                    m.color = c;
                }
            }
        }

        /// <summary>
        /// 绘制某部件的原生特效子件。矩阵 = 部件矩阵 * 子件局部矩阵，与本体严格跟着同一个姿态走，
        /// 所以 P13 修好的同心关系不会被特效层再次破坏。
        /// 只画 ep.subs 里那些 submesh —— 即"主网格材质数组没覆盖"的那几项；把主体再画一遍就会重现
        /// 那个蓝色发光圆环（玻璃罩不吃我们的 MPB，保持原版蓝，见 P18）。
        /// 传**只含光照**的 MPB、不传 PBR/自发光旋钮：特效件继续吃游戏那套全局光照（我们就在恒星系里，
        /// 全局值是合理的），但不被结构层的 albedo/emission/染色强行改写 —— 而 additive/unlit 本来就不看光照，
        /// 那些写入对它们是空操作。这样"摘出来"只改变旋钮归属，不改变它原本的光照观感。
        /// </summary>
        private static void SubmitEffects(Matrix4x4 partMatrix, EffectPiece[] pieces, Vector3 sunDir)
        {
            if (pieces == null) return;
            // 光照覆写与结构层同源（第十一轮补）：摘出的 glass 罩过去在结构层吃到过这套 _Global_*，
            // 不带过来就等于偷偷改了它的观感；unlit/additive 那些不声明的材质上是空操作，不受影响。
            MaterialPropertyBlock lp = NextBlock();
            ApplyLighting(lp, sunDir);
            for (int i = 0; i < pieces.Length; i++)
            {
                EffectPiece ep = pieces[i];
                if (ep == null || ep.mesh == null || ep.subs == null) continue;
                Matrix4x4 mtx = partMatrix * ep.local;
                for (int j = 0; j < ep.subs.Length; j++)
                {
                    Material m = ep.mats[j];
                    int sub = ep.subs[j];
                    if (m == null || sub < 0 || sub >= ep.mesh.subMeshCount) continue;
                    Graphics.DrawMesh(ep.mesh, mtx, m, DysonRenderLayer, null, sub,
                        lp, UnityEngine.Rendering.ShadowCastingMode.Off, false);
                }
            }
        }

        /// <summary>
        /// 一次性打印模型 prefab 的结构树 —— 用来把"这建筑的特效到底是子网格还是粒子"变成事实，
        /// 而不是继续靠资产文件名推断。AssetRipper 导出的 prefab 是残缺的（组件计数不可信），只能实测。
        /// </summary>
        private static void LogPrefabTree(ModelProto proto, GameObject pref)
        {
            if (pref == null) return;
            try
            {
                Renderer[] rs = pref.GetComponentsInChildren<Renderer>(true);
                ParticleSystem[] ps = pref.GetComponentsInChildren<ParticleSystem>(true);
                Utils.Log("[MMS warpModel] 结构树 " + proto.PrefabPath + " 渲染件=" +
                    (rs == null ? 0 : rs.Length) + " 粒子系统=" + (ps == null ? 0 : ps.Length));
                if (rs != null)
                {
                    int n = Mathf.Min(rs.Length, 12);
                    for (int i = 0; i < n; i++)
                    {
                        Renderer rd = rs[i];
                        if (rd == null) continue;
                        MeshFilter mf = rd.GetComponent<MeshFilter>();
                        Material[] mm = rd.sharedMaterials;
                        Utils.Log("[MMS warpModel]   [" + i + "] " + rd.transform.name +
                            " type=" + rd.GetType().Name +
                            " mesh=" + (mf != null && mf.sharedMesh != null ? mf.sharedMesh.name + "/" + mf.sharedMesh.vertexCount : "无") +
                            " mats=" + (mm == null ? 0 : mm.Length) + "/" +
                            (mm == null || mm.Length == 0 || mm[0] == null ? "?" : (mm[0].shader != null ? mm[0].shader.name : "无shader")) +
                            " kw=" + (rd is MeshRenderer && mm != null && mm.Length > 0 && mm[0] != null && mm[0].shaderKeywords != null
                                ? string.Join(",", mm[0].shaderKeywords) : "无") +
                            " active=" + rd.gameObject.activeSelf);
                    }
                }
                if (ps != null && ps.Length > 0)
                {
                    for (int i = 0; i < Mathf.Min(ps.Length, 8); i++)
                    {
                        ParticleSystem p = ps[i];
                        if (p == null) continue;
                        var m = p.main;
                        Utils.Log("[MMS warpModel]   粒子[" + i + "] " + p.transform.name +
                            " 上限=" + m.maxParticles + " 时长=" + m.duration.ToString("F2") +
                            " 循环=" + m.loop + " 播放中=" + p.isPlaying +
                            "（需 E2：粒子克隆 + 宿主跟随，本轮未实施）");
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Log("[MMS warpModel] 结构树打印异常: " + e.Message);
            }
        }

        // ---------------------------------------------------------------- E15：网格解剖诊断（切模型之前的取证）
        //
        // 用户 R35 要"把塔切掉下 40%、只留上 60%"。写裁剪器之前，"到底能不能便宜地切、该沿哪个 Y 切"必须先是**日志里的事实**：
        //   · 若某个 submesh 的 Y 范围**整体落在弃区** ⇒ 一行都不用改网格，画的时候跳过它即可（A 路线：顶点数、顶点索引、
        //     verta 寻址全都原封不动，动画零风险）；
        //   · 若每个 submesh 都横跨全高（官方建筑多半按**材质**而非按空间切分 submesh）⇒ 只能 CPU 裁三角形（B 路线），
        //     而那时必须同步重排顶点动画数据 —— 依据是 VertaBuffer.cs:16-49 与 VertType.cs：
        //     `data[frame * frameStride + vertex * vertexSize + 分量]`，`vertexSize = (int)VertType`（V=3 / VN=6 / VNT=9），
        //     即动画是**按顶点索引**寻址的。只要重建时"保留的旧顶点按旧索引升序"排，新旧索引映射就是单调的，重排纯属搬运。
        //
        // ⚠️ 解读本日志前必须知道的**方向**：`WarpArrayModelBuilder` 里塔的姿态是 `FromToRotation(Vector3.up, -dir)`
        //    （`dir` 由恒星指向节点）⇒ 塔局部 **+Y 指向恒星**、塔尖朝恒星（用户 R35 实测确认）。
        //    所以"局部 Y 小的一端"就是**远离恒星的那一端** = 用户要弃的 40%，与"下 40%/上 60%"的字面说法同向，不做镜像。
        //
        private static readonly bool AnatomyDumpEnabled = true;   // 一次性诊断开关。写成 readonly 字段而非 const，免得整段被编译成不可达（CS0162）
        private static bool anatomyDumped;
        private const int AnatomyBins = 10;                        // 直方图档数（按 Y 十等分）
        private static readonly float[] AnatomyCutRatios = { 0.3f, 0.4f, 0.5f };   // 切面模拟的三档，40% 是用户给的目标档，两侧各给一档看趋势

        /// <summary>首次资产解析成功后跑一次。只在 <see cref="AnatomyDumpEnabled"/> 为真时打，且只打一遍。</summary>
        private static void DumpGeometryAnatomy()
        {
            if (!AnatomyDumpEnabled || anatomyDumped) return;
            anatomyDumped = true;
            try
            {
                DumpPartAnatomy(nodePart, "塔(" + (nodePart.displayName ?? nodePart.hit ?? "未命中") + ")");
                if (needReceiver) DumpPartAnatomy(subPart, "接收器(" + (subPart.displayName ?? subPart.hit ?? "未命中") + ")");
            }
            catch (Exception e)
            {
                Utils.Log("[MMS anatomy] 解剖诊断异常(不影响渲染): " + e.Message);
            }
        }

        /// <summary>
        /// 把一个部件的主网格摊开成四行判据：总览 / 逐 submesh / 全网格直方图 + 切面模拟 / 特效件位置。
        /// 全部是**只读**操作（`mesh.vertices` / `GetTriangles`），不改任何资产、也不 Instantiate，
        /// 所以这一轮跑完画面应当与上一轮逐像素一致 —— 它唯一的副作用是那几行日志与一次 CPU 遍历。
        /// </summary>
        private static void DumpPartAnatomy(Part p, string tag)
        {
            Mesh mesh = p.mesh;
            if (mesh == null)
            {
                Utils.Log("[MMS anatomy] " + tag + "：网格未取到 ⇒ 无法解剖（先核对\"组装完成\"那行报的是缺还是有）");
                return;
            }
            float t0 = Time.realtimeSinceStartup;
            int nSub = mesh.subMeshCount;
            Vector3[] verts = mesh.vertices;             // 非可读网格会在这里抛异常，被外层 catch 兜住 ⇒ 所以先打 isReadable
            Bounds b = mesh.bounds;
            int vTotal = verts.Length;
            VertaBuffer vb = p.verta;

            Utils.Log("[MMS anatomy] " + tag + " 总览 mesh=\"" + mesh.name + "\" isReadable=" + mesh.isReadable +
                " 顶点=" + vTotal + " submesh=" + nSub + " 索引格式=" + mesh.indexFormat +
                "（顶点>" + 65000 + " 时裁剪后必须沿用原索引格式，不能退回 UInt16）" +
                " Y:[min=" + N(b.min.y) + ",max=" + N(b.max.y) + "] 高=" + N(b.size.y) +
                " 全bounds min=" + V(b.min) + " max=" + V(b.max) +
                " | 我们摆位用的 center=" + V(p.center) + " norm=" + N(p.norm) +
                "（切完若仍想让残件**停在原位**，就得继续用这两个**原网格**的值）");

            // verta 与网格顶点数是否一一对应，直接决定 B 路线能不能顺手把动画也裁掉
            if (vb == null)
                Utils.Log("[MMS anatomy]   verta=无 ⇒ 该模型没有顶点动画数据，裁剪不涉及索引重排（B 路线风险大降）");
            else
                Utils.Log("[MMS anatomy]   verta: type=" + vb.vertexType + " 每顶点float=" + vb.vertexSize +
                    " 顶点=" + vb.vertexCount + " 帧=" + vb.frameCount + " frameStride=" + vb.frameStride +
                    " dataLength=" + vb.dataLength + " 数组=" + (vb.data != null ? "在" : "已释放") +
                    " cbuffer=" + (vb.cbuffer != null ? "已建" : "未建") +
                    " ⇒ " + (vb.vertexCount == vTotal
                        ? "与网格顶点**一一对应**，按\"旧索引升序保留\"即可机械重排（data[frame*stride + vertex*每顶点float + 分量]）"
                        : "与网格顶点数**不等**(" + vb.vertexCount + " vs " + vTotal + ") ⇒ 裁之前必须先弄清它的索引基准，否则动画会错行"));

            // ── 逐 submesh 与切面模拟共用一趟遍历（三角形只取一次）──
            int totalTri = 0;
            int[] bins = new int[AnatomyBins];
            int[] cntKeep = new int[AnatomyCutRatios.Length];
            int[] cntDrop = new int[AnatomyCutRatios.Length];
            int[] cntMixed = new int[AnatomyCutRatios.Length];
            float sizeY = Mathf.Max(1e-5f, b.size.y);
            string skipable = null;     // 在 40% 档**整体落在弃区**的 submesh 编号（= A 路线的候选）
            for (int s = 0; s < nSub; s++)
            {
                int[] tri = mesh.GetTriangles(s);
                int nt = tri.Length / 3;
                totalTri += nt;
                int vmin = int.MaxValue, vmax = -1;
                float ymn = float.MaxValue, ymx = float.MinValue;
                for (int k = 0; k < tri.Length; k++)
                {
                    int vi = tri[k];
                    if (vi < 0 || vi >= vTotal) continue;               // 防御：越界索引不去取 y
                    float y = verts[vi].y;
                    if (vi < vmin) vmin = vi;
                    if (vi > vmax) vmax = vi;
                    if (y < ymn) ymn = y;
                    if (y > ymx) ymx = y;
                }
                if (nt == 0) { vmin = 0; vmax = -1; ymn = 0f; ymx = 0f; }
                // 三角形重心 Y：既喂给全网格直方图，也用来估"这个 submesh 主要待在哪一段"
                for (int t = 0; t < nt; t++)
                {
                    int i0 = tri[t * 3], i1 = tri[t * 3 + 1], i2 = tri[t * 3 + 2];
                    if (i0 < 0 || i0 >= vTotal || i1 < 0 || i1 >= vTotal || i2 < 0 || i2 >= vTotal) continue;
                    float cy = (verts[i0].y + verts[i1].y + verts[i2].y) / 3f;
                    int bin = Mathf.Clamp(Mathf.FloorToInt((cy - b.min.y) / sizeY * AnatomyBins), 0, AnatomyBins - 1);
                    bins[bin]++;
                }
                // 切面模拟：按**三个顶点**分类（全在保留侧才算留，全在弃侧才算弃，否则跨界）
                for (int q = 0; q < AnatomyCutRatios.Length; q++)
                {
                    float cut = b.min.y + b.size.y * AnatomyCutRatios[q];
                    for (int t = 0; t < nt; t++)
                    {
                        int i0 = tri[t * 3], i1 = tri[t * 3 + 1], i2 = tri[t * 3 + 2];
                        if (i0 < 0 || i0 >= vTotal || i1 < 0 || i1 >= vTotal || i2 < 0 || i2 >= vTotal) continue;
                        bool a = verts[i0].y >= cut, c = verts[i1].y >= cut, d = verts[i2].y >= cut;
                        if (a && c && d) cntKeep[q]++;
                        else if (!a && !c && !d) cntDrop[q]++;
                        else cntMixed[q]++;
                    }
                }
                Material om = (p.mats != null && s < p.mats.Length) ? p.mats[s] : null;
                bool splitOut = p.drawMats != null && s < p.drawMats.Length && p.drawMats[s] == null;
                // 只有 40% 那一档（用户的目标档）才拿来判 A 路线可行性
                if (nt > 0 && AnatomyCutRatios.Length > 1 && ymx < b.min.y + b.size.y * AnatomyCutRatios[1])
                    skipable = (skipable == null ? "" : skipable + ",") + s;
                Utils.Log("[MMS anatomy]   sub[" + s + "] 三角形=" + nt + " 顶点索引=[" + vmin + "," + vmax + "]" +
                    " Y=[" + N(ymn) + "," + N(ymx) + "]" +
                    " 占Y区间=[" + N((ymn - b.min.y) / sizeY) + "," + N((ymx - b.min.y) / sizeY) + "](0=远离恒星端,1=靠恒星端)" +
                    " 材质=\"" + (om == null ? "无" : om.name) + "\"" +
                    (om == null || om.shader == null ? "" : "/" + om.shader.name) +
                    (splitOut ? " 【已摘给特效克隆材质，结构层那一次没画它】" : " 【结构层绘制】"));
            }

            int mx = 1;
            for (int i = 0; i < AnatomyBins; i++) if (bins[i] > mx) mx = bins[i];
            string hist = "";
            for (int i = 0; i < AnatomyBins; i++)
            {
                int w = Mathf.RoundToInt((float)bins[i] / mx * 12f);
                hist += "|" + new string('#', w) + bins[i];
            }
            Utils.Log("[MMS anatomy]   全网格三角形=" + totalTri + " 重心Y直方图(从**远离恒星**端→靠恒星端，每档 " +
                N(sizeY / AnatomyBins) + " 单位)：" + hist);

            string cuts = "";
            for (int q = 0; q < AnatomyCutRatios.Length; q++)
            {
                cuts += " @" + N(AnatomyCutRatios[q] * 100f) + "%(cut Y=" + N(b.min.y + b.size.y * AnatomyCutRatios[q]) + ") 留/弃/跨界=" +
                    cntKeep[q] + "/" + cntDrop[q] + "/" + cntMixed[q];
            }
            Utils.Log("[MMS anatomy]   切面模拟（**保留 Y>=cut 的上 60%**，弃掉远离恒星那端）：" + cuts);
            Utils.Log("[MMS anatomy]   判读：整体落在弃区的 submesh=" + (skipable ?? "无") +
                " ⇒ " + (skipable != null
                    ? "A 路线（画时跳过它）至少有一部分可做，先看它的造型是不是你要削掉的那段"
                    : "A 路线不可行（submesh 是按材质切的、每个都横跨全高），要走 B 路线 CPU 裁") +
                "；跨界数 = 切口处的锯齿程度（B 路线若不插值切开，这些三角形会被整片丢掉）");

            EffectPiece[] pieces = p.pieces;
            if (pieces == null || pieces.Length == 0)
            {
                Utils.Log("[MMS anatomy]   特效件=0（" + (p.pieceNote ?? "未采集") + "）⇒ 裁剪只涉及主网格");
                return;
            }
            for (int i = 0; i < pieces.Length; i++)
            {
                EffectPiece e = pieces[i];
                if (e.mesh == null) continue;
                float oy = e.local.GetColumn(3).y;      // local 已是"特效件相对主网格"的矩阵，所以这一列直接可比切面
                float sc = Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(e.local.m00), Mathf.Max(Mathf.Abs(e.local.m11), Mathf.Abs(e.local.m22))));
                float half = Mathf.Max(e.mesh.bounds.extents.x, Mathf.Max(e.mesh.bounds.extents.y, e.mesh.bounds.extents.z)) * sc;
                Utils.Log("[MMS anatomy]   特效件[" + i + "] \"" + e.tag + "\" 网格=\"" + e.mesh.name + "\" 顶点=" + e.mesh.vertexCount +
                    " 相对主网格原点 Y=" + N(oy) + " Y区间≈[" + N(oy - half) + "," + N(oy + half) + "]" +
                    "（区间是按最大外延×缩放对角线的**近似**，local 含旋转时会有出入）⇒ 落在弃区的那几件，裁完会变成悬空特效，要一并跳过");
            }
            Utils.Log("[MMS anatomy] " + tag + " 解剖耗时 " + N((Time.realtimeSinceStartup - t0) * 1000f) + " ms（一次性，不进渲染路径）");
        }

        private static string N(float v) { return v.ToString("0.###"); }
        private static string V(Vector3 v) { return "(" + N(v.x) + "," + N(v.y) + "," + N(v.z) + ")"; }

        /// <summary>
        /// 打印候选模型名清单（P17 的落地手段，E9 起加了两个关键字段）。
        /// 上一轮这行只给 id + 英文路径 ⇒ 我据此猜建筑身份，结果把"电力感应塔"当成"特斯拉塔"、
        /// 又猜"地面塔没有顶点动画"，两条都被 P27/P25 推翻（R28 禁止的就是这种用法）。
        /// 现在连 **游戏自己的显示名** 与 **该 LOD 有没有 verta 数据** 一起打出来：
        /// 显示名用来对号"哪一条才是无线输电塔"，verta 直接回答"这座塔官方有没有顶点动画"，两者都是运行时实况而非我的判读。
        /// </summary>
        private static void LogModelNameMatches()
        {
            try
            {
                ModelProto[] arr = ModelArray();
                if (arr == null) return;
                // R25 / P23：上一轮的筛选词没覆盖 node-tower 整族，直接导致我误判"游戏没有第二档电塔"。
                // 现在用 ModelDumpKeys（含 node-tower / node / tower / relay），并把造型指标一起打出来，
                // 让用户能按"顶点数/材质数"认出那是不是他要的塔，不再依赖我去猜中文名。
                string[] keys = ModelDumpKeys;
                int hits = 0;
                for (int i = 0; i < arr.Length && hits < 80; i++)
                {
                    ModelProto proto = arr[i];
                    if (proto == null || string.IsNullOrEmpty(proto.PrefabPath)) continue;
                    bool hit = false;
                    for (int k = 0; k < keys.Length; k++)
                    {
                        if (proto.PrefabPath.IndexOf(keys[k], StringComparison.OrdinalIgnoreCase) >= 0) { hit = true; break; }
                    }
                    if (!hit) continue;
                    hits++;
                    string vc = "?", mc = "?", vt = "verta?";
                    try
                    {
                        PrefabDesc pd = proto.prefabDesc;
                        if (pd != null)
                        {
                            Mesh mm = pd.lodMeshes != null && pd.lodMeshes.Length > 0 ? pd.lodMeshes[0] : pd.mesh;
                            if (mm != null) vc = mm.vertexCount + "顶点/" + mm.subMeshCount + "sub";
                            Material[] ms = pd.lodMaterials != null && pd.lodMaterials.Length > 0 ? pd.lodMaterials[0] : pd.materials;
                            if (ms != null) mc = ms.Length + "材质";
                            // E9 新增两栏，用来把"这座塔到底有没有官方顶点动画"变成日志里的实况：
                            //   PrefabDesc.cs:292-299 —— 只有 .verta 文件读成功才填那一槽，读失败留 null 并 LogError；
                            //   PrefabDesc.cs:360-366 —— 不走 LOD 分支的 prefab 直接 lodVertas = 空数组 ⇒ "LOD槽0"就是官方没配。
                            // anim=x/y 是 anim_prepare_length/anim_working_length（:695-704，无 AnimDesc 时 0/1），
                            // 也就是 PlanetFactory.cs:1369-1373 喂给真实建筑 AnimData 的同一对初值。
                            int vn = pd.lodVertas == null ? 0 : pd.lodVertas.Length;
                            VertaBuffer v0 = vn > 0 ? pd.lodVertas[0] : null;
                            vt = "verta=" + (v0 != null ? ("有(顶点" + v0.vertexCount + " 帧" + v0.frameCount + " 每点" + v0.vertexSize + "float)")
                                                        : ("无(LOD槽" + vn + ")")) +
                                 " anim=" + pd.anim_prepare_length + "/" + pd.anim_working_length;
                        }
                    }
                    catch (Exception) { }
                    string dn = "?";
                    try { dn = proto.displayName; } catch (Exception) { }
                    Utils.Log("[MMS warpModel] 可选模型 id=" + proto.ID + " 显示名=" + dn + " path=" + proto.PrefabPath +
                              " 规模=" + vc + " " + mc + " " + vt);
                }
                Utils.Log("[MMS warpModel] 可选模型清单已打印 " + hits + " 条（换主发生器/接收器外观就改 NodeKeys / SubKeys 的首选名；" +
                          "本次筛选词=" + string.Join(",", ModelDumpKeys) + "，没列出不代表游戏没有，见 P23）");
            }
            catch (Exception e)
            {
                Utils.Log("[MMS warpModel] 可选模型清单异常: " + e.Message);
            }
        }

        /// <summary>
        /// 程序化圆环：单位大圆半径 1、管半径 tubeRatio，躺在 XZ 平面（法线 +Y）。
        /// E14 起本方法**只造网格、不写静态字段**（赋值交给 ModelCatalog 的环缓存），
        /// 因为现在环的管径是 Builder 逐条目给的 ⇒ 同一个 tubeRatio 只造一张、不同 tubeRatio 各造一张。
        /// 默认那一档（RingTubeRatio = 0.05）必须与重构前拿到**同一张网格**，否则"环逐像素不变"就无从谈起。
        /// </summary>
        private static Mesh BuildProceduralRing(int majorSeg, int minorSeg, float tubeRatio)
        {
            int vCount = (majorSeg + 1) * (minorSeg + 1);
            if (vCount > 65000) return null;    // 与原版一致使用 UInt16 索引，留余量
            Vector3[] vs = new Vector3[vCount];
            Vector3[] ns = new Vector3[vCount];
            Vector2[] uv = new Vector2[vCount];
            int[] tris = new int[majorSeg * minorSeg * 6];
            int v = 0;
            for (int i = 0; i <= majorSeg; i++)
            {
                float u = i * Mathf.PI * 2f / majorSeg;
                float cu = Mathf.Cos(u), su = Mathf.Sin(u);
                for (int j = 0; j <= minorSeg; j++)
                {
                    float w = j * Mathf.PI * 2f / minorSeg;
                    float cw = Mathf.Cos(w), sw = Mathf.Sin(w);
                    Vector3 n = new Vector3(cw * cu, sw, cw * su);
                    vs[v] = n * (1f / (1f / tubeRatio)) + new Vector3(cu, 0f, su);
                    ns[v] = n;
                    // 按【弧长】平铺 UV，而不是把整张贴图拉伸着铺满一圈（修 P10）：
                    // 大圆方向的展开长度是管截面的 1/tubeRatio 倍，所以 v 的重复数必须等比例缩小，
                    // 两个方向的 texel 密度才一致，贴图不会被各向异性拉糊；
                    // u 用 RingUvTiles 控制"多少个贴图模块绕一圈"，换算约 2πR/RingUvTiles 米一个模块。
                    uv[v] = new Vector2((float)i / majorSeg * RingUvTiles,
                        (float)j / minorSeg * RingUvTiles * tubeRatio);
                    v++;
                }
            }
            int t = 0;
            int ring = minorSeg + 1;
            // 绕序与 AppendTorus 同步修正（P12）：(a,c,b)/(a,d,c) 才与朝外的顶点法线一致，
            // 否则结构环的外壁会被背面剔除，管外只看得到一个凹槽，且内壁还拿着朝外的法线算光照 —— 又暗又错乱。
            for (int i = 0; i < majorSeg; i++)
                for (int j = 0; j < minorSeg; j++)
                {
                    int a = i * ring + j, b = a + ring, c = b + 1, d = a + 1;
                    tris[t++] = a; tris[t++] = c; tris[t++] = b;
                    tris[t++] = a; tris[t++] = d; tris[t++] = c;
                }
            Mesh m = new Mesh();
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            m.vertices = vs;
            m.normals = ns;
            m.uv = uv;
            m.triangles = tris;
            m.RecalculateBounds();
            // 与发光层同理（DysonShell.cs:1279）：巨构尺度下放宽自生成网格的包围盒，排除按包围盒的视锥误剔除。
            // ringPart.norm 用的是解析值而不是 bounds，所以覆盖 bounds 不会影响尺寸换算。
            m.bounds = new Bounds(m.bounds.center, new Vector3(2000000f, 2000000f, 2000000f));
            m.name = "MMS_WarpRing";
            m.hideFlags = HideFlags.HideAndDontSave;
            return m;    // 外延 = 大圆半径 1 + 管半径，交给调用方写进 Part.norm
        }
    }
}
