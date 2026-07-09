# Hoe / Soil / Planting Logic Review

괭이로 땅을 갈고, 그 위치에 씨앗을 심을 수 있게 되는 흐름을 정리한 문서입니다.

## 핵심 흐름

현재 구현은 "기존 Ground 오브젝트의 상태를 바꾸는 방식"이 아니라, 괭이 사용 위치에 `Soil` 프리팹을 새로 생성하는 방식입니다.

1. `ActionHoe.DoAction()`
   - 파일: `Assets/FarmingEngine_study/Scripts/Actions/ActionHoe.cs`
   - 플레이어가 바라보는 방향 `hoe_range` 앞 위치를 계산합니다.
   - `PlayerCharacterHoe.HoeGround(pos)`를 호출합니다.

2. `PlayerCharacterHoe.HoeGround()`
   - 파일: `Assets/FarmingEngine_study/Scripts/Player/PlayerCharacterHoe.cs`
   - `CanHoe()`로 에너지, 괭이 장착 여부, busy 상태를 확인합니다.
   - 0.8초 busy/애니메이션 후 `hoe_soil` Construction을 생성합니다.
   - `Buildable.CheckIfCanBuild()`가 통과하면 `FinishBuild()`로 실제 토양을 확정합니다.

3. `HoeSoil.asset`
   - 파일: `Assets/FarmingEngine_study/Resources/Constructions/HoeSoil.asset`
   - `construction_prefab`이 `Assets/FarmingEngine_study/Prefabs/Terrain/Soil.prefab`을 가리킵니다.

4. 씨앗 심기
   - 파일: `Assets/FarmingEngine_study/Scripts/Actions/ActionPlant.cs`
   - 씨앗 아이템의 `plant_data`를 통해 `PlayerCharacterCraft.BuildItemBuildMode()`로 들어갑니다.
   - `Plant.CreateBuildMode()`로 식물 프리뷰를 만들고, 최종 배치 전 `Buildable.CheckIfCanBuild()`를 검사합니다.

## 심기 가능 판정 방식

씨앗을 심을 수 있는지 여부는 `Soil` 컴포넌트를 직접 찾는 방식이 아닙니다.

`Buildable.floor_layer` 기반의 물리 레이어 판정입니다.

- `ProjectSettings/TagManager.asset`
  - Layer 6: `Floor`
  - Layer 7: `Soil`

- `Soil.prefab`
  - `Buildable.floor_layer.m_Bits = 64`
  - 즉, `Floor` 레이어 위에만 괭이 토양을 만들 수 있습니다.

- 작물 프리팹 예: `CarrotPlantS1.prefab`
  - `Buildable.floor_layer.m_Bits = 128`
  - 즉, `Soil` 레이어 위에만 작물을 심을 수 있습니다.

정리하면 다음 구조입니다.

```text
Ground(Floor layer)
  -> 괭이 사용
  -> Soil.prefab 생성(Soil layer)
  -> 씨앗/작물은 Soil layer 위에서만 배치 가능
```

## 이상하거나 확인 필요한 점

### 1. 괭이 내구도 감소 흐름이 사용 방식마다 다를 수 있음

`ActionHoe.DoAction()`은 `HoeGround()` 호출 후 성공 여부와 관계없이 장비 내구도를 1 깎습니다.

```csharp
hoe?.HoeGround(pos);

InventoryItemData ivdata = character.EquipData.GetInventoryItem(slot.index);
if (ivdata != null)
    ivdata.durability -= 1;
```

반면 `PlayerCharacterHoe.Update()`에서 `IsPressAttack()`으로 직접 `HoeGround()`를 호출하는 흐름에서는 내구도 감소가 보이지 않습니다.

확인할 점:
- 실제 플레이에서 괭이 사용이 `ActionHoe` 경로인지, `PlayerCharacterHoe.Update()` 경로인지 확인 필요
- 같은 괭이질인데 입력 방식에 따라 내구도 감소가 다르면 버그 가능성 있음
- 실패한 괭이질에서도 내구도가 깎이는 것이 의도인지 확인 필요

### 2. 이미 갈린 땅을 다시 괭이질하면 토양을 삭제함

`PlayerCharacterHoe.HoeGround()`에는 같은 위치에 이미 `hoe_soil`이 있고 식물이 없으면 기존 토양을 삭제하는 로직이 있습니다.

```csharp
if (prev != null && plant == null && prev.data == hoe_soil)
{
    prev.Destroy();
    return;
}
```

즉 괭이질이 토양 생성/제거 토글처럼 동작할 수 있습니다.

확인할 점:
- 의도된 동작이면 괜찮음
- 의도하지 않았다면 플레이어가 실수로 밭을 지울 수 있음
- 삭제 시 퀘스트 이벤트 `onTilledSoil`은 호출되지 않음

### 3. `ActionHoe`와 `PlayerCharacterHoe`의 타겟 위치 계산이 다름

`ActionHoe`는 플레이어가 바라보는 방향만 사용합니다.

```csharp
character.transform.position + character.GetFacing() * hoe_range
```

`PlayerCharacterHoe`의 프리뷰/공격키 흐름은 마우스 위치를 기준으로 `hoe_range` 안쪽 목표 위치를 잡습니다.

```csharp
Vector3 mouse_world = PlayerControlsMouse.Get().GetPointingPos();
Vector3 dir = mouse_world - char_pos;
dir = dir.normalized * hoe_range;
```

확인할 점:
- UI 액션으로 쓰는 괭이질과 공격키 괭이질의 목표 위치가 다를 수 있음
- 사용자가 보는 프리뷰 위치와 실제 `ActionHoe` 실행 위치가 어긋날 가능성 있음

### 4. 심기 가능 여부가 레이어/콜라이더 설정에 강하게 의존함

씨앗 심기 가능 판정은 `Soil` 컴포넌트 자체가 아니라 `Buildable.floor_layer`와 Raycast 결과에 의존합니다.

따라서 다음 설정이 틀어지면 작물이 안 심어질 수 있습니다.

- `Soil.prefab`의 레이어가 `Soil`인지
- 작물 프리팹의 `Buildable.floor_layer`가 `Soil` 레이어인지
- `Soil.prefab`에 Raycast가 맞을 수 있는 Collider가 있는지
- `build_ground_dist`, `build_obstacle_radius`, `build_flat_floor` 값이 너무 빡빡하지 않은지

### 5. 토양 생성 성공 여부를 외부에서 알기 어렵다

`HoeGround()`는 `void`입니다.

그래서 `ActionHoe` 쪽에서는 실제 토양이 생성됐는지, 이미 있던 토양을 제거했는지, 실패했는지 알 수 없습니다.

이 때문에 내구도 감소, 사운드, 퀘스트 이벤트 같은 후처리를 정확히 나누기 어렵습니다.

개선 후보:

```csharp
public enum HoeResult
{
    None,
    CreatedSoil,
    RemovedSoil,
    Failed
}
```

또는 최소한 `bool HoeGround(Vector3 pos)`로 성공 여부를 반환하게 바꾸는 방법이 있습니다.

## 관련 파일

- `Assets/FarmingEngine_study/Scripts/Actions/ActionHoe.cs`
- `Assets/FarmingEngine_study/Scripts/Player/PlayerCharacterHoe.cs`
- `Assets/FarmingEngine_study/Scripts/Gameplay/Buildable.cs`
- `Assets/FarmingEngine_study/Scripts/Gameplay/Soil.cs`
- `Assets/FarmingEngine_study/Scripts/Gameplay/Plant.cs`
- `Assets/FarmingEngine_study/Scripts/Player/PlayerCharacterCraft.cs`
- `Assets/FarmingEngine_study/Resources/Constructions/HoeSoil.asset`
- `Assets/FarmingEngine_study/Prefabs/Terrain/Soil.prefab`
- `Assets/FarmingEngine_study/Prefabs/Plants/*Plant*.prefab`
- `ProjectSettings/TagManager.asset`

---

# Bakery System Review

오븐에서 빵을 만드는 베이커리 시스템을 확인한 내용입니다.

## 핵심 흐름

현재 제빵은 오븐 오브젝트 자체가 생산 상태를 관리하는 방식이 아닙니다.

`BakingPanel` UI가 레시피 선택, 재료 차감, 타이머, 결과물 지급까지 대부분 담당합니다.

1. `ActionBake.DoAction()`
   - 파일: `Assets/FarmingEngine_study/Scripts/Actions/ActionBake.cs`
   - 선택한 오브젝트에 `BakeryOven`이 있으면 `BakingPanel.Get().ShowBaking(character, oven)`을 호출합니다.

2. `BakeryOven`
   - 파일: `Assets/FarmingEngine_study/Scripts/Gameplay/BakeryOven.cs`
   - 현재는 `UniqueID`와 static list 정도만 갖고 있습니다.
   - 오븐별 진행 상태, 작업 큐, 완료품 저장 같은 로직은 없습니다.

3. `BreadRecipeData`
   - 파일: `Assets/FarmingEngine_study/Scripts/Data/BreadRecipeData.cs`
   - `Resources/Bakery/Recipes` 아래의 `BreadRecipeData` 에셋을 로드합니다.
   - `required_items` 배열에 같은 아이템을 여러 번 넣어서 필요 수량을 표현합니다.

4. `BakingPanel`
   - 파일: `Assets/FarmingEngine_study/Scripts/UI/BakingPanel.cs`
   - 레시피 목록 표시
   - 재료 보유 여부 확인
   - 굽기 시작 시 재료 차감
   - `bake_duration` 동안 타이머 진행
   - 완료 후 결과 아이템을 플레이어 인벤토리에 지급

## 현재 데이터

현재 실제 런타임에서 쓰는 레시피는 `StreamingAssets/bakery_recipes.json`이 아니라 `Resources/Bakery/Recipes/*.asset`입니다.

확인된 레시피:

- `basic_bread`
  - 밀가루 2 + 계란 1
  - 기본 식빵 2개
  - 25초

- `croissant`
  - 밀가루 2 + 버터 1 + 계란 1
  - 크루아상 3개
  - 40초

- `tomato_bread`
  - 밀가루 1 + 토마토 2 + 계란 1
  - 토마토 빵 2개
  - 35초

## 이상하거나 확인 필요한 점

### 1. 제빵 진행 상태가 UI 패널에 묶여 있음

현재 `BakingPanel` 안에 아래 상태가 들어 있습니다.

```csharp
private bool is_baking = false;
private float bake_timer = 0f;
private BreadRecipeData pending_result = null;
```

이 구조는 빠르게 구현하기에는 좋지만, 게임 시스템으로는 약합니다.

문제:

- 패널이 닫히면 진행이 멈출 수 있음
- 오븐별로 동시에 굽는 구조가 아님
- 세이브/로드로 진행 상태를 복원할 수 없음
- 씬 전환 시 진행 상태가 날아갈 수 있음

단기 수정:

- 패널이 닫혀도 `BakingPanel.Update()`에서 타이머는 계속 돌게 분리
- `UIPanel.Hide()`는 fade-out 이후 `AfterHide()`에서 GameObject를 비활성화하므로, `BakingPanel.AfterHide()`를 오버라이드해서 비활성화하지 않도록 처리
- `bake_end_time`을 추가해서 `Time.time` 기준으로 완료 시간을 계산하도록 보강
- 이 방식이면 패널이 닫혀 있어도 시간이 흐르고, 다시 열 때 경과 시간이 반영됨

중장기 개선:

- `BakeryOven` 또는 별도 `BakeryManager`가 진행 상태를 관리
- `PlayerData`에 오븐 UID별 베이킹 상태 저장

### 2. `Scene_Farm_01`에 `BakingPanel`이 중복 존재함

`Assets/Scenes/Game/Scene_Farm_01.unity`에서 `BakingPanel` 컴포넌트가 두 번 검색됩니다.

문제:

- `BakingPanel`은 static `_instance` 싱글톤을 사용합니다.
- 씬에 패널이 2개면 어떤 인스턴스가 최종 `_instance`가 될지 Awake 순서에 의존합니다.
- 버튼 이벤트, 레시피 리스트, 결과물 출력 UI가 서로 다른 패널을 참조할 수 있습니다.

확인/수정 필요:

- 씬에서 실제로 필요한 `BakingPanel` 하나만 남기기
- `BakingPanel.Awake()`에서 중복 인스턴스 경고 또는 중복 제거 처리

처리 상태:

- 2026-07-08: `DQUICanvas` 쪽에 추가되어 있던 중복 `BakingPanel` 서브트리를 제거했습니다.
- `UICanvas` 쪽 `BakingPanel`만 남기는 방향으로 정리했습니다.
- `BakingPanel.Awake()`에 중복 인스턴스 방어 코드를 추가했습니다. 이미 `_instance`가 있으면 새 중복 패널은 경고를 남기고 비활성화합니다.

### 3. 닫았다가 다시 열 때 완료품 표시가 사라질 수 있음

기존 `ShowBaking()`은 열 때마다 아래 초기화를 실행했습니다.

```csharp
ClearRightPanel();
SetOutputVisible(false);
```

문제:

- 이미 빵이 완성되어 `pending_result`가 남아 있어도 출력 슬롯이 숨겨질 수 있습니다.
- 사용자는 빵이 완성됐는데 가져갈 수 없는 상태로 보일 수 있습니다.

수정 방향:

- `pending_result`가 있으면 레시피/진행률/완료품 UI를 복원해야 합니다.
- 아무 작업이 없을 때만 오른쪽 패널을 초기화해야 합니다.

### 4. 베이킹 완료 이벤트가 없음

퀘스트 시스템의 `QuestDatabase`에는 `BakeRecipe` objective 타입이 등록되어 있습니다.

하지만 실제 `BakingPanel.FinishBaking()` 또는 `OnClickOutput()`에서 `FarmingEvents`로 베이킹 이벤트를 발행하지 않습니다.

문제:

- "빵 굽기" 퀘스트를 만들어도 진행되지 않을 가능성이 큽니다.

개선 후보:

```csharp
public static Action<string, int> OnRecipeBaked; // recipeId, amount
```

그리고 완료 시점 또는 수령 시점에 이벤트를 발행합니다.

### 5. `StreamingAssets/bakery_recipes.json`이 런타임에서 사용되지 않음

파일은 존재하지만 현재 코드 검색 기준으로 로드하는 곳이 없습니다.

문제:

- 웹 에디터나 외부 데이터 연동용으로 보이지만 실제 게임 데이터와 분리되어 있습니다.
- JSON을 수정해도 게임 레시피에는 반영되지 않습니다.

정리 필요:

- JSON을 제거하거나
- JSON을 ScriptableObject로 동기화하는 에디터 도구를 만들거나
- 런타임 레시피 로딩을 JSON 기반으로 바꾸는 방향 중 하나를 선택해야 합니다.

### 6. 진열대 시스템은 코드만 있고 배치/완성도가 낮음

관련 파일:

- `Assets/FarmingEngine_study/Scripts/Gameplay/BreadDisplayShelf.cs`
- `Assets/FarmingEngine_study/Scripts/UI/DisplayShelfPanel.cs`

확인 결과:

- 현재 씬/프리팹 검색에서 `BreadDisplayShelf`가 배치된 흔적은 보이지 않았습니다.
- `DisplayShelfPanel` 생성 메뉴는 있지만 슬롯 연결은 "Inspector에서 직접 연결 필요" 상태입니다.

추가 문제:

`BreadDisplayShelf.Update()`가 매 프레임 진열 아이템을 전부 삭제하고 다시 생성합니다.

```csharp
void Update()
{
    RefreshDisplayItems();
}
```

이 방식은 오브젝트 생성/삭제와 Material 생성이 계속 발생해서 성능과 GC 문제가 큽니다.

개선 후보:

- 인벤토리 변경 시점에만 갱신
- 이전 표시 상태를 캐싱해서 바뀐 슬롯만 갱신
- 생성한 Material 재사용 또는 SpriteRenderer 방식 검토

## Bakery 관련 파일

- `Assets/FarmingEngine_study/Scripts/Actions/ActionBake.cs`
- `Assets/FarmingEngine_study/Scripts/Gameplay/BakeryOven.cs`
- `Assets/FarmingEngine_study/Scripts/UI/BakingPanel.cs`
- `Assets/FarmingEngine_study/Scripts/Data/BreadRecipeData.cs`
- `Assets/FarmingEngine_study/Scripts/Editor/BakingPanelSetupEditor.cs`
- `Assets/FarmingEngine_study/Scripts/Editor/BakeryItemSetupEditor.cs`
- `Assets/FarmingEngine_study/Scripts/Gameplay/BreadDisplayShelf.cs`
- `Assets/FarmingEngine_study/Scripts/UI/DisplayShelfPanel.cs`
- `Assets/FarmingEngine_study/Resources/Bakery/Recipes/*.asset`
- `Assets/FarmingEngine_study/Resources/Bakery/Items/*.asset`
- `Assets/StreamingAssets/bakery_recipes.json`
