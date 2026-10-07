using CgCar.Core.Models;
using SQLite;

namespace CgCar.Core.Data;

/// <summary>SQLite implementation of <see cref="IVehicleRepository"/> using sqlite-net's async API.</summary>
public sealed class VehicleRepository(DatabaseOptions options) : IVehicleRepository, IAsyncDisposable
{
    private const SQLiteOpenFlags OpenFlags =
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _connection;

    public async Task<IReadOnlyDictionary<VehicleCategory, int>> GetCountsByCategoryAsync()
    {
        var db = await GetConnectionAsync();
        var rows = await db.QueryAsync<CategoryCountRow>(
            "SELECT Category, COUNT(*) AS Count FROM Vehicles GROUP BY Category");

        return rows.ToDictionary(row => row.Category, row => row.Count);
    }

    public async Task<IReadOnlyList<Vehicle>> GetByCategoryAsync(VehicleCategory category)
    {
        var db = await GetConnectionAsync();

        return await db.Table<Vehicle>()
            .Where(v => v.Category == category)
            .OrderBy(v => v.Brand)
            .ThenBy(v => v.Model)
            .ToListAsync();
    }

    public async Task<Vehicle?> GetByIdAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.FindAsync<Vehicle>(id);
    }

    public async Task SaveAsync(Vehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        var db = await GetConnectionAsync();

        vehicle.Brand = vehicle.Brand.Trim();
        vehicle.Model = vehicle.Model.Trim();
        vehicle.PlateNumber = LicensePlate.Normalize(vehicle.PlateNumber);

        // Check first to give a clear error; the unique index below is the safety net.
        var plate = vehicle.PlateNumber;
        var id = vehicle.Id;
        var duplicates = await db.Table<Vehicle>().Where(v => v.PlateNumber == plate && v.Id != id).CountAsync();
        if (duplicates > 0)
        {
            throw new DuplicatePlateNumberException(plate);
        }

        try
        {
            if (vehicle.Id == 0)
            {
                vehicle.CreatedAtUtc = DateTime.UtcNow;
                vehicle.UpdatedAtUtc = null;
                await db.InsertAsync(vehicle); // sqlite-net fills in the generated Id
            }
            else
            {
                vehicle.UpdatedAtUtc = DateTime.UtcNow;
                await db.UpdateAsync(vehicle);
            }
        }
        catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint)
        {
            throw new DuplicatePlateNumberException(plate, ex);
        }
    }

    public async Task DeleteAsync(int id)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync<Vehicle>(id);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection = null;
        }
    }

    /// <summary>
    /// Opens the connection and prepares the schema on first use (lazy init), so app startup
    /// never blocks on the database. The lock makes concurrent first calls initialize only once.
    /// </summary>
    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }

        await _initLock.WaitAsync();
        try
        {
            if (_connection is null)
            {
                var connection = new SQLiteAsyncConnection(options.DatabasePath, OpenFlags);
                await connection.CreateTableAsync<Vehicle>(); // creates the table, or adds new columns to it
                await MigrateAsync(connection);
                _connection = connection;
            }

            return _connection;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// SQLite's built-in user_version number records which one-off setup steps already ran on this file.
    /// Version 1 = initial schema (+ demo data when enabled). Future changes add "if (version &lt; 2)" steps.
    /// </summary>
    private async Task MigrateAsync(SQLiteAsyncConnection connection)
    {
        var version = await connection.ExecuteScalarAsync<int>("PRAGMA user_version");

        if (version < 1)
        {
            // One transaction, so a crash can't leave demo rows without the version bump (which would re-seed).
            await connection.RunInTransactionAsync(db =>
            {
                if (options.SeedDemoData)
                {
                    db.InsertAll(DemoData.CreateVehicles(), runInTransaction: false);
                }

                db.Execute("PRAGMA user_version = 1");
            });
        }
    }

    private sealed class CategoryCountRow
    {
        public VehicleCategory Category { get; set; }

        public int Count { get; set; }
    }
}
