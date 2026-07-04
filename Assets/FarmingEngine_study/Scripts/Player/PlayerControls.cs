using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace FarmingEngine
{

    /// <summary>
    /// Keyboard controls manager
    /// </summary>

    public class PlayerControls : MonoBehaviour
    {
        public int player_id = 0;

        [Header("Actions")]
        public KeyCode action_key = KeyCode.Space;
        public KeyCode attack_key = KeyCode.LeftShift;
        public KeyCode jump_key = KeyCode.LeftControl;
        public KeyCode run_key = KeyCode.LeftShift;
        public KeyCode interact_key = KeyCode.Z;
        public KeyCode buildable_rotate_left = KeyCode.Q;
        public KeyCode buildable_rotate_right = KeyCode.E;

        [Header("Camera")]
        public KeyCode cam_rotate_left = KeyCode.Q;
        public KeyCode cam_rotate_right = KeyCode.E;

        [Header("UI")]
        public KeyCode craft_key = KeyCode.C;
        public KeyCode inventory_key = KeyCode.I;
        public KeyCode ui_select = KeyCode.Return;
        public KeyCode ui_use = KeyCode.RightShift;
        public KeyCode ui_cancel = KeyCode.Backspace;

        [Header("Menu")]
        public KeyCode menu_accept = KeyCode.Return;
        public KeyCode menu_cancel = KeyCode.Backspace;
        public KeyCode menu_pause = KeyCode.Escape;

        [Header(" ---- Gamepad Mode ---- ")]
        public bool gamepad_controls = false; //In gamepad mode, anything that can usually be done with the mouse will be replaced by keyboard/gamepad controls, 
                                              //for example the build system will place building differently

        public delegate Vector2 MoveAction();
        public delegate bool PressAction();

        [HideInInspector]
        public bool gamepad_linked = false;
        public MoveAction gamepad_move;
        public MoveAction gamepad_freelook;
        public MoveAction gamepad_menu;
        public MoveAction gamepad_dpad;
        public MoveAction gamepad_camera; //Triggers
        public PressAction gamepad_pause; //Start
        public PressAction gamepad_action; //A
        public PressAction gamepad_attack; //X or R1
        public PressAction gamepad_jump; //Y
        public PressAction gamepad_craft; //L1
        public PressAction gamepad_use; //X
        public PressAction gamepad_accept; //A
        public PressAction gamepad_cancel; //B
        public PressAction gamepad_run;  // Left Shift
        public PressAction gamepad_buildable_rotate_left;   // Q
        public PressAction gamepad_buildable_rotate_right;  // E
        public PressAction gamepad_interact; // Z
        public System.Action gamepad_update;

        private Vector2 move;
        private Vector2 freelook;
        private Vector2 menu_move;
        private Vector2 ui_move;
        private bool menu_moved;
        private bool ui_moved;
        private float rotate_cam;

        private bool press_run;
        private bool press_action;
        private bool press_attack;
        private bool press_jump;
        private bool press_craft;
        private bool press_inventory;
        private bool press_interact;
        private bool press_buildable_rotate_left;
        private bool press_buildable_rotate_right;

        private bool press_accept;
        private bool press_cancel;
        private bool press_pause;
        private bool press_ui_select;
        private bool press_ui_use;
        private bool press_ui_cancel;

        private PlayerCharacter player;
        private static PlayerControls control_first = null;
        private static List<PlayerControls> controls = new List<PlayerControls>();

        void Awake()
        {
            controls.Add(this);

            if (control_first == null || player_id < control_first.player_id)
                control_first = this;

            if (TheGame.IsMobile())
                gamepad_controls = false; //No gamepad on mobile

            LoadKeyBindings();
        }

        public void LoadKeyBindings()
        {
            action_key    = LoadKey("kb_action",    action_key);
            attack_key    = LoadKey("kb_attack",    attack_key);
            run_key       = attack_key; // 항상 attack_key와 동일하게 유지
            jump_key      = LoadKey("kb_jump",      jump_key);
            interact_key  = LoadKey("kb_interact",  interact_key);
            craft_key     = LoadKey("kb_craft",     craft_key);
            inventory_key = LoadKey("kb_inventory", inventory_key);
        }

        public static void SaveKey(string id, KeyCode key)
        {
            PlayerPrefs.SetInt(id, (int)key);
            PlayerPrefs.Save();
        }

        public static KeyCode LoadKey(string id, KeyCode defaultKey)
        {
            return (KeyCode)PlayerPrefs.GetInt(id, (int)defaultKey);
        }

        private void OnDestroy()
        {
            controls.Remove(this);
        }

        void Update()
        {
            move = Vector3.zero;
            freelook = Vector2.zero;
            menu_move = Vector2.zero;
            ui_move = Vector2.zero;
            rotate_cam = 0f;
            press_action = false;
            press_attack = false;
            press_jump = false;
            press_craft = false;
            press_inventory = false;
            press_interact = false;

            press_accept = false;
            press_cancel = false;
            press_pause = false;
            press_ui_select = false;
            press_ui_use = false;
            press_ui_cancel = false;

            press_buildable_rotate_left = false;
            press_buildable_rotate_right = false;

            Vector2 wasd = Vector2.zero;
            if (Input.GetKey(KeyCode.A))
                wasd += Vector2.left;
            if (Input.GetKey(KeyCode.D))
                wasd += Vector2.right;
            if (Input.GetKey(KeyCode.W))
                wasd += Vector2.up;
            if (Input.GetKey(KeyCode.S))
                wasd += Vector2.down;

            Vector2 arrows = Vector2.zero;
            if (Input.GetKey(KeyCode.LeftArrow))
                arrows += Vector2.left;
            if (Input.GetKey(KeyCode.RightArrow))
                arrows += Vector2.right;
            if (Input.GetKey(KeyCode.UpArrow))
                arrows += Vector2.up;
            if (Input.GetKey(KeyCode.DownArrow))
                arrows += Vector2.down;

            if (player != null && player.Crafting != null && !player.Crafting.IsBuildMode())
            {
                if (Input.GetKey(cam_rotate_left))
                    rotate_cam += -1f;
                if (Input.GetKey(cam_rotate_right))
                    rotate_cam += 1f;
            }

            if (Input.GetKeyDown(action_key))
                press_action = true;
            if (Input.GetKeyDown(attack_key))
                press_attack = true;
            if (Input.GetKeyDown(jump_key))
                press_jump = true;
            if (Input.GetKeyDown(craft_key))
                press_craft = true;
            if (Input.GetKeyDown(inventory_key))
                press_inventory = true;
            if (Input.GetKeyDown(run_key))
                press_run = true;
            if (Input.GetKeyUp(run_key))
                press_run = false;
            if (Input.GetKeyDown(interact_key))
                press_interact = true;
            if (Input.GetKeyDown(buildable_rotate_left))
                press_buildable_rotate_left = true;
            if (Input.GetKeyDown(buildable_rotate_right))
                press_buildable_rotate_right = true;

            if (Input.GetKeyDown(menu_accept))
                press_accept = true;
            if (Input.GetKeyDown(menu_cancel))
                press_cancel = true;
            if (Input.GetKeyDown(menu_pause))
                press_pause = true;

            if (Input.GetKeyDown(ui_select))
                press_ui_select = true;
            if (Input.GetKeyDown(ui_use))
                press_ui_use = true;
            if (Input.GetKeyDown(ui_cancel))
                press_ui_cancel = true;

            Vector2 both = (arrows + wasd);
            move = wasd;
            if (gamepad_controls)
                freelook = arrows;

            //Menu / UI
            if (!menu_moved && both.magnitude > 0.5f)
            {
                menu_move = both;
                menu_moved = true;
            }

            if (both.magnitude < 0.5f)
                menu_moved = false;

            if (!ui_moved && arrows.magnitude > 0.5f)
            {
                ui_move = arrows;
                ui_moved = true;
            }

            if (arrows.magnitude < 0.5f)
                ui_moved = false;

            //Gamepad
            if (gamepad_linked && gamepad_controls) {

                move += gamepad_move.Invoke();
                freelook += gamepad_freelook.Invoke();
                rotate_cam += gamepad_camera.Invoke().x;
                ui_move += gamepad_dpad.Invoke();
                menu_move += gamepad_menu.Invoke();
                menu_move += gamepad_dpad.Invoke();

                press_action = press_action || gamepad_action.Invoke();
                press_attack = press_attack || gamepad_attack.Invoke();
                press_jump = press_jump || gamepad_jump.Invoke();
                press_run = press_run || gamepad_run.Invoke();
                press_interact = press_interact || gamepad_interact.Invoke();
                press_buildable_rotate_left = press_buildable_rotate_left || gamepad_buildable_rotate_left.Invoke();
                press_buildable_rotate_right = press_buildable_rotate_right || gamepad_buildable_rotate_right.Invoke();

                press_craft = press_craft || gamepad_craft.Invoke();
                press_accept = press_accept || gamepad_accept.Invoke();
                press_cancel = press_cancel || gamepad_cancel.Invoke();
                press_pause = press_pause || gamepad_pause.Invoke();
                press_ui_select = press_ui_select || gamepad_accept.Invoke();
                press_ui_use = press_ui_use || gamepad_use.Invoke();
                press_ui_cancel = press_ui_cancel || gamepad_cancel.Invoke();

                gamepad_update?.Invoke();
            }

            move = move.normalized * Mathf.Min(move.magnitude, 1f);
            freelook = freelook.normalized * Mathf.Min(freelook.magnitude, 1f);

            // 풀 인벤토리 열려있으면 이동·전투 입력 차단 (키보드 내비게이션 전용)
            if (FullInventoryPanel.IsAnyVisible())
            {
                move          = Vector2.zero;
                freelook      = Vector2.zero;
                press_action  = false;
                press_attack  = false;
                press_jump    = false;
                press_interact = false;
                // press_inventory 는 유지 — PlayerUI.Toggle() 이 닫기에 사용
            }
        }

        public Vector2 GetMove() { return move; }
        public Vector2 GetFreelook() { return freelook; }
        public bool IsMoving() { return move.magnitude > 0.1f; }
        public float GetRotateCam() { return rotate_cam; }

        public bool IsPressAttack() { return press_attack; }
        public bool IsPressAction() { return press_action; }
        public bool IsPressJump() { return press_jump; }
        public bool IsPressCraft() { return press_craft; }
        public bool IsPressInventory() { return press_inventory; }
        public bool isPressRun() { return press_run; }
        public bool isPressBuildRotateLeft() { return press_buildable_rotate_left; }
        public bool isPressBuildRotateRight() { return press_buildable_rotate_right; }
        public bool isPressInteract() { return press_interact; }

        public Vector2 GetUIMove() { return ui_move; }
        public Vector2 GetMenuMove() { return menu_move; }

        public bool IsPressMenuAccept() { return press_accept; }
        public bool IsPressMenuCancel() { return press_cancel; }
        public bool IsPressPause() { return press_pause; }
        public bool IsPressUISelect() { return press_ui_select; }
        public bool IsPressUIUse() { return press_ui_use; }
        public bool IsPressUICancel() { return press_ui_cancel; }

        public bool IsUIPressAny() { return ui_move.magnitude > 0.5f; }
        public bool IsUIPressLeft() { return ui_move.x < -0.5f; }
        public bool IsUIPressRight() { return ui_move.x > 0.5f; }
        public bool IsUIPressUp() { return ui_move.y > 0.5f; }
        public bool IsUIPressDown() { return ui_move.y < -0.5f; }

        public bool IsMenuPressLeft() { return menu_move.x < -0.5f; }
        public bool IsMenuPressRight() { return menu_move.x > 0.5f; }
        public bool IsMenuPressUp() { return menu_move.y > 0.5f; }
        public bool IsMenuPressDown() { return menu_move.y < -0.5f; }

        public bool IsPressedByName(string name)
        {
            return Input.GetKeyDown(name);
        }

        public bool IsGamePad()
        {
            return gamepad_controls;
        }
        public void SetPlayer(PlayerCharacter player)
        {
            this.player = player;
        }

        public static bool IsAnyGamePad()
        {
            foreach (PlayerControls control in controls)
            {
                if (control.IsGamePad())
                    return true;
            }
            return false;
        }

        public static PlayerControls Get(int player_id = 0)
        {
            foreach (PlayerControls control in controls)
            {
                if (control.player_id == player_id)
                    return control;
            }
            return null;
        }

        public static PlayerControls GetFirst()
        {
            return control_first;
        }

        public static List<PlayerControls> GetAll()
        {
            return controls;
        }
    }
}