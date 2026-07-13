using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using ProControls.Theme;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace ProControls.Controls;

// ═══════════════════════════════════════════════════════════════════════════
// CLASSES DE SUPPORT (similaires à DevExpress)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Collection d'items du ComboBox (équivalent à ComboBoxItemCollection)
/// </summary>
public class ProComboBoxItemCollection : ObservableCollection<object>
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

/// <summary>
/// Propriétés du ComboBox (équivalent à RepositoryItemComboBox)
/// </summary>
public class ProComboBoxProperties : INotifyPropertyChanged
{
    private readonly ProComboBox _owner;
    private ProComboBoxItemCollection _items;
    private bool _allowNullInput = true;
    private string _nullText = "";
    private string _nullValuePrompt = "";
    private ShowNullValuePromptOptions _showNullValuePrompt = ShowNullValuePromptOptions.EmptyValue;
    private bool _readOnly;
    private int _dropDownRows = 7;
    private bool _showDropDown = true;
    private bool _caseSensitiveSearch;
    private bool _sorted;
    private TextEditStyles _textEditStyle = TextEditStyles.DisableTextEditor;
    private AutoCompleteMode _autoComplete = AutoCompleteMode.Default;
    private bool _immediatePopup;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ProComboBoxProperties(ProComboBox owner)
    {
        _owner = owner;
        _items = new ProComboBoxItemCollection();
        _items.CollectionChanged += (s, e) => _owner.OnItemsChanged();
    }

    /// <summary>
    /// Collection des items du dropdown
    /// </summary>
    public ProComboBoxItemCollection Items => _items;

    /// <summary>
    /// Permet les valeurs null
    /// </summary>
    public bool AllowNullInput
    {
        get => _allowNullInput;
        set { _allowNullInput = value; OnPropertyChanged(nameof(AllowNullInput)); }
    }

    /// <summary>
    /// Texte affiché quand la valeur est null
    /// </summary>
    public string NullText
    {
        get => _nullText;
        set { _nullText = value; OnPropertyChanged(nameof(NullText)); _owner.InvalidateVisual(); }
    }

    /// <summary>
    /// Texte de placeholder (NullValuePrompt)
    /// </summary>
    public string NullValuePrompt
    {
        get => _nullValuePrompt;
        set { _nullValuePrompt = value; OnPropertyChanged(nameof(NullValuePrompt)); _owner.OnPropertiesVisualChanged(); }
    }

    /// <summary>
    /// Quand afficher le NullValuePrompt
    /// </summary>
    public ShowNullValuePromptOptions ShowNullValuePrompt
    {
        get => _showNullValuePrompt;
        set { _showNullValuePrompt = value; OnPropertyChanged(nameof(ShowNullValuePrompt)); }
    }

    /// <summary>
    /// Mode lecture seule
    /// </summary>
    public bool ReadOnly
    {
        get => _readOnly;
        set { _readOnly = value; OnPropertyChanged(nameof(ReadOnly)); _owner.OnPropertiesVisualChanged(); }
    }

    /// <summary>
    /// Nombre de lignes visibles dans le dropdown
    /// </summary>
    public int DropDownRows
    {
        get => _dropDownRows;
        set { _dropDownRows = Math.Max(1, value); OnPropertyChanged(nameof(DropDownRows)); }
    }

    /// <summary>
    /// Afficher le bouton dropdown
    /// </summary>
    public bool ShowDropDown
    {
        get => _showDropDown;
        set { _showDropDown = value; OnPropertyChanged(nameof(ShowDropDown)); _owner.InvalidateVisual(); }
    }

    /// <summary>
    /// Recherche sensible à la casse (type-to-select clavier, FindItem)
    /// </summary>
    public bool CaseSensitiveSearch
    {
        get => _caseSensitiveSearch;
        set { _caseSensitiveSearch = value; OnPropertyChanged(nameof(CaseSensitiveSearch)); }
    }

    /// <summary>
    /// Trier les items affichés dans le dropdown (ordre alphabétique du texte affiché)
    /// </summary>
    public bool Sorted
    {
        get => _sorted;
        set { _sorted = value; OnPropertyChanged(nameof(Sorted)); }
    }

    /// <summary>
    /// Style d'édition : Standard = zone de saisie éditable,
    /// DisableTextEditor = sélection seule (défaut), HideTextEditor = pas de texte
    /// </summary>
    public TextEditStyles TextEditStyle
    {
        get => _textEditStyle;
        set { _textEditStyle = value; OnPropertyChanged(nameof(TextEditStyle)); _owner.OnTextEditStyleChanged(); }
    }

    /// <summary>
    /// Auto-complétion en mode Standard (Default = SuggestAppend) :
    /// Suggest filtre le dropdown, Append complète le texte saisi
    /// </summary>
    public AutoCompleteMode AutoComplete
    {
        get => _autoComplete;
        set { _autoComplete = value; OnPropertyChanged(nameof(AutoComplete)); }
    }

    /// <summary>Ouvrir le dropdown dès la première frappe (mode Standard)</summary>
    public bool ImmediatePopup
    {
        get => _immediatePopup;
        set { _immediatePopup = value; OnPropertyChanged(nameof(ImmediatePopup)); }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Options d'affichage du placeholder
/// </summary>
public enum ShowNullValuePromptOptions
{
    EmptyValue,
    EditorFocused,
    EditorReadOnly
}

/// <summary>
/// Style d'édition du texte
/// </summary>
public enum TextEditStyles
{
    /// <summary>Zone de saisie éditable avec auto-complétion</summary>
    Standard,
    /// <summary>Texte affiché mais non éditable — sélection par dropdown seule (défaut)</summary>
    DisableTextEditor,
    /// <summary>Aucun texte affiché</summary>
    HideTextEditor
}

/// <summary>
/// Mode d'auto-complétion (mode Standard)
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

// ═══════════════════════════════════════════════════════════════════════════
// ÉVÉNEMENTS ARGS
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Arguments pour l'événement CustomDisplayText
/// </summary>
public class ProCustomDisplayTextEventArgs : EventArgs
{
    public object? Value { get; set; }
    public string DisplayText { get; set; } = "";
}

/// <summary>
/// Arguments pour l'événement Popup
/// </summary>
public class ProPopupEventArgs : CancelEventArgs
{
}

/// <summary>
/// Arguments pour l'événement CloseUp
/// </summary>
public class ProCloseUpEventArgs : EventArgs
{
    public object? Value { get; set; }
    public bool AcceptValue { get; set; } = true;
}

/// <summary>
/// Arguments pour l'événement QueryCloseUp
/// </summary>
public class ProQueryCloseUpEventArgs : CancelEventArgs
{
}

// ═══════════════════════════════════════════════════════════════════════════
// PROCOMBOBOX - Clone de DevExpress ComboBoxEdit
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Liste déroulante style VS2022 - API compatible DevExpress ComboBoxEdit
/// </summary>
public class ProComboBox : Control
{
    // ═══════════════════════════════════════════════════════════════
    // CHAMPS PRIVÉS
    // ═══════════════════════════════════════════════════════════════

    private bool _isHovered;
    private bool _isPressed;
    private bool _isDropDownOpen;
    private Popup? _popup;
    private ListBox? _listBox;
    private ProComboBoxProperties _properties;
    private object? _editValue;
    private object? _oldEditValue;
    private bool _isModified;
    private string _text = "";

    // Mode Standard (éditable) : zone de saisie réelle + auto-complétion
    private TextBox? _editTextBox;
    private bool _syncingEditText;
    private int _lastTypedLength;

    private bool IsEditable => _properties.TextEditStyle == TextEditStyles.Standard;

    // ═══════════════════════════════════════════════════════════════
    // STYLED PROPERTIES (Avalonia)
    // ═══════════════════════════════════════════════════════════════

    public static new readonly StyledProperty<bool> IsEnabledProperty =
        AvaloniaProperty.Register<ProComboBox, bool>(nameof(IsEnabled), true);

    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS PRINCIPALES (API DevExpress)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Propriétés du ComboBox (équivalent à ComboBoxEdit.Properties)
    /// </summary>
    public ProComboBoxProperties Properties => _properties;

    /// <summary>
    /// Valeur de l'éditeur (équivalent à EditValue)
    /// </summary>
    public object? EditValue
    {
        get => _editValue;
        set
        {
            if (!Equals(_editValue, value))
            {
                var args = new ProEditValueChangingEventArgs(_editValue, value);
                EditValueChanging?.Invoke(this, args);

                if (!args.Cancel)
                {
                    _editValue = args.NewValue;
                    _isModified = true;
                    UpdateTextFromEditValue();
                    InvalidateVisual();
                    EditValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    /// <summary>
    /// Ancienne valeur (avant modification)
    /// </summary>
    public object? OldEditValue => _oldEditValue;

    /// <summary>
    /// Indique si la valeur a été modifiée
    /// </summary>
    public bool IsModified
    {
        get => _isModified;
        set => _isModified = value;
    }

    /// <summary>
    /// Index de l'item sélectionné
    /// </summary>
    public int SelectedIndex
    {
        get
        {
            if (_editValue == null) return -1;
            return _properties.Items.IndexOf(_editValue);
        }
        set
        {
            if (value >= 0 && value < _properties.Items.Count)
            {
                EditValue = _properties.Items[value];
            }
            else if (value == -1)
            {
                EditValue = null;
            }
        }
    }

    /// <summary>
    /// Item sélectionné
    /// </summary>
    public object? SelectedItem
    {
        get => _editValue;
        set => EditValue = value;
    }

    /// <summary>
    /// Texte affiché
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                SyncEditTextBox();
                InvalidateVisual();
                TextChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Lecture seule
    /// </summary>
    public bool ReadOnly
    {
        get => _properties.ReadOnly;
        set => _properties.ReadOnly = value;
    }

    /// <summary>
    /// Popup ouvert
    /// </summary>
    public bool IsPopupOpen => _isDropDownOpen;

    // ═══════════════════════════════════════════════════════════════
    // ÉVÉNEMENTS (API DevExpress)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Déclenché quand EditValue change
    /// </summary>
    public event EventHandler? EditValueChanged;

    /// <summary>
    /// Déclenché avant que EditValue change (peut être annulé)
    /// </summary>
    public event EventHandler<ProEditValueChangingEventArgs>? EditValueChanging;

    /// <summary>
    /// Déclenché quand SelectedIndex change
    /// </summary>
    public event EventHandler? SelectedIndexChanged;

    /// <summary>
    /// Déclenché quand SelectedValue change
    /// </summary>
    public event EventHandler? SelectedValueChanged;

    /// <summary>
    /// Déclenché quand le texte change
    /// </summary>
    public event EventHandler? TextChanged;

    /// <summary>
    /// Déclenché avant l'ouverture du popup (peut être annulé)
    /// </summary>
    public event EventHandler<ProPopupEventArgs>? Popup;

    /// <summary>
    /// Déclenché à la fermeture du popup
    /// </summary>
    public event EventHandler<ProCloseUpEventArgs>? CloseUp;

    /// <summary>
    /// Déclenché avant la fermeture du popup (peut être annulé)
    /// </summary>
    public event EventHandler<ProQueryCloseUpEventArgs>? QueryCloseUp;

    /// <summary>
    /// Déclenché quand le popup s'ouvre
    /// </summary>
    public event EventHandler? PopupOpened;

    /// <summary>
    /// Déclenché quand le popup se ferme
    /// </summary>
    public event EventHandler? PopupClosed;

    /// <summary>
    /// Déclenché pour personnaliser le texte affiché
    /// </summary>
    public event EventHandler<ProCustomDisplayTextEventArgs>? CustomDisplayText;

    // NOTE : l'événement owner-draw DrawItem (API DevExpress) a été retiré tant
    // que le rendu des items n'est pas personnalisable — utiliser CustomDisplayText.

    /// <summary>
    /// Déclenché lors du clic sur un bouton
    /// </summary>
    public event EventHandler? ButtonClick;

    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTEUR
    // ═══════════════════════════════════════════════════════════════

    static ProComboBox()
    {
        FocusableProperty.OverrideDefaultValue<ProComboBox>(true);
    }

    public ProComboBox()
    {
        _properties = new ProComboBoxProperties(this);
        Height = 28;
        MinWidth = 120;

        CreatePopup();
    }

    private void CreatePopup()
    {
        _listBox = new ListBox
        {
            Background = new SolidColorBrush(ProTheme.Background.Panel),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(2),
            Margin = new Thickness(0),
            SelectionMode = SelectionMode.Single
        };

        // Template pour afficher le texte personnalisé via GetItemText()
        _listBox.ItemTemplate = new FuncDataTemplate<object>((item, _) =>
        {
            return new TextBlock
            {
                Text = GetItemText(item),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(4, 2)
            };
        });

        _listBox.PointerReleased += OnListBoxPointerReleased;

        var scrollViewer = new ScrollViewer
        {
            Content = _listBox,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var dropdownBorder = new Border
        {
                Background = new SolidColorBrush(ProTheme.Background.Panel),
                BorderBrush = new SolidColorBrush(ProTheme.Border.Default),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0, 0, ProTheme.Size.CornerRadiusSmall, ProTheme.Size.CornerRadiusSmall),
                Child = scrollViewer,
                BoxShadow = new BoxShadows(new BoxShadow
                {
                    OffsetX = 0,
                    OffsetY = 2,
                    Blur = 8,
                    Color = ProTheme.Shadow.Color
                })
        };
        // Pas de SubpixelAntialias sur le dropdown : surface popup transparente,
        // le rendu LCD n'y peint aucun glyphe (cf. ProMenuPopup)

        _popup = new Popup
        {
            Child = dropdownBorder,
            PlacementTarget = this,
            Placement = PlacementMode.Bottom,
            // Garde maison (cf. ProMenuPopup) : le light dismiss Avalonia ferme
            // le dropdown à la moindre désactivation de fenêtre
            IsLightDismissEnabled = false,
            // Bas/Haut décidé nous-mêmes dans OpenDropDown (les popups overlay
            // sont clippés aux bords de la fenêtre) : on ne garde que le glissement
            // horizontal automatique, pas le flip vertical qui entrerait en conflit.
            PlacementConstraintAdjustment =
                Avalonia.Controls.Primitives.PopupPositioning.PopupPositionerConstraintAdjustment.SlideX,
            HorizontalOffset = 0,
            VerticalOffset = -1
        };

        _popup.Opened += OnPopupOpenedInstallGuard;
        _popup.Closed += OnPopupClosed;
    }

    private DateTime _lastDropDownClose;
    private TopLevel? _dismissRoot;

    private void OnPopupOpenedInstallGuard(object? sender, EventArgs e)
    {
        _dismissRoot = TopLevel.GetTopLevel(this);
        if (_dismissRoot == null) return;

        _dismissRoot.AddHandler(PointerPressedEvent, OnRootPointerPressed,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        if (_dismissRoot is Window w)
            w.Deactivated += OnRootDeactivated;
    }

    private void RemoveDismissGuard()
    {
        if (_dismissRoot == null) return;
        _dismissRoot.RemoveHandler(PointerPressedEvent, OnRootPointerPressed);
        if (_dismissRoot is Window w)
            w.Deactivated -= OnRootDeactivated;
        _dismissRoot = null;
    }

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Avalonia.Visual v && ProMenuPopup.IsInsidePopupHost(v)) return;

        _lastDropDownClose = DateTime.UtcNow;
        CloseDropDown();
    }

    private void OnRootDeactivated(object? sender, EventArgs e)
    {
        // Certains bureaux (GNOME/XWayland) font rebondir l'activation de la
        // fenêtre en permanence : ne fermer que si elle reste inactive 250 ms
        Avalonia.Threading.DispatcherTimer.RunOnce(() =>
        {
            if (_isDropDownOpen && _dismissRoot is Window { IsActive: false })
                CloseDropDown();
        }, TimeSpan.FromMilliseconds(250));
    }

    private void OnListBoxPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_listBox?.SelectedItem != null)
        {
            var oldIndex = SelectedIndex;
            EditValue = _listBox.SelectedItem;
            CloseDropDown();

            if (oldIndex != SelectedIndex)
            {
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                SelectedValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (_popup != null)
        {
            ((ISetLogicalParent)_popup).SetParent(this);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_popup != null)
        {
            _popup.IsOpen = false;
            ((ISetLogicalParent)_popup).SetParent(null);
        }

        base.OnDetachedFromVisualTree(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // MÉTHODES PUBLIQUES (API DevExpress)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Affiche le popup
    /// </summary>
    public void ShowPopup()
    {
        OpenDropDown();
    }

    /// <summary>
    /// Ferme le popup
    /// </summary>
    public void ClosePopup()
    {
        CloseDropDown();
    }

    /// <summary>
    /// Ferme le popup (alias)
    /// </summary>
    public void HidePopup()
    {
        CloseDropDown();
    }

    /// <summary>
    /// Réinitialise l'éditeur
    /// </summary>
    public void Reset()
    {
        EditValue = null;
        _isModified = false;
        _oldEditValue = null;
    }

    /// <summary>
    /// Sélectionne tout le texte de la zone de saisie (mode Standard)
    /// </summary>
    public void SelectAll() => _editTextBox?.SelectAll();

    /// <summary>
    /// Désélectionne le texte de la zone de saisie (mode Standard)
    /// </summary>
    public void DeselectAll() => _editTextBox?.ClearSelection();

    /// <summary>
    /// Trouve un item par son texte
    /// </summary>
    public int FindItem(string text)
    {
        return FindItem(text, 0);
    }

    /// <summary>
    /// Trouve un item par son texte à partir d'un index
    /// </summary>
    public int FindItem(string text, int startIndex)
    {
        var comparison = _properties.CaseSensitiveSearch
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        for (int i = startIndex; i < _properties.Items.Count; i++)
        {
            var itemText = GetItemText(_properties.Items[i]);
            if (itemText.Equals(text, comparison))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Trouve un item par préfixe
    /// </summary>
    public int FindItemByPrefix(string prefix)
    {
        var comparison = _properties.CaseSensitiveSearch
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        for (int i = 0; i < _properties.Items.Count; i++)
        {
            var itemText = GetItemText(_properties.Items[i]);
            if (itemText.StartsWith(prefix, comparison))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Obtient le texte affiché pour un item
    /// </summary>
    public string GetItemText(object? item)
    {
        if (item == null) return "";

        // Permettre la personnalisation via événement
        var args = new ProCustomDisplayTextEventArgs
        {
            Value = item,
            DisplayText = item.ToString() ?? ""
        };
        CustomDisplayText?.Invoke(this, args);

        return args.DisplayText;
    }

    /// <summary>
    /// Rafraîchit l'affichage
    /// </summary>
    public void RefreshEditValue()
    {
        UpdateTextFromEditValue();
        InvalidateVisual();
    }

    /// <summary>
    /// Force la validation
    /// </summary>
    public bool DoValidate()
    {
        _oldEditValue = _editValue;
        _isModified = false;
        return true;
    }

    // ═══════════════════════════════════════════════════════════════
    // MÉTHODES INTERNES
    // ═══════════════════════════════════════════════════════════════

    internal void OnItemsChanged()
    {
        if (_listBox != null)
        {
            _listBox.ItemsSource = _properties.Items;
        }
        InvalidateVisual();
    }

    /// <summary>Répercute NullValuePrompt/ReadOnly sur la zone de saisie interne</summary>
    internal void OnPropertiesVisualChanged()
    {
        if (_editTextBox != null)
        {
            _editTextBox.Watermark = _properties.NullValuePrompt;
            _editTextBox.IsReadOnly = _properties.ReadOnly;
        }
        InvalidateVisual();
    }

    private void UpdateTextFromEditValue()
    {
        _text = GetItemText(_editValue);
        SyncEditTextBox();
    }

    // ═══════════════════════════════════════════════════════════════
    // MODE ÉDITABLE (TextEditStyle.Standard) : saisie + auto-complétion
    // ═══════════════════════════════════════════════════════════════

    internal void OnTextEditStyleChanged()
    {
        if (IsEditable && _editTextBox == null)
            CreateEditTextBox();
        else if (!IsEditable && _editTextBox != null)
            RemoveEditTextBox();

        InvalidateMeasure();
        InvalidateVisual();
    }

    private void CreateEditTextBox()
    {
        _editTextBox = new TextBox
        {
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Padding = new Thickness(8, 0, 0, 0),
            MinHeight = 0,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            FontFamily = new FontFamily(ProTheme.Typography.FontFamily),
            FontSize = ProTheme.Typography.FontSizeBody,
            Foreground = new SolidColorBrush(ProTheme.Text.Primary),
            CaretBrush = new SolidColorBrush(ProTheme.Text.Primary),
            SelectionBrush = new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Accent.Primary, 80)),
            SelectionForegroundBrush = new SolidColorBrush(Colors.White),
            Watermark = _properties.NullValuePrompt,
            IsReadOnly = _properties.ReadOnly,
            Text = _text
        };

        _editTextBox.TextChanged += (s, e) => OnEditTextChanged();
        _editTextBox.AddHandler(KeyDownEvent, OnEditTextBoxKeyDown, RoutingStrategies.Tunnel);
        _editTextBox.GotFocus += (s, e) => InvalidateVisual();
        _editTextBox.LostFocus += (s, e) =>
        {
            // Ne pas commiter si le focus part vers le dropdown (clic sur un item)
            if (!_isDropDownOpen)
                CommitTypedText();
            InvalidateVisual();
        };

        VisualChildren.Add(_editTextBox);
        LogicalChildren.Add(_editTextBox);
    }

    private void RemoveEditTextBox()
    {
        if (_editTextBox == null) return;
        VisualChildren.Remove(_editTextBox);
        LogicalChildren.Remove(_editTextBox);
        _editTextBox = null;
    }

    private void SyncEditTextBox()
    {
        if (_editTextBox != null && _editTextBox.Text != _text)
        {
            _syncingEditText = true;
            _editTextBox.Text = _text;
            _lastTypedLength = _text.Length;
            _syncingEditText = false;
        }
    }

    private void OnEditTextChanged()
    {
        if (_syncingEditText || _editTextBox == null) return;

        var typed = _editTextBox.Text ?? "";
        var isDeletion = typed.Length < _lastTypedLength;
        _lastTypedLength = typed.Length;

        _text = typed;
        TextChanged?.Invoke(this, EventArgs.Empty);

        var mode = _properties.AutoComplete == AutoCompleteMode.Default
            ? AutoCompleteMode.SuggestAppend
            : _properties.AutoComplete;

        if (string.IsNullOrEmpty(typed))
        {
            if (_isDropDownOpen)
                RefreshDropDownItems(null);
            return;
        }

        // Suggest : ouvrir et filtrer le dropdown sur le préfixe saisi
        if (mode is AutoCompleteMode.Suggest or AutoCompleteMode.SuggestAppend
            || _properties.ImmediatePopup)
        {
            if (!_isDropDownOpen && (DateTime.UtcNow - _lastDropDownClose).TotalMilliseconds > 250)
                OpenDropDown();
            if (_isDropDownOpen)
                RefreshDropDownItems(typed);
        }

        // Append : compléter avec le premier item correspondant (pas sur effacement)
        if (mode is AutoCompleteMode.Append or AutoCompleteMode.SuggestAppend && !isDeletion)
        {
            var index = FindItemByPrefix(typed);
            if (index >= 0)
            {
                var full = GetItemText(_properties.Items[index]);
                if (full.Length > typed.Length)
                {
                    _syncingEditText = true;
                    _editTextBox.Text = full;
                    _editTextBox.SelectionStart = typed.Length;
                    _editTextBox.SelectionEnd = full.Length;
                    _lastTypedLength = full.Length;
                    _syncingEditText = false;
                    _text = full;
                }
            }
        }
    }

    /// <summary>
    /// Alimente le dropdown : vue triée si Sorted, filtrée sur le préfixe en mode Suggest
    /// </summary>
    private void RefreshDropDownItems(string? filter)
    {
        if (_listBox == null) return;

        IEnumerable<object> source = _properties.Items;
        if (_properties.Sorted)
            source = source.OrderBy(GetItemText, StringComparer.CurrentCultureIgnoreCase);

        if (!string.IsNullOrEmpty(filter))
        {
            var comparison = _properties.CaseSensitiveSearch
                ? StringComparison.CurrentCulture
                : StringComparison.CurrentCultureIgnoreCase;
            source = source.Where(i => GetItemText(i).StartsWith(filter, comparison));
        }

        var list = source.ToList();
        _listBox.ItemsSource = list;

        if (list.Count == 0 && _isDropDownOpen)
            CloseDropDown();
    }

    private void OnEditTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F4:
            case Key.Down when e.KeyModifiers.HasFlag(KeyModifiers.Alt):
                ToggleDropDown();
                e.Handled = true;
                break;

            case Key.Escape when _isDropDownOpen:
                CloseDropDown();
                e.Handled = true;
                break;

            case Key.Down when _isDropDownOpen:
                MoveListSelection(1);
                e.Handled = true;
                break;

            case Key.Up when _isDropDownOpen:
                MoveListSelection(-1);
                e.Handled = true;
                break;

            case Key.Enter:
                if (_isDropDownOpen && _listBox?.SelectedItem != null)
                    CommitItem(_listBox.SelectedItem);
                else
                    CommitTypedText();
                CloseDropDown();
                e.Handled = true;
                break;
        }
    }

    private void MoveListSelection(int delta)
    {
        if (_listBox == null || _listBox.ItemCount == 0) return;
        _listBox.SelectedIndex = Math.Clamp(_listBox.SelectedIndex + delta, 0, _listBox.ItemCount - 1);
        _listBox.ScrollIntoView(_listBox.SelectedIndex);
    }

    private void CommitItem(object item)
    {
        var oldIndex = SelectedIndex;
        EditValue = item;

        if (oldIndex != SelectedIndex)
        {
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            SelectedValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Valide le texte saisi : item correspondant si trouvé, sinon texte libre
    /// (comportement ComboBoxEdit DevExpress)
    /// </summary>
    private void CommitTypedText()
    {
        if (!IsEditable || _editTextBox == null) return;

        var typed = _editTextBox.Text ?? "";

        if (string.IsNullOrEmpty(typed))
        {
            if (_properties.AllowNullInput && _editValue != null)
                CommitItem(null!);
            return;
        }

        var index = FindItem(typed);
        if (index >= 0)
            CommitItem(_properties.Items[index]);
        else if (!Equals(_editValue, typed))
            EditValue = typed; // texte libre
    }

    // ═══════════════════════════════════════════════════════════════
    // MESURE
    // ═══════════════════════════════════════════════════════════════

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = Height > 0 ? Height : 28;
        var width = Width > 0 ? Width : (MinWidth > 0 ? MinWidth : 120);

        _editTextBox?.Measure(availableSize);

        if (double.IsInfinity(availableSize.Width))
            return new Size(width, height);

        return new Size(Math.Min(availableSize.Width, width), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Zone de saisie = zone texte (le bouton dropdown reste dessiné/hit-testé)
        if (_editTextBox != null)
        {
            var arrowWidth = _properties.ShowDropDown ? 24 : 0;
            var rect = new Rect(1, 1,
                Math.Max(0, finalSize.Width - arrowWidth - 2), Math.Max(0, finalSize.Height - 2));
            _editTextBox.Arrange(rect);
        }
        return finalSize;
    }

    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var cornerRadius = ProTheme.Size.CornerRadiusSmall;

        // Couleurs selon l'état
        Color bgColor, borderColor, textColor;

        if (!IsEnabled)
        {
            bgColor = ProTheme.Background.ControlDisabled;
            borderColor = ProTheme.Border.Subtle;
            textColor = ProTheme.Text.Disabled;
        }
        else if (_isDropDownOpen)
        {
            bgColor = ProTheme.Background.Control;
            borderColor = ProTheme.Accent.Primary;
            textColor = ProTheme.Text.Primary;
        }
        else if (_isPressed)
        {
            bgColor = ProTheme.Background.ControlPressed;
            borderColor = ProTheme.Accent.Primary;
            textColor = ProTheme.Text.Primary;
        }
        else if (_isHovered)
        {
            bgColor = ProTheme.Background.ControlHover;
            borderColor = ProTheme.Border.Default;
            textColor = ProTheme.Text.Primary;
        }
        else
        {
            bgColor = ProTheme.Background.Control;
            borderColor = ProTheme.Border.Default;
            textColor = ProTheme.Text.Primary;
        }

        // Focus (en mode éditable, c'est le TextBox interne qui porte le focus)
        if ((IsFocused || _editTextBox?.IsFocused == true) && IsEnabled)
        {
            borderColor = ProTheme.Accent.Primary;
        }

        // ReadOnly
        if (_properties.ReadOnly)
        {
            bgColor = ProTheme.Background.ControlDisabled;
        }

        // Fond
        context.DrawRectangle(
            new SolidColorBrush(bgColor),
            null,
            bounds,
            cornerRadius, cornerRadius);

        // Bordure
        var borderPen = new Pen(new SolidColorBrush(borderColor), 1);
        context.DrawRectangle(null, borderPen, bounds.Deflate(0.5), cornerRadius, cornerRadius);

        // Zone de texte
        var arrowWidth = _properties.ShowDropDown ? 24 : 0;
        var textBounds = new Rect(8, 0, bounds.Width - arrowWidth - 8, bounds.Height);

        // Déterminer le texte à afficher — en mode Standard c'est le TextBox
        // interne qui affiche texte et placeholder ; en HideTextEditor, rien
        string displayText;
        bool isPlaceholder = false;

        if (IsEditable || _properties.TextEditStyle == TextEditStyles.HideTextEditor)
        {
            displayText = "";
        }
        else if (_editValue == null || string.IsNullOrEmpty(_text))
        {
            // ShowNullValuePrompt conditionne l'affichage du placeholder
            var promptAllowed = _properties.ShowNullValuePrompt switch
            {
                ShowNullValuePromptOptions.EmptyValue => !_properties.ReadOnly,
                ShowNullValuePromptOptions.EditorFocused => IsFocused && !_properties.ReadOnly,
                ShowNullValuePromptOptions.EditorReadOnly => true,
                _ => true
            };

            // Afficher NullValuePrompt ou NullText
            if (promptAllowed && !string.IsNullOrEmpty(_properties.NullValuePrompt))
            {
                displayText = _properties.NullValuePrompt;
                isPlaceholder = true;
            }
            else if (!string.IsNullOrEmpty(_properties.NullText))
            {
                displayText = _properties.NullText;
                isPlaceholder = true;
            }
            else
            {
                displayText = "";
            }
        }
        else
        {
            displayText = _text;
        }

        if (!string.IsNullOrEmpty(displayText))
        {
            var formattedText = new FormattedText(
                displayText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(ProTheme.Typography.FontFamily),
                ProTheme.Typography.FontSizeBody,
                new SolidColorBrush(isPlaceholder ? ProTheme.Text.Secondary : textColor));

            var textY = (bounds.Height - formattedText.Height) / 2;

            using (context.PushClip(textBounds))
            {
                context.DrawText(formattedText, Crisp.Snap(new Point(textBounds.X, textY)));
            }
        }

        // Flèche dropdown
        if (_properties.ShowDropDown)
        {
            var arrowX = bounds.Width - arrowWidth / 2;
            var arrowY = bounds.Height / 2;
            var arrowSize = 4.0;

            var arrowGeometry = new StreamGeometry();
            using (var ctx = arrowGeometry.Open())
            {
                if (_isDropDownOpen)
                {
                    ctx.BeginFigure(new Point(arrowX - arrowSize, arrowY + arrowSize / 2), true);
                    ctx.LineTo(new Point(arrowX, arrowY - arrowSize / 2));
                    ctx.LineTo(new Point(arrowX + arrowSize, arrowY + arrowSize / 2));
                    ctx.EndFigure(true);
                }
                else
                {
                    ctx.BeginFigure(new Point(arrowX - arrowSize, arrowY - arrowSize / 2), true);
                    ctx.LineTo(new Point(arrowX, arrowY + arrowSize / 2));
                    ctx.LineTo(new Point(arrowX + arrowSize, arrowY - arrowSize / 2));
                    ctx.EndFigure(true);
                }
            }

            context.DrawGeometry(
                new SolidColorBrush(IsEnabled ? ProTheme.Text.Secondary : ProTheme.Text.Disabled),
                null,
                arrowGeometry);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // INTERACTIONS
    // ═══════════════════════════════════════════════════════════════

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        _isHovered = true;
        InvalidateVisual();
        base.OnPointerEntered(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _isHovered = false;
        _isPressed = false;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    /// <summary>En mode éditable, seul le bouton flèche ouvre le dropdown</summary>
    private bool IsInDropDownButton(Point pos)
        => !IsEditable || pos.X >= Bounds.Width - 24;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!IsEnabled || _properties.ReadOnly)
            return;

        // Clic dans la zone de saisie : laisser le TextBox interne prendre le focus
        if (IsEditable && !IsInDropDownButton(e.GetPosition(this)))
        {
            base.OnPointerPressed(e);
            return;
        }

        _isPressed = true;
        InvalidateVisual();

        e.Handled = true;
        if (!IsEditable)
            Focus();

        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!IsEnabled || _properties.ReadOnly)
            return;

        if (IsEditable && !IsInDropDownButton(e.GetPosition(this)))
        {
            base.OnPointerReleased(e);
            return;
        }

        if (_isPressed && _isHovered)
        {
            ToggleDropDown();
            ButtonClick?.Invoke(this, EventArgs.Empty);
        }

        _isPressed = false;
        InvalidateVisual();

        base.OnPointerReleased(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsEnabled || _properties.ReadOnly)
            return;

        switch (e.Key)
        {
            case Key.Enter:
            case Key.Space:
                ToggleDropDown();
                e.Handled = true;
                break;

            case Key.Escape:
                if (_isDropDownOpen)
                {
                    CloseDropDown();
                    e.Handled = true;
                }
                break;

            case Key.Down:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))
                {
                    OpenDropDown();
                }
                else if (_isDropDownOpen)
                {
                    SelectNextItem();
                }
                else
                {
                    SelectNextItem();
                }
                e.Handled = true;
                break;

            case Key.Up:
                if (_isDropDownOpen)
                {
                    SelectPreviousItem();
                }
                else
                {
                    SelectPreviousItem();
                }
                e.Handled = true;
                break;

            case Key.Home:
                SelectFirstItem();
                e.Handled = true;
                break;

            case Key.End:
                SelectLastItem();
                e.Handled = true;
                break;

            case Key.F4:
                ToggleDropDown();
                e.Handled = true;
                break;

            case Key.Delete:
            case Key.Back:
                // AllowNullInput : effacer la valeur au clavier
                if (_properties.AllowNullInput && _editValue != null)
                {
                    var oldIdx = SelectedIndex;
                    EditValue = null;
                    if (_listBox != null)
                        _listBox.SelectedItem = null;
                    if (oldIdx != -1)
                    {
                        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                        SelectedValueChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }

    // Type-to-select : taper les premières lettres d'un item le sélectionne
    // (buffer de préfixe réinitialisé après 1 s d'inactivité, cf. combos WinForms)
    private string _searchPrefix = "";
    private DateTime _lastSearchInput;

    protected override void OnTextInput(TextInputEventArgs e)
    {
        // En mode éditable, la saisie va au TextBox interne (auto-complétion)
        if (IsEditable || !IsEnabled || _properties.ReadOnly || string.IsNullOrEmpty(e.Text))
        {
            base.OnTextInput(e);
            return;
        }

        var now = DateTime.UtcNow;
        if ((now - _lastSearchInput).TotalMilliseconds > 1000)
            _searchPrefix = "";
        _lastSearchInput = now;

        // Espace seul = toggle du dropdown (géré par OnKeyDown), pas une recherche
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

    private void SelectItemAt(int index)
    {
        if (index < 0 || index >= _properties.Items.Count)
            return;

        var oldIndex = SelectedIndex;
        SelectedIndex = index;
        if (_listBox != null)
            _listBox.SelectedIndex = index;

        if (oldIndex != SelectedIndex)
        {
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            SelectedValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        _oldEditValue = _editValue;
        InvalidateVisual();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        DoValidate();
        InvalidateVisual();
        base.OnLostFocus(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // DROPDOWN
    // ═══════════════════════════════════════════════════════════════

    private void ToggleDropDown()
    {
        if (_isDropDownOpen)
        {
            CloseDropDown();
        }
        else if ((DateTime.UtcNow - _lastDropDownClose).TotalMilliseconds > 250)
        {
            // Ignorer la réouverture si le garde vient de fermer sur ce même clic
            OpenDropDown();
        }
    }

    private void OpenDropDown()
    {
        if (_popup == null || _listBox == null || _properties.Items.Count == 0)
            return;

        // Événement Popup (peut être annulé)
        var popupArgs = new ProPopupEventArgs();
        Popup?.Invoke(this, popupArgs);
        if (popupArgs.Cancel)
            return;

        // Mettre à jour les items (vue triée si Sorted, la collection n'est pas modifiée)
        RefreshDropDownItems(null);
        _listBox.SelectedItem = _editValue;

        // Largeur du popup = largeur du combo
        if (_popup.Child is Border border)
        {
            border.MinWidth = Bounds.Width;
            border.MaxWidth = Bounds.Width;
        }

        // Hauteur basée sur DropDownRows
        var itemHeight = 24;
        _listBox.MaxHeight = _properties.DropDownRows * itemHeight;

        // Placement Bas par défaut, basculé en Haut si le dropdown déborderait
        // le bas de la fenêtre : un popup overlay est clippé aux bords de la
        // fenêtre principale, il ne peut pas s'afficher en dehors.
        var visibleRows = Math.Min(_properties.Items.Count, _properties.DropDownRows);
        var estimatedHeight = visibleRows * itemHeight + 2; // + bordure
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null && this.TranslatePoint(new Point(0, 0), topLevel) is { } comboTopInWin)
        {
            var spaceBelow = topLevel.ClientSize.Height - (comboTopInWin.Y + Bounds.Height);
            var spaceAbove = comboTopInWin.Y;
            if (spaceBelow < estimatedHeight && spaceAbove > spaceBelow)
            {
                _popup.Placement = PlacementMode.Top;
                _popup.VerticalOffset = 1; // chevauche la bordure du combo
            }
            else
            {
                _popup.Placement = PlacementMode.Bottom;
                _popup.VerticalOffset = -1;
            }
        }

        _isDropDownOpen = true;
        _popup.IsOpen = true;
        InvalidateVisual();

        PopupOpened?.Invoke(this, EventArgs.Empty);
    }

    private void CloseDropDown()
    {
        if (_popup == null)
            return;

        // QueryCloseUp (peut être annulé)
        var queryArgs = new ProQueryCloseUpEventArgs();
        QueryCloseUp?.Invoke(this, queryArgs);
        if (queryArgs.Cancel)
            return;

        _isDropDownOpen = false;
        _popup.IsOpen = false;
        InvalidateVisual();

        // En mode éditable, rendre le focus à la zone de saisie, pas au contrôle
        if (IsEditable)
            _editTextBox?.Focus();
        else
            Focus();

        // CloseUp
        var closeUpArgs = new ProCloseUpEventArgs { Value = _editValue };
        CloseUp?.Invoke(this, closeUpArgs);

        PopupClosed?.Invoke(this, EventArgs.Empty);
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        RemoveDismissGuard();

        if (_isDropDownOpen)
        {
            _isDropDownOpen = false;
            InvalidateVisual();
            PopupClosed?.Invoke(this, EventArgs.Empty);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // NAVIGATION
    // ═══════════════════════════════════════════════════════════════

    private void SelectNextItem()
    {
        var newIndex = SelectedIndex + 1;
        if (newIndex < _properties.Items.Count)
        {
            var oldIndex = SelectedIndex;
            SelectedIndex = newIndex;
            if (_listBox != null)
                _listBox.SelectedIndex = newIndex;

            if (oldIndex != SelectedIndex)
            {
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                SelectedValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void SelectPreviousItem()
    {
        var newIndex = SelectedIndex - 1;
        if (newIndex >= 0)
        {
            var oldIndex = SelectedIndex;
            SelectedIndex = newIndex;
            if (_listBox != null)
                _listBox.SelectedIndex = newIndex;

            if (oldIndex != SelectedIndex)
            {
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                SelectedValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void SelectFirstItem()
    {
        if (_properties.Items.Count > 0)
        {
            var oldIndex = SelectedIndex;
            SelectedIndex = 0;
            if (_listBox != null)
                _listBox.SelectedIndex = 0;

            if (oldIndex != SelectedIndex)
            {
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                SelectedValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void SelectLastItem()
    {
        if (_properties.Items.Count > 0)
        {
            var oldIndex = SelectedIndex;
            SelectedIndex = _properties.Items.Count - 1;
            if (_listBox != null)
                _listBox.SelectedIndex = _properties.Items.Count - 1;

            if (oldIndex != SelectedIndex)
            {
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                SelectedValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// ÉVÉNEMENT ARGS SUPPLÉMENTAIRES
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Arguments pour EditValueChanging
/// </summary>
public class ProEditValueChangingEventArgs : CancelEventArgs
{
    public object? OldValue { get; }
    public object? NewValue { get; set; }

    public ProEditValueChangingEventArgs(object? oldValue, object? newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}
