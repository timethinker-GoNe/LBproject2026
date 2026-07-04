# hansol 브랜치 UI 레이아웃 개선 정리

작성: 2026-06-20  
목적: 01_16_Final 메인에 선택적 머지 전 내용 검토용

---

## 개요

hansol 브랜치에서 UI 레이아웃 시스템을 전면 재설계했다.  
핵심 의도는 **"Unity Inspector를 건드리지 않고, JSON + 웹 에디터만으로 모든 인벤토리 패널의 레이아웃을 제어"** 하는 것.  
퀵바, 이퀴프먼트, 인벤토리백, 창고, 크래프트, 샵 등 각 패널이 독립된 레이아웃 설정을 갖고, 웹(project_structure.html)에서 값을 바꾸면 Unity 런타임에서 슬롯 위치가 자동 재배치된다.

---

## 변경 의도와 장점

### 1. 단일 패딩 → 4방향 패딩 분리
**이전**: `pad` 하나로 상하좌우 동일 패딩  
**이후**: `padLeft`, `padRight`, `padTop`, `padBottom` 개별 제어

퀵바처럼 위쪽은 여백이 필요하고 아래쪽은 붙어야 하는 경우, 단일 패딩으로는 처리가 불가능했다.  
4방향 분리로 패널별 시각 디자인을 JSON 한 줄 수정으로 맞출 수 있다.

### 2. GridLayoutGroup 의존성 제거
**이전**: Unity의 `GridLayoutGroup` 컴포넌트가 있으면 그 설정을 바꾸고, 없으면 수동 계산 — 두 경로가 공존해 동작이 일관되지 않았다.  
또한 GLG가 붙어 있는 슬롯의 `sizeDelta`는 GLG가 덮어쓰기 때문에 `localScale` 보정 계산이 필요했다.  
**이후**: GLG 분기 완전 제거, 순수 수동 `anchoredPosition` 계산만 사용.  
`sizeDelta = (cellSize, cellSize)` 직접 대입 — scale 보정 불필요.

### 3. Flex 정렬 (justify-content / align-items)
**이전**: 슬롯 그리드가 항상 컨테이너 중앙에 배치  
**이후**: `slotJustify` (행 방향: start / center / end)와 `slotAlign` (열 방향: start / center / end) 지원

마지막 행이 꽉 차지 않을 때 start로 두면 왼쪽 정렬, center이면 가운데 정렬.  
CSS flexbox와 동일한 개념으로, JSON 값만 바꿔서 패널별 정렬을 자유롭게 조정.

### 4. 행별 개별 justify 계산
마지막 행의 슬롯 수가 cols보다 적을 때 그 행만의 실제 폭을 따로 계산해서 justify 적용.  
이전 방식(전체 그리드 폭 기준 중앙)으로는 마지막 행이 어긋나 보이는 문제가 있었다.

### 5. InventoryPanel (퀵바) 런타임 구조 자동 생성
**이전**: 씬에 BackgroundFrame / SlotContainer 오브젝트를 직접 배치해야 했음  
**이후**: `EnsureQuickbarStructure()`가 Awake 시점에 자식 오브젝트 계층을 자동 생성  
- `BackgroundFrame` (Image, raycastTarget=false)
- `SlotContainer` (슬롯들의 부모)
- UILayoutConfig에서 bgSprite / slotSprite를 읽어 스프라이트 자동 적용

씬 편집 없이 JSON + 스프라이트 파일만으로 퀵바 외관 변경 가능.

### 6. FullInventoryPanel 설정 확장 및 안정화
- `gridPad` 단일값 → `slotPadLeft/Right/Top/Bottom` 4방향
- `slotFlex` 토글: false이면 UISlotPanel의 자동 레이아웃을 건너뜀 (FullInventoryPanel은 자체 레이아웃 담당이므로 중복 실행 방지)
- `SanitizeConfig()`: 외부에서 읽어온 JSON 값이 비정상 범위일 때 클램핑
- 스프라이트 경로 탐색을 StreamingAssets뿐만 아니라 `Assets/FarmingEngine_study/Sprites/UI/`에서도 시도
- "SlotGrid" → "SlotContainer" 이름 통일 (퀵바와 동일한 이름 규칙)
- `RectMask2D` 자동 추가 (슬롯이 패널 영역을 벗어날 때 클리핑)

### 7. UILayoutConfig 확장 (JSON 읽기 단일 진입점)
새로 추가된 메서드:
- `TryGetSlotLayout(key, ..., padLeft, padRight, padTop, padBottom)` — 4방향 패딩 반환
- `IsSlotFlexEnabled(key)` — 패널별 flex 레이아웃 활성화 여부
- `TryGetSlotAlignment(key, out justify, out align)` — justify/align 반환
- `TryGetPanelSprites(key, out bgSprite, out slotSprite)` — 패널/슬롯 스프라이트 경로 반환

### 8. project_structure.html 웹 에디터 연동
웹 에디터의 UI 디자인 관리 탭에서:
- 각 패널의 `padLeft/Right/Top/Bottom`, `slotFlex`, `slotJustify`, `slotAlign` 실시간 편집
- 저장 시 `Assets/StreamingAssets/UIDesignConfig.json` 갱신 → Unity 재생 시 자동 반영
- 패널 별 bgSprite / slotSprite 경로 설정

지원 패널 목록 (한쪽에 flex 설정이 추가된 항목):
```
quickbar, equip, bag, storage, craft, craftsub, shop, mixing, action
```

---

## 영향 받는 파일 목록

| 파일 | 변경 종류 | 복사본 |
|------|-----------|--------|
| `Assets/FarmingEngine_study/Scripts/UI/UILayoutConfig.cs` | 수정 | `UILayoutConfig_hansol.cs` |
| `Assets/FarmingEngine_study/Scripts/UI/UISlotPanel.cs` | 수정 | `UISlotPanel_hansol.cs` |
| `Assets/FarmingEngine_study/Scripts/UI/InventoryPanel.cs` | 수정 | `InventoryPanel_hansol.cs` |
| `Assets/FarmingEngine_study/Scripts/UI/FullInventoryPanel.cs` | 수정 | `FullInventoryPanel_hansol.cs` |
| `project_structure.html` | 수정 (492KB, 복사 생략) | — |

> HTML은 너무 커서 복사 생략. 웹 에디터 관련 변경은 Farming_hansol/project_structure.html 직접 참조.

---

## 머지 시 확인 포인트

1. **본 브랜치(01_16)에서 UISlotPanel을 건드린 게 있다면** — `ApplySlotLayout()` 내부 로직이 크게 달라졌으므로 충돌 가능. 특히 GLG 분기 코드가 있었는지 확인.

2. **InventoryPanel 상속 구조** — hansol에서 퀵바 전용 `EnsureQuickbarStructure()`를 추가했는데, 01_16에서 InventoryPanel을 상속받는 다른 클래스가 있다면 Awake 체인 확인 필요.

3. **UIDesignConfig.json 스키마** — hansol에서 각 패널 항목에 `padLeft/Right/Top/Bottom`, `slotFlex`, `slotJustify`, `slotAlign`, `bgSprite`, `slotSprite` 키가 추가됨. 01_16의 기존 JSON에는 이 키가 없으므로 기본값 폴백(`?? pad` 등)은 처리되어 있으나, 웹 에디터와 연동하려면 HTML도 같이 반영해야 함.

4. **FullInventoryPanel.cs의 `ShouldApplyAutoSlotLayout()` 오버라이드** — `UISlotPanel`에 추가된 가상 메서드로, FullInventoryPanel이 `false`를 반환해서 자동 레이아웃을 막는다. 두 파일을 같이 머지해야 의도대로 동작.

5. **ItemSlot.GetDisplaySprite() 제거** — hansol에서 ItemSlot의 `GetDisplaySprite()`, `override_sprite` 필드 제거됨. 본 브랜치에서 이 메서드를 참조하는 코드가 있다면 `item.icon`으로 교체 필요.

---

## 핵심 코드 diff 요약

### UISlotPanel.cs — ApplySlotLayout 변경 요점

```
// 이전: GLG 분기 + 단일 패딩 + 중앙 고정 + StretchToFill
int cols; float gap, pad;
TryGetSlotLayout(layoutKey, out cols, out gap, out pad)
float cellW = (contW - pad * 2f - gap * (cols - 1)) / cols;
// GridLayoutGroup 있으면 GLG 설정, 없으면 anchoredPosition 계산
// 아이콘 StretchToFill 추가 실행

// 이후: 단일 경로 + 4방향 패딩 + flex 정렬 + 행별 justify
int cols; float gap, pad, padLeft, padRight, padTop, padBottom;
TryGetSlotLayout(layoutKey, ..., padLeft, padRight, padTop, padBottom)
string justify, align;
TryGetSlotAlignment(layoutKey, out justify, out align);
float contentW = contW - padLeft - padRight;
float contentH = contH - padTop - padBottom;
// 행별 실제 슬롯 수 계산 → 행마다 startX 개별 계산
float rowGridW = rowSlotCount * cellSize + (rowSlotCount-1) * gap;
float startX = padLeft + GetFlexOffset(contentW, rowGridW, justify);
float startY = padTop  + GetFlexOffset(contentH, gridH,  align);
```

### UILayoutConfig.cs — 새 메서드

```csharp
// 추가된 메서드
TryGetSlotLayout(..., out padLeft, out padRight, out padTop, out padBottom)
IsSlotFlexEnabled(panelKey, defaultValue = true)
TryGetSlotAlignment(panelKey, out justify, out align)
TryGetPanelSprites(panelKey, out bgSprite, out slotSprite)
```

### InventoryPanel.cs — 퀵바 구조 자동 생성

```csharp
void EnsureQuickbarStructure()
// layoutKey == "quickbar" 일 때만 실행
// BackgroundFrame (Image) + SlotContainer 자동 생성
// 슬롯들을 SlotContainer 하위로 이동
// UILayoutConfig.TryGetPanelSprites() 로 스프라이트 적용
```
