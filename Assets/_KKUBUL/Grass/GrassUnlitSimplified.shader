// Simplified Grass Shader for URP Forward Rendering
Shader "Custom/GrassUnlitSimplified"
{
    Properties
    {
        [Toggle] _Blend("Blend with Terrain", Float) = 0
        _BlendMult("Blend Multiplier", Float) = 1
        _BlendOff("Blend Offset", Float) = 0
        _Scale("Noise Scale", Float) = 50
        _Thershold("White Noise Threshold", Float) = 0.7
        _WhiteTint("White Tint Color", Color) = (1, 1, 1, 1)
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Off
            ZWrite On
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _LIGHT_COOKIES
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float _Blend;
                float _BlendMult;
                float _BlendOff;
                float _Thershold;
                float _Scale;
                float4 _WhiteTint;
            CBUFFER_END
            
            TEXTURE2D(_TerrainDiffuse);
            SAMPLER(sampler_TerrainDiffuse);
            float4 _TopTint;
            float4 _BottomTint;
            
            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 color : COLOR;
                float fogFactor : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
            };
            
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            float GradientNoise(float2 uv, float scale)
            {
                float2 p = uv * scale;
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = Hash(i);
                float b = Hash(i + float2(1.0, 0.0));
                float c = Hash(i + float2(0.0, 1.0));
                float d = Hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float3 worldPos;
                float3 normal;
                float2 uv;
                float3 paintColor;
                GetComputeData_float(input.vertexID, worldPos, normal, uv, paintColor);
                
                output.positionCS = TransformWorldToHClip(worldPos);
                output.positionWS = worldPos;
                output.normalWS = normal;
                output.uv = uv;
                output.color = paintColor;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                
                // Calculate shadow coordinates
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    output.shadowCoord = ComputeScreenPos(output.positionCS);
                #else
                    output.shadowCoord = TransformWorldToShadowCoord(worldPos);
                #endif
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Vertical blend factor
                float verticalBlend = saturate(input.uv.y * _BlendMult + _BlendOff);
                
                // Base color gradient
                float4 baseColor = lerp(_BottomTint, _TopTint, verticalBlend);
                
                // White noise variation
                float2 noiseUV = frac(input.positionWS.xz * 0.5);
                float noise = GradientNoise(noiseUV, _Scale);
                float whiteMask = step(_Thershold, noise);
                float4 whiteVariation = _WhiteTint * whiteMask;
                float4 colorWithNoise = max(baseColor, whiteVariation);
                
                // Apply paint color
                float3 finalColor = colorWithNoise.rgb * input.color;
                
                // Terrain blending
                if (_Blend > 0.5)
                {
                    float2 terrainUV;
                    GetWorldUV_float(input.positionWS, terrainUV);
                    float3 terrainColor = SAMPLE_TEXTURE2D(_TerrainDiffuse, sampler_TerrainDiffuse, terrainUV).rgb;
                    finalColor = lerp(terrainColor, finalColor, verticalBlend);
                }
                
                // Lighting with shadows and cookies
                Light mainLight = GetMainLight(input.shadowCoord);
                float NdotL = saturate(dot(input.normalWS, mainLight.direction));
                
                // Sample light cookie for directional light
                #if defined(_LIGHT_COOKIES)
                    float3 cookieColor = SampleMainLightCookie(input.positionWS);
                #else
                    float3 cookieColor = 1.0;
                #endif
                
                float3 lighting = mainLight.color * ((NdotL * 0.5 + 0.5) * mainLight.shadowAttenuation * mainLight.distanceAttenuation * cookieColor);
                
                // Ambient lighting
                float3 ambient = SampleSH(input.normalWS) * 0.5;
                lighting += ambient;
                
                finalColor *= lighting;
                
                // Fog
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return float4(finalColor, 1.0);
            }
            
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            Cull Off
            ZWrite On
            ColorMask 0
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
            
            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            float3 _LightDirection;
            
            float4 GetShadowPositionHClip(float3 positionWS, float3 normalWS)
            {
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                
                return positionCS;
            }
            
            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                
                float3 worldPos;
                float3 normal;
                float2 uv;
                float3 color;
                GetComputeData_float(input.vertexID, worldPos, normal, uv, color);
                
                output.positionCS = GetShadowPositionHClip(worldPos, normal);
                
                return output;
            }
            
            float4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            Cull Off
            ZWrite On
            ColorMask R
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
            
            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                
                float3 worldPos;
                float3 normal;
                float2 uv;
                float3 color;
                GetComputeData_float(input.vertexID, worldPos, normal, uv, color);
                
                output.positionCS = TransformWorldToHClip(worldPos);
                
                return output;
            }
            
            float4 DepthFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            
            ENDHLSL
        }
        
        // DepthNormals pass - Required for FakePointLight system
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            
            Cull Off
            ZWrite On
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
            
            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };
            
            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output;
                
                float3 worldPos;
                float3 normal;
                float2 uv;
                float3 color;
                GetComputeData_float(input.vertexID, worldPos, normal, uv, color);
                
                output.positionCS = TransformWorldToHClip(worldPos);
                output.normalWS = normal;
                
                return output;
            }
            
            float4 DepthNormalsFrag(Varyings input) : SV_Target
            {
                // Output world space normals for FakePointLight to read
                return float4(normalize(input.normalWS), 0.0);
            }
            
            ENDHLSL
        }
        
        // Custom pass for ScriptableRendererFeature
        Pass
        {
            Name "MySpecialPass"
            Tags { "LightMode" = "MyCustomPass" }
            
            Cull Off
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex CustomVert
            #pragma fragment CustomFrag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
            
            // Properties
            CBUFFER_START(UnityPerMaterial)
                float _Blend;
                float _BlendMult;
                float _BlendOff;
                float _Thershold;
                float _Scale;
                float4 _WhiteTint;
            CBUFFER_END
            
            TEXTURE2D(_TerrainDiffuse);
            SAMPLER(sampler_TerrainDiffuse);
            float4 _TopTint;
            float4 _BottomTint;
            
            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 color : COLOR;
                float fogFactor : TEXCOORD3;
            };
            
            Varyings CustomVert(Attributes input)
            {
                Varyings output;
                
                float3 worldPos;
                float3 normal;
                float2 uv;
                float3 paintColor;
                GetComputeData_float(input.vertexID, worldPos, normal, uv, paintColor);
                
                output.positionCS = TransformWorldToHClip(worldPos);
                output.positionWS = worldPos;
                output.normalWS = normal;
                output.uv = uv;
                output.color = paintColor;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }
            
            float4 CustomFrag(Varyings input) : SV_Target
            {
                // Option 1: Flat color (current)
                float3 customColor = float3(1, 0, 0);
                
                // Option 2: Keep base color with highlight
                // float verticalBlend = saturate(input.uv.y * _BlendMult + _BlendOff);
                // float4 baseColor = lerp(_BottomTint, _TopTint, verticalBlend);
                // float3 customColor = baseColor.rgb * input.color;
                
                // Option 3: Edge outline
                // float edge = abs(input.uv.x - 0.5) > 0.4 ? 1.0 : 0.0;
                // float3 customColor = lerp(float3(0,0,0), float3(1,1,0), edge);
                
                customColor = MixFog(customColor, input.fogFactor);
                return float4(customColor, 1.0);
            }
            
            ENDHLSL
        }
    }
    
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
