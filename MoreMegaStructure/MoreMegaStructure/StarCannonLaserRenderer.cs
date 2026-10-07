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
        private static ComputeBuffer buffer;
        private static int capacity;
        private static Material mat;
        private static bool matFailed;

        // 主路径(shader)
        private static readonly List<LaserSeg> segs = new List<LaserSeg>(256);
        private static ComputeBuffer segBuffer;
        private static int segCapacity;
        private static Material laserMat;
        private static bool laserMatFailed;

        private static StarCannonLaserHost host;

        /// <summary>登记 / 刷新一条光束。用相同的 key 每帧重复调用可维持其存活；停止调用后到期自动消失。</summary>
        public static void AddBeam(int key, VectorLF3 begin, VectorLF3 end, Color? color = null,
            int bulletCount = 0, float muzzleRadius = -1f, float impactRadius = -1f, float holdSeconds = 0.35f,
            float intensity = 1f, int strandCount = 0, float strandTurns = 3.5f, float strandSpeed = 1.4f)
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
                BuildRibbonSegs(gd, viewStar, cur);
                UploadAndDrawRibbon();
                return;
            }

            // 回退路径：sailBullet 粒子（bundle 未重打时保证激光不消失）
            if (!TryInitMaterial()) return;
            BuildBulletPool(gd, viewStar);
            UploadAndDraw();
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
            // 缓存当前相机投影参数，供 UploadAndDrawRibbon 计算屏幕像素下限 -> 世界宽度
            cur_Ortho = cur.orthographic;
            cur_PixelHeight = cur.pixelHeight;
            cur_Fov = cur.fieldOfView;
            VectorLF3 relativePos = gd.relativePos;
            Quaternion relativeRot = gd.relativeRot;
            VectorLF3 viewTarget = UIStarmap.viewTargetStatic;
            VectorLF3 starPos = viewStar.uPosition;

            // 远裁剪面钳制半径：把视图空间端点压到相机远裁剪面以内，避免长光束中段被 GPU 裁掉(“屏幕中间有块板子挡住”)
            float farR = cur.farClipPlane * 0.5f;
            bool doClamp = farR > 1f;

            foreach (var kv in beams)
            {
                BeamProfile p = kv.Value;
                Vector3 vb, ve;
                if (!TryViewPoints(p.begin, p.end, relativePos, relativeRot, viewTarget, starPos, out vb, out ve))
                    continue;
                if (doClamp)
                {
                    vb = ClampMag(vb, farR);
                    ve = ClampMag(ve, farR);
                }

                // 世界半宽只在主视图给(近处更粗)；星图/编辑器下坐标被 *0.00025 缩放，世界宽会给 0，纯用屏幕像素宽避免变成巨大宽面
                bool universe = DysonSphere.renderPlace == ERenderPlace.Universe;

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

                float inten = p.intensity * atten * (isBlob ? subIntensityScale : 1f);
                if (inten <= 0f) continue;
                float hw = universe ? Mathf.Max(1f, p.muzzleRadius) * atten : 0f;
                float pxScale = Mathf.Max(widthScale, 0.3f) * (isBlob ? subWidthScale : 1f);
                float coreMinPx = Mathf.Max(defaultMinPixelsCore * pxScale * atten, 1.2f);
                Vector4 c = p.color;

                // 主光束跨星系超长：细分成多段 ribbon，每段用各自中点算 sideV，避免单条超长 quad 把亮芯挤到一侧
                int n = isMain ? Mathf.Max(1, ribbonSubdiv) : 1;
                // 核心光柱（子/侧光束更细更暗，远处再衰减，避免大量重叠糊成白球）
                if ((debugKinds & 1) != 0)
                    AddRibbon(vb, ve, inten, hw, c, coreMinPx, 0f, n);
                // 外辉——仅主视图；主光束与预热光束都给外辉(预热要看到聚能发光)，目标侧子光束默认不给(subNoHalo)以消除重叠白球
                if (universe && (debugKinds & 2) != 0 && (isMain || isFocus || !subNoHalo))
                    AddRibbon(vb, ve, inten * haloIntensity, hw * haloWidthScale, c, coreMinPx * haloWidthScale, 1f, n);

                // 起点环绕的螺旋能量丝 + 炮口 Flare（仅主视图、主光束）
                if (universe && p.strandCount > 0)
                {
                    if ((debugKinds & 4) != 0)
                        GenStrands(p, relativePos, relativeRot, viewTarget, starPos, farR, doClamp, inten, c);
                    if ((debugKinds & 8) != 0)
                    segs.Add(new LaserSeg
                    {
                        beginPos = vb,
                        intensity = inten * flareIntensity,
                        endPos = vb,
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

        // 把一条 vb->ve 的光束细分成 n 段 ribbon quad；capStart/capEnd 复用为“全局沿长”(0..1)，供 shader 连续流动
        private static void AddRibbon(Vector3 vb, Vector3 ve, float inten, float hw, Vector4 c, float minPx, float kind, int n)
        {
            if (n < 1) n = 1;
            for (int i = 0; i < n; i++)
            {
                float t0 = (float)i / n;
                float t1 = (float)(i + 1) / n;
                segs.Add(new LaserSeg
                {
                    beginPos = Vector3.Lerp(vb, ve, t0),
                    endPos = Vector3.Lerp(vb, ve, t1),
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

        // 生成起点附近环绕主光束旋转/收敛的螺旋能量丝
        private static void GenStrands(in BeamProfile p, VectorLF3 relativePos, Quaternion relativeRot,
            VectorLF3 viewTarget, VectorLF3 starPos, float farR, bool doClamp, float inten, Vector4 c)
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
                    if (doClamp) { a = ClampMag(a, farR); b = ClampMag(b, farR); }

                    float flow = 0.5f + 0.5f * Mathf.Sin(u0 * p.strandTurns * twoPi - time * p.strandSpeed * 2f);
                    float si = inten * (0.35f + 0.65f * flow) * (1f - u0 * 0.6f);
                    if (si <= 0f) continue;

                    segs.Add(new LaserSeg
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

        private static void UploadAndDrawRibbon()
        {
            int count = segs.Count;
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
            segs.CopyTo(0, arr, 0, count);
            segBuffer.SetData(arr);

            // 屏幕像素下限换算系数：像素 -> 世界尺寸 = px * 距离 / _ProjFactor
            float projFactor;
            if (cur_Ortho) projFactor = 1e6f;
            else projFactor = cur_PixelHeight * 0.5f / Mathf.Max(1e-4f, Mathf.Tan(cur_Fov * 0.5f * Mathf.Deg2Rad));
            if (projFactor < 1f) projFactor = 1f;

            laserMat.SetBuffer("_LaserBuffer", segBuffer);
            laserMat.SetFloat("_ProjFactor", projFactor);
            // 单 Pass：一次 SetPass(0)+Draw 画完所有 kind(核心/外辉/丝/Flare)，片元按 kind 分流。
            // 不再做第二次 SetPass(1)+Draw：那会因 SetBuffer 绑定被首次 Draw 消费而读到脏 buffer，导致黑块/巨白球。
            laserMat.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Quads, count * VERTS_PER_SEG, 1);
        }

        // 缓存当前相机投影参数（在 BuildRibbonSegs 时设置，供 UploadAndDrawRibbon 使用）
        private static bool cur_Ortho;
        private static float cur_PixelHeight = 1080f;
        private static float cur_Fov = 60f;

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
            SailBullet b = new SailBullet();
            b.id = pool.Count + 1;
            b.maxt = 0.3f;
            b.t = head * 0.3f;               // 与 maxt 配合，驱动 shader 的流动/明暗
            b.state = 0;                     // 0 => 永不生成真正的太阳帆（关键：不污染戴森云）
            b.uBegin = p.begin + offB;
            b.uEnd = p.end + offE;
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

        private static void UploadAndDraw()
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

            mat.SetColor("_Color0", DefaultColor);
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
