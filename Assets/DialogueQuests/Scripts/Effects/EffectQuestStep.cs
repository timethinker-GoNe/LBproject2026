using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Set quest to next step (and change description)
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Quest Step", order = 10)]
    public class EffectQuestStep : EffectData
    {
        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            QuestData quest = effect.value_data as QuestData;
            if (effect.oper == NarrativeEffectOperator.Add)
                NarrativeManager.Get().AddQuestStep(quest, effect.value_int);
            else if (effect.oper == NarrativeEffectOperator.Set)
                NarrativeManager.Get().SetQuestStep(quest, effect.value_int);
        }

        public override bool ShowValueData()
        {
            return true;
        }

        public override bool ShowOperator()
        {
            return true;
        }

        public override bool ShowValueInt()
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
