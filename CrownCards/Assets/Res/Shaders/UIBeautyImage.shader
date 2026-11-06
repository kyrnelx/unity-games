Shader "UI/URP/BeautyImage"
{
    Properties
    {
        // ---------------- 基础图层与染色 ----------------
        [PerRendererData] _MainTex ("Sprite Texture 底图", 2D) = "white" {}
        _Color ("Tint 全局叠加色", Color) = (1,1,1,1)

        // ---------------- 1. SDF 圆角与宽高比 ----------------
        [Header(1. Rounded Corners)]
        _Aspect ("Aspect Ratio 宽高比 (长宽比填 Width div Height 防止椭圆变形)", Float) = 1.0
        _CornerRadii ("Corner Radii 四角半径 (X左上 Y右上 Z右下 W左下 取值0到0.5)", Vector) = (0.1, 0.1, 0.1, 0.1)

        // ---------------- 2. 内描边设置 ----------------
        [Header(2. Inner Border)]
        _BorderWidth ("Border Width 描边宽度 (0为关闭 推荐0.01至0.05)", Range(0.0, 0.2)) = 0.0
        _BorderColor ("Border Color 描边颜色 (支持透明度)", Color) = (1, 1, 1, 1)

        // ---------------- 3. 线性方向渐变 ----------------
        [Header(3. Linear Gradient)]
        _ColorA ("Color A 渐变起点色", Color) = (0.2, 0.6, 1.0, 1.0)
        _ColorB ("Color B 渐变终点色", Color) = (0.8, 0.2, 0.9, 1.0)
        _GradientAngle ("Gradient Angle 渐变旋转角度 (0左到右 90下到上 45斜向)", Range(0, 360)) = 45.0

        // ---------------- 4. 扫光流光效果 ----------------
        [Header(4. Shine Sweep)]
        [Toggle(_ENABLE_SHINE)] _EnableShine ("Enable Shine 开启流光扫光", Float) = 1.0
        [HDR] _ShineColor ("Shine HDR Color 流光颜色与发光强度", Color) = (1.5, 1.5, 1.5, 1.0)
        _ShineAngle ("Shine Angle 流光倾角 (推荐30到60度)", Range(-180, 180)) = 45.0
        _ShineWidth ("Shine Width 流光光带宽度", Range(0.01, 0.5)) = 0.12
        _ShineSoftness ("Shine Softness 羽化度 (越小越硬 越大越柔)", Range(0.01, 0.5)) = 0.15
        _ShineSpeed ("Shine Speed 流光移动速度", Range(0.1, 5.0)) = 1.2
        _ShineInterval ("Shine Interval 扫光间隔秒数 (0为无缝连续 推荐2秒)", Range(0.0, 10.0)) = 2.0

        // ---------------- UGUI 原生系统管线控制 ----------------
        [HideInInspector] _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            Name "UIBeautyPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma shader_feature_local _ENABLE_SHINE
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                float4 worldPosition: TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Aspect;
                float4 _CornerRadii;
                float _BorderWidth;
                float4 _BorderColor;
                float4 _ColorA;
                float4 _ColorB;
                float _GradientAngle;
                float4 _ShineColor;
                float _ShineAngle;
                float _ShineWidth;
                float _ShineSoftness;
                float _ShineSpeed;
                float _ShineInterval;
                float4 _ClipRect;
            CBUFFER_END

            // RectMask2D 裁剪计算
            inline float Get2DClipping(float2 position, float4 clipRect)
            {
                float2 inside = step(clipRect.xy, position) * step(position, clipRect.zw);
                return inside.x * inside.y;
            }

            // SDF 独立四圆角矩形算法
            float SDF_RoundedBox(float2 uv, float aspect, float4 radii)
            {
                float2 p = (uv - 0.5) * float2(aspect, 1.0);
                float2 halfSize = float2(0.5 * aspect, 0.5);

                // TL: radii.x, TR: radii.y, BR: radii.z, BL: radii.w
                float rad = (p.x > 0.0) ? 
                            ((p.y > 0.0) ? radii.y : radii.z) : 
                            ((p.y > 0.0) ? radii.x : radii.w);

                rad = min(rad, min(halfSize.x, halfSize.y));

                float2 q = abs(p) - halfSize + rad;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - rad;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.worldPosition = input.positionOS;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // 1. SDF 圆角与 fwidth 抗锯齿
                float safeAspect = max(_Aspect, 0.0001);
                float dist = SDF_RoundedBox(uv, safeAspect, _CornerRadii);
                
                float aa = fwidth(dist);
                float shapeAlpha = 1.0 - smoothstep(-aa * 0.5, aa * 0.5, dist);

                #ifdef UNITY_UI_ALPHACLIP
                    clip(shapeAlpha - 0.001);
                #endif

                // 2. 角度线性渐变
                float gradRad = radians(_GradientAngle);
                float2 gradDir = float2(cos(gradRad), sin(gradRad));
                float extent = 0.5 * (abs(gradDir.x) + abs(gradDir.y));
                float gradT = saturate(dot(uv - 0.5, gradDir) / (2.0 * max(extent, 0.001)) + 0.5);
                half4 gradientColor = lerp(_ColorA, _ColorB, gradT);

                // 采样 Sprite 贴图与顶点色
                half4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 finalColor = mainTexColor * gradientColor * input.color;

                // 3. 内描边
                if (_BorderWidth > 0.0001)
                {
                    float innerDist = dist + _BorderWidth;
                    float innerAlpha = 1.0 - smoothstep(-aa * 0.5, aa * 0.5, innerDist);
                    finalColor.rgb = lerp(_BorderColor.rgb, finalColor.rgb, innerAlpha);
                }

                // 4. 周期停顿扫光
                #if defined(_ENABLE_SHINE)
                    float shineDuration = 1.0 / max(_ShineSpeed, 0.001);
                    float totalCycle = shineDuration + _ShineInterval;
                    float cycleTime = fmod(_Time.y, totalCycle);

                    if (cycleTime < shineDuration)
                    {
                        float progress = cycleTime / shineDuration;
                        float shineRad = radians(_ShineAngle);
                        float2 shineDir = float2(cos(shineRad), sin(shineRad));

                        float proj = dot(uv - 0.5, shineDir);
                        float currentCenter = lerp(-0.9, 0.9, progress);
                        float distToShine = abs(proj - currentCenter);

                        float shineMask = 1.0 - smoothstep(_ShineWidth * 0.5, _ShineWidth * 0.5 + _ShineSoftness, distToShine);
                        finalColor.rgb += _ShineColor.rgb * (shineMask * _ShineColor.a);
                    }
                #endif

                // 5. 最终 Alpha 合成与 RectMask2D 剪裁
                finalColor.a *= shapeAlpha;

                #if defined(UNITY_UI_CLIP_RECT)
                    finalColor.a *= Get2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "UI/Default"
}