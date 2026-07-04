using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Check quest step value
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Conditions/Quest Step", order = 10)]
    public class ConditionQuestStep : ConditionData
    {
        public override bool IsMet(NarrativeEvent evt, NarrativeCondition condition, Actor player, Actor triggerer)
        {
            QuestData quest = condition.value_data as QuestData;
            if (quest != null)
            {
                int avalue = quest.GetQuestStep();
                return condition.CompareInt(avalue, condition.value_int);
            }
            return false;
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

        public override System.Type GetDataType()
        {
            return typeof(QuestData);
        }
    }
}
