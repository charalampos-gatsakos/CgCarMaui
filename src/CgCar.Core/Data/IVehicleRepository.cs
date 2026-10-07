using CgCar.Core.Models;

namespace CgCar.Core.Data;

/// <summary>Data access for vehicles. ViewModels depend on this interface, never on SQLite directly.</summary>
public interface IVehicleRepository
{
    /// <summary>Number of vehicles per category. Categories with no vehicles are absent from the result.</summary>
    Task<IReadOnlyDictionary<VehicleCategory, int>> GetCountsByCategoryAsync();

    /// <summary>All vehicles of a category, ordered by brand then model.</summary>
    Task<IReadOnlyList<Vehicle>> GetByCategoryAsync(VehicleCategory category);

    Task<Vehicle?> GetByIdAsync(int id);

    /// <summary>
    /// Inserts the vehicle when <see cref="Vehicle.Id"/> is 0, otherwise updates it.
    /// Normalizes the plate number and sets the created/updated timestamps.
    /// </summary>
    /// <exception cref="DuplicatePlateNumberException">Another vehicle already has the same plate number.</exception>
    Task SaveAsync(Vehicle vehicle);

    Task DeleteAsync(int id);
}
