Shader "FX/Grass_URP"
{
    Properties
    {
        [Header(Shading)]
        _TopColor("Top Color", Color) = (1,1,1,1)
        _BottomColor("Bottom Color", Color) = (1,1,1,1)
        _TranslucentGain("Translucent Gain", Range(0,1)) = 0.5

        [Header(Grass Shape)]
        _BladeWidth("Blade Width", Float) = 0.05
        _BladeWidthRandom("Blade Width Random", Float) = 0.02
        _BladeHeight("Blade Height", Float) = 0.5
        _BladeHeightRandom("Blade Height Random", Float) = 0.3
        _BendRotationRandom("Bend Rotation Random", Range(0, 1)) = 0.2
        _BladeForward("Blade Forward Amount", Float) = 0.38
        _BladeCurve("Blade Curvature Amount", Range(1, 4)) = 2

        [Header(Grass Density)]
        _TessellationUniform("Grass Density", Range(1, 64)) = 1

        [Header(Wind)]
        _WindStrength("Wind Strength", Float) = 1
        _WindFrequency("Wind Frequency", Vector) = (0.05, 0.05, 0, 0)
        _WindDistortionMap("Wind Distortion Map", 2D) = "white" {}

        [Header(Fallback)]
        [HideInInspector] _MainTex ("Base (RGB)", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    #define BLADE_SEGMENTS 16

    // SRP Batcher Compatibility
    CBUFFER_START(UnityPerMaterial)
        float4 _TopColor;
        float4 _BottomColor;
        float _TranslucentGain;
        float _BladeWidth;
        float _BladeWidthRandom;
        float _BladeHeight;
        float _BladeHeightRandom;
        float _BendRotationRandom;
        float _BladeForward;
        float _BladeCurve;
        float _TessellationUniform;
        float _WindStrength;
        float2 _WindFrequency;
        float4 _WindDistortionMap_ST;
    CBUFFER_END

    TEXTURE2D(_WindDistortionMap);
    SAMPLER(sampler_WindDistortionMap);

    // --- Helper Functions ---

    float rand(float3 co)
    {
        return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
    }

    float3x3 AngleAxis3x3(float angle, float3 axis)
    {
        float c, s;
        sincos(angle, s, c);

        float t = 1 - c;
        float x = axis.x;
        float y = axis.y;
        float z = axis.z;

        return float3x3(
            t * x * x + c, t * x * y - s * z, t * x * z + s * y,
            t * x * y + s * z, t * y * y + c, t * y * z - s * x,
            t * x * z - s * y, t * y * z + s * x, t * z * z + c
        );
    }

    // --- Structs ---

    struct Attributes
    {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float4 tangent : TANGENT;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float3 positionWS : TEXCOORD0;
        float3 normalWS : TEXCOORD1;
        float2 uv : TEXCOORD3;
    };

    struct TessellationFactors
    {
        float edge[3] : SV_TessFactor;
        float inside : SV_InsideTessFactor;
    };

    // --- Vertex / Tessellation Logic ---

    Attributes vert(Attributes v)
    {
        return v;
    }

    Attributes tessVert(Attributes v)
    {
        // Tessellation happens in Object Space, so we just pass data through
        return v;
    }

    TessellationFactors patchConstantFunction(InputPatch<Attributes, 3> patch)
    {
        TessellationFactors f;
        f.edge[0] = _TessellationUniform;
        f.edge[1] = _TessellationUniform;
        f.edge[2] = _TessellationUniform;
        f.inside = _TessellationUniform;
        return f;
    }

    [domain("tri")]
    [outputcontrolpoints(3)]
    [outputtopology("triangle_cw")]
    [partitioning("integer")]
    [patchconstantfunc("patchConstantFunction")]
    Attributes hull(InputPatch<Attributes, 3> patch, uint id : SV_OutputControlPointID)
    {
        return patch[id];
    }


    [domain("tri")]
    Attributes domain(TessellationFactors factors, OutputPatch<Attributes, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
    {
        Attributes v;

        #define DOMAIN_INTERPOLATE(fieldName) v.fieldName = \
            patch[0].fieldName * barycentricCoordinates.x + \
            patch[1].fieldName * barycentricCoordinates.y + \
            patch[2].fieldName * barycentricCoordinates.z;

        DOMAIN_INTERPOLATE(vertex)
        DOMAIN_INTERPOLATE(normal)
        DOMAIN_INTERPOLATE(tangent)

        return tessVert(v);
    }


    // --- Geometry Logic ---

    Varyings VertexOutput(float3 posOS, float2 uv, float3 normalOS)
    {
        Varyings o;
        o.positionCS = TransformObjectToHClip(posOS);
        o.positionWS = TransformObjectToWorld(posOS);
        o.normalWS = TransformObjectToWorldNormal(normalOS);
        o.uv = uv;
        return o;
    }

    Varyings GenerateGrassVertex(float3 vertexPosition, float width, float height, float forward, float2 uv, float3x3 transformMatrix)
    {
        float3 tangentPoint = float3(width, forward, height);
        float3 tangentNormal = normalize(float3(0, -1, forward));
        float3 localNormal = mul(transformMatrix, tangentNormal);
        float3 localPosition = vertexPosition + mul(transformMatrix, tangentPoint);
        return VertexOutput(localPosition, uv, localNormal);
    }

    [maxvertexcount(BLADE_SEGMENTS * 2 + 1)]
    void geo(triangle Attributes IN[3], inout TriangleStream<Varyings> triStream)
    {
        float3 pos = IN[0].vertex.xyz;
        float3 vNormal = IN[0].normal;
        float4 vTangent = IN[0].tangent;
        float3 vBinormal = cross(vNormal, vTangent.xyz) * vTangent.w;

        float3x3 tangentToLocal = float3x3(
            vTangent.x, vBinormal.x, vNormal.x,
            vTangent.y, vBinormal.y, vNormal.y,
            vTangent.z, vBinormal.z, vNormal.z
        );

        float2 windSample = float2(0, 0); // Default no wind
        if(_WindStrength > 0.01)
        {
            float2 wind_uv = pos.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindFrequency * _Time.y;
            windSample = (SAMPLE_TEXTURE2D_LOD(_WindDistortionMap, sampler_WindDistortionMap, wind_uv, 0).xy * 2 - 1) * _WindStrength;
        }

        float3 windAxis = normalize(float3(windSample.x, windSample.y, 0));
        float3x3 windRotation = AngleAxis3x3(3.14 * length(windSample), windAxis);

        float3x3 facingRotationMatrix = AngleAxis3x3(rand(pos) * 6.28, float3(0, 0, 1));
        float3x3 bendRotationMatrix = AngleAxis3x3(rand(pos.zzx) * _BendRotationRandom * 3.14 * 0.5, float3(-1, 0, 0));
        
        float3x3 transformationMatrix = mul(mul(mul(tangentToLocal, windRotation), facingRotationMatrix), bendRotationMatrix);
        float3x3 transformationMatrixFacing = mul(tangentToLocal, facingRotationMatrix);

        float height = (rand(pos.zyx) * 2 - 1) * _BladeHeightRandom + _BladeHeight;
        float width = (rand(pos.xzy) * 2 - 1) * _BladeWidthRandom + _BladeWidth;
        float forward = rand(pos.yyz) * _BladeForward;

        for (int i = 0; i < BLADE_SEGMENTS; i++)
        {
            float t = i / (float)BLADE_SEGMENTS;
            float segmentHeight = height * t;
            float segmentWidth = width * (1 - t);
            float segmentForward = pow(t, _BladeCurve) * forward;
            float3x3 transformMatrix = i == 0 ? transformationMatrixFacing : transformationMatrix;

            triStream.Append(GenerateGrassVertex(pos, segmentWidth, segmentHeight, segmentForward, float2(0, t), transformMatrix));
            triStream.Append(GenerateGrassVertex(pos, -segmentWidth, segmentHeight, segmentForward, float2(1, t), transformMatrix));
        }
        triStream.Append(GenerateGrassVertex(pos, 0, height, forward, float2(0.5, 1), transformationMatrix));
    }

    ENDHLSL

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.6
            #pragma vertex vert
            //#pragma hull hull
            //#pragma domain domain
            #pragma geometry geo
            #pragma fragment frag
            
            // URP Keywords for Lighting & Shadows
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            float4 frag(Varyings i, float facing : VFACE) : SV_Target
            {
                // Ensure normal is facing the camera
                float3 normalWS = facing > 0 ? i.normalWS : -i.normalWS;
                normalWS = normalize(normalWS);

                // Calculate Shadow Coordinate
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);

                // Get Main Light
                Light mainLight = GetMainLight(shadowCoord);
                
                // NdotL
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                // Add Translucent effect (fake subsurface scattering)
                float lightingStrength = saturate(NdotL + _TranslucentGain);

                // Light Color & Attenuation
                float3 lightColor = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);
                
                // Ambient Lighting (SH)
                float3 ambient = SampleSH(normalWS);

                // Final Color Combination
                float3 finalLight = lightColor * lightingStrength + ambient;
                float4 col = lerp(_BottomColor, _TopColor, i.uv.y);
                
                col.rgb *= finalLight;

                // Apply Fog
                float fogFactor = ComputeFogFactor(i.positionCS.z);
                col.rgb = MixFog(col.rgb, fogFactor);

                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma target 4.6
            #pragma vertex vert
            #pragma hull hull
            #pragma domain domain
            #pragma geometry geo
            #pragma fragment fragShadow
            
            #pragma multi_compile_shadowcaster

            // Shadow Caster fragment doesn't need complex lighting
            half4 fragShadow(Varyings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}