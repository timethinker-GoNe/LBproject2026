using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    public static class InventoryUITheme
    {
        private static Sprite rounded_rect_sprite;
        private static Sprite clock_icon;
        private static Sprite health_icon;
        private static Sprite gold_icon;
        private static Sprite day_icon;
        private static Sprite settings_icon;
        private static Font body_font;
        private static Font title_font;

        // Warm watercolor-inspired palette: cocoa outline, beige wash and ivory paper.
        public static readonly Color Panel = new Color(0.72f, 0.62f, 0.49f, 0.94f);
        public static readonly Color PanelBorder = new Color(0.43f, 0.31f, 0.22f, 0.92f);
        public static readonly Color SlotEmpty = new Color(0.96f, 0.91f, 0.82f, 0.99f);
        public static readonly Color SlotFilled = new Color(0.89f, 0.80f, 0.67f, 1f);
        public static readonly Color SlotBorder = new Color(0.55f, 0.41f, 0.30f, 0.92f);
        public static readonly Color Hover = new Color(0.69f, 0.46f, 0.27f, 0.28f);
        public static readonly Color Selected = new Color(0.88f, 0.54f, 0.24f, 0.46f);
        public static readonly Color TextPrimary = new Color(0.25f, 0.17f, 0.12f, 1f);
        public static readonly Color TextMuted = new Color(0.46f, 0.34f, 0.25f, 1f);

        public static Sprite RoundedRectSprite
        {
            get
            {
                if (rounded_rect_sprite == null)
                    rounded_rect_sprite = CreateRoundedRectSprite();
                return rounded_rect_sprite;
            }
        }

        public static Sprite ClockIcon => clock_icon ?? (clock_icon = LoadHUDIcon("clock"));
        public static Sprite HealthIcon => health_icon ?? (health_icon = LoadHUDIcon("health"));
        public static Sprite GoldIcon => gold_icon ?? (gold_icon = LoadHUDIcon("gold"));
        public static Sprite DayIcon => day_icon ?? (day_icon = LoadHUDIcon("day"));
        public static Sprite SettingsIcon => settings_icon ?? (settings_icon = LoadHUDIcon("settings"));
        public static Font BodyFont => body_font ?? (body_font = LoadFont("Fonts/WantedSans-Regular"));
        public static Font TitleFont => title_font ?? (title_font = LoadFont("Fonts/Cafe24Ssurround-v2.0"));

        public static void ApplyBodyFont(Text text)
        {
            if (text != null)
                text.font = BodyFont;
        }

        public static void ApplyTitleFont(Text text)
        {
            if (text != null)
            {
                text.font = TitleFont;
                text.fontStyle = FontStyle.Normal;
            }
        }

        public static Image StylePanel(Transform parent, string name)
        {
            Image panel = GetOrCreateImage(parent, name);
            Stretch(panel.rectTransform, Vector2.zero, Vector2.zero);
            panel.sprite = RoundedRectSprite;
            panel.type = Image.Type.Sliced;
            panel.color = Panel;
            panel.raycastTarget = false;
            panel.transform.SetAsFirstSibling();
            SetOutline(panel.gameObject, PanelBorder, new Vector2(2f, -2f));
            return panel;
        }

        public static void StyleSlot(ItemSlot slot, float size, string shortcut = null)
        {
            if (slot == null)
                return;

            DisableLegacySlotChrome(slot);

            Image background = GetOrCreateImage(slot.transform, "CleanSlotBackground");
            Stretch(background.rectTransform, Vector2.zero, Vector2.zero);
            background.sprite = RoundedRectSprite;
            background.type = Image.Type.Sliced;
            background.color = SlotEmpty;
            background.raycastTarget = false;
            background.transform.SetAsFirstSibling();
            SetOutline(background.gameObject, SlotBorder, new Vector2(1f, -1f));

            Image hover = GetOrCreateImage(slot.transform, "CleanSlotState");
            Stretch(hover.rectTransform, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            hover.sprite = RoundedRectSprite;
            hover.type = Image.Type.Sliced;
            hover.color = Color.clear;
            hover.raycastTarget = false;
            hover.transform.SetSiblingIndex(Mathf.Min(1, slot.transform.childCount - 1));

            StyleIcon(slot.icon, size * 0.68f);
            StyleIcon(slot.default_icon, size * 0.48f);
            StyleQuantity(slot.value, size);
            StyleDurability(slot.dura, size);

            if (slot.highlight != null)
            {
                Stretch(slot.highlight.rectTransform, new Vector2(2f, 2f), new Vector2(-2f, -2f));
                slot.highlight.color = Color.clear;
                slot.highlight.raycastTarget = false;
            }

            SetShortcutLabel(slot.transform, shortcut);
            EnsureInputCatcher(slot.transform);
            ArrangeLayers(slot);
        }

        public static void RefreshSlot(ItemSlot slot)
        {
            Transform backgroundTransform = slot.transform.Find("CleanSlotBackground");
            Image background = backgroundTransform != null ? backgroundTransform.GetComponent<Image>() : null;
            if (background != null)
                background.color = slot.GetItem() != null ? SlotFilled : SlotEmpty;

            Transform stateTransform = slot.transform.Find("CleanSlotState");
            Image state = stateTransform != null ? stateTransform.GetComponent<Image>() : null;
            if (state != null)
                state.color = slot.IsSelected() ? Selected : slot.IsHover() ? Hover : Color.clear;
        }

        private static void DisableLegacySlotChrome(ItemSlot slot)
        {
            foreach (Transform child in slot.transform)
            {
                if (child.name != "BG" && child.name != "Back" && child.name != "Border" &&
                    child.name != "Frame" && child.name != "Circle" && child.name != "HoverFrame")
                    continue;

                Image image = child.GetComponent<Image>();
                if (image != null)
                {
                    image.enabled = false;
                    image.raycastTarget = false;
                }
            }
        }

        private static void StyleIcon(Image image, float size)
        {
            if (image == null)
                return;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.one * size;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static void StyleQuantity(Text text, float size)
        {
            if (text == null)
                return;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-4f, 3f);
            rect.sizeDelta = new Vector2(size * 0.55f, 20f);
            text.alignment = TextAnchor.MiddleRight;
            ApplyBodyFont(text);
            text.fontSize = 15;
            text.fontStyle = FontStyle.Bold;
            text.color = TextPrimary;
            text.raycastTarget = false;
            Outline outline = text.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;
        }

        private static void StyleDurability(Text text, float size)
        {
            if (text == null)
                return;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 3f);
            rect.sizeDelta = new Vector2(size - 8f, 16f);
            text.alignment = TextAnchor.MiddleCenter;
            ApplyBodyFont(text);
            text.fontSize = 11;
            text.color = TextPrimary;
            text.raycastTarget = false;
        }

        private static void SetShortcutLabel(Transform parent, string shortcut)
        {
            Transform existing = parent.Find("ShortcutLabel");
            if (string.IsNullOrEmpty(shortcut))
            {
                if (existing != null)
                    existing.gameObject.SetActive(false);
                return;
            }

            GameObject labelObject = existing != null
                ? existing.gameObject
                : new GameObject("ShortcutLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            labelObject.SetActive(true);

            Text label = labelObject.GetComponent<Text>();
            label.text = shortcut;
            ApplyBodyFont(label);
            label.fontSize = 12;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.UpperLeft;
            label.color = TextMuted;
            label.raycastTarget = false;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(5f, -4f);
            rect.sizeDelta = new Vector2(18f, 18f);
        }

        private static void EnsureInputCatcher(Transform parent)
        {
            Image input = GetOrCreateImage(parent, "InputCatcher");
            Stretch(input.rectTransform, Vector2.zero, Vector2.zero);
            input.sprite = null;
            input.color = new Color(1f, 1f, 1f, 0.001f);
            input.raycastTarget = true;
        }

        private static void ArrangeLayers(ItemSlot slot)
        {
            Transform background = slot.transform.Find("CleanSlotBackground");
            Transform state = slot.transform.Find("CleanSlotState");
            if (background != null)
                background.SetAsFirstSibling();
            if (state != null)
                state.SetSiblingIndex(Mathf.Min(1, slot.transform.childCount - 1));
            if (slot.default_icon != null)
                slot.default_icon.transform.SetAsLastSibling();
            if (slot.icon != null)
                slot.icon.transform.SetAsLastSibling();
            if (slot.filter != null)
                slot.filter.transform.SetAsLastSibling();
            if (slot.dura != null)
                slot.dura.transform.SetAsLastSibling();
            if (slot.value != null)
                slot.value.transform.SetAsLastSibling();

            Transform shortcut = slot.transform.Find("ShortcutLabel");
            if (shortcut != null)
                shortcut.SetAsLastSibling();
            Transform input = slot.transform.Find("InputCatcher");
            if (input != null)
                input.SetAsLastSibling();
        }

        private static Image GetOrCreateImage(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Image image = existing.GetComponent<Image>();
                return image != null ? image : existing.gameObject.AddComponent<Image>();
            }

            GameObject child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(parent, false);
            return child.GetComponent<Image>();
        }

        private static void Stretch(RectTransform rect, Vector2 minOffset, Vector2 maxOffset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = minOffset;
            rect.offsetMax = maxOffset;
        }

        private static void SetOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
                outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static Sprite CreateRoundedRectSprite()
        {
            const int size = 32;
            const float radius = 8f;
            float half = (size - 1) * 0.5f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "RuntimeRoundedRect";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x - half) - (half - radius);
                    float qy = Mathf.Abs(y - half) - (half - radius);
                    float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                    float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                    float distance = outside + inside - radius;
                    float alpha = Mathf.Clamp01(0.5f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                Vector2.one * 0.5f,
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(9f, 9f, 9f, 9f));
        }

        private static Sprite LoadHUDIcon(string iconName)
        {
            Texture2D texture = Resources.Load<Texture2D>("HUDIcons/" + iconName);
            if (texture == null)
                return null;

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                Vector2.one * 0.5f,
                100f,
                0,
                SpriteMeshType.FullRect);
        }

        private static Font LoadFont(string resourcePath)
        {
            Font font = Resources.Load<Font>(resourcePath);
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
