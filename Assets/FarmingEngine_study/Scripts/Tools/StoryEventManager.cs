using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if DIALOGUE_QUESTS
using Newtonsoft.Json;
#endif

namespace FarmingEngine
{
    // ── 공유 데이터 모델 (DIALOGUE_QUESTS 의존성 없음) ─────────────────

    /// <summary>SEM이 조건 평가 후 반환하는 스토리이벤트 데이터. DialogueQuestsWrap이 렌더링에 사용.</summary>
    public class SEMatchedEvent
    {
        public string          id;
        public List<SELine>    lines   = new List<SELine>();
        public List<SEEffect>  effects = new List<SEEffect>();
        public List<SELifecycle> onEnd = new List<SELifecycle>();
    }

    public class SELine
    {
        public string actor;
        public string textKey;
        public string type = "normal"; // "normal" | "choice"
        public List<SEChoice> choices; // null = 선택지 없음
    }

    public class SEChoice
    {
        public string textKey;
        public string goTo;  // 선택 시 이동할 이벤트 ID
    }

    public class SEEffect
    {
        public string type;
        public string itemId;
        public string questId;
        public string npcId;
        public string eventId;
        public string flag;
        public string value;
        public int    quantity = 1;
    }

    public class SELifecycle
    {
        public string type;   // "set_npc_status"
        public string npcId;
        public string value;
    }

    // ── StoryEventManager ─────────────────────────────────────────────

    /// <summary>
    /// 스토리이벤트 최상위 레이어.
    /// story_events.json을 로드하고 NPC 클릭 등 트리거 발생 시 조건을 평가해
    /// 매칭 이벤트 데이터를 반환한다. 대화 렌더링은 DialogueQuestsWrap이 담당.
    /// </summary>
    public class StoryEventManager : MonoBehaviour
    {
        public static StoryEventManager Instance { get; private set; }

#if DIALOGUE_QUESTS

        // ── JSON 파싱 전용 내부 클래스 ──────────────────────────────────

        private class JsTrigger
        {
            public string type;
            [JsonProperty("npc_id")] public string npcId;
        }

        private class JsCondition
        {
            public string type;
            [JsonProperty("quest_id")] public string questId;
            [JsonProperty("npc_id")]   public string npcId;
            public string status;
            public string value;
        }

        private class JsLifecycle
        {
            public string type;
            [JsonProperty("npc_id")] public string npcId;
            public string value;
        }

        private class JsChoice
        {
            [JsonProperty("text_key")] public string textKey;
            [JsonProperty("go_to")]    public string goTo;
        }

        private class JsLine
        {
            public string actor;
            [JsonProperty("text_key")] public string textKey;
            public string type = "normal"; // "normal" | "choice"
            public List<JsChoice> choices;
        }

        private class JsEffect
        {
            public string type;
            [JsonProperty("item_id")]  public string itemId;
            [JsonProperty("quest_id")] public string questId;
            [JsonProperty("npc_id")]   public string npcId;
            [JsonProperty("event_id")] public string eventId;
            public string flag;
            public string value;
            public int quantity = 1;
        }

        private class JsEvent
        {
            public string id;
            public List<JsTrigger>   triggers;
            public List<JsCondition> conditions;
            [JsonProperty("on_start")] public List<JsLifecycle> onStart;
            public List<JsLine>   lines;
            public List<JsEffect> effects;
            [JsonProperty("on_end")]   public List<JsLifecycle> onEnd;
        }

        private class JsRoot { public List<JsEvent> events; }

        // ── 상태 ──────────────────────────────────────────────────────

        private JsRoot _data;

        // ── 라이프사이클 ──────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Load();
        }

        private void Load()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Dialogue", "story_events.json");
            if (!File.Exists(path))
            {
                Debug.LogWarning("[SEM] story_events.json not found: " + path);
                return;
            }
            try
            {
                _data = JsonConvert.DeserializeObject<JsRoot>(File.ReadAllText(path));
            }
            catch (System.Exception e)
            {
                Debug.LogError("[SEM] story_events.json 파싱 오류: " + e.Message);
            }
        }

        // ── Public API ────────────────────────────────────────────────

        /// <summary>
        /// npc_talk 트리거 + 조건 평가 후 첫 번째 매칭 이벤트 반환.
        /// 매칭 없으면 null.
        /// </summary>
        public SEMatchedEvent TryGetNpcTalkEvent(string npcId)
        {
            if (_data?.events == null) { Debug.LogWarning("[SEM] story_events 데이터 없음"); return null; }
            foreach (var jsEvt in _data.events)
            {
                if (!HasNpcTalkTrigger(jsEvt.triggers, npcId)) continue;
                bool met = AllConditionsMet(jsEvt.conditions);
                Debug.Log($"[SEM] '{jsEvt.id}' → {(met ? "✓ MATCH" : "✗ SKIP")}");
                if (!met) continue;
                return Convert(jsEvt);
            }
            Debug.Log($"[SEM] '{npcId}' 매칭 이벤트 없음 → 아이들 재생");
            return null;
        }

        /// <summary>ID로 직접 이벤트 조회 (트리거·조건 무시). 선택지 go_to 처리용.</summary>
        public SEMatchedEvent TryGetEventById(string eventId)
        {
            if (_data?.events == null) return null;
            var jsEvt = _data.events.Find(e => e.id == eventId);
            return jsEvt != null ? Convert(jsEvt) : null;
        }

        /// <summary>이벤트 종료 후 on_end 라이프사이클 효과 적용.</summary>
        public void ApplyOnEnd(SEMatchedEvent evt)
        {
            if (evt?.onEnd == null) return;
            foreach (var lc in evt.onEnd)
                ApplyLifecycle(lc);
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────────

        private bool HasNpcTalkTrigger(List<JsTrigger> triggers, string npcId)
        {
            if (triggers == null) return false;
            foreach (var t in triggers)
                if (t.type == "npc_talk" && t.npcId == npcId) return true;
            return false;
        }

        private bool AllConditionsMet(List<JsCondition> conditions)
        {
            if (conditions == null || conditions.Count == 0) return true;
            foreach (var c in conditions)
                if (!IsConditionMet(c)) return false;
            return true;
        }

        private bool IsConditionMet(JsCondition c)
        {
            switch (c.type)
            {
                case "quest_status":
                {
                    var qm     = FarmingQuest.QuestManager.Instance;
                    var status = qm != null ? qm.GetStatus(c.questId) : FarmingQuest.QuestStatus.NotStarted;
                    bool pass  = status.ToString() == c.status;
                    Debug.Log($"[SEM]   quest_status '{c.questId}': 현재={status} / 필요={c.status} → {(pass ? "Pass" : "Fail")}");
                    return pass;
                }
                case "npc_status":
                {
                    var pdata = PlayerData.Get();
                    string cur = (pdata?.npc_status != null &&
                                  pdata.npc_status.TryGetValue(c.npcId ?? "", out var v)) ? v : "";
                    return cur == (c.value ?? "");
                }
                default:
                    Debug.LogWarning($"[SEM] 알 수 없는 조건 타입: '{c.type}'");
                    return false;
            }
        }

        private void ApplyLifecycle(SELifecycle lc)
        {
            switch (lc.type)
            {
                case "set_npc_status":
                {
                    var pdata = PlayerData.Get();
                    if (pdata != null && !string.IsNullOrEmpty(lc.npcId))
                    {
                        pdata.npc_status[lc.npcId] = lc.value ?? "";
                        pdata.Save();
                    }
                    break;
                }
                default:
                    Debug.LogWarning($"[SEM] 알 수 없는 라이프사이클 타입: '{lc.type}'");
                    break;
            }
        }

        private SEMatchedEvent Convert(JsEvent src)
        {
            var result = new SEMatchedEvent { id = src.id };

            foreach (var l in src.lines ?? new List<JsLine>())
            {
                var line = new SELine { actor = l.actor, textKey = l.textKey, type = l.type ?? "normal" };
                if (l.choices != null)
                {
                    line.choices = new List<SEChoice>();
                    foreach (var c in l.choices)
                        line.choices.Add(new SEChoice { textKey = c.textKey, goTo = c.goTo });
                }
                result.lines.Add(line);
            }

            foreach (var e in src.effects ?? new List<JsEffect>())
                result.effects.Add(new SEEffect {
                    type = e.type, itemId = e.itemId, questId = e.questId,
                    npcId = e.npcId, eventId = e.eventId, flag = e.flag,
                    value = e.value, quantity = e.quantity
                });

            foreach (var lc in src.onEnd ?? new List<JsLifecycle>())
                result.onEnd.Add(new SELifecycle { type = lc.type, npcId = lc.npcId, value = lc.value });

            return result;
        }

#endif // DIALOGUE_QUESTS
    }
}
