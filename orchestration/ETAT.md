# État de l'orchestration

Mis à jour : 13/10/2026. Équipe d'agents active depuis la relance du 06/10.

Tranchées : QX-01 (oui, commit 49bbeb6), QX-03 (oui, mission M-01), QX-02 (option (b), vérifiée sur le bureau réel le 13/10, M-01 fusionnée).

**Orchestratrice** : `hdev-orchestration-agent-teams` [2c30b3], cheffe d'équipe (dossier `RiderProjects/HDev.UI.Controls`). Anciens noms : `orchestrate-hdev-conversations`, `hdev-ui-controls-e6`. Le nom suit le titre de la session : le vérifier avec `ListAgents` à chaque reprise.

## Exécutantes

| Session | Ref | Mission | Worktree | Branche | État | Dernier contact |
|---|---|---|---|---|---|---|
| ~~`hdev-ui-controls-2a`~~ (fermée le 06/10) | 91f9e7 | M-03 (diagnostic bureau) | atelier `HDev.UI.Controls` (+ `HDev.UI.DataGrid`) | `suite` | inactive, mode « prompting » | 05/10/2026 |
| `sandbox-6e` | 7d41e4 | hors périmètre (admin système : désinstallation FortiClient, dossier `~/SandBox`) | — | — | hors orchestration, mode « prompting » | 05/10/2026 |

## Missions

Une coéquipière meurt avec l'orchestratrice. À la reprise, relancer la mission depuis sa fiche et l'état de sa branche (`git log suite..suite-M-nn`, fichiers non commités).

| Mission | Lot | Worktree | Branche | Exécutante (coéquipière ou conversation) | État |
|---|---|---|---|---|---|
| [M-01](missions/M-01.md) | E2, socle composite | `atelier/missions/M-01/` (Controls + DataGrid en lecture) | `suite-M-01` | ancien sous-agent (session du 05/10, mission finie : ne pas recréer) | **fusionnée dans `suite` le 13/10 (4f16606)** ; bureau réel fait, focus des boutons 5485892, notes bc734ed, banc 189/189, xUnit 380 ; verif-02 FUSIONNABLE ; P1/P2 et chiffres d2dd2d3 (18 commits) ; verif-01 : 7 contrôles OK ; correctif (b) f4aa045 + Échap des menus 71c7cfc, banc 187/187 ×3 ; avant cela : **FIN** : 14 commits (49bbeb6..7ce4131), banc 175/175 ×3, xUnit 380 ; vérifié par l'orchestratrice (banc ALL PASS, xUnit 380, aucune mention DevExpress ni Co-Authored-By) |
| [M-03](missions/M-03.md) | diagnostic QX-02 (popups non maintenus sur désactivation) | `atelier/missions/M-03/` (base f666e3d, détachée) + M-01 en lecture | — | `hdev-ui-controls-2a` | 71c7cfc validé : banc 60/60, GNOME sans écran OK ; **bureau réel fait le 13/10** par une conversation de l'IDE (entrées uinput, sur M-01 d2dd2d3 puis 029009c) : close |
| [M-02](missions/M-02.md) | retrait des mentions DevExpress dans le code | `atelier/missions/M-02/` (Controls ; DataGrid en lecture) | `suite-M-02` (depuis 0adcb1e) | sous-agent hdev-executante, lancé le 13/10 par la session de l'IDE du dépôt grille (meurt avec elle : relancer depuis la fiche et la branche) | **fusionnée dans `suite` le 13/10 (31ce3d4)** : 23173ba, cf05236, 15198fd (commentaires seulement) ; banc 189/189, xUnit 380 ; verif-03 FUSIONNABLE ; exception : `context.md` |

## Questions en attente de Julien

| N° | De | Question |
|---|---|---|
| QX-04 | 2a | Décisions ouvertes du plan maître : Q2 LiveCharts2, Q4 moteur PDF/impression, Q5 formats bureautiques, Q6 IA, Q7 22 absences proposées, Q8 absences actées à reconfirmer, Q9 licences, Q10 icônes, Q11 moteur Gantt en assembly pur, Q12 formules L25, Q13 modèle de ressources agenda/Gantt. |
| QX-05 | 2a | Pour E3 : `Placeholder` dans `IHDevEditor` n'a pas de sens pour les contrôles de choix. |
| QX-06 | 2a | Proposés, non faits : note datée Q1 dans UGantt-charte.md, réalignement § 0 d'IMPROVEMENTS.md (dépôt grille), mise à jour de context.md. |
| QX-07 | 2a | Anciens : charte HDev (proposée, non validée) ; PDF du benchmark (ONLYOFFICE ou impression Edge). |

## Fusions à décider

Aucune. Dernières : `suite-M-02` → `suite` le 13/10/2026 (31ce3d4), verif-03 FUSIONNABLE ; `suite-M-01` → `suite` le 13/10/2026 (4f16606), verif-02 FUSIONNABLE. Décompte de Plan-Suite.md passé à 36/25/95/10 ; `generer.py` relancé, aucun tableau modifié. APP-09 rejoué : confirmé (voir JOURNAL du 13/10), reste en E3.

## Points d'attention

- **Grille : 12 mentions d'une bibliothèque tierce relevées par M-02, rien de modifié.** Mission de nettoyage à décider par Julien. La plus exposée : `src/HDev.UI.DataGrid.csproj:10` (`<Description>` publiée dans le paquet NuGet). Les autres : `README.md:3` ; `IMPROVEMENTS.md:1, 4, 12, 132, 247` ; thèmes WinXI de `samples/DemoApp` (`WinXIThemeBundle.axaml:6`, `WinXIComponents.axaml:6`, `WinXITheme.axaml:6 et 135`, `WinXINavigation.axaml:6`).
- Remarque mineure de verif-03, sans suite pour l'instant : `HDevComboBox.cs:312-313` relie ButtonClick au chevron. Or, quand le contrôle n'est pas éditable, un clic n'importe où sur lui le déclenche.

- Les exécutantes n'ont pas pu joindre le nom `hdev-ui-controls-e6` : `sandbox-6e` a répondu à l'adresse d'envoi. Le brief doit donc demander de répondre à l'expéditeur (`from`) du message reçu.

- État de référence (2a) : `hdev` à f666e3d dans HDev.UI.Controls, 7dd3ae1 dans HDev.UI.DataGrid ; `suite` : E0 f666e3d, E1 eeead1a, plan 6c01bfe, apports des applications 44a69bd et ec9e34b (`Plan-Apports.md`, 118 lots) ; E2 fusionné 4f16606, M-02 fusionnée 31ce3d4 ; banc 189/189, xUnit 380 ; rien de poussé.
