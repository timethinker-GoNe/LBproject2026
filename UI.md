# UI 작업 세션 노트

## 현재 UI 구조 (UICanvas.prefab)

### 퀵슬롯 바 (InventoryPanel)
- GameObject 경로: `UICanvas/PlayerUI/Inventory`
- 현재 슬롯 수: 15개 (Slot0 ~ Slot14)
- 목표 슬롯 수: 10개 (Slot0 ~ Slot9)
- 슬롯 크기: 80×80px, 패널 Width: 1240px → 10개 기준 820px
- `FullInventoryPanel.slot_offset = 10` (코드 이미 맞춰져 있음)

### 풀 인벤토리 팝업 (FullInventoryPanel)
- `I` 키로 열고 닫음
- 슬롯 동적 생성 (slot_count = 30개, 50×50px)
- slot_offset(10) 이후의 인벤토리 인덱스를 표시
- 키보드 내비게이션: WASD/화살표, Space/Enter 선택, Q=퀵슬롯 이동

### 주요 패널 목록
| 패널 클래스 | 역할 | 열기 |
|---|---|---|
| InventoryPanel | 하단 퀵슬롯 바 | 항상 표시 |
| FullInventoryPanel | 풀 인벤토리 팝업 | I 키 |
| BagPanel | 장착 가방 슬롯 | 자동 (가방 착용 시) |
| StoragePanel | 창고/상자 | 상호작용 |
| CraftPanel | 제작 창 | C 키 |
| EquipPanel | 장비 창 | 별도 |

## 작업 규칙 (이 세션)
- prefab YAML 직접 편집 금지 → Unity Editor 조작 지시만
- CLAUDE.md 대신 이 파일만 참조
- 코드 변경은 .cs 파일만
