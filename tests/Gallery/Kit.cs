using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls.Gallery;

/// <summary>
/// Une planche : identifiant de fichier, titre, largeur de rendu, construction, action
/// jouée une fois la fenêtre affichée, et fenêtre hôte (Window nue par défaut)
/// </summary>
sealed record Planche(string Id, string Title, double Width, Func<Control> Build,
    Action<Window>? AfterShow = null, Func<Window>? CreateWindow = null);

/// <summary>
/// Briques de mise en page des planches. L'habillage lit HDevTheme à la construction :
/// une planche construite après la bascule de variante est donc rendue dans ce thème.
/// </summary>
static class Kit
{
    public static IBrush Brush(Color color) => new SolidColorBrush(color);

    public static TextBlock Text(string text, double size, Color color, FontWeight weight = FontWeight.Normal) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = weight,
        Foreground = Brush(color),
        FontFamily = new FontFamily(HDevTheme.Typography.FontFamily),
        VerticalAlignment = VerticalAlignment.Center
    };

    public static TextBlock Caption(string text)
        => Text(text, HDevTheme.Typography.FontSizeCaption, HDevTheme.Text.Secondary);

    /// <summary>Page : titre, sous-titre, puis les sections empilées</summary>
    public static Control Page(string title, string subtitle, params Control[] sections)
    {
        var page = new StackPanel { Margin = new Thickness(24), Spacing = 20 };
        page.Children.Add(new StackPanel
        {
            Spacing = 4,
            Children =
            {
                Text(title, HDevTheme.Typography.FontSizeTitle, HDevTheme.Text.Primary, FontWeight.SemiBold),
                Caption(subtitle)
            }
        });
        foreach (var section in sections)
            page.Children.Add(section);
        return page;
    }

    /// <summary>Section : carte au fond panneau, titre, note facultative, contenu</summary>
    public static Control Section(string title, Control body, string? note = null)
    {
        var stack = new StackPanel { Spacing = 10 };
        stack.Children.Add(Text(title, HDevTheme.Typography.FontSizeBody, HDevTheme.Text.Primary, FontWeight.SemiBold));
        if (note != null)
            stack.Children.Add(Caption(note));
        stack.Children.Add(body);

        return new Border
        {
            Background = Brush(HDevTheme.Background.Panel),
            BorderBrush = Brush(HDevTheme.Border.Subtle),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(HDevTheme.Size.CornerRadiusLarge),
            Padding = new Thickness(16),
            Child = stack
        };
    }

    /// <summary>
    /// Matrice états × variantes. Chaque cellule est enveloppée à sa taille désirée :
    /// on y lit l'apparence intrinsèque du contrôle, pas son comportement d'étirement
    /// (c'est le rôle de <see cref="Row"/>)
    /// </summary>
    public static Control Matrix(string[] columns, IEnumerable<(string Row, Control?[] Cells)> rows)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        foreach (var _ in columns)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var c = 0; c < columns.Length; c++)
            Place(grid, Caption(columns[c]), 0, c + 1);

        var r = 1;
        foreach (var (row, cells) in rows)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Place(grid, Caption(row), r, 0);
            for (var c = 0; c < cells.Length; c++)
            {
                if (cells[c] is { } cell)
                    Place(grid, cell, r, c + 1);
            }
            r++;
        }
        return grid;
    }

    private static void Place(Grid grid, Control control, int row, int column)
    {
        var host = new Border
        {
            Padding = new Thickness(0, 0, 20, 14),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Child = control
        };
        Grid.SetRow(host, row);
        Grid.SetColumn(host, column);
        grid.Children.Add(host);
    }

    /// <summary>
    /// Rangée « cas réel » : un StackPanel horizontal tel qu'un développeur l'écrit, sur
    /// fond teinté pour révéler la hauteur de la rangée et l'étirement des contrôles
    /// </summary>
    public static Control Row(params Control[] children)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var child in children)
            row.Children.Add(child);

        return new Border
        {
            Background = Brush(HDevTheme.Background.PanelAlt),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = row
        };
    }

    /// <summary>Grille souple de vignettes légendées</summary>
    public static Control Tiles(params (string Caption, Control Content)[] tiles)
    {
        var wrap = new WrapPanel();
        foreach (var (caption, content) in tiles)
        {
            wrap.Children.Add(new StackPanel
            {
                Margin = new Thickness(0, 0, 20, 16),
                Spacing = 6,
                Children = { Caption(caption), content }
            });
        }
        return wrap;
    }

    /// <summary>Pastille de couleur : nom, hexadécimal et contraste sur le fond panneau</summary>
    public static Control Swatch(string name, Color color)
    {
        var ratio = Contrast(color, HDevTheme.Background.Panel);
        return new StackPanel
        {
            Width = 150,
            Spacing = 3,
            Children =
            {
                new Border
                {
                    Height = 32,
                    Background = Brush(color),
                    BorderBrush = Brush(HDevTheme.Border.Subtle),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(HDevTheme.Size.CornerRadiusSmall)
                },
                Text(name, HDevTheme.Typography.FontSizeCaption, HDevTheme.Text.Primary),
                Caption($"{Hex(color)} · {ratio:0.00}:1")
            }
        };
    }

    public static string Hex(Color c)
        => c.A == 255 ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

    /// <summary>Rapport de contraste WCAG 2 (couleur supposée opaque)</summary>
    public static double Contrast(Color a, Color b)
    {
        var la = Luminance(a);
        var lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(Color c)
    {
        static double Channel(byte v)
        {
            var s = v / 255.0;
            return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }
}
