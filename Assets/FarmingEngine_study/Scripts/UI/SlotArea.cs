using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 슬롯 컨테이너 오브젝트(BG 등)에 붙여 독립적인 슬롯 레이아웃을 적용한다.
    /// panelKey + areaName 조합으로 UIDesignConfig.json의 areas 배열에서 설정을 읽는다.
    /// </summary>
    public class SlotArea : MonoBehaviour
    {
        public string panelKey;
        public string areaName = "main";

        private bool _applied;

        /// <summary>
        /// layoutPanelRt: 패널 크기 폴백에 사용할 UIPanel의 RectTransform.
        /// 컨테이너(BG) rect가 0일 때 부모 anchor × 부모 크기로 계산한다.
        /// </summary>
        public void ApplyLayout(RectTransform layoutPanelRt = null)
        {
            if (_applied) return;

            var slots = GetComponentsInChildren<UISlot>(true);
            if (slots.Length == 0) return;

            int cols; float gap, slotSize, padL, padR, padT, padB;
            if (!UILayoutConfig.TryGetAreaLayout(panelKey, areaName, out cols, out gap, out slotSize, out padL, out padR, out padT, out padB))
            {
                Debug.LogWarning($"[SlotArea] '{panelKey}/{areaName}' 레이아웃 설정 없음");
                return;
            }

            string justify, align;
            UILayoutConfig.TryGetAreaAlignment(panelKey, areaName, out justify, out align);

            Canvas.ForceUpdateCanvases();

            var container = GetComponent<RectTransform>();
            if (container == null) return;

            float contW = container.rect.width;
            float contH = container.rect.height;

            if (contW < 1f || contH < 1f)
            {
                // 부모 rect × container anchor 비율로 BG 실제 크기 추정
                var parentRt = container.parent as RectTransform;
                float parentW = parentRt != null ? parentRt.rect.width  : 0f;
                float parentH = parentRt != null ? parentRt.rect.height : 0f;

                if (parentW < 1f && layoutPanelRt != null)
                {
                    // 부모(ShopPanel)도 0이면 패널 anchor × 참조해상도로 추정
                    var canvas = GetComponentInParent<Canvas>();
                    var scaler = canvas != null ? canvas.GetComponent<UnityEngine.UI.CanvasScaler>() : null;
                    float refW = scaler != null ? scaler.referenceResolution.x : 1920f;
                    float refH = scaler != null ? scaler.referenceResolution.y : 1080f;
                    parentW = (layoutPanelRt.anchorMax.x - layoutPanelRt.anchorMin.x) * refW;
                    parentH = (layoutPanelRt.anchorMax.y - layoutPanelRt.anchorMin.y) * refH;
                }

                if (parentW > 0f)
                {
                    // container(BG)의 anchor 비율 × 부모 크기 + sizeDelta
                    contW = (container.anchorMax.x - container.anchorMin.x) * parentW + container.sizeDelta.x;
                    contH = (container.anchorMax.y - container.anchorMin.y) * parentH + container.sizeDelta.y;
                }

                if (contW < 1f || contH < 1f)
                {
                    // 아직도 크기 미결정 — _applied 세우지 않고 Show() 때 재시도
                    Debug.LogWarning($"[SlotArea] {panelKey}/{areaName} 컨테이너 크기 미결정, Show() 시 재시도");
                    return;
                }
            }

            // 크기 확인 후 확정
            _applied = true;

            int rows       = Mathf.CeilToInt((float)slots.Length / cols);
            float contentW = Mathf.Max(1f, contW - padL - padR);
            float contentH = Mathf.Max(1f, contH - padT - padB);
            float maxCellW = Mathf.Max(1f, (contentW - gap * (cols - 1)) / cols);
            float maxCellH = Mathf.Max(1f, (contentH - gap * (rows - 1)) / rows);
            float maxFit   = Mathf.Min(maxCellW, maxCellH);
            float cellSize = Mathf.Min(slotSize, maxFit);

            float gridH  = rows * cellSize + (rows - 1) * gap;
            float startY = padT + GetFlexOffset(contentH, gridH, align);

            Debug.Log($"[SlotArea] {panelKey}/{areaName} | cont={contW:F1}x{contH:F1} | cols={cols} rows={rows} slotSize={slotSize} cellSize={cellSize:F1}");

            for (int i = 0; i < slots.Length; i++)
            {
                int col = i % cols;
                int row = i / cols;
                int rowStart = row * cols;
                int rowCount = Mathf.Min(cols, slots.Length - rowStart);
                float rowGridW = rowCount * cellSize + Mathf.Max(0, rowCount - 1) * gap;
                float startX = padL + GetFlexOffset(contentW, rowGridW, justify);

                var slotRt = slots[i].GetComponent<RectTransform>();
                if (slotRt == null) continue;

                float sc = Mathf.Abs(slotRt.localScale.x) > 0.001f ? slotRt.localScale.x : 1f;
                slotRt.anchorMin = slotRt.anchorMax = new Vector2(0f, 1f);
                slotRt.pivot     = new Vector2(0f, 1f);
                slotRt.sizeDelta = new Vector2(cellSize / sc, cellSize / sc);
                slotRt.anchoredPosition = new Vector2(
                    startX + col * (cellSize + gap),
                    -(startY + row * (cellSize + gap))
                );
            }
        }

        private static float GetFlexOffset(float available, float used, string alignment)
        {
            float remain = Mathf.Max(0f, available - used);
            string v = string.IsNullOrEmpty(alignment) ? "center" : alignment.ToLowerInvariant();
            if (v == "start" || v == "flex-start") return 0f;
            if (v == "end"   || v == "flex-end")   return remain;
            return remain * 0.5f;
        }
    }
}
