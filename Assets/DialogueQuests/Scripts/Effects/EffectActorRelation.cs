using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Change the relation value of an actor
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Actor Relation", order = 10)]
    public class EffectActorRelation: EffectData
    {
        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            NarrativeData ndata = NarrativeData.Get();
            ActorData actor = effect.value_data as ActorData;
            if (actor != null)
            {
                if (effect.oper == NarrativeEffectOperator.Set)
                    ndata.SetActorValue(actor.actor_id, effect.value_int);

                if (effect.oper == NarrativeEffectOperator.Add)
                {
                    int value = ndata.GetActorValue(actor.actor_id);
                    ndata.SetActorValue(actor.actor_id, value + effect.value_int);
                }
            }
        }

        public override bool ShowValueData()
        {
            return true;
        }

        public override bool ShowOperator()
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
    }

}
