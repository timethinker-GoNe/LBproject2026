using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using FarmingQuest;

namespace FarmingEngine
{
    /// <summary>
    /// 우상단 퀘스트 진행 트래커 + 퀘스트 시작/완료 토스트.
    /// 씬에 빈 GameObject 하나 만들고 이 스크립트만 붙이면 됨.
    /// UI는 Awake에서 전부 자동 생성.
    /// </summary>
    public class QuestUI : MonoBehaviour
    {
        [SerializeField] private float toastDuration = 3f;

        public static QuestUI Get() => _instance;
        private static QuestUI _instance;

        private GameObject _trackerRoot;
        private Text       _titleText;
        private Text       _objectivesText;
        private GameObject _toastRoot;
        private Text       _toastText;
        private CanvasGroup _trackerCG;
        private Coroutine  _toastCoroutine;

        // ── 초기화 ────────────────────────────────────────────────────────────

        private void Awake()
        {
            _instance = this;

            if (GetComponent<Canvas>() == null)
            {
                var c = gameObject.AddComponent<Canvas>();
                c.renderMode   = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 49;

                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight  = 0f;

                gameObject.AddComponent<GraphicRaycaster>();
            }

            BuildUI();
        }

        private void BuildUI()
        {
            Font font = InventoryUITheme.BodyFont;

            // 장비창 바로 위에 쌓이는 퀘스트 트래커
            _trackerRoot = GetOrCreate(transform, "Tracker");
            var trackerRT = EnsureRT(_trackerRoot);
            trackerRT.anchorMin        = new Vector2(1f, 0f);
            trackerRT.anchorMax        = new Vector2(1f, 0f);
            trackerRT.pivot            = new Vector2(1f, 0f);
            trackerRT.anchoredPosition = new Vector2(-24f, 178f);
            trackerRT.sizeDelta        = new Vector2(270f, 126f);
            var trackerBG = _trackerRoot.GetComponent<Image>() ?? _trackerRoot.AddComponent<Image>();
            trackerBG.sprite = InventoryUITheme.RoundedRectSprite;
            trackerBG.type = Image.Type.Sliced;
            trackerBG.color = new Color(0.33f, 0.25f, 0.20f, 0.90f);
            _trackerCG = _trackerRoot.GetComponent<CanvasGroup>() ?? _trackerRoot.AddComponent<CanvasGroup>();

            var titleGO = GetOrCreate(_trackerRoot.transform, "Title");
            var titleRT = EnsureRT(titleGO);
            titleRT.anchorMin        = new Vector2(0f, 1f);
            titleRT.anchorMax        = new Vector2(1f, 1f);
            titleRT.pivot            = new Vector2(0f, 1f);
            titleRT.anchoredPosition = new Vector2(10f, -8f);
            titleRT.sizeDelta        = new Vector2(-20f, 28f);
            _titleText = titleGO.GetComponent<Text>() ?? titleGO.AddComponent<Text>();
            _titleText.font      = InventoryUITheme.TitleFont;
            _titleText.fontSize  = 20;
            _titleText.fontStyle = FontStyle.Normal;
            _titleText.color     = InventoryUITheme.SlotEmpty;
            _titleText.alignment = TextAnchor.UpperLeft;

            var objGO = GetOrCreate(_trackerRoot.transform, "Objectives");
            var objRT = EnsureRT(objGO);
            objRT.anchorMin = Vector2.zero;
            objRT.anchorMax = new Vector2(1f, 1f);
            objRT.offsetMin = new Vector2(10f, 8f);
            objRT.offsetMax = new Vector2(-10f, -42f);
            _objectivesText = objGO.GetComponent<Text>() ?? objGO.AddComponent<Text>();
            _objectivesText.font      = font;
            _objectivesText.fontSize  = 16;
            _objectivesText.color     = new Color(0.93f, 0.86f, 0.74f, 1f);
            _objectivesText.alignment = TextAnchor.UpperLeft;

            _trackerRoot.SetActive(false);

            // 시작/완료 토스트는 트래커 위에 표시
            _toastRoot = GetOrCreate(transform, "Toast");
            var toastRT = EnsureRT(_toastRoot);
            toastRT.anchorMin        = new Vector2(1f, 0f);
            toastRT.anchorMax        = new Vector2(1f, 0f);
            toastRT.pivot            = new Vector2(1f, 0f);
            toastRT.anchoredPosition = new Vector2(-24f, 316f);
            toastRT.sizeDelta        = new Vector2(270f, 72f);
            var toastBG = _toastRoot.GetComponent<Image>() ?? _toastRoot.AddComponent<Image>();
            toastBG.sprite = InventoryUITheme.RoundedRectSprite;
            toastBG.type = Image.Type.Sliced;
            toastBG.color = new Color(0.72f, 0.55f, 0.34f, 0.96f);

            var toastTextGO = GetOrCreate(_toastRoot.transform, "ToastText");
            var toastTextRT = EnsureRT(toastTextGO);
            toastTextRT.anchorMin = Vector2.zero;
            toastTextRT.anchorMax = Vector2.one;
            toastTextRT.offsetMin = new Vector2(12f, 8f);
            toastTextRT.offsetMax = new Vector2(-12f, -8f);
            _toastText = toastTextGO.GetComponent<Text>() ?? toastTextGO.AddComponent<Text>();
            _toastText.font      = InventoryUITheme.TitleFont;
            _toastText.fontSize  = 17;
            _toastText.fontStyle = FontStyle.Normal;
            _toastText.color     = InventoryUITheme.SlotEmpty;
            _toastText.alignment = TextAnchor.MiddleCenter;

            _toastRoot.SetActive(false);
        }

        // ── 이벤트 구독 ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            QuestManager.onQuestStarted     += OnQuestStarted;
            QuestManager.onObjectiveUpdated += OnObjectiveUpdated;
            QuestManager.onQuestCompleted   += OnQuestCompleted;
            QuestManager.onRewardReceived   += OnRewardReceived;

            // 이미 진행 중인 퀘스트가 있을 경우 트래커 즉시 표시
            RefreshTracker();
        }

        private void OnDisable()
        {
            QuestManager.onQuestStarted     -= OnQuestStarted;
            QuestManager.onObjectiveUpdated -= OnObjectiveUpdated;
            QuestManager.onQuestCompleted   -= OnQuestCompleted;
            QuestManager.onRewardReceived   -= OnRewardReceived;
        }

        // ── 이벤트 핸들러 ─────────────────────────────────────────────────────

        private void OnQuestStarted(string questId)
        {
            RefreshTracker();
            ShowToast($"퀘스트 시작!\n{GetQuestTitle(questId)}");
        }

        private void OnObjectiveUpdated(string questId) => RefreshTracker();

        private void OnQuestCompleted(string questId)
        {
            RefreshTracker();
            ShowToast($"퀘스트 완료!\n{GetQuestTitle(questId)}");
        }

        private void OnRewardReceived(string questId) => RefreshTracker();

        // ── 트래커 갱신 ───────────────────────────────────────────────────────

        private void RefreshTracker()
        {
            var qm = QuestManager.Instance;
            if (qm == null) return;

            var active = qm.GetInProgressQuests();
            if (active.Count == 0)
            {
                if (_trackerRoot != null) _trackerRoot.SetActive(false);
                return;
            }

            if (_trackerRoot != null) _trackerRoot.SetActive(true);

            var progress = active[0];
            var def      = qm.GetDefinition(progress.questId);
            if (def == null) return;

            if (_titleText != null)
                _titleText.text = def.title ?? progress.questId;
            if (_objectivesText != null)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < def.objectives.Count; i++)
                {
                    var obj     = def.objectives[i];
                    int current = (i < progress.objectiveProgresses.Count)
                                    ? progress.objectiveProgresses[i].currentAmount : 0;
                    string check = (current >= obj.requiredAmount) ? "✓" : "○";
                    sb.AppendLine($"{check} {GetObjectiveLabel(obj)}  ({current}/{obj.requiredAmount})");
                }
                _objectivesText.text = sb.ToString().TrimEnd();
            }
        }

        // ── 토스트 ────────────────────────────────────────────────────────────

        private void ShowToast(string message)
        {
            if (_toastRoot == null) return;
            if (_toastText != null) _toastText.text = message;
            if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
            _toastCoroutine = StartCoroutine(ToastRoutine());
        }

        private IEnumerator ToastRoutine()
        {
            _toastRoot.SetActive(true);
            yield return new WaitForSeconds(toastDuration);
            _toastRoot.SetActive(false);
            _toastCoroutine = null;
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────────────

        private string GetQuestTitle(string questId)
        {
            var def = QuestManager.Instance?.GetDefinition(questId);
            return !string.IsNullOrEmpty(def?.title) ? def.title : questId;
        }

        private static string GetObjectiveLabel(ObjectiveData obj)
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

        private Font GetFont()
        {
            return InventoryUITheme.BodyFont;
        }

        private static GameObject GetOrCreate(Transform parent, string childName)
        {
            var existing = parent.Find(childName);
            if (existing != null)
            {
                if (existing.GetComponent<RectTransform>() != null)
                    return existing.gameObject;
                Destroy(existing.gameObject);
            }
            var go = new GameObject(childName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform EnsureRT(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            return rt != null ? rt : go.AddComponent<RectTransform>();
        }
    }
}
