using System.IO;
using UnityEditor;
using UnityEngine;

namespace FarmingEngine.EditorTool
{
    public class ClearSaveEditor : ScriptableWizard
    {
        [MenuItem("Farming Engine/Delete Save File (Full Reset)", priority = 351)]
        static void Open()
        {
            ScriptableWizard.DisplayWizard<ClearSaveEditor>("세이브 전체 초기화", "삭제");
        }

        void OnWizardUpdate()
        {
            helpString = "PlayerData (.farming) + NarrativeData (.dq) 전부 삭제합니다.\n경로: " + Application.persistentDataPath;
        }

        void OnWizardCreate()
        {
            string path = Application.persistentDataPath;
            int count = 0;

            foreach (string f in Directory.GetFiles(path, "*.farming")) { File.Delete(f); count++; }
            foreach (string f in Directory.GetFiles(path, "*.dq"))      { File.Delete(f); count++; }

            PlayerPrefs.DeleteKey("last_save_farming");
            PlayerPrefs.Save();

            Debug.Log($"[ClearSave] 세이브 {count}개 삭제 완료: {path}");
            EditorUtility.DisplayDialog("완료", $"세이브 {count}개 삭제됨.\n다음 Play Mode부터 새 게임으로 시작합니다.", "확인");
        }
    }
}
