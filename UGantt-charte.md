# UGantt — charte de projet

> Logiciel de gestion de projet pur (activités RDOV), bâti sur HDev.UI.Controls/HDevGantt.
> Document d'architecture posé le 14/07/2026, **avant** toute ligne de code.

## 1. La cible — décidée le 14/07/2026 : catégories 1 & 2

**Cap retenu : « MS Project MOINS le `.mpp`, VBA et Project Server »** — soit ~60-65 % de
Project, la part utile. Décision assumée (souveraineté + valeur d'usage réelle).

- **Catégorie 1 — le produit réel** : profondeur du moteur d'ordonnancement (durée/travail,
  types de tâches, contraintes, calendriers multiples), **ressources / coûts / EVM**, suivi.
- **Catégorie 2 — les méta-systèmes** : système de champs, vues/tables/filtres/groupes
  composables, mise en forme. *Ce sont des multiplicateurs, pas des features.*
- **Catégorie 3 — explicitement HORS scope** : `.mpp` (format propriétaire → passer par
  **MS Project XML**, documenté), VBA + modèle objet (on a C# et le source, c'est mieux),
  Project Server / Online / SharePoint / timesheets, et le legacy (Gantt Chart Wizard,
  MPX, Visual Reports/OLAP).

**La parité stricte reste un mirage** (~35 ans × une équipe) : ce qui rend le cap tenable,
c'est que ~1/3 de Project est du legacy ou de l'écosystème serveur non désiré, et que la
**séquence est jalonnée** (§7) — chaque jalon laisse un outil *shippable*.
Jalons intermédiaires nommés : **classe GanttProject / OmniPlan / Merlin** (jalon 2),
puis **« P6-lite »** (jalon 3).

## 2. La règle de partage HDev.UI.Controls ↔ UGantt

> **Promotion sur PREUVE de réemploi, pas sur généricité théorique.**
> Une brique migre dans HDev.UI.Controls quand un **second consommateur existe** —
> pas quand elle est « théoriquement générique ».

- **HDev.UI.Controls** = le moteur + le contrôle **génériques** : planifier, calculer, rendre, interagir.
- **UGantt** = **le produit** : shell, fichiers, interop, rapports, domaine.

C'est la règle qu'applique déjà la suite : HDevGantt a été *délibérément* scopé
« pas de ressources », le moteur pur vit dans la lib et gagne des tests, l'UI est dessus.

### Les quatre arbitrages, tranchés par cette règle

| Brique | Ressort | Pourquoi |
|---|---|---|
| **Ressources / coûts / EVM** | **UGantt** | Double la surface du modèle et contredit une décision écrite (« HDevGantt n'est pas un planificateur de ressources »). Le besoin ressources de Synaxis est **déjà servi par HDevScheduler** — fondre des ressources dans HDevGantt brouillerait deux contrôles séparés exprès. Le calcul EVM est trivialement liftable plus tard. |
| **Interop MSP XML / XER / CSV** | **UGantt** | « Mapping pur » ≠ « dans la lib partagée maintenant ». Module de mapping pur **à l'intérieur** d'UGantt : testable et liftable. On apprend le format en s'en servant ; on promeut si un 2ᵉ consommateur le lit. |
| **Impression paginée + PDF** | **HDev.UI.Controls** | **La seule exception** — elle a déjà gagné sa promotion : l'impression manque dans **toute** la suite. C'est une capacité **transverse** (imprimer n'importe quel contrôle custom-rendered), pas une feature Gantt. Le Gantt en est juste le premier client. |
| **Vues alternatives** | **ça se scinde** | **PERT / réseau → HDev.UI.Controls** (rendu pur du graphe de dépendances, générique). **Histogramme / usage ressources → UGantt** (dépend du modèle ressources, donc suit les ressources). Ne pas les mettre dans le même seau. |

## 3. Les points d'extension — la colonne vertébrale

Séparer un contrôle de son app, ce n'est pas d'abord « qui possède quelle feature » :
c'est **quels points d'extension HDev.UI.Controls doit exposer pour qu'UGantt construise le
produit SANS modifier HDev.UI.Controls**.

Puisque **UGantt possède les ressources/coûts** mais que **HDevGantt possède le rendu**,
HDevGantt DOIT exposer :

1. **Modèle de colonnes configurable** — pour qu'UGantt ajoute ses colonnes coût/ressource
   sans toucher à la lib. (Aujourd'hui : 4 colonnes en dur — nom/début/fin/%.)
2. **Hook de rendu / couche d'adornement par ligne** — pour qu'UGantt peigne ses barres de
   coût, ses surcharges, ses indicateurs métier.
3. **Données applicatives qui survivent au save/load** — `Tag` **ne se sérialise pas** :
   c'est `GanttTask.Id` (posé le 14/07/2026) qui porte le lien métier, plus, si besoin,
   un sac de propriétés applicatives sérialisé.

> **Si ces trois points manquent, la première vraie feature d'UGantt force une réécriture
> de HDev.UI.Controls.** Ils priment sur toute liste de features.

## 4. Ce qu'UGantt possède

- **Shell** : HDevWindow + ruban + **HDevDock** + multi-projet
- **Fichiers** : ouvrir/enregistrer/récents/autosave ; format `.ugantt`
  (la sérialisation JSON du `GanttProject` existe déjà)
- **Interop** : MS Project XML, Primavera XER, CSV/Excel
- **Ressources / coûts / EVM** + leurs vues (feuille de ressources, histogramme, usage)
- **Rapports & tableaux de bord** (LiveCharts2 — déjà acté pour Ovidie)
- **Templates, préférences, thème**
- **Domaine RDOV** si spécifique
- Collaboration / stockage partagé (si un jour)

### Dogfooding — le bénéfice caché

UGantt serait le **premier vrai consommateur de HDevDock** (phase 1 faite le 14/07/2026),
et donc le **driver naturel de ses phases 2 et 3** (drag + cibles d'ancrage, float/auto-masqué).
Idem pour HDevRibbon avancé (backstage/QAT) et l'impression, aujourd'hui inexistante.

## 5. Prérequis de démarrage (câblage vérifié le 14/07/2026)

Un projet tiers qui consomme HDev.UI.Controls **doit** :

1. **Vivre en frère** : `RiderProjects/UGantt`, référençant `..\HDev.UI.Controls\HDev.UI.Controls\HDev.UI.Controls.csproj`.
   HDev.UI.Controls tire en dur `..\..\HDev.UI.DataGrid\src\HDev.UI.DataGrid.csproj` —
   les deux dépôts doivent rester en place. `PackageId HDev.UI.Controls` existe mais
   **n'est pas publié** : c'est du `ProjectReference`.
2. **Bootstrap non négociable** (sinon écran cassé) :
   ```csharp
   .With(new X11PlatformOptions { OverlayPopups = true })  // sinon menus/dropdowns INVISIBLES sous Linux
   .WithInterFont()                                        // HDevTheme cible fonts:Inter#Inter
   ```
3. **App.axaml** : `<FluentTheme />` + `StyleInclude avares://HDev.UI.Controls/Theme/DataGrid/Index.axaml`
   + les `SystemAccentColor` Ubuntu (#E95420…).
4. **Versions** : `net8.0`, Avalonia **11.2.1** partout (+ `Avalonia.Desktop`,
   `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`).

## 6. Le préalable qui conditionne tout

**Aujourd'hui, HDevGantt ne sait pas *fabriquer* un plan** : ni créer, ni supprimer, ni
indenter une tâche depuis l'UI. Durée, contraintes, EVM sont des raffinements sur un plan
qu'on ne peut pas encore bâtir.

→ **P0 #1 = édition structurelle de la table** (HDev.UI.Controls). C'est le premier chantier
quoi qu'il arrive : il débloque l'existence même d'UGantt.

La feuille de route HDev.UI.Controls (P0 → P3) vit dans `HDev.UI.Controls/context.md`.

---

## 7. Le programme — lots, dépendances, jalons

**Chiffrage en taille relative (S/M/L/XL) + ordre de dépendance, PAS en jours** — le
wall-clock serait une fiction. Ce qui pilote, ce sont les **jalons livrables** : la séquence
est construite pour que **s'arrêter à n'importe quel jalon laisse un outil shippable**.

Ressort : **[PC]** = HDev.UI.Controls (rendu, interaction, points d'extension) ·
**[UC]** = `UGantt.Core` (domaine pur testé) · **[UG]** = app UGantt.

### Lot 0 — Versionnage du schéma · S · ✅ **FAIT le 14/07/2026** [PC]
`"schema": 1` en racine, absent = v1, schéma futur refusé. *Gratuit tant qu'aucun fichier
RDOV n'existe sur disque ; cauchemar après.* Chaque lot cassant bumpe + branche la migration.

### Socle — « savoir fabriquer un plan »
| Lot | Taille | Dépend de | Ressort |
|---|---|---|---|
| **L1 — Édition structurelle de la table** : insérer/supprimer/indenter/désindenter/réordonner (drag + clavier) | M | — | PC |
| **L2 — Modèle de colonnes configurable** — *point d'extension n° 1* | M | L1 | PC |
| **L3 — `Duration` + `Work` + types de tâches** (fixed units/duration/work, effort-driven) — **CLÉ DE VOÛTE n° 1**, refactor **CASSANT** → schéma v2 + migration | **XL** | L1 | UC |
| **L4 — Calendriers multiples + granularité horaire** (projet/tâche/ressource, exceptions, postes) — **CLÉ DE VOÛTE n° 2**, traverse tout le moteur | **XL** | L3 | UC |

> ### 🚩 JALON 1 — « éditeur de plan cohérent »
> Bâtir, structurer, dater et calendariser un vrai plan. **Shippable.**

### Cat 1 — profondeur du moteur
| Lot | Taille | Dépend de | Ressort |
|---|---|---|---|
| **L5 — Contraintes (8) + deadlines + inspecteur** (« pourquoi cette date ? ») | L | L3, L4 | UC |
| **L6 — Passe avant/arrière complète** : ES/EF/LS/LF, marge libre, chemins critiques multiples, planification à rebours | M | L3, L4 | UC |
| **L7 — Tâches scindées / récurrentes / inactives** | M | L4 | UC |
| **L8 — Baselines multiples (11) + plans intermédiaires** | S | L3 | UC |
| **L9 — Impression paginée + PDF** — *transverse à TOUTE la suite*, Gantt = 1er client | L | (L2) | **PC** |

> ### 🚩 JALON 2 — « classe GanttProject / OmniPlan / Merlin » ⭐
> Outil de projet pro complet côté planification, **sans ressources**.
> **C'est le meilleur rapport valeur/coût du programme et probablement l'essentiel de
> l'usage RDOV réel.** Point d'arrêt légitime.

### Cat 1 — le pilier ressources (absent à 100 % aujourd'hui)
| Lot | Taille | Dépend de | Ressort |
|---|---|---|---|
| **L10 — Modèle ressources** : travail/matériel/coût, unités max, disponibilité datée, calendriers ressource | L | L4 | UC |
| **L11 — Affectations** : unités, travail, liaison tâche ↔ ressource | L | L10, L3 | UC |
| **L12 — ⚠️ MOTEUR TIMEPHASED** : répartition échelonnée travail/coût dans le temps + **8 contours de charge** | **XL** | L11 | UC |
| **L13 — Sur-allocation + nivellement** (ordre, dans la marge, scinder, niveler les affectations) | **XL** | **L12** | UC |
| **L14 — Coûts** : tables de taux (5/ressource), taux variables, heures sup, imputation, coûts fixes, budget | M | L11 | UC |
| **L15 — Suivi avancé** : % travail, % physique, réel/restant, date d'état, replanifier le non-achevé | M | **L12** | UC |
| **L16 — EVM** : BCWS/BCWP/ACWP, SV/CV, SPI/CPI, EAC/BAC/VAC/TCPI | M | **L12**, L14, L8 | UC |

> **⚠️ L12 est LE boss du programme.** Le timephased conditionne **L13, L15, L16 et L18** —
> soit la moitié de Cat 1. Tous les clones de Gantt l'esquivent ; c'est précisément ce qui
> les empêche d'égaler Project. Il est plus gros que toutes les vues réunies.
> **Ne pas l'enterrer dans « affectations ».**

> ### 🚩 JALON 3 — « P6-lite »
> Ressources, coûts, nivellement, EVM. Le pilier absent est comblé.

### Cat 2 — les méta-systèmes (les multiplicateurs)
| Lot | Taille | Dépend de | Ressort |
|---|---|---|---|
| **L17 — Système de champs** : natifs + personnalisés (Text/Number/Flag/Date/Duration/Cost/Outline Code) + roll-up paramétrable | L | L2 | UC + PC |
| **L18 — Vues d'usage** : grille échelonnée **éditable** (Task/Resource Usage), histogramme ressources | L | **L12**, L17 | PC |
| **L19 — Filtres / groupes / tris** : auto-filtre, surlignage, filtres interactifs, groupes multi-niveaux | L | L17 | PC |
| **L20 — Tables & vues composables** : définitions sauvegardables, vues combinées (split), Organisateur | L | L17, L19 | PC + UG |
| **L21 — Mise en forme** : styles de barres par catégorie, styles de texte, échelle **3 niveaux**, lignes de progression — *point d'extension n° 2 (adornement)* | L | L2 | PC |
| **L22 — Réseau / PERT + diagramme de relations** (rendu pur du graphe) | M | — | PC |
| **L23 — Vues Calendrier / Chronologie / Planificateur d'équipe** | L | L11 | PC |
| **L24 — Interop MS Project XML + mappage CSV/Excel** | L | L3, L10 | UG |

> ### 🚩 JALON 4 — « catégories 1 & 2 couvertes »

### Le lot volontairement dernier
| Lot | Taille | Ressort |
|---|---|---|
| **L25 — Moteur de formules + indicateurs graphiques** : expressions + fonctions, indicateurs pilotés par formule, tables de choix | **XL** | UC |

> **🔒 DEMAND-GATED — ne pas lancer sans besoin prouvé.** Project a des formules parce que
> des *millions* d'utilisateurs voulaient des échappatoires. Pour un outil solo, **coder en
> dur les champs que RDOV réclame = 5 % du coût pour 100 % du besoin réel.** C'est la règle
> « promotion sur preuve » appliquée aux *features*, pas seulement aux libs : concret d'abord,
> méta-système quand le besoin est démontré.

### Poids du programme
**25 lots** : 5 XL (L3, L4, L12, L13, L25) · 11 L · 7 M · 2 S.
Plus gros que tout ce que la suite a produit jusqu'ici, **cumulé**. D'où les jalons :
jalon 2 est le point d'arrêt le plus rentable, jalon 4 est le cap.
