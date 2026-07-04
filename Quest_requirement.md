# Quest Requirement

유니티에서 JSON 기반 퀘스트 시스템을 구현하고 싶다.

목표는 퀘스트별 내용을 C#에 하드코딩하지 않고, JSON 데이터에 정의된 조건, 목표, 보상을 C# 시스템이 해석해서 실행하는 데이터 주도형 Quest System을 만드는 것이다.

## 핵심 설계 원칙

퀘스트 시스템은 다음 요소로 분리한다.

### 1. QuestData

- 퀘스트 원본 데이터
- JSON에서 로드
- 제목, 설명, 시작 조건, 목표, 보상, 다음 퀘스트 정보 등을 포함
- 런타임 중 변경하지 않음

### 2. QuestProgress / QuestSaveData

- 플레이어의 퀘스트 진행 상태
- 현재 진행도, 완료 여부, 보상 수령 여부 등을 저장
- 세이브/로드 대상

### 3. QuestManager

- 퀘스트 데이터 로드
- 퀘스트 상태 관리
- 시작 가능 여부 검사
- 퀘스트 시작, 완료, 보상 수령 처리
- 게임 이벤트를 받아 Objective 진행도 갱신

### 4. ConditionChecker

- JSON에 정의된 조건을 검사
- QuestCompleted, HasItem, HasRecipe, NpcFriendshipAtLeast 같은 조건 타입별 검사 로직 담당

### 5. ObjectiveProcessor

- JSON에 정의된 목표를 처리
- CollectItem, HarvestCrop, BakeRecipe, TalkToNpc 같은 목표 타입별 진행도 갱신 담당

### 6. RewardProcessor

- JSON에 정의된 보상을 지급
- Gold, Item, UnlockRecipe, NpcFriendship, UnlockArea 같은 보상 타입 처리

### 7. GameEvents

- 게임 내 행동을 이벤트로 발행
- 예: 아이템 획득, 작물 수확, 빵 제작, NPC 대화, 지역 진입 등
- QuestManager는 이 이벤트를 구독해서 진행 중인 퀘스트 목표를 갱신한다

## 퀘스트 상태

다음 `QuestStatus` enum을 사용한다.

```csharp
public enum QuestStatus
{
    NotStarted,
    Available,
    InProgress,
    Completed,
    Rewarded,
    Failed
}
```

상태 의미:

- `NotStarted`: 아직 조건을 만족하지 않음
- `Available`: 시작 가능, NPC 퀘스트 마크 표시 가능
- `InProgress`: 플레이어가 수락해서 진행 중
- `Completed`: 목표는 완료했지만 보상은 아직 받지 않음
- `Rewarded`: 보상까지 받은 상태
- `Failed`: 실패 가능한 퀘스트일 경우 사용

## JSON 구조

퀘스트 원본 데이터는 JSON으로 관리한다.

예시:

```json
{
  "quests": [
    {
      "id": "quest_bakery_002",
      "title": "꿀빵의 소문",
      "description": "루시엘에게 꿀빵에 대한 이야기를 들어보자.",
      "giverNpcId": "npc_luciel",
      "visibleConditions": [
        {
          "type": "QuestCompleted",
          "targetId": "quest_bakery_001"
        }
      ],
      "startConditions": [
        {
          "type": "NpcFriendshipAtLeast",
          "targetId": "npc_luciel",
          "value": 3
        }
      ],
      "objectives": [
        {
          "type": "TalkToNpc",
          "targetId": "npc_luciel",
          "requiredAmount": 1
        },
        {
          "type": "BakeRecipe",
          "targetId": "honey_bread",
          "requiredAmount": 1
        }
      ],
      "rewards": [
        {
          "type": "Gold",
          "amount": 100
        },
        {
          "type": "UnlockRecipe",
          "targetId": "cinnamon_roll",
          "amount": 1
        }
      ],
      "nextQuestIds": [
        "quest_bakery_003"
      ]
    }
  ]
}
```

## visibleConditions와 startConditions

퀘스트 시작 조건은 하드코딩하지 말고 JSON으로 관리한다.

퀘스트에는 두 종류의 조건을 둔다.

### visibleConditions

퀘스트가 플레이어에게 보이는 조건이다.

예:

- NPC 머리 위에 퀘스트 마크 표시
- 퀘스트 목록에 등장
- NPC 대화에서 퀘스트 관련 대사 노출

### startConditions

플레이어가 실제로 퀘스트를 수락할 수 있는 조건이다.

예:

- 특정 아이템 보유
- 특정 호감도 이상
- 특정 퀘스트 완료
- 특정 레시피 보유

예시:

```json
{
  "visibleConditions": [
    {
      "type": "QuestCompleted",
      "targetId": "quest_intro_001"
    }
  ],
  "startConditions": [
    {
      "type": "HasItem",
      "targetId": "bakery_license",
      "value": 1
    }
  ]
}
```

## 조건 타입

초기에는 다음 조건 타입을 지원한다.

- `QuestCompleted`: 특정 퀘스트 완료 여부
- `HasItem`: 특정 아이템 보유 여부
- `HasRecipe`: 특정 레시피 보유 여부
- `NpcFriendshipAtLeast`: 특정 NPC 호감도 조건
- `PlayerLevelAtLeast`: 플레이어 레벨 조건
- `DayAtLeast`: 특정 날짜 이후
- `SeasonIs`: 특정 계절
- `EnterLocation`: 특정 지역 진입 여부
- `ShopLevelAtLeast`: 가게 레벨 조건
- `ReputationAtLeast`: 마을/가게 평판 조건

조건은 JSON에서 정의하고, C#은 조건 타입별 검사 로직만 가진다.

나쁜 방식:

```csharp
if (quest.id == "quest_bakery_002")
{
    return IsQuestCompleted("quest_bakery_001")
        && GetFriendship("npc_luciel") >= 3;
}
```

좋은 방식:

```json
"startConditions": [
  {
    "type": "QuestCompleted",
    "targetId": "quest_bakery_001"
  },
  {
    "type": "NpcFriendshipAtLeast",
    "targetId": "npc_luciel",
    "value": 3
  }
]
```

C#에서는 조건 타입별로 처리한다.

```csharp
public static bool IsSatisfied(ConditionData condition)
{
    switch (condition.type)
    {
        case "QuestCompleted":
            return QuestManager.Instance.IsQuestCompleted(condition.targetId);

        case "HasItem":
            return InventoryManager.Instance.GetItemCount(condition.targetId) >= condition.value;

        case "HasRecipe":
            return RecipeManager.Instance.HasRecipe(condition.targetId);

        case "NpcFriendshipAtLeast":
            return NpcManager.Instance.GetFriendship(condition.targetId) >= condition.value;

        case "PlayerLevelAtLeast":
            return PlayerManager.Instance.Level >= condition.value;

        default:
            Debug.LogWarning($"Unknown condition type: {condition.type}");
            return false;
    }
}
```

## 목표 Objective 타입

초기에는 다음 목표 타입을 지원한다.

- `CollectItem`: 아이템 수집
- `HarvestCrop`: 작물 수확
- `BakeRecipe`: 특정 빵/레시피 제작
- `SellItem`: 아이템 또는 빵 판매
- `TalkToNpc`: 특정 NPC와 대화
- `DeliverItem`: 특정 NPC에게 아이템 전달
- `ReachLocation`: 특정 장소 도달
- `DefeatEnemy`: 적 처치
- `UpgradeShop`: 가게 업그레이드
- `RaiseReputation`: 평판 상승
- `CompleteQuest`: 다른 퀘스트 완료

Objective 예시:

```json
"objectives": [
  {
    "type": "HarvestCrop",
    "targetId": "wheat",
    "requiredAmount": 5
  },
  {
    "type": "BakeRecipe",
    "targetId": "plain_bread",
    "requiredAmount": 1
  }
]
```

## 보상 Reward 타입

초기에는 다음 보상 타입을 지원한다.

- `Gold`: 돈 지급
- `Item`: 아이템 지급
- `Exp`: 경험치 지급
- `UnlockRecipe`: 레시피 해금
- `NpcFriendship`: NPC 호감도 증가
- `Reputation`: 평판 증가
- `UnlockArea`: 지역 해금
- `UnlockShopFeature`: 상점 기능 해금
- `StartQuest`: 다음 퀘스트 자동 시작

Reward 예시:

```json
"rewards": [
  {
    "type": "Gold",
    "amount": 100
  },
  {
    "type": "UnlockRecipe",
    "targetId": "honey_bread",
    "amount": 1
  },
  {
    "type": "NpcFriendship",
    "targetId": "npc_luciel",
    "amount": 2
  }
]
```

## 진행 상태 SaveData 구조

퀘스트 원본 데이터와 플레이어 진행 상태는 반드시 분리한다.

`QuestData`는 변경되지 않는 데이터이고, `QuestProgress`는 플레이어마다 달라지는 데이터다.

예시:

```json
{
  "questId": "quest_bakery_001",
  "status": "InProgress",
  "objectiveProgress": [
    {
      "objectiveIndex": 0,
      "currentAmount": 3,
      "completed": false
    },
    {
      "objectiveIndex": 1,
      "currentAmount": 0,
      "completed": false
    }
  ]
}
```

C# 구조 예시:

```csharp
[System.Serializable]
public class QuestProgress
{
    public string questId;
    public QuestStatus status;
    public List<ObjectiveProgress> objectiveProgresses;
}

[System.Serializable]
public class ObjectiveProgress
{
    public int objectiveIndex;
    public int currentAmount;
    public bool completed;
}
```

## 이벤트 기반 진행도 갱신

`QuestManager`를 다른 시스템에서 직접 호출하지 말고, `GameEvents`를 통해 이벤트 기반으로 연결한다.

예:

```csharp
public static class GameEvents
{
    public static Action<string, int> OnItemCollected;
    public static Action<string, int> OnCropHarvested;
    public static Action<string, int> OnRecipeBaked;
    public static Action<string> OnNpcTalked;
    public static Action<string> OnLocationEntered;
}
```

예를 들어 FarmSystem에서 밀을 수확하면:

```csharp
GameEvents.OnCropHarvested?.Invoke("wheat", 1);
```

QuestManager는 이 이벤트를 구독한다.

```csharp
private void OnEnable()
{
    GameEvents.OnCropHarvested += HandleCropHarvested;
    GameEvents.OnItemCollected += HandleItemCollected;
    GameEvents.OnRecipeBaked += HandleRecipeBaked;
    GameEvents.OnNpcTalked += HandleNpcTalked;
    GameEvents.OnLocationEntered += HandleLocationEntered;
}

private void OnDisable()
{
    GameEvents.OnCropHarvested -= HandleCropHarvested;
    GameEvents.OnItemCollected -= HandleItemCollected;
    GameEvents.OnRecipeBaked -= HandleRecipeBaked;
    GameEvents.OnNpcTalked -= HandleNpcTalked;
    GameEvents.OnLocationEntered -= HandleLocationEntered;
}
```

QuestManager는 이벤트를 받으면 현재 `InProgress` 상태인 퀘스트들의 Objective를 검사하고, type과 targetId가 일치하면 진행도를 증가시킨다.

## 전체 작동 흐름

게임 시작:

1. QuestManager가 QuestData.json 로드
2. SaveData에서 QuestProgress 로드
3. 모든 퀘스트의 visibleConditions 검사
4. 조건을 만족한 퀘스트를 Available 상태로 변경
5. NPC 머리 위에 퀘스트 아이콘 표시

플레이어가 NPC와 대화:

1. 해당 NPC가 줄 수 있는 Available 퀘스트 확인
2. startConditions 검사
3. 조건 만족 시 퀘스트 수락
4. QuestProgress 생성
5. 상태를 InProgress로 변경

플레이어가 행동:

1. 아이템 획득, 작물 수확, 빵 제작, NPC 대화, 지역 진입 등 이벤트 발생
2. QuestManager가 이벤트 수신
3. 진행 중인 퀘스트의 Objective와 비교
4. type과 targetId가 맞으면 currentAmount 증가
5. requiredAmount에 도달하면 Objective completed 처리
6. 모든 Objective가 완료되면 QuestStatus를 Completed로 변경

플레이어가 보상 수령:

1. RewardProcessor가 rewards 처리
2. 상태를 Rewarded로 변경
3. nextQuestIds가 있으면 다음 퀘스트의 visible/start 조건 재검사
4. SaveData 저장

## 구현 시 주의할 점

1. QuestManager는 특정 퀘스트 ID에 대한 하드코딩 로직을 가지면 안 된다.

나쁜 예:

```csharp
if (quest.id == "quest_001") { ... }
```

2. QuestManager는 모든 퀘스트를 Dictionary 형태로 관리한다.

```csharp
Dictionary<string, QuestData> questDatabase;
Dictionary<string, QuestProgress> questProgresses;
```

3. JSON에는 퀘스트별 데이터만 들어가고, 실제 시스템 접근은 C# Processor들이 담당한다.

4. 조건, 목표, 보상 타입이 추가될 때만 C# switch 또는 전략 패턴을 수정한다.

5. 퀘스트 데이터와 세이브 데이터는 반드시 분리한다.

6. ObjectiveProgress는 `objectiveIndex` 기준으로 관리한다. 단, 나중에 안정성을 높이려면 `objectiveId`를 추가해도 된다.

7. JSON 오타를 잡기 위해 로드 시 validation을 구현한다.

예:

- 중복 quest id 검사
- 존재하지 않는 nextQuestIds 검사
- 알 수 없는 condition type 검사
- 알 수 없는 objective type 검사
- 알 수 없는 reward type 검사
- targetId 누락 검사
- requiredAmount가 0 이하인지 검사

## 원하는 구현 산출물

다음 C# 파일들을 만든다.

- `QuestStatus.cs`
- `QuestData.cs`
- `ConditionData.cs`
- `ObjectiveData.cs`
- `RewardData.cs`
- `QuestProgress.cs`
- `ObjectiveProgress.cs`
- `QuestDatabase.cs`
- `QuestManager.cs`
- `ConditionChecker.cs`
- `ObjectiveProcessor.cs`
- `RewardProcessor.cs`
- `GameEvents.cs`

가능하면 다음도 포함한다.

- `quest_data.json` 예시 파일
- `quest_save_data.json` 예시 구조
- JSON 로딩 코드
- 기본 validation 코드
- 간단한 테스트용 이벤트 호출 예시
- NPC가 `giverNpcId` 기준으로 Available 퀘스트를 찾는 메서드

## 최종 목표

최종적으로 아래와 같은 방식으로 퀘스트를 추가할 수 있어야 한다.

1. JSON에 새 퀘스트 추가
2. visibleConditions, startConditions, objectives, rewards 작성
3. 유니티 실행
4. QuestManager가 JSON을 로드
5. 조건을 만족하면 퀘스트가 Available
6. 플레이어가 수락하면 InProgress
7. 이벤트 발생에 따라 진행도 증가
8. 목표 완료 시 Completed
9. 보상 지급 후 Rewarded

즉, 대부분의 퀘스트 추가는 C# 수정 없이 JSON 수정만으로 가능해야 한다.

## 핵심 요약

JSON에는 퀘스트별 조건/목표/보상을 정의하고, C#에는 조건/목표/보상 타입별 해석 로직만 둔다.

`QuestManager`는 특정 퀘스트 내용을 몰라야 하며, 이벤트 기반으로 진행도를 갱신하고, `QuestData`와 `QuestProgress`는 반드시 분리한다.
