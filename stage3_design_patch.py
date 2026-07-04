"""
디자인 개선 패치
- 편집기 공통 CSS 클래스 추가
- renderTextKey / renderTextGroupIdle / renderTextGroupStory 개선
- renderChatMain 폰트 크기 및 레이아웃 개선
- renderEvtMain 폰트 크기 개선
"""
import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

HTML_PATH = r'C:\workspace\Farming_01_16_Final\project_structure.html'
with open(HTML_PATH, encoding='utf-8') as f:
    src = f.read()

def rep(old, new, label):
    if old not in src:
        print('MISS  [' + label + ']')
        return
    globals()['src'] = globals()['src'].replace(old, new, 1)
    print('OK    [' + label + ']')

# ═══════════════════════════════════════════════════════════════════════════════
# 1. CSS 유틸 클래스 추가 (.dq-npc-meta 뒤에)
# ═══════════════════════════════════════════════════════════════════════════════
css_add = """
  /* ── 편집기 공통 카드/섹션 ── */
  .ed-card { border:1px solid var(--border); border-radius:8px; margin-bottom:12px; overflow:hidden; }
  .ed-card-head { display:flex; align-items:center; gap:10px; padding:9px 14px; background:var(--surface2); border-bottom:1px solid var(--border); }
  .ed-card-id { font-family:monospace; font-size:12px; font-weight:700; color:var(--accent); }
  .ed-card-meta { font-size:11px; color:var(--text3); flex:1; }
  .ed-card-body { padding:12px 14px; }
  .ed-sec { background:var(--surface); border:1px solid var(--border); border-radius:6px; padding:10px 12px; margin-bottom:10px; }
  .ed-sec-title { font-size:11px; font-weight:700; text-transform:uppercase; letter-spacing:.05em; margin-bottom:8px; }
  .ed-row { display:flex; align-items:center; gap:8px; margin-bottom:7px; }
  .ed-key { font-family:monospace; font-size:11px; color:var(--accent); background:var(--surface2); padding:2px 8px; border-radius:4px; display:inline-block; margin-bottom:4px; }
  .ed-choice-row { display:flex; align-items:center; gap:6px; margin-bottom:5px; padding-left:14px; }
  .ed-choice-num { font-size:11px; color:var(--text3); min-width:22px; font-family:monospace; }
"""

rep(
    '  .dq-npc-meta { font-size: 10px; color: var(--text3); margin-top: 1px; }',
    '  .dq-npc-meta { font-size: 10px; color: var(--text3); margin-top: 1px; }\n' + css_add,
    'CSS 추가'
)

# ═══════════════════════════════════════════════════════════════════════════════
# 2. renderTextKey 개선 (텍스트 편집기 개별 키)
# ═══════════════════════════════════════════════════════════════════════════════
old_renderTextKey = """function renderTextKey(key) {
  if (!key) return '';
  const val = DQ.data[DQ.lang]?.[key] || '';
  return `<div style="margin-bottom:8px;">
    <div style="font-size:10px;color:var(--text3);font-family:monospace;background:var(--surface2);padding:2px 7px;border-radius:4px;display:inline-block;margin-bottom:3px;">${escHtml(key)}</div>
    <textarea class="dq-input" rows="2" style="width:100%;resize:none;overflow:hidden;"
      oninput="setDqText('${escHtml(key)}',this.value);this.style.height='auto';this.style.height=this.scrollHeight+'px'"
      placeholder="텍스트를 입력하세요…">${escHtml(val)}</textarea>
  </div>`;
}"""

new_renderTextKey = """function renderTextKey(key) {
  if (!key) return '';
  const val = DQ.data[DQ.lang]?.[key] || '';
  return `<div style="margin-bottom:10px;">
    <div class="ed-key">${escHtml(key)}</div>
    <textarea class="dq-input" rows="2" style="width:100%;resize:none;overflow:hidden;font-size:13px;line-height:1.6;"
      oninput="setDqText('${escHtml(key)}',this.value);this.style.height='auto';this.style.height=this.scrollHeight+'px'"
      placeholder="텍스트를 입력하세요…">${escHtml(val)}</textarea>
  </div>`;
}"""

rep(old_renderTextKey, new_renderTextKey, 'renderTextKey 개선')

# ═══════════════════════════════════════════════════════════════════════════════
# 3. renderTextGroupIdle 개선
# ═══════════════════════════════════════════════════════════════════════════════
old_idle = """function renderTextGroupIdle(npcId) {
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
}"""

new_idle = """function renderTextGroupIdle(npcId) {
  const entries = DQ.idleData?.npcs?.[npcId]?.entries || [];
  if (!entries.length) return '';
  const cardsHtml = entries.map(entry => {
    const condLabel = (entry.conditions||[]).length > 0
      ? entry.conditions.map(c => c.type==='npc_status' ? `${c.npc_id}.status="${c.value}"` : `${c.quest_id}=${c.status}`).join(' & ')
      : '조건 없음 (항상)';
    const keysHtml = [
      ...(entry.lines||[]).map(l => renderTextKey(l.text_key)),
      ...(entry.lines||[]).flatMap(l => (l.choices||[]).filter(Boolean).map(c => renderTextKey(c.text_key)))
    ].join('');
    return `<div class="ed-card">
      <div class="ed-card-head">
        <span class="ed-card-id">${escHtml(entry.id||'?')}</span>
        <span class="ed-card-meta">${escHtml(condLabel)}</span>
      </div>
      <div class="ed-card-body">${keysHtml || '<div style="color:var(--text3);font-size:12px;padding:4px 0;">텍스트 키 없음</div>'}</div>
    </div>`;
  }).join('');
  return `<div style="margin-bottom:20px;">
    <div style="font-size:13px;font-weight:700;color:var(--text);margin-bottom:10px;">💬 잡담 <span style="font-size:11px;color:var(--text3);font-weight:400; margin-left:4px;">npc_idle.json</span></div>
    ${cardsHtml}
  </div>`;
}"""

rep(old_idle, new_idle, 'renderTextGroupIdle 개선')

# ═══════════════════════════════════════════════════════════════════════════════
# 4. renderTextGroupStory 개선
# ═══════════════════════════════════════════════════════════════════════════════
old_story = """function renderTextGroupStory(npcId) {
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

new_story = """function renderTextGroupStory(npcId) {
  const events = (DQ.storyData?.events||[]).filter(e =>
    (e.triggers||[]).some(t => t.type==='npc_talk' && t.npc_id===npcId)
  );
  if (!events.length) return '';
  const cardsHtml = events.map(evt => {
    const condLabel = (evt.conditions||[]).map(c =>
      c.type==='quest_status' ? `${c.quest_id}=${c.status}` : `${c.npc_id}.status="${c.value}"`
    ).join(' & ') || '조건 없음';
    const keysHtml = [
      ...(evt.lines||[]).map(l => renderTextKey(l.text_key)),
      ...(evt.lines||[]).flatMap(l => (l.choices||[]).filter(Boolean).map(c => renderTextKey(c.text_key)))
    ].join('');
    return `<div class="ed-card">
      <div class="ed-card-head">
        <span class="ed-card-id">${escHtml(evt.id)}</span>
        <span class="ed-card-meta">${escHtml(condLabel)}</span>
      </div>
      <div class="ed-card-body">${keysHtml || '<div style="color:var(--text3);font-size:12px;padding:4px 0;">텍스트 키 없음</div>'}</div>
    </div>`;
  }).join('');
  return `<div>
    <div style="font-size:13px;font-weight:700;color:var(--text);margin-bottom:10px;">⚡ 스토리이벤트 <span style="font-size:11px;color:var(--text3);font-weight:400; margin-left:4px;">story_events.json</span></div>
    ${cardsHtml}
  </div>`;
}"""

rep(old_story, new_story, 'renderTextGroupStory 개선')

# ═══════════════════════════════════════════════════════════════════════════════
# 5. renderChatMain 개선 (폰트 크기 10px→12px, ed-card/ed-sec 클래스 사용)
# ═══════════════════════════════════════════════════════════════════════════════
# 조건 섹션 개별 행: 10px → 12px
src = src.replace(
    "style=\"display:flex; align-items:center; gap:5px; margin-bottom:5px; flex-wrap:wrap;\">",
    "class=\"ed-row\" style=\"flex-wrap:wrap;\">",
    20  # 최대 20회
)

# 조건 타입 select 10px → 12px
src = src.replace(
    'style="padding:2px 6px;font-size:10px;" onchange="setChatCondField',
    'class="dq-input" style="padding:5px 8px;font-size:12px;" onchange="setChatCondField',
    20
)
# 조건 파라미터 input 10px → 12px
src = src.replace(
    'style="flex:1;padding:2px 6px;font-size:10px;" placeholder="quest_id"\n             value="${escHtml(c.quest_id||\'\')}',
    'class="dq-input" style="flex:1;font-size:12px;" placeholder="quest_id"\n             value="${escHtml(c.quest_id||\'\')}',
    10
)
src = src.replace(
    'style="flex:1;padding:2px 6px;font-size:10px;" placeholder="npc_id"\n             value="${escHtml(c.npc_id||\'\')}',
    'class="dq-input" style="flex:1;font-size:12px;" placeholder="npc_id"\n             value="${escHtml(c.npc_id||\'\')}',
    10
)
src = src.replace(
    'style="flex:1;padding:2px 6px;font-size:10px;" placeholder="value"\n             value="${escHtml(c.value||\'\')}',
    'class="dq-input" style="flex:1;font-size:12px;" placeholder="value"\n             value="${escHtml(c.value||\'\')}',
    10
)
# 조건 status select 10px → 12px
src = src.replace(
    'style="padding:2px 6px;font-size:10px;" onchange="setChatCondField(\'${nid}\',${ei},${ci},\'status\'',
    'class="dq-input" style="padding:5px 8px;font-size:12px;" onchange="setChatCondField(\'${nid}\',${ei},${ci},\'status\'',
    10
)

# 라인 select (일반발화/답변요구) 10px → 12px
src = src.replace(
    'style="padding:2px 5px;font-size:10px;" onchange="setChatLineType',
    'class="dq-input" style="padding:5px 8px;font-size:12px;" onchange="setChatLineType',
    10
)
# 라인 actor input 10px → 12px
src = src.replace(
    'style="width:70px;padding:2px 5px;font-size:10px;" placeholder="actor"',
    'class="dq-input" style="width:80px;font-size:12px;" placeholder="actor"',
    10
)
# 라인 text_key input 10px → 12px
src = src.replace(
    'style="flex:1;padding:2px 5px;font-size:10px;font-family:monospace;" placeholder="text_key"\n            value="${escHtml(line.text_key||\'\')}',
    'class="dq-input" style="flex:1;font-size:12px;font-family:monospace;" placeholder="text_key"\n            value="${escHtml(line.text_key||\'\')}',
    10
)

# 선택지 행 (chat): 10px → 12px
src = src.replace(
    '"display:flex;align-items:center;gap:5px;margin-bottom:4px;padding-left:16px;"',
    '"display:flex;align-items:center;gap:6px;margin-bottom:5px;padding-left:16px;"',
    10
)
src = src.replace(
    'style="font-size:10px;color:var(--text3);min-width:20px;">C${ci+1}<',
    'style="font-size:11px;color:var(--text3);min-width:24px;font-family:monospace;">C${ci+1}<',
    10
)
src = src.replace(
    'style="flex:1;padding:2px 5px;font-size:10px;font-family:monospace;" placeholder="text_key"\n              value="${escHtml(c.text_key||\'\')}',
    'class="dq-input" style="flex:1;font-size:12px;font-family:monospace;" placeholder="text_key"\n              value="${escHtml(c.text_key||\'\')}',
    10
)
src = src.replace(
    'style="padding:2px 5px;font-size:10px;" onchange="setChatChoiceField',
    'class="dq-input" style="padding:5px 8px;font-size:12px;" onchange="setChatChoiceField',
    10
)

# 효과 행 (chat): 10px → 12px
src = src.replace(
    'style="padding:2px 6px;font-size:10px;" onchange="setChatEffectField',
    'class="dq-input" style="padding:5px 8px;font-size:12px;" onchange="setChatEffectField',
    10
)
src = src.replace(
    'style="flex:1;padding:2px 6px;font-size:10px;" placeholder="quest_id"\n          value="${escHtml(ef.quest_id||ef.questId||\'\')}',
    'class="dq-input" style="flex:1;font-size:12px;" placeholder="quest_id"\n          value="${escHtml(ef.quest_id||ef.questId||\'\')}',
    10
)
src = src.replace(
    'style="flex:1;padding:2px 6px;font-size:10px;" placeholder="item_id"\n          value="${escHtml(ef.item_id||ef.itemId||\'\')}',
    'class="dq-input" style="flex:1;font-size:12px;" placeholder="item_id"\n          value="${escHtml(ef.item_id||ef.itemId||\'\')}',
    10
)
src = src.replace(
    'style="width:44px;padding:2px 4px;font-size:10px;" type="number"',
    'class="dq-input" style="width:54px;font-size:12px;" type="number"',
    10
)
src = src.replace(
    'style="flex:1;padding:2px 6px;font-size:10px;" placeholder="npc_id"\n          value="${escHtml(ef.npc_id||ef.npcId||\'\')}',
    'class="dq-input" style="flex:1;font-size:12px;" placeholder="npc_id"\n          value="${escHtml(ef.npc_id||ef.npcId||\'\')}',
    10
)
src = src.replace(
    'style="flex:1;padding:2px 6px;font-size:10px;" placeholder="value"\n          value="${escHtml(ef.value||\'\')}',
    'class="dq-input" style="flex:1;font-size:12px;" placeholder="value"\n          value="${escHtml(ef.value||\'\')}',
    10
)

# 카드 헤더 ID input 크기 개선
src = src.replace(
    'style="flex:1;padding:2px 8px;font-size:11px;font-family:monospace;"\n          value="${escHtml(entry.id||\'\')}',
    'class="dq-input" style="flex:1;font-size:13px;font-family:monospace;font-weight:700;"\n          value="${escHtml(entry.id||\'\')}',
    10
)

# 섹션 타이틀 11px → 13px (chat 편집기 내 색깔 있는 제목들)
src = src.replace(
    'style="font-size:11px;font-weight:700;color:#4ade80;margin-bottom:5px;">🔀 조건',
    'style="font-size:12px;font-weight:700;color:#4ade80;margin-bottom:8px;">🔀 조건',
    10
)
src = src.replace(
    'style="font-size:11px;font-weight:700;color:#fbbf24;margin-bottom:5px;">💬 대사',
    'style="font-size:12px;font-weight:700;color:#fbbf24;margin-bottom:8px;">💬 대사',
    10
)
src = src.replace(
    'style="font-size:11px;font-weight:700;color:#f87171;margin-bottom:5px;">⚡ 효과',
    'style="font-size:12px;font-weight:700;color:#f87171;margin-bottom:8px;">⚡ 효과',
    10
)

# chat 편집기 entry 카드 패딩 개선
src = src.replace(
    'style="display:flex;flex-direction:column;gap:12px;"',
    'style="display:flex;flex-direction:column;gap:14px;"',
    10
)

# 이벤트 편집기 라인타입 select 개선
src = src.replace(
    'style="padding:2px 5px;font-size:10px;" onchange="setEvtLineType',
    'class="dq-input" style="padding:5px 8px;font-size:12px;" onchange="setEvtLineType',
    10
)
# 이벤트 편집기 라인 actor input 개선
src = src.replace(
    'style="width:70px;padding:3px 6px;font-size:10px;" placeholder="actor"\n          value="${escHtml(line.actor||\'\')}',
    'class="dq-input" style="width:80px;font-size:12px;" placeholder="actor"\n          value="${escHtml(line.actor||\'\')}',
    10
)
# 이벤트 편집기 라인 text_key input 개선
src = src.replace(
    'style="flex:1;padding:3px 6px;font-size:10px;font-family:monospace;" placeholder="text_key"\n          value="${escHtml(key)}"',
    'class="dq-input" style="flex:1;font-size:12px;font-family:monospace;" placeholder="text_key"\n          value="${escHtml(key)}"',
    10
)

print('OK    [폰트 크기 일괄 개선]')

# ═══════════════════════════════════════════════════════════════════════════════
# 6. dq-sub-chat 헤더 폰트 개선
# ═══════════════════════════════════════════════════════════════════════════════
rep(
    '<span style="font-size:12px;color:var(--text3);">💬 잡담 편집기 — npc_idle.json 구조 편집 (텍스트 내용은 대화 편집기에서)</span>',
    '<span style="font-size:13px;color:var(--text2);">💬 잡담 편집기 <span style="font-size:12px;color:var(--text3);font-weight:400;">— npc_idle.json 구조 편집 · 텍스트는 대화 편집기에서</span></span>',
    'dq-sub-chat 헤더 개선'
)

# 이벤트 편집기 헤더 개선
rep(
    '<span style="font-size:12px;color:var(--text3);">📋 story_events.json 편집 — 트리거·조건·대사·효과·on_end를 수정합니다</span>',
    '<span style="font-size:13px;color:var(--text2);">⚡ 이벤트 편집기 <span style="font-size:12px;color:var(--text3);font-weight:400;">— story_events.json 구조 편집</span></span>',
    '이벤트 편집기 헤더 개선'
)

# ═══════════════════════════════════════════════════════════════════════════════
# 저장
# ═══════════════════════════════════════════════════════════════════════════════
with open(HTML_PATH, 'w', encoding='utf-8') as f:
    f.write(src)
print('\n저장 완료. 총 라인: ' + str(src.count('\n')))
