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
    /// Condition that check a player attribute
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Conditions/Player Attribute", order = 10)]
    public class ConditionPlayerAttribute : ConditionData
    {
        public AttributeType attribute;

        public override bool IsMet(NarrativeEvent evt, NarrativeCondition condition, Actor player, Actor triggerer) 
        {
            PlayerCharacter character = player.GetComponent<PlayerCharacter>();
            float f1 = character != null ? character.Attributes.GetAttributeValue(attribute) : 0f;
            return condition.CompareFloat(f1, condition.value_float);
        }

        public override bool ShowValueFloat()
        {
            return true;
        }

        public override bool ShowOperatorInt()
        {
            return true;
        }
    }
}

#endif
