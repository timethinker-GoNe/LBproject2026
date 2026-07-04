using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Base class for creating custom effects, inherit from this class, add the [CreateAssetMenu()] tag
    /// And then create a data file based on this script, the DoEffect() function will be called when resolving the effect
    /// </summary>
    /// 
    public class EffectData : ScriptableObject
    {
        
        public virtual void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            //Override this function in your custom effect
        }

        public virtual bool ShowTargetID(){ return false; }
        public virtual bool ShowOperator(){ return false; }
        public virtual bool ShowValueObject(){ return false; }
        public virtual bool ShowValueData(){ return false; }
        public virtual bool ShowValueAudio(){ return false; }
        public virtual bool ShowValueString(){ return false; }
        public virtual bool ShowValueInt(){ return false; }
        public virtual bool ShowValueFloat(){ return false; }
        public virtual bool ShowFunction(){ return false; }

        public virtual string GetLabelTargetID() { return "Target ID"; }
        public virtual string GetLabelOperator() { return "Operator"; }
        public virtual string GetLabelValueObject() { return "Object"; }
        public virtual string GetLabelValueAudio() { return "Audio"; }
        public virtual string GetLabelValueData() { return "Value"; }
        public virtual string GetLabelValueString() { return "Value"; }
        public virtual string GetLabelValueInt() { return "Value"; }
        public virtual string GetLabelValueFloat() { return "Value"; }
        public virtual string GetLabelFunction() { return "Function"; }

        public virtual System.Type GetDataType() { return typeof(ScriptableObject); }
    }
}
