using UnityEngine;
using FarmingEngine;

namespace FarmingQuest
{
    public static class ConditionChecker
    {
        public static bool IsSatisfied(ConditionData condition)
        {
            switch (condition.type)
            {
                case "QuestCompleted":
                    return QuestManager.Instance != null
                        && QuestManager.Instance.GetStatus(condition.targetId) == QuestStatus.Rewarded;

                case "HasItem":
                {
                    var character = PlayerCharacter.Get(0);
                    if (character == null) return false;
                    var item = ItemData.Get(condition.targetId);
                    if (item == null) return false;
                    return character.Inventory.CountItem(item) >= condition.value;
                }

                case "PlayerLevelAtLeast":
                {
                    var pdata = PlayerCharacterData.Get(0);
                    if (pdata == null) return false;
                    // targetId = level category id (e.g. "farming"), value = required level
                    return pdata.GetLevel(condition.targetId) >= condition.value;
                }

                case "HasGoldAtLeast":
                {
                    var pdata = PlayerCharacterData.Get(0);
                    return pdata != null && pdata.gold >= condition.value;
                }

                case "DayAtLeast":
                {
                    var saveData = PlayerData.Get();
                    return saveData != null && saveData.day >= condition.value;
                }

                case "HasRecipe":
                {
                    var pdata = PlayerCharacterData.Get(0);
                    return pdata != null && pdata.IsIDUnlocked(condition.targetId);
                }

                case "NpcFriendshipAtLeast":
                case "SeasonIs":
                case "EnterLocation":
                case "ShopLevelAtLeast":
                case "ReputationAtLeast":
                    Debug.LogWarning($"[ConditionChecker] '{condition.type}' not yet implemented.");
                    return false;

                default:
                    Debug.LogWarning($"[ConditionChecker] Unknown condition type: {condition.type}");
                    return false;
            }
        }

        public static bool AllSatisfied(System.Collections.Generic.List<ConditionData> conditions)
        {
            if (conditions == null || conditions.Count == 0) return true;
            foreach (var c in conditions)
                if (!IsSatisfied(c)) return false;
            return true;
        }
    }
}
