using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    [System.Serializable]
    public class FullInventoryUIConfig
    {
        public float ancMinX = 0.12f, ancMinY = 0.18f;
        public float ancMaxX = 0.88f, ancMaxY = 0.88f;
        public int   slotCount = 30, cols = 6;
        public float cellSize = 54f, cellSpacing = 4f;
        public bool slotFlex = true;
        public float slotPad = 6f;
        public float slotPadLeft = 6f, slotPadRight = 6f;
        public float slotPadTop = 6f, slotPadBottom = 6f;
        public string slotJustify = "start";
        public string slotAlign = "start";
        public string panelBgSprite   = "";
        public string slotBgSprite    = "";
        public string closeBtnSprite  = "";
        public string headerBarSprite = "";
    }

    /// <summary>
    /// 별도 팝업 인벤토리 창. I 키로 열고 닫는다.
    /// InventoryPanel(하단 숏컷 바)과는 독립적으로 동작.
    /// 슬롯은 Awake 시점에 동적으로 생성한다.
    /// 키보드 내비게이션: 화살표/WASD 이동, Space/Enter 선택,
    ///   U(또는 RightShift) = 아이템 액션 메뉴, Q = 퀵슬롯 등록
    /// </summary>
    public class FullInventoryPanel : ItemSlotPanel
    {
        [Header("풀 인벤토리 설정")]
        public int slot_count  = 30;
        public int cols        = 6;
        public int slot_offset = 9;
        public Text title_text;

        [Header("스타일 스프라이트 (Inspector에서 할당 — 미할당 시 기본 색상 사용)")]
        public Sprite panelBgSprite;    // Panel_big.png  (9-slice 권장)
        public Sprite slotBgSprite;     // Slot.png
        public Sprite closeBtnSprite;   // Button_close.png
        public Sprite headerBarSprite;  // bar_long2.png

        // 런타임 JSON 설정 (Resources/UIDesignConfig.json)
        private FullInventoryUIConfig cfg = new FullInventoryUIConfig();

        private const float HeaderHeight = 48f;
        private const float ContentTop = 66f;
        private const float PanelHorizontalPadding = 24f;
        private const float PanelBottomPadding = 22f;

        private int nav_index = 0;

        private static List<FullInventoryPanel> panel_list = new List<FullInventoryPanel>();

        public static bool IsAnyVisible()
        {
            foreach (var p in panel_list)
                if (p != null && p.IsVisible()) return true;
            return false;
        }

        // ─── 초기화 ───────────────────────────────────────────────────────
        protected override void Awake()
        {
            LoadConfig();
            ApplyQuickbarOffset();
            SetupPanelRect();
            BuildVisuals();
            slots = BuildSlots();
            base.Awake();
            panel_list.Add(this);
            unfocus_when_out = true;
            slots_per_row    = cols;

            for (int i = 0; i < slots.Length; i++)
                slots[i].index = i + slot_offset;
            selection_index = slot_offset;

            Hide(true);
        }

        private void ApplyQuickbarOffset()
        {
            int quickbarCount;
            if (UILayoutConfig.TryGetSlotCount("quickbar", out quickbarCount))
                slot_offset = Mathf.Clamp(quickbarCount, 1, 20);
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override bool ShouldApplyAutoSlotLayout()
        {
            return false;
        }

        public override void InitPanel()
        {
            base.InitPanel();
            if (!IsInventorySet())
            {
                PlayerCharacter player = GetPlayer();
                if (player != null && PlayerData.Get() != null && PlayerData.Get().HasInventory(player.player_id))
                    SetInventory(InventoryType.Inventory, player.InventoryData.uid, slot_offset + slot_count);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            panel_list.Remove(this);
        }

        // ─── JSON 설정 로드 (StreamingAssets/UIDesignConfig.json) ────────
        private void LoadConfig()
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "UIDesignConfig.json");
            if (!System.IO.File.Exists(path)) { slot_count = cfg.slotCount; cols = cfg.cols; return; }
            try
            {
                var text = System.IO.File.ReadAllText(path);
                var root = Newtonsoft.Json.Linq.JObject.Parse(text);
                var node = root["fullinv"] as Newtonsoft.Json.Linq.JObject ?? root;
                Newtonsoft.Json.JsonConvert.PopulateObject(node.ToString(), cfg);
            }
            catch { }
            SanitizeConfig();
            slot_count = cfg.slotCount;
            cols       = cfg.cols;
            Debug.LogFormat("[FullInventoryConfig] path={0} count={1} cols={2} cell={3:0.##} gap={4:0.##} justify={5} align={6}",
                path, cfg.slotCount, cfg.cols, cfg.cellSize, cfg.cellSpacing, cfg.slotJustify, cfg.slotAlign);
        }

        private void SanitizeConfig()
        {
            cfg.slotCount = Mathf.Clamp(cfg.slotCount, 1, 100);
            cfg.cols = Mathf.Clamp(cfg.cols, 1, 20);
            cfg.cellSize = Mathf.Clamp(cfg.cellSize, 20f, 96f);
            cfg.cellSpacing = Mathf.Clamp(cfg.cellSpacing, 0f, 40f);
            cfg.slotPadLeft = Mathf.Clamp(cfg.slotPadLeft, 0f, 160f);
            cfg.slotPadRight = Mathf.Clamp(cfg.slotPadRight, 0f, 160f);
            cfg.slotPadTop = Mathf.Clamp(cfg.slotPadTop, 0f, 160f);
            cfg.slotPadBottom = Mathf.Clamp(cfg.slotPadBottom, 0f, 160f);
        }

        private Sprite GetSprite(Sprite inspectorSprite, string configName)
        {
            if (inspectorSprite != null) return inspectorSprite;
            if (string.IsNullOrEmpty(configName)) return null;
            string streamingPath = System.IO.Path.Combine(Application.streamingAssetsPath, "UISprites", configName);
            string projectPath = System.IO.Path.Combine(Application.dataPath, "FarmingEngine_study", "Sprites", "UI", configName);
            string filePath = ResolveSpriteFilePath(streamingPath) ?? ResolveSpriteFilePath(projectPath);
            if (filePath == null) return null;

            byte[]    data = System.IO.File.ReadAllBytes(filePath);
            Texture2D tex  = new Texture2D(2, 2);
            tex.LoadImage(data);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100f);
        }

        private static string ResolveSpriteFilePath(string pathNoExt)
        {
            if (System.IO.File.Exists(pathNoExt)) return pathNoExt;
            if (System.IO.File.Exists(pathNoExt + ".png")) return pathNoExt + ".png";
            if (System.IO.File.Exists(pathNoExt + ".jpg")) return pathNoExt + ".jpg";
            if (System.IO.File.Exists(pathNoExt + ".jpeg")) return pathNoExt + ".jpeg";
            return null;
        }

        // ─── 패널 크기 설정 ────────────────────────────────────────────────
        private void SetupPanelRect()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) return;

            int rows = Mathf.CeilToInt(slot_count / (float)cols);
            float gridWidth = cols * cfg.cellSize + Mathf.Max(0, cols - 1) * cfg.cellSpacing;
            float gridHeight = rows * cfg.cellSize + Mathf.Max(0, rows - 1) * cfg.cellSpacing;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 18f);
            rt.sizeDelta = new Vector2(
                gridWidth + PanelHorizontalPadding * 2f,
                ContentTop + gridHeight + PanelBottomPadding);
        }

        // ─── 패널 비주얼 생성 (배경 + 헤더 + 닫기 버튼) ──────────────────
        private void BuildVisuals()
        {
            // Root panel owns the visual frame so the grid can stay mathematically exact.
            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.sprite = InventoryUITheme.RoundedRectSprite;
            bg.type = Image.Type.Sliced;
            bg.color  = InventoryUITheme.Panel;
            bg.raycastTarget = true;
            var panelOutline = gameObject.GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
            panelOutline.effectColor = InventoryUITheme.PanelBorder;
            panelOutline.effectDistance = new Vector2(2f, -2f);
            panelOutline.useGraphicAlpha = true;

            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(transform, false);
            var headerImg = headerGO.AddComponent<Image>();
            headerImg.sprite = InventoryUITheme.RoundedRectSprite;
            headerImg.type = Image.Type.Sliced;
            headerImg.color = new Color(0.66f, 0.53f, 0.39f, 0.98f);
            headerImg.raycastTarget = false;
            var headerRt = headerGO.GetComponent<RectTransform>();
            headerRt.anchorMin        = new Vector2(0f, 1f);
            headerRt.anchorMax        = new Vector2(1f, 1f);
            headerRt.pivot            = new Vector2(0.5f, 1f);
            headerRt.offsetMin        = new Vector2(0f, -HeaderHeight);
            headerRt.offsetMax        = new Vector2(0f,   0f);

            // 3. 타이틀 텍스트
            var titleGO  = new GameObject("TitleText");
            titleGO.transform.SetParent(headerGO.transform, false);
            var titleTxt = titleGO.AddComponent<Text>();
            titleTxt.text      = "인벤토리";
            titleTxt.font      = InventoryUITheme.TitleFont;
            titleTxt.fontSize  = 19;
            titleTxt.fontStyle = FontStyle.Normal;
            titleTxt.color     = InventoryUITheme.SlotEmpty;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.raycastTarget = false;
            FullRect(titleGO.GetComponent<RectTransform>());
            title_text = titleTxt;

            // 4. 닫기 버튼
            var closeBtnGO  = new GameObject("CloseButton");
            closeBtnGO.transform.SetParent(headerGO.transform, false);
            var closeBtnImg = closeBtnGO.AddComponent<Image>();
            var closeBtnRt  = closeBtnGO.GetComponent<RectTransform>();
            closeBtnRt.anchorMin        = new Vector2(1f, 0.5f);
            closeBtnRt.anchorMax        = new Vector2(1f, 0.5f);
            closeBtnRt.pivot            = new Vector2(1f, 0.5f);
            closeBtnRt.sizeDelta        = new Vector2(34f, 34f);
            closeBtnRt.anchoredPosition = new Vector2(-5f, 0f);

            closeBtnImg.sprite = InventoryUITheme.RoundedRectSprite;
            closeBtnImg.type = Image.Type.Sliced;
            closeBtnImg.color = new Color(0.94f, 0.88f, 0.76f, 1f);
            var xGO  = new GameObject("X");
            xGO.transform.SetParent(closeBtnGO.transform, false);
            var xTxt = xGO.AddComponent<Text>();
            xTxt.text      = "×";
            xTxt.font      = InventoryUITheme.BodyFont;
            xTxt.fontSize  = 20;
            xTxt.color     = InventoryUITheme.TextMuted;
            xTxt.alignment = TextAnchor.MiddleCenter;
            xTxt.raycastTarget = false;
            FullRect(xGO.GetComponent<RectTransform>());

            var closeBtn = closeBtnGO.AddComponent<Button>();
            closeBtn.targetGraphic = closeBtnImg;
            var bc = closeBtn.colors;
            bc.highlightedColor = new Color(0.91f, 0.69f, 0.44f, 1f);
            bc.pressedColor     = new Color(0.82f, 0.54f, 0.31f, 1f);
            closeBtn.colors = bc;
            closeBtn.onClick.AddListener(() => Hide());

            // 5. 헤더/콘텐츠 구분선 (1px)
            var sepGO  = new GameObject("Separator");
            sepGO.transform.SetParent(transform, false);
            var sepImg = sepGO.AddComponent<Image>();
            sepImg.color = InventoryUITheme.PanelBorder;
            var sepRt  = sepGO.GetComponent<RectTransform>();
            sepRt.anchorMin        = new Vector2(0f, 1f);
            sepRt.anchorMax        = new Vector2(1f, 1f);
            sepRt.pivot            = new Vector2(0.5f, 1f);
            sepRt.offsetMin        = new Vector2(0f, -HeaderHeight - 1f);
            sepRt.offsetMax        = new Vector2(0f, -HeaderHeight);
        }

        // ─── 슬롯 그리드 생성 ─────────────────────────────────────────────
        private UISlot[] BuildSlots()
        {
            var grid = GetComponentInChildren<GridLayoutGroup>();
            if (grid == null)
            {
                var gridGO = new GameObject("SlotContainer");
                gridGO.transform.SetParent(transform, false);
                var gridRt = gridGO.AddComponent<RectTransform>();
                gridRt.anchorMin = gridRt.anchorMax = new Vector2(0.5f, 1f);
                gridRt.pivot = new Vector2(0.5f, 1f);
                grid = gridGO.AddComponent<GridLayoutGroup>();
            }

            grid.transform.SetAsFirstSibling();

            int rows = Mathf.CeilToInt(slot_count / (float)cols);
            RectTransform exactGridRect = grid.GetComponent<RectTransform>();
            exactGridRect.anchorMin = exactGridRect.anchorMax = new Vector2(0.5f, 1f);
            exactGridRect.pivot = new Vector2(0.5f, 1f);
            exactGridRect.anchoredPosition = new Vector2(0f, -ContentTop);
            exactGridRect.sizeDelta = new Vector2(
                cols * cfg.cellSize + Mathf.Max(0, cols - 1) * cfg.cellSpacing,
                rows * cfg.cellSize + Mathf.Max(0, rows - 1) * cfg.cellSpacing);

            var slotContainerImg = grid.gameObject.GetComponent<Image>() ?? grid.gameObject.AddComponent<Image>();
            slotContainerImg.sprite = null;
            slotContainerImg.color = Color.clear;
            slotContainerImg.raycastTarget = false;
            RectMask2D existingMask = grid.GetComponent<RectMask2D>();
            if (existingMask != null)
                existingMask.enabled = false;

            ApplyGridLayout(grid);

            Debug.LogFormat(
                "[FullInventoryLayout] anchors=({0:0.###},{1:0.###})-({2:0.###},{3:0.###}) count={4} cols={5} cell={6:0.##} gap={7:0.##} flex={8} padding=({9},{10},{11},{12}) justify={13} align={14}",
                cfg.ancMinX, cfg.ancMinY, cfg.ancMaxX, cfg.ancMaxY,
                slot_count, cols, cfg.cellSize, cfg.cellSpacing, cfg.slotFlex,
                grid.padding.left, grid.padding.right, grid.padding.top, grid.padding.bottom,
                cfg.slotJustify, cfg.slotAlign);

            var result = new UISlot[slot_count];
            for (int i = 0; i < slot_count; i++)
                result[i] = CreateItemSlot(grid.transform, i);
            return result;
        }

        private void RefreshLayoutFromConfig()
        {
            LoadConfig();
            SetupPanelRect();

            var grid = GetComponentInChildren<GridLayoutGroup>();
            if (grid == null) return;

            ApplyGridLayout(grid);
            if (slots != null)
            {
                foreach (UISlot slot in slots)
                {
                    RectTransform rt = slot != null ? slot.GetComponent<RectTransform>() : null;
                    if (rt != null)
                        rt.sizeDelta = new Vector2(cfg.cellSize, cfg.cellSize);
                }

                if (slots.Length != slot_count)
                {
                    Debug.LogWarningFormat(
                        "[FullInventoryLayout] slotCount changed in JSON ({0}) but runtime has {1}. Restart Play Mode to rebuild slots.",
                        slot_count, slots.Length);
                }
            }

            slots_per_row = cols;
            LayoutRebuilder.ForceRebuildLayoutImmediate(grid.GetComponent<RectTransform>());
            RectTransform gridRt = grid.GetComponent<RectTransform>();
            RectTransform panelRt = GetComponent<RectTransform>();
            Debug.LogFormat(
                "[FullInventoryRuntime] panelRect={0:0.##}x{1:0.##} gridRect={2:0.##}x{3:0.##} gridCell={4:0.##} spacing={5:0.##} padding=({6},{7},{8},{9}) alignment={10}",
                panelRt != null ? panelRt.rect.width : 0f,
                panelRt != null ? panelRt.rect.height : 0f,
                gridRt != null ? gridRt.rect.width : 0f,
                gridRt != null ? gridRt.rect.height : 0f,
                grid.cellSize.x,
                grid.spacing.x,
                grid.padding.left, grid.padding.right, grid.padding.top, grid.padding.bottom,
                grid.childAlignment);
        }

        private void ApplyGridLayout(GridLayoutGroup grid)
        {
            grid.cellSize        = new Vector2(cfg.cellSize, cfg.cellSize);
            grid.spacing         = new Vector2(cfg.cellSpacing, cfg.cellSpacing);
            grid.padding         = new RectOffset(0, 0, 0, 0);
            grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment  = TextAnchor.UpperCenter;
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;
        }

        private static TextAnchor GetGridAlignment(string justify, string align)
        {
            string h = string.IsNullOrEmpty(justify) ? "start" : justify.ToLowerInvariant();
            string v = string.IsNullOrEmpty(align) ? "start" : align.ToLowerInvariant();

            bool left = h == "start" || h == "flex-start";
            bool right = h == "end" || h == "flex-end";
            bool top = v == "start" || v == "flex-start";
            bool bottom = v == "end" || v == "flex-end";

            if (top && left) return TextAnchor.UpperLeft;
            if (top && right) return TextAnchor.UpperRight;
            if (top) return TextAnchor.UpperCenter;
            if (bottom && left) return TextAnchor.LowerLeft;
            if (bottom && right) return TextAnchor.LowerRight;
            if (bottom) return TextAnchor.LowerCenter;
            if (left) return TextAnchor.MiddleLeft;
            if (right) return TextAnchor.MiddleRight;
            return TextAnchor.MiddleCenter;
        }

        private ItemSlot CreateItemSlot(Transform parent, int index)
        {
            var go = new GameObject("Slot_" + index);
            go.transform.SetParent(parent, false);

            // Keep the root image transparent for Button input; visual styling is shared.
            var bg = go.AddComponent<Image>();
            bg.sprite = null;
            bg.color = new Color(1f, 1f, 1f, 0.001f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(cfg.cellSize, cfg.cellSize);

            // 선택 하이라이트
            var hlGO  = new GameObject("Highlight");
            hlGO.transform.SetParent(go.transform, false);
            var hlImg = hlGO.AddComponent<Image>();
            hlImg.color         = Color.clear;
            hlImg.raycastTarget = false;
            FullRect(hlGO.GetComponent<RectTransform>());

            // 아이템 아이콘
            var iconGO  = new GameObject("Icon");
            iconGO.transform.SetParent(go.transform, false);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.color           = Color.white;
            iconImg.enabled         = false;
            iconImg.raycastTarget   = false;
            var iconRt = iconGO.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = Vector2.one * 0.5f;
            iconRt.sizeDelta = Vector2.one * (cfg.cellSize * 0.68f);
            iconRt.anchoredPosition = Vector2.zero;
            iconImg.preserveAspect = true;

            // 수량 텍스트
            var qtyGO  = new GameObject("Qty");
            qtyGO.transform.SetParent(go.transform, false);
            var qtyTxt = qtyGO.AddComponent<Text>();
            qtyTxt.font      = InventoryUITheme.BodyFont;
            qtyTxt.fontSize  = 15;
            qtyTxt.fontStyle = FontStyle.Bold;
            qtyTxt.color     = InventoryUITheme.TextPrimary;
            qtyTxt.alignment = TextAnchor.LowerRight;
            var qtyRt = qtyGO.GetComponent<RectTransform>();
            qtyRt.anchorMin = new Vector2(0f, 0f);
            qtyRt.anchorMax = new Vector2(1f, 0.30f);
            qtyRt.offsetMin = new Vector2(2,  2);
            qtyRt.offsetMax = new Vector2(-2, 0);

            var slot       = go.AddComponent<ItemSlot>();
            slot.icon      = iconImg;
            slot.value     = qtyTxt;
            slot.highlight = hlImg;

            InventoryUITheme.StyleSlot(slot, cfg.cellSize);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = UnityEngine.UI.Selectable.Transition.None;

            return slot;
        }

        private static void FullRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ─── RefreshPanel 오버라이드 — 슬롯 offset 적용 ──────────────────
        protected override void RefreshPanel()
        {
            InventoryData inventory = GetInventory();
            if (inventory == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                int inv_i = i + slot_offset;
                InventoryItemData invdata = inventory.GetInventoryItem(inv_i);
                ItemData idata = ItemData.Get(invdata?.item_id);
                ItemSlot slot = (ItemSlot)slots[i];

                if (invdata != null && idata != null)
                {
                    slot.SetSlot(idata, invdata.quantity,
                        selected_slot == slot.index || selected_right_slot == slot.index);
                    slot.SetDurability(idata.GetDurabilityPercent(invdata.durability),
                        ShouldShowDurability(idata, invdata.durability));
                    slot.SetFilter(GetFilterLevel(idata, invdata.durability));
                }
                else if (inv_i < inventory_size)
                {
                    slot.SetSlot(null, 0, false);
                }
                else
                {
                    slot.Hide();
                }

                InventoryUITheme.RefreshSlot(slot);
            }

            ItemSlot sslot = GetSelectedSlot();
            if (sslot != null && sslot.GetItem() == null)
                CancelSelection();
        }

        public override void Show(bool instant = false)
        {
            RefreshLayoutFromConfig();

            if (!IsInventorySet())
            {
                PlayerCharacter player = PlayerCharacter.GetFirst();
                if (player != null && PlayerData.Get() != null && PlayerData.Get().HasInventory(player.player_id))
                {
                    SetInventory(InventoryType.Inventory, player.InventoryData.uid, slot_offset + slot_count);
                    SetPlayer(player);
                }
            }
            base.Show(instant);

            nav_index       = slot_offset;
            selected_slot   = slot_offset;
            selection_index = slot_offset;

            if (title_text != null)
                title_text.text = "인벤토리";
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
        }

        // ─── 업데이트 / 키보드 내비게이션 ────────────────────────────────
        protected override void Update()
        {
            base.Update();

            PlayerControls controls = PlayerControls.Get();
            if (controls == null || !IsVisible()) return;

            for (int i = 0; i < slots.Length; i++)
            {
                ItemSlot slot = slots[i] as ItemSlot;
                if (slot != null && slot.gameObject.activeSelf)
                    InventoryUITheme.RefreshSlot(slot);
            }

            if (controls.IsPressMenuCancel())
            {
                Hide();
                return;
            }

            Vector2 uiMove = controls.GetUIMove();
            if      (uiMove.x >  0.5f) MoveNav( 1,  0);
            else if (uiMove.x < -0.5f) MoveNav(-1,  0);
            else if (uiMove.y >  0.5f) MoveNav( 0, -1);
            else if (uiMove.y < -0.5f) MoveNav( 0,  1);

            if      (Input.GetKeyDown(KeyCode.D)) MoveNav( 1,  0);
            else if (Input.GetKeyDown(KeyCode.A)) MoveNav(-1,  0);
            else if (Input.GetKeyDown(KeyCode.W)) MoveNav( 0, -1);
            else if (Input.GetKeyDown(KeyCode.S)) MoveNav( 0,  1);

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                PressSlot(nav_index);

            if (Input.GetKeyDown(KeyCode.U) || controls.IsPressUIUse())
            {
                UISlot s = GetSlot(nav_index);
                if (s != null) onPressUse?.Invoke(s);
            }

            if (Input.GetKeyDown(KeyCode.Q))
                MoveToFirstQuickSlot();
        }

        // ─── 내비게이션 헬퍼 ─────────────────────────────────────────────
        private void MoveNav(int dx, int dy)
        {
            if (slots == null || slots.Length == 0) return;

            int local = nav_index - slot_offset;
            int col   = local % cols;
            int row   = local / cols;
            int rows  = Mathf.CeilToInt(slots.Length / (float)cols);

            col = Mathf.Clamp(col + dx, 0, cols - 1);
            row = Mathf.Clamp(row + dy, 0, rows - 1);

            int newLocal    = Mathf.Clamp(row * cols + col, 0, slots.Length - 1);
            nav_index       = newLocal + slot_offset;
            selected_slot   = nav_index;
            selection_index = nav_index;
        }

        private void MoveToFirstQuickSlot()
        {
            InventoryData inventory = GetInventory();
            if (inventory == null) return;

            if (inventory.GetInventoryItem(nav_index) == null) return;

            for (int i = 0; i < slot_offset; i++)
            {
                if (inventory.GetInventoryItem(i) == null)
                {
                    PlayerData.Get().SwapInventoryItems(inventory, nav_index, inventory, i);
                    return;
                }
            }
        }

        // ─── 정적 접근자 ──────────────────────────────────────────────────
        public static FullInventoryPanel Get(int player_id = 0)
        {
            foreach (var panel in panel_list)
            {
                PlayerCharacter player = panel.GetPlayer();
                if (player == null || player.player_id == player_id)
                    return panel;
            }
            return panel_list.Count > 0 ? panel_list[0] : null;
        }

        public static new List<FullInventoryPanel> GetAll()
        {
            return panel_list;
        }
    }
}
