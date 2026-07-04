Shader "Shader Graphs/GrassUnlitBasic"
{
    Properties
    {
        [ToggleUI]_Blend("Blend", Float) = 0
        _BlendMult("BlendMult", Float) = 1
        _BlendOff("BlendOff", Float) = 0
        _Scale("Scale", Float) = 50
        _Thershold("Thershold", Float) = 0.7
        _WhiteTint("WhiteTint", Color) = (1, 1, 1, 1)
        [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector]_QueueControl("_QueueControl", Float) = -1
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "UniversalMaterialType" = "Lit"
            "Queue"="Geometry"
            "DisableBatching"="False"
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="UniversalLitSubTarget"
        }
        Pass
        {
            Name "Universal Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
        
        // Render State
        Cull Off
        Blend One Zero
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma instancing_options renderinglayer
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DYNAMICLIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
        #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
        #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
        #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile _ SHADOWS_SHADOWMASK
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ _LIGHT_LAYERS
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma multi_compile_fragment _ _LIGHT_COOKIES
        #pragma multi_compile _ _FORWARD_PLUS
        #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        #define VARYINGS_NEED_SHADOW_COORD
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_FORWARD
        #define _FOG_FRAGMENT 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
            #if defined(LIGHTMAP_ON)
             float2 staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
             float2 dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
             float4 probeOcclusion;
            #endif
             float4 fogFactorAndVertexLight;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             float4 shadowCoord;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
             float3 WorldPos;
             float3 ColorPaint;
             float2 UV;
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
             float3 WorldPos;
             float3 ColorPaint;
             float2 UV;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if defined(LIGHTMAP_ON)
             float2 staticLightmapUV : INTERP0;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
             float2 dynamicLightmapUV : INTERP1;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh : INTERP2;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
             float4 probeOcclusion : INTERP3;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             float4 shadowCoord : INTERP4;
            #endif
             float4 tangentWS : INTERP5;
             float4 fogFactorAndVertexLight : INTERP6;
             float4 packed_positionWS_UVx : INTERP7;
             float4 packed_normalWS_UVy : INTERP8;
             float3 WorldPos : INTERP9;
             float3 ColorPaint : INTERP10;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
            output.probeOcclusion = input.probeOcclusion;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.shadowCoord = input.shadowCoord;
            #endif
            output.tangentWS.xyzw = input.tangentWS;
            output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
            output.packed_positionWS_UVx.xyz = input.positionWS;
            output.packed_positionWS_UVx.w = input.UV.x;
            output.packed_normalWS_UVy.xyz = input.normalWS;
            output.packed_normalWS_UVy.w = input.UV.y;
            output.WorldPos.xyz = input.WorldPos;
            output.ColorPaint.xyz = input.ColorPaint;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
            output.probeOcclusion = input.probeOcclusion;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.shadowCoord = input.shadowCoord;
            #endif
            output.tangentWS = input.tangentWS.xyzw;
            output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
            output.positionWS = input.packed_positionWS_UVx.xyz;
            output.UV.x = input.packed_positionWS_UVx.w;
            output.normalWS = input.packed_normalWS_UVy.xyz;
            output.UV.y = input.packed_normalWS_UVy.w;
            output.WorldPos = input.WorldPos.xyz;
            output.ColorPaint = input.ColorPaint.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _Blend;
        float _BlendMult;
        float _BlendOff;
        float _Thershold;
        float _Scale;
        float4 _WhiteTint;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TerrainDiffuse);
        SAMPLER(sampler_TerrainDiffuse);
        float4 _TerrainDiffuse_TexelSize;
        float4 _TopTint;
        float4 _BottomTint;
        
        // Graph Includes
        #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Modulo_float3(float3 A, float3 B, out float3 Out)
        {
            Out = fmod(A, B);
        }
        
        float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
        {
            float x; Hash_Tchou_2_1_float(p, x);
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
        {
            float2 p = UV * Scale.xy;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Step_float(float Edge, float In, out float Out)
        {
            Out = step(Edge, In);
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Maximum_float4(float4 A, float4 B, out float4 Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
            float3 WorldPos;
            float3 ColorPaint;
            float2 UV;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            float2 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            GetComputeData_float(IN.VertexID, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3);
            description.Position = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.Normal = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            description.Tangent = IN.ObjectSpaceTangent;
            description.WorldPos = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.ColorPaint = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            description.UV = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        output.WorldPos = input.WorldPos;
        output.ColorPaint = input.ColorPaint;
        output.UV = input.UV;
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float3 Emission;
            float Metallic;
            float Smoothness;
            float Occlusion;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Property_5d7ce0c940ef40b49a4225123448caa2_Out_0_Boolean = _Blend;
            UnityTexture2D _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_TerrainDiffuse);
            float2 _GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2;
            GetWorldUV_float(IN.WorldPos, _GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2);
            float4 _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.tex, _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.samplerstate, _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.GetTransformedUV(_GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2) );
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_R_4_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.r;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_G_5_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.g;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_B_6_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.b;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_A_7_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.a;
            float4 _Property_4e2f08a2ec1945c4a61a6673c09f0834_Out_0_Vector4 = _BottomTint;
            float4 _Property_3ab603e217ab4c3089acc1492753ca25_Out_0_Vector4 = _TopTint;
            float _Property_8d79a54a1cb3451e86bd511d107fb8a0_Out_0_Float = _BlendMult;
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_R_1_Float = IN.UV[0];
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_G_2_Float = IN.UV[1];
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_B_3_Float = 0;
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_A_4_Float = 0;
            float _Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float;
            Unity_Multiply_float_float(_Property_8d79a54a1cb3451e86bd511d107fb8a0_Out_0_Float, _Split_dafbc94f76b74c2092e3bf0e915df7ee_G_2_Float, _Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float);
            float _Property_03b460e3b1aa44a78be15ff860775597_Out_0_Float = _BlendOff;
            float _Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float;
            Unity_Add_float(_Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float, _Property_03b460e3b1aa44a78be15ff860775597_Out_0_Float, _Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float);
            float _Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float;
            Unity_Saturate_float(_Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float, _Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float);
            float4 _Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4;
            Unity_Lerp_float4(_Property_4e2f08a2ec1945c4a61a6673c09f0834_Out_0_Vector4, _Property_3ab603e217ab4c3089acc1492753ca25_Out_0_Vector4, (_Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float.xxxx), _Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4);
            float4 _Property_cbbb60a5dfca48069bb1fbcd0e59cd09_Out_0_Vector4 = _WhiteTint;
            float _Property_1db02a57d46e4e5e9ec746360323bb47_Out_0_Float = _Thershold;
            float3 _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3;
            Unity_Modulo_float3(IN.WorldPos, float3(2, 2, 2), _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3);
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_R_1_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[0];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_G_2_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[1];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_B_3_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[2];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_A_4_Float = 0;
            float2 _Vector2_4791b7b10144496eaa6c6f2dbca6b18a_Out_0_Vector2 = float2(_Split_778c39e26aa14330b1dcbcbaaa9cf8fd_R_1_Float, _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_B_3_Float);
            float _Property_bfc048d6ef324e81a06c399170b16b4d_Out_0_Float = _Scale;
            float _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float;
            Unity_GradientNoise_Deterministic_float(_Vector2_4791b7b10144496eaa6c6f2dbca6b18a_Out_0_Vector2, _Property_bfc048d6ef324e81a06c399170b16b4d_Out_0_Float, _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float);
            float _Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float;
            Unity_Step_float(_Property_1db02a57d46e4e5e9ec746360323bb47_Out_0_Float, _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float, _Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float);
            float4 _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_cbbb60a5dfca48069bb1fbcd0e59cd09_Out_0_Vector4, (_Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float.xxxx), _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4);
            float4 _Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4;
            Unity_Maximum_float4(_Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4, _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4, _Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4);
            float3 _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3;
            Unity_Multiply_float3_float3(IN.ColorPaint, (_Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4.xyz), _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3);
            float3 _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3;
            Unity_Lerp_float3((_SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.xyz), _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3, (_Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float.xxx), _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3);
            float3 _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3;
            Unity_Branch_float3(_Property_5d7ce0c940ef40b49a4225123448caa2_Out_0_Boolean, _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3, _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3, _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3);
            surface.BaseColor = _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3;
            surface.NormalTS = IN.TangentSpaceNormal;
            surface.Emission = float3(0, 0, 0);
            surface.Metallic = float(0);
            surface.Smoothness = float(0);
            surface.Occlusion = float(1);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            output.WorldPos = input.WorldPos;
        output.ColorPaint = input.ColorPaint;
        output.UV = input.UV;
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }
        
        // Render State
        Cull Off
        Blend One Zero
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma exclude_renderers gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma instancing_options renderinglayer
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DYNAMICLIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
        #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
        #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile _ SHADOWS_SHADOWMASK
        #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
        #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        #define VARYINGS_NEED_SHADOW_COORD
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_GBUFFER
        #define _FOG_FRAGMENT 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
            #if defined(LIGHTMAP_ON)
             float2 staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
             float2 dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
             float4 probeOcclusion;
            #endif
             float4 fogFactorAndVertexLight;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             float4 shadowCoord;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
             float3 WorldPos;
             float3 ColorPaint;
             float2 UV;
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
             float3 WorldPos;
             float3 ColorPaint;
             float2 UV;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if defined(LIGHTMAP_ON)
             float2 staticLightmapUV : INTERP0;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
             float2 dynamicLightmapUV : INTERP1;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh : INTERP2;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
             float4 probeOcclusion : INTERP3;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             float4 shadowCoord : INTERP4;
            #endif
             float4 tangentWS : INTERP5;
             float4 fogFactorAndVertexLight : INTERP6;
             float4 packed_positionWS_UVx : INTERP7;
             float4 packed_normalWS_UVy : INTERP8;
             float3 WorldPos : INTERP9;
             float3 ColorPaint : INTERP10;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
            output.probeOcclusion = input.probeOcclusion;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.shadowCoord = input.shadowCoord;
            #endif
            output.tangentWS.xyzw = input.tangentWS;
            output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
            output.packed_positionWS_UVx.xyz = input.positionWS;
            output.packed_positionWS_UVx.w = input.UV.x;
            output.packed_normalWS_UVy.xyz = input.normalWS;
            output.packed_normalWS_UVy.w = input.UV.y;
            output.WorldPos.xyz = input.WorldPos;
            output.ColorPaint.xyz = input.ColorPaint;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
            output.probeOcclusion = input.probeOcclusion;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.shadowCoord = input.shadowCoord;
            #endif
            output.tangentWS = input.tangentWS.xyzw;
            output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
            output.positionWS = input.packed_positionWS_UVx.xyz;
            output.UV.x = input.packed_positionWS_UVx.w;
            output.normalWS = input.packed_normalWS_UVy.xyz;
            output.UV.y = input.packed_normalWS_UVy.w;
            output.WorldPos = input.WorldPos.xyz;
            output.ColorPaint = input.ColorPaint.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _Blend;
        float _BlendMult;
        float _BlendOff;
        float _Thershold;
        float _Scale;
        float4 _WhiteTint;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TerrainDiffuse);
        SAMPLER(sampler_TerrainDiffuse);
        float4 _TerrainDiffuse_TexelSize;
        float4 _TopTint;
        float4 _BottomTint;
        
        // Graph Includes
        #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Modulo_float3(float3 A, float3 B, out float3 Out)
        {
            Out = fmod(A, B);
        }
        
        float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
        {
            float x; Hash_Tchou_2_1_float(p, x);
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
        {
            float2 p = UV * Scale.xy;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Step_float(float Edge, float In, out float Out)
        {
            Out = step(Edge, In);
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Maximum_float4(float4 A, float4 B, out float4 Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
            float3 WorldPos;
            float3 ColorPaint;
            float2 UV;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            float2 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            GetComputeData_float(IN.VertexID, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3);
            description.Position = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.Normal = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            description.Tangent = IN.ObjectSpaceTangent;
            description.WorldPos = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.ColorPaint = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            description.UV = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        output.WorldPos = input.WorldPos;
        output.ColorPaint = input.ColorPaint;
        output.UV = input.UV;
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float3 Emission;
            float Metallic;
            float Smoothness;
            float Occlusion;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Property_5d7ce0c940ef40b49a4225123448caa2_Out_0_Boolean = _Blend;
            UnityTexture2D _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_TerrainDiffuse);
            float2 _GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2;
            GetWorldUV_float(IN.WorldPos, _GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2);
            float4 _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.tex, _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.samplerstate, _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.GetTransformedUV(_GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2) );
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_R_4_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.r;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_G_5_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.g;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_B_6_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.b;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_A_7_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.a;
            float4 _Property_4e2f08a2ec1945c4a61a6673c09f0834_Out_0_Vector4 = _BottomTint;
            float4 _Property_3ab603e217ab4c3089acc1492753ca25_Out_0_Vector4 = _TopTint;
            float _Property_8d79a54a1cb3451e86bd511d107fb8a0_Out_0_Float = _BlendMult;
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_R_1_Float = IN.UV[0];
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_G_2_Float = IN.UV[1];
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_B_3_Float = 0;
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_A_4_Float = 0;
            float _Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float;
            Unity_Multiply_float_float(_Property_8d79a54a1cb3451e86bd511d107fb8a0_Out_0_Float, _Split_dafbc94f76b74c2092e3bf0e915df7ee_G_2_Float, _Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float);
            float _Property_03b460e3b1aa44a78be15ff860775597_Out_0_Float = _BlendOff;
            float _Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float;
            Unity_Add_float(_Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float, _Property_03b460e3b1aa44a78be15ff860775597_Out_0_Float, _Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float);
            float _Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float;
            Unity_Saturate_float(_Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float, _Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float);
            float4 _Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4;
            Unity_Lerp_float4(_Property_4e2f08a2ec1945c4a61a6673c09f0834_Out_0_Vector4, _Property_3ab603e217ab4c3089acc1492753ca25_Out_0_Vector4, (_Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float.xxxx), _Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4);
            float4 _Property_cbbb60a5dfca48069bb1fbcd0e59cd09_Out_0_Vector4 = _WhiteTint;
            float _Property_1db02a57d46e4e5e9ec746360323bb47_Out_0_Float = _Thershold;
            float3 _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3;
            Unity_Modulo_float3(IN.WorldPos, float3(2, 2, 2), _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3);
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_R_1_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[0];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_G_2_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[1];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_B_3_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[2];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_A_4_Float = 0;
            float2 _Vector2_4791b7b10144496eaa6c6f2dbca6b18a_Out_0_Vector2 = float2(_Split_778c39e26aa14330b1dcbcbaaa9cf8fd_R_1_Float, _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_B_3_Float);
            float _Property_bfc048d6ef324e81a06c399170b16b4d_Out_0_Float = _Scale;
            float _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float;
            Unity_GradientNoise_Deterministic_float(_Vector2_4791b7b10144496eaa6c6f2dbca6b18a_Out_0_Vector2, _Property_bfc048d6ef324e81a06c399170b16b4d_Out_0_Float, _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float);
            float _Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float;
            Unity_Step_float(_Property_1db02a57d46e4e5e9ec746360323bb47_Out_0_Float, _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float, _Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float);
            float4 _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_cbbb60a5dfca48069bb1fbcd0e59cd09_Out_0_Vector4, (_Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float.xxxx), _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4);
            float4 _Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4;
            Unity_Maximum_float4(_Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4, _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4, _Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4);
            float3 _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3;
            Unity_Multiply_float3_float3(IN.ColorPaint, (_Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4.xyz), _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3);
            float3 _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3;
            Unity_Lerp_float3((_SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.xyz), _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3, (_Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float.xxx), _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3);
            float3 _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3;
            Unity_Branch_float3(_Property_5d7ce0c940ef40b49a4225123448caa2_Out_0_Boolean, _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3, _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3, _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3);
            surface.BaseColor = _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3;
            surface.NormalTS = IN.TangentSpaceNormal;
            surface.Emission = float3(0, 0, 0);
            surface.Metallic = float(0);
            surface.Smoothness = float(0);
            surface.Occlusion = float(1);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            output.WorldPos = input.WorldPos;
        output.ColorPaint = input.ColorPaint;
        output.UV = input.UV;
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
        
        // Render State
        Cull Off
        ZTest LEqual
        ZWrite On
        ColorMask 0
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_NORMAL_WS
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_SHADOWCASTER
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _Blend;
        float _BlendMult;
        float _BlendOff;
        float _Thershold;
        float _Scale;
        float4 _WhiteTint;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TerrainDiffuse);
        SAMPLER(sampler_TerrainDiffuse);
        float4 _TerrainDiffuse_TexelSize;
        float4 _TopTint;
        float4 _BottomTint;
        
        // Graph Includes
        #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            float2 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            GetComputeData_float(IN.VertexID, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3);
            description.Position = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.Normal = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "MotionVectors"
            Tags
            {
                "LightMode" = "MotionVectors"
            }
        
        // Render State
        Cull Off
        ZTest LEqual
        ZWrite On
        ColorMask RG
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.5
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_MOTION_VECTORS
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _Blend;
        float _BlendMult;
        float _BlendOff;
        float _Thershold;
        float _Scale;
        float4 _WhiteTint;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TerrainDiffuse);
        SAMPLER(sampler_TerrainDiffuse);
        float4 _TerrainDiffuse_TexelSize;
        float4 _TopTint;
        float4 _BottomTint;
        
        // Graph Includes
        #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            float2 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            GetComputeData_float(IN.VertexID, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3);
            description.Position = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/MotionVectorPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
        
        // Render State
        Cull Off
        ZTest LEqual
        ZWrite On
        ColorMask R
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _Blend;
        float _BlendMult;
        float _BlendOff;
        float _Thershold;
        float _Scale;
        float4 _WhiteTint;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TerrainDiffuse);
        SAMPLER(sampler_TerrainDiffuse);
        float4 _TerrainDiffuse_TexelSize;
        float4 _TopTint;
        float4 _BottomTint;
        
        // Graph Includes
        #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            float2 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            GetComputeData_float(IN.VertexID, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3);
            description.Position = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.Normal = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }
        
        // Render State
        Cull Off
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHNORMALS
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv1 : TEXCOORD1;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS;
             float4 tangentWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 tangentWS : INTERP0;
             float3 normalWS : INTERP1;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.tangentWS.xyzw = input.tangentWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.tangentWS = input.tangentWS.xyzw;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _Blend;
        float _BlendMult;
        float _BlendOff;
        float _Thershold;
        float _Scale;
        float4 _WhiteTint;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TerrainDiffuse);
        SAMPLER(sampler_TerrainDiffuse);
        float4 _TerrainDiffuse_TexelSize;
        float4 _TopTint;
        float4 _BottomTint;
        
        // Graph Includes
        #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            float2 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            GetComputeData_float(IN.VertexID, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3);
            description.Position = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.Normal = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 NormalTS;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            surface.NormalTS = IN.TangentSpaceNormal;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma shader_feature _ EDITOR_VISUALIZATION
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #define ATTRIBUTES_NEED_VERTEXID
        #define ATTRIBUTES_NEED_INSTANCEID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD1
        #define VARYINGS_NEED_TEXCOORD2
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_META
        #define _FOG_FRAGMENT 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
             float4 texCoord1;
             float4 texCoord2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
             float3 WorldPos;
             float3 ColorPaint;
             float2 UV;
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldPos;
             float3 ColorPaint;
             float2 UV;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0 : INTERP0;
             float4 texCoord1 : INTERP1;
             float4 texCoord2 : INTERP2;
             float4 packed_WorldPos_UVx : INTERP3;
             float4 packed_ColorPaint_UVy : INTERP4;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            output.texCoord1.xyzw = input.texCoord1;
            output.texCoord2.xyzw = input.texCoord2;
            output.packed_WorldPos_UVx.xyz = input.WorldPos;
            output.packed_WorldPos_UVx.w = input.UV.x;
            output.packed_ColorPaint_UVy.xyz = input.ColorPaint;
            output.packed_ColorPaint_UVy.w = input.UV.y;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            output.texCoord1 = input.texCoord1.xyzw;
            output.texCoord2 = input.texCoord2.xyzw;
            output.WorldPos = input.packed_WorldPos_UVx.xyz;
            output.UV.x = input.packed_WorldPos_UVx.w;
            output.ColorPaint = input.packed_ColorPaint_UVy.xyz;
            output.UV.y = input.packed_ColorPaint_UVy.w;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _Blend;
        float _BlendMult;
        float _BlendOff;
        float _Thershold;
        float _Scale;
        float4 _WhiteTint;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TerrainDiffuse);
        SAMPLER(sampler_TerrainDiffuse);
        float4 _TerrainDiffuse_TexelSize;
        float4 _TopTint;
        float4 _BottomTint;
        
        // Graph Includes
        #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Modulo_float3(float3 A, float3 B, out float3 Out)
        {
            Out = fmod(A, B);
        }
        
        float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
        {
            float x; Hash_Tchou_2_1_float(p, x);
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
        {
            float2 p = UV * Scale.xy;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Step_float(float Edge, float In, out float Out)
        {
            Out = step(Edge, In);
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Maximum_float4(float4 A, float4 B, out float4 Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
            float3 WorldPos;
            float3 ColorPaint;
            float2 UV;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            float2 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            GetComputeData_float(IN.VertexID, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3);
            description.Position = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.Normal = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            description.Tangent = IN.ObjectSpaceTangent;
            description.WorldPos = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.ColorPaint = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            description.UV = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        output.WorldPos = input.WorldPos;
        output.ColorPaint = input.ColorPaint;
        output.UV = input.UV;
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Property_5d7ce0c940ef40b49a4225123448caa2_Out_0_Boolean = _Blend;
            UnityTexture2D _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_TerrainDiffuse);
            float2 _GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2;
            GetWorldUV_float(IN.WorldPos, _GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2);
            float4 _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.tex, _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.samplerstate, _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.GetTransformedUV(_GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2) );
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_R_4_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.r;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_G_5_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.g;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_B_6_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.b;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_A_7_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.a;
            float4 _Property_4e2f08a2ec1945c4a61a6673c09f0834_Out_0_Vector4 = _BottomTint;
            float4 _Property_3ab603e217ab4c3089acc1492753ca25_Out_0_Vector4 = _TopTint;
            float _Property_8d79a54a1cb3451e86bd511d107fb8a0_Out_0_Float = _BlendMult;
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_R_1_Float = IN.UV[0];
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_G_2_Float = IN.UV[1];
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_B_3_Float = 0;
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_A_4_Float = 0;
            float _Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float;
            Unity_Multiply_float_float(_Property_8d79a54a1cb3451e86bd511d107fb8a0_Out_0_Float, _Split_dafbc94f76b74c2092e3bf0e915df7ee_G_2_Float, _Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float);
            float _Property_03b460e3b1aa44a78be15ff860775597_Out_0_Float = _BlendOff;
            float _Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float;
            Unity_Add_float(_Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float, _Property_03b460e3b1aa44a78be15ff860775597_Out_0_Float, _Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float);
            float _Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float;
            Unity_Saturate_float(_Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float, _Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float);
            float4 _Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4;
            Unity_Lerp_float4(_Property_4e2f08a2ec1945c4a61a6673c09f0834_Out_0_Vector4, _Property_3ab603e217ab4c3089acc1492753ca25_Out_0_Vector4, (_Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float.xxxx), _Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4);
            float4 _Property_cbbb60a5dfca48069bb1fbcd0e59cd09_Out_0_Vector4 = _WhiteTint;
            float _Property_1db02a57d46e4e5e9ec746360323bb47_Out_0_Float = _Thershold;
            float3 _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3;
            Unity_Modulo_float3(IN.WorldPos, float3(2, 2, 2), _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3);
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_R_1_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[0];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_G_2_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[1];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_B_3_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[2];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_A_4_Float = 0;
            float2 _Vector2_4791b7b10144496eaa6c6f2dbca6b18a_Out_0_Vector2 = float2(_Split_778c39e26aa14330b1dcbcbaaa9cf8fd_R_1_Float, _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_B_3_Float);
            float _Property_bfc048d6ef324e81a06c399170b16b4d_Out_0_Float = _Scale;
            float _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float;
            Unity_GradientNoise_Deterministic_float(_Vector2_4791b7b10144496eaa6c6f2dbca6b18a_Out_0_Vector2, _Property_bfc048d6ef324e81a06c399170b16b4d_Out_0_Float, _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float);
            float _Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float;
            Unity_Step_float(_Property_1db02a57d46e4e5e9ec746360323bb47_Out_0_Float, _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float, _Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float);
            float4 _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_cbbb60a5dfca48069bb1fbcd0e59cd09_Out_0_Vector4, (_Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float.xxxx), _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4);
            float4 _Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4;
            Unity_Maximum_float4(_Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4, _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4, _Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4);
            float3 _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3;
            Unity_Multiply_float3_float3(IN.ColorPaint, (_Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4.xyz), _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3);
            float3 _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3;
            Unity_Lerp_float3((_SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.xyz), _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3, (_Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float.xxx), _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3);
            float3 _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3;
            Unity_Branch_float3(_Property_5d7ce0c940ef40b49a4225123448caa2_Out_0_Boolean, _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3, _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3, _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3);
            surface.BaseColor = _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3;
            surface.Emission = float3(0, 0, 0);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            output.WorldPos = input.WorldPos;
        output.ColorPaint = input.ColorPaint;
        output.UV = input.UV;
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "SceneSelectionPass"
            Tags
            {
                "LightMode" = "SceneSelectionPass"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENESELECTIONPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _Blend;
        float _BlendMult;
        float _BlendOff;
        float _Thershold;
        float _Scale;
        float4 _WhiteTint;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TerrainDiffuse);
        SAMPLER(sampler_TerrainDiffuse);
        float4 _TerrainDiffuse_TexelSize;
        float4 _TopTint;
        float4 _BottomTint;
        
        // Graph Includes
        #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        // GraphFunctions: <None>
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            float2 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            GetComputeData_float(IN.VertexID, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3);
            description.Position = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.Normal = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ScenePickingPass"
            Tags
            {
                "LightMode" = "Picking"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENEPICKINGPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
             float3 WorldPos;
             float3 ColorPaint;
             float2 UV;
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldPos;
             float3 ColorPaint;
             float2 UV;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 packed_WorldPos_UVx : INTERP0;
             float4 packed_ColorPaint_UVy : INTERP1;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.packed_WorldPos_UVx.xyz = input.WorldPos;
            output.packed_WorldPos_UVx.w = input.UV.x;
            output.packed_ColorPaint_UVy.xyz = input.ColorPaint;
            output.packed_ColorPaint_UVy.w = input.UV.y;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.WorldPos = input.packed_WorldPos_UVx.xyz;
            output.UV.x = input.packed_WorldPos_UVx.w;
            output.ColorPaint = input.packed_ColorPaint_UVy.xyz;
            output.UV.y = input.packed_ColorPaint_UVy.w;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _Blend;
        float _BlendMult;
        float _BlendOff;
        float _Thershold;
        float _Scale;
        float4 _WhiteTint;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TerrainDiffuse);
        SAMPLER(sampler_TerrainDiffuse);
        float4 _TerrainDiffuse_TexelSize;
        float4 _TopTint;
        float4 _BottomTint;
        
        // Graph Includes
        #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Modulo_float3(float3 A, float3 B, out float3 Out)
        {
            Out = fmod(A, B);
        }
        
        float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
        {
            float x; Hash_Tchou_2_1_float(p, x);
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
        {
            float2 p = UV * Scale.xy;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Step_float(float Edge, float In, out float Out)
        {
            Out = step(Edge, In);
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Maximum_float4(float4 A, float4 B, out float4 Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
            float3 WorldPos;
            float3 ColorPaint;
            float2 UV;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            float2 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            GetComputeData_float(IN.VertexID, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3);
            description.Position = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.Normal = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            description.Tangent = IN.ObjectSpaceTangent;
            description.WorldPos = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.ColorPaint = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            description.UV = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        output.WorldPos = input.WorldPos;
        output.ColorPaint = input.ColorPaint;
        output.UV = input.UV;
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Property_5d7ce0c940ef40b49a4225123448caa2_Out_0_Boolean = _Blend;
            UnityTexture2D _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_TerrainDiffuse);
            float2 _GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2;
            GetWorldUV_float(IN.WorldPos, _GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2);
            float4 _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.tex, _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.samplerstate, _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.GetTransformedUV(_GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2) );
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_R_4_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.r;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_G_5_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.g;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_B_6_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.b;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_A_7_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.a;
            float4 _Property_4e2f08a2ec1945c4a61a6673c09f0834_Out_0_Vector4 = _BottomTint;
            float4 _Property_3ab603e217ab4c3089acc1492753ca25_Out_0_Vector4 = _TopTint;
            float _Property_8d79a54a1cb3451e86bd511d107fb8a0_Out_0_Float = _BlendMult;
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_R_1_Float = IN.UV[0];
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_G_2_Float = IN.UV[1];
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_B_3_Float = 0;
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_A_4_Float = 0;
            float _Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float;
            Unity_Multiply_float_float(_Property_8d79a54a1cb3451e86bd511d107fb8a0_Out_0_Float, _Split_dafbc94f76b74c2092e3bf0e915df7ee_G_2_Float, _Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float);
            float _Property_03b460e3b1aa44a78be15ff860775597_Out_0_Float = _BlendOff;
            float _Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float;
            Unity_Add_float(_Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float, _Property_03b460e3b1aa44a78be15ff860775597_Out_0_Float, _Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float);
            float _Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float;
            Unity_Saturate_float(_Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float, _Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float);
            float4 _Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4;
            Unity_Lerp_float4(_Property_4e2f08a2ec1945c4a61a6673c09f0834_Out_0_Vector4, _Property_3ab603e217ab4c3089acc1492753ca25_Out_0_Vector4, (_Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float.xxxx), _Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4);
            float4 _Property_cbbb60a5dfca48069bb1fbcd0e59cd09_Out_0_Vector4 = _WhiteTint;
            float _Property_1db02a57d46e4e5e9ec746360323bb47_Out_0_Float = _Thershold;
            float3 _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3;
            Unity_Modulo_float3(IN.WorldPos, float3(2, 2, 2), _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3);
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_R_1_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[0];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_G_2_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[1];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_B_3_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[2];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_A_4_Float = 0;
            float2 _Vector2_4791b7b10144496eaa6c6f2dbca6b18a_Out_0_Vector2 = float2(_Split_778c39e26aa14330b1dcbcbaaa9cf8fd_R_1_Float, _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_B_3_Float);
            float _Property_bfc048d6ef324e81a06c399170b16b4d_Out_0_Float = _Scale;
            float _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float;
            Unity_GradientNoise_Deterministic_float(_Vector2_4791b7b10144496eaa6c6f2dbca6b18a_Out_0_Vector2, _Property_bfc048d6ef324e81a06c399170b16b4d_Out_0_Float, _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float);
            float _Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float;
            Unity_Step_float(_Property_1db02a57d46e4e5e9ec746360323bb47_Out_0_Float, _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float, _Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float);
            float4 _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_cbbb60a5dfca48069bb1fbcd0e59cd09_Out_0_Vector4, (_Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float.xxxx), _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4);
            float4 _Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4;
            Unity_Maximum_float4(_Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4, _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4, _Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4);
            float3 _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3;
            Unity_Multiply_float3_float3(IN.ColorPaint, (_Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4.xyz), _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3);
            float3 _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3;
            Unity_Lerp_float3((_SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.xyz), _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3, (_Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float.xxx), _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3);
            float3 _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3;
            Unity_Branch_float3(_Property_5d7ce0c940ef40b49a4225123448caa2_Out_0_Boolean, _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3, _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3, _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3);
            surface.BaseColor = _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            output.WorldPos = input.WorldPos;
        output.ColorPaint = input.ColorPaint;
        output.UV = input.UV;
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "Universal 2D"
            Tags
            {
                "LightMode" = "Universal2D"
            }
        
        // Render State
        Cull Off
        Blend One Zero
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_2D
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
             float3 WorldPos;
             float3 ColorPaint;
             float2 UV;
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldPos;
             float3 ColorPaint;
             float2 UV;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 packed_WorldPos_UVx : INTERP0;
             float4 packed_ColorPaint_UVy : INTERP1;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.packed_WorldPos_UVx.xyz = input.WorldPos;
            output.packed_WorldPos_UVx.w = input.UV.x;
            output.packed_ColorPaint_UVy.xyz = input.ColorPaint;
            output.packed_ColorPaint_UVy.w = input.UV.y;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.WorldPos = input.packed_WorldPos_UVx.xyz;
            output.UV.x = input.packed_WorldPos_UVx.w;
            output.ColorPaint = input.packed_ColorPaint_UVy.xyz;
            output.UV.y = input.packed_ColorPaint_UVy.w;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _Blend;
        float _BlendMult;
        float _BlendOff;
        float _Thershold;
        float _Scale;
        float4 _WhiteTint;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TerrainDiffuse);
        SAMPLER(sampler_TerrainDiffuse);
        float4 _TerrainDiffuse_TexelSize;
        float4 _TopTint;
        float4 _BottomTint;
        
        // Graph Includes
        #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Modulo_float3(float3 A, float3 B, out float3 Out)
        {
            Out = fmod(A, B);
        }
        
        float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
        {
            float x; Hash_Tchou_2_1_float(p, x);
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
        {
            float2 p = UV * Scale.xy;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Step_float(float Edge, float In, out float Out)
        {
            Out = step(Edge, In);
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Maximum_float4(float4 A, float4 B, out float4 Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
            float3 WorldPos;
            float3 ColorPaint;
            float2 UV;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            float2 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            GetComputeData_float(IN.VertexID, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3);
            description.Position = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.Normal = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            description.Tangent = IN.ObjectSpaceTangent;
            description.WorldPos = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.ColorPaint = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            description.UV = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        output.WorldPos = input.WorldPos;
        output.ColorPaint = input.ColorPaint;
        output.UV = input.UV;
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Property_5d7ce0c940ef40b49a4225123448caa2_Out_0_Boolean = _Blend;
            UnityTexture2D _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_TerrainDiffuse);
            float2 _GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2;
            GetWorldUV_float(IN.WorldPos, _GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2);
            float4 _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.tex, _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.samplerstate, _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.GetTransformedUV(_GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2) );
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_R_4_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.r;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_G_5_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.g;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_B_6_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.b;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_A_7_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.a;
            float4 _Property_4e2f08a2ec1945c4a61a6673c09f0834_Out_0_Vector4 = _BottomTint;
            float4 _Property_3ab603e217ab4c3089acc1492753ca25_Out_0_Vector4 = _TopTint;
            float _Property_8d79a54a1cb3451e86bd511d107fb8a0_Out_0_Float = _BlendMult;
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_R_1_Float = IN.UV[0];
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_G_2_Float = IN.UV[1];
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_B_3_Float = 0;
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_A_4_Float = 0;
            float _Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float;
            Unity_Multiply_float_float(_Property_8d79a54a1cb3451e86bd511d107fb8a0_Out_0_Float, _Split_dafbc94f76b74c2092e3bf0e915df7ee_G_2_Float, _Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float);
            float _Property_03b460e3b1aa44a78be15ff860775597_Out_0_Float = _BlendOff;
            float _Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float;
            Unity_Add_float(_Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float, _Property_03b460e3b1aa44a78be15ff860775597_Out_0_Float, _Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float);
            float _Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float;
            Unity_Saturate_float(_Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float, _Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float);
            float4 _Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4;
            Unity_Lerp_float4(_Property_4e2f08a2ec1945c4a61a6673c09f0834_Out_0_Vector4, _Property_3ab603e217ab4c3089acc1492753ca25_Out_0_Vector4, (_Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float.xxxx), _Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4);
            float4 _Property_cbbb60a5dfca48069bb1fbcd0e59cd09_Out_0_Vector4 = _WhiteTint;
            float _Property_1db02a57d46e4e5e9ec746360323bb47_Out_0_Float = _Thershold;
            float3 _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3;
            Unity_Modulo_float3(IN.WorldPos, float3(2, 2, 2), _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3);
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_R_1_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[0];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_G_2_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[1];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_B_3_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[2];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_A_4_Float = 0;
            float2 _Vector2_4791b7b10144496eaa6c6f2dbca6b18a_Out_0_Vector2 = float2(_Split_778c39e26aa14330b1dcbcbaaa9cf8fd_R_1_Float, _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_B_3_Float);
            float _Property_bfc048d6ef324e81a06c399170b16b4d_Out_0_Float = _Scale;
            float _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float;
            Unity_GradientNoise_Deterministic_float(_Vector2_4791b7b10144496eaa6c6f2dbca6b18a_Out_0_Vector2, _Property_bfc048d6ef324e81a06c399170b16b4d_Out_0_Float, _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float);
            float _Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float;
            Unity_Step_float(_Property_1db02a57d46e4e5e9ec746360323bb47_Out_0_Float, _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float, _Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float);
            float4 _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_cbbb60a5dfca48069bb1fbcd0e59cd09_Out_0_Vector4, (_Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float.xxxx), _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4);
            float4 _Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4;
            Unity_Maximum_float4(_Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4, _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4, _Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4);
            float3 _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3;
            Unity_Multiply_float3_float3(IN.ColorPaint, (_Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4.xyz), _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3);
            float3 _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3;
            Unity_Lerp_float3((_SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.xyz), _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3, (_Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float.xxx), _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3);
            float3 _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3;
            Unity_Branch_float3(_Property_5d7ce0c940ef40b49a4225123448caa2_Out_0_Boolean, _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3, _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3, _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3);
            surface.BaseColor = _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            output.WorldPos = input.WorldPos;
        output.ColorPaint = input.ColorPaint;
        output.UV = input.UV;
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "MySpecialPass"
            Tags { "LightMode"="MyCustomPass" }
        
        // Render State
        Cull Off
        Blend One Zero
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma instancing_options renderinglayer
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DYNAMICLIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
        #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
        #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
        #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
        #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile _ SHADOWS_SHADOWMASK
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ _LIGHT_LAYERS
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma multi_compile_fragment _ _LIGHT_COOKIES
        #pragma multi_compile _ _FORWARD_PLUS
        #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
        // GraphKeywords: <None>
        
        // Defines
        
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        #define VARYINGS_NEED_SHADOW_COORD
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_FORWARD
        #define _FOG_FRAGMENT 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
            #if defined(LIGHTMAP_ON)
             float2 staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
             float2 dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
             float4 probeOcclusion;
            #endif
             float4 fogFactorAndVertexLight;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             float4 shadowCoord;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
             float3 WorldPos;
             float3 ColorPaint;
             float2 UV;
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
             float3 WorldPos;
             float3 ColorPaint;
             float2 UV;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if defined(LIGHTMAP_ON)
             float2 staticLightmapUV : INTERP0;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
             float2 dynamicLightmapUV : INTERP1;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh : INTERP2;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
             float4 probeOcclusion : INTERP3;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
             float4 shadowCoord : INTERP4;
            #endif
             float4 tangentWS : INTERP5;
             float4 fogFactorAndVertexLight : INTERP6;
             float4 packed_positionWS_UVx : INTERP7;
             float4 packed_normalWS_UVy : INTERP8;
             float3 WorldPos : INTERP9;
             float3 ColorPaint : INTERP10;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
            output.probeOcclusion = input.probeOcclusion;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.shadowCoord = input.shadowCoord;
            #endif
            output.tangentWS.xyzw = input.tangentWS;
            output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
            output.packed_positionWS_UVx.xyz = input.positionWS;
            output.packed_positionWS_UVx.w = input.UV.x;
            output.packed_normalWS_UVy.xyz = input.normalWS;
            output.packed_normalWS_UVy.w = input.UV.y;
            output.WorldPos.xyz = input.WorldPos;
            output.ColorPaint.xyz = input.ColorPaint;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.staticLightmapUV = input.staticLightmapUV;
            #endif
            #if defined(DYNAMICLIGHTMAP_ON)
            output.dynamicLightmapUV = input.dynamicLightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            #if defined(USE_APV_PROBE_OCCLUSION)
            output.probeOcclusion = input.probeOcclusion;
            #endif
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            output.shadowCoord = input.shadowCoord;
            #endif
            output.tangentWS = input.tangentWS.xyzw;
            output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
            output.positionWS = input.packed_positionWS_UVx.xyz;
            output.UV.x = input.packed_positionWS_UVx.w;
            output.normalWS = input.packed_normalWS_UVy.xyz;
            output.UV.y = input.packed_normalWS_UVy.w;
            output.WorldPos = input.WorldPos.xyz;
            output.ColorPaint = input.ColorPaint.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float _Blend;
        float _BlendMult;
        float _BlendOff;
        float _Thershold;
        float _Scale;
        float4 _WhiteTint;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TerrainDiffuse);
        SAMPLER(sampler_TerrainDiffuse);
        float4 _TerrainDiffuse_TexelSize;
        float4 _TopTint;
        float4 _BottomTint;
        
        // Graph Includes
        #include_with_pragmas "Assets/_KKUBUL/Grass/Grass.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Modulo_float3(float3 A, float3 B, out float3 Out)
        {
            Out = fmod(A, B);
        }
        
        float2 Unity_GradientNoise_Deterministic_Dir_float(float2 p)
        {
            float x; Hash_Tchou_2_1_float(p, x);
            return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
        }
        
        void Unity_GradientNoise_Deterministic_float (float2 UV, float3 Scale, out float Out)
        {
            float2 p = UV * Scale.xy;
            float2 ip = floor(p);
            float2 fp = frac(p);
            float d00 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip), fp);
            float d01 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
            float d10 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
            float d11 = dot(Unity_GradientNoise_Deterministic_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
            fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
            Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
        }
        
        void Unity_Step_float(float Edge, float In, out float Out)
        {
            Out = step(Edge, In);
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Maximum_float4(float4 A, float4 B, out float4 Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
            float3 WorldPos;
            float3 ColorPaint;
            float2 UV;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            float2 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            float3 _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            GetComputeData_float(IN.VertexID, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2, _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3);
            description.Position = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.Normal = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_normal_3_Vector3;
            description.Tangent = IN.ObjectSpaceTangent;
            description.WorldPos = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_worldPos_2_Vector3;
            description.ColorPaint = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_col_5_Vector3;
            description.UV = _GetComputeDataCustomFunction_15cbf119bf1843f794b04dc31eec8ec2_uv_4_Vector2;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        output.WorldPos = input.WorldPos;
        output.ColorPaint = input.ColorPaint;
        output.UV = input.UV;
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float3 Emission;
            float Metallic;
            float Smoothness;
            float Occlusion;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float _Property_5d7ce0c940ef40b49a4225123448caa2_Out_0_Boolean = _Blend;
            UnityTexture2D _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_TerrainDiffuse);
            float2 _GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2;
            GetWorldUV_float(IN.WorldPos, _GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2);
            float4 _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.tex, _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.samplerstate, _Property_c91c7b3426df482eb9b6efcf475bd7b0_Out_0_Texture2D.GetTransformedUV(_GetWorldUVCustomFunction_bdeebcf3afad4153bf22fc7cc82c72dd_worldUV_0_Vector2) );
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_R_4_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.r;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_G_5_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.g;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_B_6_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.b;
            float _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_A_7_Float = _SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.a;
            float4 _Property_4e2f08a2ec1945c4a61a6673c09f0834_Out_0_Vector4 = _BottomTint;
            float4 _Property_3ab603e217ab4c3089acc1492753ca25_Out_0_Vector4 = _TopTint;
            float _Property_8d79a54a1cb3451e86bd511d107fb8a0_Out_0_Float = _BlendMult;
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_R_1_Float = IN.UV[0];
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_G_2_Float = IN.UV[1];
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_B_3_Float = 0;
            float _Split_dafbc94f76b74c2092e3bf0e915df7ee_A_4_Float = 0;
            float _Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float;
            Unity_Multiply_float_float(_Property_8d79a54a1cb3451e86bd511d107fb8a0_Out_0_Float, _Split_dafbc94f76b74c2092e3bf0e915df7ee_G_2_Float, _Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float);
            float _Property_03b460e3b1aa44a78be15ff860775597_Out_0_Float = _BlendOff;
            float _Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float;
            Unity_Add_float(_Multiply_7d7cd75b16a3458aa3274cd16c805d50_Out_2_Float, _Property_03b460e3b1aa44a78be15ff860775597_Out_0_Float, _Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float);
            float _Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float;
            Unity_Saturate_float(_Add_05b03d36153e42d59f0a9d9920da5054_Out_2_Float, _Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float);
            float4 _Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4;
            Unity_Lerp_float4(_Property_4e2f08a2ec1945c4a61a6673c09f0834_Out_0_Vector4, _Property_3ab603e217ab4c3089acc1492753ca25_Out_0_Vector4, (_Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float.xxxx), _Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4);
            float4 _Property_cbbb60a5dfca48069bb1fbcd0e59cd09_Out_0_Vector4 = _WhiteTint;
            float _Property_1db02a57d46e4e5e9ec746360323bb47_Out_0_Float = _Thershold;
            float3 _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3;
            Unity_Modulo_float3(IN.WorldPos, float3(2, 2, 2), _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3);
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_R_1_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[0];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_G_2_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[1];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_B_3_Float = _Modulo_4168990ec00d4072b0612c834b1d6f0a_Out_2_Vector3[2];
            float _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_A_4_Float = 0;
            float2 _Vector2_4791b7b10144496eaa6c6f2dbca6b18a_Out_0_Vector2 = float2(_Split_778c39e26aa14330b1dcbcbaaa9cf8fd_R_1_Float, _Split_778c39e26aa14330b1dcbcbaaa9cf8fd_B_3_Float);
            float _Property_bfc048d6ef324e81a06c399170b16b4d_Out_0_Float = _Scale;
            float _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float;
            Unity_GradientNoise_Deterministic_float(_Vector2_4791b7b10144496eaa6c6f2dbca6b18a_Out_0_Vector2, _Property_bfc048d6ef324e81a06c399170b16b4d_Out_0_Float, _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float);
            float _Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float;
            Unity_Step_float(_Property_1db02a57d46e4e5e9ec746360323bb47_Out_0_Float, _GradientNoise_731158a3a35e43d7adb095119a092201_Out_2_Float, _Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float);
            float4 _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_cbbb60a5dfca48069bb1fbcd0e59cd09_Out_0_Vector4, (_Step_de0ab90050af49eaba0b9017aa0b384d_Out_2_Float.xxxx), _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4);
            float4 _Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4;
            Unity_Maximum_float4(_Lerp_15f052d5e01b45818927d368bf836863_Out_3_Vector4, _Multiply_caa34fc16d4841b0b34b3f35c5aeab70_Out_2_Vector4, _Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4);
            float3 _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3;
            Unity_Multiply_float3_float3(IN.ColorPaint, (_Maximum_4ef9dcaa4dd54f97ae3cd074a3d5a07b_Out_2_Vector4.xyz), _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3);
            float3 _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3;
            Unity_Lerp_float3((_SampleTexture2D_1c8c664def3f45a5837bd13acef1d16b_RGBA_0_Vector4.xyz), _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3, (_Saturate_aa0e93e097f048aeb1085c08c3d6b87a_Out_1_Float.xxx), _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3);
            float3 _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3;
            Unity_Branch_float3(_Property_5d7ce0c940ef40b49a4225123448caa2_Out_0_Boolean, _Lerp_e3cc90bcb3a44aee959bf0f0ca32b8e7_Out_3_Vector3, _Multiply_0aa6747f00a3447f82b45b9e0cc9a545_Out_2_Vector3, _Branch_47e7e6f6c79c4f24bfc111c162c35f9f_Out_3_Vector3);
            surface.BaseColor = float3(1,0,0);
            surface.NormalTS = IN.TangentSpaceNormal;
            surface.Emission = float3(0, 0, 0);
            surface.Metallic = float(0);
            surface.Smoothness = float(0);
            surface.Occlusion = float(1);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            output.WorldPos = input.WorldPos;
        output.ColorPaint = input.ColorPaint;
        output.UV = input.UV;
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
    }
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
    CustomEditorForRenderPipeline "UnityEditor.ShaderGraphLitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
    FallBack "Hidden/Shader Graph/FallbackError"
}