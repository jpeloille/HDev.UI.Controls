# Chantier A : ancrage (docking)

Chantier du plan de la suite (`../Plan-Suite.md`). Il couvre les tableaux 4.7 et 5.3 du benchmark. État au 04/10/2026. Statut : proposition.

## 0. Point de départ

<!-- généré:SCORES:4.7, 5.3 -->
| Tableau | Lignes | Aujourd'hui | S1 | S2 | S3 | S4 | S5 | S6 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 4.7 Docking et documents | 5 | 20 % | 20 % | 20 % | 20 % | 40 % | 60 % | 60 % |
| 5.3 Docking | 16 | 44 % | 44 % | 44 % | 44 % | 88 % | 88 % | 88 % |
<!-- fin -->

- **La phase 1 est faite.**
  - Un moteur d'arbre pur et testé : séparations, groupes d'onglets, items, avec un invariant d'élagage.
  - L'ancrage aux bords et une région centrale de documents.
  - Des séparateurs à proportions, la fermeture, la sauvegarde et la restauration du layout, une API en code, le thème clair et sombre.
- **Manquent.**
  - Le glisser avec indicateurs d'ancrage, et le réordonnancement des onglets (phase 2).
  - Les panneaux flottants, l'auto-masquage et l'épinglage (phase 3).
  - Les boutons d'en-tête, les panneaux fermés restaurables, le sélecteur Ctrl+Tab.
- **Le MDI reste refusé** (incompatible avec Wayland) ; la région centrale en tient lieu.

## 1. Décisions proposées

- **La règle Wayland de juillet 2026 est gardée.** Tout le réarrangement (glisser, cibles, aperçu) vit dans l'overlay de la fenêtre, jamais dans du positionnement de fenêtres par le système. « Flottant » veut dire une fenêtre SDI ordinaire, réancrée par une action explicite.
- **Le glisser se factorise** avec la personnalisation du Layout Control (M1) et des barres (C4).
- **Absence proposée (Q7)** : l'UI « Modern » (modules plein écran, style Windows 8).

## 2. Lots

<!-- généré:LOTS:A -->
| Lot | Objet | Taille | Dépend de | Jalon | Lignes visées | Statut |
|---|---|---|---|---|---|---|
| **A0** | Caractérisation du docking | S | — | S4 | — | 🔲 |
| **A1** | Phase 2 : glisser, indicateurs d'ancrage, onglets réordonnables | L | A0 | S4 | 4.7 : 1 ; 5.3 : 2 | 🔲 |
| **A2** | Phase 3 : panneaux flottants, auto-masquage, épinglage | L | A1 | S4 | 4.7 : 1 ; 5.3 : 2 | 🔲 |
| **A3** | Confort : boutons d'en-tête, panneaux fermés, Ctrl+Tab | M | A1 | S4 | 5.3 : 3 | 🔲 |
| **A4** | Tableau de bord à widgets | M | A2 | S5 | 4.7 : 1 | 🔲 |
<!-- fin -->

## 3. Détail des lots

### A0 : caractérisation (S)

**Objet.** Figer la phase 1 avant A1 : ancrage, onglets, fermeture, layout.

### A1 : phase 2 (L)

**Contenu.**
- Le glisser d'un panneau ou d'un onglet, avec des indicateurs d'ancrage et un aperçu dans l'overlay.
- Le réordonnancement des onglets.
- Des groupes de documents côte à côte.

**Acceptation.**
- Des tests du moteur, notamment `InsertRelative` depuis le glisser.
- Des tests d'interaction.
- Une vérification sur le bureau réel.

### A2 : phase 3 (L)

**Contenu.**
- Le détachement d'un panneau en fenêtre SDI, et son réancrage explicite.
- L'auto-masquage et l'épinglage sur les bords.
- La sérialisation des panneaux flottants (la collection `Floating` existe déjà dans le modèle).

**Acceptation.** Des tests du moteur, des tests d'interaction et une vérification sous Wayland.

### A3 : confort (M)

**Contenu.**
- Les boutons d'en-tête : maximiser, et des boutons personnalisés.
- Les panneaux fermés restaurables.
- Un sélecteur de documents et de panneaux au clavier (Ctrl+Tab).

**Acceptation.** Des tests d'interaction.

### A4 : tableau de bord à widgets (M)

**Contenu.** Des widgets redimensionnables et déplaçables sur une grille, avec un layout sérialisé, sur le moteur d'arbre.

**Acceptation.** Des tests du moteur et des planches.

## 4. Projection

### 4.7 Docking et documents

<!-- généré:PROJ:4.7 -->
| Fonction | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Panneaux ancrables | 🟡 | A2 | S4 | ✅ |
| Documents à onglets | 🟡 | A1 | S4 | ✅ |
| MDI natif (fenêtres enfants) | ⚪ | acté | — | inchangé |
| UI « Modern » (modules plein écran) | ❌ | Q7 | — | ⚪ proposé |
| Tableau de bord à widgets | ❌ | A4 | S5 | ✅ |
<!-- fin -->

### 5.3 Docking

<!-- généré:PROJ:5.3 -->
| Capacité | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Ancrage aux bords, région centrale de documents | ✅ | — | — | inchangé |
| Groupes d'onglets | ✅ | — | — | inchangé |
| Splitters à proportions | ✅ | — | — | inchangé |
| Fermeture d'un panneau | ✅ | — | — | inchangé |
| Sauvegarde et restauration du layout | ✅ | — | — | inchangé |
| API de manipulation en code | ✅ | — | — | inchangé |
| Glisser un panneau avec indicateurs d'ancrage | ❌ | A1 | S4 | ✅ |
| Réordonnancement des onglets | ❌ | A1 | S4 | ✅ |
| Panneaux flottants | ❌ | A2 | S4 | ✅ |
| Auto-masquage, épinglage | ❌ | A2 | S4 | ✅ |
| Boutons d'en-tête (maximiser, personnalisés) | ❌ | A3 | S4 | ✅ |
| Panneaux fermés restaurables | ❌ | A3 | S4 | ✅ |
| Sélecteur de documents (Ctrl+Tab) | ❌ | A3 | S4 | ✅ |
| MDI (onglets ou fenêtres) | ⚪ | acté | — | inchangé |
| Fusion des barres et du ruban | ⚪ | acté | — | inchangé |
| Habillage clair et sombre | ✅ | — | — | inchangé |
<!-- fin -->

## 5. Risques

- **Le glisser dans l'overlay** doit rester fluide sur de grandes fenêtres, et sur un bureau aux rebonds d'activation. Il se vérifie sur le bureau réel.
