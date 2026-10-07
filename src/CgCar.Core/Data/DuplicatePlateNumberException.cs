namespace CgCar.Core.Data;

/// <summary>Thrown by <see cref="IVehicleRepository.SaveAsync"/> when the plate number is already registered.</summary>
public sealed class DuplicatePlateNumberException(string plateNumber, Exception? innerException = null)
    : Exception($"A vehicle with plate number '{plateNumber}' already exists.", innerException)
{
    public string PlateNumber { get; } = plateNumber;
}
