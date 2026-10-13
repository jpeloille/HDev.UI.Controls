using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Champ à popup libre : la configuration
/// nommée du socle « valeur non éditable + bouton déroulant + popup à contenu libre ».
/// <list type="bullet">
/// <item><see cref="Value"/> (<c>object?</c>) est liable dans les deux sens ; elle s'affiche par
/// <see cref="ValueTemplate"/>, à défaut par <c>ToString()</c>, sinon l'invite.</item>
/// <item>Le contenu (<see cref="HDevEditorBase.DropDownContent"/>) se lie à <see cref="DropDownValue"/>,
/// la valeur en cours : elle repart de <c>Value</c> à l'ouverture, et <c>Value</c> ne bouge pas
/// tant que le popup est ouvert (le champ l'affiche toujours).</item>
/// <item>À la fermeture, si <c>IsValueAccepted</c> (vrai par défaut pour Entrée et le clic
/// extérieur, faux pour Échap), la valeur en cours est validée comme un changement de
/// l'utilisateur (<c>ValueChanging</c> annulable, puis <c>ValueChanged</c>) ; sinon, ou si le
/// changement est annulé, elle revient à <c>Value</c>. Le popup se ferme dans tous les cas.</item>
/// <item>Un clic sur le champ, le bouton, F4 ou Alt+↓ ouvrent ; Entrée ferme en acceptant ;
/// Échap, Alt+↑ et le clic extérieur ferment.</item>
/// </list>
/// </summary>
public class HDevDropDownEdit : HDevEditorBase<object?>
{
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<HDevDropDownEdit, object?>(nameof(Value),
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true, coerce: CoerceThroughChanging);

    public static readonly StyledProperty<object?> DropDownValueProperty =
        AvaloniaProperty.Register<HDevDropDownEdit, object?>(nameof(DropDownValue),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IDataTemplate?> ValueTemplateProperty =
        AvaloniaProperty.Register<HDevDropDownEdit, IDataTemplate?>(nameof(ValueTemplate));

    private readonly ContentControl _presenter;

    protected override StyledProperty<object?> ValuePropertyCore => ValueProperty;

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValueFromCode(value);
    }

    /// <summary>
    /// Valeur en cours dans le popup, à laquelle se lie son contenu. Popup fermé, elle suit
    /// <see cref="Value"/> ; elle n'est prise qu'à la fermeture, si le choix est accepté.
    /// </summary>
    public object? DropDownValue
    {
        get => GetValue(DropDownValueProperty);
        set => SetValue(DropDownValueProperty, value);
    }

    /// <summary>Gabarit d'affichage de la valeur dans le champ (défaut : <c>ToString()</c>)</summary>
    public IDataTemplate? ValueTemplate
    {
        get => GetValue(ValueTemplateProperty);
        set => SetValue(ValueTemplateProperty, value);
    }

    public HDevDropDownEdit()
    {
        MinWidth = 120;
        _presenter = new ContentControl
        {
            Margin = new Thickness(HDevTheme.Size.EditorTextInset, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontFamily = new FontFamily(HDevTheme.Typography.FontFamily),
            FontSize = HDevTheme.Typography.FontSizeBody,
            IsHitTestVisible = false
        };
        SetValuePresenter(_presenter);
        Buttons.Add(new HDevEditorButton { Kind = HDevEditorButtonKind.DropDown });
        DropDown.MatchOwnerWidth = true;
        DropDownOpening += (_, _) => SetCurrentValue(DropDownValueProperty, Value);
        DropDownClosed += (_, e) =>
        {
            if (e.IsValueAccepted)
                CommitUserValue(DropDownValue);
            // Abandonnée, refusée par ValueChanging ou prise : la valeur en cours rejoint Value
            SetCurrentValue(DropDownValueProperty, Value);
        };
        RefreshPresenter();
    }

    protected override string FormatValue(object? value) => value?.ToString() ?? "";

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty && !DropDown.IsOpen)
            SetCurrentValue(DropDownValueProperty, Value);
        if (change.Property == ValueProperty || change.Property == ValueTemplateProperty
            || change.Property == PlaceholderProperty || change.Property == IsEffectivelyEnabledProperty)
            RefreshPresenter();
    }

    protected override void RefreshThemeBrushes()
    {
        base.RefreshThemeBrushes();
        RefreshPresenter();
    }

    /// <summary>La valeur par son gabarit, ou l'invite quand elle est vide</summary>
    private void RefreshPresenter()
    {
        var empty = Value == null;
        _presenter.ContentTemplate = empty ? null : ValueTemplate;
        _presenter.Content = empty ? Placeholder : Value;
        _presenter.Foreground = new SolidColorBrush(!IsEffectivelyEnabled ? HDevTheme.Text.Disabled
            : empty ? HDevTheme.Text.Placeholder : HDevTheme.Text.Primary);
    }
}
