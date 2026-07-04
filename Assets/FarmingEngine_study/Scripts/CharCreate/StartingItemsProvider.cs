using System.Collections;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// Scene_Farm_01 시작 시 CharCreateData.background_id에 따라 시작 아이템/골드를 지급.
    /// TheGame의 onNewGame 또는 첫 onNewDay 이벤트 후 실행.
    /// Managers 오브젝트(또는 씬 내 빈 오브젝트)에 배치.
    /// </summary>
    public class StartingItemsProvider : MonoBehaviour
    {
        [Header("농부 출신 시작 씨앗 (5종)")]
        public ItemData[] farmer_seeds;
        public ItemData farmer_watering_can;

        [Header("상인 출신 시작 씨앗 (2종)")]
        public ItemData[] merchant_seeds;
        public int merchant_gold_bonus = 200;

        [Header("요리사 출신 시작 씨앗 (2종)")]
        public ItemData[] chef_seeds;
        public ItemData chef_cooking_tool;

        private bool given = false;

        private void Start()
        {
            // 이미 저장 데이터가 있는 슬롯에서 불러온 경우라면 지급 생략
            if (PlayerData.Get() != null && PlayerData.Get().day > 1)
            {
                given = true;
                return;
            }

            StartCoroutine(GiveItemsNextFrame());
        }

        private IEnumerator GiveItemsNextFrame()
        {
            // PlayerCharacter가 씬에 완전히 등록될 때까지 한 프레임 대기
            yield return null;

            if (given) yield break;
            given = true;

            PlayerCharacter player = PlayerCharacter.Get();
            if (player == null) yield break;

            string bg = CharCreateData.background_id;

            switch (bg)
            {
                case "merchant":
                    GiveSeeds(player, merchant_seeds);
                    player.SaveData.gold += merchant_gold_bonus;
                    break;

                case "chef":
                    GiveSeeds(player, chef_seeds);
                    GiveItem(player, chef_cooking_tool);
                    break;

                case "farmer":
                default:
                    GiveSeeds(player, farmer_seeds);
                    GiveItem(player, farmer_watering_can);
                    break;
            }
        }

        private void GiveSeeds(PlayerCharacter player, ItemData[] seeds)
        {
            if (seeds == null) return;
            foreach (var seed in seeds)
                GiveItem(player, seed);
        }

        private void GiveItem(PlayerCharacter player, ItemData item, int quantity = 1)
        {
            if (item == null) return;
            player.Inventory.GainItem(item, quantity);
        }
    }
}
