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
/// Changement de valeur à venir (<see cref="HDevEditor.ValueChangingEvent"/>), levé pour tout
/// changement : utilisateur, code ou source d'une liaison (QX-09).
/// <list type="bullet">
/// <item><see cref="NewValue"/> se réécrit : l'éditeur prend la valeur substituée, repassée par
/// sa propre coercition (bornes, plage), l'affiche, la transmet à la source liée et la donne à
/// <c>ValueChanged</c>. Une valeur d'un autre type que celle de l'éditeur est ignorée.</item>
/// <item>Substituer l'ancienne valeur, ou poser <see cref="Cancel"/>, annule le changement :
/// la valeur reste, le texte y revient, aucun <c>ValueChanged</c>.</item>
/// </list>
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

    /// <summary>La valeur à venir ; un gestionnaire peut la remplacer (QX-09)</summary>
    public object? NewValue { get; set; }

    /// <summary>Vrai : la valeur ne change pas et le texte revient à la valeur courante</summary>
    public bool Cancel { get; set; }
}

/// <summary>
/// ValueChanging levé depuis la coercition de la propriété de valeur : c'est le seul point par
/// où passent tous les changements (utilisateur, code, liaison), et la valeur qu'il rend est
/// celle que prend la propriété, et que reçoit une source liée dans les deux sens.
/// </summary>
internal static class HDevValueChanging
{
    /// <param name="own">La coercition propre à l'éditeur (bornes…), appliquée avant et après</param>
    public static T Coerce<T>(Interactive owner, T current, T candidate, Func<T, T>? own, ref bool raising)
    {
        var coerced = own != null ? own(candidate) : candidate;
        // Un changement fait par un gestionnaire de ValueChanging ne relève pas l'événement
        if (raising || EqualityComparer<T>.Default.Equals(coerced, current))
            return coerced;

        var changing = new HDevValueChangingEventArgs(current, coerced);
        raising = true;
        try { owner.RaiseEvent(changing); }
        finally { raising = false; }

        if (changing.Cancel)
            return current;
        if (Equals(changing.NewValue, coerced))
            return coerced;
        // Valeur substituée : du type de l'éditeur (nul permis s'il l'admet), sinon ignorée
        var substituted = changing.NewValue is T t ? t
            : changing.NewValue is null && default(T) is null ? default! : coerced;
        return own != null ? own(substituted) : substituted;
    }
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

    private bool _raising;

    /// <summary>
    /// Coercition de la propriété de valeur, à déclarer à son enregistrement : la coercition
    /// propre au contrôle, puis ValueChanging (annulable, valeur substituable), pour tout changement
    /// </summary>
    public T Coerce(T value, Func<T, T>? own = null)
        => HDevValueChanging.Coerce(_owner, _owner.GetValue(_property), value, own, ref _raising);

    /// <summary>
    /// Pose une valeur venue de l'utilisateur par SetCurrentValue, qui garde la liaison et met la
    /// source à jour ; ValueChanging est levé par la coercition. Faux si la valeur prise n'est pas
    /// celle demandée (annulée ou substituée).
    /// </summary>
    public bool CommitUser(T value)
    {
        if (EqualityComparer<T>.Default.Equals(value, _owner.GetValue(_property)))
            return true;

        _userCommit = true;
        try { _owner.SetCurrentValue(_property, value); }
        finally { _userCommit = false; }
        return EqualityComparer<T>.Default.Equals(_owner.GetValue(_property), value);
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
