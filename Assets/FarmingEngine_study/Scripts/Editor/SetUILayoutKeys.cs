using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace FarmingEngine.EditorTool
{
    public static class SetUILayoutKeys
    {
        private static readonly Dictionary<System.Type, string> keyMap = new Dictionary<System.Type, string>
        {
            { typeof(InventoryPanel),     "quickbar" },
            { typeof(EquipPanel),         "equip"    },
            { typeof(BagPanel),           "bag"      },
            { typeof(StoragePanel),       "storage"  },
            { typeof(CraftPanel),         "craft"    },
            { typeof(CraftSubPanel),      "craftsub" },
            { typeof(ShopPanel),          "shop"     },
            { typeof(MixingPanel),        "mixing"   },
            { typeof(DialoguePanel),      "dialogue" },
            { typeof(ReadPanel),          "read"     },
            { typeof(PausePanel),         "pause"    },
            { typeof(ActionSelector),     "action"   },
            { typeof(FullInventoryPanel), "fullinv"  },
        };

        [MenuItem("Farming Engine/Set UI Layout Keys in UICanvas")]
        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/FarmingEngine_study/Prefabs/UI/UICanvas.prefab");
            if (prefab == null) { Debug.LogError("[SetUILayoutKeys] UICanvas.prefab not found"); return; }

            int count = 0;
            using (var scope = new PrefabUtility.EditPrefabContentsScope(
                "Assets/FarmingEngine_study/Prefabs/UI/UICanvas.prefab"))
            {
                var root = scope.prefabContentsRoot;
                foreach (var kv in keyMap)
                {
                    var comp = root.GetComponentInChildren(kv.Key, true);
                    if (comp == null) { Debug.LogWarning($"[SetUILayoutKeys] {kv.Key.Name} not found"); continue; }
                    var so = new SerializedObject(comp);
                    var prop = so.FindProperty("layoutKey");
                    if (prop == null) { Debug.LogWarning($"[SetUILayoutKeys] layoutKey field missing on {kv.Key.Name}"); continue; }
                    prop.stringValue = kv.Value;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log($"[SetUILayoutKeys] {comp.gameObject.name} ({kv.Key.Name}) → \"{kv.Value}\"");
                    count++;
                }
            }
            Debug.Log($"[SetUILayoutKeys] 완료: {count}개 패널 layoutKey 설정됨");
        }
    }
}
