using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreMegaStructure
{
    /// <summary>
    /// 折跃场（Warp Array）的**模型创建**：只在游戏首次读档时跑一次，产物当常量用（E14 / R34）。
    ///
    /// 这就是"我能根据自己的想象很简单地构建模型"那一层：往需要的层级上直接加预制件 ——
    /// <code>
    /// ModelLayer layer = new ModelLayer();
    /// layer.rotateSpeed = ...;                    // 这一层绕极轴转多快（弧度/秒）
    /// layer.AddRing(4000, 200);                   // 加一圈半径 4000 米、管径 200 米的环
    /// layer.AddModel(ModularModels.UnFoldTower, dir * 4000, Quaternion.FromToRotation(Vector3.up, -dir), 800);
    /// layer.AddModel(ModularModels.TowerCrop40, dir * 4000, Quaternion.FromToRotation(Vector3.up, -dir), 800);              // 同一座塔，枚举自带"切掉下 40%"
    /// layer.AddModel(ModularModels.TowerEffectOnly, dir * 4000, Quaternion.FromToRotation(Vector3.up, -dir), 800);          // 只有电流特效，没有塔身
    /// layer.AddModelCropped(ModularModels.UnFoldTower, dir * 4000, Quaternion.FromToRotation(Vector3.up, -dir), 800, 0.7f); // 不靠枚举默认，这一刀自己定
    /// layer.AddLaser(dir * 4000, VectorLF3.zero, 800);        // 一条激光：把最头 20% 的特效段沿轴拉到恒星中心（E20 / R38）
    /// </code>
    /// 单位一律是**名义米**（4000 名义米 = 该恒星的原版最小壳层半径 R，见 ModelEntry 与文档 P30 第 3 条）。
    /// ⚠️ E18（R37）起这一层的数值**全部自带**（见下面"布点数值"那一节），不再引用渲染器里的任何固定值：
    /// 渲染器只负责"把名义米换算成世界米 + 提交"，它已经不知道"六座塔""三条环"这些布局事实。
    /// 这里的数字就是"当前画面"的全部事实：3 条倾角 30°/90°/150°、快慢不同且交替方向的进动环 +
    /// 6 个摆在正八面体顶点、塔尖指向恒星的**无线输电塔特效预制件**（E17 / R36 起为"只留电流特效"，
    /// 所以画面上是 6 处悬空的电弧、没有塔身；换回塔身改 `TowerPrefab` 一个词）。没有接收器（R32），没有自研辉光（R32）。
    /// ⚠️ E20（R38）再改一档：`OctaDirs[0]` 那一条现在是**激光**（把电弧的最头一段沿光束轴拉到恒星中心）
    /// ⇒ 当前画面 = **1 条顶着恒星中心的长光束 + 5 处原来的悬空电弧**（同屏对照，见 `LaserTestIndex`）。
    /// 观感参数（颜色/亮度/动画/辉光）不在这里，那些跟着 ModularModels 住在 ModelCatalog.cs。
    /// </summary>
    public static class WarpArrayModelBuilder
    {
        public static List<List<ModelLayer>> layersByLevel; // 根据不同完成度存储模型的具体内容
        public static List<long> energyThreshold = new List<long> { 10, 20, 30, 40, 50}; // 达到多少能量可以渲染当前等级的模型，采用energyGenPerTick直接判断，暂时设定的很小用于测试
        // ↑ 更正一处（文档 P30 第 2 条）：`energyGenPerTick` 这个名字在当前反编译源码里**不存在**，
        //   真实字段是 `DysonSphere.energyGenCurrentTick`（public long，DysonSphere.cs:2156，每 tick 在 :1452-1493 重算）。
        //   读它的那一行在 ModelHandler.CurrentLevel 里，阈值表仍然只有这一处出处。

        // ── E18（R37）：**这座巨构的全部布点数值就住在这一节，别处不再有第二份** ──
        // 用户原话："所有builder里面使用的 addring addmodel 之类的里面的坐标、半径等数值，都不要引用 ModelRenderer
        // 里面的固定值，而是由开发者写好确定的数值，至少也是用变量固定在 builder 类里面"。裁决 = **搬进来并删除**
        // （渲染器侧那 10 个常量已从 `ModelRenderer.cs` 删掉，见文档 §5 E18 / §6 第十九轮）。
        // 这些数字**不是新调的观感**，它们就是 E14 那套 `ratio × 4000` 算出来的结果（`NominalBaseRadius = 4000`
        // 仍是渲染器的尺度锚，因为它是"名义米 → 世界米"的换算常数，不是布点数值）：
        //   RingRadius   = RingRadiusRatio(1.00) × 4000   RingTube   = RingTubeRatio(0.05) × 4000
        //   TowerOffset  = NodeOffsetRatio(1.00) × 4000   TowerSize  = NodeSizeRatio(0.20) × 4000
        //   ReceiverOffset = SubOffsetRatio(0.72) × 4000  ReceiverSize = SubSizeRatio(0.14) × 4000
        // ⇒ 这一轮改完，画面与上一版**逐像素相同**（R26 那条基准仍然靠"三环共用同一个 RingTube"守住）。
        // 之所以写成常量而不是散在各行的字面量：三条环/六座塔要互相耦合（同管径、同速度基准），改一个词就整套跟着走。
        private const float Spin = 0.018f;                          // 基准进动角速度（弧度/秒）：刻意取很小 ⇒ "看着在动"但不像玩具；
                                                                    // 0.018 rad/s ≈ 1°/秒，整圈约 6 分钟（各层按下面倍率取用）
        private static readonly float[] RingSpeedMul = { 1f, 0.62f, 1.35f };   // 三环快慢倍率，方向在下面是逐层各写一次的
        private const float TiltBase = 30f;                         // 第一个环：环面法线与极轴夹角（度）
        private const float TiltStep = 60f;                         // 相邻环夹角步长（30 / 90 / 150）
        private const float RingRadius = 4000f;                     // 主环大圆半径（名义米）
        private const float RingTube = 200f;                        // 主环管径（名义米，= 半径的 5%）
        private const float TowerOffset = 4000f;                    // 主发生器所在球面半径（名义米）
        private const float TowerSize = 800f;                       // 主发生器目标尺寸（名义米）
        private const float ReceiverOffset = 2880f;                 // 次级接收器所在球面半径（下面那段注释掉的层用）
        private const float ReceiverSize = 560f;                    // 次级接收器目标尺寸

        /// <summary>
        /// 整套模型在**名义米**空间里的最外沿半径（E23 的 <c>ModelRenderer.ProbeFar</c> 用它算模型外沿）。
        /// 为什么这个数由 Builder 出、而不是渲染器写死一个数：E18 已经把布点数值归还本类，
        /// "模型到底铺到多远"是布点事实，不是渲染事实；渲染器只负责把它乘上世界缩放。
        /// 取的是各角色"中心 + 自身尺寸"的保守上界，不做逐层旋转/倾斜的精确包围 ——
        /// 诊断要判的是"有没有几何越过远裁剪面"，**宁可高估也不许低估**（低估会漏报）。
        /// 接收器那一项现在恒不参与（摆放它的那段是注释掉的，见下面的 `layer0_4`，R32 撤掉了），留着是为了
        /// 将来取消注释时不必想起来补这一处。
        /// </summary>
        public static float GetNominalEnvelope()
        {
            float ring = RingRadius + RingTube;             // 主环外沿（大圆半径 + 管径）
            float tower = TowerOffset + TowerSize;          // 塔：所在球面 + 自身尺寸
            float receiver = ReceiverOffset + ReceiverSize; // 接收器：同上
            float a = ring > tower ? ring : tower;
            return a > receiver ? a : receiver;
        }

        // ── E14：这两张"布点表"是从渲染器搬过来的 ──
        // 往哪些位置放预制件属于**模型创建**，不该藏在"每一帧怎么画"的机器里。
        // 渲染器现在只知道"层 → 条目 → 提交"，不知道"六座塔""八座接收器"这种布局事实。
        // 写成 public 有两个理由：① CubeDirs 眼下只被下面那段"注释掉的接收器"用到，私有的话编译器会报
        // CS0414（赋了值却从没读）；② 你以后要给别的层级铺"球面均匀布点"时，这两张表本身就是可复用的素材。
        public static readonly Vector3[] OctaDirs =
        {
            new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f),
            new Vector3(0f, 1f, 0f), new Vector3(0f, -1f, 0f),
            new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, -1f),
        };

        public static readonly Vector3[] CubeDirs = NormalizeAll(new Vector3[]
        {
            new Vector3(1f, 1f, 1f), new Vector3(-1f, 1f, 1f),
            new Vector3(1f, -1f, 1f), new Vector3(-1f, -1f, 1f),
            new Vector3(1f, 1f, -1f), new Vector3(-1f, 1f, -1f),
            new Vector3(1f, -1f, -1f), new Vector3(-1f, -1f, -1f),
        });

        private static Vector3[] NormalizeAll(Vector3[] arr)
        {
            for (int i = 0; i < arr.Length; i++) arr[i] = arr[i].normalized;
            return arr;
        }

        // ── E16/E17：塔用哪一种预制件，就改这一个词 ──
        // 三档都是**同一座无线输电塔**，差别只在渲染侧（裁剪比例住在 ModelCatalog.DefaultCrop，特效门控住在 ModelSpec.effectOnly）：
        // · `ModularModels.TowerEffectOnly` = **只有电流特效**（结构层那一次绘制整个跳过，E17 / R36 当前档）
        // · `ModularModels.TowerCrop40`     = 上 60% 的塔（切掉远离恒星的下 40%，切口敞开、残件停在原位，E16 / R35）
        // · `ModularModels.UnFoldTower`     = 完整的一整座塔（E14 重构后的原始画面）
        // 之所以这里**不写裁剪比例**：`TowerCrop40` 自带 40% 默认值，一个数字有两个家正是上一轮被收掉的那个重复
        // （原先这里的 `TowerCropBottom` 常量已删，见文档 P31 末段）。想显式覆盖别的比例才用 `AddModelCropped(…, 0.7f)`。
        public const ModularModels TowerPrefab = ModularModels.TowerEffectOnly;

        // ── E20（R38）：六条里哪一条摆激光（裁决③"1 条激光 + 5 条纯特效"）──
        // 只改这个数字就能换一条：`OctaDirs` 的 6 个方向依次是 +X / −X / +Y(北极) / −Y(南极) / +Z / −Z。
        // 写成 6 或 −1 就是"这条不要了、六条全是纯特效"（下面的循环里那个 `i == LaserTestIndex` 永不命中）。
        // 注意激光的**长度**不由这里决定，而由 `TowerOffset`（点到恒星中心的距离）与 `TowerSize`（粗细）决定，
        // 见 ModelLayer.AddLaser 与文档 P33 第 3 条：拉伸倍率 `k = (center.y + dist·norm/scale − cutY) / (yTop − cutY)`，
        // 其中 `scaleW` 会约掉 ⇒ **光束的相对形状与恒星大小无关**，换恒星只是整条等比放大（E19）。
        public const int LaserTestIndex = 0;

        /// <summary>首次读档时建一次（由 ModelHandler.InitWhenFirstLoad 调用；那里有幂等保护，本方法本身可以重复执行）。</summary>
        public static void InitModelLayers()
        {
            layersByLevel = new List<List<ModelLayer>>();
            
            // lvl1
            layersByLevel.Add(new List<ModelLayer>());
            ModelLayer layer0_0 = new ModelLayer();
            // 接下来就直接对这一层进行设置，来创建模型了，可以反复调用创建东西之类的
            //
            // ── 环 1：倾角 30°、基准速度 ──
            // 三条环必须分属三条层：层的角速度是"整层共用"的，而旧写法里三条环各自有快慢与方向
            //（`spinDeg * RingSpeedMul[i] * (i 偶 ? +1 : -1)`）。环的倾角则属于条目自己（局部姿态）。
            layer0_0.rotateSpeed = Spin * RingSpeedMul[0];
            layer0_0.AddRing(RingRadius,
                Quaternion.Euler(TiltBase + 0f * TiltStep, 0f, 0f),
                RingTube);
            // 其他操作
            ModelLayer layer0_1 = new ModelLayer();
            // ── 环 2：倾角 90°、慢一档且反向（倍率 0.62，方向取负）──
            layer0_1.rotateSpeed = -Spin * RingSpeedMul[1];
            layer0_1.AddRing(RingRadius,
                Quaternion.Euler(TiltBase + 1f * TiltStep, 0f, 0f),
                RingTube);
            // 其他操作
            ModelLayer layer0_2 = new ModelLayer();
            // ── 环 3：倾角 150°、快一档且正向（倍率 1.35）──
            layer0_2.rotateSpeed = Spin * RingSpeedMul[2];
            layer0_2.AddRing(RingRadius,
                Quaternion.Euler(TiltBase + 2f * TiltStep, 0f, 0f),
                RingTube);
            // 其他操作
            ModelLayer layer0_3 = new ModelLayer();
            // ── 6 座主发生器（无线输电塔）：正八面体顶点，塔尖指向恒星，整组以基准速度绕极轴转 ──
            // 旧式是 `orient * FromToRotation(up, -OctaDirs[i])`，这里把 `orient`（= 层旋转）交给 rotateSpeed、
            // 把 FromToRotation 交给条目局部姿态，两者在渲染器里再乘回去，式子与重构前同一条（P30 第 4 条）。
            layer0_3.rotateSpeed = Spin;
            for (int i = 0; i < OctaDirs.Length; i++)
            {
                Vector3 dir = OctaDirs[i];
                // 位置 / 姿态 / 尺寸三个参数从 E14 重构起一个字都没动（P30 第 4 条：与重构前是同一条式子），
                // 变的只有"用哪个预制件"—— 见上面那个 TowerPrefab 常量，想换画面只改那一行。
                // ⚠️ E20（R38）裁决③：**其中一条换成激光**，与另外 5 条纯特效同屏对照。
                // 激光用的仍是同一个预制件（`TowerEffectOnly`）的**另一份网格副本** ⇒ 这一屏正好能验出
                // "只有那一条建了副本、其余 5 条与官方资产都没被污染"（P33 副作用④的实测兜底）。
                if (i == LaserTestIndex)
                {
                    // 终点写 VectorLF3.zero = 恒星中心（依据 `ModelLayer` 类注释"所有层的中心0,0,0均设定为恒星中心"）
                    layer0_3.AddLaser(dir * TowerOffset, VectorLF3.zero, TowerSize);
                    continue;
                }
                layer0_3.AddModel(TowerPrefab, dir * TowerOffset,
                    Quaternion.FromToRotation(Vector3.up, -dir), TowerSize);
            }
            // 其他操作

            layersByLevel[0].Add(layer0_0);
            layersByLevel[0].Add(layer0_1);
            layersByLevel[0].Add(layer0_2);
            layersByLevel[0].Add(layer0_3);

            // ── 想放回那 8 座射线接收器（R32 删掉的一层）就把下面这段注释去掉 ──
            // ModelLayer layer0_4 = new ModelLayer();
            // layer0_4.rotateSpeed = -Spin * 0.55f;                          // 反向且慢一档，与塔形成对转观感
            // for (int i = 0; i < CubeDirs.Length; i++)
            // {
            //     Vector3 rdir = CubeDirs[i];
            //     layer0_4.AddModel(ModularModels.RayReceiver, rdir * ReceiverOffset,
            //         Quaternion.FromToRotation(Vector3.up, -rdir), ReceiverSize);
            // }
            // layersByLevel[0].Add(layer0_4);
            // 加进来之后渲染器会自己发现"层级用到了 RayReceiver"并去装载那只 prefab（needReceiver），
            // 不需要再翻任何渲染器开关 —— 这就是 showSubStructure 退役之后剩下的那件事的正确做法。

            // lvl2
            // 类似地，但通常，lvl2可以直接在lvl1的基础上进行，以此类推。比如lvl2可以比lvl1多一层甚至多两层，也可以在第一层上多一些东西这种，我可以自由发挥
            //
            // 本轮（E14）的验收硬标准是"与现在的效果一致" ⇒ 5 档**暂时内容完全相同**，这里先用循环把
            // "上一级每层各 DeepCopy 一份"这条通用做法写掉；能量阈值于是只起到"把选级链路接通"的作用，画面上看不出差别。
            // 你发挥的时候只改这一处即可，两种粒度都支持：
            //   · 给某一级新增整层：  if (lvl == 2) { ModelLayer ex = new ModelLayer(); ex.AddRing(...); layersByLevel[lvl].Add(ex); }
            //   · 给某一级已有层加东西：layersByLevel[lvl][0].AddModel(...)（DeepCopy 复制的是值，不会回头污染 lvl-1）
            for (int lvl = 1; lvl < energyThreshold.Count; lvl++)
            {
                layersByLevel.Add(new List<ModelLayer>());
                List<ModelLayer> prev = layersByLevel[lvl - 1];
                for (int i = 0; i < prev.Count; i++)
                {
                    layersByLevel[lvl].Add(prev[i].DeepCopy());
                }
                // 对这一档想多出来的东西做操作；可以新增新的层，也可以对新增的层添加模型等
            }

            // lvl3 以此类推
        }

        // 是否还有必要其他的方法用于后续渲染工作
        // 现阶段不必要：渲染要的一切都能由 layersByLevel + 条目自己表达（环的网格/材质/旋钮由 ModelCatalog 按
        // thickness 与 modelId 现取）。真正需要"其他方法"的信号是：将来要支持**第二种巨构**（太阳舟港、戴森框架…），
        // 那时应当是"再建一个 XXXModelBuilder + 一个"这种巨构属于哪一类"的选择点，而不是往这个类里塞分支。
    }
}
