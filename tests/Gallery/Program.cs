using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using HDev.UI.Controls.Gallery;
using HDev.UI.Controls.Theme;

// Planche visuelle : chaque famille de contrôles rendue hors écran, figée dans ses
// états (survol, pression, focus, erreur, désactivé…), en clair puis en sombre.
//   dotnet run --project tests/Gallery [-- <libellé>]
//   → artifacts/gallery/<libellé>/<clair|sombre>/<planche>.png + index.html
// Rendre « avant » une modification puis « après » donne la comparaison visuelle.

var label = args.FirstOrDefault() ?? "courant";
var root = RepoRoot();
var outDir = Path.Combine(root, "artifacts", "gallery", label);

// Culture des apps hôtes (dates, mois, jours de la semaine)
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture
    = CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

AppBuilder.Configure<GalleryApp>()
    .UseSkia()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .WithInterFont()
    .SetupWithoutStarting();

foreach (var variant in new[] { HDevThemeVariant.Light, HDevThemeVariant.Dark })
{
    var themeDir = Path.Combine(outDir, ThemeFolder(variant));
    Directory.CreateDirectory(themeDir);

    foreach (var planche in Planches.All)
    {
        // Réaffirmée à chaque planche : la planche « bascule » change de variante en cours de rendu
        HDevTheme.Variant = variant;
        var path = Path.Combine(themeDir, planche.Id + ".png");
        Render(planche, variant, path);
        Console.WriteLine($"  {ThemeFolder(variant),-7} {planche.Id,-12} → {Path.GetRelativePath(root, path)}");
    }
}

HDevTheme.Variant = HDevThemeVariant.Light;
File.WriteAllText(Path.Combine(outDir, "index.html"), Index(label));
Console.WriteLine($"Planche : {Path.Combine(outDir, "index.html")}");
return 0;

static void Render(Planche planche, HDevThemeVariant variant, string path)
{
    var content = planche.Build();
    var window = planche.CreateWindow?.Invoke()
        ?? new Window { Background = new SolidColorBrush(HDevTheme.Background.Window) };
    window.Width = planche.Width;
    window.Height = 400;
    window.RequestedThemeVariant = variant == HDevThemeVariant.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
    window.Content = content;
    window.Show();
    Pump(window);

    // Les planches sont des piles verticales de largeur fixe : la fenêtre prend
    // la hauteur du contenu
    content.Measure(new Size(planche.Width, double.PositiveInfinity));
    window.Height = Math.Ceiling(content.DesiredSize.Height);
    Pump(window);

    planche.AfterShow?.Invoke(window);
    Pump(window);

    using var frame = window.CaptureRenderedFrame()
        ?? throw new InvalidOperationException($"Aucun rendu pour « {planche.Id} »");
    frame.Save(path);
    window.Close();
}

static void Pump(Window window)
{
    for (var i = 0; i < 3; i++)
    {
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(window.Width, window.Height));
        window.Arrange(new Rect(0, 0, window.Width, window.Height));
    }
    Dispatcher.UIThread.RunJobs();
}

static string ThemeFolder(HDevThemeVariant variant)
    => variant == HDevThemeVariant.Dark ? "sombre" : "clair";

static string RepoRoot()
{
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "HDev.UI.Controls.sln")))
            return dir.FullName;
    }
    return Directory.GetCurrentDirectory();
}

static string Index(string label)
{
    var html = new StringBuilder();
    html.AppendLine("<!doctype html>");
    html.AppendLine("<html lang=\"fr\"><head><meta charset=\"utf-8\">");
    html.AppendLine($"<title>Planche HDev.UI.Controls — {label}</title>");
    html.AppendLine("<style>body{font-family:Inter,system-ui,sans-serif;margin:24px;background:#e8e8e8;color:#222}"
        + "h2{margin:40px 0 8px}div.row{display:flex;gap:16px;align-items:flex-start;flex-wrap:wrap}"
        + "figure{margin:0}figcaption{font-size:12px;color:#555;margin-top:4px}"
        + "img{display:block;max-width:100%;border:1px solid #bbb}</style></head><body>");
    html.AppendLine($"<h1>Planche HDev.UI.Controls — {label}</h1>");
    foreach (var planche in Planches.All)
    {
        html.AppendLine($"<h2>{planche.Title}</h2><div class=\"row\">");
        foreach (var theme in new[] { "clair", "sombre" })
            html.AppendLine($"<figure><img src=\"{theme}/{planche.Id}.png\" alt=\"{planche.Title} ({theme})\">"
                + $"<figcaption>{theme}</figcaption></figure>");
        html.AppendLine("</div>");
    }
    html.AppendLine("</body></html>");
    return html.ToString();
}

/// <summary>Même amorçage qu'une app hôte (cf. HDev.UI.Controls.Demo/App.axaml)</summary>
sealed class GalleryApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Gallery/"))
        {
            Source = new Uri("avares://HDev.UI.Controls/Theme/DataGrid/Index.axaml")
        });

        // Accent Ubuntu des contrôles Fluent internes (TextBox des éditeurs…)
        Resources["SystemAccentColor"] = Color.Parse("#E95420");
        Resources["SystemAccentColorDark1"] = Color.Parse("#DB4C1B");
        Resources["SystemAccentColorDark2"] = Color.Parse("#C64614");
        Resources["SystemAccentColorDark3"] = Color.Parse("#A83C11");
        Resources["SystemAccentColorLight1"] = Color.Parse("#ED7748");
        Resources["SystemAccentColorLight2"] = Color.Parse("#F19970");
        Resources["SystemAccentColorLight3"] = Color.Parse("#F6BB98");
    }
}
