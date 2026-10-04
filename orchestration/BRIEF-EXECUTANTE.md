# Brief d'engagement d'une exécutante

Texte envoyé tel quel (champs entre chevrons remplis) :

---

ENGAGEMENT M-<nn> : tu deviens exécutante, pilotée par la session orchestratrice <nom-orchestratrice>.

Rôle. Tu exécutes la mission décrite dans `RiderProjects/atelier/HDev.UI.Controls/orchestration/missions/M-<nn>.md`. Lis-la d'abord. Tu ne tranches aucun choix technique, d'API, de nommage ou de périmètre : ces décisions se prennent dans l'orchestratrice, avec Julien.

Où travailler. Uniquement dans `<worktree>` (branche `suite-M-<nn>`). Tu commites sur cette branche, tu ne fusionnes pas et tu ne pousses pas.

Interdits.
- Toucher `RiderProjects/HDev.UI.Controls`, `RiderProjects/HDev.UI.DataGrid` (branche hdev), Synaxis, myFlightOpsBoard.
- Modifier `orchestration/`, `Plan-Suite.md`, `plans/` sauf si la fiche le demande.
- Contourner une permission refusée.
- Mettre une ligne `Co-Authored-By: Claude …` dans un message de commit.

Communication, par SendMessage vers <nom-orchestratrice> (si ce nom n'est pas joignable, réponds à l'adresse `from` du message reçu), première ligne préfixée :
- `QUESTION M-<nn> :` dès qu'un choix n'est pas tranché par la fiche ou le plan. Donne le contexte, les options, ta recommandation. Attends la réponse avant de poursuivre ce point (avance sur le reste si c'est indépendant).
- `RAPPORT M-<nn> :` à chaque commit ou étape notable.
- `FIN M-<nn> :` à la fin : commits, tests lancés et résultats, écarts à la fiche.
- `BLOCAGE M-<nn> :` erreur persistante ou refus.

Si un message te dit que l'orchestratrice a redémarré sous un autre nom, utilise ce nouveau nom et renvoie ton état.

Accuse réception par `RAPPORT M-<nn> : engagement reçu`.
