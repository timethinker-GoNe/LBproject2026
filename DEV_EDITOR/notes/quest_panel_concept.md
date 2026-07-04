# 할일 퀘스트 패널 — 이식용 컨셉 문서

> DEV_EDITOR(`project_structure.html`)에서 구현된 플로팅 할일 패널을 다른 웹 사이트에 적용하기 위한 설계 정리.

---

## 1. 무엇인가?

페이지 오른쪽에 고정으로 떠 있는 **RPG 퀘스트 패널 스타일의 할일 위젯**.  
내비게이션 탭(또는 현재 페이지 섹션)이 바뀌면 그 탭에 속한 할일 목록으로 자동 전환된다.

```
┌─────────────────────────────┐  ← 글래스모피즘 / 파란 왼쪽 보더
│ 📋  Scene 관리 할일   3/7 ─ │  ← 헤더 (클릭 시 접기/펼치기)
├─────────────────────────────┤
│ □  SEM 트리거 연결 확인      │  ← 미완료 항목 (클릭: 포커스 하이라이트)
│ □  QuestManager 씬 배치     │
│ ✓  NavMesh Bake            │  (취소선, 흐릿)
│ [+ 할일 추가 후 Enter      ]│  ← 인풋
│ ▶ 완료 4개                  │  ← 완료 섹션 (토글)
└─────────────────────────────┘
```

---

## 2. 핵심 특징

| 특징 | 설명 |
|---|---|
| **글래스모피즘** | `background: rgba(255,255,255,0.92)` + `backdrop-filter: blur(14px)` |
| **파란 왼쪽 보더** | `border-left: 3px solid var(--accent)` — 퀘스트 패널 느낌 |
| **섹션 연동** | 탭/페이지 전환 시 해당 섹션의 할일로 자동 갱신 |
| **포커스 하이라이트** | 한 항목을 클릭하면 노란 하이라이트 — "지금 이것" 표시 |
| **완료 분리** | 미완료 / 완료 두 섹션 분리, 완료는 접어서 숨김 |
| **드래그 리사이즈** | 왼쪽 엣지를 드래그해 폭 조절, localStorage에 저장 |
| **접기/펼치기** | 헤더 클릭 또는 ＋/－ 버튼 |
| **카운트 뱃지** | 완료/전체 수 (`3/7`) 헤더에 파란 뱃지로 표시 |
| **호버 삭제** | 항목 호버 시 우측에 ✕ 버튼 노출 |

---

## 3. HTML 구조

```html
<!-- 고정 오버레이 -->
<div id="quest-panel" class="collapsed">

  <!-- 왼쪽 엣지 리사이즈 핸들 -->
  <div class="qp-resize" id="qp-resize"></div>

  <!-- 헤더 (클릭 = 접기/펼치기) -->
  <div class="qp-header" onclick="toggleQuestPanel()">
    <span class="qp-icon">📋</span>
    <span class="qp-title" id="qp-title">할일 목록</span>
    <span class="qp-count" id="qp-count">-</span>
    <button class="qp-toggle-btn" id="qp-toggle-btn">＋</button>
  </div>

  <div class="qp-body">

    <!-- 미완료 목록 -->
    <div class="qp-active-list" id="qp-active-list">
      <div class="qp-empty">탭을 선택하세요</div>
    </div>

    <!-- 완료 섹션 (기본 접힘) -->
    <div class="qp-done-section" id="qp-done-section" style="display:none">
      <button class="qp-done-toggle" onclick="toggleDoneList()">
        <span id="qp-done-arrow">▶</span>
        <span id="qp-done-label">완료 0개</span>
      </button>
      <div class="qp-done-list" id="qp-done-list"></div>
    </div>

    <!-- 인풋 -->
    <div class="qp-add">
      <input type="text" id="qp-add-input" placeholder="할일 추가 후 Enter"
             onkeydown="if(event.key==='Enter') addQuestTodo()">
      <button onclick="addQuestTodo()">＋</button>
    </div>

  </div>
</div>
```

---

## 4. CSS 핵심 클래스

```css
/* 패널 본체 */
#quest-panel {
  position: fixed;
  top: 70px;            /* nav-bar 아래 */
  right: 16px;
  width: 268px;
  z-index: 40;
  background: rgba(255,255,255,0.92);
  backdrop-filter: blur(14px);
  -webkit-backdrop-filter: blur(14px);
  border: 1px solid rgba(168,202,240,0.5);
  border-left: 3px solid var(--accent);    /* 퀘스트 느낌의 파란 선 */
  border-radius: 16px;
  box-shadow: 0 8px 32px rgba(37,99,235,.14);
}

/* 접힘 상태 */
#quest-panel.collapsed .qp-body { display: none; }

/* 헤더 */
.qp-header {
  display: flex;
  align-items: center;
  gap: 7px;
  padding: 9px 12px;
  background: rgba(224,238,255,0.7);
  border-bottom: 1px solid rgba(168,202,240,0.4);
  cursor: pointer;
  border-radius: 14px 14px 0 0;
}
.qp-title  { font-size: 11px; font-weight: 700; flex: 1; }
.qp-count  { font-size: 10px; font-weight: 700; padding: 1px 6px;
             border-radius: 9px; background: var(--accent); color: #fff; }

/* 할일 행 */
.qp-todo {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  padding: 5px 4px;
  border-radius: 5px;
}
.qp-todo:hover { background: var(--surface2); }
.qp-todo input[type=checkbox] { margin-top: 3px; accent-color: var(--accent); }

/* 텍스트 상태 */
.qp-todo-text          { flex: 1; font-size: 12px; line-height: 1.4; }
.qp-todo-text.done     { text-decoration: line-through; color: var(--text3); }
.qp-todo-text.focused  { background: rgba(234,179,8,.18); font-weight: 600; } /* 포커스 하이라이트 */

/* 호버 삭제 버튼 */
.qp-del-btn { opacity: 0; font-size: 11px; }
.qp-todo:hover .qp-del-btn { opacity: 1; }

/* 왼쪽 리사이즈 핸들 */
.qp-resize {
  position: absolute;
  left: -5px; top: 0; bottom: 0;
  width: 10px;
  cursor: ew-resize;
  z-index: 10;
}
.qp-resize:hover, .qp-resize.dragging {
  background: rgba(37,99,235,.18);
}
```

---

## 5. JavaScript 핵심 로직

### 5-1. 데이터 구조 (단순 배열)

```javascript
// 메모리 상태
let _qpTab  = null;    // 현재 섹션 키 (예: 'scene-mgr', 'gameplay')
let _qpData = null;    // { todos: [{text, done}], focus: string }
```

### 5-2. 탭 전환 시 호출

```javascript
function showTab(name) {
  // ... 탭 전환 로직 ...
  loadQuestPanel(name);   // 이 한 줄이 핵심
}

async function loadQuestPanel(tab) {
  _qpTab = tab;
  document.getElementById('qp-title').textContent = TAB_LABELS[tab] + ' 할일';
  // 백엔드에서 해당 탭 데이터 로드
  const r = await fetch('/api/notes/' + tab);
  _qpData = await r.json();
  renderQuestPanel();
}
```

### 5-3. 렌더링

```javascript
function renderQuestPanel() {
  const todos  = _qpData.todos || [];
  const active = todos.map((t,i) => ({...t, i})).filter(t => !t.done);
  const done   = todos.map((t,i) => ({...t, i})).filter(t =>  t.done);

  document.getElementById('qp-count').textContent = `${done.length}/${todos.length}`;

  const curFocus = (_qpData.focus && _qpData.focus !== '(없음)') ? _qpData.focus : null;
  const todoHtml = t => `
    <div class="qp-todo">
      <input type="checkbox" ${t.done ? 'checked' : ''} onchange="toggleQuestTodo(${t.i})">
      <span class="qp-todo-text ${t.done ? 'done' : ''}${curFocus === t.text ? ' focused' : ''}"
            onclick="focusQuestTodo(${t.i})">${t.text}</span>
      <button class="qp-del-btn" onclick="deleteQuestTodo(${t.i})">✕</button>
    </div>`;

  document.getElementById('qp-active-list').innerHTML =
    active.length ? active.map(todoHtml).join('') : '<div class="qp-empty">할일 없음 🎉</div>';

  const doneSection = document.getElementById('qp-done-section');
  if (done.length) {
    doneSection.style.display = '';
    document.getElementById('qp-done-label').textContent = `완료 ${done.length}개`;
    document.getElementById('qp-done-list').innerHTML = done.map(todoHtml).join('');
  } else {
    doneSection.style.display = 'none';
  }
}
```

### 5-4. 접기/펼치기

```javascript
function toggleQuestPanel() {
  const p   = document.getElementById('quest-panel');
  const btn = document.getElementById('qp-toggle-btn');
  p.classList.toggle('collapsed');
  btn.textContent = p.classList.contains('collapsed') ? '＋' : '－';
}

function toggleDoneList() {
  const list  = document.getElementById('qp-done-list');
  const arrow = document.getElementById('qp-done-arrow');
  const open  = list.classList.toggle('open');
  arrow.textContent = open ? '▼' : '▶';
}
```

### 5-5. 드래그 리사이즈 (localStorage 저장)

```javascript
(function() {
  const LS_KEY = 'qp-width';
  let dragging = false, startX = 0, startW = 0;

  document.addEventListener('DOMContentLoaded', () => {
    const panel  = document.getElementById('quest-panel');
    const handle = document.getElementById('qp-resize');
    const saved  = localStorage.getItem(LS_KEY);
    if (saved) panel.style.width = saved + 'px';

    handle.addEventListener('mousedown', e => {
      dragging = true;
      startX   = e.clientX;
      startW   = panel.offsetWidth;
      handle.classList.add('dragging');
    });
    document.addEventListener('mousemove', e => {
      if (!dragging) return;
      const w = Math.max(200, Math.min(500, startW - (e.clientX - startX)));
      panel.style.width = w + 'px';
    });
    document.addEventListener('mouseup', () => {
      if (dragging) {
        dragging = false;
        handle.classList.remove('dragging');
        localStorage.setItem(LS_KEY, panel.offsetWidth);
      }
    });
  });
})();
```

---

## 6. 백엔드 API 명세 (Python Flask 예시)

현재 구현은 `asset_server.py` (Flask)를 사용하지만, 어떤 백엔드든 아래 엔드포인트만 맞추면 된다.

```
GET  /api/notes/{tab}           → { todos:[{text,done}], focus:string }
POST /api/notes/{tab}/add       → body:{text}       → { ok:true, todos:[] }
POST /api/notes/{tab}/toggle    → body:{index}      → { ok:true, todos:[] }
POST /api/notes/{tab}/focus     → body:{text}       → { ok:true }
POST /api/notes/{tab}/del       → body:{idx}        → { ok:true, todos:[] }
```

데이터는 탭별로 JSON 파일로 저장 (예: `notes/scene-mgr.json`).

---

## 7. localStorage 전용 버전 (백엔드 없는 경우)

백엔드 없이 순수 클라이언트로 구현하려면 fetch 호출 대신 localStorage를 쓰면 된다.

```javascript
function getTabData(tab) {
  try { return JSON.parse(localStorage.getItem('notes:' + tab)) || { todos:[], focus:'' }; }
  catch { return { todos:[], focus:'' }; }
}
function saveTabData(tab, data) {
  localStorage.setItem('notes:' + tab, JSON.stringify(data));
}

// addQuestTodo 예시
function addQuestTodo() {
  const input = document.getElementById('qp-add-input');
  const text  = input.value.trim();
  if (!text) return;
  const data = getTabData(_qpTab);
  data.todos.push({ text, done: false });
  saveTabData(_qpTab, data);
  _qpData = data;
  input.value = '';
  renderQuestPanel();
}
```

---

## 8. 다른 사이트 적용 시 체크리스트

- [ ] `--accent` CSS 변수 설정 (기본: `#2563eb`)
- [ ] `--surface`, `--surface2`, `--border`, `--text`, `--text3` 변수 정의
- [ ] 탭/섹션 전환 함수에 `loadQuestPanel(sectionKey)` 한 줄 추가
- [ ] `TAB_LABELS` 객체에 섹션 키 → 표시 이름 매핑 추가
- [ ] 백엔드 방식 또는 localStorage 방식 선택
- [ ] nav-bar 높이에 맞게 `positionQuestPanel()` 조정 (또는 CSS `top` 값 직접 설정)
- [ ] 다크모드 필요 시 `background: rgba(20,26,38,0.92)` 계열로 교체

---

## 9. 확장 아이디어

- **우선순위 색상**: 각 todo에 `priority: 'high'|'normal'|'low'` 추가 → 점(dot) 색상 구분
- **섹션 전체 진행도**: 헤더 아래 얇은 진행 바 추가 (`done/total * 100%`)
- **마감일 지원**: `dueDate` 필드 추가, 오늘 기준 D-day 뱃지 표시
- **드래그 정렬**: `ondragstart/ondrop`으로 todo 순서 재정렬
- **서버 없는 내보내기**: 현재 탭 할일 목록을 Markdown 형식으로 클립보드 복사
