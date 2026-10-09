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
        _MMSVersion ("Version(勿改, 仅供日志校验; 14=暴露_ZTestMode供遮挡诊断)", Float) = 14
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
    float  _MMSVersion;   // 版本指纹，不参与渲染，仅供 C# 日志校验 bundle 里是不是最新 shader

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
    };

    // 把该 quad 收成零面积(不可见)：四个顶点塌到同一点
    v2f Collapse(LaserSeg s)
    {
        v2f o;
        o.vertex = mul(UNITY_MATRIX_VP, float4(s.beginPos, 1.0));
        o.sideUV = 0; o.along = 0; o.uv = 0; o.cap = 0;
        o.col = fixed4(0,0,0,0); o.kind = s.kind;
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
            o.vertex = mul(UNITY_MATRIX_VP, float4(s.beginPos, 1.0));
            o.sideUV = 0; o.along = 0; o.uv = 0; o.cap = 0;
            o.col = fixed4(0,0,0,0); o.kind = s.kind;
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
        o.vertex  = mul(UNITY_MATRIX_VP, float4(pos, 1.0));
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
        o.vertex = mul(UNITY_MATRIX_VP, float4(pos, 1.0));
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
