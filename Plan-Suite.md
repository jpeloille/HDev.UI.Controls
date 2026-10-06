# Plan de la suite HDev.UI

Plan complet de la suite, tiré du benchmark HDEV-001/26 (`Benchmark-DevExpress.html`), état au **04/10/2026**, complété le **06/10/2026** par les apports des applications (`Plan-Apports.md`). Statut : **proposition**, à valider.

- **Numérotation.** Les familles (4.1 à 4.13) et les check-lists de profondeur (5.1 à 5.7) suivent la numérotation du HTML, qui est celle du `.md` plus un.
- **Fichiers.**
  - Ce plan maître fixe les principes, les jalons, la projection et les décisions.
  - Chaque chantier a son fichier dans `plans/`.
  - Les éditeurs gardent `Plan-Editeurs.md`, à la racine, parce que le code et les notes le citent.
  - Les apports des applications ont leur plan, `Plan-Apports.md`, à la racine : le registre APP-01 à APP-115 et le détail des lots qui en naissent.
- **Projection.** Chacune des 342 lignes du benchmark (166 fonctions, 176 capacités) a une seule destination : un lot, une absence par décision proposée, ou une décision à prendre. Les tableaux de projection sont calculés à partir de ces destinations ; leurs totaux d'aujourd'hui reproduisent ceux du benchmark.

## 0. Principes

1. **La suite se conçoit pour elle-même** (décision du 04/10/2026). Les applications s'adaptent ; leur usage ne fixe ni priorité ni compatibilité.
2. **DevExpress fixe le niveau, pas la solution technique** (04/10/2026). Ses capacités sont le plancher. Une conception plus simple ou plus pratique l'emporte, et le but est de faire mieux quand on le peut.
3. **Ordre : dépendances d'architecture, puis gain de parité.** C'est l'ordre des « Priorités recommandées » du benchmark (§ 7 du HTML) : socle des éditeurs, impression, grille, graphiques, Layout Control.
4. **Caractériser avant de refondre.** Tout chantier qui reprend un contrôle existant commence par un lot de tests de caractérisation, sur le modèle d'E0.
5. **Deux dépôts.** La grille fait foi dans HDev.UI.DataGrid (`IMPROVEMENTS.md`). Le chantier G s'y exécute.
6. **Ruptures permises**, sans alias, consignées dans une note de migration par chantier (`MIGRATION-<chantier>.md`).
7. **Chaque jalon laisse une suite livrable.**
8. **Les applications ne sont jamais impactées directement** (04/10/2026).
   - Synaxis et myFlightOpsBoard compilent contre les répertoires de la suite (HDev.UI.Controls et, par transitivité, HDev.UI.DataGrid), sur la branche `hdev`.
   - Les lots se développent dans un atelier isolé : une branche de développement et des worktrees placés ailleurs.
   - `hdev` n'avance que par une livraison que Julien décide.
   - Les applications s'adaptent alors, à leur rythme, avec la note de migration.
9. **Le Gantt s'aligne sur MS Project** (décision Q1 du 04/10/2026). La suite porte le programme de la charte UGantt, avec sa cible : MS Project sans le `.mpp`, VBA ni Project Server. La règle de la charte qui ne faisait entrer une brique dans la suite que sur preuve de réemploi ne s'applique plus.
10. **Les applications sont une source, pas une prescription** (06/10/2026).
    - Ce que Synaxis et myFlightOpsBoard ont construit de meilleur que la suite, ou qui lui manque, entre dans le plan par `Plan-Apports.md`, sur son mérite.
    - Les jalons de ces apports suivent les dépendances d'architecture, comme le reste ; l'usage d'une application ne fixe pas leur priorité (principe 1).
    - Seul `Plan-Apports.md` nomme les applications et leurs fichiers. Les chantiers y renvoient par un numéro APP et une description générique.

## 1. Registre des identifiants

| Préfixe | Sens | Fichier qui le possède |
|---|---|---|
| E0–E12, J0–J4, D1–D4 | lots, jalons et décisions des éditeurs | `Plan-Editeurs.md` |
| L0–L25, « jalon 1 » à « jalon 4 », Cat 1 à 3 | programme du Gantt, aligné sur MS Project ; porté par la suite depuis Q1 (chantier O) | `UGantt-charte.md` pour le contenu des lots ; `plans/O-gantt.md` pour leur place dans les jalons |
| P0–P3 | priorités de l'audit de juillet 2026 | `HDev.UI.Controls/context.md` |
| §1–§16, Phase 1 à 4 | feuille de route de la grille | `HDev.UI.DataGrid/IMPROVEMENTS.md` |
| **S1–S6** | jalons de la suite | ce plan |
| **Q1–Q13** | décisions | ce plan |
| **F, G, I, V, M, W, C, N, A, T, O, R**, suivis d'un numéro | lots des chantiers | `plans/<lettre>-*.md` ; le détail de F12 à F15, V5, W6, M4, N6, T6 et T7 est dans `Plan-Apports.md` |
| **APP-01–APP-115** | apports des applications, chacun avec une seule destination | `Plan-Apports.md` |

Un lot d'un autre programme garde son identifiant :
- L9, l'impression paginée de la charte UGantt, devient le lot I1 de ce plan ;
- les autres lots L du programme du Gantt gardent leur identifiant, dans le chantier O ; L25 relève de la décision Q12 ;
- les éditeurs gardent leurs lots E, et leurs jalons J1 à J4 tombent sur S1 à S4.

## 2. Point de départ

- **Catalogue** : 166 fonctions, score **<!-- généré:SC:cat,auj -->28 %<!-- fin -->** (34 ✅, 25 🟡, 97 ❌, 10 ⚪).
- **Profondeur** : 176 capacités sur sept contrôles phares, **<!-- généré:SC:prof,auj -->43 %<!-- fin -->** pour l'ensemble.
- **Plus gros écarts** : visualisation (4 %), grilles (7 %), impression et export (13 %), composants transverses (15 %).
- **Plans déjà écrits**, que ce plan englobe sans les renuméroter :
  - éditeurs : `Plan-Editeurs.md` ; E0 est fait, et E1 aussi, sauf la forme du socle, à valider ;
  - Gantt : `UGantt-charte.md` ; L0 et L1 sont faits, et depuis Q1 la suite porte tout le programme (chantier O) ;
  - grille : `IMPROVEMENTS.md` ; ses phases 1 et 2 sont en partie faites ;
  - apports des applications : `Plan-Apports.md` (06/10/2026) ; 115 apports, dont 8 déjà entrés dans la suite, et 10 lots nouveaux.

Le détail par famille et par contrôle est au § 6.

## 3. Chantiers

| Code | Chantier | Fichier | Tableaux du benchmark | Lots |
|---|---|---|---|---|
| E | Éditeurs | `Plan-Editeurs.md` | 4.2 et 5.7 ; validation et dépôt d'éditeurs de 4.13 | E0–E12 |
| F | Fondations transverses | `plans/F-fondations.md` | 4.13 ; dimensions transverses (§ 3 du HTML) | F0–F15 |
| G | Grille et données | `plans/G-grille.md` | 4.1, 5.1 | G0–G9 |
| I | Impression et export | `plans/I-impression-export.md` | 4.12 ; lignes d'impression et d'export de 5.1, 5.4, 5.5 et 5.6 | I1–I4 |
| V | Visualisation | `plans/V-visualisation.md` | 4.10 | V0–V5 |
| M | Mise en page | `plans/M-mise-en-page.md` | 4.6 | M1–M4 |
| W | Fenêtres, dialogues, notifications | `plans/W-fenetres.md` | 4.8 | W0–W6 |
| C | Commandes : boutons, ruban, barres, menus | `plans/C-commandes.md` | 4.3, 4.4, 5.2 | C0–C5 |
| N | Navigation | `plans/N-navigation.md` | 4.5 | N0–N6 |
| A | Ancrage (docking) | `plans/A-ancrage.md` | 4.7, 5.3 | A0–A4 |
| T | Temps : agenda et roster | `plans/T-temps.md` | 4.9 (agenda), 5.4 | T0–T7 |
| O | Gantt et gestion de projet, alignés sur MS Project | `plans/O-gantt.md` | 4.9 (Gantt), 5.5 | O0, O1 ; L0–L8, L10–L24 |
| R | Rédaction : texte riche | `plans/R-redaction.md` | 4.11, 5.6 | R0–R5 |

**Dépendances principales entre chantiers**
- **Le socle des éditeurs** (E1 à E3) conditionne le Layout Control (M1), le pont grille (E11), les éditeurs dans les barres (C4) et le formulaire de l'agenda (T3).
- **Le contrat des collections** (F1) conditionne le modèle de commandes (C1), l'arbre (N1) et la droite-à-gauche (F9).
- **Le moteur d'impression** (I1) conditionne toute adoption (I3, I4, R3) et les graphiques imprimables (V1).
- **La grille professionnelle** (G2) conditionne la persistance globale (F6) et toute la profondeur de la grille (G3 à G9).
- **Le modèle de ressources** (L10) sert à la fois le Gantt et l'agenda (T1), si la décision Q13 est acceptée.
- **Le thème vivant** (F13) conditionne les indicateurs (V5), le bandeau dans la page (W6) et la fiche en lecture (M4). Le bandeau conditionne à son tour le dialogue qui héberge une vue (W1).
- **Le socle des contrôles dessinés** (F14) conditionne la frise de ressources v2 (T6) et la matrice ressources × jours (T7).

## 4. Contraintes communes

Elles valent pour tout lot, dans tout chantier. Les fichiers de chantier ne les répètent pas.
- **Popups.**
  - Garde de fermeture maison, avec désactivation débouncée ; jamais le light-dismiss d'Avalonia.
  - `OverlayPopups`, et popup rattaché à l'arbre logique.
  - Le comportement se vérifie aussi sur le bureau réel (GNOME/XWayland).
- **Thème.**
  - Bascule clair/sombre à chaud : tout pinceau construit est reposé au changement de variante.
  - Contraste vérifié par `ThemeContrastTests` : texte à 4,5:1 au moins, indicateurs à 3:1.
- **Rendu.** Les origines de texte dessiné passent par `Crisp.Snap`. Les champs partagent le chrome commun : 28 px, retrait 8, filet de 1 px, halo.
- **Clavier.** Tout ce qui se fait à la souris se fait au clavier, et le focus est visible au clavier (`HDevFocus`).
- **Valeurs et collections.** Les conventions d'E1 (`MIGRATION-editeurs.md`, § 1) et de F1 s'appliquent : valeur liable dans les deux sens, `ValueChanged` commun, nommage Avalonia.
- **Tests.**
  - Caractérisation avant toute refonte.
  - Vérifications d'interaction headless.
  - Moteurs purs en xUnit.
  - Planches de `tests/Gallery`, en clair et en sombre.
  - Sur le bureau réel, aucune entrée simulée n'atteint la fenêtre sous XWayland. On atteint l'état à vérifier par la démo pilotable (F15), on vise la fenêtre par son PID et jamais par son titre, on note la fenêtre active avant tout envoi, et on ne tue jamais l'instance de l'utilisateur (APP-108).
- **Fin de lot.**
  - Note de migration à jour.
  - États de `Benchmark-DevExpress.md` à jour, puis HTML et Word régénérés.
  - Projection de ce plan revue.
- **Deux dépôts.** Ce qui touche la grille entre d'abord dans HDev.UI.DataGrid, avec ses tests.
- **Isolation des applications.**
  - Aucun lot ne modifie la bibliothèque dans les répertoires que les applications référencent, ni sur leur branche `hdev`.
  - Aucun lot ne touche au code des applications : on les lit, pour mesurer un impact, et on ne les modifie jamais.

## 5. Jalons de la suite

| Jalon | Thème | Ce qu'il apporte | Catalogue | Profondeur |
|---|---|---|---:|---:|
| S1 | Socle | contrat de valeur et socle des éditeurs ; collections et modèles liables ; moteur d'impression et PDF ; édition fiable dans la grille ; correctifs du socle (contraste, `Crisp`) ; outillage de vérification | <!-- généré:SC:cat,S1 -->29 %<!-- fin --> | <!-- généré:SC:prof,S1 -->45 %<!-- fin --> |
| S2 | Données | grille professionnelle ; éditeurs typés ; exports XLSX et PDF ; graphiques ; localisation ; icônes ; Gantt : durée, travail et calendriers ; amorçage et thème vivant ; indicateurs | <!-- généré:SC:cat,S2 -->39 %<!-- fin --> | <!-- généré:SC:prof,S2 -->51 %<!-- fin --> |
| S3 | Formulaires | Layout Control ; dialogues ; validation centralisée ; éditeurs dans la grille ; accessibilité ; TreeList et inspecteur de propriétés ; Gantt : contraintes, tâches fractionnées et récurrentes, baselines ; bandeau dans la page ; fiche en lecture | <!-- généré:SC:cat,S3 -->46 %<!-- fin --> | <!-- généré:SC:prof,S3 -->60 %<!-- fin --> |
| S4 | Application | commandes et ruban complet ; docking phases 2 et 3 ; navigation ; fenêtres et notifications ; compléments d'éditeurs ; socle des contrôles dessinés ; listes riches | <!-- généré:SC:cat,S4 -->67 %<!-- fin --> | <!-- généré:SC:prof,S4 -->76 %<!-- fin --> |
| S5 | Profondeur | agenda à ressources ; modèle de ressources partagé, mise en forme du Gantt et PERT ; texte riche complet ; visualisations avancées ; vues et volumes de grille ; thèmes et droite-à-gauche ; frise de ressources v2 ; matrice ressources × jours | <!-- généré:SC:cat,S5 -->82 %<!-- fin --> | <!-- généré:SC:prof,S5 -->92 %<!-- fin --> |
| S6 | Gestion de projet | affectations, répartition échelonnée, nivellement, coûts, suivi, valeur acquise ; champs, vues d'usage, filtres et vues composables ; vues calendrier et chronologie ; interop MS Project XML | <!-- généré:SC:cat,S6 -->83 %<!-- fin --> | <!-- généré:SC:prof,S6 -->94 %<!-- fin --> |

Composition des jalons. À l'intérieur d'un jalon, l'ordre suit les dépendances des tableaux de lots. S4 et S5 sont les plus chargés. En S4, les chantiers C, N, A et W ne dépendent pas les uns des autres et peuvent avancer en parallèle ; seul R1 attend le ruban (C3). S6 regroupe les jalons 3 et 4 de la charte UGantt, le pilier ressources et les méta-systèmes du Gantt.

<!-- généré:JALONS -->
**S1**
- E : E0 (Tests de caractérisation des éditeurs) ; E1 (Conception du socle, contrat de valeur et conventions) ; E2 (Socle composite : zone de valeur, boutons, popup, multiligne) ; E3 (Portage des éditeurs existants sur le socle)
- F : F0 (Caractérisation des contrôles à collections) ; F1 (Contrat des collections : ItemsSource, ItemTemplate, sélection liable) ; F2 (Modèles et documents liables) ; F12 (Correctifs du socle : contraste des couples d'état, Crisp public) ; F15 (Outillage de vérification : démo pilotable, états posables par code, planches réutilisables)
- G : G0 (Caractérisation de l'édition dans la grille) ; G1 (Cycle d'édition fiable)
- I : I1 (Moteur d'impression paginée, aperçu et PDF ; L9 de la charte UGantt)
- O : L0 (Versionnage du schéma JSON) ; L1 (Édition structurelle de la table)

**S2**
- E : E4 (Sélection : cases, items à gabarit, liste à cases) ; E5 (Saisie typée : masques typés, formats numériques, calculatrice) ; E6 (Date et heure : types modernes, durées, plages, créneaux) ; E7 (Lookups : saisie et filtre, recherche, multi-valeurs)
- F : F3 (Localisation : mécanisme de ressources, français et anglais) ; F4 (Banque d'icônes et API d'icône commune) ; F13 (Amorçage et thème vivant : un seul appel, palette jusqu'à la grille, jetons liés depuis le code, tons d'état, chasse fixe)
- G : G2 (Grille professionnelle : recherche, filtres, validation, ajout, layout)
- I : I2 (Formats d'export : XLSX, PDF, CSV paramétrable) ; I3 (Adoption : grille et Gantt)
- V : V0 (Prototype : graphiques sous HDevTheme) ; V1 (Graphiques, jauges, sparklines, heatmap) ; V5 (Indicateurs : tuile, jauge linéaire, progression segmentée, sparkline d'intervalles, puce d'état)
- O : O0 (Caractérisation du Gantt avant la refonte du modèle) ; O1 (Extraction du moteur dans un assembly pur (si Q11)) ; L2 (Modèle de colonnes configurable) ; L3 (Durée, travail et types de tâches ; schéma v2) ; L4 (Calendriers multiples et granularité horaire)

**S3**
- E : E8 (Validation centralisée) ; E11 (Pont grille : éditeurs HDev dans HDevDataGrid, sur deux dépôts) ; E12 (Accessibilité des éditeurs)
- F : F5 (Accessibilité de toute la suite) ; F6 (Persistance de l'espace de travail)
- G : G3 (Profondeur : filtres avancés, plages, presse-papiers, mise en forme) ; G4 (Arbre et propriétés : TreeList, grille verticale, inspecteur)
- M : M1 (Layout Control : mise en page de formulaire) ; M2 (Panneau latéral et panneau volant) ; M4 (Fiche en lecture : en-tête, ligne d'informations, cartes, sections, ouverture progressive)
- W : W0 (Caractérisation de HDevWindow) ; W1 (Dialogue qui héberge une vue) ; W6 (Bandeau dans la page, état d'une source, réserves repliables)
- O : L5 (Contraintes, échéances et inspecteur de dates) ; L6 (Passe avant et arrière complète, marges, chemins critiques multiples) ; L7 (Tâches fractionnées, récurrentes et inactives) ; L8 (Baselines multiples et plans intermédiaires)

**S4**
- E : E9 (Compléments : mémo, lien, couleur, chemin, curseur de plage) ; E10 (Compléments secondaires : MRU, curseur de zoom, notation, image)
- F : F7 (Comportements attachables et transitions) ; F14 (Socle des contrôles dessinés : carte de détail, clavier en deux dimensions, légende, motifs)
- G : G5 (Finition : formulaire d'édition, glisser, fusion, colonnes calculées)
- I : I4 (Adoption : agenda, texte riche, carte mentale, autres contrôles)
- M : M3 (Formulaire généré depuis les données)
- W : W2 (Fenêtre thémée qualifiée) ; W3 (Démarrage et attente) ; W4 (Couche d'adornement : badges et guides) ; W5 (Bureau : notifications système, zone de notification)
- C : C0 (Caractérisation du ruban, des barres et des menus) ; C1 (Modèle de commandes partagé et construction déclarative) ; C2 (Boutons à menu et partagés, popup de contrôles) ; C3 (Ruban complet) ; C4 (Barres : débordement, éditeurs, infobulles riches, recherche de commandes)
- N : N0 (Caractérisation de la navigation) ; N1 (Arbre complet : liaison, virtualisation, cases, édition) ; N2 (Accordéon à niveaux et menu hamburger) ; N3 (Parcours : fil d'Ariane, étapes, assistant) ; N4 (Pages à transitions ; onglets à débordement et glisser) ; N6 (Listes riches : groupes à en-tête libre, détail lié, fil chronologique)
- A : A0 (Caractérisation du docking) ; A1 (Phase 2 : glisser, indicateurs d'ancrage, onglets réordonnables) ; A2 (Phase 3 : panneaux flottants, auto-masquage, épinglage) ; A3 (Confort : boutons d'en-tête, panneaux fermés, Ctrl+Tab)
- R : R0 (Caractérisation du texte riche) ; R1 (Édition courante : rechercher, styles nommés, listes à niveaux, ruban intégré)

**S5**
- F : F8 (Thèmes : contraste élevé, densité, concepteur de palettes) ; F9 (Droite-à-gauche) ; F10 (Qualification Windows et macOS) ; F11 (Conception visuelle : données d'exemple et prévisualisation)
- G : G6 (Volumes et hiérarchies : sources serveur, pagination, master-detail) ; G7 (Vues alternatives : bandes, fiches, tuiles, Kanban) ; G8 (Tableau croisé dynamique) ; G9 (Lookup arborescent)
- V : V2 (Diagrammes et carte mentale v2) ; V3 (TreeMap, Sunburst, Sankey) ; V4 (Sélection de plage sur un aperçu)
- C : C5 (Personnalisation par l'utilisateur et mini-barre)
- N : N5 (Tuiles et carrousel)
- A : A4 (Tableau de bord à widgets)
- T : T0 (Caractérisation de l'agenda et du roster) ; T1 (Agenda à ressources et vue chronologique) ; T2 (Récurrences complètes) ; T3 (Vues et métadonnées : année, agenda, libellés, formulaire) ; T4 (Fuseaux horaires, rappels, iCalendar) ; T5 (Liaison par mappings : agenda et Gantt) ; T6 (Frise de ressources v2 : journée décalée, groupes, couloirs, disponibilité, carte, clavier) ; T7 (Matrice ressources × jours à zones figées)
- O : L10 (Modèle de ressources partagé et feuille de ressources) ; L21 (Mise en forme des barres et couche d'adornement) ; L22 (Vue réseau PERT)
- R : R2 (Tableaux et images dans le fil du texte) ; R3 (Mise en page, vue page, règle et tabulations) ; R4 (Formats DOCX, ODT et RTF) ; R5 (Révision : commentaires, suivi, protection, orthographe)

**S6**
- O : L11 (Affectations) ; L12 (Moteur de répartition échelonnée et contours de charge) ; L13 (Surutilisation et nivellement) ; L14 (Coûts : tables de taux, coûts fixes, budget) ; L15 (Suivi avancé : réel, restant, date d'état) ; L16 (Valeur acquise (EVM)) ; L17 (Système de champs natifs et personnalisés) ; L18 (Vues d'usage éditables et histogramme de ressources) ; L19 (Filtres, groupes et tris) ; L20 (Tables et vues composables) ; L23 (Vues calendrier, chronologie et planificateur d'équipe) ; L24 (Interop MS Project XML)
<!-- fin -->

## 6. Projection

Le score d'un tableau vaut (✅ + ½ 🟡) / lignes, comme dans le benchmark. Une absence par décision reste comptée comme non couverte. Entre parenthèses : nombre de ✅ / nombre de 🟡.

**Règle des contrôles phares.** La ligne de catalogue d'un contrôle phare passe à ✅ au premier jalon où sa check-list de profondeur atteint 75 %, et pas avant. Six lignes sont concernées :
- grille (4.1, check-list 5.1) : S3 ;
- ruban (4.4, check-list 5.2) : S4 ;
- docking (4.7, check-list 5.3) : S4 ;
- agenda (4.9, check-list 5.4) : S5 ;
- texte riche (4.11, check-list 5.6) : S5 ;
- Gantt (4.9, check-list 5.5) : S5.

Le script de projection vérifie cette règle.

### Catalogue (familles 4.1 à 4.13)

<!-- généré:CATALOGUE -->
| Tableau | Lignes | Aujourd'hui | S1 | S2 | S3 | S4 | S5 | S6 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 4.1 Grilles et données | 14 | 7 % (0/2) | 7 % (0/2) | 7 % (0/2) | 39 % (5/1) | 46 % (6/1) | 93 % (13/0) | 93 % (13/0) |
| 4.2 Éditeurs | 48 | 39 % (17/3) | 43 % (19/3) | 65 % (31/0) | 65 % (31/0) | 85 % (41/0) | 88 % (42/0) | 88 % (42/0) |
| 4.3 Boutons et libellés | 6 | 67 % (4/0) | 67 % (4/0) | 67 % (4/0) | 67 % (4/0) | 100 % (6/0) | 100 % (6/0) | 100 % (6/0) |
| 4.4 Rubans, barres et menus | 17 | 32 % (4/3) | 32 % (4/3) | 32 % (4/3) | 32 % (4/3) | 82 % (14/0) | 88 % (15/0) | 88 % (15/0) |
| 4.5 Navigation | 14 | 25 % (2/3) | 25 % (2/3) | 25 % (2/3) | 25 % (2/3) | 64 % (9/0) | 86 % (12/0) | 86 % (12/0) |
| 4.6 Mise en page | 6 | 33 % (2/0) | 33 % (2/0) | 33 % (2/0) | 83 % (5/0) | 100 % (6/0) | 100 % (6/0) | 100 % (6/0) |
| 4.7 Docking et documents | 5 | 20 % (0/2) | 20 % (0/2) | 20 % (0/2) | 20 % (0/2) | 40 % (2/0) | 60 % (3/0) | 60 % (3/0) |
| 4.8 Fenêtres, dialogues, notifications | 13 | 38 % (4/2) | 38 % (4/2) | 38 % (4/2) | 46 % (5/2) | 73 % (9/1) | 73 % (9/1) | 73 % (9/1) |
| 4.9 Planification | 10 | 20 % (0/4) | 20 % (0/4) | 20 % (0/4) | 20 % (0/4) | 20 % (0/4) | 80 % (8/0) | 90 % (9/0) |
| 4.10 Visualisation de données | 13 | 4 % (0/1) | 4 % (0/1) | 35 % (4/1) | 35 % (4/1) | 35 % (4/1) | 77 % (10/0) | 77 % (10/0) |
| 4.11 Documents | 6 | 25 % (1/1) | 25 % (1/1) | 25 % (1/1) | 25 % (1/1) | 25 % (1/1) | 50 % (3/0) | 50 % (3/0) |
| 4.12 Rapports, impression, export | 4 | 13 % (0/1) | 13 % (0/1) | 50 % (2/0) | 50 % (2/0) | 50 % (2/0) | 50 % (2/0) | 50 % (2/0) |
| 4.13 Composants transverses | 10 | 15 % (0/3) | 15 % (0/3) | 25 % (1/3) | 45 % (4/1) | 65 % (6/1) | 70 % (7/0) | 70 % (7/0) |
| **Catalogue** | **166** | **28 % (34/25)** | **29 % (36/25)** | **39 % (55/21)** | **46 % (67/18)** | **67 % (106/9)** | **82 % (136/1)** | **83 % (137/1)** |
<!-- fin -->

### Profondeur (check-lists 5.1 à 5.7)

<!-- généré:PROFONDEUR -->
| Tableau | Lignes | Aujourd'hui | S1 | S2 | S3 | S4 | S5 | S6 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 5.1 Grille | 44 | 36 % (14/4) | 36 % (14/4) | 58 % (24/3) | 77 % (34/0) | 89 % (39/0) | 98 % (43/0) | 98 % (43/0) |
| 5.2 Ruban, barres et menus | 26 | 44 % (11/1) | 44 % (11/1) | 44 % (11/1) | 44 % (11/1) | 88 % (23/0) | 94 % (24/1) | 94 % (24/1) |
| 5.3 Docking | 16 | 44 % (7/0) | 44 % (7/0) | 44 % (7/0) | 44 % (7/0) | 88 % (14/0) | 88 % (14/0) | 88 % (14/0) |
| 5.4 Agenda | 23 | 37 % (7/3) | 37 % (7/3) | 37 % (7/3) | 37 % (7/3) | 41 % (8/3) | 91 % (21/0) | 91 % (21/0) |
| 5.5 Gantt | 27 | 57 % (15/1) | 57 % (15/1) | 63 % (17/0) | 74 % (20/0) | 74 % (20/0) | 85 % (23/0) | 100 % (27/0) |
| 5.6 Texte riche | 23 | 39 % (7/4) | 39 % (7/4) | 39 % (7/4) | 39 % (7/4) | 54 % (12/1) | 91 % (21/0) | 91 % (21/0) |
| 5.7 Socle des éditeurs | 17 | 53 % (7/4) | 68 % (11/1) | 71 % (12/0) | 88 % (15/0) | 88 % (15/0) | 88 % (15/0) | 88 % (15/0) |
| **Profondeur** | **176** | **43 % (68/17)** | **45 % (72/14)** | **51 % (85/11)** | **60 % (101/8)** | **76 % (131/4)** | **92 % (161/1)** | **94 % (165/1)** |
<!-- fin -->

### Dimensions transverses (§ 3 du HTML, non notées)

| Dimension | Aujourd'hui | Lots | Cible |
|---|---|---|---|
| OS | Linux validé ; Windows et macOS possibles, non qualifiés | F10 (S5) | Windows et macOS qualifiés |
| Thèmes | Yaru clair et sombre, palettes injectables, contraste testé | F8 (S5) | plus contraste élevé, densité et concepteur de palettes |
| Accessibilité | focus visible, contraste testé, un seul `AutomationPeer` | E12, F5, G3 (S3) | toute la suite exposée aux technologies d'assistance |
| Localisation | aucune : libellés français en dur | F3 (S2) | ressources, français et anglais, culture commutable |
| Droite-à-gauche | non | F9 (S5) | contrôles dessinés en `FlowDirection` |
| MVVM et liaison | partiel | E3, F1, F2 (S1) ; C1 (S4) ; T5 (S5) | valeurs, sélections, collections et modèles liables partout |
| Conception visuelle | prévisualiseur Avalonia seul | F11 (S5) | données d'exemple ; contrôles dessinés visibles en conception |
| Impression et export | CSV (grille), PNG (Gantt, carte mentale) | I1 à I4 (S1 à S4), R3 et R4 (S5) | impression, aperçu et PDF de tout contrôle ; XLSX, DOCX, ODT |
| IA | aucune | Q6 | selon la décision |

## 7. Impression et export : moteur et adoption

Les lignes « impression » et « export » reviennent dans plusieurs check-lists : 5.1, 5.4, 5.5 et 5.6. La règle est la suivante.
- **Le moteur** (I1, I2) fournit la pagination, l'aperçu, l'impression, la sortie PDF et les formats d'export. Il ne coche à lui seul aucune ligne de check-list.
- **L'adoption** fait implémenter le contrat imprimable par un contrôle, et c'est elle qui coche les lignes de ce contrôle :
  - I3 pour la grille et le Gantt ;
  - I4 pour l'agenda, le texte riche et les autres contrôles ;
  - R3 pour la vue page du texte riche.
- **La ligne de catalogue** « impression et aperçu de tout contrôle » (4.12) passe à ✅ avec le moteur et la première adoption (I3, S2).

## 8. Décisions à prendre

- **Q1 — Règle de partage avec UGantt. Tranché le 04/10/2026** : la suite se conçoit pour elle-même, et le Gantt continue de s'aligner sur MS Project.
  - La suite porte le programme de la charte (chantier O), sauf ce qui relève d'une application : la coquille, les fichiers et le multi-projet, les modèles, les rapports et les tableaux de bord.
  - L'interop se limite à MS Project XML.
- **Q2 — LiveCharts2.**
  - La délégation du 14/07/2026 a été décidée pour une application. Reste-t-elle le choix de la suite ?
  - Si oui, faut-il la livrer dans un paquet séparé (`HDev.UI.Charts`), pour garder la bibliothèque sans dépendance tierce ?
  - La famille la plus faible du benchmark (4.10, 4 %) en dépend.
- **Q3 — Socle des éditeurs. Tranché le 05/10/2026** (`Conception-Socle-Editeurs.md`, § 10) :
  - la forme B ;
  - un éditeur par type de valeur ;
  - le nom de la valeur d'Avalonia quand il existe, `Value` sinon ;
  - une fonction n'est retirée que si elle a un équivalent.
- **Q4 — Moteur PDF et impression.**
  - Pour le PDF vectoriel, trois voies : le moteur PDF de Skia (déjà livré avec Avalonia), un écrivain maison, ou une bibliothèque.
  - Pour l'impression : le portail XDG, ou CUPS.
  - Pièces versées (`Plan-Apports.md`) : une impression par CUPS éprouvée en usage réel (APP-56) ; un PDF produit par une bibliothèque sous licence Community, ni MIT ni Apache (APP-104).
- **Q5 — Formats bureautiques.** XLSX, DOCX et ODT : une bibliothèque libre (par exemple DocumentFormat.OpenXml), ou des écrivains maison.
  - Pièce versée : un export XLSX éprouvé par ClosedXML (MIT), avec sa version figée, sa marge de largeur compensée et ses dépendances (APP-105).
- **Q6 — IA.**
  - Des composants IA dans la suite (chat, assistant, collage intelligent), indépendants du fournisseur ? Ou une absence par décision ?
  - Pièce versée : un assistant appliqué section par section, où rien n'est enregistré sans le geste de l'utilisateur (APP-106).
  - Lignes concernées :
<!-- généré:DECISION4:Q6 -->
    - 4.13 : Chat IA ; Extensions IA (assistant, Smart Paste…)
    - 5.1 : IA (Smart Paste, recherche sémantique)
    - 5.6 : IA (assistant)
    - 5.7 : IA (autocomplétion, assistant)
<!-- fin -->
- **Q7 — Absences par décision proposées (⚪)**, à accepter ou à refuser une à une :
<!-- généré:DECISION:Q7 -->
  - 4.1 : Graphique intégré à la grille
  - 4.2 : Choix de police ; Galerie en popup ; Roues tactiles ; Pinceau (dégradés) ; Code-barres ; Webcam
  - 4.4 : Menu radial
  - 4.5 : Onglets dans la barre de titre ; Boîte à outils
  - 4.7 : UI « Modern » (modules plein écran)
  - 4.8 : Ruban ou commandes dans la barre de titre ; Fenêtre Fluent ; Explorateur de fichiers embarqué
  - 4.9 : Vue Gantt de l'agenda
  - 4.10 : Graphiques 3D ; Carte géographique ; SVG interactif
  - 4.13 : Gabarits d'UI HTML-CSS
  - 5.4 : Vue Gantt de l'agenda ; Synchronisation Google ou Microsoft
  - 5.6 : Champs, publipostage
  - 5.7 : Libellé flottant
<!-- fin -->
- **Q8 — Absences déjà actées, à reconfirmer** sous le principe du 04/10/2026 :
  - MDI et fusion MDI (incompatibles avec Wayland) ;
  - tableur ;
  - visionneuse PDF, à revoir une fois I1 fait ;
  - API de documents ;
  - concepteur de rapports ;
  - tableaux de bord BI.
- **Q9 — Politique de dépendances.**
  - Licences acceptées : MIT, BSD, Apache ?
  - Paquets séparés pour les dépendances lourdes ?
  - Critères de remplacement d'une dépendance ?
  - Pièces versées : les licences de la bibliothèque PDF et des dépendances de ClosedXML (APP-104, APP-105) ; la police à chasse fixe à retenir (APP-28) ; la distribution de la suite par paquets versionnés plutôt que par chemins relatifs (APP-107).
- **Q10 — Banque d'icônes.** Un jeu maison, ou un jeu libre (licence à vérifier) ? Et sous quel format ?
- **Q11 — Moteur de planification dans un assembly pur.** Un assembly de la suite, sans Avalonia, consommé par le Gantt et l'agenda : plus facile à tester, et utilisable sans interface, au prix d'un assembly de plus. Seule la couleur par tâche le lie aujourd'hui à Avalonia. Si oui : lot O1, avant L3.
- **Q12 — Formules (L25).** La charte ne lançait L25 que sur demande prouvée, au nom d'un usage applicatif. L'alignement sur MS Project plaide pour des champs calculés. Faut-il le planifier après L17, ou le garder sur demande ?
- **Q13 — Un seul modèle de ressources** pour le Gantt et l'agenda, défini par L10 (S5) et utilisé par T1. Le reste du pilier ressources reste en S6.

Restent ouvertes ailleurs :
- la vérification des popups sur le bureau, avant E2 (`Plan-Editeurs.md`) ;
- les absences proposées des éditeurs, reprises dans Q7.

## 9. Risques

- **Volume.**
  - Le plan compte 118 lots, dont 4 déjà faits (E0, E1, L0, L1) et 10 de taille XL : G6, G8, M1, C3, T1, R3, et L3, L4, L12 et L13 pour le Gantt. C'est plusieurs fois ce que la suite a déjà produit. Dix de ces lots viennent des apports des applications (`Plan-Apports.md`).
  - Le moteur de répartition échelonnée (L12) est le lot le plus difficile : il conditionne le nivellement, le suivi, la valeur acquise et les vues d'usage.
  - Chaque jalon est un point d'arrêt légitime.
- **Vérification sur le bureau avant E2.** La survie des popups à une brève désactivation de la fenêtre ne se teste pas en headless : elle se vérifie à la main (`Plan-Editeurs.md`).
- **Deux dépôts.**
  - Le chantier G avance dans un autre dépôt.
  - `IMPROVEMENTS.md` est à réaligner : son tableau § 0 date d'avant les lots de juillet 2026.
- **Popups sous GNOME/XWayland.** Chaque nouveau popup (C, N, M2, V, T) se vérifie sur le bureau réel.
- **Dépendances tierces** (Q2, Q4, Q5, Q9) : il faut les peser pour la souveraineté et pour la maintenance.
- **Livraisons.** Chaque livraison peut casser les applications, puisque les ruptures d'API sont permises. Les notes de migration et la vérification de compatibilité en amont en limitent le coût ; le rythme reste celui de Julien.
- **Apports des applications.**
  - Une partie du code d'origine n'est pas commitée dans les applications au 06/10/2026 : ses chemins peuvent bouger. `Plan-Apports.md` les date.
  - HDevRoster ne tient pas la contrainte clavier du § 4 jusqu'à T6 (S5). C'est le choix proposé, puisque la frise est reprise et qu'aucune application ne l'utilise plus ; F12 peut l'avancer.
- **Projection optimiste.**
  - Les jalons supposent que chaque lot couvre toutes ses lignes. Un lot qui en laisse une à 🟡 baisse d'autant.
  - Le score mesure l'étendue, pas l'effort : la ligne « formatage conditionnel » vaut la ligne « code-barres ».

## 10. Tenue du plan

- **Un lot terminé** : son statut est mis à jour dans le fichier du chantier, puis les états du benchmark, puis la projection.
- **Une décision prise** (Q) : elle est inscrite ici et dans le chantier concerné. Les lignes ⚪ acceptées passent dans le benchmark.
- **Les plans existants** restent maîtres de leurs identifiants (§ 1).
- **Un apport des applications** entre par `Plan-Apports.md`, avec une seule destination. Le lot qui le reçoit le décrit en termes génériques, avec son numéro APP. L'apport est clos avec son lot.
- **Une livraison aux applications**, quand Julien la décide, suit trois étapes :
  - fusion de la branche de développement dans `hdev`, dans les deux dépôts ;
  - notes de migration à jour ;
  - vérification de compatibilité de Synaxis et de myFlightOpsBoard, en lecture seule.

  Les ruptures sont prévenues, pas réparées depuis ce travail.
- **Les tableaux balisés** (`<!-- généré:… -->`) ne se modifient pas à la main. On les recalcule avec `python3 plans/outils/generer.py`, qui s'appuie sur :
  - les états actuels du benchmark ;
  - `plans/outils/affectation.py`, qui donne la destination, la cible et le jalon de chaque ligne ;
  - `plans/outils/lots.py`, qui décrit les lots.

  Le script vérifie que chaque ligne a une seule destination, qu'aucune ne régresse, que la règle des contrôles phares est respectée et que les dépendances suivent l'ordre des jalons.
