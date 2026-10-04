using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using HDev.UI.Controls.Theme;

namespace HDev.UI.Controls;

/// <summary>
/// Focus clavier visible (« focus-visible ») et anneau de focus unique de la suite.
///
/// L'anneau ne s'affiche que si le focus arrive au clavier : Tab, flèches, ou toute
/// prise de focus qui suit une frappe (un groupe radio qui déplace le focus au clavier
/// appelle Focus() sans méthode de navigation). Jamais après un clic : la souris montre
/// déjà où l'on agit. Les champs de saisie, eux, gardent leur halo à tout focus (le
/// caret seul ne suffit pas à repérer le champ actif).
/// </summary>
internal static class HDevFocus
{
    private static bool _keyboardModality;

    static HDevFocus()
    {
        // Modalité de la dernière interaction, observée au plus tôt (tunnel), sur toute fenêtre
        InputElement.KeyDownEvent.AddClassHandler<TopLevel>((_, _) => _keyboardModality = true,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerPressedEvent.AddClassHandler<TopLevel>((_, _) => _keyboardModality = false,
            RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    /// <summary>Force l'abonnement aux entrées avant la première frappe</summary>
    public static void EnsureInitialized() { }

    /// <summary>Vrai si ce gain de focus doit montrer l'anneau</summary>
    public static bool IsVisible(FocusChangedEventArgs e)
        => e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional || _keyboardModality;

    /// <summary>
    /// Anneau rectangulaire de 2 px d'accent plein (≥ 3:1). Extérieur, à 1 px du bord,
    /// pour un contrôle autonome ; intérieur pour un élément de collection (segment,
    /// onglet, item de rail, en-tête), que l'hôte rognerait sinon.
    /// </summary>
    public static void DrawRing(DrawingContext context, Rect rect, double cornerRadius, bool inside = false)
    {
        var width = HDevTheme.Size.FocusRingWidth;
        var pen = new Pen(new SolidColorBrush(HDevTheme.Border.FocusOuter), width);

        if (inside)
        {
            var ring = rect.Deflate(width / 2);
            var radius = Math.Max(0, cornerRadius - width / 2);
            context.DrawRectangle(null, pen, ring, radius, radius);
        }
        else
        {
            var grow = HDevTheme.Size.FocusRingOffset + width / 2;
            context.DrawRectangle(null, pen, rect.Inflate(grow), cornerRadius + grow, cornerRadius + grow);
        }
    }

    /// <summary>Anneau elliptique extérieur (radio, pouce de curseur)</summary>
    public static void DrawEllipseRing(DrawingContext context, Point center, double radius)
    {
        var width = HDevTheme.Size.FocusRingWidth;
        var pen = new Pen(new SolidColorBrush(HDevTheme.Border.FocusOuter), width);
        var ringRadius = radius + HDevTheme.Size.FocusRingOffset + width / 2;
        context.DrawEllipse(null, pen, center, ringRadius, ringRadius);
    }
}
