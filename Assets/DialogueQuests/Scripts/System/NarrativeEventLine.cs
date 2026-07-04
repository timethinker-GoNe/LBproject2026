using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{

    public class NarrativeEventLine
    {
        public GameObject game_obj;
        public NarrativeEvent parent;
        public DialogueMessage dialogue = null;
        public List<DialogueChoice> choices = new List<DialogueChoice>();
        public List<NarrativeCondition> conditions = new List<NarrativeCondition>();
        public List<NarrativeEffect> effects = new List<NarrativeEffect>();

        public bool AreConditionsMet(Actor player, Actor triggerer)
        {
            bool conditions_met = true;
            foreach (NarrativeCondition condition in conditions)
            {
                if (condition.enabled && !condition.IsMet(parent, player, triggerer))
                {
                    conditions_met = false;
                }
            }
            return conditions_met && game_obj.activeSelf;
        }

        public DialogueChoice GetChoice(int index)
        {
            if (index >= 0 && index < choices.Count)
                return choices[index];
            return null;
        }

        public void TriggerLine()
        {
            NarrativeManager.Get().StartEventLine(this);
        }

        public void TriggerLineIfMet()
        {
            Actor player = NarrativeManager.Get().GetCurrentPlayer();
            Actor triggerer = NarrativeManager.Get().GetCurrentTriggerer();

            if (AreConditionsMet(player, triggerer))
            {
                NarrativeManager.Get().StartEventLine(this);
            }
        }

        public float TriggerEffects(Actor player, Actor triggerer)
        {
            float wait_timer = 0f;
            foreach (NarrativeEffect effect in effects)
            {
                if (effect.enabled)
                {
                    effect.Trigger(parent, player, triggerer);
                    wait_timer += effect.GetWaitTime();
                }
            }
            return wait_timer;
        }
    }

}
