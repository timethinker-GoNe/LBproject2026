using FarmingEngine;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// Script to allow player running
    /// </summary>

    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterRun : MonoBehaviour
    {
        public float run_speed = 20f;
        public float anim_speed_adjust = 7f;

        public UnityAction<float> onRun;
        private PlayerCharacter character;

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();
        }

        public void Run()
        {
            if (character.IsGrounded() && !character.IsBusy() && !character.IsRiding() && !character.IsSwimming())
            {
                character.SetRun(true);

                if (onRun != null)
                    onRun.Invoke(run_speed / anim_speed_adjust);
            }
        }

        public void StopRun()
        {
            character.SetRun(false);

            if (onRun != null)
                onRun.Invoke(1f);
        }
    }
}
