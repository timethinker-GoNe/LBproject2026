using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 제빵 UI 패널.
    /// 왼쪽: 레시피 목록 / 오른쪽: 재료 확인 + 베이킹 타이머 + 결과물 슬롯
    /// BakingPanelSetupEditor 메뉴로 씬에 계층구조를 자동 생성하세요.
    /// </summary>
    public class BakingPanel : UIPanel
    {
        [Header("레시피 목록 (왼쪽)")]
        public Transform recipe_list_content;       // VerticalLayoutGroup Content
        public GameObject recipe_button_prefab;     // BakingRecipeButton 프리팹 (Editor 생성)

        [Header("레시피 상세 (오른쪽)")]
        public Text recipe_title_text;
        public Text recipe_desc_text;
        public Transform ingredients_container;     // HorizontalLayoutGroup
        public GameObject ingredient_slot_prefab;   // 아이콘+수량 프리팹 (Editor 생성)

        [Header("베이킹 컨트롤")]
        public Button bake_button;
        public Slider progress_slider;
        public Text status_text;
        public Image output_icon;
        public Button output_button;
        public Text output_quantity_text;

        // ─────────────────────────────────────────
        private PlayerCharacter player;
        private BakeryOven oven;
        private BreadRecipeData selected_recipe;

        private bool is_baking = false;
        private float bake_timer = 0f;
        private float bake_end_time = 0f;
        private BreadRecipeData pending_result = null;  // 완료 대기 중인 레시피

        private List<GameObject> recipe_buttons = new List<GameObject>();
        private Dictionary<GameObject, BreadRecipeData> recipe_button_recipes = new Dictionary<GameObject, BreadRecipeData>();
        private List<GameObject> ingredient_slots = new List<GameObject>();

        private static BakingPanel _instance;
        public static BakingPanel Get() => _instance;

        // ─────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();

            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[BakingPanel] Duplicate BakingPanel disabled: " + name, this);
                gameObject.SetActive(false);
                return;
            }

            _instance = this;

            ApplyBakingStyle();

            if (bake_button != null)
                bake_button.onClick.AddListener(OnClickBake);
            if (output_button != null)
                output_button.onClick.AddListener(OnClickOutput);

            Button closeBtn = transform.Find("TitleBar/CloseButton")?.GetComponent<Button>();
            if (closeBtn != null)
                closeBtn.onClick.AddListener(() => Hide());
        }

        protected override void Start()
        {
            base.Start();
            Hide();
        }

        protected override void Update()
        {
            base.Update();

            UpdateBakingTimer();

            if (!IsVisible()) return;

            // 거리 체크 - 오븐과 너무 멀어지면 닫기
            if (oven != null && player != null)
            {
                Selectable sel = oven.GetComponent<Selectable>();
                if (sel != null)
                {
                    float dist = (sel.transform.position - player.transform.position).magnitude;
                    if (dist > sel.GetUseRange(player) * 1.4f)
                    {
                        Hide();
                        return;
                    }
                }
            }

        }

        // ─────────────────────────────────────────
        public void ShowBaking(PlayerCharacter character, BakeryOven bakeryOven)
        {
            player = character;
            oven = bakeryOven;

            ApplyBakingStyle();

            BreadRecipeData.Load();
            BuildRecipeList();

            if (pending_result != null)
            {
                UpdateBakingTimer();
                SelectRecipe(pending_result);
                RefreshBakingStateUI();
            }
            else
            {
                ClearRightPanel();
                SetOutputVisible(false);
                if (progress_slider != null)
                    progress_slider.value = 0f;
            }

            Show();
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            selected_recipe = null;
        }

        public override void AfterHide()
        {
            // BakingPanel owns active baking progress, so it must stay active while hidden.
            // UIPanel.AfterHide() disables the GameObject, which stops Update().
        }

        // ─────────────────────────────────────────
        private void BuildRecipeList()
        {
            if (recipe_list_content == null) return;

            foreach (var btn in recipe_buttons)
                Destroy(btn);
            recipe_buttons.Clear();
            recipe_button_recipes.Clear();

            List<BreadRecipeData> recipes = BreadRecipeData.GetAll();
            foreach (var recipe in recipes)
            {
                if (!recipe.unlocked_by_default) continue;

                GameObject obj;
                if (recipe_button_prefab != null)
                {
                    obj = Instantiate(recipe_button_prefab, recipe_list_content);
                }
                else
                {
                    obj = CreateFallbackRecipeButton(recipe_list_content);
                }

                // 아이콘/이름 설정
                Image iconImg = obj.transform.Find("Icon")?.GetComponent<Image>();
                Text nameText = obj.transform.Find("Name")?.GetComponent<Text>();
                if (iconImg != null && recipe.icon != null) iconImg.sprite = recipe.icon;
                if (nameText != null) nameText.text = recipe.title;

                recipe_button_recipes[obj] = recipe;
                StyleRecipeButton(obj, recipe == selected_recipe);

                // 클릭 이벤트
                BreadRecipeData captured = recipe;
                Button btn = obj.GetComponent<Button>();
                if (btn == null) btn = obj.AddComponent<Button>();
                btn.onClick.AddListener(() => SelectRecipe(captured));

                recipe_buttons.Add(obj);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(recipe_list_content as RectTransform);
        }

        private void SelectRecipe(BreadRecipeData recipe)
        {
            selected_recipe = recipe;

            foreach (GameObject recipeButton in recipe_buttons)
            {
                if (recipeButton != null && recipe_button_recipes.TryGetValue(recipeButton, out BreadRecipeData buttonRecipe))
                    StyleRecipeButton(recipeButton, buttonRecipe == selected_recipe);
            }

            if (recipe_title_text != null) recipe_title_text.text = recipe.title;
            if (recipe_desc_text != null) recipe_desc_text.text = recipe.desc;

            BuildIngredientSlots(recipe);
            RefreshBakeButton();
        }

        private void BuildIngredientSlots(BreadRecipeData recipe)
        {
            if (ingredients_container == null) return;

            foreach (var slot in ingredient_slots)
                Destroy(slot);
            ingredient_slots.Clear();

            Dictionary<ItemData, int> required = recipe.GetRequiredCounts();
            foreach (var pair in required)
            {
                bool hasEnough = player != null && player.Inventory.HasItem(pair.Key, pair.Value);

                GameObject obj;
                if (ingredient_slot_prefab != null)
                {
                    obj = Instantiate(ingredient_slot_prefab, ingredients_container);
                }
                else
                {
                    obj = CreateFallbackIngredientSlot(ingredients_container, pair.Key, pair.Value, hasEnough);
                    ingredient_slots.Add(obj);
                    continue;
                }

                Image iconImg = obj.transform.Find("Icon")?.GetComponent<Image>();
                Text countText = obj.transform.Find("Count")?.GetComponent<Text>();
                Image bg = obj.GetComponent<Image>();

                if (iconImg != null && pair.Key.icon != null) iconImg.sprite = pair.Key.icon;
                if (countText != null) countText.text = $"×{pair.Value}";
                if (bg != null) bg.color = hasEnough ? new Color(0.2f, 0.6f, 0.2f, 0.8f) : new Color(0.6f, 0.2f, 0.2f, 0.8f);

                StyleIngredientSlot(obj, hasEnough);

                ingredient_slots.Add(obj);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(ingredients_container as RectTransform);
        }

        private void RefreshBakeButton()
        {
            if (bake_button == null) return;
            bool canBake = !is_baking && selected_recipe != null && pending_result == null && CanAffordRecipe(selected_recipe);
            bake_button.interactable = canBake;

            if (status_text != null && !is_baking && pending_result == null)
                status_text.text = canBake ? "베이킹 가능!" : (selected_recipe != null ? "재료가 부족합니다" : "레시피를 선택하세요");
        }

        private bool CanAffordRecipe(BreadRecipeData recipe)
        {
            if (player == null || recipe == null) return false;
            Dictionary<ItemData, int> required = recipe.GetRequiredCounts();
            foreach (var pair in required)
            {
                if (!player.Inventory.HasItem(pair.Key, pair.Value))
                    return false;
            }
            return true;
        }

        private void OnClickBake()
        {
            if (!CanAffordRecipe(selected_recipe) || is_baking || pending_result != null) return;

            // 재료 차감
            Dictionary<ItemData, int> required = selected_recipe.GetRequiredCounts();
            foreach (var pair in required)
                player.Inventory.InventoryData.RemoveItem(pair.Key.id, pair.Value);

            // 타이머 시작
            pending_result = selected_recipe;
            bake_timer = 0f;
            bake_end_time = Time.time + Mathf.Max(pending_result.bake_duration, 0.01f);
            is_baking = true;

            if (progress_slider != null) progress_slider.value = 0f;
            if (bake_button != null) bake_button.interactable = false;
            if (status_text != null) status_text.text = $"굽는 중... {Mathf.CeilToInt(pending_result.bake_duration)}초";

            SetOutputVisible(false);
        }

        private void UpdateBakingTimer()
        {
            if (!is_baking || pending_result == null)
                return;

            float duration = Mathf.Max(pending_result.bake_duration, 0.01f);
            if (bake_end_time <= 0f)
                bake_end_time = Time.time + Mathf.Max(duration - bake_timer, 0f);

            bake_timer = Mathf.Clamp(duration - (bake_end_time - Time.time), 0f, duration);

            if (Time.time >= bake_end_time)
            {
                bake_timer = duration;
                FinishBaking();
            }
            else if (IsVisible())
            {
                RefreshBakingStateUI();
            }
        }

        private void FinishBaking()
        {
            is_baking = false;
            RefreshBakingStateUI();
        }

        private void RefreshBakingStateUI()
        {
            if (pending_result == null)
                return;

            float duration = Mathf.Max(pending_result.bake_duration, 0.01f);

            if (progress_slider != null)
                progress_slider.value = is_baking ? Mathf.Clamp01(bake_timer / duration) : 1f;

            if (bake_button != null)
                bake_button.interactable = false;

            if (is_baking)
            {
                if (status_text != null)
                {
                    int remaining = Mathf.CeilToInt(duration - bake_timer);
                    status_text.text = $"굽는 중... {remaining}초";
                }
                SetOutputVisible(false);
            }
            else
            {
                if (status_text != null)
                    status_text.text = "완성! 클릭해서 가져가세요";
                SetOutputVisible(true, pending_result);
            }
        }

        private void OnClickOutput()
        {
            if (pending_result == null || player == null) return;
            player.Inventory.GainItem(pending_result.result_item, pending_result.result_quantity);
            pending_result = null;
            bake_timer = 0f;
            bake_end_time = 0f;
            SetOutputVisible(false);
            if (progress_slider != null) progress_slider.value = 0f;
            if (status_text != null) status_text.text = "레시피를 선택하세요";
            RefreshBakeButton();
        }

        private void SetOutputVisible(bool visible, BreadRecipeData recipe = null)
        {
            if (output_icon != null)
            {
                output_icon.gameObject.SetActive(visible);
                if (visible && recipe != null && recipe.result_item != null)
                    output_icon.sprite = recipe.result_item.icon;
            }
            if (output_button != null) output_button.interactable = visible;
            if (output_quantity_text != null)
            {
                output_quantity_text.gameObject.SetActive(visible);
                if (visible && recipe != null)
                    output_quantity_text.text = recipe.result_quantity > 1 ? $"×{recipe.result_quantity}" : "";
            }
        }

        private void ClearRightPanel()
        {
            if (recipe_title_text != null) recipe_title_text.text = "레시피를 선택하세요";
            if (recipe_desc_text != null) recipe_desc_text.text = "";
            foreach (var slot in ingredient_slots) Destroy(slot);
            ingredient_slots.Clear();
            RefreshBakeButton();
        }

        private void ApplyBakingStyle()
        {
            RectTransform root = GetComponent<RectTransform>();
            root.anchorMin = root.anchorMax = Vector2.one * 0.5f;
            root.pivot = Vector2.one * 0.5f;
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(740f, 540f);
            root.localScale = Vector3.one;

            StyleSurface(gameObject, new Color(0.78f, 0.67f, 0.53f, 0.98f), true);
            Outline outline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
            outline.effectColor = InventoryUITheme.PanelBorder;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = true;

            Transform titleBar = transform.Find("TitleBar");
            if (titleBar != null)
            {
                SetRect(titleBar as RectTransform, new Vector2(0f, 1f), Vector2.one,
                    new Vector2(0f, -52f), Vector2.zero, new Vector2(0.5f, 1f));
                StyleSurface(titleBar.gameObject, new Color(0.42f, 0.30f, 0.22f, 1f), true);
                StyleText(titleBar.Find("TitleText")?.GetComponent<Text>(), 24, InventoryUITheme.SlotEmpty, TextAnchor.MiddleCenter, FontStyle.Normal, true);

                Button close = titleBar.Find("CloseButton")?.GetComponent<Button>();
                if (close != null)
                {
                    RectTransform closeRect = close.GetComponent<RectTransform>();
                    closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 0.5f);
                    closeRect.pivot = Vector2.one * 0.5f;
                    closeRect.anchoredPosition = new Vector2(-27f, 0f);
                    closeRect.sizeDelta = new Vector2(34f, 34f);
                    StyleButton(close, new Color(0.52f, 0.36f, 0.25f, 1f));
                    StyleText(close.GetComponentInChildren<Text>(), 18, InventoryUITheme.SlotEmpty, TextAnchor.MiddleCenter, FontStyle.Bold);
                }
            }

            Transform left = transform.Find("LeftPanel");
            if (left != null)
            {
                SetRect(left as RectTransform, Vector2.zero, new Vector2(0.38f, 1f),
                    new Vector2(14f, 14f), new Vector2(-7f, -64f));
                StyleSurface(left.gameObject, new Color(0.30f, 0.22f, 0.17f, 1f), true);
                Text leftTitle = left.Find("LeftTitle")?.GetComponent<Text>();
                StyleText(leftTitle, 20, InventoryUITheme.SlotEmpty, TextAnchor.MiddleLeft, FontStyle.Normal, true);
                SetTopRange(leftTitle?.rectTransform, 6f, 44f, 14f, 14f);
                SetOffsets(left.Find("RecipeScrollRect") as RectTransform, new Vector2(9f, 9f), new Vector2(-9f, -48f));

                VerticalLayoutGroup recipeLayout = recipe_list_content != null
                    ? recipe_list_content.GetComponent<VerticalLayoutGroup>() : null;
                if (recipeLayout != null)
                {
                    recipeLayout.padding = new RectOffset(4, 4, 4, 4);
                    recipeLayout.spacing = 10f;
                    recipeLayout.childAlignment = TextAnchor.UpperCenter;
                    recipeLayout.childControlWidth = true;
                    recipeLayout.childControlHeight = true;
                    recipeLayout.childForceExpandWidth = true;
                    recipeLayout.childForceExpandHeight = false;
                }
            }

            Transform right = transform.Find("RightPanel");
            if (right != null)
            {
                SetRect(right as RectTransform, new Vector2(0.38f, 0f), Vector2.one,
                    new Vector2(7f, 14f), new Vector2(-14f, -64f));
                StyleSurface(right.gameObject, new Color(0.97f, 0.93f, 0.85f, 1f), true);

                SetTopRange(recipe_title_text?.rectTransform, 12f, 48f, 18f, 18f);
                StyleText(recipe_title_text, 23, InventoryUITheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Normal, true);
                SetTopRange(recipe_desc_text?.rectTransform, 50f, 86f, 18f, 18f);
                StyleText(recipe_desc_text, 15, InventoryUITheme.TextMuted, TextAnchor.UpperLeft, FontStyle.Normal);

                Text ingredientsLabel = right.Find("IngredientsLabel")?.GetComponent<Text>();
                SetTopRange(ingredientsLabel?.rectTransform, 90f, 118f, 18f, 18f);
                StyleText(ingredientsLabel, 17, InventoryUITheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Normal, true);
                SetTopRange(ingredients_container as RectTransform, 122f, 198f, 18f, 18f);
                HorizontalLayoutGroup ingredientLayout = ingredients_container != null
                    ? ingredients_container.GetComponent<HorizontalLayoutGroup>() : null;
                if (ingredientLayout != null)
                {
                    ingredientLayout.spacing = 12f;
                    ingredientLayout.childAlignment = TextAnchor.MiddleLeft;
                    ingredientLayout.childControlWidth = true;
                    ingredientLayout.childControlHeight = true;
                    ingredientLayout.childForceExpandWidth = false;
                    ingredientLayout.childForceExpandHeight = false;
                }

                RectTransform divider = right.Find("Divider") as RectTransform;
                SetTopRange(divider, 206f, 208f, 18f, 18f);
                Image dividerImage = divider != null ? divider.GetComponent<Image>() : null;
                if (dividerImage != null) dividerImage.color = new Color(0.55f, 0.41f, 0.30f, 0.45f);

                SetTopBox(bake_button?.GetComponent<RectTransform>(), 18f, 218f, 160f, 44f);
                StyleButton(bake_button, new Color(0.70f, 0.47f, 0.25f, 1f));
                StyleText(bake_button?.GetComponentInChildren<Text>(), 17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                SetTopRange(status_text?.rectTransform, 218f, 262f, 194f, 18f);
                StyleText(status_text, 14, InventoryUITheme.TextMuted, TextAnchor.MiddleLeft, FontStyle.Bold);

                SetTopRange(progress_slider?.GetComponent<RectTransform>(), 274f, 290f, 18f, 18f);
                StyleProgressSlider();

                Text outputLabel = right.Find("OutputLabel")?.GetComponent<Text>();
                SetTopRange(outputLabel?.rectTransform, 304f, 334f, 18f, 18f);
                StyleText(outputLabel, 17, InventoryUITheme.TextPrimary, TextAnchor.MiddleLeft, FontStyle.Normal, true);
                SetTopBox(output_button?.GetComponent<RectTransform>(), 18f, 340f, 96f, 96f);
                StyleButton(output_button, new Color(0.88f, 0.78f, 0.63f, 1f));
                if (output_icon != null)
                {
                    output_icon.preserveAspect = true;
                    output_icon.raycastTarget = false;
                }
                StyleText(output_quantity_text, 14, InventoryUITheme.TextPrimary, TextAnchor.LowerRight, FontStyle.Bold);
            }
        }

        private void StyleProgressSlider()
        {
            if (progress_slider == null)
                return;

            Image background = progress_slider.transform.Find("Background")?.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = InventoryUITheme.RoundedRectSprite;
                background.type = Image.Type.Sliced;
                background.color = new Color(0.69f, 0.60f, 0.49f, 0.65f);
            }

            Image fill = progress_slider.fillRect != null ? progress_slider.fillRect.GetComponent<Image>() : null;
            if (fill != null)
            {
                fill.sprite = InventoryUITheme.RoundedRectSprite;
                fill.type = Image.Type.Sliced;
                fill.color = new Color(0.79f, 0.43f, 0.20f, 1f);
            }
        }

        private void StyleRecipeButton(GameObject obj, bool selected)
        {
            LayoutElement layout = obj.GetComponent<LayoutElement>() ?? obj.AddComponent<LayoutElement>();
            layout.minHeight = 64f;
            layout.preferredHeight = 64f;
            layout.flexibleWidth = 1f;

            HorizontalLayoutGroup row = obj.GetComponent<HorizontalLayoutGroup>() ?? obj.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(12, 12, 10, 10);
            row.spacing = 16f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            Color normal = selected
                ? new Color(0.76f, 0.49f, 0.25f, 1f)
                : new Color(0.47f, 0.33f, 0.24f, 1f);

            Image bg = obj.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = InventoryUITheme.RoundedRectSprite;
                bg.type = Image.Type.Sliced;
                bg.color = normal;
            }

            Button button = obj.GetComponent<Button>();
            StyleButton(button, normal);

            Transform markTransform = obj.transform.Find("SelectedMark");
            GameObject markObject = markTransform != null
                ? markTransform.gameObject
                : new GameObject("SelectedMark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            markObject.transform.SetParent(obj.transform, false);
            RectTransform markRect = markObject.GetComponent<RectTransform>();
            markRect.anchorMin = new Vector2(0f, 0.14f);
            markRect.anchorMax = new Vector2(0f, 0.86f);
            markRect.pivot = new Vector2(0f, 0.5f);
            markRect.anchoredPosition = new Vector2(4f, 0f);
            markRect.sizeDelta = new Vector2(5f, 0f);
            Image mark = markObject.GetComponent<Image>();
            mark.sprite = InventoryUITheme.RoundedRectSprite;
            mark.type = Image.Type.Sliced;
            mark.color = new Color(1f, 0.76f, 0.34f, 1f);
            mark.raycastTarget = false;
            markObject.SetActive(selected);
            LayoutElement markLayout = markObject.GetComponent<LayoutElement>() ?? markObject.AddComponent<LayoutElement>();
            markLayout.ignoreLayout = true;

            Image icon = obj.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                RectTransform rect = icon.rectTransform;
                rect.sizeDelta = new Vector2(44f, 44f);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                LayoutElement iconLayout = icon.GetComponent<LayoutElement>() ?? icon.gameObject.AddComponent<LayoutElement>();
                iconLayout.minWidth = iconLayout.preferredWidth = 44f;
                iconLayout.minHeight = iconLayout.preferredHeight = 44f;
                iconLayout.flexibleWidth = 0f;
                iconLayout.flexibleHeight = 0f;
                icon.transform.SetSiblingIndex(0);
            }

            Text name = obj.transform.Find("Name")?.GetComponent<Text>();
            if (name != null)
            {
                StyleText(name, 18, InventoryUITheme.SlotEmpty, TextAnchor.MiddleLeft, FontStyle.Bold);
                LayoutElement nameLayout = name.GetComponent<LayoutElement>() ?? name.gameObject.AddComponent<LayoutElement>();
                nameLayout.minWidth = 0f;
                nameLayout.minHeight = 44f;
                nameLayout.preferredHeight = 44f;
                nameLayout.flexibleWidth = 1f;
                name.transform.SetSiblingIndex(icon != null ? 1 : 0);
            }

            markObject.transform.SetAsLastSibling();
        }

        private void StyleIngredientSlot(GameObject obj, bool hasEnough)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(70f, 70f);
            LayoutElement layout = obj.GetComponent<LayoutElement>() ?? obj.AddComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = 70f;
            layout.minHeight = layout.preferredHeight = 70f;

            Image bg = obj.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = InventoryUITheme.RoundedRectSprite;
                bg.type = Image.Type.Sliced;
                bg.color = hasEnough
                    ? new Color(0.79f, 0.84f, 0.62f, 1f)
                    : new Color(0.84f, 0.62f, 0.55f, 1f);
            }

            Image icon = obj.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                RectTransform iconRect = icon.rectTransform;
                iconRect.anchorMin = new Vector2(0.16f, 0.26f);
                iconRect.anchorMax = new Vector2(0.84f, 0.90f);
                iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
                icon.preserveAspect = true;
            }
            StyleText(obj.transform.Find("Count")?.GetComponent<Text>(), 15,
                InventoryUITheme.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private static void StyleSurface(GameObject target, Color color, bool rounded)
        {
            Image image = target.GetComponent<Image>() ?? target.AddComponent<Image>();
            image.sprite = rounded ? InventoryUITheme.RoundedRectSprite : null;
            image.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
        }

        private static void StyleButton(Button button, Color normal)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = InventoryUITheme.RoundedRectSprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = Color.Lerp(normal, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(normal, Color.black, 0.12f);
            colors.selectedColor = Color.Lerp(normal, Color.white, 0.1f);
            colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.45f);
            button.colors = colors;
        }

        private static void StyleText(Text text, int size, Color color, TextAnchor alignment, FontStyle style, bool title = false)
        {
            if (text == null)
                return;
            text.font = title ? InventoryUITheme.TitleFont : InventoryUITheme.BodyFont;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.raycastTarget = false;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Vector2? pivot = null)
        {
            if (rect == null)
                return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            if (pivot.HasValue) rect.pivot = pivot.Value;
        }

        private static void SetOffsets(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null)
                return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetTopRange(RectTransform rect, float top, float bottom,
            float left, float right = 24f)
        {
            if (rect == null)
                return;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, -bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetTopBox(RectTransform rect, float left, float top, float width, float height)
        {
            if (rect == null)
                return;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        // ─── 프리팹 없을 때 폴백 UI 생성 ─────────────────
        private GameObject CreateFallbackRecipeButton(Transform parent)
        {
            var obj = new GameObject("RecipeBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 64);

            var img = obj.GetComponent<Image>();
            img.color = new Color(0.47f, 0.33f, 0.24f, 1f);

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(obj.transform, false);
            var iconRT = icon.GetComponent<RectTransform>();
            iconRT.anchorMin = iconRT.anchorMax = new Vector2(0, 0.5f);
            iconRT.pivot = Vector2.one * 0.5f;
            iconRT.anchoredPosition = new Vector2(35f, 0f);
            iconRT.sizeDelta = new Vector2(44f, 44f);

            var name = new GameObject("Name", typeof(RectTransform), typeof(Text));
            name.transform.SetParent(obj.transform, false);
            var nameRT = name.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0); nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = new Vector2(180, 12); nameRT.offsetMax = new Vector2(-24, -12);
            var txt = name.GetComponent<Text>();
            txt.font = InventoryUITheme.BodyFont;
            txt.fontSize = 18; txt.color = InventoryUITheme.SlotEmpty;
            txt.alignment = TextAnchor.MiddleLeft;

            return obj;
        }

        private GameObject CreateFallbackIngredientSlot(Transform parent, ItemData item, int count, bool hasEnough)
        {
            var obj = new GameObject("IngSlot", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(84, 84);

            var bg = obj.GetComponent<Image>();
            bg.color = hasEnough ? new Color(0.2f, 0.6f, 0.2f, 0.8f) : new Color(0.6f, 0.2f, 0.2f, 0.8f);

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(obj.transform, false);
            var iconRT = icon.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.1f, 0.2f); iconRT.anchorMax = new Vector2(0.9f, 0.9f);
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
            var iconImg = icon.GetComponent<Image>();
            if (item.icon != null) iconImg.sprite = item.icon;

            var countObj = new GameObject("Count", typeof(RectTransform), typeof(Text));
            countObj.transform.SetParent(obj.transform, false);
            var countRT = countObj.GetComponent<RectTransform>();
            countRT.anchorMin = new Vector2(0, 0); countRT.anchorMax = new Vector2(1, 0.25f);
            countRT.offsetMin = countRT.offsetMax = Vector2.zero;
            var countTxt = countObj.GetComponent<Text>();
            countTxt.font = InventoryUITheme.BodyFont;
            countTxt.text = $"×{count}"; countTxt.fontSize = 15;
            countTxt.alignment = TextAnchor.MiddleCenter; countTxt.color = InventoryUITheme.TextPrimary;

            StyleIngredientSlot(obj, hasEnough);

            return obj;
        }
    }
}
