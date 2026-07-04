using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Condition that checks the status of a quest
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Conditions/Visible", order = 10)]
    public class ConditionVisible : ConditionData
    {
        public override bool IsMet(NarrativeEvent evt, NarrativeCondition condition, Actor player, Actor triggerer) 
        {
            bool condition_met = condition.value_object != null && condition.value_object.activeSelf;
            return condition.CompareBool(condition_met);
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
            return "Target";
        }
    }

}
