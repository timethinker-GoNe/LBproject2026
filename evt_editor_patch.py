import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

HTML_PATH = r'C:\workspace\Farming_01_16_Final\project_structure.html'

with open(HTML_PATH, encoding='utf-8') as f:
    lines = f.readlines()

print(f'원본 라인 수: {len(lines)}')

# ── 1. dq-sub-evtmap 내부 콘텐츠 교체 (0-indexed [3799:4111]) ─────────────
# L3799(0-idx 3798) = 여는 div, L4112(0-idx 4111) = 닫는 div  →  사이 교체

new_evtmap_html = """\n    <!-- 헤더 -->\n    <div style="display:flex;align-items:center;gap:10px;margin-bottom:12px;padding-bottom:10px;border-bottom:1px solid var(--border);">\n      <span style="font-size:12px;color:var(--text3);">📋 story_events.json 편집 — 트리거·조건·대사·효과·on_end를 수정합니다</span>\n      <button class="dq-btn" style="margin-left:auto" onclick="evtSave()">💾 저장</button>\n    </div>\n\n    <!-- 2열 레이아웃 -->\n    <div style="display:grid;grid-template-columns:220px 1fr;gap:12px;height:560px;">\n      <div style="display:flex;flex-direction:column;gap:6px;overflow:hidden;">\n        <button class="dq-btn" style="width:100%;flex-shrink:0" onclick="openAddEvtModal()">+ 이벤트 추가</button>\n        <div id="dq-evt-sidebar" style="flex:1;overflow-y:auto;padding-right:2px;"></div>\n      </div>\n      <div id="dq-evt-main" style="overflow-y:auto;padding-right:4px;"></div>\n    </div>\n\n    <!-- 이벤트 추가 모달 -->\n    <div id="dq-modal-add-evt" class="modal-overlay">\n      <div class="modal-box" style="width:340px;">\n        <div class="modal-title">이벤트 추가</div>\n        <label class="modal-label">이벤트 ID</label>\n        <input id="dq-evt-new-id" class="modal-input" placeholder="grandma_offer_tutorial_003">\n        <label class="modal-label">트리거 NPC ID</label>\n        <input id="dq-evt-new-npc" class="modal-input" placeholder="grandma">\n        <div style="display:flex;gap:8px;margin-top:16px;">\n          <button class="dq-btn" style="flex:1" onclick="submitAddEvt()">추가</button>\n          <button class="dq-btn" style="flex:1" onclick="closeModal('dq-modal-add-evt')">취소</button>\n        </div>\n      </div>\n    </div>\n\n"""

lines[3799:4111] = [new_evtmap_html]
print(f'1. dq-sub-evtmap 교체 완료. 라인 수: {len(lines)}')

# ── 2. 서브탭 버튼 레이블 변경 ────────────────────────────────────────────────
for i, l in enumerate(lines):
    if 'data-sub="evtmap"' in l and '이벤트-대화 구조' in l:
        lines[i] = l.replace('🗣️ 이벤트-대화 구조', '⚡ 이벤트 편집기')
        print(f'2. 서브탭 버튼 레이블 변경 완료 (L{i+1})')
        break

# ── 3. DQ 객체에 currentEvt, dirtyStory 추가 ──────────────────────────────────
for i, l in enumerate(lines):
    if 'dirtyIdle: false,  // npc_idle.json' in l:
        lines[i] = l + '  currentEvt: null,   // 현재 선택된 이벤트 ID\n  dirtyStory: false, // story_events.json 변경됨\n'
        print(f'3. DQ 객체 확장 완료 (L{i+1})')
        break

# ── 4. showDqSubtab: evtmap 탭 전환 시 renderEvtSidebar() 호출 추가 ──────────
for i, l in enumerate(lines):
    if "if (name === 'questedit') QE.init();" in l:
        lines[i] = l + "  if (name === 'evtmap')    renderEvtSidebar();\n"
        print(f'4. showDqSubtab 업데이트 완료 (L{i+1})')
        break

# ── 5. dqSave에 dirtyStory 처리 추가 ──────────────────────────────────────────
for i, l in enumerate(lines):
    if "if (DQ.dirtyIdle) { await saveIdleFile();" in l:
        lines[i] = l + "  if (DQ.dirtyStory) { await saveStoryFile(); saved.push('story_events.json'); }\n"
        print(f'5. dqSave 업데이트 완료 (L{i+1})')
        break

# ── 6. 이벤트 편집기 JS 삽입 (submitAddNpc 닫힘 직후) ────────────────────────
insert_js = r"""

// ============================================================
// 이벤트 편집기 (StoryEventManager)
// ============================================================

function getEvt(evtId) { return (DQ.storyData?.events||[]).find(e => e.id === evtId); }

function renderEvtSidebar() {
  const sb = document.getElementById('dq-evt-sidebar');
  if (!sb) return;
  const events = DQ.storyData?.events || [];
  if (!events.length) {
    sb.innerHTML = '<div style="color:var(--text3);font-size:11px;padding:8px 0">이벤트 없음<br><span style="font-size:10px;">폴더 연결 후 로드됩니다</span></div>';
    return;
  }
  sb.innerHTML = events.map(evt => {
    const npcId = (evt.triggers||[]).find(t => t.type==='npc_talk')?.npc_id || '—';
    const cc = (evt.conditions||[]).length;
    const isActive = evt.id === DQ.currentEvt;
    return `<div class="dq-npc-item ${isActive?'active':''}" onclick="selectEvt('${escHtml(evt.id)}')">
      <div class="dq-npc-name" style="font-size:11px;">${escHtml(evt.id)}</div>
      <div class="dq-npc-meta">${escHtml(npcId)} · 조건 ${cc}개</div>
    </div>`;
  }).join('');
}

function selectEvt(evtId) {
  DQ.currentEvt = evtId;
  renderEvtSidebar();
  renderEvtMain();
}

function renderEvtMain() {
  const main = document.getElementById('dq-evt-main');
  if (!main) return;
  const evt = getEvt(DQ.currentEvt);
  if (!evt) {
    main.innerHTML = '<div style="color:var(--text3);padding:40px;text-align:center">이벤트를 선택하세요</div>';
    return;
  }
  const eid = escHtml(evt.id);

  // 트리거
  const triggersHtml = (evt.triggers||[]).map((t, ti) => `
    <div style="display:flex;align-items:center;gap:6px;margin-bottom:6px;">
      <span style="font-size:10px;color:var(--text3);width:60px;flex-shrink:0">npc_talk</span>
      <input class="dq-input" style="flex:1;padding:3px 8px;font-size:11px;" placeholder="npc_id"
        value="${escHtml(t.npc_id||'')}" oninput="setEvtTriggerNpc('${eid}',${ti},this.value)">
      <button class="dq-btn dq-btn-sm" style="color:var(--red)" onclick="removeEvtTrigger('${eid}',${ti})">✕</button>
    </div>`).join('') +
    `<button class="dq-btn dq-btn-sm" onclick="addEvtTrigger('${eid}')">+ 트리거</button>`;

  // 조건
  const condStatusOpts = ['Available','InProgress','Completed','Rewarded','NotStarted'];
  const conditionsHtml = (evt.conditions||[]).map((c, ci) => {
    const isQ = c.type === 'quest_status';
    const paramHtml = isQ
      ? `<input class="dq-input" style="flex:1;padding:3px 8px;font-size:11px;" placeholder="quest_id"
           value="${escHtml(c.quest_id||'')}" oninput="setEvtCondField('${eid}',${ci},'quest_id',this.value)">
         <select class="dq-input" style="padding:3px 8px;font-size:11px;" onchange="setEvtCondField('${eid}',${ci},'status',this.value)">
           ${condStatusOpts.map(s=>`<option${c.status===s?' selected':''}>${s}</option>`).join('')}
         </select>`
      : `<input class="dq-input" style="flex:1;padding:3px 8px;font-size:11px;" placeholder="npc_id"
           value="${escHtml(c.npc_id||'')}" oninput="setEvtCondField('${eid}',${ci},'npc_id',this.value)">
         <input class="dq-input" style="flex:1;padding:3px 8px;font-size:11px;" placeholder="value"
           value="${escHtml(c.value||'')}" oninput="setEvtCondField('${eid}',${ci},'value',this.value)">`;
    return `<div style="display:flex;align-items:center;gap:6px;margin-bottom:6px;flex-wrap:wrap;">
      <select class="dq-input" style="padding:3px 8px;font-size:11px;" onchange="setEvtCondField('${eid}',${ci},'type',this.value)">
        <option${c.type==='quest_status'?' selected':''}>quest_status</option>
        <option${c.type==='npc_status'?' selected':''}>npc_status</option>
      </select>
      ${paramHtml}
      <button class="dq-btn dq-btn-sm" style="color:var(--red)" onclick="removeEvtCondition('${eid}',${ci})">✕</button>
    </div>`;
  }).join('') + `<button class="dq-btn dq-btn-sm" onclick="addEvtCondition('${eid}')">+ 조건</button>`;

  // 라인
  const linesHtml = (evt.lines||[]).map((line, li) => {
    const key = line.text_key||'';
    const val = DQ.data[DQ.lang]?.[key]||'';
    return `<div style="border:1px solid var(--border);border-radius:6px;padding:8px;margin-bottom:8px;">
      <div style="display:flex;align-items:center;gap:6px;margin-bottom:5px;">
        <input class="dq-input" style="width:80px;padding:3px 6px;font-size:10px;" placeholder="actor"
          value="${escHtml(line.actor||'')}" oninput="setEvtLineField('${eid}',${li},'actor',this.value)">
        <input class="dq-input" style="flex:1;padding:3px 6px;font-size:10px;font-family:monospace;" placeholder="text_key"
          value="${escHtml(key)}" oninput="setEvtLineField('${eid}',${li},'text_key',this.value)">
        <button class="dq-btn dq-btn-sm" style="color:var(--red)" onclick="removeEvtLine('${eid}',${li})">✕</button>
      </div>
      <textarea class="dq-input" rows="2" style="width:100%;resize:none;overflow:hidden;"
        oninput="setDqText('${escHtml(key)}',this.value);this.style.height='auto';this.style.height=this.scrollHeight+'px'"
        placeholder="대사 텍스트…">${escHtml(val)}</textarea>
    </div>`;
  }).join('') + `<button class="dq-btn dq-btn-sm" onclick="addEvtLine('${eid}')">+ 라인</button>`;

  // 효과
  const effectTypes = ['start_fq_quest','receive_fq_reward','give_item','set_npc_status','go_to_event'];
  const effectsHtml = (evt.effects||[]).map((ef, ei) => {
    let p = '';
    if (ef.type==='start_fq_quest'||ef.type==='receive_fq_reward')
      p = `<input class="dq-input" style="flex:1;padding:3px 8px;font-size:11px;" placeholder="quest_id"
        value="${escHtml(ef.quest_id||'')}" oninput="setEvtEffectField('${eid}',${ei},'quest_id',this.value)">`;
    else if (ef.type==='give_item')
      p = `<input class="dq-input" style="flex:1;padding:3px 8px;font-size:11px;" placeholder="item_id"
        value="${escHtml(ef.item_id||ef.itemId||'')}" oninput="setEvtEffectField('${eid}',${ei},'item_id',this.value)">
        <input class="dq-input" style="width:50px;padding:3px 6px;font-size:11px;" type="number" min="1" placeholder="qty"
        value="${ef.quantity||1}" oninput="setEvtEffectField('${eid}',${ei},'quantity',+this.value)">`;
    else if (ef.type==='set_npc_status')
      p = `<input class="dq-input" style="flex:1;padding:3px 8px;font-size:11px;" placeholder="npc_id"
        value="${escHtml(ef.npc_id||'')}" oninput="setEvtEffectField('${eid}',${ei},'npc_id',this.value)">
        <input class="dq-input" style="flex:1;padding:3px 8px;font-size:11px;" placeholder="value"
        value="${escHtml(ef.value||'')}" oninput="setEvtEffectField('${eid}',${ei},'value',this.value)">`;
    else if (ef.type==='go_to_event')
      p = `<input class="dq-input" style="flex:1;padding:3px 8px;font-size:11px;" placeholder="event_id"
        value="${escHtml(ef.event_id||ef.eventId||'')}" oninput="setEvtEffectField('${eid}',${ei},'event_id',this.value)">`;
    return `<div style="display:flex;align-items:center;gap:6px;margin-bottom:6px;flex-wrap:wrap;">
      <select class="dq-input" style="padding:3px 8px;font-size:11px;" onchange="setEvtEffectField('${eid}',${ei},'type',this.value)">
        ${effectTypes.map(t=>`<option${ef.type===t?' selected':''}>${t}</option>`).join('')}
      </select>
      ${p}
      <button class="dq-btn dq-btn-sm" style="color:var(--red)" onclick="removeEvtEffect('${eid}',${ei})">✕</button>
    </div>`;
  }).join('') + `<button class="dq-btn dq-btn-sm" onclick="addEvtEffect('${eid}')">+ 효과</button>`;

  // on_end
  const onEndHtml = (evt.on_end||[]).map((lc, oi) => `
    <div style="display:flex;align-items:center;gap:6px;margin-bottom:6px;">
      <span style="font-size:10px;color:var(--text3);width:100px;flex-shrink:0">set_npc_status</span>
      <input class="dq-input" style="flex:1;padding:3px 8px;font-size:11px;" placeholder="npc_id"
        value="${escHtml(lc.npc_id||'')}" oninput="setEvtOnEndField('${eid}',${oi},'npc_id',this.value)">
      <input class="dq-input" style="flex:1;padding:3px 8px;font-size:11px;" placeholder="value"
        value="${escHtml(lc.value||'')}" oninput="setEvtOnEndField('${eid}',${oi},'value',this.value)">
      <button class="dq-btn dq-btn-sm" style="color:var(--red)" onclick="removeEvtOnEnd('${eid}',${oi})">✕</button>
    </div>`).join('') +
  `<button class="dq-btn dq-btn-sm" onclick="addEvtOnEnd('${eid}')">+ on_end</button>`;

  main.innerHTML = `
    <div style="display:flex;align-items:center;gap:8px;margin-bottom:14px;padding-bottom:8px;border-bottom:1px solid var(--border);">
      <span style="font-size:13px;font-weight:700;font-family:monospace;color:var(--accent);">${eid}</span>
      <button class="dq-btn dq-btn-sm" style="color:var(--red);margin-left:auto" onclick="removeStoryEvent('${eid}')">🗑 삭제</button>
    </div>
    <div style="display:flex;flex-direction:column;gap:14px;">
      <div class="qs-box"><div class="qs-box-title" style="color:#60a5fa;">🎯 트리거</div>${triggersHtml}</div>
      <div class="qs-box"><div class="qs-box-title" style="color:#4ade80;">🔀 조건 (AND 결합, 위→아래)</div>${conditionsHtml}</div>
      <div class="qs-box"><div class="qs-box-title" style="color:#fbbf24;">💬 대사 라인</div>${linesHtml}</div>
      <div class="qs-box"><div class="qs-box-title" style="color:#f87171;">⚡ 효과 (대사 후 실행)</div>${effectsHtml}</div>
      <div class="qs-box"><div class="qs-box-title" style="color:#a78bfa;">🔄 on_end (이벤트 종료 시)</div>${onEndHtml}</div>
    </div>
  `;
  main.querySelectorAll('textarea').forEach(ta => { ta.style.height='auto'; ta.style.height=ta.scrollHeight+'px'; });
}

// ── 이벤트 CRUD ──────────────────────────────────────────────────────────────
function openAddEvtModal() {
  document.getElementById('dq-evt-new-id').value = '';
  document.getElementById('dq-evt-new-npc').value = '';
  document.getElementById('dq-modal-add-evt').classList.add('open');
}
function submitAddEvt() {
  const id  = document.getElementById('dq-evt-new-id').value.trim();
  const npc = document.getElementById('dq-evt-new-npc').value.trim();
  if (!id) { alert('이벤트 ID를 입력하세요.'); return; }
  if (!DQ.storyData) DQ.storyData = { events: [] };
  if (DQ.storyData.events.find(e => e.id === id)) { alert("'" + id + "' 이벤트가 이미 존재합니다."); return; }
  DQ.storyData.events.push({
    id, triggers: npc ? [{ type:'npc_talk', npc_id:npc }] : [],
    conditions: [], on_start: [], lines: [], effects: [], on_end: []
  });
  DQ.dirtyStory = true;
  closeModal('dq-modal-add-evt');
  renderEvtSidebar();
  selectEvt(id);
}
function removeStoryEvent(evtId) {
  if (!confirm("'" + evtId + "' 이벤트를 삭제하시겠습니까?")) return;
  DQ.storyData.events = DQ.storyData.events.filter(e => e.id !== evtId);
  DQ.dirtyStory = true;
  if (DQ.currentEvt === evtId) DQ.currentEvt = null;
  renderEvtSidebar();
  renderEvtMain();
}

// ── 트리거 편집 ───────────────────────────────────────────────────────────────
function addEvtTrigger(evtId) {
  const e = getEvt(evtId); if (!e) return;
  (e.triggers = e.triggers||[]).push({ type:'npc_talk', npc_id:'' });
  DQ.dirtyStory = true; renderEvtMain();
}
function removeEvtTrigger(evtId, ti) {
  getEvt(evtId)?.triggers?.splice(ti,1);
  DQ.dirtyStory = true; renderEvtMain();
}
function setEvtTriggerNpc(evtId, ti, v) {
  const t = getEvt(evtId)?.triggers?.[ti]; if (!t) return;
  t.npc_id = v; DQ.dirtyStory = true;
  updateSyncBar('warn', '저장되지 않은 변경사항 있음');
}

// ── 조건 편집 ─────────────────────────────────────────────────────────────────
function addEvtCondition(evtId) {
  const e = getEvt(evtId); if (!e) return;
  (e.conditions = e.conditions||[]).push({ type:'quest_status', quest_id:'', status:'Available' });
  DQ.dirtyStory = true; renderEvtMain();
}
function removeEvtCondition(evtId, ci) {
  getEvt(evtId)?.conditions?.splice(ci,1);
  DQ.dirtyStory = true; renderEvtMain();
}
function setEvtCondField(evtId, ci, field, v) {
  const c = getEvt(evtId)?.conditions?.[ci]; if (!c) return;
  c[field] = v; DQ.dirtyStory = true;
  if (field === 'type') renderEvtMain();
  else updateSyncBar('warn', '저장되지 않은 변경사항 있음');
}

// ── 라인 편집 ─────────────────────────────────────────────────────────────────
function addEvtLine(evtId) {
  const e = getEvt(evtId); if (!e) return;
  (e.lines = e.lines||[]).push({ actor:'', text_key:'' });
  DQ.dirtyStory = true; renderEvtMain();
}
function removeEvtLine(evtId, li) {
  getEvt(evtId)?.lines?.splice(li,1);
  DQ.dirtyStory = true; renderEvtMain();
}
function setEvtLineField(evtId, li, field, v) {
  const line = getEvt(evtId)?.lines?.[li]; if (!line) return;
  line[field] = v; DQ.dirtyStory = true;
  updateSyncBar('warn', '저장되지 않은 변경사항 있음');
}

// ── 효과 편집 ─────────────────────────────────────────────────────────────────
function addEvtEffect(evtId) {
  const e = getEvt(evtId); if (!e) return;
  (e.effects = e.effects||[]).push({ type:'start_fq_quest', quest_id:'' });
  DQ.dirtyStory = true; renderEvtMain();
}
function removeEvtEffect(evtId, ei) {
  getEvt(evtId)?.effects?.splice(ei,1);
  DQ.dirtyStory = true; renderEvtMain();
}
function setEvtEffectField(evtId, ei, field, v) {
  const ef = getEvt(evtId)?.effects?.[ei]; if (!ef) return;
  ef[field] = v; DQ.dirtyStory = true;
  if (field === 'type') renderEvtMain();
  else updateSyncBar('warn', '저장되지 않은 변경사항 있음');
}

// ── on_end 편집 ───────────────────────────────────────────────────────────────
function addEvtOnEnd(evtId) {
  const e = getEvt(evtId); if (!e) return;
  (e.on_end = e.on_end||[]).push({ type:'set_npc_status', npc_id:'', value:'' });
  DQ.dirtyStory = true; renderEvtMain();
}
function removeEvtOnEnd(evtId, oi) {
  getEvt(evtId)?.on_end?.splice(oi,1);
  DQ.dirtyStory = true; renderEvtMain();
}
function setEvtOnEndField(evtId, oi, field, v) {
  const lc = getEvt(evtId)?.on_end?.[oi]; if (!lc) return;
  lc[field] = v; DQ.dirtyStory = true;
  updateSyncBar('warn', '저장되지 않은 변경사항 있음');
}

// ── 저장 ──────────────────────────────────────────────────────────────────────
async function saveStoryFile() {
  if (!DQ.dirHandle) return;
  try {
    if (!DQ.fileHandles['story'])
      DQ.fileHandles['story'] = await DQ.dirHandle.getFileHandle('story_events.json', { create: true });
    const writable = await DQ.fileHandles['story'].createWritable();
    const comment = DQ.storyData._comment || 'StoryEventManager가 로드.';
    await writable.write(JSON.stringify({ _comment: comment, events: DQ.storyData.events }, null, 2));
    await writable.close();
    DQ.dirtyStory = false;
    updateSyncBar('ok', 'story_events.json 저장 완료');
    showToast('저장 완료');
  } catch(e) {
    updateSyncBar('err', 'story_events.json 저장 실패: ' + e.message);
  }
}
async function evtSave() {
  if (!DQ.dirHandle) { alert('먼저 폴더를 연결해주세요.'); return; }
  let saved = [];
  if (DQ.dirtyStory) { await saveStoryFile(); saved.push('story_events.json'); }
  if (DQ.dirtyText)  { await saveJsonFile(DQ.lang); saved.push('ko.json'); }
  if (!saved.length) updateSyncBar('ok', '변경사항 없음');
  else { updateSyncBar('ok', saved.join(' + ') + ' 저장 완료'); showToast('저장 완료'); }
}

"""

# submitAddNpc 함수 닫힘 직후를 찾아서 삽입
joined = ''.join(lines)
target_str = "  DQ.dirtyIdle = true;\n  closeModal('dq-modal-add-npc');\n  renderDQSidebar();\n  selectNpc(id);\n\n}\n"
if target_str in joined:
    idx = joined.index(target_str) + len(target_str)
    joined = joined[:idx] + insert_js + joined[idx:]
    lines = joined.splitlines(keepends=True)
    print(f'6. 이벤트 편집기 JS 추가 완료. 라인 수: {len(lines)}')
else:
    print('ERROR: submitAddNpc 닫힘 패턴을 찾을 수 없음')
    # 백업: SM 섹션 직전에 삽입
    for i, l in enumerate(lines):
        if '// ============================================================' in l and 'SceneQuestManager' in lines[i+1] if i+1 < len(lines) else False:
            lines.insert(i, insert_js)
            print(f'6b. 백업: SM 섹션 직전에 삽입 완료 (L{i+1})')
            break

# ── 저장 ───────────────────────────────────────────────────────────────────────
with open(HTML_PATH, 'w', encoding='utf-8') as f:
    f.writelines(lines)
print(f'최종 라인 수: {len(lines)}')
print('저장 완료')
