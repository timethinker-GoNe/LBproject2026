using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if SURVIVAL_ENGINE || SURVIVAL_ENGINE_ONLINE
using SurvivalEngine;
#endif

#if FARMING_ENGINE
using FarmingEngine;
#endif

namespace DialogueQuests
{
    /// <summary>
    /// Destroy an object in the scene
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Destroy", order = 10)]
    public class EffectDestroy: EffectData
    {
        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            GameObject targ = effect.value_object;
#if SURVIVAL_ENGINE || FARMING_ENGINE || SURVIVAL_ENGINE_ONLINE
                Destructible destruct = targ.GetComponent<Destructible>();
                Selectable select = targ.GetComponent<Selectable>();
                if (destruct != null)
                    destruct.Kill();
                else if (select != null)
                    select.Destroy();
                else
                    Destroy(targ);
#else
            Destroy(targ);
#endif
        }

        public override bool ShowValueObject()
        {
            return true;
        }

        public override string GetLabelValueObject()
        {
            return "Prefab";
        }
    }

}
