namespace FarmingQuest
{
    public static class ObjectiveProcessor
    {
        public static bool HandleEvent(QuestProgress progress, QuestDefinition def, string objectiveType, string targetId, int amount)
        {
            bool anyUpdated = false;
            for (int i = 0; i < def.objectives.Count; i++)
            {
                var objDef = def.objectives[i];
                var objProgress = progress.objectiveProgresses[i];

                if (objProgress.completed) continue;
                if (objDef.type != objectiveType) continue;
                if (!string.IsNullOrEmpty(objDef.targetId) && objDef.targetId != targetId) continue;

                objProgress.currentAmount += amount;
                if (objProgress.currentAmount >= objDef.requiredAmount)
                {
                    objProgress.currentAmount = objDef.requiredAmount;
                    objProgress.completed = true;
                }
                anyUpdated = true;
            }
            return anyUpdated;
        }

        public static bool AllCompleted(QuestProgress progress)
        {
            foreach (var op in progress.objectiveProgresses)
                if (!op.completed) return false;
            return true;
        }
    }
}
