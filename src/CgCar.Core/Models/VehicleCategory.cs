namespace CgCar.Core.Models;

/// <summary>
/// The vehicle categories shown on the dashboard. Stored in SQLite as the integer value,
/// so existing values must never be renumbered; new categories get new numbers.
/// </summary>
public enum VehicleCategory
{
    Car = 0,
    Truck = 1,
    Tractor = 2,
}

public static class VehicleCategoryExtensions
{
    /// <summary>Plural name used for dashboard tiles and list titles.</summary>
    public static string DisplayName(this VehicleCategory category) => category switch
    {
        VehicleCategory.Car => "Cars",
        VehicleCategory.Truck => "Trucks",
        VehicleCategory.Tractor => "Tractors",
        _ => category.ToString(),
    };

    /// <summary>Emoji glyph used as the category icon and as the placeholder when a vehicle has no photo.</summary>
    public static string Icon(this VehicleCategory category) => category switch
    {
        VehicleCategory.Car => "🚗",
        VehicleCategory.Truck => "🚚",
        VehicleCategory.Tractor => "🚜",
        _ => "🚘",
    };
}
