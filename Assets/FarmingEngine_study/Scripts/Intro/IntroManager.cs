using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    public class IntroManager : MonoBehaviour
    {
        [Header("UI References")]
        public Text          story_text;
        public Text          page_counter;
        public Button        next_button;
        public Button        skip_button;
        public Image         background_image;
        public RectTransform text_container;  // TextArea — 배경 Image 컴포넌트도 여기에 붙임
        public Sprite[]      background_sprites;

        [Header("설정")]
        public float type_speed = 0.04f;

        [System.Serializable] private class SlideData { public string image; public string text; }
        [System.Serializable] private class TextStyle {
            public string text_color         = "#FFFFFF";
            public int    text_size          = 24;
            public string panel_color        = "#000000";
            public float  panel_alpha        = 0.6f;
            public float  panel_anchor_min_x = 0f;
            public float  panel_anchor_min_y = 0f;
            public float  panel_anchor_max_x = 1f;
            public float  panel_anchor_max_y = 0.35f;
        }
        [System.Serializable] private class IntroConfig {
            public List<SlideData> slides;
            public TextStyle       text_style;
        }

        private static readonly string[] DEFAULT_PAGES = {
            "황폐해진 작은 마을 '골든크로프트'.\n한때 풍요로웠지만, 오랜 가뭄과 이주로 쇠락했습니다.",
            "당신은 돌아가신 할아버지의 낡은 농장을 물려받았습니다.\n먼지 쌓인 땅, 녹슨 도구, 빈 창고만 남아있습니다.",
            "마을에 남은 몇 안 되는 주민들이 당신을 기다리고 있습니다.\n\"이 마을을 다시 살릴 수 있는 건 당신뿐이에요.\"",
            "농장을 일구고, 빵집을 열어 마을에 활기를 되돌립시다.\n\n─ 시작 ─"
        };

        private string[]  pages;
        private Sprite[]  loaded_sprites;
        private int       current_page = 0;
        private bool      is_typing    = false;
        private Coroutine type_coroutine;

        void Start()
        {
            next_button.onClick.AddListener(OnClickNext);
            skip_button.onClick.AddListener(OnClickSkip);
            StartCoroutine(LoadConfigThenStart());
        }

        private IEnumerator LoadConfigThenStart()
        {
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "intro_config.json");

            if (File.Exists(jsonPath))
            {
                var config = JsonUtility.FromJson<IntroConfig>(File.ReadAllText(jsonPath));
                if (config?.slides != null && config.slides.Count > 0)
                {
                    pages          = new string[config.slides.Count];
                    loaded_sprites = new Sprite[config.slides.Count];
                    for (int i = 0; i < config.slides.Count; i++)
                        pages[i] = config.slides[i].text;

                    ApplyTextStyle(config.text_style ?? new TextStyle());
                    yield return StartCoroutine(LoadImages(config.slides));
                }
                else
                    UseFallback();
            }
            else
                UseFallback();

            ShowPage(0);
        }

        private void ApplyTextStyle(TextStyle s)
        {
            if (story_text != null)
            {
                if (ColorUtility.TryParseHtmlString(s.text_color, out Color tc))
                    story_text.color = tc;
                story_text.fontSize = s.text_size;
            }

            if (text_container != null)
            {
                // 위치·크기 적용
                text_container.anchorMin = new Vector2(s.panel_anchor_min_x, s.panel_anchor_min_y);
                text_container.anchorMax = new Vector2(s.panel_anchor_max_x, s.panel_anchor_max_y);
                text_container.offsetMin = Vector2.zero;
                text_container.offsetMax = Vector2.zero;

                // 배경색 적용 — Image 컴포넌트가 없으면 자동 추가
                var img = text_container.GetComponent<Image>();
                if (img == null)
                    img = text_container.gameObject.AddComponent<Image>();
                if (ColorUtility.TryParseHtmlString(s.panel_color, out Color pc))
                    pc.a = s.panel_alpha;
                else
                    pc = new Color(0, 0, 0, s.panel_alpha);
                img.color = pc;
            }
        }

        private IEnumerator LoadImages(List<SlideData> slides)
        {
            for (int i = 0; i < slides.Count; i++)
            {
                if (string.IsNullOrEmpty(slides[i].image)) continue;
                string path = Path.Combine(Application.streamingAssetsPath, slides[i].image);
                if (!File.Exists(path)) continue;

                byte[] data = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2);
                if (tex.LoadImage(data))
                    loaded_sprites[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

                yield return null;
            }
        }

        private void ShowPage(int index)
        {
            current_page = index;

            if (loaded_sprites != null && index < loaded_sprites.Length && loaded_sprites[index] != null && background_image != null)
            {
                background_image.sprite = loaded_sprites[index];
                background_image.color  = Color.white;
            }
            else if (background_sprites != null && index < background_sprites.Length && background_image != null)
            {
                background_image.sprite = background_sprites[index];
                background_image.color  = Color.white;
            }

            if (page_counter != null)
                page_counter.text = (index + 1) + " / " + pages.Length;

            if (type_coroutine != null) StopCoroutine(type_coroutine);
            type_coroutine = StartCoroutine(TypeText(pages[index]));
        }

        private void UseFallback()
        {
            pages          = DEFAULT_PAGES;
            loaded_sprites = background_sprites;
        }

        private IEnumerator TypeText(string text)
        {
            is_typing = true;
            story_text.text = "";
            foreach (char c in text)
            {
                story_text.text += c;
                yield return new WaitForSeconds(type_speed);
            }
            is_typing = false;
        }

        public void OnClickNext()
        {
            if (is_typing)
            {
                if (type_coroutine != null) StopCoroutine(type_coroutine);
                story_text.text = pages[current_page];
                is_typing = false;
                return;
            }

            if (current_page >= pages.Length - 1)
            {
                GoToCharCreate();
                return;
            }

            ShowPage(current_page + 1);
        }

        public void OnClickSkip()
        {
            GoToCharCreate();
        }

        private void GoToCharCreate()
        {
            SceneNav.GoTo("Scene_CharCreate");
        }
    }
}
