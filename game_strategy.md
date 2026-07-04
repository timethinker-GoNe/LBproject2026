# Farming Simulation + Bakery Management Project

## 프로젝트 개요

Unity 기반 농장 시뮬레이션 게임 (FarmingEngine 프레임워크). 향후 농작물을 활용한 베이커리 경영 시스템을 추가할 예정.

---

## 현재 아키텍처

### 핵심 시스템

| 시스템 | 클래스 | 방식 |
|--------|--------|------|
| 게임 매니저 | `TheGame`, `TheData`, `TheUI` | 싱글톤 패턴 |
| 농작물 | `Plant.cs` + `PlantData` | ScriptableObject + 프리팹 |
| 인벤토리 | `PlayerCharacterInventory` + `InventoryData` | Dictionary 기반 |
| 액션 | `AAction`, `MAction`, `SAction` | 컴포넌트 패턴 |
| 저장 | `SaveTool` + `PlayerData` | Binary 직렬화 (BinaryFormatter) |
| UI | `TheUI` + `UISlotPanel` 계층 | 패널/슬롯 패턴 |

### 폴더 구조

```
Assets/FarmingEngine_study/Scripts/
├── Actions/      (45+ 액션 클래스)
├── Data/         (ScriptableObject 데이터)
├── Gameplay/     (51+ 게임플레이 스크립트)
├── Player/       (11개 플레이어 관련)
├── UI/           (27+ UI 패널)
└── Tools/        (저장, 네비게이션 등)
```

### 데이터 구조

- 모든 게임 데이터는 `Resources/` 폴더의 ScriptableObject `.asset` 파일
- 저장 파일: `Application.persistentDataPath` / `.farming` 확장자
- `PlayerData`에 농작물, 건설물, 인벤토리, 커스텀 변수 등 전부 포함

---

## 최적화 현황

### ✅ 적용 완료

- **`BinaryFormatter` → `Newtonsoft.Json`**: `SaveTool.cs` (FarmingEngine + DialogueQuests) 양쪽 교체 완료. 기존 바이너리 파일은 자동 마이그레이션 (JSON 파싱 실패 시 binary fallback → 다음 저장 시 JSON으로 전환). `Packages/manifest.json`에 `com.unity.nuget.newtonsoft-json: 3.2.1` 추가됨.
- **`CraftData.Get(id)` Dictionary 캐싱**: `craft_dict` 추가로 O(n) → O(1) 조회. `Load()` 시 동시 구축.

### 🟡 적용 대기

- `Plant.cs` SlowUpdate 0.5초마다 성장 체크 → `onNewDay` 이벤트 기반 전환
- `TheData.cs` 시작 시 전체 ScriptableObject 로드 → Addressables/Lazy Loading 검토

### 🔵 향후 계획 (베이커리 Phase 4 이후)

- 45개 액션 클래스 → 데이터 기반(ScriptableObject 설정) 통합
- UI 포커스 → `UIFocusManager` 상태 머신 중앙화 (BakeryPanel 추가 시점에 함께 도입 권장)
- Deprecated 씬 (`Scenes/Deprecated/`, `Scenes/pre_dev/`) 정리

---

## 베이커리 시스템 개발 로드맵

기존 아키텍처를 최대한 재활용하는 방향으로 설계. 기존 코드를 건드리지 않고 새 클래스 추가만으로 구현 가능.

### Phase 1: 기초 데이터 세팅

기존 `ItemData` / `CraftData` ScriptableObject로 에셋 생성.

```
Resources/Items/
├── wheat_flour.asset
├── egg.asset
├── milk.asset
├── sugar.asset
├── bread.asset      (eat_hp, eat_hunger 설정)
└── cake.asset
```

### Phase 2: 베이커리 오븐 건설물

- `Furnace.cs` 패턴 참고하여 `BakeryOven.cs` 작성
- `ConstructionData`로 에셋 생성
- 레시피는 `CraftData.craft_items[]`로 관리
- `ActionCook` → `ActionBake` 파생

### Phase 3: 베이커리 UI

- `CraftPanel` 패턴으로 `BakeryPanel` 작성
- 레시피 목록 표시, 재료 보유 여부 하이라이트, 베이킹 진행률(ProgressBar) 표시

### Phase 4: 저장 시스템 연동

`PlayerData`에 `BakingOvenData` 추가:

```csharp
[System.Serializable]
public class BakingOvenData
{
    public string uid;
    public string current_item_id;
    public float baking_timer;
    public int baked_quantity;
}

// PlayerData 클래스 내부에 추가
public Dictionary<string, BakingOvenData> baking_ovens;
```

### Phase 5: 베이커리 경영 시스템 (중장기)

- **NPC 손님 시스템**: 기존 `DialogueSystem` 확장 → 주문 퀘스트
- **수요/공급 시스템**: `ShopData` 확장 → 동적 가격 변동
- **평판 시스템**: `PlayerAttributes` 확장 → 베이커리 평점
- **계절/날씨 연동**: 기존 `onNewDay` 이벤트 활용

### 전체 흐름

```
농장 수확 → 재료 획득 → 베이커리 오븐 제작 → 베이킹 → 완성품 → 판매/경영
 (기존)        (기존)       (Phase 1~2)        (Phase 3~4)   (Phase 5)
```

---

## 저장 시스템 구현 전략

### 현재 상태
- PausePanel에서 `TheGame.Get().Save()` 직접 호출 — 슬롯 없음, 항상 "player" 고정 파일명
- `.farming` (FarmingEngine) + `.dq` (DialogueQuests) 두 파일이 `beforeSave` 이벤트로 동기화
- 저장 UI 없음 (시스템만 존재)

### 목표: 3슬롯 저장 시스템

**파일 매핑**: `slot1.farming` + `slot1.dq` / `slot2.farming` + `slot2.dq` / `slot3.farming` + `slot3.dq`

**새로 만들 파일**:
- `SaveSlotInfo.cs` — 슬롯 표시용 데이터 (day, play_time, current_scene, last_save)
- `SaveManager.cs` — 통합 저장/로드 조율 싱글톤 (FarmingEngine + DialogueQuests 동시 처리)
- `SaveSlotPanel.cs` — 슬롯 목록 UI 패널 (UIPanel 상속, Save/Load 두 모드)
- `SaveSlotUI.cs` — 슬롯 한 칸 표시 컴포넌트
- `ConfirmPanel.cs` — Y/N 확인 모달 (재사용 가능)

**수정할 파일**:
- `PausePanel.cs` — OnClickSave/Load를 SaveSlotPanel 호출로 교체 (2줄 변경)
- `UICanvas 프리팹` — SaveSlotPanel, ConfirmPanel 오브젝트 추가

**핵심 원칙**: 기존 `PlayerData.Save()` / `TheGame.Load()` 내부 로직은 그대로 재사용. SaveManager는 얇은 조율 레이어만 추가.

### 저장 흐름 요약
```
Pause → Save 클릭 → SaveSlotPanel → 슬롯 선택 → ConfirmPanel
  → SaveManager.SaveToSlot(i) → TheGame.Save("slotN") → .farming
                               → beforeSave 이벤트 → NarrativeData.Save() → .dq
```

---

---

## 1차 플레이어블 샘플 전략

> **⚠️ 씬 주의사항**: `Desert-Day`, `Desert-Night`, `Sample-Day`, `Sample-Night`, `InHouse` 는 쉐이더 실험용 씬. 게임 씬 목록에서 제외. 앞으로 새 씬을 직접 구축한다.

---

### 씬 구성 (1차 샘플 MVP)

```
Scene_Start        ← 타이틀 / 메인 메뉴 (가장 먼저 로드)
    ↓ 새게임
Scene_Intro        ← 세계관 설명 (텍스트 컷씬)
    ↓
Scene_CharCreate   ← 캐릭터 생성 (이름 / 외형 / 배경)
    ↓
Scene_Farm_01      ← 첫 번째 플레이어블 농장 씬
    ↓ (향후)
Scene_Town_01 / Scene_Bakery_01
```

---

### Scene_Start — 타이틀 스크린

**기술 요구사항:**
- TheGame, TheData 불필요 → ScriptableObject 로드 없음
- `StartScreenManager.cs` 싱글톤 하나만 배치
- `SaveTool` (static) 만으로 슬롯 정보 읽기 가능
- 설정값은 `PlayerPrefs`로 관리 (PlayerData 없는 상태)
- AudioSource 직접 배치 (TheAudio 대신)

**메뉴 항목 및 서브 전략:**

| 메뉴 | 조건 | 동작 |
|------|------|------|
| 새 게임 | 항상 활성 | 아래 흐름 참고 |
| 불러오기 | 세이브 1개 이상 있을 때만 활성 | SaveSlotPanel (Load 모드) |
| 설정 | 항상 활성 | SettingsPanel (오버레이) |
| 나가기 | 항상 활성 | ConfirmPanel → Application.Quit() |

**새 게임 흐름 (슬롯 선택 로직):**
```
새 게임 클릭
  ├─ 빈 슬롯이 있음 → SaveSlotPanel (NewGame 모드)
  │     빈 슬롯: "새 게임" 강조 / 데이터 있는 슬롯: "덮어쓰기 경고"
  │     슬롯 선택 → ConfirmPanel → selectedSlot 에 저장 → Scene_Intro
  └─ 슬롯 전부 차있음 → SaveSlotPanel (동일, 덮어쓰기만 선택 가능)
```

**불러오기 흐름:**
```
불러오기 클릭 → SaveSlotPanel (Load 모드)
  슬롯 선택 → SaveManager.LoadFromSlot(i)
    → PlayerData.current_scene 씬으로 직접 이동 (Intro/CharCreate 생략)
```

**설정 패널 항목:**
- 오디오: 마스터 / 음악 / 효과음 슬라이더 → PlayerPrefs 저장
- 그래픽: 해상도, 품질 레벨 → QualitySettings + PlayerPrefs
- (향후) 언어, 접근성

---

### Scene_Intro — 세계관 인트로

**기술 요구사항:**
- TheGame, TheData 불필요 (초경량 씬)
- `IntroManager.cs` 만 배치
- 텍스트 TypeWriter 효과 + 배경 이미지 순차 표시
- 건너뛰기 버튼 → 즉시 Scene_CharCreate 이동
- 씬 간 전환: `SceneNav.GoTo("Scene_CharCreate")`

**인트로 내용 (세계관 프레임워크):**
```
[1/4] 배경: 황폐해진 작은 마을 '골든크로프트'
      한때 풍요로웠지만, 오랜 가뭄과 이주로 쇠락함.

[2/4] 상황: 플레이어는 돌아가신 할아버지의 낡은 농장을 물려받음
      먼지 쌓인 땅, 녹슨 도구, 빈 창고만 남아있음.

[3/4] 전환점: 마을에 남은 몇 안 되는 주민들이 기다리고 있음
      "이 마을을 다시 살릴 수 있는 건 당신뿐이에요."

[4/4] 목표 암시: 농장을 일구고, 빵집을 열어 마을에 활기를 되돌리자.
      → 시작
```

---

### Scene_CharCreate — 캐릭터 생성

**기술 요구사항:**
- TheGame, TheData 불필요
- `CharCreateManager.cs` 배치
- 생성 완료 시점에 `PlayerData.NewGame(selectedSlot)` 호출
- 캐릭터 데이터는 임시 정적 변수 `CharCreateData` 에 저장 → NewGame 직후 PlayerData에 주입

**생성 단계:**

| 단계 | 내용 | 저장 위치 |
|------|------|-----------|
| 이름 입력 | 텍스트 InputField (기본값: "농부") | `PlayerData.player_name` |
| 외형 선택 | 스프라이트 배열 ← → 탐색 | `PlayerData.appearance_index` |
| 배경 선택 | 3종 카드 선택 | `PlayerData.background_id` |

**배경 선택 (시작 보너스):**
```
🌾 농부 출신  → 씨앗 5종 + 물뿌리개 지급
💰 상인 출신  → 골드 200 추가 + 기본 씨앗 2종
👨‍🍳 요리사 출신 → 요리법 1개 해금 + 기본 씨앗 2종 + 조리도구
```

**PlayerData 확장 필드 (추가 필요):**
```csharp
// PlayerData.cs에 추가
public string player_name = "농부";
public int    appearance_index = 0;
public string background_id = "farmer";  // "farmer" | "merchant" | "chef"
```

---

### Scene_Farm_01 — 첫 번째 농장 씬

**필수 매니저 (씬에 직접 배치):**
- `TheGame` + `TheData` — 게임/데이터 관리
- `TheCamera` + RenderQuad — 카메라
- `NarrativeManager` — 대화/퀘스트
- `DialogueQuestsWrap` — 통합 브릿지
- `SaveManager` — 저장 조율

**씬 초기 구성:**
```
Scene_Farm_01
├── 플레이어 시작 위치 (농장 입구)
├── 경작 가능한 흙 구역 (Soil × 6개)
├── 우물 (물 공급원)
├── 창고 1개 (Storage)
├── 도구 상자 (시작 아이템 지급용)
├── NPC 1명 (옆집 할머니 → 튜토리얼 퀘스트 제공)
└── ExitZone (향후 마을 연결용 — 비활성 상태)
```

**첫 플레이 시 자동 이벤트:**
- 게임 시작 → `onNewDay` + DialogueQuests 튜토리얼 퀘스트 자동 시작
- NPC 할머니 자동 대화: "오셨군요! 먼저 밭을 갈아봐요."
- 퀘스트 1: 씨앗 심기 → 수확

**시작 시간:** Day 1, 06:00 (아침)

---

### 씬 매니저 요약표

| 씬 | TheGame | TheData | NarrativeManager | SaveManager | PlayerData 상태 |
|----|---------|---------|------------------|-------------|-----------------|
| Scene_Start | ❌ | ❌ | ❌ | ❌ (static SaveTool만) | 없음 |
| Scene_Intro | ❌ | ❌ | ❌ | ❌ | 없음 |
| Scene_CharCreate | ❌ | ❌ | ❌ | ❌ | 없음 → 생성됨 |
| Scene_Farm_01 | ✅ | ✅ | ✅ | ✅ | 존재 (로드됨) |

---

### 구현 파일 목록 (1차 샘플)

**새로 만들 스크립트:**
```
Scripts/
├── StartScreen/
│   ├── StartScreenManager.cs    ← 메인 메뉴 진입점
│   └── SettingsPanel.cs         ← 설정 오버레이
├── Intro/
│   └── IntroManager.cs          ← 텍스트 타이프라이터 + 씬 전환
├── CharCreate/
│   ├── CharCreateManager.cs     ← 캐릭터 생성 흐름 관리
│   ├── CharCreateData.cs        ← 임시 정적 데이터 컨테이너
│   └── BackgroundCardUI.cs      ← 배경 선택 카드 UI
└── SaveSystem/
    ├── SaveSlotInfo.cs           ← 슬롯 표시 데이터
    ├── SaveManager.cs            ← 통합 저장/로드 조율
    ├── SaveSlotPanel.cs          ← 슬롯 목록 UI (StartScreen + InGame 공용)
    ├── SaveSlotUI.cs             ← 슬롯 한 칸 컴포넌트
    └── ConfirmPanel.cs           ← Y/N 모달 (범용)
```

**수정할 기존 파일:**
```
PlayerData.cs         ← player_name, appearance_index, background_id 필드 추가
PausePanel.cs         ← OnClickSave/Load → SaveSlotPanel 호출로 교체 (2줄)
UICanvas 프리팹       ← SaveSlotPanel, ConfirmPanel 오브젝트 추가
```

---

## 개발 원칙

- 기존 FarmingEngine 패턴(ScriptableObject, 액션 시스템, 싱글톤 매니저)을 최대한 재활용
- 새 기능은 기존 클래스 수정보다 **새 클래스 추가** 우선
- StartScreen / Intro / CharCreate 씬은 TheGame 없는 경량 씬 — FarmingEngine 매니저 불필요
- Desert/Sample/InHouse 씬은 쉐이더 실험용 — 게임 씬 경로에서 절대 참조하지 않을 것
- 씬 이름 컨벤션: `Scene_[카테고리]_[번호 또는 이름]` (예: `Scene_Farm_01`, `Scene_Town_01`)
