# Chantier T : temps (agenda et roster)

Chantier du plan de la suite (`../Plan-Suite.md`). Il couvre le tableau 5.4 du benchmark et les lignes de l'agenda dans 4.9. État au 04/10/2026. Statut : proposition.

Le Gantt a son propre chantier (O, `O-gantt.md`), aligné sur MS Project depuis la décision Q1 du 04/10/2026.

## 0. Point de départ

<!-- généré:SCORES:4.9, 5.4 -->
| Tableau | Lignes | Aujourd'hui | S1 | S2 | S3 | S4 | S5 | S6 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 4.9 Planification | 10 | 20 % | 20 % | 20 % | 20 % | 20 % | 80 % | 90 % |
| 5.4 Agenda | 23 | 37 % | 37 % | 37 % | 37 % | 41 % | 91 % | 91 % |
<!-- fin -->

- **L'agenda (HDevScheduler)** a quatre vues, des récurrences de base, le glisser et le redimensionnement annulables, et la lecture seule.
- **Il lui manque** :
  - les ressources et la vue chronologique ;
  - les vues année et agenda ;
  - les exceptions de récurrence ;
  - le formulaire de rendez-vous ;
  - les rappels, les fuseaux et iCalendar ;
  - la liaison par mappings.
- **Le roster (HDevRoster)** est un contrôle séparé, sans lien avec l'agenda.

## 1. Décisions proposées

- **Agenda et roster partagent le moteur de temps.**
  - L'agenda gagne une vue chronologique par ressources.
  - Le roster garde sa forme propre : bande d'amplitude et voies.
- **Les ressources viennent d'un seul modèle**, défini par le Gantt (L10) et partagé avec l'agenda (décision Q13). D'où la dépendance de T1 envers L10.
- **Les fuseaux horaires passent par `DateTimeOffset`**, conformément à D4. Le fuseau d'affichage est réglable.
- **La liaison par mappings.** L'agenda et le Gantt se lient à des objets de l'application par des correspondances de champs, en plus de leur propre modèle (F2). C'est le lot T5, qui couvre les deux contrôles.
- **Absences proposées (Q7)** :
  - la vue Gantt de l'agenda, que le Gantt de projet couvre ;
  - la synchronisation avec Google et Microsoft, qui est un service et non un contrôle.

## 2. Lots

<!-- généré:LOTS:T -->
| Lot | Objet | Taille | Dépend de | Jalon | Lignes visées | Statut |
|---|---|---|---|---|---|---|
| **T0** | Caractérisation de l'agenda et du roster | S | — | S5 | — | 🔲 |
| **T1** | Agenda à ressources et vue chronologique | XL | T0, F2, L10 | S5 | 4.9 : 4 ; 5.4 : 3 | 🔲 |
| **T2** | Récurrences complètes | L | T0 | S5 | 5.4 : 2 | 🔲 |
| **T3** | Vues et métadonnées : année, agenda, libellés, formulaire | L | T1, M1 | S5 | 4.9 : 2 ; 5.4 : 4 | 🔲 |
| **T4** | Fuseaux horaires, rappels, iCalendar | L | T2, E6 | S5 | 5.4 : 3 | 🔲 |
| **T5** | Liaison par mappings : agenda et Gantt | M | F2 | S5 | 5.4 : 1 ; 5.5 : 1 | 🔲 |
<!-- fin -->

## 3. Détail des lots

### T0 : caractérisation (S)

**Objet.** Figer l'agenda et le roster avant T1 et T2 : vues, glisser, récurrences, navigation, événements annulables.

### T1 : agenda à ressources et vue chronologique (XL)

**Contenu.**
- Le groupement par ressource dans toutes les vues, sur le modèle partagé (L10).
- La vue chronologique.
- L'arbre et le filtre de ressources.
- Le navigateur de dates couplé à l'agenda.

**Acceptation.** Des tests du moteur (couloirs par ressource), des tests d'interaction et des planches.

### T2 : récurrences complètes (L)

**Contenu.**
- Les récurrences annuelles.
- Les exceptions.
- La modification et le détachement d'une occurrence.
- Le glisser sur plusieurs jours.

**Acceptation.** Des tests du moteur d'expansion, et des tests d'interaction.

### T3 : vues et métadonnées (L)

**Contenu.**
- La vue année et la vue agenda (liste).
- Les libellés, statuts et catégories.
- Le formulaire d'édition de rendez-vous, sur le Layout Control (M1) et les éditeurs.

**Acceptation.** Des tests headless et des planches.

### T4 : fuseaux horaires, rappels, iCalendar (L)

**Contenu.**
- Les fuseaux : affichage dans un fuseau donné, et deux fuseaux côte à côte.
- Les rappels, avec notification par W5.
- L'import et l'export iCalendar (RFC 5545).

**Acceptation.** Des allers-retours iCalendar, un passage d'heure d'été et des fuseaux, testés en xUnit.

### T5 : liaison par mappings (M)

**Contenu.** Des correspondances de champs entre les objets de l'application et les rendez-vous de l'agenda ou les tâches du Gantt.

**Acceptation.** Un ViewModel témoin se lie sans adaptation, dans les deux sens.

## 4. Projection

### 4.9 Planification

<!-- généré:PROJ:4.9 -->
| Fonction | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Agenda | 🟡 | T1 | S5 | ✅ |
| Vue chronologique par ressources | 🟡 | T1 | S5 | ✅ |
| Vue année | ❌ | T3 | S5 | ✅ |
| Vue agenda (liste) | ❌ | T3 | S5 | ✅ |
| Vue Gantt de l'agenda | ❌ | Q7 | — | ⚪ proposé |
| Navigateur de dates | 🟡 | T1 | S5 | ✅ |
| Arbre ou filtre de ressources | ❌ | T1 | S5 | ✅ |
| Gantt de projet | 🟡 | L21 | S5 | ✅ |
| Feuille de ressources | ❌ | L10 | S5 | ✅ |
| Chronologie du projet | ❌ | L23 | S6 | ✅ |
<!-- fin -->

### 5.4 Agenda

<!-- généré:PROJ:5.4 -->
| Capacité | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Vue jour | ✅ | — | — | inchangé |
| Vues semaine ouvrée et semaine | ✅ | — | — | inchangé |
| Vue mois | ✅ | — | — | inchangé |
| Vue timeline | 🟡 | T1 | S5 | ✅ |
| Vue année | ❌ | T3 | S5 | ✅ |
| Vue agenda (liste) | ❌ | T3 | S5 | ✅ |
| Vue Gantt de l'agenda | ❌ | Q7 | — | ⚪ proposé |
| Ressources, groupement par ressource | ❌ | T1 | S5 | ✅ |
| Récurrences | 🟡 | T2 | S5 | ✅ |
| Modification d'une seule occurrence | ❌ | T2 | S5 | ✅ |
| Bandeau « toute la journée » | ✅ | — | — | inchangé |
| Ligne « maintenant », heures non ouvrées ombrées | ✅ | — | — | inchangé |
| Glisser et redimensionner, événements annulables | ✅ | — | — | inchangé |
| Formulaire d'édition de rendez-vous | ❌ | T3 | S5 | ✅ |
| Rappels | ❌ | T4 | S5 | ✅ |
| Fuseaux horaires | ❌ | T4 | S5 | ✅ |
| Import et export iCalendar | ❌ | T4 | S5 | ✅ |
| Synchronisation Google ou Microsoft | ❌ | Q7 | — | ⚪ proposé |
| Libellés, statuts, catégories | ❌ | T3 | S5 | ✅ |
| Navigateur de dates couplé | 🟡 | T1 | S5 | ✅ |
| Liaison par mappings | ❌ | T5 | S5 | ✅ |
| Lecture seule | ✅ | — | — | inchangé |
| Impression | ❌ | I4 | S4 | ✅ |
<!-- fin -->

### Lignes d'autres tableaux

<!-- généré:AUTRES:T:4.9, 5.4 -->
| Tableau | Ligne | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|---|
| 5.5 | Liaison de données (mappings) | ❌ | T5 | S5 | ✅ |
<!-- fin -->

## 5. Risques

- **T1 est de taille XL** et touche le modèle de l'agenda. T0 doit passer d'abord.
- **Le modèle de ressources partagé (Q13)** lie le calendrier de l'agenda à celui du Gantt. Si Q13 est refusée, T1 garde son propre modèle et ne dépend plus de L10.
