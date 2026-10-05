# État de l'orchestration

Mis à jour : 05/10/2026.

Tranchées : QX-01 (oui, commit 49bbeb6), QX-03 (oui, mission M-01).

**Orchestratrice** : `orchestrate-hdev-conversations` [2c30b3] (dossier `RiderProjects/HDev.UI.Controls`). Ancien nom : `hdev-ui-controls-e6`. Le nom suit le titre de la session : le vérifier avec `ListAgents` à chaque reprise.

## Exécutantes

| Session | Ref | Mission | Worktree | Branche | État | Dernier contact |
|---|---|---|---|---|---|---|
| `hdev-ui-controls-2a` | 91f9e7 | M-03 (diagnostic bureau) | atelier `HDev.UI.Controls` (+ `HDev.UI.DataGrid`) | `suite` | inactive, mode « prompting » | 05/10/2026 |
| `sandbox-6e` | 7d41e4 | hors périmètre (admin système : désinstallation FortiClient, dossier `~/SandBox`) | — | — | hors orchestration, mode « prompting » | 05/10/2026 |

## Missions

Un sous-agent meurt avec l'orchestratrice. À la reprise, relancer la mission depuis sa fiche et l'état de sa branche (`git log suite..suite-M-nn`, fichiers non commités).

| Mission | Lot | Worktree | Branche | Exécutante | État |
|---|---|---|---|---|---|
| [M-01](missions/M-01.md) | E2, socle composite | `atelier/missions/M-01/` (Controls + DataGrid en lecture) | `suite-M-01` | sous-agent d'arrière-plan de l'orchestratrice, id `a91312d0147b99d5a` | relancée pour le correctif de garde (b) ; avant cela : **FIN** : 14 commits (49bbeb6..7ce4131), banc 175/175 ×3, xUnit 380 ; vérifié par l'orchestratrice (banc ALL PASS, xUnit 380, aucune mention DevExpress ni Co-Authored-By) |
| [M-03](missions/M-03.md) | diagnostic QX-02 (popups non maintenus sur désactivation) | `atelier/missions/M-03/` (base f666e3d, détachée) + M-01 en lecture | — | `hdev-ui-controls-2a` | cause trouvée ; décision (b) ; reste la vérification sur le bureau après le correctif de M-01 (créneau Julien) |
| [M-02](missions/M-02.md) | retrait des mentions DevExpress dans le code | à créer au lancement | `suite-M-02` | sous-agent | validée, attend la fusion de M-01 |

## Questions en attente de Julien

| N° | De | Question |
|---|---|---|
| QX-02 | 2a | **Échec constaté par Julien (05/10)** : les popups ne survivent pas à une brève désactivation. Diagnostic en M-03. |
| QX-04 | 2a | Décisions ouvertes du plan maître : Q2 LiveCharts2, Q4 moteur PDF/impression, Q5 formats bureautiques, Q6 IA, Q7 22 absences proposées, Q8 absences actées à reconfirmer, Q9 licences, Q10 icônes, Q11 moteur Gantt en assembly pur, Q12 formules L25, Q13 modèle de ressources agenda/Gantt. |
| QX-05 | 2a | Pour E3 : `Placeholder` dans `IHDevEditor` n'a pas de sens pour les contrôles de choix. |
| QX-06 | 2a | Proposés, non faits : note datée Q1 dans UGantt-charte.md, réalignement § 0 d'IMPROVEMENTS.md (dépôt grille), mise à jour de context.md. |
| QX-07 | 2a | Anciens : charte HDev (proposée, non validée) ; PDF du benchmark (ONLYOFFICE ou impression Edge). |

## Fusions à décider

- `suite-M-01` → `suite` : **bloquée : QX-02 échoue** (Julien, 05/10 : les popups ne restent pas ouverts) ; diagnostic M-03, correctif à appliquer dans M-01. À la fusion, l'orchestratrice met aussi à jour le décompte écrit à la main dans Plan-Suite.md (34/25/97/10 → 36/25/95/10).

## Points d'attention

- Les exécutantes n'ont pas pu joindre le nom `hdev-ui-controls-e6` : `sandbox-6e` a répondu à l'adresse d'envoi. Le brief doit donc demander de répondre à l'expéditeur (`from`) du message reçu.

- État de référence (2a) : `hdev` à f666e3d dans HDev.UI.Controls, 7dd3ae1 dans HDev.UI.DataGrid ; `suite` : E0 f666e3d, E1 eeead1a, plan 6c01bfe ; banc 136/136, xUnit 380 ; rien de poussé.
