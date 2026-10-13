using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Controls;

namespace Interaction;

/// <summary>Formulaire témoin du socle composite (E2), lié en XAML compilé</summary>
public partial class FormulaireE2 : UserControl
{
    public FormulaireE2() => InitializeComponent();
}

/// <summary>ViewModel témoin : texte, visibilité d'un bouton, commande, valeur libre</summary>
public sealed class FormulaireE2Vm : INotifyPropertyChanged
{
    private string? _chemin = "/data/vols";
    private bool _effacerVisible;
    private object? _equipage;

    public string? Chemin { get => _chemin; set { if (_chemin == value) return; _chemin = value; Raise(); } }

    public bool EffacerVisible { get => _effacerVisible; set { _effacerVisible = value; Raise(); } }

    public object? Equipage { get => _equipage; set { if (Equals(_equipage, value)) return; _equipage = value; Raise(); } }

    public List<string> Membres { get; } = new() { "Awa Wamytan", "Teiki Brothers", "Lucie Martin" };

    public CommandeE2 Parcourir { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Commande témoin : compte ses exécutions, activable</summary>
public sealed class CommandeE2 : ICommand
{
    private bool _active = true;

    public int Executions { get; private set; }

    public bool Active
    {
        get => _active;
        set { _active = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    }

    public bool CanExecute(object? parameter) => _active;

    public void Execute(object? parameter) => Executions++;

    public event EventHandler? CanExecuteChanged;
}
