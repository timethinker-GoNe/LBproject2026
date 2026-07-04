import sys, io, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
with open(r'C:\workspace\Farming_01_16_Final\project_structure.html', encoding='utf-8') as f:
    src = f.read()
lines = src.splitlines()
print('총 라인: ' + str(len(lines)))

fns = ['renderDQMain','renderTextGroupIdle','renderTextGroupStory','renderTextKey',
       'renderChatSidebar','selectChatNpc','renderChatMain','addChatEntry',
       'setChatLineType','addChatChoice','setEvtLineType','addEvtChoice','chatSave']
for fn in fns:
    found = [i+1 for i,l in enumerate(lines) if ('function '+fn+'(') in l]
    status = 'OK' if found else 'MISSING'
    loc = str(found[0]) if found else '?'
    print(status + '  ' + fn + '  L' + loc)

# dq-sub-chat HTML
found_chat = [i+1 for i,l in enumerate(lines) if 'dq-sub-chat' in l]
print('OK  dq-sub-chat at: ' + str(found_chat))

# JS 중괄호 밸런스
pat = re.compile(r'<script[^>]*>(.*?)</script>', re.DOTALL)
for si, m in enumerate(pat.finditer(src)):
    sc = m.group(1)
    opens = sc.count('{')
    closes = sc.count('}')
    diff = opens - closes
    print('Script ' + str(si+1) + ': { ' + str(opens) + '  } ' + str(closes) + '  diff=' + str(diff))
