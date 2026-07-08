using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ProControls.Controls;

// Pilote les contrôles ProControls via de vraies entrées simulées (pipeline
// d'input headless) et vérifie les correctifs des 4 bugs bloquants :
//   1. Les boutons du ruban déclenchent Click (hit-test partagé avec le rendu)
//   2. Les sous-menus s'ouvrent et la fermeture remonte la chaîne de popups
//   3. Les popups affichent les items originaux (état IsChecked partagé)
//   4. La barre de progression indéterminée s'anime (timer actif)
// En headless, les popups sont hébergés dans l'OverlayLayer de la fenêtre :
// les items de menu sont donc atteignables en coordonnées fenêtre.

int failed = 0;
void Check(string name, bool ok)
{
    Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {name}");
    if (!ok) failed++;
}

AppBuilder.Configure<TestApp>()
    .UseSkia()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .SetupWithoutStarting();

void Pump(Window win)
{
    for (int i = 0; i < 3; i++)
    {
        Dispatcher.UIThread.RunJobs();
        win.Measure(new Size(win.Width, win.Height));
        win.Arrange(new Rect(0, 0, win.Width, win.Height));
    }
    Dispatcher.UIThread.RunJobs();
}

void ClickAt(Window win, Point p)
{
    win.MouseMove(p, RawInputModifiers.None);
    win.MouseDown(p, MouseButton.Left, RawInputModifiers.None);
    win.MouseUp(p, MouseButton.Left, RawInputModifiers.None);
    Pump(win);
}

// Centre d'un contrôle en coordonnées fenêtre (fonctionne aussi pour les
// contrôles hébergés dans l'OverlayLayer, donc dans les popups headless)
Point WindowCenter(Window win, Control c) =>
    c.TranslatePoint(new Point(c.Bounds.Width / 2, c.Bounds.Height / 2), win)!.Value;

// ═════════════════════════════════════════════════════════════════
// 1. RUBAN — clics, toggle, désactivé, dropdown
// ═════════════════════════════════════════════════════════════════
Console.WriteLine("Ruban :");

int runClicks = 0, copyClicks = 0, disabledClicks = 0;
bool dropdownOpening = false;

var ribbon = new ProRibbon();
var tab = new ProRibbonTab("Home");
tab.AddGroup("Actions", g =>
{
    g.AddLargeButton("Run", "R", () => runClicks++);
    g.AddSmallButton("Bold", "B");
    g.AddSmallButton("Copy", "C", () => copyClicks++);
    g.AddSmallButton("Del", "D", () => disabledClicks++);
    g.AddDropdownButton("More", "M", isLarge: true, m => m.Add("Sub action"));
});
ribbon.Tabs.Add(tab);

var toggleBtn = (ProRibbonButton)tab.Groups[0].Items[1];
toggleBtn.IsToggle = true;
var disabledBtn = (ProRibbonButton)tab.Groups[0].Items[3];
disabledBtn.IsEnabled = false;
var dropdownBtn = (ProRibbonButton)tab.Groups[0].Items[4];
dropdownBtn.DropdownMenu!.Opening += (s, e) => dropdownOpening = true;

var window = new Window { Width = 800, Height = 200, Content = ribbon };
window.Show();
Pump(window);

Point CenterOf(ProRibbonButton btn)
{
    foreach (var (item, rect) in ribbon.GetContentLayout())
        if (ReferenceEquals(item, btn))
            return rect.Center;
    throw new InvalidOperationException($"Bouton '{btn.Label}' introuvable dans le layout");
}

ClickAt(window, CenterOf((ProRibbonButton)tab.Groups[0].Items[0]));
Check("clic sur grand bouton déclenche OnClick", runClicks == 1);

ClickAt(window, CenterOf((ProRibbonButton)tab.Groups[0].Items[2]));
Check("clic sur petit bouton déclenche OnClick", copyClicks == 1);

ClickAt(window, CenterOf(toggleBtn));
Check("bouton toggle coché au clic", toggleBtn.IsChecked);
ClickAt(window, CenterOf(toggleBtn));
Check("bouton toggle décoché au second clic", !toggleBtn.IsChecked);

ClickAt(window, CenterOf(disabledBtn));
Check("bouton désactivé inerte", disabledClicks == 0);

ClickAt(window, CenterOf(dropdownBtn));
Check("bouton dropdown ouvre son menu (Opening)", dropdownOpening);
var dropdownItem = dropdownBtn.DropdownMenu.Items[0];
Check("le menu du dropdown est réellement rendu",
    dropdownItem.GetVisualRoot() != null && dropdownItem.Bounds.Width > 0);

window.Close();

// ═════════════════════════════════════════════════════════════════
// 2+3. MENUS — items partagés, sous-menus, fermeture en chaîne
// ═════════════════════════════════════════════════════════════════
Console.WriteLine("Menus :");

int newClicks = 0, subClicks = 0;
var newItem = new ProMenuItem("New", onClick: () => newClicks++);
var subActionItem = new ProMenuItem("Recent file A", onClick: () => subClicks++);
var recentItem = new ProMenuItem("Recent")
{
    Items = new System.Collections.ObjectModel.ObservableCollection<ProMenuItem> { subActionItem }
};
var gridItem = new ProMenuItem { Header = "Show Grid", IsCheckable = true, IsChecked = true };

var fileMenu = new ProMenuBarItem("File");
fileMenu.Items.Add(newItem);
fileMenu.Items.Add(recentItem);
fileMenu.Items.Add(gridItem);

var menuBar = new ProMenuBar();
menuBar.Items.Add(fileMenu);

var menuWindow = new Window { Width = 600, Height = 300, Content = menuBar };
menuWindow.Show();
Pump(menuWindow);

void OpenFileMenu()
{
    var p = fileMenu.TranslatePoint(new Point(fileMenu.Bounds.Width / 2, 14), menuWindow)!.Value;
    ClickAt(menuWindow, p);
}

OpenFileMenu();
var topPopup = newItem.OwnerPopup;
Check("le clic sur 'File' ouvre le popup", topPopup is { IsOpen: true });
Check("le popup affiche les items ORIGINAUX (pas de copie)",
    newItem.Parent is Panel panel && panel.Children.Contains(newItem)
    && panel.Children.Contains(gridItem));
Check("le contenu du popup est réellement rendu (layout non vide)",
    newItem.GetVisualRoot() != null && newItem.Bounds.Width > 0);

// Clic réel sur un item du popup → Click + fermeture du menu
ClickAt(menuWindow, WindowCenter(menuWindow, newItem));
Check("clic sur un item déclenche son handler", newClicks == 1);
Check("clic sur un item ferme le menu", topPopup is { IsOpen: false });

// Item cochable : l'état bascule sur l'instance unique et le menu se ferme
OpenFileMenu();
ClickAt(menuWindow, WindowCenter(menuWindow, gridItem));
Check("clic sur item cochable bascule IsChecked (instance partagée)", !gridItem.IsChecked);

// Sous-menu : le survol du parent l'ouvre, le clic dans le sous-menu
// déclenche le handler et ferme TOUTE la chaîne
OpenFileMenu();
menuWindow.MouseMove(WindowCenter(menuWindow, recentItem), RawInputModifiers.None);
Pump(menuWindow);
var subPopup = subActionItem.OwnerPopup;
Check("le survol ouvre le sous-menu", subPopup is { IsOpen: true });
Check("le sous-menu est chaîné à son parent", subPopup?.ParentPopup == newItem.OwnerPopup);
Check("le sous-menu est réellement rendu", subActionItem.GetVisualRoot() != null && subActionItem.Bounds.Width > 0);

ClickAt(menuWindow, WindowCenter(menuWindow, subActionItem));
Check("clic dans le sous-menu déclenche son handler", subClicks == 1);
Check("la fermeture remonte toute la chaîne de popups",
    subPopup is { IsOpen: false } && newItem.OwnerPopup is { IsOpen: false });

menuWindow.Close();

// ═════════════════════════════════════════════════════════════════
// 4. PROGRESS BAR — animation indéterminée
// ═════════════════════════════════════════════════════════════════
Console.WriteLine("ProgressBar :");

var progress = new ProProgressBar { IsIndeterminate = true, Width = 400 };
var pbWindow = new Window { Width = 500, Height = 100, Content = progress };
pbWindow.Show();
Pump(pbWindow);

var timerField = typeof(ProProgressBar).GetField("_animTimer",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
var timer = timerField.GetValue(progress) as DispatcherTimer;
Check("timer d'animation démarré quand indéterminée + attachée", timer is { IsEnabled: true });

var frame1 = pbWindow.CaptureRenderedFrame()!;
var pixels1 = FramePixels(frame1);
// Laisser le temps de l'horloge monotone s'écouler, puis laisser le timer
// (ou à défaut une invalidation explicite) déclencher un nouveau rendu
for (int i = 0; i < 5; i++)
{
    Thread.Sleep(50);
    Dispatcher.UIThread.RunJobs();
}
progress.InvalidateVisual();
Pump(pbWindow);
var frame2 = pbWindow.CaptureRenderedFrame()!;
var pixels2 = FramePixels(frame2);
Check("la barre indéterminée bouge entre deux rendus", !pixels1.SequenceEqual(pixels2));

progress.IsIndeterminate = false;
Pump(pbWindow);
timer = timerField.GetValue(progress) as DispatcherTimer;
Check("timer arrêté quand la barre repasse en mode déterminé", timer is { IsEnabled: false });

pbWindow.Close();

// ═════════════════════════════════════════════════════════════════
// 5. CLAVIER & ÉVÉNEMENTS PILOTÉS PAR LES PROPRIÉTÉS
// ═════════════════════════════════════════════════════════════════
Console.WriteLine("Clavier & événements :");

int btnClicks = 0;
var kbButton = new ProButton { Text = "OK" };
kbButton.Click += (s, e) => btnClicks++;

var kbCheck = new ProCheckBox { Label = "Option" };
var checkEvents = new List<bool?>();
kbCheck.CheckedChanged += (s, v) => checkEvents.Add(v);

var kbToggle = new ProToggleSwitch();
int toggleEvents = 0;
kbToggle.Toggled += (s, v) => toggleEvents++;

var kbSlider = new ProSlider { Minimum = 0, Maximum = 100, Step = 5, Value = 50 };
var sliderEvents = new List<double>();
kbSlider.ValueChanged += (s, v) => sliderEvents.Add(v);

var radioA = new ProRadioButton { Label = "A", IsChecked = true };
var radioB = new ProRadioButton { Label = "B" };
var radioC = new ProRadioButton { Label = "C" };

var kbPanel = new StackPanel();
kbPanel.Children.Add(kbButton);
kbPanel.Children.Add(kbCheck);
kbPanel.Children.Add(kbToggle);
kbPanel.Children.Add(kbSlider);
kbPanel.Children.Add(radioA);
kbPanel.Children.Add(radioB);
kbPanel.Children.Add(radioC);

var kbWindow = new Window { Width = 400, Height = 400, Content = kbPanel };
kbWindow.Show();
Pump(kbWindow);

void PressKey(PhysicalKey key)
{
    kbWindow.KeyPressQwerty(key, RawInputModifiers.None);
    kbWindow.KeyReleaseQwerty(key, RawInputModifiers.None);
    Pump(kbWindow);
}

kbButton.Focus();
PressKey(PhysicalKey.Space);
Check("Espace active ProButton", btnClicks == 1);
PressKey(PhysicalKey.Enter);
Check("Entrée active ProButton", btnClicks == 2);

kbCheck.Focus();
PressKey(PhysicalKey.Space);
Check("Espace coche ProCheckBox", kbCheck.IsChecked == true);
Check("CheckedChanged déclenché par le clavier", checkEvents.Count == 1 && checkEvents[0] == true);
kbCheck.IsChecked = false;
Check("CheckedChanged déclenché par set programmatique", checkEvents.Count == 2 && checkEvents[1] == false);

kbToggle.Focus();
PressKey(PhysicalKey.Space);
Check("Espace bascule ProToggleSwitch", kbToggle.IsOn);
kbToggle.IsOn = false;
Check("Toggled déclenché aussi par set programmatique", toggleEvents == 2);

kbSlider.Focus();
PressKey(PhysicalKey.ArrowRight);
Check("flèche droite incrémente le slider d'un Step", Math.Abs(kbSlider.Value - 55) < 0.001);
PressKey(PhysicalKey.PageDown);
Check("PageDown décrémente de 10 Steps", Math.Abs(kbSlider.Value - 5) < 0.001);
PressKey(PhysicalKey.Home);
Check("Home va au minimum", kbSlider.Value == 0);
PressKey(PhysicalKey.End);
Check("End va au maximum", kbSlider.Value == 100);
Check("ValueChanged suit chaque changement", sliderEvents.Count == 4);

radioA.Focus();
PressKey(PhysicalKey.ArrowDown);
Check("flèche bas coche le radio suivant et décoche l'actuel",
    radioB.IsChecked && !radioA.IsChecked);
radioC.IsChecked = true;
Check("exclusivité de groupe aussi en programmatique", !radioB.IsChecked && radioC.IsChecked);

kbWindow.Close();

Console.WriteLine(failed == 0 ? "Interaction ProControls : ALL PASS" : $"Interaction ProControls : {failed} FAILED");
return failed == 0 ? 0 : 1;

static byte[] FramePixels(global::Avalonia.Media.Imaging.WriteableBitmap bmp)
{
    using var fb = bmp.Lock();
    var size = fb.RowBytes * fb.Size.Height;
    var bytes = new byte[size];
    System.Runtime.InteropServices.Marshal.Copy(fb.Address, bytes, 0, size);
    return bytes;
}

class TestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }
}
