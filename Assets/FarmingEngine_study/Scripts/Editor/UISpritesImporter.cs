#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// Resources/UISprites/ 폴더에 PNG가 추가되면 자동으로 Sprite 타입으로 설정한다.
    /// </summary>
    public class UISpritesImporter : AssetPostprocessor
    {
        private const string TARGET_PATH = "Assets/FarmingEngine_study/Resources/UISprites/";

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace("\\", "/").StartsWith(TARGET_PATH))
                return;

            var ti = (TextureImporter)assetImporter;
            if (ti.textureType == TextureImporterType.Sprite)
                return;

            ti.textureType         = TextureImporterType.Sprite;
            ti.spriteImportMode    = SpriteImportMode.Single;
            ti.mipmapEnabled       = false;
            ti.filterMode          = FilterMode.Bilinear;
            ti.alphaIsTransparency = true;

            var settings = ti.GetDefaultPlatformTextureSettings();
            settings.format = TextureImporterFormat.Automatic;
            ti.SetPlatformTextureSettings(settings);
        }

        static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                if (path.Replace("\\", "/").StartsWith(TARGET_PATH) && path.EndsWith(".png"))
                    Debug.Log($"[UISpritesImporter] Sprite auto-configured: {path}");
            }
        }
    }
}
#endif
