# Plan des apports de Synaxis et de myFlightOpsBoard à la suite HDev.UI

Plan rattaché au plan de la suite (`Plan-Suite.md`), relevé du **06/10/2026**. Statut : **proposition**, à valider.

- **Objet.** Recenser la totalité de ce que Synaxis et myFlightOpsBoard ont apporté à la suite, ou construit mieux qu'elle, et donner à chaque apport une seule destination : un lot, une décision, ou un écart motivé.
- **Seul document qui nomme les applications.** Le plan maître, les fichiers de chantier et `Plan-Editeurs.md` n'y renvoient que par un numéro APP et une description générique (§ 1, règle 4).
- **Lots.** Les dix lots nés des apports (F12 à F15, V5, W6, M4, N6, T6, T7) ont leur détail ici (§ 2). `plans/outils/lots.py` les porte, et les tableaux générés des chantiers les affichent.

## 0. Contexte et méthode

**La demande.** Julien, le 06/10/2026 : « dans le plan tout ce qui est mieux et qui a été apporté par synaxis et myFlightOpsBoard », puis « enregistre la totalité dans un plan ».

**Le constat.** Le plan de la suite (`Plan-Suite.md`, `plans/*.md`, `Plan-Editeurs.md`) est tiré du seul benchmark DevExpress. Or les deux applications ont construit de leur côté des choses meilleures que la suite, ou qu'elle n'a pas. Elles ont aussi révélé des défauts de la suite.

**Trois inventaires, en lecture seule, les ont recensés :**
- le code de `Synaxis/Synaxis.Ui.Desktop` : 88 fichiers, 24 411 lignes, un seul `.axaml`, aucun contrôle dessiné, tout composé en C# ;
- le code de `myFlightOpsBoard/src/myFlightOpsBoard.App` (environ 17 000 lignes), avec `Core` et `Reporting`, sur la branche `briefing-notam-meteo` ;
- les traces écrites : mémoires des sessions des deux applications, documents, historique git des deux dépôts de la suite.

**Abréviations.**
- Chemins Synaxis : relatifs à `~/RiderProjects/Synaxis/Synaxis.Ui.Desktop/`, sauf mention de `Synaxis.Reporting`.
- Chemins du board : relatifs à `~/RiderProjects/myFlightOpsBoard/src/myFlightOpsBoard.App/`, sauf mention de `Core/` (myFlightOpsBoard.Core), `Reporting/` ou `docs/`.
- Chemins de la suite : relatifs à l'atelier, `HDev.UI.Controls/` et `HDev.UI.DataGrid/src/`.
- « nc » : non commité dans l'application au 06/10/2026, le chemin peut bouger. Les numéros de ligne datent du relevé.

**Faits vérifiés dans l'atelier par l'orchestratrice**
- **Contraste des couples d'état, en clair.**
  - Avertissement #9C6E03 sur #F9F0DC : **3,99:1**.
  - Lien #1C71D8 sur fond d'information #DDE8F9 : **3,86:1**.
  - Le succès (4,94) et l'erreur (4,83) passent.
  - En sombre, tous les couples sont au-dessus de 4,59.
  - `ThemeContrastTests` ne teste aucun couple « texte d'état sur fond d'état ».
- **`Crisp`** est `internal` (`HDevControlBase.cs:28`).
- **`HDevTheme.ApplyTo`** publie `SystemAccent*` et `HDev*Brush`, mais aucune clé `DataGrid*`. `YaruDataGrid.axaml` suit la variante (ThemeDictionaries), pas la palette.
- **`FrozenColumnCount`** est déclaré, mais jamais lu.
- **Double recalcul.** `GridDataSource` et `HDevDataGrid` s'abonnent tous deux au `CollectionChanged` de la source, d'où deux `Refresh` par changement.

Les autres défauts sont des **constats à graver** avant correction, selon la méthode d'E0. Ce ne sont pas des faits établis.

**Côté git**
- Synaxis n'a que 4 commits d'interface : 198f4a8, 4d25a03, e8af7be, 70d7937. Il a 19 fichiers modifiés et 11 nouveaux qui ne sont pas commités.
- Quatre commits de `hdev` ne sont pas poussés : c943d21, 028e7d1, 4fc218e et f666e3d. Les applications les compilent pourtant déjà.
- Les mémoires de Synaxis sont en retard sur git.

## 1. Règles du plan des apports

1. **Mérite, pas usage.** Un apport entre parce qu'il est meilleur, ou parce qu'il manque à la suite, et il est décrit en termes génériques. Les jalons suivent les dépendances d'architecture, jamais l'usage d'une application (Plan-Suite, principe 1).
2. **Une seule destination par apport**, comme pour les 342 lignes du benchmark. Les doublons des deux applications sont fusionnés.
3. **Nature de l'apport :**
   - (a) acquis, déjà dans la suite ; la colonne « figé par » nomme le lot qui le protège ;
   - (b) capacité qui enrichit un lot existant ;
   - (c) capacité qui demande un nouveau lot ;
   - (d) constat à graver puis corriger ;
   - (e) pièce au dossier d'une décision Q ;
   - (g) méthode ;
   - (f) écarté, avec sa raison.
4. **Ce plan est le seul document qui nomme les applications et leurs fichiers.** Les chantiers et le plan maître n'y renvoient que par un numéro APP et une description générique. Le benchmark ne change pas.
5. **Salle blanche.** On écrit « hors benchmark », jamais « DevExpress n'a pas X », et on ne cite aucun nom d'API DevExpress. Le code des applications appartient à Julien : il peut se porter. Les licences des dépendances tierces relèvent de Q9.
6. **On ne retire que ce qui a un équivalent.** C'est ce qui guide la reprise de HDevRoster (T6), et le sort de `GanttTask.Color` en O1.
7. **Projection inchangée.** Les nouveaux lots ne visent aucune ligne du benchmark, donc les scores restent ceux du plan maître.
8. **Jalons.**
   - Ce qui viole une contrainte commune du § 4 du plan maître (contraste à 4,5:1, `Crisp.Snap` hors de portée) passe dans un petit lot précoce, F12, au jalon S1.
   - **Exception proposée : le clavier de HDevRoster.** Il est traité avec T6, en S5, puisque la frise est reprise et qu'aucune application ne l'utilise plus. Il peut remonter en F12 si Julien préfère une conformité immédiate.

## 2. Nouveaux lots

Ils s'inscrivent dans les chantiers existants, et leur détail fait foi ici. `lots.py` passe de 108 à **118 lots**. Une dépendance existante change : W1 dépend désormais de W6, et les deux lots restent en S3.

| Lot | Objet | Taille | Dépend de | Jalon |
|---|---|---|---|---|
| F12 | Correctifs du socle : contraste des couples d'état, `Crisp` public | S | — | S1 |
| F13 | Amorçage et thème vivant : un seul appel, palette jusqu'à la grille, jetons liés depuis le code, tons d'état, chasse fixe | M | F12 | S2 |
| F14 | Socle des contrôles dessinés : carte de détail, clavier en deux dimensions, légende, motifs | M | F12 | S4 |
| F15 | Outillage de vérification : démo pilotable, états posables par code, planches réutilisables | S | — | S1 |
| V5 | Indicateurs : tuile, jauge linéaire, progression segmentée, sparkline d'intervalles, puce d'état | M | F13 | S2 |
| W6 | Bandeau dans la page, état d'une source, réserves repliables | S | F13 | S3 |
| M4 | Fiche en lecture : en-tête, ligne d'informations, cartes, sections, ouverture progressive | M | F13 | S3 |
| N6 | Listes riches : groupes à en-tête libre, détail lié, fil chronologique | M | F1 | S4 |
| T6 | Frise de ressources v2 : journée décalée, groupes, couloirs, disponibilité, carte, clavier | L | T0, F14 | S5 |
| T7 | Matrice ressources × jours à zones figées | L | F14, V5, I2 | S5 |

### F12 : correctifs du socle (S, S1)

**Pourquoi.** Le § 4 du plan maître n'est pas tenu. Deux couples texte-fond sont sous 4,5:1 sans qu'aucun test ne les voie. `Crisp.Snap`, que le § 4 impose pour tout texte dessiné, reste inaccessible hors de la bibliothèque.

**Contenu.**
- L'avertissement et le lien sur fond d'information passent à 4,5:1 au moins dans la palette claire.
- `ThemeContrastTests` couvre tous les couples « texte d'état sur fond d'état », pour toute palette complète.
- `Crisp` devient public, avec ses tests.

**Acceptation.** Les tests de contraste passent avec les nouveaux couples. Dans les planches, seules changent les teintes corrigées.

**Apports.** APP-22, APP-24.

### F13 : amorçage et thème vivant (M, S2)

**Pourquoi.** Une palette injectée n'atteint ni la grille, ni les pinceaux posés depuis le code, ni les contrôles d'Avalonia que les éditeurs embarquent. Chaque application recopie l'amorçage.

**Contenu.**
- Une extension d'amorçage unique : popups en overlay, police, styles Fluent et grille, `ApplyTo`.
- `ApplyTo` publie aussi les clés `DataGrid*`, dérivées de la palette, avec une zébrure visible.
- Un moyen de lier une propriété à un jeton depuis le code, ou un texte à rôles (légende, corps, libellé, titre, chasse fixe).
- Des tons d'état à deux variantes, et un style de valeur : libellé, abréviation, icône, teinte, emphase.
- La lecture de la palette d'une variante non active.
- Un jeton de police à chasse fixe.

**Acceptation.**
- Une application témoin démarre en un seul appel.
- Une palette injectée recolore la grille.
- Une bascule de variante repeint les pinceaux posés depuis le code, sans reconstruire la vue.
- Le contraste des tons est testé.

**Apports.** APP-23, APP-25 à APP-28. S'appuie sur les acquis APP-04 et APP-08.

### F14 : socle des contrôles dessinés (M, S4)

**Pourquoi.** Les contrôles dessinés de la suite (agenda, roster, Gantt) n'offrent qu'une infobulle texte et un clavier partiel, voire aucun.

**Contenu.**
- **La carte de détail peinte.** Son modèle est neutre : titre, sous-titre, lignes à ton, liste, alertes, pied. Il est partagé avec les infobulles riches de C4, et c'est le premier des deux lots qui le définit.
- La sélection au clavier en deux dimensions.
- La légende peinte par la routine de l'élément.
- Les motifs de rendu et les outils de texte dessiné.
- Le défilement qui amène un élément en haut.

**Acceptation.**
- Des tests de géométrie : placement et retournement de la carte.
- Des tests headless du clavier.
- Des planches des motifs, en clair et en sombre.
- Une adoption de démonstration par HDevScheduler ou par HDevGantt.

**Apports.** APP-29 à APP-34.

### F15 : outillage de vérification (S, S1)

**Pourquoi.** Le § 9 du plan maître le pose : la survie des popups ne se prouve que sur le bureau réel, où aucune entrée simulée n'atteint la fenêtre.

**Contenu.**
- La Démo pilotable par des liens de démarrage : page, thème, état (popup ouvert, sélection), et scénario minuté (désactivation brève de la fenêtre).
- Des états posables par code (sélection, survol) sur les contrôles dessinés.
- Le rendu PNG d'une vue depuis l'application.
- Un kit de planches utilisable hors de la suite.

**Acceptation.** Un état précis s'atteint sans entrée simulée et se capture. Un scénario minuté se rejoue.

**Apports.** APP-35 à APP-37. La pratique du bureau correspondante est APP-108.

### V5 : indicateurs (M, S2)

**Pourquoi.** Ni HDevCard, ni HDevProgressBar, ni HDevBadge ne portent d'indicateur. Ces indicateurs sont dessinés maison : ils ne dépendent **ni de V0 et V1** (Q2, LiveCharts2) **ni d'I1** (Q4).

**Contenu.**
- La tuile d'indicateur, une évolution de HDevCard.
- La jauge linéaire à segments, à repère et à seuils, et la micro-jauge.
- La progression segmentée par étape, une évolution de HDevProgressBar : hauteur fine et teinte réglable.
- La sparkline d'intervalles.
- La puce d'état teintée, une évolution de HDevBadge, et les marqueurs à forme redondante.

**Acceptation.**
- Des planches en clair et en sombre.
- Un contraste des puces calculé (`EnsureContrast` sur le fond composé).
- Des tests headless du filtre posé par une tuile.

**Apports.** APP-60 à APP-63.

### W6 : bandeau dans la page (S, S3)

**Pourquoi.** Il n'existe aucun message persistant posé dans la page : HDevToast est passager, HDevMessageBox est modale.

**Contenu.**
- Le bandeau : information, avertissement, erreur ; amorce en gras ; action.
- L'état d'une source :
  - à jour, à rafraîchir ou injoignable ;
  - dernière lecture datée ;
  - période couverte ;
  - action « Relecture… ».
- Les réserves repliables : en-tête compact, compte, hauteur maximale.

**Acceptation.** Les couples de contraste sont testés, et les planches existent. W1 s'en sert pour l'erreur d'un dialogue.

**Apports.** APP-77 à APP-79.

### M4 : fiche en lecture (M, S3)

**Pourquoi.** M1 vise les formulaires d'édition. Rien n'existe pour la lecture.

**Contenu.**
- L'en-tête de fiche ; la ligne d'informations à points médians.
- Les cartes (évolution de HDevCard) : clic, survol, liseré d'état, emplacement d'action, état accentué, section titrée à geste.
- Les briques de lecture : grille de valeurs, tableau léger, liste libellé/valeur à ton, historique, encadré d'alerte.
- Le gabarit fiche et colonne latérale, avec des défilements séparés et restaurés.
- L'ouverture progressive, section par section.

**Acceptation.**
- Des planches.
- Des tests headless : troncature du titre, défilement restauré après reconstruction, échec isolé d'une section.

**Apports.** APP-66 à APP-70.

### N6 : listes riches (M, S4)

**Pourquoi.** HDevListView n'a que des lignes fixes, avec titre, sous-titre et badge.

**Contenu.**
- **La liste groupée :**
  - puces de groupes, avec compte et accès au clavier ;
  - en-tête de groupe à contenu libre ;
  - groupes fermés résumés ;
  - plis ;
  - recherche qui ouvre les groupes ;
  - détail lié.
- **Le fil chronologique**, avec son champ de saisie.

**Acceptation.** Des tests headless (recherche, clavier, détail lié) et des planches.

**Apports.** APP-87, APP-88.

### T6 : frise de ressources v2 (L, S5)

**Pourquoi.** HDevRoster ne tient pas le § 4 : il n'a pas de clavier. Il perd sa sélection quand on réassigne son modèle, impose une hauteur fixe et n'a qu'une infobulle texte. Il n'a plus de consommateur.

**Contenu.** Reprise de HDevRoster :
- un axe décalé, ou un axe qui suit le modèle ;
- des intertitres de groupe, des hauteurs de voie variables, une colonne figée ;
- des blocs stylés par une description (fond, trait, pointillé, hachure, position, ordre de peinture), qui remplace le seul `LaneAdorner` ;
- des durées entre blocs, des marqueurs d'extrémité, des rangées de débordement ;
- des bandes de disponibilité à états, des couloirs à plusieurs intervalles, des marqueurs ponctuels ;
- la carte de détail et le clavier de F14 ;
- la sélection conservée par clé ;
- la frise et la liste liées par la sélection, sur le contrat de F1.

**Garde** le zoom, le glisser annulable et la virtualisation.

**Acceptation.**
- Des tests du moteur : couloirs, débordement.
- Des tests headless du clavier, et des planches.
- Les tests de T0 restent verts, ou leurs écarts vont dans la note de migration.

**Apports.** APP-94 à APP-98. Reprend l'acquis APP-05.

### T7 : matrice ressources × jours (L, S5)

**Pourquoi.** Personne ne fait cette matrice dans la suite :
- la grille ne fige pas de lignes de données et impose une hauteur uniforme ;
- la vue mois de l'agenda est un calendrier.

**Contenu.**
- Des zones figées : lignes de comptage en haut, colonne à gauche, coin.
- Des hauteurs variables, des intertitres, des totaux.
- Des cases à catégorie et à code ; des en-têtes teintés ; les jours passés estompés ; la colonne du jour.
- Des jauges en bout de ligne (V5).
- Une carte construite par l'hôte, et le clavier de F14.
- Le même plan pour l'écran et pour l'export (I2).

**Acceptation.**
- Des tests de disposition.
- Des tests headless : clavier, activation.
- Des planches.
- Un export XLSX relu.

**Apports.** APP-99, APP-100.

## 3. Registre complet : APP-01 à APP-115

### 3.1 Acquis (a) : déjà dans la suite

**APP-01 — En-tête coloré par section d'accordéon ; badge et couleur de badge sur la puce.**
- Commit : 31c7f64 (HDev.UI.Controls, 15/07).
- Origine : Synaxis. Le message du commit dit « Deux enrichissements demandés en construisant CpaListView ».
- Figé par F0. Déjà reporté dans `context.md`, l. 60-62.

**APP-02 — `GridColumn.CellTemplate` enfin rendu.**
- Commit : 571aa7a (HDev.UI.DataGrid, 15/07).
- Origine : Synaxis. Le message dit « payé en vrai par CpaListView » ; voir aussi la mémoire `project_ui_desktop_procontrols.md`.
- Figé par G0.

**APP-03 — Plus de plantage quand une cellule à gabarit est recyclée par la virtualisation.**
- Commit : 7821449 (HDev.UI.DataGrid). Commité le 10/09, pour un travail du 16/07.
- Origine : Synaxis. Le test HeadlessSmoke dit « this is what killed the app on the first double-click in Synaxis ».
- Figé par G0, qui garde le test. G0 vise par ailleurs un autre risque de recyclage, celui d'une édition restée ouverte.

**APP-04 — Palettes injectées par l'application hôte : `SetPalettes(light, dark)`.**
- Commit : 96b9932. Commité le 10/09, pour un travail du 19/07.
- Origine : Synaxis, pour son thème teal et orange (mémoire `feature_desktop_light_theme.md`).
- Figé par F13.

**APP-05 — HDevRoster (ex-ProRoster).**
- Commits : 8b835b1, puis 0e4815d (24/09).
- Origine : myFlightOpsBoard. C'est un portage du `TimelineControl` d'Organizer.Avalonia, promu parce que le board en était le second consommateur (`~/.claude/plans/mellow-mixing-pinwheel.md`, 17-18/09).
- **Le board l'a abandonné le 29/09** (aa246f2) pour ses propres frises. Aucune application ne l'utilise plus.
- Figé par T0 ; repris par T6.

**APP-06 — `GanttTask.Color`, `HDevGantt.TableWidth`, `ScrollToToday(position)`.**
- Commit : 0b1ea10 (30/09).
- Origine : Synaxis, pour l'échéancier des CPA. Le commit Synaxis e8af7be « requiert HDev.UI.Controls 0b1ea10 ».
- Figé par O0.
- `TableWidth` et `ScrollToToday` manquent au § 0 d'`O-gantt.md` : on les y inscrit.
- O1 et Q11 prévoient de remplacer `Color` par une valeur neutre : il faut en garder un équivalent.

**APP-07 — Variantes Outline et OutlineAccent, `TextWeight`, HDevIconButton, HDevRadioCard.**
- Commit : c943d21 (04/10, non poussé).
- Origine : Synaxis. Le message dit « Consommés par le dialogue d'impression des réunions de Synaxis ».
- Figé par C0. HDevRadioCard n'a pas de ligne dans le benchmark.

**APP-08 — `HDevTheme.ApplyTo`, jetons dérivés, palette témoin, `HDevPalette` en record.**
- Commit : 4fc218e (04/10, non poussé), en partie.
- Ce qu'il apporte :
  - `ApplyTo` supprime le bloc `SystemAccentColor` recopié dans chaque `App.axaml` ;
  - les jetons sont dérivés quand la palette est incomplète ;
  - la palette témoin teal de `ThemeContrastTests` est « façon Synaxis » (#267A8C) ;
  - `HDevPalette` devient un `sealed record`, donc dérivable par `with`. Le board le réclamait dans `Theme/BoardTheme.cs`, l. 19-24.
- Origine : les deux applications.
- Figé par F12.

**Attributions probables, non retenues comme acquis**
- 56de13d (15/07) : accordéon, avatar, puce, groupe segmenté.
- RichEdit v2 (a11a34c, 069451b, 16/07) et storage-v1 (070d673 à b4981ee, 19/07).

**Non attribuables aux applications**
- 9a219a2 : né d'une session HDev, puis commité depuis une session Synaxis.
- d5ba354 et 8997638 : programme « Outlook natif ».
- 028e7d1.

### 3.2 Éditeurs (`Plan-Editeurs.md`)

**APP-09 — Une ComboBox éditable à suggestion rouvre sa liste après chaque choix.** (d) → E3. Rejoué à la clôture d'E2 (13/10/2026, bureau réel) : **confirmé** après un choix dans la liste complète ouverte par le chevron ; pas de réouverture après un clic sur une suggestion filtrée par la frappe.
- Constat d'origine :
  - le défaut se produit en `SuggestAppend`, le réglage par défaut ;
  - sous Avalonia 12, `TextChanged` est différé : la garde `_syncingEditText` retombe trop tôt, et `CloseDropDown` rend le focus au champ ;
  - l'atelier a une garde de 250 ms (`HDevComboBox.cs:961-985`) ;
  - d'après l'analyse de session du 06/10, le `JustClosed` de M-01 (l. 982) ne corrige pas ce défaut ;
  - HDevSearchControl a probablement la même cause (constat E0 n° 3).
- Contournement : `AutoComplete = None`.
- Source : Synaxis `Modules/Meeting/Dialogs/OwnerPicker.cs`, l. 49-53 (nc) ; mémoire `feature_gestes_registre_desktop.md`.

**APP-10 — ComboBox : au départ du focus, la saisie n'est pas validée si la liste est ouverte.** (d) → E3
- Atelier : `HDevComboBox.cs:919-924`.
- Contournement : relire le texte affiché plutôt que la valeur.
- Source : Synaxis `OwnerPicker.cs:65-69` (nc).

**APP-11 — SearchControl : `SuggestionText` reçoit un élément nul quand la liste recycle ses lignes.** (d) → E3
- Sans fonction défensive, l'application plante à la deuxième lettre tapée.
- Atelier : `HDevSearchControl.cs:60-63`.
- Source : board `Views/CompteursActiviteView.cs:584-594`.

**APP-12 — Banc headless d'usage réel.** (g) → E3
- 39 contrôles, et un faux serveur HTTP qui relève les requêtes envoyées.
- Il reproduit APP-09. Il était dans `/tmp/owner-picker-bench` : à recréer.

**APP-13 — Case à libellé riche ; liste à cases à sens unique.** (b) → E4. Taille S à M.
- **Ce que c'est :**
  - une case qui ne se coche que dans un sens, rétablie si l'opération est refusée ;
  - le libellé barré une fois l'élément fait ;
  - un ajout par Entrée ;
  - un menu « ⋯ » pour les autres statuts.
- **Constat.** Le libellé de HDevCheckBox est une chaîne (`HDevCheckBox.cs:20`), qu'on ne peut ni barrer ni enrichir. D'où le contournement : une case nue, plus un TextBlock.
- **Source :**
  - Synaxis `Modules/DailyBoard/DailyBoardView.cs:529-586`, `Modules/Meeting/SubjectFreeCards.cs:489-606, 722-816`, `SubjectRegisterCards.cs:700-850` ;
  - board `Views/NotamTousView.cs:134-150`.

**APP-14 — Liste d'énumération à styles.** (b) → E4. Taille S.
- Des listes tirées d'un dictionnaire de styles (libellé, icône, teinte), avec une entrée « — Non défini — ».
- Source : Synaxis `Modules/Cpa/Dialogs/CpaFormControls.cs`.

**APP-15 — Créneau date + heure + durée.** (b) → E6, qui en tire trois cas d'acceptation. Taille M.
- **Ce que c'est.** HDevDateEdit, une liste d'heures par demi-heure et une liste de durées.
- **Les trois cas :**
  - une valeur hors grille (08:15, 50 min) est insérée dans la liste, pas déplacée ;
  - l'heure locale passe en UTC avec le décalage **du jour choisi**, ce qui gère le changement d'heure ;
  - une duplication décale **l'instant**, et non « date locale + heure », qui donnait un jour de retard à UTC+11.
- **Source :** Synaxis `Modules/Meeting/Dialogs/MeetingFormControls.cs` (136 l.), `DuplicateMeetingDialog.cs:52-55`, `Modules/DailyBoard/Dialogs/SerieEditDialog.cs:143-161`.

**APP-16 — Dates et heures civiles.** (b) → E6, en cas de test pour les éditeurs DateOnly, TimeOnly et DateTimeOffset déjà prévus. Taille S.
- **Ce que c'est :**
  - une date sans conversion de fuseau, et une heure civile ;
  - « aujourd'hui » en heure locale, jamais en UTC : à UTC+11, la date UTC est encore la veille avant 11 h ;
  - une échéance à minuit UTC.
- **Source :** Synaxis `Modules/Meeting/Dialogs/RegisterFormControls.cs:64-123`, `Modules/Cpa/Dialogs/CpaFormControls.cs:532-544`.

**APP-17 — Plage de dates bornée.** (b) → E6 : raccourcis du HDevDateRangeEdit prévu, et sélecteur de période autonome. Taille S.
- **Ce que c'est :**
  - deux dates « Du » et « au » ;
  - les raccourcis « Mois en cours », « Année civile » et « jusqu'à aujourd'hui » ;
  - un refus motivé au-delà de la période réellement lue, et un plafond de 400 jours ;
  - les flèches de mois, PgPréc et PgSuiv, F5 pour relire, « Aujourd'hui » et « Demain ».
- **Source :** board `Views/CompteursActiviteView.cs:149-166, 1126-1157`, `FtlAuditView.cs:163-185`, `VueMensuelleView.cs:154-161, 310-325`, `RosterView.cs:137-142, 296-314`.

**APP-18 — Saisie libre, valeur distincte de l'affichage.** (b) → E7. Taille M.
- **Ce que c'est :**
  - chaque item a une valeur (le trigramme) et un affichage (« PLL — Nom ») ;
  - le texte libre est accepté ;
  - un trigramme ou un nom tapé, en toute casse, se résout en sa graphie exacte ;
  - un champ non touché rend sa valeur d'origine telle quelle, sans réécriture silencieuse ;
  - les inactifs ne sont pas proposés, mais la valeur en place est conservée.
- **Suite.** La ComboBox existe ; LookUpEdit n'accepte pas la saisie.
- **Source :** Synaxis `Modules/Meeting/Dialogs/OwnerPicker.cs` (129 l., nc).

**APP-19 — Inactifs marqués, création à la volée.** (b) → E7. Taille S à M.
- **Ce que c'est :**
  - seuls les actifs sont proposés, plus l'élément affecté s'il a été désactivé, marqué « (désactivé) » ;
  - un bouton « Nouveau… » crée l'élément, recharge la liste et le sélectionne ;
  - aucune présélection quand le choix est irréversible et qu'il y a plusieurs candidats.
- **Source :** Synaxis `Modules/Cpa/Dialogs/CpaFormControls.cs:444-481`, `Modules/Meeting/Dialogs/RegisterCreateDialog.cs:126-192`.

**APP-20 — Recherche temporisée et annulable.** (b) → E7. Taille S.
- Un délai de 350 ms, et l'annulation de la requête précédente.
- La remise à zéro des filtres ne recharge qu'une fois.
- Suite : HDevSearchControl n'a pas de temporisation.
- Source : Synaxis `Modules/Meeting/SubjectListView.cs:139-145, 280-300`.

**APP-21 — Une date invalide laisse la valeur sur la dernière date valide.** (d) → E8, ou une lecture `TryGetValue`. Taille S.
- **Atelier :**
  - `Validate()` est public : il rend `false` et affiche l'erreur (`HDevEditorBase.cs:393`, `HDevDateEdit.cs:140-155`) ;
  - mais `Value` garde la dernière date valide (`HDevDateEdit.cs:121-135`).
- **Pourquoi y revenir.** Le constat E0 n° 5 juge ce comportement défendable. Sur le fond, une valeur qui diverge du texte affiché reste un risque.
- **Source :** Synaxis vérifie l'aller-retour texte ↔ valeur à dix endroits (`RegisterFormControls.cs:89-123`, `LogEntryEditDialog.cs:218-219`, `RegisterActionEditDialog.cs:196`, `SubjectSteeringDialog.cs:166`, `RegisterOpeningFields.cs:85`).

### 3.3 Fondations (`plans/F-fondations.md`)

**APP-22 — Contraste des couples d'état.** (d, vérifié) → F12
- Les valeurs sont au § 0.
- Synaxis a foncé l'ambre en #8A6203, qui donne 4,84:1.
- Ses bandeaux font 3,94 et 3,99:1, et sa puce Info 3,95:1.
- Source : Synaxis `Theme/StatePalette.cs:10-16`, nuancier `~/RiderProjects/Synaxis/maquettes-ui-desktop.html`.

**APP-23 — La palette injectée n'atteint pas la grille.** (d, vérifié en partie) → F13
- **Vérifié.** Aucune clé `DataGrid*` n'est publiée, et les teintes Yaru sont figées : sélection orange #FBDDD2.
- **Non vérifiés :**
  - la zébrure #FAFAF9 sur #FFFFFF est quasi invisible (`YaruDataGrid.axaml:15, 27`) ;
  - les cellules à gabarit construites par l'hôte ne sont pas reconstruites quand on réassigne la même liste.
- **Contournement.** Synaxis réécrit 16 clés `DataGrid*` à chaque variante.
- **Source :** Synaxis `Theme/DesktopTheme.cs:63-146`, `Modules/Meeting/SubjectListView.cs:355-358` ; board `Views/CompteursActiviteView.cs:203-210`.

**APP-24 — `Crisp` interne.** (d, vérifié) → F12
- Le board recopie la fonction, avec ce commentaire : « HDevControlBase.Crisp fait la même chose, mais il est internal ».
- C'était déjà un des motifs de la promotion du roster.
- Source : board `Views/GrilleMensuelleControl.cs:742-754`.

**APP-25 — Amorçage en un appel.** (c) → F13
- **Ce que chaque application recopie :**
  - `OverlayPopups=true` ;
  - la police Inter ;
  - le thème Fluent, puisque les éditeurs embarquent un TextBox Avalonia (`HDevTextBox.cs:113`, `HDevEditorBase.cs:187`) ;
  - les styles de la grille ;
  - les paquets Desktop, Fluent et Inter ;
  - l'accent, avant 4fc218e.
- **Suite.** Seule la Démo fait la même chose.
- **Source :** Synaxis `Program.cs:19-25`, `App.axaml:8-14` ; board `Program.cs:28-45`, `App.axaml:15-32`, `myFlightOpsBoard.App.csproj:41-54`, `docs/SETUP.md:144-159`.

**APP-26 — Pinceaux liés aux jetons depuis le code.** (c) → F13. Taille M.
- **Synaxis.**
  - 63 Border et 115 TextBlock construits en C# ont des pinceaux figés.
  - Dix vues s'abonnent à `VariantChanged` et reconstruisent tout, en restaurant le défilement. La sélection de la grille est perdue.
  - Fichiers : `Modules/Users/UserListView.cs:147-158`, `Modules/Meeting/MeetingListView.cs:185-196`, `SubjectListView.cs:189-198`, `MeetingDetailView.cs:90-103, 155-181`, `SubjectDetailView.cs:73-83`, `Modules/Cpa/CpaDetailView.cs:57-70`, `CpaListView.cs:154`, `Modules/DailyBoard/DailyBoardView.cs:103-109`, `DelegationsView.cs:46-50`, `WeeklyReviewView.cs:245-249`.
- **Le board.**
  - 34 couleurs sont figées à la construction, dans DataView, CrewListView, FtlAuditView, HealthView, ConnectionWindow, BoardGrid et d'autres.
  - `BoardTheme.Repaint` repeint à la main.
- **Suite.** `Track` n'invalide que la mesure et le rendu, et `ApplyTo` ne sert que le XAML.

**APP-27 — Tons d'état et style de valeur.** (c) → F13. Taille S.
- **Ce que c'est :**
  - des paires (clair, sombre) pour Ok, Risque, Retard, Neutre et Info ;
  - un style de valeur qui regroupe libellé, abréviation, deux couleurs, icône et emphase, et dont la couleur suit la variante ;
  - des dictionnaires « valeur → style » qui alimentent puces, listes et légendes.
- **Suite :**
  - jetons `Text.*` et `Background.*` ;
  - variantes de badge figées ;
  - aucun accès à la palette d'une variante non active : seul `HDevTheme.Current` existe (l. 282).
- **Source :** Synaxis `Theme/StatePalette.cs` (34 l.), `Modules/Cpa/CpaPresentation.cs:12-44`.

**APP-28 — Police à chasse fixe.** (c) → F13. Taille S. Le choix de la police relève de Q9 (licence).
- **Suite.** `HDevTheme.Typography` (l. 512-534) n'a aucun jeton de chasse fixe.
- **Le board** :
  - embarque IBM Plex Sans et Mono, sous licence OFL (`Assets/Fonts`, `myFlightOpsBoard.App.csproj:74-81`) ;
  - emploie aussi la police `monospace` du système (`Views/DetailJourneeWindow.cs:83`).
- **Synaxis** met en chasse fixe les références et la colonne horaire.

**APP-29 — Carte de détail peinte.** (c) → F14. Taille M.
- **Ce qu'elle fait :**
  - elle suit le survol, ou à défaut l'élément choisi au clavier ;
  - elle se place sous la ligne, calée sur le début de l'élément et rentrée dans le cadre ; si elle ne tient pas, elle passe au-dessus.
- **Son contenu :**
  - un coin « SURVOL » ou « SÉLECTION » ;
  - des lignes libellé/valeur et les étapes, groupées par deux ;
  - les alertes, repliées à la largeur ;
  - un pied qui dit ce que fera Entrée.
- **Son rendu :**
  - une ombre ;
  - une reconstruction seulement quand l'élément change ;
  - une troncature propre : « … suite au double-clic ».
- **Suite.** Seulement des infobulles texte (`HDevRoster.cs:1080`, `HDevGantt.cs:814`, `HDevScheduler.cs:292`).
- **Source :** board `Views/RosterJourControl.cs:681-775`, `RotationJourControl.cs:719-912`, `VueMensuelleControl.cs:887-1006` (nc).

**APP-30 — Clavier en deux dimensions.** (c) → F14, sous la forme d'une aide de sélection réutilisable. Taille M.
- **Déplacements :**
  - ↑ et ↓ changent de ligne à heure constante (l'élément le plus proche de l'instant quitté) ;
  - ← et → passent d'un élément à l'autre ;
  - Début et Fin.
- **Actions :** Entrée active (comme un double-clic) ; Échap désélectionne.
- **Période :** PgPréc et PgSuiv en changent ; T revient à aujourd'hui.
- **Affichage :**
  - le défilement est minimal et remonte l'intertitre du groupe ;
  - un cadre de focus apparaît quand rien n'est sélectionné.
- **L'hôte** intercepte Échap pour fermer d'abord un panneau, puis rend le focus au contrôle.
- **Suite :**
  - HDevRoster : rien ;
  - HDevScheduler : ←, → et Début (`HDevScheduler.cs:441-464`) ;
  - HDevGantt : l. 1269.
- **Source :** board `Views/RosterJourControl.cs:276-420`, `RotationJourControl.cs:289-432`, `VueMensuelleControl.cs:365-482`, `GrilleMensuelleControl.cs:313-415` ; hôtes `RosterView.cs:247-256`, `VueMensuelleView.cs:310-325`.

**APP-31 — Légende peinte par la routine de l'élément.** (c) → F14. Taille S.
- Chaque pastille de légende appelle la fonction qui peint l'élément qu'elle explique : la légende et le rendu ne peuvent pas diverger.
- La légende suit le vocabulaire de la grille.
- Source : board `Views/RosterPalette.cs:236-252`, `RotationPalette.cs:181-227`, `MensuelPalette.cs:187-229`, `NotamPalette.cs:195-229`, `GrilleMensuelleView.cs:174-199`.

**APP-32 — Motifs de rendu.** (c) → F14. Taille S.
- Des hachures à 135°, calées sur l'origine pour que deux plages voisines se raccordent.
- Des cadres pointillés et des losanges.
- Des pastilles cerclées de blanc, portant une lettre.
- Un libellé barré, et un souligné d'alerte de 3 px.
- Source : board `Views/NotamPalette.cs:77-192`, `RosterPalette.cs:172-186`, `MensuelPalette.cs:150-164`, `RotationPalette.cs:54-100, 162-178`.

**APP-33 — Outils de texte dessiné.** (c) → F14. Taille S.
- **Ce que fait le board :**
  - la coupe d'un libellé garde ce qui suit « → » (`RosterPalette.Libelle`, l. 195-222) ;
  - l'espacement des lettres (`VueMensuelleControl.Espacer`, l. 1026-1048) ;
  - des espaces insécables partout, sauf aux séparateurs (`NotamTousView.Insecable`, l. 1375-1377) ;
  - la conversion des points en pixels (`GrillePalette.Pixels`).
- **Suite.** HDevRoster fait disparaître un libellé trop long (`HDevRoster.cs:625-642`).

**APP-34 — Défilement qui amène un élément en haut.** (c) → F14. Taille S.
- `BringIntoView` d'Avalonia ne fait qu'effleurer la cible. Le board calcule le décalage lui-même.
- Source : board `Views/NotamTousView.cs:410-420` (nc).

**APP-35 — Démo pilotable par des liens de démarrage.** (c) → F15. Taille S.
- **Ce que fait Synaxis**, en DEBUG :
  - il lit `SYNAXIS_AUTOLOGIN`, `SYNAXIS_OPEN` (liens profonds, par exemple `meetings/<guid>/print/register`), `SYNAXIS_THEME=dark` et `SYNAXIS_TAB` ;
  - il atteint ainsi un état précis sans injecter d'entrées.
- **Méthode** (mémoire `env_desktop_visual_verification.md`) : une fausse API en Python, le binaire lancé directement, une capture par `import -window`.
- **Pour la suite :**
  - la Démo ouvre une page, un thème ou un état (popup ouvert, sélection) ;
  - elle peut enchaîner un scénario minuté, comme une désactivation brève de la fenêtre ;
  - c'est la piste pour la vérification sur le bureau réel (M-03).
- **Source :** Synaxis `Views/MainWindow.cs:100-300`, `LoginWindow.cs:86-100`, `Theme/DesktopTheme.cs:40-45`, `Modules/Cpa/CpaDashboardView.cs:93-101`.

**APP-36 — États posables par code ; kit de planches réutilisable.** (c) → F15. Taille S.
- **Le constat.** Le board pose ses états de vérification par des bascules de code temporaires, marquées `// BASCULE-*` et retirées ensuite (`docs/PLAN-ROTATION.md:356-361`, `docs/PLAN-NOTAM-PROGRAMME.md:215`).
- **Les besoins :**
  - poser par code une sélection ou un survol sur un contrôle dessiné ;
  - ouvrir le kit de planches (`tests/Gallery/Kit.cs`, avec son crochet `AfterShow`) aux projets qui consomment la suite.

**APP-37 — Rendu PNG d'une vue depuis l'application.** (g) → F15
- **La méthode :**
  - `RenderTargetBitmap` après `UpdateLayout`, posté en priorité `Background` ;
  - une minuterie enchaîne les états, et chaque PNG sert de marqueur.
- **Statut.** Méthode documentée dans la mémoire du board (`gui-avalonia-clavier-seulement.md`), mais absente de son code : le dépôt ne contient aucun `RenderTargetBitmap`.

**APP-38 — Formatage relatif des dates et des durées.** (b) → F3. Taille S.
- Synaxis : « J+3 », « aujourd'hui », « 08:30–10:00 », « 1 h 30 », avec les pluriels (`Modules/Cpa/CpaPresentation.cs:200-206`, `Modules/Meeting/MeetingPresentation.cs:96-142`, `Modules/DailyBoard/DailyBoardPresentation.cs:114-129`).
- Board :
  - un âge en « 3 j », « 2 h 05 » ou « 12 min » (`Views/NotamBandeau.cs:131`) ;
  - des durées au-delà de 24 h (`Views/BoardGrid.cs:117-120`).

**APP-39 — Désactivation motivée.** (b) → F5. Taille S.
- **Le besoin.** Désactiver un contrôle en affichant pourquoi.
- **Le piège.** Avalonia n'affiche pas l'infobulle d'un contrôle désactivé sans `ToolTip.ShowOnDisabled`. Synaxis double donc la raison d'une légende visible. Le board met le motif en infobulle.
- **Proposition.** Une propriété de raison sur `HDevControlBase`, qui pose l'infobulle et la description accessible.
- **Source :** Synaxis `Modules/Meeting/Dialogs/RegisterFormControls.cs:283-292`, `LogEntryEditDialog.cs:143-149`, `RegisterActionEditDialog.cs:128-141`, `Modules/Meeting/SubjectFreeCards.cs:517-527`, `SubjectRegisterCards.cs:947-958` ; board `Views/NotamTousView.cs:1379-1433`.

**APP-40 — Palette appliquée à une partie de l'écran ; accent par instance.** (b) → F8. Taille L.
- **Ce que fait le board.**
  - Les vues tirées des maquettes peignent une zone « papier » claire, indépendante de HDevTheme.
  - Cette zone reste claire en mode sombre, cernée d'un liseré de thème.
  - Conséquence : ces vues emploient les contrôles d'Avalonia.
- **Accent par instance.**
  - L'accent de HDevCheckBox ne se règle pas par instance (`HDevCheckBox.cs:134-136`). Le board contourne avec une case Fluent et six ressources surchargées.
  - Synaxis demande une couleur par instance pour le ruban et l'arbre, et un bouton à l'accent secondaire.
- **Source :** board `Views/RosterPalette.cs:24-99`, `GrillePalette.cs:11-16`, `GrilleMensuelleView.cs:66-73` ; mémoire Synaxis `feature_desktop_light_theme.md`.

**APP-41 — Nuancier validé avant le code ; maquette contre écran.** (g) → F8, pour le concepteur de palettes.
- Synaxis a validé le contraste et le daltonisme sur un nuancier HTML avant d'écrire son thème (`~/RiderProjects/Synaxis/maquettes-ui-desktop.html`).
- La maquette, servie par `python3 -m http.server` et capturée par Edge en headless, se compare à l'écran.

**APP-42 — ToggleButtonGroup.** (d) → F0, puis corrigé en F1.
- **Constat :**
  - un changement de sélection fait par le code déclenche l'événement, sans dire d'où il vient ;
  - il n'y a pas de sélection par valeur (`HDevToggleButtonGroup.cs:88-100, 140-155`).
- **Contournement :** un drapeau de suspension, et une recherche par `Tag`.
- **Non confirmé :** le board affirme qu'une sélection posée avant l'ajout serait perdue. En réalité, seule l'exclusivité n'est pas réappliquée.
- **Source :** board `Views/CompteursActiviteView.cs:116-125, 212-217, 840-863`, `DataView.cs:83-86, 243-260, 388-396`.

**APP-43 — NavBar.** (d) → F0, puis corrigé en F1.
- Constat : `SelectedIndex` est borné et ne vaut jamais −1 ; recliquer la même entrée ne déclenche aucun événement (`HDevNavBar.cs:60-71`).
- Contournement : écouter le clic, même déjà traité, et réagir en différé.
- Source : board `Shell/MainWindow.cs:296-305`.

### 3.4 Grille (`plans/G-grille.md`, exécuté dans HDev.UI.DataGrid)

**APP-44 — Mise à jour sur place.** (b, avec un constat vérifié) → G2. Taille M.
- **Ce que fait le board :**
  - les collections sont créées une fois, puis réutilisées ;
  - une ligne modifiée est remplacée à sa position ;
  - la vue filtrée n'est reconstruite que si l'appartenance change ;
  - le fil est limité à 500 lignes.
- **Constat vérifié :**
  - la source (`GridDataSource.cs:38-40, 429-432`) et la grille (`HDevDataGrid.cs:778-780, 794-797`) s'abonnent toutes deux au changement ;
  - chaque modification déclenche donc deux recalculs complets (tri, filtre, groupes) ; vider puis remplir N lignes en coûte 2(N+1) ;
  - des objets neufs perdent la sélection ;
  - il n'y a ni mise à jour par lot, ni clé de ligne.
- **Synaxis :** réassigner la liste vide la sélection (`HDevDataGrid.cs:790`) et remet le défilement à zéro.
- **Pour G2 :** un seul abonnement, la mise à jour par lot, une clé de ligne, la sélection et le défilement restaurés.
- **Source :** board `Presenters/BoardPresenter.cs:104-112`, `SnapshotPresenter.cs`.

**APP-45 — Colonne de durée.** (b) → G2. Taille S.
- **Ce que c'est :**
  - des grilles en lecture seule, configurées une fois ;
  - des colonnes typées : texte, date et heure, date, nombre, durée ;
  - une durée triée sur sa valeur et affichée « 156 h 24 ».
- **Constats :**
  - le tri se fait sur la valeur brute, sans comparateur ni champ de tri (`GridDataSource.cs:314-350`) ;
  - le format `hh` plafonne à 23 h ;
  - le gabarit reçoit la ligne entière, et non la valeur (`HDevDataGridCell.cs:276-298`) ;
  - une seconde colonne de durée lève `NotSupportedException`.
- **Source :** board `Views/BoardGrid.cs:22-143`, `CompteursActiviteView.cs:1076-1087`.

**APP-46 — Texte d'absence ; pied qui explique un vide.** (b) → G2. Taille S.
- **Ce que fait le board :**
  - une colonne que la source ne sait pas remplir est masquée, et un bandeau dit ce qui manque ;
  - une valeur inconnue s'affiche vide ou « — », jamais « 0 » ;
  - un écart de comptage est nommé, au lieu d'être donné brut.
- **Synaxis :** le pied de liste explique la cause d'un vide, la période ou les filtres.
- **Suite :** `FormatValue` rend une chaîne vide pour null (`GridColumn.cs:371`), et `EmptyContent` est statique.
- **Source :** board `Views/DayBoardView.cs:13-91`, `Presenters/Rows/CompteurRow.cs:8-17`, `FleetRow.cs:28-32`, `SnapshotRow.cs:44-74` ; Synaxis `Modules/Meeting/MeetingListView.cs:317-325`.

**APP-47 — Jeux de colonnes nommés.** (b) → G2 (sélecteur de colonnes, layout). Taille S.
- **Ce que c'est :**
  - les colonnes sont construites une fois, et seule leur visibilité change selon le jeu choisi ;
  - une colonne peut appartenir à plusieurs jeux ;
  - les colonnes d'identité restent figées ;
  - les totaux se recalculent sur la vue filtrée, et la densité est réglée.
- **Constat lié :** APP-51, `IsVisible`.
- **Source :** board `Core/Compteurs/CompteursColonnes.cs` (345 l.), `Views/CompteursActiviteView.cs:195-217, 950-1067`.

**APP-48 — Facettes recalculées.** (b) → G2. Taille M.
- Les facettes se recalculent depuis les données, en gardant les coches par valeur.
- La barre affiche « n sur N », avec une puce de réinitialisation.
- Le filtre se rejoue sans relire la base.
- Source : board `Views/CompteursActiviteView.cs:572-944`.

**APP-49 — Recherche insensible aux accents.** (b) → G2. Taille S.
- Une recherche par mots, qui ignore les majuscules et les accents.
- Suite : les filtres ignorent la casse, pas les accents (`GridFilter.cs:86-131`).
- Source : board `Core/Board/NotamTous.cs:1077-1092` (nc).

**APP-50 — Clic droit.** (d) → G0, puis corrigé en G2.
- **Constat :**
  - la ligne ne réagit qu'au bouton gauche (`HDevDataGridRow.cs:228`) ;
  - rien ne désigne la ligne visée par le menu contextuel.
- **Contournement :** un gestionnaire en phase Tunnel retrouve la ligne, et le menu est reconstruit à l'ouverture.
- **Source :** Synaxis `Modules/Users/UserListView.cs:111-113, 426-436`, et les vues Meeting, Subject et CPA.

**APP-51 — Constats hors édition.** (d) → G0, élargi ; corrections en G2 et G3.
- **`FrozenColumnCount`** est déclaré, mais jamais lu (vérifié ; `HDevDataGrid.cs:135-136, 375-378`). Contournement : `IsFrozen` sur chaque colonne.
- **`IsVisible`** est sans effet à chaud. Contournement : `UpdateVisibleIndices()` puis `RefreshData()`, rejoués (board `CompteursActiviteView.cs:461-465, 969-992`).
- **Formatage sans culture explicite**, avec `ToShortDateString` (`GridColumn.cs:369-379`). Contournement : la culture fr-FR forcée pour tout le processus (board `Program.cs:13-21`, `docs/SETUP.md:155-158`).
- **Colonne désignée par une chaîne.** Un nom inconnu donne une colonne vide, sans erreur (`PropertyAccessor.cs:43-47`). Contournement : une vérification par réflexion au démarrage (`CompteursActiviteView.cs:1092-1120`).
- **Textes en anglais** → F3 : « Drag a column header here… » (`HDevDataGridGroupPanel.cs:27-29`), « No data to display » (`HDevDataGrid.cs:139`).
- **Densité par défaut** de 36/40 (`HDevDataGrid.cs:72-76`) → F8.

**APP-52 — Totaux de durées impossibles.** (d) → G3
- Les résumés passent par une conversion en décimal (`GridSummary.cs:53-63, 111-118`).
- Contournement : le total est affiché hors du pied.
- Source : board `Views/CompteursActiviteView.cs:473, 1045-1064`.

**APP-53 — Lecture tronquée.** (b) → G6. Taille S.
- **Ce que fait le board :**
  - la lecture est découpée en tranches de 14 jours, avec une progression ;
  - il signale une tranche qui atteint le plafond de lignes ;
  - les vues refusent alors d'afficher.
- **Pour la grille :** un état « source incomplète, rien n'est affiché ».
- **Source :** board `Views/LectureParTranches.cs` (138 l.), `VueMensuelleView.cs:441-463`.

**APP-54 — Kanban.** (b) → G7. Taille L.
- **Ce que c'est :**
  - des colonnes qui servent de zones de dépôt ;
  - un glisser qui ne démarre qu'au-delà de 5 px, pour ne pas voler le clic des boutons de la carte ;
  - une limite d'en-cours (3) : le dépôt est refusé, avec un message.
- **La variante en lecture seule :**
  - colonnes étirées, d'au moins 200 px ;
  - défilement horizontal en deçà ;
  - compteur et puce de retard par colonne.
- **Pièges d'Avalonia :**
  - un ScrollViewer mesure en largeur infinie, ce qui casse les colonnes en étoile (`CpaKanbanBoard.cs:42-52`) ;
  - `DoDragDropAsync` exige l'appui d'origine (`DailyBoardView.cs:927`).
- **Source :** Synaxis `Modules/DailyBoard/DailyBoardView.cs:786-980`, `Modules/Cpa/CpaKanbanBoard.cs` (272 l.).

### 3.5 Impression et export (`plans/I-impression-export.md`)

**APP-55 — Fenêtre d'impression à planches.** (b) → I1, dont elle précise l'aperçu. Taille M.
- **À gauche :**
  - le choix du document en cartes radio, avec le nombre réel de pages en sous-titre ;
  - les options propres à chaque document.
- **À droite :**
  - l'aperçu par planches de deux pages A4, rendu à 96 dpi et affiché à 0,5 ;
  - ◀ ▶ pour passer d'une planche à l'autre.
- **En pied :**
  - un résumé : « 13 pages A4 · 7 feuilles en recto verso · 2 exemplaires » ;
  - les copies, le recto verso, « Enregistrer en PDF » et « Imprimer… ».
- **Comportement :**
  - préchargement des documents en parallèle ;
  - cache indexé par document et par options sérialisées ;
  - rendu temporisé (300 ms), et rendus périmés annulés ;
  - la planche courante est gardée quand une option change ;
  - Échap ferme.
- **Manquent :** le zoom, la plage de pages, l'orientation.
- **Source :** Synaxis `Modules/Meeting/Print/PrintMeetingDialog.cs` (1 052 l., nc), `MeetingPrintContracts.cs` (92 l., nc).

**APP-56 — Impression par CUPS.** (b) → I1, et pièce pour Q4. Taille S.
- **Ce que c'est :**
  - les imprimantes se lisent par `lpstat -l -p`, et celle par défaut par `lpstat -d` ;
  - l'impression passe par `lp -d … -n <1-99> -o sides=… -o media=A4 -o collate=true -t <titre>`, sur un fichier temporaire, et rend l'identifiant de la requête ;
  - `LANG=C` rend les sorties analysables ;
  - le délai est de 20 s ;
  - la liste est vide si CUPS est absent ;
  - la dernière imprimante utilisée vient en tête.
- **Manquent :** les capacités de l'imprimante (lpoptions), le suivi du travail, Windows.
- **Le code note :** « Avalonia n'offre aucune boîte d'impression ».
- **Source :** Synaxis `Modules/Meeting/Print/CupsPrinting.cs` (156 l., nc).

**APP-57 — Rendu déterministe.** (b) → I1, comme critère d'acceptation (rendu reproductible). Taille S.
- Un rapport rendu avec l'heure passée en paramètre, et une feuille de style d'impression.
- Source : board `Core/Ftl/FtlReport.cs`.

**APP-58 — Export XLSX fidèle à l'écran.** (b) → I2, et principe pour I3. Taille M.
- **Ce que c'est :**
  - un seul jeu de couleurs et un seul plan de mise en page, pour l'écran et pour le classeur (`Core/Grille/GrilleTokens.cs`, `GrillePlan.cs`) ;
  - des feuilles de tableau avec un titre, les réserves en pied, l'en-tête figé, et un nom d'onglet tronqué à 31 caractères ;
  - une écriture hors du fil d'interface ;
  - un seul choix de fichier produit le rapport et ses annexes ;
  - on exporte ce qui est affiché, jamais un recalcul.
- **Vérifié par l'application :** une parité à l'octet avec l'oracle (`docs/VUE-MENSUELLE.md:13`), et un test qui relit le XML.
- **Source :** board `Reporting/GrilleXlsx.cs` (527 l.), `Reporting/TableauXlsx.cs` (128 l.).

**APP-59 — PDF du HTML riche.** (b) → I4, pour l'export PDF simple du texte riche ; à reprendre en R3.
- Le serveur Synaxis rend le HTML TipTap en PDF, avec HtmlAgilityPack, à côté du parseur storage-v1 de la suite.
- Il y a donc deux vocabulaires à rapprocher.
- Source : `~/RiderProjects/Synaxis/Synaxis.Reporting/Html/HtmlRichRenderer.cs`.

### 3.6 Visualisation (`plans/V-visualisation.md`)

**APP-60 — Tuile d'indicateur.** (c) → V5. Taille S à M.
- **Dans Synaxis :**
  - une valeur en 24 px demi-gras, un libellé en légende, un sous-texte ;
  - un état « chaud » : la valeur prend la teinte d'alerte, et la bordure se teinte à 35 % tant que le compteur n'est pas nul ;
  - des bandes de 4 à 6 tuiles.
- **Dans le board :**
  - un libellé en capitales espacées, une grande valeur colorée, un détail tronqué ;
  - un liseré gauche rouge ou ambre ;
  - un état « filtre posé », signalé par une bordure plus épaisse ; un clic pose ou retire le filtre ;
  - la tuile n'est cliquable que si elle filtre et si la donnée existe ; sinon, elle est estompée.
- **Suite :** HDevCard n'a qu'un titre, une élévation et une bordure.
- **Source :**
  - Synaxis `Modules/Users/UserListView.cs:180-279`, `Modules/Meeting/MeetingListView.cs:226-377`, `SubjectListView.cs:366-406`, `Modules/Cpa/CpaListView.cs`, `CpaDashboardView.cs:177-220`, `Modules/DailyBoard/DelegationsView.cs:180-192` ;
  - board `Views/TuilesKpi.cs` (140 l.).

**APP-61 — Jauges linéaires et progression segmentée.** (c) → V5, comme évolution de HDevProgressBar. Taille M.
- **Le board :**
  - une piste arrondie de 10 px, des segments empilés ;
  - un repère de médiane ou de seuil qui dépasse de la piste ;
  - le gris « absent » ;
  - l'ambre au-delà de 75 %, le rouge au-delà de 90 %.
- **Synaxis :**
  - un segment par étape, coloré une fois l'étape faite, même hors séquence ;
  - le segment de l'étape en retard est cerné de rouge, avec un libellé et une infobulle ;
  - une micro-jauge de 64 × 4 px : « 3/5 clôturées ».
- **Constat :**
  - HDevProgressBar n'a qu'une valeur, quatre variantes, et une piste de 4 ou 6 px ;
  - sa hauteur minimale est de 8 px, et sa teinte ne se règle pas (`HDevProgressBar.cs:9-14, 141-158`) ;
  - le board pose donc une ProgressBar Avalonia de 3 px par-dessus (`VueMensuelleView.cs:216-226`).
- **Source :** board `Views/MensuelPalette.cs:166-184`, `VueMensuelleControl.cs:779-850`, `FicheNavigantPanel.cs:341-404` ; Synaxis `Modules/Cpa/CpaVisuals.cs:239-309`, `CpaDetailView.cs:774-806`, `Modules/DailyBoard/DailyBoardView.cs:1104-1146`.

**APP-62 — Sparkline d'intervalles.** (c) → V5. Taille S.
- Un axe de 05 à 21 h, gradué toutes les deux heures, et une bande de 10 px par service.
- Les plages assurées sont pleines, hachurées hors des horaires, grises si elles n'ont pas été lues.
- Le niveau s'écrit dans la plage si elle fait au moins 11 px, et une infobulle complète.
- Source : board `Views/NotamTousFrise.cs` (118 l., nc).

**APP-63 — Puce d'état teintée et marqueurs d'état.** (c) → V5. Taille S.
- **La puce :**
  - une pilule à fond teinté à 14 % et à bordure à 30 % ;
  - un texte de la teinte pleine, en capitales espacées ;
  - une icône, pour que la couleur ne porte jamais seule le sens.
- **L'emphase :**
  - pleine (fond plein, texte blanc) pour le signal le plus fort ;
  - neutre pour les identités.
- **Les variantes :** une pastille de titre, un code en chasse fixe.
- **Les marqueurs :**
  - un disque plein de 16 px, avec ✓ ou ! ;
  - un anneau à point central ; un anneau vide ;
  - en sombre, le glyphe passe à l'encre foncée.
- **Contraste :** le texte se calcule par `EnsureContrast` sur le fond composé par `Composite` (`HDevTheme.cs:557-607`). La puce Info de Synaxis ne fait que 3,95:1.
- **Suite :** HDevBadge a six variantes figées ; HDevChip est interactive ; HDevAvatar n'a qu'une pastille de présence.
- **Source :** Synaxis `Modules/Cpa/CpaVisuals.cs:20-162`, `Modules/Meeting/SubjectSheet.cs:126-143`.

### 3.7 Mise en page (`plans/M-mise-en-page.md`)

**APP-64 — Jeu minimal de formulaire.** (b) → M1. Critère d'acceptation : ce jeu se compose sans code. Taille S.
- **Le jeu :**
  - un champ avec son libellé au-dessus, et « * » s'il est requis ;
  - deux champs côte à côte ;
  - un pied « Annuler + action principale » ;
  - un bandeau d'erreur masqué tant qu'il est vide ;
  - une page qui défile ;
  - un éditeur riche de hauteur bornée ;
  - une zone de texte en lecture seule, mais copiable.
- **Son usage :** c'est le jeu employé par les 35 dialogues de Synaxis.
- **Source :** Synaxis `Modules/Cpa/Dialogs/CpaFormControls.cs` (221 l.), `Modules/Meeting/Dialogs/RegisterFormControls.cs` (353 l.).

**APP-65 — Panneau en surcouche.** (b) → M2, à ses critères d'acceptation. Taille M.
- **Le panneau :** 880 px, à droite, sur toute la hauteur, avec un filet et une ombre.
- **Le focus :**
  - l'en-tête n'est posé qu'une fois, pour que le bouton de fermeture garde le focus ;
  - Échap ferme le panneau ;
  - le focus reste sur la frise : ↑ et ↓ continuent de fonctionner, et Entrée rouvre le panneau sur le nouvel élément.
- **Source :** board `Views/FicheNavigantPanel.cs` (535 l.), `RotationDetailPanel.cs` (198 l.).

**APP-66 — En-tête de fiche ; ligne d'informations.** (c) → M4. Taille S.
- **L'en-tête :**
  - un titre de 26 px, ou une référence en chasse fixe ;
  - une pastille de statut, puis une ligne d'informations ;
  - les gestes à droite : principal, secondaire, « ⋯ », actualiser ;
  - le titre se tronque avant la pastille, par un ajustement sur `SizeChanged`.
- **La ligne d'informations :**
  - des éléments séparés par « · », qui ne se coupe qu'entre deux éléments ;
  - une amorce, puis la valeur en gras ;
  - une valeur manquante s'affiche « — », en retrait.
- **Piège :** un StackPanel horizontal ne borne pas le titre.
- **Source :** Synaxis `Modules/Meeting/SubjectSheet.cs:80-124, 145-202` (nc), `MeetingDetailView.cs:184-292`, `Modules/Cpa/CpaDetailView.cs:164-283`.

**APP-67 — Cartes.** (c) → M4, comme évolution de HDevCard. Taille S.
- **La carte d'état :**
  - un liseré gauche de 3 px à la couleur d'état, réservé aux états chauds ;
  - une bordure renforcée si la carte est chaude, et qui change au survol ;
  - toute la carte est cliquable : curseur main, infobulle.
- **Les variantes :**
  - une carte de section à libellé en petites capitales, ou à titre avec un geste à droite (« + Action ») ;
  - une variante accentuée (bordure de 2 px à l'accent) pour l'élément courant ;
  - un élément de rail : libellé au-dessus de la valeur.
- **Source :** Synaxis `Modules/Cpa/CpaKanbanBoard.cs:154-271`, `CpaDetailView.cs:844-1029`, `CpaVisuals.cs:184-237`, `Modules/Meeting/SubjectSheet.cs:252-323`.

**APP-68 — Briques de lecture.** (c) → M4. Taille M.
- Un encadré d'alerte à cases (`FicheNavigantPanel.cs:174-219`).
- Une grille de valeurs à filets d'un pixel (l. 221-310).
- Un tableau léger (l. 312-339).
- Une liste libellé/valeur colorée (l. 406-447).
- Un historique (l. 449-480).
- Source : board `Views/FicheNavigantPanel.cs`.

**APP-69 — Fiche à colonne latérale.** (c) → M4. Taille M.
- L'en-tête et le bandeau restent fixes.
- La colonne principale et la colonne latérale (296 ou 420 px) défilent chacune de leur côté.
- Le défilement est restauré après chaque reconstruction.
- Piège : la barre de défilement flottante recouvre le padding (`MeetingDetailView.cs:147-148`).
- Source : Synaxis `Modules/Meeting/SubjectSheet.cs:30-78` (nc), `MeetingDetailView.cs:137-182`.

**APP-70 — Ouverture progressive.** (c) → M4. Taille S.
- La fiche s'ouvre tout de suite, avec ce qu'on a déjà.
- Plusieurs lectures partent en parallèle. Chacune peut échouer seule, et inscrit alors son motif dans sa section.
- Rouvrir la fiche annule les lectures en cours.
- Source : board `Views/FicheNavigantOuverture.cs` (159 l., nc).

### 3.8 Fenêtres et dialogues (`plans/W-fenetres.md`)

**APP-71 — Dialogue à action asynchrone.** (b) → W1. Taille M.
- **Le patron :**
  - `ShowAsync` charge d'abord les référentiels, puis rend un résultat typé ;
  - le bouton d'action est désactivé pendant l'appel ;
  - une erreur de l'API reste dans le bandeau, et la saisie est conservée.
- **Pourquoi le dialogue appelle l'API lui-même** (`LogEntryEditDialog.cs:20-48`) : sinon, le refus arrive fenêtre fermée et la saisie est perdue.
- **Constats dans HDevWindow :**
  - Échap n'est géré qu'une fois (`PrintMeetingDialog.cs:187-194`) ;
  - la taille n'épouse pas le contenu : largeur et hauteur sont fixes ;
  - il n'y a pas de résultat.
- **Source :** Synaxis, 35 dialogues dans `Modules/*/Dialogs/`.

**APP-72 — Fenêtre de détail qui se complète sur place.** (b) → W1. Taille S.
- Une fenêtre modale ; Échap la ferme.
- Des sections en texte sélectionnable, en chasse fixe.
- « Calcul en cours… » est remplacé sur place à chaque calcul abouti, et les calculs sont mis en cache pour le mois.
- Source : board `Views/DetailJourneeWindow.cs` (99 l.), `VueMensuelleView.cs:718-774`.

**APP-73 — Toast depuis n'importe quel contrôle.** (b) → W1. Taille S.
- On retrouve la fenêtre du contrôle ; s'il n'est pas attaché, on ne fait rien.
- Suite : `HDevToast.Show(TopLevel, …)` (`HDevToast.cs:24`).
- Source : Synaxis `Theme/ToastExtensions.cs` (24 l.).

**APP-74 — Fenêtre maximisée avant l'affichage.** (d) → W2, dont l'acceptation reçoit le dock.
- Le panneau de bord du dock disparaît.
- W2 couvre déjà la restauration.
- Source : mémoire Synaxis `project_ui_desktop_procontrols.md`.

**APP-75 — Fenêtre d'échec au démarrage.** (b) → W3. Taille S.
- Elle affiche l'erreur et une aide ; la fermer quitte l'application.
- Source : board `Shell/StartupFailureWindow.cs` (59 l.).

**APP-76 — Console de tâche longue.** (b) → W3. Taille M.
- Trois intentions distinctes : télécharger le périmètre choisi, tout recopier, reprendre ce qui manque. S'y ajoutent Annuler et Tester.
- Une barre de progression, et une grille mise à jour ligne par ligne.
- Un bilan qui nomme les dix premiers échecs, et un filtre « échecs seulement ».
- La durée annoncée avant de lancer.
- Source : board `Views/DataView.cs` (520 l.).

**APP-77 — Bandeau dans la page.** (c) → W6. Taille S.
- **Ce que c'est :**
  - un message persistant, posé dans la page ;
  - le fond du ton, une pastille de 8 px, une amorce en gras, puis la phrase ;
  - des variantes information et avertissement ;
  - dans un formulaire, il précède les champs.
- **Exigence.** Les couples texte-fond doivent être garantis : les bandeaux de Synaxis font 3,94 et 3,99:1.
- **Suite :** HDevToast est passager, HDevMessageBox est modale.
- **Source :** Synaxis `Modules/Meeting/Dialogs/RegisterFormControls.cs:296-316`, `Modules/Meeting/SubjectSheet.cs:204-250`, `MeetingDetailView.cs:384-420`, `Modules/Cpa/CpaDetailView.cs:321-350` ; board `Views/BoardGrid.cs:145-159`.

**APP-78 — État d'une source.** (c) → W6. Taille M.
- **Un bandeau à trois états :** à jour ; à rafraîchir, en ambre ; injoignable, en rouge.
- **Ce qu'il dit :** d'où viennent les données, de quand elles datent, et quelle période elles couvrent.
- **En cas d'échec,** il garde la dernière lecture réussie, datée.
- **Le bouton :** il devient plein quand l'état se dégrade, et affiche « Relecture… » pendant la lecture.
- **Le rouge** est réservé au mois partiel.
- **Source :** board `Views/NotamTousView.cs:196-242, 496-528`, `NotamBandeau.cs` (135 l., nc), `GrilleMensuelleView.cs:102-125`.

**APP-79 — Réserves repliables.** (c) → W6. Taille S.
- **Ce que c'est :**
  - un bloc replié par défaut ;
  - un en-tête « ▸ ⚠ titre — n réserves » de 28 px ;
  - un contenu qui défile au-delà de 260 px ;
  - la même liste à l'écran et à l'export.
- **Comparaison :** l'Expander Fluent fait 48 px ; l'en-tête de HDevAccordion est fixé à 38 px (`HDevAccordion.cs:99`).
- **Source :** board `Views/BoardGrid.cs:161-271`, `VueMensuelleView.cs:257-288, 508-513`, `RosterView.cs:551-608`, `RotationView.cs:390-415`.

### 3.9 Commandes (`plans/C-commandes.md`)

**APP-80 — Visibilité dans le ruban.** (d) → C0, puis corrigé en C3.
- **Constat :**
  - `HDevRibbonTab` n'a pas de visibilité ;
  - `HDevRibbonItem.IsVisible` est déclaré (`HDevRibbonElements.cs:132`), mais `HDevRibbon.cs` ne le lit jamais.
- **Contournement :** l'onglet n'est ajouté que pour un administrateur, et la coquille est reconstruite à chaque connexion.
- **Source :** Synaxis `Views/MainWindow.cs:38-42`.

**APP-81 — Un appui sur un contrôle HDev n'est pas marqué comme traité.** (d) → C0, puis corrigé en C2.
- **Constat :** `HDevControlBase.OnPointerPressed` (l. 128) laisse passer l'appui. Un parent qui démarre un glisser vole alors le clic des boutons enfants.
- **Contournement :** un seuil de 5 px, et un test des ancêtres HDevButton.
- **Source :** Synaxis `Modules/DailyBoard/DailyBoardView.cs:923-946`.

**APP-82 — Item de menu.** (b) → C1. Taille S.
- **Texte secondaire.** Synaxis glisse « par défaut » dans l'emplacement du raccourci (`PrintMeetingDialog.cs:966-967` ; voir `HDevContextMenu.cs:107`).
- **En-têtes de section.** Synaxis les émule par des items désactivés.
- **Item coloré.** Voir la mémoire Synaxis.

**APP-83 — Raccourcis propres à une vue.** (b) → C1. Taille M.
- **Portée :** les touches d'une seule lettre ne valent que dans la vue active.
- **Priorité de la vue :** elle capte certaines touches avant les contrôles. Échap ferme le panneau avant de désélectionner.
- **Dans un champ de recherche :** les lettres s'écrivent ; Échap vide le champ, puis en sort ; ↓ rend la main à la liste.
- **Les raccourcis** sont rappelés dans les infobulles.
- **Suite :** `HDevShortcutManager` agit au niveau de la fenêtre, et le contrôle qui a le focus reste prioritaire (`HDevShortcuts.cs:14-60`). Il n'y a ni portée par vue, ni arbitrage avec les champs.
- **Source :** board `Views/NotamProgrammeView.cs:900-953`, `NotamTousView.cs:1212-1307` (nc), `RotationView.cs:214-226`, `VueMensuelleView.cs:310-325`.

**APP-84 — Bouton : lien et état actif.** (b) → C2. Taille S.
- **Ce que fait le board :**
  - un bouton à état actif, un bouton de taille fixe ;
  - un bouton sans fond qui porte un dessin ;
  - un bouton plein ou contour, désactivé avec son motif ;
  - un lien souligné ; une ligne de texte entière cliquable.
- **Ce que demande Synaxis** (mémoire `feature_registre_suivi_sujets.md`, 30/09) : une variante « lien ». La variante Ghost force le texte primaire.
- **Constats :**
  - HDevButton n'a pas d'état basculé, et le Button Fluent repeint son fond au survol ;
  - on ne peut pas mettre de lien dans le fil du texte : un bouton placé dans un texte fait échouer le rendu (`NotamProgrammeView.cs:750-757`, `docs/PLAN-NOTAM-PROGRAMME.md:20-21`).
- **Source :** board `Views/RosterView.cs:683-735`, `VueMensuelleView.cs:849-884`, `NotamTousView.cs:1379-1433`, `NotamProgrammeView.cs:750-779, 851-873`.

### 3.10 Navigation (`plans/N-navigation.md`)

**APP-85 — Frise verticale d'étapes dépliables.** (b) → N3. Taille M.
- **Les marqueurs** (26 px) : ✓ pour une étape faite, son numéro pour une étape à venir, un contour rouge pour une étape en retard.
- **Le trait de liaison** va jusqu'au marqueur suivant ; l'écart entre étapes est placé dans le corps.
- **L'en-tête** est cliquable.
- **Le corps** est masqué ou affiché, jamais reconstruit, si bien que le défilement ne saute pas.
- **Les étapes ouvertes** sont gardées d'un rechargement à l'autre.
- **Source :** Synaxis `Modules/Cpa/CpaDetailView.cs:431-610`.

**APP-86 — Sous-vue avec retour.** (b) → N4. Taille S.
- La liste complète s'ouvre dans l'onglet.
- Échap ou « ← » ramène en arrière.
- « Voir dans le programme » ramène sur la bonne carte.
- Source : board `Views/NotamProgrammeView.cs:298-328`, `NotamTousView.cs:24-37` (nc).

**APP-87 — Liste groupée riche.** (c) → N6. Taille L.
- **Les groupes :**
  - une rangée de puces, une par groupe : compte, badge, touches 1 à 9 ;
  - les groupes ouverts, en cartes à en-tête riche, avec la sparkline d'intervalles ;
  - les groupes fermés, résumés ensemble ;
  - une ouverture par défaut qui dépend du contenu ;
  - les éléments secondaires, repliés sous un filet pointillé.
- **La recherche** ouvre les groupes concernés et affiche « n trouvés ».
- **La ligne choisie** porte un trait d'accent de 3 px.
- **Le panneau de détail :**
  - des champs ;
  - le texte brut, sélectionnable ;
  - un encadré ;
  - les actions désactivées, avec leur motif.
- **Le défilement** amène le groupe ouvert en haut.
- **Suite :** HDevListView a des lignes de hauteur fixe (titre, sous-titre, badge), et des groupes repliables avec leur compte.
- **Source :** board `Views/NotamTousView.cs` (1 455 l., nc).

**APP-88 — Fil chronologique.** (c) → N6. Taille M.
- **La saisie :** un champ d'une ligne et un bouton ; Entrée publie, et le texte est échappé dans un paragraphe HTML.
- **Le fil :**
  - il va du plus récent au plus ancien, avec une colonne de date ;
  - chaque entrée porte la signature de son auteur et un menu « ⋯ » ;
  - les entrées antérieures sont en lecture seule.
- **Source :** Synaxis `Modules/Cpa/CpaDetailView.cs:351-429`, `Modules/Meeting/SubjectFreeCards.cs:261-469`, `SubjectRegisterCards.cs:394-655`, `SubjectSheet.cs:325-367`.

### 3.11 Ancrage (`plans/A-ancrage.md`)

**APP-89 — Proportions du dock selon l'ordre d'ajout.** (d) → A0, puis corrigé en A1.
- **Constat.** Une zone de documents ajoutée après un volet latéral ne reçoit que 0,22 de la largeur : le premier volet prend 78 % (`DockModel.cs:153, 374-399`).
- **Contournement :** ajouter la zone de documents en premier.
- **Source :** Synaxis `Views/MainWindow.cs:436-441`.

**APP-90 — Savoir si un volet est ouvert, et l'activer.** (b) → A3. Taille S.
- **Constat.** L'API publique se limite à `AddPanel`, `ClosePanel`, `SaveLayout`, `LoadLayout` et `PanelClosed` (`HDevDockManager.cs:55-100`).
- **Contournement :** un ensemble d'identifiants tenu à la main, et fermer puis rouvrir le volet, avec rechargement de ses données.
- **Source :** Synaxis `Views/MainWindow.cs:640-645, 689-698` ; commit e8af7be.

### 3.12 Temps (`plans/T-temps.md`)

**APP-91 — Éditeur de récurrence.** (b) → T2. Taille M.
- Une fréquence quotidienne, hebdomadaire ou mensuelle.
- Un champ « jour » qui s'adapte à la fréquence.
- Des cadences préréglées.
- Suite : le modèle `ScheduleRecurrence` existe, mais sans éditeur.
- Source : Synaxis `Modules/DailyBoard/Dialogs/RecurringTaskEditDialog.cs` (162 l.), `SerieEditDialog.cs:31-36`.

**APP-92 — Liste d'agenda du jour.** (b) → T3. Taille M.
- Les événements « toute la journée » d'abord.
- Une colonne horaire en chasse fixe.
- L'entrée en cours en demi-gras, avec un liseré.
- Des gestes propres à chaque entrée.
- Source : Synaxis `Modules/DailyBoard/DailyBoardView.cs:628-716`.

**APP-93 — Navigation par jour ou par semaine.** (b) → T3. Taille S.
- ◀ Aujourd'hui ▶, par jour ou par semaine.
- Un titre long : « Lundi 17 juillet 2026 ».
- La semaine résumée en sept cartes cliquables.
- Suite : seulement la navigation interne à HDevScheduler (`GoToToday`).
- Source : Synaxis `Modules/DailyBoard/DailyBoardView.cs:113-170, 1148-1224`.

**APP-94 — Axe et organisation de la frise.** (c) → T6
- **L'axe :**
  - 24 h fixes, à partir de 05:00, avec « 00 J+1 » à minuit ;
  - ou un axe qui suit le modèle : de 05 à 21 h, élargi si un vol déborde.
- **Les lignes :**
  - les personnes, sous des intertitres de groupe (titre et sous-titre) ;
  - une colonne de gauche figée : code, fonction, pastilles.
- **Le trait « maintenant »** court sur toute la hauteur, avec une pastille horaire.
- **L'échelle** s'ajuste à la largeur jusqu'à 36 px/h, puis la frise défile ; la hauteur est bornée au contenu.
- **Suite, dans HDevRoster :**
  - de minuit à minuit ;
  - une hauteur de voie fixe (`RowHeight = 40`, l. 35) ;
  - un trait « maintenant » limité à une voie ;
  - un axe relatif au début de la voie.
- **Source :** board `Views/RosterJourControl.cs` (783 l.), `RotationJourControl.cs` (923 l.).

**APP-95 — Blocs et marqueurs.** (c) → T6
- **Les blocs :**
  - ils se superposent par couches, selon leur nature : cadre pointillé, bande de service, étapes ;
  - ils sont barrés s'ils sont annulés, en pointillé s'ils n'ont pas de ressource.
- **Les textes :**
  - la route s'écrit sous le bloc et déborde sur le vide qui suit ;
  - la durée entre deux blocs s'écrit entre eux (« 30' »), en gras ambre sous un seuil.
- **Les marqueurs :** des pastilles à lettre au départ et à l'arrivée, et des losanges.
- **La mise en page :**
  - des rangées de débordement, dont seule la première porte l'en-tête ;
  - un cadre de hauteur minimale, pour que la carte tienne.
- **Source :** board `Views/RotationJourControl.cs`, `RotationPalette.cs`, `RosterPalette.cs`.

**APP-96 — Disponibilité et couloirs.** (c) → T6
- **La bande de fond** a trois états : assuré, hors horaires (hachuré), inconnu (gris).
- **Les barres** peuvent couvrir plusieurs intervalles. Chacune prend le premier couloir libre, et la ligne grandit de 18 px par couloir.
- **Les marqueurs ponctuels :** un trait à barre, un losange, un cercle vide pour un annulé. Leurs libellés se répartissent sur deux rangs.
- **L'interaction :**
  - les zones cliquables sont mémorisées pendant le rendu, chacune avec son infobulle ;
  - choisir une clé encadre toutes ses barres.
- **Suite :** `RosterBand` est une seule bande, sans états ; `ScheduleEngine.AssignLanes` ne range que des intervalles simples (`ScheduleModel.cs:181`).
- **Source :** board `Views/NotamProgrammeControl.cs` (253 l.), `Core/Board/NotamDuProgramme.cs`, méthode `Placer`, l. 642 (nc).

**APP-97 — Sélection conservée ; clavier.** (c + d) → T6
- **Constat sur HDevRoster :**
  - réassigner le modèle remet la sélection et le défilement à zéro (`HDevRoster.cs:141-142`) ;
  - pourtant, `RosterLane.Id` est documenté « pour retrouver la sélection » (`RosterModel.cs:110`).
- **Ce que fait le board :** il garde la sélection par clé et par l'instant le plus proche (`RosterJourControl.cs:87-109`).
- **Autres limites de HDevRoster :**
  - aucun clavier ;
  - il ne réagit qu'au clic simple ;
  - son point d'extension, `LaneAdorner`, ne suffit pas (`RosterJourControl.cs:17-21`, `RotationView.cs:27-31`, `docs/PLAN-ROTATION.md:68`).

**APP-98 — Frise et cartes liées par la sélection.** (c) → T6, sur le contrat de sélection de F1
- La frise et une colonne de cartes partagent la même sélection : cliquer une barre choisit sa carte et l'amène à l'écran.
- ↑ et ↓ parcourent les cartes ; Entrée ou Espace déplient le texte intégral.
- On trouve aussi un filtre par tuile, un badge « NOUVEAU » et l'action « marquer comme lu ».
- Source : board `Views/NotamProgrammeView.cs` (954 l., nc).

**APP-99 — Matrice ressources × jours.** (c) → T7. Taille L.
- **La structure :**
  - les personnes en lignes, les jours en colonnes ;
  - quatre zones : le corps, la colonne figée, la bande figée en haut (avec les lignes de comptage), le coin.
- **Les lignes :** des hauteurs différentes (18, 14 et 20 px), des intertitres, des colonnes de totaux.
- **Les cases :** la couleur de leur catégorie, avec un code.
- **Les en-têtes :** teintés pour les vacances et les week-ends. Une ligne « rien lu » est tramée.
- **L'interaction :** la sélection est encadrée ; les flèches, Entrée ou un double-clic déclenchent l'activation.
- **La cohérence avec l'export :** les largeurs sont comptées en caractères, comme dans l'export, et la légende est tirée de la grille.
- **Suite :**
  - la grille ne fige pas de lignes de données et impose une hauteur uniforme (`GrilleMensuelleControl.cs:20-27`) ;
  - la vue mois de l'agenda est un calendrier.
- **Source :** board `Views/GrilleMensuelleControl.cs` (759 l.), `GrilleMensuelleView.cs` (200 l.), `GrillePalette.cs` (71 l.), `Core/Grille/GrillePlan.cs` (227 l.), `GrilleTokens.cs` (270 l.).

**APP-100 — Vue de couverture.** (c) → T7
- **Les lignes de comptage** sont figées. Une case vire au rouge si le compte tombe à zéro.
- **Les cases** sont pleines, cadrées, hachurées ou soulignées de rouge.
- **Le temps :**
  - les jours passés sont estompés (opacité 0,72) ;
  - la colonne du jour est encadrée de rouge sur toute la hauteur.
- **Le bout de ligne** porte trois jauges : des barres empilées, avec un repère de médiane ou de seuil.
- **La carte** est construite par des fonctions de l'hôte.
- **L'interaction :**
  - un double-clic sur un jour d'en-tête, ou la touche R, ouvre ce jour ;
  - PgPréc et PgSuiv changent de mois ;
  - la sélection est gardée tant qu'on reste dans le même mois.
- **Source :** board `Views/VueMensuelleControl.cs` (1 056 l., nc), `VueMensuelleView.cs` (884 l.), `MensuelPalette.cs` (nc), `Core/Grille/VueMensuelle.cs` (nc).

### 3.13 Gantt et texte riche

**APP-101 — Légende des couleurs des barres.** (b) → L21. Taille S.
- Des puces expliquent le sens des couleurs des barres.
- Suite : HDevGantt n'a pas de légende.
- Destination : notée dans `O-gantt.md` ; `UGantt-charte.md` reste inchangée.
- Source : Synaxis `Modules/Cpa/CpaStageTimeline.cs:112-129`.

**APP-102 — HtmlView a un retrait fixe.** (d) → R0, puis corrigé en R1.
- Constat : un retrait interne de 8 px, codé en dur (`HDevHtmlView.cs:168, 287, 392, 398`), sans propriété `Padding`.
- Contournement : une marge négative (−8, −8, −8, −16).
- Source : Synaxis `Modules/Meeting/SubjectSheet.cs:369-379`.

**APP-103 — RichEdit normalise le HTML.** (d) → R0, à documenter.
- Au premier aller-retour, HDevRichEdit réécrit le HTML.
- C'est probablement la normalisation voulue de storage-v1.
- Source : Synaxis `Modules/Meeting/SubjectDetailView.cs:142-146`.

### 3.14 Pièces aux décisions (e) et méthodes du plan maître (g)

**APP-104 — Pour Q4, et pour Q9.**
- Le chemin CUPS (APP-56) fonctionne en usage réel.
- Synaxis produit son PDF et son aperçu avec QuestPDF 2024.12.3, côté serveur (`GeneratePdf`, `GenerateImages`), avec la police Carlito embarquée.
- QuestPDF est sous licence Community, qui n'est ni MIT ni Apache : c'est aussi une pièce pour Q9.
- Ses pièges sont consignés dans la mémoire `feature_impression_reunions.md`.

**APP-105 — Pour Q5, et pour Q9.**
- ClosedXML (MIT) est figé en `[0.105.0]`.
- Il ajoute 0,710625 à la largeur de chaque colonne. L'application compense cette marge, et un test qui relit le XML le vérifie.
- Il entraîne DocumentFormat.OpenXml, SixLabors.Fonts, ExcelNumberFormat, RBush et SkiaSharp, dont les licences restent à vérifier.

**APP-106 — Pour Q6.** Un patron d'interaction indépendant du fournisseur, l'assistant appliqué section par section :
- trois temps : présentation, génération (barre indéterminée, délai de 300 s), résultat ;
- chaque section s'applique à part, et rien n'est enregistré sans ce geste ;
- on peut régénérer.

Source : Synaxis `Modules/Cpa/Dialogs/GeneratePacDialog.cs` (265 l.).

**APP-107 — Pour Q9, la distribution de la suite.**
- **Constat :**
  - les applications référencent la suite par un chemin relatif ;
  - le nom de paquet déclaré ne correspond à aucun paquet publié (`HDev.UI.Controls.csproj:9`) ;
  - la grille est référencée par un chemin relatif codé en dur ;
  - la cible unique `net10.0` a obligé à scinder les frameworks (commits 4d25a03 et 2a89641).
- **Piste :** des paquets versionnés, sur un flux local, qui découpleraient les applications du répertoire de travail.

**APP-108 — Pour le § 4 du plan maître (tests).**
- **Hygiène du pilotage du bureau :**
  - viser la fenêtre par son PID, jamais par son titre : le 30/09, la fenêtre de Julien a été capturée par erreur ;
  - noter `ACT=$(xdotool getactivewindow)` avant tout envoi ;
  - ne jamais lancer `pkill -f` dans une ligne qui contient ailleurs le même chemin ;
  - ne pas tuer l'instance de l'utilisateur : une seconde instance cohabite avec elle.
- **Limites de XWayland, mesurées :**
  - aucun clic synthétique n'atteint la fenêtre ;
  - le clavier ne passe qu'après un clic humain ;
  - `xdotool windowactivate` est sans effet ;
  - les captures restent figées quand la fenêtre est recouverte ;
  - pour `windowsize`, les avis divergent : la fenêtre s'agrandit selon Synaxis, elle laisse une zone noire selon le board.
- **Compilation :**
  - valider dans un worktree voisin des dépôts frères, à cause des références relatives ;
  - poser `set -o pipefail`, sans quoi un échec de compilation laisse tourner l'ancien binaire.

### 3.15 Écartés (f)

**APP-109 — La coquille.** Elle relève de l'application (Q1).
- **Ce qui est écarté :**
  - un rail de navigation synchronisé avec les onglets, et une barre d'état vivante ;
  - le ruban et l'arbre de la coquille, et les routes ;
  - la fenêtre de connexion : tentative silencieuse, mot de passe demandé seulement après un échec, issue « hors ligne » ;
  - la carte d'état de la connexion.
- **Ce qui reste au plan :** ses primitives (dock, ruban, arbre, onglets). L'API qui manque au dock est APP-90.

**APP-110 — Les services de données.** Ce ne sont pas des contrôles.
- Une boucle de rafraîchissement : un cycle à la fois, sans empilement.
- Une source partagée qui garde la dernière lecture valable.
- La lecture par tranches, sauf l'état « tronqué », retenu en APP-53.
- Le fil des changements.
- Des mots de passe mémorisés, mais en clair : ce n'est pas mieux, et une version sérieuse passerait par le trousseau du système.
- La couche d'accès HTTP.

**APP-111 — La composition d'écran de tableau de bord.** Elle relève de l'application, comme les tableaux de bord applicatifs (Q8, tableaux de bord BI).
- Ce qui est écarté : surtitre en capitales, titre en deux graisses, outils, rangée de tuiles, cadre arrondi, légende, « lu à… », réserves.
- Ce qui est retenu : ses briques, en V5, W6 et F14.

**APP-112 — Ce qui n'est pas mieux que la suite.**
- **Thème :**
  - BoardTheme, en retard sur `ApplyTo`, `Track` et `with` ;
  - le bloc d'accent de `App.axaml`, que `ApplyTo` remplace ; en sombre, il laisse l'accent Fluent en teal ;
  - les abonnements manuels à `VariantChanged`, que `Track` remplace.
- **Contrôles :**
  - la compensation de la marge de HDevButton, obsolète depuis 4fc218e : elle espace désormais les boutons de 2 px ;
  - le dispatch de l'arbre par libellé, alors que `HDevTreeNode.Tag` existe ;
  - les puces de personnes du board, qui réimplémentent HDevTokenEdit ;
  - les contrôles existants non employés : ToggleButtonGroup pour Jour/Semaine, ProgressBar pour la journée, TokenEdit pour les mots-clés.
- **Divers :**
  - l'aplatissement du HTML par expressions régulières, moins bon que `HtmlParser…GetText()` ;
  - l'ordre des paramètres de `HDevStatusBar.AddPanel`, une question d'ergonomie à revoir avec C1 ;
  - le drapeau `_fillingFilters`, puisque l'origine des changements arrive en E3.

**APP-113 — Un comportement voulu.** `HDevChip.Closed` ne retire pas la puce. C'est à documenter en F1.

**APP-114 — Le bouton à menu.** HDevButton et HDevContextMenu sont associés à 16 endroits. C'est déjà prévu tel quel en C2.

**APP-115 — Le contenu métier.**
- CPA, réunions, sujets, registre, FTL, NOTAM, rotations, rituels GTD.
- Les constructeurs du Core : seul se réutilise le principe « le Core décide, la vue peint ».
- Les dialogues métier, et les dictionnaires de présentation.
- Les projets Briefing, Apm et Offline.
- Les maquettes de `boards/`, qui restent une référence visuelle.

## 4. Index par lot

| Lot | Apports | Lot | Apports |
|---|---|---|---|
| E2 | rejouer APP-09 à la clôture | I1 | APP-55, 56, 57 |
| E3 | APP-09, 10, 11, 12 | I2 | APP-58 |
| E4 | APP-13, 14 | I4 | APP-59 |
| E6 | APP-15, 16, 17 | V5 | APP-60 à 63 |
| E7 | APP-18, 19, 20 | M1 | APP-64 |
| E8 | APP-21 | M2 | APP-65 |
| F0 | APP-42, 43 ; fige APP-01 | M4 | APP-66 à 70 |
| F1 | corrige APP-42, 43 ; documente APP-113 | W1 | APP-71, 72, 73 |
| F3 | APP-38 ; textes de la grille (APP-51) | W2 | APP-74 |
| F5 | APP-39 | W3 | APP-75, 76 |
| F8 | APP-40, 41 ; densité (APP-51) | W6 | APP-77, 78, 79 |
| F12 | APP-22, 24 ; fige APP-08 | C0 | APP-80, 81 ; fige APP-07 |
| F13 | APP-23, 25, 26, 27, 28 ; fige APP-04 | C1 | APP-82, 83 |
| F14 | APP-29 à 34 | C2 | APP-84 ; corrige APP-81 |
| F15 | APP-35, 36, 37 | C3 | corrige APP-80 |
| G0 | APP-50, 51 ; fige APP-02, 03 | C4 | modèle de carte partagé avec F14 |
| G2 | APP-44 à 49 ; corrige APP-50, 51 | N3, N4, N6 | APP-85 ; APP-86 ; APP-87, 88 |
| G3 | APP-52 ; colonnes figées (APP-51) | A0, A1, A3 | APP-89 ; corrige APP-89 ; APP-90 |
| G6 | APP-53 | T0, T2, T3 | fige APP-05 ; APP-91 ; APP-92, 93 |
| G7 | APP-54 | T6, T7 | APP-94 à 98 ; APP-99, 100 |
| O § 0, O0, O1, L21 | APP-06 ; APP-101 | R0, R1 | APP-102, 103 ; corrige APP-102 |
| Q4, Q5, Q6, Q9 | APP-104 ; 105 ; 106 ; 104, 105, 107, 28 | Plan-Suite § 4 | APP-108 |

## 5. Points signalés, hors périmètre

- **eBurner.App** (`~/RiderProjects/Tools/eBurner`) compile lui aussi contre `RiderProjects/HDev.UI.Controls`. C'est un troisième consommateur de `hdev`, absent de la vérification de compatibilité (Plan-Suite, § 0.8 et § 10). Je propose de l'y ajouter, avec son accord.
- **M-03.** Les liens de démarrage (APP-35) et la capture par PID (APP-108) sont une piste directe pour la vérification sur le bureau réel.
- **Dette documentaire (QX-06).**
  - `context.md` décrit mal Synaxis et oublie myFlightOpsBoard.
  - `UGantt-charte.md`, l. 42, prête à HDevScheduler un usage qu'aucune application n'a.
  - `IMPROVEMENTS.md`, dans le dépôt de la grille, devra reprendre les constats G0 élargis.
