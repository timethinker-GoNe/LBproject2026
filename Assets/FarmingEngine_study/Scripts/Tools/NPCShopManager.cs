using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace FarmingEngine
{
    [System.Serializable]
    public class ShopItemEntry
    {
        public string id;
        public int? price;      // null → ItemData 기본값 사용
        public bool? sell_able; // null → ItemData 기본값 사용
    }

    [System.Serializable]
    public class NPCShopData
    {
        public string shop_title;
        public List<ShopItemEntry> for_sale = new List<ShopItemEntry>();
        public List<ShopItemEntry> accepts   = new List<ShopItemEntry>();

        public int GetBuyPrice(ItemData item)
        {
            ShopItemEntry e = for_sale?.Find(x => x.id == item.id);
            return (e != null && e.price.HasValue) ? e.price.Value : item.buy_cost;
        }

        public int GetSellPrice(ItemData item)
        {
            ShopItemEntry e = accepts?.Find(x => x.id == item.id);
            return (e != null && e.price.HasValue) ? e.price.Value : item.sell_cost;
        }

        public bool CanSell(ItemData item)
        {
            ShopItemEntry e = accepts?.Find(x => x.id == item.id);
            if (e != null && e.sell_able.HasValue) return e.sell_able.Value;
            return item.sell_able;
        }
    }

    public class NPCShopManager : MonoBehaviour
    {
        private Dictionary<string, NPCShopData> _shops = new Dictionary<string, NPCShopData>();

        private static NPCShopManager _instance;
        public static NPCShopManager Get() => _instance;

        private void Awake()
        {
            _instance = this;
            Load();
        }

        private void Load()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "npc_shop.json");
            if (!File.Exists(path)) { Debug.LogWarning("npc_shop.json not found: " + path); return; }

            var root = JsonConvert.DeserializeObject<NpcShopRoot>(File.ReadAllText(path));
            if (root?.npcs != null)
                _shops = root.npcs;
        }

        public NPCShopData GetShopData(string npc_id)
        {
            if (npc_id != null && _shops.TryGetValue(npc_id, out NPCShopData data))
                return data;
            return null;
        }

        private class NpcShopRoot
        {
            public Dictionary<string, NPCShopData> npcs;
        }
    }
}
