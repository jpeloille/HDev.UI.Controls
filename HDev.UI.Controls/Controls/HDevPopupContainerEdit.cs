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
/// <item>Le contenu (<see cref="HDevEditorBase.DropDownContent"/>) se lie à <c>Value</c> de
/// l'éditeur : ce qu'il y écrit est un changement de l'utilisateur. La fermeture ne touche
/// jamais à la valeur, quelle que soit <c>AcceptValue</c>.</item>
/// <item>Un clic sur le champ, le bouton, F4 ou Alt+↓ ouvrent ; Entrée (non prise par le
/// contenu) ferme en acceptant ; Échap, Alt+↑ et le clic extérieur ferment.</item>
/// </list>
/// </summary>
public class HDevPopupContainerEdit : HDevEditorBase<object?>
{
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<HDevPopupContainerEdit, object?>(nameof(Value),
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);

    public static readonly StyledProperty<IDataTemplate?> ValueTemplateProperty =
        AvaloniaProperty.Register<HDevPopupContainerEdit, IDataTemplate?>(nameof(ValueTemplate));

    private readonly ContentControl _presenter;

    protected override StyledProperty<object?> ValuePropertyCore => ValueProperty;

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValueFromCode(value);
    }

    /// <summary>Gabarit d'affichage de la valeur dans le champ (défaut : <c>ToString()</c>)</summary>
    public IDataTemplate? ValueTemplate
    {
        get => GetValue(ValueTemplateProperty);
        set => SetValue(ValueTemplateProperty, value);
    }

    public HDevPopupContainerEdit()
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
        RefreshPresenter();
    }

    protected override string FormatValue(object? value) => value?.ToString() ?? "";

    private protected override bool IsUserEditInProgress => DropDown.IsOpen;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
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
