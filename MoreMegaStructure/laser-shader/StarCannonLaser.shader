// StarCannonLaser.shader  (高保真复刻地面激光塔 "Local Laser Continuous")
// 单 Pass 设计，纯加法(ZWrite Off Blend One One)，所有 quad 一次绘制，按 kind 在片元里分流着色。
//   为何不再用双 Pass：双 Pass 需 SetPass(0)+Draw 再 SetPass(1)+Draw；但 Material.SetBuffer 的绑定会被首次 DrawProceduralNow 消费，
//   第二次 Draw 读到脏/失效 buffer -> 外辉/丝/Flare 变成黑块与星图巨白球。改为单 Pass 后只 Draw 一次，彻底规避。
// 【重要】也不要给核心开 ZWrite On 做“深度代理”：本 Mod 在 OnRenderObject 阶段绘制，戴森壳/背景在其后才画，宽核心 quad 写深度会把壳/星球剔成黑洞。
//   核心 quad 很宽且“只要通过 ZTest 就写深度(与亮度无关)”，会把之后绘制的壳/星球按深度剔成黑色太空 -> 出现黑带/扇形黑条/黑球。
// kind: 0=核心光束 1=外辉 2=螺旋能量丝 3=炮口Flare(相机朝向广告牌)。
// 目标引擎：Unity 2018.4.12f1 / StandaloneWindows64 / Direct3D11。
// 绘制：Graphics.DrawProceduralNow(MeshTopology.Quads, segCount*4, 1)；单 pass 只画一遍 buffer。
// 位置在 CPU 端已换算成视图空间(与 SpaceLaserOneShot / 戴森云子弹同一约定)。
// 宽度 = max(世界半宽, 屏幕像素下限 * 距离 / _ProjFactor)。
Shader "MMS/StarCannonLaser"
{
    Properties
    {
        _ProjFactor ("Projection Factor", Float) = 800
        _LaserTex ("Laser Streak (vfx-laser-seq)", 2D) = "white" {}
        _FlareTex ("Muzzle Flare (common-flare-0)", 2D) = "white" {}
        _NoiseTex ("Noise (noise-t1)", 2D) = "white" {}
        _LaserTile ("Laser Tile/Scroll (x:沿长平铺 y:流速)", Vector) = (4, 1.5, 0, 0)
        _NoiseAmp ("Noise 边缘扰动幅度", Float) = 0.15
        _NoiseTile ("Noise Tile (x:沿长 y:时间流速)", Vector) = (3, 0.6, 0, 0)
        _PulseParams ("流动脉冲 x:空间频率(越小越拉长) y:速度 z:锐度(越小越圆滑) w:对比", Vector) = (2, 1.5, 1, 0.6)
        _FlowRand ("流动间隔随机度(0=均匀,越大越随机)", Float) = 0.7
        _HaloEndFade ("外辉首尾渐变长度(占全长比例)", Float) = 0.06
        _ClipFar ("外辉远端深度渐隐-相机远裁剪半径(视图单位)", Float) = 1000000000
        _HaloFadeFrac ("外辉远端深度渐隐起点(占裁剪半径比例)", Float) = 0.4
        _ZTestMode ("深度测试(0=Disable 2=Less 3=Equal 4=LessEqual 5=Greater 7=GreaterEqual 8=Always)", Int) = 8
        // 远平面兜底 eps: >0 时把顶点裁剪深度压到 z <= w*(1-eps)。只用作"超出硬件 far 的部分"的兜底(取极小值 1e-7)。
        // 必须保留: 当相机离载入星系很远(整个星系超出 far)时, 连近端起点自己的视深都 > far => 不兜底就会被远平面切断。
        // 这个量对应多远的视深无关紧要(它只在"实在无处可放"时才接住顶点), 但绝不能拿它当主壁: 主壁 = 上面的球壁。
        _ZCompress ("远平面兜底 eps(0=关闭)", Float) = 0
        // 球形深度壁的"触发球"半径(视图单位, 0=关闭): 球心 = 视图帧原点 = 载入恒星 = 戴森壳心。
        // ★为什么必需是球而不是平面: 壳是球面, 沿离轴角 θ 的壳面视深 = R*cosθ; 固定视深的平面壁会让
        //   屏幕中心(θ小)与边缘(θ大)的遮挡次序恰好反过来(边界为半顶角 acos(wall/R) 的圆锥, 实测 17.9° 对应 mul=0.95)。
        // ★为何在 shader 里做而不是 CPU 改形状: 沿相机射线改顶点位置虽然不改单点投影, 但 ribbon 的 dir/sideV/宽度
        //   是从两端点之差重新算的 -> 长尾多段折到球面同一片区域后 quad 退化(直接塌缩看不见) = 远端被截断(已踩过此坑)。
        //   本 shader 只对"用来投影的坐标"做折叠, 形状/宽度/方向全部用原始坐标算 => 两者兼得。
        // ★★★ [v19 多层壳修正] 一个恒星最多有 10 层戴森壳, 每层自己的 orbitRadius 不同(源码: minOrbitRadius=physicsRadius*1.5,
        //   maxOrbitRadius = defOrbitRadius*2 = dysonRadius*40000*2), 且各层独立旋转。把"一层的壳"当成一个整体球壁是错的:
        //   只取单一半径(旧版用 defOrbitRadius) 时, 比它外的层"打得到壳、打不到壁" => 那圈不折 => 被 far 兜底钉到最远 => 误挡,
        //   屏幕上就是一圈圈"按壳层显露"的边界。现在改成逐视线的规则:
        //   激光落点 = 这条视线上**最近的那个后侧壳面**再往相机方向回退 _SysFoldPull。
        //   ⇒ 每一层的后半球都被激光盖住, 而前半球(比落点近很多)照样能挡住激光 —— 不存在任何环带漏洞。
        // 实现: 以 ρ = 视线到星心(帧原点)的垂距为参, 取参考半径 rt = max(_SysFoldR, ρ), 落点 = -b + sqrt(rt^2-ρ^2) - pull。
        //   ρ < _SysFoldR 时就是球面远交点; ρ ≥ _SysFoldR 时自然退化到"星心在该视线上的最近点", 且 rt^2-ρ^2 永远 ≥ 0(无打空分支)。
        _SysFoldR ("落点参考半径=最内层壳半径(0=关闭)", Float) = 0
        _SysFoldOuterR ("门控半径=最外层壳半径(0=不门控)", Float) = 0
        // 落点沿射线向相机回退量(视图单位, 0=贴参考面): 保证激光严格在该参考面**之前**。按 float32 ulp 留余量(见 C# 的 [MMS zGap])。
        _SysFoldPull ("落点沿射线回退量(0=贴参考面)", Float) = 0
        // 诊断用: >0 时把本顶点最终裁剪深度(device depth, 0=近平面 1=远平面)分档编进颜色:
        // 红=接近 0(上限算坏, 永不遮挡的典型特征), 蓝=中等(真实深度), 绿=接近远端(已折到球壁), 紫=被 far 兜底钉住。测试完请关回 0。
        _ZDebug ("深度自检着色(0=关)", Float) = 0
        _MMSVersion ("Version(勿改, 仅供日志校验; 19=逐视线落点:最内层参考+最外层门控)", Float) = 19
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    struct LaserSeg
    {
        float3 beginPos;   float intensity;
        float3 endPos;     float halfWidth;
        float4 color;
        float  minPx;      float capStart;   float capEnd;   float kind;
    };
    StructuredBuffer<LaserSeg> _LaserBuffer;
    float _ProjFactor;
    sampler2D _LaserTex;
    sampler2D _FlareTex;
    sampler2D _NoiseTex;
    float4 _LaserTile;
    float4 _NoiseTile;
    float  _NoiseAmp;
    float4 _PulseParams;
    float  _FlowRand;
    float  _HaloEndFade;   // 外辉首尾渐变长度(占全长比例)，由 C# SetFloat 覆盖
    float  _ClipFar;       // 相机远裁剪半径(视图单位)；外辉按到相机距离接近它时淡出
    float  _HaloFadeFrac;  // 从该比例的裁剪半径处开始深度渐隐
    int    _ZTestMode;     // 深度测试模式(渲染状态用, 见 Properties)：遮挡诊断期间由 C# SetInt 覆盖
    float  _ZCompress;     // 远平面兜底 eps; 0=关闭。由 C# 仅在"宇宙现实视角"下发非零, 其它视角维持旧行为
    float  _SysFoldR;      // 落点参考半径 = 最内层壳半径(视图单位); 0=关闭
    float  _SysFoldOuterR; // 门控半径 = 最外层壳半径; 视线垂距超过它就完全不折(那里没有任何壳); 0=不门控
    float  _SysFoldPull;   // 落点沿射线向相机回退量(视图单位); 0=贴在参考面上
    float  _ZDebug;        // 深度自检着色开关
    float  _MMSVersion;   // 版本指纹，不参与渲染，仅供 C# 日志校验 bundle 里是不是最新 shader

    // 逐视线的球形深度壁: 把顶点沿【相机->该点】射线折到"该视线上最近的后侧壳面再往相机回退 _SysFoldPull"的位置,
    // 然后走**同一个 UNITY_MATRIX_VP** 投影。星心 = 帧原点, 所以一个 ρ 就完全描述了这条视线。
    // 只折"用来投影的坐标": 图元的形状/宽度/方向仍然完全由原始 beginPos/endPos 算(那些在 LineVert 里已经算完),
    // 而沿相机射线折叠本身不改变屏幕投影位置 => 既不塌缩(不截断)也不变形。
    // ★关键: 绝不能自己拿解析式 d = f(z-n)/(z(f-n)) 算上限再写回 v.z!
    //   壁与壳的半径差只占几个百分点, 换算到 device depth 只有 ~3e-7, 而 float32 在 1.0 附近的 ulp = 1.19e-7,
    //   解析式里 f*(z-n) 与 z*(f-n) 又是 5.9e9 量级(ulp=512, 相对误差 ~8.6e-8) => 舍入误差与信号同量级且系统性偏小,
    //   结果就是激光稳定地落在壳前 => "完全不遮挡 + 也永远不会被 far 切断"(用户实测到的两条现象同根)。
    //   投影走同一个矩阵 => 深度与游戏自己画的戴森壳逐位同构, 只差真实的那几个百分点。
    // dev: 输出本顶点最终的 device depth(0..1), 仅在 _ZDebug>0 时被片元拿去编色。
    //   ★用 out 参数而不是裸全局变量: HLSL 里未加语义的全局会被归进 $Globals 常量缓冲, 在函数内写它
    //     在部分 fxc 版本上直接编译失败 => Unity 打包时报错、bundle 里只剩旧 shader(排查代价极大)。
    float4 BandZ(float3 wp, out float dev)
    {
        float3 zc = wp;
        if (_SysFoldR > 0.0)
        {
            float3 cam = _WorldSpaceCameraPos;          // 与 wp 同坐标系(视图帧), 已由 D=distance(camPos,p) 的宽度/渐隐行为验证
            float3 dv = wp - cam;
            float  dist = length(dv);
            if (dist > 1e-3)
            {
                float3 dir = dv / dist;
                float  cc  = dot(cam, cam);
                float  b   = dot(cam, dir);                    // 星心到射线的有符号投影(相机前方分量)
                float  rho2 = max(cc - b * b, 0.0);            // 视线到星心的垂距^2
                // 门控: 视线连最外层壳都擦不到 => 那条视线上没有任何后侧壳面需要绕开 => 保留真实深度(交给 far 兜底)
                if (_SysFoldOuterR <= 0.0 || rho2 < _SysFoldOuterR * _SysFoldOuterR)
                {
                    // rt = max(最内层半径, ρ): ρ 小于最内层时落点就在最内层球面上; ρ 更大时落点自然退化成"星心最近点"
                    // 此时 rt^2 - ρ^2 恒 >= 0, 所以绝不会出现"打空"分支 => 没有任何环带漏洞
                    float  rt  = max(_SysFoldR, sqrt(rho2));
                    float  tLand = -b + sqrt(max(rt * rt - rho2, 0.0)) - _SysFoldPull;
                    if (tLand > 0.0 && tLand < dist) zc = cam + dir * tLand;   // 只有比落点更远的顶点才折
                }
            }
        }
        float4 v = mul(UNITY_MATRIX_VP, float4(zc, 1.0));
        // 兜底: 折完仍然在硬件远平面之外(相机离星系太远 / 视线打不到球)时, 把 z 钉在 far 内侧, 否则会被远平面裁掉。
        if (_ZCompress > 0.0 && v.w > 0.0)
        {
            #if defined(UNITY_REVERSED_Z)
                v.z = max(v.z, v.w * _ZCompress);          // reversed-Z: 远平面在 0 侧
            #else
                v.z = min(v.z, v.w * (1.0 - _ZCompress));  // 非反转(本作的 GL 兼容路径, 深度重映射到[0,1]): 远平面在 1 侧
            #endif
        }
        dev = (v.w > 0.0) ? (v.z / v.w) : 0.0;
        return v;
    }

    struct appdata { uint id : SV_VertexID; };

    struct v2f
    {
        float4 vertex : SV_POSITION;
        float  sideUV : TEXCOORD0;
        float  along  : TEXCOORD1;
        fixed4 col    : TEXCOORD2;
        float2 cap    : TEXCOORD3;
        float2 uv     : TEXCOORD4;
        float  kind   : TEXCOORD5;
        float  dev    : TEXCOORD6;
    };

    // 把该 quad 收成零面积(不可见)：四个顶点塌到同一点
    v2f Collapse(LaserSeg s)
    {
        v2f o;
        float dev;
        o.vertex = BandZ(s.beginPos, dev);
        o.sideUV = 0; o.along = 0; o.uv = 0; o.cap = 0;
        o.col = fixed4(0,0,0,0); o.kind = s.kind; o.dev = dev;
        return o;
    }

    // 线状几何(核心/外辉/丝)的通用顶点（对退化线段与“光束近乎平行于视线”做了 NaN 防护）
    v2f LineVert(LaserSeg s, uint corner)
    {
        v2f o;
        float along = (corner == 2 || corner == 3) ? 1.0 : 0.0;
        float side  = (corner == 1 || corner == 2) ? 1.0 : -1.0;

        float3 dvec = s.endPos - s.beginPos;
        float  dl   = length(dvec);
        if (dl < 1e-3)   // 退化线段：塌成零面积不可见，避免 normalize 产生 NaN 撑成巨大垃圾四边形
        {
            float devDeg;
            o.vertex = BandZ(s.beginPos, devDeg);
            o.sideUV = 0; o.along = 0; o.uv = 0; o.cap = 0;
            o.col = fixed4(0,0,0,0); o.kind = s.kind; o.dev = devDeg;
            return o;
        }
        float3 dir    = dvec / dl;
        float3 camPos = _WorldSpaceCameraPos;
        float3 p      = lerp(s.beginPos, s.endPos, along);

        // sideV 用整段中点算一次、两端共用：否则 sideV 沿单条超长 quad 旋转/翻折(bowtie 自交四边形)，
        // 会让亮芯从光晕中心随距离越偏越远、并在远端发淡——表现为“一条线+单侧片状光晕”。
        float3 mid  = (s.beginPos + s.endPos) * 0.5;
        float3 cr   = cross(dir, camPos - mid);
        float  crl  = length(cr);
        float3 sideV;
        if (crl > 1e-3)
            sideV = cr / crl;
        else
        {
            float3 tmp = (abs(dir.y) < 0.9) ? float3(0, 1, 0) : float3(1, 0, 0);
            sideV = normalize(cross(dir, tmp));
        }

        float D      = distance(camPos, p);
        // 像素下限(minPx*D/proj)让光束在屏幕上保持恒定 ~minPx 宽——这对"核心"和"星图(世界宽=0)"是必要的(远处可见)。
        // 但现实视角的外辉本身有世界宽度, 套用像素下限会在远处把光晕强行撑成一根 ~minPx 的厚条, 一直粗到灭点再被 quad 末端截断
        // = 你看到的"整齐切口"。所以: 现实视角的外辉(kind==1 且有世界宽度)只用世界宽度, 随透视自然收敛到灭点, 不再被切断。
        float floorW = s.minPx * D / max(_ProjFactor, 1e-3);
        float halfW  = (s.kind > 0.5 && s.kind < 1.5 && s.halfWidth > 1e-3)
                     ? s.halfWidth
                     : max(s.halfWidth, floorW);
        // 外辉(kind==1)在整条光束真正的首尾两端把"宽度"也收敛到尖端：宽光晕(尤其现实视角世界宽度)抵达星球处的
        // 整齐切口本质是矩形平直末端, 仅靠亮度淡不够 -> 这里让几何宽度也收成点。用全局沿长 ga 判定, 细分主光束只有首尾段受影响。
        if (s.kind > 0.5 && s.kind < 1.5)
        {
            float gae = lerp(s.capStart, s.capEnd, along);
            float ef  = max(_HaloEndFade, 1e-3);
            halfW *= smoothstep(0.0, ef, gae) * smoothstep(0.0, ef, 1.0 - gae);
        }
        float3 pos   = p + sideV * (side * halfW);
        o.vertex  = BandZ(pos, o.dev);
        o.sideUV  = side;
        o.along   = along;
        o.uv      = float2(along, side * 0.5 + 0.5);
        // 外辉按"相机深度"在远端渐隐：现实视角(透视)光束一直延伸到远裁剪面，末端被 GPU/钳制造成硬切——
        // 这里让亮度随到相机距离接近裁剪半径而淡出，末端在抵达裁剪面之前就化入远处。
        // 星图/编辑器是均匀缩放视图, 视图坐标很小 -> D 远小于 _ClipFar -> dfade≈1, 此淡出不生效, 仍由上面的长度渐变负责首尾。
        float dfade = 1.0;
        if (s.kind > 0.5 && s.kind < 1.5)
            dfade = saturate((_ClipFar - D) / max(_ClipFar * (1.0 - _HaloFadeFrac), 1e-2));
        o.col     = (fixed4)(s.color * s.intensity * dfade);
        o.cap     = float2(s.capStart, s.capEnd);
        o.kind    = s.kind;
        return o;
    }

    // 炮口 Flare 顶点(相机朝向广告牌)
    v2f FlareVert(LaserSeg s, uint corner)
    {
        v2f o;
        float sx = (corner == 0 || corner == 3) ? -1.0 : 1.0;
        float sy = (corner == 0 || corner == 1) ? -1.0 : 1.0;
        float3 right = float3(UNITY_MATRIX_V[0][0], UNITY_MATRIX_V[1][0], UNITY_MATRIX_V[2][0]);
        float3 up    = float3(UNITY_MATRIX_V[0][1], UNITY_MATRIX_V[1][1], UNITY_MATRIX_V[2][1]);
        float3 pos   = s.beginPos + (right * sx + up * sy) * s.halfWidth;
        o.vertex = BandZ(pos, o.dev);
        o.sideUV = sx; o.along = sy;
        o.uv = float2(sx * 0.5 + 0.5, sy * 0.5 + 0.5);
        o.col  = (fixed4)(s.color * s.intensity);
        o.cap  = float2(0, 0); o.kind = s.kind;
        return o;
    }

    fixed4 FragCore(v2f i)
    {
        float t = _Time.y;
        float ga = lerp(i.cap.x, i.cap.y, i.along);   // 全局沿长(0..1)：细分后各段共用同一坐标，流动不逐段重复
        // 噪声只沿光束长度变化、横向取固定 y：否则两侧不同 -> 不对称
        float nz = (tex2D(_NoiseTex, float2(ga * _NoiseTile.x - t * _NoiseTile.y, 0.5)).r - 0.5) * _NoiseAmp;
        float jag = sin(ga * 28.0 + t * 12.0) * 0.07 + sin(ga * 60.0 - t * 18.0) * 0.03;
        float edge = saturate(abs(i.sideUV - jag - nz));
        float glow = pow(1.0 - edge, 1.6);
        float filament = pow(1.0 - edge, 8.0);
        // 主体亮度只靠 glow+filament；流动感交给下方低频 pulse（不再用 _LaserTex，避免一颗颗小球）
        float profile = glow + filament;

        float phase = ga * _PulseParams.x - t * _PulseParams.y;
        phase += (tex2D(_NoiseTex, float2(ga * 0.7 - t * 0.15, 0.33)).r - 0.5) * _FlowRand;
        float pulse = pow(0.5 + 0.5 * sin(phase * 6.2831853), _PulseParams.z);
        float amt = profile * (1.0 - _PulseParams.w * 0.5 + _PulseParams.w * pulse);

        // [深度自检] _ZDebug>0 时把 device depth 分档编色(加法混合下直接看颜色就能判深度落在哪个区间):
        //   红 = d < 0.02 相当于贴近平面 => 它永远不会被任何东西遮住(上限算坏的故障特征)
        //   蓝 = 0.02..0.98 中等深度(真实几何就在此区间, 会被前方的壳/星体遮)
        //   绿 = d >= 0.98 靠近远平面但未被钉(典型: 已折到球壁)
        //   紫 = d >= 1-eps 被远平面兜底钉住(整个系统超出 far)
        if (_ZDebug > 0.5)
        {
            float dd = saturate(i.dev);
            float3 tint = float3(1, 0, 0);
            tint = lerp(tint, float3(0, 0, 1), smoothstep(0.02, 0.06, dd));
            tint = lerp(tint, float3(0, 1, 0), smoothstep(0.98, 0.985, dd));
            tint = lerp(tint, float3(0.7f, 0, 1), saturate((dd - (1.0 - _ZCompress)) * 1e7));
            amt *= 2.2;
            return fixed4(tint * amt, 1);
        }

        return i.col * amt;   // 末端不再按百分比渐隐(起点可藏在恒星里)
    }

    fixed4 FragGlow(v2f i)
    {
        float t = _Time.y;
        fixed4 result = fixed4(0, 0, 0, 0);
        if (i.kind > 2.5)   // 炮口 Flare
        {
            fixed4 f = tex2D(_FlareTex, i.uv);
            result = i.col * f.a;
        }
        else if (i.kind < 1.5)   // 外辉：纯平滑光晕，无锐利芯丝(消除星图并排双线)
        {
            float ga = lerp(i.cap.x, i.cap.y, i.along);   // 全局沿长：细分后流动不逐段重复
            float edge = saturate(abs(i.sideUV));   // 必须 saturate：否则 pow(负,2.2)=NaN -> 黑边
            float glow = pow(1.0 - edge, 2.2);
            float phase = ga * _PulseParams.x - t * _PulseParams.y;
            phase += (tex2D(_NoiseTex, float2(ga * 0.7 - t * 0.15, 0.33)).r - 0.5) * _FlowRand;
            float pulse = pow(0.5 + 0.5 * sin(phase * 6.2831853), _PulseParams.z);
            float amt = glow * (1.0 - _PulseParams.w * 0.6 + _PulseParams.w * 1.2 * pulse);
            // 仅对"外辉"在整条光束真正的首尾两端做渐变收敛(ga≈0 / ga≈1)，消除抵达目标恒星处的整齐切口。
            // 用全局沿长 ga 判定：主光束细分后只有最首/最尾几段受影响，中段不渐隐；核心光束(FragCore)完全不受影响。
            float ef = max(_HaloEndFade, 1e-3);
            float endFade = smoothstep(0.0, ef, ga) * smoothstep(0.0, ef, 1.0 - ga);
            amt *= endFade;
            result = i.col * amt;   // 首尾平滑收敛，中段与核心维持无渐变
        }
        else   // 螺旋丝(kind==2)：细丝 + 电弧条纹
        {
            float nz = (tex2D(_NoiseTex, float2(i.uv.x * _NoiseTile.x - t * _NoiseTile.y, i.uv.y)).r - 0.5) * _NoiseAmp;
            float jag = sin(i.along * 55.0 + t * 26.0) * 0.10 + sin(i.along * 133.0 - t * 41.0) * 0.04;
            float edge2 = saturate(abs(i.sideUV - jag - nz));
            float profile = pow(1.0 - edge2, 1.4) + pow(1.0 - edge2, 12.0) * 1.5;
            float4 lt = tex2D(_LaserTex, float2(i.uv.x * _LaserTile.x - t * _LaserTile.y, i.uv.y));
            profile *= (0.5 + 1.0 * lt.a);
            float amt = profile * (0.7 + 0.6 * sin(i.along * 20.0 + t * 22.0));
            amt *= lerp(1.0, saturate(i.along * 24.0), i.cap.x) * lerp(1.0, saturate((1.0 - i.along) * 24.0), i.cap.y);
            result = i.col * amt;
        }
        // [深度自检] 与 FragCore 同一套分档编色(只对外辉/丝生效, Flare 保持原样便于对比)
        if (_ZDebug > 0.5 && i.kind < 2.5)
        {
            float dd = saturate(i.dev);
            float3 tint = float3(1, 0, 0);
            tint = lerp(tint, float3(0, 0, 1), smoothstep(0.02, 0.06, dd));
            tint = lerp(tint, float3(0, 1, 0), smoothstep(0.98, 0.985, dd));
            tint = lerp(tint, float3(0.7f, 0, 1), saturate((dd - (1.0 - _ZCompress)) * 1e7));
            float lum = max(max(result.r, result.g), result.b);
            result = fixed4(tint * lum * 2.2, 1);
        }
        return result;
    }
    ENDCG

    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        // 单 Pass：所有 quad 一次绘制，按 kind 在片元里分流着色(0核心 / 1外辉 / 2螺旋丝 / 3炮口Flare)。
        // 不再用双 Pass：双 Pass 的第二次 SetPass(1)+DrawProceduralNow 会因 SetBuffer 绑定被首次 Draw 消费而读到脏 buffer -> 黑块/巨球。
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off
            ZTest [_ZTestMode]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            v2f vert(appdata v)
            {
                uint seg = v.id >> 2; uint corner = v.id & 3;
                LaserSeg s = _LaserBuffer[seg];
                if (s.kind > 2.5) return FlareVert(s, corner);  // 炮口Flare：相机朝向广告牌
                return LineVert(s, corner);                     // 核心/外辉/螺旋丝：线状 ribbon
            }
            fixed4 frag(v2f i) : SV_Target
            {
                if (i.kind < 0.5) return FragCore(i);   // 核心
                return FragGlow(i);                      // 外辉/螺旋丝/Flare(FragGlow 内部再按 kind 细分)
            }
            ENDCG
        }
    }
    Fallback Off
}
