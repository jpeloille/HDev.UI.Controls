import sys; sys.dont_write_bytecode = True   # pas de __pycache__ dans le dépôt
import re, pathlib, sys
from benchmark import lignes
from affectation import A
from lots import LOTS
from projection import etat_a, score, verifier, JALONS
ROWS = lignes(); verifier()
EMO = {'C': '✅', 'P': '🟡', 'A': '❌', 'D': '⚪', 'Q': '⚪ proposé'}
ORDRE = ['E', 'F', 'G', 'I', 'V', 'M', 'W', 'C', 'N', 'A', 'T', 'O', 'R']
FICHIER = {'E': '../Plan-Editeurs.md', 'F': 'F-fondations.md', 'G': 'G-grille.md', 'I': 'I-impression-export.md',
           'V': 'V-visualisation.md', 'M': 'M-mise-en-page.md', 'W': 'W-fenetres.md', 'C': 'C-commandes.md',
           'N': 'N-navigation.md', 'A': 'A-ancrage.md', 'T': 'T-temps.md', 'R': 'R-redaction.md'}
TITRES = {'4.1': 'Grilles et données', '4.2': 'Éditeurs', '4.3': 'Boutons et libellés', '4.4': 'Rubans, barres et menus',
          '4.5': 'Navigation', '4.6': 'Mise en page', '4.7': 'Docking et documents', '4.8': 'Fenêtres, dialogues, notifications',
          '4.9': 'Planification', '4.10': 'Visualisation de données', '4.11': 'Documents', '4.12': 'Rapports, impression, export',
          '4.13': 'Composants transverses', '5.1': 'Grille', '5.2': 'Ruban, barres et menus', '5.3': 'Docking',
          '5.4': 'Agenda', '5.5': 'Gantt', '5.6': 'Texte riche', '5.7': 'Socle des éditeurs'}
def pct(x): return f'{int(x * 100 + 0.5)} %'   # arrondi au demi supérieur, comme le benchmark
def cle(sec): a, b = sec.split('.'); return (int(a), int(b))
SECS = sorted({r[0] for r in ROWS}, key=cle)

def lignes_visees(lot_id):
    from collections import Counter
    c = Counter(r[0] for r in ROWS if A[f'{r[0]}|{r[1]}'][0] == lot_id and A[f'{r[0]}|{r[1]}'][2] is not None)
    return ' ; '.join(f'{s} : {n}' for s, n in sorted(c.items(), key=lambda kv: cle(kv[0]))) or '—'

def table_lots(ch):
    out = ['| Lot | Objet | Taille | Dépend de | Jalon | Lignes visées | Statut |', '|---|---|---|---|---|---|---|']
    for id, (c, objet, taille, dep, jalon, statut) in LOTS.items():
        if c == ch:
            out.append(f'| **{id}** | {objet} | {taille} | {dep} | {jalon} | {lignes_visees(id)} | {statut} |')
    return '\n'.join(out)

def table_proj(sec):
    tete = 'Fonction' if sec.startswith('4.') else 'Capacité'
    out = [f'| {tete} | Aujourd\'hui | Lot | Jalon | Cible |', '|---|---|---|---|---|']
    for s, l, now in ROWS:
        if s != sec: continue
        dest, cible, j = A[f'{s}|{l}']
        out.append(f'| {l} | {EMO[now]} | {dest} | {j or "—"} | {EMO[cible] if j or cible != now else "inchangé"} |')
    return '\n'.join(out)

def table_scores(secs):
    out = ['| Tableau | Lignes | Aujourd\'hui | ' + ' | '.join(JALONS) + ' |', '|---|---:|---:|' + '---:|' * len(JALONS)]
    for sec in secs:
        rs = [r for r in ROWS if r[0] == sec]
        vals = [pct(score([r[2] for r in rs])[3])] + [pct(score([etat_a(r, j) for r in rs])[3]) for j in JALONS]
        out.append(f'| {sec} {TITRES[sec]} | {len(rs)} | ' + ' | '.join(vals) + ' |')
    return '\n'.join(out)

def table_scores_detail(secs, total_label):
    out = ['| Tableau | Lignes | Aujourd\'hui | ' + ' | '.join(JALONS) + ' |', '|---|---:|---:|' + '---:|' * len(JALONS)]
    def cellule(etats):
        c, p, a, sc = score(etats); return f'{pct(sc)} ({c}/{p})'
    allrows = []
    for sec in secs:
        rs = [r for r in ROWS if r[0] == sec]; allrows += rs
        out.append(f'| {sec} {TITRES[sec]} | {len(rs)} | ' + ' | '.join([cellule([r[2] for r in rs])] + [cellule([etat_a(r, j) for r in rs]) for j in JALONS]) + ' |')
    out.append(f'| **{total_label}** | **{len(allrows)}** | ' + ' | '.join(['**' + cellule([r[2] for r in allrows]) + '**'] + ['**' + cellule([etat_a(r, j) for r in allrows]) + '**' for j in JALONS]) + ' |')
    return '\n'.join(out)

def table_autres(ch, propres):
    ids = {id for id, v in LOTS.items() if v[0] == ch}
    rs = [r for r in ROWS if r[0] not in propres and A[f'{r[0]}|{r[1]}'][0] in ids]
    if not rs: return '_Aucune._'
    out = ['| Tableau | Ligne | Aujourd\'hui | Lot | Jalon | Cible |', '|---|---|---|---|---|---|']
    for s, l, now in rs:
        dest, cible, j = A[f'{s}|{l}']
        out.append(f'| {s} | {l} | {EMO[now]} | {dest} | {j or "—"} | {EMO[cible]} |')
    return '\n'.join(out)

def table_jalons():
    out = []
    for j in JALONS:
        out.append(f'**{j}**')
        for ch in ORDRE:
            ids = [id for id, v in LOTS.items() if v[0] == ch and v[4] == j]
            if ids:
                out.append(f'- {ch} : ' + ' ; '.join(f'{id} ({LOTS[id][1]})' for id in ids))
        out.append('')
    return '\n'.join(out).rstrip()

def liste_decision(code, cible=None, retrait=2):
    rs = [r for r in ROWS if A[f'{r[0]}|{r[1]}'][0] == code and (cible is None or A[f'{r[0]}|{r[1]}'][1] == cible)]
    par = {}
    for s, l, _ in rs: par.setdefault(s, []).append(l)
    return '\n'.join(' ' * retrait + f'- {s} : ' + ' ; '.join(ls) for s, ls in sorted(par.items(), key=lambda kv: cle(kv[0])))

def remplir(texte):
    def sub(m):
        cmd, arg = m.group(1), (m.group(2) or '').strip()
        if cmd == 'LOTS': return table_lots(arg)
        if cmd == 'PROJ': return table_proj(arg)
        if cmd == 'SCORES': return table_scores([a.strip() for a in arg.split(',')])
        if cmd == 'AUTRES':
            ch, propres = arg.split(':'); return table_autres(ch.strip(), [p.strip() for p in propres.split(',')])
        if cmd == 'CATALOGUE': return table_scores_detail([s for s in SECS if s.startswith('4.')], 'Catalogue')
        if cmd == 'PROFONDEUR': return table_scores_detail([s for s in SECS if s.startswith('5.')], 'Profondeur')
        if cmd == 'JALONS': return table_jalons()
        if cmd == 'SC':
            quoi, j = [a.strip() for a in arg.split(',')]
            rs = [r for r in ROWS if r[0].startswith('4.' if quoi == 'cat' else '5.')]
            return pct(score([etat_a(r, j) if j != 'auj' else r[2] for r in rs])[3])
        if cmd == 'DECISION4':
            return liste_decision(arg.strip(), None, 4)
        if cmd == 'DECISION':
            code, *c = arg.split(':'); return liste_decision(code.strip(), c[0].strip() if c else None)
        raise ValueError(cmd)
    return re.sub(r'<!--(\w+)(?::([^>]*))?-->', sub, texte)

BLOCS_EN_LIGNE = {'SC'}

def calculer(cmd, arg):
    return remplir(f'<!--{cmd}:{arg}-->' if arg else f'<!--{cmd}-->')

def baliser(texte):
    """Modèle à marqueurs <!--CMD:ARG--> -> fichier à blocs balisés, recalculables en place"""
    def sub(m):
        cmd, arg = m.group(1), (m.group(2) or '').strip()
        corps = calculer(cmd, arg)
        tete = f'<!-- généré:{cmd}:{arg} -->' if arg else f'<!-- généré:{cmd} -->'
        return f'{tete}{corps}<!-- fin -->' if cmd in BLOCS_EN_LIGNE else f'{tete}\n{corps}\n<!-- fin -->'
    return re.sub(r'<!--(\w+)(?::([^>]*))?-->', sub, texte)

def recalculer(texte):
    """Recalcule en place le contenu de chaque bloc balisé"""
    def sub(m):
        cmd, arg = m.group(1), (m.group(2) or '').strip()
        corps = calculer(cmd, arg)
        return f'{m.group(0).split("-->", 1)[0]}-->{corps}<!-- fin -->' if cmd in BLOCS_EN_LIGNE \
            else f'{m.group(0).split("-->", 1)[0]}-->\n{corps}\n<!-- fin -->'
    return re.sub(r'<!-- généré:(\w+)(?::([^>]*?))? -->.*?<!-- fin -->', sub, texte, flags=re.S)

if __name__ == '__main__':
    # Usage : python3 plans/outils/generer.py            (recalcule les tableaux en place)
    #         python3 plans/outils/generer.py --depuis D  (première génération depuis des modèles)
    depot = pathlib.Path(__file__).resolve().parents[2]
    cibles = [depot / 'Plan-Suite.md'] + sorted((depot / 'plans').glob('*.md'))
    if len(sys.argv) == 3 and sys.argv[1] == '--depuis':
        for f in sorted(pathlib.Path(sys.argv[2]).glob('*.md')):
            out = depot / 'Plan-Suite.md' if f.name == 'Plan-Suite.md' else depot / 'plans' / f.name
            out.write_text(baliser(f.read_text(encoding='utf-8')), encoding='utf-8')
        print('généré depuis les modèles')
    else:
        changes = 0
        for f in cibles:
            avant = f.read_text(encoding='utf-8'); apres = recalculer(avant)
            if apres != avant:
                f.write_text(apres, encoding='utf-8'); changes += 1
        print(f'tableaux recalculés ({changes} fichier(s) modifié(s))')
