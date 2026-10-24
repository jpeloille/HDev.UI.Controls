using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls.Gallery;

/// <summary>
/// Export pour Claude Design (<c>/design-sync</c>) : jetons générés depuis <see cref="HDevPalette"/>
/// (jamais recopiés à la main) et une carte HTML par groupe, qui affiche les planches rendues.
///   artifacts/design/tokens/hdev-tokens.json · hdev-tokens.css · cards/&lt;groupe&gt;.html · img/
/// </summary>
static class DesignExport
{
    // Planche → groupe de carte
    private static readonly (string Group, string[] Sheets)[] Groups =
    {
        ("Fondations", new[] { "fondations" }),
        ("Boutons", new[] { "boutons" }),
        ("Champs", new[] { "champs" }),
        ("Choix", new[] { "choix" }),
        ("Collections", new[] { "collections" }),
        ("Popovers", new[] { "barres" }),
        ("Données", new[] { "donnees", "documents", "grilles" }),
    };

    public static string Write(string repoRoot, string galleryDir)
    {
        var designDir = Path.Combine(repoRoot, "artifacts", "design");
        if (Directory.Exists(designDir)) Directory.Delete(designDir, recursive: true);
        Directory.CreateDirectory(Path.Combine(designDir, "tokens"));
        Directory.CreateDirectory(Path.Combine(designDir, "cards"));
        Directory.CreateDirectory(Path.Combine(designDir, "img"));

        var light = Tokens(HDevThemeVariant.Light);
        var dark = Tokens(HDevThemeVariant.Dark);
        HDevTheme.Variant = HDevThemeVariant.Light;

        File.WriteAllText(Path.Combine(designDir, "tokens", "hdev-tokens.json"),
            JsonSerializer.Serialize(new { light, dark, scale = Scale() },
                new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(designDir, "tokens", "hdev-tokens.css"), Css(light, dark));

        foreach (var (group, sheets) in Groups)
        {
            var html = new StringBuilder();
            html.AppendLine($"<!-- @dsCard group=\"{group}\" -->");
            html.AppendLine("<!doctype html><html lang=\"fr\"><meta charset=\"utf-8\">");
            html.AppendLine($"<title>{group}</title>");
            html.AppendLine("<style>body{font-family:Inter,system-ui,sans-serif;margin:16px;background:#f4f4f3}"
                + "figure{margin:0 0 16px}img{max-width:100%;border:1px solid #bbb}figcaption{font-size:12px;color:#555}</style>");
            html.AppendLine($"<h1>{group}</h1>");
            foreach (var sheet in sheets)
            foreach (var theme in new[] { "clair", "sombre" })
            {
                var source = Path.Combine(galleryDir, theme, sheet + ".png");
                if (!File.Exists(source)) continue;
                var name = $"{sheet}-{theme}.png";
                File.Copy(source, Path.Combine(designDir, "img", name), overwrite: true);
                html.AppendLine($"<figure><img src=\"../img/{name}\" alt=\"{sheet} ({theme})\"><figcaption>{sheet} · {theme}</figcaption></figure>");
            }
            File.WriteAllText(Path.Combine(designDir, "cards", Slug(group) + ".html"), html.ToString());
        }

        return designDir;
    }

    private static string Slug(string s) => new string(s.ToLowerInvariant()
        .Normalize(NormalizationForm.FormD).Where(c => char.IsAsciiLetterOrDigit(c)).ToArray());

    /// <summary>Toutes les couleurs de la palette active, plus les jetons dérivés et la série catégorielle</summary>
    private static SortedDictionary<string, string> Tokens(HDevThemeVariant variant)
    {
        HDevTheme.Variant = variant;
        var tokens = new SortedDictionary<string, string>();
        foreach (var property in typeof(HDevPalette).GetProperties())
            if (property.GetValue(HDevTheme.Current) is Color color)
                tokens[Kebab(property.Name)] = Hex(color);

        // Jetons optionnels : valeur résolue (explicite ou dérivée)
        tokens["accent-fill"] = Hex(HDevTheme.Accent.Fill);
        tokens["accent-fill-hover"] = Hex(HDevTheme.Accent.FillHover);
        tokens["accent-fill-pressed"] = Hex(HDevTheme.Accent.FillPressed);
        tokens["accent-success-fill"] = Hex(HDevTheme.Accent.SuccessFill);
        tokens["accent-info-fill"] = Hex(HDevTheme.Accent.InfoFill);
        tokens["border-input"] = Hex(HDevTheme.Border.Input);
        tokens["border-input-hover"] = Hex(HDevTheme.Border.InputHover);
        tokens["text-selection"] = Hex(HDevTheme.Background.TextSelection);
        tokens["text-on-warning"] = Hex(HDevTheme.Text.OnWarning);
        for (var i = 0; i < HDevTheme.CategoricalCount; i++)
            tokens[$"categorical-{i + 1}"] = Hex(HDevTheme.Categorical(i));
        return tokens;
    }

    /// <summary>Constantes d'échelle (tailles, rayons, hauteurs, typographie)</summary>
    private static SortedDictionary<string, double> Scale()
    {
        var scale = new SortedDictionary<string, double>();
        foreach (var type in new[] { typeof(HDevTheme.Size), typeof(HDevTheme.Typography) })
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                if (field.IsLiteral && field.GetRawConstantValue() is double value)
                    scale[$"{type.Name.ToLowerInvariant()}-{Kebab(field.Name)}"] = value;
        return scale;
    }

    private static string Css(IDictionary<string, string> light, IDictionary<string, string> dark)
    {
        var css = new StringBuilder("/* Généré par tests/Gallery depuis HDevPalette — ne pas éditer à la main */\n:root {\n");
        foreach (var (name, value) in light) css.AppendLine($"  --hdev-{name}: {value};");
        css.AppendLine("}\n\n:root[data-theme=\"dark\"] {");
        foreach (var (name, value) in dark) css.AppendLine($"  --hdev-{name}: {value};");
        css.AppendLine("}");
        return css.ToString();
    }

    private static string Hex(Color c) => c.A == 255
        ? $"#{c.R:X2}{c.G:X2}{c.B:X2}"
        : $"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}";

    private static string Kebab(string name)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) sb.Append('-');
            sb.Append(char.ToLower(name[i], CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }
}
