using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// Scene_CharCreate 전용 매니저.
    /// TheGame / TheData 없이 동작하는 경량 씬.
    /// 완료 시: PlayerData.NewGame() → 필드 주입 → Scene_Farm_01 이동.
    /// </summary>
    public class CharCreateManager : MonoBehaviour
    {
        [Header("이름 입력")]
        public InputField name_input;

        [Header("외형 선택")]
        public Image   appearance_preview;
        public Sprite[] appearance_sprites;   // Inspector에서 스프라이트 배열 연결
        public Text    appearance_index_text; // "1 / 4"

        [Header("배경 카드")]
        public BackgroundCardUI[] background_cards; // Inspector에서 3개 연결

        private static readonly string[] BG_IDS    = { "farmer",   "merchant",  "chef"   };
        private static readonly string[] BG_TITLES = { "농부 출신", "상인 출신", "요리사 출신" };
        private static readonly string[] BG_DESCS  = {
            "씨앗 5종 + 물뿌리개",
            "골드 200 + 기본 씨앗 2종",
            "요리법 1개 해금 + 씨앗 2종 + 조리도구"
        };

        private int current_appearance = 0;

        void Start()
        {
            // 이름 기본값
            if (name_input != null)
                name_input.text = CharCreateData.player_name;

            // 외형 초기화
            current_appearance = CharCreateData.appearance_index;
            RefreshAppearance();

            // 배경 카드 초기화
            for (int i = 0; i < background_cards.Length && i < BG_IDS.Length; i++)
            {
                int idx = i; // 클로저 캡처용
                background_cards[i].Setup(BG_IDS[i], BG_TITLES[i], BG_DESCS[i], OnSelectBackground);
            }
            // 기본 선택 하이라이트
            RefreshBackgroundSelection();
        }

        // ── 외형 탐색 ────────────────────────────────────────────────────────────

        public void OnClickAppearancePrev()
        {
            if (appearance_sprites == null || appearance_sprites.Length == 0) return;
            current_appearance = (current_appearance - 1 + appearance_sprites.Length) % appearance_sprites.Length;
            RefreshAppearance();
        }

        public void OnClickAppearanceNext()
        {
            if (appearance_sprites == null || appearance_sprites.Length == 0) return;
            current_appearance = (current_appearance + 1) % appearance_sprites.Length;
            RefreshAppearance();
        }

        private void RefreshAppearance()
        {
            if (appearance_sprites != null && appearance_sprites.Length > 0 && appearance_preview != null)
                appearance_preview.sprite = appearance_sprites[current_appearance];

            if (appearance_index_text != null && appearance_sprites != null)
                appearance_index_text.text = (current_appearance + 1) + " / " + appearance_sprites.Length;
        }

        // ── 배경 선택 ────────────────────────────────────────────────────────────

        private void OnSelectBackground(string bgId)
        {
            CharCreateData.background_id = bgId;
            RefreshBackgroundSelection();
        }

        private void RefreshBackgroundSelection()
        {
            foreach (var card in background_cards)
                card.SetSelected(card.GetBackgroundId() == CharCreateData.background_id);
        }

        // ── 완료 ────────────────────────────────────────────────────────────────

        public void OnClickConfirm()
        {
            // CharCreateData 에 최신 값 반영
            CharCreateData.player_name      = (name_input != null && name_input.text.Length > 0)
                                              ? name_input.text : "농부";
            CharCreateData.appearance_index = current_appearance;

            // PlayerData 생성
            string slot = SaveManager.SlotNames[CharCreateData.selected_slot];
            PlayerData pdata = PlayerData.NewGame(slot);

            // 필드 주입
            pdata.player_name      = CharCreateData.player_name;
            pdata.appearance_index = CharCreateData.appearance_index;
            pdata.background_id    = CharCreateData.background_id;

            // 배경별 시작 아이템은 Scene_Farm_01의 onStartNewGame 이벤트에서 처리
            // (TheGame이 있어야 인벤토리 접근 가능하므로 여기서는 background_id만 기록)

            // 저장 후 씬 이동
            PlayerData.Save(slot, pdata);
            SaveSlotInfo.SaveFromPlayerData(slot, pdata);

            CharCreateData.Reset();
            SceneNav.GoTo("Scene_Farm_01");
        }

        public void OnClickBack()
        {
            SceneNav.GoTo("Scene_Intro");
        }
    }
}
