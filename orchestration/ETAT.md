# État de l'orchestration

Mis à jour : 13/10/2026. Équipe d'agents active depuis la relance du 06/10.

Tranchées : QX-01 (oui, commit 49bbeb6), QX-03 (oui, mission M-01), QX-02 (option (b), vérifiée sur le bureau réel le 13/10, M-01 fusionnée), QX-05 (13/10 : `Placeholder` sort de `IHDevEditor`, interface propre aux éditeurs à zone de texte), QX-08 (13/10 : les 8 recommandations de m-05 pour E3, voir JOURNAL et fiche M-05).

**Orchestratrice** : `hdev-ui-controls-49` [28aee0] depuis la reprise du 13/10, cheffe d'équipe, travaille dans l'atelier (`atelier/HDev.UI.Controls`, branche `suite`). Anciens noms : `hdev-orchestration-agent-teams` [2c30b3], `orchestrate-hdev-conversations`, `hdev-ui-controls-e6`. Sessions vivantes à la reprise : `hdev-ui-controls-e2` [32ff21] (IDE, sans mission), `igor-*` (hors périmètre). Le nom suit le titre de la session : le vérifier avec `ListAgents` à chaque reprise.

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
| [M-04](missions/M-04.md) | F13 (+ F12 en partie, début de F8) : catalogue de thèmes `themes-catalogue` repris sur `suite`, unifié avec `cd5e68b` | `atelier/missions/M-04/` (Controls ; DataGrid en lecture) | `suite-M-04` (depuis dd88c51) | sous-agent hdev-executante `m-04`, lancé le 13/10 par l'orchestratrice | **fusionnée dans `suite` le 13/10 (9c35aa1)** : 9acb18d..b1f8a4a + renommage `HDevThemeVariantKeys` ; banc 189/189, xUnit 422 ; verif-04 FUSIONNABLE ; écarts 1–5 validés par Julien |
| [M-05](missions/M-05.md) | E3, portage des éditeurs existants sur le socle | `atelier/missions/M-05/` (Controls ; DataGrid en lecture) | `suite-M-05` (depuis b9573c5) | sous-agent hdev-executante `m-05`, lancé le 13/10 par l'orchestratrice | **en cours** (QX-08 validée, reprise le 13/10) : 8d52d25 (APP-09/10/11, n° 3), b32a627 (SpinEdit, TimeEdit), 8eca09d (SearchControl) ; banc 203/203 ×3, xUnit 422 |

## Questions en attente de Julien

| N° | De | Question |
|---|---|---|
| QX-04 | 2a | Décisions ouvertes du plan maître : Q2 LiveCharts2, Q4 moteur PDF/impression, Q5 formats bureautiques, Q6 IA, Q7 22 absences proposées, Q8 absences actées à reconfirmer, Q9 licences, Q10 icônes, Q11 moteur Gantt en assembly pur, Q12 formules L25, Q13 modèle de ressources agenda/Gantt. |
| QX-06 | 2a | Proposés, non faits : note datée Q1 dans UGantt-charte.md, réalignement § 0 d'IMPROVEMENTS.md (dépôt grille), mise à jour de context.md. |
| QX-07 | 2a | Anciens : charte HDev (proposée, non validée) ; PDF du benchmark (ONLYOFFICE ou impression Edge). |

## Fusions à décider

Aucune. Dernière : `suite-M-04` → `suite` le 13/10/2026 (9c35aa1), `generer.py` relancé, aucun tableau modifié. Dernières : `suite-M-02` → `suite` le 13/10/2026 (31ce3d4), verif-03 FUSIONNABLE ; `suite-M-01` → `suite` le 13/10/2026 (4f16606), verif-02 FUSIONNABLE. Décompte de Plan-Suite.md passé à 36/25/95/10 ; `generer.py` relancé, aucun tableau modifié. APP-09 rejoué : confirmé (voir JOURNAL du 13/10), reste en E3.

## Points d'attention

- **Grille : 12 mentions d'une bibliothèque tierce relevées par M-02, rien de modifié.** Mission de nettoyage à décider par Julien. La plus exposée : `src/HDev.UI.DataGrid.csproj:10` (`<Description>` publiée dans le paquet NuGet). Les autres : `README.md:3` ; `IMPROVEMENTS.md:1, 4, 12, 132, 247` ; thèmes WinXI de `samples/DemoApp` (`WinXIThemeBundle.axaml:6`, `WinXIComponents.axaml:6`, `WinXITheme.axaml:6 et 135`, `WinXINavigation.axaml:6`).
- Remarque mineure de verif-03, sans suite pour l'instant : `HDevComboBox.cs:312-313` relie ButtonClick au chevron. Or, quand le contrôle n'est pas éditable, un clic n'importe où sur lui le déclenche.

- Les exécutantes n'ont pas pu joindre le nom `hdev-ui-controls-e6` : `sandbox-6e` a répondu à l'adresse d'envoi. Le brief doit donc demander de répondre à l'expéditeur (`from`) du message reçu.

- État de référence (2a) : `hdev` à f666e3d dans HDev.UI.Controls, 7dd3ae1 dans HDev.UI.DataGrid ; `suite` : E0 f666e3d, E1 eeead1a, plan 6c01bfe, apports des applications 44a69bd et ec9e34b (`Plan-Apports.md`, 118 lots) ; E2 fusionné 4f16606, M-02 fusionnée 31ce3d4 ; banc 189/189, xUnit 380 ; rien de poussé.
