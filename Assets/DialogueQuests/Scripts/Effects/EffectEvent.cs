using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Starts another Narrative Event
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Start Event", order = 10)]
    public class EffectEvent: EffectData
    {
        public bool check_conditions;

        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            NarrativeEvent nevent = effect.value_object.GetComponent<NarrativeEvent>();
            if (nevent != null)
            {
                if (!check_conditions || nevent.AreConditionsMet(player, triggerer))
                {
                    nevent.TriggerImmediately(player, triggerer);
                }
            }
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
