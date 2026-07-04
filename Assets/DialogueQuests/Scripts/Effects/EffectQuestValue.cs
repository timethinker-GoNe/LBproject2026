using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Add to quest custom value (each quest can have custom values assigned)
    /// They are integers
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Quest Value", order = 10)]
    public class EffectQuestValue : EffectData
    {
        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            QuestData quest = effect.value_data as QuestData;
            if (effect.oper == NarrativeEffectOperator.Add)
                NarrativeManager.Get().AddQuestValue(quest, effect.target_id, effect.value_int);
            else if (effect.oper == NarrativeEffectOperator.Set)
                NarrativeManager.Get().SetQuestValue(quest, effect.target_id, effect.value_int);
        }

        public override bool ShowTargetID()
        {
            return true;
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
