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

                // 클릭 이벤트
                BreadRecipeData captured = recipe;
                Button btn = obj.GetComponent<Button>();
                if (btn == null) btn = obj.AddComponent<Button>();
                btn.onClick.AddListener(() => SelectRecipe(captured));

                recipe_buttons.Add(obj);
            }
        }

        private void SelectRecipe(BreadRecipeData recipe)
        {
            selected_recipe = recipe;

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

                ingredient_slots.Add(obj);
            }
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

        // ─── 프리팹 없을 때 폴백 UI 생성 ─────────────────
        private GameObject CreateFallbackRecipeButton(Transform parent)
        {
            var obj = new GameObject("RecipeBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 180);

            var img = obj.GetComponent<Image>();
            img.color = new Color(0.25f, 0.22f, 0.18f, 1f);

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(obj.transform, false);
            var iconRT = icon.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0, 0); iconRT.anchorMax = new Vector2(0, 1);
            iconRT.offsetMin = new Vector2(24, 24); iconRT.offsetMax = new Vector2(156, -24);

            var name = new GameObject("Name", typeof(RectTransform), typeof(Text));
            name.transform.SetParent(obj.transform, false);
            var nameRT = name.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0); nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = new Vector2(180, 12); nameRT.offsetMax = new Vector2(-24, -12);
            var txt = name.GetComponent<Text>();
            txt.fontSize = 48; txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;

            return obj;
        }

        private GameObject CreateFallbackIngredientSlot(Transform parent, ItemData item, int count, bool hasEnough)
        {
            var obj = new GameObject("IngSlot", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(192, 192);

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
            countTxt.text = $"×{count}"; countTxt.fontSize = 39;
            countTxt.alignment = TextAnchor.MiddleCenter; countTxt.color = Color.white;

            return obj;
        }
    }
}
