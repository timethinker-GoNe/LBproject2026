using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace FarmingEngine.EditorTool
{
    /// <summary>
    /// UICanvas.prefab에서 각 패널의 GridLayoutGroup 실제 값을 읽어
    /// StreamingAssets/UIDesignConfig.json의 slotCols/Gap/Pad 필드를 갱신한다.
    /// 앵커 값(ancMinX 등)은 건드리지 않는다.
    /// </summary>
    public static class ExportCurrentLayout
    {
        private static readonly Dictionary<System.Type, string> keyMap = new Dictionary<System.Type, string>
        {
            { typeof(InventoryPanel),  "quickbar" },
            { typeof(EquipPanel),      "equip"    },
            { typeof(BagPanel),        "bag"      },
            { typeof(StoragePanel),    "storage"  },
            { typeof(CraftPanel),      "craft"    },
            { typeof(CraftSubPanel),   "craftsub" },
            { typeof(ShopPanel),       "shop"     },
            { typeof(MixingPanel),     "mixing"   },
            { typeof(ActionSelector),  "action"   },
        };

        [MenuItem("Farming Engine/Export Current Slot Layout to JSON")]
        public static void Run()
        {
            string prefabPath = "Assets/FarmingEngine_study/Prefabs/UI/UICanvas.prefab";
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "UIDesignConfig.json");

            JObject root = File.Exists(jsonPath)
                ? JObject.Parse(File.ReadAllText(jsonPath))
                : new JObject();

            // LoadPrefabContents: nested prefab 포함 전체 계층 로드 (읽기 전용, 수정 없음)
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null) { Debug.LogError("[ExportLayout] UICanvas.prefab 로드 실패"); return; }

            int count = 0;
            try
            {
                foreach (var kv in keyMap)
                {
                    var comp = prefabRoot.GetComponentInChildren(kv.Key, true);
                    if (comp == null) { Debug.LogWarning($"[ExportLayout] {kv.Key.Name} not found"); continue; }

                    if (root[kv.Value] == null) root[kv.Value] = new JObject();
                    var p = (JObject)root[kv.Value];

                    // slotCount: UISlotPanel.slots 배열 길이로 읽기
                    var slotPanel = comp as UISlotPanel;
                    if (slotPanel != null && slotPanel.slots != null)
                        p["slotCount"] = slotPanel.slots.Length;

                    // GridLayoutGroup이 있으면 cols/gap/pad도 읽기 (없는 경우 JSON 기존 값 유지)
                    var glg = comp.GetComponentInChildren<GridLayoutGroup>(true);
                    if (glg != null)
                    {
                        int cols = (glg.constraint == GridLayoutGroup.Constraint.FixedColumnCount && glg.constraintCount > 0)
                            ? glg.constraintCount : 1;
                        p["slotCols"] = cols;
                        p["slotGap"]  = Mathf.RoundToInt(glg.spacing.x);
                        p["slotPad"]  = glg.padding.left;
                        Debug.Log($"[ExportLayout] {kv.Key.Name} ({kv.Value}) → count={slotPanel?.slots.Length}, cols={cols}, gap={glg.spacing.x}, pad={glg.padding.left}");
                    }
                    else
                    {
                        Debug.Log($"[ExportLayout] {kv.Key.Name} ({kv.Value}) → count={slotPanel?.slots.Length} (수동 배치, GridLayoutGroup 없음 - cols/gap/pad는 JSON 기존 값 유지)");
                    }

                    count++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            File.WriteAllText(jsonPath, root.ToString(Newtonsoft.Json.Formatting.Indented));
            AssetDatabase.Refresh();
            Debug.Log($"[ExportLayout] 완료: {count}개 패널 → {jsonPath}");
        }
    }
}
