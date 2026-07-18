using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{

    /// <summary>
    /// Just a bar showing one of the attributes
    /// </summary>

    [RequireComponent(typeof(ProgressBar))]
    public class AttributeBar : MonoBehaviour
    {
        public AttributeType attribute;

        private PlayerUI parent_ui;
        private ProgressBar bar;

        void Awake()
        {
            parent_ui = GetComponentInParent<PlayerUI>();
            bar = GetComponent<ProgressBar>();

            if (attribute == AttributeType.Energy)
            {
                gameObject.SetActive(false);
                return;
            }

            if (attribute == AttributeType.Health)
                ApplyHealthStyle();
        }

        private void ApplyHealthStyle()
        {
            Transform legacyCircle = transform.Find("Circle");
            if (legacyCircle != null)
                legacyCircle.gameObject.SetActive(false);

            RectTransform root = GetComponent<RectTransform>();
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(18f, -64f);
            root.sizeDelta = new Vector2(210f, 40f);
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

            Image track = GetOrCreateImage("CleanHealthTrack");
            RectTransform trackRect = track.rectTransform;
            trackRect.anchorMin = trackRect.anchorMax = new Vector2(0f, 0.5f);
            trackRect.pivot = new Vector2(0f, 0.5f);
            trackRect.anchoredPosition = new Vector2(48f, 0f);
            trackRect.sizeDelta = new Vector2(116f, 12f);
            track.sprite = InventoryUITheme.RoundedRectSprite;
            track.type = Image.Type.Sliced;
            track.color = new Color(0.96f, 0.91f, 0.82f, 0.82f);
            track.raycastTarget = false;
            track.transform.SetAsFirstSibling();

            if (bar.bar_fill != null)
            {
                RectTransform fillRect = bar.bar_fill.rectTransform;
                fillRect.anchorMin = fillRect.anchorMax = new Vector2(0f, 0.5f);
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.anchoredPosition = new Vector2(48f, 0f);
                fillRect.sizeDelta = new Vector2(116f, 12f);
                bar.bar_fill.sprite = InventoryUITheme.RoundedRectSprite;
                bar.bar_fill.color = new Color(0.76f, 0.32f, 0.25f, 1f);
                bar.bar_fill.type = Image.Type.Filled;
                bar.bar_fill.fillMethod = Image.FillMethod.Horizontal;
                bar.bar_fill.fillOrigin = 0;
                bar.bar_fill.raycastTarget = false;
                bar.bar_fill.transform.SetAsLastSibling();
            }

            if (bar.bar_text != null)
            {
                RectTransform valueRect = bar.bar_text.rectTransform;
                valueRect.anchorMin = valueRect.anchorMax = new Vector2(1f, 0.5f);
                valueRect.pivot = new Vector2(1f, 0.5f);
                valueRect.anchoredPosition = new Vector2(-10f, 0f);
                valueRect.sizeDelta = new Vector2(34f, 24f);
                bar.bar_text.font = InventoryUITheme.BodyFont;
                bar.bar_text.fontSize = 14;
                bar.bar_text.fontStyle = FontStyle.Bold;
                bar.bar_text.alignment = TextAnchor.MiddleRight;
                bar.bar_text.color = InventoryUITheme.SlotEmpty;
                bar.bar_text.raycastTarget = false;
                bar.bar_text.transform.SetAsLastSibling();
            }

            Text label = GetOrCreateText("HealthLabel");
            label.gameObject.SetActive(false);

            Image healthIcon = GetOrCreateImage("CleanHealthIcon");
            healthIcon.sprite = InventoryUITheme.HealthIcon;
            healthIcon.preserveAspect = true;
            healthIcon.color = Color.white;
            healthIcon.raycastTarget = false;
            RectTransform iconRect = healthIcon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(24f, 0f);
            iconRect.sizeDelta = new Vector2(34f, 34f);
            healthIcon.transform.SetAsLastSibling();
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

            Text text = child.GetComponent<Text>();
            text.font = InventoryUITheme.BodyFont;
            return text;
        }

        void Update()
        {
            PlayerCharacter character = GetPlayer();
            if (character != null)
            {
                bar.SetMax(Mathf.RoundToInt(character.Attributes.GetAttributeMax(attribute)));
                bar.SetValue(Mathf.RoundToInt(character.Attributes.GetAttributeValue(attribute)));
            }
        }
		
		public PlayerCharacter GetPlayer()
        {
            return parent_ui ? parent_ui.GetPlayer() : PlayerCharacter.GetFirst();
        }
    }

}
