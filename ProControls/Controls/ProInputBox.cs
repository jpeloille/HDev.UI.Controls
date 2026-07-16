using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using ProControls.Theme;
using System.Threading.Tasks;

namespace ProControls.Controls;

/// <summary>
/// Boîte de saisie modale (équivalent XtraInputBox) : libellé + ProTextBox +
/// OK/Annuler. Retourne null si annulée.
/// Usage : var url = await ProInputBox.ShowAsync(owner, "Adresse du lien :",
///     "Insérer un lien", "https://");
/// </summary>
public class ProInputBox : ProWindow
{
    private string? _result;
    private readonly ProTextBox _input;

    private ProInputBox(string prompt, string caption, string initialValue)
    {
        Title = caption;
        Width = 440;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        CanMinimize = false;
        CanMaximize = false;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _input = new ProTextBox { Text = initialValue };

        var ok = new ProButton { Text = "OK", Variant = ButtonVariant.Primary, Width = 96 };
        var cancel = new ProButton { Text = "Annuler", Variant = ButtonVariant.Secondary, Width = 96 };
        ok.Click += (s, e) => { _result = _input.Text ?? ""; Close(); };
        cancel.Click += (s, e) => Close();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { ok, cancel }
        };

        Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20, 16),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = prompt,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(ProTheme.Text.Primary)
                },
                _input,
                buttons
            }
        };

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter) { _result = _input.Text ?? ""; Close(); e.Handled = true; }
            else if (e.Key == Key.Escape) { Close(); e.Handled = true; }
        };

        Opened += (s, e) => _input.Focus();
    }

    /// <summary>Affiche la boîte ; retourne la saisie, ou null si annulée</summary>
    public static async Task<string?> ShowAsync(Window owner, string prompt,
        string caption, string initialValue = "")
    {
        var box = new ProInputBox(prompt, caption, initialValue);
        await box.ShowDialog(owner);
        return box._result;
    }
}
