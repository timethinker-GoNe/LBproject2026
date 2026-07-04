using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Grass Settings", menuName = "Utility/GrassSettings")]
public class SO_GrassSettings : ScriptableObject
{
    public ComputeShader shaderToUse;
    public Material materialToUse;
    // Blade
    [Header("Blade")]
    [Range(0, 5)] public float grassRandomHeightMin = 0.0f;
    [Range(0, 5)] public float grassRandomHeightMax = 0.0f;
    [Range(0, 1)] public float bladeRadius = 0.2f;
    [Range(0, 1)] public float bladeForwardAmount = 0.38f;
    [Range(1, 5)] public float bladeCurveAmount = 2;

    [Range(0, 1)] public float bottomWidth = 0.1f;

    [Range(0.01f, 1)] public float MinWidth = 0.01f;
    [Range(0.01f, 1)] public float MinHeight = 0.01f;

    [Range(0.01f, 1)] public float MaxWidth = 1f;
    [Range(0.01f, 1)] public float MaxHeight = 1f;


    // Wind
    [Header("Wind")]
    public float windSpeed = 10;
    public float windStrength = 0.05f;

    [Header("Grass")]
    [Range(1, 8)] public int allowedBladesPerVertex = 4;
    [Range(1, 5)] public int allowedSegmentsPerBlade = 4;
    // Interactor

    [Header("Interactor Strength")]
    public float affectStrength = 1;

    // Material
    [Header("Material")]
    public Color topTint = new Color(1, 1, 1);
    public Color bottomTint = new Color(0, 0, 1);

    // Color Variation
    [Header("Color Variation")]
    [Range(0f, 1f)] 
    public float colorVariationProbability = 0.15f;
    public Color variationBottomTint = new Color(0.8f, 0.9f, 0.6f);
    public Color variationTopTint = new Color(0.9f, 1.0f, 0.7f);
    [Range(1f, 100f)]
    public float colorVariationNoiseScale = 50f;
    [Range(0f, 1f)]
    public float colorVariationThreshold = 0.7f;

    // Brightness Variation
    [Header("Brightness Variation (Stepped Noise)")]
    [Range(0.01f, 100f)]
    public float brightnessVariationScale = 30f;
    [Range(2, 20)]
    public int brightnessVariationSteps = 4;
    [Range(0f, 1f)]
    public float brightnessVariationStrength = 0.3f;

    [Header("LOD/ Culling")]
    public bool drawBounds;
    public float minFadeDistance = 40;

    public float maxDrawDistance = 125;


    public int cullingTreeDepth = 4;

    [Header("Other")]
    public UnityEngine.Rendering.ShadowCastingMode castShadow;


}
