"""
3단계 웹 에디터 패치
1. 잡담 편집기 서브탭 버튼 추가
2. showDqSubtab에 'chat' 처리 추가
3. _notify / openDialogueFolder → renderChatSidebar 호출 추가
4. renderDQMain → 텍스트 전용 그룹 에디터로 교체
5. renderIdleSection / renderStorySection 제거 → renderTextGroupIdle/Story로 교체
6. dq-sub-editor 뒤에 dq-sub-chat HTML 삽입
7. 잡담 편집기 JS 함수 전체 추가
8. renderEvtMain lines 섹션 업데이트 (타입 + 선택지)
"""
import sys, io, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

HTML_PATH = r'C:\workspace\Farming_01_16_Final\project_structure.html'
with open(HTML_PATH, encoding='utf-8') as f:
    src = f.read()

# ── 헬퍼 ──────────────────────────────────────────────────────────────────────
def replace_once(text, old, new, label):
    if old not in text:
        print(f'ERROR [{label}]: 패턴 없음')
        return text
    print(f'OK    [{label}]')
    return text.replace(old, new, 1)

# ═══════════════════════════════════════════════════════════════════════════════
# 1. 서브탭 버튼: 대화 편집기 뒤에 잡담 편집기 삽입
# ═══════════════════════════════════════════════════════════════════════════════
src = replace_once(src,
    """    <div class="dq-subtab-btn active" data-sub="editor" onclick="showDqSubtab('editor')">✏️ 대화 편집기</div>""",
    """    <div class="dq-subtab-btn active" data-sub="editor" onclick="showDqSubtab('editor')">✏️ 대화 편집기</div>
    <div class="dq-subtab-btn" data-sub="chat" onclick="showDqSubtab('chat')">💬 잡담 편집기</div>""",
    '서브탭 버튼 추가')

# ═══════════════════════════════════════════════════════════════════════════════
# 2. showDqSubtab에 chat 추가
# ═══════════════════════════════════════════════════════════════════════════════
src = replace_once(src,
    "  document.getElementById('dq-sub-evtmap').style.display    = name === 'evtmap'    ? '' : 'none';",
    "  document.getElementById('dq-sub-evtmap').style.display    = name === 'evtmap'    ? '' : 'none';\n  document.getElementById('dq-sub-chat').style.display      = name === 'chat'      ? '' : 'none';",
    'showDqSubtab chat display')

src = replace_once(src,
    "  if (name === 'evtmap')    renderEvtSidebar();\n",
    "  if (name === 'evtmap')    renderEvtSidebar();\n  if (name === 'chat')      renderChatSidebar();\n",
    'showDqSubtab renderChatSidebar')

# ═══════════════════════════════════════════════════════════════════════════════
# 3. _notify / openDialogueFolder → renderChatSidebar 추가
# ═══════════════════════════════════════════════════════════════════════════════
src = replace_once(src,
    "        renderDQSidebar();\n        renderDQMain();\n      }\n      // QE 편집기 자동 로드",
    "        renderDQSidebar();\n        renderDQMain();\n        renderChatSidebar();\n      }\n      // QE 편집기 자동 로드",
    '_notify renderChatSidebar')

src = replace_once(src,
    "  renderDQSidebar();\n  renderDQMain();\n}\n\nasync function loadJsonFile",
    "  renderDQSidebar();\n  renderDQMain();\n  renderChatSidebar();\n}\n\nasync function loadJsonFile",
    'openDialogueFolder renderChatSidebar')

# ═══════════════════════════════════════════════════════════════════════════════
# 4. renderDQMain 교체 (텍스트 전용 그룹 에디터)
# ═══════════════════════════════════════════════════════════════════════════════
old_renderDQMain = """function renderDQMain() {
  const main = document.getElementById('dq-main-content');
  if (!main) return;
  if (!DQ.currentNpc) {
    main.innerHTML = '<div style="color:var(--text3);padding:40px;text-align:center">NPC를 선택하세요</div>';
    return;
  }
  main.innerHTML = renderIdleSection(DQ.currentNpc) +
    '<div style="height:24px;border-top:1px solid var(--border);margin:16px 0"></div>' +
    renderStorySection(DQ.currentNpc);
  main.querySelectorAll('textarea').forEach(ta => { ta.style.height='auto'; ta.style.height=ta.scrollHeight+'px'; });
}"""

new_renderDQMain = """function renderDQMain() {
  const main = document.getElementById('dq-main-content');
  if (!main) return;
  if (!DQ.currentNpc) {
    main.innerHTML = '<div style="color:var(--text3);padding:40px;text-align:center">NPC를 선택하세요</div>';
    return;
  }
  const idlePart  = renderTextGroupIdle(DQ.currentNpc);
  const storyPart = renderTextGroupStory(DQ.currentNpc);
  main.innerHTML = (idlePart || storyPart)
    ? idlePart + (idlePart && storyPart ? '<div style="height:1px;background:var(--border);margin:16px 0"></div>' : '') + storyPart
    : '<div style="color:var(--text3);font-size:12px;padding:20px 0;">이 NPC에 연결된 텍스트 없음</div>';
  main.querySelectorAll('textarea').forEach(ta => { ta.style.height='auto'; ta.style.height=ta.scrollHeight+'px'; });
}

function renderTextKey(key) {
  if (!key) return '';
  const val = DQ.data[DQ.lang]?.[key] || '';
  return `<div style="margin-bottom:8px;">
    <div style="font-size:10px;color:var(--text3);font-family:monospace;background:var(--surface2);padding:2px 7px;border-radius:4px;display:inline-block;margin-bottom:3px;">${escHtml(key)}</div>
    <textarea class="dq-input" rows="2" style="width:100%;resize:none;overflow:hidden;"
      oninput="setDqText('${escHtml(key)}',this.value);this.style.height='auto';this.style.height=this.scrollHeight+'px'"
      placeholder="텍스트를 입력하세요…">${escHtml(val)}</textarea>
  </div>`;
}

function renderTextGroupIdle(npcId) {
  const entries = DQ.idleData?.npcs?.[npcId]?.entries || [];
  if (!entries.length) return '';
  const cardsHtml = entries.map(entry => {
    const condLabel = (entry.conditions||[]).length > 0
      ? entry.conditions.map(c => c.type==='npc_status' ? `${c.npc_id}.status=${c.value}` : `${c.quest_id}=${c.status}`).join(' & ')
      : '항상';
    const keysHtml = [
      ...(entry.lines||[]).map(l => renderTextKey(l.text_key)),
      ...(entry.lines||[]).flatMap(l => (l.choices||[]).filter(Boolean).map(c => renderTextKey(c.text_key)))
    ].join('');
    return `<div style="margin-bottom:8px;border:1px solid var(--border);border-radius:6px;overflow:hidden;">
      <div style="background:var(--surface2);padding:4px 10px;font-size:10px;font-family:monospace;display:flex;gap:8px;">
        <span style="color:var(--accent);font-weight:700;">${escHtml(entry.id||'?')}</span>
        <span style="color:var(--text3);">${escHtml(condLabel)}</span>
      </div>
      <div style="padding:8px 10px;">${keysHtml || '<div style="color:var(--text3);font-size:11px;">텍스트 키 없음</div>'}</div>
    </div>`;
  }).join('');
  return `<div style="margin-bottom:18px;">
    <div style="font-size:12px;font-weight:700;color:var(--text2);margin-bottom:8px;">💬 잡담 <span style="font-size:10px;color:var(--text3);font-weight:400;">npc_idle.json</span></div>
    ${cardsHtml}
  </div>`;
}

function renderTextGroupStory(npcId) {
  const events = (DQ.storyData?.events||[]).filter(e =>
    (e.triggers||[]).some(t => t.type==='npc_talk' && t.npc_id===npcId)
  );
  if (!events.length) return '';
  const cardsHtml = events.map(evt => {
    const keysHtml = [
      ...(evt.lines||[]).map(l => renderTextKey(l.text_key)),
      ...(evt.lines||[]).flatMap(l => (l.choices||[]).filter(Boolean).map(c => renderTextKey(c.text_key)))
    ].join('');
    return `<div style="margin-bottom:8px;border:1px solid var(--border);border-radius:6px;overflow:hidden;">
      <div style="background:var(--surface2);padding:4px 10px;font-size:10px;font-family:monospace;color:var(--accent);font-weight:700;">${escHtml(evt.id)}</div>
      <div style="padding:8px 10px;">${keysHtml || '<div style="color:var(--text3);font-size:11px;">텍스트 키 없음</div>'}</div>
    </div>`;
  }).join('');
  return `<div>
    <div style="font-size:12px;font-weight:700;color:var(--text2);margin-bottom:8px;">⚡ 스토리이벤트 <span style="font-size:10px;color:var(--text3);font-weight:400;">story_events.json</span></div>
    ${cardsHtml}
  </div>`;
}"""

src = replace_once(src, old_renderDQMain, new_renderDQMain, 'renderDQMain 교체')

# ═══════════════════════════════════════════════════════════════════════════════
# 5. 구 renderIdleSection / renderStorySection 제거
#    (addIdleEntry, removeIdleEntry 등 구조 편집 함수들도 잡담 편집기로 이전)
# ═══════════════════════════════════════════════════════════════════════════════
# renderIdleSection 함수 전체 제거
idle_sec_start = "// ── 아이들 분기 섹션 ──\nfunction renderIdleSection(npcId) {"
idle_sec_end   = "\n\n// ── 스토리이벤트 대사 섹션 ──"
if idle_sec_start in src and idle_sec_end in src:
    s = src.index(idle_sec_start)
    e = src.index(idle_sec_end)
    src = src[:s] + "// ── 아이들/스토리 텍스트 그룹은 renderTextGroupIdle/Story 참조 ──" + src[e:]
    print('OK    [renderIdleSection 제거]')
else:
    print('WARN  [renderIdleSection 제거]: 패턴 미발견, 스킵')

# renderStorySection 함수 전체 제거
story_sec_start = "// ── 스토리이벤트 대사 섹션 ──\nfunction renderStorySection(npcId) {"
story_sec_end   = "\n\n// ── 텍스트 저장 ──"
if story_sec_start in src and story_sec_end in src:
    s = src.index(story_sec_start)
    e = src.index(story_sec_end)
    src = src[:s] + src[e:]
    print('OK    [renderStorySection 제거]')
else:
    print('WARN  [renderStorySection 제거]: 패턴 미발견, 스킵')

# ═══════════════════════════════════════════════════════════════════════════════
# 6. dq-sub-editor 뒤에 dq-sub-chat HTML 삽입
# ═══════════════════════════════════════════════════════════════════════════════
chat_html = """
  <!-- ── 잡담 편집기 서브탭 ── -->
  <div id="dq-sub-chat" style="display:none; padding:4px 0 28px;">

    <div style="display:flex;align-items:center;gap:10px;margin-bottom:12px;padding-bottom:10px;border-bottom:1px solid var(--border);">
      <span style="font-size:12px;color:var(--text3);">💬 잡담 편집기 — npc_idle.json 구조 편집 (텍스트 내용은 대화 편집기에서)</span>
      <button class="dq-btn" style="margin-left:auto" onclick="chatSave()">💾 저장</button>
    </div>

    <div style="display:grid;grid-template-columns:180px 1fr;gap:12px;height:580px;">
      <div style="display:flex;flex-direction:column;gap:6px;overflow:hidden;">
        <button class="dq-btn" style="width:100%;flex-shrink:0" onclick="openAddNpcModal()">+ NPC 추가</button>
        <div id="dq-chat-npc-sidebar" style="flex:1;overflow-y:auto;"></div>
      </div>
      <div id="dq-chat-main" style="overflow-y:auto;padding-right:4px;"></div>
    </div>

  </div><!-- #dq-sub-chat -->

"""

src = replace_once(src,
    "  </div><!-- #dq-sub-editor -->\n\n  <!-- ── 퀘스트 편집기 서브탭",
    "  </div><!-- #dq-sub-editor -->" + chat_html + "  <!-- ── 퀘스트 편집기 서브탭",
    'dq-sub-chat HTML 삽입')

# ═══════════════════════════════════════════════════════════════════════════════
# 7. 잡담 편집기 JS 전체 추가 (submitAddNpc 이후)
# ═══════════════════════════════════════════════════════════════════════════════
chat_js = r"""

// ============================================================
// 잡담 편집기 (npc_idle.json 구조)
// ============================================================

let chatCurrentNpc = null;

function renderChatSidebar() {
  const sb = document.getElementById('dq-chat-npc-sidebar');
  if (!sb) return;
  const ids = Object.keys(DQ.idleData?.npcs || {});
  if (!ids.length) {
    sb.innerHTML = '<div style="color:var(--text3);font-size:11px;padding:8px 0">NPC 없음<br><span style="font-size:10px;">폴더 연결 후 로드됩니다</span></div>';
    return;
  }
  sb.innerHTML = ids.map(id => {
    const entries = DQ.idleData.npcs[id]?.entries || [];
    return `<div class="dq-npc-item ${id===chatCurrentNpc?'active':''}" onclick="selectChatNpc('${escHtml(id)}')">
      <div class="dq-npc-name">${escHtml(id)}</div>
      <div class="dq-npc-meta">${entries.length}개 대화 흐름</div>
    </div>`;
  }).join('');
}

function selectChatNpc(npcId) {
  chatCurrentNpc = npcId;
  renderChatSidebar();
  renderChatMain();
}

function renderChatMain() {
  const main = document.getElementById('dq-chat-main');
  if (!main) return;
  if (!chatCurrentNpc) {
    main.innerHTML = '<div style="color:var(--text3);padding:40px;text-align:center">NPC를 선택하세요</div>';
    return;
  }
  const npc = DQ.idleData?.npcs?.[chatCurrentNpc];
  const entries = npc?.entries || [];
  const entryIds = entries.map(e => e.id || '').filter(Boolean);
  const nid = escHtml(chatCurrentNpc);

  const cards = entries.map((entry, ei) => {
    // conditions
    const condHtml = (entry.conditions||[]).map((c, ci) => {
      const isQ = c.type === 'quest_status';
      const paramHtml = isQ
        ? `<input class="dq-input" style="flex:1;padding:2px 6px;font-size:10px;" placeholder="quest_id"
             value="${escHtml(c.quest_id||'')}" oninput="setChatCondField('${nid}',${ei},${ci},'quest_id',this.value)">
           <select class="dq-input" style="padding:2px 6px;font-size:10px;" onchange="setChatCondField('${nid}',${ei},${ci},'status',this.value)">
             ${['Available','InProgress','Completed','Rewarded','NotStarted'].map(s=>`<option${c.status===s?' selected':''}>${s}</option>`).join('')}
           </select>`
        : `<input class="dq-input" style="flex:1;padding:2px 6px;font-size:10px;" placeholder="npc_id"
             value="${escHtml(c.npc_id||'')}" oninput="setChatCondField('${nid}',${ei},${ci},'npc_id',this.value)">
           <input class="dq-input" style="flex:1;padding:2px 6px;font-size:10px;" placeholder="value"
             value="${escHtml(c.value||'')}" oninput="setChatCondField('${nid}',${ei},${ci},'value',this.value)">`;
      return `<div style="display:flex;align-items:center;gap:5px;margin-bottom:5px;flex-wrap:wrap;">
        <select class="dq-input" style="padding:2px 6px;font-size:10px;" onchange="setChatCondField('${nid}',${ei},${ci},'type',this.value)">
          <option${c.type==='npc_status'?' selected':''}>npc_status</option>
          <option${c.type==='quest_status'?' selected':''}>quest_status</option>
        </select>
        ${paramHtml}
        <button class="dq-btn dq-btn-sm" style="color:var(--red)" onclick="removeChatCondition('${nid}',${ei},${ci})">✕</button>
      </div>`;
    }).join('') + `<button class="dq-btn dq-btn-sm" onclick="addChatCondition('${nid}',${ei})">+ 조건</button>`;

    // lines
    const linesHtml = (entry.lines||[]).map((line, li) => {
      const isChoice = line.type === 'choice';
      const choicesHtml = isChoice
        ? (line.choices||[]).map((c, ci) => `
          <div style="display:flex;align-items:center;gap:5px;margin-bottom:4px;padding-left:16px;">
            <span style="font-size:10px;color:var(--text3);min-width:20px;">C${ci+1}</span>
            <input class="dq-input" style="flex:1;padding:2px 5px;font-size:10px;font-family:monospace;" placeholder="text_key"
              value="${escHtml(c.text_key||'')}" oninput="setChatChoiceField('${nid}',${ei},${li},${ci},'text_key',this.value)">
            <select class="dq-input" style="padding:2px 5px;font-size:10px;" onchange="setChatChoiceField('${nid}',${ei},${li},${ci},'go_to',this.value||null)">
              <option value="">이동 없음</option>
              ${entryIds.map(id=>`<option value="${escHtml(id)}"${c.go_to===id?' selected':''}>${escHtml(id)}</option>`).join('')}
            </select>
            <button class="dq-btn dq-btn-sm" style="color:var(--red)" onclick="removeChatChoice('${nid}',${ei},${li},${ci})">✕</button>
          </div>`).join('') +
          `<button class="dq-btn dq-btn-sm" style="margin-left:16px;margin-top:4px" onclick="addChatChoice('${nid}',${ei},${li})">+ 선택지</button>`
        : '';
      return `<div style="border:1px solid var(--border);border-radius:5px;padding:7px;margin-bottom:7px;">
        <div style="display:flex;align-items:center;gap:5px;margin-bottom:${isChoice?'6px':'0'};">
          <select class="dq-input" style="padding:2px 5px;font-size:10px;" onchange="setChatLineType('${nid}',${ei},${li},this.value)">
            <option${line.type!=='choice'?' selected':''} value="normal">일반발화</option>
            <option${line.type==='choice'?' selected':''} value="choice">답변요구</option>
          </select>
          <input class="dq-input" style="width:70px;padding:2px 5px;font-size:10px;" placeholder="actor"
            value="${escHtml(line.actor||'')}" oninput="setChatLineField('${nid}',${ei},${li},'actor',this.value)">
          <input class="dq-input" style="flex:1;padding:2px 5px;font-size:10px;font-family:monospace;" placeholder="text_key"
            value="${escHtml(line.text_key||'')}" oninput="setChatLineField('${nid}',${ei},${li},'text_key',this.value)">
          <button class="dq-btn dq-btn-sm" style="color:var(--red)" onclick="removeChatLine('${nid}',${ei},${li})">✕</button>
        </div>
        ${choicesHtml}
      </div>`;
    }).join('') + `<button class="dq-btn dq-btn-sm" onclick="addChatLine('${nid}',${ei})">+ 라인</button>`;

    // effects
    const effectTypes = ['give_item','start_fq_quest','receive_fq_reward','set_npc_status'];
    const effectsHtml = (entry.effects||[]).map((ef, efi) => {
      let p = '';
      if (ef.type==='start_fq_quest'||ef.type==='receive_fq_reward')
        p = `<input class="dq-input" style="flex:1;padding:2px 6px;font-size:10px;" placeholder="quest_id"
          value="${escHtml(ef.quest_id||ef.questId||'')}" oninput="setChatEffectField('${nid}',${ei},${efi},'quest_id',this.value)">`;
      else if (ef.type==='give_item')
        p = `<input class="dq-input" style="flex:1;padding:2px 6px;font-size:10px;" placeholder="item_id"
          value="${escHtml(ef.item_id||ef.itemId||'')}" oninput="setChatEffectField('${nid}',${ei},${efi},'item_id',this.value)">
          <input class="dq-input" style="width:44px;padding:2px 4px;font-size:10px;" type="number" min="1" placeholder="qty"
          value="${ef.quantity||1}" oninput="setChatEffectField('${nid}',${ei},${efi},'quantity',+this.value)">`;
      else if (ef.type==='set_npc_status')
        p = `<input class="dq-input" style="flex:1;padding:2px 6px;font-size:10px;" placeholder="npc_id"
          value="${escHtml(ef.npc_id||ef.npcId||'')}" oninput="setChatEffectField('${nid}',${ei},${efi},'npc_id',this.value)">
          <input class="dq-input" style="flex:1;padding:2px 6px;font-size:10px;" placeholder="value"
          value="${escHtml(ef.value||'')}" oninput="setChatEffectField('${nid}',${ei},${efi},'value',this.value)">`;
      return `<div style="display:flex;align-items:center;gap:5px;margin-bottom:5px;flex-wrap:wrap;">
        <select class="dq-input" style="padding:2px 6px;font-size:10px;" onchange="setChatEffectField('${nid}',${ei},${efi},'type',this.value)">
          ${effectTypes.map(t=>`<option${ef.type===t?' selected':''}>${t}</option>`).join('')}
        </select>
        ${p}
        <button class="dq-btn dq-btn-sm" style="color:var(--red)" onclick="removeChatEffect('${nid}',${ei},${efi})">✕</button>
      </div>`;
    }).join('') + `<button class="dq-btn dq-btn-sm" onclick="addChatEffect('${nid}',${ei})">+ 효과</button>`;

    return `<div style="border:1px solid var(--border);border-radius:8px;margin-bottom:14px;overflow:hidden;">
      <div style="display:flex;align-items:center;gap:8px;padding:7px 12px;background:var(--surface2);border-bottom:1px solid var(--border);">
        <span style="font-size:10px;color:var(--text3);">ID</span>
        <input class="dq-input" style="flex:1;padding:2px 8px;font-size:11px;font-family:monospace;"
          value="${escHtml(entry.id||'')}" oninput="setChatEntryId('${nid}',${ei},this.value)">
        <button class="dq-btn dq-btn-sm" style="color:var(--red)" onclick="removeChatEntry('${nid}',${ei})">× 삭제</button>
      </div>
      <div style="padding:10px 12px;display:flex;flex-direction:column;gap:12px;">
        <div><div style="font-size:11px;font-weight:700;color:#4ade80;margin-bottom:5px;">🔀 조건 <span style="font-size:10px;color:var(--text3);font-weight:400;">(빈 배열=항상 매칭)</span></div>${condHtml}</div>
        <div><div style="font-size:11px;font-weight:700;color:#fbbf24;margin-bottom:5px;">💬 대사 라인</div>${linesHtml}</div>
        <div><div style="font-size:11px;font-weight:700;color:#f87171;margin-bottom:5px;">⚡ 효과</div>${effectsHtml}</div>
      </div>
    </div>`;
  }).join('');

  main.innerHTML = `
    <div style="display:flex;justify-content:flex-end;margin-bottom:10px;">
      <button class="dq-btn" onclick="addChatEntry('${nid}')">+ 대화 흐름 추가</button>
    </div>
    ${cards || '<div style="color:var(--text3);font-size:12px;padding:8px 0;">대화 흐름 없음</div>'}
    <div style="font-size:10px;color:var(--text3);margin-top:8px;">위→아래 순서 평가. 조건 모두 충족하는 첫 entry 실행. 선택지 go_to로 다른 entry 이동.</div>
  `;
}

// ── entry CRUD ─────────────────────────────────────────────────────────────────
function addChatEntry(npcId) {
  if (!DQ.idleData) return;
  if (!DQ.idleData.npcs[npcId]) DQ.idleData.npcs[npcId] = { entries: [] };
  const idx = DQ.idleData.npcs[npcId].entries.length;
  DQ.idleData.npcs[npcId].entries.push({ id: 'entry_' + idx, conditions: [], lines: [], effects: [] });
  DQ.dirtyIdle = true; renderChatMain(); renderDQSidebar();
}
function removeChatEntry(npcId, ei) {
  if (!confirm('이 대화 흐름을 삭제하시겠습니까?')) return;
  DQ.idleData?.npcs?.[npcId]?.entries?.splice(ei, 1);
  DQ.dirtyIdle = true; renderChatMain(); renderDQSidebar(); renderDQMain();
}
function setChatEntryId(npcId, ei, v) {
  const e = DQ.idleData?.npcs?.[npcId]?.entries?.[ei]; if (!e) return;
  e.id = v; DQ.dirtyIdle = true;
}

// ── condition ──────────────────────────────────────────────────────────────────
function addChatCondition(npcId, ei) {
  const e = DQ.idleData?.npcs?.[npcId]?.entries?.[ei]; if (!e) return;
  (e.conditions = e.conditions||[]).push({ type:'npc_status', npc_id: npcId, value:'' });
  DQ.dirtyIdle = true; renderChatMain();
}
function removeChatCondition(npcId, ei, ci) {
  DQ.idleData?.npcs?.[npcId]?.entries?.[ei]?.conditions?.splice(ci, 1);
  DQ.dirtyIdle = true; renderChatMain();
}
function setChatCondField(npcId, ei, ci, field, v) {
  const c = DQ.idleData?.npcs?.[npcId]?.entries?.[ei]?.conditions?.[ci]; if (!c) return;
  c[field] = v; DQ.dirtyIdle = true;
  if (field === 'type') renderChatMain();
}

// ── line ───────────────────────────────────────────────────────────────────────
function addChatLine(npcId, ei) {
  const e = DQ.idleData?.npcs?.[npcId]?.entries?.[ei]; if (!e) return;
  (e.lines = e.lines||[]).push({ type:'normal', actor: npcId, text_key:'', choices: null });
  DQ.dirtyIdle = true; renderChatMain();
}
function removeChatLine(npcId, ei, li) {
  DQ.idleData?.npcs?.[npcId]?.entries?.[ei]?.lines?.splice(li, 1);
  DQ.dirtyIdle = true; renderChatMain();
}
function setChatLineField(npcId, ei, li, field, v) {
  const line = DQ.idleData?.npcs?.[npcId]?.entries?.[ei]?.lines?.[li]; if (!line) return;
  line[field] = v; DQ.dirtyIdle = true;
}
function setChatLineType(npcId, ei, li, type) {
  const line = DQ.idleData?.npcs?.[npcId]?.entries?.[ei]?.lines?.[li]; if (!line) return;
  line.type = type;
  if (type === 'choice' && !line.choices) line.choices = [];
  if (type === 'normal') line.choices = null;
  DQ.dirtyIdle = true; renderChatMain();
}

// ── choice ─────────────────────────────────────────────────────────────────────
function addChatChoice(npcId, ei, li) {
  const line = DQ.idleData?.npcs?.[npcId]?.entries?.[ei]?.lines?.[li]; if (!line) return;
  (line.choices = line.choices||[]).push({ text_key:'', go_to: null });
  DQ.dirtyIdle = true; renderChatMain();
}
function removeChatChoice(npcId, ei, li, ci) {
  DQ.idleData?.npcs?.[npcId]?.entries?.[ei]?.lines?.[li]?.choices?.splice(ci, 1);
  DQ.dirtyIdle = true; renderChatMain();
}
function setChatChoiceField(npcId, ei, li, ci, field, v) {
  const c = DQ.idleData?.npcs?.[npcId]?.entries?.[ei]?.lines?.[li]?.choices?.[ci]; if (!c) return;
  c[field] = v || null; DQ.dirtyIdle = true;
}

// ── effect ─────────────────────────────────────────────────────────────────────
function addChatEffect(npcId, ei) {
  const e = DQ.idleData?.npcs?.[npcId]?.entries?.[ei]; if (!e) return;
  (e.effects = e.effects||[]).push({ type:'set_npc_status', npc_id: npcId, value:'' });
  DQ.dirtyIdle = true; renderChatMain();
}
function removeChatEffect(npcId, ei, efi) {
  DQ.idleData?.npcs?.[npcId]?.entries?.[ei]?.effects?.splice(efi, 1);
  DQ.dirtyIdle = true; renderChatMain();
}
function setChatEffectField(npcId, ei, efi, field, v) {
  const ef = DQ.idleData?.npcs?.[npcId]?.entries?.[ei]?.effects?.[efi]; if (!ef) return;
  ef[field] = v; DQ.dirtyIdle = true;
  if (field === 'type') renderChatMain();
}

// ── chatSave ───────────────────────────────────────────────────────────────────
async function chatSave() {
  if (!DQ.dirHandle) { alert('먼저 폴더를 연결해주세요.'); return; }
  let saved = [];
  if (DQ.dirtyIdle) { await saveIdleFile(); saved.push('npc_idle.json'); }
  if (DQ.dirtyText) { await saveJsonFile(DQ.lang); saved.push('ko.json'); }
  if (!saved.length) updateSyncBar('ok', '변경사항 없음');
  else { updateSyncBar('ok', saved.join(' + ') + ' 저장 완료'); showToast('저장 완료'); }
}

"""

target_after = "  DQ.dirtyIdle = true;\n  closeModal('dq-modal-add-npc');\n  renderDQSidebar();\n  selectNpc(id);\n\n}\n"
if target_after in src:
    idx = src.index(target_after) + len(target_after)
    src = src[:idx] + chat_js + src[idx:]
    print('OK    [잡담 편집기 JS 삽입]')
else:
    print('ERROR [잡담 편집기 JS 삽입]: 삽입 위치 없음')

# ═══════════════════════════════════════════════════════════════════════════════
# 8. renderEvtMain lines 섹션 업데이트 (타입 + 선택지)
# ═══════════════════════════════════════════════════════════════════════════════
old_lines_section = """  // 라인
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
  }).join('') + `<button class="dq-btn dq-btn-sm" onclick="addEvtLine('${eid}')">+ 라인</button>`;"""

new_lines_section = """  // 라인
  const evtEntryIds = (DQ.storyData?.events||[]).map(e => e.id).filter(Boolean);
  const linesHtml = (evt.lines||[]).map((line, li) => {
    const key = line.text_key||'';
    const val = DQ.data[DQ.lang]?.[key]||'';
    const isChoice = line.type === 'choice';
    const choicesHtml = isChoice
      ? (line.choices||[]).map((c, ci) => {
          const cKey = c.text_key||'';
          const cVal = DQ.data[DQ.lang]?.[cKey]||'';
          return `<div style="border-left:2px solid var(--border);padding-left:10px;margin:6px 0 6px 10px;">
            <div style="display:flex;align-items:center;gap:5px;margin-bottom:4px;">
              <span style="font-size:10px;color:var(--text3);min-width:24px;">C${ci+1}</span>
              <input class="dq-input" style="flex:1;padding:2px 5px;font-size:10px;font-family:monospace;" placeholder="text_key"
                value="${escHtml(cKey)}" oninput="setEvtChoiceField('${eid}',${li},${ci},'text_key',this.value)">
              <select class="dq-input" style="padding:2px 5px;font-size:10px;" onchange="setEvtChoiceField('${eid}',${li},${ci},'go_to',this.value||null)">
                <option value="">이동 없음</option>
                ${evtEntryIds.map(id=>`<option value="${escHtml(id)}"${c.go_to===id?' selected':''}>${escHtml(id)}</option>`).join('')}
              </select>
              <button class="dq-btn dq-btn-sm" style="color:var(--red)" onclick="removeEvtChoice('${eid}',${li},${ci})">✕</button>
            </div>
            <div style="font-size:10px;color:var(--text3);padding:2px 7px;font-family:monospace;background:var(--surface2);border-radius:3px;margin-bottom:3px;">${escHtml(cKey)||'(키 미설정)'}</div>
            <div style="font-size:11px;color:var(--text2);padding:2px 0;">${escHtml(cVal)||'<span style="color:var(--text3);">텍스트 없음</span>'}</div>
          </div>`;
        }).join('') + `<button class="dq-btn dq-btn-sm" style="margin-left:10px;margin-top:4px" onclick="addEvtChoice('${eid}',${li})">+ 선택지</button>`
      : '';
    return `<div style="border:1px solid var(--border);border-radius:6px;padding:8px;margin-bottom:8px;">
      <div style="display:flex;align-items:center;gap:6px;margin-bottom:5px;">
        <select class="dq-input" style="padding:2px 5px;font-size:10px;" onchange="setEvtLineType('${eid}',${li},this.value)">
          <option${line.type!=='choice'?' selected':''} value="normal">일반발화</option>
          <option${line.type==='choice'?' selected':''} value="choice">답변요구</option>
        </select>
        <input class="dq-input" style="width:70px;padding:3px 6px;font-size:10px;" placeholder="actor"
          value="${escHtml(line.actor||'')}" oninput="setEvtLineField('${eid}',${li},'actor',this.value)">
        <input class="dq-input" style="flex:1;padding:3px 6px;font-size:10px;font-family:monospace;" placeholder="text_key"
          value="${escHtml(key)}" oninput="setEvtLineField('${eid}',${li},'text_key',this.value)">
        <button class="dq-btn dq-btn-sm" style="color:var(--red)" onclick="removeEvtLine('${eid}',${li})">✕</button>
      </div>
      <div style="font-size:11px;color:var(--text2);background:var(--surface2);padding:4px 8px;border-radius:4px;margin-bottom:${isChoice?'6px':'0'};">${escHtml(val)||'<span style="color:var(--text3);">텍스트 없음 (대화 편집기에서 입력)</span>'}</div>
      ${choicesHtml}
    </div>`;
  }).join('') + `<button class="dq-btn dq-btn-sm" onclick="addEvtLine('${eid}')">+ 라인</button>`;"""

src = replace_once(src, old_lines_section, new_lines_section, 'renderEvtMain lines 섹션 업데이트')

# 이벤트 라인 타입/선택지 편집 함수 추가 (evtSave 앞에)
evt_extra_js = """
function setEvtLineType(evtId, li, type) {
  const line = getEvt(evtId)?.lines?.[li]; if (!line) return;
  line.type = type;
  if (type === 'choice' && !line.choices) line.choices = [];
  if (type === 'normal') line.choices = null;
  DQ.dirtyStory = true; renderEvtMain();
}
function addEvtChoice(evtId, li) {
  const line = getEvt(evtId)?.lines?.[li]; if (!line) return;
  (line.choices = line.choices||[]).push({ text_key:'', go_to: null });
  DQ.dirtyStory = true; renderEvtMain();
}
function removeEvtChoice(evtId, li, ci) {
  getEvt(evtId)?.lines?.[li]?.choices?.splice(ci, 1);
  DQ.dirtyStory = true; renderEvtMain();
}
function setEvtChoiceField(evtId, li, ci, field, v) {
  const c = getEvt(evtId)?.lines?.[li]?.choices?.[ci]; if (!c) return;
  c[field] = v || null; DQ.dirtyStory = true;
}

"""
src = replace_once(src,
    "async function evtSave() {",
    evt_extra_js + "async function evtSave() {",
    '이벤트 선택지 편집 함수 추가')

# ═══════════════════════════════════════════════════════════════════════════════
# 저장
# ═══════════════════════════════════════════════════════════════════════════════
with open(HTML_PATH, 'w', encoding='utf-8') as f:
    f.write(src)
lines_count = src.count('\n')
print(f'\n총 라인 수: {lines_count}')
print('저장 완료')
