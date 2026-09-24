using System.Collections.Generic;
using System.Linq;

namespace HDev.UI.Controls;

/// <summary>
/// Historique undo/redo par snapshots (chaînes JSON opaques). Pur et testable :
/// ne connaît ni GanttProject ni Avalonia. HDevGantt l'alimente avec des ToJson()
/// et restaure via LoadJson().
///
/// Protocole : appeler <see cref="Record"/> AVANT une mutation réelle (pousse
/// l'état courant sur la pile undo, vide la pile redo). <see cref="Undo"/> /
/// <see cref="Redo"/> reçoivent l'état vivant courant et rendent l'état à restaurer.
/// </summary>
public sealed class GanttHistory
{
    private readonly List<string> _undo = new();
    private readonly List<string> _redo = new();

    /// <summary>Profondeur maximale (un snapshot = un projet complet sérialisé).</summary>
    public int Capacity { get; }

    /// <summary>Notifié quand la disponibilité undo/redo change.</summary>
    public event Action? Changed;

    public GanttHistory(int capacity = 50)
    {
        Capacity = capacity < 1 ? 1 : capacity;
    }

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    /// <summary>
    /// Enregistre l'état AVANT une mutation. Ignore un snapshot identique au sommet
    /// (mutation no-op / double-capture) pour éviter les entrées fantômes.
    /// </summary>
    public void Record(string snapshotBefore)
    {
        if (_undo.Count > 0 && _undo[^1] == snapshotBefore) return;
        _undo.Add(snapshotBefore);
        if (_undo.Count > Capacity) _undo.RemoveAt(0);
        _redo.Clear();
        Changed?.Invoke();
    }

    /// <summary>
    /// Rend l'état à restaurer (le sommet undo), en empilant l'état courant côté redo.
    /// Retourne null si rien à annuler.
    /// </summary>
    public string? Undo(string currentSnapshot)
    {
        if (_undo.Count == 0) return null;
        var target = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(currentSnapshot);
        Changed?.Invoke();
        return target;
    }

    /// <summary>Rend l'état à rétablir (le sommet redo), en empilant l'état courant côté undo.</summary>
    public string? Redo(string currentSnapshot)
    {
        if (_redo.Count == 0) return null;
        var target = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(currentSnapshot);
        Changed?.Invoke();
        return target;
    }

    /// <summary>Vide l'historique (ex. au chargement d'un nouveau projet).</summary>
    public void Clear()
    {
        if (_undo.Count == 0 && _redo.Count == 0) return;
        _undo.Clear();
        _redo.Clear();
        Changed?.Invoke();
    }
}
