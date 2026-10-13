using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using HDev.UI.Controls.Theme;
using HDev.UI.DataGrid;
using HDev.UI.DataGrid.Models;
using System.Collections;
using System.Collections.ObjectModel;

namespace HDev.UI.Controls;

/// <summary>
/// Éditeur de recherche dans un référentiel, sur le socle des éditeurs : le popup est un
/// HDevDataGrid multi-colonnes ; choisir une ligne pose <see cref="SelectedItem"/> et
/// <see cref="Value"/> (<see cref="SelectedValueBinding"/> de la ligne, à défaut la ligne),
/// et affiche <see cref="DisplayMemberBinding"/> de la ligne.
/// </summary>
public class HDevLookUpEdit : HDevEditorBase<object?>
{
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<HDevLookUpEdit, object?>(nameof(Value),
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true, coerce: CoerceThroughChanging);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<HDevLookUpEdit, object?>(nameof(SelectedItem),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<HDevLookUpEdit, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<BindingBase?> DisplayMemberBindingProperty =
        AvaloniaProperty.Register<HDevLookUpEdit, BindingBase?>(nameof(DisplayMemberBinding));

    public static readonly StyledProperty<BindingBase?> SelectedValueBindingProperty =
        AvaloniaProperty.Register<HDevLookUpEdit, BindingBase?>(nameof(SelectedValueBinding));

    private readonly HDevDataGrid _grid;
    private readonly ContentControl _presenter;
    private readonly HDevBindingEvaluator _display = new();
    private readonly HDevBindingEvaluator _selectedValue = new();
    private bool _syncing;

    // ═══════════════════════════════════════════════════════════════
    // API
    // ═══════════════════════════════════════════════════════════════

    protected override StyledProperty<object?> ValuePropertyCore => ValueProperty;

    /// <summary>
    /// Valeur : <see cref="SelectedValueBinding"/> de la ligne choisie, ou la ligne si elle n'est
    /// pas posée. Poser une valeur recherche la ligne correspondante dans <see cref="ItemsSource"/>.
    /// </summary>
    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValueFromCode(value);
    }

    /// <summary>Ligne choisie dans le référentiel ; la poser pose la valeur</summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>Lignes du référentiel</summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Texte affiché pour une ligne (défaut : ToString) : <c>new Binding("Nom")</c> ou liaison compilée</summary>
    [AssignBinding]
    public BindingBase? DisplayMemberBinding
    {
        get => GetValue(DisplayMemberBindingProperty);
        set => SetValue(DisplayMemberBindingProperty, value);
    }

    /// <summary>Valeur tirée d'une ligne (défaut : la ligne entière)</summary>
    [AssignBinding]
    public BindingBase? SelectedValueBinding
    {
        get => GetValue(SelectedValueBindingProperty);
        set => SetValue(SelectedValueBindingProperty, value);
    }

    /// <summary>Colonnes du popup (vides = auto-génération HDevDataGrid)</summary>
    public ObservableCollection<GridColumn> Columns { get; } = new();

    /// <summary>Nombre de lignes visibles dans le popup</summary>
    public int DropDownRows { get; set; } = 7;

    /// <summary>Largeur du popup (0 = max(largeur du contrôle, 320))</summary>
    public double DropDownWidth { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTION
    // ═══════════════════════════════════════════════════════════════

    static HDevLookUpEdit()
    {
        ItemsSourceProperty.Changed.AddClassHandler<HDevLookUpEdit>((x, _) => x.SyncFromValue());
        SelectedValueBindingProperty.Changed.AddClassHandler<HDevLookUpEdit>((x, _) => x.SyncFromValue());
        DisplayMemberBindingProperty.Changed.AddClassHandler<HDevLookUpEdit>((x, _) => { x.ResyncText(); x.RefreshPresenter(); });
        SelectedItemProperty.Changed.AddClassHandler<HDevLookUpEdit>((x, _) => x.OnSelectedItemChanged());
    }

    public HDevLookUpEdit()
    {
        MinWidth = 160;

        _grid = new HDevDataGrid { SelectionMode = HDev.UI.DataGrid.Models.SelectionMode.Single };
        _grid.SelectionChanged += OnGridSelectionChanged;

        // Grille accolée au champ, bordure fusionnée, coins du bas seuls
        DropDown.Content = _grid;
        DropDown.Chrome.Padding = new Thickness(0);
        DropDown.Chrome.CornerRadius = new CornerRadius(0, 0, HDevTheme.Size.CornerRadiusSmall, HDevTheme.Size.CornerRadiusSmall);
        DropDown.SetGap(HDevPopupPlacement.FusedBorder);
        DropDownOpening += OnDropDownOpening;
        DropDownOpened += (_, _) => UpdateVisualState();
        DropDownClosed += (_, _) => UpdateVisualState();

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
        RefreshPresenter();
    }

    // ═══════════════════════════════════════════════════════════════
    // VALEUR ET LIGNE
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Valeur tirée d'une ligne : SelectedValueBinding, ou la ligne</summary>
    private object? ValueOf(object? row)
    {
        if (row == null || SelectedValueBinding is not { } binding) return row;
        _selectedValue.Use(binding);
        return _selectedValue.Evaluate(row);
    }

    /// <summary>Texte affiché pour une ligne : DisplayMemberBinding, ou ToString</summary>
    private string DisplayOf(object? row)
    {
        if (row == null) return "";
        if (DisplayMemberBinding is { } binding)
        {
            _display.Use(binding);
            if (_display.Evaluate(row) is { } shown)
                return shown.ToString() ?? "";
        }
        return row.ToString() ?? "";
    }

    private object? FindRowByValue(object? value)
    {
        if (value == null || ItemsSource == null) return null;
        if (SelectedValueBinding == null) return value;

        foreach (var row in ItemsSource)
            if (Equals(ValueOf(row), value))
                return row;
        return null;
    }

    protected override string FormatValue(object? value) => DisplayOf(FindRowByValue(value));

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty)
            SyncFromValue();
        else if (change.Property == PlaceholderProperty || change.Property == IsEffectivelyEnabledProperty)
            RefreshPresenter();
    }

    /// <summary>La ligne suit la valeur</summary>
    private void SyncFromValue()
    {
        _syncing = true;
        try { SetCurrentValue(SelectedItemProperty, FindRowByValue(Value)); }
        finally { _syncing = false; }
        RefreshPresenter();
    }

    /// <summary>SelectedItem posé par le code ou une liaison : sa valeur devient la valeur</summary>
    private void OnSelectedItemChanged()
    {
        if (_syncing) return;
        SetCurrentValue(ValueProperty, ValueOf(SelectedItem));
    }

    // ═══════════════════════════════════════════════════════════════
    // AFFICHAGE
    // ═══════════════════════════════════════════════════════════════

    private void RefreshPresenter()
    {
        var row = SelectedItem;
        _presenter.Content = row != null ? DisplayOf(row) : Placeholder;
        _presenter.Foreground = new SolidColorBrush(!IsEffectivelyEnabled ? HDevTheme.Text.Disabled
            : row == null ? HDevTheme.Text.Placeholder : HDevTheme.Text.Primary);
    }

    protected override void RefreshThemeBrushes()
    {
        base.RefreshThemeBrushes();
        RefreshPresenter();
    }

    /// <summary>Fond de contrôle, filet et halo de focus quand le popup est ouvert ou le champ focalisé</summary>
    private protected override void ApplyVisualState(bool isEnabled, bool hasError, bool isFocused, bool isHovered)
    {
        var active = isFocused || (HasDropDown && DropDown.IsOpen);
        Color bg, border;
        Color? halo = null;

        if (!isEnabled)
        {
            bg = HDevTheme.Background.ControlDisabled;
            border = HDevTheme.Border.Disabled;
        }
        else if (hasError)
        {
            bg = HDevTheme.Background.Error;
            border = HDevTheme.Accent.Error;
            halo = HDevInputChrome.ErrorHalo;
        }
        else
        {
            bg = HDevTheme.Background.Control;
            border = active ? HDevTheme.Border.Focused : isHovered ? HDevTheme.Border.Hover : HDevTheme.Border.Default;
            if (active) halo = HDevInputChrome.FocusHalo;
        }

        Background = new SolidColorBrush(bg);
        BorderBrush = new SolidColorBrush(border);
        BorderThickness = new Thickness(HDevTheme.Size.BorderThin);
        BoxShadow = halo is { } h
            ? new BoxShadows(new BoxShadow { Blur = 0, Spread = 2, Color = h })
            : new BoxShadows();
    }

    // ═══════════════════════════════════════════════════════════════
    // POPUP
    // ═══════════════════════════════════════════════════════════════

    private void OnDropDownOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (ItemsSource == null)
        {
            e.Cancel = true;
            return;
        }

        // Colonnes explicites (sinon auto-génération HDevDataGrid)
        if (Columns.Count > 0 && _grid.Columns.Count == 0)
            foreach (var column in Columns)
                _grid.Columns.Add(column);

        _syncing = true;
        try
        {
            _grid.ItemsSource = ItemsSource;
            _grid.SelectedItem = SelectedItem;
        }
        finally { _syncing = false; }

        DropDown.Chrome.Width = DropDownWidth > 0 ? DropDownWidth : Math.Max(Bounds.Width, 320);
        DropDown.Chrome.Height = DropDownRows * 30 + 40; // lignes + en-tête
    }

    private void OnGridSelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (_syncing || _grid.SelectedItem is not { } row) return;

        CommitUserValue(ValueOf(row));
        DropDown.Close(HDevDropDownCloseReason.Accepted);
        FocusEditor();
    }

    // ═══════════════════════════════════════════════════════════════
    // CLAVIER
    // ═══════════════════════════════════════════════════════════════

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && IsEffectivelyEnabled && !IsReadOnly && e.KeyModifiers == KeyModifiers.None)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (DropDown.IsOpen) DropDown.Close(HDevDropDownCloseReason.Accepted);
                    else DropDown.Open();
                    e.Handled = true;
                    break;
                case Key.Delete or Key.Back when !DropDown.IsOpen:
                    if (Value != null) CommitUserValue(null);
                    e.Handled = true;
                    break;
            }
        }
        base.OnKeyDown(e);
    }
}

/// <summary>
/// Évalue une liaison sur un objet quelconque (une ligne d'un référentiel) : l'objet devient
/// le DataContext d'un élément léger, la liaison pose sa propriété
/// </summary>
internal sealed class HDevBindingEvaluator : StyledElement
{
    public static readonly StyledProperty<object?> ResultProperty =
        AvaloniaProperty.Register<HDevBindingEvaluator, object?>("Result");

    private BindingBase? _binding;
    private BindingExpressionBase? _expression;

    /// <summary>Lie le résultat à cette liaison (sans effet si c'est déjà elle)</summary>
    public void Use(BindingBase binding)
    {
        if (ReferenceEquals(binding, _binding)) return;
        _expression?.Dispose();
        _expression = Bind(ResultProperty, binding);
        _binding = binding;
    }

    public object? Evaluate(object? item)
    {
        if (!Equals(item, DataContext)) DataContext = item;
        return GetValue(ResultProperty);
    }
}
