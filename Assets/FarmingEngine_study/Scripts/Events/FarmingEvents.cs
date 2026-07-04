using System;

namespace FarmingEngine
{
    public static class FarmingEvents
    {
        public static Action<PlayerCharacter> onTilledSoil;
        public static Action<PlayerCharacter> onPlantedSeed;
        public static Action<PlayerCharacter> onWateredPlant;
        public static Action<PlayerCharacter> onHarvestedPlant;

        // Quest system events
        public static Action<string, int> OnCropHarvested;   // cropId, amount
        public static Action<string, int> OnItemCollected;   // itemId, amount
        public static Action<string> OnNpcTalked;            // npcId
    }
}
