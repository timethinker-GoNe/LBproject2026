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
    /// Condition that count SE objects
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Conditions/Count SE Objects", order = 10)]
    public class ConditionCountObjSE : ConditionData
    {
        public override bool IsMet(NarrativeEvent evt, NarrativeCondition condition, Actor player, Actor triggerer) 
        {
            IdData item = condition.value_data as IdData;
            int i1 = SObject.CountSceneObjects(item);
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
            return "SE Object";
        }

        public override Type GetDataType()
        {
            return typeof(IdData);
        }
    }
}

#endif
