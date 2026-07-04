using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Instantiate an object from a prefab
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Instantiate", order = 10)]
    public class EffectInstantiate : EffectData
    {
        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            if (effect.value_object != null && !string.IsNullOrWhiteSpace(effect.target_id))
            {
                Region region = Region.Get(effect.target_id);
                if (region != null)
                {
                    Vector3 pos = region.PickRandomPosition();
                    Instantiate(effect.value_object, pos, Quaternion.identity);
                }
            }
        }

        public override bool ShowValueObject()
        {
            return true;
        }

        public override string GetLabelValueObject()
        {
            return "Prefab";
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
