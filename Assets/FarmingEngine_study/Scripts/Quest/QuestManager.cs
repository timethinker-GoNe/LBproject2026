using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FarmingEngine;

namespace FarmingQuest
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        // UI 등이 구독할 수 있는 이벤트
        public static event System.Action<string> onQuestStarted;      // questId
        public static event System.Action<string> onObjectiveUpdated;  // questId
        public static event System.Action<string> onQuestCompleted;    // questId
        public static event System.Action<string> onRewardReceived;    // questId

        private QuestDatabase _db = new QuestDatabase();
        private Dictionary<string, QuestProgress> _progresses = new Dictionary<string, QuestProgress>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            string path = Path.Combine(Application.streamingAssetsPath, "Dialogue", "quest_data.json");
            _db.Load(path);
            LoadProgress();
            RefreshAvailableQuests();
        }

        private void OnEnable()
        {
            FarmingEvents.onTilledSoil += OnTilledSoil;
            FarmingEvents.onPlantedSeed += OnPlantedSeed;
            FarmingEvents.onWateredPlant += OnWateredPlant;
            FarmingEvents.OnCropHarvested += HandleCropHarvested;
            FarmingEvents.OnItemCollected += HandleItemCollected;
            FarmingEvents.OnNpcTalked += HandleNpcTalked;
        }

        private void OnDisable()
        {
            FarmingEvents.onTilledSoil -= OnTilledSoil;
            FarmingEvents.onPlantedSeed -= OnPlantedSeed;
            FarmingEvents.onWateredPlant -= OnWateredPlant;
            FarmingEvents.OnCropHarvested -= HandleCropHarvested;
            FarmingEvents.OnItemCollected -= HandleItemCollected;
            FarmingEvents.OnNpcTalked -= HandleNpcTalked;
        }

        // ── FarmingEvents bridges ──

        private void OnTilledSoil(PlayerCharacter _) => DispatchObjectiveEvent("TillSoil", "", 1);
        private void OnPlantedSeed(PlayerCharacter _) => DispatchObjectiveEvent("PlantSeed", "", 1);
        private void OnWateredPlant(PlayerCharacter _) => DispatchObjectiveEvent("WaterPlant", "", 1);

        private void HandleCropHarvested(string cropId, int amount) => DispatchObjectiveEvent("HarvestCrop", cropId, amount);
        private void HandleItemCollected(string itemId, int amount) => DispatchObjectiveEvent("CollectItem", itemId, amount);
        private void HandleNpcTalked(string npcId) => DispatchObjectiveEvent("TalkToNpc", npcId, 1);

        // ── Core event dispatch ──

        private void DispatchObjectiveEvent(string objectiveType, string targetId, int amount)
        {
            Debug.Log($"[QuestManager] 이벤트 수신: type={objectiveType}" + (string.IsNullOrEmpty(targetId) ? "" : $" target={targetId}"));
            bool anyChanged = false;
            foreach (var progress in _progresses.Values)
            {
                if (progress.status != QuestStatus.InProgress) continue;
                var def = _db.GetQuest(progress.questId);
                if (def == null) continue;

                bool updated = ObjectiveProcessor.HandleEvent(progress, def, objectiveType, targetId, amount);
                if (updated)
                {
                    // 업데이트된 목표 진행도 로그
                    for (int i = 0; i < def.objectives.Count; i++)
                        Debug.Log($"[QuestManager]   {progress.questId} 목표[{i}] {def.objectives[i].type}: {progress.objectiveProgresses[i].currentAmount}/{def.objectives[i].requiredAmount}");

                    string qid = progress.questId;
                    if (ObjectiveProcessor.AllCompleted(progress))
                    {
                        progress.status = QuestStatus.Completed;
                        Debug.Log($"[QuestManager] ★ 퀘스트 완료: {qid}");
                        onQuestCompleted?.Invoke(qid);
                    }
                    else
                    {
                        onObjectiveUpdated?.Invoke(qid);
                    }
                    anyChanged = true;
                }
            }
            if (anyChanged) SaveProgress();
        }

        // ── Public API ──

        public bool StartQuest(string questId)
        {
            var def = _db.GetQuest(questId);
            if (def == null)
            {
                Debug.LogWarning($"[QuestManager] StartQuest: quest '{questId}' not found.");
                return false;
            }

            if (_progresses.TryGetValue(questId, out var existing)
                && existing.status != QuestStatus.NotStarted
                && existing.status != QuestStatus.Available)
            {
                Debug.LogWarning($"[QuestManager] Quest '{questId}' already started (status: {existing.status}).");
                return false;
            }

            if (!ConditionChecker.AllSatisfied(def.startConditions))
            {
                Debug.LogWarning($"[QuestManager] Start conditions not met for '{questId}'.");
                return false;
            }

            _progresses[questId] = new QuestProgress(questId, def.objectives.Count);
            SaveProgress();
            Debug.Log($"[QuestManager] Quest started: {questId}");
            onQuestStarted?.Invoke(questId);
            return true;
        }

        public bool ReceiveReward(string questId)
        {
            if (!_progresses.TryGetValue(questId, out var progress)
                || progress.status != QuestStatus.Completed)
                return false;

            var def = _db.GetQuest(questId);
            if (def == null) return false;

            RewardProcessor.GiveRewards(def.rewards);
            progress.status = QuestStatus.Rewarded;
            SaveProgress();

            RefreshAvailableQuests();
            Debug.Log($"[QuestManager] Reward received: {questId}");
            onRewardReceived?.Invoke(questId);
            return true;
        }

        public void RefreshAvailableQuests()
        {
            foreach (var def in _db.GetAll())
            {
                if (_progresses.TryGetValue(def.id, out var p)
                    && p.status != QuestStatus.NotStarted)
                    continue;

                if (ConditionChecker.AllSatisfied(def.visibleConditions))
                {
                    if (!_progresses.ContainsKey(def.id))
                        _progresses[def.id] = new QuestProgress { questId = def.id, status = QuestStatus.Available };
                    else
                        _progresses[def.id].status = QuestStatus.Available;
                }
            }
        }

        public QuestStatus GetStatus(string questId)
        {
            if (_progresses.TryGetValue(questId, out var p)) return p.status;
            return QuestStatus.NotStarted;
        }

        public QuestProgress GetProgress(string questId)
        {
            _progresses.TryGetValue(questId, out var p);
            return p;
        }

        public QuestDefinition GetDefinition(string questId) => _db.GetQuest(questId);

        public List<QuestProgress> GetInProgressQuests()
        {
            var result = new List<QuestProgress>();
            foreach (var p in _progresses.Values)
                if (p.status == QuestStatus.InProgress)
                    result.Add(p);
            return result;
        }

        public List<QuestProgress> GetCompletedQuests()
        {
            var result = new List<QuestProgress>();
            foreach (var p in _progresses.Values)
                if (p.status == QuestStatus.Completed || p.status == QuestStatus.Rewarded)
                    result.Add(p);
            return result;
        }

        public List<QuestDefinition> GetAvailableForNpc(string npcId)
        {
            var result = new List<QuestDefinition>();
            foreach (var def in _db.GetQuestsByGiver(npcId))
            {
                var status = GetStatus(def.id);
                if (status == QuestStatus.Available || status == QuestStatus.InProgress || status == QuestStatus.Completed)
                    result.Add(def);
            }
            return result;
        }

        // ── Save / Load ──

        private void LoadProgress()
        {
            var pdata = PlayerData.Get();
            if (pdata?.quest_progress == null) return;
            foreach (var kv in pdata.quest_progress)
                _progresses[kv.Key] = kv.Value;
        }

        private void SaveProgress()
        {
            var pdata = PlayerData.Get();
            if (pdata == null) return;
            pdata.quest_progress = new Dictionary<string, QuestProgress>(_progresses);
            pdata.Save();
        }
    }
}
