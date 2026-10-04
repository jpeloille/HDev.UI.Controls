# Chantier C : commandes (boutons, ruban, barres, menus)

Chantier du plan de la suite (`../Plan-Suite.md`). Il couvre les tableaux 4.3, 4.4 et 5.2 du benchmark. État au 04/10/2026. Statut : proposition.

## 0. Point de départ

<!-- généré:SCORES:4.3, 4.4, 5.2 -->
| Tableau | Lignes | Aujourd'hui | S1 | S2 | S3 | S4 | S5 | S6 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 4.3 Boutons et libellés | 6 | 67 % | 67 % | 67 % | 67 % | 100 % | 100 % | 100 % |
| 4.4 Rubans, barres et menus | 17 | 32 % | 32 % | 32 % | 32 % | 82 % | 88 % | 88 % |
| 5.2 Ruban, barres et menus | 26 | 44 % | 44 % | 44 % | 44 % | 88 % | 94 % | 94 % |
<!-- fin -->

**Couvert**
- Boutons : 6 variantes, 3 tailles, icône seule, bascule et groupe segmenté.
- Barre de menus : Alt+lettre, navigation clavier complète, raccourcis réels.
- Menu contextuel, barre d'état, accélérateurs (`HDevShortcutManager`).
- Ruban de base : onglets, groupes, grands et petits boutons, déroulants, bascules, lanceur de dialogue, repli.

**Manque**
- Les boutons à menu et partagés hors du ruban.
- Le ruban avancé :
  - onglets contextuels (`IsContextual` et `AccentColor` sont déclarés, jamais rendus) ;
  - Backstage, barre d'accès rapide, galeries, key tips ;
  - ruban simplifié.
- Barres : débordement, éditeurs dans les barres, recherche de commandes.
- Infobulles riches : le texte seul existe aujourd'hui.

**Le problème de fond** : chaque contrôle (ruban, barre, menu) a ses propres éléments, construits en code. Il n'existe pas de modèle de commandes partagé.

## 1. Décisions proposées

- **Un modèle de commandes partagé, consommé par toutes les surfaces.** Une commande porte :
  - libellé, icône (F4) et raccourci ;
  - état (actif, coché) et `ICommand` ;
  - infobulle riche : titre, description, pied et raccourci.

  Le ruban, les barres, les menus, les boutons et la recherche de commandes affichent tous les mêmes commandes. On y gagne sur DevExpress, où chaque surface a ses éléments.
- **Construction déclarative.** `ItemsSource` et gabarits, sur le contrat des collections (F1). Le code reste possible.
- **Recherche de commandes intégrée**, à la manière d'une palette de commandes : elle trouve toute commande enregistrée, avec son raccourci.
- **Absence proposée (Q7)** : le menu radial, pensé pour le tactile. La fusion MDI reste refusée (Wayland).

## 2. Lots

<!-- généré:LOTS:C -->
| Lot | Objet | Taille | Dépend de | Jalon | Lignes visées | Statut |
|---|---|---|---|---|---|---|
| **C0** | Caractérisation du ruban, des barres et des menus | S | — | S4 | — | 🔲 |
| **C1** | Modèle de commandes partagé et construction déclarative | L | C0, F1, F4 | S4 | 5.2 : 2 | 🔲 |
| **C2** | Boutons à menu et partagés, popup de contrôles | M | C1, E2 | S4 | 4.3 : 2 ; 4.4 : 1 | 🔲 |
| **C3** | Ruban complet | XL | C1 | S4 | 4.4 : 5 ; 5.2 : 6 | 🔲 |
| **C4** | Barres : débordement, éditeurs, infobulles riches, recherche de commandes | L | C1, E3 | S4 | 4.4 : 4 ; 5.2 : 4 | 🔲 |
| **C5** | Personnalisation par l'utilisateur et mini-barre | M | C3 | S5 | 4.4 : 1 ; 5.2 : 2 | 🔲 |
<!-- fin -->

## 3. Détail des lots

### C0 : caractérisation (S)

**Objet.** Figer le ruban, les barres, les menus, les raccourcis et la navigation clavier avant C1. On reprend les vérifications existantes et on les complète.

### C1 : modèle de commandes et construction déclarative (L)

**Contenu.**
- Le type commande, ses états et ses notifications.
- Le registre des commandes d'une fenêtre, lié à `HDevShortcutManager`.
- La construction du ruban, des barres et des menus par `ItemsSource`.

**Acceptation.**
- La même commande s'affiche dans un menu, une barre et le ruban, et son état se propage partout.
- Un formulaire XAML construit un ruban sans code.

### C2 : boutons à menu, partagés et popup de contrôles (M)

**Contenu.**
- Un bouton à menu, et un bouton partagé (action et menu).
- Un bouton qui ouvre un contenu libre, sur la pièce popup d'E2.

**Acceptation.** Des tests d'interaction (souris, clavier, Échap) et des planches.

### C3 : ruban complet (XL)

**Contenu.**
- Les onglets contextuels, colorés, qui apparaissent avec leur contexte.
- Le menu application et le Backstage.
- La barre d'accès rapide.
- Les galeries, en ligne et déroulantes.
- Les key tips (Alt).
- Le ruban simplifié et les groupes adaptatifs, qui se replient selon la largeur.

**Acceptation.** Des tests headless pour chaque fonction, des planches en clair et en sombre, et une navigation entièrement au clavier.

### C4 : barres (L)

**Contenu.**
- Le débordement vers un menu.
- Les déroulants.
- Les éditeurs dans les barres et le ruban, sur le socle (E3).
- Les infobulles riches.
- La recherche de commandes.
- Des barres déplaçables et ancrables dans l'overlay, sans fenêtre flottante du système.

**Acceptation.** Des tests d'interaction et des planches.

### C5 : personnalisation et mini-barre (M)

**Contenu.**
- La personnalisation par l'utilisateur : ajouter, retirer et réordonner, avec persistance par F6.
- Une mini-barre contextuelle près de la sélection.

**Acceptation.** Des tests headless. La ligne « Mini-barre, menu radial » reste 🟡 tant que le menu radial est écarté.

## 4. Projection

### 4.3 Boutons et libellés

<!-- généré:PROJ:4.3 -->
| Fonction | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Bouton | ✅ | — | — | inchangé |
| Bouton à icône seule | ✅ | — | — | inchangé |
| Bouton à menu | ❌ | C2 | S4 | ✅ |
| Bouton partagé (split) | ❌ | C2 | S4 | ✅ |
| Bouton bascule, groupe segmenté | ✅ | — | — | inchangé |
| Libellé formaté | ✅ | — | — | inchangé |
<!-- fin -->

### 4.4 Rubans, barres et menus

<!-- généré:PROJ:4.4 -->
| Fonction | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Ruban | 🟡 | C3 | S4 | ✅ |
| Onglets contextuels | ❌ | C3 | S4 | ✅ |
| Menu application, Backstage | ❌ | C3 | S4 | ✅ |
| Barre d'accès rapide (QAT) | ❌ | C3 | S4 | ✅ |
| Galeries | ❌ | C3 | S4 | ✅ |
| Mini-barre contextuelle | ❌ | C5 | S5 | ✅ |
| Recherche de commandes | ❌ | C4 | S4 | ✅ |
| Barres d'outils | 🟡 | C4 | S4 | ✅ |
| Éditeurs dans les barres | ❌ | C4 | S4 | ✅ |
| Menu principal | ✅ | — | — | inchangé |
| Menu contextuel | ✅ | — | — | inchangé |
| Menu radial | ❌ | Q7 | — | ⚪ proposé |
| Popup de contrôles | ❌ | C2 | S4 | ✅ |
| Barre d'état | ✅ | — | — | inchangé |
| Fusion MDI des barres | ⚪ | acté | — | inchangé |
| Infobulles riches | 🟡 | C4 | S4 | ✅ |
| Accélérateurs clavier | ✅ | — | — | inchangé |
<!-- fin -->

### 5.2 Ruban, barres et menus

<!-- généré:PROJ:5.2 -->
| Capacité | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Onglets et groupes | ✅ | — | — | inchangé |
| Boutons grands et petits | ✅ | — | — | inchangé |
| Boutons déroulants | ✅ | — | — | inchangé |
| Boutons bascule | ✅ | — | — | inchangé |
| Lanceur de dialogue de groupe | ✅ | — | — | inchangé |
| Repli du ruban | ✅ | — | — | inchangé |
| Onglets contextuels | ❌ | C3 | S4 | ✅ |
| Key tips (Alt) | ❌ | C3 | S4 | ✅ |
| Menu application, Backstage | ❌ | C3 | S4 | ✅ |
| Barre d'accès rapide | ❌ | C3 | S4 | ✅ |
| Galeries | ❌ | C3 | S4 | ✅ |
| Éditeurs dans le ruban et les barres | ❌ | C4 | S4 | ✅ |
| Ruban simplifié, groupes adaptatifs | ❌ | C3 | S4 | ✅ |
| Mini-barre, menu radial | ❌ | C5 | S5 | 🟡 |
| Recherche de commandes | ❌ | C4 | S4 | ✅ |
| Personnalisation par l'utilisateur | ❌ | C5 | S5 | ✅ |
| Fusion MDI | ⚪ | acté | — | inchangé |
| Modèle de commandes partagé entre ruban, barres et menus | ❌ | C1 | S4 | ✅ |
| Infobulles riches | 🟡 | C4 | S4 | ✅ |
| Accélérateurs clavier | ✅ | — | — | inchangé |
| Barre de menus, navigation clavier complète | ✅ | — | — | inchangé |
| Menus contextuels (sous-menus, cochables) | ✅ | — | — | inchangé |
| Barres d'outils | ✅ | — | — | inchangé |
| Débordement, barres flottantes ou ancrables | ❌ | C4 | S4 | ✅ |
| Barre d'état à panneaux | ✅ | — | — | inchangé |
| Construction déclarative (`ItemsSource`) | ❌ | C1 | S4 | ✅ |
<!-- fin -->

## 5. Risques

- **C1 change l'API de trois contrôles.** C0 doit la protéger, et la note de migration doit la consigner (`MIGRATION-commandes.md`).
- **Les galeries et le Backstage** ouvrent des popups larges. Il faut vérifier leur garde sur le bureau réel.
