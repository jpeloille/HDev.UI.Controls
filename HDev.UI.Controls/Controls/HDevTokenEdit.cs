using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Champ à jetons (champs À/Cc d'un courriel), sur le socle des éditeurs : jetons avec ×, saisie
/// en ligne, suggestions par fonction, Entrée, « ; » et Tab valident, Retour arrière à vide retire
/// le dernier jeton.
/// <para><see cref="Value"/> est la liste des jetons, liable dans les deux sens. Elle n'est jamais
/// modifiée sur place : chaque ajout ou retrait la remplace par une nouvelle liste, précédé de
/// <c>ValueChanging</c> (annulable) et suivi de <c>ValueChanged</c>.</para>
/// </summary>
public class HDevTokenEdit : HDevEditorBase<IReadOnlyList<object>>
{
    public static readonly StyledProperty<IReadOnlyList<object>> ValueProperty =
        AvaloniaProperty.Register<HDevTokenEdit, IReadOnlyList<object>>(nameof(Value), Array.Empty<object>(),
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true, coerce: CoerceThroughChanging);

    private readonly WrapPanel _panel;
    private readonly ListBox _listBox;
    private bool _openingFromQuery;

    protected override StyledProperty<IReadOnlyList<object>> ValuePropertyCore => ValueProperty;

    /// <summary>Les jetons (jamais nul, vide par défaut) ; remplacée à chaque changement</summary>
    public IReadOnlyList<object> Value
    {
        get => GetValue(ValueProperty);
        set => SetValueFromCode(value);
    }

    /// <summary>Texte affiché pour un jeton (défaut : ToString) ; jamais appelé avec null</summary>
    public Func<object, string>? TokenText { get; set; }

    /// <summary>Suggestions pour la saisie en cours</summary>
    public Func<string, IEnumerable<object>>? SuggestionsProvider { get; set; }

    /// <summary>
    /// Convertit un texte libre en jeton (null = refusé) ; défaut : le texte brut
    /// </summary>
    public Func<string, object?>? TokenValidator { get; set; }

    static HDevTokenEdit()
    {
        // Le champ grandit avec ses lignes de jetons : centré dans une rangée plus haute
        VerticalAlignmentProperty.OverrideDefaultValue<HDevTokenEdit>(VerticalAlignment.Center);
    }

    public HDevTokenEdit()
    {
        // Une ligne de jetons tient dans la hauteur du gabarit (28, constat E0 n° 8) ; au-delà, le champ grandit
        MaxHeight = double.PositiveInfinity;
        MinWidth = 0;

        _panel = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(3, 0, 0, 0) };
        // 3 (panneau) + 1 (filet) + retrait = même retrait du texte que les autres champs
        InnerTextBox.MinWidth = 90;
        InnerTextBox.Padding = new Thickness(HDevTheme.Size.EditorTextInset - 4, 3);
        HostTextBoxIn(_panel);
        InnerTextBox.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);

        _listBox = new ListBox
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            SelectionMode = SelectionMode.Single,
            MaxHeight = 200,
            // Une ligne recyclée par la liste est reconstruite pour un contenu nul : le nul ne
            // va jamais à TokenText (même défaut que APP-11)
            ItemTemplate = new FuncDataTemplate<object?>((item, _) => new TextBlock
            {
                Text = item is null ? "" : GetTokenText(item),
                Margin = new Thickness(6, 3),
                FontSize = 13
            })
        };
        _listBox.PointerReleased += (s, e) => CommitSuggestion();

        DropDown.Content = _listBox;
        CloseDropDownOnFocusLoss = true;
        // F4 et Alt+↓ ouvrent les suggestions de la saisie, s'il y en a
        DropDownOpening += (_, e) => { if (!_openingFromQuery) e.Cancel = !LoadSuggestions(InnerText); };
    }

    /// <summary>Jamais nulle : une liste vide à la place</summary>
    protected override IReadOnlyList<object> CoerceOwnValue(IReadOnlyList<object> value) => value ?? Array.Empty<object>();

    private string GetTokenText(object token) => TokenText?.Invoke(token) ?? token.ToString() ?? "";

    /// <summary>Le champ texte porte la saisie en cours, pas la valeur : il se vide quand la valeur change</summary>
    protected override string FormatValue(IReadOnlyList<object> value) => "";

    protected override object? ConvertTextToValue() => Value;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty)
            RebuildChips();
    }

    protected override void RefreshThemeBrushes()
    {
        base.RefreshThemeBrushes();
        RebuildChips(); // les jetons portent des brushes construits
    }

    // ═══════════════════════════════════════════════════════════════
    // JETONS
    // ═══════════════════════════════════════════════════════════════

    private void AddToken(object token) => CommitUserValue(Value.Append(token).ToArray());

    private void RemoveToken(object token)
    {
        var list = Value.ToList();
        var index = list.IndexOf(token);
        if (index < 0) return;
        list.RemoveAt(index);
        CommitUserValue(list.ToArray());
    }

    private void RebuildChips()
    {
        // Tout sauf la saisie, en dernier
        while (_panel.Children.Count > 1)
            _panel.Children.RemoveAt(0);

        var insertAt = 0;
        foreach (var token in Value)
            _panel.Children.Insert(insertAt++, BuildChip(token));
    }

    private Control BuildChip(object token)
    {
        var label = new TextBlock
        {
            Text = GetTokenText(token),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(HDevTheme.Text.Primary)
        };

        var remove = new HDevEditorGlyphButton { Glyph = "✕", GlyphSize = 9, Width = 18, Height = 18 };
        remove.Click += (s, e) =>
        {
            if (!IsReadOnly) RemoveToken(token);
        };

        return new Border
        {
            Background = new SolidColorBrush(HDevTheme.WithOpacity(HDevTheme.Accent.Primary, 30)),
            BorderBrush = new SolidColorBrush(HDevTheme.WithOpacity(HDevTheme.Accent.Primary, 90)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Padding = new Thickness(8, 1, 2, 1),
            Margin = new Thickness(2),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Children = { label, remove }
            }
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // SAISIE + SUGGESTIONS
    // ═══════════════════════════════════════════════════════════════

    protected override void OnInnerTextChanged()
    {
        if (IsUpdatingText) return;

        if (LoadSuggestions(InnerText))
        {
            _openingFromQuery = true;
            try { DropDown.Open(); }
            finally { _openingFromQuery = false; }
        }
        else
            DropDown.Close(HDevDropDownCloseReason.Canceled);
    }

    /// <summary>Remplit la liste pour une saisie ; faux s'il n'y a aucune suggestion</summary>
    private bool LoadSuggestions(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || SuggestionsProvider == null)
            return false;

        var suggestions = SuggestionsProvider(query)
            .Where(s => !Value.Contains(s))
            .Take(10).ToList();
        if (suggestions.Count == 0)
            return false;

        _listBox.ItemsSource = suggestions;
        return true;
    }

    private void CommitSuggestion()
    {
        if (_listBox.SelectedItem is not { } item) return;
        DropDown.Close(HDevDropDownCloseReason.Accepted);
        AddToken(item);
        FocusEditor();
    }

    private void CommitFreeText()
    {
        var text = InnerText.Trim().TrimEnd(';', ',');
        if (text.Length == 0) return;

        var token = TokenValidator != null ? TokenValidator(text) : text;
        if (token == null) return; // refusé par la validation : la saisie reste

        DropDown.Close(HDevDropDownCloseReason.Canceled);
        AddToken(token);
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (IsReadOnly) return;
        var open = DropDown.IsOpen;
        var hasText = !string.IsNullOrWhiteSpace(InnerText);

        switch (e.Key)
        {
            case Key.Enter:
            case Key.Tab when hasText:
            case Key.OemSemicolon:
                if (open && _listBox.SelectedItem != null)
                    CommitSuggestion();
                else
                    CommitFreeText();
                e.Handled = e.Key != Key.Tab || hasText;
                break;

            case Key.Back when InnerText.Length == 0 && Value.Count > 0:
                RemoveToken(Value[^1]);
                e.Handled = true;
                break;

            case Key.Down when open && e.KeyModifiers == KeyModifiers.None:
                _listBox.SelectedIndex = Math.Min(_listBox.ItemCount - 1, _listBox.SelectedIndex + 1);
                e.Handled = true;
                break;

            case Key.Up when open && e.KeyModifiers == KeyModifiers.None:
                _listBox.SelectedIndex = Math.Max(0, _listBox.SelectedIndex - 1);
                e.Handled = true;
                break;
        }
    }
}
