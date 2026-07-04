using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Wait for amount of time
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Wait", order = 10)]
    public class EffectWait : EffectData
    {
        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            //Wait is the only effect with different timing, so its hardcoded into other core scripts
        }

        public override bool ShowValueFloat()
        {
            return true;
        }

        public override string GetLabelValueFloat()
        {
            return "Seconds";
        }
    }

}
