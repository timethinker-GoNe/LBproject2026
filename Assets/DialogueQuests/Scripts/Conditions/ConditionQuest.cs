using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Condition that checks the status of a quest
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Conditions/Quest", order = 10)]
    public class ConditionQuest : ConditionData
    {
        public QuestConditionType type;

        public override bool IsMet(NarrativeEvent evt, NarrativeCondition condition, Actor player, Actor triggerer) 
        {
            QuestData quest = (QuestData)condition.value_data;
            if (quest != null)
            {
                bool condition_met = false;
                
                if(type == QuestConditionType.Started)
                    condition_met  = quest.IsStarted();

                if (type == QuestConditionType.Active)
                    condition_met = quest.IsActive();

                if (type == QuestConditionType.Completed)
                    condition_met = quest.IsCompleted();

                if (type == QuestConditionType.Failed)
                    condition_met = quest.IsFailed();

                return condition.CompareBool(condition_met);
            }

            return false;
        }

        public override bool ShowOperatorBool()
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

    public enum QuestConditionType
    {
        None = 0,
        Started = 5,     // Quest is either active, completed or failed
        Active = 10,     // Quest is currently active
        Completed = 20,   // Quest is currently completed
        Failed = 30,      // Quest is currently failed
    }
}
