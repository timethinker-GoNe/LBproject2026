# QA.md

프로젝트에서 발생한 문제와 해결 방법을 기록합니다. 비슷한 증상이 재발하면 여기서 먼저 확인하세요.

---

## PlayerCharacter가 이동은 하는데 애니메이션이 뻣뻣함 (T-포즈처럼 굳어있음)

**증상**
- `[Character]/PlayerCharacter`가 이동은 정상적으로 되는데, 걷기/달리기 애니메이션이 재생되지 않고 몸이 뻣뻣한 채로 위치만 이동함.

**원인**
캐릭터 모델과 애니메이션 클립의 **Rig Animation Type이 서로 불일치**했음.
- `Assets/FarmingEngine_study/Models/Character/FarmerBoy.fbx` (캐릭터 메시): `animationType: 2` (**Generic**)
- `Assets/FarmingEngine_study/Models/Character/Anims/character@*.fbx` (idle, run, attack 등 23개 클립 전부): `animationType: 3` (**Humanoid**)

Generic으로 임포트된 모델의 Animator에는 Humanoid 클립이 적용되지 않아 본 애니메이션이 재생되지 않고, 이동은 `Character.cs`의 Rigidbody 물리 이동이라 애니메이션과 무관하게 정상 작동 → "이동은 되는데 뻣뻣함" 증상이 발생함.

참고로 다른 동물(bear, bee 등)은 모델과 애니메이션 클립이 전부 Generic으로 일관되어 있어 정상 작동. FarmerBoy.fbx만 유독 Generic으로 설정되어 있었음 (원본 애셋 패키지 Farming Engine v1.21 기준으로는 원래 Humanoid로 맞춰져 있었을 가능성이 높음).

**검증 방법** (원인 특정 시 확인한 순서)
1. `PlayerCharacterAnim.cs` / `Character.controller`에서 파라미터명(Move, MoveX, MoveZ 등)과 State 전환 조건 확인 → 정상.
2. `TheGame.IsPausedByPlayer()`로 인해 Animator가 꺼지는 버그인지 확인 → 배제 (paused 상태면 이동 자체도 멈춰야 하는데 실제로는 이동이 되고 있었음).
3. 캐릭터 모델(FarmerBoy.fbx)과 그 모델이 쓰는 애니메이션 클립들(character@*.fbx)의 `.meta` 파일에서 `animationType` 값 비교 → 불일치 발견.

**해결 방법**
Unity Editor에서 `FarmerBoy.fbx` 선택 → Inspector Rig 탭 → Animation Type을 **Generic → Humanoid**로 변경 → Apply.
(본 이름이 hips/spine/chest/head/upper_arm.L 등 표준 명명 규칙을 따르고 있어 Auto-Map 성공. Configure 화면에서 매핑이 초록색 체크로 제대로 됐는지 확인 필요)

이 모델은 `PlayerCharacter.prefab`, `PlayerCharacter1PS.prefab`, `PlayerCharacter3PS.prefab`에서만 사용되므로 수정 범위는 좁음.

**관련 파일**
- `Assets/FarmingEngine_study/Models/Character/FarmerBoy.fbx`
- `Assets/FarmingEngine_study/Models/Character/Anims/character@idle.fbx`, `character@run.fbx` 등
- `Assets/FarmingEngine_study/Models/Character/Character.controller`
- `Assets/FarmingEngine_study/Scripts/Player/PlayerCharacterAnim.cs`
- `Assets/FarmingEngine_study/Scripts/Gameplay/Character.cs`

**같은 패턴으로 의심되는 경우**
캐릭터/NPC/동물이 "이동은 되는데 애니메이션이 안 나온다"는 증상이면, 먼저 해당 모델과 그 애니메이션 클립들의 `.meta` 파일에서 `animationType` 값이 서로 같은지부터 확인할 것.
