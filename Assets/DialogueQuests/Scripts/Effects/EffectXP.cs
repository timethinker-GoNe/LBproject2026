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
    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Player XP", order = 10)]
    public class EffectXP : EffectData
    {
        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            PlayerCharacter character = player.GetComponent<PlayerCharacter>();
            if (character != null)
                character.Attributes.GainXP(effect.target_id, effect.value_int);
        }

        public override bool ShowTargetID()
        {
            return true;
        }

        public override bool ShowValueInt()
        {
            return true;
        }
    }

}

#endif