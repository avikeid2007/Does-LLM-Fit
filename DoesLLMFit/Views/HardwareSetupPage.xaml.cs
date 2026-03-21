using DoesLLMFit.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DoesLLMFit.Views;

public sealed partial class HardwareSetupPage : Page
{
    private HardwareSetupViewModel ViewModel { get; }

    public HardwareSetupPage()
    {
        ViewModel = App.Services.GetRequiredService<HardwareSetupViewModel>();
        this.InitializeComponent();
        Loaded += async (_, _) => await InitializePageAsync();
    }

    private async Task InitializePageAsync()
    {
        await ViewModel.InitializeAsync();
        GpuComboBox.ItemsSource = ViewModel.GpuNames;
    }

    private void Architecture_Changed(object sender, RoutedEventArgs e)
    {
        bool isApple = AppleSiliconRadio.IsChecked == true;
        ViewModel.IsAppleSilicon = isApple;
        UnifiedMemoryPanel.Visibility = isApple ? Visibility.Visible : Visibility.Collapsed;
        GpuComboBox.ItemsSource = ViewModel.GpuNames;
        GpuComboBox.SelectedItem = null;
    }

    private void GpuComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GpuComboBox.SelectedItem is string gpuName)
        {
            ViewModel.SelectedGpuName = gpuName;
            VramBox.Value = ViewModel.Hardware.VramGb;
            BandwidthBox.Value = ViewModel.Hardware.MemoryBandwidthGBs;
            RamBox.Value = ViewModel.Hardware.SystemRamGb;
        }
    }

    private void VramBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!double.IsNaN(args.NewValue))
            ViewModel.Hardware.VramGb = args.NewValue;
    }

    private void RamBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!double.IsNaN(args.NewValue))
            ViewModel.Hardware.SystemRamGb = args.NewValue;
    }

    private void BandwidthBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!double.IsNaN(args.NewValue))
            ViewModel.Hardware.MemoryBandwidthGBs = args.NewValue;
    }

    private void UnifiedMemorySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        ViewModel.Hardware.UnifiedMemoryPercent = (int)e.NewValue;
        if (UnifiedMemoryPercentText is not null)
            UnifiedMemoryPercentText.Text = $" ({(int)e.NewValue}%)";
    }

    private void ContextComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ContextComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tagStr && int.TryParse(tagStr, out int ctx))
        {
            ViewModel.SelectedContextLength = ctx;
        }
    }

    private void CheckButton_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to Results page with the hardware profile
        var frame = this.Frame;
        if (frame?.Parent is Microsoft.UI.Xaml.Controls.NavigationView navView)
        {
            // Select the "Results" nav item
            foreach (var menuItem in navView.MenuItems)
            {
                if (menuItem is Microsoft.UI.Xaml.Controls.NavigationViewItem nvi && nvi.Tag?.ToString() == "Results")
                {
                    navView.SelectedItem = nvi;
                    break;
                }
            }
        }

        // Pass hardware to Results
        frame?.Navigate(typeof(ResultsPage), ViewModel.Hardware);
    }
}
