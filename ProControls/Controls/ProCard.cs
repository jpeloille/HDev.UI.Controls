using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ProControls.Theme;

namespace ProControls.Controls;

public enum CardElevation
{
    Flat,
    Low,
    Medium,
    High
}

/// <summary>
/// Carte/conteneur avec titre style VS2022
/// </summary>
public class ProCard : ContentControl
{
    private bool _initialized;
    private object? _originalContent;
    
    public static readonly StyledProperty<CardElevation> ElevationProperty =
        AvaloniaProperty.Register<ProCard, CardElevation>(nameof(Elevation), CardElevation.Low);
    
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ProCard, string>(nameof(Title), "");
    
    public static readonly StyledProperty<bool> HasBorderProperty =
        AvaloniaProperty.Register<ProCard, bool>(nameof(HasBorder), true);

    public CardElevation Elevation
    {
        get => GetValue(ElevationProperty);
        set => SetValue(ElevationProperty, value);
    }
    
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    
    public bool HasBorder
    {
        get => GetValue(HasBorderProperty);
        set => SetValue(HasBorderProperty, value);
    }

    public ProCard()
    {
        // Style de base
        Margin = new Thickness(4);
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        if (!_initialized)
        {
            _initialized = true;
            BuildLayout();
        }
    }
    
    private void BuildLayout()
    {
        // Sauvegarder le contenu original défini par XAML
        _originalContent = Content;
        
        // Détacher le contenu original de ce ContentControl
        Content = null;
        
        // Créer la structure de la carte
        var cardBorder = new Border
        {
            Background = new SolidColorBrush(VS2022Theme.Background.Panel),
            BorderBrush = new SolidColorBrush(VS2022Theme.Border.Subtle),
            BorderThickness = HasBorder ? new Thickness(1) : new Thickness(0),
            CornerRadius = new CornerRadius(VS2022Theme.Size.CornerRadiusMedium)
        };
        
        var innerPanel = new StackPanel();
        
        // Titre (si présent)
        if (!string.IsNullOrEmpty(Title))
        {
            var titleBlock = new TextBlock
            {
                Text = Title,
                FontFamily = new FontFamily(VS2022Theme.Typography.FontFamily),
                FontSize = VS2022Theme.Typography.FontSizeSubtitle,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(VS2022Theme.Text.Primary),
                Margin = new Thickness(16, 12, 16, 8)
            };
            innerPanel.Children.Add(titleBlock);
            
            // Séparateur
            var separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(VS2022Theme.Border.Subtle),
                Margin = new Thickness(16, 0, 16, 12)
            };
            innerPanel.Children.Add(separator);
        }
        
        // Contenu original
        var contentBorder = new Border
        {
            Margin = string.IsNullOrEmpty(Title) 
                ? new Thickness(16) 
                : new Thickness(16, 0, 16, 16),
            Child = _originalContent as Control
        };
        innerPanel.Children.Add(contentBorder);
        
        cardBorder.Child = innerPanel;
        
        // Remplacer le contenu
        Content = cardBorder;
    }
}
