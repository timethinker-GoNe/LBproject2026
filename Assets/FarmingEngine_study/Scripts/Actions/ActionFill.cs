using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// Fill a jug with water (or other)
    /// </summary>
    
    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Fill", order = 50)]
    public class ActionFill : MAction
    {
        public ItemData filled_item;
        public ItemData empty_item; // the item to consume when triggered from Selectable side (e.g. WateringCan)

        //Merge action (item → selectable)
        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            if (select.HasGroup(merge_target))
            {
                InventoryData inventory = slot.GetInventory();
                inventory.RemoveItemAt(slot.index, 1);
                character.Inventory.GainItem(inventory, filled_item, 1);
            }
        }

        //Selectable action (click Well → ActionSelector → Fill)
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            if (empty_item == null || filled_item == null) return;
            InventoryData inventory = FindInventoryWithItem(character);
            if (inventory == null) return;
            int slot = inventory.GetFirstItemSlot(empty_item.id, 99);
            if (slot >= 0)
            {
                inventory.RemoveItemAt(slot, 1);
                character.Inventory.GainItem(inventory, filled_item, 1);
            }
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            if (empty_item == null) return false;
            return FindInventoryWithItem(character) != null;
        }

        private InventoryData FindInventoryWithItem(PlayerCharacter character)
        {
            InventoryData inv = InventoryData.Get(InventoryType.Inventory, character.player_id);
            if (inv != null && inv.HasItem(empty_item.id)) return inv;
            InventoryData equip = InventoryData.GetEquip(InventoryType.Equipment, character.player_id);
            if (equip != null && equip.HasItem(empty_item.id)) return equip;
            return null;
        }

    }

}