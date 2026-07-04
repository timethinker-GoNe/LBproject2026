using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// Scene_Start 타이틀 스크린 매니저.
    /// TheGame / TheData 없이 동작하는 경량 씬.
    /// 세이브 존재 여부에 따라 불러오기 버튼 활성화.
    /// </summary>
    public class StartScreenManager : MonoBehaviour
    {
        [Header("메뉴 버튼")]
        public Button new_game_button;
        public Button load_game_button;
        public Button settings_button;
        public Button quit_button;

        [Header("패널 참조")]
        public SaveSlotPanel  save_slot_panel;
        public ConfirmPanel   confirm_panel;
        public SettingsPanel  settings_panel;

        private static StartScreenManager _instance;
        public static StartScreenManager Get() { return _instance; }

        void Awake()
        {
            _instance = this;
        }

        void Start()
        {
            // 불러오기: 세이브 없으면 비활성
            if (load_game_button != null)
                load_game_button.interactable = SaveManager.HasAnySave();
        }

        // ── 버튼 콜백 ────────────────────────────────────────────────────────────

        public void OnClickNewGame()
        {
            if (save_slot_panel != null)
                save_slot_panel.ShowNewGame();
        }

        public void OnClickLoadGame()
        {
            if (!SaveManager.HasAnySave()) return;
            if (save_slot_panel != null)
                save_slot_panel.ShowLoad();
        }

        public void OnClickSettings()
        {
            if (settings_panel != null)
                settings_panel.Show();
        }

        public void OnClickQuit()
        {
            if (confirm_panel != null)
                confirm_panel.Show("게임을 종료하시겠습니까?", () => Application.Quit());
            else
                Application.Quit();
        }
    }
}
