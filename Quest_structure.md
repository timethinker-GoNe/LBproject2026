# Quest_structure.md

퀘스트 시스템 구현 전략 메모. 필요한 것만.

## 핵심 결정사항

### 네임스페이스
- 모든 새 파일: `namespace FarmingQuest`
- 이유: `DialogueQuests.Data.QuestData`, `DialogueQuests.Data.ConditionData` 와 이름 충돌 방지
- using 없이 쓸 때는 `FarmingQuest.QuestData`로 명시

### 파일 위치
- `Assets/FarmingEngine_study/Scripts/Quest/`

### 기존 시스템과 관계
- `NarrativeManager` / `DialogueQuests` 패키지 → 대화 UI 담당, 그대로 유지
- 새 `QuestManager` → JSON 퀘스트 데이터/상태 담당, 독립적으로 운영
- 브릿지: NPC 대화 완료 시 `GameEvents.OnNpcTalked` 발행 (ActionNPCDialogue 또는 DialogueQuestsWrap에서)
- 농사 이벤트 브릿지: `ActionHarvest`, `ActionWaterPlant` 등에서 `GameEvents` 발행

### 세이브 연동
- `PlayerData.cs`에 `List<QuestSaveData> questSaveData` 필드 추가
- 기존 SaveTool 흐름 그대로 활용

---

## 파일 목록 및 역할

| 파일 | 역할 |
|------|------|
| `QuestStatus.cs` | enum 정의 |
| `QuestJsonModels.cs` | JSON 역직렬화용 데이터 클래스들 (QuestDefinition, ConditionData, ObjectiveData, RewardData) |
| `QuestProgress.cs` | 플레이어 진행 상태 (QuestProgress, ObjectiveProgress, QuestSaveData) |
| `QuestDatabase.cs` | JSON 로드 + 검증, Dictionary<string, QuestDefinition> 관리 |
| `ConditionChecker.cs` | 조건 타입별 검사 로직 |
| `ObjectiveProcessor.cs` | 목표 타입별 진행도 갱신 |
| `RewardProcessor.cs` | 보상 타입별 지급 |
| `QuestManager.cs` | 싱글톤, 전체 오케스트레이션 |

> GameEvents.cs 별도 생성 안 함. 기존 `FarmingEvents.cs`를 확장해서 단일 이벤트 허브 유지.
> QuestDefinition/ConditionData/ObjectiveData/RewardData는 JSON 모델이므로 한 파일에 묶음.

---

## 구현 순서

1. `FarmingEvents.cs` 확장 — `OnNpcTalked`, `OnItemCollected` 추가 (허브 단일화)
2. `QuestStatus.cs` + `QuestJsonModels.cs` + `QuestProgress.cs` — 데이터 클래스
3. `QuestDatabase.cs` — JSON 로드 + validation
4. `ConditionChecker.cs`, `ObjectiveProcessor.cs`, `RewardProcessor.cs`
5. `QuestManager.cs` — 위 모든 것 조합, FarmingEvents 구독
6. **브릿지 연결** — ActionNPCDialogue/ActionTake 등에서 FarmingEvents 발행 추가
7. **세이브 연결** — PlayerData 구조 확인 후 questSaveData 필드 추가
8. UI — NPC 퀘스트 마크, 퀘스트 로그 패널 (나중에)

---

## JSON 파일 위치

- `Assets/StreamingAssets/Dialogue/quest_data.json` — 퀘스트 원본 정의
- 기존 `quest_config.json`은 구 시스템용, 새 시스템은 `quest_data.json` 사용

---

## 지원할 조건/목표/보상 타입

**[1차: 실제 연결 가능한 것만]**

조건:
- `QuestCompleted` — QuestManager.GetStatus() 조회 ✓
- `HasItem` — PlayerCharacterInventory.CountItem() ✓
- `HasGoldAtLeast` — PlayerCharacterData.gold ✓
- `PlayerLevelAtLeast` — PlayerCharacterData.GetLevel(targetId), targetId=레벨카테고리ID ✓
- `DayAtLeast` — PlayerData.day ✓
- `HasRecipe` — PlayerCharacterData.IsIDUnlocked(targetId) ✓

목표 (FarmingEvents 기반):
- `TillSoil` → `FarmingEvents.onTilledSoil`
- `PlantSeed` → `FarmingEvents.onPlantedSeed`
- `WaterPlant` → `FarmingEvents.onWateredPlant`
- `HarvestCrop` → `FarmingEvents.onHarvestedPlant`
- `CollectItem` → `FarmingEvents.OnItemCollected` (추가 예정)
- `TalkToNpc` → `FarmingEvents.OnNpcTalked` (추가 예정)

보상:
- `Gold` — PlayerData.gold
- `Item` — PlayerCharacterInventory
- `StartQuest` — QuestManager.StartQuest()

**[stub 처리: switch default에서 LogWarning, 나중에 구현]**
- 조건: `HasRecipe`, `NpcFriendshipAtLeast`, `DayAtLeast`, `SeasonIs`, `ShopLevelAtLeast`, `ReputationAtLeast`, `EnterLocation`
- 목표: `BakeRecipe`, `SellItem`, `DeliverItem`, `ReachLocation`, `DefeatEnemy`, `UpgradeShop`, `RaiseReputation`, `CompleteQuest`
- 보상: `Exp`, `UnlockRecipe`, `NpcFriendship`, `Reputation`, `UnlockArea`, `UnlockShopFeature`

---

## FarmingEvents 브릿지 계획

기존 4개는 이미 발행 중. 추가만 하면 됨.

| 이벤트 | 현황 | 발행 위치 |
|--------|------|-----------|
| `onTilledSoil` | 기존 | PlayerCharacterHoe.cs |
| `onPlantedSeed` | 기존 | PlayerCharacterHoe.cs |
| `onWateredPlant` | 기존 | ActionWaterPlant.cs |
| `onHarvestedPlant` | 기존 | ActionHarvest.cs |
| `OnItemCollected` | **추가** | ActionTake.cs, ActionCollectProduct.cs |
| `OnNpcTalked` | **추가** | ActionNPCDialogue.cs 또는 DialogueQuestsWrap.cs |

---

## 주의사항

- `QuestManager`에 특정 quest id 하드코딩 절대 금지
- JSON 로드 시 validation 필수 (중복 id, 누락 targetId, 알 수 없는 type 등)
- `QuestDefinition`(불변) / `QuestProgress`(변경가능) 반드시 분리
- 구 시스템 이중 상태 방지: `quest_data.json`의 퀘스트 ID는 NarrativeManager 퀘스트 ID와 겹치지 않게 관리 (접두사 구분 등)
- 세이브: PlayerData 구조 확인 후 결정. 기본 방향은 PlayerData에 questSaveData 추가 (A안)
