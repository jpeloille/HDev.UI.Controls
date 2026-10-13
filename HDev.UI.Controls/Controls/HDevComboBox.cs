using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using HDev.UI.Controls.Theme;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace HDev.UI.Controls;

// ═══════════════════════════════════════════════════════════════════════════
// CLASSES DE SUPPORT
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Collection d'items du ComboBox
/// </summary>
public class HDevComboBoxItemCollection : ObservableCollection<object>
{
    private bool _updating;

    /// <summary>
    /// Commence une mise à jour par lot (évite les rafraîchissements multiples)
    /// </summary>
    public void BeginUpdate()
    {
        _updating = true;
    }

    /// <summary>
    /// Termine une mise à jour par lot
    /// </summary>
    public void EndUpdate()
    {
        _updating = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Ajoute une plage d'éléments
    /// </summary>
    public void AddRange(IEnumerable items)
    {
        BeginUpdate();
        try
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
        finally
        {
            EndUpdate();
        }
    }

    /// <summary>
    /// Ajoute une plage d'éléments
    /// </summary>
    public void AddRange(object[] items)
    {
        AddRange((IEnumerable)items);
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_updating)
        {
            base.OnCollectionChanged(e);
        }
    }
}

/// <summary>Quand l'invite (<see cref="HDevEditorBase.Placeholder"/>) d'une liste non éditable s'affiche</summary>
public enum HDevPlaceholderVisibility
{
    /// <summary>Valeur vide, hors lecture seule (défaut)</summary>
    WhenEditable,

    /// <summary>Valeur vide, champ focalisé, hors lecture seule</summary>
    WhenFocused,

    /// <summary>Valeur vide, lecture seule comprise</summary>
    Always
}

/// <summary>Style d'édition du texte d'une <see cref="HDevComboBox"/></summary>
public enum HDevTextEditStyle
{
    /// <summary>Zone de saisie éditable, avec auto-complétion ; un texte libre peut devenir la valeur</summary>
    Editable,

    /// <summary>Texte affiché mais non éditable : choix par la liste seule (défaut)</summary>
    SelectOnly,

    /// <summary>Aucun texte affiché : le bouton seul</summary>
    NoText
}

/// <summary>
/// Mode d'auto-complétion (mode Editable)
/// </summary>
public enum AutoCompleteMode
{
    /// <summary>Équivaut à SuggestAppend</summary>
    Default,
    None,
    /// <summary>Complète le texte avec le premier item correspondant (suffixe sélectionné)</summary>
    Append,
    /// <summary>Filtre le dropdown sur le préfixe saisi</summary>
    Suggest,
    SuggestAppend
}

/// <summary>
/// Arguments pour l'événement CustomDisplayText
/// </summary>
public class HDevCustomDisplayTextEventArgs : EventArgs
{
    public object? Value { get; set; }
    public string DisplayText { get; set; } = "";
}

// ═══════════════════════════════════════════════════════════════════════════
// HDEVCOMBOBOX - liste déroulante, éditable ou non
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Liste déroulante sur le socle des éditeurs : items (<see cref="Items"/> ou
/// <see cref="ItemsSource"/>), saisie libre et auto-complétion optionnelles, bouton déroulant.
/// <list type="bullet">
/// <item><see cref="Value"/> est l'élément choisi ou, en saisie libre, le texte validé ; liable
/// dans les deux sens. <see cref="SelectedItem"/> est l'élément de la liste (nul pour un texte libre).</item>
/// <item>Liste ouverte, les flèches déplacent la surbrillance sans changer la valeur ; Entrée ou
/// un clic l'acceptent, Échap rend la valeur d'ouverture.</item>
/// <item>Fermée et non éditable, ↑ et ↓ changent la valeur, une frappe sélectionne par préfixe.</item>
/// </list>
/// </summary>
public class HDevComboBox : HDevEditorBase<object?>
{
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<HDevComboBox, object?>(nameof(Value),
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true, coerce: CoerceThroughChanging);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<HDevComboBox, object?>(nameof(SelectedItem),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<HDevComboBox, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<HDevComboBox, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<HDevTextEditStyle> TextEditStyleProperty =
        AvaloniaProperty.Register<HDevComboBox, HDevTextEditStyle>(nameof(TextEditStyle), HDevTextEditStyle.SelectOnly);

    public static readonly StyledProperty<HDevPlaceholderVisibility> ShowPlaceholderProperty =
        AvaloniaProperty.Register<HDevComboBox, HDevPlaceholderVisibility>(nameof(ShowPlaceholder));

    private readonly ListBox _listBox;
    private readonly ContentControl _presenter;
    private INotifyCollectionChanged? _observedSource;
    private bool _syncingSelection;
    private bool _highlightByUser;
    private bool _isPressed;

    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════

    protected override StyledProperty<object?> ValuePropertyCore => ValueProperty;

    /// <summary>Valeur : l'élément choisi ou, en saisie libre, le texte validé</summary>
    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValueFromCode(value);
    }

    /// <summary>L'élément de la liste qui est la valeur ; nul pour un texte libre ou une valeur vide</summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>Index de l'élément choisi dans la liste (-1 : aucun)</summary>
    public int SelectedIndex
    {
        get => IndexOf(Value);
        set
        {
            var items = EffectiveItems;
            if (value >= 0 && value < items.Count) Value = items[value];
            else if (value == -1) Value = null;
        }
    }

    /// <summary>Éléments posés par le code ; ignorés quand <see cref="ItemsSource"/> est posé</summary>
    public HDevComboBoxItemCollection Items { get; } = new();

    /// <summary>Source liée des éléments ; prend le pas sur <see cref="Items"/></summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Gabarit d'un élément, dans la liste et dans le champ non éditable (défaut : son texte)</summary>
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>
    /// Style d'édition : <see cref="HDevTextEditStyle.Editable"/> = zone de saisie éditable,
    /// <see cref="HDevTextEditStyle.SelectOnly"/> = sélection seule (défaut),
    /// <see cref="HDevTextEditStyle.NoText"/> = pas de texte
    /// </summary>
    public HDevTextEditStyle TextEditStyle
    {
        get => GetValue(TextEditStyleProperty);
        set => SetValue(TextEditStyleProperty, value);
    }

    /// <summary>Quand l'invite s'affiche, valeur vide, en mode non éditable</summary>
    public HDevPlaceholderVisibility ShowPlaceholder
    {
        get => GetValue(ShowPlaceholderProperty);
        set => SetValue(ShowPlaceholderProperty, value);
    }

    /// <summary>
    /// Auto-complétion en mode Editable (Default = SuggestAppend) :
    /// Suggest filtre la liste, Append complète le texte saisi
    /// </summary>
    public AutoCompleteMode AutoComplete { get; set; } = AutoCompleteMode.Default;

    /// <summary>Recherche sensible à la casse (sélection par préfixe, complétion, FindItem)</summary>
    public bool CaseSensitiveSearch { get; set; }

    /// <summary>Ouvrir la liste dès la première frappe (mode Editable), quel que soit AutoComplete</summary>
    public bool OpenDropDownOnInput { get; set; }

    /// <summary>La valeur peut être vidée (Suppr, Retour arrière, texte effacé)</summary>
    public bool AllowNull { get; set; } = true;

    /// <summary>Nombre de lignes visibles dans la liste</summary>
    public int DropDownRows
    {
        get => _dropDownRows;
        set => _dropDownRows = Math.Max(1, value);
    }

    private int _dropDownRows = 7;

    /// <summary>Trier les éléments affichés dans la liste (ordre alphabétique de leur texte)</summary>
    public bool Sorted { get; set; }

    /// <summary>La valeur a changé depuis la création ou le dernier <see cref="Reset"/></summary>
    public bool IsModified { get; set; }

    private bool IsEditable => TextEditStyle == HDevTextEditStyle.Editable;

    /// <summary>Texte affiché pour personnaliser celui d'un élément</summary>
    public event EventHandler<HDevCustomDisplayTextEventArgs>? CustomDisplayText;

    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTION
    // ═══════════════════════════════════════════════════════════════

    static HDevComboBox()
    {
        TextEditStyleProperty.Changed.AddClassHandler<HDevComboBox>((x, _) => x.ApplyTextEditStyle());
        ItemsSourceProperty.Changed.AddClassHandler<HDevComboBox>((x, _) => x.OnItemsSourceChanged());
        ItemTemplateProperty.Changed.AddClassHandler<HDevComboBox>((x, _) => { x.ApplyItemTemplate(); x.RefreshPresenter(); });
        ShowPlaceholderProperty.Changed.AddClassHandler<HDevComboBox>((x, _) => x.RefreshPresenter());
        SelectedItemProperty.Changed.AddClassHandler<HDevComboBox>((x, _) => x.OnSelectedItemChanged());
    }

    public HDevComboBox()
    {
        MinWidth = 120;

        _listBox = new ListBox
        {
            Background = new SolidColorBrush(HDevTheme.Background.Panel),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(2),
            Margin = new Thickness(0),
            SelectionMode = SelectionMode.Single
        };
        ApplyItemTemplate();
        _listBox.PointerReleased += OnListBoxPointerReleased;

        // Liste accolée au champ, bordure fusionnée, coins du bas seuls : l'habillage de la
        // liste d'avant le socle
        DropDown.Content = new ScrollViewer
        {
            Content = _listBox,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        DropDown.Chrome.Padding = new Thickness(0);
        DropDown.Chrome.CornerRadius = new CornerRadius(0, 0, HDevTheme.Size.CornerRadiusSmall, HDevTheme.Size.CornerRadiusSmall);
        DropDown.SetGap(HDevPopupPlacement.FusedBorder);
        DropDown.MatchOwnerWidth = true;
        CloseDropDownOnFocusLoss = true;
        DropDownOpening += OnDropDownOpening;
        DropDownOpened += (_, _) => UpdateVisualState();
        DropDownClosed += OnDropDownClosed;

        _presenter = new ContentControl
        {
            Margin = new Thickness(HDevTheme.Size.EditorTextInset, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontFamily = new FontFamily(HDevTheme.Typography.FontFamily),
            FontSize = HDevTheme.Typography.FontSizeBody,
            IsHitTestVisible = false
        };

        Buttons.Add(new HDevEditorButton { Kind = HDevEditorButtonKind.DropDown });
        Items.CollectionChanged += (_, _) => { if (ItemsSource == null) OnItemsChanged(); };

        // Mode éditable : complétion à la frappe, clavier de la liste, validation au départ du focus
        InnerTextBox.AddHandler(TextInputEvent, OnEditTextInput, RoutingStrategies.Tunnel);
        InnerTextBox.AddHandler(KeyDownEvent, OnEditTextBoxKeyDown, RoutingStrategies.Tunnel);
        InnerTextBox.LostFocus += (_, e) =>
        {
            // Le focus quitte la combo, pas vers sa liste (clic sur un item) : la saisie est validée (APP-10)
            if (!DropDown.Contains(e.NewFocusedElement))
                CommitTypedText();
        };

        ApplyTextEditStyle();
    }

    // ═══════════════════════════════════════════════════════════════
    // ÉLÉMENTS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Les éléments en vigueur : la source liée, sinon <see cref="Items"/></summary>
    private IReadOnlyList<object> EffectiveItems
        => ItemsSource switch
        {
            null => Items,
            IReadOnlyList<object> list => list,
            var source => source.Cast<object>().ToList()
        };

    private int IndexOf(object? item)
    {
        if (item == null) return -1;
        var items = EffectiveItems;
        for (var i = 0; i < items.Count; i++)
            if (Equals(items[i], item)) return i;
        return -1;
    }

    private void OnItemsSourceChanged()
    {
        if (_observedSource != null) _observedSource.CollectionChanged -= OnSourceCollectionChanged;
        _observedSource = ItemsSource as INotifyCollectionChanged;
        if (_observedSource != null) _observedSource.CollectionChanged += OnSourceCollectionChanged;
        OnItemsChanged();
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnItemsChanged();

    private void OnItemsChanged()
    {
        SyncSelectedItem();
        if (DropDown.IsOpen) RefreshDropDownItems(null);
        RefreshPresenter();
    }

    private void ApplyItemTemplate()
        // Une ligne recyclée est reconstruite pour un contenu nul : jamais de texte pour le nul (APP-11)
        => _listBox.ItemTemplate = ItemTemplate ?? new FuncDataTemplate<object?>((item, _) => new TextBlock
        {
            Text = item is null ? "" : GetItemText(item),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 2)
        });

    // ═══════════════════════════════════════════════════════════════
    // MÉTHODES PUBLIQUES
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Vide la valeur (changement du code) et remet <see cref="IsModified"/> à faux</summary>
    public void Reset()
    {
        Value = null;
        IsModified = false;
    }

    /// <summary>Désélectionne le texte de la zone de saisie (mode Editable)</summary>
    public void DeselectAll() => InnerTextBox.ClearSelection();

    /// <summary>Trouve un item par son texte</summary>
    public int FindItem(string text) => FindItem(text, 0);

    /// <summary>Trouve un item par son texte à partir d'un index</summary>
    public int FindItem(string text, int startIndex)
    {
        var items = EffectiveItems;
        for (var i = startIndex; i < items.Count; i++)
            if (GetItemText(items[i]).Equals(text, OrdinalComparison))
                return i;
        return -1;
    }

    /// <summary>Trouve un item par préfixe</summary>
    public int FindItemByPrefix(string prefix)
    {
        var items = EffectiveItems;
        for (var i = 0; i < items.Count; i++)
            if (GetItemText(items[i]).StartsWith(prefix, OrdinalComparison))
                return i;
        return -1;
    }

    private StringComparison OrdinalComparison
        => CaseSensitiveSearch ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    /// <summary>Texte affiché pour un item (<see cref="CustomDisplayText"/>, sinon ToString)</summary>
    public string GetItemText(object? item)
    {
        if (item == null) return "";

        var args = new HDevCustomDisplayTextEventArgs { Value = item, DisplayText = item.ToString() ?? "" };
        CustomDisplayText?.Invoke(this, args);
        return args.DisplayText;
    }

    // ═══════════════════════════════════════════════════════════════
    // VALEUR
    // ═══════════════════════════════════════════════════════════════

    protected override string FormatValue(object? value) => GetItemText(value);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty)
        {
            IsModified = true;
            SyncSelectedItem();
            RefreshPresenter();
        }
        else if (change.Property == PlaceholderProperty || change.Property == IsReadOnlyProperty
                 || change.Property == IsEffectivelyEnabledProperty)
        {
            RefreshPresenter();
            UpdateVisualState();
        }
    }

    /// <summary>SelectedItem suit la valeur : l'élément de la liste, ou nul</summary>
    private void SyncSelectedItem()
    {
        _syncingSelection = true;
        try { SetCurrentValue(SelectedItemProperty, IndexOf(Value) >= 0 ? Value : null); }
        finally { _syncingSelection = false; }
    }

    /// <summary>SelectedItem posé par le code ou une liaison : il devient la valeur</summary>
    private void OnSelectedItemChanged()
    {
        if (_syncingSelection || Equals(SelectedItem, Value)) return;
        if (SelectedItem == null && IndexOf(Value) < 0) return;
        SetCurrentValue(ValueProperty, SelectedItem);
    }

    /// <summary>Un choix de l'utilisateur</summary>
    private void CommitItem(object? item) => CommitUserValue(item);

    /// <summary>Valide le texte saisi : item correspondant si trouvé, sinon texte libre</summary>
    private void CommitTypedText()
    {
        if (!IsEditable || IsReadOnly) return;

        var typed = InnerText;
        if (string.IsNullOrEmpty(typed))
        {
            if (AllowNull && Value != null)
                CommitItem(null);
            return;
        }

        var index = FindItem(typed);
        if (index >= 0)
            CommitItem(EffectiveItems[index]);
        else if (!Equals(Value, typed))
            CommitItem(typed); // texte libre
    }

    // ═══════════════════════════════════════════════════════════════
    // AFFICHAGE
    // ═══════════════════════════════════════════════════════════════

    private void ApplyTextEditStyle()
    {
        SetValuePresenter(IsEditable ? null : _presenter);
        RefreshPresenter();
        UpdateVisualState();
    }

    /// <summary>Champ non éditable : l'élément par son gabarit, son texte, ou l'invite</summary>
    private void RefreshPresenter()
    {
        if (TextEditStyle == HDevTextEditStyle.NoText)
        {
            _presenter.Content = null;
            return;
        }

        var value = Value;
        var text = GetItemText(value);
        var empty = value == null || string.IsNullOrEmpty(text);
        if (!empty)
        {
            _presenter.ContentTemplate = ItemTemplate;
            _presenter.Content = ItemTemplate != null ? value : text;
            _presenter.Foreground = new SolidColorBrush(IsEffectivelyEnabled ? HDevTheme.Text.Primary : HDevTheme.Text.Disabled);
            return;
        }

        var promptAllowed = ShowPlaceholder switch
        {
            HDevPlaceholderVisibility.WhenEditable => !IsReadOnly,
            HDevPlaceholderVisibility.WhenFocused => IsFocused && !IsReadOnly,
            _ => true
        };
        _presenter.ContentTemplate = null;
        _presenter.Content = promptAllowed ? Placeholder : null;
        _presenter.Foreground = new SolidColorBrush(IsEffectivelyEnabled ? HDevTheme.Text.Placeholder : HDevTheme.Text.Disabled);
    }

    protected override void RefreshThemeBrushes()
    {
        base.RefreshThemeBrushes();
        _listBox.Background = new SolidColorBrush(HDevTheme.Background.Panel);
        RefreshPresenter();
    }

    /// <summary>
    /// Non éditable, la combo garde l'allure d'une liste déroulante : le gris des boutons, au
    /// survol et à la pression, le filet accent liste ouverte. Éditable, c'est un champ de
    /// saisie. Dans les deux cas : filet et halo de focus, fond grisé en lecture seule.
    /// </summary>
    private protected override void ApplyVisualState(bool isEnabled, bool hasError, bool isFocused, bool isHovered)
    {
        // Appelé dès le constructeur du socle : le popup n'existe peut-être pas encore
        var open = HasDropDown && DropDown.IsOpen;
        Color bg, border;
        Color? halo = null;

        if (!isEnabled)
        {
            bg = HDevTheme.Background.ControlDisabled;
            border = HDevTheme.Border.Subtle;
        }
        else if (IsEditable)
        {
            bg = HDevTheme.Background.Input;
            border = open ? HDevTheme.Accent.Primary : isHovered ? HDevTheme.Border.Hover : HDevTheme.Border.Default;
        }
        else
        {
            bg = open ? HDevTheme.Background.Control
                : _isPressed ? HDevTheme.Background.ControlPressed
                : isHovered ? HDevTheme.Background.ControlHover
                : HDevTheme.Background.Control;
            border = open || _isPressed ? HDevTheme.Accent.Primary : HDevTheme.Border.Default;
        }

        if (isEnabled && hasError)
        {
            bg = HDevTheme.Background.Error;
            border = HDevTheme.Accent.Error;
            halo = HDevInputChrome.ErrorHalo;
        }
        else if (isEnabled && isFocused)
        {
            border = HDevTheme.Border.Focused;
            halo = HDevInputChrome.FocusHalo;
        }

        if (IsReadOnly)
            bg = HDevTheme.Background.ControlDisabled;

        Background = new SolidColorBrush(bg);
        BorderBrush = new SolidColorBrush(border);
        BorderThickness = new Thickness(HDevTheme.Size.BorderThin);
        BoxShadow = halo is { } h
            ? new BoxShadows(new BoxShadow { Blur = 0, Spread = 2, Color = h })
            : new BoxShadows();
        InnerTextBox.Foreground = new SolidColorBrush(isEnabled ? HDevTheme.Text.Primary : HDevTheme.Text.Disabled);
    }

    /// <summary>Fige survol et pression pour la planche visuelle headless (tests/Gallery)</summary>
    internal new void ForceVisualState(bool hovered = false, bool pressed = false)
    {
        _isPressed = pressed;
        base.ForceVisualState(hovered: hovered);
    }

    // ═══════════════════════════════════════════════════════════════
    // LISTE
    // ═══════════════════════════════════════════════════════════════

    private void OnDropDownOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (EffectiveItems.Count == 0)
        {
            e.Cancel = true;
            return;
        }

        RefreshDropDownItems(null);
        _listBox.SelectedItem = SelectedItem;
        _highlightByUser = false;
        _listBox.MaxHeight = DropDownRows * 24;
        DropDown.Chrome.MaxWidth = Bounds.Width;
    }

    /// <summary>
    /// Fermeture : un choix accepté (Entrée, clic extérieur) prend l'élément en surbrillance si
    /// l'utilisateur l'a déplacée, sinon, en mode éditable, le texte saisi ; Échap ne change rien
    /// (constat E0 n° 2 : la valeur d'ouverture reste)
    /// </summary>
    private void OnDropDownClosed(object? sender, HDevDropDownClosedEventArgs e)
    {
        var highlight = _highlightByUser ? _listBox.SelectedItem : null;
        _highlightByUser = false;
        if (e.IsValueAccepted)
        {
            if (highlight != null)
                CommitItem(highlight);
            else if (e.CloseReason == HDevDropDownCloseReason.LightDismiss)
                CommitTypedText();
        }
        UpdateVisualState();
    }

    /// <summary>Alimente la liste : vue triée si Sorted, filtrée sur le préfixe en mode Suggest</summary>
    private void RefreshDropDownItems(string? filter)
    {
        IEnumerable<object> source = EffectiveItems;
        if (Sorted)
            source = source.OrderBy(GetItemText, StringComparer.CurrentCultureIgnoreCase);

        if (!string.IsNullOrEmpty(filter))
        {
            var comparison = CaseSensitiveSearch
                ? StringComparison.CurrentCulture
                : StringComparison.CurrentCultureIgnoreCase;
            source = source.Where(i => GetItemText(i).StartsWith(filter, comparison));
        }

        var list = source.ToList();
        _listBox.ItemsSource = list;

        if (list.Count == 0 && DropDown.IsOpen)
            DropDown.Close(HDevDropDownCloseReason.Canceled);
    }

    private void OnListBoxPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_listBox.SelectedItem is not { } item) return;
        _highlightByUser = false;
        CommitItem(item);
        DropDown.Close(HDevDropDownCloseReason.Accepted);
    }

    /// <summary>Déplace la surbrillance de la liste ouverte (la valeur ne change qu'à l'acceptation)</summary>
    private void MoveHighlight(int delta, bool toEnd = false)
    {
        var count = _listBox.ItemCount;
        if (count == 0) return;
        var index = toEnd ? (delta < 0 ? 0 : count - 1)
            : Math.Clamp(_listBox.SelectedIndex + delta, 0, count - 1);
        _listBox.SelectedIndex = index;
        _listBox.ScrollIntoView(index);
        _highlightByUser = true;
    }

    // ═══════════════════════════════════════════════════════════════
    // MODE ÉDITABLE : saisie et auto-complétion
    // ═══════════════════════════════════════════════════════════════

    private AutoCompleteMode EffectiveAutoComplete
        => AutoComplete == AutoCompleteMode.Default ? AutoCompleteMode.SuggestAppend : AutoComplete;

    /// <summary>
    /// Changement du texte par l'utilisateur hors complétion (effacement, coupe, frappe sans
    /// correspondance) : filtre les suggestions. Un texte réécrit depuis la valeur n'ouvre rien (APP-09).
    /// </summary>
    protected override void OnInnerTextChanged()
    {
        if (IsUpdatingText || !IsEditable || !InnerTextBox.IsFocused) return;
        Suggest(InnerText);
    }

    /// <summary>Suggest : ouvrir et filtrer la liste sur le préfixe saisi</summary>
    private void Suggest(string typed)
    {
        if (string.IsNullOrEmpty(typed))
        {
            if (DropDown.IsOpen)
                RefreshDropDownItems(null);
            return;
        }

        if (EffectiveAutoComplete is AutoCompleteMode.Suggest or AutoCompleteMode.SuggestAppend || OpenDropDownOnInput)
        {
            if (!DropDown.IsOpen && !DropDown.JustClosedByGuard)
                DropDown.Open();
            if (DropDown.IsOpen)
                RefreshDropDownItems(typed);
        }
    }

    /// <summary>
    /// Append : la frappe est complétée avec le premier item correspondant, le suffixe ajouté
    /// restant sélectionné. Traitée avant le champ, frappe par frappe : la lettre suivante
    /// remplace le suffixe et complète à nouveau, quelle que soit la vitesse de frappe (APP-09)
    /// </summary>
    private void OnEditTextInput(object? sender, TextInputEventArgs e)
    {
        if (!IsEditable || string.IsNullOrEmpty(e.Text) || IsReadOnly
            || EffectiveAutoComplete is not (AutoCompleteMode.Append or AutoCompleteMode.SuggestAppend))
            return;

        var text = InnerText;
        var start = Math.Min(InnerTextBox.SelectionStart, InnerTextBox.SelectionEnd);
        var end = Math.Max(InnerTextBox.SelectionStart, InnerTextBox.SelectionEnd);
        if (start == end) start = end = Math.Min(InnerTextBox.CaretIndex, text.Length);
        // Compléter seulement en bout de texte
        if (end < text.Length) return;

        var typed = text[..start] + e.Text;
        var index = FindItemByPrefix(typed);
        if (index < 0) return;
        var full = GetItemText(EffectiveItems[index]);
        if (full.Length <= typed.Length) return;

        e.Handled = true;
        SetTextFromValue(full);
        InnerTextBox.SelectionStart = typed.Length;
        InnerTextBox.SelectionEnd = full.Length;
        Suggest(typed);
    }

    private void OnEditTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsEditable || e.Handled) return;
        var open = DropDown.IsOpen;
        var plain = e.KeyModifiers == KeyModifiers.None;

        switch (e.Key)
        {
            case Key.Down when open && plain:
                MoveHighlight(1);
                e.Handled = true;
                break;

            case Key.Up when open && plain:
                MoveHighlight(-1);
                e.Handled = true;
                break;

            case Key.Enter when plain:
                if (open && _highlightByUser && _listBox.SelectedItem != null)
                {
                    var item = _listBox.SelectedItem;
                    _highlightByUser = false;
                    CommitItem(item);
                }
                else
                    CommitTypedText();
                DropDown.Close(HDevDropDownCloseReason.Canceled);
                e.Handled = true;
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // MODE NON ÉDITABLE : clavier et souris du champ
    // ═══════════════════════════════════════════════════════════════

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsEditable && !e.Handled && IsEffectivelyEnabled && !IsReadOnly && e.KeyModifiers == KeyModifiers.None)
            HandleSelectKey(e);
        base.OnKeyDown(e);
    }

    private void HandleSelectKey(KeyEventArgs e)
    {
        var open = DropDown.IsOpen;
        switch (e.Key)
        {
            case Key.Enter or Key.Space when open:
                DropDown.Close(HDevDropDownCloseReason.Accepted);
                e.Handled = true;
                break;
            case Key.Enter or Key.Space:
                DropDown.Open();
                e.Handled = true;
                break;
            case Key.Down:
                if (open) MoveHighlight(1); else SelectItemAt(SelectedIndex + 1);
                e.Handled = true;
                break;
            case Key.Up:
                if (open) MoveHighlight(-1); else if (SelectedIndex > 0) SelectItemAt(SelectedIndex - 1);
                e.Handled = true;
                break;
            case Key.Home:
                if (open) MoveHighlight(-1, toEnd: true); else SelectItemAt(0);
                e.Handled = true;
                break;
            case Key.End:
                if (open) MoveHighlight(1, toEnd: true); else SelectItemAt(EffectiveItems.Count - 1);
                e.Handled = true;
                break;
            case Key.Delete or Key.Back when !open:
                // AllowNull : effacer la valeur au clavier
                if (AllowNull && Value != null)
                    CommitItem(null);
                e.Handled = true;
                break;
        }
    }

    /// <summary>Fermée : l'élément devient la valeur ; ouverte : il passe en surbrillance</summary>
    private void SelectItemAt(int index)
    {
        var items = EffectiveItems;
        if (index < 0 || index >= items.Count) return;

        if (DropDown.IsOpen)
        {
            _listBox.SelectedItem = items[index];
            _highlightByUser = true;
        }
        else
            CommitItem(items[index]);
    }

    // Sélection par préfixe : taper les premières lettres d'un item le sélectionne
    // (préfixe réinitialisé après 1 s d'inactivité)
    private string _searchPrefix = "";
    private DateTime _lastSearchInput;

    protected override void OnTextInput(TextInputEventArgs e)
    {
        // En mode éditable, la saisie va au champ (auto-complétion)
        if (IsEditable || e.Handled || !IsEffectivelyEnabled || IsReadOnly || string.IsNullOrEmpty(e.Text))
        {
            base.OnTextInput(e);
            return;
        }

        var now = DateTime.UtcNow;
        if ((now - _lastSearchInput).TotalMilliseconds > 1000)
            _searchPrefix = "";
        _lastSearchInput = now;

        // Espace seul = ouverture de la liste (OnKeyDown), pas une recherche
        if (e.Text == " " && _searchPrefix.Length == 0)
        {
            base.OnTextInput(e);
            return;
        }

        _searchPrefix += e.Text;
        var index = FindItemByPrefix(_searchPrefix);
        if (index >= 0)
            SelectItemAt(index);

        e.Handled = true;
        base.OnTextInput(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!IsEditable && IsEffectivelyEnabled && !IsReadOnly)
        {
            _isPressed = true;
            UpdateVisualState();
        }
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_isPressed)
        {
            _isPressed = false;
            UpdateVisualState();
        }
        base.OnPointerReleased(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _isPressed = false;
        base.OnPointerExited(e);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        RefreshPresenter();
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        // Liste non éditable : le focus qui part ailleurs que vers la liste la ferme
        if (!IsEditable && DropDown.IsOpen && !IsKeyboardFocusWithin && !DropDown.Contains(e.NewFocusedElement))
            DropDown.Close(HDevDropDownCloseReason.LightDismiss);
        RefreshPresenter();
    }
}
