// Water Flow Liquid Shader — URP / HLSL only (no Shader Graph)
// Single mesh, single material. Color changes are driven entirely by shader
// properties (_CurrentColor / _TargetColor / _Transition) via MaterialPropertyBlock.
Shader "Custom/URP/WaterFlow"
{
    Properties
    {
        [Header(Water Texture)]
        _MainTex ("Main Texture", 2D) = "white" {}
        _NoiseTex ("Noise / Distortion Texture", 2D) = "gray" {}
        _Tiling ("Main Tiling (XY)", Vector) = (1, 1, 0, 0)

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

            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ENABLE_DISTORTION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Tiling;
                float _ScrollSpeed;
                float _DistortionStrength;
                float _DistortionSpeed;
                float4 _DistortionTiling;

                half4 _CurrentColor;
                half4 _TargetColor;
                float _Transition;
                float _TransitionSpeed;
                float _TransitionSoftness;
                float _TransitionNoiseStrength;

                float _DepthFade;
                float _DepthPower;

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

                // ---- Depth fade using URP Camera Depth Texture (soft intersection) ----
                float2 screenUV = IN.positionCS.xy / _ScreenParams.xy;
                float sceneRawDepth = SampleSceneDepth(screenUV);
                float sceneEyeDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                float surfaceEyeDepth = LinearEyeDepth(IN.positionCS.z, _ZBufferParams);
                float depthDiff = max(sceneEyeDepth - surfaceEyeDepth, 0.0);
                float depthFade = saturate(depthDiff / max(_DepthFade, 0.0001));
                depthFade = pow(depthFade, max(_DepthPower, 0.0001));

                half alpha = tex.a * _Alpha * depthFade;

                // ---- Optional emission ----
                finalColor.rgb += finalColor.rgb * _EmissionStrength;

                return half4(finalColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
