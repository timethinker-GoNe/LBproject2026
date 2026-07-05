using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using FarmingEngine;
using DialogueQuests;

namespace FarmingEngine.SceneTools
{
    /// <summary>
    /// 1차 플레이어블 샘플 씬 자동 생성 도구.
    /// 메뉴: FarmingEngine > Build Scenes > Build All Scenes
    /// </summary>
    public static class SceneBuilder
    {
        private const string SCENES_PATH = "Assets/Scenes/Game";

        // ── 프리팹 GUIDs ──
        private const string GUID_MANAGERS       = "fd394a7c438f4b769def2b51097f42a1";
        private const string GUID_THE_CAMERA     = "56f93d781ca7499d8f8c8d40c0990ab8";
        private const string GUID_PLAYER_CHAR    = "94ba7609da314cbd94b8d5881f8696cc";
        private const string GUID_UI_CANVAS      = "2326e0f7e2ae4c2b854cda1e5f382174";
        private const string GUID_DQ_MANAGER     = "2d6a2278e87d77c47b3d2b7b2842a3c6";
        private const string GUID_DQ_UI_CANVAS   = "cfe499bb5caab3b489f054c2e0b5000f";

        // ── 월드 오브젝트 프리팹 GUIDs ──
        private const string GUID_SOIL           = "edc4d2ec5098a4f4c9e3f64a25ff3170";
        private const string GUID_WELL           = "09c456283dc90a84f8fcc88867aff074";
        private const string GUID_CHEST_WOOD     = "9f02664e2b124b7fa041e988c75c323b";
        private const string GUID_NPC_GIRL       = "63a88fef67c79074990902514ccec79e";
        private const string GUID_EXIT_ZONE      = "0e5f3c5030af4e7ebbf0db6a89c2f12e";
        private const string GUID_NAVMESH        = "49af39be596740e0aceb5b29e322d6d1";

        // ── 스크립트 GUIDs ──
        private const string GUID_START_SCREEN_MGR  = "3ca8c57667381244294c5903f9467b38";
        private const string GUID_SETTINGS_PANEL     = "50f3284aa36d6394c8204b152921107d";
        private const string GUID_SAVE_SLOT_PANEL    = "994fcfcf762dc1448a7360dfbf0b9f7f";
        private const string GUID_CONFIRM_PANEL      = "1ede6780bad74ee4e8b2b99327362675";
        private const string GUID_SAVE_SLOT_UI       = "84aafbf2504daf1419a6db6e563163de";
        private const string GUID_SAVE_MGR           = "2c056ccd382b3fc4b902fab60e3da50a";
        private const string GUID_INTRO_MGR          = "d59e23b1cc0e1f4429f0a5bb6f407ddc";
        private const string GUID_CHAR_CREATE_MGR    = "02e8f1f81b4f9d64eba47764cb7e33c9";
        private const string GUID_BACKGROUND_CARD    = "d0e27efec5ecbbd4fa666e87724ae435";

        // ════════════════════════════════════════════════════════════════════════
        // Patch — 기존 씬의 특정 그룹만 교체 (수동 수정 내역 유지)
        // ════════════════════════════════════════════════════════════════════════

        [MenuItem("Farming Engine/Patch Scene/Farm01 - [Quests] 재빌드")]
        public static void PatchFarm01Quests()
        {
            if (!EnsureScene("Scene_Farm_01")) return;
            ReplaceGroup("[Quests]", BuildQuestsGroup_Farm01);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[SceneBuilder] Patch 완료: Scene_Farm_01 [Quests]");
        }

        [MenuItem("Farming Engine/Patch Scene/Farm01 - [Characters] 재빌드")]
        public static void PatchFarm01Characters()
        {
            if (!EnsureScene("Scene_Farm_01")) return;
            ReplaceGroup("[Characters]", BuildCharactersGroup_Farm01);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[SceneBuilder] Patch 완료: Scene_Farm_01 [Characters]");
        }

        [MenuItem("Farming Engine/Patch Scene/Farm01 - [Managers] 재빌드")]
        public static void PatchFarm01Managers()
        {
            if (!EnsureScene("Scene_Farm_01")) return;
            ReplaceGroup("[Managers]", BuildManagersGroup_Farm01);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[SceneBuilder] Patch 완료: Scene_Farm_01 [Managers]");
        }

        // ── Patch 헬퍼 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 지정 씬이 이미 열려 있으면 그대로 사용. 다른 씬이 열려 있으면 OpenExistingScene으로 교체.
        /// 이미 열린 씬에서는 강제 재로드하지 않아 다른 패치의 변경사항이 유실되지 않는다.
        /// </summary>
        private static bool EnsureScene(string sceneName)
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.name == sceneName) return true;
            return OpenExistingScene(sceneName);
        }

        /// <summary>씬 파일에서 씬 열기. 현재 씬과 다를 때만 호출. 없으면 false 반환.</summary>
        private static bool OpenExistingScene(string sceneName)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            string path = SCENES_PATH + "/" + sceneName + ".unity";
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("씬 없음",
                    $"{path} 을 찾을 수 없습니다.\n먼저 Build All Scenes를 실행하세요.", "확인");
                return false;
            }
            EditorSceneManager.OpenScene(path);
            return true;
        }

        /// <summary>씬에서 그룹 오브젝트를 찾아 삭제 후 재생성.</summary>
        private static void ReplaceGroup(string groupName, System.Action<GameObject> buildAction)
        {
            // 기존 그룹 삭제
            var existing = GameObject.Find(groupName);
            if (existing != null) Object.DestroyImmediate(existing);

            // 새 그룹 생성
            var newGroup = new GameObject(groupName);
            buildAction(newGroup);
        }

        // ── 그룹별 빌드 함수 ─────────────────────────────────────────────────────

        private static void BuildQuestsGroup_Farm01(GameObject questGroup)
        {
            // 구 NarrativeEvent 방식 이벤트 오브젝트 제거됨.
            // 스토리이벤트는 StoryEventManager + story_events.json 으로 관리.
            // 이 그룹은 향후 퀘스트 관련 씬 오브젝트(구역 트리거 등) 배치용으로 예약.
        }

        private static void BuildCharactersGroup_Farm01(GameObject charGroup)
        {
            int defaultLayer = LayerMask.NameToLayer("Default");

            var playerGO = InstantiatePrefab(GUID_PLAYER_CHAR, "PlayerCharacter", Vector3.zero);
            if (playerGO != null)
            {
                playerGO.transform.SetParent(charGroup.transform);
                SetLayerRecursively(playerGO, defaultLayer);
                AttachActor(playerGO, FindOrCreateActorData("Actor_Player", "player", isPlayer: true));
            }
            var npcGO = InstantiatePrefab(GUID_NPC_GIRL, "NPC_Grandma", new Vector3(2f, 0f, 3f));
            if (npcGO != null)
            {
                npcGO.transform.SetParent(charGroup.transform);
                SetLayerRecursively(npcGO, defaultLayer);
                AttachActor(npcGO, FindOrCreateActorData("Actor_Grandma", "grandma", isPlayer: false));
            }
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static void BuildManagersGroup_Farm01(GameObject mgrGroup)
        {
            var managersGO = InstantiatePrefab(GUID_MANAGERS, "Managers", Vector3.zero);
            if (managersGO != null) managersGO.transform.SetParent(mgrGroup.transform);

            var saveMgrGO = new GameObject("SaveManager");
            saveMgrGO.transform.SetParent(mgrGroup.transform);
            AddScriptComponent<SaveManager>(saveMgrGO, GUID_SAVE_MGR);

            // DQManager — NarrativeManager + DialogueQuests 핵심 컴포넌트 포함 프리팹
            var dqMgrGO = InstantiatePrefab(GUID_DQ_MANAGER, "DQManager", Vector3.zero);
            if (dqMgrGO != null) dqMgrGO.transform.SetParent(mgrGroup.transform);

            // QuestManager — 퀘스트 진행 추적 (FarmingQuest)
            var questMgrGO = new GameObject("QuestManager");
            questMgrGO.transform.SetParent(mgrGroup.transform);
            questMgrGO.AddComponent<FarmingQuest.QuestManager>();

            // StoryEventManager — NPC 클릭 트리거·조건 평가 (story_events.json)
            var semGO = new GameObject("StoryEventManager");
            semGO.transform.SetParent(mgrGroup.transform);
            semGO.AddComponent<StoryEventManager>();
        }

        // ────────────────────────────────────────────────────────────────────────
        [MenuItem("Farming Engine/Build Scenes/Build All Scenes")]
        public static void BuildAllScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!Directory.Exists(SCENES_PATH))
                Directory.CreateDirectory(SCENES_PATH);

            BuildSceneStart();
            BuildSceneIntro();
            BuildSceneCharCreate();
            BuildSceneFarm01();

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("씬 생성 완료",
                "4개 씬이 생성되었습니다.\n\nAssets/Scenes/Game/\n  Scene_Start.unity\n  Scene_Intro.unity\n  Scene_CharCreate.unity\n  Scene_Farm_01.unity\n\n각 씬을 열어 컴포넌트 참조(슬롯 배열 등)를 Inspector에서 연결해 주세요.",
                "확인");
        }

        // ════════════════════════════════════════════════════════════════════════
        // Scene_Start  — 타이틀 스크린
        // ════════════════════════════════════════════════════════════════════════
        [MenuItem("Farming Engine/Build Scenes/Scene_Start Only")]
        public static void BuildSceneStart()
        {
            var scene = NewScene("Scene_Start");

            AddUICamera();
            AddEventSystem();
            AddDirectionalLight();

            // ── 배경 캔버스 (검정 배경) ──
            var bgCanvas = CreateUICanvas("BackgroundCanvas", 0);
            var bgPanel  = AddUIImage(bgCanvas, "Background", Color.black,
                           new Vector2(0,0), new Vector2(1,1));

            var titleImg = AddUIImage(bgCanvas, "TitleLogo", new Color(1f,0.85f,0.4f,1f),
                           new Vector2(0.3f,0.65f), new Vector2(0.7f,0.9f));
            AddUIText(titleImg, "TitleText", "골든크로프트", 48, Color.white, TextAnchor.MiddleCenter);

            // ── 메인 메뉴 캔버스 ──
            var menuCanvas = CreateUICanvas("MenuCanvas", 1);

            var menuRoot = CreatePanel(menuCanvas, "MenuPanel",
                           new Vector2(0.35f,0.2f), new Vector2(0.65f,0.65f));

            var btnNewGame  = CreateButton(menuRoot, "Btn_NewGame",  "새 게임",  new Vector2(0,0.75f), new Vector2(1,1f));
            var btnLoad     = CreateButton(menuRoot, "Btn_LoadGame", "불러오기", new Vector2(0,0.5f),  new Vector2(1,0.74f));
            var btnSettings = CreateButton(menuRoot, "Btn_Settings", "설정",     new Vector2(0,0.25f), new Vector2(1,0.49f));
            var btnQuit     = CreateButton(menuRoot, "Btn_Quit",     "나가기",   new Vector2(0,0f),    new Vector2(1,0.24f));

            // ── SaveSlotPanel ──
            var slotPanelGO = CreatePanelGO(menuCanvas, "SaveSlotPanel", GUID_SAVE_SLOT_PANEL,
                              new Vector2(0.2f,0.1f), new Vector2(0.8f,0.9f));
            var titleTxt    = AddUIText(slotPanelGO, "Title", "슬롯 선택", 28, Color.white, TextAnchor.UpperCenter,
                              new Vector2(0.1f,0.88f), new Vector2(0.9f,1f));
            var slotContainer = CreateEmptyRect(slotPanelGO, "SlotContainer",
                                new Vector2(0,0.15f), new Vector2(1,0.88f));
            var slot1 = CreateSlotUI(slotContainer, "SlotUI_0", 0);
            var slot2 = CreateSlotUI(slotContainer, "SlotUI_1", 1);
            var slot3 = CreateSlotUI(slotContainer, "SlotUI_2", 2);
            var slotCloseBtn = CreateButton(slotPanelGO, "Btn_Close", "← 뒤로",
                               new Vector2(0.05f,0.02f), new Vector2(0.35f,0.12f));
            HidePanelInEditor(slotPanelGO);

            // ── ConfirmPanel ──
            var confirmGO = CreatePanelGO(menuCanvas, "ConfirmPanel", GUID_CONFIRM_PANEL,
                            new Vector2(0.3f,0.35f), new Vector2(0.7f,0.65f));
            var msgTxt    = AddUIText(confirmGO, "MessageText", "확인하시겠습니까?", 22, Color.white, TextAnchor.MiddleCenter);
            var btnYes    = CreateButton(confirmGO, "Btn_Confirm", "예",  new Vector2(0.05f,0.1f), new Vector2(0.45f,0.45f));
            var btnNo     = CreateButton(confirmGO, "Btn_Cancel",  "아니오", new Vector2(0.55f,0.1f), new Vector2(0.95f,0.45f));
            HidePanelInEditor(confirmGO);

            // ── SettingsPanel ──
            var settingsGO = CreatePanelGO(menuCanvas, "SettingsPanel", GUID_SETTINGS_PANEL,
                             new Vector2(0.15f,0.05f), new Vector2(0.85f,0.95f));
            HidePanelInEditor(settingsGO);
            BuildSettingsPanel(settingsGO);

            // ── StartScreenManager 오브젝트 ──
            var mgrGO = new GameObject("StartScreenManager");
            var mgr   = AddScriptComponent<StartScreenManager>(mgrGO, GUID_START_SCREEN_MGR);

            // Inspector 연결 (컴포넌트 참조)
            if (mgr != null)
            {
                SerializedObject so = new SerializedObject(mgr);
                so.FindProperty("new_game_button") .objectReferenceValue = btnNewGame .GetComponent<Button>();
                so.FindProperty("load_game_button").objectReferenceValue = btnLoad    .GetComponent<Button>();
                so.FindProperty("settings_button") .objectReferenceValue = btnSettings.GetComponent<Button>();
                so.FindProperty("quit_button")     .objectReferenceValue = btnQuit    .GetComponent<Button>();
                so.FindProperty("save_slot_panel") .objectReferenceValue = slotPanelGO.GetComponent<SaveSlotPanel>();
                so.FindProperty("confirm_panel")   .objectReferenceValue = confirmGO  .GetComponent<ConfirmPanel>();
                so.FindProperty("settings_panel")  .objectReferenceValue = settingsGO .GetComponent<SettingsPanel>();
                so.ApplyModifiedProperties();
            }

            // 버튼 이벤트 연결
            if (mgr != null)
            {
                AddButtonOnClick(btnNewGame,  mgr, "OnClickNewGame");
                AddButtonOnClick(btnLoad,     mgr, "OnClickLoadGame");
                AddButtonOnClick(btnSettings, mgr, "OnClickSettings");
                AddButtonOnClick(btnQuit,     mgr, "OnClickQuit");
            }

            // ConfirmPanel 연결
            var confirmMono = confirmGO.GetComponent<ConfirmPanel>();
            if (confirmMono != null)
            {
                SerializedObject cso = new SerializedObject(confirmMono);
                cso.FindProperty("message_text")   .objectReferenceValue = msgTxt;
                cso.FindProperty("confirm_button") .objectReferenceValue = btnYes.GetComponent<Button>();
                cso.FindProperty("cancel_button")  .objectReferenceValue = btnNo .GetComponent<Button>();
                cso.ApplyModifiedProperties();
            }

            // SaveSlotPanel 연결
            var slotPanelMono = slotPanelGO.GetComponent<SaveSlotPanel>();
            if (slotPanelMono != null)
            {
                SerializedObject pso = new SerializedObject(slotPanelMono);
                pso.FindProperty("title_text")  .objectReferenceValue = titleTxt;
                pso.FindProperty("close_button").objectReferenceValue = slotCloseBtn.GetComponent<Button>();
                var slotArr = pso.FindProperty("slot_uis");
                slotArr.arraySize = 3;
                slotArr.GetArrayElementAtIndex(0).objectReferenceValue = slot1.GetComponent<SaveSlotUI>();
                slotArr.GetArrayElementAtIndex(1).objectReferenceValue = slot2.GetComponent<SaveSlotUI>();
                slotArr.GetArrayElementAtIndex(2).objectReferenceValue = slot3.GetComponent<SaveSlotUI>();
                pso.ApplyModifiedProperties();

                AddButtonOnClick(slotCloseBtn, slotPanelMono, "OnClickClose");
            }

            SaveScene(scene, "Scene_Start");
        }

        // ════════════════════════════════════════════════════════════════════════
        // Scene_Intro  — 세계관 인트로
        // ════════════════════════════════════════════════════════════════════════
        [MenuItem("Farming Engine/Build Scenes/Scene_Intro Only")]
        public static void BuildSceneIntro()
        {
            var scene = NewScene("Scene_Intro");
            AddUICamera();
            AddEventSystem();
            AddDirectionalLight();

            var canvas = CreateUICanvas("IntroCanvas", 0);

            var bg       = AddUIImage(canvas,   "Background",    Color.black,              new Vector2(0,0),    new Vector2(1,1));
            var textArea = CreateEmptyRect(canvas, "TextArea",                              new Vector2(0.05f,0.1f), new Vector2(0.95f,0.7f));
            var storyTxt = AddUIText(textArea,  "StoryText",     "",  22,  new Color(0.9f,0.9f,0.9f,1f), TextAnchor.UpperLeft);
            storyTxt.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Wrap;

            var pageCnt  = AddUIText(canvas,    "PageCounter",   "1 / 4", 18, Color.gray, TextAnchor.MiddleRight,
                           new Vector2(0.7f,0.88f), new Vector2(0.95f,0.95f));
            var nextBtn  = CreateButton(canvas,  "Btn_Next",      "다음 ▶", new Vector2(0.75f,0.02f), new Vector2(0.95f,0.1f));
            var skipBtn  = CreateButton(canvas,  "Btn_Skip",      "건너뛰기", new Vector2(0.05f,0.02f), new Vector2(0.25f,0.1f));

            var mgrGO = new GameObject("IntroManager");
            var mgr   = AddScriptComponent<IntroManager>(mgrGO, GUID_INTRO_MGR);
            if (mgr != null)
            {
                SerializedObject so = new SerializedObject(mgr);
                so.FindProperty("story_text")   .objectReferenceValue = storyTxt.GetComponent<Text>();
                so.FindProperty("page_counter") .objectReferenceValue = pageCnt.GetComponent<Text>();
                so.FindProperty("next_button")  .objectReferenceValue = nextBtn.GetComponent<Button>();
                so.FindProperty("skip_button")  .objectReferenceValue = skipBtn.GetComponent<Button>();
                so.ApplyModifiedProperties();

                AddButtonOnClick(nextBtn, mgr, "OnClickNext");
                AddButtonOnClick(skipBtn, mgr, "OnClickSkip");
            }

            SaveScene(scene, "Scene_Intro");
        }

        // ════════════════════════════════════════════════════════════════════════
        // Scene_CharCreate  — 캐릭터 생성
        // ════════════════════════════════════════════════════════════════════════
        [MenuItem("Farming Engine/Build Scenes/Scene_CharCreate Only")]
        public static void BuildSceneCharCreate()
        {
            var scene = NewScene("Scene_CharCreate");
            AddUICamera();
            AddEventSystem();
            AddDirectionalLight();

            var canvas = CreateUICanvas("CharCreateCanvas", 0);
            AddUIImage(canvas, "Background", new Color(0.1f,0.08f,0.06f,1f), new Vector2(0,0), new Vector2(1,1));
            AddUIText(canvas, "Title", "캐릭터 생성", 36, Color.white, TextAnchor.UpperCenter,
                      new Vector2(0.2f,0.88f), new Vector2(0.8f,0.98f));

            // ── 이름 입력 ──
            var nameLabel  = AddUIText(canvas, "Label_Name", "캐릭터 이름", 20, Color.white, TextAnchor.MiddleLeft,
                             new Vector2(0.05f,0.75f), new Vector2(0.3f,0.83f));
            var nameInputGO = CreateInputField(canvas, "NameInput", "농부", new Vector2(0.3f,0.75f), new Vector2(0.7f,0.83f));

            // ── 외형 선택 ──
            AddUIText(canvas, "Label_Appearance", "외형 선택", 20, Color.white, TextAnchor.MiddleLeft,
                      new Vector2(0.05f,0.6f), new Vector2(0.3f,0.68f));
            var prevBtn     = CreateButton(canvas, "Btn_AppearPrev", "◀", new Vector2(0.3f,0.55f),  new Vector2(0.38f,0.72f));
            var appearImg   = AddUIImage(canvas,   "AppearancePreview", new Color(0.5f,0.5f,0.5f,0.5f),
                              new Vector2(0.4f,0.55f), new Vector2(0.6f,0.72f));
            var nextBtn2    = CreateButton(canvas, "Btn_AppearNext", "▶", new Vector2(0.62f,0.55f), new Vector2(0.7f,0.72f));
            var appearIdxTxt = AddUIText(canvas, "AppearanceIndex", "1 / 1", 16, Color.gray, TextAnchor.MiddleCenter,
                               new Vector2(0.4f,0.51f), new Vector2(0.6f,0.56f));

            // ── 배경 카드 3개 ──
            AddUIText(canvas, "Label_Background", "배경 선택", 20, Color.white, TextAnchor.MiddleLeft,
                      new Vector2(0.05f,0.35f), new Vector2(0.3f,0.43f));

            var card1 = CreateBgCard(canvas, "Card_Farmer",   "농부 출신",    "씨앗 5종 + 물뿌리개",              new Vector2(0.05f,0.1f), new Vector2(0.32f,0.43f));
            var card2 = CreateBgCard(canvas, "Card_Merchant", "상인 출신",    "골드 200 + 씨앗 2종",               new Vector2(0.37f,0.1f), new Vector2(0.64f,0.43f));
            var card3 = CreateBgCard(canvas, "Card_Chef",     "요리사 출신",  "요리법 해금 + 씨앗 2종 + 조리도구",  new Vector2(0.69f,0.1f), new Vector2(0.96f,0.43f));

            var confirmBtn = CreateButton(canvas, "Btn_Confirm", "완료", new Vector2(0.6f,0.01f), new Vector2(0.85f,0.09f));
            var backBtn    = CreateButton(canvas, "Btn_Back",    "뒤로", new Vector2(0.15f,0.01f), new Vector2(0.4f,0.09f));

            var mgrGO = new GameObject("CharCreateManager");
            var mgr   = AddScriptComponent<CharCreateManager>(mgrGO, GUID_CHAR_CREATE_MGR);
            if (mgr != null)
            {
                SerializedObject so = new SerializedObject(mgr);
                so.FindProperty("name_input")          .objectReferenceValue = nameInputGO.GetComponent<InputField>();
                so.FindProperty("appearance_preview")  .objectReferenceValue = appearImg.GetComponent<Image>();
                so.FindProperty("appearance_index_text").objectReferenceValue = appearIdxTxt.GetComponent<Text>();

                var cards = so.FindProperty("background_cards");
                cards.arraySize = 3;
                cards.GetArrayElementAtIndex(0).objectReferenceValue = card1.GetComponent<BackgroundCardUI>();
                cards.GetArrayElementAtIndex(1).objectReferenceValue = card2.GetComponent<BackgroundCardUI>();
                cards.GetArrayElementAtIndex(2).objectReferenceValue = card3.GetComponent<BackgroundCardUI>();
                so.ApplyModifiedProperties();

                AddButtonOnClick(prevBtn,     mgr, "OnClickAppearancePrev");
                AddButtonOnClick(nextBtn2,    mgr, "OnClickAppearanceNext");
                AddButtonOnClick(confirmBtn,  mgr, "OnClickConfirm");
                AddButtonOnClick(backBtn,     mgr, "OnClickBack");
            }

            SaveScene(scene, "Scene_CharCreate");
        }

        // ════════════════════════════════════════════════════════════════════════
        // Scene_Farm_01  — 첫 번째 농장 씬 (풀 매니저)
        // ════════════════════════════════════════════════════════════════════════
        [MenuItem("Farming Engine/Build Scenes/Scene_Farm_01 Only")]
        public static void BuildSceneFarm01()
        {
            var scene = NewScene("Scene_Farm_01");

            // ════════════════════════════════
            // [Environment]
            // ════════════════════════════════
            var envGroup = new GameObject("[Environment]");
            AddDirectionalLight().transform.SetParent(envGroup.transform);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Ground";
            floor.transform.SetParent(envGroup.transform);
            floor.transform.localScale = new Vector3(5, 1, 5);
            floor.layer = 9;

            var navMeshGO = InstantiatePrefab(GUID_NAVMESH, "NavMesh", Vector3.zero);
            if (navMeshGO != null) navMeshGO.transform.SetParent(envGroup.transform);

            // ════════════════════════════════
            // [Managers]
            // ════════════════════════════════
            var mgrGroup = new GameObject("[Managers]");

            var managersGO = InstantiatePrefab(GUID_MANAGERS, "Managers", Vector3.zero);
            if (managersGO != null) managersGO.transform.SetParent(mgrGroup.transform);

            var saveMgrGO = new GameObject("SaveManager");
            saveMgrGO.transform.SetParent(mgrGroup.transform);
            AddScriptComponent<SaveManager>(saveMgrGO, GUID_SAVE_MGR);

            // DQManager — NarrativeManager + DialogueQuests 핵심 컴포넌트 포함 프리팹
            var dqMgrGO = InstantiatePrefab(GUID_DQ_MANAGER, "DQManager", Vector3.zero);
            if (dqMgrGO != null) dqMgrGO.transform.SetParent(mgrGroup.transform);

            // QuestManager — 퀘스트 진행 추적 (FarmingQuest)
            var questMgrGO = new GameObject("QuestManager");
            questMgrGO.transform.SetParent(mgrGroup.transform);
            questMgrGO.AddComponent<FarmingQuest.QuestManager>();

            // StoryEventManager — NPC 클릭 트리거·조건 평가 (story_events.json)
            var semGO = new GameObject("StoryEventManager");
            semGO.transform.SetParent(mgrGroup.transform);
            semGO.AddComponent<StoryEventManager>();

            // ════════════════════════════════
            // [Characters]
            // ════════════════════════════════
            var charGroup = new GameObject("[Characters]");
            BuildCharactersGroup_Farm01(charGroup);

            // ════════════════════════════════
            // [World]
            // ════════════════════════════════
            var worldGroup = new GameObject("[World]");

            var soilParent = new GameObject("SoilArea");
            soilParent.transform.SetParent(worldGroup.transform);
            for (int i = 0; i < 6; i++)
            {
                var soil = InstantiatePrefab(GUID_SOIL, "Soil_" + (i + 1), Vector3.zero);
                if (soil != null)
                {
                    soil.transform.SetParent(soilParent.transform);
                    soil.transform.position = new Vector3((i % 3) - 1, 0f, Mathf.Floor(i / 3f));
                }
            }

            var wellGO = InstantiatePrefab(GUID_WELL, "Well", new Vector3(4f, 0f, 0f));
            if (wellGO != null) wellGO.transform.SetParent(worldGroup.transform);

            var chestGO = InstantiatePrefab(GUID_CHEST_WOOD, "Storage_ChestWood", new Vector3(-4f, 0f, 2f));
            if (chestGO != null) chestGO.transform.SetParent(worldGroup.transform);

            var exitZoneGO = InstantiatePrefab(GUID_EXIT_ZONE, "ExitZone_Town", new Vector3(0f, 0f, 8f));
            if (exitZoneGO != null)
            {
                exitZoneGO.transform.SetParent(worldGroup.transform);
                exitZoneGO.SetActive(false);
            }

            // ════════════════════════════════
            // [Camera]
            // ════════════════════════════════
            var camGroup = new GameObject("[Camera]");
            var cameraGO = InstantiatePrefab(GUID_THE_CAMERA, "TheCamera", new Vector3(0, 8, -7));
            if (cameraGO != null) cameraGO.transform.SetParent(camGroup.transform);

            // ════════════════════════════════
            // [UI]  — Canvas는 root에 두는 것이 Unity 표준이나
            //         하위 그룹으로 묶어 정리
            // ════════════════════════════════
            var uiGroup = new GameObject("[UI]");

            var uiCanvasGO = InstantiatePrefab(GUID_UI_CANVAS, "UICanvas", Vector3.zero);
            if (uiCanvasGO != null) uiCanvasGO.transform.SetParent(uiGroup.transform);

            // DQUICanvas — DialoguePanel 등 DialogueQuests UI. Quest UI는 FarmingQuest 시스템이 대체.
            var dqUIGO = InstantiatePrefab(GUID_DQ_UI_CANVAS, "DQUICanvas", Vector3.zero);
            if (dqUIGO != null)
            {
                dqUIGO.transform.SetParent(uiGroup.transform);
                foreach (var n in new[] { "QuestPanel", "QuestBox", "QuestNotice", "QuestPanelSimple" })
                {
                    var t = dqUIGO.transform.Find(n);
                    if (t != null) t.gameObject.SetActive(false);
                }
            }

            // 최우선 오버레이 캔버스 (sortOrder=100 → UICanvas·DQUICanvas 위에 렌더링)
            var saveUICanvas = CreateUICanvas("OverlayCanvas", 100);
            saveUICanvas.transform.SetParent(uiGroup.transform);

            var f01SlotPanel = CreatePanelGO(saveUICanvas, "SaveSlotPanel", GUID_SAVE_SLOT_PANEL,
                               new Vector2(0.2f,0.1f), new Vector2(0.8f,0.9f));
            var f01SlotTitle = AddUIText(f01SlotPanel, "Title", "슬롯 선택", 28, Color.white, TextAnchor.UpperCenter,
                               new Vector2(0.1f,0.88f), new Vector2(0.9f,1f));
            var f01SlotContainer = CreateEmptyRect(f01SlotPanel, "SlotContainer",
                                   new Vector2(0,0.15f), new Vector2(1,0.88f));
            var f01Slot1 = CreateSlotUI(f01SlotContainer, "SlotUI_0", 0);
            var f01Slot2 = CreateSlotUI(f01SlotContainer, "SlotUI_1", 1);
            var f01Slot3 = CreateSlotUI(f01SlotContainer, "SlotUI_2", 2);
            var f01SlotClose = CreateButton(f01SlotPanel, "Btn_Close", "← 뒤로",
                               new Vector2(0.05f,0.02f), new Vector2(0.35f,0.12f));
            HidePanelInEditor(f01SlotPanel);

            var f01ConfirmPanel = CreatePanelGO(saveUICanvas, "ConfirmPanel", GUID_CONFIRM_PANEL,
                                  new Vector2(0.3f,0.35f), new Vector2(0.7f,0.65f));
            var f01MsgTxt = AddUIText(f01ConfirmPanel, "MessageText", "확인하시겠습니까?", 22, Color.white, TextAnchor.MiddleCenter);
            var f01BtnYes = CreateButton(f01ConfirmPanel, "Btn_Confirm", "예",    new Vector2(0.05f,0.1f), new Vector2(0.45f,0.45f));
            var f01BtnNo  = CreateButton(f01ConfirmPanel, "Btn_Cancel",  "아니오", new Vector2(0.55f,0.1f), new Vector2(0.95f,0.45f));
            HidePanelInEditor(f01ConfirmPanel);

            var f01SettingsGO = CreatePanelGO(saveUICanvas, "SettingsPanel", GUID_SETTINGS_PANEL,
                                new Vector2(0.15f,0.05f), new Vector2(0.85f,0.95f));
            HidePanelInEditor(f01SettingsGO);
            BuildSettingsPanel(f01SettingsGO);

            // FullInventoryPanel — I키로 여닫는 별도 인벤토리 팝업
            var fullInvGO = new GameObject("FullInventoryPanel");
            fullInvGO.transform.SetParent(saveUICanvas.transform, false);
            var fullInvImg = fullInvGO.AddComponent<Image>();
            fullInvImg.color = new Color(0.08f, 0.08f, 0.08f, 0.97f);
            fullInvGO.AddComponent<CanvasGroup>();
            SetAnchors(fullInvGO.GetComponent<RectTransform>(),
                       new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.92f));
            var fullInvMono = fullInvGO.AddComponent<FullInventoryPanel>();
            var fullInvTitle = AddUIText(fullInvGO, "Title", "인벤토리", 24,
                               Color.white, TextAnchor.UpperCenter,
                               new Vector2(0f, 0.93f), new Vector2(1f, 1f));
            var fullInvSO = new SerializedObject(fullInvMono);
            fullInvSO.FindProperty("title_text").objectReferenceValue = fullInvTitle.GetComponent<Text>();
            fullInvSO.ApplyModifiedProperties();
            HidePanelInEditor(fullInvGO);

            // QuestUI — 퀘스트 트래커·토스트 ([UI] 그룹 직속, sortOrder:49)
            var questUIGO = new GameObject("QuestUI");
            questUIGO.transform.SetParent(uiGroup.transform);
            questUIGO.AddComponent<QuestUI>();

            // FarmingQuestPanel — J키 퀘스트 저널 ([UI] 그룹 직속, sortOrder:50)
            var farmingQuestPanelGO = new GameObject("FarmingQuestPanel");
            farmingQuestPanelGO.transform.SetParent(uiGroup.transform);
            farmingQuestPanelGO.AddComponent<FarmingQuestPanel>();

            // Inspector 참조 연결
            var f01SlotMono = f01SlotPanel.GetComponent<SaveSlotPanel>();
            if (f01SlotMono != null)
            {
                SerializedObject pso = new SerializedObject(f01SlotMono);
                pso.FindProperty("title_text")  .objectReferenceValue = f01SlotTitle;
                pso.FindProperty("close_button").objectReferenceValue = f01SlotClose.GetComponent<Button>();
                var arr = pso.FindProperty("slot_uis");
                arr.arraySize = 3;
                arr.GetArrayElementAtIndex(0).objectReferenceValue = f01Slot1.GetComponent<SaveSlotUI>();
                arr.GetArrayElementAtIndex(1).objectReferenceValue = f01Slot2.GetComponent<SaveSlotUI>();
                arr.GetArrayElementAtIndex(2).objectReferenceValue = f01Slot3.GetComponent<SaveSlotUI>();
                pso.ApplyModifiedProperties();
                AddButtonOnClick(f01SlotClose, f01SlotMono, "OnClickClose");
            }

            var f01ConfirmMono = f01ConfirmPanel.GetComponent<ConfirmPanel>();
            if (f01ConfirmMono != null)
            {
                SerializedObject cso = new SerializedObject(f01ConfirmMono);
                cso.FindProperty("message_text") .objectReferenceValue = f01MsgTxt;
                cso.FindProperty("confirm_button").objectReferenceValue = f01BtnYes.GetComponent<Button>();
                cso.FindProperty("cancel_button") .objectReferenceValue = f01BtnNo .GetComponent<Button>();
                cso.ApplyModifiedProperties();
            }

            // ════════════════════════════════
            // [Quests]
            // ════════════════════════════════
            var questGroup = new GameObject("[Quests]");
            BuildQuestsGroup_Farm01(questGroup);

            SaveScene(scene, "Scene_Farm_01");
        }

        // ════════════════════════════════════════════════════════════════════════
        // 헬퍼 메서드
        // ════════════════════════════════════════════════════════════════════════

        // ── NPC 이벤트 자동 생성 ─────────────────────────────────────────────────

        /// <summary>
        /// 모든 NPC에 기본 적용되는 intro + idle 이벤트 그룹 생성.
        /// 키 컨벤션: {sceneId}.{npcId}.{plotId}.{lineId}
        /// - {npcId}_intro : 1회, 첫 인사 ({sceneId}.{npcId}.intro.start 키)
        /// - {npcId}_idle  : 무한, 잡담 ({sceneId}.{npcId}.idle.greet_01~02 키)
        /// 퀘스트별 추가 이벤트는 반환된 그룹 오브젝트에 직접 추가.
        /// </summary>
        private static GameObject AddNPCEventGroup(GameObject questGroup,
            ActorData actorData, string npcId, string sceneId = "farm_01")
        {
            var groupGO = new GameObject(npcId.Substring(0, 1).ToUpper() +
                                         npcId.Substring(1) + "_Events");
            groupGO.transform.SetParent(questGroup.transform);
            groupGO.AddComponent<NarrativeGroup>();

            // ── intro (1회) ──
            var introGO  = new GameObject(npcId + "_intro");
            introGO.transform.SetParent(groupGO.transform);
            var introEvt = introGO.AddComponent<NarrativeEvent>();
            SetNarrativeEvent(introEvt, npcId + "_intro", actorData,
                              NarrativeEventType.InteractActor, triggerLimit: 1);
            AddDialogueLine(introGO, "Line_01", actorData,
                            sceneId + "." + npcId + ".intro.start");

            // ── idle (무한) ──
            var idleGO  = new GameObject(npcId + "_idle");
            idleGO.transform.SetParent(groupGO.transform);
            var idleEvt = idleGO.AddComponent<NarrativeEvent>();
            SetNarrativeEvent(idleEvt, npcId + "_idle", actorData,
                              NarrativeEventType.InteractActor, triggerLimit: 0);
            AddDialogueLine(idleGO, "Line_01", actorData,
                            sceneId + "." + npcId + ".idle.greet_01");
            AddDialogueLine(idleGO, "Line_02", actorData,
                            sceneId + "." + npcId + ".idle.greet_02");

            return groupGO;
        }

        // ── Actor 헬퍼 ──────────────────────────────────────────────────────────

        /// <summary>
        /// ActorData 에셋을 이름으로 찾고, 없으면 자동 생성.
        /// 생성 경로: Assets/FarmingEngine_study/Data/Actors/
        /// </summary>
        private static ActorData FindOrCreateActorData(string assetName, string actorId, bool isPlayer)
        {
            var existing = LoadAsset<ActorData>(assetName);
            if (existing != null) return existing;

            // 에셋 없으면 새로 생성
            string dir = "Assets/FarmingEngine_study/Data/Actors";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var asset = ScriptableObject.CreateInstance<ActorData>();
            asset.actor_id = actorId;
            asset.is_player = isPlayer;
            asset.title = isPlayer ? "player.name" : (actorId + ".name");

            string path = dir + "/" + assetName + ".asset";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SceneBuilder] ActorData 생성: {path}");
            return asset;
        }

        /// <summary>
        /// 오브젝트에 Actor 컴포넌트 추가 및 ActorData 연결.
        /// 이미 Actor가 있으면 데이터만 교체.
        /// </summary>
        private static void AttachActor(GameObject go, ActorData actorData)
        {
            if (actorData == null) return;
            var actor = go.GetComponent<Actor>() ?? go.AddComponent<Actor>();
            var so = new SerializedObject(actor);
            so.FindProperty("data").objectReferenceValue = actorData;
            so.ApplyModifiedProperties();
        }

        // ── DialogueQuests 헬퍼 ─────────────────────────────────────────────────

        /// <summary>이름으로 에셋 검색. 못 찾으면 경고 후 null 반환.</summary>
        private static T LoadAsset<T>(string assetName) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets(assetName + " t:" + typeof(T).Name);
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[SceneBuilder] 에셋 없음: {assetName} ({typeof(T).Name}). 먼저 에셋을 생성하세요.");
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        /// <summary>NarrativeEvent 기본 설정.</summary>
        private static void SetNarrativeEvent(NarrativeEvent evt, string eventId,
            ActorData triggerActor, NarrativeEventType triggerType, int triggerLimit)
        {
            var so = new SerializedObject(evt);
            so.FindProperty("event_id")     .stringValue          = eventId;
            so.FindProperty("trigger_type") .intValue             = (int)triggerType;
            so.FindProperty("trigger_actor").objectReferenceValue = triggerActor;
            so.FindProperty("trigger_limit").intValue             = triggerLimit;
            so.ApplyModifiedProperties();
        }

        /// <summary>NarrativeEvent 하위에 DialogueMessage 자식 오브젝트 추가.</summary>
        private static GameObject AddDialogueLine(GameObject parent, string lineName,
            ActorData actor, string textKey)
        {
            var lineGO = new GameObject(lineName);
            lineGO.transform.SetParent(parent.transform);
            var msg = lineGO.AddComponent<DialogueMessage>();
            var so  = new SerializedObject(msg);
            so.FindProperty("actor").objectReferenceValue = actor;
            so.FindProperty("text") .stringValue          = textKey;
            so.ApplyModifiedProperties();
            return lineGO;
        }

        /// <summary>대사 라인 오브젝트에 NarrativeEffect 추가.</summary>
        private static void AddNarrativeEffect(GameObject lineGO,
            EffectData effect, ScriptableObject valueData, int valueInt = 0)
        {
            var ne = lineGO.AddComponent<NarrativeEffect>();
            var so = new SerializedObject(ne);
            so.FindProperty("effect")    .objectReferenceValue = effect;
            so.FindProperty("value_data").objectReferenceValue = valueData;
            so.FindProperty("value_int") .intValue             = valueInt;
            so.ApplyModifiedProperties();
        }

        // ────────────────────────────────────────────────────────────────────────

        private static Scene NewScene(string name)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = name;
            return scene;
        }

        private static void SaveScene(Scene scene, string name)
        {
            string path = SCENES_PATH + "/" + name + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("[SceneBuilder] 씬 저장 완료: " + path);
        }

        private static GameObject AddDirectionalLight()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            return go;
        }

        private static GameObject CreateUICanvas(string name, int sortOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            go.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            return go;
        }

        private static GameObject AddUIImage(GameObject parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
            return go;
        }

        private static GameObject AddUIText(GameObject parent, string name, string text, int size, Color color, TextAnchor anchor,
            Vector2? anchorMin = null, Vector2? anchorMax = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rt = go.GetComponent<RectTransform>();
            if (anchorMin.HasValue && anchorMax.HasValue)
                SetAnchors(rt, anchorMin.Value, anchorMax.Value);
            else
                SetAnchors(rt, Vector2.zero, Vector2.one);
            return go;
        }

        private static GameObject CreatePanel(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.7f);
            SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
            return go;
        }

        private static GameObject CreatePanelGO(GameObject parent, string name, string scriptGuid, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            go.AddComponent<CanvasGroup>();
            SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
            AttachScriptByGuid(go, scriptGuid);
            return go;
        }

        private static GameObject CreateEmptyRect(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            SetAnchors(rt, anchorMin, anchorMax);
            return go;
        }

        private static GameObject CreateButton(GameObject parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.4f, 0.35f, 0.1f);
            btn.colors = colors;
            SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);

            var txt = new GameObject("Text");
            txt.transform.SetParent(go.transform, false);
            var t = txt.AddComponent<Text>();
            t.text = label;
            t.fontSize = 20;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            SetAnchors(txt.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            return go;
        }

        private static GameObject CreateSlotUI(GameObject parent, string name, int index)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f,0.2f,0.2f,1f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1f - (index + 1) * 0.33f);
            rt.anchorMax = new Vector2(1, 1f - index * 0.33f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            AddScriptComponent<SaveSlotUI>(go, GUID_SAVE_SLOT_UI);

            var slotLabel = AddUIText(go, "SlotLabel",    "슬롯 " + (index+1), 18, Color.white,  TextAnchor.MiddleLeft, new Vector2(0.02f,0.6f), new Vector2(0.4f,1f));
            var nameText  = AddUIText(go, "PlayerName",   "",                  16, Color.yellow, TextAnchor.MiddleLeft, new Vector2(0.02f,0.3f), new Vector2(0.5f,0.6f));
            var dayText   = AddUIText(go, "DayText",      "",                  14, Color.gray,   TextAnchor.MiddleLeft, new Vector2(0.5f,0.3f),  new Vector2(0.75f,0.6f));
            var timeText  = AddUIText(go, "PlayTimeText", "",                  13, Color.gray,   TextAnchor.MiddleLeft, new Vector2(0.02f,0.02f), new Vector2(0.5f,0.3f));
            var lastSave  = AddUIText(go, "LastSaveText", "",                  12, Color.gray,   TextAnchor.MiddleRight, new Vector2(0.5f,0.02f), new Vector2(0.98f,0.3f));
            var emptyLbl  = AddUIText(go, "EmptyLabel",   "빈 슬롯",           16, Color.gray,   TextAnchor.MiddleCenter);
            var selBtn    = CreateButton(go, "SelectButton", "",               Vector2.zero, Vector2.one);
            // 버튼 투명하게 (전체 클릭 영역)
            selBtn.GetComponent<Image>().color = Color.clear;

            var sui = go.GetComponent<SaveSlotUI>();
            if (sui != null)
            {
                SerializedObject so = new SerializedObject(sui);
                so.FindProperty("slot_label")       .objectReferenceValue = slotLabel.GetComponent<Text>();
                so.FindProperty("player_name_text") .objectReferenceValue = nameText .GetComponent<Text>();
                so.FindProperty("day_text")         .objectReferenceValue = dayText  .GetComponent<Text>();
                so.FindProperty("play_time_text")   .objectReferenceValue = timeText .GetComponent<Text>();
                so.FindProperty("last_save_text")   .objectReferenceValue = lastSave .GetComponent<Text>();
                so.FindProperty("empty_label")      .objectReferenceValue = emptyLbl .GetComponent<Text>();
                so.FindProperty("select_button")    .objectReferenceValue = selBtn   .GetComponent<Button>();
                so.ApplyModifiedProperties();
            }

            return go;
        }

        private static GameObject CreateBgCard(GameObject parent, string name, string title, string desc, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f,0.15f,0.1f,1f);
            go.AddComponent<CanvasGroup>();
            SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
            AttachScriptByGuid(go, GUID_BACKGROUND_CARD);

            AddUIText(go, "TitleText", title, 18, Color.white,  TextAnchor.UpperCenter,  new Vector2(0.05f,0.7f), new Vector2(0.95f,0.95f));
            AddUIText(go, "DescText",  desc,  13, Color.yellow, TextAnchor.MiddleCenter, new Vector2(0.05f,0.15f), new Vector2(0.95f,0.65f));

            var selFrame = AddUIImage(go, "SelectedFrame", new Color(1f,0.85f,0.2f,0.8f), Vector2.zero, Vector2.one);
            selFrame.GetComponent<Image>().enabled = false;

            var selBtn = CreateButton(go, "SelectButton", "", Vector2.zero, Vector2.one);
            selBtn.GetComponent<Image>().color = Color.clear;

            var card = go.GetComponent<BackgroundCardUI>();
            if (card != null)
            {
                SerializedObject so = new SerializedObject(card);
                so.FindProperty("title_text")    .objectReferenceValue = go.transform.Find("TitleText")   .GetComponent<Text>();
                so.FindProperty("desc_text")     .objectReferenceValue = go.transform.Find("DescText")    .GetComponent<Text>();
                so.FindProperty("selected_frame").objectReferenceValue = selFrame.GetComponent<Image>();
                so.FindProperty("select_button") .objectReferenceValue = selBtn.GetComponent<Button>();
                so.ApplyModifiedProperties();
            }

            return go;
        }

        private static GameObject CreateInputField(GameObject parent, string name, string placeholder, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.3f,0.3f,0.3f,1f);
            SetAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);

            var inputField = go.AddComponent<InputField>();

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var t = textGO.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 18;
            t.color = Color.white;
            SetAnchors(textGO.GetComponent<RectTransform>(), new Vector2(0.02f,0), new Vector2(0.98f,1));

            var phGO = new GameObject("Placeholder");
            phGO.transform.SetParent(go.transform, false);
            var ph = phGO.AddComponent<Text>();
            ph.text = placeholder;
            ph.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ph.fontSize = 18;
            ph.fontStyle = FontStyle.Italic;
            ph.color = new Color(0.6f,0.6f,0.6f,1f);
            SetAnchors(phGO.GetComponent<RectTransform>(), new Vector2(0.02f,0), new Vector2(0.98f,1));

            inputField.textComponent = t;
            inputField.placeholder    = ph;
            inputField.text           = "";

            return go;
        }

        private static GameObject CreateSlider(GameObject parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var container = new GameObject(name);
            container.transform.SetParent(parent.transform, false);
            SetAnchors(container.AddComponent<RectTransform>(), anchorMin, anchorMax);

            AddUIText(container, "Label", label, 16, Color.white, TextAnchor.MiddleLeft, new Vector2(0,0), new Vector2(0.3f,1));

            var sliderGO = new GameObject("Slider");
            sliderGO.transform.SetParent(container.transform, false);
            SetAnchors(sliderGO.AddComponent<RectTransform>(), new Vector2(0.32f,0.1f), new Vector2(1f,0.9f));

            var bg = new GameObject("Background");
            bg.transform.SetParent(sliderGO.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.3f,0.3f,0.3f,1f);
            SetAnchors(bg.GetComponent<RectTransform>(), new Vector2(0,0.25f), new Vector2(1,0.75f));

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGO.transform, false);
            SetAnchors(fillArea.AddComponent<RectTransform>(), new Vector2(0,0.25f), new Vector2(1,0.75f));
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.9f,0.75f,0.2f,1f);
            SetAnchors(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderGO.transform, false);
            SetAnchors(handleArea.AddComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(20,0);
            handleRt.anchorMin = handleRt.anchorMax = new Vector2(0.5f,0.5f);

            var slider = sliderGO.AddComponent<Slider>();
            slider.fillRect   = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.minValue   = 0f;
            slider.maxValue   = 1f;
            slider.value      = 1f;

            return container;
        }

        /// <summary>
        /// SettingsPanel 생성 — 탭 3개(오디오/화면/키 설정).
        /// SettingsPanel.cs의 public 필드와 1:1 매핑.
        /// </summary>
        private static void BuildSettingsPanel(GameObject settingsGO)
        {
            var settingsMono = settingsGO.GetComponent<SettingsPanel>();
            if (settingsMono == null) return;

            // ── 타이틀 ──────────────────────────────────────────────────────
            AddUIText(settingsGO, "Title", "설정", 26, Color.white, TextAnchor.UpperCenter,
                      new Vector2(0f, 0.93f), new Vector2(1f, 1f));

            // ── 탭 바 ───────────────────────────────────────────────────────
            var tabBar = CreateEmptyRect(settingsGO, "TabBar",
                         new Vector2(0f, 0.84f), new Vector2(1f, 0.92f));
            var tabAudioGO   = CreateButton(tabBar, "Tab_Audio",   "오디오",  new Vector2(0.01f,0.05f), new Vector2(0.32f,0.95f));
            var tabDisplayGO = CreateButton(tabBar, "Tab_Display", "화면",    new Vector2(0.34f,0.05f), new Vector2(0.65f,0.95f));
            var tabKeysGO    = CreateButton(tabBar, "Tab_Keys",    "키 설정", new Vector2(0.67f,0.05f), new Vector2(0.98f,0.95f));

            // ── Section_Audio ───────────────────────────────────────────────
            var secAudio = CreateEmptyRect(settingsGO, "Section_Audio",
                           new Vector2(0f, 0.09f), new Vector2(1f, 0.83f));
            var masterSlider = CreateSlider(secAudio, "Slider_Master", "마스터 볼륨",
                               new Vector2(0.05f,0.68f), new Vector2(0.95f,0.92f));
            var musicSlider  = CreateSlider(secAudio, "Slider_Music",  "음악",
                               new Vector2(0.05f,0.38f), new Vector2(0.95f,0.62f));
            var sfxSlider    = CreateSlider(secAudio, "Slider_SFX",    "효과음",
                               new Vector2(0.05f,0.08f), new Vector2(0.95f,0.32f));

            // ── Section_Display ─────────────────────────────────────────────
            var secDisplay = CreateEmptyRect(settingsGO, "Section_Display",
                             new Vector2(0f, 0.09f), new Vector2(1f, 0.83f));
            AddUIText(secDisplay, "Label_Quality", "화면 품질", 18, Color.white, TextAnchor.MiddleLeft,
                      new Vector2(0.05f, 0.77f), new Vector2(0.45f, 0.90f));
            // Dropdown은 복잡한 Template 구성이 필요하므로 GameObject만 생성 — Inspector에서 연결
            var qualityDropdownGO = new GameObject("Quality_Dropdown");
            qualityDropdownGO.transform.SetParent(secDisplay.transform, false);
            qualityDropdownGO.AddComponent<Image>().color = new Color(0.25f,0.25f,0.25f,1f);
            var qdRt = qualityDropdownGO.GetComponent<RectTransform>();
            qdRt.anchorMin = new Vector2(0.47f, 0.77f);
            qdRt.anchorMax = new Vector2(0.95f, 0.90f);
            qdRt.offsetMin = qdRt.offsetMax = Vector2.zero;
            var qdText = AddUIText(qualityDropdownGO, "Label", "Inspector에서 Dropdown 설정",
                         14, new Color(0.6f,0.6f,0.6f), TextAnchor.MiddleCenter);
            AddUIText(secDisplay, "Hint_Display", "해상도 / 품질 설정은 Inspector에서 Dropdown 컴포넌트 추가 후 연결하세요.",
                      14, new Color(0.6f,0.6f,0.6f,1f), TextAnchor.UpperLeft,
                      new Vector2(0.05f,0.05f), new Vector2(0.95f,0.72f));

            // ── Section_Keys ────────────────────────────────────────────────
            var secKeys = CreateEmptyRect(settingsGO, "Section_Keys",
                          new Vector2(0f, 0.09f), new Vector2(1f, 0.83f));
            AddUIText(secKeys, "Label_Help", "클릭 → 원하는 키 입력  (Esc = 취소)", 13,
                      new Color(0.7f,0.85f,1f), TextAnchor.MiddleLeft,
                      new Vector2(0.03f, 0.92f), new Vector2(0.97f, 1f));

            var keyDefs = new (string label, string id, float y0, float y1)[]
            {
                ("액션 (Space)",        "Btn_Bind_Action",    0.78f, 0.91f),
                ("상호작용 (Z)",         "Btn_Bind_Interact",  0.65f, 0.78f),
                ("제작 패널 (C)",        "Btn_Bind_Craft",     0.52f, 0.65f),
                ("인벤토리 (I)",         "Btn_Bind_Inventory", 0.39f, 0.52f),
                ("점프 (LeftCtrl)",      "Btn_Bind_Jump",      0.26f, 0.39f),
                ("공격/달리기 (LShift)", "Btn_Bind_Attack",    0.13f, 0.26f),
                ("퀘스트 저널 (J)",      "Btn_Bind_Journal",   0.00f, 0.13f),
            };

            Button btnAction  = null, btnInteract = null, btnCraft   = null;
            Button btnInv     = null, btnJump     = null, btnAttack  = null, btnJournal = null;

            foreach (var row in keyDefs)
            {
                AddUIText(secKeys, row.id + "_Lbl", row.label, 15, Color.white, TextAnchor.MiddleLeft,
                          new Vector2(0.03f, row.y0 + 0.005f), new Vector2(0.52f, row.y1 - 0.005f));
                var btnGO = CreateButton(secKeys, row.id, "?",
                            new Vector2(0.54f, row.y0 + 0.005f), new Vector2(0.97f, row.y1 - 0.005f));
                var btn = btnGO.GetComponent<Button>();
                if (row.id.Contains("Action"))    btnAction   = btn;
                if (row.id.Contains("Interact"))  btnInteract = btn;
                if (row.id.Contains("Craft"))     btnCraft    = btn;
                if (row.id.Contains("Inventory")) btnInv      = btn;
                if (row.id.Contains("Jump"))      btnJump     = btn;
                if (row.id.Contains("Attack"))    btnAttack   = btn;
                if (row.id.Contains("Journal"))   btnJournal  = btn;
            }

            // ── 하단 버튼 ────────────────────────────────────────────────────
            var resetBtnGO = CreateButton(settingsGO, "Btn_ResetKeys", "키 초기화",
                             new Vector2(0.05f, 0.01f), new Vector2(0.45f, 0.08f));
            var closeBtnGO = CreateButton(settingsGO, "Btn_Close",     "닫기",
                             new Vector2(0.55f, 0.01f), new Vector2(0.95f, 0.08f));

            // ── Inspector 연결 ───────────────────────────────────────────────
            SerializedObject sso = new SerializedObject(settingsMono);
            sso.FindProperty("tab_audio")      .objectReferenceValue = tabAudioGO  .GetComponent<Button>();
            sso.FindProperty("tab_display")    .objectReferenceValue = tabDisplayGO.GetComponent<Button>();
            sso.FindProperty("tab_keys")       .objectReferenceValue = tabKeysGO   .GetComponent<Button>();
            sso.FindProperty("section_audio")  .objectReferenceValue = secAudio;
            sso.FindProperty("section_display").objectReferenceValue = secDisplay;
            sso.FindProperty("section_keys")   .objectReferenceValue = secKeys;
            sso.FindProperty("master_slider")  .objectReferenceValue = masterSlider.GetComponentInChildren<Slider>();
            sso.FindProperty("music_slider")   .objectReferenceValue = musicSlider .GetComponentInChildren<Slider>();
            sso.FindProperty("sfx_slider")     .objectReferenceValue = sfxSlider   .GetComponentInChildren<Slider>();
            sso.FindProperty("bind_action")    .objectReferenceValue = btnAction;
            sso.FindProperty("bind_interact")  .objectReferenceValue = btnInteract;
            sso.FindProperty("bind_craft")     .objectReferenceValue = btnCraft;
            sso.FindProperty("bind_inventory") .objectReferenceValue = btnInv;
            sso.FindProperty("bind_jump")      .objectReferenceValue = btnJump;
            sso.FindProperty("bind_attack")    .objectReferenceValue = btnAttack;
            sso.FindProperty("bind_journal")   .objectReferenceValue = btnJournal;
            sso.ApplyModifiedProperties();

            AddButtonOnClick(closeBtnGO, settingsMono, "OnClickClose");
            AddButtonOnClick(resetBtnGO, settingsMono, "OnClickResetKeys");
        }

        private static void AddEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        /// <summary>UI 전용 카메라 — 경량 씬(Start/Intro/CharCreate)에 사용</summary>
        private static GameObject AddUICamera()
        {
            var go = new GameObject("UICamera");
            var cam = go.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask     = ~0; // Everything — cullingMask=0이면 URP가 카메라를 스킵해 "No cameras rendering" 경고 발생
            cam.orthographic    = true;
            cam.depth           = -1;
            cam.allowMSAA       = false; // Blitter MSAA 키워드 경고 방지
            cam.allowHDR        = false;
            go.tag = "MainCamera";

            // URP 카메라 데이터 — MSAA/후처리 비활성화로 Blitter 키워드 충돌 방지
            var urpData = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            urpData.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;
            urpData.renderPostProcessing = false;
            urpData.requiresColorOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off;
            urpData.requiresDepthOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off;

            return go;
        }

        private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject InstantiatePrefab(string guid, string name, Vector3 pos)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) { Debug.LogWarning("[SceneBuilder] Prefab not found: " + guid); return null; }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.position = pos;
            return go;
        }

        private static T AddScriptComponent<T>(GameObject go, string scriptGuid) where T : Component
        {
            string path = AssetDatabase.GUIDToAssetPath(scriptGuid);
            if (string.IsNullOrEmpty(path)) return go.AddComponent<T>();
            // 스크립트가 이미 컴파일됐으면 AddComponent<T>() 사용
            return go.AddComponent<T>();
        }

        private static void AttachScriptByGuid(GameObject go, string scriptGuid)
        {
            // MonoScript를 통해 컴포넌트 추가
            string path = AssetDatabase.GUIDToAssetPath(scriptGuid);
            if (string.IsNullOrEmpty(path)) return;
            var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (monoScript == null) return;
            System.Type t = monoScript.GetClass();
            if (t != null) go.AddComponent(t);
        }

        private static void AddButtonOnClick(GameObject btnGO, MonoBehaviour target, string methodName)
        {
            var btn = btnGO.GetComponent<Button>();
            if (btn == null) return;

            var evt = new UnityEngine.Events.UnityEvent();
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btn.onClick,
                System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), target, methodName)
                as UnityEngine.Events.UnityAction);
        }

        private static Material CreateColorMaterial(Color color)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            return mat;
        }

        private static void SetMaterialColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = CreateColorMaterial(color);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // UICanvas 프리팹 PausedPanel 패치
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// UICanvas 프리팹의 PausedPanel 버튼을
        /// Resume / Save / Load / Back to Menu 구성으로 교체한다.
        /// </summary>
        [MenuItem("Farming Engine/Patch PausedPanel Buttons")]
        public static void PatchPausedPanel()
        {
            string prefabPath = "Assets/FarmingEngine_study/Prefabs/UI/UICanvas.prefab";
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                EditorUtility.DisplayDialog("오류", "UICanvas.prefab을 찾을 수 없습니다.", "확인");
                return;
            }

            // PausedPanel 찾기
            var pausedPanel = FindInChildren(prefabRoot.transform, "PausedPanel");
            if (pausedPanel == null)
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                EditorUtility.DisplayDialog("오류", "PausedPanel을 찾지 못했습니다.", "확인");
                return;
            }

            // PausePanel 스크립트 컴포넌트
            // GetComponent<MonoBehaviour>()는 Image 등 첫 번째 MB를 반환하므로 명시적으로 지정
            var pausePanelMono = pausedPanel.GetComponent<PausePanel>();
            if (pausePanelMono == null)
            {
                // fallback: 스크립트 GUID로 직접 탐색
                foreach (var mono in pausedPanel.GetComponents<MonoBehaviour>())
                {
                    if (mono != null && mono.GetType().Name == "PausePanel")
                    {
                        pausePanelMono = (PausePanel)mono;
                        break;
                    }
                }
            }
            if (pausePanelMono == null)
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                EditorUtility.DisplayDialog("오류", "PausedPanel에서 PausePanel 컴포넌트를 찾지 못했습니다.", "확인");
                return;
            }

            // ── 1. NewButton → 계속하기 ──────────────────────────────────────
            var newBtn = FindInChildren(pausedPanel, "NewButton");
            if (newBtn != null)
            {
                SetButtonLabel(newBtn, "계속하기");
                RewireButtonOnClick(newBtn.gameObject, pausePanelMono, "OnClickResume");
            }

            // ── 2. SAVE → 저장 ───────────────────────────────────────────────
            var saveBtn = FindButtonByMethodName(pausedPanel, "OnClickSave");
            if (saveBtn != null)
            {
                SetButtonLabel(saveBtn.transform, "저장");
                RewireButtonOnClick(saveBtn.gameObject, pausePanelMono, "OnClickSave");
            }

            // ── 3. LOAD LAST → 불러오기 ──────────────────────────────────────
            var loadBtn = FindButtonByMethodName(pausedPanel, "OnClickLoad");
            if (loadBtn != null)
            {
                SetButtonLabel(loadBtn.transform, "불러오기");
                RewireButtonOnClick(loadBtn.gameObject, pausePanelMono, "OnClickLoad");
            }

            // ── 4. 음악 토글은 그대로 유지 (아이콘 버튼이라 메뉴 버튼으로 사용 불가)

            // ── 5 & 6. 설정 / 메인메뉴로 버튼 — 중복 전부 삭제 후 새로 생성 ──
            RectTransform baseRT = newBtn != null ? newBtn.GetComponent<RectTransform>() : null;

            // 중복 제거: 같은 이름 버튼이 여러 개 있을 수 있으므로 전부 삭제
            DestroyAllChildren(pausedPanel, "SettingsButton");
            DestroyAllChildren(pausedPanel, "BackToMenuButton");

            if (baseRT != null)
            {
                // 설정 버튼 — NewButton 바로 아래
                Vector2 settingsPos = baseRT.anchoredPosition + new Vector2(0, -(baseRT.sizeDelta.y + 10f));
                var settingsGO = MakePauseButton(pausedPanel, "SettingsButton", "설정",
                                                 new Color(0.15f, 0.25f, 0.4f),
                                                 settingsPos,
                                                 baseRT.sizeDelta, baseRT.anchorMin, baseRT.anchorMax);
                RewireButtonOnClick(settingsGO, pausePanelMono, "OnClickSettings");

                // 메인 메뉴로 버튼 — 설정 버튼 바로 아래
                Vector2 backPos = settingsPos + new Vector2(0, -(baseRT.sizeDelta.y + 10f));
                var backGO = MakePauseButton(pausedPanel, "BackToMenuButton", "메인 메뉴로",
                                             new Color(0.45f, 0.1f, 0.08f),
                                             backPos,
                                             baseRT.sizeDelta, baseRT.anchorMin, baseRT.anchorMax);
                RewireButtonOnClick(backGO, pausePanelMono, "OnClickBackToMenu");
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("완료",
                "PausedPanel 버튼 패치 완료!\n\n" +
                "• 계속하기  → OnClickResume\n" +
                "• 저장      → OnClickSave\n" +
                "• 불러오기  → OnClickLoad\n" +
                "• 설정      → OnClickSettings\n" +
                "• 메인 메뉴로 → OnClickBackToMenu\n\n" +
                "⚠️ UICanvas 프리팹 > PausedPanel > PausePanel 컴포넌트에서\n" +
                "save_slot_panel / confirm_panel / settings_panel 참조 연결 필요",
                "확인");
        }

        private static GameObject MakePauseButton(Transform parent, string goName, string label,
                                                   Color bgColor, Vector2 anchoredPos, Vector2 sizeDelta,
                                                   Vector2 anchorMin, Vector2 anchorMax)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.text      = label;
            txt.fontSize  = 20;
            txt.color     = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var tRT = txtGO.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.offsetMin = tRT.offsetMax = Vector2.zero;
            return go;
        }

        /// <summary>UIPanel 패널을 에디터에서 보이지 않게 설정 (alpha=0). Awake()가 없는 에디터 모드에서도 숨김 유지.</summary>
        private static void HidePanelInEditor(GameObject panelGO)
        {
            var cg = panelGO.GetComponent<CanvasGroup>();
            if (cg == null) cg = panelGO.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        /// <summary>parent 직계 자식 중 name과 일치하는 것을 모두 즉시 삭제 (중복 버튼 정리용)</summary>
        private static void DestroyAllChildren(Transform parent, string name)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    Object.DestroyImmediate(child.gameObject);
                else
                    DestroyAllChildren(child, name); // 재귀 탐색
            }
        }

        private static Transform FindInChildren(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                var found = FindInChildren(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Button FindButtonByMethodName(Transform root, string methodName)
        {
            foreach (var btn in root.GetComponentsInChildren<Button>(true))
            {
                var onClick = btn.onClick;
                var calls = new SerializedObject(btn).FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                if (calls == null) continue;
                for (int i = 0; i < calls.arraySize; i++)
                {
                    var call = calls.GetArrayElementAtIndex(i);
                    if (call.FindPropertyRelative("m_MethodName").stringValue == methodName)
                        return btn;
                }
            }
            return null;
        }

        private static void SetButtonLabel(Transform btnRoot, string label)
        {
            var txt = btnRoot.GetComponentInChildren<Text>();
            if (txt != null) txt.text = label;
        }

        private static void RewireButtonOnClick(GameObject btnGO, PausePanel target, string methodName)
        {
            var btn = btnGO.GetComponent<Button>();
            if (btn == null || target == null) return;

            // 기존 persistent 리스너 전부 제거
            var so = new SerializedObject(btn);
            var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (calls != null) { calls.arraySize = 0; so.ApplyModifiedProperties(); }

            // Unity 공식 API로 재등록
            try
            {
                var action = System.Delegate.CreateDelegate(
                    typeof(UnityEngine.Events.UnityAction), target, methodName)
                    as UnityEngine.Events.UnityAction;
                UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btn.onClick, action);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SceneBuilder] RewireButtonOnClick 실패 ({methodName}): {e.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // 씬 맵 Export / Import  (웹 미니맵 연동)
        // JSON 형식: Assets/StreamingAssets/scene_config.json
        // 좌표계: Unity worldPos.x → unityX, worldPos.z → unityZ
        // ══════════════════════════════════════════════════════════════════

        [MenuItem("Farming Engine/Scene Map/씬 맵 내보내기 (Export → JSON)")]
        static void ExportSceneMap()
        {
            var activeScene = SceneManager.GetActiveScene();
            string sceneName = activeScene.name;                          // "Scene_Farm_01"
            string sceneId   = sceneName.Replace("Scene_", "").ToLower(); // "farm_01"

            // ── 오브젝트 수집 ──
            var entries  = new System.Collections.Generic.List<SceneObjEntry>();
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            void Track(float x, float z) {
                if (x < minX) minX = x; if (x > maxX) maxX = x;
                if (z < minZ) minZ = z; if (z > maxZ) maxZ = z;
            }

            // Player / NPC (Actor 컴포넌트)
            foreach (var actor in Object.FindObjectsOfType<Actor>())
            {
                var pos = actor.transform.position;
                Track(pos.x, pos.z);
                bool isPlayer = actor.data != null && actor.data.is_player;
                entries.Add(new SceneObjEntry {
                    id        = actor.gameObject.name.ToLower().Replace(" ","_"),
                    unityName = actor.gameObject.name,
                    type      = isPlayer ? "player" : "npc",
                    label     = actor.data?.title ?? actor.gameObject.name,
                    emoji     = isPlayer ? "🧑" : "👤",
                    unityX    = pos.x,
                    unityZ    = pos.z,
                });
            }

            // Construction (건물, 우물 등) — Soil 컴포넌트 가진 오브젝트는 별도 처리
            foreach (var c in Object.FindObjectsOfType<Construction>())
            {
                if (c.GetComponent<Actor>() != null) continue; // NPC와 중복 방지
                if (c.GetComponent<Soil>() != null)  continue; // Soil은 아래에서 별도 처리
                var pos = c.transform.position;
                Track(pos.x, pos.z);
                entries.Add(new SceneObjEntry {
                    id        = c.gameObject.name.ToLower().Replace(" ","_").Replace("(clone)",""),
                    unityName = c.gameObject.name,
                    type      = "structure",
                    label     = c.data?.title ?? c.gameObject.name,
                    emoji     = "🏚️",
                    unityX    = pos.x,
                    unityZ    = pos.z,
                });
            }

            // Plant (작물)
            foreach (var plant in Object.FindObjectsOfType<Plant>())
            {
                var pos = plant.transform.position;
                Track(pos.x, pos.z);
                entries.Add(new SceneObjEntry {
                    id        = "plant_" + Mathf.Abs(plant.GetInstanceID()).ToString(),
                    unityName = plant.gameObject.name,
                    type      = "plant",
                    label     = plant.data?.title ?? plant.gameObject.name,
                    emoji     = "🌱",
                    unityX    = pos.x,
                    unityZ    = pos.z,
                });
            }

            // Soil
            foreach (var soil in Object.FindObjectsOfType<Soil>())
            {
                var pos = soil.transform.position;
                Track(pos.x, pos.z);
                entries.Add(new SceneObjEntry {
                    id        = "soil_" + Mathf.Abs(soil.GetInstanceID()).ToString(),
                    unityName = soil.gameObject.name,
                    type      = "soil",
                    label     = "흙밭",
                    emoji     = "",
                    unityX    = pos.x,
                    unityZ    = pos.z,
                    w         = 1.0f,
                    h         = 1.0f,
                });
            }

            // ExitZone: 이름에 "Exit"/"Zone" 포함 + 월드 공간에 있는 오브젝트만
            // UI 오브젝트(Canvas 하위 등)는 position이 수백~수천 단위로 벗어나므로 bounds 내 오브젝트만 포함
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (!go.name.Contains("Exit") && !go.name.Contains("Zone")) continue;
                if (go.GetComponent<Actor>() != null || go.GetComponent<Construction>() != null) continue;
                // Canvas 하위 UI 오브젝트 제외
                if (go.GetComponentInParent<Canvas>() != null) continue;
                var pos = go.transform.position;
                // bounds 계산 전이므로 현재 수집된 entries 기반 임시 범위로 UI 좌표 걸러냄
                // position magnitude가 비정상적으로 크면 제외 (UI 버튼 등)
                if (Mathf.Abs(pos.x) > 500 || Mathf.Abs(pos.z) > 500) continue;
                Track(pos.x, pos.z);
                entries.Add(new SceneObjEntry {
                    id        = go.name.ToLower().Replace(" ","_"),
                    unityName = go.name,
                    type      = "exit",
                    label     = go.name,
                    emoji     = "🚪",
                    unityX    = pos.x,
                    unityZ    = pos.z,
                });
            }

            if (entries.Count == 0)
            {
                EditorUtility.DisplayDialog("내보내기", "씬에서 오브젝트를 찾지 못했습니다.\n씬을 먼저 열어주세요.", "확인");
                return;
            }

            // ── Bounds (여백 포함) ──
            float pad = 6f;
            if (minX == float.MaxValue) { minX = -10; maxX = 50; minZ = -10; maxZ = 40; }
            else { minX -= pad; maxX += pad; minZ -= pad; maxZ += pad; }

            // ── JSON 직렬화 (Newtonsoft.Json) ──
            var objArray = new Newtonsoft.Json.Linq.JArray();
            foreach (var e in entries)
            {
                var j = new Newtonsoft.Json.Linq.JObject();
                j["id"]        = e.id;
                j["unityName"] = e.unityName;
                j["type"]      = e.type;
                j["label"]     = e.label;
                j["emoji"]     = e.emoji;
                j["unityX"]    = e.unityX;
                j["unityZ"]    = e.unityZ;
                if (e.questId != null) j["questId"] = e.questId;
                if (e.type == "quest_zone") { j["w"] = e.w; j["h"] = e.h; }
                objArray.Add(j);
            }

            var sceneJson = new Newtonsoft.Json.Linq.JObject();
            sceneJson["name"]   = sceneName;
            sceneJson["bounds"] = new Newtonsoft.Json.Linq.JObject {
                ["minX"] = minX, ["maxX"] = maxX,
                ["minZ"] = minZ, ["maxZ"] = maxZ
            };
            sceneJson["objects"] = objArray;

            // ── 기존 파일 병합 ──
            string path = Application.dataPath + "/StreamingAssets/scene_config.json";
            Newtonsoft.Json.Linq.JObject root;
            if (File.Exists(path))
                root = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path));
            else
                root = new Newtonsoft.Json.Linq.JObject();

            root[sceneId] = sceneJson;
            File.WriteAllText(path, root.ToString(Newtonsoft.Json.Formatting.Indented));
            AssetDatabase.Refresh();

            Debug.Log($"[SceneBuilder] 씬 맵 내보내기 완료: {path} / {entries.Count}개 오브젝트");
            EditorUtility.DisplayDialog("내보내기 완료",
                $"{sceneId} 씬 → scene_config.json\n{entries.Count}개 오브젝트 저장됨.\n\n웹 미니맵에서 📂 불러오기 후 확인하세요.", "확인");
        }

        [MenuItem("Farming Engine/Scene Map/씬 맵 가져오기 (Import ← JSON)")]
        static void ImportSceneMap()
        {
            string path = Application.dataPath + "/StreamingAssets/scene_config.json";
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("가져오기 오류", "scene_config.json 파일이 없습니다.\n먼저 웹 미니맵에서 편집 후 저장하세요.", "확인");
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            string sceneId = activeScene.name.Replace("Scene_", "").ToLower();

            var root = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path));
            if (!root.ContainsKey(sceneId))
            {
                EditorUtility.DisplayDialog("가져오기 오류",
                    $"scene_config.json에 '{sceneId}' 데이터가 없습니다.\n먼저 Export로 초기 데이터를 만든 뒤 웹에서 편집하세요.", "확인");
                return;
            }

            var sceneData = root[sceneId];
            var bounds    = sceneData["bounds"];
            var objects   = sceneData["objects"] as Newtonsoft.Json.Linq.JArray;
            if (objects == null) return;

            if (!EditorUtility.DisplayDialog("씬 맵 가져오기",
                $"scene_config.json의 {sceneId} 데이터로 오브젝트를 동기화합니다.\n\n" +
                "• JSON에 있는 오브젝트: 위치 업데이트 / 없으면 생성\n" +
                "• [World] 하위 프리팹 중 JSON에 없는 것: 삭제\n\n" +
                "계속하시겠습니까?", "가져오기", "취소"))
                return;

            // JSON에 있는 unityName 목록 + locked 오브젝트는 삭제 대상에서 제외
            var jsonNames  = new System.Collections.Generic.HashSet<string>();
            var lockedNames = new System.Collections.Generic.HashSet<string>();
            foreach (Newtonsoft.Json.Linq.JObject obj in objects)
            {
                string n = obj["unityName"]?.ToString();
                if (string.IsNullOrEmpty(n)) continue;
                jsonNames.Add(n);
                if (obj["locked"]?.ToObject<bool>() == true)
                    lockedNames.Add(n);
            }

            var worldParent = GameObject.Find("[World]");
            int moved = 0, notFound = 0, deleted = 0;

            foreach (Newtonsoft.Json.Linq.JObject obj in objects)
            {
                string unityName = obj["unityName"]?.ToString();
                if (string.IsNullOrEmpty(unityName))
                {
                    string objId = obj["id"]?.ToString() ?? "?";
                    Debug.LogWarning($"[SceneBuilder] Import: id='{objId}' — unityName 없음. 웹에서 추가된 오브젝트는 Unity에서 직접 생성한 뒤 Props 패널의 unityName을 실제 오브젝트 이름과 일치시켜 주세요.");
                    notFound++;
                    continue;
                }

                float ux = obj["unityX"] != null ? (float)obj["unityX"] : 0f;
                float uz = obj["unityZ"] != null ? (float)obj["unityZ"] : 0f;

                var go = GameObject.Find(unityName);
                if (go != null)
                {
                    // 같은 이름의 parent가 있으면 위로 올라간다 (예: Well > Well 구조에서 child가 먼저 검색되는 경우)
                    var target = go.transform;
                    while (target.parent != null && target.parent.name == unityName)
                        target = target.parent;

                    Undo.RecordObject(target, "Import Scene Map");
                    target.position = new Vector3(ux, target.position.y, uz);

                    // 이전 잘못된 import로 인해 child의 localPosition이 어긋난 경우 복구
                    foreach (Transform child in target)
                    {
                        if (child.name == unityName && child.localPosition != Vector3.zero)
                        {
                            Undo.RecordObject(child, "Import Scene Map - Fix Child LocalPos");
                            child.localPosition = Vector3.zero;
                        }
                    }

                    moved++;
                }
                else
                {
                    // 씬에 없으면 프리팹 폴더에서 같은 이름의 프리팹을 찾아 생성
                    string prefabPath = FindPrefabByName(unityName);
                    if (prefabPath != null)
                    {
                        var prefab   = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        instance.name = unityName;
                        instance.transform.position = new Vector3(ux, 0f, uz);

                        if (worldParent != null)
                            instance.transform.SetParent(worldParent.transform, true);

                        Undo.RegisterCreatedObjectUndo(instance, "Import Scene Map - Spawn");
                        moved++;
                        Debug.Log($"[SceneBuilder] Import: '{unityName}' 프리팹으로 생성됨 ({prefabPath})");
                    }
                    else
                    {
                        Debug.LogWarning($"[SceneBuilder] Import: '{unityName}' — 씬에도 없고 Prefabs 폴더에서도 찾지 못했습니다.");
                        notFound++;
                    }
                }
            }

            // JSON에 없는 [World] 하위 프리팹 인스턴스 삭제
            if (worldParent != null)
            {
                var toDelete = new System.Collections.Generic.List<GameObject>();
                foreach (Transform child in worldParent.transform)
                {
                    if (!jsonNames.Contains(child.name)
                        && !lockedNames.Contains(child.name)
                        && PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))
                        toDelete.Add(child.gameObject);
                }
                foreach (var go in toDelete)
                {
                    Debug.Log($"[SceneBuilder] Import: '{go.name}' — JSON에 없어 삭제됨");
                    Undo.DestroyObjectImmediate(go);
                    deleted++;
                }
            }

            string msg = $"완료: {moved}개 오브젝트 위치 업데이트/생성";
            if (deleted  > 0) msg += $"\n삭제: {deleted}개 (JSON에 없는 [World] 오브젝트)";
            if (notFound > 0) msg += $"\n미매칭: {notFound}개 (Console 확인)";
            EditorUtility.DisplayDialog("가져오기 완료", msg, "확인");
        }

        static string FindPrefabByName(string name)
        {
            string[] folders = { "Constructions", "Plants", "Environment", "Decoration", "Town", "Animals", "Enemies", "Equip", "Terrain", "Zones" };
            string   root    = "Assets/FarmingEngine_study/Prefabs";
            foreach (var folder in folders)
            {
                string path = $"{root}/{folder}/{name}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                    return path;
            }
            // 루트 레벨도 확인 (PlayerCharacter 등)
            string rootPath = $"{root}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(rootPath) != null)
                return rootPath;
            return null;
        }

        // ══════════════════════════════════════════════════════════════════
        // 프리팹 썸네일 내보내기
        // 결과: Assets/StreamingAssets/prefab_thumbnails/{카테고리}/{이름}.png
        // ══════════════════════════════════════════════════════════════════

        [MenuItem("Farming Engine/Scene Map/프리팹 썸네일 내보내기")]
        static void ExportPrefabThumbnails()
        {
            string prefabRoot = Application.dataPath + "/FarmingEngine_study/Prefabs";
            string thumbRoot  = Application.dataPath + "/StreamingAssets/prefab_thumbnails";
            string[] categories = { "Constructions", "Plants", "Environment", "Decoration", "Town", "Animals", "Enemies", "Terrain", "Zones" };

            // 전체 파일 수 먼저 집계 (프로그레스 바용)
            int total = 0;
            foreach (var cat in categories)
            {
                string fp = prefabRoot + "/" + cat;
                if (Directory.Exists(fp)) total += Directory.GetFiles(fp, "*.prefab").Length;
            }

            int current = 0, saved = 0, failed = 0;

            foreach (var cat in categories)
            {
                string folderPath = prefabRoot + "/" + cat;
                if (!Directory.Exists(folderPath)) continue;

                string thumbDir = thumbRoot + "/" + cat;
                if (!Directory.Exists(thumbDir)) Directory.CreateDirectory(thumbDir);

                foreach (var pf in Directory.GetFiles(folderPath, "*.prefab"))
                {
                    current++;
                    string name    = Path.GetFileNameWithoutExtension(pf);
                    string relPath = $"Assets/FarmingEngine_study/Prefabs/{cat}/{Path.GetFileName(pf)}";

                    EditorUtility.DisplayProgressBar("프리팹 썸네일 내보내기",
                        $"{cat}/{name}  ({current}/{total})", (float)current / total);

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(relPath);
                    if (prefab == null) { failed++; continue; }

                    // RenderStaticPreview: 동기 즉시 렌더 — Project 창 썸네일과 동일한 방식
                    var editor = UnityEditor.Editor.CreateEditor(prefab);
                    var tex    = editor.RenderStaticPreview(relPath, null, 128, 128);
                    Object.DestroyImmediate(editor);

                    if (tex == null) { failed++; continue; }

                    string thumbPath = thumbDir + "/" + name + ".png";
                    try
                    {
                        File.WriteAllBytes(thumbPath, tex.EncodeToPNG());
                        saved++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[SceneBuilder] 썸네일 저장 실패: {name} — {e.Message}");
                        failed++;
                    }
                    Object.DestroyImmediate(tex);
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("완료",
                $"프리팹 썸네일 내보내기 완료\n{saved}개 저장 / {failed}개 실패\nStreamingAssets/prefab_thumbnails/", "확인");
        }

        // Export용 내부 데이터 클래스
        private class SceneObjEntry
        {
            public string id, unityName, type, label, emoji, questId;
            public float  unityX, unityZ, w = 2f, h = 2f;
        }
    }

}
