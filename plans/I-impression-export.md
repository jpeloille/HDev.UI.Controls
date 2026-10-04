# Chantier I : impression et export

Chantier du plan de la suite (`../Plan-Suite.md`). Il couvre le tableau 4.12 du benchmark, ainsi que les lignes d'impression et d'export des check-lists 5.1, 5.4, 5.5 et 5.6. État au 04/10/2026. Statut : proposition.

## 0. Point de départ

<!-- généré:SCORES:4.12 -->
| Tableau | Lignes | Aujourd'hui | S1 | S2 | S3 | S4 | S5 | S6 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 4.12 Rapports, impression, export | 4 | 13 % | 13 % | 50 % | 50 % | 50 % | 50 % | 50 % |
<!-- fin -->

- **Aujourd'hui.**
  - Aucune impression, nulle part dans la suite.
  - Export CSV de la grille.
  - Export PNG du Gantt et de la carte mentale, par rendu hors écran.
- **La charte UGantt l'a déjà promue.** L'impression paginée et le PDF (son lot L9) sont « la seule exception » à sa règle de partage : une capacité transverse, dont le Gantt n'est que le premier client. Ce plan reprend L9 sous le nom I1.
- **En face**, chaque contrôle DevExpress s'imprime et s'exporte (PDF, XLSX, DOCX).

## 1. Décisions proposées

- **Un contrat imprimable, commun à tous les contrôles.** Pour une taille de page donnée, un contrôle fournit :
  - son nombre de pages ;
  - le dessin de chaque page dans un `DrawingContext` ;
  - ses en-têtes et pieds répétés.

  L'aperçu, l'impression et le PDF consomment ce seul contrat.
- **PDF vectoriel** (Q4). Piste privilégiée : le moteur PDF de Skia, déjà livré avec Avalonia, qui garde le texte sélectionnable et les traits nets. Une sortie raster reste le repli.
- **Impression sous Linux** (Q4) par le portail XDG, qui donne le dialogue du bureau, ou par CUPS.
- **Formats bureautiques** (Q5). XLSX d'abord, pour la grille : par une bibliothèque libre, ou par un écrivain maison limité aux feuilles, aux formats et aux groupes.
- **La règle moteur et adoption** est dans `../Plan-Suite.md`, § 7 : le moteur ne coche aucune ligne de check-list, l'adoption par chaque contrôle si.

## 2. Lots

<!-- généré:LOTS:I -->
| Lot | Objet | Taille | Dépend de | Jalon | Lignes visées | Statut |
|---|---|---|---|---|---|---|
| **I1** | Moteur d'impression paginée, aperçu et PDF ; L9 de la charte UGantt | L | Q4 | S1 | — | 🔲 |
| **I2** | Formats d'export : XLSX, PDF, CSV paramétrable | M | I1, Q5 | S2 | 4.12 : 1 | 🔲 |
| **I3** | Adoption : grille et Gantt | M | I1, I2 | S2 | 4.12 : 1 ; 5.1 : 2 ; 5.5 : 1 | 🔲 |
| **I4** | Adoption : agenda, texte riche, carte mentale, autres contrôles | M | I1 | S4 | 5.4 : 1 ; 5.6 : 1 | 🔲 |
<!-- fin -->

## 3. Détail des lots

### I1 : moteur d'impression, aperçu et PDF (L), le lot L9 de la charte UGantt

**Contenu.**
- Le contrat imprimable ; la mise en page (format, orientation, marges, en-têtes et pieds avec numéros de page).
- L'aperçu : un contrôle avec zoom, défilement et choix des pages.
- La sortie PDF, et l'impression par le portail ou par CUPS.

**Acceptation.**
- Le PDF produit se relit par un outil tiers : texte extractible, polices incorporées.
- L'aperçu est testé en headless.
- Une page témoin est imprimée sur le bureau réel.

### I2 : formats d'export (M)

**Contenu.**
- XLSX (feuilles, formats de nombre et de date, groupes, totaux).
- PDF par I1.
- CSV paramétrable (séparateur, culture, encodage).

**Acceptation.** Des fichiers témoins relus par une application tierce, et des allers-retours testés en xUnit.

### I3 : adoption par la grille et le Gantt (M)

**Contenu.**
- La grille : en-têtes répétés, groupes et totaux, colonnes visibles, export XLSX de la vue courante.
- Le Gantt : pages en mosaïque (table et diagramme), échelle de temps répétée, export PDF.

**Acceptation.** Les lignes « Export » et « Impression, aperçu » de 5.1, et « Impression, export » de 5.5, passent à ✅. Planches PDF à l'appui.

### I4 : adoption par l'agenda, le texte riche et les autres contrôles (M)

**Contenu.**
- L'agenda : vues jour, semaine et mois paginées.
- Le texte riche : un export PDF simple, en attendant la vue page (R3).
- La carte mentale et les graphiques (V1).

**Acceptation.** La ligne « Impression » de 5.4 et la ligne « Export PDF » de 5.6 passent à ✅.

## 4. Projection

### 4.12 Rapports, impression, export

<!-- généré:PROJ:4.12 -->
| Fonction | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Impression et aperçu de tout contrôle | ❌ | I3 | S2 | ✅ |
| Export de données | 🟡 | I2 | S2 | ✅ |
| Concepteur de rapports | ⚪ | acté | — | inchangé |
| Tableaux de bord BI | ⚪ | acté | — | inchangé |
<!-- fin -->

### Lignes d'autres tableaux

<!-- généré:AUTRES:I:4.12 -->
| Tableau | Ligne | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|---|
| 5.1 | Export | 🟡 | I3 | S2 | ✅ |
| 5.1 | Impression, aperçu | ❌ | I3 | S2 | ✅ |
| 5.4 | Impression | ❌ | I4 | S4 | ✅ |
| 5.5 | Impression, export | 🟡 | I3 | S2 | ✅ |
| 5.6 | Export PDF | ❌ | I4 | S4 | ✅ |
<!-- fin -->

Le concepteur de rapports et les tableaux de bord BI restent des absences actées, à reconfirmer (Q8).

## 5. Risques

- **Le moteur PDF de Skia** est accessible par Avalonia, mais pas par son API publique de haut niveau. Il faudra d'abord un prototype qui mesure la fidélité du texte et des traits (Q4).
- **L'impression sous Linux** dépend du portail du bureau. Elle se vérifie sur GNOME et sur un autre bureau.
- **Chaque contrôle doit savoir se paginer**, et c'est le vrai coût de l'adoption.
