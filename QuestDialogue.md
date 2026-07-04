# QuestDialogue.md

퀘스트/대화 작업 전 참고용 짧은 메모.

## 현재 구조

- 완전 JSON 기반이 아님.
- `DialogueQuests` 패키지의 ScriptableObject 시스템 위에 JSON 래퍼를 얹은 상태.
- 런타임 핵심 래퍼:
  - `Assets/FarmingEngine_study/Scripts/Tools/DialogueQuestsWrap.cs`
- 텍스트 로컬라이저:
  - `Assets/FarmingEngine_study/Scripts/Tools/DialogueLocalizer.cs`

## 원본 시스템

- 퀘스트 정의:
  - `Assets/DialogueQuests/Scripts/Data/QuestData.cs`
  - `Assets/FarmingEngine_study/Data/Quests/*.asset`
- 액터 정의:
  - `Assets/DialogueQuests/Scripts/Data/ActorData.cs`
  - `Assets/FarmingEngine_study/Data/Actor/*.asset`
- 실행/저장:
  - `NarrativeManager.cs`
  - `NarrativeData.cs`
- 로더:
  - `Assets/DialogueQuests/Scripts/TheLoader.cs`
  - `Resources/Actors`, `Resources/Quests`에서 ScriptableObject 로드

## JSON 파일

- `Assets/StreamingAssets/Dialogue/quest_config.json`
  - 퀘스트 step/objective/effect 정의
  - 현재 목표 이벤트: `onTilledSoil`, `onPlantedSeed`, `onWateredPlant`, `onHarvestedPlant`

- `Assets/StreamingAssets/Dialogue/dialogue_tree.json`
  - NPC별 대화 진입점과 조건 분기 정의
  - `actor_events`: actor_id -> event_id
  - `events`: event_id -> branches

- `Assets/StreamingAssets/Dialogue/ko.json`
  - 텍스트 키 -> 한국어 문장

- `Assets/StreamingAssets/Dialogue/events_manifest.json`
  - 웹 UI용 이벤트 목록 메타데이터
  - active/planned 섞여 있음

## 대화 흐름

1. NPC 상호작용.
2. `DialogueQuestsWrap.StartDialogueTree()`.
3. `dialogue_tree.json`에서 actor_id 진입 이벤트 찾음.
4. branch 조건 평가.
5. 런타임에 임시 `NarrativeEvent`, `DialogueMessage`, `DialogueChoice` 생성.
6. `NarrativeManager.StartEvent()` 실행.
7. 이벤트 종료 후 `quest_config.event_effects` 처리.

## 퀘스트 흐름

1. 대화 effect에서 `start_quest`.
2. `QuestData.Get(quest_id)`로 ScriptableObject 찾음.
3. `NarrativeManager.StartQuest()`.
4. `OnQuestStart()`가 JSON 첫 step 설정.
5. 농사 이벤트 발생.
6. `OnFarmingEvent()`가 JSON objective와 비교.
7. value 증가.
8. 목표 충족 시 다음 step 또는 complete.

## 중요한 제약

- JSON에 퀘스트만 추가하면 안 됨. 현재는 대응 `QuestData` asset도 필요.
- JSON에 NPC만 추가하면 안 됨. 대응 `ActorData`와 씬/프리팹 Actor 연결 필요.
- `quest_config.json`의 일부 필드는 아직 런타임에서 거의 안 씀.
  - `start_event`, `complete_events`, step `title`, objective `label` 등
- 대화 조건 타입은 제한적.
  - `quest_step`
  - `quest_active`
  - `quest_complete`
  - `quest_not_started`
- 퀘스트 목표 이벤트는 현재 농사 4개만 직접 연결됨.
- `asset_server.py`는 아직 퀘스트/대화 JSON 편집 API 없음.

## 앞으로 방향

- JSON을 작성 원천으로 만들기.
- ScriptableObject는 가능하면 런타임 실행 엔진/등록 껍데기로만 쓰기.
- 우선순위:
  1. JSON 스키마 정리
  2. JSON 퀘스트를 런타임 `QuestData`로 등록
  3. 이벤트 연결 확장
  4. `asset_server.py`에 Dialogue JSON API 추가
  5. 웹 편집 UI 추가

## 주의

- ScriptableObject 시스템을 한 번에 제거하지 말 것.
- 기존 `NarrativeManager`, `NarrativeData`, `QuestPanel`은 최대한 재사용.
- UTF-8 JSON은 정상. PowerShell 출력만 한글이 깨져 보일 수 있음.
