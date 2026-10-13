using Avalonia;
using Avalonia.Controls;
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

    /// <summary>Erreur affichée : saisie invalide d'abord, sinon erreur de la liaison (null = valide)</summary>
    string? ErrorText { get; }

    /// <summary>Valide la saisie ; faux si elle est refusée (l'erreur est affichée)</summary>
    bool Validate();

    void FocusEditor();

    event EventHandler<HDevValueChangingEventArgs>? ValueChanging;

    event EventHandler<HDevValueChangedEventArgs>? ValueChanged;
}

/// <summary>
/// Éditeur de champ (D1) : un éditeur qui a une zone de valeur, texte ou présentateur, et donc
/// une invite quand elle est vide (QX-05). Les contrôles de choix et le calendrier suivent
/// <see cref="IHDevEditor"/> sans invite.
/// </summary>
public interface IHDevFieldEditor : IHDevEditor
{
    /// <summary>Invite affichée quand la valeur est vide</summary>
    string? Placeholder { get; set; }
}

/// <summary>
/// Le contrat de valeur pour un contrôle hors du socle (cases, bascule, radios, curseur,
/// calendrier) : changement de l'utilisateur annulable, événement commun avec son origine,
/// erreur de la liaison. Le contrôle déclare sa StyledProperty et délègue ici.
/// </summary>
internal sealed class HDevValueCore<T>
{
    private readonly Control _owner;
    private readonly StyledProperty<T> _property;
    private bool _userCommit;
    private string? _bindingError;
    private HDevErrorTip _tip;

    public HDevValueCore(Control owner, StyledProperty<T> property)
    {
        _owner = owner;
        _property = property;
    }

    /// <summary>Erreur venue de la liaison de la valeur (null = valide)</summary>
    public string? ErrorText => _bindingError;

    /// <summary>
    /// Pose une valeur venue de l'utilisateur : ValueChanging (annulable), puis SetCurrentValue,
    /// qui garde la liaison et met la source à jour. Faux si un abonné a annulé.
    /// </summary>
    public bool CommitUser(T value)
    {
        var current = _owner.GetValue(_property);
        if (EqualityComparer<T>.Default.Equals(value, current))
            return true;

        var changing = new HDevValueChangingEventArgs(current, value);
        _owner.RaiseEvent(changing);
        if (changing.Cancel)
            return false;

        _userCommit = true;
        try { _owner.SetCurrentValue(_property, value); }
        finally { _userCommit = false; }
        return true;
    }

    /// <summary>À appeler depuis OnPropertyChanged : lève ValueChanged, avec son origine</summary>
    public void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == _property)
            _owner.RaiseEvent(new HDevValueChangedEventArgs(change.OldValue, change.NewValue,
                _userCommit ? HDevValueOrigin.User : HDevValueOrigin.Code));
    }

    /// <summary>À appeler depuis UpdateDataValidation : garde l'erreur de la liaison, en infobulle</summary>
    public void OnDataValidation(AvaloniaProperty property, Exception? error)
    {
        if (property != _property) return;
        _bindingError = error is AggregateException many
            ? string.Join(Environment.NewLine, many.InnerExceptions.Select(x => x.Message))
            : error?.Message;
        _tip.Show(_owner, _bindingError);
        _owner.InvalidateVisual();
    }
}

/// <summary>
/// L'erreur d'un éditeur s'affiche en infobulle le temps qu'elle dure : l'infobulle posée par
/// l'application est mise de côté, puis rendue quand l'erreur disparaît
/// </summary>
internal struct HDevErrorTip
{
    private bool _showing;
    private object? _appTip;

    public void Show(Control owner, string? error)
    {
        if (error != null)
        {
            if (!_showing) _appTip = ToolTip.GetTip(owner);
            _showing = true;
            ToolTip.SetTip(owner, error);
        }
        else if (_showing)
        {
            _showing = false;
            ToolTip.SetTip(owner, _appTip);
            _appTip = null;
        }
    }
}
