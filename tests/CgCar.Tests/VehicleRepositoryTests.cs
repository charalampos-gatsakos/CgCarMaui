using CgCar.Core.Data;
using CgCar.Core.Models;
using CgCar.Tests.TestSupport;

namespace CgCar.Tests;

/// <summary>Runs against a real SQLite file, so these also verify the sqlite-net mappings and SQL.</summary>
public class VehicleRepositoryTests
{
    private static Vehicle NewVehicle(
        string plate = "IKA1234",
        VehicleCategory category = VehicleCategory.Car,
        string brand = "Toyota",
        string model = "Yaris") =>
        new() { Category = category, Brand = brand, Model = model, PlateNumber = plate };

    [Fact]
    public async Task SaveAsync_NewVehicle_AssignsIdAndCreatedTimestamp()
    {
        await using var db = new TestDatabase();
        var vehicle = NewVehicle();

        await db.Repository.SaveAsync(vehicle);

        Assert.True(vehicle.Id > 0);
        Assert.NotEqual(default, vehicle.CreatedAtUtc);
        Assert.Null(vehicle.UpdatedAtUtc);

        var stored = await db.Repository.GetByIdAsync(vehicle.Id);
        Assert.NotNull(stored);
        Assert.Equal("Toyota", stored.Brand);
        Assert.Equal(vehicle.CreatedAtUtc, stored.CreatedAtUtc);
    }

    [Fact]
    public async Task SaveAsync_ExistingVehicle_SetsUpdatedTimestampAndKeepsCreated()
    {
        await using var db = new TestDatabase();
        var vehicle = NewVehicle();
        await db.Repository.SaveAsync(vehicle);
        var created = vehicle.CreatedAtUtc;

        vehicle.Model = "Corolla";
        await db.Repository.SaveAsync(vehicle);

        var stored = await db.Repository.GetByIdAsync(vehicle.Id);
        Assert.NotNull(stored);
        Assert.Equal("Corolla", stored.Model);
        Assert.Equal(created, stored.CreatedAtUtc);
        Assert.NotNull(stored.UpdatedAtUtc);
    }

    [Fact]
    public async Task SaveAsync_StoresPlateInNormalizedForm()
    {
        await using var db = new TestDatabase();
        var vehicle = NewVehicle(plate: "ικα-1234");

        await db.Repository.SaveAsync(vehicle);

        var stored = await db.Repository.GetByIdAsync(vehicle.Id);
        Assert.Equal("IKA1234", stored!.PlateNumber);
    }

    [Theory]
    [InlineData("IKA1234")]
    [InlineData("ΙΚΑ-1234")]  // same plate typed with Greek letters
    [InlineData("ika 1234")]
    public async Task SaveAsync_PlateAlreadyUsedByAnotherVehicle_Throws(string duplicatePlate)
    {
        await using var db = new TestDatabase();
        await db.Repository.SaveAsync(NewVehicle(plate: "IKA-1234"));

        await Assert.ThrowsAsync<DuplicatePlateNumberException>(
            () => db.Repository.SaveAsync(NewVehicle(plate: duplicatePlate, category: VehicleCategory.Truck)));
    }

    [Fact]
    public async Task SaveAsync_UpdatingVehicleWithItsOwnPlate_IsAllowed()
    {
        await using var db = new TestDatabase();
        var vehicle = NewVehicle();
        await db.Repository.SaveAsync(vehicle);

        vehicle.Year = 2020;
        await db.Repository.SaveAsync(vehicle); // must not count itself as a duplicate

        Assert.Equal(2020, (await db.Repository.GetByIdAsync(vehicle.Id))!.Year);
    }

    [Fact]
    public async Task GetCountsByCategoryAsync_CountsPerCategory()
    {
        await using var db = new TestDatabase();
        await db.Repository.SaveAsync(NewVehicle("AAA1111", VehicleCategory.Car));
        await db.Repository.SaveAsync(NewVehicle("AAA2222", VehicleCategory.Car));
        await db.Repository.SaveAsync(NewVehicle("AAA3333", VehicleCategory.Tractor));

        var counts = await db.Repository.GetCountsByCategoryAsync();

        Assert.Equal(2, counts[VehicleCategory.Car]);
        Assert.Equal(1, counts[VehicleCategory.Tractor]);
        Assert.False(counts.ContainsKey(VehicleCategory.Truck));
    }

    [Fact]
    public async Task GetByCategoryAsync_ReturnsOnlyThatCategory_SortedByBrandThenModelIgnoringCase()
    {
        await using var db = new TestDatabase();
        await db.Repository.SaveAsync(NewVehicle("AAA1111", VehicleCategory.Car, "Toyota", "Yaris"));
        await db.Repository.SaveAsync(NewVehicle("AAA2222", VehicleCategory.Car, "audi", "A4"));
        await db.Repository.SaveAsync(NewVehicle("AAA3333", VehicleCategory.Car, "Toyota", "Auris"));
        await db.Repository.SaveAsync(NewVehicle("AAA4444", VehicleCategory.Truck, "Volvo", "FH16"));

        var cars = await db.Repository.GetByCategoryAsync(VehicleCategory.Car);

        Assert.Equal(["audi A4", "Toyota Auris", "Toyota Yaris"], cars.Select(v => $"{v.Brand} {v.Model}"));
    }

    [Fact]
    public async Task DeleteAsync_RemovesVehicle()
    {
        await using var db = new TestDatabase();
        var vehicle = NewVehicle();
        await db.Repository.SaveAsync(vehicle);

        await db.Repository.DeleteAsync(vehicle.Id);

        Assert.Null(await db.Repository.GetByIdAsync(vehicle.Id));
    }

    [Fact]
    public async Task DemoData_IsSeededOnlyWhenTheDatabaseIsFirstCreated()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cgcar-test-{Guid.NewGuid():N}.db3");
        try
        {
            var first = new VehicleRepository(new DatabaseOptions(path, SeedDemoData: true));
            var seeded = (await first.GetCountsByCategoryAsync()).Values.Sum();
            Assert.True(seeded > 0);

            // The user deletes every vehicle...
            foreach (var category in Enum.GetValues<VehicleCategory>())
            {
                foreach (var vehicle in await first.GetByCategoryAsync(category))
                {
                    await first.DeleteAsync(vehicle.Id);
                }
            }

            await first.DisposeAsync();

            // ...and on the next app start the demo data must not come back.
            var second = new VehicleRepository(new DatabaseOptions(path, SeedDemoData: true));
            Assert.Empty(await second.GetCountsByCategoryAsync());
            await second.DisposeAsync();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
