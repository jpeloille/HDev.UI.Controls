using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using HDev.UI.Controls.Theme;
using System.Threading.Tasks;

namespace HDev.UI.Controls;

/// <summary>
/// Boutons standards de la boîte de message
/// </summary>
public enum HDevMessageBoxButtons
{
    OK,
    OKCancel,
    YesNo,
    YesNoCancel
}

/// <summary>
/// Icône de la boîte de message
/// </summary>
public enum HDevMessageBoxIcon
{
    None,
    Information,
    Warning,
    Error,
    Question
}

/// <summary>
/// Résultat d'une boîte de dialogue
/// </summary>
public enum HDevDialogResult
{
    None,
    OK,
    Cancel,
    Yes,
    No
}

/// <summary>
/// Boîte de message modale (équivalent XtraMessageBox) :
/// icône, texte, boutons standards, résultat typé.
/// Usage : var r = await HDevMessageBox.ShowAsync(owner, "Texte", "Titre",
///     HDevMessageBoxButtons.YesNo, HDevMessageBoxIcon.Question);
/// </summary>
public class HDevMessageBox : HDevWindow
{
    private HDevDialogResult _result;
    private readonly HDevDialogResult _closeResult;

    private HDevMessageBox(string text, string caption,
        HDevMessageBoxButtons buttons, HDevMessageBoxIcon icon)
    {
        Title = caption;
        Width = 440;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        CanMinimize = false;
        CanMaximize = false;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        // Résultat renvoyé si la fenêtre est fermée sans cliquer (croix, Échap)
        _closeResult = buttons switch
        {
            HDevMessageBoxButtons.OK => HDevDialogResult.OK,
            HDevMessageBoxButtons.OKCancel => HDevDialogResult.Cancel,
            HDevMessageBoxButtons.YesNo => HDevDialogResult.No,
            HDevMessageBoxButtons.YesNoCancel => HDevDialogResult.Cancel,
            _ => HDevDialogResult.None
        };
        _result = _closeResult;

        Content = BuildContent(text, buttons, icon);
    }

    private Control BuildContent(string text, HDevMessageBoxButtons buttons, HDevMessageBoxIcon icon)
    {
        var root = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 20
        };

        // Ligne icône + message
        var messageRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16
        };

        if (icon != HDevMessageBoxIcon.None)
            messageRow.Children.Add(BuildIcon(icon));

        messageRow.Children.Add(new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 330,
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily(HDevTheme.Typography.FontFamily),
            FontSize = HDevTheme.Typography.FontSizeBody,
            Foreground = new SolidColorBrush(HDevTheme.Text.Primary)
        });

        root.Children.Add(messageRow);

        // Rangée de boutons alignée à droite
        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        // Convention GNOME : les actions secondaires (Outline) à gauche, l'action par défaut (Primary) à droite
        var defs = GetButtonDefs(buttons).ToList();
        foreach (var (label, result, isDefault) in defs.Where(d => !d.Item3).Concat(defs.Where(d => d.Item3)))
        {
            var button = new HDevButton
            {
                Text = label,
                Variant = isDefault ? ButtonVariant.Primary : ButtonVariant.Outline,
                MinWidth = 90
            };
            var capturedResult = result;
            button.Click += (s, e) => CloseWith(capturedResult);
            buttonRow.Children.Add(button);

            if (isDefault)
                Opened += (s, e) => button.Focus();
        }

        root.Children.Add(buttonRow);
        return root;
    }

    private static Control BuildIcon(HDevMessageBoxIcon icon)
    {
        (HDevIcon glyph, Color color) = icon switch
        {
            HDevMessageBoxIcon.Information => (HDevIcon.Info, HDevTheme.Accent.Primary),
            HDevMessageBoxIcon.Warning => (HDevIcon.Warning, HDevTheme.Accent.Warning),
            HDevMessageBoxIcon.Error => (HDevIcon.Error, HDevTheme.Accent.Error),
            HDevMessageBoxIcon.Question => (HDevIcon.Question, HDevTheme.Accent.Primary),
            _ => (HDevIcon.None, HDevTheme.Accent.Primary)
        };

        return new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            Background = new SolidColorBrush(HDevTheme.WithOpacity(color, 30)),
            VerticalAlignment = VerticalAlignment.Top,
            Child = new HDevIconView
            {
                Icon = glyph,
                IconSize = HDevTheme.Size.IconLarge + 4,
                Color = HDevTheme.EnsureContrast(color, HDevTheme.Background.Window, 3),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private static (string Label, HDevDialogResult Result, bool IsDefault)[] GetButtonDefs(
        HDevMessageBoxButtons buttons) => buttons switch
    {
        HDevMessageBoxButtons.OK => new[]
        {
            ("OK", HDevDialogResult.OK, true)
        },
        HDevMessageBoxButtons.OKCancel => new[]
        {
            ("OK", HDevDialogResult.OK, true),
            ("Annuler", HDevDialogResult.Cancel, false)
        },
        HDevMessageBoxButtons.YesNo => new[]
        {
            ("Oui", HDevDialogResult.Yes, true),
            ("Non", HDevDialogResult.No, false)
        },
        HDevMessageBoxButtons.YesNoCancel => new[]
        {
            ("Oui", HDevDialogResult.Yes, true),
            ("Non", HDevDialogResult.No, false),
            ("Annuler", HDevDialogResult.Cancel, false)
        },
        _ => Array.Empty<(string, HDevDialogResult, bool)>()
    };

    private void CloseWith(HDevDialogResult result)
    {
        _result = result;
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseWith(_closeResult);
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // API STATIQUE
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Affiche une boîte de message modale et retourne le bouton choisi
    /// </summary>
    public static async Task<HDevDialogResult> ShowAsync(Window owner, string text,
        string caption = "",
        HDevMessageBoxButtons buttons = HDevMessageBoxButtons.OK,
        HDevMessageBoxIcon icon = HDevMessageBoxIcon.None)
    {
        var box = new HDevMessageBox(text, caption, buttons, icon);
        await box.ShowDialog(owner);
        return box._result;
    }

    /// <summary>Raccourci : information avec bouton OK</summary>
    public static Task<HDevDialogResult> ShowInfoAsync(Window owner, string text, string caption = "Information")
        => ShowAsync(owner, text, caption, HDevMessageBoxButtons.OK, HDevMessageBoxIcon.Information);

    /// <summary>Raccourci : avertissement avec bouton OK</summary>
    public static Task<HDevDialogResult> ShowWarningAsync(Window owner, string text, string caption = "Avertissement")
        => ShowAsync(owner, text, caption, HDevMessageBoxButtons.OK, HDevMessageBoxIcon.Warning);

    /// <summary>Raccourci : erreur avec bouton OK</summary>
    public static Task<HDevDialogResult> ShowErrorAsync(Window owner, string text, string caption = "Erreur")
        => ShowAsync(owner, text, caption, HDevMessageBoxButtons.OK, HDevMessageBoxIcon.Error);

    /// <summary>Raccourci : question Oui/Non</summary>
    public static Task<HDevDialogResult> ShowQuestionAsync(Window owner, string text, string caption = "Confirmation")
        => ShowAsync(owner, text, caption, HDevMessageBoxButtons.YesNo, HDevMessageBoxIcon.Question);
}
