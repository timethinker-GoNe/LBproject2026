using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    /// <summary>
    /// Effect to play Audio fx
    /// </summary>
    /// 

    [CreateAssetMenu(fileName = "condition", menuName = "DialogueQuests/Effects/Play Audio", order = 10)]
    public class EffectPlayAudio : EffectData
    {
        public EffectAudioType type;
        public bool play;

        public override void DoEffect(NarrativeEvent evt, NarrativeEffect effect, Actor player, Actor triggerer)
        {
            if (type == EffectAudioType.SFX && play)
            {
                NarrativeManager.Get().PlaySFX(effect.target_id, effect.value_audio, effect.value_float);
            }

            if (type == EffectAudioType.Music && play)
            {
                NarrativeManager.Get().PlayMusic(effect.target_id, effect.value_audio, effect.value_float);
            }

            if (type == EffectAudioType.Music && !play)
            {
                NarrativeManager.Get().StopMusic(effect.target_id);
            }

        }

        public override bool ShowValueAudio()
        {
            return true;
        }

        public override bool ShowTargetID()
        {
            return true;
        }

        public override bool ShowValueFloat()
        {
            return true;
        }

        public override string GetLabelTargetID()
        {
            return "Channel";
        }

        public override string GetLabelValueFloat()
        {
            return "Volume";
        }
    }

    public enum EffectAudioType 
    {
        SFX = 10,
        Music = 20,
     }
}
