using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueQuests
{
    [CreateAssetMenu(fileName ="Quest", menuName = "DialogueQuests/Quest", order= 0)]
    public class QuestData : ScriptableObject {

        [Tooltip("Important: make sure all quests have a unique ID")]
        public string quest_id;

        [Header("Visuals")]
        public string title;
        public Sprite icon;
        [TextArea(3, 5)]
        public string desc;
        public int sort_order;

        [Header("Quest Steps")]
        public QuestStep[] steps;

        public void Begin(){ NarrativeData.Get().StartQuest(quest_id);}
        public void Complete(){ NarrativeData.Get().CompleteQuest(quest_id);}
        public void Fail(){ NarrativeData.Get().FailQuest(quest_id); }
        public void Cancel(){ NarrativeData.Get().CancelQuest(quest_id); }

        public void AddQuestValue(string variable_id, int value){ NarrativeData.Get().AddQuestValue(quest_id, variable_id, value); }
        public void SetQuestValue(string variable_id, int value){ NarrativeData.Get().SetQuestValue(quest_id, variable_id, value); }
        public void AddQuestStep(int value){ NarrativeData.Get().AddQuestStep(quest_id, value); }
        public void SetQuestStep(int value){ NarrativeData.Get().SetQuestStep(quest_id, value); }

        public bool IsStarted() { return NarrativeData.Get().IsQuestStarted(quest_id); }
        public bool IsActive() { return NarrativeData.Get().IsQuestActive(quest_id); }
        public bool IsCompleted() { return NarrativeData.Get().IsQuestCompleted(quest_id); }
        public bool IsFailed() { return NarrativeData.Get().IsQuestFailed(quest_id); }
        public int GetQuestStatus(){ return NarrativeData.Get().GetQuestStatus(quest_id);}
        public int GetQuestValue(string variable_id) { return NarrativeData.Get().GetQuestValue(quest_id, variable_id); }
        public int GetQuestStep() { return NarrativeData.Get().GetQuestStep(quest_id); }

        private static List<QuestData> quest_list = new List<QuestData>();

        public string GetTitle()
        {
            string qtitle = title;
            int step = GetQuestStep() - 1;
            if (step >= 0 && step < steps.Length)
            {
                qtitle = steps[step].title;
            }

            string txt = NarrativeTool.Translate(qtitle);
            return NarrativeTool.ReplaceCodes(txt);
        }

        public string GetDesc()
        {
            string qdescription = desc;
            int step = GetQuestStep() - 1;
            if (step >= 0 && step < steps.Length)
            {
                qdescription = steps[step].desc;
            }

            string txt = NarrativeTool.Translate(qdescription);
            return NarrativeTool.ReplaceCodes(txt);
        }

        public static void Load(QuestData quest)
        {
            if (!quest_list.Contains(quest))
            {
                quest_list.Add(quest);

                if (quest is QuestAutoData)
                    ((QuestAutoData)quest).OnLoad();
            }
        }

        public static QuestData Get(string quest_id)
        {
            foreach (QuestData quest in GetAll())
            {
                if (quest.quest_id == quest_id)
                    return quest;
            }
            return null;
        }

        public static List<QuestData> GetAllActive()
        {
            List<QuestData> valid_list = new List<QuestData>();
            foreach (QuestData aquest in GetAll())
            {
                if (aquest.IsActive())
                    valid_list.Add(aquest);
            }
            return valid_list;
        }

        public static List<QuestData> GetAllStarted()
        {
            List<QuestData> valid_list = new List<QuestData>();
            foreach (QuestData aquest in GetAll())
            {
                if (aquest.IsStarted())
                    valid_list.Add(aquest);
            }
            return valid_list;
        }

        public static List<QuestData> GetAllActiveOrCompleted()
        {
            List<QuestData> valid_list = new List<QuestData>();
            foreach (QuestData aquest in GetAll())
            {
                if (aquest.IsActive() || aquest.IsCompleted())
                    valid_list.Add(aquest);
            }
            return valid_list;
        }

        public static List<QuestData> GetAllActiveOrFailed()
        {
            List<QuestData> valid_list = new List<QuestData>();
            foreach (QuestData aquest in GetAll())
            {
                if (aquest.IsActive() || aquest.IsFailed())
                    valid_list.Add(aquest);
            }
            return valid_list;
        }

        public static List<QuestData> GetAll()
        {
            return quest_list;
        }
    }

    [System.Serializable]
    public class QuestStep
    {
        public string title;
        [TextArea(3, 5)]
        public string desc;
    }
}
