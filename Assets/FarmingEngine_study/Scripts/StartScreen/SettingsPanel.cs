using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DialogueQuests;

namespace FarmingEngine
{
    /// <summary>
    /// 설정 패널. 내부 탭 3개(오디오/화면/키 설정).
    /// PlayerPrefs 기반 — PlayerData 없이 StartScreen에서도 동작.
    /// </summary>
    public class SettingsPanel : UIPanel
    {
        [Header("탭 버튼")]
        public Button tab_audio;
        public Button tab_display;
        public Button tab_keys;

        [Header("섹션 패널 (SetActive 로 전환)")]
        public GameObject section_audio;
        public GameObject section_display;
        public GameObject section_keys;

        [Header("오디오")]
        public Slider master_slider;
        public Slider music_slider;
        public Slider sfx_slider;

        [Header("그래픽")]
        public Dropdown quality_dropdown;

        [Header("키 바인딩 버튼 (각 버튼의 자식 Text에 현재 키 표시)")]
        public Button bind_action;
        public Button bind_interact;
        public Button bind_craft;
        public Button bind_inventory;
        public Button bind_jump;
        public Button bind_attack;
        public Button bind_journal;

        private const string KEY_MASTER  = "settings_master";
        private const string KEY_MUSIC   = "settings_music";
        private const string KEY_SFX     = "settings_sfx";
        private const string KEY_QUALITY = "settings_quality";

        private static readonly Color TAB_ACTIVE   = new Color(0.72f, 0.50f, 0.29f, 1f);
        private static readonly Color TAB_INACTIVE = new Color(0.72f, 0.62f, 0.49f, 1f);

        private Button   listening_button  = null;
        private Coroutine listen_coroutine = null;
        private int       current_tab      = 0;
        private Button    quality_cycle_button;
        private Text      quality_cycle_label;

        private static SettingsPanel _instance;
        public static SettingsPanel Get() { return _instance; }

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
            ApplyBakeryStyle();
        }

        protected override void Start()
        {
            base.Start();
            LoadSettings();

            if (master_slider    != null) master_slider   .onValueChanged.AddListener(v => { AudioListener.volume = v; PlayerPrefs.SetFloat(KEY_MASTER,  v); });
            if (music_slider     != null) music_slider    .onValueChanged.AddListener(v => PlayerPrefs.SetFloat(KEY_MUSIC,   v));
            if (sfx_slider       != null) sfx_slider      .onValueChanged.AddListener(v => PlayerPrefs.SetFloat(KEY_SFX,    v));
            if (quality_dropdown != null) quality_dropdown.onValueChanged.AddListener(v => { QualitySettings.SetQualityLevel(v); PlayerPrefs.SetInt(KEY_QUALITY, v); });
            SetupQualityButton();

            if (tab_audio   != null) tab_audio  .onClick.AddListener(() => ShowTab(0));
            if (tab_display != null) tab_display.onClick.AddListener(() => ShowTab(1));
            if (tab_keys    != null) tab_keys   .onClick.AddListener(() => ShowTab(2));

            SetupBindButton(bind_action,    "kb_action",    KeyCode.Space);
            SetupBindButton(bind_interact,  "kb_interact",  KeyCode.Z);
            SetupBindButton(bind_craft,     "kb_craft",     KeyCode.C);
            SetupBindButton(bind_inventory, "kb_inventory", KeyCode.I);
            SetupBindButton(bind_jump,      "kb_jump",      KeyCode.LeftControl);
            SetupBindButton(bind_attack,    "kb_attack",    KeyCode.LeftShift);
            SetupBindButton(bind_journal,   "kb_journal",   KeyCode.J);

            ApplyBakeryStyle();
            ShowTab(0);
        }

        // ─── 탭 전환 ──────────────────────────────────────────────────────────
        public void ShowTab(int index)
        {
            current_tab = index;
            if (section_audio   != null) section_audio  .SetActive(index == 0);
            if (section_display != null) section_display.SetActive(index == 1);
            if (section_keys    != null) section_keys   .SetActive(index == 2);
            RefreshTabColors();
        }

        private void RefreshTabColors()
        {
            SetTabColor(tab_audio,   current_tab == 0);
            SetTabColor(tab_display, current_tab == 1);
            SetTabColor(tab_keys,    current_tab == 2);
        }

        private static void SetTabColor(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = InventoryUITheme.RoundedRectSprite;
                img.type = Image.Type.Sliced;
                img.color = active ? TAB_ACTIVE : TAB_INACTIVE;
            }
        }

        private void SetupQualityButton()
        {
            if (quality_dropdown != null)
                return;

            Transform target = section_display != null
                ? section_display.transform.Find("Quality_Dropdown")
                : null;
            if (target == null)
                return;

            quality_cycle_button = target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
            StyleButton(quality_cycle_button);
            quality_cycle_label = target.GetComponentInChildren<Text>(true);
            if (quality_cycle_label != null)
            {
                quality_cycle_label.font = InventoryUITheme.BodyFont;
                quality_cycle_label.fontSize = 17;
                quality_cycle_label.fontStyle = FontStyle.Bold;
                quality_cycle_label.color = InventoryUITheme.TextPrimary;
                quality_cycle_label.alignment = TextAnchor.MiddleCenter;
            }

            quality_cycle_button.onClick.AddListener(CycleQuality);
            RefreshQualityLabel();
        }

        private void CycleQuality()
        {
            int count = QualitySettings.names.Length;
            if (count == 0)
                return;

            int next = (QualitySettings.GetQualityLevel() + 1) % count;
            QualitySettings.SetQualityLevel(next);
            PlayerPrefs.SetInt(KEY_QUALITY, next);
            RefreshQualityLabel();
        }

        private void RefreshQualityLabel()
        {
            if (quality_cycle_label == null)
                return;

            int count = QualitySettings.names.Length;
            int index = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, Mathf.Max(0, count - 1));
            float ratio = count <= 1 ? 1f : index / (float)(count - 1);
            quality_cycle_label.text = ratio < 0.34f ? "낮음" : ratio < 0.68f ? "보통" : "높음";
        }

        // ─── 키 바인딩 ────────────────────────────────────────────────────────
        private void SetupBindButton(Button btn, string prefKey, KeyCode defaultKey)
        {
            if (btn == null) return;
            RefreshBindLabel(btn, prefKey, defaultKey);
            btn.onClick.AddListener(() => StartListen(btn, prefKey, defaultKey));
        }

        private void RefreshBindLabel(Button btn, string prefKey, KeyCode defaultKey)
        {
            if (btn == null) return;
            KeyCode current = PlayerControls.LoadKey(prefKey, defaultKey);
            Text lbl = btn.GetComponentInChildren<Text>();
            if (lbl != null) lbl.text = current.ToString();
        }

        private void StartListen(Button btn, string prefKey, KeyCode defaultKey)
        {
            if (listen_coroutine != null) StopCoroutine(listen_coroutine);
            listening_button = btn;
            Text lbl = btn.GetComponentInChildren<Text>();
            if (lbl != null) lbl.text = "…";
            listen_coroutine = StartCoroutine(ListenForKey(btn, prefKey, defaultKey));
        }

        private IEnumerator ListenForKey(Button btn, string prefKey, KeyCode defaultKey)
        {
            yield return null;
            yield return null;

            float elapsed = 0f;
            while (elapsed < 8f)
            {
                elapsed += Time.unscaledDeltaTime;

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    RefreshBindLabel(btn, prefKey, defaultKey);
                    listening_button = null;
                    yield break;
                }

                foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (kc == KeyCode.None || kc == KeyCode.Escape) continue;
                    if (kc >= KeyCode.Mouse0 && kc <= KeyCode.Mouse6) continue;
                    if (kc >= KeyCode.JoystickButton0) continue;

                    if (Input.GetKeyDown(kc))
                    {
                        PlayerControls.SaveKey(prefKey, kc);

                        PlayerControls pc = PlayerControls.GetFirst();
                        if (pc != null) pc.LoadKeyBindings();

                        if (prefKey == "kb_journal")
                        {
                            NarrativeControls.SaveJournalKey(kc);
                            if (NarrativeControls.Get() != null)
                                NarrativeControls.Get().LoadKeyBindings();
                        }

                        Text lbl = btn.GetComponentInChildren<Text>();
                        if (lbl != null) lbl.text = kc.ToString();
                        listening_button = null;
                        yield break;
                    }
                }

                yield return null;
            }

            RefreshBindLabel(btn, prefKey, defaultKey);
            listening_button = null;
        }

        // ─── 설정 로드 ────────────────────────────────────────────────────────
        private void LoadSettings()
        {
            float master  = PlayerPrefs.GetFloat(KEY_MASTER,  1f);
            float music   = PlayerPrefs.GetFloat(KEY_MUSIC,   1f);
            float sfx     = PlayerPrefs.GetFloat(KEY_SFX,     1f);
            int   quality = PlayerPrefs.GetInt  (KEY_QUALITY, QualitySettings.GetQualityLevel());

            AudioListener.volume = master;
            QualitySettings.SetQualityLevel(quality);

            if (master_slider    != null) master_slider.value    = master;
            if (music_slider     != null) music_slider.value     = music;
            if (sfx_slider       != null) sfx_slider.value       = sfx;
            if (quality_dropdown != null) quality_dropdown.value = quality;
            RefreshQualityLabel();
        }

        private void ApplyBakeryStyle()
        {
            RectTransform root = GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.06f, 0.06f);
            root.anchorMax = new Vector2(0.94f, 0.94f);
            root.pivot = Vector2.one * 0.5f;
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = Vector2.zero;
            root.localScale = Vector3.one;

            foreach (RectTransform child in transform)
                child.localScale = Vector3.one;

            Image background = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            background.sprite = InventoryUITheme.RoundedRectSprite;
            background.type = Image.Type.Sliced;
            background.color = new Color(0.96f, 0.91f, 0.82f, 0.99f);

            Outline outline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
            outline.effectColor = InventoryUITheme.PanelBorder;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = true;

            Image header = GetOrCreateImage(transform, "CleanHeader");
            SetRect(header.rectTransform, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -62f), Vector2.zero);
            header.sprite = InventoryUITheme.RoundedRectSprite;
            header.type = Image.Type.Sliced;
            header.color = new Color(0.43f, 0.31f, 0.22f, 1f);
            header.raycastTarget = false;
            header.transform.SetAsFirstSibling();

            Text titleText = transform.Find("Title")?.GetComponent<Text>();
            if (titleText != null)
            {
                SetRect(titleText.rectTransform, new Vector2(0f, 1f), Vector2.one,
                    new Vector2(24f, -62f), new Vector2(-24f, 0f));
                titleText.font = InventoryUITheme.TitleFont;
                titleText.fontSize = 28;
                titleText.fontStyle = FontStyle.Normal;
                titleText.color = InventoryUITheme.SlotEmpty;
                titleText.alignment = TextAnchor.MiddleCenter;
            }

            RectTransform tabBar = transform.Find("TabBar") as RectTransform;
            if (tabBar != null)
                SetRect(tabBar, new Vector2(0f, 1f), Vector2.one,
                    new Vector2(24f, -116f), new Vector2(-24f, -70f));

            StyleSection(section_audio);
            StyleSection(section_display);
            StyleSection(section_keys);
            StyleBottomButton(transform.Find("Btn_ResetKeys")?.GetComponent<Button>(), false);
            StyleBottomButton(transform.Find("Btn_Close")?.GetComponent<Button>(), true);

            foreach (Text label in GetComponentsInChildren<Text>(true))
            {
                if (label == titleText)
                    continue;
                label.font = InventoryUITheme.BodyFont;
                label.color = InventoryUITheme.TextPrimary;
                label.raycastTarget = false;
            }

            foreach (Button button in GetComponentsInChildren<Button>(true))
                StyleButton(button);
            foreach (Slider slider in GetComponentsInChildren<Slider>(true))
                StyleSlider(slider);

            Transform hint = section_display != null ? section_display.transform.Find("Hint_Display") : null;
            if (hint != null)
                hint.gameObject.SetActive(false);
        }

        private static void StyleSection(GameObject section)
        {
            if (section == null)
                return;
            RectTransform rect = section.GetComponent<RectTransform>();
            SetRect(rect, Vector2.zero, Vector2.one,
                new Vector2(28f, 76f), new Vector2(-28f, -126f));
            if (section.GetComponent<RectMask2D>() == null)
                section.AddComponent<RectMask2D>();
        }

        private static void StyleBottomButton(Button button, bool right)
        {
            if (button == null)
                return;
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(right ? 1f : 0f, 0f);
            rect.pivot = new Vector2(right ? 1f : 0f, 0f);
            rect.anchoredPosition = new Vector2(right ? -24f : 24f, 18f);
            rect.sizeDelta = new Vector2(156f, 42f);
        }

        private static void StyleButton(Button button)
        {
            if (button == null)
                return;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = InventoryUITheme.RoundedRectSprite;
                image.type = Image.Type.Sliced;
                image.color = TAB_INACTIVE;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.90f, 0.72f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.78f, 0.62f, 0.45f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            button.colors = colors;
        }

        private static void StyleSlider(Slider slider)
        {
            foreach (Image image in slider.GetComponentsInChildren<Image>(true))
            {
                string lower = image.name.ToLowerInvariant();
                image.sprite = InventoryUITheme.RoundedRectSprite;
                image.type = Image.Type.Sliced;
                if (lower.Contains("fill"))
                    image.color = new Color(0.72f, 0.50f, 0.29f, 1f);
                else if (lower.Contains("handle"))
                    image.color = new Color(0.43f, 0.31f, 0.22f, 1f);
                else
                    image.color = new Color(0.82f, 0.75f, 0.64f, 1f);
            }
        }

        private static Image GetOrCreateImage(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();

            GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(parent, false);
            return child.GetComponent<Image>();
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null)
                return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        // ─── 업데이트 / 닫기 ──────────────────────────────────────────────────
        protected override void Update()
        {
            base.Update();
            if (IsVisible() && listening_button == null && Input.GetKeyDown(KeyCode.Escape))
                OnClickClose();
        }

        public void OnClickClose()
        {
            if (listen_coroutine != null)
            {
                StopCoroutine(listen_coroutine);
                listen_coroutine = null;
                listening_button = null;
            }
            PlayerPrefs.Save();
            Hide();
        }

        public void OnClickResetKeys()
        {
            string[] keys = { "kb_action", "kb_interact", "kb_craft", "kb_inventory", "kb_jump", "kb_attack", "kb_journal" };
            foreach (string k in keys) PlayerPrefs.DeleteKey(k);
            PlayerPrefs.Save();

            PlayerControls pc = PlayerControls.GetFirst();
            if (pc != null) pc.LoadKeyBindings();
            if (NarrativeControls.Get() != null) NarrativeControls.Get().LoadKeyBindings();

            RefreshBindLabel(bind_action,    "kb_action",    KeyCode.Space);
            RefreshBindLabel(bind_interact,  "kb_interact",  KeyCode.Z);
            RefreshBindLabel(bind_craft,     "kb_craft",     KeyCode.C);
            RefreshBindLabel(bind_inventory, "kb_inventory", KeyCode.I);
            RefreshBindLabel(bind_jump,      "kb_jump",      KeyCode.LeftControl);
            RefreshBindLabel(bind_attack,    "kb_attack",    KeyCode.LeftShift);
            RefreshBindLabel(bind_journal,   "kb_journal",   KeyCode.J);
        }

        public static void ApplyToTheAudio()
        {
            if (TheAudio.Get() == null) return;
            TheAudio.Get().RefreshVolume();
        }
    }
}
