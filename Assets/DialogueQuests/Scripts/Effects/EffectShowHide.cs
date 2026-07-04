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
    /// Effect show or hide an object
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Show Hide", order = 10)]
    public class EffectShowHide : EffectData
    {
        public bool show;

        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            if (show)
            {
                GameObject targ = effect.value_object;
                if (targ != null)
                {
#if SURVIVAL_ENGINE || FARMING_ENGINE || SURVIVAL_ENGINE_ONLINE
                    UniqueID uid = targ.GetComponent<UniqueID>();
                    if(uid != null)
                        uid.Show();
                    else
                        targ.SetActive(true);
#else
                    targ.SetActive(true);
#endif
                }
            }

            else
            {
                GameObject targ = effect.value_object;
                if (targ != null)
                {
#if SURVIVAL_ENGINE || FARMING_ENGINE || SURVIVAL_ENGINE_ONLINE
                    UniqueID uid = targ.GetComponent<UniqueID>();
                    if(uid != null)
                        uid.Hide();
                    else
                        targ.SetActive(false);
#else
                    targ.SetActive(false);
#endif
                }
            }

        }

        public override bool ShowValueObject()
        {
            return true;
        }

        public override string GetLabelValueObject()
        {
            return "Target";
        }
    }

}
