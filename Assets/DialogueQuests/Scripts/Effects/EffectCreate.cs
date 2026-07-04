using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if SURVIVAL_ENGINE || SURVIVAL_ENGINE_ONLINE
using SurvivalEngine;
#endif

#if FARMING_ENGINE
using FarmingEngine;
#endif

#if SURVIVAL_ENGINE || SURVIVAL_ENGINE_ONLINE || FARMING_ENGINE

namespace DialogueQuests
{
    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Create SE", order = 10)]
    public class EffectCreate : EffectData
    {
        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            Region region = Region.Get(effect.target_id);
            if (region != null)
                CraftData.Create((CraftData)effect.value_data, region.transform.position);
        }

        public override bool ShowTargetID()
        {
            return true;
        }

        public override bool ShowValueData()
        {
            return true;
        }

        public override string GetLabelTargetID()
        {
            return "Region";
        }
    }

}

#endif