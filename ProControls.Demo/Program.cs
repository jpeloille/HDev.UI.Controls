using Avalonia;
using System;

namespace ProControls.Demo;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // Popups en overlay (rendus dans la surface de la fenêtre principale)
            // au lieu de fenêtres natives : sur les compositeurs Linux (X11/XWayland),
            // la surface native transparente des popups ne peint aucun contenu et se
            // referme sur la moindre perte de focus. L'overlay rend enfin visibles
            // les dropdowns ProComboBox/menus et correspond au mode que suppose déjà
            // la garde de dismiss (IsInsidePopupHost -> OverlayPopupHost).
            .With(new X11PlatformOptions { OverlayPopups = true })
            .WithInterFont()
            .LogToTrace();
}
