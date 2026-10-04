# État de l'orchestration

Mis à jour : 05/10/2026.

Tranchées : QX-01 (oui, commit 49bbeb6), QX-03 (oui, mission M-01).

**Orchestratrice** : `orchestrate-hdev-conversations` [2c30b3] (dossier `RiderProjects/HDev.UI.Controls`). Ancien nom : `hdev-ui-controls-e6`. Le nom suit le titre de la session : le vérifier avec `ListAgents` à chaque reprise.

## Exécutantes

| Session | Ref | Mission | Worktree | Branche | État | Dernier contact |
|---|---|---|---|---|---|---|
| `hdev-ui-controls-2a` | 91f9e7 | — (non engagée ; auteure d'E0, E1, du plan ; n'exécute pas M-01) | atelier `HDev.UI.Controls` (+ `HDev.UI.DataGrid`) | `suite` | inactive, mode « prompting » | 05/10/2026 |
| `sandbox-6e` | 7d41e4 | hors périmètre (admin système : désinstallation FortiClient, dossier `~/SandBox`) | — | — | hors orchestration, mode « prompting » | 05/10/2026 |

## Missions

Un sous-agent meurt avec l'orchestratrice. À la reprise, relancer la mission depuis sa fiche et l'état de sa branche (`git log suite..suite-M-nn`, fichiers non commités).


| Mission | Lot | Worktree | Branche | Exécutante | État |
|---|---|---|---|---|---|
| [M-01](missions/M-01.md) | E2, socle composite | `atelier/missions/M-01/` (Controls + DataGrid en lecture) | `suite-M-01` | sous-agent d'arrière-plan de l'orchestratrice, id `a91312d0147b99d5a` | arrêtée sur QUESTION M-01 (6 points), 6 commits e5cbd95…bc088e6, banc 151/151 ×3, xUnit 380 |

## Questions en attente de Julien

| N° | De | Question |
|---|---|---|
| QX-02 | 2a | Préalable à la **fusion** d'E2 (M-01) : vérification sur le bureau : une liste déroulante ouverte survit-elle à une brève désactivation de la fenêtre ? (non testable en headless ; à faire par Julien) |
| QX-04 | 2a | Décisions ouvertes du plan maître : Q2 LiveCharts2, Q4 moteur PDF/impression, Q5 formats bureautiques, Q6 IA, Q7 22 absences proposées, Q8 absences actées à reconfirmer, Q9 licences, Q10 icônes, Q11 moteur Gantt en assembly pur, Q12 formules L25, Q13 modèle de ressources agenda/Gantt. |
| QX-05 | 2a | Pour E3 : `Placeholder` dans `IHDevEditor` n'a pas de sens pour les contrôles de choix. |
| QX-06 | 2a | Proposés, non faits : note datée Q1 dans UGantt-charte.md, réalignement § 0 d'IMPROVEMENTS.md (dépôt grille), mise à jour de context.md. |
| QX-07 | 2a | Anciens : charte HDev (proposée, non validée) ; PDF du benchmark (ONLYOFFICE ou impression Edge). |

### M-01 : questions d'API (posées le 05/10/2026)

Détail : JOURNAL.md, à la même date. Pour chaque question, la recommandation de l'exécutante :

1. **Bouton** : `HDevEditorButton` avec `Kind` / `HDevEditorButtonKind` (`DropDown, Clear, Ellipsis, Search, Plus, Minus, Custom`) et `Icon` pour `Custom`.
   - L'infobulle, la commande et la visibilité viennent d'Avalonia (`ToolTip.Tip`, `Command`, `IsVisible`, `IsEnabled`).
   - `ButtonClick` est un événement CLR.
   - Actions par défaut : `DropDown` bascule le popup ; `Clear` vide la valeur, sauf si l'événement est traité.
   - Les boutons propres à un éditeur entrent dans `Buttons` au portage, en E3.
   - Pas de boutons à gauche en E2.
2. **Popup public** :
   - la propriété de contenu s'appelle `DropDownContent` ;
   - `DropDownOpening` reçoit un `CancelEventArgs` ;
   - `DropDownClosing` reçoit `HDevDropDownClosingEventArgs` (`Cancel`, `Reason` = `Accept`/`Cancel`/`Outside`, `AcceptValue`) ;
   - `DropDownClosed` reçoit `Reason` et `AcceptValue` ;
   - ce sont des événements CLR.
3. **Glyphes** : `Clear, Ellipsis, Search, Clock, Calculator, Link, Folder, Swatch, Plus, Minus`.
4. **Valeur de HDevButtonEdit** : (i) la couche typée ne déclare plus `Value` ; chaque éditeur déclare sa propriété de valeur, ce qui touche E1. Ou bien (ii) l'éditeur reste hors de la couche typée. `Text` est une StyledProperty liable dans les deux sens, avec un bouton `Ellipsis` par défaut. Recommandation : (i).
5. **Valeur de HDevPopupContainerEdit** : `Value` (`object?`), affichée par `ValueTemplate` ou `ToString()`, non éditable. Le contenu se lie à `Value`, et `AcceptValue = false` ne touche à rien.
6. **Zone de valeur** : le point d'accroche reste protégé en E2 (`SetValuePresenter`).

## Fusions à décider

Aucune.

## Points d'attention

- Les exécutantes n'ont pas pu joindre le nom `hdev-ui-controls-e6` : `sandbox-6e` a répondu à l'adresse d'envoi. Le brief doit donc demander de répondre à l'expéditeur (`from`) du message reçu.

- État de référence (2a) : `hdev` à f666e3d dans HDev.UI.Controls, 7dd3ae1 dans HDev.UI.DataGrid ; `suite` : E0 f666e3d, E1 eeead1a, plan 6c01bfe ; banc 136/136, xUnit 380 ; rien de poussé.
