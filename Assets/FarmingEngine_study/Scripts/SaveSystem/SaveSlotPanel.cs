using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 세이브 슬롯 목록 UI 패널.
    /// 모드에 따라 동작 분기:
    ///   NewGame — 슬롯 선택 후 CharCreateData.selected_slot 저장 → Scene_Intro 이동
    ///   Save    — 선택 슬롯에 현재 게임 저장 (인게임 PausePanel에서 사용)
    ///   Load    — 슬롯 선택 후 SaveManager.LoadFromSlot() (StartScreen + 인게임)
    /// UIPanel 상속 — TheUI 없이 경량 씬에서도 동작.
    /// </summary>
    public class SaveSlotPanel : UIPanel
    {
        public enum Mode { NewGame, Save, Load }

        [Header("UI References")]
        public Text title_text;
        public SaveSlotUI[] slot_uis;   // Inspector에서 3개 연결
        public Button close_button;

        private Mode current_mode;
        private static SaveSlotPanel _instance;
        public static SaveSlotPanel Get() { return _instance; }

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        protected override void Update()
        {
            base.Update();
            if (IsVisible() && Input.GetKeyDown(KeyCode.Escape))
                OnClickClose();
        }

        public void OnClickClose()
        {
            Hide();
        }

        // ── 외부 진입점 ──────────────────────────────────────────────────────────

        public void ShowNewGame()  => OpenPanel(Mode.NewGame);
        public void ShowSave()     => OpenPanel(Mode.Save);
        public void ShowLoad()     => OpenPanel(Mode.Load);

        private void OpenPanel(Mode mode)
        {
            current_mode = mode;

            if (title_text != null)
            {
                title_text.text = mode == Mode.NewGame ? "새 게임 — 슬롯 선택"
                                : mode == Mode.Save    ? "저장"
                                :                        "불러오기";
            }

            RefreshSlots();
            Show();
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < slot_uis.Length; i++)
            {
                SaveSlotInfo info = SaveManager.GetSlotInfo(i);
                slot_uis[i].Setup(i, info, OnSlotSelected);

                // Load 모드: 데이터 없는 슬롯 비활성
                if (current_mode == Mode.Load)
                    slot_uis[i].select_button.interactable = SaveManager.HasSave(i);
            }
        }

        private void OnSlotSelected(int index)
        {
            switch (current_mode)
            {
                case Mode.NewGame:
                    OnSelectNewGame(index);
                    break;
                case Mode.Save:
                    OnSelectSave(index);
                    break;
                case Mode.Load:
                    OnSelectLoad(index);
                    break;
            }
        }

        // ── 모드별 처리 ─────────────────────────────────────────────────────────

        private void OnSelectNewGame(int index)
        {
            bool hasExisting = SaveManager.HasSave(index);
            string msg = hasExisting
                ? "슬롯 " + (index + 1) + "의 기존 데이터를 덮어씁니다. 계속하시겠습니까?"
                : "슬롯 " + (index + 1) + "에 새 게임을 시작합니다.";

            ConfirmPanel.Get()?.Show(msg, () =>
            {
                Hide();
                CharCreateData.selected_slot = index;
                SceneNav.GoTo("Scene_Intro");
            });
        }

        private void OnSelectSave(int index)
        {
            bool hasExisting = SaveManager.HasSave(index);
            string msg = hasExisting
                ? "슬롯 " + (index + 1) + "에 덮어씁니까?"
                : "슬롯 " + (index + 1) + "에 저장합니까?";

            ConfirmPanel.Get()?.Show(msg, () =>
            {
                Hide();
                SaveManager.Get()?.SaveToSlot(index);
            });
        }

        private void OnSelectLoad(int index)
        {
            ConfirmPanel.Get()?.Show("슬롯 " + (index + 1) + "을 불러옵니까?", () =>
            {
                Hide();
                SaveManager.LoadFromSlot(index);
            });
        }
    }
}
