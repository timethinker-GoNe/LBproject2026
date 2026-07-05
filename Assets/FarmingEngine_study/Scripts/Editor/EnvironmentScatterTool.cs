using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace FarmingEngine.SceneTools
{
    /// <summary>
    /// Ground 가장자리를 따라 나무(Deprecated/Scene4에서 쓰던 _KKUBUL 에셋)를 정사각형 띠 모양으로
    /// 빽빽하게 배치하고, 그 안쪽에 수풀을 얇게 두르는 도구.
    /// 메뉴: Farming Engine > Environment > Scatter / Clear Boundary Decoration
    /// </summary>
    public static class EnvironmentScatterTool
    {
        private const string SCATTER_GROUP_NAME = "BoundaryScatter";

        private const float BAND_INNER_RATIO = 0.80f; // Ground half-extent 대비 나무 띠 안쪽 경계
        private const float BAND_OUTER_RATIO = 0.97f; // 가장자리에 너무 붙지 않게 여유
        private const float GRID_SPACING = 2.8f;      // 격자 간격 (작을수록 빽빽함)
        private const float JITTER = 0.6f;             // 격자에서 랜덤하게 흐트러뜨리는 정도
        private const float TREE_PROBABILITY = 1f;     // 격자 지점마다 실제로 배치할 확률
        private const float MIN_SCALE = 0.85f;
        private const float MAX_SCALE = 1.25f;

        // 나무 안쪽으로 얇게 두르는 수풀 띠
        private const float BUSH_BAND_INNER_RATIO = 0.72f;
        private const float BUSH_BAND_OUTER_RATIO = 0.80f;
        private const float BUSH_GRID_SPACING = 4f;
        private const float BUSH_JITTER = 0.6f;
        private const float BUSH_PROBABILITY = 0.35f;  // 듬성듬성 배치

        private static readonly string[] PrefabPaths =
        {
            "Assets/_KKUBUL/Prefabs/Tree.prefab",
            "Assets/_KKUBUL/Prefabs/Tree2.prefab",
            "Assets/_KKUBUL/Prefabs/Tree3.prefab",
        };

        private static readonly string[] BushPrefabPaths =
        {
            "Assets/_KKUBUL/Prefabs/Bush.prefab",
        };

        [MenuItem("Farming Engine/Environment/Scatter Boundary Decoration")]
        public static void ScatterBoundary()
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                EditorUtility.DisplayDialog("Ground 없음", "씬에서 'Ground' 오브젝트를 찾을 수 없습니다.", "확인");
                return;
            }

            Renderer groundRenderer = ground.GetComponent<Renderer>();
            if (groundRenderer == null)
            {
                EditorUtility.DisplayDialog("Renderer 없음", "'Ground'에 Renderer가 없어 크기를 계산할 수 없습니다.", "확인");
                return;
            }

            Bounds bounds = groundRenderer.bounds;
            Vector3 center = bounds.center;
            float halfExtent = Mathf.Min(bounds.extents.x, bounds.extents.z);

            List<GameObject> prefabs = LoadPrefabs(PrefabPaths);
            List<GameObject> bushPrefabs = LoadPrefabs(BushPrefabPaths);

            if (prefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("프리팹 없음", "스캐터할 프리팹을 하나도 찾지 못했습니다.", "확인");
                return;
            }

            // 기존 스캐터 결과가 있으면 제거하고 다시 생성
            GameObject existingGroup = GameObject.Find(SCATTER_GROUP_NAME);
            if (existingGroup != null) Undo.DestroyObjectImmediate(existingGroup);

            GameObject scatterGroup = new GameObject(SCATTER_GROUP_NAME);
            Undo.RegisterCreatedObjectUndo(scatterGroup, "Scatter Boundary Decoration");
            scatterGroup.transform.SetParent(ground.transform.parent, false);

            int treeCount = ScatterBand(scatterGroup.transform, center, halfExtent, prefabs,
                BAND_INNER_RATIO, BAND_OUTER_RATIO, GRID_SPACING, JITTER, MIN_SCALE, MAX_SCALE, TREE_PROBABILITY);

            int bushCount = 0;
            if (bushPrefabs.Count > 0)
            {
                bushCount = ScatterBand(scatterGroup.transform, center, halfExtent, bushPrefabs,
                    BUSH_BAND_INNER_RATIO, BUSH_BAND_OUTER_RATIO, BUSH_GRID_SPACING, BUSH_JITTER, MIN_SCALE, MAX_SCALE, BUSH_PROBABILITY);
            }

            Selection.activeGameObject = scatterGroup;
            Debug.Log($"[EnvironmentScatterTool] 나무 {treeCount}개, 수풀 {bushCount}개를 Ground 가장자리에 배치했습니다.");
        }

        private static List<GameObject> LoadPrefabs(string[] paths)
        {
            List<GameObject> prefabs = new List<GameObject>();
            foreach (string path in paths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) prefabs.Add(prefab);
                else Debug.LogWarning("[EnvironmentScatterTool] Prefab not found: " + path);
            }
            return prefabs;
        }

        // 격자를 훑으면서, 중심에서 각 축 중 더 먼 거리(체비쇼프 거리)가
        // innerRatio~outerRatio 사이인 지점만 남겨서 정사각형 띠 모양으로 배치한다
        private static int ScatterBand(Transform parent, Vector3 center, float halfExtent, List<GameObject> prefabs,
            float innerRatio, float outerRatio, float gridSpacing, float jitterAmount, float minScale, float maxScale,
            float placementProbability)
        {
            float outerBound = halfExtent * outerRatio;
            float innerBound = halfExtent * innerRatio;
            float groundY = center.y;
            int placed = 0;

            for (float x = -outerBound; x <= outerBound; x += gridSpacing)
            {
                for (float z = -outerBound; z <= outerBound; z += gridSpacing)
                {
                    float edgeDist = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
                    if (edgeDist < innerBound || edgeDist > outerBound)
                        continue;

                    if (Random.value > placementProbability)
                        continue;

                    GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
                    Vector3 jitter = new Vector3(Random.Range(-jitterAmount, jitterAmount), 0f, Random.Range(-jitterAmount, jitterAmount));
                    Vector3 pos = center + new Vector3(x, 0f, z) + jitter;
                    pos.y = groundY;

                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    instance.transform.SetParent(parent, false);
                    instance.transform.position = pos;
                    instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    instance.transform.localScale *= Random.Range(minScale, maxScale);

                    SnapToGround(instance, groundY);

                    Undo.RegisterCreatedObjectUndo(instance, "Scatter Boundary Decoration");
                    placed++;
                }
            }

            return placed;
        }

        // 프리팹마다 피벗 위치가 제각각이라(예: 트렁크 메시가 중심 피벗), 배치 후 실제 렌더러
        // 바운즈의 최하단이 지면과 맞도록 위로 밀어 올려 보정한다.
        private static void SnapToGround(GameObject instance, float groundY)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float offset = groundY - bounds.min.y;
            instance.transform.position += new Vector3(0f, offset, 0f);
        }

        [MenuItem("Farming Engine/Environment/Clear Boundary Decoration")]
        public static void ClearBoundary()
        {
            GameObject existing = GameObject.Find(SCATTER_GROUP_NAME);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                Debug.Log("[EnvironmentScatterTool] Boundary decoration 제거됨.");
            }
            else
            {
                Debug.Log("[EnvironmentScatterTool] 제거할 대상이 없습니다.");
            }
        }
    }
}
