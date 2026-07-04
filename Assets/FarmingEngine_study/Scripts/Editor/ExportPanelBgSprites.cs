using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace FarmingEngine.EditorTool
{
    public static class ExportPanelBgSprites
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

        [MenuItem("Farming Engine/Export Panel BG Sprites to UISprites")]
        public static void Run()
        {
            string prefabPath = "Assets/FarmingEngine_study/Prefabs/UI/UICanvas.prefab";
            string jsonPath   = Path.Combine(Application.streamingAssetsPath, "UIDesignConfig.json");
            string spritesDir = Path.Combine(Application.streamingAssetsPath, "UISprites");

            if (!Directory.Exists(spritesDir)) Directory.CreateDirectory(spritesDir);

            JObject root = File.Exists(jsonPath)
                ? JObject.Parse(File.ReadAllText(jsonPath))
                : new JObject();

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null) { Debug.LogError("[ExportBG] UICanvas.prefab 로드 실패"); return; }

            int count = 0;
            try
            {
                foreach (var kv in keyMap)
                {
                    var comp = prefabRoot.GetComponentInChildren(kv.Key, true) as MonoBehaviour;
                    if (comp == null) { Debug.LogWarning($"[ExportBG] {kv.Key.Name} not found"); continue; }

                    Sprite sprite = FindBgSprite(comp);
                    if (sprite == null) { Debug.LogWarning($"[ExportBG] {kv.Key.Name}: 배경 Image/Sprite 없음"); continue; }

                    string filename = kv.Value + "_bg.png";
                    var tex = SpriteToTexture2D(sprite);
                    File.WriteAllBytes(Path.Combine(spritesDir, filename), tex.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(tex);

                    if (root[kv.Value] == null) root[kv.Value] = new JObject();
                    ((JObject)root[kv.Value])["bgSprite"] = "UISprites/" + filename;

                    // 슬롯 배경 이미지 (ItemSlot 첫 번째 슬롯의 배경)
                    Sprite slotSprite = FindSlotBgSprite(comp);
                    if (slotSprite != null)
                    {
                        string slotFilename = kv.Value + "_slot.png";
                        var slotTex = SpriteToTexture2D(slotSprite);
                        File.WriteAllBytes(Path.Combine(spritesDir, slotFilename), slotTex.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(slotTex);
                        ((JObject)root[kv.Value])["slotSprite"] = "UISprites/" + slotFilename;
                        Debug.Log($"[ExportBG] {kv.Key.Name} 슬롯 → {slotFilename}");
                    }

                    Debug.Log($"[ExportBG] {kv.Key.Name} ({kv.Value}) → {filename}");
                    count++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            File.WriteAllText(jsonPath, root.ToString(Newtonsoft.Json.Formatting.Indented));
            AssetDatabase.Refresh();
            Debug.Log($"[ExportBG] 완료: {count}개 패널 배경 이미지 → {spritesDir}");
        }

        static Sprite FindSlotBgSprite(MonoBehaviour comp)
        {
            // ItemSlot(UISlot 서브클래스) 우선 탐색
            var itemSlot = comp.GetComponentInChildren<ItemSlot>(true);
            MonoBehaviour slot = itemSlot != null ? (MonoBehaviour)itemSlot : comp.GetComponentInChildren<UISlot>(true);
            if (slot == null) return null;

            // 슬롯 루트의 Image
            var img = slot.GetComponent<Image>();
            if (img != null && img.sprite != null) return img.sprite;

            // 배경 계열 이름을 전체 자손에서 탐색 (깊이 무관)
            string[] bgNames = { "BG", "Background", "Bg", "bg", "background", "SlotBG", "Frame", "Back", "SlotFrame", "Border" };
            foreach (Transform t in slot.GetComponentsInChildren<Transform>(true))
            {
                foreach (var n in bgNames)
                {
                    if (!t.name.Equals(n, System.StringComparison.OrdinalIgnoreCase)) continue;
                    var ti = t.GetComponent<Image>();
                    if (ti != null && ti.sprite != null) return ti.sprite;
                }
            }

            // 폴백: 아이콘/텍스트 계열 이름 제외하고 sprite 있는 첫 번째 Image
            string[] excludeKeywords = { "icon", "item", "count", "text", "title", "label", "cooldown", "highlight", "select" };
            foreach (var childImg in slot.GetComponentsInChildren<Image>(true))
            {
                string lower = childImg.gameObject.name.ToLower();
                bool skip = false;
                foreach (var kw in excludeKeywords) if (lower.Contains(kw)) { skip = true; break; }
                if (!skip && childImg.sprite != null) return childImg.sprite;
            }

            return null;
        }

        static Sprite FindBgSprite(MonoBehaviour comp)
        {
            // 1. "BG"/"Background" 직계 자식 우선 — 인게임 BG GO의 Image가 실제 배경 스프라이트
            foreach (var n in new[] { "BG", "Background", "Bg", "bg", "background" })
            {
                var child = comp.transform.Find(n);
                if (child == null) continue;
                var childImg = child.GetComponent<Image>();
                if (childImg != null && childImg.sprite != null) return childImg.sprite;
            }

            // 2. 폴백: 패널 루트 GO의 Image
            var img = comp.GetComponent<Image>();
            if (img != null && img.sprite != null) return img.sprite;

            return null;
        }

        static Texture2D SpriteToTexture2D(Sprite sprite)
        {
            var src = sprite.texture;
            var rt  = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(src, rt);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var full = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            full.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            full.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            // 아틀라스 스프라이트인 경우 해당 영역만 크롭
            var r = sprite.textureRect;
            Texture2D result;
            if ((int)r.width == src.width && (int)r.height == src.height)
            {
                result = full;
            }
            else
            {
                var pixels = full.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
                UnityEngine.Object.DestroyImmediate(full);
                result = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
                result.SetPixels(pixels);
                result.Apply();
            }

            // Unity ReadPixels는 Y=0을 하단으로 읽으므로 브라우저 표시를 위해 수직 반전
            FlipVertical(result);
            return result;
        }

        static void FlipVertical(Texture2D tex)
        {
            int w = tex.width, h = tex.height;
            var pixels = tex.GetPixels();
            var flipped = new Color[pixels.Length];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    flipped[y * w + x] = pixels[(h - 1 - y) * w + x];
            tex.SetPixels(flipped);
            tex.Apply();
        }
    }
}
