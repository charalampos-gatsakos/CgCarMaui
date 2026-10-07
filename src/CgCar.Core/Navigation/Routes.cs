namespace CgCar.Core.Navigation;

/// <summary>Shell route names. The app registers each one against its page in AppShell.</summary>
public static class Routes
{
    public const string VehicleList = "vehicles";
    public const string VehicleEdit = "vehicle-edit";
}

/// <summary>Keys of the parameters passed between pages.</summary>
public static class NavigationParameters
{
    /// <summary>A <see cref="Models.VehicleCategory"/>: the list to show, or the default category of a new vehicle.</summary>
    public const string Category = "category";

    /// <summary>An <see cref="int"/> vehicle id to edit. Absent when creating a new vehicle.</summary>
    public const string VehicleId = "vehicleId";
}
