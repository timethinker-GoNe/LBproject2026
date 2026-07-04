using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 빵 진열대. 상호작용 시 DisplayShelfPanel을 열고,
    /// display_points 위치에 저장된 아이템의 3D 아이콘을 스폰한다.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public class BreadDisplayShelf : MonoBehaviour
    {
        public int max_slots = 6;
        public Transform[] display_points;  // 3D 시각화 위치
        public float display_item_scale = 0.4f;

        private Selectable select;
        private UniqueID unique_id;

        private List<GameObject> spawned_items = new List<GameObject>();

        private static List<BreadDisplayShelf> shelf_list = new List<BreadDisplayShelf>();
        public static List<BreadDisplayShelf> GetAll() => shelf_list;

        void Awake()
        {
            shelf_list.Add(this);
            select = GetComponent<Selectable>();
            unique_id = GetComponent<UniqueID>();
        }

        void OnDestroy()
        {
            shelf_list.Remove(this);
        }

        void Start()
        {
            select.onUse += OnUse;
        }

        void Update()
        {
            RefreshDisplayItems();
        }

        private void OnUse(PlayerCharacter player)
        {
            if (string.IsNullOrEmpty(unique_id.unique_id))
            {
                Debug.LogError("BreadDisplayShelf: UID가 없습니다. UniqueID 컴포넌트에서 생성하세요.");
                return;
            }
            DisplayShelfPanel panel = DisplayShelfPanel.Get();
            if (panel != null)
                panel.ShowShelf(player, unique_id.unique_id, max_slots);
        }

        private void RefreshDisplayItems()
        {
            if (display_points == null || display_points.Length == 0) return;

            InventoryData inv = InventoryData.Get(InventoryType.Storage, unique_id?.unique_id);

            // 기존 스폰 아이템 제거
            foreach (var obj in spawned_items)
            {
                if (obj != null) Destroy(obj);
            }
            spawned_items.Clear();

            if (inv == null) return;

            int pointIndex = 0;
            foreach (var pair in inv.items)
            {
                if (pointIndex >= display_points.Length) break;
                InventoryItemData invData = pair.Value;
                if (invData == null || invData.quantity <= 0) continue;

                ItemData idata = ItemData.Get(invData.item_id);
                if (idata == null || idata.icon == null)
                {
                    pointIndex++;
                    continue;
                }

                Transform point = display_points[pointIndex];
                if (point == null) { pointIndex++; continue; }

                // 아이콘을 Billboard Quad로 시각화
                GameObject icon = CreateItemIcon(idata, point);
                spawned_items.Add(icon);
                pointIndex++;
            }
        }

        private GameObject CreateItemIcon(ItemData idata, Transform point)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = $"DisplayItem_{idata.id}";
            obj.transform.SetParent(point, false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = Vector3.one * display_item_scale;

            // 충돌체 제거 (클릭 방해 방지)
            var col = obj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // 스프라이트 렌더러 활용 또는 MeshRenderer로 텍스처 표시
            var renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null && idata.icon != null)
            {
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.mainTexture = idata.icon.texture;
                renderer.material = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            return obj;
        }

        public string GetUID() => unique_id != null ? unique_id.unique_id : "";
    }
}
