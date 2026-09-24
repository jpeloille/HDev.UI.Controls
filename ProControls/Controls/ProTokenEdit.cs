using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ProControls.Theme;
using System.Collections.ObjectModel;

namespace ProControls.Controls;

/// <summary>
/// Champ à jetons (équivalent TokenEdit — champs À/Cc) : chips avec ×,
/// saisie inline, suggestions par callback, Enter/;/Tab valide,
/// Backspace à vide retire le dernier jeton
/// </summary>
public class ProTokenEdit : Border
{
    private readonly WrapPanel _panel;
    private readonly TextBox _input;
    private readonly Popup _popup;
    private readonly ListBox _listBox;
    private readonly ProPopupDismissGuard _dismissGuard;
    private bool _isOpen;

    public ObservableCollection<object> Tokens { get; } = new();

    /// <summary>Texte affiché pour un jeton (défaut : ToString)</summary>
    public Func<object, string>? TokenText { get; set; }

    /// <summary>Suggestions pour la saisie en cours</summary>
    public Func<string, IEnumerable<object>>? SuggestionsProvider { get; set; }

    /// <summary>
    /// Convertit un texte libre en jeton (null = refusé) ; défaut : le texte brut
    /// </summary>
    public Func<string, object?>? TokenValidator { get; set; }

    public string Placeholder
    {
        get => _input.PlaceholderText ?? "";
        set => _input.PlaceholderText = value;
    }

    /// <summary>Déclenché quand la collection de jetons change</summary>
    public event EventHandler? TokensChanged;

    public ProTokenEdit()
    {
        Background = new SolidColorBrush(ProTheme.Background.Control);
        BorderBrush = new SolidColorBrush(ProTheme.Border.Default);
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(ProTheme.Size.CornerRadiusSmall);
        Padding = new Thickness(4, 2);
        MinHeight = ProTheme.Size.ControlHeightSmall;

        _panel = new WrapPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };

        _input = new TextBox
        {
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            MinWidth = 90,
            MinHeight = 0,
            Padding = new Thickness(4, 3),
            FontFamily = new FontFamily(ProTheme.Typography.FontFamily),
            FontSize = 13,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        _input.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);
        _input.TextChanged += (s, e) => UpdateSuggestions();

        _panel.Children.Add(_input);
        Child = _panel;

        Tokens.CollectionChanged += (s, e) =>
        {
            RebuildChips();
            TokensChanged?.Invoke(this, EventArgs.Empty);
        };

        // Popup de suggestions
        _listBox = new ListBox
        {
            Background = new SolidColorBrush(ProTheme.Background.Panel),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(2),
            SelectionMode = SelectionMode.Single,
            MaxHeight = 200
        };
        _listBox.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<object>((item, _) =>
            new TextBlock
            {
                Text = GetTokenText(item),
                Margin = new Thickness(6, 3),
                FontSize = 13
            });
        _listBox.PointerReleased += (s, e) => CommitSuggestion();

        var popupBorder = new Border
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
            Child = popupBorder,
            PlacementTarget = this,
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            IsLightDismissEnabled = false, // garde maison
            VerticalOffset = 2
        };
        _dismissGuard = new ProPopupDismissGuard(() => _isOpen, CloseSuggestions);
        _popup.Opened += (s, e) => _dismissGuard.Install(this);
        _popup.Closed += (s, e) => { _dismissGuard.Remove(); _isOpen = false; };
        _panel.Children.Add(_popup);
    }

    private string GetTokenText(object token) => TokenText?.Invoke(token) ?? token.ToString() ?? "";

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ProTheme.VariantChanged += OnThemeVariantChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ProTheme.VariantChanged -= OnThemeVariantChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        Background = new SolidColorBrush(ProTheme.Background.Control);
        BorderBrush = new SolidColorBrush(ProTheme.Border.Default);
        RebuildChips(); // les chips portent des brushes construits

        // Dropdown de suggestions
        _listBox.Background = new SolidColorBrush(ProTheme.Background.Panel);
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
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.Handled)
            _input.Focus();
    }

    // ═══════════════════════════════════════════════════════════════
    // CHIPS
    // ═══════════════════════════════════════════════════════════════

    private void RebuildChips()
    {
        // Retirer tout sauf la saisie et le popup
        for (int i = _panel.Children.Count - 1; i >= 0; i--)
        {
            if (_panel.Children[i] is not TextBox && _panel.Children[i] is not Popup)
                _panel.Children.RemoveAt(i);
        }

        var insertAt = 0;
        foreach (var token in Tokens)
        {
            _panel.Children.Insert(insertAt++, BuildChip(token));
        }
    }

    private Control BuildChip(object token)
    {
        var label = new TextBlock
        {
            Text = GetTokenText(token),
            FontSize = 12,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Foreground = new SolidColorBrush(ProTheme.Text.Primary)
        };

        var remove = new ProEditorGlyphButton { Glyph = "✕", GlyphSize = 9, Width = 18, Height = 18 };
        remove.Click += (s, e) => Tokens.Remove(token);

        var content = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 2,
            Children = { label, remove }
        };

        return new Border
        {
            Background = new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Accent.Primary, 30)),
            BorderBrush = new SolidColorBrush(ProTheme.WithOpacity(ProTheme.Accent.Primary, 90)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Padding = new Thickness(8, 1, 2, 1),
            Margin = new Thickness(2),
            Child = content
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // SAISIE + SUGGESTIONS
    // ═══════════════════════════════════════════════════════════════

    private void UpdateSuggestions()
    {
        var query = _input.Text ?? "";
        if (string.IsNullOrWhiteSpace(query) || SuggestionsProvider == null)
        {
            CloseSuggestions();
            return;
        }

        var suggestions = SuggestionsProvider(query)
            .Where(s => !Tokens.Contains(s))
            .Take(10).ToList();

        if (suggestions.Count == 0)
        {
            CloseSuggestions();
            return;
        }

        _listBox.ItemsSource = suggestions;
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
        Tokens.Add(_listBox.SelectedItem);
        _input.Text = "";
        CloseSuggestions();
        _input.Focus();
    }

    private void CommitFreeText()
    {
        var text = (_input.Text ?? "").Trim().TrimEnd(';', ',');
        if (text.Length == 0) return;

        var token = TokenValidator != null ? TokenValidator(text) : text;
        if (token == null) return; // refusé par la validation

        Tokens.Add(token);
        _input.Text = "";
        CloseSuggestions();
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
            case Key.Tab when !string.IsNullOrWhiteSpace(_input.Text):
            case Key.OemSemicolon:
                if (_isOpen && _listBox.SelectedItem != null)
                    CommitSuggestion();
                else
                    CommitFreeText();
                e.Handled = e.Key != Key.Tab || !string.IsNullOrWhiteSpace(_input.Text);
                break;

            case Key.Back when string.IsNullOrEmpty(_input.Text) && Tokens.Count > 0:
                Tokens.RemoveAt(Tokens.Count - 1);
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
