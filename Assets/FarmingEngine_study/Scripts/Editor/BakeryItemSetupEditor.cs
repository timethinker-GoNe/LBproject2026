using UnityEngine;
using UnityEditor;
using System.IO;

namespace FarmingEngine
{
    /// <summary>
    /// FarmingEngine > Bakery > Create Bread Items 메뉴로 실행.
    /// 베이커리에서 사용할 재료/빵 ItemData 에셋을 자동 생성한다.
    /// </summary>
    public class BakeryItemSetupEditor
    {
        [MenuItem("Farming Engine/Bakery/Create Bread Item Assets")]
        public static void CreateBreadItems()
        {
            string dir = "Assets/FarmingEngine_study/Resources/Bakery/Items";
            Directory.CreateDirectory(dir);

            // 재료 아이템
            CreateItemAsset(dir, "Flour", "flour", "밀가루", "제빵의 기본 재료", ItemType.Basic, 50);
            CreateItemAsset(dir, "Butter", "butter", "버터", "빵을 촉촉하게 만드는 재료", ItemType.Basic, 30);

            // 빵 결과물 아이템
            CreateItemAsset(dir, "BasicBread", "basic_bread", "기본 식빵",
                "직접 만든 식빵. 배가 든든해진다.", ItemType.Consumable, 20, eatHunger: 25, eatHp: 5);
            CreateItemAsset(dir, "Croissant", "croissant", "크루아상",
                "버터 향기가 가득한 크루아상.", ItemType.Consumable, 20, eatHunger: 20, eatHp: 8);
            CreateItemAsset(dir, "TomatoBread", "tomato_bread", "토마토 빵",
                "토마토의 비타민이 가득. 피로 회복에 좋다.", ItemType.Consumable, 20, eatHunger: 22, eatHp: 10);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"베이커리 아이템 에셋 생성 완료: {dir}");
            Debug.Log("각 아이템의 icon 필드를 Inspector에서 직접 연결해주세요.");
        }

        private static void CreateItemAsset(string dir, string fileName, string id,
            string title, string desc, ItemType itemType, int inventoryMax,
            int eatHunger = 0, int eatHp = 0)
        {
            string path = $"{dir}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null)
            {
                Debug.Log($"이미 존재: {path} (건너뜀)");
                return;
            }

            var item = ScriptableObject.CreateInstance<ItemData>();
            item.id = id;
            item.title = title;
            item.desc = desc;
            item.type = itemType;
            item.inventory_max = inventoryMax;
            item.craftable = false;  // 베이킹으로만 획득
            item.eat_hunger = eatHunger;
            item.eat_hp = eatHp;

            AssetDatabase.CreateAsset(item, path);
            Debug.Log($"생성: {path}");
        }
    }
}
