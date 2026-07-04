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

#if SURVIVAL_ENGINE || FARMING_ENGINE

namespace DialogueQuests
{
    /// <summary>
    /// Condition that check the time of day
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Conditions/Day Time", order = 10)]
    public class ConditionDayTime : ConditionData
    {
        public override bool IsMet(NarrativeEvent evt, NarrativeCondition condition, Actor player, Actor triggerer) 
        {
            float time = PlayerData.Get().day_time;
            return condition.CompareFloat(time, condition.value_float);
        }

        public override bool ShowOperatorInt()
        {
            return true;
        }

        public override bool ShowValueFloat()
        {
            return true;
        }

        public override string GetLabelValueFloat()
        {
            return "Time";
        }
    }
}

#endif
