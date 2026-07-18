# Format de stockage du texte riche — dialecte HTML « storage-v1 » (TipTap)

> **Source de vérité exécutable : `HtmlVocabulary.cs`. Toute divergence entre ce
> document et la classe est un bug.** Version figée : `storage-v1`. Ne pas
> étendre sans bump de version (v2, etc.).

## Pourquoi

Le modèle natif de TipTap est un document JSON (ProseMirror) ; `getHTML()` en
est une sérialisation **fermée et prévisible** : elle ne contient que ce que les
extensions activées savent produire — pas de soupe de balises arbitraire. Ce
HTML est donc un bon format de stockage, à condition d'en figer le vocabulaire.
Côté ProControls, la même pile sert trois canaux avec trois listes distinctes :

- **vocabulaire ACCEPTÉ** (parseur tolérant, profil `Mail`) — inchangé, pour le
  lecteur de mails (ProHtmlView) et le collage riche ;
- **vocabulaire STRICT** (`storage-v1`, ce document) — validé par
  `HtmlParser.Parse(html, HtmlParseOptions.Storage)` : diagnostics, jamais
  d'exception, jamais de perte de contenu ;
- **vocabulaire ÉMIS** (`HtmlSerializer`) — sous-ensemble strict du précédent :
  la sortie de `ProRichEdit.Html` est toujours du storage-v1 propre.

## Vocabulaire

### Blocs

| Balise | Attributs | CSS (`style=""`) | Structure |
|---|---|---|---|
| `p` | — | `text-align: center\|right\|justify` | inlines |
| `h1`…`h6` | — | `text-align` idem | inlines |
| `ul`, `ol` | — | — | `li` uniquement |
| `li` | — | — | un `p` puis éventuellement `ul`/`ol` (imbrication TipTap) |
| `blockquote` | — | — | blocs |
| `pre` | — | — | exactement un `code` (forme CodeBlock TipTap ; `pre` nu = diagnostic) |
| `hr` | — | — | void |
| `table`, `tbody`, `tr` | — | — | **statut « compat »** : lecture/round-trip, pas d'édition avant la v3 tables |
| `th`, `td` | `colspan` | — | statut « compat » ; contiennent des blocs |

### Inlines / marques

| Balise | Attributs | CSS (`style=""`) |
|---|---|---|
| `strong`, `em`, `u`, `s`, `code` | — | — |
| `span` | — | `color`, `font-size` (px) |
| `mark` | `data-color` | `background-color` |
| `a` | `href` (obligatoire), `target`, `rel` | — |
| `img` | `src`, `alt`, `title`, `width`, `height` | — |
| `br` | — | — |

### Formes canoniques (sortie `HtmlSerializer`)

- Couleurs : hex minuscule `#rrggbb` (en entrée, `rgb(r, g, b)` de TipTap
  accepté aussi).
- `text-align:center` sans espace (TipTap émet `text-align: center` avec
  espace ; les deux graphies sont acceptées en entrée).
- Liens : `<a target="_blank" rel="noopener noreferrer nofollow" href="…">` —
  constantes de la spec (celles de l'extension Link), non stockées dans le
  modèle.
- Listes : `<ul><li><p>…</p></li></ul>` ; sous-liste en dernière position de
  son `<li>`.
- Bloc de code : `<pre><code>…</code></pre>`, contenu échappé.
- Paragraphe vide : `<p></p>` (matérialisé au parse — une ligne vide survit au
  round-trip).

## Hors vocabulaire strict (mais toujours acceptés par le profil Mail)

`b`, `i`, `strike`, `del`, `div`, `font`, `sub`, `sup`, `small`, `big`,
`font-family`, `rowspan`, `colgroup`/`col`/`colwidth`, tout attribut
d'événement (`on*`), `class`, `id`, CSS de mise en page (`margin`, `position`,
`float`…). En mode strict ils produisent un diagnostic (`DisallowedTag` /
`DisallowedAttribute` / `DisallowedCssProperty`) mais le texte est toujours
conservé dans le document.

`font-family` : asymétrie assumée — parsé et rendu (canal Mail), jamais
sérialisé. Passera au vocabulaire v2 le jour où la toolbar l'offre (extension
TipTap FontFamily côté web).

## Exigences côté TipTap

Extensions requises pour produire exactement ce dialecte : **StarterKit**
(p, h1-h6, listes, blockquote, codeBlock, hr, br, bold→`strong`, italic→`em`,
strike→`s`, code), **Underline** (`u`), **Link** (`a` + target/rel par défaut),
**TextStyle + Color** (`span style="color"`), **Highlight** en mode
`multicolor` (`mark data-color`), **TextAlign** (p + headings), **FontSize**
(via TextStyle, `span style="font-size"`), **Image** — avec
`Image.extend({ addAttributes })` pour déclarer `width`/`height`/`title`,
sinon ProseMirror jette ces attributs au chargement.

TipTap Table est une extension séparée : non requise ; les tables du
vocabulaire « compat » servent à ne jamais détruire un contenu venu du canal
Mail (répondre à un mail → sauvegarde).
