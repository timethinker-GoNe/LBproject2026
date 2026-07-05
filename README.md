# Project-Farm-main
farming engine 개발

## Farming Engine 에디터 메뉴 도구

Unity 메뉴바의 **Farming Engine** 메뉴에서 실행할 수 있는 커스텀 에디터 도구 목록입니다. 대부분 `Assets/FarmingEngine_study/Scripts/Editor/`에 있습니다.

### 씬 빌드 / 패치
- **Build Scenes > Scene_Start / Scene_Intro / Scene_CharCreate / Scene_Farm_01 Only** — 각 씬을 처음부터 새로 생성
- **Build Scenes > Build All Scenes** — 위 4개 씬을 한 번에 모두 빌드
- **Patch Scene > Farm01 - [Quests] / [Characters] / [Managers] 재빌드** — 기존 씬에서 해당 그룹만 삭제 후 재생성 (수동 수정 내역은 유지)
- **Patch PausedPanel Buttons** — UICanvas 프리팹의 일시정지 패널 버튼을 Resume/Save/Load/Back to Menu 구성으로 교체

### 씬 맵 (웹 에디터 연동)
- **Scene Map > 씬 맵 내보내기 (Export → JSON)** — 현재 씬의 오브젝트 좌표를 `StreamingAssets/scene_config.json`으로 내보냄 (DEV_EDITOR 미니맵 연동용)
- **Scene Map > 씬 맵 가져오기 (Import ← JSON)** — JSON에 정의된 좌표대로 프리팹을 씬에 스폰
- **Scene Map > 프리팹 썸네일 내보내기** — 프리팹 미리보기 이미지를 `StreamingAssets/prefab_thumbnails/`로 생성

### 환경 데코레이션
- **Environment > Scatter Boundary Decoration** — Ground 가장자리를 따라 나무(정사각형 띠)와 수풀을 자동 배치
- **Environment > Clear Boundary Decoration** — 위에서 배치한 나무/수풀 제거
- **Environment > GPU Grass Tool** — `GrassGroundZone` 마커가 붙은 오브젝트 위에 GPU 컴퓨트 셰이더 기반 잔디를 생성/제거하는 도구 창 (밀도·범위 슬라이더 포함)

### 베이커리 시스템
- **Bakery > Create Bread Item Assets** — 제빵 재료/결과물 ItemData 에셋 생성
- **Bakery > Create Sample Recipe Assets** — 샘플 레시피(BreadRecipeData) 에셋 생성
- **Bakery > Setup Baking Panel** — UICanvas 아래에 제빵 UI(BakingPanel) 계층을 자동 구성
- **Bakery > Create Display Shelf Panel** — 빵 진열대 UI 패널 생성

### 퀘스트 UI
- **Setup Quest Mini Tracker** — 화면 좌상단 퀘스트 미니 트래커 UI 생성
- **Setup Quest Journal Panel** — J키로 여는 퀘스트 저널(카드형) UI 생성

### UI 레이아웃 (웹 에디터 지원)
- **Set UI Layout Keys in UICanvas** — 각 UI 패널에 웹 에디터용 레이아웃 키(quickbar, equip, storage 등)를 부여
- **Export Panel BG Sprites to UISprites** — UI 패널 배경 스프라이트를 웹 에디터가 읽을 수 있는 폴더로 내보냄
- **Export Current Slot Layout to JSON** — UICanvas의 실제 GridLayoutGroup 값을 `StreamingAssets/UIDesignConfig.json`에 반영

### 오브젝트 편집 유틸리티
- **Create New Object** — Item/Construction/Plant/Character/Destructible/Selectable 데이터 에셋+프리팹을 한 번에 생성
- **Duplicate Object** — 기존 CraftData 오브젝트와 연결된 에셋을 통째로 복제
- **Replace Prefab** — 씬에서 선택한 오브젝트들을 지정한 프리팹으로 일괄 교체
- **Randomize Objects** — 선택한 오브젝트들의 X/Z 위치를 무작위로 흩어서 배치 패턴이 반복되지 않게 함
- **Transform Group** — 선택한 오브젝트들을 정확한 값만큼 한 번에 이동/회전
- **Align Objects** — 선택한 오브젝트들의 위치를 정수 좌표로 반올림해서 정렬

### UID 관리
- **Generate UIDs** — 씬의 빈 Unique ID를 모두 채우고 중복 UID를 새 값으로 교체
- **Clear UIDs** — 씬의 모든 Unique ID를 초기화 (기존 세이브 파일과 호환 깨짐 주의)

### 세이브 관리
- **Delete Save File** — 에셋 버전 변경으로 인한 세이브 이슈 자동 수정 시도
- **Delete Save File (Full Reset)** — 세이브 파일 전체 삭제
