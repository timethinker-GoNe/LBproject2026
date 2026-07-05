using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace FarmingEngine.SceneTools
{
    /// <summary>
    /// GrassGroundZone 컴포넌트가 붙은 오브젝트 위에만 GPU 컴퓨트 셰이더 기반 잔디
    /// (Assets/_KKUBUL/Grass, GrassComputeScript)를 자동으로 채우는 도구.
    /// Deprecated/Scene4에서 쓰던 것과 같은 프리셋(Grass Settings.asset)을 재사용한다.
    /// 메뉴: Farming Engine > Environment > GPU Grass Tool
    /// </summary>
    public class GpuGrassTool : EditorWindow
    {
        private const string GRASS_HOLDER_NAME = "Grass System - Holder";
        private const string SETTINGS_PATH = "Assets/_KKUBUL/Grass/Grass Settings.asset";

        private float coverageRatio = 0.85f; // Ground 크기 대비 잔디 커버 비율
        private float spacing = 0.35f;       // 작을수록 빽빽함
        private float jitter = 0.15f;
        private Vector2 bladeLength = new Vector2(2f, 2f);

        [MenuItem("Farming Engine/Environment/GPU Grass Tool")]
        public static void ShowWindow()
        {
            GetWindow<GpuGrassTool>("GPU Grass Tool");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Ground 위에 GPU 잔디 채우기", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            coverageRatio = EditorGUILayout.Slider("Coverage Ratio", coverageRatio, 0.1f, 1f);
            spacing = EditorGUILayout.Slider("Spacing (작을수록 빽빽)", spacing, 0.1f, 2f);
            jitter = EditorGUILayout.Slider("Jitter", jitter, 0f, 1f);
            bladeLength = EditorGUILayout.Vector2Field("Blade Length", bladeLength);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate / Regenerate"))
                Generate();

            if (GUILayout.Button("Clear"))
                Clear();
        }

        private void Generate()
        {
            GrassGroundZone[] zones = Object.FindObjectsByType<GrassGroundZone>(FindObjectsSortMode.None);
            if (zones.Length == 0)
            {
                EditorUtility.DisplayDialog("GrassGroundZone 없음",
                    "씬에서 GrassGroundZone 컴포넌트가 붙은 오브젝트를 찾을 수 없습니다.\n잔디를 깔 Ground 오브젝트에 GrassGroundZone을 추가해주세요.", "확인");
                return;
            }

            SO_GrassSettings settings = AssetDatabase.LoadAssetAtPath<SO_GrassSettings>(SETTINGS_PATH);
            if (settings == null)
            {
                EditorUtility.DisplayDialog("프리셋 없음", SETTINGS_PATH + " 를 찾을 수 없습니다.", "확인");
                return;
            }

            GameObject existing = GameObject.Find(GRASS_HOLDER_NAME);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GrassExcludeZone[] excludeZones = Object.FindObjectsByType<GrassExcludeZone>(FindObjectsSortMode.None);
            List<Bounds> excludeBounds = new List<Bounds>();
            foreach (GrassExcludeZone exclude in excludeZones)
            {
                Renderer excludeRenderer = exclude.GetComponent<Renderer>();
                if (excludeRenderer != null) excludeBounds.Add(excludeRenderer.bounds);
            }

            List<GrassData> grassData = new List<GrassData>();
            Transform parent = zones[0].transform.parent;

            foreach (GrassGroundZone zone in zones)
            {
                Renderer zoneRenderer = zone.GetComponent<Renderer>();
                if (zoneRenderer == null)
                {
                    Debug.LogWarning("[GpuGrassTool] Renderer가 없어 건너뜀: " + zone.name);
                    continue;
                }

                Bounds bounds = zoneRenderer.bounds;
                float halfSize = Mathf.Min(bounds.extents.x, bounds.extents.z) * coverageRatio;
                float groundY = bounds.center.y;

                for (float x = -halfSize; x <= halfSize; x += spacing)
                {
                    for (float z = -halfSize; z <= halfSize; z += spacing)
                    {
                        Vector3 jitterOffset = new Vector3(Random.Range(-jitter, jitter), 0f, Random.Range(-jitter, jitter));
                        Vector3 pos = bounds.center + new Vector3(x, 0f, z) + jitterOffset;
                        pos.y = groundY;

                        if (IsInsideAnyBoundsXZ(pos, excludeBounds))
                            continue;

                        grassData.Add(new GrassData
                        {
                            position = pos,
                            normal = Vector3.up,
                            length = bladeLength,
                            color = Vector3.one
                        });
                    }
                }
            }

            GameObject holder = new GameObject(GRASS_HOLDER_NAME);
            Undo.RegisterCreatedObjectUndo(holder, "Add GPU Grass");
            holder.transform.SetParent(parent, false);

            GrassComputeScript grassCompute = holder.AddComponent<GrassComputeScript>();
            grassCompute.currentPresets = settings;
            grassCompute.SetGrassPaintedDataList = grassData;
            grassCompute.Reset();

            Selection.activeGameObject = holder;
            Debug.Log($"[GpuGrassTool] GrassGroundZone {zones.Length}개에서 GPU 잔디 {grassData.Count}개 생성 완료.");
        }

        private static bool IsInsideAnyBoundsXZ(Vector3 pos, List<Bounds> boundsList)
        {
            foreach (Bounds b in boundsList)
            {
                if (pos.x >= b.min.x && pos.x <= b.max.x && pos.z >= b.min.z && pos.z <= b.max.z)
                    return true;
            }
            return false;
        }

        private void Clear()
        {
            GameObject existing = GameObject.Find(GRASS_HOLDER_NAME);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                Debug.Log("[GpuGrassTool] GPU 잔디 제거됨.");
            }
            else
            {
                Debug.Log("[GpuGrassTool] 제거할 대상이 없습니다.");
            }
        }
    }
}
