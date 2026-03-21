using DoesLLMFit.Models;
using DoesLLMFit.Services;
using DoesLLMFit.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace DoesLLMFit;

public sealed partial class MainPage : Page
{
    private readonly ModelCatalogService _catalog;
    private readonly CompatibilityCalculator _calculator;
    private readonly HardwareProfile _hardware = new();
    private bool _initialized;
    private bool _uiReady;

    public MainPage()
    {
        _catalog = App.Services.GetRequiredService<ModelCatalogService>();
        _calculator = App.Services.GetRequiredService<CompatibilityCalculator>();
        this.InitializeComponent();
        _uiReady = true;
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;

        await _catalog.InitializeAsync();
        UpdateGpuList();
    }

    // ─── GPU Brand Icons ─────────────────────────────

    private const string NvidiaPathData = "M8.948 8.798v-1.43a6.7 6.7 0 0 1 .424-.018c3.922-.124 6.493 3.374 6.493 3.374s-2.774 3.851-5.75 3.851c-.398 0-.787-.062-1.158-.185v-4.346c1.528.185 1.837.857 2.747 2.385l2.04-1.714s-1.492-1.952-4-1.952a6.016 6.016 0 0 0-.796.035m0-4.735v2.138l.424-.027c5.45-.185 9.01 4.47 9.01 4.47s-4.08 4.964-8.33 4.964c-.37 0-.733-.035-1.095-.097v1.325c.3.035.61.062.91.062 3.957 0 6.82-2.023 9.593-4.408.459.371 2.34 1.263 2.73 1.652-2.633 2.208-8.772 3.984-12.253 3.984-.335 0-.653-.018-.971-.053v1.864H24V4.063zm0 10.326v1.131c-3.657-.654-4.673-4.46-4.673-4.46s1.758-1.944 4.673-2.262v1.237H8.94c-1.528-.186-2.73 1.245-2.73 1.245s.68 2.412 2.739 3.11M2.456 10.9s2.164-3.197 6.5-3.533V6.201C4.153 6.59 0 10.653 0 10.653s2.35 6.802 8.948 7.42v-1.237c-4.84-.6-6.492-5.936-6.492-5.936z";
    private const string IntelPathData = "M9.427 14.401v5.167h-1.646v-6.495h3.396c1.443 0 1.932 1.021 1.932 1.943v4.552h-1.641v-4.542c0-0.391-0.198-0.625-0.682-0.625zM20.615 14.323c-0.568 0-1 0.286-1.182 0.682-0.104 0.219-0.156 0.458-0.156 0.703h2.531c-0.031-0.703-0.354-1.385-1.193-1.385zM19.276 16.828c0 0.839 0.521 1.464 1.458 1.464 0.724 0 1.083-0.203 1.505-0.625l1.016 0.974c-0.646 0.641-1.333 1.031-2.536 1.031-1.573 0-3.078-0.859-3.078-3.359 0-2.141 1.313-3.349 3.042-3.349 1.755 0 2.766 1.417 2.766 3.271v0.589h-4.172zM16.25 19.557c-1.339 0-1.906-0.932-1.906-1.854v-6.401h1.641v1.771h1.234v1.328h-1.234v3.198c0 0.38 0.177 0.589 0.568 0.589h0.667v1.37zM6.318 12.177h-1.656v-1.578h1.656zM6.323 19.635c-1.24-0.12-1.661-0.87-1.661-1.74v-4.823h1.656v6.568zM26.063 19.495c-1.24-0.12-1.656-0.87-1.656-1.734v-7.38h1.656v9.12zM31.859 11.448c-1.5-7.328-15.724-7.792-24.885-2.214v0.62c9.151-4.708 22.141-4.677 23.323 2.063 0.391 2.234-0.865 4.557-3.109 5.896v1.75c2.703-0.99 5.474-4.198 4.672-8.115zM15.198 24.26c-6.323 0.583-12.917-0.339-13.839-5.276-0.448-2.438 0.667-5.021 2.13-6.625v-0.854c-2.646 2.323-4.083 5.266-3.255 8.74 1.057 4.458 6.714 6.984 15.344 6.146 3.417-0.333 7.891-1.432 10.995-3.141v-2.422c-2.818 1.682-7.49 3.073-11.375 3.432zM27.979 10.865c0-0.078-0.052-0.104-0.156-0.104h-0.104v0.229h0.104c0.104 0 0.156-0.031 0.156-0.109zM28.141 11.432h-0.125c-0.01 0-0.021-0.005-0.026-0.016l-0.167-0.286c-0.005-0.005-0.016-0.01-0.026-0.01h-0.073v0.281c0 0.016-0.016 0.031-0.031 0.031h-0.109c-0.016 0-0.031-0.016-0.031-0.031v-0.714c0-0.036 0.021-0.057 0.052-0.063 0.068-0.005 0.135-0.005 0.203-0.005 0.203 0 0.328 0.057 0.328 0.25v0.01c0 0.12-0.063 0.182-0.151 0.214l0.172 0.292c0 0.005 0.005 0.016 0.005 0.021 0.005 0.01-0.005 0.026-0.021 0.026zM27.849 10.484c-0.302 0-0.547 0.245-0.547 0.547 0.005 0.302 0.25 0.547 0.552 0.547 0.297 0 0.542-0.245 0.542-0.542 0-0.302-0.245-0.552-0.547-0.552zM27.849 11.693c-0.365 0-0.661-0.292-0.661-0.656s0.297-0.661 0.661-0.661c0.359 0 0.661 0.297 0.661 0.661s-0.302 0.656-0.661 0.656z";
    private const string AmdPathData = "M33.614,33.614 L42.864,42.864 L42.864,5.864 L5.864,5.864 L15.114,15.114 L33.614,15.114 Z M15.114,33.614 L15.114,19.55 L5.885,28.778 L5.864,42.864 L19.949,42.842 L29.177,33.614 Z";
    private const string ApplePathData = "M18.71 19.5C17.88 20.74 17 21.95 15.66 21.97C14.32 22 13.89 21.18 12.37 21.18C10.84 21.18 10.37 21.95 9.09997 22C7.78997 22.05 6.79997 20.68 5.95997 19.47C4.24997 17 2.93997 12.45 4.69997 9.39C5.56997 7.87 7.12997 6.91 8.81997 6.88C10.1 6.86 11.32 7.75 12.11 7.75C12.89 7.75 14.37 6.68 15.92 6.84C16.57 6.87 18.39 7.1 19.56 8.82C19.47 8.88 17.39 10.1 17.41 12.63C17.44 15.65 20.06 16.66 20.09 16.67C20.06 16.74 19.67 18.11 18.71 19.5ZM13 3.5C13.73 2.67 14.94 2.04 15.94 2C16.07 3.17 15.6 4.35 14.9 5.19C14.21 6.04 13.07 6.7 11.95 6.61C11.8 5.46 12.36 4.26 13 3.5Z";

    private static (string pathData, double viewboxSize, Color brandColor) GetGpuBrandInfo(string gpuName)
    {
        if (gpuName.StartsWith("RTX", StringComparison.OrdinalIgnoreCase) ||
            gpuName.StartsWith("GTX", StringComparison.OrdinalIgnoreCase) ||
            gpuName.Contains("GeForce", StringComparison.OrdinalIgnoreCase))
            return (NvidiaPathData, 24, Color.FromArgb(255, 118, 185, 0));   // NVIDIA green

        if (gpuName.StartsWith("RX", StringComparison.OrdinalIgnoreCase) ||
            gpuName.Contains("Radeon", StringComparison.OrdinalIgnoreCase))
            return (AmdPathData, 48, Color.FromArgb(255, 237, 28, 36));      // AMD red

        if (gpuName.StartsWith("Intel", StringComparison.OrdinalIgnoreCase) ||
            gpuName.StartsWith("Arc", StringComparison.OrdinalIgnoreCase))
            return (IntelPathData, 32, Color.FromArgb(255, 0, 113, 197));    // Intel blue

        if (gpuName.StartsWith("Apple", StringComparison.OrdinalIgnoreCase))
            return (ApplePathData, 24, Color.FromArgb(255, 162, 170, 173));  // Apple silver

        return ("", 0, Color.FromArgb(255, 144, 160, 176));                  // Unknown
    }

    private void UpdateGpuList()
    {
        var arch = _hardware.Architecture == ArchitectureType.AppleSilicon ? "AppleSilicon" : "PC";
        var gpuNames = _catalog.GetGpusByArchitecture(arch).Select(g => g.Name).ToList();
        GpuComboBox.Items.Clear();
        foreach (var name in gpuNames)
        {
            var (pathData, vbSize, brandColor) = GetGpuBrandInfo(name);

            var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

            if (!string.IsNullOrEmpty(pathData))
            {
                var viewbox = new Viewbox { Width = 18, Height = 18, VerticalAlignment = VerticalAlignment.Center };
                var canvas = new Canvas { Width = vbSize, Height = vbSize };
                var icon = (Microsoft.UI.Xaml.Shapes.Path)XamlReader.Load(
                    $"<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Data='{pathData}' />");
                icon.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(brandColor);
                canvas.Children.Add(icon);
                viewbox.Child = canvas;
                sp.Children.Add(viewbox);
            }

            sp.Children.Add(new TextBlock
            {
                Text = name,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(230, 232, 237, 243))
            });

            GpuComboBox.Items.Add(new ComboBoxItem { Content = sp, Tag = name });
        }
        GpuComboBox.SelectedItem = null;
    }

    // ─── Event Handlers ───────────────────────────────

    private void RamBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!_uiReady || double.IsNaN(args.NewValue)) return;
        _hardware.SystemRamGb = args.NewValue;
        RamDisplayText.Text = ((int)args.NewValue).ToString();
    }

    private void VramSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_uiReady) return;
        _hardware.VramGb = e.NewValue;
        var vramInt = (int)e.NewValue;
        VramDisplayText.Text = vramInt.ToString();
        VramLabelText.Text = $"{vramInt} GB VRAM";
    }

    private void BandwidthBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!_uiReady || double.IsNaN(args.NewValue)) return;
        _hardware.MemoryBandwidthGBs = args.NewValue;
    }

    private void GpuComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var gpuName = (GpuComboBox.SelectedItem as ComboBoxItem)?.Tag as string;
        if (gpuName is not null)
        {
            _hardware.GpuName = gpuName;
            var gpu = _catalog.FindGpu(gpuName);
            if (gpu is not null)
            {
                _hardware.VramGb = gpu.VramGb;
                _hardware.MemoryBandwidthGBs = gpu.BandwidthGBs;

                VramSlider.Value = gpu.VramGb;
                BandwidthBox.Value = gpu.BandwidthGBs;

                if (_hardware.Architecture == ArchitectureType.AppleSilicon)
                {
                    _hardware.SystemRamGb = gpu.VramGb;
                    RamBox.Value = gpu.VramGb;
                }
            }
        }
    }

    private void AppleSiliconToggle_Toggled(object sender, RoutedEventArgs e)
    {
        bool isApple = AppleSiliconToggle.IsOn;
        _hardware.Architecture = isApple ? ArchitectureType.AppleSilicon : ArchitectureType.PC;

        VramTypeText.Text = isApple ? "Unified Memory" : "Dedicated GPU VRAM";
        UnifiedMemoryPanel.Visibility = isApple ? Visibility.Visible : Visibility.Collapsed;
        UnifiedMemoryColumn.Width = isApple
            ? new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star)
            : new Microsoft.UI.Xaml.GridLength(0);

        UpdateGpuList();
    }

    private void ContextComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ContextComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tagStr && int.TryParse(tagStr, out int ctx))
        {
            _hardware.ContextLength = ctx;
        }
    }

    private void UnifiedMemorySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_uiReady) return;
        _hardware.UnifiedMemoryPercent = (int)e.NewValue;
        UnifiedMemoryPercentText.Text = $"({(int)e.NewValue}%)";
    }

    // ─── Run Evaluation ───────────────────────────

    private void CheckButton_Click(object sender, RoutedEventArgs e)
    {
        var models = _catalog.Models;
        if (models.Count == 0) return;

        var results = models.Select(m => _calculator.EvaluateModel(m, _hardware)).ToList();

        int green = results.Count(r => r.OverallStatus == FitStatus.Green);
        int yellow = results.Count(r => r.OverallStatus == FitStatus.Yellow);
        int red = results.Count(r => r.OverallStatus == FitStatus.Red);

        GreenCountText.Text = green.ToString();
        YellowCountText.Text = yellow.ToString();
        RedCountText.Text = red.ToString();

        SummaryPanel.Visibility = Visibility.Visible;
        EmptyHint.Visibility = Visibility.Collapsed;
        ModelCardsScroller.Visibility = Visibility.Visible;
        ResultsHeader.Visibility = Visibility.Visible;
        ResultsCountText.Text = $"({results.Count} models evaluated)";

        // Build model cards — sorted: green first, then yellow, then red
        ModelCardsPanel.Children.Clear();
        var sorted = results
            .OrderBy(r => r.OverallStatus)
            .ThenByDescending(r => r.BestFit?.EstimatedToksPerSec ?? 0)
            .ToList();

        foreach (var summary in sorted)
        {
            ModelCardsPanel.Children.Add(CreateModelCard(summary, _hardware));
        }
    }

    // ─── Model Card Builder ─────────────────────────

    private static Border CreateModelCard(ModelCompatibilitySummary summary, HardwareProfile hw)
    {
        var statusColor = summary.OverallStatus switch
        {
            FitStatus.Green => Color.FromArgb(255, 76, 175, 80),
            FitStatus.Yellow => Color.FromArgb(255, 255, 193, 7),
            FitStatus.Red => Color.FromArgb(255, 244, 67, 54),
            _ => Color.FromArgb(255, 128, 128, 128)
        };

        var cardBg = summary.OverallStatus switch
        {
            FitStatus.Green => Color.FromArgb(255, 14, 36, 28),
            FitStatus.Yellow => Color.FromArgb(255, 38, 33, 14),
            FitStatus.Red => Color.FromArgb(255, 40, 18, 20),
            _ => Color.FromArgb(255, 21, 29, 43)
        };

        // Outer wrapper with accent bar at top
        var outerCard = new Border
        {
            Width = 280,
            MinHeight = 240,
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(18),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(cardBg),
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(50, statusColor.R, statusColor.G, statusColor.B)),
            BorderThickness = new Microsoft.UI.Xaml.Thickness(1)
        };

        // Inner grid — accent bar + content
        var innerGrid = new Grid();
        innerGrid.RowDefinitions.Add(new RowDefinition { Height = new Microsoft.UI.Xaml.GridLength(4) });
        innerGrid.RowDefinitions.Add(new RowDefinition { Height = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star) });

        // Top accent strip
        var accentBar = new Border
        {
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(18, 18, 0, 0),
            Background = new Microsoft.UI.Xaml.Media.LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 0),
                GradientStops =
                {
                    new Microsoft.UI.Xaml.Media.GradientStop { Color = Color.FromArgb(0, statusColor.R, statusColor.G, statusColor.B), Offset = 0.0 },
                    new Microsoft.UI.Xaml.Media.GradientStop { Color = Color.FromArgb(180, statusColor.R, statusColor.G, statusColor.B), Offset = 0.5 },
                    new Microsoft.UI.Xaml.Media.GradientStop { Color = Color.FromArgb(0, statusColor.R, statusColor.G, statusColor.B), Offset = 1.0 }
                }
            }
        };
        Grid.SetRow(accentBar, 0);
        innerGrid.Children.Add(accentBar);

        var panel = new StackPanel
        {
            Spacing = 10,
            Padding = new Microsoft.UI.Xaml.Thickness(22, 18, 22, 20)
        };
        Grid.SetRow(panel, 1);

        // ── Row 1: Status badge + model name ──
        var headerRow = new Grid();
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = Microsoft.UI.Xaml.GridLength.Auto });

        var nameBlock = new TextBlock
        {
            Text = summary.Model.Name,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(240, 232, 237, 243)),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(nameBlock, 0);
        headerRow.Children.Add(nameBlock);

        // Status badge
        var statusLabel = summary.OverallStatus switch
        {
            FitStatus.Green => "\uE73E",   // checkmark
            FitStatus.Yellow => "\uE7BA",  // warning
            _ => "\uE711"                  // X
        };
        var statusBadge = new Border
        {
            Width = 28, Height = 28,
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(14),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(50, statusColor.R, statusColor.G, statusColor.B)),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new FontIcon
            {
                Glyph = statusLabel,
                FontSize = 13,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetColumn(statusBadge, 1);
        headerRow.Children.Add(statusBadge);
        panel.Children.Add(headerRow);

        // ── Row 2: Icon + Parameter count ──
        var paramRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        var brainCircle = new Border
        {
            Width = 42, Height = 42,
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(21),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(18, 68, 164, 255)),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new FontIcon
            {
                Glyph = "\uE945",
                FontSize = 20,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 68, 164, 255)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        paramRow.Children.Add(brainCircle);

        var paramStack = new StackPanel { Spacing = 0, VerticalAlignment = VerticalAlignment.Center };
        // Smart formatting: show decimal for values < 10 (e.g. 0.5B, 1.5B, 3.8B), whole number for >= 10
        var paramText = summary.Model.ParametersB < 10
            ? $"{summary.Model.ParametersB:G3}B"
            : $"{summary.Model.ParametersB:F0}B";
        paramStack.Children.Add(new TextBlock
        {
            Text = paramText,
            FontSize = 28,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 232, 237, 243))
        });
        paramStack.Children.Add(new TextBlock
        {
            Text = "Parameters",
            FontSize = 11,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(100, 160, 180, 200))
        });
        paramRow.Children.Add(paramStack);
        panel.Children.Add(paramRow);

        // ── Row 3: VRAM bar with percentage ──
        var bestQuant = summary.BestFit;
        if (bestQuant is not null)
        {
            double availableVram = hw.EffectiveVramGb;
            double usedVram = bestQuant.EstimatedVramGb;
            double pct = availableVram > 0 ? Math.Min(usedVram / availableVram, 1.0) : 1.0;

            var vramSection = new StackPanel { Spacing = 5 };
            // Label row
            var vramLabelRow = new Grid();
            vramLabelRow.Children.Add(new TextBlock
            {
                Text = $"VRAM: ~{usedVram:F1} GB",
                FontSize = 11,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(140, 160, 180, 200)),
                HorizontalAlignment = HorizontalAlignment.Left
            });
            vramLabelRow.Children.Add(new TextBlock
            {
                Text = $"{pct * 100:F0}%",
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusColor),
                HorizontalAlignment = HorizontalAlignment.Right
            });
            vramSection.Children.Add(vramLabelRow);

            // Progress bar
            var barTrack = new Border
            {
                Height = 6,
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(3),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(30, 255, 255, 255))
            };
            var barGrid = new Grid { Height = 6 };
            barGrid.Children.Add(barTrack);
            var barFill = new Border
            {
                Height = 6,
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(3),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusColor)
            };
            // Set width by binding to parent via a SizeChanged trick
            barGrid.SizeChanged += (s, args) =>
            {
                barFill.Width = args.NewSize.Width * pct;
            };
            barGrid.Children.Add(barFill);
            vramSection.Children.Add(barGrid);

            panel.Children.Add(vramSection);
        }

        // ── Row 4: tok/s display ──
        if (bestQuant?.EstimatedToksPerSec is { } toks)
        {
            var toksRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            toksRow.Children.Add(new Ellipse
            {
                Width = 8, Height = 8,
                Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusColor),
                VerticalAlignment = VerticalAlignment.Center
            });
            toksRow.Children.Add(new TextBlock
            {
                Text = $"~{toks:F0} tok/s",
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusColor),
                VerticalAlignment = VerticalAlignment.Center
            });
            // Quant name pill
            toksRow.Children.Add(new Border
            {
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6),
                Padding = new Microsoft.UI.Xaml.Thickness(8, 2, 8, 2),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = bestQuant.QuantDisplayName,
                    FontSize = 10,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(160, 200, 210, 220))
                }
            });
            panel.Children.Add(toksRow);
        }

        // ── Row 5: Separator ──
        panel.Children.Add(new Border
        {
            Height = 1,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
            Margin = new Microsoft.UI.Xaml.Thickness(0, 2, 0, 2)
        });

        // ── Row 6: Category badge with icon ──
        var primaryCat = summary.Model.Categories.FirstOrDefault() ?? "General";
        var (catLabel, catIcon, catColor) = primaryCat switch
        {
            "General Chat" => ("General Chat", "\uE8BD", Color.FromArgb(255, 68, 164, 255)),
            "Coding" => ("Developer", "\uE943", Color.FromArgb(255, 130, 180, 80)),
            "Reasoning" => ("Reasoning", "\uEA80", Color.FromArgb(255, 180, 130, 255)),
            "Small & Fast" => ("Lightweight", "\uE916", Color.FromArgb(255, 255, 180, 60)),
            "Multimodal" => ("Multimodal", "\uE8B9", Color.FromArgb(255, 255, 120, 150)),
            _ => (primaryCat, "\uE8F1", Color.FromArgb(255, 180, 180, 180))
        };
        var catBadge = new Border
        {
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(10),
            Padding = new Microsoft.UI.Xaml.Thickness(10, 4, 14, 4),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(15, catColor.R, catColor.G, catColor.B)),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var catRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        catRow.Children.Add(new FontIcon
        {
            Glyph = catIcon,
            FontSize = 12,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(catColor),
            VerticalAlignment = VerticalAlignment.Center
        });
        catRow.Children.Add(new TextBlock
        {
            Text = catLabel,
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(catColor)
        });
        catBadge.Child = catRow;
        panel.Children.Add(catBadge);

        innerGrid.Children.Add(panel);
        outerCard.Child = innerGrid;
        return outerCard;
    }
}
