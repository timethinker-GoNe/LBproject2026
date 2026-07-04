using UnityEngine;
using UnityEngine.Events;

#if DIALOGUE_QUESTS
using DialogueQuests;
#endif

namespace FarmingEngine
{
    /// <summary>
    /// 통합 저장/로드 조율 매니저.
    /// FarmingEngine(.farming) + DialogueQuests(.dq) + SlotInfo(.slotinfo) 동시 처리.
    /// Scene_Farm_01 이상 게임 씬에 배치. 경량 씬(StartScreen 등)에서는 static 메서드만 사용.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static readonly string[] SlotNames = { "slot1", "slot2", "slot3" };

        private static SaveManager _instance;
        public static SaveManager Get() { return _instance; }

        void Awake()
        {
            _instance = this;
        }

        // ── 인게임 저장 (TheGame이 존재하는 씬에서 호출) ──────────────────────────

        /// <summary>현재 게임 상태를 슬롯에 저장</summary>
        public void SaveToSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotNames.Length) return;
            string slot = SlotNames[slotIndex];

            TheGame game = TheGame.Get();
            if (game == null) { Debug.LogWarning("[SaveManager] TheGame not found"); return; }

            // TheGame.Save()가 beforeSave → NarrativeData.Save() 까지 연쇄 처리
            game.Save(slot);

            // SlotInfo 갱신
            SaveSlotInfo.SaveFromPlayerData(slot, PlayerData.Get());

            Debug.Log("[SaveManager] Saved to " + slot);
        }

        // ── 정적 로드 (씬 전환 포함, StartScreen에서도 호출 가능) ─────────────────

        /// <summary>슬롯에서 불러오기 → PlayerData.current_scene으로 씬 이동</summary>
        public static void LoadFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotNames.Length) return;
            string slot = SlotNames[slotIndex];

            if (!PlayerData.HasSave(slot))
            {
                Debug.LogWarning("[SaveManager] No save found for " + slot);
                return;
            }

            TheGame.Load(slot); // PlayerData 로드 + afterLoad 이벤트 + 씬 전환
        }

        /// <summary>슬롯 삭제 (PlayerData + DQ + SlotInfo)</summary>
        public static void DeleteSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotNames.Length) return;
            string slot = SlotNames[slotIndex];

            TheGame.DeleteGame(slot);

#if DIALOGUE_QUESTS
            DialogueQuests.NarrativeData.Delete(slot);
#endif
            SaveSlotInfo.Delete(slot);

            Debug.Log("[SaveManager] Deleted " + slot);
        }

        /// <summary>슬롯 미리보기 정보 반환 (씬 로드 없이)</summary>
        public static SaveSlotInfo GetSlotInfo(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotNames.Length) return null;
            return SaveSlotInfo.Load(SlotNames[slotIndex]);
        }

        /// <summary>해당 슬롯에 세이브가 존재하는지</summary>
        public static bool HasSave(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotNames.Length) return false;
            return PlayerData.HasSave(SlotNames[slotIndex]);
        }

        /// <summary>세이브가 하나라도 있는지 (불러오기 버튼 활성화용)</summary>
        public static bool HasAnySave()
        {
            foreach (string slot in SlotNames)
                if (PlayerData.HasSave(slot)) return true;
            return false;
        }
    }
}
