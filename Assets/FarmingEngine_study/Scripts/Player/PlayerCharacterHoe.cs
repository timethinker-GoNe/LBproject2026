using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{

    /// <summary>
    /// Add to your player character for HOE feature
    /// </summary>

    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterHoe : MonoBehaviour
    {
        public GroupData hoe_item;
        public ConstructionData hoe_soil;
        public float hoe_range = 1f;
        public float hoe_build_radius = 0.5f;
        public int hoe_energy = 1;

        [Header("Indicator")]
        public Color indicator_valid_color = Color.white;
        public Color indicator_invalid_color = new Color(1f, 0.3f, 0.3f, 1f);
        public float indicator_width = 0.04f;
        public float indicator_size = 1f;

        private PlayerCharacter character;
        private Construction hoe_preview = null;
        private LineRenderer hoe_indicator = null;

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();
        }

        private void OnDestroy()
        {
            if (hoe_preview != null)
                Destroy(hoe_preview.gameObject);
            if (hoe_indicator != null)
                Destroy(hoe_indicator.gameObject);
        }

        private void Start()
        {

        }

        void FixedUpdate()
        {

        }

        private void Update()
        {
            UpdateHoePreview();

            PlayerControls control = PlayerControls.Get();
            if (control.IsPressAttack() && character.IsControlsEnabled())
            {
                Vector3 hoe_pos = hoe_preview != null
                    ? hoe_preview.transform.position
                    : character.GetInteractCenter() + character.GetFacing() * hoe_range;
                HoeGround(hoe_pos);
            }
        }

        private void UpdateHoePreview()
        {
            bool should_show = hoe_soil != null && HasHoeEquipped()
                && !character.IsBusy() && !character.IsDead()
                && !TheGame.Get().IsPaused();

            if (should_show && hoe_preview == null)
            {
                hoe_preview = Construction.CreateBuildMode(hoe_soil, character.transform.position);
                hoe_preview.GetBuildable().StartBuild(character);
            }
            else if (!should_show && hoe_preview != null)
            {
                Destroy(hoe_preview.gameObject);
                hoe_preview = null;
            }

            if (hoe_preview != null)
            {
                hoe_preview.GetBuildable().SetBuildPosition(GetHoeTargetPos());
                hoe_preview.GetBuildable().SetBuildVisible(false);
                bool valid = hoe_preview.GetBuildable().CheckIfCanBuild();
                UpdateIndicator(hoe_preview.transform.position, valid);
            }
            else
            {
                HideIndicator();
            }
        }

        private void UpdateIndicator(Vector3 pos, bool valid)
        {
            if (hoe_indicator == null)
            {
                GameObject go = new GameObject("HoeIndicator");
                hoe_indicator = go.AddComponent<LineRenderer>();
                hoe_indicator.loop = true;
                hoe_indicator.positionCount = 4;
                hoe_indicator.widthMultiplier = indicator_width;
                hoe_indicator.useWorldSpace = true;
                hoe_indicator.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                hoe_indicator.receiveShadows = false;
                hoe_indicator.material = new Material(Shader.Find("Sprites/Default"));
            }

            hoe_indicator.widthMultiplier = indicator_width;
            float half = indicator_size * 0.5f;
            float y = pos.y + 0.02f;
            hoe_indicator.SetPositions(new Vector3[] {
                new Vector3(pos.x - half, y, pos.z - half),
                new Vector3(pos.x + half, y, pos.z - half),
                new Vector3(pos.x + half, y, pos.z + half),
                new Vector3(pos.x - half, y, pos.z + half),
            });
            Color c = valid ? indicator_valid_color : indicator_invalid_color;
            hoe_indicator.startColor = c;
            hoe_indicator.endColor = c;
            hoe_indicator.enabled = true;
        }

        private void HideIndicator()
        {
            if (hoe_indicator != null)
                hoe_indicator.enabled = false;
        }

        private Vector3 GetHoeTargetPos()
        {
            Vector3 mouse_world = PlayerControlsMouse.Get().GetPointingPos();
            Vector3 char_pos = character.transform.position;
            Vector3 dir = mouse_world - char_pos;
            dir.y = 0f;
            dir = dir.magnitude > 0.1f ? dir.normalized * hoe_range : character.GetFacing() * hoe_range;

            Vector3 target = char_pos + dir;
            target.y = GetFloorY(target);
            return target;
        }

        private float GetFloorY(Vector3 pos)
        {
            LayerMask floor_mask = hoe_preview != null ? hoe_preview.GetBuildable().floor_layer : (1 << 9);
            RaycastHit[] hits = Physics.RaycastAll(
                new Vector3(pos.x, pos.y + 20f, pos.z),
                Vector3.down, 30f, floor_mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(character.transform) ||
                    hit.collider.transform == character.transform)
                    continue;
                return hit.point.y;
            }
            return 0f;
        }

        public void TriggerHoe()
        {
            Vector3 hoe_pos = hoe_preview != null
                ? hoe_preview.transform.position
                : character.GetInteractCenter() + character.GetFacing() * hoe_range;
            HoeGround(hoe_pos);
        }

        private bool HasHoeEquipped()
        {
            InventoryItemData ivdata = character.EquipData.GetEquippedItem(EquipSlot.Hand);
            ItemData idata = ItemData.Get(ivdata?.item_id);
            return idata != null && idata.HasGroup(hoe_item);
        }

        public void HoeGround(Vector3 pos)
        {
            if (!CanHoe())
                return;

            character.StopMove();
            character.Attributes.AddAttribute(AttributeType.Energy, -hoe_energy);

            character.TriggerAnim(character.Animation ? character.Animation.hoe_anim : "", pos);
            character.TriggerBusy(0.8f, () =>
            {
                Construction prev = Construction.GetNearest(pos, hoe_build_radius);
                Plant plant = Plant.GetNearest(pos, hoe_build_radius);
                if (prev != null && plant == null && prev.data == hoe_soil)
                {
                    prev.Destroy(); //Destroy previous, if no plant on it
                    return;
                }

                Construction construct = Construction.CreateBuildMode(hoe_soil, pos);
                construct.GetBuildable().StartBuild(character);
                construct.GetBuildable().SetBuildPositionTemporary(pos);
                if (construct.GetBuildable().CheckIfCanBuild())
                {
                    construct.GetBuildable().FinishBuild();
                    FarmingEvents.onTilledSoil?.Invoke(character);
                }
                else
                {
                    Destroy(construct.gameObject);
                }
            });

        }

        public bool CanHoe()
        {
            bool has_energy = character.Attributes.GetAttributeValue(AttributeType.Energy) >= hoe_energy;
            InventoryItemData ivdata = character.EquipData.GetEquippedItem(EquipSlot.Hand);
            ItemData idata = ItemData.Get(ivdata?.item_id);
            return has_energy && idata != null && idata.HasGroup(hoe_item) && !character.IsBusy();
        }

        public void HoeGroundAuto(Vector3 pos)
        {
            Vector3 dir = pos - transform.position;
            if (character.IsBusy() || character.Crafting.ClickedBuild() || dir.magnitude > hoe_range
                || character.GetAutoSelectTarget() != null || character.GetAutoDropInventory() != null)
                return;

            InventoryItemData ivdata = character.EquipData.GetEquippedItem(EquipSlot.Hand);
            if (ivdata != null && CanHoe())
            {
                HoeGround(pos);

                if (ivdata != null)
                    ivdata.durability -= 1;
            }
        }
    }

}