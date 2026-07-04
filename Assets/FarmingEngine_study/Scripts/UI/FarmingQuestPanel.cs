using UnityEngine;
using UnityEngine.UI;
using FarmingQuest;

#if DIALOGUE_QUESTS
using DialogueQuests;
#endif

namespace FarmingEngine
{
    [RequireComponent(typeof(CanvasGroup))]
    public class FarmingQuestPanel : MonoBehaviour
    {
        private CanvasGroup   _cg;
        private bool          _visible;
        private bool          _subscribed;
        private RectTransform _contentRT;   // ScrollRect 안 Content
        private Font          _font;

        // ── 치수 상수 ─────────────────────────────────────────────────────────
        const float HeaderH  = 64f;
        const float FooterH  = 40f;
        const float SecH     = 36f;  // 섹션 헤더 높이
        const float TitleH   = 38f;  // 퀘스트 제목 행
        const float ObjH     = 30f;  // 목표 1개 높이
        const float CardPadV = 14f;  // 카드 위/아래 패딩
        const float CardPadH = 16f;  // 카드 좌우 패딩
        const float Gap      = 6f;   // 아이템 사이 간격

        // ── 색상 ─────────────────────────────────────────────────────────────
        static readonly Color CPanel      = new Color(0.05f, 0.10f, 0.04f, 0.97f);
        static readonly Color CHeader     = new Color(0.07f, 0.18f, 0.05f, 1.00f);
        static readonly Color CSecActive  = new Color(0.10f, 0.30f, 0.07f, 1.00f);
        static readonly Color CSecDone    = new Color(0.22f, 0.22f, 0.22f, 1.00f);
        static readonly Color CCardActive = new Color(0.09f, 0.20f, 0.07f, 0.92f);
        static readonly Color CCardDone   = new Color(0.16f, 0.16f, 0.16f, 0.88f);
        static readonly Color CDivider    = new Color(0.28f, 0.60f, 0.22f, 0.70f);
        static readonly Color CTitle      = new Color(0.92f, 0.97f, 0.88f, 1.00f);
        static readonly Color CQTitle     = Color.white;
        static readonly Color CObj        = new Color(0.74f, 0.88f, 0.70f, 1.00f);
        static readonly Color CDone       = new Color(0.44f, 0.92f, 0.73f, 1.00f);
        static readonly Color CProg       = new Color(0.98f, 0.76f, 0.16f, 1.00f);
        static readonly Color CHint       = new Color(0.52f, 0.62f, 0.48f, 1.00f);

        private static FarmingQuestPanel _instance;
        public  static FarmingQuestPanel Get() => _instance;

        // ── 초기화 ────────────────────────────────────────────────────────────

        private void Awake()
        {
            _instance = this;

            if (GetComponent<Canvas>() == null)
            {
                var c = gameObject.AddComponent<Canvas>();
                c.renderMode   = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 50;
                var sc = gameObject.AddComponent<CanvasScaler>();
                sc.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                sc.referenceResolution = new Vector2(1920, 1080);
                sc.matchWidthOrHeight  = 0f;
                gameObject.AddComponent<GraphicRaycaster>();
            }

            _cg = GetComponent<CanvasGroup>();
            _cg.alpha = 0f; _cg.blocksRaycasts = false; _cg.interactable = false;
            _font = GetFont();
            BuildStaticUI();
        }

        // ── 정적 구조 ─────────────────────────────────────────────────────────

        private void BuildStaticUI()
        {
            // 패널 배경 (12%~88% 너비, 4%~96% 높이)
            var panel = R(transform, "Panel");
            panel.anchorMin = new Vector2(0.12f, 0.04f);
            panel.anchorMax = new Vector2(0.88f, 0.96f);
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            Bg(panel, CPanel);

            // 헤더
            var hdr = TopStrip(panel, "Header", HeaderH);
            Bg(hdr, CHeader);
            Txt(hdr, "퀘스트 일지", 32, FontStyle.Bold, CTitle, TextAnchor.MiddleCenter,
                0f, 0f, 0f, 0f);

            // 헤더 하단 구분선
            var div = R(panel, "Div");
            div.anchorMin        = new Vector2(0f, 1f);
            div.anchorMax        = new Vector2(1f, 1f);
            div.pivot            = new Vector2(0.5f, 1f);
            div.anchoredPosition = new Vector2(0f, -HeaderH);
            div.sizeDelta        = new Vector2(-24f, 2f);
            Bg(div, CDivider);

            // 스크롤 뷰
            var scroll = R(panel, "Scroll");
            scroll.anchorMin = Vector2.zero;
            scroll.anchorMax = Vector2.one;
            scroll.offsetMin = new Vector2(0f,  FooterH);
            scroll.offsetMax = new Vector2(0f, -HeaderH - 2f);

            var viewport = R(scroll, "Viewport");
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>(); // Mask 대신 RectMask2D (Image 불필요)

            var content = R(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot     = new Vector2(0.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            _contentRT = content;

            var sr = scroll.gameObject.AddComponent<ScrollRect>();
            sr.content          = content;
            sr.viewport         = viewport;
            sr.horizontal       = false; sr.vertical = true;
            sr.scrollSensitivity = 40f;
            sr.movementType     = ScrollRect.MovementType.Clamped;

            // 푸터
            var ftr = BotStrip(panel, "Footer", FooterH);
            Bg(ftr, CHeader);
            Txt(ftr, "[J] 닫기", 18, FontStyle.Normal, CHint, TextAnchor.MiddleCenter,
                0f, 0f, 0f, 0f);
        }

        // ── Update ────────────────────────────────────────────────────────────

        private void Update()
        {
#if DIALOGUE_QUESTS
            if (!_subscribed && NarrativeControls.Get() != null)
            {
                NarrativeControls.Get().onPressJournal += Toggle;
                _subscribed = true;
            }
#endif
            float target = _visible ? 1f : 0f;
            _cg.alpha          = Mathf.MoveTowards(_cg.alpha, target, Time.deltaTime * 8f);
            _cg.blocksRaycasts = _visible;
            _cg.interactable   = _visible;
        }

        private void OnDestroy()
        {
#if DIALOGUE_QUESTS
            if (_subscribed && NarrativeControls.Get() != null)
                NarrativeControls.Get().onPressJournal -= Toggle;
#endif
        }

        // ── 표시 제어 ─────────────────────────────────────────────────────────

        public void Toggle() { if (_visible) Hide(); else Show(); }
        public void Show()   { _visible = true; Refresh(); }
        public void Hide()   { _visible = false; }

        // ── 동적 콘텐츠 ──────────────────────────────────────────────────────

        private void Refresh()
        {
            if (_contentRT == null) return;

            for (int i = _contentRT.childCount - 1; i >= 0; i--)
                DestroyImmediate(_contentRT.GetChild(i).gameObject);

            var qm = QuestManager.Instance;
            if (qm == null) { PlaceMsg("QuestManager 없음", 0f); _contentRT.sizeDelta = new Vector2(0f, 60f); return; }

            var inProg    = qm.GetInProgressQuests();
            var completed = qm.GetCompletedQuests();

            if (inProg.Count == 0 && completed.Count == 0)
            {
                PlaceMsg("진행 중인 퀘스트가 없습니다.", 0f);
                _contentRT.sizeDelta = new Vector2(0f, 60f);
                return;
            }

            float y = Gap;

            if (inProg.Count > 0)
            {
                y = PlaceSectionHeader("▶  진행 중", CSecActive, y);
                foreach (var p in inProg)
                {
                    var def = qm.GetDefinition(p.questId);
                    if (def != null) y = PlaceCard(def, p, false, y);
                }
            }

            if (completed.Count > 0)
            {
                y = PlaceSectionHeader("✓  완료", CSecDone, y);
                foreach (var p in completed)
                {
                    var def = qm.GetDefinition(p.questId);
                    if (def != null) y = PlaceCard(def, p, true, y);
                }
            }

            _contentRT.sizeDelta = new Vector2(0f, y + Gap);
        }

        // ── 아이템 배치 ───────────────────────────────────────────────────────

        private float PlaceSectionHeader(string label, Color color, float y)
        {
            var rt = Item(_contentRT, "Sec", y, SecH);
            Bg(rt, color);
            Txt(rt, label, 22, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft,
                16f, 0f, 0f, 0f);
            return y + SecH + Gap;
        }

        private float PlaceCard(QuestDefinition def, QuestProgress progress, bool done, float y)
        {
            int  objCount = done ? 0 : def.objectives.Count;
            float cardH   = CardPadV + TitleH + objCount * ObjH + CardPadV;
            var card = Item(_contentRT, "Card", y, cardH);
            Bg(card, done ? CCardDone : CCardActive);

            // 카드 내부 좌측 강조선
            var accent = R(card, "Accent");
            accent.anchorMin        = new Vector2(0f, 0f);
            accent.anchorMax        = new Vector2(0f, 1f);
            accent.pivot            = new Vector2(0f, 0.5f);
            accent.anchoredPosition = Vector2.zero;
            accent.sizeDelta        = new Vector2(4f, -8f);
            Bg(accent, done ? CSecDone : CSecActive);

            // 퀘스트 제목
            var titleRT = Item(card, "QTitle", CardPadV, TitleH);
            titleRT.offsetMin = new Vector2(CardPadH + 8f, titleRT.offsetMin.y);
            titleRT.offsetMax = new Vector2(-CardPadH,     titleRT.offsetMax.y);
            Txt(titleRT, def.title ?? def.id, 26, FontStyle.Bold,
                done ? CDone : CQTitle, TextAnchor.MiddleLeft, 0f, 0f, 0f, 0f);

            if (!done)
            {
                for (int i = 0; i < def.objectives.Count; i++)
                {
                    var  obj     = def.objectives[i];
                    int  cur     = (i < progress.objectiveProgresses.Count)
                                     ? progress.objectiveProgresses[i].currentAmount : 0;
                    bool objDone = cur >= obj.requiredAmount;
                    float oy     = CardPadV + TitleH + i * ObjH;

                    var row = Item(card, "Obj", oy, ObjH);
                    row.offsetMin = new Vector2(CardPadH + 8f,  row.offsetMin.y);
                    row.offsetMax = new Vector2(-CardPadH,       row.offsetMax.y);

                    // 체크/원 아이콘
                    var chkRT = R(row, "Chk");
                    chkRT.anchorMin = new Vector2(0f, 0f); chkRT.anchorMax = new Vector2(0f, 1f);
                    chkRT.pivot     = new Vector2(0f, 0.5f);
                    chkRT.anchoredPosition = Vector2.zero;
                    chkRT.sizeDelta = new Vector2(26f, 0f);
                    Txt(chkRT, objDone ? "✓" : "○", 20, FontStyle.Normal,
                        objDone ? CDone : CObj, TextAnchor.MiddleCenter, 0f, 0f, 0f, 0f);

                    // 목표 텍스트
                    var lblRT = R(row, "Lbl");
                    lblRT.anchorMin = new Vector2(0f, 0f); lblRT.anchorMax = new Vector2(1f, 1f);
                    lblRT.offsetMin = new Vector2(30f, 0f); lblRT.offsetMax = new Vector2(-72f, 0f);
                    Txt(lblRT, ObjLabel(obj), 20, FontStyle.Normal,
                        objDone ? new Color(CObj.r, CObj.g, CObj.b, 0.55f) : CObj,
                        TextAnchor.MiddleLeft, 0f, 0f, 0f, 0f);

                    // 진행 수치
                    var progRT = R(row, "Prog");
                    progRT.anchorMin = new Vector2(1f, 0f); progRT.anchorMax = new Vector2(1f, 1f);
                    progRT.pivot     = new Vector2(1f, 0.5f);
                    progRT.anchoredPosition = Vector2.zero;
                    progRT.sizeDelta = new Vector2(68f, 0f);
                    Txt(progRT, $"{cur}/{obj.requiredAmount}", 19, FontStyle.Normal,
                        objDone ? CDone : CProg, TextAnchor.MiddleRight, 0f, 0f, 0f, 0f);
                }
            }

            return y + cardH + Gap;
        }

        private void PlaceMsg(string msg, float y)
        {
            var rt = Item(_contentRT, "Msg", y, 60f);
            Txt(rt, msg, 22, FontStyle.Normal, CHint, TextAnchor.MiddleCenter, 0f, 0f, 0f, 0f);
        }

        // ── UI 빌더 헬퍼 ──────────────────────────────────────────────────────

        // 컨테이너 안에 top-anchored 항목 생성 (y=top에서 아래로 h픽셀)
        static RectTransform Item(RectTransform parent, string name, float y, float h)
        {
            var rt = R(parent, name);
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -y);
            rt.sizeDelta        = new Vector2(0f, h);
            return rt;
        }

        // 상단 고정 스트립
        static RectTransform TopStrip(RectTransform parent, string name, float h)
        {
            var rt = R(parent, name);
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, h);
            return rt;
        }

        // 하단 고정 스트립
        static RectTransform BotStrip(RectTransform parent, string name, float h)
        {
            var rt = R(parent, name);
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, h);
            return rt;
        }

        static RectTransform R(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static void Bg(RectTransform rt, Color color)
        {
            var img = rt.gameObject.GetComponent<Image>() ?? rt.gameObject.AddComponent<Image>();
            img.color = color;
        }

        void Txt(RectTransform parent, string text, int size, FontStyle style,
                 Color color, TextAnchor align,
                 float ol, float ob, float or_, float ot)
        {
            var go = new GameObject("T", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(ol, ob);
            rt.offsetMax = new Vector2(-or_, -ot);
            var t = go.AddComponent<Text>();
            t.font = _font; t.text = text; t.fontSize = size;
            t.fontStyle = style; t.color = color; t.alignment = align;
        }

        Font GetFont()
        {
            foreach (var t in FindObjectsOfType<Text>())
                if (t.font != null) return t.font;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        static string ObjLabel(ObjectiveData obj)
        {
            switch (obj.type)
            {
                case "TillSoil":    return "밭 갈기";
                case "PlantSeed":   return string.IsNullOrEmpty(obj.targetId) ? "씨앗 심기"   : $"{obj.targetId} 심기";
                case "WaterPlant":  return "물주기";
                case "HarvestCrop": return string.IsNullOrEmpty(obj.targetId) ? "작물 수확"   : $"{obj.targetId} 수확";
                case "CollectItem": return string.IsNullOrEmpty(obj.targetId) ? "아이템 수집" : $"{obj.targetId} 수집";
                case "TalkToNpc":   return string.IsNullOrEmpty(obj.targetId) ? "NPC와 대화"  : $"{obj.targetId}와 대화";
                default:            return obj.type;
            }
        }
    }
}
