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

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Gold", order = 10)]
    public class EffectGold : EffectData
    {
        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            PlayerCharacter character = player.GetComponent<PlayerCharacter>();
            if (character != null)
            {
                character.SaveData.gold += effect.value_int;
                character.SaveData.gold = Mathf.Max(character.SaveData.gold, 0);
            }
        }

        public override bool ShowValueInt()
        {
            return true;
        }
    }

}

#endif