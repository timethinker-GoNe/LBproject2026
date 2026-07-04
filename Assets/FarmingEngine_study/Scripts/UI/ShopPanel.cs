using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    public class ShopPanel : UISlotPanel
    {
        public Text shop_title;
        public Text gold_value;
        public ShopSlot[] buy_slots;
        public ShopSlot[] sell_slots;
        public AudioClip buy_sell_audio;

        [Header("Description")]
        public Text title;
        public Text desc;
        public Text buy_cost;
        public Button button;
        public Text button_text;
        public GameObject desc_group;

        private PlayerCharacter current_player;
        private NPCShopData current_shop;
        private ShopSlot selected = null;

        private static ShopPanel instance;

        protected override bool ShouldApplyAutoSlotLayout() => false;

        protected override void Awake()
        {
            base.Awake();
            instance = this;

            for (int i = 0; i < slots.Length; i++)
                ((ShopSlot)slots[i]).Hide();

            onClickSlot += OnClickSlot;
            onRightClickSlot += OnRightClickSlot;
            onPressAccept += OnAccept;
            onPressCancel += OnCancel;
        }

        protected override void Start()
        {
            base.Start();
            ApplyShopSlotLayout();
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            ApplyShopSlotLayout();
        }

        private bool _shopLayoutApplied = false;

        private void ApplyShopSlotLayout()
        {
            if (_shopLayoutApplied) return;
            bool buyOk  = LayoutAreaSlots(buy_slots,  "shop", "buy");
            bool sellOk = LayoutAreaSlots(sell_slots, "shop", "sell");
            if (buyOk && sellOk) _shopLayoutApplied = true;
        }

        private static bool LayoutAreaSlots(ShopSlot[] areaSlots, string panelKey, string areaName)
        {
            if (areaSlots == null || areaSlots.Length == 0) return false;
            int cols; float gap, slotSize, padL, padR, padT, padB;
            if (!UILayoutConfig.TryGetAreaLayout(panelKey, areaName, out cols, out gap, out slotSize, out padL, out padR, out padT, out padB)) return false;
            string justify, align;
            UILayoutConfig.TryGetAreaAlignment(panelKey, areaName, out justify, out align);

            Canvas.ForceUpdateCanvases();
            var container = areaSlots[0].GetComponent<RectTransform>().parent as RectTransform;
            if (container == null) return false;
            float contW = container.rect.width;
            float contH = container.rect.height;
            if (contW < 1f || contH < 1f)
            {
                Debug.LogWarning($"[ShopPanel] {panelKey}/{areaName} 컨테이너 크기 미결정, Show() 시 재시도");
                return false;
            }

            int count = areaSlots.Length;
            int rows = Mathf.CeilToInt((float)count / cols);
            float contentW = Mathf.Max(1f, contW - padL - padR);
            float contentH = Mathf.Max(1f, contH - padT - padB);
            float maxCellW = Mathf.Max(1f, (contentW - gap * (cols - 1)) / cols);
            float maxCellH = Mathf.Max(1f, (contentH - gap * (rows - 1)) / rows);
            float cellSize = Mathf.Min(slotSize, Mathf.Min(maxCellW, maxCellH));
            float gridH = rows * cellSize + (rows - 1) * gap;
            float startY = padT + ShopFlexOffset(contentH, gridH, align);

            Debug.Log($"[ShopPanel] {areaName} | cont={contW:F1}x{contH:F1} | cols={cols} rows={rows} cell={cellSize:F1}");

            for (int i = 0; i < count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                int rowCount = Mathf.Min(cols, count - row * cols);
                float rowGridW = rowCount * cellSize + Mathf.Max(0f, rowCount - 1) * gap;
                float startX = padL + ShopFlexOffset(contentW, rowGridW, justify);
                var slotRt = areaSlots[i].GetComponent<RectTransform>();
                float sc = Mathf.Abs(slotRt.localScale.x) > 0.001f ? slotRt.localScale.x : 1f;
                slotRt.anchorMin = slotRt.anchorMax = new Vector2(0f, 1f);
                slotRt.pivot     = new Vector2(0f, 1f);
                slotRt.sizeDelta = new Vector2(cellSize / sc, cellSize / sc);
                slotRt.anchoredPosition = new Vector2(
                    startX + col * (cellSize + gap),
                    -(startY + row * (cellSize + gap)));
            }
            return true;
        }

        private static float ShopFlexOffset(float available, float used, string alignment)
        {
            float remain = Mathf.Max(0f, available - used);
            string v = string.IsNullOrEmpty(alignment) ? "center" : alignment.ToLowerInvariant();
            if (v == "start" || v == "flex-start") return 0f;
            if (v == "end"   || v == "flex-end")   return remain;
            return remain * 0.5f;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void RefreshPanel()
        {
            base.RefreshPanel();

            gold_value.text = "0";

            foreach (ShopSlot slot in buy_slots)
                slot.Hide();
            foreach (ShopSlot slot in sell_slots)
                slot.Hide();

            if (current_player == null) return;

            gold_value.text = current_player.SaveData.gold.ToString();

            // Buy slots — NPC가 플레이어에게 파는 목록
            int index = 0;
            if (current_shop?.for_sale != null)
            {
                foreach (ShopItemEntry entry in current_shop.for_sale)
                {
                    if (index >= buy_slots.Length) break;
                    ItemData idata = ItemData.Get(entry.id);
                    if (idata == null) continue;
                    int price = entry.price ?? idata.buy_cost;
                    buy_slots[index].SetBuySlot(idata, price);
                    buy_slots[index].SetSelected(selected == buy_slots[index]);
                    index++;
                }
            }

            // Sell slots — 플레이어가 NPC에게 팔 수 있는 목록
            index = 0;
            foreach (KeyValuePair<int, InventoryItemData> pair in current_player.InventoryData.items)
            {
                if (index >= sell_slots.Length) break;
                InventoryItemData invItem = pair.Value;
                ItemData idata = ItemData.Get(invItem?.item_id);
                if (idata == null) continue;
                bool can_sell = CanSell(idata);
                int sell_price = GetSellPrice(idata);
                sell_slots[index].SetSellSlot(idata, sell_price, invItem.quantity, can_sell);
                sell_slots[index].SetSelected(selected == sell_slots[index]);
                index++;
            }

            // Description
            ItemData select_item = selected?.GetItem();
            desc_group.SetActive(select_item != null);
            if (select_item != null)
            {
                title.text = select_item.title;
                desc.text  = select_item.desc;
                bool is_sell = selected.IsSell();
                int cost = is_sell ? GetSellPrice(select_item) : GetBuyPrice(select_item);
                buy_cost.text    = cost.ToString();
                button_text.text = is_sell ? "SELL" : "BUY";
                button.interactable = is_sell
                    ? (cost > 0 && CanSell(select_item))
                    : (cost <= current_player.SaveData.gold);
            }

            // Gamepad
            PlayerControls controls = PlayerControls.Get(current_player.player_id);
            if (UISlotPanel.GetFocusedPanel() != this && controls.IsGamePad())
                Focus();
        }

        private bool CanSell(ItemData item)
        {
            if (current_shop != null) return current_shop.CanSell(item);
            return item.sell_able;
        }

        private int GetSellPrice(ItemData item)
        {
            if (current_shop != null) return current_shop.GetSellPrice(item);
            return item.sell_cost;
        }

        private int GetBuyPrice(ItemData item)
        {
            if (current_shop != null) return current_shop.GetBuyPrice(item);
            return item.buy_cost;
        }

        public void ShowShop(PlayerCharacter player, NPCShopData shopData)
        {
            current_player = player;
            current_shop   = shopData;
            shop_title.text = shopData?.shop_title ?? "";
            selected = null;
            RefreshPanel();
            Show();
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            current_player = null;
            current_shop   = null;
        }

        private void OnClickSlot(UISlot islot)
        {
            ShopSlot slot = (ShopSlot)islot;
            selected = (slot != null && slot.GetItem() != null && selected != slot) ? slot : null;
            RefreshPanel();
        }

        private void OnAccept(UISlot islot)
        {
            if (selected == islot) OnClickBuy();
            else OnClickSlot(islot);
        }

        private void OnCancel(UISlot islot)
        {
            if (selected != null) selected = null;
            else Hide();
        }

        public void OnClickBuy()
        {
            if (selected == null) return;
            bool is_sell = selected.IsSell();
            ItemData item = selected.GetItem();
            if (item == null) return;

            if (is_sell)
            {
                int sell_price = GetSellPrice(item);
                if (current_player.InventoryData.HasItem(item.id, 1) && sell_price > 0 && CanSell(item))
                {
                    current_player.SaveData.gold += sell_price;
                    current_player.InventoryData.RemoveItem(item.id, 1);
                    TheAudio.Get().PlaySFX("shop", buy_sell_audio);
                }
            }
            else
            {
                int buy_price = GetBuyPrice(item);
                if (current_player.SaveData.gold >= buy_price)
                {
                    current_player.SaveData.gold -= buy_price;
                    current_player.Inventory.GainItem(item, 1);
                    TheAudio.Get().PlaySFX("shop", buy_sell_audio);
                }
            }
            RefreshPanel();
        }

        private void OnRightClickSlot(UISlot islot) { }

        public PlayerCharacter GetPlayer() => current_player;

        public static ShopPanel Get() => instance;

        public static bool IsAnyVisible() => instance && instance.IsVisible();
    }
}
