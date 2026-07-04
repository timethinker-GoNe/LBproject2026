using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using FarmingEngine;

namespace FarmingEngine.EditorTool
{
    public static class SetupQuestUIEditor
    {
        // 씬에 이미 있는 Text 컴포넌트에서 폰트를 빌려옴 (Unity 버전 무관)
        static Font DefaultFont
        {
            get
            {
                foreach (var t in Object.FindObjectsOfType<Text>())
                    if (t.font != null) return t.font;
                // fallback: Unity 내장
                return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        // ── ② 미니 트래커 ──────────────────────────────────────────────────────
        [MenuItem("Farming Engine/Setup Quest Mini Tracker", priority = 400)]
        static void SetupMiniTracker()
        {
            var questUiGO = GameObject.Find("QuestUI");
            if (questUiGO == null)
            {
                EditorUtility.DisplayDialog("오류", "'QuestUI' GameObject를 씬에서 찾을 수 없습니다.", "확인");
                return;
            }

            var tracker = questUiGO.transform.Find("Tracker")?.gameObject;
            var toast   = questUiGO.transform.Find("Toast")?.gameObject;
            if (tracker == null || toast == null)
            {
                EditorUtility.DisplayDialog("오류", "QuestUI 하위에 'Tracker' 또는 'Toast'가 없습니다.", "확인");
                return;
            }

            // QuestUI 자체를 full-stretch로 설정
            var questRT = EnsureComponent<RectTransform>(questUiGO);
            questRT.anchorMin        = Vector2.zero;
            questRT.anchorMax        = Vector2.one;
            questRT.offsetMin        = Vector2.zero;
            questRT.offsetMax        = Vector2.zero;

            SetupTracker(tracker);
            SetupToast(toast);

            var questUI = questUiGO.GetComponent<QuestUI>() ?? questUiGO.AddComponent<QuestUI>();
            var so = new SerializedObject(questUI);
            so.FindProperty("trackerRoot").objectReferenceValue    = tracker;
            so.FindProperty("questTitleText").objectReferenceValue = tracker.transform.Find("Title")?.GetComponent<Text>();
            so.FindProperty("objectivesText").objectReferenceValue = tracker.transform.Find("Objectives")?.GetComponent<Text>();
            so.FindProperty("toastRoot").objectReferenceValue      = toast;
            so.FindProperty("toastText").objectReferenceValue      = toast.transform.Find("ToastText")?.GetComponent<Text>();
            so.ApplyModifiedProperties();

            tracker.SetActive(false);
            toast.SetActive(false);

            EditorUtility.SetDirty(questUiGO);
            Debug.Log("[SetupQuestUI] 미니 트래커 설정 완료.");
            EditorUtility.DisplayDialog("완료", "미니 트래커 설정 완료!\n퀘스트를 받으면 우상단에 표시됩니다.", "확인");
        }

        static void SetupTracker(GameObject tracker)
        {
            var rt = EnsureComponent<RectTransform>(tracker);
            rt.anchorMin        = new Vector2(1f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-12f, -12f);
            rt.sizeDelta        = new Vector2(230f, 120f);

            var bg = EnsureComponent<Image>(tracker);
            bg.color = new Color(0f, 0f, 0f, 0.65f);
            EnsureComponent<CanvasGroup>(tracker);

            var titleGO = GetOrCreate(tracker, "Title");
            var titleRT = EnsureComponent<RectTransform>(titleGO);
            titleRT.anchorMin        = new Vector2(0f, 1f);
            titleRT.anchorMax        = new Vector2(1f, 1f);
            titleRT.pivot            = new Vector2(0f, 1f);
            titleRT.anchoredPosition = new Vector2(10f, -10f);
            titleRT.sizeDelta        = new Vector2(-20f, 30f);
            var titleTxt = EnsureComponent<Text>(titleGO);
            titleTxt.font      = DefaultFont;
            titleTxt.fontSize  = 13;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color     = Color.white;
            titleTxt.alignment = TextAnchor.UpperLeft;
            titleTxt.text      = "퀘스트 제목";

            var objGO = GetOrCreate(tracker, "Objectives");
            var objRT = EnsureComponent<RectTransform>(objGO);
            objRT.anchorMin        = Vector2.zero;
            objRT.anchorMax        = new Vector2(1f, 1f);
            objRT.offsetMin        = new Vector2(10f, 10f);
            objRT.offsetMax        = new Vector2(-10f, -48f);
            var objTxt = EnsureComponent<Text>(objGO);
            objTxt.font      = DefaultFont;
            objTxt.fontSize  = 11;
            objTxt.color     = new Color(0.85f, 0.85f, 0.85f, 1f);
            objTxt.alignment = TextAnchor.UpperLeft;
            objTxt.text      = "○ 목표";
        }

        static void SetupToast(GameObject toast)
        {
            var rt = EnsureComponent<RectTransform>(toast);
            rt.anchorMin        = new Vector2(0.5f, 1f);
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -80f);
            rt.sizeDelta        = new Vector2(300f, 70f);

            var bg = EnsureComponent<Image>(toast);
            bg.color = new Color(0.1f, 0.3f, 0.1f, 0.85f);
            EnsureComponent<CanvasGroup>(toast);

            var textGO = GetOrCreate(toast, "ToastText");
            var textRT = EnsureComponent<RectTransform>(textGO);
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(12f, 8f);
            textRT.offsetMax = new Vector2(-12f, -8f);
            var txt = EnsureComponent<Text>(textGO);
            txt.font      = DefaultFont;
            txt.fontSize  = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text      = "퀘스트 시작!";
        }

        // ── ③ 저널 패널 ───────────────────────────────────────────────────────
        [MenuItem("Farming Engine/Setup Quest Journal Panel", priority = 401)]
        static void SetupJournalPanel()
        {
            var canvasGO = GameObject.Find("UICanvas");
            if (canvasGO == null)
            {
                EditorUtility.DisplayDialog("오류", "'UICanvas' GameObject를 씬에서 찾을 수 없습니다.", "확인");
                return;
            }

            var existing = canvasGO.transform.Find("FarmingQuestPanel");
            var panelGO  = existing != null ? existing.gameObject
                         : new GameObject("FarmingQuestPanel", typeof(RectTransform));
            panelGO.transform.SetParent(canvasGO.transform, false);

            // 우측 40% 영역
            var rt = EnsureComponent<RectTransform>(panelGO);
            rt.anchorMin = new Vector2(0.6f, 0.1f);
            rt.anchorMax = new Vector2(0.98f, 0.9f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            EnsureComponent<CanvasGroup>(panelGO);
            var bg = EnsureComponent<Image>(panelGO);
            bg.color = new Color(0.05f, 0.08f, 0.05f, 0.92f);

            // 제목
            var titleGO = GetOrCreate(panelGO, "PanelTitle");
            var titleRT = EnsureComponent<RectTransform>(titleGO);
            titleRT.anchorMin        = new Vector2(0f, 1f);
            titleRT.anchorMax        = new Vector2(1f, 1f);
            titleRT.pivot            = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = Vector2.zero;
            titleRT.sizeDelta        = new Vector2(0f, 40f);
            var titleTxt = EnsureComponent<Text>(titleGO);
            titleTxt.font      = DefaultFont;
            titleTxt.text      = "퀘스트 일지";
            titleTxt.fontSize  = 16;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color     = new Color(0.7f, 1f, 0.7f, 1f);
            titleTxt.alignment = TextAnchor.MiddleCenter;

            // 닫기 힌트
            var hintGO = GetOrCreate(panelGO, "CloseHint");
            var hintRT = EnsureComponent<RectTransform>(hintGO);
            hintRT.anchorMin        = new Vector2(0f, 0f);
            hintRT.anchorMax        = new Vector2(1f, 0f);
            hintRT.pivot            = new Vector2(0.5f, 0f);
            hintRT.anchoredPosition = Vector2.zero;
            hintRT.sizeDelta        = new Vector2(0f, 28f);
            var hintTxt = EnsureComponent<Text>(hintGO);
            hintTxt.font      = DefaultFont;
            hintTxt.text      = "[J] 닫기";
            hintTxt.fontSize  = 11;
            hintTxt.color     = new Color(0.6f, 0.6f, 0.6f, 1f);
            hintTxt.alignment = TextAnchor.MiddleCenter;

            // 내용
            var contentGO = GetOrCreate(panelGO, "Content");
            var contentRT = EnsureComponent<RectTransform>(contentGO);
            contentRT.anchorMin = Vector2.zero;
            contentRT.anchorMax = Vector2.one;
            contentRT.offsetMin = new Vector2(16f, 32f);
            contentRT.offsetMax = new Vector2(-16f, -44f);
            var contentTxt = EnsureComponent<Text>(contentGO);
            contentTxt.font            = DefaultFont;
            contentTxt.fontSize        = 12;
            contentTxt.color           = Color.white;
            contentTxt.alignment       = TextAnchor.UpperLeft;
            contentTxt.supportRichText = true;
            contentTxt.text            = "";

            // FarmingQuestPanel 부착 (UI는 Awake에서 BuildUI가 직접 생성)
            EnsureComponent<FarmingQuestPanel>(panelGO);

            panelGO.SetActive(true);

            // DQ QuestPanel 비활성화
            var dqPanel = GameObject.Find("QuestPanel");
            if (dqPanel != null)
            {
                dqPanel.SetActive(false);
                Debug.Log("[SetupQuestUI] DQ QuestPanel 비활성화.");
            }

            EditorUtility.SetDirty(panelGO);
            Debug.Log("[SetupQuestUI] 저널 패널 설정 완료.");
            EditorUtility.DisplayDialog("완료", "저널 패널 생성 완료!\nJ 키로 열고 닫습니다.", "확인");
        }

        // ── 유틸 ──────────────────────────────────────────────────────────────
        static T EnsureComponent<T>(GameObject go) where T : Component
            => go.GetComponent<T>() ?? go.AddComponent<T>();

        static GameObject GetOrCreate(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go;
        }
    }
}
