using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Reactive;

namespace HDev.UI.Controls;

/// <summary>
/// Rendu « désactivé » des contrôles composites (onglets, barre de navigation, accordéon,
/// segments, progression, badge) : atténuation uniforme, qui suit aussi un parent désactivé.
/// </summary>
internal static class HDevDisabled
{
    public const double Opacity = 0.5;

    /// <summary>À appeler depuis le constructeur statique du contrôle</summary>
    public static void Dim<T>() where T : Control
        => InputElement.IsEffectivelyEnabledProperty.Changed.Subscribe(
            new AnonymousObserver<AvaloniaPropertyChangedEventArgs<bool>>(e =>
            {
                if (e.Sender is T control)
                    control.Opacity = control.IsEffectivelyEnabled ? 1 : Opacity;
            }));
}
