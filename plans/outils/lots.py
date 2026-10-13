# Lots : id -> (chantier, objet, taille, dépend de, jalon, statut)
LOTS = {}
def lot(ch, id, objet, taille, dep, jalon, statut='🔲'):
    LOTS[id] = (ch, objet, taille, dep, jalon, statut)

# E — Éditeurs (Plan-Editeurs.md fait foi ; J1 → S1, J2 → S2, J3 → S3, J4 → S4)
for id, obj, t, dep, j, st in [
    ('E0', 'Tests de caractérisation des éditeurs', 'S', '—', 'S1', '✅'),
    ('E1', 'Conception du socle, contrat de valeur et conventions', 'M', 'E0', 'S1', '✅'),
    ('E2', 'Socle composite : zone de valeur, boutons, popup, multiligne', 'L', 'E1', 'S1', '✅'),
    ('E3', 'Portage des éditeurs existants sur le socle', 'L', 'E2', 'S1', '✅'),
    ('E4', 'Sélection : cases, items à gabarit, liste à cases', 'M', 'E3', 'S2', '🔲'),
    ('E5', 'Saisie typée : masques typés, formats numériques, calculatrice', 'M', 'E3', 'S2', '🔲'),
    ('E6', 'Date et heure : types modernes, durées, plages, créneaux', 'L', 'E3', 'S2', '🔲'),
    ('E7', 'Lookups : saisie et filtre, recherche, multi-valeurs', 'M', 'E3', 'S2', '🔲'),
    ('E8', 'Validation centralisée', 'S', 'E1, E3', 'S3', '🔲'),
    ('E9', 'Compléments : mémo, lien, couleur, chemin, curseur de plage', 'M', 'E2', 'S4', '🔲'),
    ('E10', 'Compléments secondaires : MRU, curseur de zoom, notation, image', 'M', 'E2', 'S4', '🔲'),
    ('E11', 'Pont grille : éditeurs HDev dans HDevDataGrid, sur deux dépôts', 'M', 'E3, G1', 'S3', '🔲'),
    ('E12', 'Accessibilité des éditeurs', 'M', 'E3', 'S3', '🔲')]:
    lot('E', id, obj, t, dep, j, st)

# F — Fondations transverses
lot('F', 'F0', 'Caractérisation des contrôles à collections', 'S', '—', 'S1')
lot('F', 'F1', 'Contrat des collections : ItemsSource, ItemTemplate, sélection liable', 'L', 'F0, E1', 'S1')
lot('F', 'F2', 'Modèles et documents liables', 'M', 'E1', 'S1')
lot('F', 'F3', 'Localisation : mécanisme de ressources, français et anglais', 'M', '—', 'S2')
lot('F', 'F4', "Banque d\'icônes et API d\'icône commune", 'M', 'Q10', 'S2')
lot('F', 'F5', 'Accessibilité de toute la suite', 'L', '—', 'S3')
lot('F', 'F6', "Persistance de l\'espace de travail", 'M', 'G2', 'S3')
lot('F', 'F7', 'Comportements attachables et transitions', 'M', '—', 'S4')
lot('F', 'F8', 'Thèmes : contraste élevé, densité, concepteur de palettes', 'L', '—', 'S5')
lot('F', 'F9', 'Droite-à-gauche', 'L', 'F1', 'S5')
lot('F', 'F10', 'Qualification Windows et macOS', 'M', '—', 'S5')
lot('F', 'F11', 'Conception visuelle : données d\'exemple et prévisualisation', 'S', 'F1', 'S5')
# Lots nés des apports des applications (Plan-Apports.md fait foi pour leur détail)
lot('F', 'F12', "Correctifs du socle : contraste des couples d'état, Crisp public", 'S', '—', 'S1')
lot('F', 'F13', "Amorçage et thème vivant : un seul appel, palette jusqu'à la grille, jetons liés depuis le code, tons d'état, chasse fixe", 'M', 'F12', 'S2')
lot('F', 'F14', 'Socle des contrôles dessinés : carte de détail, clavier en deux dimensions, légende, motifs', 'M', 'F12', 'S4')
lot('F', 'F15', 'Outillage de vérification : démo pilotable, états posables par code, planches réutilisables', 'S', '—', 'S1')

# G — Grille et données (HDev.UI.DataGrid fait foi : IMPROVEMENTS.md)
lot('G', 'G0', "Caractérisation de l\'édition dans la grille", 'S', '—', 'S1')
lot('G', 'G1', "Cycle d\'édition fiable", 'M', 'G0', 'S1')
lot('G', 'G2', 'Grille professionnelle : recherche, filtres, validation, ajout, layout', 'L', 'G1', 'S2')
lot('G', 'G3', 'Profondeur : filtres avancés, plages, presse-papiers, mise en forme', 'L', 'G2', 'S3')
lot('G', 'G4', 'Arbre et propriétés : TreeList, grille verticale, inspecteur', 'L', 'G2', 'S3')
lot('G', 'G5', "Finition : formulaire d\'édition, glisser, fusion, colonnes calculées", 'L', 'G3, M1', 'S4')
lot('G', 'G6', 'Volumes et hiérarchies : sources serveur, pagination, master-detail', 'XL', 'G2', 'S5')
lot('G', 'G7', 'Vues alternatives : bandes, fiches, tuiles, Kanban', 'L', 'G2', 'S5')
lot('G', 'G8', 'Tableau croisé dynamique', 'XL', 'G2', 'S5')
lot('G', 'G9', 'Lookup arborescent', 'S', 'G4, E7', 'S5')

# I — Impression et export
lot('I', 'I1', 'Moteur d\'impression paginée, aperçu et PDF ; L9 de la charte UGantt', 'L', 'Q4', 'S1')
lot('I', 'I2', "Formats d\'export : XLSX, PDF, CSV paramétrable", 'M', 'I1, Q5', 'S2')
lot('I', 'I3', 'Adoption : grille et Gantt', 'M', 'I1, I2', 'S2')
lot('I', 'I4', 'Adoption : agenda, texte riche, carte mentale, autres contrôles', 'M', 'I1', 'S4')

# V — Visualisation
lot('V', 'V0', 'Prototype : graphiques sous HDevTheme', 'S', 'Q2', 'S2')
lot('V', 'V1', 'Graphiques, jauges, sparklines, heatmap', 'L', 'V0, I1', 'S2')
lot('V', 'V2', 'Diagrammes et carte mentale v2', 'L', '—', 'S5')
lot('V', 'V3', 'TreeMap, Sunburst, Sankey', 'L', 'V1', 'S5')
lot('V', 'V4', "Sélection de plage sur un aperçu", 'M', 'V1', 'S5')
lot('V', 'V5', "Indicateurs : tuile, jauge linéaire, progression segmentée, sparkline d'intervalles, puce d'état", 'M', 'F13', 'S2')

# C — Commandes (boutons, ruban, barres, menus)
lot('C', 'C0', 'Caractérisation du ruban, des barres et des menus', 'S', '—', 'S4')
lot('C', 'C1', 'Modèle de commandes partagé et construction déclarative', 'L', 'C0, F1, F4', 'S4')
lot('C', 'C2', 'Boutons à menu et partagés, popup de contrôles', 'M', 'C1, E2', 'S4')
lot('C', 'C3', 'Ruban complet', 'XL', 'C1', 'S4')
lot('C', 'C4', 'Barres : débordement, éditeurs, infobulles riches, recherche de commandes', 'L', 'C1, E3', 'S4')
lot('C', 'C5', 'Personnalisation par l\'utilisateur et mini-barre', 'M', 'C3', 'S5')

# M — Mise en page
lot('M', 'M1', 'Layout Control : mise en page de formulaire', 'XL', 'E3, Q3', 'S3')
lot('M', 'M2', 'Panneau latéral et panneau volant', 'M', 'E2', 'S3')
lot('M', 'M3', 'Formulaire généré depuis les données', 'L', 'M1, E6', 'S4')
lot('M', 'M4', "Fiche en lecture : en-tête, ligne d'informations, cartes, sections, ouverture progressive", 'M', 'F13', 'S3')

# N — Navigation
lot('N', 'N0', 'Caractérisation de la navigation', 'S', '—', 'S4')
lot('N', 'N1', 'Arbre complet : liaison, virtualisation, cases, édition', 'L', 'N0, F1', 'S4')
lot('N', 'N2', 'Accordéon à niveaux et menu hamburger', 'M', 'N0', 'S4')
lot('N', 'N3', "Parcours : fil d\'Ariane, étapes, assistant", 'L', '—', 'S4')
lot('N', 'N4', 'Pages à transitions ; onglets à débordement et glisser', 'M', 'F7', 'S4')
lot('N', 'N5', 'Tuiles et carrousel', 'L', '—', 'S5')
lot('N', 'N6', 'Listes riches : groupes à en-tête libre, détail lié, fil chronologique', 'M', 'F1', 'S4')

# A — Ancrage (docking)
lot('A', 'A0', 'Caractérisation du docking', 'S', '—', 'S4')
lot('A', 'A1', 'Phase 2 : glisser, indicateurs d\'ancrage, onglets réordonnables', 'L', 'A0', 'S4')
lot('A', 'A2', 'Phase 3 : panneaux flottants, auto-masquage, épinglage', 'L', 'A1', 'S4')
lot('A', 'A3', "Confort : boutons d\'en-tête, panneaux fermés, Ctrl+Tab", 'M', 'A1', 'S4')
lot('A', 'A4', 'Tableau de bord à widgets', 'M', 'A2', 'S5')

# W — Fenêtres, dialogues, notifications
lot('W', 'W0', 'Caractérisation de HDevWindow', 'S', '—', 'S3')
lot('W', 'W1', 'Dialogue qui héberge une vue', 'M', 'W6', 'S3')
lot('W', 'W2', 'Fenêtre thémée qualifiée', 'M', 'W0', 'S4')
lot('W', 'W3', 'Démarrage et attente', 'M', '—', 'S4')
lot('W', 'W4', "Couche d\'adornement : badges et guides", 'M', '—', 'S4')
lot('W', 'W5', 'Bureau : notifications système, zone de notification', 'M', '—', 'S4')
lot('W', 'W6', "Bandeau dans la page, état d'une source, réserves repliables", 'S', 'F13', 'S3')

# T — Temps : agenda, roster, Gantt (UGantt-charte.md fait foi pour les lots L)
lot('T', 'T0', "Caractérisation de l\'agenda et du roster", 'S', '—', 'S5')
lot('T', 'T1', 'Agenda à ressources et vue chronologique', 'XL', 'T0, F2, L10', 'S5')
lot('T', 'T2', 'Récurrences complètes', 'L', 'T0', 'S5')
lot('T', 'T3', 'Vues et métadonnées : année, agenda, libellés, formulaire', 'L', 'T1, M1', 'S5')
lot('T', 'T4', 'Fuseaux horaires, rappels, iCalendar', 'L', 'T2, E6', 'S5')
lot('T', 'T5', 'Liaison par mappings : agenda et Gantt', 'M', 'F2', 'S5')
lot('T', 'T6', 'Frise de ressources v2 : journée décalée, groupes, couloirs, disponibilité, carte, clavier', 'L', 'T0, F14', 'S5')
lot('T', 'T7', 'Matrice ressources × jours à zones figées', 'L', 'F14, V5, I2', 'S5')
# O — Gantt et gestion de projet, aligné sur MS Project (Q1 du 04/10/2026).
# Les lots L gardent les identifiants et le contenu de UGantt-charte.md ; O0 et O1 sont propres au plan.
lot('O', 'L0', 'Versionnage du schéma JSON', 'S', '—', 'S1', '✅')
lot('O', 'L1', 'Édition structurelle de la table', 'M', '—', 'S1', '✅')
lot('O', 'O0', 'Caractérisation du Gantt avant la refonte du modèle', 'S', 'L1', 'S2')
lot('O', 'O1', 'Extraction du moteur dans un assembly pur (si Q11)', 'M', 'O0, Q11', 'S2')
lot('O', 'L2', 'Modèle de colonnes configurable', 'M', 'L1', 'S2')
lot('O', 'L3', 'Durée, travail et types de tâches ; schéma v2', 'XL', 'O0', 'S2')
lot('O', 'L4', 'Calendriers multiples et granularité horaire', 'XL', 'L3', 'S2')
lot('O', 'L5', 'Contraintes, échéances et inspecteur de dates', 'L', 'L3, L4', 'S3')
lot('O', 'L6', 'Passe avant et arrière complète, marges, chemins critiques multiples', 'M', 'L3, L4', 'S3')
lot('O', 'L7', 'Tâches fractionnées, récurrentes et inactives', 'M', 'L4', 'S3')
lot('O', 'L8', 'Baselines multiples et plans intermédiaires', 'S', 'L3', 'S3')
lot('O', 'L10', 'Modèle de ressources partagé et feuille de ressources', 'L', 'L4', 'S5')
lot('O', 'L11', 'Affectations', 'L', 'L10, L3', 'S6')
lot('O', 'L12', 'Moteur de répartition échelonnée et contours de charge', 'XL', 'L11', 'S6')
lot('O', 'L13', 'Surutilisation et nivellement', 'XL', 'L12', 'S6')
lot('O', 'L14', 'Coûts : tables de taux, coûts fixes, budget', 'M', 'L11', 'S6')
lot('O', 'L15', 'Suivi avancé : réel, restant, date d\'état', 'M', 'L12', 'S6')
lot('O', 'L16', 'Valeur acquise (EVM)', 'M', 'L12, L14, L8', 'S6')
lot('O', 'L17', 'Système de champs natifs et personnalisés', 'L', 'L2', 'S6')
lot('O', 'L18', "Vues d\'usage éditables et histogramme de ressources", 'L', 'L12, L17', 'S6')
lot('O', 'L19', 'Filtres, groupes et tris', 'L', 'L17', 'S6')
lot('O', 'L20', 'Tables et vues composables', 'L', 'L17, L19', 'S6')
lot('O', 'L21', "Mise en forme des barres et couche d\'adornement", 'L', 'L2', 'S5')
lot('O', 'L22', 'Vue réseau PERT', 'M', '—', 'S5')
lot('O', 'L23', 'Vues calendrier, chronologie et planificateur d\'équipe', 'L', 'L11', 'S6')
lot('O', 'L24', 'Interop MS Project XML', 'L', 'L3, L10', 'S6')

# R — Rédaction (texte riche)
lot('R', 'R0', 'Caractérisation du texte riche', 'S', '—', 'S4')
lot('R', 'R1', 'Édition courante : rechercher, styles nommés, listes à niveaux, ruban intégré', 'M', 'R0, C3', 'S4')
lot('R', 'R2', 'Tableaux et images dans le fil du texte', 'L', 'R0', 'S5')
lot('R', 'R3', 'Mise en page, vue page, règle et tabulations', 'XL', 'R2, I1', 'S5')
lot('R', 'R4', 'Formats DOCX, ODT et RTF', 'L', 'R3, Q5', 'S5')
lot('R', 'R5', 'Révision : commentaires, suivi, protection, orthographe', 'L', 'R0', 'S5')
