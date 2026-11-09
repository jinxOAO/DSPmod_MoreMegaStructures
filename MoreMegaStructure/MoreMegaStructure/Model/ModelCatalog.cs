using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMegaStructure
{
    /// <summary>
    /// 这套架构里"预制件"的清单：**每个枚举代表一整套模型 + 调整好的参数 + 动画位点或者设置 + 是否有辉光以及辉光颜色**。
    ///
    /// 为什么是枚举而不是直接写 LDB 的建筑 id：一个 id 只代表"哪只 prefab"，而这里的每一项还绑定了
    /// 它的材质包 / PBR 旋钮包（<see cref="RoleKnobs"/>）/ 要不要喂动画数据 / 能量环该多大。
    /// 这些"调好的东西"必须有一个地方一次性放好，Builder 里才只需要写 `AddModel(ModularModels.UnFoldTower, …)` 一行。
    /// 具体配套全在 `ModelCatalog.cs` 的 <see cref="ModelRenderer.TryGetSpec"/> 里（那才是"字典"本体）。
    ///
    /// 想加新东西时的正确姿势（三步，都在 ModelCatalog.cs 里）：
    /// 1) 在这里加一个枚举值；
    /// 2) 在 ModelRenderer 里给它备好部件与旋钮（多半可以复用 FindPartByModelName / FindPartFirst 那两条候选链，
    ///    并像 <see cref="ModelRenderer.ResolveAssets"/> 那样按"层级有没有用到它"决定要不要装载）；
    /// 3) 在 TryGetSpec 的 switch 里加一个 case。
    /// Builder 与渲染器都不用再改 —— 这就是把"新增一种部件"的成本压到一处的目的。
    /// </summary>
    public enum ModularModels
    {
        /// <summary>无线输电塔，展开状态定格（当前默认）。塔尖朝恒星、带电流特效、有能量环辉光位。</summary>
        UnFoldTower = 0,
        /// <summary>
        /// 同一座塔的"收起"状态。⚠️ 现阶段与 <see cref="UnFoldTower"/> **画面完全相同**：
        /// 塔的动画数据是渲染器级唯一的一份 `_AnimBuffer`（见 ModelRenderer.SetupAnim / animHoldExpanded），
        /// 没法在同一帧里让两个枚举各演各的。要真正分开，得先把动画缓冲改成"每个动画状态一份"（E14 刻意不动它，
        /// 以免牵连 R26/R30 已实测通过的动画链路）。登记于文档 P30 第 7 条。
        /// </summary>
        FoldedTower = 1,
        /// <summary>射线接收器（R32 起 Builder 没有再放它 ⇒ 画面上没有；想放回就在某层里 AddModel 一条）。</summary>
        RayReceiver = 2,
        /// <summary>
        /// E17（R36）：**上 60% 的塔** —— 与 <see cref="UnFoldTower"/> 完全是同一套配方（同一份资产、同一包旋钮、同一个辉光位），
        /// 唯一区别是它**自带默认裁剪比例 40%**（见 <see cref="DefaultCrop"/>）。
        /// 为什么要占一个枚举位（用户的裁决）：这样 `AddModel(ModularModels.TowerCrop40, …)` 一行就是"这个预制件"，
        /// 不必每次记着再写一遍 0.4f。⚠️ 代价是"裁剪比例"有了两个来源，所以优先级只留一条规则：
        /// **条目显式给的 `cropBottom` &gt; 枚举自带的默认**；`cropBottom = 0` 的意思是"**跟随默认**"而不是"不切"
        /// —— 想要一整座没切过的塔，正确写法是换回 <see cref="UnFoldTower"/>。
        /// </summary>
        TowerCrop40 = 3,
        /// <summary>
        /// E17（R36）：**只保留电流特效**的那座塔。画的是同一张主网格的 `sub[1]`（实测 80 个三角形，材质
        /// `charging-pole-effect` / `VF Shaders/Forward/Unlit Additive SHM`），**结构层那一次绘制整个跳过**
        /// （本体 1522 三角、外扩纯色壳、能量环都不画）⇒ 画面上是一缕没有实体的电弧/线圈。
        /// 为什么不用新切模型、也不用新材质（依据见文档 P31 第 1/2 条）：那个特效 submesh 早在 E5② 就被
        /// <see cref="SplitMainMeshEffects"/> 摘出来交给自用克隆材质了，结构层数组里对应位置是 null，
        /// 特效本来就走 `SubmitEffects` 这条**独立**的提交 —— 所谓"只留特效"就是少调一次 `Submit`。
        /// 特效件不裁（用户裁决"全留"），也不给它配自研发光。
        /// ⚠️ 未证实项：特效件那次提交既不传 knobs 也不调 BindAnim ⇒ 它吃不吃顶点动画**未知**，等实测（P31 第 5 条）。
        /// </summary>
        TowerEffectOnly = 4,
    }

    // ── ModelRenderer 的另一半（partial）：枚举 → 一整套模型/材质/旋钮/辉光的目录 ──
    // 分成两个文件的原因很实际：ModelRenderer.cs 是"每一帧怎么画"的机器（搬迁自旧渲染器，2300 行），
    // 而这里是"某一种部件长什么样"的配置表。你以后新增部件类型只碰这一个文件，不用在渲染机器里翻。
    public static partial class ModelRenderer
    {
        /// <summary>
        /// 一个部件身份对应的全套绘制参数（E14 / R34）。
        /// struct ⇒ 每帧逐条目取一份也不产生分配；里面的 <see cref="Part"/> 与材质数组都是已解析好的引用，不复制内容。
        /// </summary>
        private struct ModelSpec
        {
            public Part part;                     // 网格 + 材质 + 几何中心 + 归一化外延 + 特效子件
            public RoleKnobs knobs;               // 那一整套调好的 PBR / 自发光 / 动画参数（R27 / R31 的成果都住在这里）
            public Quaternion meshRotFix;         // 网格自身坐标系与"环面在 XZ、法线 +Y"约定之间的修正（只有换用游戏自带 Torus 时非单位）
            public float glowHaloNominal;         // 该部件位的能量环名义半径（米）；<= 0 ⇒ 这个部件不给能量环
            public bool ringGlow;                 // 环条目才为 true：允许它去点亮自研的发光环 + 光核那两层
            /// <summary>
            /// E17（R36）：true ⇒ **只画特效件**，跳过结构层那一次 `Submit`（连带外扩壳与能量环）。
            /// 与全局 `showStructure` 的区别很关键：`showStructure=false` 会把 `Submit` 和 `SubmitEffects`
            /// **一起**关掉（两者在同一个 if 块里），而这条是**逐条目**的"只要后半句"。依据见文档 P31 第 2/3 条。
            /// </summary>
            public bool effectOnly;
        }

        /// <summary>
        /// 按 modelId 取"这一整套"。取不到就返回 false（这一条目当帧不画），并把未知 id 只记一次日志。
        /// 注意这里返回的 part 只是**引用**：网格/材质的真正解析仍然只在 <see cref="ResolveAssets"/> 里发生一次。
        /// E16（R35）第二个参数是 `cropBottom`；**E20（R38）起改成直接收整条 <see cref="ModelEntry"/>**，
        /// 因为变体机器的输入从"一个数"变成了"裁剪比例 + 拉伸比例 + 尖端距离"三个数（再往签名上堆参数就是自找麻烦）。
        /// 唯一的调用点在 <see cref="DrawModelEntry"/>，所以这次改签名没有牵连任何别处。
        /// 其余（材质包 / PBR 旋钮 / 动画 / 辉光位）**一律沿用原部件** —— 切一刀/拉一段只改几何，不改观感配方。
        /// E17（R36）将裁剪比例升级为**两级来源**：条目显式给了就用条目的，`0` 则回落到该枚举自带的默认
        /// （<see cref="DefaultCrop"/>）⇒ `TowerCrop40` 什么都不写也是切 40%，而 `UnFoldTower` 依然一整座。
        /// </summary>
        private static bool TryGetSpec(ModelEntry en, out ModelSpec spec)
        {
            int modelId = en.modelId;
            spec = default(ModelSpec);
            ModularModels mm = (ModularModels)modelId;
            switch (mm)
            {
                case ModularModels.UnFoldTower:
                case ModularModels.FoldedTower:
                case ModularModels.TowerCrop40:
                    // FoldedTower 为什么与 UnFoldTower 走同一条：见 ModularModels.FoldedTower 的注释（动画缓冲是渲染器级唯一一份）。
                    // TowerCrop40 也走同一条：它与上面两个**只差裁剪比例**（差在 DefaultCrop 那张表里），配方完全相同。
                    spec.part = nodePart;
                    spec.knobs = NodeKnobs;                                   // 塔那一包：keepOfficial（R31 交回官方值）+ bindAnim（喂 _AnimBuffer）
                    spec.glowHaloNominal = GlowNodeHaloRatio * NominalBaseRadius;   // = 1200 名义米（塔身 800，环套在外面不遮本体）
                    break;
                case ModularModels.TowerEffectOnly:
                    // 同一座塔的**同一份部件**（特效件就挂在这份 part.pieces 上，摘出时已配好自用克隆材质），
                    // 只是下面 effectOnly=true 让结构层那一次提交不发生。knobs 仍给塔那一包：这一趟并不绘制 PBR 本体，
                    // 但保留它意味着"以后想让这个预制件也吃自研发光/外壳"只要把 effectOnly 拿掉，不用再配一遍参数。
                    spec.part = nodePart;
                    spec.knobs = NodeKnobs;
                    spec.effectOnly = true;
                    spec.glowHaloNominal = 0f;        // 这个预制件的定义就是"只有电流特效"，不配能量环那一层自研发光
                    break;
                case ModularModels.RayReceiver:
                    spec.part = subPart;
                    spec.knobs = SubKnobs;
                    spec.glowHaloNominal = GlowSubHaloRatio * NominalBaseRadius;    // = 760 名义米（罩身 560）
                    break;
                default:
                    LogUnknownModelId(modelId);
                    return false;
            }
            spec.meshRotFix = Quaternion.identity;
            spec.ringGlow = false;
            if (spec.part.mesh == null) return false;
            // 变体（裁剪 / 拉伸）放在最后：它只需要"原部件"这一件事，与上面选了哪套配方无关。
            // 裁剪比例的优先级只有这一条式子（条目显式值 > 枚举默认），所以"两处数值会打架"从根上不存在（P31 末段）。
            // E20：拉伸比例**没有**"枚举默认"这一层 —— 拉伸是"条目行为"（只有 AddLaser 会给），不是"预制件身份"。
            float crop = en.cropBottom > 0f ? en.cropBottom : DefaultCrop(mm);
            if (crop > 0f || en.stretchTopFrac > 0f) ApplyVariant(ref spec.part, spec.effectOnly, en, crop);
            return true;
        }

        /// <summary>
        /// 各枚举**自带**的默认裁剪比例（E17 / R36）。裁剪比例的权威出处就在这一行起：
        /// Builder 里 `AddModel(ModularModels.TowerCrop40, …)` 不必再写数，`AddModelCropped(…, 0.7f)` 才是显式覆盖。
        /// 新增"切好形的预制件"就只在这里加一个 case，别在 Builder 里再留一份数字（上一轮那个
        /// `WarpArrayModelBuilder.TowerCropBottom` 常量已删，理由就是同一个 40% 不该有两个家）。
        /// </summary>
        private static float DefaultCrop(ModularModels mm)
        {
            switch (mm)
            {
                case ModularModels.TowerCrop40: return 0.4f;
                default: return 0f;      // 其余预制件一律不切（TowerEffectOnly 按用户裁决"全留"）
            }
        }

        // ---------------------------------------------------------------- E16（R35）：把预制模型"切一刀"
        //
        // 路线是上一轮的解剖日志定下来的（§6 第十六轮）：`整体落在弃区的 submesh=无` ⇒ A 路线（整段跳过某个 submesh）
        // 不可行，因为官方这座塔的 submesh 是**按材质**分的（结构 1522 三角 / 电流特效 80 三角），每个都横跨全高 ⇒ 只能 CPU 裁。
        //
        // 而真正动手时发现比上一轮的计划省了一大步，**这条差异必须写在代码旁边**（否则以后会有人照着旧计划去重排 verta）：
        // 上一轮 §5 E15 写的是"Instantiate 后按旧索引升序重建顶点表 + 同步重排 verta.data"，本轮换成
        // **顶点表一个都不动，只重写逐 submesh 的三角形索引**。四条理由：
        //   ①渲染由索引驱动，没被任何三角形引用到的顶点不参与顶点着色 ⇒ 留着那 2600 个顶点不花 GPU 钱；
        //   ②顶点索引与原版**完全一致** ⇒ `_VertaBuffer` 继续只读共享那一份（VNT 每顶点 9 float × 31 帧 ≈ 2.9 MB
        //     的新数组与新 ComputeBuffer 全免），而且动画侧 SetupAnim/BindAnim/Submit **一行都不用改**；
        //   ③`Object.Instantiate(mesh)` 保留 bounds ⇒ `p.center / p.norm` 天然还是原值，
        //     用户裁决的"残件停在原位"不需要任何额外代码（也不会出现"切完自己浮到中心"的怪事）；
        //   ④避开"只缩短 vertices 数组会让 normals / uvs 长度不匹配"这类真实风险，不动它反而最安全。
        //
        // 切口处理按裁决"先直接敞口"：三顶点不全在保留侧的三角形**整片丢弃**（上一轮日志量到 40% 那一刀只有 36/1602 个
        // 跨界 ⇒ 锯齿量级很小，而切面恰好落在塔的细颈上）。将来若要干净的台面，改的是下面这一处判据：
        // 沿 cutY 插值切开 + 补一片盖住切口的平面三角形，其余机器不用动。
        // ── E20（R38）：同一套机器现在还要能"把最头一段拉到指定距离"（激光），所以名字从 Cropped 改成 Variant ──
        // 两条路的共同点：都在 `Object.Instantiate` 出来的**副本**上动刀、都不改顶点数与索引、都要把特效件重新指认。
        // 两条路唯一的**关键差异**（写在这里是因为以后一定有人想"统一"掉它）：
        //   · 裁剪**故意不调** `RecalculateBounds` 之外的任何几何变化 ⇒ 副本 bounds 与原件相同 ⇒ "残件停在原位"；
        //   · 拉伸**必须** `RecalculateBounds()` ⇒ 本地 bounds 从 7.6 高变成 20+ 高，沿用旧 bounds 的话
        //     视锥剔除会按"一座塔那么大"去判 ⇒ **某个角度整条光束凭空消失**（P33 副作用③，本轮头号嫌疑）。
        private struct MeshVariant
        {
            public Mesh source;               // 这张副本是基于哪张原网格建的 ⇒ 引用判等，重新解析过资产就自动作废重建
            public Mesh mesh;                 // 变体网格；null ⇒ 这一刀/这一拉没能做成（按未变体绘制，原因已记日志）
            public EffectPiece[] pieces;      // 改指副本的特效件数组；同一条记录里 hasPieces=false 时用 null 表示"不用换"
            public int cropKey;               // 量化后的裁剪比例（见 CropKeyScale）
            public int stretchFracKey;        // 量化后的"被拉的最头比例"（E20；0 = 本变体不拉伸）
            public int stretchDistKey;        // 量化后的"尖端要到达的名义米距离"（E20）
            public int kept;
            public int dropped;
            public int mixed;
        }
        /// <summary>
        /// 已建出的变体清单。E17 把键从 `modelId*10000 + 比例` 换成 **(源网格引用, 比例)** 并改为线性扫描，两个理由：
        /// ①现在 4 个塔枚举共享**同一张** `nodePart.mesh`，按 modelId 分键会让"同样切 40%"裁出两份副本；
        /// ②反过来，若两个不同部件恰好同比例，按"比例 + 单一槽位"存会互相覆盖 ⇒ 每帧重建（比多占内存更糟）。
        /// E20 在此基础上再加两个拉伸分量 ⇒ 键 = **(源网格引用, crop, 拉伸比例, 尖端距离)** 四元组：
        /// 同一张塔网格现在会同时存在"未变体（原件）/ 切 40% / 激光版"三种，各占一条，互不覆盖。
        /// 条目数就是"网格 × 刀口 × 拉法"的组合数（实际仍是个位数），扫描比 Dictionary 装箱/哈希更省，也不会 thrash。
        /// ⚠️ 键里带"尖端距离"意味着**每条不同长度的激光各建一张网格** —— 当前只有 1 条（Builder 的裁决③），
        /// 若将来要"一屏几十条不同长度的激光"，得改成"按轴缩放矩阵画"或把长度量化到少数档，别放任它线性增长。
        /// </summary>
        private static readonly List<MeshVariant> meshVariants = new List<MeshVariant>();
        private const float CropKeyScale = 1000f;        // 0.1% 的分辨率：0.4 与 0.4001 同一张副本，0.4 与 0.45 各一张
        private const float StretchFracKeyScale = 1000f; // 拉伸比例同样 0.1% 分辨率
        private const float StretchDistKeyScale = 10f;   // 尖端距离量化到 0.1 名义米（名义米数量级是千，0.1 m 的差异画面看不见）

        /// <summary>
        /// 把部件换成"切过 / 拉过"的那一份。**只在第一次遇到这个 (原网格, crop, 拉伸比例, 尖端距离) 时真正建副本**，
        /// 之后走清单（与 Builder"首次读档跑一次、后面当常量用"是同一条精神）。
        /// 任何一步失败都退回未变体的原部件，绝不因为切不出/拉不出东西而让整座巨构消失。
        /// </summary>
        /// <param name="effectOnly">本条目的预制件是不是"只画特效件"⇒ 决定"哪些 submesh 真的会被画出来"，
        /// 而那部分顶点的 Y 区间才是拉伸段的依据（P33 第 2 条：塔特效实测 Y=[0,6.787]，把结构层的 Y 算进来就错了）。</param>
        /// <param name="crop">已经合并过优先级的最终裁剪比例（条目显式值 &gt; 枚举默认，见 <see cref="TryGetSpec"/>）。</param>
        private static void ApplyVariant(ref Part p, bool effectOnly, ModelEntry en, float crop)
        {
            int ck = Mathf.RoundToInt(crop * CropKeyScale);
            int fk = en.stretchTopFrac > 0f ? Mathf.RoundToInt(en.stretchTopFrac * StretchFracKeyScale) : 0;
            int dk = fk != 0 ? Mathf.RoundToInt((float)en.stretchTipDist * StretchDistKeyScale) : 0;
            MeshVariant vm = default(MeshVariant);
            bool found = false;
            for (int i = 0; i < meshVariants.Count; i++)
            {
                MeshVariant e = meshVariants[i];
                if (e.cropKey == ck && e.stretchFracKey == fk && e.stretchDistKey == dk && ReferenceEquals(e.source, p.mesh)) { vm = e; found = true; break; }
            }
            if (!found)
            {
                vm = BuildVariant(p, effectOnly, en, crop);
                vm.cropKey = ck;
                vm.stretchFracKey = fk;
                vm.stretchDistKey = dk;
                meshVariants.Add(vm);      // 失败结果也存（source=p.mesh, mesh=null）⇒ 不会每帧重试同一个失败
            }
            if (vm.mesh == null) return;
            p.mesh = vm.mesh;
            if (vm.pieces != null) p.pieces = vm.pieces;
            // center / norm **故意不重算**（两条路共用这条）：裁决"停在原位"= 剩下的部分要继续待在它原来的空间位置上，
            // 而摆放公式只认 center/norm（`DrawModelEntry` 的枢轴与 `s = scale·scaleW/norm`），
            // ⇒ 拉伸段解出来的 k 也正是按"原 center/norm"折算的（P33 第 3 条），这里一重算就会把那个解作废。
            // ⚠️ 与上面那条差异说明对照着读：**不重算的是 Part 的 center/norm，重算的是 Mesh 自己的 bounds**，两者不矛盾。
        }

        /// <summary>真正下刀：只读原网格，写在自己的副本上。</summary>
        private static MeshVariant BuildVariant(Part basePart, bool effectOnly, ModelEntry en, float cropBottom)
        {
            MeshVariant fail = new MeshVariant();
            Mesh src = basePart.mesh;
            fail.source = src;
            if (src == null) return fail;
            float stretchFrac = en.stretchTopFrac;
            float tipDist = (float)en.stretchTipDist;
            // 这条判据是上一轮日志里唯一"如果为假就整条路不通"的项，所以它得由运行时自己再确认一次
            if (!src.isReadable)
            {
                LogVariant(en.modelId, cropBottom, stretchFrac, tipDist, "**跳过**：网格 isReadable=false ⇒ CPU 侧读不到顶点，无法裁剪/拉伸，按未变体绘制（这是资产事实，代码绕不过去）");
                return fail;
            }
            try
            {
                float t0 = Time.realtimeSinceStartup;
                Bounds b = src.bounds;
                float cropY = b.min.y + b.size.y * cropBottom;
                Vector3[] verts = src.vertices;          // 这份数组**只读**（后面要拿它自检"原件有没有被污染"，所以一个字节都不改它）
                int vTotal = verts.Length;
                int nSub = src.subMeshCount;
                Vector3 scl = (Vector3)en.scale;         // VectorLF3 → Vector3 是隐式转换（VectorLF3.cs:207）

                // ── 第 0 步（E20 新增）：搞清"哪些 submesh 会真的被画出来" ──
                // 拉伸段要按**实际参与绘制的几何**的 Y 区间来定，否则"只画特效件"的激光会被不画的结构层顶点把区间撑大。
                // 判据完全照抄绘制侧的两个事实：结构层画 `drawMats[s] != null` 的位置（摘给特效的那些位置是 null，P22），
                // 特效层画 `pieces[i].subs[]` 里那些段且**网格引用必须是同一张**（独立 prefab 子件不在这张网格的空间里）。
                HashSet<int> drawn = new HashSet<int>();
                if (!effectOnly && basePart.drawMats != null)
                {
                    int dn = Mathf.Min(nSub, basePart.drawMats.Length);
                    for (int s = 0; s < dn; s++) if (basePart.drawMats[s] != null) drawn.Add(s);
                }
                EffectPiece[] srcPieces = basePart.pieces;
                if (srcPieces != null)
                {
                    for (int i = 0; i < srcPieces.Length; i++)
                    {
                        EffectPiece ep = srcPieces[i];
                        if (ep == null || ep.subs == null || !ReferenceEquals(ep.mesh, src)) continue;
                        for (int j = 0; j < ep.subs.Length; j++) { int sb = ep.subs[j]; if (sb >= 0 && sb < nSub) drawn.Add(sb); }
                    }
                }
                if (drawn.Count == 0) for (int s = 0; s < nSub; s++) drawn.Add(s);   // 兜底：认不出来就全算，行为退回 E16 那条路
                string drawnTag = "";
                for (int s = 0; s < nSub; s++) if (drawn.Contains(s)) drawnTag += (drawnTag.Length == 0 ? "" : "/") + s;

                // ── 第 1 步：逐 submesh 过滤三角形（E16 的原样保留），并标记"会被画到的顶点" ──
                bool[] used = new bool[vTotal];
                int keptAll = 0, droppedAll = 0, mixedAll = 0;
                int[] srcTriCount = new int[nSub];
                List<int>[] keeps = new List<int>[nSub];
                for (int s = 0; s < nSub; s++)
                {
                    int[] tri = src.GetTriangles(s);
                    srcTriCount[s] = tri.Length;
                    bool drawThis = drawn.Contains(s);
                    List<int> keep = new List<int>(tri.Length);
                    for (int t = 0; t + 2 < tri.Length; t += 3)
                    {
                        int i0 = tri[t], i1 = tri[t + 1], i2 = tri[t + 2];
                        // 越界索引（正常不该出现）一律保守保留，免得凭空少一块
                        if (i0 < 0 || i0 >= vTotal || i1 < 0 || i1 >= vTotal || i2 < 0 || i2 >= vTotal) { keep.Add(i0); keep.Add(i1); keep.Add(i2); keptAll++; continue; }
                        bool a = verts[i0].y >= cropY, c = verts[i1].y >= cropY, d = verts[i2].y >= cropY;
                        if (a && c && d) { keep.Add(i0); keep.Add(i1); keep.Add(i2); keptAll++; if (drawThis) MarkUsed(used, i0, i1, i2); }
                        else if (!a && !c && !d) droppedAll++;
                        else mixedAll++;      // 跨界：敞口裁决下整片丢弃（要改成插值切开就动这里）
                    }
                    keeps[s] = keep;
                }

                // ── 第 2 步：量出"会被画出来的那部分几何"的 Y 区间 + 尖端的横向残差（P33 副作用②要的就是这个数）──
                float yBot = float.MaxValue, yTop = float.MinValue, tipLat = 0f;
                int usedCount = 0;
                for (int i = 0; i < vTotal; i++)
                {
                    if (!used[i]) continue;
                    usedCount++;
                    Vector3 v = verts[i];
                    if (v.y < yBot) yBot = v.y;
                    if (v.y > yTop) yTop = v.y;
                }
                bool stretched = false;
                float cutY = 0f, k = 1f, tipTargetY = 0f;
                if (stretchFrac > 0f && yTop > yBot && scl.y > 0f)
                {
                    cutY = yBot + (yTop - yBot) * (1f - stretchFrac);
                    // 尖端要落到的**局部网格 Y**：世界沿轴位移 = (y' − center.y)·s，而 s = scale·scaleW/norm，
                    // 令它等于 dist·scaleW ⇒ scaleW 约掉 ⇒ tipTargetY = center.y + dist·norm/scale（P33 第 3 条）。
                    tipTargetY = basePart.center.y + tipDist * basePart.norm / scl.y;
                    if (tipTargetY > cutY)
                    {
                        k = (tipTargetY - cutY) / (yTop - cutY);
                        // 横向残差（P33 副作用②）：只改 Y ⇒ 尖端顶点若 x/z ≠ 0，落点会在垂直于光束轴的方向偏
                        // `|(x,z)| · scale/norm` 名义米。解剖日志没打过这个数 ⇒ 这里量**尖端那一圈**顶点里最歪的一个，
                        // 直接打进群组日志（不是全网格最大值，那会把"塔腰比较粗"误报成"落点歪"）。
                        for (int i = 0; i < vTotal; i++)
                        {
                            if (!used[i] || verts[i].y < yTop - 1e-4f) continue;
                            float ax = Mathf.Abs(verts[i].x), az = Mathf.Abs(verts[i].z);
                            float lat = ax > az ? ax : az;
                            if (lat > tipLat) tipLat = lat;
                        }
                        stretched = true;
                    }
                }

                // ── 第 3 步：建副本并写回（顺序：先改顶点、再写三角形索引、最后重算 bounds）──
                Mesh cp = UnityEngine.Object.Instantiate(src);
                if (cp == null)
                {
                    LogVariant(en.modelId, cropBottom, stretchFrac, tipDist, "**失败**：Object.Instantiate(网格) 返回 null ⇒ 按未变体绘制");
                    return fail;
                }
                cp.name = src.name + "_mmsVar" + (cropBottom > 0f ? "c" + Mathf.RoundToInt(cropBottom * 100f) : "") +
                          (stretched ? "s" + Mathf.RoundToInt(stretchFrac * 100f) + "t" + Mathf.RoundToInt(tipDist) : "");
                cp.hideFlags = HideFlags.HideAndDontSave;   // 与克隆材质同一惯例：不进资源枚举、不被当成场景资产卸载
                if (stretched)
                {
                    Vector3[] nv = (Vector3[])verts.Clone();     // 原件那份数组继续不动 ⇒ 下面的自检才有意义
                    for (int i = 0; i < vTotal; i++)
                    {
                        if (nv[i].y <= cutY) continue;
                        Vector3 v = nv[i];
                        v.y = cutY + (v.y - cutY) * k;           // **只改 Y**：横向分量原样保留（粗细节正是用户注释里的 scale 说了算）
                        nv[i] = v;
                    }
                    cp.vertices = nv;                            // 数组长度不变 ⇒ 法线/UV/切线等通道一律保持原值
                }
                for (int s = 0; s < nSub; s++) cp.SetTriangles(keeps[s].ToArray(), s);
                // ⚠️ 这一句在**两条路上都必须调**，但理由完全相反，别把它"统一"掉（上面 MeshVariant 那段注释是权威版本）：
                // · 拉伸：本地 bounds 从 7.61 高变成 20+ 高，不重算 ⇒ 视锥剔除仍按"一座塔那么大"判 ⇒ **某个角度整条光束凭空消失**（P33 副作用③）；
                // · 裁剪：顶点表一个没动 ⇒ 重算出来与原件同一 bounds，这条调用只是自证"残件停在原位"（裁决④），删掉它画面不变。
                cp.RecalculateBounds();

                // ── 副本独立性自检（本轮唯一没有源码先例的 API 假设，所以不去"证明"它，直接验证后果）──
                // 官方反编译源码里从来没有 `Instantiate(mesh)` 这种用法（游戏一律 `new Mesh()` + `MeshData.FillMesh` 自己填），
                // 所以"它一定是深拷贝、写副本不会污染共享资产"这句话**没有项目内依据**。这里改用反向验证：
                // E16 只回读"每段三角形数"，E20 **真的写顶点** ⇒ 再抽样回读首/中/末三个顶点的坐标。
                // 只要有一个变了，就说明我们在改**全游戏所有原版塔共用的那张网格**
                // —— 那是比"拉不动"严重得多的事故，必须立刻在日志里喊出来（副本照常返回，因为画面本身还是对的）。
                bool srcIntact = src.vertexCount == vTotal && cp.vertexCount == vTotal;
                for (int s = 0; srcIntact && s < nSub; s++)
                {
                    if (src.GetTriangles(s).Length != srcTriCount[s]) srcIntact = false;
                }
                if (srcIntact && vTotal > 0)
                {
                    Vector3[] after = src.vertices;
                    if (after == null || after.Length != vTotal) srcIntact = false;
                    else
                    {
                        int[] probes = { 0, vTotal / 2, vTotal - 1 };
                        for (int i = 0; i < probes.Length && srcIntact; i++)
                        {
                            Vector3 p0 = verts[probes[i]], p1 = after[probes[i]];
                            if (p0.x != p1.x || p0.y != p1.y || p0.z != p1.z) srcIntact = false;
                        }
                    }
                }

                MeshVariant r = new MeshVariant();
                r.source = src;
                r.mesh = cp;
                r.kept = keptAll;
                r.dropped = droppedAll;
                r.mixed = mixedAll;

                // 特效件：Part 是 struct、pieces 是 **class** 数组 ⇒ 直接改 basePart.pieces[i].mesh 会把
                // **未裁剪那份 spec** 也一起改掉（两个 spec 共用同一个数组对象）。必须新建数组 + 逐件克隆。
                // 这座塔的特效件是"主网格摘出来的 submesh"（日志：顶点数与主网格同为 2600）⇒ 引用同一个副本，
                // 于是下半部分的电流特效自动跟着消失/拉长，不会在切掉的区域留一团悬空的光。
                if (srcPieces != null && srcPieces.Length > 0)
                {
                    EffectPiece[] arr = new EffectPiece[srcPieces.Length];
                    for (int i = 0; i < srcPieces.Length; i++)
                    {
                        EffectPiece ep = srcPieces[i];
                        if (ep == null) continue;
                        EffectPiece ne = new EffectPiece();
                        ne.mesh = ReferenceEquals(ep.mesh, src) ? cp : ep.mesh;   // 独立 prefab 网格的件本轮不裁（保持原样画）
                        ne.mats = ep.mats;               // 材质数组**只共享引用**：染色/门控状态必须与原部件同步，克隆一份反而会分叉
                        ne.subs = ep.subs;
                        ne.baseColors = ep.baseColors;
                        ne.local = ep.local;
                        ne.tag = ep.tag;
                        arr[i] = ne;
                    }
                    r.pieces = arr;
                }
                LogVariant(en.modelId, cropBottom, stretchFrac, tipDist,
                      (stretched
                          ? "**激光副本已建** 段 Y[" + N(yBot) + "~" + N(yTop) + "]（取自**会被画出的**" + usedCount + "/" + vTotal + "个顶点，submesh=" + drawnTag + "）" +
                            " 切口cutY=" + N(cutY) + " 目标尖端Y=" + N(tipTargetY) + "（center.y=" + N(basePart.center.y) + " + dist" + N(tipDist) + "×norm" + N(basePart.norm) + "/scale" + N(scl.y) + "）" +
                            " ⇒ **k=" + N(k) + "倍**（世界缩放 scaleW 已约掉 ⇒ 与恒星大小无关）" +
                            " 尖端横向残差≤" + N(tipLat * scl.y / basePart.norm) + "名义米（P33副作用②，未重算法线）" +
                            " 副本bounds中心=" + cp.bounds.center + " 高=" + N(cp.bounds.size.y) + "（原 " + N(b.size.y) + "）"
                          : "副本已建 cutY=" + N(cropY) + "（bounds Y " + N(b.min.y) + "~" + N(b.max.y) + " 高 " + N(b.size.y) + "）") +
                      " 三角形 留/弃/跨界=" + keptAll + "/" + droppedAll + "/" + mixedAll +
                      " 顶点表" + (stretched ? "仅Y被改、数量保持" + vTotal : "保持" + vTotal) + "（索引不变 ⇒ _VertaBuffer 继续只读共享，动画无需改动）" +
                      " 原件未受污染=" + (srcIntact ? "是" : "**否 ⇒ Instantiate 不是深拷贝，共享资产正被改写，必须立刻停用（拉伸：Builder 把那一条改回 AddModel；裁剪：DefaultCrop 归零 + Builder 改用 UnFoldTower）**") +
                      " 副本顶点=" + cp.vertexCount + " 副本submesh=" + cp.subMeshCount + "（原 " + nSub + "）" +
                      " 索引格式=" + cp.indexFormat +
                      " 特效件" + (r.pieces == null ? 0 : r.pieces.Length) + "件" + (r.pieces != null ? "改指副本" : "") +
                      " 耗时 " + ((Time.realtimeSinceStartup - t0) * 1000f).ToString("0.##") + "ms" +
                      "（center/norm **不重算**、沿用原值 " + basePart.center + "/" + N(basePart.norm) + " ⇒ 残件/光束停在原位）");
                return r;
            }
            catch (Exception e)
            {
                LogVariant(en.modelId, cropBottom, stretchFrac, tipDist, "**失败**：" + e.Message + " ⇒ 按未变体绘制（不崩、不缺席）");
                return fail;
            }
        }

        /// <summary>把"会被画到的"三个顶点标进 used 表（只为第 2 步统计 Y 区间用，不改变任何几何）。</summary>
        private static void MarkUsed(bool[] used, int i0, int i1, int i2)
        {
            used[i0] = true; used[i1] = true; used[i2] = true;
        }

        /// <summary>
        /// 每个 (部件, 刀口/拉法) 只报一次：建副本是首次一次性的动作，刷屏比不报更糟。
        /// 标签按内容分成两种，为的是实测时能一眼 grep：拉伸的打 `[MMS laser]`，纯裁剪继续打 `[MMS crop]`。
        /// </summary>
        private static void LogVariant(int modelId, float cropBottom, float stretchFrac, float tipDist, string what)
        {
            if (stretchFrac > 0f)
                Utils.Log("[MMS laser] 部件id=" + modelId + " 把最头 " + (stretchFrac * 100f).ToString("0.#") + "% 沿 +Y 拉到 " + N(tipDist) + " 名义米 ⇒ " + what);
            else
                Utils.Log("[MMS crop] 部件id=" + modelId + " 切掉下 " + (cropBottom * 100f).ToString("0.#") + "% ⇒ " + what);
        }

        private static bool unknownModelLogged;
        /// <summary>只说一次：Builder 里写了目录还不认识的 id，最典型的后果就是"画面少了一个东西但没有任何报错"。</summary>
        private static void LogUnknownModelId(int modelId)
        {
            if (unknownModelLogged) return;
            unknownModelLogged = true;
            Utils.Log("[MMS warpModel] ModelCatalog 里没有 id=" + modelId + " 这一档部件（现支持 " +
                      string.Join("/", System.Enum.GetNames(typeof(ModularModels))) +
                      "）。这条条目跳过不画；要新增就按 ModelCatalog.cs 顶部那三步来。");
        }

        // ---------------------------------------------------------------- 环的"预制件"：按管径比缓存的程序化网格

        /// <summary>管径比 → 缓存键的量化倍率（1e-5 的分辨率，写 200/4000 这种整数比正好落在同一个键上）。</summary>
        private const float RingKeyScale = 100000f;
        /// <summary>默认那一档（管径比 = RingTubeRatio = 0.05）的键。命中它就必须复用重构前那**同一张**网格。</summary>
        private static readonly int DefaultRingKey = Mathf.RoundToInt(RingTubeRatio * RingKeyScale);
        private static readonly Dictionary<int, Mesh> extraRingMeshes = new Dictionary<int, Mesh>();
        private static readonly Dictionary<int, float> extraRingNorms = new Dictionary<int, float>();

        /// <summary>
        /// 取一个"环"预制件。环不是游戏建筑，所以它的身份是**管径比**（= 条目 thickness / radius），不是 modelId：
        /// · 管径比 == 默认档 ⇒ 直接用 <see cref="ringPart"/> 那一张（程序化的 <see cref="proceduralRing"/>，
        ///   或 useVanillaTorus 时的游戏自带 Torus）与它的 norm / ringBaseRot ⇒ **主环与重构前逐像素同一张网格**；
        /// · 别的管径比 ⇒ 每个比例只造一张并缓存（<see cref="BuildProceduralRing"/> 的分段数沿用主环那档）。
        /// 于是 Builder 里三环都写 `thickness = 200, radius = 4000` 时，三次绘制吃到的就是同一张网格、同一个材质数组
        /// —— 这正是 R26"环逐像素不变"的落实方式（若三环各写不同管径，它们本来也该长得不一样）。
        /// </summary>
        private static bool GetRingSpec(float tubeRatio, out ModelSpec spec)
        {
            spec = default(ModelSpec);
            if (!(tubeRatio > 0f)) return false;         // NaN / 0 / 负数：这条不画（AddRing 忘给 thickness 会落到这里）
            if (tubeRatio > 0.45f) tubeRatio = 0.45f;    // 管半径接近大圆半径时圆环自己穿自己，钳住比让它崩掉好判读

            Part p = default(Part);
            int key = Mathf.RoundToInt(tubeRatio * RingKeyScale);
            if (key == DefaultRingKey)
            {
                p = ringPart;
                // 默认档连"网格自身坐标系的修正"一起沿用 ringBaseRot：程序化环那条是单位四元数，
                // 而 useVanillaTorus 时的游戏自带 Torus 躺在 XY 平面、要转 90° —— 两种来源都由 ResolveAssets 写进那一个字段。
                spec.meshRotFix = ringBaseRot;
            }
            else
            {
                Mesh m;
                float norm;
                if (!extraRingMeshes.TryGetValue(key, out m))
                {
                    m = BuildProceduralRing(RingMajorSeg, RingMinorSeg, tubeRatio);
                    if (m == null) return false;         // 顶点数超 UInt16 上限（分段数太大），跳过而不是崩
                    norm = 1f + tubeRatio;               // 外延 = 大圆半径 1 + 管半径，与默认档那条 norm 同一式子
                    extraRingMeshes[key] = m;
                    extraRingNorms[key] = norm;
                }
                else
                {
                    norm = extraRingNorms[key];
                }
                p.mesh = m;
                p.norm = norm;
                p.center = Vector3.zero;                 // 自造环以原点为圆心，不需要 P13 那种枢轴修正
                spec.meshRotFix = Quaternion.identity;   // 程序化环本就躺在 XZ 平面
            }
            // 材质只在"环借对撞机那套"这一处（ResolveAssets 里算好的 ringMats），所以每次都现取引用、不缓存：
            // 换恒星/重解析后 ringMats 可能换一批，缓存住 spec 会让旧材质挂在层上一整局。
            p.mats = ringMats;
            p.drawMats = ringMats;
            spec.part = p;
            spec.knobs = RingKnobs;
            spec.ringGlow = true;
            spec.glowHaloNominal = 0f;
            return p.mesh != null;
        }
    }
}
