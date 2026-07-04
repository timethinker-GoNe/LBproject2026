using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    public class PausePanel : UISlotPanel
    {
        [Header("Pause Panel")]
        public Image speaker_btn;
        public Sprite speaker_on;
        public Sprite speaker_off;

        [Header("패널 참조 (인게임)")]
        public SaveSlotPanel save_slot_panel;
        public ConfirmPanel  confirm_panel;
        public SettingsPanel settings_panel;

        private static PausePanel _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();

            if (speaker_btn != null)
                speaker_btn.sprite = PlayerData.Get().master_volume > 0.1f ? speaker_on : speaker_off;
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
        }

        // ── 버튼 콜백 ─────────────────────────────────────────────────────────────

        /// <summary>계속하기 — 퍼즈 해제</summary>
        public void OnClickResume()
        {
            TheGame.Get().Unpause();
        }

        /// <summary>저장 — 슬롯 선택 패널 열기</summary>
        public void OnClickSave()
        {
            var panel = save_slot_panel != null ? save_slot_panel : SaveSlotPanel.Get();
            if (panel != null) panel.ShowSave();
        }

        /// <summary>불러오기 — 슬롯 선택 패널 열기</summary>
        public void OnClickLoad()
        {
            var panel = save_slot_panel != null ? save_slot_panel : SaveSlotPanel.Get();
            if (panel != null) panel.ShowLoad();
        }

        /// <summary>메인 메뉴로 — 확인 후 Scene_Start 이동</summary>
        public void OnClickBackToMenu()
        {
            var cp = confirm_panel != null ? confirm_panel : ConfirmPanel.Get();
            if (cp != null)
                cp.Show("메인 메뉴로 돌아갑니까?\n저장되지 않은 내용은 사라집니다.", GoToMainMenu);
            else
                GoToMainMenu();
        }

        private void GoToMainMenu()
        {
            TheGame.Get().Unpause();
            SceneNav.GoTo("Scene_Start");
        }

        /// <summary>설정 패널 열기</summary>
        public void OnClickSettings()
        {
            var sp = settings_panel != null ? settings_panel : SettingsPanel.Get();
            if (sp != null) sp.Show();
        }

        /// <summary>음악 토글</summary>
        public void OnClickMusicToggle()
        {
            PlayerData.Get().master_volume = PlayerData.Get().master_volume > 0.1f ? 0f : 1f;
            TheAudio.Get().RefreshVolume();
        }

        // 하위 호환 — 기존 프리팹 버튼에 연결된 메서드
        public void OnClickNew()
        {
            StartCoroutine(NewRoutine());
        }

        private IEnumerator NewRoutine()
        {
            BlackPanel.Get().Show();
            yield return new WaitForSeconds(1f);
            TheGame.NewGame();
        }

        public static PausePanel Get()
        {
            return _instance;
        }
    }
}
