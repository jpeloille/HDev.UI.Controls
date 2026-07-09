using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Layout;
using ProControls.Theme;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ProControls.Controls;

/// <summary>
/// Ruban professionnel style Office/VS2022
/// </summary>
public class ProRibbon : Control
{
    private const double TabRowHeight = 28;
    private const double ContentHeight = 90;
    private const double GroupLabelHeight = 18;
    
    private int _hoveredTabIndex = -1;
    private Point _mousePosition;
    private ProRibbonButton? _hoveredButton;
    private ProRibbonButton? _pressedButton;
    
    // ═══════════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════════
    
    public static readonly StyledProperty<int> SelectedTabIndexProperty =
        AvaloniaProperty.Register<ProRibbon, int>(nameof(SelectedTabIndex), 0);
    
    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<ProRibbon, bool>(nameof(IsCollapsed), false);

    public int SelectedTabIndex
    {
        get => GetValue(SelectedTabIndexProperty);
        set => SetValue(SelectedTabIndexProperty, Math.Clamp(value, 0, Math.Max(0, Tabs.Count - 1)));
    }
    
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }
    
    public ObservableCollection<ProRibbonTab> Tabs { get; } = new();
    
    public ProRibbonTab? SelectedTab => 
        SelectedTabIndex >= 0 && SelectedTabIndex < Tabs.Count ? Tabs[SelectedTabIndex] : null;
    
    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTEUR
    // ═══════════════════════════════════════════════════════════════
    
    static ProRibbon()
    {
        AffectsRender<ProRibbon>(SelectedTabIndexProperty, IsCollapsedProperty);
        AffectsMeasure<ProRibbon>(IsCollapsedProperty);
    }
    
    public ProRibbon()
    {
        ClipToBounds = true;
        Tabs.CollectionChanged += (s, e) => InvalidateVisual();
    }
    
    // ═══════════════════════════════════════════════════════════════
    // MESURE
    // ═══════════════════════════════════════════════════════════════
    
    protected override Size MeasureOverride(Size availableSize)
    {
        var height = TabRowHeight + (IsCollapsed ? 0 : ContentHeight);
        return new Size(availableSize.Width, height);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // INTERACTIONS
    // ═══════════════════════════════════════════════════════════════
    
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        _mousePosition = e.GetPosition(this);

        // Déterminer l'onglet survolé
        var newHovered = GetTabIndexAtPosition(_mousePosition.X);
        if (newHovered != _hoveredTabIndex)
        {
            _hoveredTabIndex = newHovered;
            InvalidateVisual();
        }

        // Déterminer le bouton survolé dans le contenu
        var newHoveredButton = GetButtonAtPosition(_mousePosition);
        if (newHoveredButton != _hoveredButton)
        {
            _hoveredButton = newHoveredButton;
            InvalidateVisual();
        }

        base.OnPointerMoved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        _hoveredTabIndex = -1;
        _hoveredButton = null;
        _pressedButton = null;
        InvalidateVisual();
        base.OnPointerExited(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(this);

        // Clic sur un onglet ?
        if (pos.Y < TabRowHeight)
        {
            var tabIndex = GetTabIndexAtPosition(pos.X);
            if (tabIndex >= 0 && tabIndex < Tabs.Count)
            {
                SelectedTabIndex = tabIndex;
                if (IsCollapsed)
                    IsCollapsed = false;
                InvalidateVisual();
            }

            // Clic sur le bouton collapse ?
            if (pos.X > Bounds.Width - 40)
            {
                IsCollapsed = !IsCollapsed;
                InvalidateMeasure();
            }
        }
        else
        {
            // Clic sur un bouton du contenu ?
            var btn = GetButtonAtPosition(pos);
            if (btn is { IsEnabled: true })
            {
                _pressedButton = btn;
                InvalidateVisual();
                e.Handled = true;
            }
        }

        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_pressedButton != null)
        {
            var pos = e.GetPosition(this);
            var released = GetButtonAtPosition(pos);
            var btn = _pressedButton;
            _pressedButton = null;
            InvalidateVisual();

            if (released == btn)
            {
                btn.RaiseClick();

                if (btn.HasDropdown && btn.DropdownMenu != null)
                    btn.DropdownMenu.Show(this, pos);

                e.Handled = true;
            }
        }

        base.OnPointerReleased(e);
    }

    // ═══════════════════════════════════════════════════════════════
    // LAYOUT DES BOUTONS (partagé entre rendu et hit-test)
    // ═══════════════════════════════════════════════════════════════

    private ProRibbonButton? GetButtonAtPosition(Point pos)
    {
        if (IsCollapsed || pos.Y <= TabRowHeight) return null;

        foreach (var (item, rect) in GetContentLayout())
        {
            if (item is ProRibbonButton btn && rect.Contains(pos))
                return btn;
        }
        return null;
    }

    /// <summary>
    /// Énumère les items du tab sélectionné avec leur rectangle : la même marche
    /// est utilisée par le rendu et par le hit-test, pour que ce qui est dessiné
    /// soit exactement ce qui est cliquable.
    /// </summary>
    internal IEnumerable<(ProRibbonItem Item, Rect Rect)> GetContentLayout()
    {
        if (SelectedTab == null) yield break;

        double groupX = 6;
        foreach (var group in SelectedTab.Groups)
        {
            var groupWidth = MeasureGroupWidth(group);
            foreach (var entry in GetGroupItemsLayout(group, groupX, TabRowHeight))
                yield return entry;
            groupX += groupWidth;
        }
    }

    private IEnumerable<(ProRibbonItem Item, Rect Rect)> GetGroupItemsLayout(
        ProRibbonGroup group, double x, double y)
    {
        var largeButtonHeight = ContentHeight - GroupLabelHeight - 10;
        double itemX = x + 6;
        double itemY = y + 4;
        int smallRow = 0;
        double smallColumnX = 0;
        double maxSmallWidth = 0;

        foreach (var item in group.Items)
        {
            if (item is ProRibbonButton btn)
            {
                if (btn.IsLarge)
                {
                    if (smallRow > 0)
                    {
                        itemX = smallColumnX + maxSmallWidth + 4;
                        smallRow = 0;
                        maxSmallWidth = 0;
                    }

                    yield return (btn, new Rect(itemX, itemY, 50, largeButtonHeight));
                    itemX += 54;
                }
                else
                {
                    if (smallRow == 0)
                    {
                        smallColumnX = itemX;
                        maxSmallWidth = 0;
                    }

                    var btnWidth = MeasureSmallButtonWidth(btn);
                    maxSmallWidth = Math.Max(maxSmallWidth, btnWidth);
                    yield return (btn, new Rect(smallColumnX, itemY + smallRow * 22, btnWidth, 20));

                    smallRow++;
                    if (smallRow >= 3)
                    {
                        itemX = smallColumnX + maxSmallWidth + 4;
                        smallRow = 0;
                        maxSmallWidth = 0;
                    }
                }
            }
            else if (item is ProRibbonSeparator sep)
            {
                if (smallRow > 0)
                {
                    itemX = smallColumnX + maxSmallWidth + 4;
                    smallRow = 0;
                    maxSmallWidth = 0;
                }

                yield return (sep, new Rect(itemX + 4, y + 8, 1, ContentHeight - GroupLabelHeight - 16));
                itemX += 12;
            }
        }
    }
    
    private int GetTabIndexAtPosition(double x)
    {
        double tabX = 8;
        for (int i = 0; i < Tabs.Count; i++)
        {
            var tabWidth = MeasureTabWidth(Tabs[i]);
            if (x >= tabX && x < tabX + tabWidth)
                return i;
            tabX += tabWidth + 4;
        }
        return -1;
    }
    
    private double MeasureTabWidth(ProRibbonTab tab)
    {
        var text = CreateFormattedText(tab.Header, VS2022Theme.Text.Primary, 12);
        return Math.Max(60, text.Width + 24);
    }
    
    // ═══════════════════════════════════════════════════════════════
    // RENDU
    // ═══════════════════════════════════════════════════════════════
    
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        
        // Fond général
        context.FillRectangle(new SolidColorBrush(VS2022Theme.Background.Toolbar), bounds);
        
        // ══════════════════════════════════════════════════════════
        // RANGÉE DES ONGLETS
        // ══════════════════════════════════════════════════════════
        
        RenderTabs(context, bounds);
        
        // ══════════════════════════════════════════════════════════
        // CONTENU DU RUBAN
        // ══════════════════════════════════════════════════════════
        
        if (!IsCollapsed && SelectedTab != null)
        {
            RenderContent(context, bounds);
        }
        
        // Bordure en bas
        var borderPen = new Pen(new SolidColorBrush(VS2022Theme.Border.Subtle), 1);
        context.DrawLine(borderPen, 
            new Point(0, bounds.Height - 0.5), 
            new Point(bounds.Width, bounds.Height - 0.5));
    }
    
    private void RenderTabs(DrawingContext context, Rect bounds)
    {
        double tabX = 8;
        
        for (int i = 0; i < Tabs.Count; i++)
        {
            var tab = Tabs[i];
            var tabWidth = MeasureTabWidth(tab);
            var isSelected = i == SelectedTabIndex;
            var isHovered = i == _hoveredTabIndex && !isSelected;
            
            // Fond de l'onglet
            if (isSelected)
            {
                // Onglet sélectionné
                var tabRect = new Rect(tabX, 0, tabWidth, TabRowHeight);
                context.FillRectangle(new SolidColorBrush(VS2022Theme.Background.Panel), tabRect);
                
                // Ligne d'accent en haut
                var accentPen = new Pen(new SolidColorBrush(VS2022Theme.Accent.Primary), 2);
                context.DrawLine(accentPen,
                    new Point(tabX + 1, 1),
                    new Point(tabX + tabWidth - 1, 1));
            }
            else if (isHovered)
            {
                // Onglet survolé
                var hoverRect = new Rect(tabX, 2, tabWidth, TabRowHeight - 4);
                context.FillRectangle(new SolidColorBrush(VS2022Theme.Background.ControlHover), hoverRect);
            }
            
            // Texte
            var textColor = isSelected ? VS2022Theme.Text.Primary : VS2022Theme.Text.Secondary;
            var tabText = CreateFormattedText(tab.Header, textColor, 12);
            context.DrawText(tabText, Crisp.Snap(new Point(
                tabX + (tabWidth - tabText.Width) / 2,
                (TabRowHeight - tabText.Height) / 2)));
            
            tabX += tabWidth + 4;
        }
        
        // Bouton collapse/expand
        RenderCollapseButton(context, bounds);
        
        // Ligne de séparation sous les onglets (seulement si non collapsed)
        if (!IsCollapsed)
        {
            var sepPen = new Pen(new SolidColorBrush(VS2022Theme.Border.Subtle), 1);
            context.DrawLine(sepPen, 
                new Point(0, TabRowHeight - 0.5), 
                new Point(bounds.Width, TabRowHeight - 0.5));
        }
    }
    
    private void RenderCollapseButton(DrawingContext context, Rect bounds)
    {
        var btnX = bounds.Width - 28;
        var btnY = 6;
        var pen = new Pen(new SolidColorBrush(VS2022Theme.Text.Tertiary), 1.5);
        
        if (IsCollapsed)
        {
            // Flèche vers le bas ∨
            context.DrawLine(pen, new Point(btnX, btnY + 4), new Point(btnX + 6, btnY + 10));
            context.DrawLine(pen, new Point(btnX + 6, btnY + 10), new Point(btnX + 12, btnY + 4));
        }
        else
        {
            // Flèche vers le haut ∧
            context.DrawLine(pen, new Point(btnX, btnY + 10), new Point(btnX + 6, btnY + 4));
            context.DrawLine(pen, new Point(btnX + 6, btnY + 4), new Point(btnX + 12, btnY + 10));
        }
    }
    
    private void RenderContent(DrawingContext context, Rect bounds)
    {
        var contentRect = new Rect(0, TabRowHeight, bounds.Width, ContentHeight);
        
        // Fond du contenu
        context.FillRectangle(new SolidColorBrush(VS2022Theme.Background.Panel), contentRect);
        
        if (SelectedTab == null) return;
        
        // Dessiner les groupes
        double groupX = 6;
        
        foreach (var group in SelectedTab.Groups)
        {
            var groupWidth = MeasureGroupWidth(group);
            RenderGroup(context, group, groupX, TabRowHeight, groupWidth);
            groupX += groupWidth;
        }
    }
    
    private double MeasureGroupWidth(ProRibbonGroup group)
    {
        double width = 12; // Padding
        double largeButtonsWidth = 0;
        double smallButtonsMaxWidth = 0;
        double currentSmallRow = 0;
        int smallInRow = 0;
        
        foreach (var item in group.Items)
        {
            if (item is ProRibbonButton btn)
            {
                if (btn.IsLarge)
                {
                    largeButtonsWidth += 54;
                }
                else
                {
                    var btnWidth = MeasureSmallButtonWidth(btn);
                    currentSmallRow = Math.Max(currentSmallRow, btnWidth);
                    smallInRow++;
                    if (smallInRow >= 3)
                    {
                        smallButtonsMaxWidth += currentSmallRow + 4;
                        currentSmallRow = 0;
                        smallInRow = 0;
                    }
                }
            }
            else if (item is ProRibbonSeparator)
            {
                width += 12;
            }
        }
        
        if (smallInRow > 0)
            smallButtonsMaxWidth += currentSmallRow + 4;
        
        width += largeButtonsWidth + smallButtonsMaxWidth;
        
        // Largeur minimum pour le label
        var labelText = CreateFormattedText(group.Header, VS2022Theme.Text.Tertiary, 11);
        width = Math.Max(width, labelText.Width + 20);
        
        return width;
    }
    
    private double MeasureSmallButtonWidth(ProRibbonButton btn)
    {
        var text = CreateFormattedText(btn.Label.Replace("\n", " "), VS2022Theme.Text.Primary, 11);
        return 22 + text.Width + 8; // icône + texte + padding
    }
    
    private void RenderGroup(DrawingContext context, ProRibbonGroup group, 
        double x, double y, double width)
    {
        // Séparateur à droite
        var sepPen = new Pen(new SolidColorBrush(VS2022Theme.Border.Default), 1);
        context.DrawLine(sepPen,
            new Point(x + width - 1, y + 6),
            new Point(x + width - 1, y + ContentHeight - GroupLabelHeight - 4));
        
        // Label du groupe en bas
        var labelText = CreateFormattedText(group.Header, VS2022Theme.Text.Tertiary, 11);
        context.DrawText(labelText, Crisp.Snap(new Point(
            x + (width - labelText.Width) / 2,
            y + ContentHeight - GroupLabelHeight + 2)));
        
        // Ligne au-dessus du label
        var labelLinePen = new Pen(new SolidColorBrush(VS2022Theme.Border.Subtle), 1);
        context.DrawLine(labelLinePen,
            new Point(x + 4, y + ContentHeight - GroupLabelHeight - 2),
            new Point(x + width - 8, y + ContentHeight - GroupLabelHeight - 2));
        
        // Dessiner les items (positions issues du même walker que le hit-test)
        foreach (var (item, rect) in GetGroupItemsLayout(group, x, y))
        {
            if (item is ProRibbonButton btn)
            {
                if (btn.IsLarge)
                    RenderLargeButton(context, btn, rect);
                else
                    RenderSmallButton(context, btn, rect);
            }
            else if (item is ProRibbonSeparator)
            {
                context.DrawLine(sepPen,
                    new Point(rect.X, rect.Top),
                    new Point(rect.X, rect.Bottom));
            }
        }
    }

    private void RenderButtonBackground(DrawingContext context, ProRibbonButton btn, Rect btnRect, float radius)
    {
        var isHovered = btn == _hoveredButton && btn.IsEnabled;
        var isPressed = btn == _pressedButton && isHovered;

        if (isPressed || btn.IsChecked)
        {
            context.FillRectangle(
                new SolidColorBrush(VS2022Theme.Background.ControlPressed),
                btnRect, radius);

            if (btn.IsChecked)
            {
                var checkedPen = new Pen(new SolidColorBrush(VS2022Theme.Accent.Primary), 1);
                context.DrawRectangle(null, checkedPen, btnRect.Deflate(0.5), radius, radius);
            }
        }
        else if (isHovered)
        {
            context.FillRectangle(
                new SolidColorBrush(VS2022Theme.Background.ControlHover),
                btnRect, radius);
        }
    }

    private void RenderLargeButton(DrawingContext context, ProRibbonButton btn, Rect btnRect)
    {
        RenderButtonBackground(context, btn, btnRect, 3);

        var textColor = btn.IsEnabled ? VS2022Theme.Text.Primary : VS2022Theme.Text.Disabled;
        var x = btnRect.X;
        var y = btnRect.Y;

        // Icône (grande, centrée en haut)
        if (!string.IsNullOrEmpty(btn.Icon))
        {
            var iconText = CreateFormattedText(btn.Icon, textColor, 22);
            context.DrawText(iconText, Crisp.Snap(new Point(
                x + (50 - iconText.Width) / 2,
                y + 6)));
        }

        // Texte (en bas, peut être sur 2 lignes)
        var lines = btn.Label.Split('\n');
        double textY = y + 38;
        foreach (var line in lines)
        {
            var lineText = CreateFormattedText(line.Trim(), textColor, 11);
            context.DrawText(lineText, Crisp.Snap(new Point(
                x + (50 - lineText.Width) / 2,
                textY)));
            textY += 12;
        }
    }

    private void RenderSmallButton(DrawingContext context, ProRibbonButton btn, Rect btnRect)
    {
        RenderButtonBackground(context, btn, btnRect, 2);

        var textColor = btn.IsEnabled ? VS2022Theme.Text.Primary : VS2022Theme.Text.Disabled;
        var x = btnRect.X;
        var y = btnRect.Y;

        // Icône
        if (!string.IsNullOrEmpty(btn.Icon))
        {
            var iconText = CreateFormattedText(btn.Icon, textColor, 12);
            context.DrawText(iconText, Crisp.Snap(new Point(x + 4, y + (20 - iconText.Height) / 2)));
        }

        // Texte
        var label = btn.Label.Replace("\n", " ");
        var labelText = CreateFormattedText(label, textColor, 11);
        context.DrawText(labelText, Crisp.Snap(new Point(x + 22, y + (20 - labelText.Height) / 2)));
    }
    
    private FormattedText CreateFormattedText(string text, Color color, double size)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(VS2022Theme.Typography.FontFamily),
            size,
            new SolidColorBrush(color));
    }
}
