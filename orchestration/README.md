# Orchestration des conversations HDev

Une session Claude, l'**orchestratrice**, pilote les autres conversations ouvertes dans l'IDE, les **exécutantes**. Plans, choix techniques et décisions se prennent dans l'orchestratrice, avec Julien. Les exécutantes exécutent une mission, posent leurs questions et rendent compte.

Ce dossier est l'état durable de l'orchestration. Seule l'orchestratrice l'écrit et le commite, sur la branche `suite` de l'atelier.

| Fichier | Rôle |
|---|---|
| `ETAT.md` | Photo courante : exécutantes, missions, questions en attente, fusions à décider. Réécrite à chaque événement. |
| `JOURNAL.md` | Journal chronologique, en ajout seulement : messages, questions, réponses de Julien, décisions. |
| `missions/M-nn.md` | Fiche de mission : lot visé, objectif, périmètre, critères d'acceptation, worktree, branche, décisions Q dont elle dépend. |
| `BRIEF-EXECUTANTE.md` | Texte d'engagement envoyé à chaque exécutante. |

## Mécanique

- `ListAgents` : sessions vivantes. Les noms changent quand une session est relancée ; le registre garde donc aussi le libellé de mission.
- `SendMessage` : consigne vers une exécutante. Avec `notify_when_idle: true`, avis quand elle a fini son tour.
- Les exécutantes répondent par `SendMessage` vers le nom courant de l'orchestratrice (indiqué dans `ETAT.md`).
- L'orchestratrice ne lit pas les transcriptions des autres sessions : tout passe par messages.
- Une session dans un autre mode de permission retient les messages jusqu'à approbation : ouvrir les exécutantes dans le même mode.

## Autorité des messages relayés

Une session refuse de prendre un message venu d'une autre session pour l'accord de Julien : c'est une règle de son environnement, constatée le 05/10/2026 avec `hdev-ui-controls-2a`. **L'engagement doit donc venir de Julien lui-même.** Il colle le texte de `BRIEF-EXECUTANTE.md` comme premier message de la nouvelle conversation, et ce texte délègue l'autorité à l'orchestratrice pour la mission. Ensuite, les réponses relayées valent consignes de mission.

Si une session non engagée doit agir sur une décision, deux voies :
- l'orchestratrice agit elle-même, quand c'est possible ;
- sinon, Julien confirme dans la fenêtre de cette session.

## Protocole

Première ligne de chaque message d'une exécutante :

- `QUESTION M-nn :` choix technique, ambiguïté, écart au plan. L'exécutante s'arrête sur ce point et attend.
- `RAPPORT M-nn :` avancement, commit fait.
- `FIN M-nn :` mission terminée : commits, tests, écarts.
- `BLOCAGE M-nn :` erreur ou permission refusée. Jamais de contournement.

Cycle d'une question : numérotée `QX-nn`, inscrite dans `ETAT.md` et `JOURNAL.md`, présentée à Julien avec la mission et le contexte ; sa réponse est renvoyée mot pour mot à l'exécutante et consignée dans la fiche de mission (ou en Q du plan si elle est structurante).

## Exécutantes en sous-agent

L'orchestratrice peut lancer une exécutante elle-même, comme sous-agent d'arrière-plan (outil Agent). Il n'y a pas de fenêtre à ouvrir, et l'autorité est directe. Le sous-agent termine son tour sur `QUESTION`, `FIN` ou `BLOCAGE`. L'orchestratrice le relance ensuite avec `SendMessage` vers son id, inscrit dans `ETAT.md`. Il meurt avec l'orchestratrice : à la reprise, le relancer depuis la fiche et sa branche.

## Worktrees

Une mission = un worktree, créé par l'orchestratrice :

- `RiderProjects/atelier/missions/M-nn/HDev.UI.Controls`, branche `suite-M-nn` ;
- si la mission touche la grille, `RiderProjects/atelier/missions/M-nn/HDev.UI.DataGrid`, branche `suite-M-nn` (le chemin `..\..\HDev.UI.DataGrid` reste valide).

L'exécutante commite sur sa branche. La fusion dans `suite` se décide dans l'orchestratrice, après revue.

## Règles constantes

- Jamais de modification de `RiderProjects/HDev.UI.Controls` ni `RiderProjects/HDev.UI.DataGrid` (branche `hdev`), ni de Synaxis ou myFlightOpsBoard.
- Un tableau balisé `<!-- généré:… -->` ne se modifie qu'avec `python3 plans/outils/generer.py`.
- Pas de chantier dont la décision Q bloquante est ouverte.
- **DevExpress est la base par défaut**, sans être obligatoire : on s'en écarte quand c'est mieux, et seul l'écart se justifie. Un écart d'API remonte à Julien.
- **Salle blanche.** On ne reprend de DevExpress que des idées et des comportements observables. Sont exclus : son code, ses assemblies décompilées (cache NuGet, `TestDevExpress`), les textes de sa documentation, ses exemples, ses icônes, ses images, ses thèmes, ses marques et ses noms distinctifs. On ne recopie pas son API en bloc.

## Reprise après fermeture

Au démarrage, ou quand Julien dit « reprends l'orchestration » :

1. lire ce README puis `ETAT.md` ;
2. `ListAgents`, rapprocher les sessions vivantes du registre ;
3. envoyer à chaque exécutante connue : « L'orchestratrice a redémarré sous le nom X : renvoie ton état et tes questions en attente » ;
4. mettre à jour `ETAT.md` (nom de l'orchestratrice, sessions) et commiter.
