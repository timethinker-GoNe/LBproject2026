using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Base class for creating custom conditions, inherit from this class, add the [CreateAssetMenu()] tag
    /// And then create a data file based on this script, the IsMet() function will be called when checking the condition
    /// </summary>

    public class ConditionData : ScriptableObject
    {
        public virtual bool IsMet(NarrativeEvent evt, NarrativeCondition condition, Actor player, Actor triggerer) 
        {
            return true; //Override this function in your custom condition
        }

        public virtual bool ShowTargetID() { return false; }
        public virtual bool ShowOtherTargetID() { return false; }
        public virtual bool ShowOperatorInt() { return false; }
        public virtual bool ShowOperatorBool() { return false; }
        public virtual bool ShowValueObject() { return false; }
        public virtual bool ShowValueData() { return false; }
        public virtual bool ShowValueString() { return false; }
        public virtual bool ShowValueInt() { return false; }
        public virtual bool ShowValueFloat() { return false; }

        public virtual string GetLabelTargetID() { return "Target ID"; }
        public virtual string GetLabelOtherTargetID() { return "Other Target ID"; }
        public virtual string GetLabelOperatorInt() { return "Operator"; }
        public virtual string GetLabelOperatorBool() { return "Operator"; }
        public virtual string GetLabelValueObject() { return "Value"; }
        public virtual string GetLabelValueData() { return "Value"; }
        public virtual string GetLabelValueString() { return "Value"; }
        public virtual string GetLabelValueInt() { return "Value"; }
        public virtual string GetLabelValueFloat() { return "Value"; }

        public virtual System.Type GetDataType() { return typeof(ScriptableObject); }


    }

}
