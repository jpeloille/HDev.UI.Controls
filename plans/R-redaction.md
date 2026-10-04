# Chantier R : rédaction (texte riche)

Chantier du plan de la suite (`../Plan-Suite.md`). Il couvre les tableaux 4.11 et 5.6 du benchmark. État au 04/10/2026. Statut : proposition.

## 0. Point de départ

<!-- généré:SCORES:4.11, 5.6 -->
| Tableau | Lignes | Aujourd'hui | S1 | S2 | S3 | S4 | S5 | S6 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 4.11 Documents | 6 | 25 % | 25 % | 25 % | 25 % | 25 % | 50 % | 50 % |
| 5.6 Texte riche | 23 | 39 % | 39 % | 39 % | 39 % | 54 % | 91 % | 91 % |
<!-- fin -->

- **HDevRichEdit** est un éditeur sur un cœur pur et testé.
  - Mise en forme des caractères, titres, listes à puces et numérotées.
  - Alignement, liens, images en blocs.
  - Annuler et rétablir, presse-papiers riche, raccourcis.
- **HDevHtmlView** affiche des documents HTML, assainis par construction.
- **Le format de stockage** est le HTML de TipTap, figé sous le nom `storage-v1` (décision du 19/07/2026).
- **Manquent** :
  - les tableaux éditables ;
  - les images dans le fil du texte ;
  - les styles nommés ;
  - les listes à niveaux ;
  - la mise en page et la vue page ;
  - les formats DOCX, RTF et ODT ;
  - le PDF ;
  - la recherche ;
  - la révision ;
  - l'orthographe.
- **Le tableur, la visionneuse PDF et l'API de documents** sont des absences actées, à reconfirmer (Q8).

## 1. Décisions proposées

- **Le HTML `storage-v1` reste le format pivot.** DOCX, ODT et RTF sont des convertisseurs en entrée et en sortie (Q5), testés par aller-retour sur un corpus.
- **La vue page** s'appuie sur la mise en page du moteur d'impression (I1). Un même calcul de pages sert l'écran et le papier.
- **L'orthographe** s'appuie sur une bibliothèque libre de dictionnaires (licence à vérifier, Q9), avec les dictionnaires du système.
- **Absence proposée (Q7)** : les champs et le publipostage, une fonction d'application plutôt que d'éditeur.

## 2. Lots

<!-- généré:LOTS:R -->
| Lot | Objet | Taille | Dépend de | Jalon | Lignes visées | Statut |
|---|---|---|---|---|---|---|
| **R0** | Caractérisation du texte riche | S | — | S4 | — | 🔲 |
| **R1** | Édition courante : rechercher, styles nommés, listes à niveaux, ruban intégré | M | R0, C3 | S4 | 5.6 : 4 | 🔲 |
| **R2** | Tableaux et images dans le fil du texte | L | R0 | S5 | 5.6 : 2 | 🔲 |
| **R3** | Mise en page, vue page, règle et tabulations | XL | R2, I1 | S5 | 4.11 : 1 ; 5.6 : 3 | 🔲 |
| **R4** | Formats DOCX, ODT et RTF | L | R3, Q5 | S5 | 5.6 : 1 | 🔲 |
| **R5** | Révision : commentaires, suivi, protection, orthographe | L | R0 | S5 | 4.11 : 1 ; 5.6 : 3 | 🔲 |
<!-- fin -->

## 3. Détail des lots

### R0 : caractérisation (S)

**Objet.** Figer l'édition, le presse-papiers et l'aller-retour `storage-v1` avant R1 et R2. Le moteur de documents a déjà ses tests xUnit ; cette caractérisation y ajoute l'interaction.

### R1 : édition courante (M)

**Contenu.**
- Rechercher et remplacer.
- Les styles nommés.
- Les listes à plusieurs niveaux (Tab).
- Le ruban intégré, sur le chantier C (C3).

**Acceptation.** Des tests du moteur et des tests d'interaction.

### R2 : tableaux et images dans le fil du texte (L)

**Contenu.**
- L'insertion de tableaux, avec lignes, colonnes, fusion et largeurs.
- Les images dans le fil du texte et flottantes.

**Acceptation.** Des tests du moteur, et un aller-retour `storage-v1` à point fixe.

### R3 : mise en page, vue page, règle et tabulations (XL)

**Contenu.**
- Les sections, marges, colonnes, en-têtes et pieds de page.
- La vue page et l'impression (I1).
- La règle et les tabulations.

**Acceptation.** Des tests de pagination, et une comparaison entre l'écran et le PDF.

### R4 : formats DOCX, ODT et RTF (L)

**Contenu.** L'import et l'export, sur un corpus de documents témoins.

**Acceptation.** Les allers-retours sont mesurés, et les pertes documentées par fonction.

### R5 : révision (L)

**Contenu.**
- Les commentaires.
- Le suivi des modifications.
- La protection de zones.
- Le correcteur orthographique.

**Acceptation.** Des tests du moteur, et des tests d'interaction.

## 4. Projection

### 4.11 Documents

<!-- généré:PROJ:4.11 -->
| Fonction | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Traitement de texte | 🟡 | R3 | S5 | ✅ |
| Mémo riche léger | ✅ | — | — | inchangé |
| Tableur | ⚪ | acté | — | inchangé |
| Visionneuse PDF | ⚪ | Q8 | — | inchangé |
| Correcteur orthographique | ❌ | R5 | S5 | ✅ |
| API de documents (Word, Excel, PDF) | ⚪ | acté | — | inchangé |
<!-- fin -->

### 5.6 Texte riche

<!-- généré:PROJ:5.6 -->
| Capacité | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Mise en forme des caractères | ✅ | — | — | inchangé |
| Titres, styles de paragraphe | 🟡 | R1 | S4 | ✅ |
| Listes | 🟡 | R1 | S4 | ✅ |
| Alignement, justification | ✅ | — | — | inchangé |
| Liens | ✅ | — | — | inchangé |
| Images | 🟡 | R2 | S5 | ✅ |
| Tableaux | ❌ | R2 | S5 | ✅ |
| Annuler, rétablir | ✅ | — | — | inchangé |
| Presse-papiers riche | ✅ | — | — | inchangé |
| Import et export HTML | ✅ | — | — | inchangé |
| DOCX, RTF, ODT | ❌ | R4 | S5 | ✅ |
| Export PDF | ❌ | I4 | S4 | ✅ |
| Mise en page (sections, marges, colonnes, en-têtes et pieds de page) | ❌ | R3 | S5 | ✅ |
| Vue page, impression | ❌ | R3 | S5 | ✅ |
| Champs, publipostage | ❌ | Q7 | — | ⚪ proposé |
| Commentaires, suivi des modifications | ❌ | R5 | S5 | ✅ |
| Rechercher, remplacer | ❌ | R1 | S4 | ✅ |
| Orthographe | ❌ | R5 | S5 | ✅ |
| Protection du document | ❌ | R5 | S5 | ✅ |
| Règle, tabulations | ❌ | R3 | S5 | ✅ |
| Barre d'outils ou ruban intégré | 🟡 | R1 | S4 | ✅ |
| Raccourcis clavier | ✅ | — | — | inchangé |
| IA (assistant) | ❌ | Q6 | — | ⚪ proposé |
<!-- fin -->

## 5. Risques

- **R3 et R4 sont lourds.** La fidélité DOCX est un puits sans fond : il faut fixer un corpus et un seuil d'acceptation avant de commencer.
- **Une évolution du format `storage-v1`** suit sa propre règle : vocabulaire figé, et toute divergence est un bogue.
