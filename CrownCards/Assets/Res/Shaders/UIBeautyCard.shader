Shader "UIBeautyCard"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Card Quality Settings)]
        [KeywordEnum(Common, Rare, Epic, Legendary)] _Quality ("Card Quality", Float) = 1
        
        [Header(3D Card Shape)]
        _CardRadius ("Corner Radius", Range(0.01, 0.4)) = 0.08
        _BorderWidth ("Border Width", Range(0.01, 0.2)) = 0.08
        _BevelDepth ("3D Bevel Intensity", Range(0.0, 5.0)) = 3.0

        [Header(Traveling Light Settings)]
        _GlowFrequency ("Glow Orbit Speed / Freq", Range(0.1, 5.0)) = 1.0
        _GlowIntensity ("Light Point Brightness", Range(0.0, 5.0)) = 2.5
        _PointSize ("Light Point Size / Trail", Range(0.02, 0.25)) = 0.08

        // UGUI 必需属性
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _QUALITY_COMMON _QUALITY_RARE _QUALITY_EPIC _QUALITY_LEGENDARY
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float _CardRadius;
            float _BorderWidth;
            float _BevelDepth;
            float _GlowFrequency;
            float _GlowIntensity;
            float _PointSize;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.uv = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            // 圆角矩形 SDF
            float sdRoundedBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 p = IN.uv - 0.5;
                float2 cardSize = float2(0.5, 0.5);

                // 1. 卡牌圆角外轮廓与抗锯齿
                float distOuter = sdRoundedBox(p, cardSize, _CardRadius);
                float aa = fwidth(distOuter);
                float cardMask = 1.0 - smoothstep(-aa, aa, distOuter);
                if (cardMask <= 0.0) discard;

                // 2. 边框划分与 3D 立体倒角法线
                float borderFactor = smoothstep(0.0, -_BorderWidth, distOuter); // 0 为边框边缘，1 为内部卡板
                float edgeBevel = sin(borderFactor * 3.1415926);                // 边框拱形凸起
                float2 normalXY = -normalize(p + 1e-5) * edgeBevel;
                float3 normal = normalize(float3(normalXY * _BevelDepth, 1.0));

                // 3. 基础自然对角渐变 (自左上向右下平滑过渡，纯净无噪点)
                float gradFactor = saturate((1.0 - IN.uv.x) * 0.45 + IN.uv.y * 0.55);

                // 4. 四种品质的平滑色彩配置 (浅色 colA -> 深色 colB)
                float3 colA, colB, innerA, innerB;

                #if defined(_QUALITY_COMMON) // 白 (0) - 纯净冷银白渐变到深银灰
                    colA   = float3(0.96, 0.97, 1.00); 
                    colB   = float3(0.40, 0.43, 0.50); 
                    innerA = float3(0.13, 0.14, 0.17); 
                    innerB = float3(0.05, 0.06, 0.07);

                #elif defined(_QUALITY_RARE) // 蓝 (1) - 浅亮天蓝平滑渐变到深邃宝石蓝
                    colA   = float3(0.20, 0.85, 1.00); 
                    colB   = float3(0.02, 0.16, 0.55); 
                    innerA = float3(0.04, 0.12, 0.26); 
                    innerB = float3(0.01, 0.03, 0.08);

                #elif defined(_QUALITY_EPIC) // 紫 (2) - 明艳淡紫粉平滑渐变到高贵夜空深紫
                    colA   = float3(0.95, 0.45, 0.98); 
                    colB   = float3(0.30, 0.03, 0.52); 
                    innerA = float3(0.17, 0.04, 0.24);
                    innerB = float3(0.04, 0.01, 0.07);

                #else // 传说 (3) - 耀眼香槟金平滑渐变到沉稳浓郁赤金
                    colA   = float3(1.00, 0.92, 0.55); 
                    colB   = float3(0.70, 0.35, 0.04); 
                    innerA = float3(0.20, 0.12, 0.03);
                    innerB = float3(0.06, 0.03, 0.01);
                #endif

                // 纯净渐变混合
                float3 frameColor = lerp(colB, colA, gradFactor);
                float3 plateColor = lerp(innerB, innerA, gradFactor);

                // 固定弱主光（确保没有发光点经过时，卡牌依然有立体的底盘质感）
                float3 fixedLight = normalize(float3(0.3, 0.5, 1.0));
                float baseDiffuse = max(0.0, dot(normal, fixedLight)) * 0.3 + 0.7;
                frameColor *= baseDiffuse;

                // ================= 5. 游动发光点核心逻辑 =================
                // 时间与游动速度/频率绑定
                float timeAngle = _Time.y * _GlowFrequency * 2.0;

                // A. 3D 旋转游动光源产生的聚焦高光点 (沿边框倒角旋转追逐)
                float3 orbitLight = normalize(float3(cos(timeAngle) * 0.8, sin(timeAngle) * 0.8, 0.65));
                float3 halfDir = normalize(orbitLight + float3(0, 0, 1));
                float movingSpec = pow(max(0.0, dot(normal, halfDir)), 28.0) * 2.2;

                // B. 沿卡牌边缘环形游走的能量光斑与拖尾 (沿四周转动)
                float currentAngle = atan2(p.y, p.x); // 当前像素在卡牌中的极坐标角度 [-PI, PI]
                float angleNorm = currentAngle / 6.2831853; // [-0.5, 0.5]
                float orbitProgress = frac(_Time.y * _GlowFrequency * 0.35); // 游动周期进度
                
                // 计算当前点到游动光点中心的角距离（带环状包裹）
                float angleDist = abs(frac(angleNorm - orbitProgress + 0.5) - 0.5);
                
                // 光点聚焦形态 (高斯分布，仅聚集在边框隆起处)
                float spotMask = exp(-pow(angleDist / _PointSize, 2.0)) * edgeBevel;
                float spotGlow = exp(-pow(angleDist / (_PointSize * 2.5), 2.0)) * edgeBevel * 0.4;
                float travelingPoint = (spotMask + spotGlow);

                // 综合游动光斑与 3D 镜面反光
                float totalGlow = (movingSpec + travelingPoint * 1.5) * _GlowIntensity;

                // 边框与内板混合
                float3 finalColor = lerp(frameColor, plateColor, borderFactor);

                // 将游动发光点叠加到卡牌上 (偏亮色 + 泛白芯)
                float3 pointColor = lerp(colA, float3(1.0, 1.0, 1.0), 0.5);
                finalColor += pointColor * totalGlow;

                // 叠加 UGUI 顶点色
                finalColor *= IN.color.rgb;
                float finalAlpha = cardMask * IN.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                finalAlpha *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(finalAlpha - 0.001);
                #endif

                return fixed4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}