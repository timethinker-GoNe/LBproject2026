using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests {

    public class InteractIconFX : MonoBehaviour
    {
        public GameObject icon;

        private GameObject target;
        private Vector3 offset;
        private float timer = 0f;

        void Start()
        {
            icon.SetActive(false);

            if (NarrativeManager.Get())
            {
                NarrativeManager.Get().onEventStart += OnEventStart;
            }
        }

        void Update()
        {
            if(target != null)
                transform.position = target.transform.position + offset;

            Camera cam = Camera.main;
            if (cam != null)
            {
                transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
            }

            timer += Time.deltaTime;
            if (timer > 0.5f)
            {
                timer = 0f;
                SlowUpdate();
            }
        }

        private void SlowUpdate()
        {
            bool should_show = false;
            Actor player = Actor.GetPlayerActor();
            if (player != null)
            {
                Actor nearest = Actor.GetNearestNPC(player.transform.position);
                if (nearest != null && nearest.show_interact && nearest.CanInteract(player))
                {
                    if (nearest.HasImportantEvents(NarrativeEventType.InteractActor, player))
                    {
                        should_show = true;
                        target = nearest.gameObject;
                        offset = nearest.icon_offset;
                        transform.position = nearest.transform.position + nearest.icon_offset;
                        transform.localScale = Vector3.one * nearest.icon_size;
                    }
                }
            }

            if (should_show != icon.activeSelf)
                icon.SetActive(should_show);
        }

        private void OnEventStart(NarrativeEvent evt)
        {
            if(icon.activeSelf)
                icon.SetActive(false);
        }
    }

}
