import re
from benchmark import lignes
from affectation import A
from collections import Counter, defaultdict
ROWS = lignes()
JALONS = ['S1', 'S2', 'S3', 'S4', 'S5', 'S6']

def etat_a(row, jalon):
    sec, label, now = row
    dest, cible, j = A[f'{sec}|{label}']
    if j is None or JALONS.index(jalon) < JALONS.index(j):
        return now
    return cible

def score(etats):
    c = sum(1 for e in etats if e == 'C'); p = sum(1 for e in etats if e == 'P')
    return c, p, len(etats) - c - p, (c + p / 2) / len(etats)

def verifier():
    cles = {f'{s}|{l}' for s, l, _ in ROWS}
    manquantes = cles - set(A); orphelines = set(A) - cles
    assert not manquantes, f'lignes sans destination : {sorted(manquantes)}'
    assert not orphelines, f'destinations sans ligne : {sorted(orphelines)}'
    # une ligne déjà couverte ne régresse jamais ; un état cible n'est jamais moins bon que l'état actuel
    rang = {'A': 0, 'D': 0, 'Q': 0, 'P': 1, 'C': 2}
    for s, l, now in ROWS:
        dest, cible, j = A[f'{s}|{l}']
        assert rang[cible] >= rang[now], (s, l, now, cible)
        if j is None: assert cible == now or (now in 'AD' and cible in 'QD'), (s, l, now, cible, 'jalon manquant')
    # Règle des contrôles phares : la ligne de catalogue passe à ✅ au premier jalon où la check-list atteint 75 %
    PHARES = {'4.1|Grille de données, vue tableau': '5.1', '4.4|Ruban': '5.2', '4.7|Panneaux ancrables': '5.3',
              '4.9|Agenda': '5.4', '4.9|Gantt de projet': '5.5', '4.11|Traitement de texte': '5.6'}
    for cle_ligne, liste in PHARES.items():
        rs = [r for r in ROWS if r[0] == liste]
        premier = next((j for j in JALONS if score([etat_a(r, j) for r in rs])[3] >= 0.75), None)
        dest, cible, j = A[cle_ligne]
        attendu = (premier, 'C') if premier else (None, ROWS_PAR_CLE[cle_ligne])
        assert (j, cible) == attendu, (cle_ligne, j, cible, 'attendu', attendu)
    # Aucun lot ne dépend d'un lot d'un jalon plus tardif (colonne « Dépend de »)
    from lots import LOTS
    for id, (ch, objet, taille, dep, jalon, statut) in LOTS.items():
        for d in re.findall(r'\b([A-Z]\d{1,2})\b', dep):
            assert d not in LOTS or JALONS.index(LOTS[d][4]) <= JALONS.index(jalon), (id, jalon, d, LOTS[d][4])
    return True

ROWS_PAR_CLE = {f'{s}|{l}': e for s, l, e in lignes()}
