using System.Collections.Generic;
using UnityEngine;
using FarmingEngine;

namespace FarmingQuest
{
    public static class RewardProcessor
    {
        public static void GiveRewards(List<RewardData> rewards)
        {
            if (rewards == null) return;
            foreach (var r in rewards)
                GiveReward(r);
        }

        private static void GiveReward(RewardData reward)
        {
            switch (reward.type)
            {
                case "Gold":
                {
                    var pdata = PlayerData.Get()?.GetPlayerCharacter(0);
                    if (pdata != null)
                        pdata.gold += reward.amount;
                    break;
                }

                case "Item":
                {
                    var character = PlayerCharacter.Get(0);
                    var item = ItemData.Get(reward.targetId);
                    if (character != null && item != null)
                        character.Inventory.GainItem(item, reward.amount);
                    else
                        Debug.LogWarning($"[RewardProcessor] Item reward failed: character={character != null}, item={item != null}, targetId={reward.targetId}");
                    break;
                }

                case "StartQuest":
                    if (QuestManager.Instance != null)
                        QuestManager.Instance.StartQuest(reward.targetId);
                    break;

                case "Exp":
                case "UnlockRecipe":
                case "NpcFriendship":
                case "Reputation":
                case "UnlockArea":
                case "UnlockShopFeature":
                    Debug.LogWarning($"[RewardProcessor] Reward type '{reward.type}' not yet implemented.");
                    break;

                default:
                    Debug.LogWarning($"[RewardProcessor] Unknown reward type: {reward.type}");
                    break;
            }
        }
    }
}
