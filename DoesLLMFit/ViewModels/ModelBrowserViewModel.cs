using DoesLLMFit.Services;

namespace DoesLLMFit.ViewModels;

public partial class ModelBrowserViewModel : ObservableObject
{
    private readonly ModelCatalogService _catalogService;

    [ObservableProperty]
    private IReadOnlyList<LlmModel> _allModels = [];

    [ObservableProperty]
    private IReadOnlyList<LlmModel> _filteredModels = [];

    [ObservableProperty]
    private IReadOnlyList<string> _categories = [];

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public ModelBrowserViewModel(ModelCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public void Initialize()
    {
        AllModels = _catalogService.Models;
        var cats = new List<string> { "All" };
        cats.AddRange(_catalogService.GetAllCategories());
        Categories = cats;
        ApplyFilters();
    }

    partial void OnSelectedCategoryChanged(string value) => ApplyFilters();
    partial void OnSearchQueryChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var models = AllModels.AsEnumerable();

        if (SelectedCategory != "All")
        {
            models = models.Where(m =>
                m.Categories.Contains(SelectedCategory, StringComparer.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            models = models.Where(m =>
                m.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                m.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        FilteredModels = models.OrderBy(m => m.ParametersB).ToList();
    }
}
