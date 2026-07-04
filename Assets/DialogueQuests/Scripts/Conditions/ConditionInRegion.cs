using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Condition that checks the status of a quest
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Conditions/Inside Region", order = 10)]
    public class ConditionInRegion : ConditionData
    {
        public override bool IsMet(NarrativeEvent evt, NarrativeCondition condition, Actor player, Actor triggerer)
        {
            bool met = false;
            ActorData adata = condition.value_data as ActorData;
            if (adata)
            {
                Actor actor = Actor.Get(adata);
                Region region = Region.Get(condition.target_id);
                if (actor && region)
                    met = region.IsInsideXZ(actor.transform.position);
            }

            return condition.CompareBool(met);
        }

        public override bool ShowTargetID()
        {
            return true;
        }

        public override bool ShowOperatorBool()
        {
            return true;
        }

        public override bool ShowValueData()
        {
            return true;
        }

        public override string GetLabelValueData()
        {
            return "Actor";
        }

        public override string GetLabelTargetID()
        {
            return "Region";
        }

        public override System.Type GetDataType()
        {
            return typeof(ActorData);
        }
    }

}
