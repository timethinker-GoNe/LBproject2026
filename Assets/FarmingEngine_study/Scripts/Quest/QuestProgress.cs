using System.Collections.Generic;

namespace FarmingQuest
{
    [System.Serializable]
    public class QuestProgress
    {
        public string questId;
        public QuestStatus status;
        public List<ObjectiveProgress> objectiveProgresses = new List<ObjectiveProgress>();

        public QuestProgress() { }

        public QuestProgress(string id, int objectiveCount)
        {
            questId = id;
            status = QuestStatus.InProgress;
            for (int i = 0; i < objectiveCount; i++)
                objectiveProgresses.Add(new ObjectiveProgress { objectiveIndex = i });
        }
    }

    [System.Serializable]
    public class ObjectiveProgress
    {
        public int objectiveIndex;
        public int currentAmount;
        public bool completed;
    }
}
