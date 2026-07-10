using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using ProControls.Theme;
using System.Threading.Tasks;

namespace ProControls.Controls;

/// <summary>
/// Boutons standards de la boîte de message
/// </summary>
public enum ProMessageBoxButtons
{
    OK,
    OKCancel,
    YesNo,
    YesNoCancel
}

/// <summary>
/// Icône de la boîte de message
/// </summary>
public enum ProMessageBoxIcon
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
public enum ProDialogResult
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
/// Usage : var r = await ProMessageBox.ShowAsync(owner, "Texte", "Titre",
///     ProMessageBoxButtons.YesNo, ProMessageBoxIcon.Question);
/// </summary>
public class ProMessageBox : ProWindow
{
    private ProDialogResult _result;
    private readonly ProDialogResult _closeResult;

    private ProMessageBox(string text, string caption,
        ProMessageBoxButtons buttons, ProMessageBoxIcon icon)
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
            ProMessageBoxButtons.OK => ProDialogResult.OK,
            ProMessageBoxButtons.OKCancel => ProDialogResult.Cancel,
            ProMessageBoxButtons.YesNo => ProDialogResult.No,
            ProMessageBoxButtons.YesNoCancel => ProDialogResult.Cancel,
            _ => ProDialogResult.None
        };
        _result = _closeResult;

        Content = BuildContent(text, buttons, icon);
    }

    private Control BuildContent(string text, ProMessageBoxButtons buttons, ProMessageBoxIcon icon)
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

        if (icon != ProMessageBoxIcon.None)
            messageRow.Children.Add(BuildIcon(icon));

        messageRow.Children.Add(new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 330,
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily(ProTheme.Typography.FontFamily),
            FontSize = ProTheme.Typography.FontSizeBody,
            Foreground = new SolidColorBrush(ProTheme.Text.Primary)
        });

        root.Children.Add(messageRow);

        // Rangée de boutons alignée à droite
        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        foreach (var (label, result, isDefault) in GetButtonDefs(buttons))
        {
            var button = new ProButton
            {
                Text = label,
                Variant = isDefault ? ButtonVariant.Primary : ButtonVariant.Secondary,
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

    private static Control BuildIcon(ProMessageBoxIcon icon)
    {
        (string glyph, Color color) = icon switch
        {
            ProMessageBoxIcon.Information => ("ℹ", ProTheme.Accent.Primary),
            ProMessageBoxIcon.Warning => ("!", ProTheme.Accent.Warning),
            ProMessageBoxIcon.Error => ("✕", ProTheme.Accent.Error),
            ProMessageBoxIcon.Question => ("?", ProTheme.Accent.Primary),
            _ => ("", ProTheme.Accent.Primary)
        };

        return new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            Background = new SolidColorBrush(ProTheme.WithOpacity(color, 30)),
            VerticalAlignment = VerticalAlignment.Top,
            Child = new TextBlock
            {
                Text = glyph,
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(color),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private static (string Label, ProDialogResult Result, bool IsDefault)[] GetButtonDefs(
        ProMessageBoxButtons buttons) => buttons switch
    {
        ProMessageBoxButtons.OK => new[]
        {
            ("OK", ProDialogResult.OK, true)
        },
        ProMessageBoxButtons.OKCancel => new[]
        {
            ("OK", ProDialogResult.OK, true),
            ("Annuler", ProDialogResult.Cancel, false)
        },
        ProMessageBoxButtons.YesNo => new[]
        {
            ("Oui", ProDialogResult.Yes, true),
            ("Non", ProDialogResult.No, false)
        },
        ProMessageBoxButtons.YesNoCancel => new[]
        {
            ("Oui", ProDialogResult.Yes, true),
            ("Non", ProDialogResult.No, false),
            ("Annuler", ProDialogResult.Cancel, false)
        },
        _ => Array.Empty<(string, ProDialogResult, bool)>()
    };

    private void CloseWith(ProDialogResult result)
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
    public static async Task<ProDialogResult> ShowAsync(Window owner, string text,
        string caption = "",
        ProMessageBoxButtons buttons = ProMessageBoxButtons.OK,
        ProMessageBoxIcon icon = ProMessageBoxIcon.None)
    {
        var box = new ProMessageBox(text, caption, buttons, icon);
        await box.ShowDialog(owner);
        return box._result;
    }

    /// <summary>Raccourci : information avec bouton OK</summary>
    public static Task<ProDialogResult> ShowInfoAsync(Window owner, string text, string caption = "Information")
        => ShowAsync(owner, text, caption, ProMessageBoxButtons.OK, ProMessageBoxIcon.Information);

    /// <summary>Raccourci : avertissement avec bouton OK</summary>
    public static Task<ProDialogResult> ShowWarningAsync(Window owner, string text, string caption = "Avertissement")
        => ShowAsync(owner, text, caption, ProMessageBoxButtons.OK, ProMessageBoxIcon.Warning);

    /// <summary>Raccourci : erreur avec bouton OK</summary>
    public static Task<ProDialogResult> ShowErrorAsync(Window owner, string text, string caption = "Erreur")
        => ShowAsync(owner, text, caption, ProMessageBoxButtons.OK, ProMessageBoxIcon.Error);

    /// <summary>Raccourci : question Oui/Non</summary>
    public static Task<ProDialogResult> ShowQuestionAsync(Window owner, string text, string caption = "Confirmation")
        => ShowAsync(owner, text, caption, ProMessageBoxButtons.YesNo, ProMessageBoxIcon.Question);
}
