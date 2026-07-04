using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    /// Gain or lose item
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Inventory", order = 10)]
    public class EffectInventory : EffectData
    {
        public bool gain;
        public bool lose;

        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            PlayerCharacter character = player.GetComponent<PlayerCharacter>();
            ItemData item = effect.value_data as ItemData;
            if (character != null)
            {
                if(gain)
                    character.Inventory.GainItem(item, effect.value_int, character.transform.position);
                if(lose)
                    character.Inventory.UseItem(item, effect.value_int);
            }
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

        public override System.Type GetDataType()
        {
            return typeof(ItemData);
        }
    }

}

#endif