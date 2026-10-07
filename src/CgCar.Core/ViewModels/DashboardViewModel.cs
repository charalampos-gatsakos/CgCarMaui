using CgCar.Core.Data;
using CgCar.Core.Models;
using CgCar.Core.Navigation;
using CgCar.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CgCar.Core.ViewModels;

/// <summary>Page 1: one tile per category with its vehicle count.</summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly IVehicleRepository _repository;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;

    public DashboardViewModel(IVehicleRepository repository, INavigationService navigation, IDialogService dialogs)
    {
        _repository = repository;
        _navigation = navigation;
        _dialogs = dialogs;

        Categories = Enum.GetValues<VehicleCategory>()
            .Select(category => new CategoryTileViewModel(category, OpenCategoryAsync))
            .ToList();
    }

    /// <summary>
    /// Built once from the enum; only the counts change on reload, so the tiles update in place
    /// instead of the whole collection being rebuilt every time the page appears.
    /// </summary>
    public IReadOnlyList<CategoryTileViewModel> Categories { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalText))]
    public partial int TotalCount { get; set; }

    public string TotalText => TotalCount == 1 ? "1 vehicle registered" : $"{TotalCount} vehicles registered";

    /// <summary>Runs every time the page appears, so counts are fresh after adding or deleting vehicles.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var counts = await _repository.GetCountsByCategoryAsync();

            foreach (var tile in Categories)
            {
                tile.Count = counts.GetValueOrDefault(tile.Category);
            }

            TotalCount = counts.Values.Sum();
        }
        catch (Exception ex)
        {
            await _dialogs.AlertAsync("Error", $"Could not load the vehicle counts. {ex.Message}");
        }
    }

    // Invoked through each tile's OpenCommand (see CategoryTileViewModel).
    private Task OpenCategoryAsync(CategoryTileViewModel tile) =>
        _navigation.GoToAsync(Routes.VehicleList, new Dictionary<string, object>
        {
            [NavigationParameters.Category] = tile.Category,
        });
}
