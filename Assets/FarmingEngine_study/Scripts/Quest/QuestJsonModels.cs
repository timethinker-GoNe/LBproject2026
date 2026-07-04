using System.Collections.Generic;

namespace FarmingQuest
{
    [System.Serializable]
    public class QuestDefinitionList
    {
        public List<QuestDefinition> quests = new List<QuestDefinition>();
    }

    [System.Serializable]
    public class QuestDefinition
    {
        public string id;
        public string title;
        public string description;
        public string giverNpcId;
        public string triggerNote; // 런타임 미사용 — 편집기 참고용 메모
        public List<ConditionData> visibleConditions = new List<ConditionData>();
        public List<ConditionData> startConditions = new List<ConditionData>();
        public List<ObjectiveData> objectives = new List<ObjectiveData>();
        public List<RewardData> rewards = new List<RewardData>();
        public List<string> nextQuestIds = new List<string>();
    }

    [System.Serializable]
    public class ConditionData
    {
        public string type;
        public string targetId;
        public int value;
    }

    [System.Serializable]
    public class ObjectiveData
    {
        public string type;
        public string targetId;
        public int requiredAmount = 1;
    }

    [System.Serializable]
    public class RewardData
    {
        public string type;
        public string targetId;
        public int amount;
    }
}
