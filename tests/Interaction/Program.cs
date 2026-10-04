using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HDev.UI.Controls;
using HDev.UI.Controls.Documents;
using Interaction;

// Pilote les contrôles HDev.UI.Controls via de vraies entrées simulées (pipeline
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

var ribbon = new HDevRibbon();
var tab = new HDevRibbonTab("Home");
tab.AddGroup("Actions", g =>
{
    g.AddLargeButton("Run", "R", () => runClicks++);
    g.AddSmallButton("Bold", "B");
    g.AddSmallButton("Copy", "C", () => copyClicks++);
    g.AddSmallButton("Del", "D", () => disabledClicks++);
    g.AddDropdownButton("More", "M", isLarge: true, m => m.Add("Sub action"));
});
ribbon.Tabs.Add(tab);

var toggleBtn = (HDevRibbonButton)tab.Groups[0].Items[1];
toggleBtn.IsToggle = true;
var disabledBtn = (HDevRibbonButton)tab.Groups[0].Items[3];
disabledBtn.IsEnabled = false;
var dropdownBtn = (HDevRibbonButton)tab.Groups[0].Items[4];
dropdownBtn.DropdownMenu!.Opening += (s, e) => dropdownOpening = true;

var window = new Window { Width = 800, Height = 200, Content = ribbon };
window.Show();
Pump(window);

Point CenterOf(HDevRibbonButton btn)
{
    foreach (var (item, rect) in ribbon.GetContentLayout())
        if (ReferenceEquals(item, btn))
            return rect.Center;
    throw new InvalidOperationException($"Bouton '{btn.Label}' introuvable dans le layout");
}

ClickAt(window, CenterOf((HDevRibbonButton)tab.Groups[0].Items[0]));
Check("clic sur grand bouton déclenche OnClick", runClicks == 1);

ClickAt(window, CenterOf((HDevRibbonButton)tab.Groups[0].Items[2]));
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
    dropdownItem.IsAttachedToVisualTree() && dropdownItem.Bounds.Width > 0);

window.Close();

// ═════════════════════════════════════════════════════════════════
// 2+3. MENUS — items partagés, sous-menus, fermeture en chaîne
// ═════════════════════════════════════════════════════════════════
Console.WriteLine("Menus :");

int newClicks = 0, subClicks = 0;
var newItem = new HDevMenuItem("New", onClick: () => newClicks++);
var subActionItem = new HDevMenuItem("Recent file A", onClick: () => subClicks++);
var recentItem = new HDevMenuItem("Recent")
{
    Items = new System.Collections.ObjectModel.ObservableCollection<HDevMenuItem> { subActionItem }
};
var gridItem = new HDevMenuItem { Header = "Show Grid", IsCheckable = true, IsChecked = true };

var fileMenu = new HDevMenuBarItem("File");
fileMenu.Items.Add(newItem);
fileMenu.Items.Add(recentItem);
fileMenu.Items.Add(gridItem);

var menuBar = new HDevMenuBar();
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
    newItem.IsAttachedToVisualTree() && newItem.Bounds.Width > 0);

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
Check("le sous-menu est réellement rendu", subActionItem.IsAttachedToVisualTree() && subActionItem.Bounds.Width > 0);

ClickAt(menuWindow, WindowCenter(menuWindow, subActionItem));
Check("clic dans le sous-menu déclenche son handler", subClicks == 1);
Check("la fermeture remonte toute la chaîne de popups",
    subPopup is { IsOpen: false } && newItem.OwnerPopup is { IsOpen: false });

menuWindow.Close();

// ═════════════════════════════════════════════════════════════════
// 4. PROGRESS BAR — animation indéterminée
// ═════════════════════════════════════════════════════════════════
Console.WriteLine("ProgressBar :");

var progress = new HDevProgressBar { IsIndeterminate = true, Width = 400 };
var pbWindow = new Window { Width = 500, Height = 100, Content = progress };
pbWindow.Show();
Pump(pbWindow);

var timerField = typeof(HDevProgressBar).GetField("_animTimer",
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
var kbButton = new HDevButton { Text = "OK" };
kbButton.Click += (s, e) => btnClicks++;

var kbCheck = new HDevCheckBox { Label = "Option" };
var checkEvents = new List<bool?>();
kbCheck.CheckedChanged += (s, v) => checkEvents.Add(v);

var kbToggle = new HDevToggleSwitch();
int toggleEvents = 0;
kbToggle.Toggled += (s, v) => toggleEvents++;

var kbSlider = new HDevSlider { Minimum = 0, Maximum = 100, Step = 5, Value = 50 };
var sliderEvents = new List<double>();
kbSlider.ValueChanged += (s, v) => sliderEvents.Add(v);

var radioA = new HDevRadioButton { Label = "A", IsChecked = true };
var radioB = new HDevRadioButton { Label = "B" };
var radioC = new HDevRadioButton { Label = "C" };

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
Check("Espace active HDevButton", btnClicks == 1);
PressKey(PhysicalKey.Enter);
Check("Entrée active HDevButton", btnClicks == 2);

kbCheck.Focus();
PressKey(PhysicalKey.Space);
Check("Espace coche HDevCheckBox", kbCheck.IsChecked == true);
Check("CheckedChanged déclenché par le clavier", checkEvents.Count == 1 && checkEvents[0] == true);
kbCheck.IsChecked = false;
Check("CheckedChanged déclenché par set programmatique", checkEvents.Count == 2 && checkEvents[1] == false);

kbToggle.Focus();
PressKey(PhysicalKey.Space);
Check("Espace bascule HDevToggleSwitch", kbToggle.IsOn);
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

// ═════════════════════════════════════════════════════════════════
// N. PROROSTER — survol, clic, glisser, annulation, voie cible
// ═════════════════════════════════════════════════════════════════
Console.WriteLine("HDevRoster :");

var jour = new DateTime(2026, 9, 14);   // lundi
var rosterModel = new RosterModel { Origin = jour };
foreach (var l in RosterEngine.BuildDailyLanes(jour, 5, d => d.ToString("ddd dd/MM")))
    rosterModel.Lanes.Add(l);

var vol0 = new RosterBlock(jour.AddHours(9), jour.AddHours(11), "TY201");
rosterModel.Lanes[0].Bands.Add(new RosterBand(jour.AddHours(5), jour.AddHours(15), "AM"));
rosterModel.Lanes[0].Blocks.Add(vol0);

var roster = new HDevRoster { Model = rosterModel };

var rosterWindow = new Window { Width = 1000, Height = 400, Content = roster };
rosterWindow.Show();
Pump(rosterWindow);

// Geometrie attendue : gouttiere 120, en-tete 26, voie 40, 60 px/heure.
//   09:00 -> x = 120 + 9*60 = 660 ; voie 0 -> y du bloc ~ 41
Point OnBlock() => new(700, 41);
Point OnLane1() => new(700, 81);

int blockClicks = 0, laneClicks = 0, summaries = 0, moving = 0, moved = 0;
roster.BlockClicked += (_, _) => blockClicks++;
roster.LaneClicked += (_, _) => laneClicks++;
roster.LaneSummaryRequested += (_, _) => summaries++;
roster.BlockMoving += (_, _) => moving++;
roster.BlockMoved += (_, _) => moved++;

// Le hit-test ne depend PAS d'un rendu prealable : la frise d'origine remplissait
// ses regions pendant Render et ne repondait a rien avant la premiere peinture.
ClickAt(rosterWindow, OnBlock());
Check("clic sur un bloc : BlockClicked", blockClicks == 1);
Check("clic sur un bloc : selection posee", ReferenceEquals(roster.SelectedBlock, vol0));

BreakClickChain();
ClickAt(rosterWindow, new Point(300, 41));
Check("clic hors bloc : LaneClicked", laneClicks == 1);

BreakClickChain();
rosterWindow.MouseMove(OnBlock(), RawInputModifiers.None);
rosterWindow.MouseDown(OnBlock(), MouseButton.Left, RawInputModifiers.Shift);
rosterWindow.MouseUp(OnBlock(), MouseButton.Left, RawInputModifiers.Shift);
Pump(rosterWindow);
Check("Maj + clic : LaneSummaryRequested", summaries == 1);

// Deux appuis rapproches au MEME endroit sont un double-clic — comportement voulu.
// Le harnais doit donc rompre la chaine de clics entre deux gestes, sinon le second
// appui part dans la branche double-clic et n'arme jamais le glisser.
void BreakClickChain() => System.Threading.Thread.Sleep(600);

int doubleClicks = 0;
roster.BlockDoubleClicked += (_, _) => doubleClicks++;
BreakClickChain();
ClickAt(rosterWindow, OnBlock());
rosterWindow.MouseDown(OnBlock(), MouseButton.Left, RawInputModifiers.None);
rosterWindow.MouseUp(OnBlock(), MouseButton.Left, RawInputModifiers.None);
Pump(rosterWindow);
Check("double-clic sur un bloc : BlockDoubleClicked", doubleClicks == 1);

// Glisser d'une heure vers la droite (60 px), aimante a 5 min.
void Drag(Point from, Point to, RawInputModifiers mods = RawInputModifiers.None)
{
    BreakClickChain();
    rosterWindow.MouseMove(from, RawInputModifiers.None);
    rosterWindow.MouseDown(from, MouseButton.Left, mods);
    rosterWindow.MouseMove(new Point((from.X + to.X) / 2, (from.Y + to.Y) / 2), RawInputModifiers.LeftMouseButton);
    rosterWindow.MouseMove(to, RawInputModifiers.LeftMouseButton);
    rosterWindow.MouseUp(to, MouseButton.Left, mods);
    Pump(rosterWindow);
}

var clicsAvantGlisser = blockClicks;
Drag(OnBlock(), new Point(760, 41));
Check("glisser : BlockMoving puis BlockMoved", moving == 1 && moved == 1);
Check("glisser : le bloc a avance d'une heure", vol0.Start == jour.AddHours(10) && vol0.End == jour.AddHours(12));
Check("glisser : la duree est preservee", vol0.End - vol0.Start == TimeSpan.FromHours(2));
Check("glisser : pas de clic parasite", blockClicks == clicsAvantGlisser);

// Annulation : le metier refuse, le modele ne bouge pas.
var avant = vol0.Start;
EventHandler<RosterBlockMoveEventArgs> refuse = (_, e) => e.Cancel = true;
roster.BlockMoving += refuse;
Drag(new Point(760, 41), new Point(880, 41));
Check("BlockMoving annulable : le bloc n'a pas bouge", vol0.Start == avant);
Check("BlockMoving annule : pas de BlockMoved", moved == 1);
roster.BlockMoving -= refuse;

// Correction par le metier : le gestionnaire reecrit les dates proposees.
EventHandler<RosterBlockMoveEventArgs> recale = (_, e) => e.NewStart = e.NewStart.Date.AddHours(6);
roster.BlockMoving += recale;
Drag(new Point(760, 41), new Point(820, 41));
Check("BlockMoving : le metier peut corriger la date proposee", vol0.Start.Hour == 6);
roster.BlockMoving -= recale;

// Voie cible : le geste qui distingue une frise multi-voies d'un agenda.
vol0.Start = jour.AddHours(9);
vol0.End = jour.AddHours(11);
rosterModel.Touch();
Pump(rosterWindow);
Drag(OnBlock(), OnLane1());
Check("glisser d'une voie a l'autre : le bloc a change de voie",
    rosterModel.Lanes[1].Blocks.Contains(vol0) && !rosterModel.Lanes[0].Blocks.Contains(vol0));
Check("glisser d'une voie a l'autre : la date suit la voie cible",
    vol0.Start.Date == rosterModel.Lanes[1].WindowStart.Date);

// Lecture seule : on selectionne, on ne deplace pas.
var posee = vol0.Start;
roster.IsReadOnly = true;
Drag(new Point(700, 81), new Point(880, 81));
Check("IsReadOnly : le glisser est inerte", vol0.Start == posee);
roster.IsReadOnly = false;

rosterWindow.Close();

// ═══ Glisser-déposer applicatif HDevListView → HDevTreeView (côté dépôt) ═══
// Le départ du drag (DoDragDropAsync) est du ressort de l'OS ; le dépôt se rejoue
// en headless avec le même DataTransfer in-process que construit HDevListView.
Console.WriteLine("Glisser-déposer liste → arbre");
var dropTree = new HDevTreeView { AllowDropItems = true };
dropTree.Add("Boîte de réception");
var archives = dropTree.Add("Archives");
(HDevTreeNode Node, object Item)? dropped = null;
dropTree.ItemDropped += (_, d) => dropped = d;
var treeWindow = new Window { Width = 300, Height = 200, Content = dropTree };
treeWindow.Show();
Pump(treeWindow);

object mail = "mail #42";
var itemDrag = new DataTransfer();
itemDrag.Add(DataTransferItem.Create(HDevListView.DragDataFormat, mail));
var archivesPt = dropTree.TranslatePoint(new Point(60, 26 + 13), treeWindow) ?? new Point();
treeWindow.DragDrop(archivesPt, RawDragEventType.DragEnter, itemDrag, DragDropEffects.Move, RawInputModifiers.None);
treeWindow.DragDrop(archivesPt, RawDragEventType.DragOver, itemDrag, DragDropEffects.Move, RawInputModifiers.None);
treeWindow.DragDrop(archivesPt, RawDragEventType.Drop, itemDrag, DragDropEffects.Move, RawInputModifiers.None);
Pump(treeWindow);
Check("dépôt d'un item de liste sur un nœud : ItemDropped levé", dropped != null);
Check("le dépôt vise le nœud survolé (Archives) et porte l'item glissé",
    dropped is { } d1 && ReferenceEquals(d1.Node, archives) && ReferenceEquals(d1.Item, mail));

dropTree.AllowDropItems = false;
dropped = null;
treeWindow.DragDrop(archivesPt, RawDragEventType.DragEnter, itemDrag, DragDropEffects.Move, RawInputModifiers.None);
treeWindow.DragDrop(archivesPt, RawDragEventType.DragOver, itemDrag, DragDropEffects.Move, RawInputModifiers.None);
treeWindow.DragDrop(archivesPt, RawDragEventType.Drop, itemDrag, DragDropEffects.Move, RawInputModifiers.None);
Pump(treeWindow);
Check("AllowDropItems=false : le dépôt est refusé", dropped == null);
treeWindow.Close();

// ═══ HDevRichEdit : copier/coller riche (texte + text/html via DataTransfer) ═══
Console.WriteLine("HDevRichEdit copier/coller");
var srcEdit = new HDevRichEdit { Html = "<p><strong>Gras</strong> et normal</p>", Height = 120 };
var dstEdit = new HDevRichEdit { Height = 120 };
var editWindow = new Window { Width = 500, Height = 300, Content = new StackPanel { Children = { srcEdit, dstEdit } } };
editWindow.Show();
Pump(editWindow);

srcEdit.Focus();
srcEdit.SelectAll();
editWindow.KeyPress(Key.C, RawInputModifiers.Control, PhysicalKey.C, "c");
Pump(editWindow);
dstEdit.Focus();
editWindow.KeyPress(Key.V, RawInputModifiers.Control, PhysicalKey.V, "v");
Pump(editWindow);
Check($"coller restitue le contenu riche (html={dstEdit.Html})",
    dstEdit.Html.Contains("<strong>Gras</strong>") && dstEdit.Html.Contains("et normal"));
editWindow.Close();

// ═══ Design P0 : mise en page, focus clavier, bascule de thème, toast ═══
Console.WriteLine("Design P0 :");

// Rangée mixte : chaque contrôle garde sa hauteur et se centre (rien ne s'étire)
var small = new HDevButton { Text = "Small", Size = ButtonSize.Small };
var medium = new HDevButton { Text = "Medium" };
var large = new HDevButton { Text = "Large", Size = ButtonSize.Large };
var iconButton = new HDevIconButton { Icon = HDevEditorIcon.ChevronRight };
var field = new HDevTextBox { Width = 120, Text = "TY201" };
var rowPanel = new StackPanel
{
    Orientation = Avalonia.Layout.Orientation.Horizontal,
    Children = { small, medium, large, iconButton, field }
};
var rowWindow = new Window { Width = 800, Height = 120, Content = rowPanel };
rowWindow.Show();
Pump(rowWindow);
Check($"tailles dans une rangée commune : 28/34/42 (lu {small.Bounds.Height}/{medium.Bounds.Height}/{large.Bounds.Height})",
    small.Bounds.Height == 28 && medium.Bounds.Height == 34 && large.Bounds.Height == 42);
Check("les boutons d'une rangée partagent la même ligne médiane",
    Math.Abs(small.Bounds.Center.Y - large.Bounds.Center.Y) < 0.5);
Check($"bouton-icône carré dans une rangée plus haute (lu {iconButton.Bounds.Width}×{iconButton.Bounds.Height})",
    iconButton.Bounds.Width == 34 && iconButton.Bounds.Height == 34);
Check($"champ mono-ligne à EditorHeight dans une rangée plus haute (lu {field.Bounds.Height})",
    field.Bounds.Height == 28);

// Focus visible au clavier seulement
var focusVisible = typeof(HDevControlBase).GetProperty("IsFocusVisible",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
ClickAt(rowWindow, WindowCenter(rowWindow, small));
Check("focus pris au clic : pas d'anneau", small.IsFocused && !(bool)focusVisible.GetValue(small)!);
rowWindow.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
rowWindow.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
Pump(rowWindow);
Check("focus pris par Tab : anneau visible", medium.IsFocused && (bool)focusVisible.GetValue(medium)!);
rowWindow.Close();

// Bascule à chaud dans une HDevWindow : barre de menu et zone de contenu suivent
var themedMenu = new HDevMenuBar();
themedMenu.Items.Add(new HDevMenuBarItem("Fichier"));
var themedWindow = new HDevWindow { Width = 500, Height = 200, Content = themedMenu };
themedWindow.Show();
Pump(themedWindow);
HDev.UI.Controls.Theme.HDevTheme.Variant = HDev.UI.Controls.Theme.HDevThemeVariant.Dark;
Pump(themedWindow);
var contentArea = (Border)typeof(HDevWindow).GetField("_contentArea",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(themedWindow)!;
Check("bascule sombre : la barre de menu prend le fond sombre",
    themedMenu.Background is Avalonia.Media.ISolidColorBrush menuBrush
    && menuBrush.Color == HDev.UI.Controls.Theme.HDevTheme.Background.Toolbar);
Check("bascule sombre : la zone de contenu de HDevWindow suit",
    contentArea.Background is Avalonia.Media.ISolidColorBrush areaBrush
    && areaBrush.Color == HDev.UI.Controls.Theme.HDevTheme.Background.Window);
HDev.UI.Controls.Theme.HDevTheme.Variant = HDev.UI.Controls.Theme.HDevThemeVariant.Light;

// Toast : ancré en bas à droite de la fenêtre (l'OverlayLayer ignore l'alignement)
HDevToast.Show(themedWindow, "Titre", "Message");
Pump(themedWindow);
var overlay = Avalonia.Controls.Primitives.OverlayLayer.GetOverlayLayer(themedWindow)!;
var toast = overlay.Children[^1];
Check($"toast en bas à droite (bord droit {toast.Bounds.Right:0} / {overlay.Bounds.Width:0}, bas {toast.Bounds.Bottom:0} / {overlay.Bounds.Height:0})",
    Math.Abs(toast.Bounds.Right - (overlay.Bounds.Width - 16)) < 1
    && Math.Abs(toast.Bounds.Bottom - (overlay.Bounds.Height - 16)) < 1);
themedWindow.Close();

// Combo éditable : une valeur posée par code n'ouvre pas les suggestions
var editableCombo = new HDevComboBox { Width = 160 };
editableCombo.Properties.Items.AddRange(new object[] { "Basse", "Normale", "Haute" });
editableCombo.Properties.TextEditStyle = TextEditStyles.Standard;
var comboWindow = new Window { Width = 300, Height = 200, Content = editableCombo };
comboWindow.Show();
Pump(comboWindow);
editableCombo.SelectedIndex = 1;
Pump(comboWindow);
Check("combo éditable : SelectedIndex par code n'ouvre pas les suggestions", !editableCombo.IsPopupOpen);
comboWindow.Close();

// Liste : GroupSelector posé après ItemsSource est pris en compte
var groupedList = new HDevListView
{
    ItemsSource = new[] { "a1", "a2", "b1" },
    GroupSelector = item => ((string)item)[..1]
};
var rows = (System.Collections.IList)typeof(HDevListView).GetField("_rows",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(groupedList)!;
var headers = rows.Cast<object>().Count(r => (bool)r.GetType().GetProperty("IsHeader")!.GetValue(r)!);
Check($"liste : GroupSelector après ItemsSource produit les en-têtes de groupe (lu {headers})", headers == 2);

// ═════════════════════════════════════════════════════════════════
// ÉDITEURS : caractérisation E0 (Plan-Editeurs.md), avant la refonte du socle
// ═════════════════════════════════════════════════════════════════
// Comportements observés ET plausiblement voulus. Les écarts surprenants sont listés
// dans Plan-Editeurs.md (constats E0) au lieu d'être gravés ici.
// Les adaptateurs (« Adaptateurs E0 » ci-dessous) sont le seul endroit qui lit ou écrit
// la valeur, le texte, les événements, le focus et les popups des éditeurs, et le seul
// qui touche à leurs champs internes. La configuration (bornes, sources, options) reste
// écrite en clair : si l'API la renomme, la correction est mécanique. Quand l'API change
// (lots E1 à E3), on corrige l'adaptateur et on consigne la rupture dans
// MIGRATION-editeurs.md ; une attente qui change est un changement de COMPORTEMENT,
// à justifier.
// Non testable en headless : la fermeture débouncée sur désactivation de fenêtre
// (aucune API pour désactiver une fenêtre headless).
Console.WriteLine("Éditeurs (caractérisation E0) :");
{
    var cultureAvant = System.Globalization.CultureInfo.CurrentCulture;
    var cultureUiAvant = System.Globalization.CultureInfo.CurrentUICulture;
    System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
    System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("fr-FR");

    // ─── Outils d'entrée et de géométrie (indépendants de l'API des éditeurs) ───
    Window Hote(Control c, double top = 20)
    {
        Canvas.SetLeft(c, 20); Canvas.SetTop(c, top);
        var win = new Window { Width = 500, Height = 420, Content = new Canvas { Children = { c } } };
        win.Show(); Pump(win); return win;
    }
    void Touche(Window w, PhysicalKey k, RawInputModifiers m = RawInputModifiers.None)
    { w.KeyPressQwerty(k, m); w.KeyReleaseQwerty(k, m); Pump(w); }
    void Tape(Window w, string texte) { w.KeyTextInput(texte); Pump(w); }
    void TapeParCaractere(Window w, string texte) { foreach (var c in texte) Tape(w, c.ToString()); }
    void Colle(Window w, string texte)
    {
        Avalonia.Input.Platform.ClipboardExtensions.SetTextAsync(TopLevel.GetTopLevel(w)!.Clipboard!, texte).GetAwaiter().GetResult();
        Touche(w, PhysicalKey.V, RawInputModifiers.Control);
    }
    Rect Boite(Window w, Control c) => new(c.TranslatePoint(new Point(0, 0), w)!.Value, c.Bounds.Size);
    Rect? BoitePopup(Window w, Editeur e) => e.ContenuPopup() is { } c && c.TranslatePoint(new Point(0, 0), w) is { } p
        ? new Rect(p, c.Bounds.Size) : null;
    var horsChamp = new Point(480, 400);
    // Accolé = à 2 px du bord du champ. La fusion des bordures (-1 px, HDevComboBox) et le jour
    // de 2 px (HDevDateEdit) sont tous deux voulus ; aligné à gauche quand il s'ouvre dessous.
    bool Dessous(Rect? pop, Rect champ) => pop is { } r && Math.Abs(r.Top - champ.Bottom) <= 2 && Math.Abs(r.Left - champ.Left) <= 1;
    bool Dessus(Rect? pop, Rect champ) => pop is { } r && r.Top < champ.Top && Math.Abs(r.Bottom - champ.Top) <= 2;

    // ─── Adaptateurs E0 (périmètre : voir l'en-tête de la section) ───
    T Champ<T>(object o, string nom)
    {
        for (var t = o.GetType(); t != null; t = t.BaseType)
            if (t.GetField(nom, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) is { } f)
                return (T)f.GetValue(o)!;
        throw new InvalidOperationException($"champ {nom} introuvable sur {o.GetType().Name}");
    }
    Control? EnfantPopup(object editeur)
    {
        var popup = Champ<Avalonia.Controls.Primitives.Popup>(editeur, "_popup");
        return popup.IsOpen ? popup.Child as Control : null;
    }
    Editeur AdTextBox(HDevTextBox e) => new()
    {
        Controle = e, PoserValeur = v => e.Text = (string?)v, LireValeur = () => e.Text, LireTexte = () => e.Text ?? "",
        SurValeurChangee = a => e.TextChanged += (_, _) => a(), Focaliser = () => e.FocusTextBox()
    };
    TextBox InterieurTextBox(HDevTextBox e) => Champ<TextBox>(e, "_innerTextBox");
    Editeur AdDateEdit(HDevDateEdit e) => new()
    {
        Controle = e, PoserValeur = v => e.Value = (DateTime?)v, LireValeur = () => e.Value, LireTexte = () => e.Text,
        SurValeurChangee = a => e.ValueChanged += (_, _) => a(), Focaliser = () => { e.FocusEditor(); e.SelectAll(); },
        PopupOuvert = () => Champ<bool>(e, "_isPopupOpen"), ContenuPopup = () => EnfantPopup(e),
        Valider = () => e.Validate(), Erreur = () => e.ErrorText
    };
    void ChoisirJourDuCalendrier(HDevDateEdit e, DateTime jour) => Champ<HDevMonthCalendar>(e, "_calendar").SelectedDate = jour;
    Editeur AdTimeEdit(HDevTimeEdit e) => new()
    {
        Controle = e, PoserValeur = v => e.Value = (TimeSpan?)v, LireValeur = () => e.Value, LireTexte = () => e.Text,
        SurValeurChangee = a => e.ValueChanged += (_, _) => a(), Focaliser = () => { e.FocusEditor(); e.SelectAll(); },
        Valider = () => e.Validate(), Erreur = () => e.ErrorText
    };
    Editeur AdSpinEdit(HDevSpinEdit e) => new()
    {
        Controle = e, PoserValeur = v => e.Value = (decimal)v!, LireValeur = () => e.Value, LireTexte = () => e.Text,
        SurValeurChangee = a => e.ValueChanged += (_, _) => a(), Focaliser = () => { e.FocusEditor(); e.SelectAll(); }
    };
    Editeur AdComboBox(HDevComboBox e) => new()
    {
        Controle = e, PoserValeur = v => e.SelectedIndex = (int)v!, LireValeur = () => e.EditValue, LireTexte = () => e.Text,
        SurValeurChangee = a => e.EditValueChanged += (_, _) => a(), Focaliser = () => e.Focus(),
        PopupOuvert = () => e.IsPopupOpen, ContenuPopup = () => EnfantPopup(e)
    };
    void ComboItems(HDevComboBox e, params object[] items) => e.Properties.Items.AddRange(items);
    void ComboEditable(HDevComboBox e) => e.Properties.TextEditStyle = TextEditStyles.Standard;
    int ComboIndex(HDevComboBox e) => e.SelectedIndex;
    void SurChoixUtilisateur(HDevComboBox e, Action a) => e.SelectedIndexChanged += (_, _) => a();
    Control? ComboItem(HDevComboBox e, int i) => Champ<ListBox>(e, "_listBox").ContainerFromIndex(i) as Control;
    void FocaliserSaisieCombo(HDevComboBox e) => Champ<TextBox>(e, "_editTextBox").Focus();
    void ToutSelectionnerCombo(HDevComboBox e) => Champ<TextBox>(e, "_editTextBox").SelectAll();
    Editeur AdLookUpEdit(HDevLookUpEdit e) => new()
    {
        Controle = e, PoserValeur = v => e.EditValue = v, LireValeur = () => e.EditValue,
        LireTexte = () => (string)typeof(HDevLookUpEdit).GetMethod("GetDisplayText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(e, null)!,
        SurValeurChangee = a => e.EditValueChanged += (_, _) => a(), Focaliser = () => e.Focus(),
        PopupOuvert = () => Champ<bool>(e, "_isDropDownOpen"), ContenuPopup = () => EnfantPopup(e)
    };
    void ChoisirLigne(HDevLookUpEdit e, object ligne) => Champ<HDev.UI.DataGrid.HDevDataGrid>(e, "_grid").SelectedItem = ligne;
    Editeur AdSearch(HDevSearchControl e) => new()
    {
        Controle = e, LireTexte = () => e.Text, Focaliser = () => { e.FocusEditor(); e.SelectAll(); },
        PopupOuvert = () => Champ<bool>(e, "_isOpen"), ContenuPopup = () => EnfantPopup(e)
    };
    int NbSuggestions(HDevSearchControl e) => Champ<ListBox>(e, "_listBox").ItemCount;
    void ViderSaisieJeton(HDevTokenEdit e) => Champ<TextBox>(e, "_input").Text = "";
    Editeur AdTokenEdit(HDevTokenEdit e) => new()
    {
        Controle = e, LireValeur = () => e.Tokens.ToList(), LireTexte = () => Champ<TextBox>(e, "_input").Text ?? "",
        SurValeurChangee = a => e.TokensChanged += (_, _) => a(), Focaliser = () => Champ<TextBox>(e, "_input").Focus(),
        PopupOuvert = () => Champ<bool>(e, "_isOpen"), ContenuPopup = () => EnfantPopup(e)
    };
    Editeur AdCalendrier(HDevMonthCalendar e) => new()
    {
        Controle = e, PoserValeur = v => e.SelectedDate = (DateTime?)v, LireValeur = () => e.SelectedDate,
        SurValeurChangee = a => e.SelectedDateChanged += (_, _) => a(), Focaliser = () => e.Focus()
    };

    // ─── HDevTextBox ───
    {
        var tb = new HDevTextBox { Width = 200 }; var ed = AdTextBox(tb); int n = 0; ed.SurValeurChangee(() => n++);
        var w = Hote(tb);
        ed.PoserValeur("abc"); Pump(w);
        Check("TextBox : texte posé par code affiché, TextChanged une fois", InterieurTextBox(tb).Text == "abc" && n == 1);
        var n0 = n; ed.Focaliser(); InterieurTextBox(tb).CaretIndex = 3; Tape(w, "xy");
        Check("TextBox : la frappe s'ajoute au texte et relève TextChanged", ed.LireTexte() == "abcxy" && n == n0 + 1);
        tb.IsPassword = true; Pump(w);
        Check("TextBox : mot de passe masqué", InterieurTextBox(tb).PasswordChar != '\0');
        tb.IsPassword = false; ed.PoserValeur(""); tb.Placeholder = "Nom"; Pump(w);
        Check("TextBox : invite affichée quand le champ est vide", InterieurTextBox(tb).PlaceholderText == "Nom");
        tb.IsMultiline = true; ed.PoserValeur("1\n2\n3\n4"); Pump(w);
        Check($"TextBox : en multiligne, le champ grandit au-delà de 28 px (lu {tb.Bounds.Height})", tb.Bounds.Height > 28);
        w.Close();
    }

    // ─── HDevDateEdit ───
    {
        var de = new HDevDateEdit { Width = 160 }; var ed = AdDateEdit(de); int n = 0; ed.SurValeurChangee(() => n++);
        var w = Hote(de);
        ed.PoserValeur(new DateTime(2026, 9, 14)); Pump(w);
        Check("DateEdit : valeur posée par code affichée jj/mm/aaaa, ValueChanged une fois", ed.LireTexte() == "14/09/2026" && n == 1);
        var n0 = n; ed.PoserValeur(new DateTime(2026, 9, 14)); Pump(w);
        Check("DateEdit : reposer la même valeur ne relève rien", n == n0);
        n0 = n; ed.Focaliser(); Tape(w, "15092026");
        Check("DateEdit : la frappe sous masque donne la date (littéraux insérés)",
            Equals(ed.LireValeur(), new DateTime(2026, 9, 15)) && ed.LireTexte() == "15/09/2026" && n == n0 + 1);
        ed.Focaliser(); Tape(w, "1509");
        Check("DateEdit : une saisie incomplète est refusée à la validation", !ed.Valider() && ed.Erreur() != null);
        n0 = n; ed.Focaliser(); Colle(w, "20/09/2026");
        Check("DateEdit : coller une date au format du masque la pose",
            Equals(ed.LireValeur(), new DateTime(2026, 9, 20)) && ed.LireTexte() == "20/09/2026" && n == n0 + 1);
        // Curseur placé par une frappe caractère par caractère. Pas par Fin : après un collage,
        // Avalonia 12.1.3 envoie Fin en 0 (reproduit sur un TextBox nu). Pas par une frappe en bloc :
        // après un collage, elle laisse la sélection active (constat E0 n° 7, Plan-Editeurs.md).
        ed.Focaliser(); TapeParCaractere(w, "14092026");
        for (var i = 0; i < 5; i++) Touche(w, PhysicalKey.Backspace);
        Check("DateEdit : Retour arrière tronque au dernier emplacement de saisie, séparateur compris", ed.LireTexte() == "14/0");
        ed.PoserValeur(new DateTime(2026, 9, 1)); ed.PoserValeur(new DateTime(2026, 9, 14)); Pump(w);
        Touche(w, PhysicalKey.F4);
        var ouvert = ed.PopupOuvert(); var dessous = Dessous(BoitePopup(w, ed), Boite(w, de));
        Touche(w, PhysicalKey.Escape);
        Check("DateEdit : F4 ouvre le calendrier sous le champ, Échap le ferme", ouvert && dessous && !ed.PopupOuvert());
        Touche(w, PhysicalKey.ArrowDown, RawInputModifiers.Alt); var ouvertAlt = ed.PopupOuvert();
        var nAvant = n; ChoisirJourDuCalendrier(de, new DateTime(2026, 9, 20)); Pump(w);
        Check("DateEdit : Alt+↓ ouvre ; choisir un jour pose la valeur et ferme",
            ouvertAlt && Equals(ed.LireValeur(), new DateTime(2026, 9, 20)) && !ed.PopupOuvert() && n == nAvant + 1);
        Touche(w, PhysicalKey.F4); ClickAt(w, horsChamp);
        Check("DateEdit : un clic extérieur ferme le calendrier", !ed.PopupOuvert());
        de.MinDate = new DateTime(2026, 1, 1); ed.Focaliser(); Tape(w, "01012020");
        Check("DateEdit : une date avant MinDate est refusée, la valeur reste",
            !ed.Valider() && ed.Erreur()!.Contains("antérieure") && Equals(ed.LireValeur(), new DateTime(2026, 9, 20)));
        var nVidage = n; ed.Focaliser(); Touche(w, PhysicalKey.Delete);
        Check("DateEdit : tout sélectionner puis Suppr vide le champ ; valeur nulle, valide",
            ed.LireTexte() == "" && ed.LireValeur() == null && n == nVidage + 1 && ed.Valider());
        w.Close();
        var bas = new HDevDateEdit { Width = 160 }; var edBas = AdDateEdit(bas); var wBas = Hote(bas, top: 380);
        edBas.Focaliser(); Touche(wBas, PhysicalKey.F4);
        Check("DateEdit : près du bas de la fenêtre, le calendrier s'ouvre au-dessus",
            Dessus(BoitePopup(wBas, edBas), Boite(wBas, bas)));
        wBas.Close();
    }

    // ─── HDevTimeEdit ───
    {
        var te = new HDevTimeEdit { Width = 120 }; var ed = AdTimeEdit(te); int n = 0; ed.SurValeurChangee(() => n++);
        var w = Hote(te);
        ed.PoserValeur(new TimeSpan(8, 30, 0)); Pump(w);
        Check("TimeEdit : valeur posée affichée hh:mm, ValueChanged une fois", ed.LireTexte() == "08:30" && n == 1);
        var n0 = n; ed.Focaliser(); Tape(w, "0915");
        Check("TimeEdit : la frappe sous masque donne l'heure", Equals(ed.LireValeur(), new TimeSpan(9, 15, 0)) && n == n0 + 1);
        Touche(w, PhysicalKey.ArrowUp); var plusMinute = ed.LireValeur();
        Touche(w, PhysicalKey.PageUp); var plusHeure = ed.LireValeur();
        Touche(w, PhysicalKey.ArrowDown); Touche(w, PhysicalKey.PageDown);
        Check("TimeEdit : ↑ +1 min, Pg préc. +1 h, ↓ et Pg suiv. reviennent",
            Equals(plusMinute, new TimeSpan(9, 16, 0)) && Equals(plusHeure, new TimeSpan(10, 16, 0))
            && Equals(ed.LireValeur(), new TimeSpan(9, 15, 0)));
        ed.Focaliser(); Tape(w, "2599");
        Check("TimeEdit : 25:99 est refusé, la valeur précédente reste",
            !ed.Valider() && ed.Erreur() != null && Equals(ed.LireValeur(), new TimeSpan(9, 15, 0)));
        ed.Focaliser(); Touche(w, PhysicalKey.Delete);
        Check("TimeEdit : tout sélectionner puis Suppr vide le champ ; valeur nulle", ed.LireTexte() == "" && ed.LireValeur() == null);
        w.Close();
    }

    // ─── HDevSpinEdit ───
    {
        var sp = new HDevSpinEdit { Width = 120, Decimals = 2, Increment = 0.5m, MinValue = 0, MaxValue = 100 };
        var ed = AdSpinEdit(sp); int n = 0; ed.SurValeurChangee(() => n++);
        var autre = new HDevTextBox { Width = 80 }; Canvas.SetTop(autre, 200);
        var w = Hote(sp); ((Canvas)w.Content!).Children.Add(autre); Pump(w);
        ed.PoserValeur(12.5m); Pump(w);
        Check("SpinEdit : valeur affichée avec ses décimales (fr-FR), ValueChanged une fois", ed.LireTexte() == "12,50" && n == 1);
        ed.Focaliser(); Touche(w, PhysicalKey.ArrowUp); var monte = ed.LireValeur();
        Touche(w, PhysicalKey.ArrowDown); Touche(w, PhysicalKey.ArrowDown);
        Check("SpinEdit : ↑ et ↓ avancent d'un pas", Equals(monte, 13.0m) && Equals(ed.LireValeur(), 12.0m));
        ed.PoserValeur(150m); Pump(w);
        Check("SpinEdit : une valeur hors bornes est ramenée à la borne", Equals(ed.LireValeur(), 100m) && ed.LireTexte() == "100,00");
        ed.PoserValeur(10m); Pump(w); ed.Focaliser(); TapeParCaractere(w, "4a2,7");
        var pendant = ed.LireTexte(); AdTextBox(autre).Focaliser(); Pump(w);
        Check("SpinEdit : la frappe filtre les lettres, la perte de focus formate",
            pendant == "42,7" && ed.LireTexte() == "42,70" && Equals(ed.LireValeur(), 42.7m));
        ed.Focaliser(); w.MouseWheel(WindowCenter(w, sp), new Vector(0, 1), RawInputModifiers.None); Pump(w);
        Check("SpinEdit : la molette sur le champ focalisé avance d'un pas", Equals(ed.LireValeur(), 43.2m));
        w.Close();
    }

    // ─── HDevComboBox ───
    {
        var cb = new HDevComboBox { Width = 160 }; var ed = AdComboBox(cb); int nVal = 0, nChoix = 0;
        ed.SurValeurChangee(() => nVal++); SurChoixUtilisateur(cb, () => nChoix++);
        ComboItems(cb, "Basse", "Normale", "Haute");
        var w = Hote(cb);
        ed.PoserValeur(1); Pump(w);
        Check("ComboBox : index posé par code affiché, liste fermée, EditValueChanged une fois",
            ed.LireTexte() == "Normale" && Equals(ed.LireValeur(), "Normale") && nVal == 1 && !ed.PopupOuvert());
        ClickAt(w, WindowCenter(w, cb));
        Check("ComboBox : un clic ouvre la liste sous le champ",
            ed.PopupOuvert() && Dessous(BoitePopup(w, ed), Boite(w, cb)));
        var c0 = nChoix; var v0 = nVal; ClickAt(w, WindowCenter(w, ComboItem(cb, 2)!));
        Check("ComboBox : cliquer un item le choisit et ferme ; SelectedIndexChanged et EditValueChanged",
            ComboIndex(cb) == 2 && ed.LireTexte() == "Haute" && !ed.PopupOuvert() && nChoix == c0 + 1 && nVal == v0 + 1);
        Touche(w, PhysicalKey.F4); Touche(w, PhysicalKey.ArrowUp); Touche(w, PhysicalKey.Enter);
        Check("ComboBox : F4 puis ↑ et Entrée choisissent l'item précédent", ComboIndex(cb) == 1 && !ed.PopupOuvert());
        Touche(w, PhysicalKey.ArrowDown, RawInputModifiers.Alt); var ouvert = ed.PopupOuvert();
        Touche(w, PhysicalKey.Escape);
        Check("ComboBox : Alt+↓ ouvre, Échap ferme sans changer la valeur", ouvert && !ed.PopupOuvert() && ComboIndex(cb) == 1);
        Touche(w, PhysicalKey.F4); ClickAt(w, horsChamp);
        Check("ComboBox : un clic extérieur ferme la liste", !ed.PopupOuvert());
        Touche(w, PhysicalKey.ArrowDown);
        Check("ComboBox : fermée, ↓ passe à l'item suivant sans ouvrir", ComboIndex(cb) == 2 && !ed.PopupOuvert());
        w.Close();
        var bas = new HDevComboBox { Width = 160 }; var edBas = AdComboBox(bas); ComboItems(bas, "a", "b", "c", "d");
        var wBas = Hote(bas, top: 380); edBas.Focaliser(); Touche(wBas, PhysicalKey.F4);
        Check("ComboBox : près du bas de la fenêtre, la liste s'ouvre au-dessus",
            Dessus(BoitePopup(wBas, edBas), Boite(wBas, bas)));
        wBas.Close();
        var cbEd = new HDevComboBox { Width = 160 }; var edEd = AdComboBox(cbEd); ComboItems(cbEd, "Basse", "Normale", "Haute"); ComboEditable(cbEd);
        var wEd = Hote(cbEd); FocaliserSaisieCombo(cbEd); Pump(wEd);
        Tape(wEd, "Ha"); var complete = edEd.LireTexte(); var propose = edEd.PopupOuvert();
        Touche(wEd, PhysicalKey.Enter);
        Check("ComboBox éditable : la frappe complète et propose, Entrée choisit",
            complete == "Haute" && propose && ComboIndex(cbEd) == 2 && Equals(edEd.LireValeur(), "Haute") && !edEd.PopupOuvert());
        ToutSelectionnerCombo(cbEd); Tape(wEd, "Libre"); Touche(wEd, PhysicalKey.Enter);
        Check("ComboBox éditable : un texte libre est accepté comme valeur", ComboIndex(cbEd) == -1 && Equals(edEd.LireValeur(), "Libre"));
        wEd.Close();
    }

    // ─── HDevLookUpEdit ───
    {
        var refs = new List<RefE0> { new(1, "Alpha"), new(2, "Beta"), new(3, "Gamma") };
        var lu = new HDevLookUpEdit { Width = 180, DataSource = refs, DisplayMember = "Nom", ValueMember = "Id" };
        var ed = AdLookUpEdit(lu); int n = 0; ed.SurValeurChangee(() => n++);
        var w = Hote(lu);
        ed.PoserValeur(2); Pump(w);
        Check("LookUpEdit : valeur posée par code affichée par son libellé, EditValueChanged une fois", ed.LireTexte() == "Beta" && n == 1);
        ed.Focaliser(); Touche(w, PhysicalKey.F4);
        Check("LookUpEdit : F4 ouvre la grille sous le champ",
            ed.PopupOuvert() && Dessous(BoitePopup(w, ed), Boite(w, lu)));
        var n0 = n; ChoisirLigne(lu, refs[2]); Pump(w);
        Check("LookUpEdit : choisir une ligne pose la valeur et ferme", Equals(ed.LireValeur(), 3) && !ed.PopupOuvert() && n == n0 + 1);
        Touche(w, PhysicalKey.Enter); var ouvert = ed.PopupOuvert(); Touche(w, PhysicalKey.Escape);
        Check("LookUpEdit : Entrée ouvre, Échap ferme", ouvert && !ed.PopupOuvert());
        n0 = n; Touche(w, PhysicalKey.Delete);
        Check("LookUpEdit : Suppr efface la valeur", ed.LireValeur() == null && n == n0 + 1);
        Touche(w, PhysicalKey.F4); ClickAt(w, horsChamp);
        Check("LookUpEdit : un clic extérieur ferme la grille", !ed.PopupOuvert());
        w.Close();
        var bas = new HDevLookUpEdit { Width = 180, DataSource = refs, DisplayMember = "Nom", ValueMember = "Id" };
        var edBas = AdLookUpEdit(bas); var wBas = Hote(bas, top: 380); edBas.Focaliser(); Touche(wBas, PhysicalKey.F4);
        Check("LookUpEdit : près du bas de la fenêtre, la grille s'ouvre au-dessus",
            Dessus(BoitePopup(wBas, edBas), Boite(wBas, bas)));
        wBas.Close();
    }

    // ─── HDevSearchControl ───
    {
        var sc = new HDevSearchControl { Width = 200 }; var ed = AdSearch(sc);
        var choisis = new List<object>(); var requetes = new List<string>();
        sc.SuggestionsProvider = t => new object[] { "Gamma", "Gantt", "Grille" }
            .Where(x => ((string)x).StartsWith(t, StringComparison.OrdinalIgnoreCase));
        sc.SuggestionChosen += (_, o) => choisis.Add(o); sc.SearchRequested += (_, t) => requetes.Add(t);
        var w = Hote(sc); ed.Focaliser(); Tape(w, "Ga");
        Check("SearchControl : la frappe propose les suggestions correspondantes", ed.PopupOuvert() && NbSuggestions(sc) == 2);
        Touche(w, PhysicalKey.ArrowDown); Touche(w, PhysicalKey.Enter);
        Check("SearchControl : ↓ et Entrée choisissent la suggestion et lancent la recherche",
            choisis.SequenceEqual(new object[] { "Gamma" }) && requetes.LastOrDefault() == "Gamma" && ed.LireTexte() == "Gamma");
        ed.Focaliser(); Tape(w, "Gr"); Touche(w, PhysicalKey.Escape); var fermeParEchap = !ed.PopupOuvert();
        Touche(w, PhysicalKey.Enter);
        Check("SearchControl : Échap ferme ; Entrée sans suggestion lance la recherche du texte",
            fermeParEchap && requetes.LastOrDefault() == "Gr");
        w.Close();
        var bas = new HDevSearchControl { Width = 200, SuggestionsProvider = sc.SuggestionsProvider }; var edBas = AdSearch(bas);
        var wBas = Hote(bas, top: 380); edBas.Focaliser(); Tape(wBas, "Ga");
        Check("SearchControl : près du bas de la fenêtre, les suggestions s'ouvrent au-dessus",
            edBas.PopupOuvert() && Dessus(BoitePopup(wBas, edBas), Boite(wBas, bas)));
        wBas.Close();
    }

    // ─── HDevTokenEdit ───
    {
        var tk = new HDevTokenEdit { Width = 260 }; var ed = AdTokenEdit(tk); int n = 0; ed.SurValeurChangee(() => n++);
        int Nb() => ((List<object>)ed.LireValeur()!).Count;
        var w = Hote(tk); ed.Focaliser(); Pump(w);
        Tape(w, "Alice"); Touche(w, PhysicalKey.Enter);
        Check("TokenEdit : Entrée valide le jeton et vide la saisie ; TokensChanged", Nb() == 1 && n == 1 && ed.LireTexte() == "");
        Tape(w, "Bob"); Touche(w, PhysicalKey.Semicolon);
        Check("TokenEdit : « ; » valide aussi", Nb() == 2 && ed.LireTexte() == "");
        Touche(w, PhysicalKey.Backspace);
        Check("TokenEdit : Retour arrière sur saisie vide retire le dernier jeton", Nb() == 1);
        tk.TokenValidator = t => t.Contains('@') ? t : null; Tape(w, "x"); Touche(w, PhysicalKey.Enter);
        Check("TokenEdit : un texte refusé par le validateur reste dans la saisie", Nb() == 1 && ed.LireTexte() == "x");
        ViderSaisieJeton(tk); tk.TokenValidator = null;
        tk.SuggestionsProvider = t => new object[] { "Carla", "Cedric" }.Where(x => ((string)x).StartsWith(t, StringComparison.OrdinalIgnoreCase));
        Tape(w, "Ca"); var propose = ed.PopupOuvert(); Touche(w, PhysicalKey.ArrowDown); Touche(w, PhysicalKey.Enter);
        Check("TokenEdit : la frappe propose ; ↓ et Entrée ajoutent la suggestion",
            propose && ((List<object>)ed.LireValeur()!).Contains("Carla"));
        w.Close();
        var bas = new HDevTokenEdit { Width = 260, SuggestionsProvider = tk.SuggestionsProvider }; var edBas = AdTokenEdit(bas);
        var wBas = Hote(bas, top: 380); edBas.Focaliser(); Pump(wBas); Tape(wBas, "C");
        Check("TokenEdit : près du bas de la fenêtre, les suggestions s'ouvrent au-dessus",
            edBas.PopupOuvert() && Dessus(BoitePopup(wBas, edBas), Boite(wBas, bas)));
        wBas.Close();
    }

    // ─── HDevMonthCalendar ───
    {
        var mc = new HDevMonthCalendar { Width = 260, Height = 240 }; var ed = AdCalendrier(mc); int n = 0; ed.SurValeurChangee(() => n++);
        var w = Hote(mc);
        ed.PoserValeur(new DateTime(2026, 9, 14)); Pump(w);
        Check("MonthCalendar : date posée par code, SelectedDateChanged une fois", n == 1);
        ed.Focaliser(); Touche(w, PhysicalKey.ArrowRight); var droite = ed.LireValeur();
        Touche(w, PhysicalKey.ArrowDown); var bas = ed.LireValeur(); Touche(w, PhysicalKey.PageDown);
        Check("MonthCalendar : → +1 jour, ↓ +7 jours, Pg suiv. +1 mois",
            Equals(droite, new DateTime(2026, 9, 15)) && Equals(bas, new DateTime(2026, 9, 22))
            && Equals(ed.LireValeur(), new DateTime(2026, 10, 22)));
        var nAvant = n; mc.MaxDate = new DateTime(2026, 10, 25); Touche(w, PhysicalKey.ArrowDown);
        Check("MonthCalendar : MaxDate borne la navigation, sans événement", Equals(ed.LireValeur(), new DateTime(2026, 10, 22)) && n == nAvant);
        w.Close();
    }

    // ─── Thème à chaud ───
    {
        var de = new HDevDateEdit { Width = 160 }; var tb = new HDevTextBox { Width = 160 };
        var w = new Window { Width = 300, Height = 200, Content = new StackPanel { Children = { de, tb } } }; w.Show(); Pump(w);
        Avalonia.Media.Color? Fond(Control c) => ((c as Border)?.Background as Avalonia.Media.ISolidColorBrush)?.Color;
        var varianteAvant = HDev.UI.Controls.Theme.HDevTheme.Variant;
        HDev.UI.Controls.Theme.HDevTheme.Variant = HDev.UI.Controls.Theme.HDevThemeVariant.Dark; Pump(w);
        var sombre = HDev.UI.Controls.Theme.HDevTheme.Background.Input;
        var suitSombre = Fond(de) == sombre && Fond(tb) == sombre;
        HDev.UI.Controls.Theme.HDevTheme.Variant = HDev.UI.Controls.Theme.HDevThemeVariant.Light; Pump(w);
        var clair = HDev.UI.Controls.Theme.HDevTheme.Background.Input;
        Check("thème : les champs suivent le fond de saisie à chaud, aller et retour",
            suitSombre && sombre != clair && Fond(de) == clair && Fond(tb) == clair);
        HDev.UI.Controls.Theme.HDevTheme.Variant = varianteAvant; Pump(w);
        w.Close();
    }

    // ═══ CONTRAT DES ÉDITEURS (E1) : valeur liable, événement commun, erreurs de liaison ═══
    // Témoin : HDevDateEdit, lié en XAML compilé (FormulaireE1.axaml) à un ViewModel
    // INotifyDataErrorInfo. Ces attentes décrivent le contrat (Conception-Socle-Editeurs.md) ;
    // chaque éditeur s'y ajoutera à son portage (E3).
    Console.WriteLine("Contrat des éditeurs (E1) :");
    {
        var vm = new FormulaireE1Vm();
        var vue = new FormulaireE1 { DataContext = vm };
        var w = new Window { Width = 500, Height = 420, Content = vue }; w.Show(); Pump(w);
        var de = vue.FindControl<HDevDateEdit>("Depart")!;
        var journal = new List<HDevValueChangedEventArgs>();
        vue.FindControl<StackPanel>("Formulaire")!.AddHandler(HDevEditor.ValueChangedEvent,
            (object? _, HDevValueChangedEventArgs e) => journal.Add(e));
        void Saisit(string chiffres) { de.FocusEditor(); de.SelectAll(); TapeParCaractere(w, chiffres); }

        Check("liaison XAML sans Mode : la valeur du ViewModel s'affiche", de.Text == "14/09/2026");
        Saisit("20092026");
        Check("la saisie remonte au ViewModel (TwoWay par défaut)", vm.Depart == new DateTime(2026, 9, 20));
        Check("le formulaire reçoit un seul ValueChanged : origine Utilisateur, ancienne et nouvelle valeur",
            journal.Count == 1 && journal[0].IsUserChange && ReferenceEquals(journal[0].Source, de)
            && Equals(journal[0].OldValue, new DateTime(2026, 9, 14)) && Equals(journal[0].NewValue, new DateTime(2026, 9, 20)));
        vm.Depart = new DateTime(2026, 10, 1); Pump(w);
        Check("le ViewModel repousse une valeur : affichée, origine Code, la liaison a survécu à la saisie",
            de.Text == "01/10/2026" && journal.Count == 2 && journal[1].Origin == HDevValueOrigin.Code);

        vm.ErreurDepart("Jour non ouvré"); Pump(w);
        Check("erreur INotifyDataErrorInfo affichée par l'éditeur", de.ErrorText == "Jour non ouvré" && de.HasError);
        Saisit("02102026");
        Check("l'erreur du ViewModel survit à une saisie valide : seule la source la retire",
            vm.Depart == new DateTime(2026, 10, 2) && de.ErrorText == "Jour non ouvré");
        Saisit("3"); var saisieRefusee = !de.Validate(); var erreurDeSaisie = de.ErrorText;
        TapeParCaractere(w, "1102026"); de.Validate();
        Check("une saisie invalide passe devant l'erreur du ViewModel, qui revient ensuite",
            saisieRefusee && erreurDeSaisie != "Jour non ouvré" && erreurDeSaisie != null && de.ErrorText == "Jour non ouvré");
        vm.ErreurDepart(null); Pump(w);
        Check("erreur retirée par le ViewModel : l'éditeur redevient valide", de.ErrorText == null && !de.HasError);

        var refus = 0;
        void RefuseLeWeekEnd(object? _, HDevValueChangingEventArgs e)
        {
            if (e.NewValue is DateTime d && d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) { e.Cancel = true; refus++; }
        }
        de.ValueChanging += RefuseLeWeekEnd;
        var avantRefus = vm.Depart; var nJournal = journal.Count;
        Saisit("03102026"); // un samedi
        Check("ValueChanging annulé : la valeur reste, le texte revient, aucun ValueChanged",
            refus == 1 && vm.Depart == avantRefus && de.Text == "31/10/2026" && journal.Count == nJournal);
        de.ValueChanging -= RefuseLeWeekEnd;

        nJournal = journal.Count;
        de.FocusEditor(); Touche(w, PhysicalKey.F4); ChoisirJourDuCalendrier(de, new DateTime(2026, 10, 7)); Pump(w);
        Check("encapsulation : choisir un jour fait remonter un seul ValueChanged, celui de l'éditeur",
            vm.Depart == new DateTime(2026, 10, 7) && journal.Count == nJournal + 1
            && ReferenceEquals(journal[^1].Source, de) && journal[^1].IsUserChange);

        // Texte invalide obtenu par une saisie incomplète : depuis E2, un collage « abc » est refusé
        // par le masque (constat E0 n° 6) et ne peut plus servir à fabriquer un texte invalide
        Saisit("15"); var texteInvalide = de.Text;
        de.Value = de.Value; Pump(w);
        Check("reposer la valeur courante par code réaffiche son texte (constat E0 n° 5)",
            texteInvalide == "15" && de.Text == "07/10/2026" && de.ErrorText == null);

        vm.Depart = new DateTime(2026, 10, 7, 10, 30, 0); Pump(w);
        var heureGardee = vm.Depart == new DateTime(2026, 10, 7, 10, 30, 0) && de.Text == "07/10/2026";
        Saisit("08102026");
        Check("l'heure de la valeur n'est ni affichée ni perdue : un autre jour la garde",
            heureGardee && vm.Depart == new DateTime(2026, 10, 8, 10, 30, 0));

        IHDevEditor contrat = de;
        contrat.EditValue = new DateTime(2026, 11, 11); Pump(w);
        Check("IHDevEditor : EditValue pose la valeur typée, jusqu'au ViewModel",
            vm.Depart == new DateTime(2026, 11, 11) && Equals(contrat.EditValue, new DateTime(2026, 11, 11)));
        w.Close();
    }

    // ═══ SOCLE COMPOSITE (E2) : masque et collage ═══
    Console.WriteLine("Socle des éditeurs (E2) :");
    {
        var de = new HDevDateEdit { Width = 160 }; var te = new HDevTimeEdit { Width = 120 };
        var sp = new HDevSpinEdit { Width = 120, Decimals = 2, MinValue = 0, MaxValue = 100 };
        Canvas.SetTop(te, 80); Canvas.SetTop(sp, 140);
        var w = Hote(de); ((Canvas)w.Content!).Children.Add(te); ((Canvas)w.Content!).Children.Add(sp); Pump(w);
        void ToutSelectionner(HDevEditorBase e) { e.FocusEditor(); e.SelectAll(); Pump(w); }

        ToutSelectionner(de); Colle(w, "20092026");
        Check("collage sous masque : « 20092026 » passe par le masque et donne la date (constat E0 n° 6)",
            de.Text == "20/09/2026" && de.Value == new DateTime(2026, 9, 20));
        ToutSelectionner(de); Colle(w, "2026-09-21"); var iso = de.Text;
        ToutSelectionner(de); Colle(w, "abc");
        Check("collage sous masque : un texte qui ne tient pas dans le masque est refusé en entier",
            iso == "20/09/2026" && de.Text == "20/09/2026" && de.Value == new DateTime(2026, 9, 20));
        ToutSelectionner(te); Colle(w, "1745");
        Check("collage sous masque : « 1745 » donne 17:45 dans TimeEdit", te.Text == "17:45" && te.Value == new TimeSpan(17, 45, 0));
        ToutSelectionner(sp); Colle(w, "12a,5"); var texteRefuse = sp.Text;
        ToutSelectionner(sp); Colle(w, "12,5");
        Check("collage filtré : SpinEdit refuse « 12a,5 » et prend « 12,5 »",
            texteRefuse == "0,00" && sp.Text == "12,5" && sp.Value == 12.5m);
        ToutSelectionner(de); Colle(w, "20/09/2026"); ToutSelectionner(de); Tape(w, "14092026");
        Touche(w, PhysicalKey.Backspace);
        Check("après un collage, une frappe en bloc réduit la sélection : Retour arrière n'efface qu'un chiffre (constat E0 n° 7)",
            de.Text == "14/09/202");
        w.Close();
    }

    // ═══ SOCLE COMPOSITE (E2) : retournement des popups, mesuré en géométrie (constat E0 n° 4) ═══
    // L'écart au champ doit être le même dessous et dessus : 2 px de jour, ou 1 px de bordure
    // fusionnée (LookUpEdit). Les contrôles Dessous/Dessus d'E0 tolèrent 2 px et ne le voient pas.
    {
        (double Dessous, double Dessus) Ecarts(Func<Control> creer, Func<Control, Editeur> adapter, Action<Window, Editeur> ouvrir)
        {
            double Mesure(double top, bool dessus)
            {
                var c = creer(); var ed = adapter(c); var w = Hote(c, top); ed.Focaliser(); Pump(w); ouvrir(w, ed);
                var champ = Boite(w, c); var pop = BoitePopup(w, ed); w.Close();
                if (pop is not { } r) return double.NaN;
                return dessus ? champ.Top - r.Bottom : r.Top - champ.Bottom;
            }
            return (Mesure(20, false), Mesure(380, true));
        }
        string Lu((double Dessous, double Dessus) e) => $"lu {e.Dessous:0.##} dessous, {e.Dessus:0.##} dessus";
        Func<string, IEnumerable<object>> suggestions = t => new object[] { "Gamma", "Gantt", "Carla" }
            .Where(x => ((string)x).StartsWith(t, StringComparison.OrdinalIgnoreCase));

        var date = Ecarts(() => new HDevDateEdit { Width = 160 }, c => AdDateEdit((HDevDateEdit)c),
            (w, _) => Touche(w, PhysicalKey.F4));
        Check($"DateEdit : 2 px de jour dessous comme dessus ({Lu(date)})", date.Dessous == 2 && date.Dessus == 2);
        var search = Ecarts(() => new HDevSearchControl { Width = 200, SuggestionsProvider = suggestions },
            c => AdSearch((HDevSearchControl)c), (w, _) => Tape(w, "Ga"));
        Check($"SearchControl : 2 px de jour dessous comme dessus ({Lu(search)})", search.Dessous == 2 && search.Dessus == 2);
        var jetons = Ecarts(() => new HDevTokenEdit { Width = 260, SuggestionsProvider = suggestions },
            c => AdTokenEdit((HDevTokenEdit)c), (w, _) => Tape(w, "C"));
        Check($"TokenEdit : 2 px de jour dessous comme dessus ({Lu(jetons)})", jetons.Dessous == 2 && jetons.Dessus == 2);
        var refs = new List<RefE0> { new(1, "Alpha"), new(2, "Beta"), new(3, "Gamma") };
        var lookup = Ecarts(() => new HDevLookUpEdit { Width = 180, DataSource = refs, DisplayMember = "Nom", ValueMember = "Id" },
            c => AdLookUpEdit((HDevLookUpEdit)c), (w, _) => Touche(w, PhysicalKey.F4));
        Check($"LookUpEdit : bordure fusionnée dessous comme dessus ({Lu(lookup)})", lookup.Dessous == -1 && lookup.Dessus == -1);
    }

    // ═══ SOCLE COMPOSITE (E2) : multiligne ═══
    {
        var memo = new EditeurNuE2 { Width = 200 }; var w = Hote(memo);
        var hauteurMono = memo.Bounds.Height;
        memo.IsMultiline = true; Pump(w);
        var hauteurVide = memo.Bounds.Height;
        memo.FocusEditor(); Tape(w, "un"); Touche(w, PhysicalKey.Enter); Tape(w, "deux");
        Check($"multiligne : Entrée insère un saut de ligne, le champ part de trois lignes (lu {hauteurMono} puis {hauteurVide})",
            memo.Text == "un\ndeux" && hauteurMono == 28 && hauteurVide == 84);
        memo.Text = "1\n2\n3\n4\n5\n6\n7"; Pump(w); var haut = memo.Bounds.Height;
        Colle(w, "\nhuit\nneuf"); var colle = memo.Text;
        Check($"multiligne : la hauteur suit le texte, le collage garde ses lignes (lu {haut}, {colle.Replace("\n", "⏎")})",
            haut > 84 && colle.Contains("\nhuit\nneuf") && colle.Split('\n').Length == 9);
        memo.IsMultiline = false; Pump(w);
        Check($"retour au monoligne : hauteur du gabarit (lu {memo.Bounds.Height})", memo.Bounds.Height == 28);
        w.Close();
    }

    System.Globalization.CultureInfo.CurrentCulture = cultureAvant;
    System.Globalization.CultureInfo.CurrentUICulture = cultureUiAvant;
}

Console.WriteLine(failed == 0 ? "Interaction HDev.UI.Controls : ALL PASS" : $"Interaction HDev.UI.Controls : {failed} FAILED");
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

/// <summary>Éditeur nu sur le socle, sans pièce ni valeur typée : ce que le socle seul fait (E2)</summary>
sealed class EditeurNuE2 : HDevEditorBase { }

// ─── Types des adaptateurs E0 (section « Éditeurs ») ───
sealed record RefE0(int Id, string Nom);

sealed class Editeur
{
    public required Control Controle { get; init; }
    public Action<object?> PoserValeur { get; init; } = _ => throw new NotSupportedException();
    public Func<object?> LireValeur { get; init; } = () => throw new NotSupportedException();
    public Func<string> LireTexte { get; init; } = () => "";
    public Action<Action> SurValeurChangee { get; init; } = _ => { };
    public Action Focaliser { get; init; } = () => { };
    public Func<bool> PopupOuvert { get; init; } = () => false;
    public Func<Control?> ContenuPopup { get; init; } = () => null;
    public Func<bool> Valider { get; init; } = () => true;
    public Func<string?> Erreur { get; init; } = () => null;
}
