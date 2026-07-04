# 세션: 게임 아이콘 관리

## 목적
게임 아이콘을 프로젝트에 추가하고, Import 설정을 올바르게 관리하는 작업.

## 현재 파일 구조
- 아이콘 폴더: `Assets/FarmingEngine_study/icon/`
- 기존 FarmingEngine 아이콘: `Assets/FarmingEngine_study/Sprites/Icons/` (Acorn.png 등)
- 아이템 에셋: `Assets/FarmingEngine_study/Resources/Items/`

## 현재 작업 아이콘
| 파일 | GUID | 연결된 아이템 에셋 |
|------|------|------------------|
| `icon/sugar_tester.png` | `7b792895bec28a141a5a554c2e3baff0` | `Resources/Items/Other/SugarTester.asset` |

## 씬 배치
- `Assets/Scenes/Game/Scene_Farm_01.unity`의 PlayerCharacter `starting_items[4]`에 SugarTester 아이템이 등록됨

---

## 아이콘 깨짐 진단 (sugar_tester.png)

### 문제 원인 (우선순위 순)

#### 1순위: Wrap Mode = Repeat (주요 원인 가능성 높음)
- 현재 설정: `wrapU: 0, wrapV: 0` → **Repeat(반복)**
- 올바른 설정: `wrapU: 1, wrapV: 1` → **Clamp(고정)**
- UI 아이콘으로 사용하는 스프라이트에 Repeat 모드를 쓰면, 텍스처 샘플링 시 반대쪽 엣지 픽셀이 새어나와 외곽선이 이상한 색으로 번지는 아티팩트 발생
- 참고: Acorn.png 등 기존 아이콘은 모두 Clamp 사용

#### 2순위: Standalone/WebGL 플랫폼 오버라이드 — 저품질 압축
- Standalone 플랫폼 오버라이드가 `textureCompression: 1` (Low Quality)로 설정됨
- 빌드 시 압축 아티팩트 발생
- 에디터 Play Mode에서는 DefaultTexturePlatform(Normal) 적용되므로 에디터에서는 안 나타날 수 있음

#### 3순위: 이미지 크기가 2의 거듭제곱이 아닐 가능성
- nPOTScale: 0 (None = 크기 변환 없음) 설정 중
- PNG가 128x128, 256x256 등 POT 크기가 아니면 일부 압축 포맷에서 품질 저하

---

## 수정 방법

### Unity Editor에서 직접 수정 (권장)
1. `Assets/FarmingEngine_study/icon/sugar_tester.png` 선택
2. Inspector에서:
   - **Wrap Mode**: Repeat → **Clamp**
   - **Filter Mode**: Bilinear 유지 (OK)
   - **Max Size**: 2048 → 실제 아이콘 용도면 256~512로 낮춰도 됨
   - 하단 Platform 탭에서 Standalone 오버라이드가 있다면 제거하거나 Compression을 Normal/None으로 변경
3. **Apply** 버튼 클릭

### .meta 파일 직접 수정 (에디터 열지 않고)
`sugar_tester.png.meta`에서:
```yaml
# 변경 전
wrapU: 0
wrapV: 0
# 변경 후
wrapU: 1
wrapV: 1
```
그리고 Standalone 플랫폼 오버라이드 섹션 전체 제거 또는 `textureCompression: 2`로 변경.

---

## 아이콘 추가 시 체크리스트

새 아이콘을 추가할 때마다 확인할 항목:
- [ ] Texture Type: **Sprite (2D and UI)**
- [ ] Sprite Mode: **Single**
- [ ] Wrap Mode: **Clamp**
- [ ] Filter Mode: Bilinear (또는 프로젝트 기본값)
- [ ] Alpha Is Transparency: **체크**
- [ ] Max Size: 256 또는 512 (UI 아이콘 기준)
- [ ] Compression: **Normal Quality** (Standalone 별도 오버라이드 필요 시 Normal 이상)
- [ ] 이미지 크기: 가능하면 POT (128x128, 256x256, 512x512)
- [ ] ItemData .asset 파일에서 icon 필드에 연결 확인

---

## 관련 파일
- `Assets/FarmingEngine_study/Sprites/Icons/` — 기존 FarmingEngine 아이콘 레퍼런스
- `Assets/FarmingEngine_study/Resources/Items/` — ItemData 에셋 (아이콘 연결 지점)
