using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Condition to check a custom value (string, int or float)
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Conditions/Custom Value", order = 10)]
    public class ConditionCustomValue : ConditionData
    {
        public CustomValueType type;
        public CustomValueTarget target;

        public override bool IsMet(NarrativeEvent evt, NarrativeCondition condition, Actor player, Actor triggerer)
        {
            NarrativeData ndata = NarrativeData.Get();

            if (type == CustomValueType.Integer)
            {
                int i1 = ndata.GetCustomInt(condition.target_id);
                int i2 = target == CustomValueTarget.OtherCustomValue ? ndata.GetCustomInt(condition.other_target_id) : condition.value_int;
                return condition.CompareInt(i1, i2);
            }

            if (type == CustomValueType.Float)
            {
                float f1 = ndata.GetCustomFloat(condition.target_id);
                float f2 = target == CustomValueTarget.OtherCustomValue ? ndata.GetCustomFloat(condition.other_target_id) : condition.value_float;
                return condition.CompareFloat(f1, f2);
            }

            if (type == CustomValueType.String)
            {
                string s1 = ndata.GetCustomString(condition.target_id);
                string s2 = target == CustomValueTarget.OtherCustomValue ? ndata.GetCustomString(condition.other_target_id) : condition.value_string;
                return condition.CompareString(s1, s2);
            }

            return false;
        }

        public override bool ShowTargetID()
        {
            return true;
        }

        public override bool ShowOtherTargetID()
        {
            return target == CustomValueTarget.OtherCustomValue;
        }

        public override bool ShowOperatorInt()
        {
            return type == CustomValueType.Integer || type == CustomValueType.Float;
        }

        public override bool ShowOperatorBool()
        {
            return type == CustomValueType.String;
        }

        public override bool ShowValueInt()
        {
            return type == CustomValueType.Integer && target == CustomValueTarget.FixedValue;
        }

        public override bool ShowValueFloat()
        {
            return type == CustomValueType.Float && target == CustomValueTarget.FixedValue;
        }
        
        public override bool ShowValueString()
        {
            return type == CustomValueType.String && target == CustomValueTarget.FixedValue;
        }

        public override string GetLabelTargetID()
        {
            return "Variable ID";
        }
    }

    public enum CustomValueTarget
    {
        FixedValue = 0,
        OtherCustomValue = 10,
    }
}
