using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace Interaction;

/// <summary>Formulaire du portage des éditeurs (E3), lié en XAML compilé</summary>
public partial class FormulaireE3 : UserControl
{
    public FormulaireE3() => InitializeComponent();
}

/// <summary>ViewModel du formulaire E3 : une propriété par éditeur porté</summary>
public sealed class FormulaireE3Vm : INotifyPropertyChanged
{
    private string? _nom = "TY201";
    private decimal _quantite = 12.5m;
    private TimeSpan? _heure = new TimeSpan(8, 30, 0);
    private string? _recherche = "Gantt";
    private object? _priorite = "Normale";
    private object? _chefId = 2;
    private bool? _confirme = false;
    private bool _actif, _matin = true, _soir;
    private double _charge = 40;
    private DateTime? _jour = new DateTime(2026, 10, 13, 9, 30, 0);
    private IReadOnlyList<object> _destinataires = new object[] { "ops@aircal.nc" };

    public string? Nom { get => _nom; set => Set(ref _nom, value); }
    public decimal Quantite { get => _quantite; set => Set(ref _quantite, value); }
    public TimeSpan? Heure { get => _heure; set => Set(ref _heure, value); }
    public string[] Priorites { get; } = { "Basse", "Normale", "Haute" };
    public object? Priorite { get => _priorite; set => Set(ref _priorite, value); }
    public Equipier[] Equipiers { get; } = { new(1, "Awa Wamytan"), new(2, "Teiki Brothers"), new(3, "Lucie Martin") };
    public object? ChefId { get => _chefId; set => Set(ref _chefId, value); }
    public IReadOnlyList<object> Destinataires { get => _destinataires; set => Set(ref _destinataires, value); }
    public bool? Confirme { get => _confirme; set => Set(ref _confirme, value); }
    public bool Actif { get => _actif; set => Set(ref _actif, value); }
    public bool Matin { get => _matin; set => Set(ref _matin, value); }
    public bool Soir { get => _soir; set => Set(ref _soir, value); }
    public double Charge { get => _charge; set => Set(ref _charge, value); }
    public DateTime? Jour { get => _jour; set => Set(ref _jour, value); }
    public string? Recherche { get => _recherche; set => Set(ref _recherche, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>Ligne du référentiel du LookUpEdit</summary>
public sealed record Equipier(int Id, string Nom);
