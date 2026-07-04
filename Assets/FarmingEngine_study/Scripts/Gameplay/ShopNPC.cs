using UnityEngine;

namespace FarmingEngine
{
    [RequireComponent(typeof(Selectable))]
    public class ShopNPC : MonoBehaviour
    {
        public string title;

        public void OpenShop()
        {
            PlayerCharacter character = PlayerCharacter.GetNearest(transform.position);
            if (character != null) OpenShop(character);
        }

        public void OpenShop(PlayerCharacter player)
        {
            string npc_id = GetComponent<DialogueQuests.Actor>()?.data?.actor_id;
            NPCShopData shopData = npc_id != null ? NPCShopManager.Get()?.GetShopData(npc_id) : null;

            if (shopData == null)
            {
                Debug.LogWarning($"ShopNPC: npc_shop.json에 '{npc_id}' 항목 없음. 상점을 열 수 없습니다.");
                return;
            }

            ShopPanel.Get().ShowShop(player, shopData);
        }
    }
}
