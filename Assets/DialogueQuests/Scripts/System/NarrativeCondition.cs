using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DialogueQuests
{
    public class NarrativeCondition : MonoBehaviour
    {
        public ConditionData condition;
        public NarrativeConditionOperatorInt oper;
        public NarrativeConditionOperatorBool oper2;
        public string target_id = "";
        public string other_target_id;
        public GameObject value_object;
        public ScriptableObject value_data;
        public int value_int = 0;
        public float value_float = 0f;
        public string value_string = "";

        private void Start()
        {
            if (value_data != null)
            {
                if (value_data is ActorData)
                    ActorData.Load((ActorData)value_data);
                if (value_data is QuestData)
                    QuestData.Load((QuestData)value_data);
            }
        }

        public bool IsMet(NarrativeEvent evt, Actor player, Actor triggerer)
        {
            if (condition != null)
                return condition.IsMet(evt, this, player, triggerer);
            return true;
        }

        public bool CompareInt(int ival1, int ival2)
        {
            bool condition_met = true;
            if (oper == NarrativeConditionOperatorInt.Equal && ival1 != ival2)
            {
                condition_met = false;
            }
            if (oper == NarrativeConditionOperatorInt.NotEqual && ival1 == ival2)
            {
                condition_met = false;
            }
            if (oper == NarrativeConditionOperatorInt.GreaterEqual && ival1 < ival2)
            {
                condition_met = false;
            }
            if (oper == NarrativeConditionOperatorInt.LessEqual && ival1 > ival2)
            {
                condition_met = false;
            }
            if (oper == NarrativeConditionOperatorInt.Greater && ival1 <= ival2)
            {
                condition_met = false;
            }
            if (oper == NarrativeConditionOperatorInt.Less && ival1 >= ival2)
            {
                condition_met = false;
            }
            return condition_met;
        }

        public bool CompareFloat(float fval1, float fval2)
        {
            bool condition_met = true;
            if (oper == NarrativeConditionOperatorInt.Equal && fval1 != fval2)
            {
                condition_met = false;
            }
            if (oper == NarrativeConditionOperatorInt.NotEqual && fval1 == fval2)
            {
                condition_met = false;
            }
            if (oper == NarrativeConditionOperatorInt.GreaterEqual && fval1 < fval2)
            {
                condition_met = false;
            }
            if (oper == NarrativeConditionOperatorInt.LessEqual && fval1 > fval2)
            {
                condition_met = false;
            }
            if (oper == NarrativeConditionOperatorInt.Greater && fval1 <= fval2)
            {
                condition_met = false;
            }
            if (oper == NarrativeConditionOperatorInt.Less && fval1 >= fval2)
            {
                condition_met = false;
            }
            return condition_met;
        }

        public bool CompareString(string sval1, string sval2)
        {
            bool condition_met = true;
            if (oper == NarrativeConditionOperatorInt.Equal && sval1 != sval2)
            {
                condition_met = false;
            }
            if (oper == NarrativeConditionOperatorInt.NotEqual && sval1 == sval2)
            {
                condition_met = false;
            }
            if (oper == NarrativeConditionOperatorInt.GreaterEqual && sval1 != sval2)
            {
                condition_met = false;
            }
            if (oper == NarrativeConditionOperatorInt.LessEqual && sval1 != sval2)
            {
                condition_met = false;
            }
            if (oper == NarrativeConditionOperatorInt.Greater && sval1 == sval2)
            {
                condition_met = false;
            }
            if (oper == NarrativeConditionOperatorInt.Less && sval1 == sval2)
            {
                condition_met = false;
            }
            return condition_met;
        }

        public bool CompareBool(bool cond)
        {
            if (oper2 == NarrativeConditionOperatorBool.IsFalse)
                return !cond;
            return cond;
        }
    }

    public enum NarrativeConditionOperatorInt
    {
        Equal = 0,
        NotEqual = 1,
        GreaterEqual = 2,
        LessEqual = 3,
        Greater = 4,
        Less = 5,
    }

    public enum NarrativeConditionOperatorBool
    {
        IsTrue = 0,
        IsFalse = 1,
    }

    public enum NarrativeConditionTargetType
    {
        Value = 0,
        Target = 1,
    }

}
