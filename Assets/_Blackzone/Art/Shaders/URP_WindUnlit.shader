// BLACKZONE URP Wind-Animated Unlit Shader
// Vertex displacement wind effect for vegetation and props.
// Driven by WindManager global properties (_WindDir, _WindStrength, _WindSpeed).
// Uses object-space height mask: vertices at the base stay anchored,
// vertices at the top sway with the wind. Frequency-based sin/cos creates
// natural swaying motion with secondary flutter.

Shader "Blackzone/WindUnlit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0,1)) = 0.1
        _WindAmplitude ("Wind Amplitude", Range(0, 2)) = 0.15
        _WindFrequency ("Wind Frequency", Range(0.5, 8)) = 2.5
        _WindFlutter ("Wind Flutter", Range(0, 3)) = 1.2
        _WindFlutterFreq ("Flutter Frequency", Range(1, 12)) = 6.0
        _BendStiffness ("Bend Stiffness", Range(0.1, 2)) = 0.8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Global wind properties set by WindManager via Shader.SetGlobalVector
            float3 _WindDir;
            float _WindStrength;
            float _WindSpeed;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            half4 _Color;
            half _Smoothness;
            half _WindAmplitude;
            half _WindFrequency;
            half _WindFlutter;
            half _WindFlutterFreq;
            half _BendStiffness;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 posOS = IN.positionOS.xyz;

                // Height mask: 0 at base (y=0), 1 at top (object-space)
                // This anchors the bottom and lets the top sway freely.
                float heightMask = saturate(posOS.y * _BendStiffness);

                // Main wind sway (large, slow oscillation)
                float windPhase = _Time.y * _WindSpeed * _WindFrequency;
                float mainSway = sin(windPhase + posOS.x * 0.3 + posOS.z * 0.2) * _WindAmplitude;

                // Secondary flutter (fast, small oscillation for realism)
                float flutterPhase = _Time.y * _WindFlutterFreq;
                float flutter = sin(flutterPhase + posOS.y * 2.0 + posOS.x * 0.5) * _WindFlutter * 0.15;

                // Third harmonic — very high frequency micro-tremor
                float microTremor = cos(flutterPhase * 1.7 + posOS.y * 4.0) * _WindFlutter * 0.05;

                // Combine wind displacement along wind direction
                float totalDisplacement = (mainSway + flutter + microTremor) * _WindStrength;
                posOS.xz += _WindDir.xz * totalDisplacement * heightMask;

                // Slight vertical compression when swaying (squash & stretch)
                posOS.y -= abs(totalDisplacement) * heightMask * 0.05;

                OUT.positionHCS = TransformObjectToHClip(posOS);
                OUT.positionWS = TransformObjectToWorld(posOS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

                // Simple directional lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalize(IN.normalWS), mainLight.direction));
                half3 diffuse = albedo.rgb * mainLight.color * NdotL;
                half3 ambient = albedo.rgb * half3(0.35, 0.38, 0.42); // desert ambient

                half3 finalColor = diffuse + ambient * 0.6;

                // Apply fog
                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // Shadow caster pass for vegetation shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _WindDir;
            float _WindStrength;
            float _WindSpeed;
            half _WindAmplitude;
            half _WindFrequency;
            half _WindFlutter;
            half _WindFlutterFreq;
            half _BendStiffness;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionHCS : SV_POSITION;
            };

            float3 GetLightDirection()
            {
                return normalize(_MainLightPosition.xyz);
            }

            ShadowVaryings ShadowVert(ShadowAttributes IN)
            {
                ShadowVaryings OUT;
                float3 posOS = IN.positionOS.xyz;

                // Same wind displacement as main pass
                float heightMask = saturate(posOS.y * _BendStiffness);
                float windPhase = _Time.y * _WindSpeed * _WindFrequency;
                float mainSway = sin(windPhase + posOS.x * 0.3 + posOS.z * 0.2) * _WindAmplitude;
                float flutter = sin(_Time.y * _WindFlutterFreq + posOS.y * 2.0) * _WindFlutter * 0.15;
                float totalDisp = (mainSway + flutter) * _WindStrength;
                posOS.xz += _WindDir.xz * totalDisp * heightMask;

                float3 posWS = TransformObjectToWorld(posOS);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionHCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, GetLightDirection()));
                return OUT;
            }

            half4 ShadowFrag(ShadowVaryings IN) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }

    // Fallback for non-URP: use Standard Unlit
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float3 _WindDir;
            float _WindStrength;
            float _WindSpeed;
            float4 _Color;
            half _WindAmplitude;
            half _WindFrequency;
            half _WindFlutter;
            half _WindFlutterFreq;
            half _BendStiffness;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                float3 pos = v.vertex.xyz;
                float heightMask = saturate(pos.y * _BendStiffness);
                float windPhase = _Time.y * _WindSpeed * _WindFrequency;
                float sway = sin(windPhase + pos.x * 0.3 + pos.z * 0.2) * _WindAmplitude;
                float flutter = sin(_Time.y * _WindFlutterFreq + pos.y * 2.0) * _WindFlutter * 0.15;
                pos.xz += _WindDir.xz * (sway + flutter) * _WindStrength * heightMask;
                o.pos = UnityObjectToClipPos(float4(pos, 1.0));
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }

    FallbackOff
}
