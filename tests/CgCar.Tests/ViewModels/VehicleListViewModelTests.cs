using CgCar.Core.Models;
using CgCar.Core.Navigation;
using CgCar.Core.ViewModels;
using CgCar.Tests.TestSupport;

namespace CgCar.Tests.ViewModels;

public class VehicleListViewModelTests
{
    private readonly FakeNavigationService _navigation = new();
    private readonly FakeDialogService _dialogs = new();
    private readonly FakePhotoService _photos = new();

    private async Task<VehicleListViewModel> CreateLoadedCarListAsync(TestDatabase db)
    {
        await db.Repository.SaveAsync(new Vehicle { Category = VehicleCategory.Car, Brand = "Toyota", Model = "Yaris", PlateNumber = "IKA1234", PhotoPath = "photos/yaris.jpg" });
        await db.Repository.SaveAsync(new Vehicle { Category = VehicleCategory.Car, Brand = "BMW", Model = "320d", PlateNumber = "YXN8814" });
        await db.Repository.SaveAsync(new Vehicle { Category = VehicleCategory.Truck, Brand = "Volvo", Model = "FH16", PlateNumber = "XEP7719" });

        var vm = new VehicleListViewModel(db.Repository, _navigation, _dialogs, _photos);
        vm.ReceiveParameters(new Dictionary<string, object> { [NavigationParameters.Category] = VehicleCategory.Car });
        await vm.LoadCommand.ExecuteAsync(null);
        return vm;
    }

    [Fact]
    public async Task Load_ShowsOnlyVehiclesOfTheCategory_WithFormattedValues()
    {
        await using var db = new TestDatabase();

        var vm = await CreateLoadedCarListAsync(db);

        Assert.Equal("Cars", vm.Title);
        Assert.Equal(["BMW 320d", "Toyota Yaris"], vm.Vehicles.Select(v => v.Title));
        var yaris = vm.Vehicles[1];
        Assert.Equal("IKA-1234", yaris.PlateNumber);
        Assert.Equal("/data/photos/yaris.jpg", yaris.PhotoFullPath);
        Assert.False(vm.Vehicles[0].HasPhoto);
    }

    [Theory]
    [InlineData("toy", "Toyota Yaris")]     // brand, case-insensitive
    [InlineData("320", "BMW 320d")]         // model
    [InlineData("ικα-12", "Toyota Yaris")]  // plate typed in Greek with a dash
    [InlineData("yxn 88", "BMW 320d")]      // plate with a space
    public async Task Search_FiltersByBrandModelOrPlate(string search, string expectedTitle)
    {
        await using var db = new TestDatabase();
        var vm = await CreateLoadedCarListAsync(db);

        vm.SearchText = search;

        Assert.Equal(expectedTitle, Assert.Single(vm.Vehicles).Title);
    }

    [Fact]
    public async Task Search_WithNoMatches_ShowsNoMatchesMessage_AndClearingRestoresAll()
    {
        await using var db = new TestDatabase();
        var vm = await CreateLoadedCarListAsync(db);

        vm.SearchText = "zzz";
        Assert.Empty(vm.Vehicles);
        Assert.Equal("No vehicles match your search.", vm.EmptyMessage);

        vm.SearchText = "";
        Assert.Equal(2, vm.Vehicles.Count);
    }

    [Fact]
    public async Task Delete_WhenConfirmed_RemovesVehicleAndItsPhoto()
    {
        await using var db = new TestDatabase();
        var vm = await CreateLoadedCarListAsync(db);
        var yaris = vm.Vehicles.Single(v => v.Title == "Toyota Yaris");

        await yaris.DeleteCommand.ExecuteAsync(null);

        Assert.DoesNotContain(yaris, vm.Vehicles);
        Assert.Null(await db.Repository.GetByIdAsync(yaris.Vehicle.Id));
        Assert.Equal(["photos/yaris.jpg"], _photos.Deleted);
        Assert.Equal(["Vehicle deleted"], _dialogs.Toasts);
    }

    [Fact]
    public async Task Delete_WhenCancelled_KeepsVehicle()
    {
        await using var db = new TestDatabase();
        var vm = await CreateLoadedCarListAsync(db);
        var yaris = vm.Vehicles.Single(v => v.Title == "Toyota Yaris");
        _dialogs.ConfirmResult = false;

        await yaris.DeleteCommand.ExecuteAsync(null);

        Assert.Contains(yaris, vm.Vehicles);
        Assert.NotNull(await db.Repository.GetByIdAsync(yaris.Vehicle.Id));
        Assert.Empty(_photos.Deleted);
    }

    [Fact]
    public async Task Add_NavigatesToEditorWithTheCurrentCategory()
    {
        await using var db = new TestDatabase();
        var vm = await CreateLoadedCarListAsync(db);

        await vm.AddCommand.ExecuteAsync(null);

        var (route, parameters) = Assert.Single(_navigation.Navigations);
        Assert.Equal(Routes.VehicleEdit, route);
        Assert.Equal(VehicleCategory.Car, parameters![NavigationParameters.Category]);
        Assert.False(parameters.ContainsKey(NavigationParameters.VehicleId));
    }

    [Fact]
    public async Task Edit_NavigatesToEditorWithTheVehicleId()
    {
        await using var db = new TestDatabase();
        var vm = await CreateLoadedCarListAsync(db);

        await vm.Vehicles[0].EditCommand.ExecuteAsync(null);

        var (route, parameters) = Assert.Single(_navigation.Navigations);
        Assert.Equal(Routes.VehicleEdit, route);
        Assert.Equal(vm.Vehicles[0].Vehicle.Id, parameters![NavigationParameters.VehicleId]);
    }
}
