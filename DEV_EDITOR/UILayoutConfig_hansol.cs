using UnityEngine;
using System.IO;

namespace FarmingEngine
{
    /// <summary>
    /// StreamingAssets/UIDesignConfig.json에서 패널별 앵커 설정을 읽는 정적 헬퍼.
    /// 각 패널의 Awake에서 호출해 RectTransform 앵커를 런타임 적용 가능.
    /// </summary>
    public static class UILayoutConfig
    {
        private static Newtonsoft.Json.Linq.JObject _root;

        private static Newtonsoft.Json.Linq.JObject GetRoot()
        {
            if (_root != null) return _root;
            string path = Path.Combine(Application.streamingAssetsPath, "UIDesignConfig.json");
            if (!File.Exists(path)) return null;
            try { _root = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path)); }
            catch { _root = null; }
            return _root;
        }

        /// <summary>
        /// 패널 키(예: "quickbar", "equip", "bag")에 해당하는 anchorMin/Max를 반환한다.
        /// JSON에 해당 키가 없으면 false를 반환하며 out 값은 0/1 기본값.
        /// </summary>
        public static bool TryGetAnchors(string panelKey,
            out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = minY = 0f; maxX = maxY = 1f;
            var root = GetRoot();
            if (root == null) return false;
            var p = root[panelKey];
            if (p == null) return false;
            minX = p.Value<float?>("ancMinX") ?? minX;
            minY = p.Value<float?>("ancMinY") ?? minY;
            maxX = p.Value<float?>("ancMaxX") ?? maxX;
            maxY = p.Value<float?>("ancMaxY") ?? maxY;
            return true;
        }

        /// <summary>
        /// 패널 키에 해당하는 슬롯 레이아웃 설정(cols, gap, pad)을 반환한다.
        /// JSON에 해당 키가 없으면 false를 반환하며 out 값은 기본값 유지.
        /// </summary>
        public static bool TryGetSlotLayout(string panelKey,
            out int cols, out float gap, out float pad,
            out float padLeft, out float padRight, out float padTop, out float padBottom)
        {
            cols = 1; gap = 4f; pad = 4f;
            padLeft = padRight = padTop = padBottom = pad;
            var root = GetRoot();
            if (root == null) return false;
            var p = root[panelKey];
            if (p == null) return false;
            cols = p.Value<int?>("slotCols") ?? cols;
            gap  = p.Value<float?>("slotGap") ?? gap;
            pad  = p.Value<float?>("slotPad") ?? pad;
            padLeft = p.Value<float?>("slotPadLeft") ?? pad;
            padRight = p.Value<float?>("slotPadRight") ?? pad;
            padTop = p.Value<float?>("slotPadTop") ?? pad;
            padBottom = p.Value<float?>("slotPadBottom") ?? pad;
            return true;
        }

        public static bool IsSlotFlexEnabled(string panelKey, bool defaultValue = true)
        {
            var root = GetRoot();
            if (root == null) return defaultValue;
            var p = root[panelKey];
            if (p == null) return defaultValue;
            return p.Value<bool?>("slotFlex") ?? defaultValue;
        }

        public static bool TryGetSlotAlignment(string panelKey, out string justify, out string align)
        {
            justify = "center";
            align = "center";
            var root = GetRoot();
            if (root == null) return false;
            var p = root[panelKey];
            if (p == null) return false;
            justify = p.Value<string>("slotJustify") ?? justify;
            align = p.Value<string>("slotAlign") ?? align;
            return true;
        }

        public static bool TryGetPanelSprites(string panelKey,
            out string bgSprite, out string slotSprite)
        {
            bgSprite = "";
            slotSprite = "";
            var root = GetRoot();
            if (root == null) return false;
            var p = root[panelKey];
            if (p == null) return false;
            bgSprite = p.Value<string>("bgSprite") ?? "";
            slotSprite = p.Value<string>("slotSprite") ?? "";
            return !string.IsNullOrEmpty(bgSprite) || !string.IsNullOrEmpty(slotSprite);
        }

        /// <summary>캐시를 비운다. 게임 재시작 없이 JSON을 다시 읽을 때 호출.</summary>
        public static void InvalidateCache() { _root = null; }
    }
}
