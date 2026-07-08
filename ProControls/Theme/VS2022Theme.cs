using Avalonia.Media;

namespace ProControls.Theme;

/// <summary>
/// Design tokens VS2022 - Système de design centralisé
/// Toutes les couleurs, dimensions et constantes visuelles
/// </summary>
public static class VS2022Theme
{
    // ═══════════════════════════════════════════════════════════════
    // COULEURS DE FOND
    // ═══════════════════════════════════════════════════════════════
    
    public static class Background
    {
        public static readonly Color Window = Color.Parse("#F5F5F5");
        public static readonly Color Panel = Color.Parse("#FFFFFF");
        public static readonly Color PanelAlt = Color.Parse("#FAFAFA");
        public static readonly Color Toolbar = Color.Parse("#EEEEF2");
        public static readonly Color StatusBar = Color.Parse("#68217A"); // Accent violet VS
        
        // Contrôles
        public static readonly Color Control = Color.Parse("#FDFDFD");
        public static readonly Color ControlHover = Color.Parse("#C9DEF5");
        public static readonly Color ControlPressed = Color.Parse("#A6C8E6");
        public static readonly Color ControlDisabled = Color.Parse("#F5F5F5");
        public static readonly Color ControlFocused = Color.Parse("#E5F1FB");
        
        // Sélection
        public static readonly Color Selection = Color.Parse("#0078D4");
        public static readonly Color SelectionInactive = Color.Parse("#CCCEDB");
        
        // États spéciaux
        public static readonly Color Success = Color.Parse("#DFF6DD");
        public static readonly Color Warning = Color.Parse("#FFF4CE");
        public static readonly Color Error = Color.Parse("#FDE7E9");
        public static readonly Color Info = Color.Parse("#E5F1FB");
    }
    
    // ═══════════════════════════════════════════════════════════════
    // COULEURS DE BORDURE
    // ═══════════════════════════════════════════════════════════════
    
    public static class Border
    {
        public static readonly Color Default = Color.Parse("#CCCEDB");
        public static readonly Color Hover = Color.Parse("#0078D4");
        public static readonly Color Pressed = Color.Parse("#005A9E");
        public static readonly Color Focused = Color.Parse("#0078D4");
        public static readonly Color Disabled = Color.Parse("#E0E0E0");
        
        public static readonly Color Subtle = Color.Parse("#E5E5E5");
        public static readonly Color Strong = Color.Parse("#8A8A8A");
        
        // Focus ring
        public static readonly Color FocusOuter = Color.Parse("#005A9E");
        public static readonly Color FocusInner = Color.Parse("#FFFFFF");
    }
    
    // ═══════════════════════════════════════════════════════════════
    // COULEURS DE TEXTE
    // ═══════════════════════════════════════════════════════════════
    
    public static class Text
    {
        public static readonly Color Primary = Color.Parse("#1E1E1E");
        public static readonly Color Secondary = Color.Parse("#5D5D5D");
        public static readonly Color Tertiary = Color.Parse("#8A8A8A");
        public static readonly Color Disabled = Color.Parse("#A0A0A0");
        public static readonly Color Placeholder = Color.Parse("#8A8A8A");
        
        public static readonly Color OnAccent = Color.Parse("#FFFFFF");
        public static readonly Color Link = Color.Parse("#0066CC");
        public static readonly Color LinkHover = Color.Parse("#004080");
        
        public static readonly Color Success = Color.Parse("#0E700E");
        public static readonly Color Warning = Color.Parse("#8A6914");
        public static readonly Color Error = Color.Parse("#C42B1C");
    }
    
    // ═══════════════════════════════════════════════════════════════
    // COULEURS D'ACCENT
    // ═══════════════════════════════════════════════════════════════
    
    public static class Accent
    {
        public static readonly Color Primary = Color.Parse("#0078D4");
        public static readonly Color PrimaryHover = Color.Parse("#106EBE");
        public static readonly Color PrimaryPressed = Color.Parse("#005A9E");
        
        public static readonly Color Secondary = Color.Parse("#68217A"); // Violet VS
        public static readonly Color SecondaryHover = Color.Parse("#7B2A8E");
        public static readonly Color SecondaryPressed = Color.Parse("#551B63");
        
        public static readonly Color Success = Color.Parse("#107C10");
        public static readonly Color Warning = Color.Parse("#CA5010");
        public static readonly Color Error = Color.Parse("#C42B1C");
    }

    // ═══════════════════════════════════════════════════════════════
    // COULEURS DANGER (BOUTONS DESTRUCTIFS)
    // ═══════════════════════════════════════════════════════════════

    public static class Danger
    {
        public static readonly Color Default = Color.Parse("#C42B1C");
        public static readonly Color Hover = Color.Parse("#D83B2B");
        public static readonly Color Pressed = Color.Parse("#A82515");

        // États disabled
        public static readonly Color DisabledBackground = Color.Parse("#F5E5E5");
        public static readonly Color DisabledBorder = Color.Parse("#DDBBBB");
        public static readonly Color DisabledText = Color.Parse("#AA8888");
    }

    // ═══════════════════════════════════════════════════════════════
    // CHROME DE FENÊTRE
    // ═══════════════════════════════════════════════════════════════

    public static class WindowChrome
    {
        public static readonly Color CloseHover = Color.Parse("#E81123");
        public static readonly Color ClosePressed = Color.Parse("#C42B1C");
    }

    // ═══════════════════════════════════════════════════════════════
    // ÉTATS DISABLED GÉNÉRIQUES
    // ═══════════════════════════════════════════════════════════════

    public static class Disabled
    {
        public static readonly Color Background = Color.Parse("#CCCCCC");
        public static readonly Color Border = Color.Parse("#BBBBBB");
        public static readonly Color Text = Color.Parse("#888888");
    }

    // ═══════════════════════════════════════════════════════════════
    // DIMENSIONS
    // ═══════════════════════════════════════════════════════════════
    
    public static class Size
    {
        // Hauteurs de contrôles
        public const double ControlHeightSmall = 24;
        public const double ControlHeightMedium = 32;
        public const double ControlHeightLarge = 40;
        
        // Rayons de coins
        public const double CornerRadiusSmall = 2;
        public const double CornerRadiusMedium = 4;
        public const double CornerRadiusLarge = 6;
        
        // Épaisseurs de bordure
        public const double BorderThin = 1;
        public const double BorderMedium = 1.5;
        public const double BorderThick = 2;
        
        // Espacements
        public const double SpacingXS = 4;
        public const double SpacingS = 8;
        public const double SpacingM = 12;
        public const double SpacingL = 16;
        public const double SpacingXL = 24;
        
        // Paddings internes
        public const double PaddingXS = 4;
        public const double PaddingS = 8;
        public const double PaddingM = 12;
        public const double PaddingL = 16;
        
        // Icônes
        public const double IconSmall = 12;
        public const double IconMedium = 16;
        public const double IconLarge = 20;
    }
    
    // ═══════════════════════════════════════════════════════════════
    // TYPOGRAPHIE
    // ═══════════════════════════════════════════════════════════════
    
    public static class Typography
    {
        public static readonly string FontFamily = "Segoe UI, Inter, sans-serif";
        
        public const double FontSizeCaption = 11;
        public const double FontSizeBody = 13;
        public const double FontSizeSubtitle = 14;
        public const double FontSizeTitle = 18;
        public const double FontSizeHeader = 24;
        
        public static readonly FontWeight WeightLight = FontWeight.Light;
        public static readonly FontWeight WeightRegular = FontWeight.Regular;
        public static readonly FontWeight WeightMedium = FontWeight.Medium;
        public static readonly FontWeight WeightSemiBold = FontWeight.SemiBold;
        public static readonly FontWeight WeightBold = FontWeight.Bold;
    }
    
    // ═══════════════════════════════════════════════════════════════
    // OMBRES
    // ═══════════════════════════════════════════════════════════════
    
    public static class Shadow
    {
        public static readonly Color Color = Color.Parse("#1A000000");
        public static readonly Color ColorStrong = Color.Parse("#33000000");
        
        // Paramètres d'ombre (offsetX, offsetY, blur, spread)
        public static readonly (double, double, double, double) Subtle = (0, 1, 2, 0);
        public static readonly (double, double, double, double) Medium = (0, 2, 6, 0);
        public static readonly (double, double, double, double) Strong = (0, 4, 12, 0);
        public static readonly (double, double, double, double) Elevated = (0, 8, 24, 0);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════
    
    public static SolidColorBrush ToBrush(Color color) => new(color);
    
    public static Pen ToPen(Color color, double thickness = 1) 
        => new(new SolidColorBrush(color), thickness);
    
    public static Color WithOpacity(Color color, byte opacity)
        => Color.FromArgb(opacity, color.R, color.G, color.B);
    
    public static Color Lerp(Color a, Color b, double t)
    {
        return Color.FromArgb(
            (byte)(a.A + (b.A - a.A) * t),
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));
    }
}
