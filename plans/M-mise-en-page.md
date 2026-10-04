# Chantier M : mise en page

Chantier du plan de la suite (`../Plan-Suite.md`). Il couvre le tableau 4.6 du benchmark. État au 04/10/2026. Statut : proposition.

## 0. Point de départ

<!-- généré:SCORES:4.6 -->
| Tableau | Lignes | Aujourd'hui | S1 | S2 | S3 | S4 | S5 | S6 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 4.6 Mise en page | 6 | 33 % | 33 % | 33 % | 83 % | 100 % | 100 % | 100 % |
<!-- fin -->

- **Couvert** : le groupe titré (HDevCard) et les conteneurs de base d'Avalonia (pile, tableau, défilement, séparateur, déroulant).
- **Manquent** :
  - le Layout Control, qui aligne les formulaires et se personnalise à l'exécution ;
  - le formulaire généré depuis les données ;
  - le panneau latéral redimensionnable ;
  - le panneau ou dialogue volant.
- **Aujourd'hui**, un formulaire se bâtit avec le `Grid` d'Avalonia, sans alignement automatique des libellés.

## 1. Décisions proposées

- **Le Layout Control repose sur le socle des éditeurs.**
  - Il reconnaît un éditeur (`IHDevEditor`) et lui associe un libellé.
  - Il aligne les libellés d'un groupe et place l'erreur de validation.
  - Il dépend donc de Q3 (`Conception-Socle-Editeurs.md`, § 10) et d'E3.
- **La personnalisation à l'exécution** déplace et regroupe les éléments dans l'overlay, comme le docking. Le layout s'enregistre par F6.
- **Le formulaire généré** prend l'éditeur qui correspond au type de chaque propriété : c'est la règle « un éditeur par type » de D4, proposée en Q3. Les annotations de données fournissent les libellés, l'ordre et la validation.
- **Le panneau volant** reprend la pièce popup du socle (E2), avec sa garde, son retournement et son clavier.

## 2. Lots

<!-- généré:LOTS:M -->
| Lot | Objet | Taille | Dépend de | Jalon | Lignes visées | Statut |
|---|---|---|---|---|---|---|
| **M1** | Layout Control : mise en page de formulaire | XL | E3, Q3 | S3 | 4.6 : 1 | 🔲 |
| **M2** | Panneau latéral et panneau volant | M | E2 | S3 | 4.6 : 2 | 🔲 |
| **M3** | Formulaire généré depuis les données | L | M1, E6 | S4 | 4.6 : 1 | 🔲 |
<!-- fin -->

## 3. Détail des lots

### M1 : Layout Control (XL)

**Contenu.**
- Des éléments qui portent un libellé et un éditeur, des groupes titrés, des onglets et des colonnes.
- L'alignement automatique des libellés et un espacement uniforme.
- Le redimensionnement.
- La personnalisation à l'exécution : masquer, déplacer, regrouper, et restaurer.
- La sérialisation du layout.

**Acceptation.**
- Des tests de disposition (alignement, colonnes) en xUnit.
- La personnalisation testée en headless.
- Des planches en clair et en sombre.

### M2 : panneau latéral et panneau volant (M)

**Contenu.**
- Un panneau latéral redimensionnable, repliable et ancré à un bord.
- Un panneau volant ancré à un contrôle, qui sert aussi de petit dialogue sur la pièce popup d'E2.

**Acceptation.** Des tests d'interaction, dont le clic extérieur et Échap, et la vérification sur le bureau réel.

### M3 : formulaire généré depuis les données (L)

**Contenu.**
- La génération à partir d'un type : éditeur par type de propriété, libellés, ordre, groupes et validation par annotations.
- Le formulaire se régénère quand le type change.

**Acceptation.** Un ViewModel témoin donne un formulaire complet, lié dans les deux sens et validé.

## 4. Projection

<!-- généré:PROJ:4.6 -->
| Fonction | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Mise en page de formulaire | ❌ | M1 | S3 | ✅ |
| Formulaire généré depuis les données | ❌ | M3 | S4 | ✅ |
| Groupe titré, panneau | ✅ | — | — | inchangé |
| Conteneurs de base (pile, tableau, défilement, séparateur) | ✅ | — | — | inchangé |
| Panneau latéral redimensionnable | ❌ | M2 | S3 | ✅ |
| Panneau ou dialogue volant | ❌ | M2 | S3 | ✅ |
<!-- fin -->

## 5. Risques

- **M1 est le plus gros lot de S3, et il attend Q3.** Un retard sur le socle des éditeurs le décale d'autant.
- **La personnalisation à l'exécution** partage avec le docking (A1) le glisser dans l'overlay. Il faut factoriser le mécanisme plutôt que l'écrire deux fois.
