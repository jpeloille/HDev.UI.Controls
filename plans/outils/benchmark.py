import re, pathlib, json
# Lignes du benchmark HDEV-001/26, numérotées comme le HTML (le .md plus un)
MD = (pathlib.Path(__file__).resolve().parents[2] / 'Benchmark-DevExpress.md').read_text(encoding='utf-8')
ETATS = {'✅': 'C', '🟡': 'P', '❌': 'A', '⚪': 'D'}
def lignes():
    rows = []; sec = None; kind = None
    for line in MD.splitlines():
        m = re.match(r'^### (3|4)\.(\d+) ', line)
        if m:
            num = int(m.group(2))
            sec = f'{4 if m.group(1) == "3" else 5}.{num}'   # numérotation du HTML = md + 1
            kind = 'cat' if m.group(1) == '3' else 'prof'
            continue
        if line.startswith('## '):
            sec = None
            continue
        if sec and line.startswith('| ') and not line.startswith('| Fonction') and not line.startswith('| Capacité') and not line.startswith('|---'):
            cells = [c.strip() for c in line.strip('|').split('|')]
            if kind == 'cat':
                label, etat = cells[0], cells[5]
            else:
                label, etat = cells[0], cells[1]
            e = next((v for k, v in ETATS.items() if etat.startswith(k)), None)
            if e: rows.append((sec, label, e))
    return rows
