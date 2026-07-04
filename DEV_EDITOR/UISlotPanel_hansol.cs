using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// Basic class for any panel containing slots that can be selected
    /// </summary>

    public class UISlotPanel : UIPanel
    {
        [Header("Slot Panel")]
        public float refresh_rate = 0.1f; //For optimization, set to 0f to refresh every frame
        public int slots_per_row = 99; //Useful for gamepad controls (know how the rows/column are setup)
        public UISlot[] slots;

        public UnityAction<UISlot> onClickSlot;
        public UnityAction<UISlot> onRightClickSlot;
        public UnityAction<UISlot> onLongClickSlot;
        public UnityAction<UISlot> onDoubleClickSlot;

        public UnityAction<UISlot> onDragStart; //When you started dragging and exit the first slot
        public UnityAction<UISlot> onDragEnd; //When dragging and releasing
        public UnityAction<UISlot, UISlot> onDragTo; //When dragging slot and releasing on another slot

        public UnityAction<UISlot> onPressAccept;
        public UnityAction<UISlot> onPressCancel;
        public UnityAction<UISlot> onPressUse;

        [HideInInspector]
        public int selection_index = 0; //For gamepad selection

        [HideInInspector]
        public bool unfocus_when_out = false; //Unfocus automatically if go out of panel

        [HideInInspector]
        public bool focused = false; //Focused panel

        private float timer = 0f;
        private bool _layoutApplied = false;

        private static List<UISlotPanel> slot_panels = new List<UISlotPanel>();

        protected override void Awake()
        {
            base.Awake();
            slot_panels.Add(this);

            for (int i = 0; i < slots.Length; i++)
            {
                int index = i; //Important to copy so not overwritten in loop
                slots[i].index = index;
                slots[i].onClick += OnClickSlot;
                slots[i].onClickRight += OnClickSlotRight;
                slots[i].onClickLong += OnClickSlotLong;
                slots[i].onClickDouble += OnClickSlotDouble;

                slots[i].onDragStart += OnDragStart;
                slots[i].onDragEnd += OnDragEnd;
                slots[i].onDragTo += OnDragTo;

                slots[i].onPressAccept += OnPressAccept;
                slots[i].onPressCancel += OnPressCancel;
                slots[i].onPressUse += OnPressUse;
            }
        }

        protected override void Start()
        {
            base.Start();
            TryApplySlotLayout();
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            TryApplySlotLayout();
        }

        private void TryApplySlotLayout()
        {
            if (_layoutApplied) return;
            if (string.IsNullOrEmpty(layoutKey)) return;
            if (!ShouldApplyAutoSlotLayout()) return;
            _layoutApplied = true;
            if (!UILayoutConfig.IsSlotFlexEnabled(layoutKey))
                return;
            ApplySlotLayout();
        }

        protected virtual bool ShouldApplyAutoSlotLayout()
        {
            return true;
        }

        private void ApplySlotLayout()
        {
            int cols; float gap, pad, padLeft, padRight, padTop, padBottom;
            if (!UILayoutConfig.TryGetSlotLayout(layoutKey, out cols, out gap, out pad, out padLeft, out padRight, out padTop, out padBottom)) return;
            if (slots == null || slots.Length == 0) return;
            string justify, align;
            UILayoutConfig.TryGetSlotAlignment(layoutKey, out justify, out align);

            Canvas.ForceUpdateCanvases();

            var container = slots[0].transform.parent as RectTransform;
            if (container == null) return;

            var panelRt = GetComponent<RectTransform>();
            float contW = container.rect.width;
            float contH = container.rect.height;

            Debug.Log($"[SlotLayout] {gameObject.name} | container={container.name} | panel=({panelRt.anchorMin},{panelRt.anchorMax}) | contRect={contW:F1}x{contH:F1} | panelRect={panelRt.rect.width:F1}x{panelRt.rect.height:F1} | slot[0]pivot={slots[0].GetComponent<RectTransform>().pivot} | cols={cols} gap={gap} pad={pad} left={padLeft} right={padRight} top={padTop} bottom={padBottom} justify={justify} align={align}");

            if (contW < 1f || contH < 1f)
            {
                var canvas = GetComponentInParent<Canvas>();
                var scaler = canvas != null ? canvas.GetComponent<UnityEngine.UI.CanvasScaler>() : null;
                float refW = (scaler != null) ? scaler.referenceResolution.x : 1920f;
                float refH = (scaler != null) ? scaler.referenceResolution.y : 1080f;
                contW = (panelRt.anchorMax.x - panelRt.anchorMin.x) * refW;
                contH = (panelRt.anchorMax.y - panelRt.anchorMin.y) * refH;
                Debug.Log($"[SlotLayout] {gameObject.name} | rect 0이라 폴백: contW={contW:F1} contH={contH:F1}");
            }

            int rows = Mathf.CeilToInt((float)slots.Length / cols);
            float contentW = Mathf.Max(1f, contW - padLeft - padRight);
            float contentH = Mathf.Max(1f, contH - padTop - padBottom);
            float cellW = Mathf.Max(1f, (contentW - gap * (cols - 1)) / cols);
            float cellH = Mathf.Max(1f, (contentH - gap * (rows - 1)) / rows);
            float cellSize = Mathf.Max(10f, Mathf.Min(cellW, cellH));

            float gridH = rows * cellSize + (rows - 1) * gap;
            float startY = padTop + GetFlexOffset(contentH, gridH, align);

            Debug.Log($"[SlotLayout] {gameObject.name} | cellSize={cellSize:F1} gridH={gridH:F1} startY={startY:F1}");

            for (int i = 0; i < slots.Length; i++)
            {
                int col = i % cols;
                int row = i / cols;
                int rowStartIndex = row * cols;
                int rowSlotCount = Mathf.Min(cols, slots.Length - rowStartIndex);
                float rowGridW = rowSlotCount * cellSize + Mathf.Max(0, rowSlotCount - 1) * gap;
                float startX = padLeft + GetFlexOffset(contentW, rowGridW, justify);
                var slotRt = slots[i].GetComponent<RectTransform>();
                if (slotRt == null) continue;

                slotRt.anchorMin = slotRt.anchorMax = new Vector2(0f, 1f);
                slotRt.pivot     = new Vector2(0f, 1f);
                slotRt.sizeDelta = new Vector2(cellSize, cellSize);
                slotRt.anchoredPosition = new Vector2(
                    startX + col * (cellSize + gap),
                    -(startY + row * (cellSize + gap))
                );
            }

            slots_per_row = cols;
        }

        private static float GetFlexOffset(float available, float used, string alignment)
        {
            float remain = Mathf.Max(0f, available - used);
            string value = string.IsNullOrEmpty(alignment) ? "center" : alignment.ToLowerInvariant();
            if (value == "start" || value == "flex-start")
                return 0f;
            if (value == "end" || value == "flex-end")
                return remain;
            return remain * 0.5f;
        }

        protected virtual void OnDestroy()
        {
            slot_panels.Remove(this);
        }

        protected override void Update()
        {
            base.Update();

            timer += Time.deltaTime;
            if (IsVisible())
            {
                if (timer > refresh_rate)
                {
                    timer = 0f;
                    SlowUpdate();
                }
            }
        }

        private void SlowUpdate()
        {
            RefreshPanel();
        }

        protected virtual void RefreshPanel()
        {

        }

        public void Focus()
        {
            UnfocusAll();
            focused = true;
            UISlot slot = GetSelectSlot();
            if(slot != null && !slot.IsVisible() && slots.Length > 0)
                selection_index = slots[0].index;
            if (slot == null && slots.Length > 0)
                selection_index = slots[0].index;
        }

        public void PressSlot(int index)
        {
            UISlot slot = GetSlot(index);
            if (slot != null && onPressAccept != null)
                onPressAccept.Invoke(slot);
        }

        private void OnPressAccept(UISlot slot)
        {
            if (onPressAccept != null)
                onPressAccept.Invoke(slot);
        }

        private void OnPressCancel(UISlot slot)
        {
            if (onPressCancel != null)
                onPressCancel.Invoke(slot);
        }

        private void OnPressUse(UISlot slot)
        {
            if (onPressUse != null)
                onPressUse.Invoke(slot);
        }

        private void OnClickSlot(UISlot islot)
        {
            if (onClickSlot != null)
                onClickSlot.Invoke(islot);
        }

        private void OnClickSlotRight(UISlot islot)
        {
            if (onRightClickSlot != null)
                onRightClickSlot.Invoke(islot);
        }

        private void OnClickSlotLong(UISlot islot)
        {
            if (onLongClickSlot != null)
                onLongClickSlot.Invoke(islot);
        }

        private void OnClickSlotDouble(UISlot islot)
        {
            if (onDoubleClickSlot != null)
                onDoubleClickSlot.Invoke(islot);
        }

        private void OnDragStart(UISlot islot)
        {
            if (onDragStart != null)
                onDragStart.Invoke(islot);
        }

        private void OnDragEnd(UISlot islot)
        {
            if (onDragEnd != null)
                onDragEnd.Invoke(islot);
        }

        private void OnDragTo(UISlot islot, UISlot target)
        {
            if (onDragTo != null)
                onDragTo.Invoke(islot, target);
        }

        public int CountActiveSlots()
        {
            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].gameObject.activeSelf)
                    count++;
            }
            return count;
        }

        public UISlot GetSlot(int index)
        {
            foreach (UISlot slot in slots)
            {
                if (slot.index == index)
                    return slot;
            }
            return null;
        }

        public UISlot GetSelectSlot()
        {
            return GetSlot(selection_index);
        }

        public ItemSlot GetDragSlot()
        {
            foreach (ItemSlot slot in slots)
            {
                if (slot.IsDrag())
                    return slot;
            }
            return null;
        }

        public bool IsSelectedInvisible()
        {
            UISlot slot = GetSelectSlot();
            return slot != null && !slot.IsVisible();
        }

        public bool IsSelectedValid()
        {
            UISlot slot = GetSelectSlot();
            return slot != null && slot.IsVisible();
        }

        public static void UnfocusAll()
        {
            foreach (UISlotPanel panel in slot_panels)
                panel.focused = false;
        }

        public static UISlotPanel GetFocusedPanel()
        {
            foreach (UISlotPanel panel in slot_panels)
            {
                if (panel.focused)
                    return panel;
            }
            return null;
        }

        public static List<UISlotPanel> GetAll()
        {
            return slot_panels;
        }
    }

}
