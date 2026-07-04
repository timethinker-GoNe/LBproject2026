using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    [RequireComponent(typeof(Selectable))]
    public class NPCDialogue : MonoBehaviour
    {
        public string character_name;
        private Selectable selectable;

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
        }

        public void OpenDialogue(PlayerCharacter player)
        {
            OpenDialogue(character_name, "Hi", null, player);
        }

        public void OpenDialogue(string character_name, string dialogue, Sprite sp, PlayerCharacter player)
        {
            DialoguePanel.Get().ShowPanel(character_name, dialogue, sp, player);
        }
    }
}