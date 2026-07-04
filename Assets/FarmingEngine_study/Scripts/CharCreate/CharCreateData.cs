namespace FarmingEngine
{
    /// <summary>
    /// 캐릭터 생성 화면에서 Scene_Intro → Scene_CharCreate 사이
    /// 임시로 데이터를 보관하는 정적 컨테이너.
    /// CharCreateManager.OnClickConfirm()에서 PlayerData에 주입 후 초기화.
    /// </summary>
    public static class CharCreateData
    {
        public static int    selected_slot      = 0;       // SaveSlotPanel에서 선택한 슬롯 번호
        public static string player_name        = "농부";
        public static int    appearance_index   = 0;
        public static string background_id      = "farmer"; // "farmer" | "merchant" | "chef"

        public static void Reset()
        {
            player_name      = "농부";
            appearance_index = 0;
            background_id    = "farmer";
        }
    }
}
