using DoesLLMFit.Services;

namespace DoesLLMFit.ViewModels;

public partial class ResultsViewModel : ObservableObject
{
    private readonly CompatibilityCalculator _calculator;
    private readonly ModelCatalogService _catalogService;

    [ObservableProperty]
    private HardwareProfile _hardware = new();

    [ObservableProperty]
    private IReadOnlyList<ModelCompatibilitySummary> _allResults = [];

    [ObservableProperty]
    private IReadOnlyList<ModelCompatibilitySummary> _filteredResults = [];

    [ObservableProperty]
    private string _activeFilter = "All";

    [ObservableProperty]
    private string _activeCategoryFilter = "All";

    [ObservableProperty]
    private IReadOnlyList<string> _categories = [];

    [ObservableProperty]
    private int _greenCount;

    [ObservableProperty]
    private int _yellowCount;

    [ObservableProperty]
    private int _redCount;

    public ResultsViewModel(CompatibilityCalculator calculator, ModelCatalogService catalogService)
    {
        _calculator = calculator;
        _catalogService = catalogService;
    }

    public void Evaluate(HardwareProfile hardware)
    {
        Hardware = hardware;
        var models = _catalogService.Models;
        var results = models.Select(m => _calculator.EvaluateModel(m, hardware)).ToList();

        AllResults = results;
        GreenCount = results.Count(r => r.OverallStatus == FitStatus.Green);
        YellowCount = results.Count(r => r.OverallStatus == FitStatus.Yellow);
        RedCount = results.Count(r => r.OverallStatus == FitStatus.Red);

        var cats = new List<string> { "All" };
        cats.AddRange(_catalogService.GetAllCategories());
        Categories = cats;

        ActiveFilter = "All";
        ActiveCategoryFilter = "All";
        ApplyFilters();
    }

    partial void OnActiveFilterChanged(string value) => ApplyFilters();
    partial void OnActiveCategoryFilterChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var results = AllResults.AsEnumerable();

        // Status filter
        results = ActiveFilter switch
        {
            "Green" => results.Where(r => r.OverallStatus == FitStatus.Green),
            "Yellow" => results.Where(r => r.OverallStatus == FitStatus.Yellow),
            "Red" => results.Where(r => r.OverallStatus == FitStatus.Red),
            _ => results
        };

        // Category filter
        if (ActiveCategoryFilter != "All")
        {
            results = results.Where(r =>
                r.Model.Categories.Contains(ActiveCategoryFilter, StringComparer.OrdinalIgnoreCase));
        }

        // Sort: Green first, then Yellow, then Red; within each group sort by VRAM ascending
        FilteredResults = results
            .OrderBy(r => r.OverallStatus)
            .ThenBy(r => r.BestFit?.EstimatedVramGb ?? double.MaxValue)
            .ToList();
    }
}
