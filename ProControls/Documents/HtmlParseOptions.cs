namespace ProControls.Documents;

/// <summary>
/// Profil de parsing HTML.
/// Mail : tolérant (défaut historique) — HTML du monde extérieur (mails,
/// collage riche). Storage : mêmes règles de construction du document,
/// plus un diagnostic par construction hors vocabulaire storage-v1
/// (HtmlVocabulary) — jamais d'exception, jamais de perte de contenu.
/// </summary>
public enum HtmlParseProfile { Mail, Storage }

public sealed record HtmlParseOptions
{
    public HtmlParseProfile Profile { get; init; } = HtmlParseProfile.Mail;

    public static readonly HtmlParseOptions Mail = new();
    public static readonly HtmlParseOptions Storage = new() { Profile = HtmlParseProfile.Storage };
}

public enum HtmlDiagnosticKind
{
    DisallowedTag,
    DisallowedAttribute,
    DisallowedCssProperty,
    DisallowedStructure
}

/// <summary>Position = offset du caractère '&lt;' fautif dans la chaîne source.</summary>
public sealed record HtmlDiagnostic(HtmlDiagnosticKind Kind, string Subject, int Position, string? Detail = null);

public sealed class HtmlParseResult
{
    public required ProDocument Document { get; init; }
    public required IReadOnlyList<HtmlDiagnostic> Diagnostics { get; init; }

    /// <summary>Vrai si le HTML est du storage-v1 pur (aucun diagnostic).</summary>
    public bool IsCleanStorageHtml => Diagnostics.Count == 0;
}
