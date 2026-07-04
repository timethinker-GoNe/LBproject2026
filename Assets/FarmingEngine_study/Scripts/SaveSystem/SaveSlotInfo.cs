using System;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 세이브 슬롯 표시용 경량 데이터 컨테이너.
    /// PlayerData 전체를 로드하지 않고 슬롯 미리보기에 사용.
    /// </summary>
    [System.Serializable]
    public class SaveSlotInfo
    {
        public string slot_name;        // "slot1", "slot2", "slot3"
        public string player_name;      // 캐릭터 이름
        public int day;                 // 현재 날짜
        public float play_time;         // 총 플레이 시간(초)
        public string current_scene;    // 마지막 씬
        public DateTime last_save;      // 마지막 저장 시각

        public const string extension = ".slotinfo";

        /// <summary>슬롯 파일이 존재하는지 확인</summary>
        public static bool HasSlot(string slot_name)
        {
            return SaveTool.DoesFileExist(slot_name + extension);
        }

        /// <summary>슬롯 정보 로드. 없으면 null 반환</summary>
        public static SaveSlotInfo Load(string slot_name)
        {
            return SaveTool.LoadFile<SaveSlotInfo>(slot_name + extension);
        }

        /// <summary>슬롯 정보를 PlayerData에서 읽어 저장</summary>
        public static void SaveFromPlayerData(string slot_name, PlayerData pdata)
        {
            SaveSlotInfo info = new SaveSlotInfo();
            info.slot_name    = slot_name;
            info.player_name  = pdata.player_name;
            info.day          = pdata.day;
            info.play_time    = pdata.play_time;
            info.current_scene = pdata.current_scene;
            info.last_save    = DateTime.Now;
            SaveTool.SaveFile<SaveSlotInfo>(slot_name + extension, info);
        }

        /// <summary>슬롯 파일 삭제</summary>
        public static void Delete(string slot_name)
        {
            SaveTool.DeleteFile(slot_name + extension);
        }

        /// <summary>플레이 시간을 "H:MM:SS" 형식으로 반환</summary>
        public string GetPlayTimeString()
        {
            int total = Mathf.RoundToInt(play_time);
            int h = total / 3600;
            int m = (total % 3600) / 60;
            int s = total % 60;
            return string.Format("{0}:{1:D2}:{2:D2}", h, m, s);
        }
    }
}
