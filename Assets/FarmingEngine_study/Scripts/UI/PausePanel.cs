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
            ApplyBakeryStyle();
        }

        protected override void Start()
        {
            base.Start();
            ApplyBakeryStyle();
        }

        protected override bool ShouldApplyAutoSlotLayout() => false;

        private void ApplyBakeryStyle()
        {
            Image overlay = GetComponent<Image>();
            if (overlay != null)
            {
                overlay.sprite = null;
                overlay.color = new Color(0.20f, 0.14f, 0.10f, 0.58f);
            }

            Image card = GetOrCreateImage("CleanPauseCard");
            RectTransform cardRect = card.rectTransform;
            cardRect.anchorMin = cardRect.anchorMax = Vector2.one * 0.5f;
            cardRect.pivot = Vector2.one * 0.5f;
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(360f, 420f);
            card.sprite = InventoryUITheme.RoundedRectSprite;
            card.type = Image.Type.Sliced;
            card.color = new Color(0.96f, 0.91f, 0.82f, 0.99f);
            card.raycastTarget = false;
            card.transform.SetAsFirstSibling();
            Outline outline = card.GetComponent<Outline>() ?? card.gameObject.AddComponent<Outline>();
            outline.effectColor = InventoryUITheme.PanelBorder;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = true;

            Text title = GetOrCreateText("CleanPauseTitle");
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = titleRect.anchorMax = Vector2.one * 0.5f;
            titleRect.pivot = Vector2.one * 0.5f;
            titleRect.anchoredPosition = new Vector2(0f, 166f);
            titleRect.sizeDelta = new Vector2(300f, 48f);
            title.text = "메뉴";
            title.font = InventoryUITheme.TitleFont;
            title.fontSize = 28;
            title.fontStyle = FontStyle.Normal;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = InventoryUITheme.TextPrimary;
            title.raycastTarget = false;
            title.transform.SetAsLastSibling();

            StyleMenuButton("NewButton", "계속하기", 104f);
            StyleMenuButton("SaveButton", "저장", 46f);
            StyleMenuButton("LoadButton", "불러오기", -12f);
            StyleMenuButton("SettingsButton", "설정", -70f);
            StyleMenuButton("BackToMenuButton", "메인 메뉴", -128f);

            if (speaker_btn != null)
            {
                RectTransform speakerRect = speaker_btn.rectTransform;
                speakerRect.anchorMin = speakerRect.anchorMax = Vector2.one * 0.5f;
                speakerRect.pivot = Vector2.one * 0.5f;
                speakerRect.anchoredPosition = new Vector2(142f, 166f);
                speakerRect.sizeDelta = new Vector2(34f, 34f);
                speaker_btn.preserveAspect = true;
                speaker_btn.color = InventoryUITheme.TextMuted;
                speaker_btn.transform.SetAsLastSibling();
            }
        }

        private void StyleMenuButton(string childName, string label, float y)
        {
            Transform child = transform.Find(childName);
            if (child == null)
                return;

            RectTransform rect = child as RectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
            rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(280f, 48f);
            rect.localScale = Vector3.one;

            Image image = child.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = InventoryUITheme.RoundedRectSprite;
                image.type = Image.Type.Sliced;
                image.color = new Color(0.76f, 0.66f, 0.53f, 1f);
            }

            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.88f, 0.68f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = new Color(0.76f, 0.58f, 0.40f, 1f);
                colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
                button.colors = colors;
                button.targetGraphic = image;
            }

            Text text = child.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
                text.font = InventoryUITheme.BodyFont;
                text.fontSize = 18;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = InventoryUITheme.TextPrimary;
                text.raycastTarget = false;
            }

            foreach (Image state in child.GetComponentsInChildren<Image>(true))
            {
                if (state == image || state.name != "Highlight")
                    continue;
                state.sprite = InventoryUITheme.RoundedRectSprite;
                state.type = Image.Type.Sliced;
                state.color = new Color(0.92f, 0.58f, 0.25f, 0.45f);
            }

            child.SetAsLastSibling();
        }

        private Image GetOrCreateImage(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                GameObject go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
                child = go.transform;
            }
            return child.GetComponent<Image>();
        }

        private Text GetOrCreateText(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                GameObject go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                go.transform.SetParent(transform, false);
                child = go.transform;
            }
            return child.GetComponent<Text>();
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
