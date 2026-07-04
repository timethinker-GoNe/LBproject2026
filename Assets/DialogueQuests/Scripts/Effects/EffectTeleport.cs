using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Effect to teleport a gameobject
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Teleport", order = 10)]
    public class EffectTeleport : EffectData
    {
        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            GameObject targ = effect.value_object;
            if (targ != null && !string.IsNullOrWhiteSpace(effect.target_id))
            {
                Region region = Region.Get(effect.target_id);
                if (region != null)
                {
                    Vector3 pos = region.PickRandomPosition();
                    targ.transform.position = pos;
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

        public override bool ShowTargetID()
        {
            return true;
        }

        public override string GetLabelTargetID()
        {
            return "Region";
        }
    }

}
