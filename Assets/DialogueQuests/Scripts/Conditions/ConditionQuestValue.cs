using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Check quest custom value (each quest can have custom values assigned)
    /// They are integers
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Conditions/Quest Value", order = 10)]
    public class ConditionQuestValue : ConditionData
    {
        public override bool IsMet(NarrativeEvent evt, NarrativeCondition condition, Actor player, Actor triggerer)
        {
            QuestData quest = condition.value_data as QuestData;
            if (quest != null)
            {
                int avalue = quest.GetQuestValue(condition.target_id);
                return condition.CompareInt(avalue, condition.value_int);
            }
            return false;
        }

        public override bool ShowTargetID()
        {
            return true;
        }

        public override bool ShowOperatorInt()
        {
            return true;
        }

        public override bool ShowValueInt()
        {
            return true;
        }

        public override bool ShowValueData()
        {
            return true;
        }

        public override string GetLabelValueData()
        {
            return "Quest";
        }

        public override string GetLabelTargetID()
        {
            return "Variable ID";
        }

        public override System.Type GetDataType()
        {
            return typeof(QuestData);
        }
    }
}
