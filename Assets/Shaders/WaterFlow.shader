// Water Flow Liquid Shader — URP / HLSL only (no Shader Graph)
// Single mesh, single material. Color changes are driven entirely by shader
// properties (_CurrentColor / _TargetColor / _Transition) via MaterialPropertyBlock.
Shader "Custom/URP/WaterFlow"
{
    Properties
    {
        [Header(Water Texture)]
        _MainTex ("Main Texture", 2D) = "white" {}
        [Toggle(_ENABLE_PT_MAIN)] _EnablePTMain ("Enable PT Main", Float) = 1
        _PT_Main ("PT Main", 2D) = "white" {}
        _PTMainColor ("PT Main Color", Color) = (1, 1, 1, 1)
        _PTMainTiling ("PT Main Tiling (XY)", Vector) = (1, 1, 0, 0)
        _PTMainScrollSpeed ("PT Main Scroll Speed (Y)", Float) = 0.5
        [KeywordEnum(Multiply, Add, Screen)] _PTMainBlendMode ("PT Main Blend Mode", Float) = 1
        _NoiseTex ("Noise / Distortion Texture", 2D) = "gray" {}
        _Tiling ("Main Tiling (XY)", Vector) = (1, 1, 0, 0)

        [Header(Normal Map)]
        [Toggle(_ENABLE_NORMALMAP)] _EnableNormalMap ("Enable Normal Map", Float) = 1
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.3
        _NormalLightStrength ("Normal Light Strength", Range(0, 1)) = 0.5

        [Header(Flow)]
        _ScrollSpeed ("Scroll Speed (Y)", Float) = 0.5
        [Toggle(_ENABLE_DISTORTION)] _EnableDistortion ("Enable Distortion", Float) = 1
        _DistortionStrength ("Distortion Strength", Range(0, 0.5)) = 0.05
        _DistortionSpeed ("Distortion Speed", Float) = 0.5
        _DistortionTiling ("Distortion Tiling (XY)", Vector) = (1, 1, 0, 0)

        [Header(Color Transition)]
        _CurrentColor ("Current Color", Color) = (0.8, 0.1, 0.1, 1)
        _TargetColor ("Target Color", Color) = (0.8, 0.1, 0.1, 1)
        _Transition ("Transition Progress", Range(0, 1)) = 1
        _TransitionSpeed ("Transition Speed (driven by controller)", Float) = 1
        _TransitionSoftness ("Transition Softness", Range(0.001, 1)) = 0.15
        _TransitionNoiseStrength ("Transition Noise Strength", Range(0, 1)) = 0.25

        [Header(Depth)]
        _DepthFade ("Depth Fade Distance", Float) = 1.0
        _DepthPower ("Depth Power", Float) = 1.0

        [Header(Foam Intersection Line)]
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamDistance ("Foam Distance", Float) = 0.25
        _FoamNoiseStrength ("Foam Noise Strength", Range(0, 10)) = 0.3

        [Header(Dissolve)]
        _DissolveTex ("Dissolve Texture", 2D) = "white" {}
        _DissolveTiling ("Dissolve Tiling (XY)", Vector) = (1, 1, 0, 0)
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveNoiseStrength ("Dissolve Noise Strength", Range(0, 10)) = 0.2
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0.001, 1)) = 0.08
        _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1, 0.6, 0.1, 1)

        [Header(Visual)]
        _Alpha ("Alpha", Range(0, 1)) = 0.85
        _EmissionStrength ("Emission Strength", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalRenderPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ENABLE_DISTORTION
            #pragma shader_feature_local _ENABLE_PT_MAIN
            #pragma shader_feature_local _PTMAINBLENDMODE_MULTIPLY _PTMAINBLENDMODE_ADD _PTMAINBLENDMODE_SCREEN
            #pragma shader_feature_local _ENABLE_NORMALMAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_PT_Main);
            SAMPLER(sampler_PT_Main);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_DissolveTex);
            SAMPLER(sampler_DissolveTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _Tiling;
                float _ScrollSpeed;
                half4 _PTMainColor;
                float4 _PTMainTiling;
                float _PTMainScrollSpeed;
                float _DistortionStrength;
                float _DistortionSpeed;
                float4 _DistortionTiling;

                float _NormalStrength;
                float _NormalLightStrength;

                half4 _CurrentColor;
                half4 _TargetColor;
                float _Transition;
                float _TransitionSpeed;
                float _TransitionSoftness;
                float _TransitionNoiseStrength;

                float _DepthFade;
                float _DepthPower;

                half4 _FoamColor;
                float _FoamDistance;
                float _FoamNoiseStrength;

                float4 _DissolveTiling;
                float _DissolveAmount;
                float _DissolveNoiseStrength;
                float _DissolveEdgeWidth;
                half4 _DissolveEdgeColor;

                float _Alpha;
                float _EmissionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ---- Noise sample (shared for distortion + transition edge) ----
                float2 noiseUV = IN.uv * _DistortionTiling.xy + float2(0.0, _Time.y * _DistortionSpeed);
                half4 noiseSample = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV);

                // ---- Dissolve: dedicated texture pattern with a glowing edge ----
                float2 dissolveUV = IN.uv * _DissolveTiling.xy;
                half dissolveSample = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, dissolveUV).r;
                float dissolveNoise = (noiseSample.b - 0.5) * _DissolveNoiseStrength;
                float dissolveDiff = dissolveSample - _DissolveAmount + dissolveNoise;
                clip(dissolveDiff);
                float dissolveEdgeMask = 1.0 - smoothstep(0.0, max(_DissolveEdgeWidth, 0.001), dissolveDiff);

                // ---- Water flow UV: continuous Y scroll ----
                float2 mainUV = IN.uv * _Tiling.xy;
                mainUV.y += _Time.y * _ScrollSpeed;

                #if defined(_ENABLE_DISTORTION)
                    float2 distortOffset = (noiseSample.rg - 0.5h) * 2.0 * _DistortionStrength;
                    mainUV += distortOffset;
                #endif

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUV);

                // ---- Color flow transition mask: UV.y + TransitionProgress + Noise ----
                float noiseInfluence = (noiseSample.r - 0.5) * _TransitionNoiseStrength;
                float threshold = 1.0 - _Transition;
                float edge = IN.uv.y - threshold + noiseInfluence;
                float halfSoft = max(_TransitionSoftness, 0.001) * 0.5;
                float mask = smoothstep(-halfSoft, halfSoft, edge);

                half4 flowColor = lerp(_CurrentColor, _TargetColor, mask);
                half4 finalColor = tex * flowColor;

                // ---- PT Main: independent pattern + own color, not tied to Current/Target ----
                // _PTMainColor.a controls strength: 0 = no effect, 1 = full effect.
                #if defined(_ENABLE_PT_MAIN)
                    float2 ptMainUV = IN.uv * _PTMainTiling.xy;
                    ptMainUV.y += _Time.y * _PTMainScrollSpeed;
                    half3 ptMain = SAMPLE_TEXTURE2D(_PT_Main, sampler_PT_Main, ptMainUV).rgb * _PTMainColor.rgb;

                    half3 ptMainResult;
                    #if defined(_PTMAINBLENDMODE_ADD)
                        ptMainResult = finalColor.rgb + ptMain;
                    #elif defined(_PTMAINBLENDMODE_SCREEN)
                        ptMainResult = 1.0 - (1.0 - finalColor.rgb) * (1.0 - ptMain);
                    #else
                        ptMainResult = finalColor.rgb * ptMain;
                    #endif

                    finalColor.rgb = lerp(finalColor.rgb, ptMainResult, saturate(_PTMainColor.a));
                #endif

                // ---- Normal map: cheap ripple shading against the scene's main light ----
                // Approximates the world normal for a flat water plane (up perturbed by the
                // packed normal XY) instead of a full TBN, since there is no tangent input.
                #if defined(_ENABLE_NORMALMAP)
                    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, mainUV), _NormalStrength);
                    half3 approxNormalWS = normalize(half3(normalTS.x, 1.0, normalTS.y));
                    Light mainLight = GetMainLight();
                    half ndotl = saturate(dot(approxNormalWS, mainLight.direction));
                    half normalLightTerm = lerp(1.0 - _NormalLightStrength, 1.0 + _NormalLightStrength, ndotl);
                    finalColor.rgb *= normalLightTerm;
                #endif

                // ---- Scene depth via URP Camera Depth Texture ----
                float2 screenUV = IN.positionCS.xy / _ScreenParams.xy;
                float sceneRawDepth = SampleSceneDepth(screenUV);
                float sceneEyeDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                float surfaceEyeDepth = LinearEyeDepth(IN.positionCS.z, _ZBufferParams);
                float depthDiff = max(sceneEyeDepth - surfaceEyeDepth, 0.0);

                // ---- Foam line where water intersects geometry, wobbled by noise ----
                float foamNoise = (noiseSample.g - 0.5) * _FoamNoiseStrength;
                float foamEdge = depthDiff + foamNoise;
                float foamMask = 1.0 - smoothstep(0.0, max(_FoamDistance, 0.0001), foamEdge);
                finalColor.rgb = lerp(finalColor.rgb, _FoamColor.rgb, foamMask * _FoamColor.a);

                // ---- Depth fade for soft intersection alpha ----
                float depthFade = saturate(depthDiff / max(_DepthFade, 0.0001));
                depthFade = pow(depthFade, max(_DepthPower, 0.0001));

                half alpha = tex.a * _Alpha * depthFade;

                // ---- Optional emission ----
                finalColor.rgb += finalColor.rgb * _EmissionStrength;

                // ---- Dissolve edge glow ----
                finalColor.rgb += _DissolveEdgeColor.rgb * dissolveEdgeMask;

                return half4(finalColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
