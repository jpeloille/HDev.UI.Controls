using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace Interaction;

/// <summary>Formulaire témoin du contrat des éditeurs (E1), lié en XAML compilé</summary>
public partial class FormulaireE1 : UserControl
{
    public FormulaireE1() => InitializeComponent();
}

/// <summary>ViewModel témoin : notification de propriété et erreurs INotifyDataErrorInfo</summary>
public sealed class FormulaireE1Vm : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private DateTime? _depart = new DateTime(2026, 9, 14);
    private string? _erreurDepart;

    public DateTime? Depart
    {
        get => _depart;
        set
        {
            if (_depart == value) return;
            _depart = value;
            Raise();
        }
    }

    /// <summary>Pose (ou retire, avec null) l'erreur métier de Depart</summary>
    public void ErreurDepart(string? erreur)
    {
        _erreurDepart = erreur;
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Depart)));
    }

    public bool HasErrors => _erreurDepart != null;

    public IEnumerable GetErrors(string? propertyName)
        => propertyName == nameof(Depart) && _erreurDepart != null ? new[] { _erreurDepart } : Array.Empty<string>();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    private void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
