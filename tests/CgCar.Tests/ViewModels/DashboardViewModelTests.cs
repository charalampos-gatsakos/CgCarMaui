using CgCar.Core.Models;
using CgCar.Core.Navigation;
using CgCar.Core.ViewModels;
using CgCar.Tests.TestSupport;

namespace CgCar.Tests.ViewModels;

public class DashboardViewModelTests
{
    [Fact]
    public async Task Load_ShowsEveryCategoryWithItsCount_IncludingEmptyOnes()
    {
        await using var db = new TestDatabase();
        await db.Repository.SaveAsync(new Vehicle { Category = VehicleCategory.Car, Brand = "Fiat", Model = "Panda", PlateNumber = "AAA1111" });
        await db.Repository.SaveAsync(new Vehicle { Category = VehicleCategory.Car, Brand = "Fiat", Model = "500", PlateNumber = "AAA2222" });
        await db.Repository.SaveAsync(new Vehicle { Category = VehicleCategory.Tractor, Brand = "Fendt", Model = "724", PlateNumber = "AAA3333" });
        var vm = new DashboardViewModel(db.Repository, new FakeNavigationService(), new FakeDialogService());

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(
            [(VehicleCategory.Car, 2), (VehicleCategory.Truck, 0), (VehicleCategory.Tractor, 1)],
            vm.Categories.Select(tile => (tile.Category, tile.Count)));
        Assert.Equal(3, vm.TotalCount);
        Assert.Equal("Cars", vm.Categories[0].Name);
        Assert.Equal("2 vehicles", vm.Categories[0].CountText);
        Assert.Equal("1 vehicle", vm.Categories[2].CountText);
    }

    [Fact]
    public async Task OpenCategory_NavigatesToListWithTheCategory()
    {
        await using var db = new TestDatabase();
        var navigation = new FakeNavigationService();
        var vm = new DashboardViewModel(db.Repository, navigation, new FakeDialogService());

        await vm.Categories[1].OpenCommand.ExecuteAsync(null);

        var (route, parameters) = Assert.Single(navigation.Navigations);
        Assert.Equal(Routes.VehicleList, route);
        Assert.Equal(VehicleCategory.Truck, parameters![NavigationParameters.Category]);
    }
}
