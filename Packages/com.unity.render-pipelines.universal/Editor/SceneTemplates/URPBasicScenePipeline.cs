using UnityEngine;
using UnityEditor.SceneTemplate;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEditor.Rendering.Utilities;
using System.IO;

namespace UnityEditor.Rendering.Universal
{
    static class SampleUtilities
    {
        // Simple fallback for CopyFilesInFolder used by scene templates.
        // It tries AssetDatabase.CopyAsset first (works for package -> Assets copying),
        // and falls back to FileUtil + AssetDatabase.ImportAsset where possible.
        public static void CopyFilesInFolder(string parentFolderName, Dictionary<string, string> filesToImport)
        {
            if (filesToImport == null)
                return;

            foreach (var kv in filesToImport)
            {
                var src = kv.Key.Replace('\\', '/');
                var destFolder = kv.Value.Replace('\\', '/').Trim('/');

                // Ensure destination is under Assets/
                if (!destFolder.StartsWith("Assets/"))
                    destFolder = "Assets/" + destFolder.TrimStart('/');

                EnsureFolderExists(destFolder);

                var fileName = Path.GetFileName(src);
                var destPath = $"{destFolder}/{fileName}";

                // Try AssetDatabase copy (works if source is an imported asset, e.g. in Packages/)
                if (AssetDatabase.CopyAsset(src, destPath))
                {
                    continue;
                }

                // As fallback, try filesystem copy if source exists on disk (best-effort)
                try
                {
                    // If src is a package path, AssetDatabase.CopyAsset should have worked.
                    // But attempt FileUtil.CopyFileOrDirectory anyway for other cases.
                    FileUtil.CopyFileOrDirectory(src, destPath);
                    AssetDatabase.ImportAsset(destPath);
                }
                catch (System.Exception)
                {
                    Debug.LogWarning($"SampleUtilities: Failed to copy '{src}' to '{destPath}'");
                }
            }
        }

        static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            var parts = folderPath.Split('/');
            string cur = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; ++i)
            {
                var next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
    class URPBasicScenePipeline : ISceneTemplatePipeline
    {
        void ISceneTemplatePipeline.AfterTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, Scene scene, bool isAdditive, string sceneName)
        {
            //To avoid problematic behavior and warnings in the future, let's remove all missing scripts monobehaviors. 
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }

        void ISceneTemplatePipeline.BeforeTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, bool isAdditive, string sceneName)
        {
            string parentFolderName = "SceneTemplateAssets";
            string commonFolderName = "Common";
            string templateSpecificFolderName = sceneTemplateAsset.templateName;

            string completeTemplateSpecificFolderName = parentFolderName + "/" + sceneTemplateAsset.templateName;
            string completeCommonFolderName = parentFolderName + "/" + commonFolderName;

            Dictionary<string, string> filesToImport;

            switch (sceneTemplateAsset.templateName)
            {
                case "Basic (URP)":
                    // Nothing to import specifically.
                    break;
                case "Standard (URP)":
                    filesToImport = new Dictionary<string, string>();
                    filesToImport.Add("Packages/com.unity.render-pipelines.core/Samples~/Common/Models/UnityMaterialBall.fbx", completeCommonFolderName + "/Models/");
                    SampleUtilities.CopyFilesInFolder(parentFolderName, filesToImport);
                    break;
                default:
                    break;
            }

            AssetDatabase.Refresh();
        }

        bool ISceneTemplatePipeline.IsValidTemplateForInstantiation(SceneTemplateAsset sceneTemplateAsset)
        {

            return true;
        }
    }
}
