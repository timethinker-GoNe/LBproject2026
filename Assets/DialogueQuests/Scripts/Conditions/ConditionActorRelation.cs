using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Check the actor relation "value" of an actor
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Conditions/Actor Relation", order = 10)]
    public class ConditionActorRelation : ConditionData
    {
        public override bool IsMet(NarrativeEvent evt, NarrativeCondition condition, Actor player, Actor triggerer)
        {
            NarrativeData ndata = NarrativeData.Get();
            ActorData actor = condition.value_data as ActorData;
            if (actor != null)
            {
                int avalue = ndata.GetActorValue(actor.actor_id);
                return condition.CompareInt(avalue, condition.value_int);;
            }
            return false;
        }

        public override bool ShowValueData()
        {
            return true;
        }

        public override bool ShowOperatorInt()
        {
            return true;
        }

        public override bool ShowValueInt()
        {
            return true;
        }

        public override string GetLabelValueData()
        {
            return "Actor";
        }

        public override System.Type GetDataType()
        {
            return typeof(ActorData);
        }
    }

}
