using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using ProControls.Theme;
using Julien.Avalonia.DataGrid.Controls;
using Julien.Avalonia.DataGrid.Models;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;

namespace ProControls.Controls;

/// <summary>
/// Éditeur de recherche dans un référentiel (équivalent LookUpEdit/GridLookUpEdit) :
/// le dropdown est un JDataGrid multi-colonnes ; la sélection d'une ligne pose
/// EditValue (ValueMember) et affiche DisplayMember
/// </summary>
public class ProLookUpEdit : Control
{
    private bool _isHovered;
    private bool _isDropDownOpen;
    private object? _editValue;
    private object? _selectedRow;

    private readonly Popup _popup;
    private readonly JDataGrid _grid;
    private readonly ProPopupDismissGuard _guard;
    private bool _syncingSelection;

    private static readonly Dictionary<(Type, string), PropertyInfo?> PropertyCache = new();

    // ═══════════════════════════════════════════════════════════════
    // API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Source de données du référentiel</summary>
    public IEnumerable? DataSource { get; set; }

    /// <summary>Propriété affichée dans la zone de texte</summary>
    public string DisplayMember { get; set; } = "";

    /// <summary>Propriété utilisée comme valeur (EditValue) ; vide = la ligne entière</summary>
    public string ValueMember { get; set; } = "";

    /// <summary>Texte affiché quand EditValue est null</summary>
    public string NullText { get; set; } = "";

    /// <summary>Colonnes du dropdown (vides = auto-génération JDataGrid)</summary>
    public ObservableCollection<GridColumn> Columns { get; } = new();

    /// <summary>Nombre de lignes visibles dans le dropdown</summary>
    public int DropDownRows { get; set; } = 7;

    /// <summary>Largeur du dropdown (0 = max(largeur du contrôle, 320))</summary>
    public double DropDownWidth { get; set; }

    public bool ReadOnly { get; set; }

    /// <summary>Ligne sélectionnée dans le référentiel</summary>
    public object? SelectedRow => _selectedRow;

    /// <summary>Déclenché quand EditValue change</summary>
    public event EventHandler? EditValueChanged;

    /// <summary>
    /// Valeur de l'éditeur : ValueMember de la ligne choisie (ou la ligne si ValueMember vide).
    /// Poser une valeur recherche la ligne correspondante dans DataSource.
    /// </summary>
    public object? EditValue
    {
        get => _editValue;
        set
        {
            if (Equals(_editValue, value)) return;

            _editValue = value;
            _selectedRow = FindRowByValue(value);
            InvalidateVisual();
            EditValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTION
    // ═══════════════════════════════════════════════════════════════

    static ProLookUpEdit()
    {
        FocusableProperty.OverrideDefaultValue<ProLookUpEdit>(true);
    }

    public ProLookUpEdit()
    {
        Height = 28;
        MinWidth = 160;

        _grid = new JDataGrid
        {
            SelectionMode = Julien.Avalonia.DataGrid.Models.SelectionMode.Single
        };
        _grid.SelectionChanged += OnGridSelectionChanged;

        var border = new Border
        {
            Background = new SolidColorBrush(ProTheme.Background.Panel),
            BorderBrush = new SolidColorBrush(ProTheme.Border.Default),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(0, 0, ProTheme.Size.CornerRadiusSmall, ProTheme.Size.CornerRadiusSmall),
            Child = _grid,
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0, OffsetY = 2, Blur = 8,
                Color = ProTheme.Shadow.Color
            })
        };

        _popup = new Popup
        {
            Child = border,
            PlacementTarget = this,
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            // Garde maison (cf. ProComboBox) : light dismiss instable sous GNOME/XWayland
            IsLightDismissEnabled = false,
            VerticalOffset = -1
        };
        _guard = new ProPopupDismissGuard(() => _isDropDownOpen, CloseDropDown);
        _popup.Opened += (s, e) => _guard.Install(this);
        _popup.Closed += (s, e) => { _guard.Remove(); _isDropDownOpen = false; InvalidateVisual(); };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ((ISetLogicalParent)_popup).SetParent(this);
        ProTheme.VariantChanged += OnThemeVariantChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ProTheme.VariantChanged -= OnThemeVariantChanged;
        _popup.IsOpen = false;
        ((ISetLogicalParent)_popup).SetParent(null);
        base.OnDetachedFromVisualTree(e);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        // Le conteneur du dropdown est construit une fois (la grille elle-même
        // suit RequestedThemeVariant via ses ThemeDictionaries)
        if (_popup.Child is Border border)
        {
            border.Background = new SolidColorBrush(ProTheme.Background.Panel);
            border.BorderBrush = new SolidColorBrush(ProTheme.Border.Default);
            border.BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0, OffsetY = 2, Blur = 8,
                Color = ProTheme.Shadow.Color
            });
        }
        InvalidateVisual();
    }

    // ═══════════════════════════════════════════════════════════════
    // DROPDOWN
    // ═══════════════════════════════════════════════════════════════

    private void ToggleDropDown()
    {
        if (_isDropDownOpen)
            CloseDropDown();
        else if (!_guard.JustClosed)
            OpenDropDown();
    }

    /// <summary>Ouvre le dropdown (API DevExpress)</summary>
    public void ShowPopup() => OpenDropDown();

    /// <summary>Ferme le dropdown (API DevExpress)</summary>
    public void ClosePopup() => CloseDropDown();

    private void OpenDropDown()
    {
        if (!IsEnabled || ReadOnly || DataSource == null) return;

        // Colonnes explicites (sinon auto-génération JDataGrid)
        if (Columns.Count > 0 && _grid.Columns.Count == 0)
        {
            foreach (var column in Columns)
                _grid.Columns.Add(column);
        }

        _grid.ItemsSource = DataSource;

        _syncingSelection = true;
        _grid.SelectedItem = _selectedRow;
        _syncingSelection = false;

        var width = DropDownWidth > 0 ? DropDownWidth : Math.Max(Bounds.Width, 320);
        if (_popup.Child is Border b)
        {
            b.Width = width;
            b.Height = DropDownRows * 30 + 40; // lignes + en-tête
        }

        _isDropDownOpen = true;
        _popup.IsOpen = true;
        InvalidateVisual();
    }

    private void CloseDropDown()
    {
        _isDropDownOpen = false;
        _popup.IsOpen = false;
        InvalidateVisual();
    }

    private void OnGridSelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (_syncingSelection || _grid.SelectedItem == null) return;

        _selectedRow = _grid.SelectedItem;
        var newValue = string.IsNullOrEmpty(ValueMember)
            ? _selectedRow
            : GetMemberValue(_selectedRow, ValueMember);

        if (!Equals(_editValue, newValue))
        {
            _editValue = newValue;
            EditValueChanged?.Invoke(this, EventArgs.Empty);
        }

        CloseDropDown();
        Focus();
        InvalidateVisual();
    }

    // ═══════════════════════════════════════════════════════════════
    // RÉSOLUTION DES MEMBRES (réflexion, avec cache)
    // ═══════════════════════════════════════════════════════════════

    private static object? GetMemberValue(object? row, string member)
    {
        if (row == null || string.IsNullOrEmpty(member)) return row;

        var key = (row.GetType(), member);
        if (!PropertyCache.TryGetValue(key, out var prop))
        {
            prop = row.GetType().GetProperty(member);
            PropertyCache[key] = prop;
        }
        return prop?.GetValue(row);
    }

    private object? FindRowByValue(object? value)
    {
        if (value == null || DataSource == null) return null;
        if (string.IsNullOrEmpty(ValueMember)) return value;

        foreach (var row in DataSource)
        {
            if (Equals(GetMemberValue(row, ValueMember), value))
                return row;
        }
        return null;
    }

    private string GetDisplayText()
    {
        if (_selectedRow == null)
            return NullText;

        var display = GetMemberValue(_selectedRow, DisplayMember);
        return display?.ToString() ?? _selectedRow.ToString() ?? "";
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
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!IsEnabled || ReadOnly) return;
        Focus();
        ToggleDropDown();
        e.Handled = true;
        base.OnPointerPressed(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsEnabled || ReadOnly) return;

        switch (e.Key)
        {
            case Key.F4:
            case Key.Enter:
            case Key.Down when e.KeyModifiers.HasFlag(KeyModifiers.Alt):
                ToggleDropDown();
                e.Handled = true;
                break;

            case Key.Escape when _isDropDownOpen:
                CloseDropDown();
                e.Handled = true;
                break;

            case Key.Delete:
            case Key.Back:
                if (_editValue != null)
                {
                    _editValue = null;
                    _selectedRow = null;
                    InvalidateVisual();
                    EditValueChanged?.Invoke(this, EventArgs.Empty);
                }
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        InvalidateVisual();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(Avalonia.Interactivity.RoutedEventArgs e)
    {
        InvalidateVisual();
        base.OnLostFocus(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // MESURE + RENDU (même langage visuel que ProComboBox)
    // ═══════════════════════════════════════════════════════════════

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = Height > 0 ? Height : 28;
        var width = Width > 0 ? Width : (MinWidth > 0 ? MinWidth : 160);

        if (double.IsInfinity(availableSize.Width))
            return new Size(width, height);

        return new Size(Math.Min(availableSize.Width, width), height);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var radius = ProTheme.Size.CornerRadiusSmall;

        Color bgColor, borderColor, textColor;

        if (!IsEnabled)
        {
            bgColor = ProTheme.Background.ControlDisabled;
            borderColor = ProTheme.Border.Disabled;
            textColor = ProTheme.Text.Disabled;
        }
        else if (_isDropDownOpen || IsFocused)
        {
            bgColor = ProTheme.Background.Control;
            borderColor = ProTheme.Border.Focused;
            textColor = ProTheme.Text.Primary;
        }
        else if (_isHovered)
        {
            bgColor = ProTheme.Background.Control;
            borderColor = ProTheme.Border.Hover;
            textColor = ProTheme.Text.Primary;
        }
        else
        {
            bgColor = ProTheme.Background.Control;
            borderColor = ProTheme.Border.Default;
            textColor = ProTheme.Text.Primary;
        }

        context.DrawRectangle(new SolidColorBrush(bgColor), null, bounds, radius, radius);
        context.DrawRectangle(null, new Pen(new SolidColorBrush(borderColor), 1),
            bounds.Deflate(0.5), radius, radius);

        // Texte
        var displayText = GetDisplayText();
        var isPlaceholder = _selectedRow == null;
        if (!string.IsNullOrEmpty(displayText))
        {
            var text = new FormattedText(displayText, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, new Typeface(ProTheme.Typography.FontFamily),
                ProTheme.Typography.FontSizeBody,
                new SolidColorBrush(isPlaceholder ? ProTheme.Text.Secondary : textColor));

            var textBounds = new Rect(8, 0, Math.Max(0, bounds.Width - 32), bounds.Height);
            using (context.PushClip(textBounds))
            {
                context.DrawText(text, Crisp.Snap(new Point(8, (bounds.Height - text.Height) / 2)));
            }
        }

        // Chevron
        var pen = new Pen(new SolidColorBrush(ProTheme.Text.Secondary), 1.4);
        var cx = bounds.Width - 13;
        var cy = bounds.Height / 2 + (_isDropDownOpen ? 1.5 : -1.5);
        var dir = _isDropDownOpen ? -3.0 : 3.0;
        context.DrawLine(pen, new Point(cx - 4, cy), new Point(cx, cy + dir));
        context.DrawLine(pen, new Point(cx, cy + dir), new Point(cx + 4, cy));
    }
}
