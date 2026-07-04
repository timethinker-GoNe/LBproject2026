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
    /// Gain or lose player attributes
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Player Attributes", order = 10)]
    public class EffectPlayerAttribute : EffectData
    {
        public AttributeType attribute;

        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            PlayerCharacter character = player.GetComponent<PlayerCharacter>();
            if (character != null && effect.oper == NarrativeEffectOperator.Set)
                character.Attributes.SetAttribute(attribute, effect.value_float);
            if (character != null && effect.oper == NarrativeEffectOperator.Add)
                character.Attributes.AddAttribute(attribute, effect.value_float);
        }

        public override bool ShowOperator()
        {
            return true;
        }

        public override bool ShowValueFloat()
        {
            return true;
        }
    }

}

#endif