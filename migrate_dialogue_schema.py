"""
대화 시스템 JSON 스키마 마이그레이션
npc_idle.json : status(str) → conditions[], entry id 추가, line type 추가
story_events.json : line에 type:"normal" 추가
"""
import json, sys, io, copy
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

BASE = r'C:\workspace\Farming_01_16_Final\Assets\StreamingAssets\Dialogue'

# ── npc_idle.json ─────────────────────────────────────────────────────────────
idle_path = BASE + r'\npc_idle.json'
with open(idle_path, encoding='utf-8') as f:
    idle = json.load(f)

for npc_id, npc_data in idle.get('npcs', {}).items():
    id_counts = {}
    for entry in npc_data.get('entries', []):
        # 1) id 생성: status값 → id, 빈값 → "default"
        raw = entry.get('status', '').strip()
        base_id = raw if raw else 'default'
        id_counts[base_id] = id_counts.get(base_id, 0) + 1
        entry_id = base_id if id_counts[base_id] == 1 else f'{base_id}_{id_counts[base_id]}'
        entry['id'] = entry_id

        # 2) status → conditions[]
        if raw:
            entry['conditions'] = [
                {'type': 'npc_status', 'npc_id': npc_id, 'value': raw}
            ]
        else:
            entry['conditions'] = []
        del entry['status']

        # 3) line에 type 추가
        for line in entry.get('lines', []):
            if 'type' not in line:
                line['type'] = 'normal'
            # choices 없으면 명시
            if 'choices' not in line:
                line['choices'] = None

        # 4) 필드 순서 정렬 (가독성)
        ordered = {'id': entry['id'], 'conditions': entry['conditions'],
                   'lines': entry['lines'], 'effects': entry.get('effects', [])}
        entry.clear()
        entry.update(ordered)

idle['_comment'] = (
    'NPC 잡담 대화. 스토리이벤트 매칭이 없을 때 재생. '
    'entries를 위→아래 순서로 평가, conditions 모두 충족하는 첫 entry 실행. '
    'conditions 빈 배열=항상. entry간 go_to로 흐름 연결.'
)

with open(idle_path, 'w', encoding='utf-8') as f:
    json.dump(idle, f, ensure_ascii=False, indent=2)
print('npc_idle.json 마이그레이션 완료')

# ── story_events.json ─────────────────────────────────────────────────────────
story_path = BASE + r'\story_events.json'
with open(story_path, encoding='utf-8') as f:
    story = json.load(f)

for evt in story.get('events', []):
    for line in evt.get('lines', []):
        if 'type' not in line:
            line['type'] = 'normal'
        if 'choices' not in line:
            line['choices'] = None

with open(story_path, 'w', encoding='utf-8') as f:
    json.dump(story, f, ensure_ascii=False, indent=2)
print('story_events.json 마이그레이션 완료')

# ── 결과 확인 ─────────────────────────────────────────────────────────────────
print('\n=== npc_idle.json 결과 ===')
for npc_id, npc_data in idle['npcs'].items():
    print(f'  NPC: {npc_id}')
    for e in npc_data['entries']:
        print(f'    entry id={e["id"]}  conditions={e["conditions"]}')
        for l in e['lines']:
            print(f'      line type={l["type"]}  key={l.get("text_key")}')

print('\n=== story_events.json 결과 (라인 타입) ===')
for evt in story['events']:
    print(f'  [{evt["id"]}]')
    for l in evt.get('lines', []):
        print(f'    type={l["type"]}  key={l.get("text_key")}')
