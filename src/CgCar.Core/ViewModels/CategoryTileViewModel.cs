using CgCar.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CgCar.Core.ViewModels;

/// <summary>One dashboard tile: a category, how many vehicles it has, and the command that opens its list.</summary>
public partial class CategoryTileViewModel : ObservableObject
{
    public CategoryTileViewModel(VehicleCategory category, Func<CategoryTileViewModel, Task> open)
    {
        Category = category;
        Name = category.DisplayName();
        Icon = category.Icon();
        OpenCommand = new AsyncRelayCommand(() => open(this));
    }

    public VehicleCategory Category { get; }

    public string Name { get; }

    public string Icon { get; }

    public IAsyncRelayCommand OpenCommand { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CountText))]
    public partial int Count { get; set; }

    public string CountText => Count == 1 ? "1 vehicle" : $"{Count} vehicles";
}
