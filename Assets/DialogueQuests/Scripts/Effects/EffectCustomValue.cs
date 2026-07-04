using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Effect to edit a custom value (string, int or float)
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Custom Value", order = 10)]
    public class EffectCustomValue : EffectData
    {
        public CustomValueType type;

        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            NarrativeData ndata = NarrativeData.Get();

            if (type == CustomValueType.Integer)
            {
                if (effect.oper == NarrativeEffectOperator.Set)
                    ndata.SetCustomInt(effect.target_id, effect.value_int);

                if (effect.oper == NarrativeEffectOperator.Add)
                {
                    int value = ndata.GetCustomInt(effect.target_id);
                    ndata.SetCustomInt(effect.target_id, value + effect.value_int);
                }
            }

            if (type == CustomValueType.Float)
            {
                if (effect.oper == NarrativeEffectOperator.Set)
                    ndata.SetCustomFloat(effect.target_id, effect.value_float);

                if (effect.oper == NarrativeEffectOperator.Add)
                {
                    float value = ndata.GetCustomFloat(effect.target_id);
                    ndata.SetCustomFloat(effect.target_id, value + effect.value_float);
                }
            }

            if (type == CustomValueType.String)
            {
                ndata.SetCustomString(effect.target_id, effect.value_string);
            }

        }

        public override bool ShowTargetID()
        {
            return true;
        }

        public override bool ShowOperator()
        {
            return type == CustomValueType.Integer || type == CustomValueType.Float;
        }

        public override bool ShowValueInt()
        {
            return type == CustomValueType.Integer;
        }

        public override bool ShowValueFloat()
        {
            return type == CustomValueType.Float;
        }

        public override bool ShowValueString()
        {
            return type == CustomValueType.String;
        }

        public override string GetLabelTargetID()
        {
            return "Variable ID";
        }
    }

    public enum CustomValueType
    {
        None = 0,
        String = 10,
        Integer = 20,
        Float = 30,
    }

}
