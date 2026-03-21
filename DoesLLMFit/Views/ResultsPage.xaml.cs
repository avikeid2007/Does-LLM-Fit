using DoesLLMFit.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace DoesLLMFit.Views;

public sealed partial class ResultsPage : Page
{
    private ResultsViewModel ViewModel { get; }

    public ResultsPage()
    {
        ViewModel = App.Services.GetRequiredService<ResultsViewModel>();
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is HardwareProfile hardware)
        {
            ViewModel.Evaluate(hardware);
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        var hw = ViewModel.Hardware;
        HardwareSummaryText.Text = hw.Architecture == ArchitectureType.AppleSilicon
            ? $"{hw.GpuName} · {hw.VramGb:F0} GB Unified ({hw.UnifiedMemoryPercent}%) · {hw.MemoryBandwidthGBs:F0} GB/s · {hw.ContextLength:N0} ctx"
            : $"{hw.GpuName} · {hw.VramGb:F0} GB VRAM · {hw.SystemRamGb:F0} GB RAM · {hw.MemoryBandwidthGBs:F0} GB/s · {hw.ContextLength:N0} ctx";

        GreenCountText.Text = ViewModel.GreenCount.ToString();
        YellowCountText.Text = ViewModel.YellowCount.ToString();
        RedCountText.Text = ViewModel.RedCount.ToString();

        CategoryFilter.ItemsSource = ViewModel.Categories;
        CategoryFilter.SelectedIndex = 0;

        BindResults();
        EmptyState.Visibility = ViewModel.FilteredResults.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void BindResults()
    {
        ResultsPanel.Children.Clear();
        foreach (var summary in ViewModel.FilteredResults)
        {
            var card = CreateResultCard(summary);
            ResultsPanel.Children.Add(card);
        }
        EmptyState.Visibility = ViewModel.FilteredResults.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private Border CreateResultCard(ModelCompatibilitySummary summary)
    {
        var statusColor = summary.OverallStatus switch
        {
            FitStatus.Green => Color.FromArgb(255, 76, 175, 80),
            FitStatus.Yellow => Color.FromArgb(255, 255, 193, 7),
            FitStatus.Red => Color.FromArgb(255, 244, 67, 54),
            _ => Color.FromArgb(255, 128, 128, 128)
        };
        var statusBgColor = summary.OverallStatus switch
        {
            FitStatus.Green => Color.FromArgb(12, 76, 175, 80),
            FitStatus.Yellow => Color.FromArgb(12, 255, 193, 7),
            FitStatus.Red => Color.FromArgb(12, 244, 67, 54),
            _ => Color.FromArgb(8, 128, 128, 128)
        };
        var statusBorderColor = summary.OverallStatus switch
        {
            FitStatus.Green => Color.FromArgb(40, 76, 175, 80),
            FitStatus.Yellow => Color.FromArgb(40, 255, 193, 7),
            FitStatus.Red => Color.FromArgb(40, 244, 67, 54),
            _ => Color.FromArgb(25, 128, 128, 128)
        };

        // Outer card
        var card = new Border
        {
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(14),
            Padding = new Microsoft.UI.Xaml.Thickness(0),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusBgColor),
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusBorderColor),
            BorderThickness = new Microsoft.UI.Xaml.Thickness(1),
            Tag = summary
        };
        card.Tapped += ResultCard_Tapped;

        var outerGrid = new Grid();
        outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
        outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left status accent strip
        var statusStrip = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusColor),
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(14, 0, 0, 14),
            Width = 5
        };
        Grid.SetColumn(statusStrip, 0);
        outerGrid.Children.Add(statusStrip);

        // Content area
        var contentGrid = new Grid
        {
            Padding = new Microsoft.UI.Xaml.Thickness(16, 14, 16, 14),
            ColumnSpacing = 16,
            RowSpacing = 8
        };
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetColumn(contentGrid, 1);

        // Row 0: Model name + params badge + status label
        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        headerPanel.Children.Add(new TextBlock
        {
            Text = summary.Model.Name,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 15
        });
        // Params badge
        headerPanel.Children.Add(new Border
        {
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6),
            Padding = new Microsoft.UI.Xaml.Thickness(8, 2, 8, 2),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(25, 89, 70, 210)),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = $"{summary.Model.ParametersB:F1}B",
                FontSize = 11,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 89, 70, 210))
            }
        });
        // Category chips
        foreach (var cat in summary.Model.Categories.Take(2))
        {
            headerPanel.Children.Add(new Border
            {
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6),
                Padding = new Microsoft.UI.Xaml.Thickness(8, 2, 8, 2),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(15, 128, 128, 128)),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = cat,
                    FontSize = 11,
                    Opacity = 0.6
                }
            });
        }
        Grid.SetRow(headerPanel, 0);
        Grid.SetColumn(headerPanel, 0);
        contentGrid.Children.Add(headerPanel);

        // Status badge (top right)
        var statusBadge = new Border
        {
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
            Padding = new Microsoft.UI.Xaml.Thickness(12, 4, 12, 4),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(20, statusColor.R, statusColor.G, statusColor.B)),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Child = new TextBlock
            {
                Text = summary.OverallStatus switch
                {
                    FitStatus.Green => "✓ Runs Great",
                    FitStatus.Yellow => "⚠ Tight Fit",
                    FitStatus.Red => "✗ Won't Fit",
                    _ => ""
                },
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusColor)
            }
        };
        Grid.SetRow(statusBadge, 0);
        Grid.SetColumn(statusBadge, 1);
        contentGrid.Children.Add(statusBadge);

        // Row 1: Stats row with VRAM bar
        var statsGrid = new Grid { ColumnSpacing = 20 };
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetRow(statsGrid, 1);
        Grid.SetColumnSpan(statsGrid, 2);

        // VRAM usage bar
        if (summary.BestFit is not null)
        {
            var vramRatio = Math.Min(summary.BestFit.EstimatedVramGb / summary.BestFit.AvailableVramGb, 1.5);
            var barPanel = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center, MinWidth = 140 };
            barPanel.Children.Add(new TextBlock
            {
                Text = $"{summary.BestFit.EstimatedVramGb:F1} / {summary.BestFit.AvailableVramGb:F1} GB VRAM",
                FontSize = 11,
                Opacity = 0.6
            });
            var barBg = new Border
            {
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(3),
                Height = 6,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(20, 128, 128, 128))
            };
            var barFill = new Border
            {
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(3),
                Height = 6,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusColor),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = Math.Max(4, Math.Min(140, 140 * vramRatio))
            };
            var barContainer = new Grid { Height = 6 };
            barContainer.Children.Add(barBg);
            barContainer.Children.Add(barFill);
            barPanel.Children.Add(barContainer);
            Grid.SetColumn(barPanel, 0);
            statsGrid.Children.Add(barPanel);
        }

        // Stat: Best Quant
        var quantStat = CreateStatBlock(summary.BestFit?.QuantDisplayName ?? "—", "Best Quant");
        Grid.SetColumn(quantStat, 1);
        statsGrid.Children.Add(quantStat);

        // Stat: VRAM
        var vramStat = CreateStatBlock(
            summary.BestFit is not null ? $"{summary.BestFit.EstimatedVramGb:F1} GB" : "—", "VRAM");
        Grid.SetColumn(vramStat, 2);
        statsGrid.Children.Add(vramStat);

        // Stat: tok/s
        var toksStat = CreateStatBlock(
            summary.BestFit?.EstimatedToksPerSec is { } t ? $"~{t:F0}" : "—", "tok/s");
        Grid.SetColumn(toksStat, 3);
        statsGrid.Children.Add(toksStat);

        contentGrid.Children.Add(statsGrid);

        outerGrid.Children.Add(contentGrid);
        card.Child = outerGrid;
        return card;
    }

    private static StackPanel CreateStatBlock(string value, string label)
    {
        var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, MinWidth = 60 };
        panel.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 10,
            Opacity = 0.5,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        return panel;
    }

    private void FilterStatus_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string filter)
        {
            ViewModel.ActiveFilter = filter;
            BindResults();
        }
    }

    private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryFilter.SelectedItem is string cat)
        {
            ViewModel.ActiveCategoryFilter = cat;
            BindResults();
        }
    }

    private async void ResultCard_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is ModelCompatibilitySummary summary)
        {
            await ShowQuantDetailDialog(summary);
        }
    }

    private async Task ShowQuantDetailDialog(ModelCompatibilitySummary summary)
    {
        var dialog = new ContentDialog
        {
            Title = $"{summary.Model.Name} — All Quantizations",
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot
        };

        var panel = new StackPanel { Spacing = 4, MinWidth = 480 };

        // Description
        panel.Children.Add(new TextBlock
        {
            Text = summary.Model.Description,
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7,
            Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 12)
        });

        // Column headers
        var headerRow = new Grid { ColumnSpacing = 12, Padding = new Microsoft.UI.Xaml.Thickness(8, 0, 8, 8) };
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        AddHeaderCell(headerRow, "", 0);
        AddHeaderCell(headerRow, "Quant", 1);
        AddHeaderCell(headerRow, "VRAM", 2);
        AddHeaderCell(headerRow, "tok/s", 3);
        AddHeaderCell(headerRow, "Status", 4);
        panel.Children.Add(headerRow);

        foreach (var result in summary.AllQuants)
        {
            var color = result.Status switch
            {
                FitStatus.Green => Color.FromArgb(255, 76, 175, 80),
                FitStatus.Yellow => Color.FromArgb(255, 255, 193, 7),
                FitStatus.Red => Color.FromArgb(255, 244, 67, 54),
                _ => Color.FromArgb(255, 128, 128, 128)
            };
            var rowBg = result.Status switch
            {
                FitStatus.Green => Color.FromArgb(8, 76, 175, 80),
                FitStatus.Yellow => Color.FromArgb(8, 255, 193, 7),
                FitStatus.Red => Color.FromArgb(8, 244, 67, 54),
                _ => Color.FromArgb(0, 0, 0, 0)
            };

            var rowBorder = new Border
            {
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(rowBg),
                Padding = new Microsoft.UI.Xaml.Thickness(8, 6, 8, 6),
                Margin = new Microsoft.UI.Xaml.Thickness(0, 1, 0, 1)
            };

            var row = new Grid { ColumnSpacing = 12 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var dot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(color),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(dot, 0);
            row.Children.Add(dot);

            var quantText = new TextBlock
            {
                Text = result.QuantDisplayName,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(quantText, 1);
            row.Children.Add(quantText);

            var vramText = new TextBlock
            {
                Text = $"{result.EstimatedVramGb:F1} GB",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(vramText, 2);
            row.Children.Add(vramText);

            var toksText = new TextBlock
            {
                Text = result.EstimatedToksPerSec is { } tok ? $"~{tok:F0}" : "—",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(toksText, 3);
            row.Children.Add(toksText);

            var statusText = new TextBlock
            {
                Text = result.Status switch
                {
                    FitStatus.Green => "Runs Great",
                    FitStatus.Yellow => "Tight Fit",
                    FitStatus.Red => "Won't Fit",
                    _ => ""
                },
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(color),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(statusText, 4);
            row.Children.Add(statusText);

            rowBorder.Child = row;
            panel.Children.Add(rowBorder);
        }

        dialog.Content = new ScrollViewer { Content = panel, MaxHeight = 450 };
        await dialog.ShowAsync();
    }

    private static void AddHeaderCell(Grid row, string text, int col)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Opacity = 0.5,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(tb, col);
        row.Children.Add(tb);
    }
}
