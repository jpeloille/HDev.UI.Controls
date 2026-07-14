using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Runtime.CompilerServices;

namespace ProControls.Controls;

/// <summary>
/// Registre d'accélérateurs clavier par fenêtre : les Shortcut des menus et
/// du ruban deviennent RÉELS. Écoute le KeyDown de la fenêtre (bulle : un
/// TextBox qui consomme Ctrl+C garde la priorité), déclenche la première
/// correspondance active.
/// </summary>
public sealed class ProShortcutManager
{
    private static readonly ConditionalWeakTable<TopLevel, ProShortcutManager> Managers = new();

    private readonly List<Binding> _bindings = new();

    private sealed class Binding
    {
        public required KeyGesture Gesture { get; init; }
        public required Action Execute { get; init; }
        public Func<bool>? CanExecute { get; init; }
        public bool Disposed;
    }

    private ProShortcutManager(TopLevel topLevel)
    {
        topLevel.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble);
    }

    /// <summary>Le gestionnaire de la fenêtre (créé au premier appel)</summary>
    public static ProShortcutManager GetFor(TopLevel topLevel)
        => Managers.GetValue(topLevel, tl => new ProShortcutManager(tl));

    /// <summary>
    /// « Ctrl+N », « Ctrl+Shift+S », « F1 », « Alt+F4 »… → KeyGesture ;
    /// null si la syntaxe n'est pas reconnue (raccourci purement décoratif)
    /// </summary>
    public static KeyGesture? TryParse(string? shortcut)
    {
        if (string.IsNullOrWhiteSpace(shortcut)) return null;
        try
        {
            return KeyGesture.Parse(shortcut);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Enregistre un accélérateur ; disposer pour retirer</summary>
    public IDisposable Register(KeyGesture gesture, Action execute, Func<bool>? canExecute = null)
    {
        var binding = new Binding { Gesture = gesture, Execute = execute, CanExecute = canExecute };
        _bindings.Add(binding);
        return new Registration(this, binding);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        foreach (var binding in _bindings)
        {
            if (binding.Disposed || !binding.Gesture.Matches(e)) continue;
            if (binding.CanExecute != null && !binding.CanExecute()) continue;

            binding.Execute();
            e.Handled = true;
            return;
        }
    }

    private sealed class Registration : IDisposable
    {
        private readonly ProShortcutManager _manager;
        private readonly Binding _binding;

        public Registration(ProShortcutManager manager, Binding binding)
        {
            _manager = manager;
            _binding = binding;
        }

        public void Dispose()
        {
            _binding.Disposed = true;
            _manager._bindings.Remove(_binding);
        }
    }
}
