using System;
using UnityEngine;

namespace MoreMegaStructure
{
    /// <summary>
    /// 层级里一条条目"是什么东西"。
    /// · Ring  = 一圈圆环（几何由渲染器程序化生成，管径由条目自己给）；
    /// · Model = 一个游戏里的建筑模型（按 <see cref="ModelEntry.modelId"/> 去 ModelCatalog 换成"网格+材质+旋钮+动画+辉光"那一整套）。
    /// 之所以只有这两种：用户要的"往层级上加预制件"里的"预制件"就是这两类 —— 要么是环，要么是游戏自带的建筑。
    /// </summary>
    public enum EModelEntryKind
    {
        Ring = 0,
        Model = 1,
    }

    /// <summary>
    /// 这一条目**受光方向**怎么算。不是渲染风格，是物理事实：
    /// · RadialFromStar = "从恒星中心沿它所在半径射出去的光"，环着恒星分布的建筑必须走这条（P1 的成因：
    ///   游戏把恒星当方向光用，只有一面亮、其余全背光 ⇒ 必须逐部件把光方向还原成径向）；
    /// · ViewFacing     = "朝向相机的那条方向光"，环整体包围恒星、任何单一方向都无法表达"从内部被照亮"，
    ///   所以取朝相机的这条以获得最完整的受光面（远看是一条亮环）。
    /// 默认值必须是 RadialFromStar = 0：<see cref="ModelLayer.AddModel(int, VectorLF3, Quaternion, float)"/>
    /// 那条最简洁的写法不带这个参数，结构体零值即"径向受光"，正好是塔与接收器该有的方式。
    /// </summary>
    public enum EModelLightMode
    {
        RadialFromStar = 0,
        ViewFacing = 1,
    }

    /// <summary>
    /// 一层里的**一个**放置记录（E14 / R34）。
    ///
    /// 这是"你想一个数据结构用于存储所有模型放置的设定"那条要求的答案：
    /// 一条记录 = "在【层坐标系】的哪儿、摆成什么姿态、多大、放什么东西、它怎么被照亮"，仅此而已。
    /// 它是 struct：条目只是数据，一条层里可能有几十上百条，装箱成对象不值得；拷贝（含 DeepCopy）也就是复制这几个字段。
    ///
    /// ── 单位约定：**全部是"名义米"**，且原点 = 恒星中心、上方向 = 层的极轴 ──
    /// 渲染器把名义米换算成世界米只做一次乘法：`assemblyScale = ModelRenderer.GetAdaptiveScale(star, sphere)`
    /// （E19 / R37 起 = `max(R / NominalBaseRadius, star.radius / ScaleRefRadius)`，R = 该恒星的原版最小壳层半径；
    /// 在 `radius ≤ 1` 的恒星上它就是旧那条 `R / 4000 = 1.0`，更大的恒星按半径线性放大 ⇒ 名义米**永远不用改**）。
    /// 于是"我以为这座巨构有多大"直接写米数就行，而 4000 名义米恰好等于今天的 1.00×R
    /// （等价推导与出处见 `_dev/warpmodel/warp-array-model.md` P30 第 3 条 + P32）。
    /// 好处：你调的是设计尺寸，不是某个星系的比例；换一个恒星整套模型等比缩放、相对关系不变。
    /// ⚠️ E18（R37）起这些**布点数值住在 `WarpArrayModelBuilder`**（环半径/管径、塔位/塔尺寸、倾角、角速度），
    /// 不再从渲染器取常量；渲染器只负责"名义米 → 世界米 + 提交"，不知道"六座塔/三条环"这些布局事实。
    /// </summary>
    public struct ModelEntry
    {
        /// <summary>这条是环还是模型。<see cref="ModelLayer.AddRing"/> 写 Ring，<see cref="ModelLayer.AddModel(int, VectorLF3, Quaternion, float)"/> 写 Model。</summary>
        public EModelEntryKind kind;

        // ───────────────────────── kind == Ring 时有效的字段 ─────────────────────────

        /// <summary>环的大圆半径（名义米）。渲染器按 `thickness / radius` 去取对应管径的网格。</summary>
        public float radius;
        /// <summary>环管的直径（名义米）。想复刻现在的主环就写 200（= 半径 4000 的 5%）。</summary>
        public float thickness;

        // ───────────────────────── kind == Model 时有效的字段 ─────────────────────────

        /// <summary>
        /// 模型的模型 id（对应 <see cref="ModularModels"/>，存 int 是为了让 <see cref="ModelLayer.AddModel(int, VectorLF3, Quaternion, float)"/>
        /// 也能直接吃 LDB 里的原始建筑 id —— 将来要放游戏里任何别的建筑时不用改这套结构）。
        /// </summary>
        public int modelId;
        /// <summary>模型中心在层坐标系里的位置（名义米，恒星中心为原点）。同时决定它的"径向受光"方向。</summary>
        public VectorLF3 position;
        /// <summary>
        /// 模型的局部姿态：先按这个转，再被所在层整体转（渲染器算的是 `rot = layerRot * 这里`）。
        /// 例：`Quaternion.FromToRotation(Vector3.up, -dir)` = "让建筑的 +Y 轴指向恒星"（现在那 6 座塔就是这么摆的）。
        /// 环用它表示"环面相对层平面的倾角"，如 `Quaternion.Euler(30, 0, 0)`。
        /// </summary>
        public Quaternion rotation;
        /// <summary>
        /// 模型的目标尺寸（名义米）。渲染器会再除以网格自己的归一化外延，所以这里写的是"我要它多大"，
        /// 与模型 prefab 内部单位无关。三个轴可以分开给（各向同性时 x=y=z）。
        /// </summary>
        public VectorLF3 scale;

        // ───────────────────────── 两者都可能用到的 ─────────────────────────

        /// <summary>受光方式（见 <see cref="EModelLightMode"/>）。环目前固定走"朝向相机"那一路，这个字段对环暂不生效。</summary>
        public EModelLightMode lightMode;

        /// <summary>
        /// E16（R35）：**从这个模型的下部切掉多少比例**（0.4 = 切掉下 40% 只留上 60%）。
        ///
        /// ⚠️ E17（R36）起 `0` 的含义变了：**"跟随该枚举自带的默认"**（`ModelCatalog.DefaultCrop`），
        /// 而**不是**"不切"。所以 `TowerCrop40` 这一条目写 0 也是切 40%；想切别的比例才在这里给数，
        /// 想**完全不切**要改用不带默认的枚举位（`UnFoldTower`）。日常写 Builder 用 <see cref="ModelLayer.AddModel(ModularModels, VectorLF3, Quaternion, float)"/>
        /// 就够了，这个字段只在需要"同一个枚举、这一条目切得跟别人不一样"时填。
        ///
        /// "下"指的是**模型自己局部坐标的 Y 小的一端**，不是屏幕上的下方。对无线输电塔来说那一端正好是
        /// **远离恒星的那头**（Builder 里 `FromToRotation(Vector3.up, -dir)` 把 +Y 摆成指向恒星 ⇒ 塔尖朝恒星）。
        /// 切面位置 = `mesh.bounds.min.y + bounds.size.y * 这个值`，三角形按"三个顶点是否全在保留侧"取舍。
        ///
        /// 三条使用须知：
        /// · 只作用在**我们自己 Instantiate 出来的网格副本**上，游戏资产一个字节都不改（原版塔不受影响）；
        /// · 顶点表保持原样、只重写三角形索引 ⇒ 顶点动画数据(`_VertaBuffer`)继续按原索引寻址，动画不会错行；
        /// · 摆放用的 `center/norm` 仍取原网格 bounds ⇒ 剩下的部分**停在原位**，不会被重新居中或放大。
        /// 同一个模型在不同层可以各切各的比例，每个比例只建一张副本并缓存（第一次画那一刀时建，之后当常量用）；
        /// 缓存键是 **(源网格, 比例)** 而不是 (modelId, 比例) —— 四个塔枚举共用同一张 `nodePart.mesh`，那样会裁出重复副本。
        /// </summary>
        public float cropBottom;

        /// <summary>
        /// E20（R38）：**把模型最头 <c>topFraction</c> 那一段沿它自身的 +Y 轴拉伸，直到尖端顶点正好落在指定距离上**。
        /// 这是 <see cref="ModelLayer.AddLaser"/> 的实现路径，日常写 Builder 不要手填这两个字段，调一个方法就行。
        ///
        /// 语义（推导与全部依据在文档 **P33**，这里是使用须知）：
        /// · `0` = 本条目**不拉伸**（绝大多数条目走这一路，与 E16/E17 的画面完全相同）；
        /// · `0.2f` = "竖直上最头 20% 的这一段"是被拉的对象，**其余顶点一个字都不动**（用户原话"其他部分不动"）；
        ///   段的位置按"本条目实际参与绘制的那部分顶点的 Y 区间"算，不是按整个网格的 bounds ⇒ 只画特效件时，
        ///   结构层那 1522 个不画的顶点不会把区间撑大（塔特效实测 Y = [0, 6.787] ⇒ 切口 5.4296）。
        /// · <see cref="stretchTipDist"/> = 沿 +Y 方向，**尖端顶点最终要到达的位置距条目中心多远**（名义米）。
        ///   解出的倍率 `k = (center.y + dist·norm/scale − cutY) / (yTop − cutY)`：`k` 与恒星大小**无关**
        ///  （世界缩放 `scaleW` 在式子里约掉了），所以激光的相对形状在任何恒星系都一样，只是整条被等比放大。
        /// · 与 <see cref="cropBottom"/> **可以共存**（先按裁剪决定哪些三角形活下来，再对活着的顶点做 Y 重映射），
        ///   但当前出厂档"激光"用的预制件是 <see cref="ModularModels.TowerEffectOnly"/>，它 `DefaultCrop = 0` ⇒ 只做拉伸。
        /// · ⚠️ 拉伸**必然重算网格 bounds**（与裁剪那一路"故意不重算"相反）：本地 bounds 会从 7.6 高变成 20+ 高，
        ///   沿用旧 bounds 会让视锥剔除按"塔那么大"去判 ⇒ 某个角度整条光束凭空消失。`center/norm` 仍取原值 ⇒ 摆位不变。
        /// · 只有**第一个**用到某个 (源网格, 切口, 尖端距离) 的条目会真正建一张网格副本，之后按缓存复用。
        /// </summary>
        public float stretchTopFrac;

        /// <summary>与 <see cref="stretchTopFrac"/> 配对：尖端顶点要达到的**名义米距离**（沿条目 +Y 方向，从条目中心量起）。</summary>
        public float stretchTipDist;
    }
}
