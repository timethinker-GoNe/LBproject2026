using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if DIALOGUE_QUESTS
using DialogueQuests;
using Newtonsoft.Json;
#endif

namespace FarmingEngine
{
    /// <summary>
    /// DialogueQuests ↔ FarmingEngine 브릿지.
    /// 스토리이벤트: StoryEventManager가 조건 평가 → 이 클래스가 NarrativeManager로 렌더링.
    /// 아이들 대화:  npc_idle.json 기반, 스토리이벤트 매칭이 없을 때 재생.
    /// </summary>
    public class DialogueQuestsWrap : MonoBehaviour
    {

#if DIALOGUE_QUESTS

        // ── npc_idle.json 파싱용 내부 클래스 ─────────────────────────

        private class JsIdleChoice
        {
            [JsonProperty("text_key")] public string textKey;
            [JsonProperty("go_to")]    public string goTo;
        }

        private class JsIdleLine
        {
            public string actor;
            [JsonProperty("text_key")] public string textKey;
            public string type = "normal"; // "normal" | "choice"
            public List<JsIdleChoice> choices;
        }

        private class JsIdleEffect
        {
            public string type;
            [JsonProperty("item_id")]  public string itemId;
            [JsonProperty("quest_id")] public string questId;
            [JsonProperty("npc_id")]   public string npcId;
            public string value;
            public int quantity = 1;
        }

        private class JsIdleCondition
        {
            public string type;                            // "npc_status" | "quest_status"
            [JsonProperty("npc_id")]   public string npcId;
            public string value;                           // npc_status용
            [JsonProperty("quest_id")] public string questId;
            public string status;                          // quest_status용
        }

        private class JsIdleEntry
        {
            public string id;
            public List<JsIdleCondition> conditions;
            public List<JsIdleLine>      lines;
            public List<JsIdleEffect>    effects;
        }

        private class JsIdleNpc
        {
            public List<JsIdleEntry> entries;
        }

        private class JsIdleRoot
        {
            public Dictionary<string, JsIdleNpc> npcs;
        }

        // 이벤트 GameObject에 효과+액터 정보를 첨부하기 위한 내부 컴포넌트
        private class BranchEffectHolder : MonoBehaviour
        {
            public List<SEEffect> effects;
            public Actor playerActor;
            public Actor npcActor;
        }

        // ── 상태 ─────────────────────────────────────────────────────

        private JsIdleRoot _idleData;
        private HashSet<Actor> inited_actors = new HashSet<Actor>();
        private float timer = 1f;
        private bool _dialogueActive = false;
        private bool _startingEvent = false;
        private SEMatchedEvent _currentSEMEvent; // 현재 재생 중인 스토리이벤트 (null = 아이들)
        [SerializeField] private bool pauseGameDuringDialogue = true;

        // 선택지 go_to: onSelect는 부모 이벤트 종료 전에 불리므로 OnEventEnd 후 처리
        private string _pendingGoToNpcId;
        private string _pendingGoToTargetId;
        private Actor  _pendingGoToPlayer;
        private Actor  _pendingGoToNpc;

        // ── 정적 초기화 ───────────────────────────────────────────────

        static DialogueQuestsWrap()
        {
            TheGame.afterLoad    += ReloadDQ;
            TheGame.afterNewGame += NewDQ;
        }

        // ── 라이프사이클 ──────────────────────────────────────────────

        private void Awake()
        {
            LoadNpcIdle();
            DialogueLocalizer.Load("ko");
            PlayerData.LoadLast();

            NarrativeManager narrative = FindObjectOfType<NarrativeManager>();
            if (narrative != null)
            {
                narrative.onPauseGameplay   += OnPauseGameplay;
                narrative.onUnpauseGameplay += OnUnpauseGameplay;
                narrative.onPlaySFX         += OnPlaySFX;
                narrative.onPlayMusic       += OnPlayMusic;
                narrative.onStopMusic       += OnStopMusic;
                narrative.getTimestamp      += GetTimestamp;
                narrative.use_custom_audio   = true;
            }
            else
            {
                Debug.LogError("[DQW] NarrativeManager not found in scene.");
            }

            TheGame the_game = FindObjectOfType<TheGame>();
            if (the_game != null)
            {
                the_game.beforeSave += SaveDQ;
                LoadDQ();
            }
        }

        private void Start()
        {
            if (Actor.GetPlayerActor() == null)
                Debug.LogError("[DQW] Player Actor not found. Add Actor component with is_player=true.");

            NarrativeManager nm = NarrativeManager.Get();
            if (nm != null)
                nm.onEventEnd += OnEventEnd;
        }

        private void OnDestroy()
        {
            NarrativeManager nm = NarrativeManager.Get();
            if (nm != null)
                nm.onEventEnd -= OnEventEnd;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer > 1f)
            {
                timer = 0f;
                SlowUpdate();
            }
        }

        private void SlowUpdate()
        {
            foreach (Actor actor in Actor.GetAll())
            {
                if (!inited_actors.Contains(actor))
                {
                    inited_actors.Add(actor);
                    InitActor(actor);
                }
            }
        }

        // ── 초기화 ────────────────────────────────────────────────────

        private void LoadNpcIdle()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Dialogue", "npc_idle.json");
            if (!File.Exists(path)) { Debug.LogWarning("[DQW] npc_idle.json not found: " + path); return; }
            try { _idleData = JsonConvert.DeserializeObject<JsIdleRoot>(File.ReadAllText(path)); }
            catch (System.Exception e) { Debug.LogError("[DQW] npc_idle.json 파싱 오류: " + e.Message); }
        }

        private void InitActor(Actor actor)
        {
            if (actor == null) return;
            Selectable select = actor.GetComponent<Selectable>();
            if (select == null) return;

            actor.auto_interact_enabled = false;
            select.onUse += (PlayerCharacter character) =>
            {
                if (_dialogueActive) return;

                character.StopMove();
                character.FaceTorward(actor.transform.position);

                string npcId = actor?.data?.actor_id ?? "";
                FarmingEvents.OnNpcTalked?.Invoke(npcId);

                Actor playerActor = character.GetComponent<Actor>();

                // StoryEventManager에 스토리이벤트 조회 → 없으면 아이들 재생
                SEMatchedEvent storyEvt = StoryEventManager.Instance?.TryGetNpcTalkEvent(npcId);
                if (storyEvt != null)
                {
                    _currentSEMEvent = storyEvt;
                    StartEvent(storyEvt.id, storyEvt.lines, storyEvt.effects, playerActor, actor);
                }
                else
                {
                    _currentSEMEvent = null;
                    StartIdleEvent(npcId, playerActor, actor);
                }
            };
        }

        // ── 아이들 대화 ───────────────────────────────────────────────

        private void StartIdleEvent(string npcId, Actor playerActor, Actor npcActor)
        {
            if (_idleData?.npcs == null || !_idleData.npcs.TryGetValue(npcId, out var idleNpc))
            {
                npcActor.Interact(playerActor);
                return;
            }

            JsIdleEntry chosen = null;
            foreach (var entry in idleNpc.entries ?? new List<JsIdleEntry>())
            {
                if (EvalIdleConditions(entry.conditions, npcId))
                {
                    chosen = entry;
                    break;
                }
            }

            if (chosen == null)
            {
                Debug.LogWarning($"[DQW] npc '{npcId}' 매칭 idle entry 없음");
                return;
            }

            var lines   = ConvertIdleLines(chosen.lines);
            var effects = ConvertIdleEffects(chosen.effects);
            StartEvent("_idle_" + npcId + "_" + (chosen.id ?? ""), lines, effects, playerActor, npcActor);
        }

        // conditions[] 평가: 빈 배열 = 항상 매칭 (fallback)
        private bool EvalIdleConditions(List<JsIdleCondition> conditions, string npcId)
        {
            if (conditions == null || conditions.Count == 0) return true;
            var pdata = PlayerData.Get();
            foreach (var c in conditions)
                if (!EvalIdleCondition(c, pdata, npcId)) return false;
            return true;
        }

        private bool EvalIdleCondition(JsIdleCondition c, PlayerData pdata, string npcId)
        {
            switch (c.type)
            {
                case "npc_status":
                {
                    string targetNpc = c.npcId ?? npcId;
                    string cur = (pdata?.npc_status != null &&
                                  pdata.npc_status.TryGetValue(targetNpc, out var v)) ? v : "";
                    return cur == (c.value ?? "");
                }
                case "quest_status":
                {
                    var qm     = FarmingQuest.QuestManager.Instance;
                    var status = qm != null ? qm.GetStatus(c.questId) : FarmingQuest.QuestStatus.NotStarted;
                    return status.ToString() == c.status;
                }
                default:
                    Debug.LogWarning($"[DQW] 알 수 없는 idle 조건 타입: '{c.type}'");
                    return false;
            }
        }

        // go_to: 아이들 entry ID로 직접 점프
        private bool TryStartIdleEntryById(string npcId, string entryId, Actor playerActor, Actor npcActor)
        {
            if (_idleData?.npcs == null || !_idleData.npcs.TryGetValue(npcId, out var idleNpc)) return false;
            var entry = idleNpc.entries?.Find(e => e.id == entryId);
            if (entry == null) return false;
            var lines   = ConvertIdleLines(entry.lines);
            var effects = ConvertIdleEffects(entry.effects);
            if (lines.Count == 0)
            {
                ApplyEffects(effects, playerActor, npcActor);
                return true;
            }
            StartEvent("_idle_" + npcId + "_" + entryId, lines, effects, playerActor, npcActor);
            return true;
        }

        private List<SELine> ConvertIdleLines(List<JsIdleLine> src)
        {
            var result = new List<SELine>();
            if (src == null) return result;
            foreach (var l in src)
            {
                var line = new SELine { actor = l.actor, textKey = l.textKey, type = l.type ?? "normal" };
                if (l.choices != null)
                {
                    line.choices = new List<SEChoice>();
                    foreach (var c in l.choices)
                        line.choices.Add(new SEChoice { textKey = c.textKey, goTo = c.goTo });
                }
                result.Add(line);
            }
            return result;
        }

        private List<SEEffect> ConvertIdleEffects(List<JsIdleEffect> src)
        {
            var result = new List<SEEffect>();
            if (src == null) return result;
            foreach (var e in src)
                result.Add(new SEEffect {
                    type = e.type, itemId = e.itemId, questId = e.questId,
                    npcId = e.npcId, value = e.value, quantity = e.quantity
                });
            return result;
        }

        // ── 이벤트 플레이 ─────────────────────────────────────────────

        private void StartEvent(string eventId, List<SELine> lines, List<SEEffect> effects, Actor playerActor, Actor npcActor)
        {
            if (_dialogueActive) return;

            // 자식 라인 먼저 생성 (NarrativeEvent.Awake가 자식 스캔)
            GameObject evtGO = new GameObject("_dtree_" + eventId);

            foreach (var lineData in lines ?? new List<SELine>())
            {
                GameObject lineGO = new GameObject("_line");
                lineGO.transform.SetParent(evtGO.transform);

                DialogueMessage msg = lineGO.AddComponent<DialogueMessage>();
                string lineActorId  = !string.IsNullOrEmpty(lineData.actor)
                                        ? lineData.actor
                                        : npcActor?.data?.actor_id;
                msg.actor = ResolveActorData(lineActorId, npcActor, playerActor);
                msg.text  = lineData.textKey ?? "";

                if (lineData.choices != null)
                {
                    foreach (var choiceData in lineData.choices)
                    {
                        DialogueChoice choice = lineGO.AddComponent<DialogueChoice>();
                        choice.text = choiceData.textKey ?? "";
                        if (!string.IsNullOrEmpty(choiceData.goTo))
                        {
                            string targetId   = choiceData.goTo;
                            string npcIdForGo = npcActor?.data?.actor_id ?? "";
                            Actor cp = playerActor, cn = npcActor;
                            choice.onSelect = _ =>
                            {
                                _pendingGoToNpcId    = npcIdForGo;
                                _pendingGoToTargetId = targetId;
                                _pendingGoToPlayer   = cp;
                                _pendingGoToNpc      = cn;
                            };
                        }
                    }
                }
            }

            // 효과 정보를 GameObject에 첨부 → OnEventEnd에서 읽음
            var holder         = evtGO.AddComponent<BranchEffectHolder>();
            holder.effects     = effects ?? new List<SEEffect>();
            holder.playerActor = playerActor;
            holder.npcActor    = npcActor;

            NarrativeEvent evt = evtGO.AddComponent<NarrativeEvent>();
            evt.event_id       = eventId;
            evt.trigger_type   = NarrativeEventType.Manual;
            evt.trigger_limit  = 0;

            _dialogueActive = true;
            if (pauseGameDuringDialogue) TheGame.Get()?.PauseScripts();

            _startingEvent = true;
            NarrativeManager.Get().StartEvent(evt, playerActor, npcActor);
            _startingEvent = false;
        }

        private ActorData ResolveActorData(string actorId, Actor npcActor, Actor playerActor)
        {
            if (string.IsNullOrEmpty(actorId))          return npcActor?.data;
            if (npcActor?.data?.actor_id == actorId)    return npcActor.data;
            if (playerActor?.data?.actor_id == actorId) return playerActor.data;
            foreach (Actor a in Actor.GetAll())
                if (a.data?.actor_id == actorId) return a.data;
            return npcActor?.data;
        }

        // ── 이벤트 종료 → 효과 실행 ──────────────────────────────────

        private void OnEventEnd(NarrativeEvent evt)
        {
            if (evt?.gameObject == null || !evt.gameObject.name.StartsWith("_dtree_")) return;

            // _startingEvent 중 호출: StartEvent() 내부의 StopEvent() 인터럽트 → 효과 미적용
            if (!_startingEvent)
            {
                var holder = evt.GetComponent<BranchEffectHolder>();
                if (holder != null)
                    ApplyEffects(holder.effects, holder.playerActor, holder.npcActor);

                // 스토리이벤트였으면 on_end 라이프사이클 적용
                if (_currentSEMEvent != null)
                {
                    StoryEventManager.Instance?.ApplyOnEnd(_currentSEMEvent);
                    _currentSEMEvent = null;
                }

                if (pauseGameDuringDialogue) TheGame.Get()?.UnpauseScripts();
                _dialogueActive = false;

                if (_pendingGoToTargetId != null)
                {
                    string pid = _pendingGoToNpcId, tid = _pendingGoToTargetId;
                    Actor pp = _pendingGoToPlayer, pn = _pendingGoToNpc;
                    _pendingGoToNpcId = _pendingGoToTargetId = null;
                    _pendingGoToPlayer = _pendingGoToNpc = null;
                    if (!TryStartIdleEntryById(pid, tid, pp, pn))
                    {
                        SEMatchedEvent tgt = StoryEventManager.Instance?.TryGetEventById(tid);
                        if (tgt != null) StartEvent(tgt.id, tgt.lines, tgt.effects, pp, pn);
                        else Debug.LogWarning($"[DQW] go_to '{tid}' — idle entries·story_events 모두에서 찾을 수 없음.");
                    }
                }
            }

            Destroy(evt.gameObject);
        }

        private void ApplyEffects(List<SEEffect> effects, Actor playerActor, Actor npcActor)
        {
            if (effects == null || effects.Count == 0) return;
            PlayerCharacter player = PlayerCharacter.GetFirst();

            foreach (var effect in effects)
            {
                switch (effect.type)
                {
                    case "give_item":
                        if (player != null && !string.IsNullOrEmpty(effect.itemId))
                        {
                            ItemData item = ItemData.Get(effect.itemId);
                            if (item != null) player.Inventory.GainItem(item, effect.quantity);
                            else Debug.LogWarning($"[DQW] give_item: ItemData '{effect.itemId}' 없음");
                        }
                        break;

                    case "start_fq_quest":
                        Debug.Log($"[DQW] start_fq_quest '{effect.questId}' / QM={FarmingQuest.QuestManager.Instance}");
                        FarmingQuest.QuestManager.Instance?.StartQuest(effect.questId);
                        break;

                    case "receive_fq_reward":
                        FarmingQuest.QuestManager.Instance?.ReceiveReward(effect.questId);
                        break;

                    case "set_npc_status":
                    {
                        var pdata = PlayerData.Get();
                        if (pdata != null && !string.IsNullOrEmpty(effect.npcId))
                        {
                            pdata.npc_status[effect.npcId] = effect.value ?? "";
                            pdata.Save();
                        }
                        break;
                    }

                    case "set_npc_flag":
                    {
                        // 하위 호환 유지
                        var pdata = PlayerData.Get();
                        if (pdata != null && !string.IsNullOrEmpty(effect.npcId) && !string.IsNullOrEmpty(effect.flag))
                        {
                            if (!pdata.npc_flags.ContainsKey(effect.npcId))
                                pdata.npc_flags[effect.npcId] = new Dictionary<string, bool>();
                            pdata.npc_flags[effect.npcId][effect.flag] = true;
                            pdata.Save();
                        }
                        break;
                    }

                    case "go_to_event":
                        Debug.LogWarning($"[DQW] go_to_event '{effect.eventId}' — effects에서의 이벤트 점프는 현재 미지원. 선택지의 go_to 사용 권장.");
                        break;

                    case "open_shop":
                    {
                        string npcId = !string.IsNullOrEmpty(effect.npcId)
                            ? effect.npcId
                            : npcActor?.data?.actor_id;
                        NPCShopData shopData = npcId != null ? NPCShopManager.Get()?.GetShopData(npcId) : null;
                        if (shopData == null) { Debug.LogWarning($"[DQW] open_shop: '{npcId}' 상점 데이터 없음"); break; }
                        ShopPanel.Get()?.ShowShop(player, shopData);
                        break;
                    }

                    default:
                        Debug.LogWarning($"[DQW] 알 수 없는 effect 타입: '{effect.type}'");
                        break;
                }
            }
        }

        // ── 세이브/로드 헬퍼 ─────────────────────────────────────────

        private static void ReloadDQ()
        {
            NarrativeData.Unload();
            LoadDQ();
        }

        private static void NewDQ()
        {
            PlayerData pdata = PlayerData.Get();
            if (pdata != null)
            {
                NarrativeData.Unload();
                NarrativeData.NewGame(pdata.filename);
            }
        }

        private static void LoadDQ()
        {
            PlayerData pdata = PlayerData.Get();
            if (pdata != null)
                NarrativeData.AutoLoad(pdata.filename);
        }

        private void SaveDQ(string filename)
        {
            if (NarrativeData.Get() != null && !string.IsNullOrEmpty(filename))
                NarrativeData.Save(filename, NarrativeData.Get());
        }

        // ── 오디오 / 타임스탬프 브릿지 ───────────────────────────────

        private void OnPauseGameplay()   => TheGame.Get().PauseScripts();
        private void OnUnpauseGameplay() => TheGame.Get().UnpauseScripts();
        private void OnPlaySFX(string channel, AudioClip clip, float vol = 0.8f)   => TheAudio.Get().PlaySFX(channel, clip, vol);
        private void OnPlayMusic(string channel, AudioClip clip, float vol = 0.4f) => TheAudio.Get().PlayMusic(channel, clip, vol);
        private void OnStopMusic(string channel) => TheAudio.Get().StopMusic(channel);
        private float GetTimestamp() => TheGame.Get().GetTimestamp();

#endif

    }
}
