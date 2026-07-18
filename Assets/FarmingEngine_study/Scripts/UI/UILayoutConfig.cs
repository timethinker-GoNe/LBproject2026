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
        /// 패널 키에 해당하는 슬롯 레이아웃 설정을 반환한다.
        /// areas 배열이 있으면 areas[0]에서, 없으면 flat 키에서 읽는다.
        /// slotPadLeft/Right/Top/Bottom 없으면 slotPad 값으로 폴백.
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

            // areas 배열이 있으면 첫 번째 area에서 읽기 (기존 단일-area 패널 폴백)
            var areas = p["areas"] as Newtonsoft.Json.Linq.JArray;
            var src   = (areas != null && areas.Count > 0) ? areas[0] : p;

            cols = src.Value<int?>("slotCols") ?? cols;
            gap  = src.Value<float?>("slotGap") ?? gap;
            pad  = src.Value<float?>("slotPad") ?? pad;
            padLeft   = src.Value<float?>("slotPadLeft")   ?? pad;
            padRight  = src.Value<float?>("slotPadRight")  ?? pad;
            padTop    = src.Value<float?>("slotPadTop")    ?? pad;
            padBottom = src.Value<float?>("slotPadBottom") ?? pad;
            return true;
        }

        public static bool TryGetSlotCount(string panelKey, out int slotCount)
        {
            slotCount = 0;
            var root = GetRoot();
            if (root == null) return false;
            var p = root[panelKey];
            if (p == null) return false;

            var areas = p["areas"] as Newtonsoft.Json.Linq.JArray;
            var src = (areas != null && areas.Count > 0) ? areas[0] : p;
            slotCount = src.Value<int?>("slotCount") ?? 0;
            return slotCount > 0;
        }

        /// <summary>
        /// areas 배열에서 특정 areaName을 찾아 슬롯 레이아웃을 반환한다.
        /// slotSize: 직접 지정값 (컨테이너 최대 크기로 캡핑은 SlotArea에서 처리).
        /// </summary>
        public static bool TryGetAreaLayout(string panelKey, string areaName,
            out int cols, out float gap, out float slotSize,
            out float padLeft, out float padRight, out float padTop, out float padBottom)
        {
            cols = 1; gap = 4f; slotSize = 60f;
            padLeft = padRight = padTop = padBottom = 4f;
            var root = GetRoot();
            if (root == null) return false;
            var p = root[panelKey];
            if (p == null) return false;

            Newtonsoft.Json.Linq.JToken src = null;
            var areas = p["areas"] as Newtonsoft.Json.Linq.JArray;
            if (areas != null)
            {
                foreach (var area in areas)
                    if (area.Value<string>("name") == areaName) { src = area; break; }
            }
            // flat 키 폴백 (areas 없거나 areaName=="main")
            if (src == null && (areas == null || areaName == "main"))
                src = p;
            if (src == null) return false;

            cols      = src.Value<int?>("slotCols")  ?? cols;
            gap       = src.Value<float?>("slotGap") ?? gap;
            slotSize  = src.Value<float?>("slotSize") ?? slotSize;
            float pad = src.Value<float?>("slotPad")  ?? 4f;
            padLeft   = src.Value<float?>("slotPadLeft")   ?? pad;
            padRight  = src.Value<float?>("slotPadRight")  ?? pad;
            padTop    = src.Value<float?>("slotPadTop")    ?? pad;
            padBottom = src.Value<float?>("slotPadBottom") ?? pad;
            return true;
        }

        public static bool TryGetAreaAlignment(string panelKey, string areaName,
            out string justify, out string align)
        {
            justify = "center"; align = "center";
            var root = GetRoot();
            if (root == null) return false;
            var p = root[panelKey];
            if (p == null) return false;

            Newtonsoft.Json.Linq.JToken src = null;
            var areas = p["areas"] as Newtonsoft.Json.Linq.JArray;
            if (areas != null)
                foreach (var area in areas)
                    if (area.Value<string>("name") == areaName) { src = area; break; }
            if (src == null) src = p;

            justify = src.Value<string>("slotJustify") ?? justify;
            align   = src.Value<string>("slotAlign")   ?? align;
            return true;
        }

        public static bool IsSlotFlexEnabled(string panelKey, bool defaultValue = true)
        {
            var root = GetRoot();
            if (root == null) return defaultValue;
            var p = root[panelKey];
            if (p == null) return defaultValue;
            // areas 배열이 있으면 항상 flex 활성
            if (p["areas"] is Newtonsoft.Json.Linq.JArray) return true;
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

            // areas 배열이 있으면 areas[0]에서 읽기
            var areas = p["areas"] as Newtonsoft.Json.Linq.JArray;
            var src   = (areas != null && areas.Count > 0) ? areas[0] : p;
            justify = src.Value<string>("slotJustify") ?? justify;
            align   = src.Value<string>("slotAlign")   ?? align;
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
            bgSprite   = p.Value<string>("bgSprite")   ?? "";
            slotSprite = p.Value<string>("slotSprite") ?? "";
            return !string.IsNullOrEmpty(bgSprite) || !string.IsNullOrEmpty(slotSprite);
        }

        /// <summary>캐시를 비운다. 게임 재시작 없이 JSON을 다시 읽을 때 호출.</summary>
        public static void InvalidateCache() { _root = null; }
    }
}
