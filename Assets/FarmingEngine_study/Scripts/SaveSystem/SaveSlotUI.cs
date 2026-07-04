using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 세이브 슬롯 한 칸 UI 컴포넌트.
    /// SaveSlotPanel이 3개 인스턴스를 관리.
    /// </summary>
    public class SaveSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        public Text slot_label;       // "슬롯 1"
        public Text player_name_text; // 캐릭터 이름
        public Text day_text;         // "Day 12"
        public Text play_time_text;   // "1:23:45"
        public Text last_save_text;   // "2024-01-16 15:30"
        public Text empty_label;      // "빈 슬롯" (데이터 없을 때)
        public Button select_button;

        private int slot_index;
        private bool has_data;
        private UnityAction<int> on_select;

        public void Setup(int index, SaveSlotInfo info, UnityAction<int> onSelect)
        {
            slot_index = index;
            on_select  = onSelect;
            has_data   = (info != null);

            if (slot_label != null)
                slot_label.text = "슬롯 " + (index + 1);

            if (has_data)
            {
                if (empty_label   != null) empty_label.gameObject.SetActive(false);
                if (player_name_text != null) player_name_text.text = info.player_name;
                if (day_text      != null) day_text.text      = "Day " + info.day;
                if (play_time_text != null) play_time_text.text = info.GetPlayTimeString();
                if (last_save_text != null) last_save_text.text = info.last_save.ToString("yy-MM-dd HH:mm");
            }
            else
            {
                if (empty_label    != null) empty_label.gameObject.SetActive(true);
                if (player_name_text != null) player_name_text.text = "";
                if (day_text       != null) day_text.text      = "";
                if (play_time_text != null) play_time_text.text = "";
                if (last_save_text != null) last_save_text.text = "";
            }

            select_button.onClick.RemoveAllListeners();
            select_button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            on_select?.Invoke(slot_index);
        }

        public bool HasData() { return has_data; }
        public int  GetIndex() { return slot_index; }
    }
}
