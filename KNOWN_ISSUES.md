# Known Issues & Fix History

발생한 경고/에러와 해결 기록. 같은 문제 재발 시 여기서 먼저 확인.

---

## ✅ [2026-04-11] FarmingEngine.Editor 네임스페이스 충돌

**에러 메시지**
```
Assets\FarmingEngine_study\Scripts\Editor\GrassCircleEditor.cs(13,38):
error CS0118: 'Editor' is a namespace but is used like a type
```

**원인**  
`SceneBuilder.cs`에 `namespace FarmingEngine.Editor`를 선언하면서  
`FarmingEngine` 하위에 `Editor`라는 네임스페이스가 생성됨.  
같은 `FarmingEngine` 하위에 있는 `GrassCircleEditor.cs`가 `UnityEditor.Editor`를  
상속하려 할 때 컴파일러가 `FarmingEngine.Editor`(네임스페이스)를 먼저 찾아 충돌.

**해결**  
`SceneBuilder.cs` 네임스페이스를 `FarmingEngine.SceneTools`로 변경.  
> 규칙: `FarmingEngine.Editor`는 절대 사용 금지. `FarmingEngine.EditorTool` 또는 `FarmingEngine.SceneTools` 사용.

**수정 파일**  
`Assets/FarmingEngine_study/Scripts/Editor/SceneBuilder.cs`

---

## ✅ [2026-04-11] URPWater 반사 카메라 MSAA 키워드 경고

**에러 메시지**
```
Local keyword _MSAA_2X doesn't exist in the shader.
Local keyword _MSAA_8X doesn't exist in the shader.
Trying to draw gizmos while inside a renderpass. This is not supported, gizmo drawing will be skipped.
```

**원인**  
`URPWaterReflection.cs`가 반사 카메라를 렌더링할 때 메인 카메라를  
`dest.CopyFrom(src)`로 복사 → MSAA 설정 상속.  
URP가 `_MSAA_8X` 등의 키워드를 셰이더에 설정하려 하지만  
URPWater 셰이더가 해당 키워드를 `#pragma`로 선언하지 않아 경고 발생.  
(Gizmo 경고도 같은 반사 카메라 렌더 패스 내 발생)

**해결**  
`UpdateCamera()` 내부에서 반사 카메라의 MSAA를 명시적으로 비활성화.

```csharp
// URPWaterReflection.cs - UpdateCamera() 내부
dest.CopyFrom(src);
dest.useOcclusionCulling = false;
dest.allowMSAA = false; // 추가 — _MSAA_nX 키워드 경고 방지
```

**수정 파일**  
`Assets/_KKUBUL/_3rdParty/URPWater/Scripts/URPWaterReflection.cs` (line 94)

---

## ✅ [2026-04-11] URPWater 셰이더 MSAA 로컬 키워드 미선언 경고

**에러 메시지**
```
Local keyword _MSAA_2X doesn't exist in the shader.
Local keyword _MSAA_4X doesn't exist in the shader.
Local keyword _MSAA_8X doesn't exist in the shader.
```

**원인**  
URP 렌더링 중 `_MSAA_2X/_MSAA_4X/_MSAA_8X` 키워드를 셰이더에 로컬 키워드로 설정하려 하지만  
`URPWater_Standard.shader` / `URPWater_Tessellation.shader` 양쪽 모두  
해당 키워드가 `#pragma multi_compile_local`로 선언되어 있지 않아 경고 발생.

**해결**  
두 셰이더의 `HLSLPROGRAM` pragma 블록에 아래 한 줄 추가:

```hlsl
#pragma multi_compile_local _ _MSAA_2X _MSAA_4X _MSAA_8X
```

**수정 파일**  
- `Assets/_KKUBUL/_3rdParty/URPWater/Shaders/HLSL/URPWater_Standard.shader`
- `Assets/_KKUBUL/_3rdParty/URPWater/Shaders/HLSL/URPWater_Tessellation.shader`

---

## ✅ [2026-04-11] URPWater SceneView 기즈모 렌더 패스 충돌 에러

**에러 메시지**
```
Trying to draw gizmos while inside a renderpass.
This is not supported, gizmo drawing will be skipped.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)
```
(198개 이상 반복 발생)

**원인**  
`URPWaterReflection.cs`가 `beginCameraRendering` 콜백 내부에서  
`RenderSingleCamera`를 호출 → URP 렌더 패스가 이미 활성 상태.  
에디터 SceneView 카메라도 이 콜백을 트리거하는데,  
SceneView 렌더 중 반사 카메라를 추가 렌더하면  
에디터 기즈모 시스템이 렌더 패스 내부에서 기즈모를 그리려 해 충돌.

**해결**  
`ComputeReflections()` 진입부에서 SceneView 카메라를 조기 반환으로 건너뜀:

```csharp
// URPWaterReflection.cs - ComputeReflections() 내부
#if UNITY_EDITOR
if (camera.cameraType == CameraType.SceneView)
    return;
#endif
```

> SceneView에서 물 반사가 안 보이는 부작용이 있지만 에디터 전용이라 게임 플레이에 영향 없음.

**수정 파일**  
- `Assets/_KKUBUL/_3rdParty/URPWater/Scripts/URPWaterReflection.cs`
- `Assets/_KKUBUL/_3rdParty/URPWater/Scripts/URPWaterDynamicEffects.cs`

> **추가 원인**: 두 스크립트 모두 `[ExecuteAlways]` 속성 보유. 에디터에서 이전 씬에 열린 적 있으면 `RenderPipelineManager.beginCameraRendering` static 이벤트 구독이 남아 **다른 씬에서도** 에러 발생. SceneView 가드로 두 스크립트 모두 해결.

---

## ✅ [2026-04-11] CoreBlitColorAndDepth 셰이더 MSAA 로컬 키워드 미선언 경고

**에러 메시지**
```
Local keyword _MSAA_2X doesn't exist in the shader.
Local keyword _MSAA_4X doesn't exist in the shader.
Local keyword _MSAA_8X doesn't exist in the shader.
```
(더블클릭 시 `UnityEngine.Rendering.Blitter.cs:172~174` 하이라이트)

**원인**  
`Blitter.cs:172-174`에서 `CoreBlitColorAndDepth` 셰이더에 대해 `LocalKeyword` 객체를 생성:

```csharp
s_ResolveDepthMSAA2X = new(s_BlitColorAndDepth.shader, "_MSAA_2X");
s_ResolveDepthMSAA4X = new(s_BlitColorAndDepth.shader, "_MSAA_4X");
s_ResolveDepthMSAA8X = new(s_BlitColorAndDepth.shader, "_MSAA_8X");
```

`CoreBlitColorAndDepth.shader`는 `BlitColorAndDepth.hlsl`을 `#include`로 불러오는데,  
해당 `.hlsl` 안의 `#pragma multi_compile _ _MSAA_2X _MSAA_4X _MSAA_8X`는  
**포함된 파일의 pragma는 Unity 셰이더 컴파일러가 인식하지 않으므로** 키워드가 미선언 상태.  
또한 `multi_compile` (전역 키워드) 선언인데 `LocalKeyword`로 조회하므로 타입 불일치 발생.

> URP 패키지 버전 불일치로 인한 내부 버그.

**해결**  
`CoreBlitColorAndDepth.shader`의 각 Pass HLSLPROGRAM 블록에 직접 `multi_compile_local` 추가:

```hlsl
#pragma multi_compile_local _ _MSAA_2X _MSAA_4X _MSAA_8X
```

**수정 파일**  
`Packages/com.unity.render-pipelines.universal/Shaders/Utils/CoreBlitColorAndDepth.shader`

---

## 🟡 템플릿

```
## [YYYY-MM-DD] 제목

**에러 메시지**
\```
(에러/경고 전문)
\```

**원인**
(왜 발생했는지)

**해결**
(어떻게 고쳤는지, 코드 포함)

**수정 파일**
(파일 경로)
```
