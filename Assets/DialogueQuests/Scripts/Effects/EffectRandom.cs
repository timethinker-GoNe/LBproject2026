using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Generate random value to use with ConditionRandom
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Random", order = 10)]
    public class EffectRandom : EffectData
    {
        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            NarrativeManager.Get().RollRandomValue(effect.target_id, 1, effect.value_int);
        }

        public override bool ShowTargetID()
        {
            return true;
        }

        public override bool ShowValueInt()
        {
            return true;
        }
    }

}
