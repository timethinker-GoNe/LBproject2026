using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if SURVIVAL_ENGINE || SURVIVAL_ENGINE_ONLINE
using SurvivalEngine;
#endif

#if FARMING_ENGINE
using FarmingEngine;
#endif

#if SURVIVAL_ENGINE || SURVIVAL_ENGINE_ONLINE || FARMING_ENGINE

namespace DialogueQuests
{
    /// <summary>
    /// Condition that check item quantity in the inventory
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Conditions/Inventory", order = 10)]
    public class ConditionInventory : ConditionData
    {
        public override bool IsMet(NarrativeEvent evt, NarrativeCondition condition, Actor player, Actor triggerer) 
        {
            PlayerCharacter character = player.GetComponent<PlayerCharacter>();
            ItemData item = condition.value_data as ItemData;
            int i1 = character != null && item != null ? character.Inventory.CountItem(item) : 0;
            return condition.CompareInt(i1, condition.value_int);
        }

        public override bool ShowValueData()
        {
            return true;
        }

        public override bool ShowValueInt()
        {
            return true;
        }

        public override string GetLabelValueData()
        {
            return "Item";
        }

        public override bool ShowOperatorInt()
        {
            return true;
        }

        public override Type GetDataType()
        {
            return typeof(ItemData);
        }
    }
}

#endif
