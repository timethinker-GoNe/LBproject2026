using UnityEngine;
using FarmingEngine;

namespace DialogueQuests
{
    [CreateAssetMenu(fileName = "EffectOpenShop", menuName = "DialogueQuests/Effects/Open Shop", order = 50)]
    public class EffectOpenShop : EffectData
    {
        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            string npc_id = triggerer?.data?.actor_id;
            NPCShopData shopData = npc_id != null ? NPCShopManager.Get()?.GetShopData(npc_id) : null;

            if (shopData == null)
            {
                Debug.LogWarning($"EffectOpenShop: npc_shop.json에 '{npc_id}' 항목 없음.");
                return;
            }

            PlayerCharacter character = player?.GetComponent<PlayerCharacter>();
            if (character == null)
                character = PlayerCharacter.GetNearest(triggerer.transform.position);

            ShopPanel.Get()?.ShowShop(character, shopData);
        }
    }
}
