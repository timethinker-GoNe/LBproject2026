using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DialogueQuests
{
    public class NarrativeEffect : MonoBehaviour
    {
        public EffectData effect;
        public string target_id = "";
        public NarrativeEffectOperator oper;
        public GameObject value_object;
        public ScriptableObject value_data;
        public AudioClip value_audio;
        public int value_int = 0;
        public float value_float = 1f;
        public string value_string = "";

        [SerializeField]
        public UnityEvent callfunc_evt;

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

        public void Trigger(NarrativeEvent evt, Actor player, Actor triggerer)
        {
            if (effect != null)
                effect.DoEffect(evt, this, player, triggerer);
        }

        public float GetWaitTime()
        {
            if (effect is EffectWait)
            {
                return value_float;
            }
            return 0f;
        }
    }

    public enum NarrativeEffectOperator
    {
        Add = 0,
        Set = 1,
    }

}