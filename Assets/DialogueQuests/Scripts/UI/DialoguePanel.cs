using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FarmingEngine;

namespace DialogueQuests
{
    public class DialoguePanel : UIPanel
    {
        public DialogueMessageType type;

        [Header("Ref")]
        public Image portrait;
        public Animator portrait_animator;
        public Image title_box;
        public Text title;
        public Text text;
        public Image arrow;

        [Header("Full Portrait")]
        public Image portrait_full_left;
        public Image portrait_full_right;
        public Image portrait_name_bg_left;
        public Image portrait_name_bg_right;
        public Text portrait_name_left;
        public Text portrait_name_right;

        [Header("Type FX")]
        public bool type_fx = true;
        public float type_fx_speed = 30f;

        [Header("Choices")]
        public DialogueChoiceButton[] choices;

        private NarrativeEventLine current_line;
        private string current_text = "";
        private bool text_skipable;
        private bool event_skipable;
        
        private string set_anim = "";
        private bool should_hide = false;
        private float hide_timer = 0f;
        private int selected_arrow = 0;
        private RectTransform arrow_rect;

        private Coroutine text_anim;
        private bool text_anim_completed = true;
        private Canvas dialogue_canvas;

        private static DialoguePanel _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;

            EnsureTopCanvas();
            ApplyBakeryDialogueStyle();

            if (arrow != null)
            {
                arrow_rect = arrow.GetComponent<RectTransform>();
                arrow.enabled = false;
            }
        }

        protected override void Start()
        {
            base.Start();
            ApplyBakeryDialogueStyle();
            Hide();

            if (NarrativeManager.Get())
            {
                NarrativeManager.Get().onDialogueMessageStart += OnStart;
                NarrativeManager.Get().onDialogueMessageEnd += OnEnd;
            }

            if(NarrativeControls.Get())
            {
                NarrativeControls.Get().onPressTalk += OnClickChoice;
                NarrativeControls.Get().onPressTalk += OnClickSkip;
                NarrativeControls.Get().onPressTalkMouse += OnClickSkip;
                NarrativeControls.Get().onPressArrow += OnClickArrow;
                NarrativeControls.Get().onPressCancel += OnPressCancel;
                NarrativeControls.Get().onPressCancelMouse += OnPressCancel;
            }
        }

        protected override void Update()
        {
            base.Update();

            hide_timer += Time.deltaTime;
            if (IsVisible() && should_hide && hide_timer > 0.2f)
                Hide();

            //Set current choice visibility
            if (IsVisible() && NarrativeControls.Get().keyboard_controls)
            {
                foreach (DialogueChoiceButton button in choices)
                    button.SetHighlight(false);

                if(arrow != null)
                    arrow.enabled = HasChoices();

                if (selected_arrow >= 0 && selected_arrow < choices.Length) {

                    DialogueChoiceButton button = choices[selected_arrow];
                    button.SetHighlight(true);

                    if (arrow_rect != null)
                    {
                        arrow_rect.anchoredPosition = button.GetRect().anchoredPosition
                        + (Vector2.left * button.GetRect().rect.width * 0.5f)
                        + (Vector2.left * arrow_rect.sizeDelta.x * 0.5f);
                    }
                }
            }

            //Set anim
            if (IsVisible() && portrait_animator != null)
            {
                if (portrait_animator.runtimeAnimatorController != null && !string.IsNullOrEmpty(set_anim))
                {
                    if (HasParameter(portrait_animator, set_anim))
                        portrait_animator.SetTrigger(set_anim);
                    else
                        portrait_animator.Rebind();
                    set_anim = "";
                }
            }
        }

        public void SetDialogue(NarrativeEventLine line, DialogueMessage msg)
        {
            current_line = line;
            current_text = msg.GetText();
            this.text.text = current_text;

            string actorTitle = ResolveActorTitle(msg.actor);
            if (title != null)
                title.text = actorTitle;

            if (portrait_animator != null)
            {
                portrait_animator.runtimeAnimatorController = msg.actor.animation;
                set_anim = msg.anim; //Set animation next frame, or it wont work if controller was just set
            }

            if (portrait != null)
                portrait.enabled = false;
            if (title_box != null)
                title_box.enabled = false;
            if (title != null)
                title.enabled = false;

            if (portrait_full_left != null) portrait_full_left.enabled = false;
            if (portrait_full_right != null) portrait_full_right.enabled = false;
            if (portrait_name_bg_left != null) portrait_name_bg_left.enabled = false;
            if (portrait_name_bg_right != null) portrait_name_bg_right.enabled = false;
            if (portrait_name_left != null) portrait_name_left.enabled = false;
            if (portrait_name_right != null) portrait_name_right.enabled = false;

            if (msg.actor != null && msg.actor.portrait_full != null)
            {
                bool isLeft = msg.actor.portrait_side == PortraitSide.Left;
                Image fullTarget = isLeft ? portrait_full_left : portrait_full_right;
                if (fullTarget != null)
                {
                    fullTarget.sprite = msg.actor.portrait_full;
                    fullTarget.enabled = true;
                    fullTarget.transform.SetAsFirstSibling();
                }
            }

            // Full portraits are root-level siblings of the dialogue panel.
            // Keep the illustration behind the complete box, name and body text.
            transform.SetAsLastSibling();
            RectTransform dialogueBox = text != null ? text.transform.parent as RectTransform : null;
            if (dialogueBox != null)
                dialogueBox.SetAsLastSibling();
            ApplySpeakerLayout(msg.actor != null && msg.actor.portrait_full != null, line.choices.Count > 0);

            // Always keep the speaker name inside the dialogue box. Scene-specific
            // full-portrait labels can sit outside the safe area after prefab overrides.
            if (msg.actor != null)
            {
                if (title_box != null)
                    title_box.enabled = true;
                if (title != null)
                {
                    title.text = actorTitle;
                    title.enabled = true;
                    title.transform.SetAsLastSibling();
                }
                if (title_box != null)
                    title_box.transform.SetAsLastSibling();
                if (title != null)
                    title.transform.SetAsLastSibling();
            }

            text_skipable = line.parent.dialogue_type == DialogueMessageType.DialoguePanel;
            event_skipable = !line.parent.important;
            text_anim_completed = true;
            should_hide = false;
            selected_arrow = 0;

            if (type_fx && type_fx_speed > 1f)
            {
                text.text = "";
                gameObject.SetActive(true); //Allow starting coroutine
                text_anim_completed = false;
                text_anim = StartCoroutine(AnimateText());
            }

            foreach (DialogueChoiceButton button in choices)
                button.HideButton();

            if (line.choices.Count > 0 && choices.Length > 0)
            {
                text_skipable = false;
                for (int i = 0; i < line.choices.Count; i++)
                {
                    if (i < choices.Length)
                    {
                        DialogueChoiceButton button = choices[i];
                        DialogueChoice choice = line.choices[i];
                        button.ShowButton(i, choice);
                    }
                }
            }
        }

        public void SkipTextAnim()
        {
            this.text.text = current_text;
            text_anim_completed = true;
            if(text_anim != null)
                StopCoroutine(text_anim);
        }

        public bool IsTextAnimCompleted()
        {
            return text_anim_completed;
        }

        IEnumerator AnimateText()
        {
            for (int i = 0; i < (current_text.Length + 1); i++)
            {
                this.text.text = current_text.Substring(0, i);
                yield return new WaitForSeconds(1f/type_fx_speed);
            }
            text_anim_completed = true;
        }

        public void OnClickChoice()
        {
            if (IsVisible() && HasChoices() && IsTextAnimCompleted())
            {
                NarrativeManager.Get().SelectChoice(selected_arrow);
            }
        }

        public void OnClickSkip()
        {
            if (IsVisible() && !HasChoices() && text_skipable)
            {
                if (IsTextAnimCompleted())
                    NarrativeManager.Get().StopDialogue();
                else
                    SkipTextAnim();
            }
        }

        public void OnPressCancel()
        {
            if (IsVisible() && event_skipable)
            {
                NarrativeManager.Get().StopEvent();
                Hide();
            }
        }

        public void OnClickArrow(Vector2 arrow)
        {
            if (IsVisible() && HasChoices())
            {
                if (arrow.x > 0.5f)
                    selected_arrow++;
                if (arrow.x < -0.5f)
                    selected_arrow--;
                if (arrow.y > 0.5f)
                    selected_arrow += 2;
                if (arrow.y < -0.5f)
                    selected_arrow -= 2;

                selected_arrow = Mathf.Clamp(selected_arrow, 0, current_line.choices.Count-1);
            }
        }

        public bool HasChoices() {
            return current_line != null && current_line.choices.Count > 0;
        }

        private bool HasParameter(Animator animator, string name)
        {
            if (animator.runtimeAnimatorController != null)
            {
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.name == name)
                        return true;
                }
            }
            return false;
        }

        private void OnStart(NarrativeEventLine line, DialogueMessage msg)
        {
            if (line.parent.dialogue_type == type)
            {
                EnsureTopCanvas();
                SetDialogue(line, msg);
                Show();

                NarrativeManager.Get().PlaySFX("dialogue", msg.audio_clip);
            }
        }

        private void OnEnd(NarrativeEventLine line, DialogueMessage msg)
        {
            if (IsVisible())
            {
                if (text_anim != null)
                    StopCoroutine(text_anim);
                text.text = current_text;
                should_hide = true;
                hide_timer = 0f;
            }
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
        }

        public override void AfterHide()
        {
            if (portrait_full_left != null) portrait_full_left.enabled = false;
            if (portrait_full_right != null) portrait_full_right.enabled = false;
            if (portrait_name_bg_left != null) portrait_name_bg_left.enabled = false;
            if (portrait_name_bg_right != null) portrait_name_bg_right.enabled = false;
            if (portrait_name_left != null) portrait_name_left.enabled = false;
            if (portrait_name_right != null) portrait_name_right.enabled = false;
            if (portrait != null) portrait.enabled = false;
            if (title_box != null) title_box.enabled = false;
            if (title != null) title.enabled = false;

            base.AfterHide();
        }

        public static DialoguePanel Get()
        {
            return _instance;
        }

        private void EnsureTopCanvas()
        {
            if (dialogue_canvas == null)
                dialogue_canvas = GetComponentInParent<Canvas>();
            if (dialogue_canvas == null)
                return;

            dialogue_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dialogue_canvas.worldCamera = null;
            dialogue_canvas.overrideSorting = true;
            SortingLayer[] layers = SortingLayer.layers;
            if (layers.Length > 0)
                dialogue_canvas.sortingLayerID = layers[layers.Length - 1].id;
            dialogue_canvas.sortingOrder = 32700;
        }

        private void ApplyBakeryDialogueStyle()
        {
            RectTransform box = text != null ? text.transform.parent as RectTransform : null;
            if (box == null)
                return;

            box.anchorMin = new Vector2(0.08f, 0f);
            box.anchorMax = new Vector2(0.92f, 0f);
            box.pivot = new Vector2(0.5f, 0f);
            box.anchoredPosition = new Vector2(0f, 18f);
            box.sizeDelta = new Vector2(0f, 230f);
            box.localScale = Vector3.one;
            box.SetAsLastSibling();

            Image boxImage = box.GetComponent<Image>() ?? box.gameObject.AddComponent<Image>();
            boxImage.sprite = InventoryUITheme.RoundedRectSprite;
            boxImage.type = Image.Type.Sliced;
            boxImage.color = new Color(0.96f, 0.91f, 0.82f, 0.98f);
            boxImage.raycastTarget = true;

            Outline boxOutline = box.GetComponent<Outline>() ?? box.gameObject.AddComponent<Outline>();
            boxOutline.effectColor = InventoryUITheme.PanelBorder;
            boxOutline.effectDistance = new Vector2(3f, -3f);
            boxOutline.useGraphicAlpha = true;

            StylePortrait();
            StyleNameBadge(title_box, title);
            StyleNameBadge(portrait_name_bg_left, portrait_name_left);
            StyleNameBadge(portrait_name_bg_right, portrait_name_right);

            PositionNameBadge(title_box, title, 220f);
            if (title_box != null)
                title_box.transform.SetAsLastSibling();
            if (title != null)
                title.transform.SetAsLastSibling();

            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(220f, 24f);
            textRect.offsetMax = new Vector2(-24f, -64f);
            text.font = InventoryUITheme.BodyFont;
            text.fontSize = 30;
            text.fontStyle = FontStyle.Normal;
            text.alignment = TextAnchor.UpperLeft;
            text.color = InventoryUITheme.TextPrimary;
            text.lineSpacing = 1.15f;
            text.raycastTarget = false;

            for (int i = 0; i < choices.Length; i++)
                StyleChoice(choices[i], i);

            if (arrow != null)
            {
                arrow.color = new Color(0.89f, 0.55f, 0.22f, 1f);
                arrow.rectTransform.sizeDelta = new Vector2(30f, 30f);
            }
        }

        private void StylePortrait()
        {
            if (portrait == null)
                return;

            RectTransform portraitRect = portrait.rectTransform;
            portraitRect.anchorMin = portraitRect.anchorMax = Vector2.zero;
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = new Vector2(122f, 172f);
            portraitRect.sizeDelta = new Vector2(184f, 184f);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
        }

        private static void StyleNameBadge(Image badge, Text label)
        {
            if (badge != null)
            {
                badge.sprite = InventoryUITheme.RoundedRectSprite;
                badge.type = Image.Type.Sliced;
                badge.color = new Color(0.42f, 0.30f, 0.22f, 0.98f);
                badge.raycastTarget = false;
            }

            if (label != null)
            {
                label.font = InventoryUITheme.TitleFont;
                label.fontSize = 25;
                label.fontStyle = FontStyle.Normal;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = InventoryUITheme.SlotEmpty;
                label.raycastTarget = false;
            }
        }

        private static void PositionNameBadge(Image badge, Text label, float left)
        {
            if (badge != null)
            {
                RectTransform rect = badge.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(left, -14f);
                rect.sizeDelta = new Vector2(240f, 46f);
            }

            if (label != null)
            {
                RectTransform rect = label.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(left, -14f);
                rect.sizeDelta = new Vector2(240f, 46f);
            }
        }

        private static void StyleChoice(DialogueChoiceButton choice, int index)
        {
            if (choice == null)
                return;

            RectTransform rect = choice.GetRect();
            if (rect == null)
                rect = choice.GetComponent<RectTransform>();

            int column = index % 2;
            int row = index / 2;
            float left = column == 0 ? 0.21f : 0.60f;
            float right = column == 0 ? 0.58f : 0.97f;
            rect.anchorMin = new Vector2(left, 0f);
            rect.anchorMax = new Vector2(right, 0f);
            rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2(0f, 63f - row * 44f);
            rect.sizeDelta = new Vector2(0f, 38f);
            rect.localScale = Vector3.one;

            Image background = choice.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = InventoryUITheme.RoundedRectSprite;
                background.type = Image.Type.Sliced;
                background.color = Color.white;
            }

            Button button = choice.GetComponent<Button>();
            if (button != null)
            {
                Color normal = new Color(0.79f, 0.69f, 0.55f, 1f);
                ColorBlock colors = button.colors;
                colors.normalColor = normal;
                colors.highlightedColor = new Color(0.91f, 0.67f, 0.36f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = new Color(0.68f, 0.45f, 0.25f, 1f);
                colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.45f);
                button.colors = colors;
            }

            if (choice.highlight != null)
            {
                RectTransform highlightRect = choice.highlight.rectTransform;
                highlightRect.anchorMin = Vector2.zero;
                highlightRect.anchorMax = Vector2.one;
                highlightRect.offsetMin = new Vector2(2f, 2f);
                highlightRect.offsetMax = new Vector2(-2f, -2f);
                choice.highlight.sprite = InventoryUITheme.RoundedRectSprite;
                choice.highlight.type = Image.Type.Sliced;
                choice.highlight.color = new Color(0.93f, 0.62f, 0.28f, 0.92f);
                choice.highlight.raycastTarget = false;
                choice.highlight.transform.SetAsFirstSibling();
            }

            if (choice.text != null)
            {
                RectTransform labelRect = choice.text.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(16f, 4f);
                labelRect.offsetMax = new Vector2(-16f, -4f);
                choice.text.font = InventoryUITheme.TitleFont;
                choice.text.fontSize = 22;
                choice.text.fontStyle = FontStyle.Normal;
                choice.text.alignment = TextAnchor.MiddleLeft;
                choice.text.color = InventoryUITheme.TextPrimary;
                choice.text.raycastTarget = false;
                choice.text.transform.SetAsLastSibling();
            }
        }

        private void ApplySpeakerLayout(bool hasFullPortrait, bool hasChoices)
        {
            RectTransform box = text != null ? text.transform.parent as RectTransform : null;
            if (box == null)
                return;

            box.anchorMin = new Vector2(hasFullPortrait ? 0.18f : 0.08f, 0f);
            box.anchorMax = new Vector2(hasFullPortrait ? 0.94f : 0.92f, 0f);
            box.anchoredPosition = new Vector2(0f, 18f);
            box.sizeDelta = new Vector2(0f, 230f);
            box.SetAsLastSibling();

            float contentLeft = hasFullPortrait ? 28f : 220f;
            PositionNameBadge(title_box, title, contentLeft);

            RectTransform textRect = text.rectTransform;
            textRect.offsetMin = new Vector2(contentLeft, hasChoices ? 88f : 24f);
            textRect.offsetMax = new Vector2(-24f, -64f);

            for (int i = 0; i < choices.Length; i++)
            {
                StyleChoice(choices[i], i);
                if (!hasFullPortrait || choices[i] == null)
                    continue;

                RectTransform choiceRect = choices[i].GetRect();
                int column = i % 2;
                choiceRect.anchorMin = new Vector2(column == 0 ? 0.02f : 0.51f, 0f);
                choiceRect.anchorMax = new Vector2(column == 0 ? 0.49f : 0.98f, 0f);
            }
        }

        private static string ResolveActorTitle(ActorData actor)
        {
            if (actor == null)
                return "";

            string actorTitle = actor.GetTitle();
            if (string.IsNullOrWhiteSpace(actorTitle) || actorTitle == actor.title)
            {
                string fallback = DialogueLocalizer.Get(actor.actor_id + ".name");
                if (!string.IsNullOrWhiteSpace(fallback))
                    actorTitle = fallback;
            }
            return actorTitle;
        }
    }

}
