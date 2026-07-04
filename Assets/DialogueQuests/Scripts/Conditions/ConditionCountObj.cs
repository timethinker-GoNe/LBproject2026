using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Condition that counts scene objects
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Conditions/Count Scene Objects", order = 10)]
    public class ConditionCountObj : ConditionData
    {
        public override bool IsMet(NarrativeEvent evt, NarrativeCondition condition, Actor player, Actor triggerer) 
        {
            GameObject[] objs = GameObject.FindGameObjectsWithTag(condition.target_id);
            int i1 = objs.Length;
            int i2 = condition.value_int;
            return condition.CompareInt(i1, i2);
        }

        public override bool ShowTargetID()
        {
            return true;
        }

        public override bool ShowValueInt()
        {
            return true;
        }

        public override bool ShowOperatorInt()
        {
            return true;
        }

        public override string GetLabelTargetID()
        {
            return "Object Tag";
        }
    }
}
