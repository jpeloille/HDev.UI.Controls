# Chantier N : navigation

Chantier du plan de la suite (`../Plan-Suite.md`). Il couvre le tableau 4.5 du benchmark. État au 04/10/2026. Statut : proposition.

## 0. Point de départ

<!-- généré:SCORES:4.5 -->
| Tableau | Lignes | Aujourd'hui | S1 | S2 | S3 | S4 | S5 | S6 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 4.5 Navigation | 14 | 25 % | 25 % | 25 % | 25 % | 64 % | 86 % | 86 % |
<!-- fin -->

- **Couvert.**
  - Les onglets, avec fermeture annulable et clavier, mais sans débordement ni glisser.
  - La barre de modules : un rail vertical avec icône, libellé et badge, et un mode compact.
- **Partiel.**
  - L'accordéon : pas de sous-niveaux, de recherche ni de mode replié.
  - Le menu hamburger : pas de mode overlay.
  - L'arbre : pas de virtualisation, de cases, d'édition ni de `ItemsSource`.
- **Manquent** : les pages à transitions, les tuiles, le fil d'Ariane, les étapes d'un processus, l'assistant, le carrousel.

## 1. Décisions proposées

- **L'arbre reste mono-colonne.** L'arbre multi-colonnes, c'est la TreeList (G4). L'arbre gagne la liaison hiérarchique (F1), la virtualisation (lignes visibles aplaties, comme la grille), les cases et l'édition du libellé.
- **Les transitions** reposent sur F7. L'assistant et les étapes partagent un même modèle d'étapes, avec validation avant de passer à l'étape suivante (E8).
- **Absences proposées (Q7)** :
  - les onglets dans la barre de titre, contraires au chrome natif ;
  - la boîte à outils, fonction propre à WinForms.

## 2. Lots

<!-- généré:LOTS:N -->
| Lot | Objet | Taille | Dépend de | Jalon | Lignes visées | Statut |
|---|---|---|---|---|---|---|
| **N0** | Caractérisation de la navigation | S | — | S4 | — | 🔲 |
| **N1** | Arbre complet : liaison, virtualisation, cases, édition | L | N0, F1 | S4 | 4.5 : 1 | 🔲 |
| **N2** | Accordéon à niveaux et menu hamburger | M | N0 | S4 | 4.5 : 2 | 🔲 |
| **N3** | Parcours : fil d'Ariane, étapes, assistant | L | — | S4 | 4.5 : 3 | 🔲 |
| **N4** | Pages à transitions ; onglets à débordement et glisser | M | F7 | S4 | 4.5 : 1 | 🔲 |
| **N5** | Tuiles et carrousel | L | — | S5 | 4.5 : 3 | 🔲 |
<!-- fin -->

## 3. Détail des lots

### N0 : caractérisation (S)

**Objet.** Figer les onglets, l'accordéon, la barre de modules et l'arbre avant N1 et N2.

### N1 : arbre complet (L)

**Contenu.**
- La source hiérarchique liée (F1).
- La virtualisation de milliers de nœuds.
- Des cases, tri-état comprises, propagées aux parents.
- L'édition du libellé en place.
- La recherche et le filtre.

**Acceptation.**
- Un banc sur 10 000 nœuds.
- Des tests d'interaction.
- Les tests F0 et N0 restent verts.

### N2 : accordéon à niveaux et menu hamburger (M)

**Contenu.**
- L'accordéon gagne des sous-niveaux, la recherche et le mode replié en icônes.
- Le menu hamburger gagne le mode overlay.

**Acceptation.** Des tests d'interaction et des planches.

### N3 : parcours (L)

**Contenu.**
- Le fil d'Ariane, cliquable, avec menus de frères et saisie de chemin.
- Les étapes d'un processus.
- L'assistant : des pages, la validation et les boutons Précédent et Suivant.

**Acceptation.** Des tests d'interaction et des planches.

### N4 : pages à transitions et onglets avancés (M)

**Contenu.**
- Un cadre de navigation qui gère l'historique et les transitions (F7).
- Le débordement des onglets vers un menu, et leur réordonnancement par glisser.

**Acceptation.** Des tests d'interaction.

### N5 : tuiles et carrousel (L)

**Contenu.**
- Un contrôle de tuiles : groupes, tailles, glisser.
- Une barre et un menu de tuiles.
- Un diaporama.

**Acceptation.** Des tests d'interaction et des planches.

## 4. Projection

<!-- généré:PROJ:4.5 -->
| Fonction | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Onglets | ✅ | N4 | — | inchangé |
| Onglets dans la barre de titre | ❌ | Q7 | — | ⚪ proposé |
| Pages avec transitions | ❌ | N4 | S4 | ✅ |
| Accordéon, barre latérale à groupes | 🟡 | N2 | S4 | ✅ |
| Barre de modules (Outlook) | ✅ | — | — | inchangé |
| Menu hamburger | 🟡 | N2 | S4 | ✅ |
| Tuiles | ❌ | N5 | S5 | ✅ |
| Barre et menu de tuiles | ❌ | N5 | S5 | ✅ |
| Arbre | 🟡 | N1 | S4 | ✅ |
| Fil d'Ariane | ❌ | N3 | S4 | ✅ |
| Étapes d'un processus | ❌ | N3 | S4 | ✅ |
| Assistant | ❌ | N3 | S4 | ✅ |
| Boîte à outils | ❌ | Q7 | — | ⚪ proposé |
| Diaporama, carrousel | ❌ | N5 | S5 | ✅ |
<!-- fin -->

## 5. Risques

- **La virtualisation de l'arbre** change sa structure interne. N0 et F0 doivent couvrir ce que les applications voient.
