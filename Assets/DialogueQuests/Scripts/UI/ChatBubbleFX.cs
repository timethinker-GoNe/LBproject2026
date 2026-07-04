using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests {

    public class ChatBubbleFX : MonoBehaviour
    {
        private GameObject target;
        private Vector3 offset;
        private bool should_hide = false;
        private float timer = 0f;

        void Start()
        {
            gameObject.SetActive(false);

            if (NarrativeManager.Get())
            {
                NarrativeManager.Get().onDialogueMessageStart += OnMsg;
                NarrativeManager.Get().onDialogueMessageEnd += OnMsgEnd;
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
            if(should_hide && timer > 0.1f)
                gameObject.SetActive(false);
        }

        void OnMsg(NarrativeEventLine line, DialogueMessage msg) {

            if (msg.actor)
            {
                Actor player = Actor.GetPlayerActor();
                Vector3 pos = player ? player.transform.position : transform.position;
                Actor actor = Actor.GetNearestActor(msg.actor, pos);
                if (actor != null && actor.show_chat)
                {
                    target = actor.gameObject;
                    offset = actor.icon_offset;
                    transform.position = actor.transform.position + actor.icon_offset;
                    transform.localScale = Vector3.one * actor.icon_size;
                    should_hide = false;
                    timer = 0f;

                    gameObject.SetActive(true);
                }
            }
        }

        void OnMsgEnd(NarrativeEventLine line, DialogueMessage msg) {
            should_hide = true;
            timer = 0f;
        }
    }

}
