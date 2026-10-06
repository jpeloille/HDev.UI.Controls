# Chantier W : fenêtres, dialogues, notifications

Chantier du plan de la suite (`../Plan-Suite.md`). Il couvre le tableau 4.8 du benchmark. État au 04/10/2026. Statut : proposition.

## 0. Point de départ

<!-- généré:SCORES:4.8 -->
| Tableau | Lignes | Aujourd'hui | S1 | S2 | S3 | S4 | S5 | S6 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 4.8 Fenêtres, dialogues, notifications | 13 | 38 % | 38 % | 38 % | 46 % | 73 % | 73 % | 73 % |
<!-- fin -->

- **Couvert** :
  - boîte de message ;
  - boîte de saisie ;
  - dialogues de fichiers, par le sélecteur natif du bureau ;
  - alertes dans l'application (HDevToast).
- **HDevWindow** prend les décorations natives de GNOME par défaut, un choix dicté par Wayland. Le chrome maison reste une voie secondaire.
- **Manquent** :
  - le dialogue qui héberge une vue ;
  - l'écran de démarrage et l'indicateur d'attente (seule la grille a un voile de chargement) ;
  - la couche d'adornement : les badges ne s'y posent pas sur un contrôle, et les guides n'existent pas ;
  - les notifications du bureau ;
  - la zone de notification.

## 1. Décisions proposées

- **Les décorations natives restent la règle.** Le chrome maison devient une option qualifiée, et non plus un chemin secondaire.
- **Absences proposées (Q7)** :
  - le ruban et les commandes dans la barre de titre, contraires au chrome natif ;
  - la fenêtre Fluent, acrylique et propre à Windows ;
  - l'explorateur de fichiers embarqué, puisque le portail du bureau couvre le choix de fichiers.
- **Les notifications passent par le portail XDG.** La zone de notification passe par le `TrayIcon` d'Avalonia, habillé.
- **Le dialogue qui héberge une vue suit MVVM** : un service de dialogue, un résultat typé, la validation de la vue (E8) avant fermeture.

## 2. Lots

<!-- généré:LOTS:W -->
| Lot | Objet | Taille | Dépend de | Jalon | Lignes visées | Statut |
|---|---|---|---|---|---|---|
| **W0** | Caractérisation de HDevWindow | S | — | S3 | — | 🔲 |
| **W1** | Dialogue qui héberge une vue | M | W6 | S3 | 4.8 : 1 | 🔲 |
| **W2** | Fenêtre thémée qualifiée | M | W0 | S4 | 4.8 : 1 | 🔲 |
| **W3** | Démarrage et attente | M | — | S4 | 4.8 : 1 | 🔲 |
| **W4** | Couche d'adornement : badges et guides | M | — | S4 | 4.8 : 1 | 🔲 |
| **W5** | Bureau : notifications système, zone de notification | M | — | S4 | 4.8 : 2 | 🔲 |
| **W6** | Bandeau dans la page, état d'une source, réserves repliables | S | F13 | S3 | — | 🔲 |
<!-- fin -->

## 3. Détail des lots

### W0 : caractérisation de HDevWindow (S)

**Objet.** Figer le comportement avant W2 : décorations natives et maison, restauration, thème, raccourcis.

### W1 : dialogue qui héberge une vue (M)

**Contenu.**
- HDevDialog : une vue, des boutons configurables, un résultat typé, l'ouverture asynchrone.
- Un service de dialogue pour les ViewModels.
- La validation avant fermeture.

**Acceptation.** Des tests headless (ouverture, validation refusée, résultat) et des planches.

**Apports des applications** (`../Plan-Apports.md`) :
- **Le dialogue à action asynchrone (APP-71) :**
  - l'action s'exécute dans le dialogue, et le bouton est désactivé pendant l'appel ;
  - un refus s'affiche dans le bandeau (W6), et la saisie est conservée ;
  - Échap ferme le dialogue, et la taille suit le contenu.
- **La fenêtre de détail (APP-72) :** elle se complète sur place à mesure que les calculs aboutissent.
- **Le toast (APP-73) :** il s'émet depuis n'importe quel contrôle, qui résout seul sa fenêtre.

### W2 : fenêtre thémée qualifiée (M)

**Contenu.**
- Le chrome maison complet en option : déplacement, redimensionnement, boutons, mode sombre.
- La restauration d'état, y compris la fenêtre maximisée avant affichage.
- Le suivi du thème.

**Acceptation.** La vérification sur GNOME, en X11 et sous XWayland, et des planches.

**Apports des applications** (`../Plan-Apports.md`). Une fenêtre maximisée avant l'affichage fait disparaître le panneau de bord du docking : ce cas entre dans l'acceptation (APP-74).

### W3 : démarrage et attente (M)

**Contenu.**
- Un écran de démarrage, avec progression.
- Un indicateur d'attente.
- Un voile de chargement générique sur n'importe quel contrôle.

**Acceptation.** Des tests headless et des planches.

**Apports des applications** (`../Plan-Apports.md`) :
- **La fenêtre d'échec au démarrage (APP-75) :** elle affiche l'erreur et une aide, et la fermer quitte l'application.
- **La console de tâche longue (APP-76) :**
  - des intentions distinctes ;
  - la durée annoncée avant de lancer ;
  - la progression ;
  - un bilan qui nomme les premiers échecs ;
  - l'annulation.

### W4 : couche d'adornement (M)

**Contenu.**
- Une couche qui pose badges et marqueurs sur n'importe quel contrôle.
- Des guides : une visite guidée qui met en évidence des contrôles avec un texte.

**Acceptation.** Tests de géométrie (position, suivi au redimensionnement) et planches.

### W5 : bureau (M)

**Contenu.**
- Les notifications système, par le portail XDG, avec actions.
- La zone de notification, par `TrayIcon` habillé.

**Acceptation.** Vérification sur le bureau réel. La barre des tâches (progression, listes de raccourcis) n'a pas d'équivalent standard sous Linux, d'où la cible 🟡.

### W6 : bandeau dans la page (S), né des apports des applications

Son détail fait foi dans `../Plan-Apports.md`, § 2. Il comprend :
- le bandeau persistant (information, avertissement, erreur), avec ses couples de contraste garantis (APP-77) ;
- l'état d'une source : à jour, à rafraîchir ou injoignable ; dernière lecture datée, période couverte (APP-78) ;
- les réserves repliables, avec un en-tête compact, un compte et une hauteur maximale (APP-79).

W1 s'en sert pour afficher l'erreur d'un dialogue.

## 4. Projection

<!-- généré:PROJ:4.8 -->
| Fonction | Aujourd'hui | Lot | Jalon | Cible |
|---|---|---|---|---|
| Fenêtre thémée | 🟡 | W2 | S4 | ✅ |
| Ruban ou commandes dans la barre de titre | ❌ | Q7 | — | ⚪ proposé |
| Fenêtre Fluent | ❌ | Q7 | — | ⚪ proposé |
| Boîte de message | ✅ | — | — | inchangé |
| Boîte de saisie | ✅ | — | — | inchangé |
| Dialogue qui héberge une vue | ❌ | W1 | S3 | ✅ |
| Dialogues de fichiers | ✅ | — | — | inchangé |
| Explorateur de fichiers embarqué | ❌ | Q7 | — | ⚪ proposé |
| Alertes dans l'application | ✅ | — | — | inchangé |
| Notifications système | ❌ | W5 | S4 | ✅ |
| Écran de démarrage, attente | ❌ | W3 | S4 | ✅ |
| Badges et guides posés sur un contrôle | 🟡 | W4 | S4 | ✅ |
| Barre des tâches, zone de notification | ❌ | W5 | S4 | 🟡 |
<!-- fin -->

## 5. Risques

- **Les portails** varient selon le bureau. Il faut vérifier sur GNOME et sur au moins un autre.
- **Le chrome maison** a déjà buté sur XWayland : des marges fantômes et des clics décalés. W0 doit couvrir ces cas avant W2.
