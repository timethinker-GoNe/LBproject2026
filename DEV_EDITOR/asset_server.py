#!/usr/bin/env python3
"""
FarmingEngine ScriptableObject 웹 에디터 서버
=============================================
브라우저(project_structure.html)에서 Unity ScriptableObject를
직접 읽고 수정할 수 있게 해주는 로컬 HTTP 서버.

Unity 에디터는 .asset 파일 변경을 자동으로 감지하여 리로드합니다.

사용법:
  1. Python 3.8 이상 설치 확인
  2. pip install flask flask-cors
  3. 이 파일이 있는 폴더(프로젝트 루트)에서: python asset_server.py
  4. 브라우저에서 project_structure.html 열기
  5. 대시보드 상단에 "서버 연결됨" 표시 확인
"""

import sys
import io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

from flask import Flask, jsonify, request, send_file
from flask_cors import CORS
import re
import os
import glob
import json
import shutil
import struct
from datetime import datetime

app = Flask(__name__)
CORS(app)  # 브라우저에서 localhost:5000 접근 허용

# ── 경로 설정 ─────────────────────────────────────────────────────────
SCRIPT_DIR      = os.path.dirname(os.path.abspath(__file__))   # DEV_EDITOR/
PROJECT_ROOT    = os.path.dirname(SCRIPT_DIR)                  # Farming_01_16_Final/
ASSETS_ROOT     = os.path.join(PROJECT_ROOT, 'Assets', 'FarmingEngine_study', 'Resources')
PREFABS_ROOT    = os.path.join(PROJECT_ROOT, 'Assets', 'FarmingEngine_study', 'Prefabs')
THUMBNAILS_ROOT = os.path.join(PROJECT_ROOT, 'Assets', 'StreamingAssets', 'prefab_thumbnails')
NOTES_ROOT      = os.path.join(SCRIPT_DIR, 'notes')            # DEV_EDITOR/notes/
DEVPLAN_PATH    = os.path.join(NOTES_ROOT, 'devplan.json')
NPC_SHOP_PATH   = os.path.join(PROJECT_ROOT, 'Assets', 'StreamingAssets', 'npc_shop.json')
SCENE_VER_ROOT  = os.path.join(NOTES_ROOT, 'scene_versions')
CHARACTER_IMPORT_ROOT = os.path.join(PROJECT_ROOT, 'Assets', 'FarmingEngine_study', 'CharacterImports')
CHARACTER_REQUESTS    = os.path.join(PROJECT_ROOT, 'Assets', 'StreamingAssets', 'CharacterPrefabRequests.json')

# ── 스프라이트 교체 시스템 ────────────────────────────────────────────
SPRITE_FOLDERS = {
    'FE/UI': os.path.join(PROJECT_ROOT, 'Assets', 'FarmingEngine_study', 'Sprites', 'UI'),
    'DQ':    os.path.join(PROJECT_ROOT, 'Assets', 'DialogueQuests', 'Sprites'),
}
SPRITE_HISTORY_ROOT = os.path.join(SCRIPT_DIR, 'sprite_history')

# ── 프리팹 카테고리 정의 ──────────────────────────────────────────────
# key : (한글 표시명, scene map type)
PREFAB_CATEGORIES = [
    ('Constructions', '건설물',  'structure'),
    ('Plants',        '식물',    'plant'),
    ('Environment',   '환경',    'structure'),
    ('Decoration',    '장식',    'structure'),
    ('Town',          '마을/NPC','npc'),
    ('Animals',       '동물',    'npc'),
    ('Enemies',       '적',      'npc'),
    ('Terrain',       '지형',    'structure'),
    ('Zones',         '존',      'quest_zone'),
]

# ── ScriptableObject 카테고리 정의 ────────────────────────────────────
# key : (glob 패턴, 한글 표시명)
CATEGORIES = {
    'items_food':     ('Items/Food/**/*.asset',       'Items / 음식'),
    'items_equip':    ('Items/Equipment/**/*.asset',  'Items / 장비'),
    'items_cooking':  ('Items/Cooking/**/*.asset',    'Items / 요리'),
    'items_other':    ('Items/Other/**/*.asset',      'Items / 기타'),
    'items_animals':  ('Items/Animals/**/*.asset',    'Items / 동물 아이템'),
    'plants':         ('Plants/**/*.asset',           '식물'),
    'constructions':  ('Constructions/**/*.asset',    '건설물'),
    'actions':        ('Actions/**/*.asset',          '액션'),
    'characters':     ('Characters/**/*.asset',       '캐릭터/동물'),
}

# Unity 내부 전용 필드 — 절대 수정 불가
READONLY_KEYS = {
    'm_ObjectHideFlags', 'm_CorrespondingSourceObject', 'm_PrefabInstance',
    'm_PrefabAsset', 'm_GameObject', 'm_Enabled', 'm_EditorHideFlags',
    'm_Script', 'm_Name', 'm_EditorClassIdentifier',
}

# 참조 타입인지 확인 (fileID 포함 값 = 다른 에셋 참조)
def is_reference(value: str) -> bool:
    return value.startswith('{') or value.startswith('-')

def _folder_key(folder: str) -> str:
    """폴더명을 파일시스템 안전한 키로 변환. 예: 'FE/UI' → 'FE_UI'"""
    return folder.replace('/', '_')

def _validate_sprite(folder: str, filename: str):
    """(abs_path, error) 반환. 보안 검증 포함."""
    if folder not in SPRITE_FOLDERS:
        return None, f'알 수 없는 폴더: {folder}'
    if not re.match(r'^[\w\- .]+\.png$', filename, re.IGNORECASE):
        return None, '잘못된 파일명 (영문·숫자·공백·대시·점·_·.png 만 허용)'
    base = os.path.normpath(SPRITE_FOLDERS[folder])
    abs_path = os.path.normpath(os.path.join(base, filename))
    if not abs_path.startswith(base):
        return None, '경로 탈출 감지'
    return abs_path, None

def _read_png_info(file_path: str) -> dict:
    """PNG 헤더를 직접 파싱해 너비·높이·색상 타입 반환 (PIL 불필요)."""
    COLOR_TYPE = {0:'Grayscale', 2:'RGB', 3:'Indexed', 4:'Grayscale+A', 6:'RGBA'}
    try:
        with open(file_path, 'rb') as f:
            hdr = f.read(26)
        if len(hdr) < 26 or hdr[:8] != b'\x89PNG\r\n\x1a\n':
            return {}
        w, h       = struct.unpack('>II', hdr[16:24])
        bit_depth  = hdr[24]
        color_type = hdr[25]
        return {
            'width':      w,
            'height':     h,
            'bit_depth':  bit_depth,
            'color_type': COLOR_TYPE.get(color_type, f'unknown({color_type})'),
            'file_size':  os.path.getsize(file_path),
        }
    except Exception:
        return {}

def _read_sprite_meta(meta_path: str) -> dict:
    """Unity .meta 파일에서 주요 임포트 설정 추출."""
    if not os.path.isfile(meta_path):
        return {}
    try:
        with open(meta_path, 'r', encoding='utf-8') as f:
            content = f.read()
    except Exception:
        return {}

    result = {}
    for field, cast in [('textureType',int),('maxTextureSize',int),('filterMode',int),
                        ('spriteMode',int),('enableMipMap',int),('alphaIsTransparency',int),
                        ('spritePixelsToUnits',float)]:
        m = re.search(rf'\b{field}:\s*(\S+)', content)
        if m:
            try: result[field] = cast(m.group(1))
            except ValueError: pass

    # 9-slice 보더
    m = re.search(r'spriteBorder:\s*\{l:\s*([\d.]+),\s*t:\s*([\d.]+),\s*r:\s*([\d.]+),\s*b:\s*([\d.]+)\}', content)
    if m:
        result['spriteBorder'] = f'{m.group(1)} {m.group(2)} {m.group(3)} {m.group(4)}'

    return result

def parse_asset(file_path: str) -> dict:
    """
    Unity YAML .asset 파일을 파싱하여 편집 가능한 프로퍼티만 반환.

    반환 형식:
      { 'id': 'apple', 'title': 'Apple', 'eat_hp': 1, ... }

    건너뛰는 것:
      - m_* 로 시작하는 Unity 내부 필드
      - {fileID: ...} 형태의 에셋 참조
      - [] 빈 배열 아닌 배열 (복잡한 구조)
    """
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
    except Exception:
        return {}

    props = {}
    in_mono = False

    for line in content.splitlines():
        # MonoBehaviour: 블록 시작 감지
        if line.strip() == 'MonoBehaviour:':
            in_mono = True
            continue
        if not in_mono:
            continue

        # 정확히 2칸 들여쓰기된 "key: value" 패턴
        m = re.match(r'^  (\w+): (.+)$', line)
        if not m:
            continue

        key   = m.group(1)
        value = m.group(2).strip()

        # Unity 내부 필드 제외
        if key in READONLY_KEYS or key.startswith('m_'):
            continue
        # 에셋 참조 제외
        if is_reference(value):
            continue
        # 빈 배열은 메타로 포함
        if value == '[]':
            props[key] = '__empty_array__'
            continue

        # 숫자 변환 시도
        try:
            if '.' in value:
                props[key] = float(value)
            else:
                props[key] = int(value)
        except ValueError:
            props[key] = value

    return props


def scan_category(cat_key: str) -> list:
    """카테고리의 모든 .asset 파일을 스캔하여 리스트 반환."""
    if cat_key not in CATEGORIES:
        return []

    pattern, _ = CATEGORIES[cat_key]
    full_pattern = os.path.join(ASSETS_ROOT, pattern)
    files = sorted(glob.glob(full_pattern, recursive=True))

    results = []
    for fp in files:
        rel = os.path.relpath(fp, PROJECT_ROOT).replace('\\', '/')
        name = os.path.splitext(os.path.basename(fp))[0]
        props = parse_asset(fp)
        if props:
            results.append({'name': name, 'path': rel, 'props': props})

    return results


def parse_prefab_plant(file_path: str) -> dict:
    """Plant MonoBehaviour 컴포넌트 데이터를 .prefab 파일에서 추출."""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
    except Exception:
        return {}

    for section in re.split(r'--- !u!\d+ &\d+', content):
        if 'MonoBehaviour:' not in section or 'grow_time:' not in section:
            continue
        props = {}
        in_mono = False
        for line in section.splitlines():
            if line.strip() == 'MonoBehaviour:':
                in_mono = True
                continue
            if not in_mono:
                continue
            m = re.match(r'^  (\w+): (.+)$', line)
            if not m:
                continue
            key, value = m.group(1), m.group(2).strip()
            if key == 'grow_time':
                try: props['grow_time'] = float(value)
                except ValueError: pass
            elif key == 'grow_require_water':
                try: props['grow_require_water'] = bool(int(value))
                except ValueError: pass
            elif key == 'fruit_grow_time':
                try: props['fruit_grow_time'] = float(value)
                except ValueError: pass
            elif key == 'fruit':
                props['fruit'] = 'fileID: 0' not in value
            elif key == 'fruit_require_water':
                try: props['fruit_require_water'] = bool(int(value))
                except ValueError: pass
            elif key == 'death_on_harvest':
                try: props['death_on_harvest'] = bool(int(value))
                except ValueError: pass
            elif key == 'regrow_on_death':
                try: props['regrow_on_death'] = bool(int(value))
                except ValueError: pass
        if 'grow_time' in props:
            return props
    return {}


# ── 캐릭터 프리팹 헬퍼 ───────────────────────────────────────────────

def rel_project_path(abs_path: str) -> str:
    return os.path.relpath(abs_path, PROJECT_ROOT).replace('\\', '/')


def safe_asset_name(name: str) -> str:
    cleaned = re.sub(r'[^A-Za-z0-9_]+', '_', name.strip())
    cleaned = cleaned.strip('_')
    return cleaned or 'NewCharacter'


def read_character_requests() -> dict:
    if not os.path.isfile(CHARACTER_REQUESTS):
        return {'requests': []}
    try:
        with open(CHARACTER_REQUESTS, 'r', encoding='utf-8') as f:
            data = json.load(f)
        if isinstance(data, dict) and isinstance(data.get('requests'), list):
            return data
    except Exception:
        pass
    return {'requests': []}


def scan_player_prefabs() -> list:
    result = []
    seen = set()
    player_script_guid = '71ef48a91b33525499bcaff13b0a7258'
    for fp in sorted(glob.glob(os.path.join(PREFABS_ROOT, '*.prefab'))):
        try:
            with open(fp, 'r', encoding='utf-8') as f:
                content = f.read()
        except Exception:
            continue
        if player_script_guid not in content and not os.path.basename(fp).startswith('PlayerCharacter'):
            continue
        name = os.path.splitext(os.path.basename(fp))[0]
        seen.add(name)
        result.append({'name': name, 'folder': '', 'path': rel_project_path(fp), 'thumbnail': None, 'mapType': 'player'})
    for req in read_character_requests().get('requests', []):
        if req.get('role', 'player') != 'player':
            continue
        output = req.get('outputPrefab', '')
        abs_path = os.path.normpath(os.path.join(PROJECT_ROOT, output))
        if not output or not os.path.isfile(abs_path):
            continue
        name = os.path.splitext(os.path.basename(output))[0]
        if name in seen:
            continue
        seen.add(name)
        result.append({'name': name, 'folder': '', 'path': output, 'thumbnail': None, 'mapType': 'player'})
    return result


# ── API 엔드포인트 ────────────────────────────────────────────────────

@app.route('/api/status')
def api_status():
    """서버 활성 여부 확인용 ping."""
    return jsonify({'ok': True, 'version': '1.0',
                    'categories': list(CATEGORIES.keys())})


@app.route('/api/assets/<cat_key>')
def api_get_assets(cat_key):
    """카테고리 전체 에셋 목록 + 프로퍼티 반환."""
    assets = scan_category(cat_key)
    return jsonify(assets)


@app.route('/api/prefabs')
def api_get_prefabs():
    """프리팹 카테고리별 목록 반환 (썸네일 유무 포함)."""
    result = []
    for folder, label, map_type in PREFAB_CATEGORIES:
        folder_path = os.path.join(PREFABS_ROOT, folder)
        if not os.path.isdir(folder_path):
            continue
        prefabs = []
        for fp in sorted(glob.glob(os.path.join(folder_path, '*.prefab'))):
            name = os.path.splitext(os.path.basename(fp))[0]
            thumb_path = os.path.join(THUMBNAILS_ROOT, folder, name + '.png')
            has_thumb = os.path.isfile(thumb_path)
            prefabs.append({
                'name':      name,
                'folder':    folder,
                'thumbnail': f'/thumbnail/{folder}/{name}.png' if has_thumb else None,
            })
        if prefabs:
            result.append({'key': folder, 'label': label, 'mapType': map_type, 'prefabs': prefabs})
    return jsonify(result)


@app.route('/api/player-prefabs')
def api_player_prefabs():
    """맵에디터 캐릭터 패널용 PlayerCharacter 프리팹 목록."""
    return jsonify(scan_player_prefabs())


@app.route('/api/character-prefab-upload', methods=['POST'])
def api_character_prefab_upload():
    """FBX/PNG 파일 업로드 후 Unity Editor 프리팹 빌드 요청 생성."""
    from werkzeug.utils import secure_filename
    char_name = safe_asset_name(request.form.get('name', ''))
    role = request.form.get('role', 'player').strip().lower()
    if role not in {'player', 'npc'}:
        role = 'player'
    default_template = 'NPCGirl' if role == 'npc' else 'PlayerCharacter'
    template = request.form.get('templatePrefab', default_template).strip() or default_template
    scale = request.form.get('scale', '0.5').strip() or '0.5'

    if 'fbx' not in request.files:
        return jsonify({'ok': False, 'error': 'fbx 파일이 필요합니다'}), 400

    allowed = {
        'fbx': {'.fbx'},
        'bodyTexture': {'.png'},
        'dressTexture': {'.png'},
        'hairTexture': {'.png'},
    }
    target_dir = os.path.join(CHARACTER_IMPORT_ROOT, char_name)
    source_dir = os.path.join(target_dir, 'Source')
    os.makedirs(source_dir, exist_ok=True)

    saved = {}
    for field, exts in allowed.items():
        file = request.files.get(field)
        if not file or not file.filename:
            continue
        original = secure_filename(file.filename)
        ext = os.path.splitext(original)[1].lower()
        if ext not in exts:
            return jsonify({'ok': False, 'error': f'{field} 확장자는 {", ".join(sorted(exts))}만 허용됩니다'}), 400
        filename = f'{char_name}_{field}{ext}'
        abs_path = os.path.join(source_dir, filename)
        file.save(abs_path)
        saved[field] = rel_project_path(abs_path)

    request_data = {
        'name': char_name,
        'role': role,
        'templatePrefab': template,
        'fbx': saved.get('fbx'),
        'bodyTexture': saved.get('bodyTexture', ''),
        'dressTexture': saved.get('dressTexture', ''),
        'hairTexture': saved.get('hairTexture', ''),
        'scale': float(scale) if re.match(r'^-?\d+(\.\d+)?$', scale) else 0.5,
        'outputPrefab': (
            f'Assets/FarmingEngine_study/Prefabs/Town/{char_name}.prefab'
            if role == 'npc'
            else f'Assets/FarmingEngine_study/Prefabs/{char_name}.prefab'
        ),
        'status': 'pending',
    }

    data = read_character_requests()
    data['requests'] = [
        r for r in data['requests']
        if not (r.get('name') == char_name and r.get('role', 'player') == role)
    ]
    data['requests'].append(request_data)
    os.makedirs(os.path.dirname(CHARACTER_REQUESTS), exist_ok=True)
    with open(CHARACTER_REQUESTS, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

    return jsonify({'ok': True, 'request': request_data})


@app.route('/thumbnail/<folder>/<filename>')
def api_thumbnail(folder, filename):
    """StreamingAssets/prefab_thumbnails/ 의 썸네일 PNG 서빙."""
    # 경로 탈출 방지
    if not re.match(r'^[A-Za-z0-9_]+$', folder) or not re.match(r'^[A-Za-z0-9_]+\.png$', filename):
        return '', 403
    abs_path = os.path.normpath(os.path.join(THUMBNAILS_ROOT, folder, filename))
    if not abs_path.startswith(os.path.normpath(THUMBNAILS_ROOT)):
        return '', 403
    if not os.path.isfile(abs_path):
        return '', 404
    return send_file(abs_path, mimetype='image/png')


@app.route('/api/plant-stages')
def api_plant_stages():
    """식물 스테이지 프리팹의 Plant 컴포넌트 데이터 반환."""
    plants_dir = os.path.join(PREFABS_ROOT, 'Plants')
    result = {}
    for fp in sorted(glob.glob(os.path.join(plants_dir, '*.prefab'))):
        name = os.path.splitext(os.path.basename(fp))[0]
        data = parse_prefab_plant(fp)
        if data:
            result[name] = data
    return jsonify(result)


@app.route('/api/plant-stage/<prefab_name>', methods=['PUT'])
def api_update_plant_stage(prefab_name):
    """Plant 스테이지 프리팹의 단일 필드 업데이트."""
    if not re.match(r'^[A-Za-z0-9_]+$', prefab_name):
        return jsonify({'ok': False, 'error': '잘못된 프리팹 이름'}), 400

    data = request.get_json(silent=True)
    if not data:
        return jsonify({'ok': False, 'error': 'JSON 바디 필요'}), 400

    field = data.get('field', '')
    value = data.get('value')

    allowed = {'grow_time', 'grow_require_water', 'fruit_grow_time', 'fruit_require_water', 'death_on_harvest', 'regrow_on_death'}
    if field not in allowed:
        return jsonify({'ok': False, 'error': f'수정 불가 필드: {field}'}), 403

    prefab_path = os.path.join(PREFABS_ROOT, 'Plants', prefab_name + '.prefab')
    if not os.path.isfile(prefab_path):
        return jsonify({'ok': False, 'error': '프리팹 파일 없음'}), 404

    try:
        with open(prefab_path, 'r', encoding='utf-8') as f:
            content = f.read()

        str_val = ('1' if value else '0') if field == 'grow_require_water' else str(value)
        new_content, n = re.subn(
            rf'^  {re.escape(field)}: .+$',
            f'  {field}: {str_val}',
            content, flags=re.MULTILINE
        )
        if n == 0:
            return jsonify({'ok': False, 'error': f'필드 "{field}" 없음'}), 404

        with open(prefab_path, 'w', encoding='utf-8') as f:
            f.write(new_content)

        return jsonify({'ok': True, 'field': field, 'value': value})
    except Exception as e:
        return jsonify({'ok': False, 'error': str(e)}), 500


@app.route('/api/asset', methods=['PUT'])
def api_update_asset():
    """
    단일 프로퍼티 업데이트.

    요청 바디 (JSON):
      { "path": "Assets/.../Apple.asset", "key": "eat_hp", "value": "5" }
    """
    data = request.get_json(silent=True)
    if not data:
        return jsonify({'ok': False, 'error': 'JSON 바디가 필요합니다'}), 400

    path  = data.get('path', '')
    key   = data.get('key', '')
    value = str(data.get('value', ''))

    # ── 보안 검증 ──────────────────────────────────────────
    if not path or not key:
        return jsonify({'ok': False, 'error': 'path, key 필드가 필요합니다'}), 400

    # 경로 탈출 공격 방지
    abs_path = os.path.normpath(os.path.join(PROJECT_ROOT, path))
    if not abs_path.startswith(os.path.normpath(PROJECT_ROOT)):
        return jsonify({'ok': False, 'error': '허용되지 않은 경로입니다'}), 403

    # 읽기 전용 필드 수정 차단
    if key in READONLY_KEYS or key.startswith('m_'):
        return jsonify({'ok': False, 'error': f'"{key}"는 수정 불가 필드입니다'}), 403

    # 줄바꿈, 탭 제거 (YAML 파일 구조 보호)
    value = value.replace('\n', ' ').replace('\r', '').replace('\t', ' ').strip()

    # {, [ 로 시작하는 값은 참조/배열 — 차단
    if value.startswith('{') or value.startswith('['):
        return jsonify({'ok': False, 'error': '참조/배열 타입은 이 도구로 수정할 수 없습니다'}), 403
    # ────────────────────────────────────────────────────────

    if not os.path.isfile(abs_path):
        return jsonify({'ok': False, 'error': '파일을 찾을 수 없습니다'}), 404

    if not abs_path.endswith('.asset'):
        return jsonify({'ok': False, 'error': '.asset 파일만 수정 가능합니다'}), 403

    try:
        with open(abs_path, 'r', encoding='utf-8') as f:
            content = f.read()

        # 해당 키의 라인을 찾아 값만 교체
        pattern     = rf'^  {re.escape(key)}: .+$'
        replacement = f'  {key}: {value}'
        new_content, count = re.subn(pattern, replacement, content, flags=re.MULTILINE)

        if count == 0:
            return jsonify({'ok': False, 'error': f'필드 "{key}"를 파일에서 찾을 수 없습니다'}), 404

        with open(abs_path, 'w', encoding='utf-8') as f:
            f.write(new_content)

        return jsonify({
            'ok':      True,
            'message': f'✅ {os.path.basename(abs_path)} → {key} = {value}',
            'key':     key,
            'value':   value,
        })

    except Exception as e:
        return jsonify({'ok': False, 'error': f'파일 쓰기 오류: {str(e)}'}), 500


# ── Notes API ────────────────────────────────────────────────────────

VALID_TABS = {'overview','scene-mgr','gameplay','system-dev','assets','dialogue','map-editor','uidesign','optimization','farm-bakery','shop-manage'}

def _notes_path(tab: str) -> str:
    return os.path.join(NOTES_ROOT, f'{tab}.md')

def _parse_notes(content: str) -> dict:
    result = {'focus': '', 'priority': 'normal', 'todos': [], 'issues': []}
    section = None
    for line in content.splitlines():
        s = line.strip()
        if s.startswith('## '):
            h = s[3:].lower()
            if '포커스' in h or 'focus' in h:       section = 'focus'
            elif '우선순위' in h or 'priority' in h: section = 'priority'
            elif '할일' in h or 'todo' in h:         section = 'todos'
            elif '이슈' in h or 'issue' in h:        section = 'issues'
            else:                                     section = None
        elif section == 'focus' and s and not s.startswith('#'):
            result['focus'] = (result['focus'] + ' ' + s).strip()
        elif section == 'priority' and s:
            result['priority'] = s
        elif section == 'todos' and s.startswith('- '):
            done = s[2:5] in ('[x]', '[X]')
            text = s[6:] if s[2:5] in ('[ ]', '[x]', '[X]') else s[2:]
            # 누적 [x]/[ ] 잔여 제거 (파싱-저장 루프 버그 방어)
            text = re.sub(r'^(\[[ xX]\]\s*)+', '', text).strip()
            result['todos'].append({'done': done, 'text': text})
        elif section == 'issues' and s.startswith('- '):
            result['issues'].append(s[2:].strip())
    return result

def _write_notes(tab: str, parsed: dict):
    lines = [f'# {tab}\n']
    lines.append('\n## 현재 포커스\n')
    lines.append((parsed['focus'] or '(없음)') + '\n')
    lines.append('\n## 우선순위\n')
    lines.append(parsed['priority'] + '\n')
    lines.append('\n## 할일\n')
    for t in parsed['todos']:
        mark = '[x]' if t['done'] else '[ ]'
        lines.append(f"- {mark} {t['text']}\n")
    lines.append('\n## 이슈\n')
    for iss in parsed['issues']:
        lines.append(f'- {iss}\n')
    with open(_notes_path(tab), 'w', encoding='utf-8') as f:
        f.writelines(lines)

@app.route('/api/notes')
def api_notes_all():
    summary = []
    for tab in VALID_TABS:
        p = _notes_path(tab)
        if not os.path.isfile(p):
            summary.append({'tab': tab, 'focus': '', 'priority': 'normal', 'done': 0, 'total': 0, 'issues': 0})
            continue
        with open(p, 'r', encoding='utf-8') as f:
            parsed = _parse_notes(f.read())
        done  = sum(1 for t in parsed['todos'] if t['done'])
        total = len(parsed['todos'])
        summary.append({'tab': tab, 'focus': parsed['focus'], 'priority': parsed['priority'],
                         'done': done, 'total': total, 'issues': len(parsed['issues'])})
    return jsonify(summary)

@app.route('/api/notes/<tab>')
def api_notes_get(tab):
    if tab not in VALID_TABS:
        return jsonify({'ok': False, 'error': '알 수 없는 탭'}), 404
    p = _notes_path(tab)
    if not os.path.isfile(p):
        return jsonify({'ok': True, 'tab': tab, 'focus': '', 'priority': 'normal', 'todos': [], 'issues': []})
    with open(p, 'r', encoding='utf-8') as f:
        parsed = _parse_notes(f.read())
    return jsonify({'ok': True, 'tab': tab, **parsed})

@app.route('/api/notes/<tab>/toggle', methods=['POST'])
def api_notes_toggle(tab):
    if tab not in VALID_TABS:
        return jsonify({'ok': False, 'error': '알 수 없는 탭'}), 404
    data = request.get_json(silent=True) or {}
    idx  = data.get('index')
    p    = _notes_path(tab)
    if not os.path.isfile(p):
        parsed = {'focus': '(없음)', 'priority': 'normal', 'todos': [], 'issues': []}
    else:
        with open(p, 'r', encoding='utf-8') as f:
            parsed = _parse_notes(f.read())
    if idx is None or not (0 <= idx < len(parsed['todos'])):
        return jsonify({'ok': False, 'error': '인덱스 범위 초과'}), 400
    parsed['todos'][idx]['done'] = not parsed['todos'][idx]['done']
    _write_notes(tab, parsed)
    return jsonify({'ok': True, 'todos': parsed['todos']})

@app.route('/api/notes/<tab>/add', methods=['POST'])
def api_notes_add(tab):
    if tab not in VALID_TABS:
        return jsonify({'ok': False, 'error': '알 수 없는 탭'}), 404
    data = request.get_json(silent=True) or {}
    text = (data.get('text') or '').strip()
    if not text:
        return jsonify({'ok': False, 'error': 'text 필드가 필요합니다'}), 400
    p = _notes_path(tab)
    if not os.path.isfile(p):
        parsed = {'focus': '(없음)', 'priority': 'normal', 'todos': [], 'issues': []}
    else:
        with open(p, 'r', encoding='utf-8') as f:
            parsed = _parse_notes(f.read())
    parsed['todos'].append({'done': False, 'text': text})
    _write_notes(tab, parsed)
    return jsonify({'ok': True, 'todos': parsed['todos']})

@app.route('/api/notes/<tab>/delete', methods=['POST'])
def api_notes_delete(tab):
    if tab not in VALID_TABS:
        return jsonify({'ok': False, 'error': '알 수 없는 탭'}), 404
    data = request.get_json(silent=True) or {}
    idx  = data.get('idx')
    p = _notes_path(tab)
    if not os.path.isfile(p):
        parsed = {'focus': '(없음)', 'priority': 'normal', 'todos': [], 'issues': []}
    else:
        with open(p, 'r', encoding='utf-8') as f:
            parsed = _parse_notes(f.read())
    if idx is None or idx < 0 or idx >= len(parsed['todos']):
        return jsonify({'ok': False, 'error': '잘못된 인덱스'}), 400
    parsed['todos'].pop(idx)
    _write_notes(tab, parsed)
    return jsonify({'ok': True, 'todos': parsed['todos']})

@app.route('/api/notes/<tab>/focus', methods=['POST'])
def api_notes_focus(tab):
    if tab not in VALID_TABS:
        return jsonify({'ok': False, 'error': '알 수 없는 탭'}), 404
    data = request.get_json(silent=True) or {}
    text = (data.get('text') or '').strip()
    p    = _notes_path(tab)
    if not os.path.isfile(p):
        parsed = {'focus': '(없음)', 'priority': 'normal', 'todos': [], 'issues': []}
    else:
        with open(p, 'r', encoding='utf-8') as f:
            parsed = _parse_notes(f.read())
    parsed['focus'] = text
    _write_notes(tab, parsed)
    return jsonify({'ok': True, 'focus': parsed['focus']})


@app.route('/api/devplan', methods=['GET'])
def api_devplan_get():
    if not os.path.exists(DEVPLAN_PATH):
        return jsonify({"nodes": []})
    with open(DEVPLAN_PATH, 'r', encoding='utf-8') as f:
        return jsonify(json.load(f))

@app.route('/api/devplan', methods=['POST'])
def api_devplan_save():
    data = request.get_json(silent=True)
    if not data:
        return jsonify({'ok': False, 'error': '데이터 없음'}), 400
    with open(DEVPLAN_PATH, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    return jsonify({'ok': True})


# ── 씬 버전 관리 ──────────────────────────────────────────────────────
@app.route('/api/scene-versions', methods=['GET'])
def api_scene_versions_list():
    os.makedirs(SCENE_VER_ROOT, exist_ok=True)
    files = sorted(f for f in os.listdir(SCENE_VER_ROOT) if f.endswith('.json'))
    versions = []
    for fname in files:
        path = os.path.join(SCENE_VER_ROOT, fname)
        with open(path, 'r', encoding='utf-8') as f:
            d = json.load(f)
        versions.append({'name': d.get('version', fname[:-5]), 'label': d.get('label', ''), 'date': d.get('date', '')})
    return jsonify({'versions': versions})

@app.route('/api/scene-versions/<name>', methods=['GET'])
def api_scene_version_get(name):
    path = os.path.join(SCENE_VER_ROOT, name + '.json')
    if not os.path.isfile(path):
        return jsonify({'error': '버전 없음'}), 404
    with open(path, 'r', encoding='utf-8') as f:
        return jsonify(json.load(f))

@app.route('/api/scene-versions', methods=['POST'])
def api_scene_version_save():
    data = request.get_json(silent=True) or {}
    name = data.get('version')
    if not name:
        return jsonify({'ok': False, 'error': 'version 필드 필요'}), 400
    os.makedirs(SCENE_VER_ROOT, exist_ok=True)
    path = os.path.join(SCENE_VER_ROOT, name + '.json')
    with open(path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    return jsonify({'ok': True})

# ── 스프라이트 교체 API ───────────────────────────────────────────────

@app.route('/api/sprite/spec')
def api_sprite_spec():
    """스프라이트 파일의 해상도·색상·메타 설정 반환."""
    folder   = request.args.get('folder', '')
    filename = request.args.get('filename', '')

    abs_path, err = _validate_sprite(folder, filename)
    if err:
        return jsonify({'ok': False, 'error': err}), 400
    if not os.path.isfile(abs_path):
        return jsonify({'ok': False, 'error': '파일 없음'}), 404

    png  = _read_png_info(abs_path)
    meta = _read_sprite_meta(abs_path + '.meta')
    return jsonify({'ok': True, 'png': png, 'meta': meta})


@app.route('/api/sprite/replace', methods=['POST'])
def api_sprite_replace():
    """스프라이트 파일 교체 + 히스토리 백업."""
    folder   = request.form.get('folder', '')
    filename = request.form.get('filename', '')
    label    = (request.form.get('label', '') or '').strip()[:100]
    file_obj = request.files.get('file')

    if not file_obj:
        return jsonify({'ok': False, 'error': '파일이 없습니다'}), 400

    abs_path, err = _validate_sprite(folder, filename)
    if err:
        return jsonify({'ok': False, 'error': err}), 400

    if not os.path.isfile(abs_path):
        return jsonify({'ok': False, 'error': '원본 파일을 찾을 수 없습니다'}), 404

    # 히스토리 폴더 준비
    ts       = datetime.now().strftime('%Y%m%d_%H%M%S')
    hist_dir = os.path.join(SPRITE_HISTORY_ROOT, _folder_key(folder))
    os.makedirs(hist_dir, exist_ok=True)

    # 현재 파일 백업
    hist_filename = f'{ts}_{filename}'
    hist_path     = os.path.join(hist_dir, hist_filename)
    shutil.copy2(abs_path, hist_path)

    # 메타데이터 저장
    with open(hist_path + '.json', 'w', encoding='utf-8') as f:
        json.dump({'ts': ts, 'label': label, 'original': filename}, f, ensure_ascii=False)

    # 새 파일 쓰기
    try:
        file_obj.save(abs_path)
    except Exception as e:
        shutil.copy2(hist_path, abs_path)  # 실패 시 복원
        return jsonify({'ok': False, 'error': f'파일 저장 실패: {str(e)}'}), 500

    return jsonify({
        'ok':     True,
        'ts':     ts,
        'label':  label,
        'backup': hist_filename,
        'message': f'{filename} 교체 완료 — Unity 에디터 포커스 시 자동 reimport',
    })


@app.route('/api/sprite/history')
def api_sprite_history():
    """특정 스프라이트의 교체 히스토리 목록 반환."""
    folder   = request.args.get('folder', '')
    filename = request.args.get('filename', '')

    _, err = _validate_sprite(folder, filename)
    if err:
        return jsonify({'ok': False, 'error': err}), 400

    hist_dir = os.path.join(SPRITE_HISTORY_ROOT, _folder_key(folder))
    if not os.path.isdir(hist_dir):
        return jsonify({'ok': True, 'history': []})

    suffix   = '_' + filename
    entries  = []
    for fname in sorted(os.listdir(hist_dir), reverse=True):
        if not fname.endswith(suffix) or fname.endswith('.json'):
            continue
        ts_part = fname[:-len(suffix)]
        if not re.match(r'^\d{8}_\d{6}$', ts_part):
            continue

        label = ''
        meta_path = os.path.join(hist_dir, fname + '.json')
        if os.path.isfile(meta_path):
            try:
                with open(meta_path, 'r', encoding='utf-8') as f:
                    label = json.load(f).get('label', '')
            except Exception:
                pass

        entries.append({
            'ts':      ts_part,
            'label':   label,
            'file':    fname,
            'img_url': f'/api/sprite/history-img/{_folder_key(folder)}/{fname}',
        })

    return jsonify({'ok': True, 'history': entries})


@app.route('/api/sprite/rollback', methods=['POST'])
def api_sprite_rollback():
    """히스토리 버전으로 롤백. 현재 파일도 자동 백업."""
    data     = request.get_json(silent=True) or {}
    folder   = data.get('folder', '')
    filename = data.get('filename', '')
    ts       = data.get('ts', '')

    abs_path, err = _validate_sprite(folder, filename)
    if err:
        return jsonify({'ok': False, 'error': err}), 400

    if not re.match(r'^\d{8}_\d{6}$', ts):
        return jsonify({'ok': False, 'error': '잘못된 타임스탬프 형식'}), 400

    hist_dir      = os.path.join(SPRITE_HISTORY_ROOT, _folder_key(folder))
    hist_filename = f'{ts}_{filename}'
    hist_path     = os.path.join(hist_dir, hist_filename)

    if not os.path.isfile(hist_path):
        return jsonify({'ok': False, 'error': '히스토리 파일 없음'}), 404

    # 현재 파일도 백업
    if os.path.isfile(abs_path):
        now_ts      = datetime.now().strftime('%Y%m%d_%H%M%S')
        backup_path = os.path.join(hist_dir, f'{now_ts}_{filename}')
        shutil.copy2(abs_path, backup_path)
        with open(backup_path + '.json', 'w', encoding='utf-8') as f:
            json.dump({'ts': now_ts, 'label': f'[롤백 전 자동백업] → {ts}', 'original': filename},
                      f, ensure_ascii=False)

    shutil.copy2(hist_path, abs_path)
    return jsonify({'ok': True, 'message': f'{filename} → {ts} 버전으로 롤백 완료'})


@app.route('/api/sprite/history-img/<folder_key>/<filename>')
def api_sprite_history_img(folder_key, filename):
    """히스토리 PNG 서빙."""
    if not re.match(r'^[A-Za-z0-9_]+$', folder_key):
        return '', 403
    if not re.match(r'^[\w\- .]+\.png$', filename, re.IGNORECASE):
        return '', 403
    abs_path = os.path.normpath(os.path.join(SPRITE_HISTORY_ROOT, folder_key, filename))
    if not abs_path.startswith(os.path.normpath(SPRITE_HISTORY_ROOT)):
        return '', 403
    if not os.path.isfile(abs_path):
        return '', 404
    return send_file(abs_path, mimetype='image/png')


@app.route('/api/npc-shop', methods=['GET'])
def api_npc_shop_get():
    if not os.path.isfile(NPC_SHOP_PATH):
        return jsonify({})
    with open(NPC_SHOP_PATH, 'r', encoding='utf-8') as f:
        data = json.load(f)
    return jsonify(data.get('npcs', {}))


@app.route('/api/npc-shop', methods=['POST'])
def api_npc_shop_post():
    npcs = request.json.get('npcs', {})
    existing = {}
    if os.path.isfile(NPC_SHOP_PATH):
        with open(NPC_SHOP_PATH, 'r', encoding='utf-8') as f:
            existing = json.load(f)
    existing['npcs'] = npcs
    with open(NPC_SHOP_PATH, 'w', encoding='utf-8') as f:
        json.dump(existing, f, ensure_ascii=False, indent=2)
    return jsonify({'ok': True})


@app.route('/api/sprite/img/<folder_key>/<filename>')
def api_sprite_img(folder_key, filename):
    """현재 스프라이트 서빙 (교체 후 캐시 버스팅용)."""
    folder = None
    for k in SPRITE_FOLDERS:
        if _folder_key(k) == folder_key:
            folder = k
            break
    if not folder:
        return '', 404
    abs_path, err = _validate_sprite(folder, filename)
    if err:
        return '', 403
    if not os.path.isfile(abs_path):
        return '', 404
    return send_file(abs_path, mimetype='image/png')


# ── 실행 ─────────────────────────────────────────────────────────────
if __name__ == '__main__':
    print()
    print("=" * 60)
    print("  🌾  FarmingEngine Asset Editor Server  v1.0")
    print("=" * 60)
    print(f"  프로젝트 루트 : {PROJECT_ROOT}")
    print(f"  에셋 경로     : {ASSETS_ROOT}")
    print()

    # 에셋 경로 존재 확인
    if not os.path.isdir(ASSETS_ROOT):
        print(f"  ⚠️  경고: 에셋 경로를 찾을 수 없습니다!")
        print(f"     예상 경로: {ASSETS_ROOT}")
    else:
        # 카테고리별 파일 수 미리 확인
        print("  카테고리별 에셋 수:")
        for k, (pat, label) in CATEGORIES.items():
            files = glob.glob(os.path.join(ASSETS_ROOT, pat), recursive=True)
            print(f"    {label:22s}  {len(files):3d}개")

    print()
    print("  서버 주소: http://localhost:5000")
    print("  중지: Ctrl+C")
    print("=" * 60)
    print()

    app.run(host='localhost', port=5000, debug=False)
