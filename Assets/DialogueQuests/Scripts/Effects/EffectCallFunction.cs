using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Effect to teleport a gameobject
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Call Function", order = 10)]
    public class EffectCallFunction : EffectData
    {
        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            if (effect.callfunc_evt != null)
            {
                effect.callfunc_evt.Invoke();
            }
        }

        public override bool ShowFunction()
        {
            return true;
        }
    }

}
