# 대화 시스템 오버홀 — 작업 현황 (2026-05-17)

> 재개 시 이 파일을 먼저 읽고, `QuestDialogue.md` · `Quest_structure.md` 도 함께 참고할 것.

---

## 목표

NarrativeManager 기반 구 퀘스트·대화 시스템을 제거하고,  
새 **FarmingQuest** 퀘스트 시스템 + **이벤트 중심 대화 시스템**으로 완전히 교체한다.

---

## 완료된 작업

### 새 퀘스트 시스템 (FarmingQuest) — 완료
- `Scripts/Quest/` 아래 전체 구현 완료
  - `QuestStatus.cs` · `QuestJsonModels.cs` · `QuestProgress.cs`
  - `QuestDatabase.cs` · `ConditionChecker.cs` · `ObjectiveProcessor.cs`
  - `RewardProcessor.cs` · `QuestManager.cs`
- `PlayerData.cs`에 `quest_progress` / `npc_flags` 필드 추가
- `FarmingEvents.cs`에 `OnCropHarvested` · `OnItemCollected` · `OnNpcTalked` 추가
- `ActionHarvest.cs` · `PlayerCharacterInventory.cs` · `DialogueQuestsWrap.cs` 이벤트 연결
- `quest_data.json` — fq_tutorial_001 / fq_tutorial_002 예제 작성 (giverNpcId: "grandma")
- 유니티 씬에 빈 오브젝트 + QuestManager 컴포넌트 배치 완료

### 웹 편집기 (project_structure.html) — 완료
- 대화 탭 하위에 서브탭 4개:
  - ✏️ 대화 편집기 (기존)
  - 📋 퀘스트 편집기 (quest_data.json CRUD)
  - 🗺️ 퀘스트 구조 (참조용 시각 문서)
  - 🗣️ 이벤트-대화 구조 (새 시스템 설계 시각화)

---

## 미완료 — 다음 재개 시 해야 할 것

### 1순위: DialogueQuestsWrap.cs 리라이트

현재 `DialogueQuestsWrap.cs`는 구 시스템(NarrativeManager)과 신 시스템이 혼재한다.  
아래 내용으로 교체해야 한다.

**제거 대상:**
- `quest_config.json` 로딩 (`LoadQuestConfig()` 및 관련 구조체)
- `OnFarmingEvent()` — 구 퀘스트 스텝 진행 로직
- `IsBranchConditionMet()` — NarrativeManager `QuestData` 기반 조건 체크
- `OnQuestStart()` — NarrativeManager 퀘스트 스텝 설정

**유지·수정 대상:**
- `LoadDialogueTree()` — 파일명·구조 변경에 맞게 수정
- `StartDialogueTree()` · `StartTreeEvent()` — 구조 유지
- `OnEventEnd()` — effects를 quest_config.json이 아닌 branch.effects에서 읽도록 변경
- `InitActor()` — 유지

**신규 추가:**
- `IsBranchConditionMet()` — FarmingQuest.QuestManager 기반으로 재작성
  - `fq_not_started` / `fq_available` / `fq_in_progress` / `fq_completed` / `fq_rewarded`
  - `npc_flag` — `PlayerData.npc_flags[npc_id][flag]` 체크
- effect 처리에 `set_npc_flag` · `go_to_event` 추가
- 농업 이벤트 구독 (`onTilledSoil` 등) 제거 — QuestManager가 직접 처리

### 2순위: dialogue_tree.json 재작성

**새 스키마:**
```json
{
  "npcs": {
    "grandma": { "entry_event": "grandma_talk" }
  },
  "events": {
    "grandma_talk": {
      "branches": [
        {
          "id": "offer_tutorial",
          "condition": { "type": "fq_not_started", "quest_id": "fq_tutorial_001" },
          "lines": [
            { "actor": "grandma", "text_key": "farm_01.grandma.intro.start" }
          ],
          "effects": [
            { "type": "start_fq_quest", "quest_id": "fq_tutorial_001" },
            { "type": "give_item", "item_id": "seed_tomato", "quantity": 5 }
          ]
        },
        {
          "id": "reward_001",
          "condition": { "type": "fq_completed", "quest_id": "fq_tutorial_001" },
          "lines": [
            { "actor": "grandma", "text_key": "farm_01.grandma.tutorial.complete" }
          ],
          "effects": [
            { "type": "receive_fq_reward", "quest_id": "fq_tutorial_001" },
            { "type": "start_fq_quest", "quest_id": "fq_tutorial_002" }
          ]
        },
        {
          "id": "reward_002",
          "condition": { "type": "fq_completed", "quest_id": "fq_tutorial_002" },
          "lines": [
            { "actor": "grandma", "text_key": "farm_01.grandma.complete.thanks" }
          ],
          "effects": [
            { "type": "receive_fq_reward", "quest_id": "fq_tutorial_002" }
          ]
        },
        {
          "id": "idle",
          "condition": null,
          "lines": [
            { "actor": "grandma", "text_key": "farm_01.grandma.idle.greet_01" }
          ],
          "effects": []
        }
      ]
    }
  }
}
```

### 3순위: quest_config.json 처리

- `quests` 섹션 완전 제거 (→ quest_data.json으로 이관 완료)
- `event_effects` 섹션도 제거 (→ dialogue_tree.json branches[].effects로 이관)
- 파일 자체를 삭제하거나 빈 `{}` 스텁으로 남기기

### 4순위: PlayerData.cs — npc_flags 필드

`IsBranchConditionMet()`의 `npc_flag` 조건이 필요로 하는 저장소:

```csharp
// PlayerData.cs에 추가
public Dictionary<string, Dictionary<string, bool>> npc_flags
    = new Dictionary<string, Dictionary<string, bool>>();

// FixData()에 추가
if (npc_flags == null)
    npc_flags = new Dictionary<string, Dictionary<string, bool>>();
```

### 5순위: 구 Grandma_Events 씬 오브젝트 비활성화

유니티 씬에서 기존 NarrativeManager 기반 `[Quest]` 오브젝트 (Grandma_Events) Disable.  
새 시스템 정상 작동 확인 후 삭제.

---

## 파일별 최종 상태 목표

| 파일 | 상태 | 비고 |
|---|---|---|
| `Scripts/Quest/*.cs` | ✅ 완료 | 건드리지 않아도 됨 |
| `Scripts/Tools/DialogueQuestsWrap.cs` | ⚠️ 교체 필요 | 핵심 작업 |
| `Scripts/Data/PlayerData.cs` | ⚠️ npc_flags 추가 필요 | 소규모 |
| `StreamingAssets/Dialogue/dialogue_tree.json` | ⚠️ 재작성 필요 | 새 스키마로 |
| `StreamingAssets/Dialogue/quest_config.json` | ⚠️ 제거 예정 | |
| `StreamingAssets/Dialogue/quest_data.json` | ✅ 완료 | fq_tutorial_001/002 |
| `StreamingAssets/Dialogue/ko.json` | ✅ 유지 | 텍스트 키 그대로 사용 |
| `project_structure.html` | ✅ 완료 | 서브탭 4개 |

---

## 새 조건 타입 참조

| type | 통과 조건 | 파라미터 |
|---|---|---|
| `fq_not_started` | QuestStatus == NotStarted | quest_id |
| `fq_available` | QuestStatus == Available | quest_id |
| `fq_in_progress` | QuestStatus == InProgress | quest_id |
| `fq_completed` | QuestStatus == Completed (보상 전) | quest_id |
| `fq_rewarded` | QuestStatus == Rewarded | quest_id |
| `npc_flag` | PlayerData.npc_flags[npc_id][flag] == true | npc_id, flag |
| `null` | 항상 통과 (idle fallback) | — |

## 새 효과 타입 참조

| type | 동작 | 파라미터 |
|---|---|---|
| `give_item` | 아이템 지급 | item_id, quantity |
| `start_fq_quest` | QuestManager.StartQuest() | quest_id |
| `receive_fq_reward` | QuestManager.ReceiveReward() | quest_id |
| `set_npc_flag` | PlayerData.npc_flags 설정 | npc_id, flag |
| `go_to_event` | 다른 이벤트 즉시 시작 | event_id |

---

## 주의사항

- `QuestManager.StartQuest()`는 `startConditions` 미충족 시 자체 거부하므로, 대화 effect에서 중복 호출해도 안전하다.
- 분기 조건은 **위에서 아래 순서**로 평가 — 구체적 조건을 위에, `null` (idle)을 항상 맨 마지막에.
- `fq_tutorial_001/002`의 giverNpcId는 `"grandma"` (ActorData.actor_id 기준).
- 구 시스템 `tutorial_01` (NarrativeManager)과 ID 충돌 없음 — fq_ 접두사 규칙 유지.
