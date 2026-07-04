using FarmingEngine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FarmingEngine
{
    public class DialoguePanel : UIPanel
    {
        public TextMeshProUGUI character_name;
        public TextMeshProUGUI dialogue;
        public Image character_image;
        public Button next_button;
        public Button exit_button;

        private PlayerCharacter current_player;
        private static DialoguePanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
            next_button.onClick.AddListener(() => ClickNext(character_name.text, "Not Found", null));
            exit_button.onClick.AddListener(ClickOK);
        }

        private void SetData(string character_name, string dialogue, Sprite character_sprite)
        {
            this.character_name.text = character_name;

            if (this.dialogue != null)
                this.dialogue.text = dialogue;

            if (character_image != null)
                character_image.sprite = character_sprite;
        }

        public void ShowPanel(string character_name, string dialogue, Sprite character_sprite, PlayerCharacter player)
        {
            SetData(character_name, dialogue, character_sprite);
            Show();

            current_player = player;
            current_player.SetBusy(true);
        }

        // 현재 사용 X, 다음 대화로 넘어가는 기능 추가 후 사용될 예정
        public void ClickNext(string character_name, string dialogue, Sprite character_sprite)
        {
            SetData(character_name, dialogue, character_sprite);
        }

        public void ClickOK()
        {
            current_player.SetBusy(false);
            Hide();
        }

        public static DialoguePanel Get()
        {
            return instance;
        }
    }
}
