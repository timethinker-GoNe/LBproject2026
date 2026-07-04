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
    /// Condition that check the day count
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Conditions/Day", order = 10)]
    public class ConditionDay : ConditionData
    {
        public override bool IsMet(NarrativeEvent evt, NarrativeCondition condition, Actor player, Actor triggerer) 
        {
            int day = PlayerData.Get().day;
            return condition.CompareInt(day, condition.value_int);
        }

        public override bool ShowOperatorInt()
        {
            return true;
        }

        public override bool ShowValueInt()
        {
            return true;
        }

        public override string GetLabelValueInt()
        {
            return "Day";
        }
    }
}

#endif
