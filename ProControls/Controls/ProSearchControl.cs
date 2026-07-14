using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ProControls.Theme;

namespace ProControls.Controls;

/// <summary>
/// Recherche à suggestions (équivalent SearchControl) : loupe + saisie +
/// effacement, popup de suggestions fournies par l'application, Enter lance
/// la recherche
/// </summary>
public class ProSearchControl : ProEditorBase
{
    private readonly Popup _popup;
    private readonly ListBox _listBox;
    private readonly ProPopupDismissGuard _dismissGuard;
    private bool _isOpen;
    private bool _syncing;

    /// <summary>Fournit les suggestions pour un texte (null/vide = aucune)</summary>
    public Func<string, IEnumerable<object>>? SuggestionsProvider { get; set; }

    /// <summary>Texte affiché pour une suggestion (défaut : ToString)</summary>
    public Func<object, string>? SuggestionText { get; set; }

    /// <summary>Enter (texte libre) ou suggestion choisie</summary>
    public event EventHandler<string>? SearchRequested;

    /// <summary>Une suggestion précise a été choisie</summary>
    public event EventHandler<object>? SuggestionChosen;

    public ProSearchControl()
    {
        Placeholder = "Rechercher…";
        MinWidth = 180;

        // Loupe à gauche (dans la zone droite inversée : on garde simple —
        // loupe en préfixe via bouton inactif, croix d'effacement à droite)
        var clear = new ProEditorGlyphButton { Glyph = "✕", GlyphSize = 11 };
        clear.Click += (s, e) =>
        {
            Text = "";
            CloseSuggestions();
            FocusEditor();
        };
        SetRightElement(clear);

        _listBox = new ListBox
        {
            Background = new SolidColorBrush(ProTheme.Background.Panel),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(2),
            SelectionMode = SelectionMode.Single,
            MaxHeight = 240
        };
        _listBox.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<object>((item, _) =>
            new TextBlock
            {
                Text = SuggestionText?.Invoke(item) ?? item?.ToString() ?? "",
                Margin = new Thickness(6, 3),
                FontSize = 13
            });
        _listBox.PointerReleased += (s, e) => CommitSuggestion();

        var border = new Border
        {
            Background = new SolidColorBrush(ProTheme.Background.Panel),
            BorderBrush = new SolidColorBrush(ProTheme.Border.Default),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(ProTheme.Size.CornerRadiusSmall),
            Child = _listBox,
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0, OffsetY = 2, Blur = 8, Color = ProTheme.Shadow.Color
            })
        };

        _popup = new Popup
        {
            Child = border,
            PlacementTarget = this,
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            IsLightDismissEnabled = false, // garde maison (GNOME/XWayland)
            VerticalOffset = 2
        };
        _dismissGuard = new ProPopupDismissGuard(() => _isOpen, CloseSuggestions);
        _popup.Opened += (s, e) => _dismissGuard.Install(this);
        _popup.Closed += (s, e) => { _dismissGuard.Remove(); _isOpen = false; };
        AttachPopup(_popup);

        TextChanged += (s, e) => OnQueryChanged();
        InnerTextBox.AddHandler(KeyDownEvent, OnSearchKeyDown, RoutingStrategies.Tunnel);
    }

    protected override void RefreshThemeBrushes()
    {
        base.RefreshThemeBrushes();
        _listBox.Background = new SolidColorBrush(ProTheme.Background.Panel);
        if (_popup.Child is Border border)
            RefreshPopupBorder(border);
    }

    private void OnQueryChanged()
    {
        if (_syncing) return;

        var query = Text;
        if (string.IsNullOrWhiteSpace(query) || SuggestionsProvider == null)
        {
            CloseSuggestions();
            return;
        }

        var suggestions = SuggestionsProvider(query).Take(12).ToList();
        if (suggestions.Count == 0)
        {
            CloseSuggestions();
            return;
        }

        _listBox.ItemsSource = suggestions;
        if (_popup.Child is Border b)
        {
            b.MinWidth = Bounds.Width;
        }

        _isOpen = true;
        _popup.IsOpen = true;
    }

    private void CloseSuggestions()
    {
        _isOpen = false;
        _popup.IsOpen = false;
    }

    private void CommitSuggestion()
    {
        if (_listBox.SelectedItem == null) return;

        var item = _listBox.SelectedItem;
        var text = SuggestionText?.Invoke(item) ?? item.ToString() ?? "";

        _syncing = true;
        Text = text;
        _syncing = false;

        CloseSuggestions();
        SuggestionChosen?.Invoke(this, item);
        SearchRequested?.Invoke(this, text);
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                if (_isOpen && _listBox.SelectedItem != null)
                    CommitSuggestion();
                else if (!string.IsNullOrWhiteSpace(Text))
                {
                    CloseSuggestions();
                    SearchRequested?.Invoke(this, Text);
                }
                e.Handled = true;
                break;

            case Key.Escape when _isOpen:
                CloseSuggestions();
                e.Handled = true;
                break;

            case Key.Down when _isOpen:
                _listBox.SelectedIndex = Math.Min(_listBox.ItemCount - 1, _listBox.SelectedIndex + 1);
                e.Handled = true;
                break;

            case Key.Up when _isOpen:
                _listBox.SelectedIndex = Math.Max(0, _listBox.SelectedIndex - 1);
                e.Handled = true;
                break;
        }
    }
}
