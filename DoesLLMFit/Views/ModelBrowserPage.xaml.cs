using DoesLLMFit.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace DoesLLMFit.Views;

public sealed partial class ModelBrowserPage : Page
{
    private ModelBrowserViewModel ViewModel { get; }

    public ModelBrowserPage()
    {
        ViewModel = App.Services.GetRequiredService<ModelBrowserViewModel>();
        this.InitializeComponent();
        Loaded += ModelBrowserPage_Loaded;
    }

    private void ModelBrowserPage_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Initialize();
        CategoryComboBox.ItemsSource = ViewModel.Categories;
        CategoryComboBox.SelectedIndex = 0;
        BindModels();
    }

    private void BindModels()
    {
        var cards = new List<UIElement>();
        foreach (var model in ViewModel.FilteredModels)
        {
            cards.Add(CreateModelCard(model));
        }
        ModelRepeater.ItemsSource = cards;
    }

    private static UIElement CreateModelCard(LlmModel model)
    {
        // Determine accent color based on primary category
        var primaryCat = model.Categories.FirstOrDefault() ?? "";
        var (accentColor, accentBg) = GetCategoryAccent(primaryCat);

        var border = new Border
        {
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(16),
            Padding = new Microsoft.UI.Xaml.Thickness(0),
            BorderThickness = new Microsoft.UI.Xaml.Thickness(1),
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(25, 128, 128, 128)),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(6, 128, 128, 128))
        };

        var outerPanel = new StackPanel();

        // Top accent bar
        var accentBar = new Border
        {
            Height = 4,
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(16, 16, 0, 0),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(accentColor)
        };
        outerPanel.Children.Add(accentBar);

        var panel = new StackPanel { Spacing = 10, Padding = new Microsoft.UI.Xaml.Thickness(20, 16, 20, 20) };

        // Name + params badge
        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        headerPanel.Children.Add(new TextBlock
        {
            Text = model.Name,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center
        });
        headerPanel.Children.Add(new Border
        {
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6),
            Padding = new Microsoft.UI.Xaml.Thickness(8, 3, 8, 3),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(20, 89, 70, 210)),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = $"{model.ParametersB:F1}B",
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 89, 70, 210))
            }
        });
        panel.Children.Add(headerPanel);

        // Description
        panel.Children.Add(new TextBlock
        {
            Text = model.Description,
            FontSize = 13,
            Opacity = 0.65,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2,
            LineHeight = 20
        });

        // Categories as chips
        var catPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        foreach (var cat in model.Categories)
        {
            var (chipColor, chipBg) = GetCategoryAccent(cat);
            catPanel.Children.Add(new Border
            {
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                Padding = new Microsoft.UI.Xaml.Thickness(10, 3, 10, 3),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(chipBg),
                Child = new TextBlock
                {
                    Text = cat,
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(chipColor)
                }
            });
        }
        panel.Children.Add(catPanel);

        // Divider
        panel.Children.Add(new Border
        {
            Height = 1,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(15, 128, 128, 128)),
            Margin = new Microsoft.UI.Xaml.Thickness(0, 2, 0, 2)
        });

        // Bottom info row: quant range + HF ID
        var infoPanel = new StackPanel { Spacing = 4 };

        var quantText = model.SupportedQuants.Count > 0
            ? $"Quants: {model.SupportedQuants.First()} — {model.SupportedQuants.Last()}"
            : "Quants: Q2_K — F16";

        var quantRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        quantRow.Children.Add(new FontIcon
        {
            Glyph = "\uE943",
            FontSize = 12,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),
            Opacity = 0.4,
            VerticalAlignment = VerticalAlignment.Center
        });
        quantRow.Children.Add(new TextBlock { Text = quantText, FontSize = 11, Opacity = 0.5 });
        infoPanel.Children.Add(quantRow);

        if (!string.IsNullOrEmpty(model.HuggingFaceId))
        {
            var hfRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            hfRow.Children.Add(new FontIcon
            {
                Glyph = "\uE71B",
                FontSize = 12,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),
                Opacity = 0.4,
                VerticalAlignment = VerticalAlignment.Center
            });
            hfRow.Children.Add(new TextBlock
            {
                Text = model.HuggingFaceId,
                FontSize = 11,
                Opacity = 0.4,
                IsTextSelectionEnabled = true,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            infoPanel.Children.Add(hfRow);
        }

        panel.Children.Add(infoPanel);

        outerPanel.Children.Add(panel);
        border.Child = outerPanel;
        return border;
    }

    private static (Color accent, Color background) GetCategoryAccent(string category) => category switch
    {
        "Coding" => (Color.FromArgb(255, 0, 120, 215), Color.FromArgb(30, 0, 150, 255)),
        "Reasoning" => (Color.FromArgb(255, 200, 120, 0), Color.FromArgb(30, 255, 150, 0)),
        "General Chat" => (Color.FromArgb(255, 56, 142, 60), Color.FromArgb(30, 76, 175, 80)),
        "Small & Fast" => (Color.FromArgb(255, 200, 150, 0), Color.FromArgb(30, 255, 193, 7)),
        "Multimodal" => (Color.FromArgb(255, 142, 36, 170), Color.FromArgb(30, 156, 39, 176)),
        _ => (Color.FromArgb(255, 128, 128, 128), Color.FromArgb(20, 128, 128, 128))
    };

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.SearchQuery = SearchBox.Text;
        BindModels();
    }

    private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryComboBox.SelectedItem is string cat)
        {
            ViewModel.SelectedCategory = cat;
            BindModels();
        }
    }
}
