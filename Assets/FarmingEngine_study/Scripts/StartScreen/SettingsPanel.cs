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

        private static readonly Color TAB_ACTIVE   = new Color(0.45f, 0.38f, 0.10f, 1f);
        private static readonly Color TAB_INACTIVE = new Color(0.20f, 0.20f, 0.20f, 1f);

        private Button   listening_button  = null;
        private Coroutine listen_coroutine = null;
        private int       current_tab      = 0;

        private static SettingsPanel _instance;
        public static SettingsPanel Get() { return _instance; }

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
        }

        protected override void Start()
        {
            base.Start();
            LoadSettings();

            if (master_slider    != null) master_slider   .onValueChanged.AddListener(v => { AudioListener.volume = v; PlayerPrefs.SetFloat(KEY_MASTER,  v); });
            if (music_slider     != null) music_slider    .onValueChanged.AddListener(v => PlayerPrefs.SetFloat(KEY_MUSIC,   v));
            if (sfx_slider       != null) sfx_slider      .onValueChanged.AddListener(v => PlayerPrefs.SetFloat(KEY_SFX,    v));
            if (quality_dropdown != null) quality_dropdown.onValueChanged.AddListener(v => { QualitySettings.SetQualityLevel(v); PlayerPrefs.SetInt(KEY_QUALITY, v); });

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
            if (img != null) img.color = active ? TAB_ACTIVE : TAB_INACTIVE;
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
