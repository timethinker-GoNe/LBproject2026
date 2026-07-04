using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 캐릭터 배경 선택 카드 UI 컴포넌트.
    /// CharCreateManager가 3개 인스턴스를 관리.
    /// </summary>
    public class BackgroundCardUI : MonoBehaviour
    {
        [Header("UI References")]
        public Text    title_text;
        public Text    desc_text;
        public Image   card_image;
        public Button  select_button;
        public Image   selected_frame;  // 선택됐을 때 하이라이트 테두리

        private string bg_id;
        private UnityAction<string> on_select;

        public void Setup(string backgroundId, string title, string desc, UnityAction<string> onSelect)
        {
            bg_id     = backgroundId;
            on_select = onSelect;

            if (title_text != null) title_text.text = title;
            if (desc_text  != null) desc_text.text  = desc;

            select_button.onClick.RemoveAllListeners();
            select_button.onClick.AddListener(() => on_select?.Invoke(bg_id));

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selected_frame != null)
                selected_frame.enabled = selected;
        }

        public string GetBackgroundId() { return bg_id; }
    }
}
