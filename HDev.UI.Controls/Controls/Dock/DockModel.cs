using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HDev.UI.Controls;

// ═══════════════════════════════════════════════════════════════════════════
// HDevDock — moteur d'arbre pur (aucune dépendance Avalonia, testable headless)
//
// STANCE « DÉTACHABLE » sous la décision anti-MDI (context.md, 14/07/2026) :
//   Le réarrangement (drag, cibles d'ancrage, aperçu) vivra dans l'OverlayLayer
//   de la fenêtre — JAMAIS via du positionnement de fenêtres OS. C'est ce qui
//   rend ce docking Wayland-natif : il n'a besoin d'aucune position globale.
//   « Détaché » = pop-out en fenêtre SDI ordinaire (le compositeur la place) ;
//   le re-dock se fait par action explicite, pas par tracking de position.
//   Le modèle SAIT que les floats existent (collection Floating, sérialisée)
//   mais la phase 1 n'en crée pas : elle pose l'arbre docké + splitters +
//   onglets + sérialisation. Zéro drag.
//
// INVARIANT central : après tout retrait, Prune() élague les nœuds dégénérés —
//   tab groups vides supprimés, splits mono-enfant effondrés, splits imbriqués
//   de même orientation aplatis. L'arbre n'accumule jamais de nœuds inutiles.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Orientation d'un conteneur à volets redimensionnables.</summary>
public enum DockOrientation { Horizontal, Vertical }

/// <summary>Zone d'ancrage au bord de la racine (ou centre = zone document).</summary>
public enum DockRegion { Left, Right, Top, Bottom, Center }

/// <summary>
/// Côté d'insertion relatif à un groupe cible (utilisé par l'ancrage et,
/// en phase 2, par le drag). Center = ajout comme onglet dans le groupe.
/// </summary>
public enum DockSide { Left, Right, Top, Bottom, Center }

/// <summary>Nœud de l'arbre de docking : un split ou un groupe d'onglets.</summary>
public abstract class DockNode
{
    /// <summary>Split parent (null pour la racine).</summary>
    public DockSplit? Parent { get; internal set; }
}

/// <summary>
/// Conteneur redimensionnable : enfants disposés côte à côte (Horizontal) ou
/// empilés (Vertical), chacun avec une proportion (somme normalisée à 1).
/// </summary>
public sealed class DockSplit : DockNode
{
    public DockOrientation Orientation { get; set; }

    /// <summary>Enfants dans l'ordre visuel (gauche→droite ou haut→bas).</summary>
    public List<DockNode> Children { get; } = new();

    /// <summary>Proportion de chaque enfant, parallèle à <see cref="Children"/>.</summary>
    public List<double> Proportions { get; } = new();

    internal void Add(DockNode node, double proportion)
    {
        node.Parent = this;
        Children.Add(node);
        Proportions.Add(proportion);
    }

    internal void Insert(int index, DockNode node, double proportion)
    {
        node.Parent = this;
        Children.Insert(index, node);
        Proportions.Insert(index, proportion);
    }

    internal void RemoveAt(int index)
    {
        Children[index].Parent = null;
        Children.RemoveAt(index);
        Proportions.RemoveAt(index);
    }

    /// <summary>Renormalise les proportions pour que leur somme vaille 1.</summary>
    internal void Normalize()
    {
        var sum = Proportions.Sum();
        if (sum <= 0)
        {
            var even = Children.Count > 0 ? 1.0 / Children.Count : 0;
            for (int i = 0; i < Proportions.Count; i++) Proportions[i] = even;
            return;
        }
        for (int i = 0; i < Proportions.Count; i++) Proportions[i] /= sum;
    }
}

/// <summary>Groupe d'onglets empilés (une seule feuille visible à la fois).</summary>
public sealed class DockTabGroup : DockNode
{
    public List<DockItem> Items { get; } = new();
    public int SelectedIndex { get; set; }

    /// <summary>true = zone document centrale (cible de <see cref="DockRegion.Center"/>).</summary>
    public bool IsDocumentArea { get; set; }

    public DockItem? Selected =>
        SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

    internal void Add(DockItem item)
    {
        item.Group = this;
        Items.Add(item);
    }
}

/// <summary>
/// Feuille du docking : identité + métadonnées sérialisables. Le contenu
/// (Control Avalonia) vit dans HDevDockManager, réassocié par <see cref="Id"/>.
/// </summary>
public sealed class DockItem
{
    public string Id { get; }
    public string Title { get; set; }
    public string? Icon { get; set; }
    public bool CanClose { get; set; }

    internal DockTabGroup? Group { get; set; }

    public DockItem(string id, string title, string? icon = null, bool canClose = true)
    {
        Id = id;
        Title = title;
        Icon = icon;
        CanClose = canClose;
    }
}

/// <summary>
/// Arbre de docking pur : opérations d'ancrage, retrait+élagage, normalisation,
/// sérialisation topologie+identité. Ne connaît pas Avalonia.
/// </summary>
public sealed class DockLayout
{
    /// <summary>Racine de l'arbre docké (null si vide).</summary>
    public DockNode? Root { get; private set; }

    /// <summary>
    /// Racines détachées (floats). Réservé — la phase 1 ne les remplit pas mais
    /// le modèle et la sérialisation les portent (cf. stance en tête de fichier).
    /// </summary>
    public List<DockNode> Floating { get; } = new();

    /// <summary>Notifié après toute mutation structurelle.</summary>
    public event Action? Changed;

    private const double DefaultSideProportion = 0.22;

    // ── Recherche ──────────────────────────────────────────────────────────

    public DockItem? Find(string id) => EnumerateItems(Root).FirstOrDefault(i => i.Id == id);

    /// <summary>Zone document (premier groupe marqué <see cref="DockTabGroup.IsDocumentArea"/>).</summary>
    public DockTabGroup? DocumentArea =>
        EnumerateGroups(Root).FirstOrDefault(g => g.IsDocumentArea);

    public IEnumerable<DockItem> AllItems() => EnumerateItems(Root);
    public IEnumerable<DockTabGroup> AllGroups() => EnumerateGroups(Root);

    private static IEnumerable<DockItem> EnumerateItems(DockNode? node)
    {
        foreach (var g in EnumerateGroups(node))
            foreach (var item in g.Items)
                yield return item;
    }

    private static IEnumerable<DockTabGroup> EnumerateGroups(DockNode? node)
    {
        switch (node)
        {
            case null: yield break;
            case DockTabGroup g: yield return g; break;
            case DockSplit s:
                foreach (var child in s.Children)
                    foreach (var g in EnumerateGroups(child))
                        yield return g;
                break;
        }
    }

    // ── Ancrage ──────────────────────────────────────────────────────────────

    /// <summary>Ancre un volet à un bord de la racine (ou l'ajoute à la zone document).</summary>
    public void AddToRegion(DockItem item, DockRegion region)
    {
        if (Root == null)
        {
            Root = NewGroup(item, document: region == DockRegion.Center);
            Changed?.Invoke();
            return;
        }

        if (region == DockRegion.Center)
        {
            var doc = DocumentArea;
            if (doc != null)
            {
                AddTab(doc, item);
                return;
            }
            // Aucune zone document : ce volet la devient, l'existant se range à gauche.
            WrapOrExtendRoot(NewGroup(item, document: true), DockOrientation.Horizontal, atStart: false);
            Changed?.Invoke();
            return;
        }

        var (orientation, atStart) = region switch
        {
            DockRegion.Left => (DockOrientation.Horizontal, true),
            DockRegion.Right => (DockOrientation.Horizontal, false),
            DockRegion.Top => (DockOrientation.Vertical, true),
            _ => (DockOrientation.Vertical, false), // Bottom
        };
        WrapOrExtendRoot(NewGroup(item), orientation, atStart);
        Changed?.Invoke();
    }

    /// <summary>Ajoute un volet comme onglet d'un groupe existant et le sélectionne.</summary>
    public void AddTab(DockTabGroup group, DockItem item)
    {
        group.Add(item);
        group.SelectedIndex = group.Items.Count - 1;
        Changed?.Invoke();
    }

    /// <summary>
    /// Insère un volet relativement à un groupe cible (cœur partagé ancrage/drag).
    /// Center = onglet dans la cible ; sinon crée/étend un split adjacent.
    /// </summary>
    public void InsertRelative(DockTabGroup target, DockItem item, DockSide side)
    {
        if (side == DockSide.Center)
        {
            AddTab(target, item);
            return;
        }

        var orientation = side is DockSide.Left or DockSide.Right
            ? DockOrientation.Horizontal
            : DockOrientation.Vertical;
        var before = side is DockSide.Left or DockSide.Top;
        var newGroup = NewGroup(item);

        var parent = target.Parent;
        if (parent != null && parent.Orientation == orientation)
        {
            // Étendre le split parent : couper la part de la cible en deux.
            var idx = parent.Children.IndexOf(target);
            var half = parent.Proportions[idx] / 2;
            parent.Proportions[idx] = half;
            parent.Insert(before ? idx : idx + 1, newGroup, half);
            parent.Normalize();
        }
        else
        {
            // Envelopper la cible dans un nouveau split.
            var split = new DockSplit { Orientation = orientation };
            var (a, b) = before ? (newGroup, (DockNode)target) : (target, newGroup);
            ReplaceInParent(target, split);
            split.Add(a, 0.5);
            split.Add(b, 0.5);
        }
        Changed?.Invoke();
    }

    // ── Retrait + élagage ─────────────────────────────────────────────────────

    /// <summary>Retire un volet par Id, puis élague les nœuds dégénérés.</summary>
    public bool Remove(string id)
    {
        var item = Find(id);
        if (item?.Group == null) return false;

        var group = item.Group;
        var idx = group.Items.IndexOf(item);
        group.Items.RemoveAt(idx);
        item.Group = null;
        if (group.SelectedIndex >= group.Items.Count)
            group.SelectedIndex = group.Items.Count - 1;

        Prune();
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Élague l'arbre : groupes vides retirés, splits mono-enfant effondrés,
    /// splits imbriqués de même orientation aplatis, proportions renormalisées.
    /// </summary>
    public void Prune()
    {
        if (Root is DockTabGroup tg)
        {
            if (tg.Items.Count == 0) Root = null;
            return;
        }
        if (Root is DockSplit s)
        {
            Root = PruneSplit(s);
            if (Root != null) Root.Parent = null;
        }
    }

    /// <summary>Élague récursivement un split ; retourne le nœud de remplacement (ou null).</summary>
    private static DockNode? PruneSplit(DockSplit split)
    {
        var newChildren = new List<DockNode>();
        var newProportions = new List<double>();

        for (int i = 0; i < split.Children.Count; i++)
        {
            var child = split.Children[i];
            var prop = split.Proportions[i];

            DockNode? replacement = child switch
            {
                DockTabGroup g => g.Items.Count == 0 ? null : g,
                DockSplit cs => PruneSplit(cs),
                _ => child,
            };
            if (replacement == null) continue;

            // Aplatir un sous-split de même orientation dans le parent.
            if (replacement is DockSplit inner && inner.Orientation == split.Orientation)
            {
                var innerSum = inner.Proportions.Sum();
                for (int j = 0; j < inner.Children.Count; j++)
                {
                    var c = inner.Children[j];
                    c.Parent = split;
                    newChildren.Add(c);
                    newProportions.Add(prop * (innerSum > 0 ? inner.Proportions[j] / innerSum : 0));
                }
                continue;
            }

            replacement.Parent = split;
            newChildren.Add(replacement);
            newProportions.Add(prop);
        }

        split.Children.Clear();
        split.Children.AddRange(newChildren);
        split.Proportions.Clear();
        split.Proportions.AddRange(newProportions);

        if (split.Children.Count == 0) return null;
        if (split.Children.Count == 1)
        {
            var only = split.Children[0];
            only.Parent = null;
            return only; // split mono-enfant effondré
        }
        split.Normalize();
        return split;
    }

    // ── Helpers de structure ──────────────────────────────────────────────────

    private static DockTabGroup NewGroup(DockItem item, bool document = false)
    {
        var g = new DockTabGroup { IsDocumentArea = document };
        g.Add(item);
        g.SelectedIndex = 0;
        return g;
    }

    private void WrapOrExtendRoot(DockTabGroup newGroup, DockOrientation orientation, bool atStart)
    {
        if (Root is DockSplit s && s.Orientation == orientation)
        {
            // Étendre le split racine ; le nouveau volet prend une part de bord.
            var p = DefaultSideProportion;
            if (atStart) s.Insert(0, newGroup, p);
            else s.Add(newGroup, p);
            s.Normalize();
            return;
        }

        var split = new DockSplit { Orientation = orientation };
        var existing = Root!;
        var pSide = DefaultSideProportion;
        var pMain = 1 - pSide;
        if (atStart)
        {
            split.Add(newGroup, pSide);
            split.Add(existing, pMain);
        }
        else
        {
            split.Add(existing, pMain);
            split.Add(newGroup, pSide);
        }
        Root = split;
    }

    private void ReplaceInParent(DockNode oldNode, DockNode newNode)
    {
        var parent = oldNode.Parent;
        if (parent == null)
        {
            Root = newNode;
            newNode.Parent = null;
            oldNode.Parent = null;
            return;
        }
        var idx = parent.Children.IndexOf(oldNode);
        parent.Children[idx] = newNode;
        newNode.Parent = parent;
        oldNode.Parent = null;
    }

    // ── Sérialisation (topologie + identité, PAS le contenu) ──────────────────

    public string ToJson()
    {
        var root = new JsonObject
        {
            ["root"] = SerializeNode(Root),
            ["floating"] = new JsonArray(Floating.Select(SerializeNode).ToArray()),
        };
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    public static DockLayout FromJson(string json)
    {
        var layout = new DockLayout();
        var obj = JsonNode.Parse(json)!.AsObject();

        layout.Root = DeserializeNode(obj["root"]);
        if (layout.Root != null) layout.Root.Parent = null;

        if (obj["floating"] is JsonArray floats)
            foreach (var f in floats)
            {
                var node = DeserializeNode(f);
                if (node != null) layout.Floating.Add(node);
            }
        return layout;
    }

    private static JsonNode? SerializeNode(DockNode? node)
    {
        switch (node)
        {
            case null:
                return null;
            case DockTabGroup g:
                return new JsonObject
                {
                    ["type"] = "tabs",
                    ["selected"] = g.SelectedIndex,
                    ["document"] = g.IsDocumentArea,
                    ["items"] = new JsonArray(g.Items.Select(i => (JsonNode)new JsonObject
                    {
                        ["id"] = i.Id,
                        ["title"] = i.Title,
                        ["icon"] = i.Icon,
                        ["canClose"] = i.CanClose,
                    }).ToArray()),
                };
            case DockSplit s:
                return new JsonObject
                {
                    ["type"] = "split",
                    ["orientation"] = s.Orientation.ToString(),
                    ["children"] = new JsonArray(s.Children.Select((c, i) => (JsonNode)new JsonObject
                    {
                        ["p"] = s.Proportions[i],
                        ["node"] = SerializeNode(c),
                    }).ToArray()),
                };
            default:
                return null;
        }
    }

    private static DockNode? DeserializeNode(JsonNode? node)
    {
        if (node is not JsonObject obj) return null;
        var type = (string?)obj["type"];

        if (type == "tabs")
        {
            var g = new DockTabGroup
            {
                IsDocumentArea = (bool?)obj["document"] ?? false,
                SelectedIndex = (int?)obj["selected"] ?? 0,
            };
            if (obj["items"] is JsonArray items)
                foreach (var it in items)
                {
                    var io = it!.AsObject();
                    g.Add(new DockItem(
                        (string)io["id"]!,
                        (string?)io["title"] ?? "",
                        (string?)io["icon"],
                        (bool?)io["canClose"] ?? true));
                }
            return g;
        }

        if (type == "split")
        {
            var s = new DockSplit
            {
                Orientation = System.Enum.Parse<DockOrientation>((string)obj["orientation"]!),
            };
            if (obj["children"] is JsonArray children)
                foreach (var c in children)
                {
                    var co = c!.AsObject();
                    var child = DeserializeNode(co["node"]);
                    if (child != null) s.Add(child, (double?)co["p"] ?? 0);
                }
            s.Normalize();
            return s;
        }

        return null;
    }
}
