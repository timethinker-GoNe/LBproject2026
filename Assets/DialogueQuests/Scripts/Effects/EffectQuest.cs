using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Effect to start/complete/cancel a quest
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Quest", order = 10)]
    public class EffectQuest : EffectData
    {
        public QuestEffectType type;
        
        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            QuestData quest = effect.value_data as QuestData;
            if (quest != null)
            {
                if (type == QuestEffectType.Start)
                    NarrativeManager.Get().StartQuest(quest);

                if (type == QuestEffectType.Complete)
                    NarrativeManager.Get().CompleteQuest(quest);

                if (type == QuestEffectType.Fail)
                    NarrativeManager.Get().FailQuest(quest);

                if (type == QuestEffectType.Cancel)
                    NarrativeManager.Get().CancelQuest(quest);
            }

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

    public enum QuestEffectType
    {
        None = 0,
        Start = 10,     //Start quest
        Complete = 20,  //Complete quest successfully
        Fail = 30,      //Complete quest unsuccesfully (cant be done again)
        Cancel = 40,    //Cancel quest as if its not started (can be done again later)
    }
}
