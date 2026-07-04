using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 범용 Y/N 확인 모달 패널.
    /// UIPanel 상속 — TheUI 없이 경량 씬에서도 동작.
    /// 사용법: ConfirmPanel.Get().Show("메시지", onYes, onNo);
    /// </summary>
    public class ConfirmPanel : UIPanel
    {
        [Header("UI References")]
        public Text message_text;
        public Button confirm_button;
        public Button cancel_button;

        private UnityAction on_confirm;
        private UnityAction on_cancel;

        private static ConfirmPanel _instance;
        public static ConfirmPanel Get() { return _instance; }

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        protected override void Start()
        {
            base.Start();
            confirm_button.onClick.AddListener(OnClickConfirm);
            cancel_button.onClick.AddListener(OnClickCancel);
        }

        protected override void Update()
        {
            base.Update();
            if (IsVisible() && Input.GetKeyDown(KeyCode.Escape))
                OnClickCancel();
        }

        /// <summary>확인 패널 열기</summary>
        public void Show(string message, UnityAction onConfirm, UnityAction onCancel = null)
        {
            on_confirm = onConfirm;
            on_cancel  = onCancel;

            if (message_text != null)
                message_text.text = message;

            Show();
        }

        private void OnClickConfirm()
        {
            Hide();
            on_confirm?.Invoke();
        }

        private void OnClickCancel()
        {
            Hide();
            on_cancel?.Invoke();
        }
    }
}
