using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Recherche à suggestions : saisie, bouton d'effacement, liste de suggestions fournies par
/// l'application, Entrée lance la recherche. La valeur est le texte (D3) : <see cref="Text"/>,
/// liable dans les deux sens. Les suggestions vivent dans le popup du socle.
/// </summary>
public class HDevSearchControl : HDevEditorBase<string?>
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<HDevSearchControl, string?>(nameof(Text), "",
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);

    private readonly ListBox _listBox;
    private bool _openingFromQuery;

    protected override StyledProperty<string?> ValuePropertyCore => TextProperty;

    /// <summary>Le texte recherché, valeur de l'éditeur (StyledProperty liable)</summary>
    public override string Text
    {
        get => GetValue(TextProperty) ?? "";
        set => SetValueFromCode(value);
    }

    /// <summary>Fournit les suggestions pour un texte (null/vide = aucune)</summary>
    public Func<string, IEnumerable<object>>? SuggestionsProvider { get; set; }

    /// <summary>Texte affiché pour une suggestion (défaut : ToString) ; jamais appelé avec null</summary>
    public Func<object, string>? SuggestionText { get; set; }

    /// <summary>Enter (texte libre) ou suggestion choisie</summary>
    public event EventHandler<string>? SearchRequested;

    /// <summary>Une suggestion précise a été choisie</summary>
    public event EventHandler<object>? SuggestionChosen;

    public HDevSearchControl()
    {
        Placeholder = "Rechercher…";
        MinWidth = 180;
        Buttons.Add(new HDevEditorButton { Kind = HDevEditorButtonKind.Clear });

        _listBox = new ListBox
        {
            Background = Avalonia.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            SelectionMode = SelectionMode.Single,
            MaxHeight = 240,
            // Une ligne recyclée par la liste est reconstruite pour un contenu nul : le nul ne
            // va jamais à SuggestionText (APP-11)
            ItemTemplate = new FuncDataTemplate<object?>((item, _) => new TextBlock
            {
                Text = item is null ? "" : SuggestionText?.Invoke(item) ?? item.ToString() ?? "",
                Margin = new Thickness(6, 3),
                FontSize = 13
            })
        };
        _listBox.PointerReleased += (s, e) => CommitSuggestion();

        DropDown.Content = _listBox;
        DropDown.MatchOwnerWidth = true;
        CloseDropDownOnFocusLoss = true;
        // F4 et Alt+↓ ouvrent les suggestions du texte courant, s'il y en a
        DropDownOpening += (_, e) => { if (!_openingFromQuery) e.Cancel = !LoadSuggestions(Text); };

        InnerTextBox.AddHandler(KeyDownEvent, OnSearchKeyDown, RoutingStrategies.Tunnel);
    }

    protected override string FormatValue(string? value) => value ?? "";

    protected override void OnInnerTextChanged()
    {
        // Texte réécrit depuis la valeur (choix d'une suggestion, code) : il ne rouvre pas la
        // liste (constat E0 n° 3)
        if (IsUpdatingText) return;
        CommitUserValue(InnerText, fromText: true);
        OnQueryChanged();
    }

    private void OnQueryChanged()
    {
        if (LoadSuggestions(Text))
        {
            _openingFromQuery = true;
            try { DropDown.Open(); }
            finally { _openingFromQuery = false; }
        }
        else
            DropDown.Close(HDevDropDownCloseReason.Canceled);
    }

    /// <summary>Remplit la liste pour une requête ; faux s'il n'y a aucune suggestion</summary>
    private bool LoadSuggestions(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || SuggestionsProvider == null)
            return false;

        var suggestions = SuggestionsProvider(query).Take(12).ToList();
        if (suggestions.Count == 0)
            return false;

        _listBox.ItemsSource = suggestions;
        return true;
    }

    private void CommitSuggestion()
    {
        if (_listBox.SelectedItem is not { } item) return;

        var text = SuggestionText?.Invoke(item) ?? item.ToString() ?? "";
        DropDown.Close(HDevDropDownCloseReason.Accepted);
        CommitUserValue(text);
        SuggestionChosen?.Invoke(this, item);
        SearchRequested?.Invoke(this, text);
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        var open = DropDown.IsOpen;
        switch (e.Key)
        {
            case Key.Enter:
                if (open && _listBox.SelectedItem != null)
                    CommitSuggestion();
                else if (!string.IsNullOrWhiteSpace(Text))
                {
                    DropDown.Close(HDevDropDownCloseReason.Canceled);
                    SearchRequested?.Invoke(this, Text);
                }
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
