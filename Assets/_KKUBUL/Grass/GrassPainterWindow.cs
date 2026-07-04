#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GrassPainterWindow : EditorWindow
{

    // main tabs
    readonly string[] mainTabBarStrings = { "Paint/Edit", "Flood", "Generate", "General Settings" };

    int mainTab_current;
    Vector2 scrollPos;

    bool paintModeActive;

    readonly string[] toolbarStrings = { "Add", "Remove", "Edit", "Reproject", "Auto Fill" };

    readonly string[] toolbarStringsEdit = { "Edit Colors", "Edit Length/Width", "Both" };



    Vector3 hitPos;
    Vector3 hitNormal;

    [SerializeField]
    SO_GrassToolSettings toolSettings;


    // options
    [HideInInspector]
    public int toolbarInt = 0;
    [HideInInspector]
    public int toolbarIntEdit = 0;

    public Material mat;

    public List<GrassData> grassData = new();

    [HideInInspector]
    int grassAmount = 0;


    Ray ray;
    // raycast vars
    [HideInInspector]
    public Vector3 hitPosGizmo;
    Vector3 mousePos;

    RaycastHit[] terrainHit;
    private int flowTimer;
    private Vector3 lastPosition = Vector3.zero;

    [SerializeField]
    GameObject grassObject;


    GrassComputeScript grassCompute;

    NativeArray<float> sizes;
    NativeArray<float> cumulativeSizes;
    NativeArray<float> total;

    Vector3 cachedPos;

    bool showLayers;
    
    // Improved painting
    private bool isPainting = false;
    private float paintTimer = 0f;
    private float paintInterval = 0.05f; // Paint every 50ms for smooth painting

    [MenuItem("Tools/Grass Tool")]
    static void Init()
    {
        Debug.Log("init");
        // Get existing open window or if none, make a new one:
        GrassPainterWindow window = (GrassPainterWindow)EditorWindow.GetWindow(typeof(GrassPainterWindow), false, "Grass Tool", true);
        var icon = EditorGUIUtility.FindTexture("tree_icon");
        window.titleContent = new GUIContent("Grass Tool", icon);
        window.LoadOrCreateToolSettings();
        window.Show();
    }

    void OnEnable()
    {
        LoadOrCreateToolSettings();
#if UNITY_EDITOR
        Editor_OnEnable();
#endif
	}

	void LoadOrCreateToolSettings()
    {
        if (toolSettings == null)
        {
            toolSettings = (SO_GrassToolSettings)AssetDatabase.LoadAssetAtPath("Assets/_KKUBUL/Grass/Settings/grassToolSettings.asset", typeof(SO_GrassToolSettings));
        }
        
        if (toolSettings == null)
        {
            Debug.Log("Creating new grassToolSettings");
            toolSettings = CreateInstance<SO_GrassToolSettings>();
            
            if (!AssetDatabase.IsValidFolder("Assets/_KKUBUL/Grass/Settings"))
            {
                AssetDatabase.CreateFolder("Assets/_KKUBUL/Grass", "Settings");
            }
            
            AssetDatabase.CreateAsset(toolSettings, "Assets/_KKUBUL/Grass/Settings/grassToolSettings.asset");
            toolSettings.CreateNewLayers();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        if (toolSettings == null)
        {
            EditorGUILayout.HelpBox("Tool Settings not initialized. Please reopen the window from Tools > Grass Tool", MessageType.Error);
            EditorGUILayout.EndScrollView();
            return;
        }
        
        if (GUILayout.Button("Manual Update"))
        {
            grassCompute.Reset();
        }
        grassObject = (GameObject)EditorGUILayout.ObjectField("Grass Compute Object", grassObject, typeof(GameObject), true);




        if (grassObject == null)
        {
            grassObject = FindObjectOfType<GrassComputeScript>()?.gameObject;

        }


        if (grassObject != null)
        {
            grassCompute = grassObject.GetComponent<GrassComputeScript>();
            grassCompute.currentPresets = (SO_GrassSettings)EditorGUILayout.ObjectField("Grass Settings Object", grassCompute.currentPresets, typeof(SO_GrassSettings), false);



            if (grassCompute.SetGrassPaintedDataList.Count > 0)
            {
                grassData = grassCompute.SetGrassPaintedDataList;
                grassAmount = grassData.Count;
            }
            else
            {
                grassData.Clear();
            }


            if (grassCompute.currentPresets == null)
            {
                EditorGUILayout.LabelField("No Grass Settings Set, create or assign one first. \n Create > Utility> Grass Settings", GUILayout.Height(150));
                EditorGUILayout.EndScrollView();
                return;
            }
        }
        else
        {

            if (GUILayout.Button("Create Grass Object"))
            {
                if (EditorUtility.DisplayDialog("Create a new Grass Object?",
                   "No Grass Object Found, create a new Object?", "Yes", "No"))
                {
                    CreateNewGrassObject();
                }


            }
            EditorGUILayout.LabelField("No Grass System Holder found, create a new one", EditorStyles.label);
            EditorGUILayout.EndScrollView();
            return;

        }
        EditorGUILayout.Separator();

        EditorGUILayout.BeginHorizontal();
        grassCompute.currentPresets.materialToUse = (Material)EditorGUILayout.ObjectField("Grass Material", grassCompute.currentPresets.materialToUse, typeof(Material), false);

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Total Grass Amount: " + grassAmount.ToString(), EditorStyles.label);
        EditorGUILayout.BeginHorizontal();
        mainTab_current = GUILayout.Toolbar(mainTab_current, mainTabBarStrings, GUILayout.Height(30));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();
        EditorGUILayout.Separator();

        switch (mainTab_current)
        {
            case 0: //paint
                ShowPaintPanel();
                break;
            case 1: // flood
                ShowFloodPanel();
                break;

            case 2: // generate
                ShowGeneratePanel();
                break;

            case 3: //settings
                ShowMainSettingsPanel();
                break;
        }

        if (GUILayout.Button("Clear Grass"))
        {
            if (EditorUtility.DisplayDialog("Clear All Grass?",
               "Are you sure you want to clear the grass?", "Clear", "Don't Clear"))
            {
                ClearMesh();
            }
        }

        EditorGUILayout.EndScrollView();

        EditorUtility.SetDirty(toolSettings);
        EditorUtility.SetDirty(grassCompute.currentPresets);
    }

    void ShowFloodPanel()
    {

        EditorGUILayout.LabelField("Flood Options", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Flood Length/Width"))
        {
            FloodLengthAndWidth();
        }
        if (GUILayout.Button("Flood Colors"))
        {
            FloodColor();
        }
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.Separator();

        EditorGUILayout.LabelField("Width and Length ", EditorStyles.boldLabel);
        toolSettings.sizeWidth = EditorGUILayout.Slider("Grass Width", toolSettings.sizeWidth, 0.01f, 2f);
        toolSettings.sizeLength = EditorGUILayout.Slider("Grass Length", toolSettings.sizeLength, 0.01f, 2f);
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
        toolSettings.AdjustedColor = EditorGUILayout.ColorField("Brush Color", toolSettings.AdjustedColor);
        EditorGUILayout.LabelField("Random Color Variation", EditorStyles.boldLabel);
        toolSettings.rangeR = EditorGUILayout.Slider("Red", toolSettings.rangeR, 0f, 1f);
        toolSettings.rangeG = EditorGUILayout.Slider("Green", toolSettings.rangeG, 0f, 1f);
        toolSettings.rangeB = EditorGUILayout.Slider("Blue", toolSettings.rangeB, 0f, 1f);
    }

    void ShowGeneratePanel()
    {
        EditorGUILayout.HelpBox("Generate grass automatically on selected meshes/terrains or all paintable surfaces.", MessageType.Info);
        EditorGUILayout.Separator();
        
        toolSettings.grassAmountToGenerate = EditorGUILayout.IntField("Grass Place Max Amount", toolSettings.grassAmountToGenerate);
        toolSettings.generationDensity = EditorGUILayout.Slider("Grass Place Density", toolSettings.generationDensity, 0.01f, 1f);

        EditorGUILayout.Separator();
        LayerMask tempMask0 = EditorGUILayout.MaskField("Blocking Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(toolSettings.paintBlockMask), InternalEditorUtility.layers);
        toolSettings.paintBlockMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask0);


        toolSettings.VertexColorSettings = (SO_GrassToolSettings.VertexColorSetting)EditorGUILayout.EnumPopup("Block On vertex Colors", toolSettings.VertexColorSettings);
        toolSettings.VertexFade = (SO_GrassToolSettings.VertexColorSetting)EditorGUILayout.EnumPopup("Fade on Vertex Colors", toolSettings.VertexFade);

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Width and Length ", EditorStyles.boldLabel);
        toolSettings.sizeWidth = EditorGUILayout.Slider("Grass Width", toolSettings.sizeWidth, 0.01f, 2f);
        toolSettings.sizeLength = EditorGUILayout.Slider("Grass Length", toolSettings.sizeLength, 0.01f, 2f);
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
        toolSettings.AdjustedColor = EditorGUILayout.ColorField("Brush Color", toolSettings.AdjustedColor);
        EditorGUILayout.LabelField("Random Color Variation", EditorStyles.boldLabel);
        toolSettings.rangeR = EditorGUILayout.Slider("Red", toolSettings.rangeR, 0f, 1f);
        toolSettings.rangeG = EditorGUILayout.Slider("Green", toolSettings.rangeG, 0f, 1f);
        toolSettings.rangeB = EditorGUILayout.Slider("Blue", toolSettings.rangeB, 0f, 1f);
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Normal Limit", EditorStyles.boldLabel);
        toolSettings.normalLimit = EditorGUILayout.Slider("Normal Limit", toolSettings.normalLimit, 0f, 1f);

        EditorGUILayout.Separator();
        showLayers = EditorGUILayout.Foldout(showLayers, "Layer Settings(Cutoff Value, Fade Height Toggle");

        if (showLayers)
        {
            for (int i = 0; i < toolSettings.layerBlocking.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                toolSettings.layerBlocking[i] = EditorGUILayout.Slider(i.ToString(), toolSettings.layerBlocking[i], 0f, 1f);
                toolSettings.layerFading[i] = EditorGUILayout.Toggle(toolSettings.layerFading[i]);
                EditorGUILayout.EndHorizontal();
            }
        }


        GameObject[] selection = Selection.gameObjects;

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Quick Generate", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Fill All Paintable Surfaces (Auto)", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Auto-Fill All Surfaces?",
               "This will automatically generate grass on all objects with Paint Mask layers. This may take a while for large scenes.", "Generate", "Cancel"))
            {
                Undo.RegisterCompleteObjectUndo(this, "Auto-Generated Grass on All Surfaces");
                GenerateOnAllPaintableSurfaces();
            }
        }
        
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Manual Selection", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Positions From Selected Mesh"))
        {
            if (selection == null || selection.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select one or more GameObjects with MeshFilter or Terrain components.", "OK");
            }
            else
            {
                Undo.RegisterCompleteObjectUndo(this, "Add new Positions from Mesh(es)");
                for (int i = 0; i < selection.Length; i++)
                {
                    GeneratePositions(selection[i]);
                }
            }
        }
        
        if (GUILayout.Button("Regenerate on Selected Mesh (Clears Current)"))
        {
            if (selection == null || selection.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select one or more GameObjects with MeshFilter or Terrain components.", "OK");
                return;
            }
            else
            {
                if (EditorUtility.DisplayDialog("Clear and Regenerate?",
                   "This will clear all existing grass and regenerate on selected meshes.", "Regenerate", "Cancel"))
                {
                    ClearMesh();
                    Undo.RegisterCompleteObjectUndo(this, "Regenerated Positions on Mesh(es)");
                    for (int i = 0; i < selection.Length; i++)
                    {
                        GeneratePositions(selection[i]);
                    }
                }
            }
        }

        EditorGUILayout.Separator();
    }

    void ShowPaintPanel()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Paint Mode:", EditorStyles.boldLabel);
        paintModeActive = EditorGUILayout.Toggle(paintModeActive);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Hit Settings", EditorStyles.boldLabel);
        LayerMask tempMask = EditorGUILayout.MaskField("Hit Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(toolSettings.hitMask), InternalEditorUtility.layers);
        toolSettings.hitMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
        LayerMask tempMask2 = EditorGUILayout.MaskField("Painting Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(toolSettings.paintMask), InternalEditorUtility.layers);
        toolSettings.paintMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask2);
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Paint Status (Right-Mouse Button to paint)", EditorStyles.boldLabel);
        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel);
        toolSettings.brushSize = EditorGUILayout.Slider("Brush Size", toolSettings.brushSize, 0.1f, 50f);

        if (toolbarInt == 0 || toolbarInt == 4) // Add or Auto Fill
        {
            toolSettings.normalLimit = EditorGUILayout.Slider("Normal Limit", toolSettings.normalLimit, 0f, 1f);
            toolSettings.density = EditorGUILayout.Slider("Density", toolSettings.density, 0.1f, 10f);
        }
        
        if (toolbarInt == 4) // Auto Fill
        {
            EditorGUILayout.HelpBox("Auto Fill Mode: Click to automatically fill the brush area with evenly distributed grass.", MessageType.Info);
        }

        if (toolbarInt == 2)
        {

            toolbarIntEdit = GUILayout.Toolbar(toolbarIntEdit, toolbarStringsEdit);
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Soft Falloff Settings", EditorStyles.boldLabel);
            toolSettings.brushFalloffSize = EditorGUILayout.Slider("Brush Falloff Size", toolSettings.brushFalloffSize, 0.01f, 1f);
            toolSettings.Flow = EditorGUILayout.Slider("Brush Flow", toolSettings.Flow, 0.1f, 10f);
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Adjust Width and Length Gradually", EditorStyles.boldLabel);
            toolSettings.adjustWidth = EditorGUILayout.Slider("Grass Width Adjustment", toolSettings.adjustWidth, -1f, 1f);
            toolSettings.adjustLength = EditorGUILayout.Slider("Grass Length Adjustment", toolSettings.adjustLength, -1f, 1f);

            toolSettings.adjustWidthMax = EditorGUILayout.Slider("Grass Width Adjustment Max Clamp", toolSettings.adjustWidthMax, 0.01f, 3f);
            toolSettings.adjustHeightMax = EditorGUILayout.Slider("Grass Length Adjustment Max Clamp", toolSettings.adjustHeightMax, 0.01f, 3f);
            EditorGUILayout.Separator();
        }

        if (toolbarInt == 0 || toolbarInt == 2)
        {
            EditorGUILayout.Separator();

            if (toolbarInt == 0)
            {
                EditorGUILayout.LabelField("Width and Length ", EditorStyles.boldLabel);
                toolSettings.sizeWidth = EditorGUILayout.Slider("Grass Width", toolSettings.sizeWidth, 0.01f, 2f);
                toolSettings.sizeLength = EditorGUILayout.Slider("Grass Length", toolSettings.sizeLength, 0.01f, 2f);
            }



            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
            toolSettings.AdjustedColor = EditorGUILayout.ColorField("Brush Color", toolSettings.AdjustedColor);
            EditorGUILayout.LabelField("Random Color Variation", EditorStyles.boldLabel);
            toolSettings.rangeR = EditorGUILayout.Slider("Red", toolSettings.rangeR, 0f, 1f);
            toolSettings.rangeG = EditorGUILayout.Slider("Green", toolSettings.rangeG, 0f, 1f);
            toolSettings.rangeB = EditorGUILayout.Slider("Blue", toolSettings.rangeB, 0f, 1f);
        }

        if (toolbarInt == 3)
        {
            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Reprojection Y Offset", EditorStyles.boldLabel);

            toolSettings.reprojectOffset = EditorGUILayout.FloatField(toolSettings.reprojectOffset);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.Separator();
    }

    void ShowMainSettingsPanel()
    {
        EditorGUILayout.LabelField("Blade Mix/Max Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        grassCompute.currentPresets.MinWidth = EditorGUILayout.FloatField(grassCompute.currentPresets.MinWidth);
        grassCompute.currentPresets.MaxWidth = EditorGUILayout.FloatField(grassCompute.currentPresets.MaxWidth);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.MinMaxSlider("Blade Width Min/Max", ref grassCompute.currentPresets.MinWidth, ref grassCompute.currentPresets.MaxWidth, 0.01f, 1f);
        EditorGUILayout.BeginHorizontal();
        grassCompute.currentPresets.MinHeight = EditorGUILayout.FloatField(grassCompute.currentPresets.MinHeight);
        grassCompute.currentPresets.MaxHeight = EditorGUILayout.FloatField(grassCompute.currentPresets.MaxHeight);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.MinMaxSlider("Blade Height Min/Max", ref grassCompute.currentPresets.MinHeight, ref grassCompute.currentPresets.MaxHeight, 0.01f, 1f);

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Random Height", EditorStyles.boldLabel);
        grassCompute.currentPresets.grassRandomHeightMin = EditorGUILayout.FloatField("Min Random:", grassCompute.currentPresets.grassRandomHeightMin);
        grassCompute.currentPresets.grassRandomHeightMax = EditorGUILayout.FloatField("Max Random:", grassCompute.currentPresets.grassRandomHeightMax);

        EditorGUILayout.MinMaxSlider("Random Grass Height", ref grassCompute.currentPresets.grassRandomHeightMin, ref grassCompute.currentPresets.grassRandomHeightMax, -5f, 5f);
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Blade Shape Settings", EditorStyles.boldLabel);
        grassCompute.currentPresets.bladeRadius = EditorGUILayout.Slider("Blade Radius", grassCompute.currentPresets.bladeRadius, 0f, 2f);
        grassCompute.currentPresets.bladeForwardAmount = EditorGUILayout.Slider("Blade Forward", grassCompute.currentPresets.bladeForwardAmount, 0f, 2f);
        grassCompute.currentPresets.bladeCurveAmount = EditorGUILayout.Slider("Blade Curve", grassCompute.currentPresets.bladeCurveAmount, 0f, 2f);
        grassCompute.currentPresets.bottomWidth = EditorGUILayout.Slider("Bottom Width", grassCompute.currentPresets.bottomWidth, 0f, 2f);
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Blade Amount Settings", EditorStyles.boldLabel);
        grassCompute.currentPresets.allowedBladesPerVertex = EditorGUILayout.IntSlider("Allowed Blades Per Vertex", grassCompute.currentPresets.allowedBladesPerVertex, 1, 10);
        grassCompute.currentPresets.allowedSegmentsPerBlade = EditorGUILayout.IntSlider("Allowed Segments Per Blade", grassCompute.currentPresets.allowedSegmentsPerBlade, 1, 4);

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Wind Settings", EditorStyles.boldLabel);
        grassCompute.currentPresets.windSpeed = EditorGUILayout.Slider("Wind Speed", grassCompute.currentPresets.windSpeed, -2f, 2f);
        grassCompute.currentPresets.windStrength = EditorGUILayout.Slider("Wind Strength", grassCompute.currentPresets.windStrength, -2f, 2f);

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Tinting Settings", EditorStyles.boldLabel);
        grassCompute.currentPresets.topTint = EditorGUILayout.ColorField("Top Tint", grassCompute.currentPresets.topTint);
        grassCompute.currentPresets.bottomTint = EditorGUILayout.ColorField("Bottom Tint", grassCompute.currentPresets.bottomTint);

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("LOD/Culling Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Show Culling Bounds:", EditorStyles.boldLabel);
        grassCompute.currentPresets.drawBounds = EditorGUILayout.Toggle(grassCompute.currentPresets.drawBounds);
        EditorGUILayout.EndHorizontal();
        grassCompute.currentPresets.minFadeDistance = EditorGUILayout.FloatField("Min Fade Distance", grassCompute.currentPresets.minFadeDistance);
        grassCompute.currentPresets.maxDrawDistance = EditorGUILayout.FloatField("Max Draw Distance", grassCompute.currentPresets.maxDrawDistance);
        grassCompute.currentPresets.cullingTreeDepth = EditorGUILayout.IntField("Culling Tree Depth", grassCompute.currentPresets.cullingTreeDepth);


        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Other Settings", EditorStyles.boldLabel);
        grassCompute.currentPresets.affectStrength = EditorGUILayout.FloatField("Interactor Bend Strength", grassCompute.currentPresets.affectStrength);
        grassCompute.currentPresets.castShadow = (UnityEngine.Rendering.ShadowCastingMode)EditorGUILayout.EnumPopup("Shadow Settings", grassCompute.currentPresets.castShadow);


    }

    void CreateNewGrassObject()
    {
        grassObject = new GameObject();
        grassObject.name = "Grass System - Holder";
        grassCompute = grassObject.AddComponent<GrassComputeScript>();

        // setup object
        grassData = new List<GrassData>();
        grassCompute.SetGrassPaintedDataList = grassData;
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (this.hasFocus && paintModeActive)
        {
            DrawHandles();
        }
    }

    RaycastHit[] m_Results = new RaycastHit[1];
    // draw the painter handles
    void DrawHandles()
    {
        int hits = Physics.RaycastNonAlloc(ray, m_Results, 200f, toolSettings.hitMask.value);
        for (int i = 0; i < hits; i++)
        {
            hitPos = m_Results[i].point;
            hitNormal = m_Results[i].normal;
        }

        //base
        Color discColor = Color.green;
        Color discColor2 = new(0, 0.5f, 0, 0.4f);
        switch (toolbarInt)
        {
            case 1:
                discColor = Color.red;
                discColor2 = new Color(0.5f, 0f, 0f, 0.4f);
                break;
            case 2:
                discColor = Color.yellow;
                discColor2 = new Color(0.5f, 0.5f, 0f, 0.4f);

                Handles.color = discColor;
                Handles.DrawWireDisc(hitPos, hitNormal, (toolSettings.brushFalloffSize * toolSettings.brushSize));
                Handles.color = discColor2;
                Handles.DrawSolidDisc(hitPos, hitNormal, (toolSettings.brushFalloffSize * toolSettings.brushSize));

                break;
            case 3:
                discColor = Color.cyan;
                discColor2 = new Color(0, 0.5f, 0.5f, 0.4f);
                break;
            case 4: // Auto Fill
                discColor = Color.magenta;
                discColor2 = new Color(0.5f, 0f, 0.5f, 0.4f);
                break;
        }


        Handles.color = discColor;
        Handles.DrawWireDisc(hitPos, hitNormal, toolSettings.brushSize);
        Handles.color = discColor2;
        Handles.DrawSolidDisc(hitPos, hitNormal, toolSettings.brushSize);
        
        // Draw brush center point for better feedback
        Handles.color = new Color(1, 1, 1, 0.8f);
        Handles.DrawSolidDisc(hitPos, hitNormal, 0.1f);

        if (hitPos != cachedPos)
        {
            SceneView.RepaintAll();
            cachedPos = hitPos;
        }
    }

#if UNITY_EDITOR
    public void HandleUndo()
    {
        if (grassCompute != null)
        {

            SceneView.RepaintAll();
            grassCompute.Reset();
        }
    }

    private void Editor_OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        SceneView.duringSceneGui += this.OnScene;
        Undo.undoRedoPerformed += this.HandleUndo;
        terrainHit = new RaycastHit[1];
    }

    void RemoveDelegates()
    {
        // When the window is destroyed, remove the delegate
        // so that it will no longer do any drawing.
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui -= this.OnScene;
        Undo.undoRedoPerformed -= this.HandleUndo;
    }

    void OnDisable()
    {
        RemoveDelegates();
    }

    void OnDestroy()
    {
        RemoveDelegates();
    }

    public void ClearMesh()
    {
        Undo.RegisterCompleteObjectUndo(this, "Cleared Grass");
        grassAmount = 0;
        grassData.Clear();
        grassCompute.SetGrassPaintedDataList = grassData;
        grassCompute.Reset();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    void GenerateOnAllPaintableSurfaces()
    {
        // Find all objects on paintable layers
        List<GameObject> paintableObjects = new List<GameObject>();
        
        // Get all MeshFilters and Terrains in the scene
        MeshFilter[] allMeshFilters = FindObjectsOfType<MeshFilter>();
        Terrain[] allTerrains = FindObjectsOfType<Terrain>();
        
        // Filter by paint mask
        foreach (MeshFilter mf in allMeshFilters)
        {
            if ((toolSettings.paintMask.value & (1 << mf.gameObject.layer)) > 0)
            {
                paintableObjects.Add(mf.gameObject);
            }
        }
        
        foreach (Terrain terrain in allTerrains)
        {
            if ((toolSettings.paintMask.value & (1 << terrain.gameObject.layer)) > 0)
            {
                paintableObjects.Add(terrain.gameObject);
            }
        }
        
        if (paintableObjects.Count == 0)
        {
            EditorUtility.DisplayDialog("No Paintable Objects Found", 
                "No objects found on Paint Mask layers. Please check your Paint Mask settings in the Generate tab.", "OK");
            return;
        }
        
        // Show progress bar and generate
        int totalObjects = paintableObjects.Count;
        for (int i = 0; i < totalObjects; i++)
        {
            float progress = (float)i / totalObjects;
            EditorUtility.DisplayProgressBar("Auto-Generating Grass", 
                $"Processing {paintableObjects[i].name} ({i + 1}/{totalObjects})", progress);
            
            GeneratePositions(paintableObjects[i]);
        }
        
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("Generation Complete", 
            $"Successfully generated grass on {totalObjects} object(s). Total grass count: {grassData.Count}", "OK");
    }

    public void GeneratePositions(GameObject selection)
    {
        // mesh
        if (selection.TryGetComponent(out MeshFilter sourceMesh))
        {

            CalcAreas(sourceMesh.sharedMesh);
            Matrix4x4 localToWorld = sourceMesh.transform.localToWorldMatrix;

            var oTriangles = sourceMesh.sharedMesh.triangles;
            var oVertices = sourceMesh.sharedMesh.vertices;
            var oColors = sourceMesh.sharedMesh.colors;
            var oNormals = sourceMesh.sharedMesh.normals;

            var meshTriangles = new NativeArray<int>(oTriangles.Length, Allocator.Temp);
            var meshVertices = new NativeArray<Vector4>(oVertices.Length, Allocator.Temp);
            var meshColors = new NativeArray<Color>(oVertices.Length, Allocator.Temp);
            var meshNormals = new NativeArray<Vector3>(oNormals.Length, Allocator.Temp);
            for (int i = 0; i < meshTriangles.Length; i++)
            {
                meshTriangles[i] = oTriangles[i];
            }

            for (int i = 0; i < meshVertices.Length; i++)
            {
                meshVertices[i] = oVertices[i];
                meshNormals[i] = oNormals[i];
                if (oColors.Length == 0)
                {
                    meshColors[i] = Color.black;
                }
                else
                {
                    meshColors[i] = oColors[i];
                }

            }

            var point = new NativeArray<Vector3>(1, Allocator.Temp);

            var normals = new NativeArray<Vector3>(1, Allocator.Temp);

            var lengthWidth = new NativeArray<float>(1, Allocator.Temp);
            var job = new MyJob
            {
                CumulativeSizes = cumulativeSizes,
                MeshColors = meshColors,
                MeshTriangles = meshTriangles,
                MeshVertices = meshVertices,
                MeshNormals = meshNormals,
                Total = total,
                Sizes = sizes,
                Point = point,
                Normals = normals,
                VertexColorSettings = toolSettings.VertexColorSettings,
                VertexFade = toolSettings.VertexFade,
                LengthWidth = lengthWidth,
            };


            Bounds bounds = sourceMesh.sharedMesh.bounds;

            Vector3 meshSize = new Vector3(
                bounds.size.x * sourceMesh.transform.lossyScale.x,
                bounds.size.y * sourceMesh.transform.lossyScale.y,
                bounds.size.z * sourceMesh.transform.lossyScale.z
            );
            meshSize += Vector3.one;

            float meshVolume = meshSize.x * meshSize.y * meshSize.z;
            int numPoints = Mathf.Min(Mathf.FloorToInt(meshVolume * toolSettings.generationDensity), toolSettings.grassAmountToGenerate);


            for (int j = 0; j < numPoints; j++)
            {
                job.Execute();
                GrassData newData = new();
                Vector3 newPoint = point[0];
                newData.position = localToWorld.MultiplyPoint3x4(newPoint);

                Collider[] cols = Physics.OverlapBox(newData.position, Vector3.one * 0.2f, Quaternion.identity, toolSettings.paintBlockMask);
                if (cols.Length > 0)
                {
                    newPoint = Vector3.zero;
                }
                // check normal limit

                Vector3 worldNormal = selection.transform.TransformDirection(normals[0]);

                if (worldNormal.y <= (1 + toolSettings.normalLimit) && worldNormal.y >= (1 - toolSettings.normalLimit))
                {

                    if (newPoint != Vector3.zero)
                    {
                        newData.color = GetRandomColor();
                        newData.length = new Vector2(toolSettings.sizeWidth, toolSettings.sizeLength) * lengthWidth[0];
                        newData.normal = worldNormal;
                        grassData.Add(newData);
                    }
                }



            }

            sizes.Dispose();
            cumulativeSizes.Dispose();
            total.Dispose();
            meshColors.Dispose();
            meshTriangles.Dispose();
            meshVertices.Dispose();
            meshNormals.Dispose();
            point.Dispose();
            lengthWidth.Dispose();

            RebuildMesh();
        }

        else if (selection.TryGetComponent(out Terrain terrain))
        {
            // terrainmesh



            float meshVolume = terrain.terrainData.size.x * terrain.terrainData.size.y * terrain.terrainData.size.z;
            int numPoints = Mathf.Min(Mathf.FloorToInt(meshVolume * toolSettings.generationDensity), toolSettings.grassAmountToGenerate);


            for (int j = 0; j < numPoints; j++)
            {
                Matrix4x4 localToWorld = terrain.transform.localToWorldMatrix;
                GrassData newData = new();
                Vector3 newPoint = Vector3.zero;
                Vector3 newNormal = Vector3.zero;
                float[,,] maps = new float[0, 0, 0];
                GetRandomPointOnTerrain(localToWorld, ref maps, terrain, terrain.terrainData.size, ref newPoint, ref newNormal);
                newData.position = newPoint;

                Collider[] cols = Physics.OverlapBox(newData.position, Vector3.one * 0.2f, Quaternion.identity, toolSettings.paintBlockMask);
                if (cols.Length > 0)
                {
                    newPoint = Vector3.zero;
                }


                float getFadeMap = 0;
                // check map layers
                for (int i = 0; i < maps.Length; i++)
                {
                    getFadeMap += System.Convert.ToInt32(toolSettings.layerFading[i]) * maps[0, 0, i];
                    if (maps[0, 0, i] > toolSettings.layerBlocking[i])
                    {
                        newPoint = Vector3.zero;
                    }
                }

                if (newNormal.y <= (1 + toolSettings.normalLimit) && newNormal.y >= (1 - toolSettings.normalLimit))
                {
                    float fade = Mathf.Clamp((getFadeMap), 0, 1f);
                    newData.color = GetRandomColor();
                    newData.length = new Vector2(toolSettings.sizeWidth, toolSettings.sizeLength * fade);
                    newData.normal = newNormal;
                    if (newPoint != Vector3.zero)
                    {
                        grassData.Add(newData);
                    }
                }


            }
            RebuildMesh();
        }

    }

    Vector3 GetRandomColor()
    {
        Color newRandomCol = new(toolSettings.AdjustedColor.r + (Random.Range(0, 1.0f) * toolSettings.rangeR), toolSettings.AdjustedColor.g + (Random.Range(0, 1.0f) * toolSettings.rangeG), toolSettings.AdjustedColor.b + (Random.Range(0, 1.0f) * toolSettings.rangeB), 1);
        Vector3 color = new(newRandomCol.r, newRandomCol.g, newRandomCol.b);
        return color;
    }

    void GetRandomPointOnTerrain(Matrix4x4 localToWorld, ref float[,,] maps, Terrain terrain, Vector3 size, ref Vector3 point, ref Vector3 normal)
    {
        point = new Vector3(Random.Range(0, size.x), 0, Random.Range(0, size.z));
        // sample layers wip

        float pointSizeX = (point.x / size.x);
        float pointSizeZ = (point.z / size.z);

        Vector3 newScale2 = new(pointSizeX * terrain.terrainData.alphamapResolution, 0, pointSizeZ * terrain.terrainData.alphamapResolution);
        int terrainx = Mathf.RoundToInt(newScale2.x);
        int terrainz = Mathf.RoundToInt(newScale2.z);

        maps = terrain.terrainData.GetAlphamaps(terrainx, terrainz, 1, 1);
        normal = terrain.terrainData.GetInterpolatedNormal(pointSizeX, pointSizeZ);
        point = localToWorld.MultiplyPoint3x4(point);
        point.y = terrain.SampleHeight(point) + terrain.GetPosition().y;
    }


    public void CalcAreas(Mesh mesh)
    {
        sizes = GetTriSizes(mesh.triangles, mesh.vertices);
        cumulativeSizes = new NativeArray<float>(sizes.Length, Allocator.Temp);
        total = new NativeArray<float>(1, Allocator.Temp);

        for (int i = 0; i < sizes.Length; i++)
        {
            total[0] += sizes[i];
            cumulativeSizes[i] = total[0];
        }
    }

    // Using BurstCompile to compile a Job with burst
    // Set CompileSynchronously to true to make sure that the method will not be compiled asynchronously
    // but on the first schedule
    [BurstCompile(CompileSynchronously = true)]
    private struct MyJob : IJob
    {
        [ReadOnly]
        public NativeArray<float> Sizes;

        [ReadOnly]
        public NativeArray<float> Total;

        [ReadOnly]
        public NativeArray<float> CumulativeSizes;

        [ReadOnly]
        public NativeArray<Color> MeshColors;

        [ReadOnly]
        public NativeArray<Vector4> MeshVertices;

        [ReadOnly]
        public NativeArray<Vector3> MeshNormals;

        [ReadOnly]
        public NativeArray<int> MeshTriangles;

        [WriteOnly]
        public NativeArray<Vector3> Point;

        [WriteOnly]
        public NativeArray<float> LengthWidth;

        [WriteOnly]
        public NativeArray<Vector3> Normals;

        public SO_GrassToolSettings.VertexColorSetting VertexColorSettings;


        public SO_GrassToolSettings.VertexColorSetting VertexFade;

        public void Execute()
        {
            float randomsample = Random.value * Total[0];
            int triIndex = -1;

            for (int i = 0; i < Sizes.Length; i++)
            {
                if (randomsample <= CumulativeSizes[i])
                {
                    triIndex = i;
                    break;
                }
            }
            if (triIndex == -1)
                Debug.LogError("triIndex should never be -1");

            switch (VertexColorSettings)
            {
                case SO_GrassToolSettings.VertexColorSetting.Red:
                    if (MeshColors[MeshTriangles[triIndex * 3]].r > 0.5f)
                    {
                        Point[0] = Vector3.zero;
                        return;
                    }
                    break;
                case SO_GrassToolSettings.VertexColorSetting.Green:
                    if (MeshColors[MeshTriangles[triIndex * 3]].g > 0.5f)
                    {
                        Point[0] = Vector3.zero;
                        return;
                    }
                    break;
                case SO_GrassToolSettings.VertexColorSetting.Blue:
                    if (MeshColors[MeshTriangles[triIndex * 3]].b > 0.5f)
                    {
                        Point[0] = Vector3.zero;
                        return;
                    }
                    break;
            }

            switch (VertexFade)
            {
                case SO_GrassToolSettings.VertexColorSetting.Red:
                    float red = MeshColors[MeshTriangles[triIndex * 3]].r;
                    float red2 = MeshColors[MeshTriangles[triIndex * 3 + 1]].r;
                    float red3 = MeshColors[MeshTriangles[triIndex * 3 + 2]].r;

                    LengthWidth[0] = 1.0f - ((red + red2 + red3) * 0.3f);
                    break;
                case SO_GrassToolSettings.VertexColorSetting.Green:
                    float green = MeshColors[MeshTriangles[triIndex * 3]].g;
                    float green2 = MeshColors[MeshTriangles[triIndex * 3 + 1]].g;
                    float green3 = MeshColors[MeshTriangles[triIndex * 3 + 2]].g;

                    LengthWidth[0] = 1.0f - ((green + green2 + green3) * 0.3f);
                    break;
                case SO_GrassToolSettings.VertexColorSetting.Blue:
                    float blue = MeshColors[MeshTriangles[triIndex * 3]].b;
                    float blue2 = MeshColors[MeshTriangles[triIndex * 3 + 1]].b;
                    float blue3 = MeshColors[MeshTriangles[triIndex * 3 + 2]].b;

                    LengthWidth[0] = 1.0f - ((blue + blue2 + blue3) * 0.3f);
                    break;
                case SO_GrassToolSettings.VertexColorSetting.None:
                    LengthWidth[0] = 1.0f;
                    break;
            }

            Vector3 a = MeshVertices[MeshTriangles[triIndex * 3]];
            Vector3 b = MeshVertices[MeshTriangles[triIndex * 3 + 1]];
            Vector3 c = MeshVertices[MeshTriangles[triIndex * 3 + 2]];

            // Generate random barycentric coordinates
            float r = Random.value;
            float s = Random.value;

            if (r + s >= 1)
            {
                r = 1 - r;
                s = 1 - s;
            }

            Normals[0] = MeshNormals[MeshTriangles[triIndex * 3 + 1]];

            // Turn point back to a Vector3
            Vector3 pointOnMesh = a + r * (b - a) + s * (c - a);

            Point[0] = pointOnMesh;

        }
    }

    public NativeArray<float> GetTriSizes(int[] tris, Vector3[] verts)
    {
        int triCount = tris.Length / 3;
        var sizes = new NativeArray<float>(triCount, Allocator.Temp);
        for (int i = 0; i < triCount; i++)
        {
            sizes[i] = .5f * Vector3.Cross(
                verts[tris[i * 3 + 1]] - verts[tris[i * 3]],
                verts[tris[i * 3 + 2]] - verts[tris[i * 3]]).magnitude;
        }
        return sizes;
    }

    public void FloodColor()
    {
        Undo.RegisterCompleteObjectUndo(this, "Flooded Color");
        for (int i = 0; i < grassData.Count; i++)
        {
            GrassData newData = grassData[i];
            newData.color = GetRandomColor();
            grassData[i] = newData;

        }
        RebuildMesh();
    }

    public void FloodLengthAndWidth()
    {
        Undo.RegisterCompleteObjectUndo(this, "Flooded Length/Width");
        for (int i = 0; i < grassData.Count; i++)
        {
            GrassData newData = grassData[i];
            newData.length = new Vector2(toolSettings.sizeWidth, toolSettings.sizeLength);
            grassData[i] = newData;

        }
        RebuildMesh();
    }

    Ray RandomRay(Vector3 position, Vector3 normal, float radius, float falloff)
    {
        Vector3 a = Vector3.zero;
        Quaternion rotation = Quaternion.LookRotation(normal, Vector3.up);

        var rad = Random.Range(0f, 2 * Mathf.PI);
        a.x = Mathf.Cos(rad);
        a.y = Mathf.Sin(rad);

        float r;

        //In the case the curve isn't valid, only sample within the falloff range
        r = Mathf.Sqrt(Random.Range(0f, falloff));

        a = position + (rotation * (a.normalized * r * radius));
        return new Ray(a + normal, -normal);
    }

    void OnScene(SceneView scene)
    {
        if (this != null && paintModeActive)
        {
            Event e = Event.current;
            mousePos = e.mousePosition;
            float ppp = EditorGUIUtility.pixelsPerPoint;
            mousePos.y = scene.camera.pixelHeight - mousePos.y * ppp;
            mousePos.x *= ppp;
            mousePos.z = 0;

            // ray for gizmo(disc)
            ray = scene.camera.ScreenPointToRay(mousePos);
            
            // Handle mouse down
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                isPainting = true;
                paintTimer = 0f;
                e.Use();
                
                switch (toolbarInt)
                {
                    case 0:
                        Undo.RegisterCompleteObjectUndo(this, "Added Grass");
                        // Paint immediately on mouse down
                        AddGrassPainting(terrainHit, e);
                        RebuildMeshFast();
                        break;
                    case 1:
                        Undo.RegisterCompleteObjectUndo(this, "Removed Grass");
                        RemoveAtPoint(terrainHit, e);
                        RebuildMeshFast();
                        break;
                    case 2:
                        Undo.RegisterCompleteObjectUndo(this, "Edited Grass");
                        EditGrassPainting(terrainHit, e);
                        RebuildMeshFast();
                        break;
                    case 3:
                        Undo.RegisterCompleteObjectUndo(this, "Reprojected Grass");
                        ReprojectGrassPainting(terrainHit, e);
                        RebuildMeshFast();
                        break;
                    case 4:
                        Undo.RegisterCompleteObjectUndo(this, "Auto-Filled Grass");
                        AutoFillBrushArea(terrainHit, e);
                        RebuildMesh(); // Full rebuild for auto-fill
                        break;
                }
            }
            
            // Handle mouse drag - continuous painting (not for Auto Fill)
            if (e.type == EventType.MouseDrag && e.button == 1 && isPainting && toolbarInt != 4)
            {
                e.Use();
                paintTimer += 0.016f; // Approximate frame time
                
                if (paintTimer >= paintInterval)
                {
                    paintTimer = 0f;
                    
                    switch (toolbarInt)
                    {
                        case 0:
                            AddGrassPainting(terrainHit, e);
                            break;
                        case 1:
                            RemoveAtPoint(terrainHit, e);
                            break;
                        case 2:
                            EditGrassPainting(terrainHit, e);
                            break;
                        case 3:
                            ReprojectGrassPainting(terrainHit, e);
                            break;
                    }
                    RebuildMeshFast();
                }
            }

            // Handle mouse up
            if (e.type == EventType.MouseUp && e.button == 1)
            {
                isPainting = false;
                paintTimer = 0f;
                e.Use();
                RebuildMesh();
            }
            
            // Force repaint for smooth brush cursor
            if (e.type == EventType.MouseMove)
            {
                SceneView.RepaintAll();
            }
        }
    }

    private void RemovePositionsNearRaycastHit(Vector3 hitPoint, float radius)
    {
        // More efficient removal - reverse iteration
        for (int i = grassData.Count - 1; i >= 0; i--)
        {
            if (Vector3.Distance(grassData[i].position, hitPoint) <= radius)
            {
                grassData.RemoveAt(i);
            }
        }
    }


    public void RemoveAtPoint(RaycastHit[] terrainHit, Event e)
    {
        int hits = Physics.RaycastNonAlloc(ray, terrainHit, 100f, toolSettings.hitMask.value);
        for (int i = 0; i < hits; i++)
        {
            hitPos = terrainHit[i].point;
            hitPosGizmo = hitPos;
            hitNormal = terrainHit[i].normal;
            RemovePositionsNearRaycastHit(hitPos, toolSettings.brushSize);
            break; // Only process first hit
        }

        e.Use();
    }

    public void AddGrassPainting(RaycastHit[] terrainHit, Event e)
    {
        // Main raycast to get hit position
        int hits = Physics.RaycastNonAlloc(ray, terrainHit, 200f, toolSettings.hitMask.value);
        if (hits == 0) return;
        
        for (int i = 0; i < hits; i++)
        {
            if ((toolSettings.paintMask.value & (1 << terrainHit[i].transform.gameObject.layer)) > 0)
            {
                Vector3 centerHitPos = terrainHit[i].point;
                Vector3 centerHitNormal = terrainHit[i].normal;
                
                // Calculate number of grass blades to place based on density
                int grassToPlace = Mathf.Max(1, (int)(toolSettings.density * toolSettings.brushSize * 2));
                
                // Use Poisson disc sampling for better distribution
                for (int k = 0; k < grassToPlace; k++)
                {
                    // Generate random point within brush radius
                    Vector2 randomCircle = Random.insideUnitCircle * toolSettings.brushSize;
                    Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
                    
                    // Transform offset to world space aligned with hit normal
                    Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, centerHitNormal);
                    Vector3 worldOffset = normalRotation * randomOffset;
                    Vector3 targetPosition = centerHitPos + worldOffset;
                    
                    // Raycast down from above the target position
                    Vector3 rayStart = targetPosition + centerHitNormal * 5f;
                    Ray localRay = new Ray(rayStart, -centerHitNormal);
                    
                    if (Physics.Raycast(localRay, out RaycastHit localHit, 10f, toolSettings.paintMask.value))
                    {
                        // Check normal limit
                        if (localHit.normal.y >= (1 - toolSettings.normalLimit) && localHit.normal.y <= (1 + toolSettings.normalLimit))
                        {
                            // Check minimum distance from existing grass (avoid overlap)
                            float minDistance = 0.2f; // Minimum spacing between grass
                            bool tooClose = false;
                            
                            // Only check nearby grass for performance
                            for (int g = Mathf.Max(0, grassData.Count - 100); g < grassData.Count; g++)
                            {
                                if (Vector3.Distance(grassData[g].position, localHit.point) < minDistance)
                                {
                                    tooClose = true;
                                    break;
                                }
                            }
                            
                            if (!tooClose)
                            {
                                GrassData newData = new GrassData();
                                newData.color = GetRandomColor();
                                newData.position = localHit.point;
                                newData.length = new Vector2(toolSettings.sizeWidth, toolSettings.sizeLength);
                                newData.normal = localHit.normal;
                                grassData.Add(newData);
                            }
                        }
                    }
                }
                
                lastPosition = centerHitPos;
                break; // Only process first valid hit
            }
        }
        
        e.Use();
    }

    void EditGrassPainting(RaycastHit[] terrainHit, Event e)
    {
        int hits = Physics.RaycastNonAlloc(ray, terrainHit, 200f, toolSettings.hitMask.value);
        if (hits == 0) return;
        
        for (int i = 0; i < hits; i++)
        {
            hitPos = terrainHit[i].point;
            hitPosGizmo = hitPos;
            hitNormal = terrainHit[i].normal;
            
            // Edit all grass within brush radius
            for (int j = 0; j < grassData.Count; j++)
            {
                Vector3 pos = grassData[j].position;
                float dist = Vector3.Distance(terrainHit[i].point, pos);

                // if its within the radius of the brush
                if (dist <= toolSettings.brushSize)
                {
                    float falloff = Mathf.Clamp01(dist / (toolSettings.brushFalloffSize * toolSettings.brushSize));
                    float strength = 1f - falloff;

                    //store the original color
                    Vector3 origColor = grassData[j].color;
                    Vector3 newCol = GetRandomColor();
                    
                    Vector2 origLength = grassData[j].length;
                    Vector2 lengthAdjust = new Vector2(toolSettings.adjustWidth, toolSettings.adjustLength);

                    flowTimer++;
                    if (flowTimer > toolSettings.Flow)
                    {
                        GrassData newData = grassData[j];
                        
                        // edit colors
                        if (toolbarIntEdit == 0 || toolbarIntEdit == 2)
                        {
                            newData.color = Vector3.Lerp(origColor, newCol, strength * 0.5f);
                        }
                        
                        // edit grass length/width
                        if (toolbarIntEdit == 1 || toolbarIntEdit == 2)
                        {
                            Vector2 targetLength = origLength + lengthAdjust * strength * 0.1f;
                            newData.length = Vector2.Lerp(origLength, targetLength, 0.5f);
                            newData.length.x = Mathf.Clamp(newData.length.x, 0.01f, toolSettings.adjustWidthMax);
                            newData.length.y = Mathf.Clamp(newData.length.y, 0.01f, toolSettings.adjustHeightMax);
                        }
                        
                        grassData[j] = newData;
                        flowTimer = 0;
                    }
                }
            }
            
            break; // Only process first hit
        }
        e.Use();
    }

    void ReprojectGrassPainting(RaycastHit[] terrainHit, Event e)
    {
        int hits = Physics.RaycastNonAlloc(ray, terrainHit, 200f, toolSettings.hitMask.value);
        if (hits == 0) return;
        
        for (int i = 0; i < hits; i++)
        {
            hitPos = terrainHit[i].point;
            hitPosGizmo = hitPos;
            hitNormal = terrainHit[i].normal;

            // Reproject all grass within brush radius
            for (int j = 0; j < grassData.Count; j++)
            {
                Vector3 pos = grassData[j].position;
                float dist = Vector3.Distance(terrainHit[i].point, pos);

                // if its within the radius of the brush, raycast to a new position
                if (dist <= toolSettings.brushSize)
                {
                    Vector3 rayStart = new Vector3(pos.x, pos.y + toolSettings.reprojectOffset, pos.z);
                    if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit raycastHit, toolSettings.reprojectOffset + 100f, toolSettings.paintMask.value))
                    {
                        GrassData newData = grassData[j];
                        newData.position = raycastHit.point;
                        newData.normal = raycastHit.normal;
                        grassData[j] = newData;
                    }
                }
            }
            
            break; // Only process first hit
        }
        e.Use();
    }

    void AutoFillBrushArea(RaycastHit[] terrainHit, Event e)
    {
        // Main raycast to get hit position
        int hits = Physics.RaycastNonAlloc(ray, terrainHit, 200f, toolSettings.hitMask.value);
        if (hits == 0) return;
        
        for (int i = 0; i < hits; i++)
        {
            if ((toolSettings.paintMask.value & (1 << terrainHit[i].transform.gameObject.layer)) > 0)
            {
                Vector3 centerHitPos = terrainHit[i].point;
                Vector3 centerHitNormal = terrainHit[i].normal;
                
                // Calculate grid-based distribution for uniform coverage
                float spacing = 0.3f; // Distance between grass blades
                int gridSize = Mathf.CeilToInt(toolSettings.brushSize * 2f / spacing);
                
                // Calculate total grass based on density
                int totalGrass = Mathf.Max(1, (int)(toolSettings.density * toolSettings.brushSize * toolSettings.brushSize * 10));
                int grassPlaced = 0;
                
                // Use Poisson disk sampling for natural distribution
                List<Vector3> sampledPositions = PoissonDiskSampling(centerHitPos, centerHitNormal, toolSettings.brushSize, spacing, totalGrass);
                
                foreach (Vector3 samplePos in sampledPositions)
                {
                    // Raycast down to find exact surface position
                    Vector3 rayStart = samplePos + centerHitNormal * 5f;
                    Ray localRay = new Ray(rayStart, -centerHitNormal);
                    
                    if (Physics.Raycast(localRay, out RaycastHit localHit, 10f, toolSettings.paintMask.value))
                    {
                        // Check normal limit
                        if (localHit.normal.y >= (1 - toolSettings.normalLimit) && localHit.normal.y <= (1 + toolSettings.normalLimit))
                        {
                            // Check if position already has grass nearby
                            bool hasNearbyGrass = false;
                            for (int g = Mathf.Max(0, grassData.Count - 50); g < grassData.Count; g++)
                            {
                                if (Vector3.Distance(grassData[g].position, localHit.point) < spacing * 0.8f)
                                {
                                    hasNearbyGrass = true;
                                    break;
                                }
                            }
                            
                            if (!hasNearbyGrass)
                            {
                                GrassData newData = new GrassData();
                                newData.color = GetRandomColor();
                                newData.position = localHit.point;
                                newData.length = new Vector2(toolSettings.sizeWidth, toolSettings.sizeLength);
                                newData.normal = localHit.normal;
                                grassData.Add(newData);
                                grassPlaced++;
                            }
                        }
                    }
                }
                
                Debug.Log($"Auto-Fill: Placed {grassPlaced} grass blades in brush area");
                lastPosition = centerHitPos;
                break; // Only process first valid hit
            }
        }
        
        e.Use();
    }
    
    // Poisson Disk Sampling for natural grass distribution
    List<Vector3> PoissonDiskSampling(Vector3 center, Vector3 normal, float radius, float minDistance, int maxSamples)
    {
        List<Vector3> samples = new List<Vector3>();
        List<Vector3> activeList = new List<Vector3>();
        
        // Start with center point
        samples.Add(center);
        activeList.Add(center);
        
        int attempts = 0;
        int maxAttempts = maxSamples * 30; // Safety limit
        
        while (activeList.Count > 0 && samples.Count < maxSamples && attempts < maxAttempts)
        {
            attempts++;
            
            // Pick random point from active list
            int randomIndex = Random.Range(0, activeList.Count);
            Vector3 point = activeList[randomIndex];
            
            bool found = false;
            
            // Try to generate new point around it
            for (int k = 0; k < 30; k++)
            {
                // Random angle and distance
                float angle = Random.value * Mathf.PI * 2f;
                float distance = minDistance * (1f + Random.value);
                
                // Create offset in tangent plane
                Vector3 tangent = Vector3.Cross(normal, Vector3.up);
                if (tangent.magnitude < 0.1f)
                    tangent = Vector3.Cross(normal, Vector3.forward);
                tangent.Normalize();
                
                Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;
                Vector3 offset = tangent * Mathf.Cos(angle) * distance + bitangent * Mathf.Sin(angle) * distance;
                Vector3 newPoint = point + offset;
                
                // Check if within radius
                if (Vector3.Distance(center, newPoint) <= radius)
                {
                    // Check if far enough from other samples
                    bool valid = true;
                    foreach (Vector3 sample in samples)
                    {
                        if (Vector3.Distance(sample, newPoint) < minDistance)
                        {
                            valid = false;
                            break;
                        }
                    }
                    
                    if (valid)
                    {
                        samples.Add(newPoint);
                        activeList.Add(newPoint);
                        found = true;
                        break;
                    }
                }
            }
            
            if (!found)
            {
                activeList.RemoveAt(randomIndex);
            }
        }
        
        return samples;
    }

    private bool RemovePoints(GrassData point)
    {
        RaycastHit terrainHit;
        if (Physics.Raycast(ray, out terrainHit, 100f, toolSettings.hitMask.value))
        {
            hitPos = terrainHit.point;
            hitPosGizmo = hitPos;
            hitNormal = terrainHit.normal;

            Vector3 pos = point.position;
            // pos += grassObject.transform.position;
            float dist = Vector3.Distance(terrainHit.point, pos);

            // if its within the radius of the brush, remove all info
            if (dist <= toolSettings.brushSize)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    void RebuildMesh()
    {
        grassAmount = grassData.Count;
        grassCompute.Reset();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

    }

    void RebuildMeshFast()
    {
        grassAmount = grassData.Count;
        grassCompute.ResetFaster();

    }

#endif
}
#endif // UNITY_EDITOR

