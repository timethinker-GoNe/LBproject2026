# UI 작업 히스토리

---

## [2026-05-03] 퀵슬롯 바 15개 → 10개 축소

### 목표
하단 퀵슬롯 바 슬롯 수를 15개에서 10개로 줄이기

### 작업 내용

**Unity Editor 작업:**
- `UICanvas > PlayerUI > Inventory` 하위 Slot10~Slot14 GameObject 삭제
- `InventoryPanel` 컴포넌트 `Slots` 배열 Size → 10
- `Inventory` RectTransform Width → 820px
- Slot0~Slot9 Pos X 재배치 (중앙정렬, 80px 간격):
  - Slot0: -360 / Slot1: -280 / Slot2: -200 / Slot3: -120 / Slot4: -40
  - Slot5: 40 / Slot6: 120 / Slot7: 200 / Slot8: 280 / Slot9: 360

**코드 변경 없음** — `FullInventoryPanel.cs`의 `slot_offset = 10`이 이미 맞춰져 있었음

### 트러블슈팅

| 증상 | 원인 | 해결 |
|------|------|------|
| 패널 오른쪽에 슬롯 5개 유령처럼 떠있음 | Slot10~14 GameObject가 삭제됐지만 `slots` 배열 Size를 10으로 안 줄임 → 오브젝트가 Hierarchy에 잔존 | `slots` Size 10으로 수정 + Hierarchy에서 직접 삭제 |
| 슬롯 0~2가 패널 왼쪽 밖으로 삐져나옴 | 슬롯이 절대좌표(AnchoredPosition) 고정이라 너비 변경만으로 위치 안 바뀜 | 슬롯 Pos X 수동 재배치 |
| Inspector에 Axe 아이콘 / Value 99 표시 | 이전 테스트 상태가 프리팹에 스냅샷으로 저장된 것 — 런타임에 SetSlot()이 덮어씀 | 무시해도 됨 |
| 런타임에 Slot9만 도끼/99 표시 | 이전 테스트 저장 파일(player.farming)에 인벤토리 9번 인덱스 데이터 잔존 | Application.persistentDataPath의 .farming 파일 삭제 |

### 관련 파일
- `Assets/FarmingEngine_study/Prefabs/UI/UICanvas.prefab`
- `Assets/FarmingEngine_study/Scripts/UI/InventoryPanel.cs`
- `Assets/FarmingEngine_study/Scripts/UI/FullInventoryPanel.cs` (slot_offset = 10)

---

## [2026-05-03] 드래그 고스트 일반화 + 크로스 패널 드래그 수정

### 목표
- 드래그 고스트를 모든 인벤토리 패널에 적용
- 퀵슬롯 바 → FullInventoryPanel 크로스 드래그 이동 수정

### 작업 내용

**드래그 고스트 일반화 (`ItemSlotPanel.cs`):**
- `FullInventoryPanel`에 있던 고스트 로직을 `ItemSlotPanel` 베이스 클래스로 이동
- `OnGhostDragStart` / `OnGhostDragEnd` / `UpdateGhostPosition` 세 메서드 추가
- 고스트 부모를 씬 내 `sortingOrder` 가장 높은 루트 Canvas로 설정 → 모든 패널 위에 렌더
- 위치 추적: `ScreenPointToWorldPointInRectangle` + `transform.position` 사용 (anchoredPosition 방식의 캔버스 스케일 문제 회피)

**크로스 패널 드래그 수정 (`UISlot.cs`):**
- `GetNearestActive()`에서 `WorldToCanvasPos(slot.transform.position)` → `ScreenPointToCanvasPos(screen_pos)` 로 교체
- 원인: Screen Space Overlay UI의 `transform.position`은 스크린 픽셀 좌표인데, `WorldToCanvasPos`는 3D 게임 카메라 변환을 거쳐 완전히 다른 좌표를 반환

**패널 닫힘 중 인벤토리 null 방지 (`FullInventoryPanel.cs`):**
- `InitPanel()` 오버라이드 추가 — 패널이 닫혀있어도 `SetInventory()` 호출 보장

### 트러블슈팅

| 증상 | 원인 | 해결 |
|------|------|------|
| 드래그 고스트가 패널 밖에서 작아짐 | 고스트가 패널 자식으로 붙어 좌표 계산이 패널 로컬 공간 기준 | 루트 Canvas에 부모 설정 + world position 추적으로 변경 |
| 고스트가 FullInventoryPanel에 가려짐 | FullInventoryPanel이 SaveUI_Canvas(sort 100)에 있어 UICanvas보다 위에 렌더 | 모든 Canvas 중 최고 sort order에 고스트 부모 설정 |
| 퀵슬롯→인벤토리창 드래그 이동 안됨 | `GetNearestActive`가 3D 카메라 좌표 변환으로 슬롯 위치를 완전히 잘못 계산 | `ScreenPointToCanvasPos` 직접 사용으로 교체 |

### 관련 파일
- `Assets/FarmingEngine_study/Scripts/UI/ItemSlotPanel.cs`
- `Assets/FarmingEngine_study/Scripts/UI/UISlot.cs`
- `Assets/FarmingEngine_study/Scripts/UI/FullInventoryPanel.cs`

---

## [2026-05-03] FullInventoryPanel 디자인 리뉴얼 + 월드 이동 금지

### 목표
- FarmingEngine 스타일(오렌지-브라운 나무 판자)로 인벤토리 창 디자인 통일
- 인벤토리 창 열릴 때 마우스 월드 이동 금지

### 작업 내용

**FullInventoryPanel 디자인 (`FullInventoryPanel.cs`):**
- `BuildVisuals()` 메서드 추가: 패널 배경 Image, 헤더 바, 타이틀 텍스트, 닫기 버튼, 구분선을 코드로 생성
- 슬롯 배경: 어두운 단색 `(0.12, 0.12, 0.12)` → 따뜻한 베이지 `(0.80, 0.60, 0.28)` 로 변경
- 슬롯 크기: 50×50 → 54×54px
- Inspector에서 스프라이트 3종 할당 가능: `panelBgSprite`(Panel_big.png), `slotBgSprite`(Slot.png), `closeBtnSprite`(Button_close.png)

**월드 이동 금지 (`TheUI.cs`):**
- `IsBlockingPanelOpened()`에 `FullInventoryPanel.IsAnyVisible()` 추가
- 영향 범위: 마우스 클릭 이동(PlayerControlsMouse), 카메라 드래그(TheCamera), 월드 호버(RaycastSelectables) 세 곳 동시 차단

### 관련 파일
- `Assets/FarmingEngine_study/Scripts/UI/FullInventoryPanel.cs`
- `Assets/FarmingEngine_study/Scripts/UI/TheUI.cs`
- `Assets/FarmingEngine_study/Sprites/UI/Panel_big.png` / `Slot.png` / `Button_close.png`
