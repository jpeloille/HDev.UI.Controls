// Prototype jetable du lot E1 (Conception-Socle-Editeurs.md, § 4), hors solution :
// mécanismes Avalonia 12 du contrat de valeur et de la forme B, mesurés en headless.
//   dotnet run --project tests/Prototypes/SocleE1
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Spike;

AppBuilder.Configure<SpikeApp>().UseSkia()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();

int failed = 0;
void Check(string n, bool ok) { Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {n}"); if (!ok) failed++; }
void Pump() { for (int i = 0; i < 3; i++) Dispatcher.UIThread.RunJobs(); }

var vm = new SpikeVm { Date = new DateTime(2026, 9, 14), Amount = 12.5m };
var view = new SpikeView { DataContext = vm };
var win = new Window { Content = view, Width = 400, Height = 300 }; win.Show(); Pump();
var de = view.FindControl<SpikeDateEdit>("De")!; var sp = view.FindControl<SpikeSpinEdit>("Sp")!;
var form = view.FindControl<StackPanel>("Form")!;
var recus = new List<(string, object?, object?, ValueOrigin)>();
var sources = new List<object?>();
form.AddHandler(EditorBase.ValueChangedEvent, (object? s, ValueChangedArgs e) => { recus.Add((e.Source?.GetType().Name ?? "", e.OldValue, e.NewValue, e.Origin)); sources.Add(e.Source); });

Check("XAML compilé : la liaison pose la valeur initiale (VM → éditeur)", de.Value == new DateTime(2026, 9, 14) && sp.Value == 12.5m);
vm.Date = new DateTime(2026, 10, 1); Pump();
Check("VM → éditeur après coup", de.Value == new DateTime(2026, 10, 1));
de.CommitFromUser(new DateTime(2026, 12, 25)); Pump();
Check("éditeur → VM sans Mode explicite (TwoWay par défaut), liaison intacte", vm.Date == new DateTime(2026, 12, 25));
vm.Date = new DateTime(2027, 1, 2); Pump();
Check("la liaison survit au SetCurrentValue (VM → éditeur encore)", de.Value == new DateTime(2027, 1, 2));
sp.CommitFromUser(42m); Pump();
Check("deux fermetures génériques (DateTime?, decimal) cohabitent", vm.Amount == 42m);
Check("événement routé reçu par le conteneur, avec l'origine",
    recus.Any(r => r.Item4 == ValueOrigin.User && Equals(r.Item3, new DateTime(2026, 12, 25)))
    && recus.Any(r => r.Item4 == ValueOrigin.Code && Equals(r.Item3, new DateTime(2027, 1, 2)))
    && recus.Any(r => r.Item1 == nameof(SpikeSpinEdit) && Equals(r.Item3, 42m)));
vm.SetDateError("La date doit être un jour ouvré"); Pump();
Check($"erreur INotifyDataErrorInfo remontée (lu : {de.BindingError ?? "null"})", de.BindingError == "La date doit être un jour ouvré");
vm.SetDateError(null); Pump();
Check("erreur effacée côté VM, effacée côté éditeur", de.BindingError == null);
var ob = view.FindControl<SpikeObjEdit>("Ob")!;
ob.CommitFromUser(new DateTime(1990, 5, 17)); Pump();
Console.WriteLine($"    D4 : valeur non typée, source DateOnly? nulle, l'éditeur produit un DateTime -> VM.Birth = {vm.Birth?.ToString() ?? "null"} ; erreur = {ob.BindingError ?? "aucune"}");
vm.Birth = new DateOnly(1990, 5, 17); Pump();
Console.WriteLine($"    D4 : source DateOnly renseignée -> l'éditeur reçoit {ob.Value?.GetType().Name}");
var fe = view.FindControl<SpikeFieldEdit>("Fe")!;
Check("forme B : boutons déclarés en XAML", fe.Buttons.Count == 2 && fe.Buttons[0].Kind == "Clear");
vm.CanClear = true; Pump(); var montre = fe.Buttons[0].IsShown; vm.CanClear = false; Pump();
Check("forme B : un bouton se lie au ViewModel (IsShown suit CanClear)", montre && !fe.Buttons[0].IsShown);
fe.Popup.IsOpen = true; Pump();
var label = fe.PopupContent!.FindControl<TextBlock>("PopupLabel") ?? (fe.PopupContent as StackPanel)!.Children.OfType<TextBlock>().First();
Check($"forme B : contenu de popup déclaré en XAML, lié au ViewModel (lu : {label.Text ?? "null"})", label.Text == "Alpha");
var inner = (fe.PopupContent as StackPanel)!.Children.OfType<SpikeSpinEdit>().First();
var avantForm = sources.Count; var surFe = 0; fe.ValueChanged += (_, _) => surFe++;
var trajet = new List<string>();
fe.AddHandler(EditorBase.ValueChangedEvent, (object? s, ValueChangedArgs e) => { if (ReferenceEquals(e.Source, inner)) trajet.Add($"hôte (traité={e.Handled})"); }, Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
win.AddHandler(EditorBase.ValueChangedEvent, (object? s, ValueChangedArgs e) => { if (ReferenceEquals(e.Source, inner)) trajet.Add($"fenêtre (traité={e.Handled})"); }, Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
inner.CommitFromUser(7m); Pump();
var vusApresInterne = sources.Skip(avantForm).ToList();
Console.WriteLine($"    trajet de l'événement interne : {string.Join(" -> ", trajet)}");
Check($"imbrication : la pièce interne a bien poussé sa valeur (VM.Amount = {vm.Amount})", vm.Amount == 7m);
Check($"imbrication : le ValueChanged de la pièce interne ne sort pas de l'hôte (vus au formulaire : {string.Join(", ", vusApresInterne.Select(x => ReferenceEquals(x, inner) ? "interne" : (x as Control)?.Name))} ; hôte : {surFe})",
    !vusApresInterne.Any(x => ReferenceEquals(x, inner)) && surFe == 0);
var avantHote = sources.Count; fe.CommitFromUser("Beta"); Pump();
Check("imbrication : les changements propres de l'hôte sortent", surFe == 1 && sources.Skip(avantHote).Any(x => ReferenceEquals(x, fe)) && vm.Name == "Beta");
fe.Popup.IsOpen = false; Pump();
var bo = view.FindControl<SpikeBoundedEdit>("Bo")!;
vm.Percent = 150m; Pump();
Console.WriteLine($"    bornage : VM pose 150 -> éditeur {bo.Value}, VM {vm.Percent}");
bo.CommitFromUser(250m); Pump();
Console.WriteLine($"    bornage : l'utilisateur pose 250 -> éditeur {bo.Value}, VM {vm.Percent}");
Console.WriteLine($"    porteur des événements hors hiérarchie : {EditorEvents.ValueChangedEvent.OwnerType.Name}");
de.Value = new DateTime(2030, 1, 1); Pump();
Console.WriteLine($"    SetValue sur une valeur liée TwoWay : VM.Date = {vm.Date:yyyy-MM-dd}");
vm.Date = new DateTime(2031, 2, 2); Pump();
Console.WriteLine($"    puis le VM repousse 2031-02-02 : éditeur = {de.Value:yyyy-MM-dd} (liaison {(de.Value == new DateTime(2031, 2, 2) ? "intacte" : "rompue")})");
foreach (var r in recus) Console.WriteLine($"    recu {r.Item1} {r.Item2} -> {r.Item3} ({r.Item4})");
Console.WriteLine(failed == 0 ? "SPIKE : ALL PASS" : $"SPIKE : {failed} FAILED");

public class SpikeApp : Application
{
    public override void Initialize() => Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
}
