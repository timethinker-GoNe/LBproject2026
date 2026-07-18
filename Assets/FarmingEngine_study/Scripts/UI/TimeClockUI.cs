using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{

    /// <summary>
    /// Clock showing days and time
    /// </summary>

    public class TimeClockUI : MonoBehaviour
    {
        public Text day_txt;
        public Text time_txt;
        public Image clock_fill;

        void Start()
        {
            ApplyHUDStyle();
        }

        private void ApplyHUDStyle()
        {
            RectTransform root = GetComponent<RectTransform>();
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(18f, -18f);
            root.sizeDelta = new Vector2(210f, 44f);
            root.localScale = Vector3.one;

            Image panel = GetComponent<Image>();
            if (panel != null)
            {
                panel.sprite = InventoryUITheme.RoundedRectSprite;
                panel.type = Image.Type.Sliced;
                panel.color = new Color(0.27f, 0.22f, 0.19f, 0.78f);
                panel.raycastTarget = false;
            }

            Outline panelOutline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.67f, 0.53f, 0.40f, 0.42f);
            panelOutline.effectDistance = new Vector2(1f, -1f);
            panelOutline.useGraphicAlpha = true;

            if (clock_fill != null)
            {
                clock_fill.enabled = false;
            }

            Image clockIcon = GetOrCreateClockIcon();
            clockIcon.sprite = InventoryUITheme.ClockIcon;
            clockIcon.preserveAspect = true;
            clockIcon.color = Color.white;
            clockIcon.raycastTarget = false;
            RectTransform iconRect = clockIcon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(24f, 0f);
            iconRect.sizeDelta = new Vector2(36f, 36f);

            Image dayIcon = GetOrCreateIcon("CleanDayIcon");
            dayIcon.sprite = InventoryUITheme.DayIcon;
            dayIcon.preserveAspect = true;
            dayIcon.color = Color.white;
            dayIcon.raycastTarget = false;
            RectTransform dayIconRect = dayIcon.rectTransform;
            dayIconRect.anchorMin = dayIconRect.anchorMax = new Vector2(0f, 0.5f);
            dayIconRect.pivot = new Vector2(0.5f, 0.5f);
            dayIconRect.anchoredPosition = new Vector2(126f, 0f);
            dayIconRect.sizeDelta = new Vector2(30f, 30f);

            StyleClockText(time_txt, new Vector2(47f, 0f), new Vector2(62f, 44f), 16, TextAnchor.MiddleLeft);
            StyleClockText(day_txt, new Vector2(145f, 0f), new Vector2(55f, 44f), 15, TextAnchor.MiddleRight);
        }

        private Image GetOrCreateClockIcon()
        {
            return GetOrCreateIcon("CleanClockIcon");
        }

        private Image GetOrCreateIcon(string iconName)
        {
            Transform existing = transform.Find(iconName);
            if (existing != null)
                return existing.GetComponent<Image>();

            GameObject icon = new GameObject(iconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icon.transform.SetParent(transform, false);
            return icon.GetComponent<Image>();
        }

        private static void StyleClockText(Text text, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            if (text == null)
                return;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            text.font = InventoryUITheme.BodyFont;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = InventoryUITheme.SlotEmpty;
            text.raycastTarget = false;
            Outline outline = text.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;
        }

        void Update()
        {
            PlayerData pdata = PlayerData.Get();
            int time_hours = Mathf.FloorToInt(pdata.day_time);
            int time_secs = Mathf.FloorToInt((pdata.day_time * 60f) % 60f);

            day_txt.text = pdata.day + "일";
            time_txt.text = time_hours + ":" + time_secs.ToString("00");

            bool clockwise = pdata.day_time <= 12f;
            clock_fill.fillClockwise = clockwise;
            if (clockwise)
            {
                float value = pdata.day_time / 12f; //0f to 1f
                clock_fill.fillAmount = value;
            }
            else
            {
                float value = (pdata.day_time - 12f) / 12f; //0f to 1f
                clock_fill.fillAmount = 1f - value;
            }
        }
    }

}
