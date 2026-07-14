using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ProControls.Theme;

namespace ProControls.Controls;

/// <summary>
/// Notifications toast (équivalent AlertControl) : empilées en bas à droite
/// de la fenêtre via l'OverlayLayer, auto-fermeture, clic actionnable.
/// Usage : ProToast.Show(fenêtre, "Nouveau message", "De : Julien", "📧",
///     onClick: () => ...);
/// </summary>
public static class ProToast
{
    private static readonly Dictionary<OverlayLayer, List<Control>> Active = new();
    private const double ToastWidth = 320;
    private const double Spacing = 8;

    public static void Show(TopLevel host, string title, string message,
        string? icon = null, TimeSpan? duration = null, Action? onClick = null)
    {
        var layer = OverlayLayer.GetOverlayLayer(host);
        if (layer == null) return;

        if (!Active.TryGetValue(layer, out var list))
        {
            list = new List<Control>();
            Active[layer] = list;
        }

        var toast = BuildToast(title, message, icon, onClick != null);
        list.Add(toast);
        layer.Children.Add(toast);
        Restack(layer, list);

        void Close()
        {
            if (!list.Remove(toast)) return;
            layer.Children.Remove(toast);
            Restack(layer, list);
        }

        toast.PointerPressed += (s, e) =>
        {
            // La croix porte sa propre fermeture ; le corps exécute l'action
            if (e.Source is Visual v && v.GetType() == typeof(ProEditorGlyphButton))
                return;
            onClick?.Invoke();
            Close();
            e.Handled = true;
        };

        if (toast.Tag is ProEditorGlyphButton closeButton)
            closeButton.Click += (s, e) => Close();

        DispatcherTimer.RunOnce(Close, duration ?? TimeSpan.FromSeconds(5));
    }

    private static void Restack(OverlayLayer layer, List<Control> list)
    {
        // Empilement du bas vers le haut
        for (int i = 0; i < list.Count; i++)
        {
            var fromBottom = list.Count - 1 - i;
            list[i].Margin = new Thickness(0, 0, 16, 16 + fromBottom * (74 + Spacing));
        }
        _ = layer;
    }

    private static Control BuildToast(string title, string message, string? icon, bool clickable)
    {
        var closeButton = new ProEditorGlyphButton { Glyph = "✕", GlyphSize = 10, Width = 22, Height = 22 };

        var textPanel = new StackPanel { Spacing = 2 };
        textPanel.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeight.SemiBold,
            FontSize = 13,
            Foreground = new SolidColorBrush(ProTheme.Text.Primary),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        textPanel.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 12,
            Foreground = new SolidColorBrush(ProTheme.Text.Secondary),
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        var row = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(closeButton, Dock.Right);
        row.Children.Add(closeButton);

        if (!string.IsNullOrEmpty(icon))
        {
            var iconBlock = new TextBlock
            {
                Text = icon,
                FontSize = 20,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(iconBlock, Dock.Left);
            row.Children.Add(iconBlock);
        }

        row.Children.Add(textPanel);

        var toast = new Border
        {
            Width = ToastWidth,
            MinHeight = 64,
            Background = new SolidColorBrush(ProTheme.Background.Panel),
            BorderBrush = new SolidColorBrush(ProTheme.Border.Default),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(ProTheme.Size.CornerRadiusMedium),
            Padding = new Thickness(12, 10),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0, OffsetY = 4, Blur = 16,
                Color = ProTheme.Shadow.ColorStrong
            }),
            Child = row,
            Cursor = clickable ? new Cursor(StandardCursorType.Hand) : Cursor.Default,
            Tag = closeButton
        };

        return toast;
    }
}
