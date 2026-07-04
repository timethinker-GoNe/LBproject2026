using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Check if another event has already been triggered
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Conditions/Event Triggered", order = 10)]
    public class ConditionEventTriggered : ConditionData
    {
        public override bool IsMet(NarrativeEvent evt, NarrativeCondition condition, Actor player, Actor triggerer) 
        {
            GameObject targ = condition.value_object;
            if (targ && targ.GetComponent<NarrativeEvent>())
            {
                NarrativeEvent oevt = targ.GetComponent<NarrativeEvent>();
                return condition.CompareBool(oevt.GetTriggerCount() >= 1);
            }
            return false;
        }

        public override bool ShowOperatorBool()
        {
            return true;
        }

        public override bool ShowValueObject()
        {
            return true;
        }

        public override string GetLabelValueObject()
        {
            return "Event";
        }
    }

}
