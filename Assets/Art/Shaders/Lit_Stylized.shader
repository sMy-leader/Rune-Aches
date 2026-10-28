// ─────────────────────────────────────────────────────────────────────────────
// RuneAshes/Lit_Stylized
// 手绘低模统一材质：平涂三阶光照 + 环境色 + 边缘光 + 自发光 + 受击闪白 + 溶解。
//
// 设计约束（见 Docs/美术风格与资源清单_v1.md 2.3）：
//   - 参数名固定，程序通过 MaterialPropertyBlock 改 _HitFlash / _DissolveAmount；
//   - 只吃主光 + 环境光，不接收实时阴影（杂兵用 blob shadow，玩家/Boss 的阴影 W4 再加）；
//   - 平涂（toon）而不是 PBR，保证不同来源的模型看起来是同一套语言。
//
// [坑] MaterialPropertyBlock 会破坏 SRP Batcher 的批处理。W3 先用它换手感，
//      W5 性能冲刺时再评估改成 per-instance 属性 / 顶点色。
// ─────────────────────────────────────────────────────────────────────────────
Shader "RuneAshes/Lit_Stylized"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionStrength ("Emission Strength", Range(0, 8)) = 0

        _RimColor ("Rim Color", Color) = (0.27, 0.85, 0.77, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.5

        _HitFlash ("Hit Flash", Range(0, 1)) = 0
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveScale ("Dissolve Scale", Float) = 12

        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        _ToonLow ("Toon Low", Range(0, 1)) = 0.45
        _ToonMid ("Toon Mid", Range(0, 1)) = 0.72
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // ── 主渲染 ────────────────────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                half _EmissionStrength;
                half4 _RimColor;
                half _RimPower;
                half _RimStrength;
                half _HitFlash;
                half _DissolveAmount;
                half _DissolveScale;
                half _AlphaClip;
                half _Cutoff;
                half _ToonLow;
                half _ToonMid;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // 世界坐标哈希噪声：不额外占一张溶解贴图，足够做"灰烬消散"。
            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = baseMap.rgb * _BaseColor.rgb;
                half alpha = baseMap.a * _BaseColor.a;

                #ifdef _ALPHATEST_ON
                    clip(alpha - _Cutoff);
                #endif

                // 溶解：_DissolveAmount = 0 时全部保留，= 1 时全部消失。
                float dissolveNoise = Hash13(input.positionWS * _DissolveScale);
                clip(dissolveNoise - _DissolveAmount);

                float3 N = normalize(input.normalWS);
                float3 V = normalize(input.viewDirWS);

                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(N, mainLight.direction));

                // 三阶平涂：比连续 PBR 更容易让不同来源的模型统一。
                float toon = NdotL > 0.66 ? 1.0 : (NdotL > 0.33 ? _ToonMid : _ToonLow);
                half3 ambient = SampleSH(N);
                half3 lit = albedo * (ambient + mainLight.color * toon);

                float rim = pow(1.0 - saturate(dot(N, V)), _RimPower) * _RimStrength;
                half3 emission = _EmissionColor.rgb * _EmissionStrength;

                half3 finalColor = lit + _RimColor.rgb * rim + emission;
                finalColor = lerp(finalColor, half3(1, 1, 1), _HitFlash);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }

        // ── 阴影投射 ──────────────────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ── 深度 ──────────────────────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ── 深度+法线（SSAO / 深度法线后处理需要）──────────────────────────────
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFrag(Varyings input) : SV_Target
            {
                return half4(normalize(input.normalWS), 0);
            }
            ENDHLSL
        }
    }

    // 没有自定义 Fallback：所有 pass 都显式提供，避免 URP 找不到对应 LightMode。
}
