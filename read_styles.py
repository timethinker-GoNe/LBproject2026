import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
with open(r'C:\workspace\Farming_01_16_Final\project_structure.html', encoding='utf-8') as f:
    lines = f.readlines()

print('=== QE / QS CSS 클래스 ===')
for i,l in enumerate(lines):
    if any(k in l for k in ['.qe-', '.qs-box', 'qs-box-title', 'qs-type-table', 'qs-field']):
        print(f'L{i+1}: {l.rstrip()[:120]}')

print()
print('=== DQ CSS 클래스 ===')
for i,l in enumerate(lines):
    if '.dq-' in l and '{' in l:
        print(f'L{i+1}: {l.rstrip()[:120]}')

print()
print('=== QE HTML 시작부분 (3452~3510) ===')
# dq-sub-questedit 찾기
for i,l in enumerate(lines):
    if 'dq-sub-questedit' in l and 'style="display' in l:
        start = i
        for j in range(start, min(start+60, len(lines))):
            print(f'L{j+1}: {lines[j].rstrip()[:120]}')
        break
