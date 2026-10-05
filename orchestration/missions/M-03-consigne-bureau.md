# M-03 : consigne de vérification sur le bureau réel

Texte destiné à une conversation de l'IDE. **Julien l'autorise expressément à piloter son bureau réel** (clics, Alt+Tab, captures) pour cette vérification, et à rendre compte à l'orchestratrice `hdev-orchestration-agent-teams` par SendMessage.

## À lire d'abord

- `missions/M-03.md`, dans ce même dossier.
- `/tmp/hdev-m03/LISEZMOI.md` : le banc et les outils de la session précédente, avec l'injecteur RemoteDesktop.

## La règle testée (option b)

Un popup dessiné dans la fenêtre, ce qui est le cas de cette démo, **reste ouvert** quand la fenêtre perd le focus, quelle qu'en soit la durée. Il se ferme sur un clic dans la fenêtre hors de lui, sur Échap ou sur un choix.

## Lancement

```
cd /home/jpeloille/RiderProjects/atelier/missions/M-01/HDev.UI.Controls   # d2dd2d3, ne rien modifier
dotnet run --project HDev.UI.Controls.Demo
```

## Garde-fous

- Ne jamais envoyer d'entrée aux fenêtres de myFlightOpsBoard.
- Vérifier la fenêtre active avant chaque action.
- S'arrêter si la souris bouge.
- Pas de xdotool : il passe par le portail EI, qui ouvrirait une demande à l'écran. Passer par RemoteDesktop de mutter.

## Popups et étapes

Les popups :
- le calendrier de DateEdit et le LookUpEdit (carte « Editors — masque, validation, lookup ») ;
- la ComboBox ;
- le menu File ;
- le menu contextuel (onglet « Roster » de la carte TabControl, clic droit sur un bloc).

Pour chacun :
1. Ouverture par un clic réel.
2. Alt+Tab aller-retour : **il reste ouvert.**
3. Clic dans la fenêtre hors du popup : **il se ferme.**
4. Clic dans une autre fenêtre (pas myFlightOpsBoard), puis retour : **il reste ouvert**, puis se ferme au clic hors du popup.
5. Pour le menu contextuel : Alt+Tab aller-retour, puis Échap : **il se ferme.**
6. Choix d'une date, d'un pays : **le choix est pris** et le popup se ferme.

Une capture avant et après chaque étape, dans `/home/jpeloille/RiderProjects/atelier/missions/M-03/captures/bureau-reel-d2dd2d3/`.

## Fin

- Fermer la démo et rendre le bureau tel qu'on l'a trouvé.
- Aucun commit.
- Envoyer à `hdev-orchestration-agent-teams` un message qui commence par « FIN M-03 : », avec le tableau popup × étape, les chemins des captures et les écarts. En cas de blocage, commencer par « BLOCAGE M-03 : ».
