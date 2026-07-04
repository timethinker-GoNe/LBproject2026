# 퀘스트 UI 현황 (2026-06-01)

## 구현 완료 ✅

### ① QuestBox 토스트 알림
- `QuestNotificationBridge.cs` — FarmingQuest 이벤트 → DQ QuestBox 알림 연결
- `QuestBox.cs` — `ShowBox(string boxText, string questTitle, Sprite icon)` 오버로드 추가
- **동작 확인됨**: 퀘스트 시작/완료 시 화면 상단 알림 표시

---

## 미완료 ❌

### ② 미니 트래커 (우상단 상시 표시)
- `QuestUI.cs` 스크립트 있음
- `SetupQuestUIEditor.cs` → "Farming Engine/Setup Quest Mini Tracker" 메뉴
- **현상**: 우상단에 검은 패널은 보이나 텍스트 없음
- **미확인**: QuestUI 컴포넌트가 Inspector 필드(trackerRoot, questTitleText 등)에 제대로 연결됐는지
- **관련 파일**: `Assets/FarmingEngine_study/Scripts/UI/QuestUI.cs`

### ③ 저널 패널 (J키)
- `FarmingQuestPanel.cs` — 완전히 새로 작성됨 (자체 UI 생성, Inspector 연결 불필요)
- **현재 상태**: FarmingQuestPanel GameObject가 UICanvas 하위에 있으나 **일반 Transform** (RectTransform 아님)
  - 원인: 에디터 스크립트가 `new GameObject()` 로 생성 → Canvas 하위여도 일반 Transform 부여됨
  - `AddComponent<FarmingQuestPanel>()`도 당시 타입 미인식으로 실패 → 컴포넌트 없음
- **해결 방법**:
  1. Hierarchy에서 `UICanvas > FarmingQuestPanel` **삭제**
  2. UICanvas 우클릭 → **UI > Empty** (이렇게 해야 RectTransform 자동)
  3. 이름 `FarmingQuestPanel`로 변경
  4. Inspector RectTransform 앵커 설정:
     - Anchor Min: X=0.6, Y=0.1
     - Anchor Max: X=0.98, Y=0.9
     - Left/Right/Top/Bottom 전부 0
  5. **Add Component → FarmingQuestPanel**
  6. Play Mode → Console에서 `[FQP] Awake 완료` 확인
  7. `[FQP] J키 구독 완료` 확인 → J키로 저널 열리는지 테스트
- **NarrativeControls**: `[UI] > [DQUICanvas] > NarrativeControls` 오브젝트에 붙어있음 (씬에 존재 확인됨)
- **DQ QuestPanel**: `[UI] > [DQUICanvas] > QuestPanel` — 이미 비활성화됨 (건드릴 필요 없음)

---

## 관련 파일 목록

| 파일 | 상태 |
|------|------|
| `Assets/FarmingEngine_study/Scripts/UI/QuestNotificationBridge.cs` | ✅ 완료 |
| `Assets/FarmingEngine_study/Scripts/UI/QuestUI.cs` | ⚠️ 미니 트래커 텍스트 미표시 |
| `Assets/FarmingEngine_study/Scripts/UI/FarmingQuestPanel.cs` | ⚠️ 스크립트는 OK, GameObject 재생성 필요 |
| `Assets/FarmingEngine_study/Scripts/Editor/SetupQuestUIEditor.cs` | ⚠️ RectTransform 버그 수정됨 (new GameObject → typeof(RectTransform) 추가) |
| `Assets/DialogueQuests/Scripts/UI/QuestBox.cs` | ✅ 오버로드 추가됨 |
| `Assets/FarmingEngine_study/Scripts/Quest/QuestManager.cs` | ✅ GetCompletedQuests() 추가됨 |
| `Assets/FarmingEngine_study/Scripts/Editor/ClearSaveEditor.cs` | ✅ 세이브 초기화 메뉴 |

---

## 씬 구조 참고

```
[UI]
  └─ UICanvas               ← FarmingEngine UI 루트
       ├─ QuestUI            ← 미니 트래커 부모 (QuestUI 컴포넌트 붙어야 함)
       │    ├─ Tracker       ← 우상단 패널
       │    └─ Toast         ← 토스트 팝업
       └─ FarmingQuestPanel  ← ⚠️ 현재 망가짐, 재생성 필요

[DQUICanvas]                ← DialogueQuests 시스템 루트
  ├─ DialoguePanel          ← 대화창 (건드리지 말것)
  ├─ QuestNotice
  │    └─ QuestBox          ← ✅ 토스트 알림 사용 중
  └─ QuestPanel             ← 비활성화됨 (대체됨)
```

---

## 새 세션 시작 시 할 일 순서

1. **③ 저널 패널 먼저**: 위 "해결 방법" 5단계 수행 → Play Mode 테스트
2. **② 미니 트래커**: Play Mode에서 QuestUI 컴포넌트 Inspector 필드 연결 확인
3. 전부 동작 확인 후 debug 로그 정리
