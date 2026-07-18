using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>Shows currently equipped items.</summary>
    public class EquipPanel : ItemSlotPanel
    {
        private const int Columns = 3;
        private const int VisibleSlots = 6;
        private const float SlotSize = 56f;
        private const float SlotGap = 8f;
        private const float Padding = 12f;

        private static readonly List<EquipPanel> panel_list = new List<EquipPanel>();
        private bool styled;

        protected override void Awake()
        {
            base.Awake();
            panel_list.Add(this);
            unfocus_when_out = true;
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
            ApplyEquipStyle();
        }

        protected override void Update()
        {
            base.Update();
            RefreshEquipVisuals();
        }

        public override void InitPanel()
        {
            base.InitPanel();
            if (IsInventorySet())
                return;

            PlayerCharacter player = GetPlayer();
            if (player == null || !PlayerData.Get().HasInventory(player.player_id))
                return;

            SetInventory(InventoryType.Equipment, player.EquipData.uid, player.EquipData.size);
            SetPlayer(player);
            Show(true);
            ApplyEquipStyle();
        }

        protected override void RefreshPanel()
        {
            InventoryData inventory = GetInventory();
            if (inventory != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    EquipSlotUI slot = slots[i] as EquipSlotUI;
                    if (slot == null)
                        continue;

                    InventoryItemData invdata = inventory.GetInventoryItem((int)slot.equip_slot);
                    ItemData idata = ItemData.Get(invdata?.item_id);
                    if (invdata != null && idata != null)
                    {
                        slot.SetSlot(idata, invdata.quantity, selected_slot == slot.index || selected_right_slot == slot.index);
                        slot.SetDurability(idata.GetDurabilityPercent(invdata.durability), ShouldShowDurability(idata, invdata.durability));
                        slot.SetFilter(GetFilterLevel(idata, invdata.durability));
                    }
                    else
                    {
                        slot.SetSlot(null, 0, false);
                    }
                }
            }

            RefreshEquipVisuals();
        }

        protected override bool ShouldApplyAutoSlotLayout()
        {
            return layoutKey != "equip";
        }

        private void ApplyEquipStyle()
        {
            if (layoutKey != "equip" || styled || slots == null)
                return;

            styled = true;
            int visibleCount = Mathf.Min(VisibleSlots, slots.Length);
            int rows = Mathf.CeilToInt(visibleCount / (float)Columns);
            float contentWidth = Columns * SlotSize + (Columns - 1) * SlotGap;
            float contentHeight = rows * SlotSize + (rows - 1) * SlotGap;

            RectTransform panel = GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(1f, 0f);
            panel.anchoredPosition = new Vector2(-24f, 22f);
            panel.sizeDelta = new Vector2(contentWidth + Padding * 2f, contentHeight + Padding * 2f);

            HideLegacyChrome();
            InventoryUITheme.StylePanel(transform, "CleanEquipPanel");

            float startX = -contentWidth * 0.5f + SlotSize * 0.5f;
            float startY = contentHeight * 0.5f - SlotSize * 0.5f;
            for (int i = 0; i < slots.Length; i++)
            {
                UISlot slot = slots[i];
                bool visible = i < visibleCount;
                slot.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                int col = i % Columns;
                int row = i / Columns;
                RectTransform slotRect = slot.GetComponent<RectTransform>();
                slotRect.anchorMin = slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.localScale = Vector3.one;
                slotRect.sizeDelta = Vector2.one * SlotSize;
                slotRect.anchoredPosition = new Vector2(
                    startX + col * (SlotSize + SlotGap),
                    startY - row * (SlotSize + SlotGap));
                InventoryUITheme.StyleSlot((ItemSlot)slot, SlotSize);
            }

            slots_per_row = Columns;
            RefreshEquipVisuals();
        }

        private void HideLegacyChrome()
        {
            foreach (Transform child in transform)
            {
                if (child.name == "BG" || child.name == "BG (1)" || child.name == "Back" ||
                    child.name == "EquipBackplate" || child.name == "EquipShadow")
                    child.gameObject.SetActive(false);
            }
        }

        private void RefreshEquipVisuals()
        {
            if (!styled || slots == null)
                return;

            int count = Mathf.Min(VisibleSlots, slots.Length);
            for (int i = 0; i < count; i++)
            {
                ItemSlot slot = slots[i] as ItemSlot;
                if (slot != null && slot.gameObject.activeSelf)
                    InventoryUITheme.RefreshSlot(slot);
            }
        }

        public static EquipPanel Get(int player_id = 0)
        {
            foreach (EquipPanel panel in panel_list)
            {
                PlayerCharacter player = panel.GetPlayer();
                if (player != null && player.player_id == player_id)
                    return panel;
            }
            return null;
        }

        public static new List<EquipPanel> GetAll()
        {
            return panel_list;
        }
    }
}
