using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace FarmingQuest
{
    public class QuestDatabase
    {
        private Dictionary<string, QuestDefinition> _quests = new Dictionary<string, QuestDefinition>();

        private static readonly HashSet<string> ValidConditionTypes = new HashSet<string>
        {
            "QuestCompleted", "HasItem", "HasRecipe", "HasGoldAtLeast",
            "NpcFriendshipAtLeast", "PlayerLevelAtLeast", "DayAtLeast",
            "SeasonIs", "EnterLocation", "ShopLevelAtLeast", "ReputationAtLeast"
        };

        private static readonly HashSet<string> ValidObjectiveTypes = new HashSet<string>
        {
            "TillSoil", "PlantSeed", "WaterPlant", "HarvestCrop",
            "CollectItem", "TalkToNpc", "BakeRecipe", "SellItem",
            "DeliverItem", "ReachLocation", "DefeatEnemy", "UpgradeShop",
            "RaiseReputation", "CompleteQuest"
        };

        private static readonly HashSet<string> ValidRewardTypes = new HashSet<string>
        {
            "Gold", "Item", "Exp", "UnlockRecipe", "NpcFriendship",
            "Reputation", "UnlockArea", "UnlockShopFeature", "StartQuest"
        };

        public void Load(string jsonPath)
        {
            _quests.Clear();
            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning("[QuestDatabase] File not found: " + jsonPath);
                return;
            }

            string json = File.ReadAllText(jsonPath);
            var list = JsonConvert.DeserializeObject<QuestDefinitionList>(json);
            if (list?.quests == null) return;

            foreach (var q in list.quests)
                RegisterQuest(q);

            ValidateCrossReferences();
            Debug.Log($"[QuestDatabase] Loaded {_quests.Count} quests.");
        }

        private void RegisterQuest(QuestDefinition q)
        {
            if (string.IsNullOrEmpty(q.id))
            {
                Debug.LogWarning("[QuestDatabase] Quest with empty id skipped.");
                return;
            }
            if (_quests.ContainsKey(q.id))
            {
                Debug.LogWarning($"[QuestDatabase] Duplicate quest id: {q.id}");
                return;
            }

            ValidateConditionList(q.id, q.visibleConditions);
            ValidateConditionList(q.id, q.startConditions);
            ValidateObjectiveList(q.id, q.objectives);
            ValidateRewardList(q.id, q.rewards);

            _quests[q.id] = q;
        }

        private void ValidateConditionList(string questId, List<ConditionData> conditions)
        {
            if (conditions == null) return;
            foreach (var c in conditions)
                if (!ValidConditionTypes.Contains(c.type))
                    Debug.LogWarning($"[QuestDatabase] {questId}: unknown condition type '{c.type}'");
        }

        private void ValidateObjectiveList(string questId, List<ObjectiveData> objectives)
        {
            if (objectives == null) return;
            foreach (var o in objectives)
            {
                if (!ValidObjectiveTypes.Contains(o.type))
                    Debug.LogWarning($"[QuestDatabase] {questId}: unknown objective type '{o.type}'");
                if (o.requiredAmount <= 0)
                    Debug.LogWarning($"[QuestDatabase] {questId}: objective '{o.type}' has requiredAmount <= 0");
            }
        }

        private void ValidateRewardList(string questId, List<RewardData> rewards)
        {
            if (rewards == null) return;
            foreach (var r in rewards)
                if (!ValidRewardTypes.Contains(r.type))
                    Debug.LogWarning($"[QuestDatabase] {questId}: unknown reward type '{r.type}'");
        }

        private void ValidateCrossReferences()
        {
            foreach (var q in _quests.Values)
            {
                if (q.nextQuestIds == null) continue;
                foreach (var nextId in q.nextQuestIds)
                    if (!_quests.ContainsKey(nextId))
                        Debug.LogWarning($"[QuestDatabase] {q.id}: nextQuestId '{nextId}' not found in database");
            }
        }

        public QuestDefinition GetQuest(string id)
        {
            _quests.TryGetValue(id, out var q);
            return q;
        }

        public IEnumerable<QuestDefinition> GetAll() => _quests.Values;

        public List<QuestDefinition> GetQuestsByGiver(string npcId)
        {
            var result = new List<QuestDefinition>();
            foreach (var q in _quests.Values)
                if (q.giverNpcId == npcId)
                    result.Add(q);
            return result;
        }
    }
}
