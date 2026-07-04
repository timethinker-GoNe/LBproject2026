using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DialogueQuests
{

    public class QuestPanelLine : MonoBehaviour, IPointerClickHandler {

        public Text quest_title;
        public Text quest_text;
        public Image quest_icon;
        public Image quest_completed;
        public Image highlight;

        public Sprite success_sprite;
        public Sprite fail_sprite;

        public UnityAction<QuestPanelLine> onClick;

        private QuestData quest;
        private bool selected;

        void Awake()
        {

        }

        public void SetLine(QuestData quest)
        {
            this.quest = quest;
            quest_title.text = quest.GetTitle();

            if (quest_text != null)
                quest_text.text = quest.GetDesc();

            if (quest_icon != null)
            {
                quest_icon.sprite = quest.icon;
                quest_icon.enabled = quest.icon != null;
            }

            if (quest_completed != null)
            {
                bool completed = quest.IsCompleted();
                bool failed = quest.IsFailed();
                quest_completed.enabled = completed || failed;
                quest_completed.sprite = completed ? success_sprite : fail_sprite;
            }

            gameObject.SetActive(true);
        }

        public void SetSelected(bool selected)
        {
            this.selected = selected;
            if (highlight != null)
                highlight.enabled = selected;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            onClick?.Invoke(this);
        }

        public QuestData GetQuest()
        {
            return quest;
        }

        public bool GetSelected()
        {
            return selected;
        }
    }

}
