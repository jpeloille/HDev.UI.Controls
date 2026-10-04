# État de l'orchestration

Mis à jour : 05/10/2026.

**Orchestratrice** : `orchestrate-hdev-conversations` [2c30b3] (dossier `RiderProjects/HDev.UI.Controls`). Ancien nom : `hdev-ui-controls-e6`. Le nom suit le titre de la session : le vérifier avec `ListAgents` à chaque reprise.

## Exécutantes

| Session | Ref | Mission | Worktree | Branche | État | Dernier contact |
|---|---|---|---|---|---|---|
| `hdev-ui-controls-2a` | 91f9e7 | — (non engagée ; auteure d'E0, E1, du plan de la suite) | atelier `HDev.UI.Controls` (+ `HDev.UI.DataGrid`) | `suite` | inactive, mode « prompting » | 05/10/2026 |
| `sandbox-6e` | 7d41e4 | hors périmètre (admin système : désinstallation FortiClient, dossier `~/SandBox`) | — | — | hors orchestration, mode « prompting » | 05/10/2026 |

## Missions

Aucune.

## Questions en attente de Julien

| N° | De | Question |
|---|---|---|
| QX-01 | 2a | Commiter sur `suite` les 5 fichiers de report des décisions Q3 (Conception-Socle-Editeurs.md, MIGRATION-editeurs.md, Plan-Editeurs.md, Plan-Suite.md, plans/outils/lots.py ; E1 passe à ✅) ? |
| QX-02 | 2a | Vérification sur le bureau avant E2 : une liste déroulante ouverte survit-elle à une brève désactivation de la fenêtre ? (non testable en headless ; à faire par Julien) |
| QX-03 | 2a | Lancer E2 (socle composite des éditeurs) ? |
| QX-04 | 2a | Décisions ouvertes du plan maître : Q2 LiveCharts2, Q4 moteur PDF/impression, Q5 formats bureautiques, Q6 IA, Q7 22 absences proposées, Q8 absences actées à reconfirmer, Q9 licences, Q10 icônes, Q11 moteur Gantt en assembly pur, Q12 formules L25, Q13 modèle de ressources agenda/Gantt. |
| QX-05 | 2a | Pour E3 : `Placeholder` dans `IHDevEditor` n'a pas de sens pour les contrôles de choix. |
| QX-06 | 2a | Proposés, non faits : note datée Q1 dans UGantt-charte.md, réalignement § 0 d'IMPROVEMENTS.md (dépôt grille), mise à jour de context.md. |
| QX-07 | 2a | Anciens : charte HDev (proposée, non validée) ; PDF du benchmark (ONLYOFFICE ou impression Edge). |

## Fusions à décider

Aucune.

## Points d'attention

- Les exécutantes n'ont pas pu joindre le nom `hdev-ui-controls-e6` : `sandbox-6e` a répondu à l'adresse d'envoi. Le brief doit donc demander de répondre à l'expéditeur (`from`) du message reçu.

- L'atelier (`suite`) porte 5 modifications non commitées de `hdev-ui-controls-2a` (report Q3) : voir QX-01.
- État de référence (2a) : `hdev` à f666e3d dans HDev.UI.Controls, 7dd3ae1 dans HDev.UI.DataGrid ; `suite` : E0 f666e3d, E1 eeead1a, plan 6c01bfe ; banc 136/136, xUnit 380 ; rien de poussé.
