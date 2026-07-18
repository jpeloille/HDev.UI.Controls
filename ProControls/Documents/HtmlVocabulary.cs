namespace ProControls.Documents;

/// <summary>
/// Vocabulaire HTML de stockage figé « storage-v1 » (dialecte TipTap).
/// SOURCE DE VÉRITÉ du contrat : le parseur strict (HtmlParser.Parse avec
/// HtmlParseOptions.Storage) et les tests de contrat consultent ces listes.
/// Le miroir humain est Documents/StorageHtml-v1.md — toute divergence entre
/// les deux est un bug. NE PAS étendre sans bump de version.
/// Trois listes distinctes dans la pile : vocabulaire ACCEPTÉ (parseur
/// tolérant, profil Mail) ⊃ vocabulaire STRICT (ces listes) ⊇ vocabulaire
/// ÉMIS (HtmlSerializer).
/// </summary>
public static class HtmlVocabulary
{
    public const string Version = "storage-v1";

    // Balise → attributs autorisés (hors style="", régi par CssByTag)
    private static readonly Dictionary<string, string[]> AttributesByTag = new(StringComparer.OrdinalIgnoreCase)
    {
        // Blocs
        ["p"] = [],
        ["h1"] = [], ["h2"] = [], ["h3"] = [], ["h4"] = [], ["h5"] = [], ["h6"] = [],
        ["ul"] = [], ["ol"] = [], ["li"] = [],
        ["blockquote"] = [],
        ["pre"] = [],
        ["hr"] = [],
        // Tables : statut « compat » (lecture/round-trip, pas d'édition avant la v3 tables)
        ["table"] = [], ["tbody"] = [], ["tr"] = [],
        ["th"] = ["colspan"], ["td"] = ["colspan"],
        // Inlines / marques
        ["strong"] = [], ["em"] = [], ["u"] = [], ["s"] = [], ["code"] = [],
        ["span"] = [],
        ["mark"] = ["data-color"],
        ["a"] = ["href", "target", "rel"],
        ["img"] = ["src", "alt", "title", "width", "height"],
        ["br"] = [],
    };

    // Balise → propriétés autorisées dans style=""
    private static readonly Dictionary<string, string[]> CssByTag = new(StringComparer.OrdinalIgnoreCase)
    {
        ["p"] = ["text-align"],
        ["h1"] = ["text-align"], ["h2"] = ["text-align"], ["h3"] = ["text-align"],
        ["h4"] = ["text-align"], ["h5"] = ["text-align"], ["h6"] = ["text-align"],
        ["span"] = ["color", "font-size"],
        ["mark"] = ["background-color"],
    };

    /// <summary>Balises au statut « compat » : round-trippées sans être éditables.</summary>
    public static readonly IReadOnlySet<string> CompatTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "table", "tbody", "tr", "th", "td"
    };

    public static bool IsAllowedTag(string tag) => AttributesByTag.ContainsKey(tag);

    public static bool IsAllowedAttribute(string tag, string attribute)
    {
        if (attribute.Equals("style", StringComparison.OrdinalIgnoreCase))
            return CssByTag.ContainsKey(tag);
        return AttributesByTag.TryGetValue(tag, out var allowed) &&
               allowed.Contains(attribute, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsAllowedCssProperty(string tag, string property) =>
        CssByTag.TryGetValue(tag, out var allowed) &&
        allowed.Contains(property, StringComparer.OrdinalIgnoreCase);
}
