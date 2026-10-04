using Avalonia.Interactivity;

namespace HDev.UI.Controls;

// ═══════════════════════════════════════════════════════════════════════════
// CONTRAT DE VALEUR DES ÉDITEURS (D2, lot E1 — Conception-Socle-Editeurs.md)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Provenance d'un changement de valeur</summary>
public enum HDevValueOrigin
{
    /// <summary>Le code ou une liaison (le ViewModel)</summary>
    Code,

    /// <summary>L'utilisateur : saisie, choix dans un popup, bouton, molette</summary>
    User
}

/// <summary>
/// Changement de valeur d'un éditeur (<see cref="HDevEditor.ValueChangedEvent"/>).
/// Ancienne et nouvelle valeur ne sont pas typées : un seul événement sert toute la
/// famille, ce qui permet à un formulaire de tout écouter d'un seul AddHandler.
/// La valeur typée se lit sur l'éditeur (<c>Value</c>).
/// </summary>
public class HDevValueChangedEventArgs : RoutedEventArgs
{
    public HDevValueChangedEventArgs(object? oldValue, object? newValue, HDevValueOrigin origin)
        : base(HDevEditor.ValueChangedEvent)
    {
        OldValue = oldValue;
        NewValue = newValue;
        Origin = origin;
    }

    public object? OldValue { get; }
    public object? NewValue { get; }
    public HDevValueOrigin Origin { get; }

    /// <summary>Vrai si le changement vient de l'utilisateur (formulaire « modifié »)</summary>
    public bool IsUserChange => Origin == HDevValueOrigin.User;
}

/// <summary>
/// Changement de valeur à venir, annulable (<see cref="HDevEditor.ValueChangingEvent"/>).
/// Levé pour les changements de l'utilisateur seulement : une valeur posée par le code ou
/// par la source d'une liaison ne s'annule pas, elle se valide.
/// </summary>
public class HDevValueChangingEventArgs : RoutedEventArgs
{
    public HDevValueChangingEventArgs(object? oldValue, object? newValue)
        : base(HDevEditor.ValueChangingEvent)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public object? OldValue { get; }
    public object? NewValue { get; }

    /// <summary>Vrai : la valeur ne change pas et le texte revient à la valeur courante</summary>
    public bool Cancel { get; set; }
}

/// <summary>
/// Événements communs à tous les éditeurs HDev, hors de toute hiérarchie de classes (à la
/// manière de <c>Gestures</c>) : leur propriétaire ne bouge pas quand le socle évolue.
/// <code>formulaire.AddHandler(HDevEditor.ValueChangedEvent, (s, e) => { if (e.IsUserChange) Modifie = true; });</code>
/// Un éditeur ne fait pas sortir les événements de ses pièces internes (calendrier, liste,
/// grille d'un popup) : seuls les siens remontent.
/// </summary>
public sealed class HDevEditor
{
    private HDevEditor() { }

    public static readonly RoutedEvent<HDevValueChangedEventArgs> ValueChangedEvent =
        RoutedEvent.Register<HDevEditor, HDevValueChangedEventArgs>("ValueChanged", RoutingStrategies.Bubble);

    public static readonly RoutedEvent<HDevValueChangingEventArgs> ValueChangingEvent =
        RoutedEvent.Register<HDevEditor, HDevValueChangingEventArgs>("ValueChanging", RoutingStrategies.Bubble);
}

/// <summary>
/// Contrat de valeur commun aux éditeurs (D2) : ce dont ont besoin le code générique, la
/// grille (E11) et la validation de formulaire (E8). Chaque éditeur expose en plus sa
/// valeur typée <c>Value</c>, liable dans les deux sens.
/// </summary>
public interface IHDevEditor
{
    /// <summary>La valeur, non typée</summary>
    object? EditValue { get; set; }

    bool IsReadOnly { get; set; }

    string? Placeholder { get; set; }

    /// <summary>Erreur affichée : saisie invalide d'abord, sinon erreur de la liaison (null = valide)</summary>
    string? ErrorText { get; }

    /// <summary>Valide la saisie ; faux si elle est refusée (l'erreur est affichée)</summary>
    bool Validate();

    void FocusEditor();

    event EventHandler<HDevValueChangingEventArgs>? ValueChanging;

    event EventHandler<HDevValueChangedEventArgs>? ValueChanged;
}
