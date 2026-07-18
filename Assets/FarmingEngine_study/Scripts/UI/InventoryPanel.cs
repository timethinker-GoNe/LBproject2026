using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>Main inventory quickbar.</summary>
    public class InventoryPanel : ItemSlotPanel
    {
        private const float SlotSize = 64f;
        private const float SlotGap = 6f;
        private const float HorizontalPadding = 18f;
        private const float VerticalPadding = 12f;

        private static readonly List<InventoryPanel> panel_list = new List<InventoryPanel>();
        private int visible_slot_count;
        private bool styled;

        protected override void Awake()
        {
            base.Awake();
            panel_list.Add(this);
            unfocus_when_out = true;

            for (int i = 0; i < slots.Length; i++)
                slots[i].onPressKey += OnPressShortcut;

            Hide(true);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            panel_list.Remove(this);
        }

        protected override void Start()
        {
            base.Start();
            ApplyQuickbarStyle();
        }

        protected override void Update()
        {
            base.Update();
            RefreshQuickbarVisuals();
        }

        public override void InitPanel()
        {
            base.InitPanel();
            if (IsInventorySet())
                return;

            PlayerCharacter player = GetPlayer();
            if (player == null || !PlayerData.Get().HasInventory(player.player_id))
                return;

            SetInventory(InventoryType.Inventory, player.InventoryData.uid, player.InventoryData.size);
            SetPlayer(player);
            Show(true);
            ApplyQuickbarStyle();
        }

        protected override void RefreshPanel()
        {
            base.RefreshPanel();
            RefreshQuickbarVisuals();
        }

        protected override bool ShouldApplyAutoSlotLayout()
        {
            return layoutKey != "quickbar";
        }

        private void ApplyQuickbarStyle()
        {
            if (layoutKey != "quickbar" || styled || slots == null)
                return;

            styled = true;
            visible_slot_count = Mathf.Min(GetConfiguredSlotCount(), slots.Length);

            float contentWidth = visible_slot_count * SlotSize + Mathf.Max(0, visible_slot_count - 1) * SlotGap;
            RectTransform panel = GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0f);
            panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = new Vector2(0f, 22f);
            panel.sizeDelta = new Vector2(contentWidth + HorizontalPadding * 2f, SlotSize + VerticalPadding * 2f);

            HideLegacyChrome();
            InventoryUITheme.StylePanel(transform, "CleanQuickbarPanel");

            float startX = -contentWidth * 0.5f + SlotSize * 0.5f;
            for (int i = 0; i < slots.Length; i++)
            {
                UISlot slot = slots[i];
                bool visible = i < visible_slot_count;
                slot.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                RectTransform slotRect = slot.GetComponent<RectTransform>();
                slotRect.anchorMin = slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.localScale = Vector3.one;
                slotRect.sizeDelta = Vector2.one * SlotSize;
                slotRect.anchoredPosition = new Vector2(startX + i * (SlotSize + SlotGap), 0f);
                InventoryUITheme.StyleSlot((ItemSlot)slot, SlotSize, i < 9 ? (i + 1).ToString() : "0");
            }

            slots_per_row = visible_slot_count;
            RefreshQuickbarVisuals();
        }

        private int GetConfiguredSlotCount()
        {
            int count;
            return UILayoutConfig.TryGetSlotCount(layoutKey, out count) ? Mathf.Clamp(count, 1, 10) : 9;
        }

        private void HideLegacyChrome()
        {
            foreach (Transform child in transform)
            {
                if (child.name == "BG" || child.name == "Back" || child.name == "QuickbarBackplate" ||
                    child.name == "QuickbarShadow" || child.name == "QuickbarTopLine")
                    child.gameObject.SetActive(false);
            }
        }

        private void RefreshQuickbarVisuals()
        {
            if (!styled || slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (i >= visible_slot_count)
                {
                    slots[i].gameObject.SetActive(false);
                    continue;
                }

                ItemSlot slot = slots[i] as ItemSlot;
                if (slot != null && slot.gameObject.activeSelf)
                    InventoryUITheme.RefreshSlot(slot);
            }
        }

        private void OnPressShortcut(UISlot slot)
        {
            CancelSelection();
            PressSlot(slot.index);
        }

        public static InventoryPanel Get(int player_id = 0)
        {
            foreach (InventoryPanel panel in panel_list)
            {
                PlayerCharacter player = panel.GetPlayer();
                if (player != null && player.player_id == player_id)
                    return panel;
            }
            return null;
        }

        public static new List<InventoryPanel> GetAll()
        {
            return panel_list;
        }
    }
}
