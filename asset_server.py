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

app = Flask(__name__)
CORS(app)  # 브라우저에서 localhost:5000 접근 허용

# ── 경로 설정 ─────────────────────────────────────────────────────────
PROJECT_ROOT    = os.path.dirname(os.path.abspath(__file__))
ASSETS_ROOT     = os.path.join(PROJECT_ROOT, 'Assets', 'FarmingEngine_study', 'Resources')
PREFABS_ROOT    = os.path.join(PROJECT_ROOT, 'Assets', 'FarmingEngine_study', 'Prefabs')
THUMBNAILS_ROOT = os.path.join(PROJECT_ROOT, 'Assets', 'StreamingAssets', 'prefab_thumbnails')

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
        print(f"     이 스크립트는 프로젝트 루트에서 실행해야 합니다.")
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
