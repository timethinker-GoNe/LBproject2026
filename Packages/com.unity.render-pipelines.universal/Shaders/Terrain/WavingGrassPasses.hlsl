#ifndef UNIVERSAL_WAVING_GRASS_PASSES_INCLUDED
#define UNIVERSAL_WAVING_GRASS_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"

struct GrassVertexInput
{
    float4 vertex       : POSITION;
    float3 normal       : NORMAL;
    float4 tangent      : TANGENT;
    half4 color         : COLOR;
    float2 texcoord     : TEXCOORD0;
    float2 lightmapUV   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct GrassVertexOutput
{
    float2 uv                       : TEXCOORD0;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

    float4 posWSShininess           : TEXCOORD2;    // xyz: posWS, w: Shininess * 128

    half3  normal                   : TEXCOORD3;
    half3 viewDir                   : TEXCOORD4;

    half4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD6;
#endif
    half4 color                     : TEXCOORD7;

#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion           : TEXCOORD8;
#endif

    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(GrassVertexOutput input, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.posWSShininess.xyz;

    inputData.positionCS = input.clipPos;

    half3 viewDirWS = input.viewDir;
    viewDirWS = SafeNormalize(viewDirWS);

    inputData.normalWS = NormalizeNormalPerPixel(input.normal);
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

#if defined(_FOG_FRAGMENT)
    float clipZ = input.clipPos.z;
    #if !UNITY_REVERSED_Z
    clipZ = lerp(UNITY_NEAR_CLIP_VALUE, 1, clipZ);    // OpenGL NDC, -1 < z < 1
    #endif
    clipZ *= input.clipPos.w;
    inputData.fogCoord = ComputeFogFactor(clipZ);
#else
    inputData.fogCoord = input.fogFactorAndVertexLight.x;
#endif
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;

#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, NOT_USED, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.vertexSH,
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        input.clipPos.xy,
        input.probeOcclusion,
        inputData.shadowMask);
#else
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
#endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.clipPos);

    #if defined(DEBUG_DISPLAY)
    #if defined(DYNAMICLIGHTMAP_ON)
    inputData.staticLightmapUV = input.lightmapUV;
    #elif defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.lightmapUV;
    #else
    inputData.vertexSH = input.vertexSH;
    #endif
    #if defined(USE_APV_PROBE_OCCLUSION)
    inputData.probeOcclusion = input.probeOcclusion;
    #endif
    #endif
}

void InitializeVertData(GrassVertexInput input, inout GrassVertexOutput vertData)
{
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);

    vertData.uv = input.texcoord;
    vertData.posWSShininess.xyz = vertexInput.positionWS;
    vertData.posWSShininess.w = 32;
    vertData.clipPos = vertexInput.positionCS;

    vertData.viewDir = GetCameraPositionWS() - vertexInput.positionWS;

    vertData.viewDir = SafeNormalize(vertData.viewDir);

    vertData.normal = TransformObjectToWorldNormal(input.normal);

    // We either sample GI from lightmap or SH.
    // Lightmap UV and vertex SH coefficients use the same interpolator ("float2 lightmapUV" for lightmap or "half3 vertexSH" for SH)
    // see DECLARE_LIGHTMAP_OR_SH macro.
    // The following funcions initialize the correct variable with correct data
    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, vertData.lightmapUV);
    OUTPUT_SH4(vertexInput.positionWS, vertData.normal.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), vertData.vertexSH, vertData.probeOcclusion);

    half3 vertexLight = VertexLighting(vertexInput.positionWS, vertData.normal.xyz);
#if defined(_FOG_FRAGMENT)
    half fogFactor = 0;
#else
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
#endif
    vertData.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    vertData.shadowCoord = GetShadowCoord(vertexInput);
#endif
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Grass: appdata_full usage
// color        - .xyz = color, .w = wave scale
// normal       - normal
// tangent.xy   - billboard extrusion
// texcoord     - UV coords
// texcoord1    - 2nd UV coords

half4 CustomTerrainWaveGrass(inout float4 vertex, float waveAmount, half4 color)
{
	float4 _waveXSize = float4(0.012, 0.02, 0.06, 0.024) * _WaveAndDistance.y;
	float4 _waveZSize = float4(0.006, .02, 0.02, 0.05) * _WaveAndDistance.y;
	float4 waveSpeed = float4(0.3, .5, .4, 1.2) * 4;

	float4 _waveXmove = float4(0.012, 0.02, -0.06, 0.048) * 2;
	float4 _waveZmove = float4(0.006, .02, -0.02, 0.1);

	float4 waves;
	waves = vertex.x * _waveXSize;
	waves += vertex.z * _waveZSize;

    // Add in time to model them over time
	waves += _WaveAndDistance.x * waveSpeed;

	float4 s, c;
	waves = frac(waves);
	FastSinCos(waves, s, c);

	s = s * s;

	s = s * s;

	float lighting = dot(s, normalize(float4(1, 1, .4, .2))) * .7;

	s = s * waveAmount;

	float3 waveMove = float3(0, 0, 0);
    
	waveMove.x = dot(s, _waveXmove);
	waveMove.z = dot(s, _waveZmove);

	vertex.xz -= waveMove.xz * _WaveAndDistance.z;

    // apply color animation

    // fix for dx11/etc warning
	half3 waveColor = lerp(half3(0.5, 0.5, 0.5), _WavingTint.rgb, half3(lighting, lighting, lighting));

    // Fade the grass out before detail distance.
    // Saturate because Radeon HD drivers on OS X 10.4.10 don't saturate vertex colors properly.
	float3 offset = vertex.xyz - _CameraPosition.xyz;
	color.a = saturate(2 * (_WaveAndDistance.w - dot(offset, offset)) * _CameraPosition.w);

	return half4(color.rgb, color.a);
}

float GetGrassNoise(float2 posXZ)
{
    // 노이즈의 크기(Scale): 숫자가 작을수록 얼룩이 커집니다. (0.05 ~ 0.5 추천)
	float noiseScale = 0.1;

    // sin, cos를 섞어서 패턴이 반복되지 않는 것처럼 보이게 만듭니다.
	float noise = sin(posXZ.x * noiseScale) + cos(posXZ.y * noiseScale * 0.85);
    
    // -2 ~ 2 범위를 0 ~ 1 범위로 대략 변환
	return frac(noise * 0.5 + 0.5);
}

GrassVertexOutput WavingGrassVert(GrassVertexInput v)
{
    GrassVertexOutput o = (GrassVertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    // MeshGrass v.color.a: 1 on top vertices, 0 on bottom vertices
    // _WaveAndDistance.z == 0 for MeshLit
    float waveAmount = v.color.a * _WaveAndDistance.z;
	o.color = CustomTerrainWaveGrass(v.vertex, waveAmount, v.color);

    InitializeVertData(v, o);

    return o;
}

GrassVertexOutput WavingGrassBillboardVert(GrassVertexInput v)
{
    GrassVertexOutput o = (GrassVertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    
	float3 worldPos = TransformObjectToWorld(v.vertex.xyz);

    // 2. 노이즈 값을 구합니다. (0.0 ~ 1.0)
	float noiseVal = GetGrassNoise(worldPos.xz);

    // 3. 노이즈를 색상 밝기로 변환 (예: 0.8배 ~ 1.2배 사이로 밝기 조절)
    // lerp(최소밝기, 최대밝기, 노이즈값)
	float brightness = lerp(0.7, 1.3, noiseVal);
    
	v.color.rgb = _WavingTint.rgb * brightness;
    TerrainBillboardGrass (v.vertex, v.tangent.xy);
    // wave amount defined by the grass height
	float waveAmount = v.tangent.y * v.color.a;
	o.color = CustomTerrainWaveGrass(v.vertex, waveAmount, v.color);

    InitializeVertData(v, o);

    return o;
}

inline void InitializeSimpleLitSurfaceData(GrassVertexOutput input, out SurfaceData outSurfaceData)
{
    half4 diffuseAlpha = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex));
    half3 diffuse = diffuseAlpha.rgb * input.color.rgb;

    half alpha = diffuseAlpha.a * input.color.a;
    alpha = AlphaDiscard(alpha, _Cutoff);

    outSurfaceData = (SurfaceData)0;
    outSurfaceData.alpha = alpha;
    outSurfaceData.albedo = diffuse;
    outSurfaceData.metallic = 0.0; // unused
    outSurfaceData.specular = 0.1;// SampleSpecularSmoothness(uv, diffuseAlpha.a, _SpecColor, TEXTURE2D_ARGS(_SpecGlossMap, sampler_SpecGlossMap));
    outSurfaceData.smoothness = input.posWSShininess.w;
    outSurfaceData.normalTS = 0.0; // unused
    outSurfaceData.occlusion = 1.0;
    outSurfaceData.emission = 0.0;
}

// Used for StandardSimpleLighting shader
#ifdef TERRAIN_GBUFFER
FragmentOutput LitPassFragmentGrass(GrassVertexOutput input)
#else
half4 LitPassFragmentGrass(GrassVertexOutput input) : SV_Target
#endif
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceData surfaceData;
    InitializeSimpleLitSurfaceData(input, surfaceData);

    InputData inputData;
    InitializeInputData(input, inputData);
    SETUP_DEBUG_TEXTURE_DATA_FOR_TEX(inputData, input.uv, _MainTex);

#ifdef TERRAIN_GBUFFER
    half4 color = half4(inputData.bakedGI * surfaceData.albedo + surfaceData.emission, surfaceData.alpha);
    return SurfaceDataToGbuffer(surfaceData, inputData, color.rgb, kLightingSimpleLit);
#else
    half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return half4(color.rgb, OutputAlpha(surfaceData.alpha, IsSurfaceTypeTransparent(_Surface)));
#endif
};

struct GrassVertexDepthOnlyInput
{
    float4 vertex       : POSITION;
    float4 tangent      : TANGENT;
    half4 color         : COLOR;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct GrassVertexDepthOnlyOutput
{
    float2 uv           : TEXCOORD0;
    half4 color         : TEXCOORD1;
    float4 clipPos      : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeVertData(GrassVertexDepthOnlyInput input, inout GrassVertexDepthOnlyOutput vertData)
{
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);

    vertData.uv = input.texcoord;
    vertData.clipPos = vertexInput.positionCS;
}

GrassVertexDepthOnlyOutput DepthOnlyVertex(GrassVertexDepthOnlyInput v)
{
    GrassVertexDepthOnlyOutput o = (GrassVertexDepthOnlyOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    // MeshGrass v.color.a: 1 on top vertices, 0 on bottom vertices
    // _WaveAndDistance.z == 0 for MeshLit
    float waveAmount = v.color.a * _WaveAndDistance.z;
    o.color = TerrainWaveGrass(v.vertex, waveAmount, v.color);

    InitializeVertData(v, o);

    return o;
}

GrassVertexDepthOnlyOutput DepthOnlyBillboardVertex(GrassVertexDepthOnlyInput v)
{
    GrassVertexDepthOnlyOutput o = (GrassVertexDepthOnlyOutput) 0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    TerrainBillboardGrass (v.vertex, v.tangent.xy);

    // wave amount defined by the grass height
    float waveAmount = v.tangent.y;
    o.color = TerrainWaveGrass (v.vertex, waveAmount, v.color);

    InitializeVertData(v, o);

    return o;
}

half4 DepthOnlyFragment(GrassVertexDepthOnlyOutput input) : SV_TARGET
{
    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex)).a, input.color, _Cutoff);
    return input.clipPos.z;
}
#endif
