using SQLite;

namespace CgCar.Core.Models;

/// <summary>A registered vehicle. This class is also the SQLite table definition (sqlite-net maps it by attributes).</summary>
[Table("Vehicles")]
public class Vehicle
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Indexed because the dashboard counts and the list page both filter by category.</summary>
    [Indexed]
    public VehicleCategory Category { get; set; }

    [NotNull, MaxLength(50), Collation("NOCASE")]
    public string Brand { get; set; } = string.Empty;

    [NotNull, MaxLength(50), Collation("NOCASE")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Always stored in canonical form (see <see cref="LicensePlate.Normalize"/>), so the unique index
    /// also catches duplicates typed with different spacing, case or Greek/Latin letters.
    /// </summary>
    [NotNull, Unique, MaxLength(20)]
    public string PlateNumber { get; set; } = string.Empty;

    public int? Year { get; set; }

    [MaxLength(500)]
    public string? Comments { get; set; }

    /// <summary>
    /// Path of the photo relative to the app data folder (e.g. "photos/3f2a….jpg"), not an absolute path:
    /// on iOS the app's container path changes on every update/reinstall, which would break absolute paths.
    /// </summary>
    public string? PhotoPath { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
