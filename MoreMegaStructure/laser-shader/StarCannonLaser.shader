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
        _MMSVersion ("Version(勿改, 仅供日志校验; 5=外辉saturate修复)", Float) = 5
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

        // 垂直于“光束方向 且 朝向相机”的方向；当光束几乎正对/背对相机时 cross 会接近 0，需回退
        float3 cr   = cross(dir, camPos - p);
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
        float halfW  = max(s.halfWidth, s.minPx * D / max(_ProjFactor, 1e-3));
        float3 pos   = p + sideV * (side * halfW);
        o.vertex  = mul(UNITY_MATRIX_VP, float4(pos, 1.0));
        o.sideUV  = side;
        o.along   = along;
        o.uv      = float2(along, side * 0.5 + 0.5);
        o.col     = (fixed4)(s.color * s.intensity);
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
        float nz = (tex2D(_NoiseTex, float2(i.uv.x * _NoiseTile.x - t * _NoiseTile.y, i.uv.y)).r - 0.5) * _NoiseAmp;
        // 更低频率与速度：粗细/抖动变化更慢
        float jag = sin(i.along * 28.0 + t * 12.0) * 0.07 + sin(i.along * 60.0 - t * 18.0) * 0.03;
        float edge = saturate(abs(i.sideUV - jag - nz));
        float glow = pow(1.0 - edge, 1.6);
        float filament = pow(1.0 - edge, 8.0);
        float4 lt = tex2D(_LaserTex, float2(i.uv.x * _LaserTile.x - t * _LaserTile.y, i.uv.y));
        // 降低贴图滞动带来的“一颗颗球”：把 _LaserTex 只当很淡的丝光叠加，主体亮度靠 glow+filament
        float profile = glow * (0.8 + 0.4 * lt.a) + filament * 1.0;

        // 沿整条光束的低频流动脉冲：低对比+圆滑 => 不再是离散的球，而是整条在向前流动
        float phase = i.along * _PulseParams.x - t * _PulseParams.y;
        phase += (tex2D(_NoiseTex, float2(i.along * 0.7 - t * 0.15, 0.33)).r - 0.5) * _FlowRand; // 间隔随机化
        float pulse = pow(0.5 + 0.5 * sin(phase * 6.2831853), _PulseParams.z);
        // 压低脉冲对比(原 1-w+2w*pulse)，避免亮度随时间形成离散的移动亮球；保留轻微流动
        float amt = profile * (1.0 - _PulseParams.w * 0.5 + _PulseParams.w * pulse);

        amt *= lerp(1.0, saturate(i.along * 24.0), i.cap.x) * lerp(1.0, saturate((1.0 - i.along) * 24.0), i.cap.y);
        return i.col * amt;
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
            float edge = saturate(abs(i.sideUV));   // 必须 saturate：否则外辉外缘 edge 略超过1 -> (1-edge)<0 -> pow(负,2.2)=NaN -> 加法下显为黑边且随镜头闪烁
            float glow = pow(1.0 - edge, 2.2);
            float phase = i.along * _PulseParams.x - t * _PulseParams.y;
            phase += (tex2D(_NoiseTex, float2(i.along * 0.7 - t * 0.15, 0.33)).r - 0.5) * _FlowRand;
            float pulse = pow(0.5 + 0.5 * sin(phase * 6.2831853), _PulseParams.z);
            float amt = glow * (1.0 - _PulseParams.w * 0.6 + _PulseParams.w * 1.2 * pulse);
            amt *= lerp(1.0, saturate(i.along * 24.0), i.cap.x) * lerp(1.0, saturate((1.0 - i.along) * 24.0), i.cap.y);
            result = i.col * amt;
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
            ZTest LEqual
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
