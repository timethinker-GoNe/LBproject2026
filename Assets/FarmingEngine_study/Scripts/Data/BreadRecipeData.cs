using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    [CreateAssetMenu(fileName = "BreadRecipe", menuName = "FarmingEngine/Bakery/BreadRecipe", order = 10)]
    public class BreadRecipeData : ScriptableObject
    {
        [Header("Display")]
        public string id;
        public string title;
        public Sprite icon;
        [TextArea(2, 4)]
        public string desc;

        [Header("Recipe")]
        public ItemData[] required_items;  // 각 항목 = 해당 아이템 1개 필요 (중복 허용)
        public ItemData result_item;
        public int result_quantity = 1;
        public float bake_duration = 30f;  // 실제 초 단위

        [Header("Unlock")]
        public bool unlocked_by_default = true;

        private static List<BreadRecipeData> recipe_list = new List<BreadRecipeData>();
        private static Dictionary<string, BreadRecipeData> recipe_dict = new Dictionary<string, BreadRecipeData>();

        public static void Load(string folder = "Bakery/Recipes")
        {
            recipe_list.Clear();
            recipe_dict.Clear();
            recipe_list.AddRange(Resources.LoadAll<BreadRecipeData>(folder));
            foreach (var r in recipe_list)
            {
                if (!string.IsNullOrEmpty(r.id))
                    recipe_dict[r.id] = r;
            }
        }

        public static BreadRecipeData Get(string id)
        {
            recipe_dict.TryGetValue(id, out BreadRecipeData result);
            return result;
        }

        public static List<BreadRecipeData> GetAll()
        {
            return recipe_list;
        }

        // 재료별 필요 수량 집계
        public Dictionary<ItemData, int> GetRequiredCounts()
        {
            var counts = new Dictionary<ItemData, int>();
            foreach (var item in required_items)
            {
                if (item == null) continue;
                counts[item] = counts.ContainsKey(item) ? counts[item] + 1 : 1;
            }
            return counts;
        }
    }
}
