using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// StreamingAssets/Icons/{item_id}.png 파일이 있으면 기존 아이콘 대신 사용.
    /// 아이콘 디자이너 테스트용 — 파일을 지우면 원래 에셋으로 복귀.
    /// </summary>
    public class IconOverrideLoader : MonoBehaviour
    {
        private static Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        private static IconOverrideLoader _instance;

        void Awake()
        {
            if (_instance != null) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAll();
        }

        private void LoadAll()
        {
            cache.Clear();
            string dir = Path.Combine(Application.streamingAssetsPath, "Icons");
            if (!Directory.Exists(dir)) return;

            foreach (string path in Directory.GetFiles(dir, "*.png"))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (!tex.LoadImage(bytes)) { Destroy(tex); continue; }

                Sprite sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );

                string itemId = Path.GetFileNameWithoutExtension(path);
                cache[itemId] = sprite;
            }

            Debug.Log($"[IconOverride] {cache.Count}개 아이콘 로드 완료 ({dir})");
        }

        public static Sprite GetOverride(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            cache.TryGetValue(itemId, out Sprite sprite);
            return sprite;
        }
    }
}
