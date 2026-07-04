# 씬 관리 탭 업데이트 프로토콜

사용자가 "씬 업데이트해줘" 또는 "씬에 컴포넌트 추가됐어" 등을 요청할 때 따르는 절차.

---

## 1단계 — 현재 문서화 상태 파악 (JSON 읽기)

```
DEV_EDITOR/notes/scene_versions/<최신버전>.json
```

- 어떤 씬이 문서화되어 있는지, 현재 컴포넌트 목록이 무엇인지 파악
- HTML 전체를 읽지 말 것 — JSON이 "현재 문서 상태의 인덱스"

---

## 2단계 — Unity 씬 파일에서 변경사항 추출 (grep 우선)

`.unity` 파일은 YAML이라 크기가 크다. 전체를 Read하지 말고 grep으로 필요한 정보만 추출:

```bash
# 씬에 붙은 컴포넌트 스크립트 이름 목록
grep -n "m_Name\|MonoBehaviour\|m_Script" Assets/Scenes/Game/Scene_Farm_01.unity | head -80
```

파악할 내용:
- 추가된 GameObject / 컴포넌트
- 제거된 GameObject / 컴포넌트
- 그룹 구조 변경 (부모-자식 관계)

---

## 3단계 — JSON diff 작성 → 새 버전 저장

최신 JSON을 복사해 변경 내용만 수정 후 새 버전으로 저장:

```
DEV_EDITOR/notes/scene_versions/v2_YYYYMMDD.json
```

`changelog` 필드에 변경 내용 한 줄 요약 필수.

---

## 4단계 — HTML surgical edit

변경된 씬의 `.sm-sub` 섹션만 수정. 수정 범위:
- `sm-hier-body` 내 해당 씬 계층 트리
- `sm-vgroups` 내 해당 씬 컴포넌트 설명 카드

**HTML 전체를 읽지 말 것.** Grep으로 변경 대상 라인 번호 파악 후 Read(offset, limit)로 해당 구간만 읽기.

---

## 씬 파일 경로

| 씬 | 경로 |
|----|------|
| Scene_Start | `Assets/Scenes/Game/Scene_Start.unity` |
| Scene_Intro | `Assets/Scenes/Game/Scene_Intro.unity` |
| Scene_CharCreate | `Assets/Scenes/Game/Scene_CharCreate.unity` |
| Scene_Farm_01 | `Assets/Scenes/Game/Scene_Farm_01.unity` |

버전 JSON: `DEV_EDITOR/notes/scene_versions/`  
씬 관리 HTML: `DEV_EDITOR/project_structure.html` (씬 관리 탭 섹션)

---

## 버전 JSON 스키마

```json
{
  "version": "v2_20260701",
  "date": "2026-07-01",
  "label": "한 줄 요약",
  "changelog": "변경 내용 서술",
  "scenes": {
    "Scene_Farm_01": {
      "path": "Assets/Scenes/Game/Scene_Farm_01.unity",
      "components": [
        {"group": "MANAGERS", "name": "컴포넌트명", "ns": "네임스페이스", "desc": "설명"}
      ]
    }
  }
}
```
