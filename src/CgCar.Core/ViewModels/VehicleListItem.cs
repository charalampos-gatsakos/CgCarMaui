using System.Globalization;
using CgCar.Core.Models;
using CommunityToolkit.Mvvm.Input;

namespace CgCar.Core.ViewModels;

/// <summary>
/// A row in the vehicle list: the vehicle, values already formatted for display, and the row's own commands.
/// The commands call back into the list ViewModel, so the row template binds them directly instead of
/// looking up the list ViewModel through the visual tree (which is null while CollectionView recycles a row).
/// </summary>
public sealed class VehicleListItem
{
    public VehicleListItem(
        Vehicle vehicle,
        string? photoFullPath,
        Func<VehicleListItem, Task> edit,
        Func<VehicleListItem, Task> delete)
    {
        Vehicle = vehicle;
        Title = $"{vehicle.Brand} {vehicle.Model}";
        PlateNumber = LicensePlate.Format(vehicle.PlateNumber);
        YearText = vehicle.Year?.ToString(CultureInfo.InvariantCulture);
        PhotoFullPath = photoFullPath;
        PlaceholderIcon = vehicle.Category.Icon();
        EditCommand = new AsyncRelayCommand(() => edit(this));
        DeleteCommand = new AsyncRelayCommand(() => delete(this));
    }

    public Vehicle Vehicle { get; }

    public string Title { get; }

    public string PlateNumber { get; }

    public string? YearText { get; }

    /// <summary>Absolute path of the photo, resolved from the stored relative reference. Null shows the placeholder.</summary>
    public string? PhotoFullPath { get; }

    public bool HasPhoto => PhotoFullPath is not null;

    public string PlaceholderIcon { get; }

    public IAsyncRelayCommand EditCommand { get; }

    public IAsyncRelayCommand DeleteCommand { get; }
}
