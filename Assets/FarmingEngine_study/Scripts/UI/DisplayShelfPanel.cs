using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 빵 진열대 전용 UI. ItemSlotPanel을 직접 상속하여
    /// StoragePanel의 panel_list 와 충돌하지 않는다.
    /// 진열 슬롯에 빵을 드래그·클릭으로 배치한다.
    /// </summary>
    public class DisplayShelfPanel : ItemSlotPanel
    {
        private static DisplayShelfPanel _instance;
        public static DisplayShelfPanel Get() => _instance;

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
            unfocus_when_out = true;
            onPressCancel += _ => Hide();
        }

        protected override void Update()
        {
            base.Update();

            PlayerControls controls = PlayerControls.Get();
            if (IsVisible() && controls.IsPressMenuCancel())
                Hide();

            // 너무 멀어지면 닫기
            if (IsVisible())
            {
                Selectable select = Selectable.GetByUID(inventory_uid);
                PlayerCharacter player = GetPlayer();
                if (player != null && select != null)
                {
                    float dist = (select.transform.position - player.transform.position).magnitude;
                    if (dist > select.GetUseRange(player) * 1.4f)
                        Hide();
                }
            }
        }

        public void ShowShelf(PlayerCharacter player, string uid, int max_slots)
        {
            if (string.IsNullOrEmpty(uid)) return;
            SetInventory(InventoryType.Storage, uid, max_slots);
            SetPlayer(player);
            RefreshPanel();
            Show();
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            SetInventory(InventoryType.Storage, "", 0);
            CancelSelection();
        }
    }
}
