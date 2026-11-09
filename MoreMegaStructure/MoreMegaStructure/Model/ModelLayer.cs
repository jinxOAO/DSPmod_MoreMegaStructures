using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreMegaStructure
{
    /// <summary>
    /// 这个类是为了每个不同巨构可能有多层可以以不同速度旋转的层级的模型而准备的。部分巨构如果不需要互相旋转，则只需要一层这个modelLayer来构建模型即可。但整个层一定是一个整体，绝对不会互相移动位置
    /// 所有层的中心0,0,0均设定为恒信中心。相当于是下面使用的坐标都是相对坐标，旋转也是
    ///
    /// ── E14（R34）落实说明 ──
    /// 一类 = 一个"绕极轴转的刚体"。层内所有条目共享同一个角速度 <see cref="rotateSpeed"/>，
    /// 所以"整个层一定是一个整体"这件事由渲染器保证：它每帧只算一次 `layerRot = frame * Euler(0, t*rotateSpeed, 0)`，
    /// 然后前乘到该层每一条目的局部姿态/局部位置上，条目之间永远不会互相移动。
    /// 单位一律是**名义米**（原点 = 恒星中心），换算关系见 <see cref="ModelEntry"/> 与文档 P30 第 3 条。
    /// </summary>
    public class ModelLayer
    {
        public float rotateSpeed; // 该层的所有组装好的模型绕y旋转的速度。

        // 这里还应该存储一个数据结构用于存储所有模型放置的设定，你想一个。有必要的话新增新的类也没问题，在这个文件下或者新建一个文件的一个类。
        // 答：就是下面的 entries（每条是一个 ModelEntry，定义在 Model/ModelEntry.cs）。
        // 为什么用"扁平一条记录"而不是"每种东西一个 List"：Builder 里你只会反复调 AddRing/AddModel 往里追加，
        // 顺序即绘制顺序；渲染器只管从头到尾遍历，不需要知道"第 3 个环"这种全局序号，也就不存在多处索引要同步。

        // 是否还应该存储一些特殊的渲染设置
        // 暂时**不需要**，而且刻意不加：观感类设置（亮度档、自发光倍率、掺色强度、辉光开关、动画定格……）全都属于
        // "这个模型该长什么样"，已经由 ModelRenderer/ModelCatalog 按 modelId 一次性配好（每个枚举值一整套参数），
        // 层只负责"摆哪儿、怎么转"。真到了"同一个模型在不同层要长得不一样"那一天，再往 ModelEntry 加一个 knobs 字段即可，
        // 现在先留空是为了不让你面对一堆用不上的旋钮。

        /// <summary>这一层里的所有放置记录，顺序即绘制顺序（结构层是 opaque，顺序只影响极少数同深度情形）。</summary>
        public readonly List<ModelEntry> entries = new List<ModelEntry>();

        /// <summary>
        /// 
        /// </summary>
        public ModelLayer() 
        {

        }

        /// <summary>给一个"会转的层"：角度速度按 <see cref="rotateSpeed"/>（弧度/秒，绕层自身的 Y 轴 = 极轴）。</summary>
        public ModelLayer(float rotateSpeed)
        {
            this.rotateSpeed = rotateSpeed;
        }

        /// <summary>
        /// 为这一层新增一个测试样例中的实体环
        /// </summary>
        /// <param name="radius"></param>
        public void AddRing(float radius, float thickness = 1)
        {
            AddRing(radius, Quaternion.identity, thickness);
        }

        /// <summary>
        /// 创建不是平面垂直于转轴的环
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="rotation"></param>
        /// <param name="thickness"></param>
        public void AddRing(float radius, Quaternion rotation, float thickness = 1)
        {
            ModelEntry en = new ModelEntry();
            en.kind = EModelEntryKind.Ring;
            en.radius = radius;
            en.thickness = thickness;
            en.rotation = rotation;        // 环的"倾角"：局部姿态先转，再被整层前乘（渲染器那条 rot = layerRot * rotation）
            en.position = VectorLF3.zero;  // 环永远以恒星中心为圆心（要偏心请用它所属的层去转，别改这里）
            en.scale = VectorLF3.zero;     // 环的尺寸由 radius/thickness 决定，这两个字段对环无效
            en.lightMode = EModelLightMode.ViewFacing;   // 环整体包围恒星，取"朝相机的受光方向"才看得出完整亮环（见 EModelLightMode）
            entries.Add(en);
        }

        public void AddModel(int modelId, VectorLF3 position, Quaternion rotate, float scale)
        {
            AddModel(modelId, position, rotate, new VectorLF3(scale, scale, scale));
        }

        /// <summary>
        /// 该方法用于在当前层加入一个模型实体
        /// </summary>
        /// <param name="modelId"></param>
        /// <param name="position"></param>
        /// <param name="rotate"></param>
        /// <param name="scale"></param>
        public void AddModel(int modelId, VectorLF3 position, Quaternion rotate, VectorLF3 scale)
        {
            AddModel(modelId, position, rotate, scale, EModelLightMode.RadialFromStar);
        }

        /// <summary>写 Builder 时更顺手的一档：直接给 <see cref="ModularModels"/> 枚举，不用自己 (int) 转来转去。</summary>
        public void AddModel(ModularModels modelId, VectorLF3 position, Quaternion rotate, VectorLF3 scale)
        {
            AddModel((int)modelId, position, rotate, scale, EModelLightMode.RadialFromStar);
        }

        /// <summary>
        /// 同上，但各向同性尺寸（绝大多数情况）。这也是 WarpArrayModelBuilder 里实际用到的那一条。
        /// E17 起它的裁剪比例**由枚举自己决定**（`ModelCatalog.DefaultCrop`）：`AddModel(ModularModels.TowerCrop40, …)`
        /// 不写任何数字也是切 40%，而 `UnFoldTower` 是完整的一整座、`TowerEffectOnly` 不切但只画特效件。要覆盖这个默认才用 <see cref="AddModelCropped(ModularModels, VectorLF3, Quaternion, float, float)"/>。
        /// </summary>
        public void AddModel(ModularModels modelId, VectorLF3 position, Quaternion rotate, float scale)
        {
            AddModel((int)modelId, position, rotate, new VectorLF3(scale, scale, scale), EModelLightMode.RadialFromStar);
        }

        /// <summary>
        /// E16（R35）：**显式指定切掉模型下部**的一档 —— 与前一条唯一的区别就是多给一个 `cropBottom`。
        /// 用法（Builder 里就这么一行，别的都不用碰）：
        /// <code>layer.AddModelCropped(ModularModels.UnFoldTower, dir * 4000, Quaternion.FromToRotation(Vector3.up, -dir), 800, 0.4f);</code>
        /// 意思是"这座塔我只留上 60%，切掉远离恒星那 40%"。
        /// ⚠️ E17（R36）之后 **0f 不再等于"不切"，而是"跟随该枚举自带的默认"**（见 <see cref="AddModel(ModularModels, VectorLF3, Quaternion, float)"/>）：
        /// `AddModelCropped(ModularModels.TowerCrop40, …, 0f)` 照样切 40%。想**完全不切**的正确写法是改用不带默认的枚举位
        /// （`AddModel(ModularModels.UnFoldTower, …)`），不是在这里写 0f。
        /// 具体语义（"下"是哪一头、切口为什么是敞开的、为什么残件停在原位）全在 <see cref="ModelEntry.cropBottom"/> 的注释里。
        /// </summary>
        /// <param name="modelId">部件种类（ModelCatalog 里的枚举）</param>
        /// <param name="position">层坐标系里的位置（名义米）</param>
        /// <param name="rotate">局部姿态（先它后层）</param>
        /// <param name="scale">各向同性尺寸（名义米）</param>
        /// <param name="cropBottom">从下部切掉的比例，0~0.95；0 = 用该枚举自带的默认（<c>DefaultCrop</c>）</param>
        public void AddModelCropped(ModularModels modelId, VectorLF3 position, Quaternion rotate, float scale, float cropBottom)
        {
            AddModel((int)modelId, position, rotate, new VectorLF3(scale, scale, scale), EModelLightMode.RadialFromStar, cropBottom);
        }

        /// <summary>同上，但三轴尺寸分开给（切完之后还想把某个轴再拉伸时用这一条）。</summary>
        public void AddModelCropped(ModularModels modelId, VectorLF3 position, Quaternion rotate, VectorLF3 scale, float cropBottom)
        {
            AddModel((int)modelId, position, rotate, scale, EModelLightMode.RadialFromStar, cropBottom);
        }

        /// <summary>
        /// 最完整的一档：连受光方式也给出来。
        /// 默认 RadialFromStar（"从恒星中心沿半径射出的光"）= 现在这 6 座塔与旧 8 座接收器的实际受光方式；
        /// 只有当你放了一个"不该被恒星星光照亮"的东西（例如一块自发光板）才需要换 ViewFacing。
        /// </summary>
        public void AddModel(int modelId, VectorLF3 position, Quaternion rotate, VectorLF3 scale, EModelLightMode lightMode)
        {
            AddModel(modelId, position, rotate, scale, lightMode, 0f);
        }

        /// <summary>
        /// 所有 AddModel / AddModelCropped / AddLaser 的**唯一汇合点**：一条记录在这里填齐并追加进 entries。
        /// 新增字段只改这一处（E16 加 cropBottom、E20 加那两个 stretch 字段都是只动这里 + 上面的转发），
        /// 所以其余重载的语义绝不会走偏。
        /// </summary>
        public void AddModel(int modelId, VectorLF3 position, Quaternion rotate, VectorLF3 scale, EModelLightMode lightMode, float cropBottom)
        {
            AddModel(modelId, position, rotate, scale, lightMode, cropBottom, 0f, 0f);
        }

        /// <summary>
        /// 最完整的一档（E20 / R38）：连"把最头一段拉到多远"也给出来。
        /// **日常请调 <see cref="AddLaser(VectorLF3, VectorLF3, float)"/>，不要手填后两个参数** ——
        /// 这个重载存在只是为了让"汇合点只有一个"这句话仍然成立（<see cref="AddLaser"/> 也走这里）。
        /// 后两个字段的语义与三条使用须知（为什么必须重算 bounds、为什么 `k` 与恒星大小无关）见 <see cref="ModelEntry.stretchTopFrac"/>。
        /// </summary>
        public void AddModel(int modelId, VectorLF3 position, Quaternion rotate, VectorLF3 scale, EModelLightMode lightMode, float cropBottom, float stretchTopFrac, float stretchTipDist)
        {
            ModelEntry en = new ModelEntry();
            en.kind = EModelEntryKind.Model;
            en.modelId = modelId;
            en.position = position;
            en.rotation = rotate;
            en.scale = scale;
            en.lightMode = lightMode;
            en.radius = 0f;        // 环字段对模型无效，明确写成 0 以免以后判读日志时以为"这里还有个环"
            en.thickness = 0f;
            // 钳到 [0, 0.95]：写负数一律归 0（0 的语义是"跟随枚举默认"，见 ModelCatalog.DefaultCrop），
            // 写 >=1 也不会变成"整个消失"（宁可留 5% 也别让画面少一个东西还查半天）
            en.cropBottom = cropBottom <= 0f ? 0f : (cropBottom > 0.95f ? 0.95f : cropBottom);
            // 拉伸的两个字段：比例钳到 (0, 0.95]，距离必须为正；任一不成立就**整条不拉**（退回普通模型，
            // 宁可少一个特效也不要"解出个负倍率把尖端翻到屁股后面去"这种难查画面）
            if (stretchTopFrac > 0f && stretchTipDist > 0f)
            {
                en.stretchTopFrac = stretchTopFrac > 0.95f ? 0.95f : stretchTopFrac;
                en.stretchTipDist = stretchTipDist;
            }
            entries.Add(en);
        }

        /// <summary>激光预制件：默认**只画电流特效**那一档（E17 的成果，用户 R38 称它"很可能就是未来激光的基础预制件"）。</summary>
        public const ModularModels LaserPrefab = ModularModels.TowerEffectOnly;

        /// <summary>默认被拉的是"最头 20%"那一段（用户 R38 原话里的数字）。要改比例就用带 `topFraction` 的那条重载。</summary>
        public const float LaserTipFrac = 0.2f;

        /// <summary>
        /// 增加一个类似激光样的电流特效，朝向根据position和endPosition自动计算
        /// </summary>
        /// <param name="position">原本模型的中心</param>
        /// <param name="endPosition">原本模型通过任意倍率纵向拉长头部的20%的特效使得最顶端的坐标点直达endPosition</param>
        /// <param name="scale">原始模型的缩放，对于纵向任意延长的修改来讲实际上控制了粗细</param>
        public void AddLaser(VectorLF3 position, VectorLF3 endPosition, float scale)
        {
            AddLaser(position, endPosition, scale, LaserTipFrac);
        }

        /// <summary>
        /// 同上，但自己决定"拉最头那百分之几"（E20 / R38）。默认档见 <see cref="LaserTipFrac"/>。
        ///
        /// ── 这条方法到底做了什么（推导与全部依据在文档 **P33**）──
        /// 1. **朝向**：`FromToRotation(Vector3.up, endPosition − position)` ⇒ 模型的 +Y 轴正好指向终点
        ///    （与 Builder 里那 5 条纯特效"塔尖朝恒星"是同一个方向约定，因为它们的 +Y 也指向恒星）。
        /// 2. **长度**：把两者距离（名义米）交给条目，由 <c>ModelCatalog</c> 在**第一次绘制**时解出倍率
        ///    `k = (center.y + dist·norm/scale − cutY) / (yTop − cutY)`，然后只对 `y > cutY` 的顶点做
        ///    `y' = cutY + (y − cutY)·k`。⇒ 其余顶点一字不动（"其他部分不动"），而尖端顶点精确落到 `endPosition`。
        /// 3. **粗细**：只改 Y ⇒ 横向尺寸仍由 `scale` 决定，所以 `scale` 在这里的实际作用就是"光束多粗"
        ///    （用户注释原文）。同一张网格副本的横向残差另见 P33 副作用②（本轮在 `[MMS laser]` 里打出来）。
        /// 4. **与恒星大小无关**：`k` 的式子里世界缩放 `scaleW` 会约掉 ⇒ 换恒星只是整条等比放大（E19 负责那个倍率）。
        /// 5. **代价**：这条会为该预制件建**一张额外的网格副本**（与另外 5 条纯特效不共用，所以那 5 条与官方资产
        ///    仍是同一张原网格）；副本的 `bounds` 会被重算（拉伸必须重算，否则视锥剔除会把长光束误剔掉）。
        /// ⚠️ 若把 <see cref="LaserPrefab"/> 换成带结构层的枚举（`UnFoldTower` / `TowerCrop40`），拉伸会**作用在整张网格上**
        ///    ⇒ 塔身也会一起变长（这不是 bug：特效件与主网格本就是同一张网格的两个 submesh）。要"只长光束不长塔身"就用默认档。
        /// </summary>
        public void AddLaser(VectorLF3 position, VectorLF3 endPosition, float scale, float topFraction)
        {
            Vector3 d = (Vector3)endPosition - (Vector3)position;      // VectorLF3 → Vector3 是隐式转换（VectorLF3.cs:207）
            if (d.sqrMagnitude < 1e-6f) return;   // 终点与起点重合 ⇒ 没有轴向可解，整条不建（宁缺毋滥，别留一根竖着的短桩）
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, d.normalized);
            float dist = (float)position.Distance(endPosition);        // 名义米；用 double 版 Distance 少一次浮点误差
            // 走唯一汇合点：受光方式仍给 RadialFromStar（激光是"从恒星中心沿半径射出去"的，与那 5 条一致）
            AddModel((int)LaserPrefab, position, rot, new VectorLF3(scale, scale, scale),
                EModelLightMode.RadialFromStar, 0f, topFraction, dist);
        }

        /// <summary>
        /// 深拷贝，用于各个巨构创建不同阶段甚至同一阶段不同层级时使用
        /// </summary>
        /// <returns></returns>
        public ModelLayer DeepCopy()
        {
            var copy = new ModelLayer();
            copy.rotateSpeed = this.rotateSpeed;
            // ModelEntry 是 struct ⇒ AddRange 复制的是**值**，副本与原件之后再各自 AddModel 也绝不会互相影响
            // （这正是"lvl2 在 lvl1 基础上继续加东西"要的那条语义；若条目是 class，这里就必须逐字段 new 一遍）。
            if (entries.Count > 0) copy.entries.AddRange(entries);
            return copy;
        }
    }
}
