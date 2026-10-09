using CgCar.Core.Models;
using CgCar.Core.Navigation;
using CgCar.Core.Services;
using CgCar.Core.ViewModels;
using CgCar.Tests.TestSupport;

namespace CgCar.Tests.ViewModels;

public class VehicleEditViewModelTests
{
    private readonly FakeNavigationService _navigation = new();
    private readonly FakeDialogService _dialogs = new();
    private readonly FakePhotoService _photos = new();

    private VehicleEditViewModel CreateViewModel(TestDatabase db) =>
        new(db.Repository, _navigation, _dialogs, _photos);

    private async Task<VehicleEditViewModel> OpenNewAsync(TestDatabase db, VehicleCategory category = VehicleCategory.Car)
    {
        var vm = CreateViewModel(db);
        vm.ReceiveParameters(new Dictionary<string, object> { [NavigationParameters.Category] = category });
        await vm.LoadCommand.ExecuteAsync(null);
        return vm;
    }

    private async Task<VehicleEditViewModel> OpenExistingAsync(TestDatabase db, int id)
    {
        var vm = CreateViewModel(db);
        vm.ReceiveParameters(new Dictionary<string, object> { [NavigationParameters.VehicleId] = id });
        await vm.LoadCommand.ExecuteAsync(null);
        return vm;
    }

    private static async Task<Vehicle> InsertAsync(TestDatabase db, string plate = "IKA1234", string? photo = null)
    {
        var vehicle = new Vehicle
        {
            Category = VehicleCategory.Truck, Brand = "Volvo", Model = "FH16", PlateNumber = plate, Year = 2018, PhotoPath = photo,
        };
        await db.Repository.SaveAsync(vehicle);
        return vehicle;
    }

    [Fact]
    public async Task NewVehicle_UsesCategoryFromNavigation()
    {
        await using var db = new TestDatabase();

        var vm = await OpenNewAsync(db, VehicleCategory.Tractor);

        Assert.Equal(VehicleCategory.Tractor, vm.Category);
        Assert.Equal("New vehicle", vm.Title);
        Assert.False(vm.IsExisting);
    }

    [Fact]
    public async Task NewVehicle_DoesNotShowErrorsBeforeTheUserTypesOrSaves()
    {
        await using var db = new TestDatabase();

        var vm = await OpenNewAsync(db);

        Assert.False(vm.HasErrors);
        Assert.Null(vm.BrandError);
    }

    [Fact]
    public async Task Save_WithMissingRequiredFields_ShowsErrorsAndDoesNotSave()
    {
        await using var db = new TestDatabase();
        var vm = await OpenNewAsync(db);
        vm.Brand = "   "; // whitespace only counts as empty

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Brand is required.", vm.BrandError);
        Assert.Equal("Model is required.", vm.ModelError);
        Assert.Equal("Plate number is required.", vm.PlateNumberError);
        Assert.Null(vm.YearTextError); // year is optional
        Assert.Empty(await db.Repository.GetCountsByCategoryAsync());
        Assert.Equal(0, _navigation.BackNavigations);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1899")]
    [InlineData("-2000")]
    [InlineData("20.5")]
    public async Task InvalidYear_ShowsYearError(string year)
    {
        await using var db = new TestDatabase();
        var vm = await OpenNewAsync(db);

        vm.YearText = year;

        Assert.StartsWith("Enter a year between 1900 and", vm.YearTextError);
    }

    [Fact]
    public async Task YearAfterNextYear_IsRejected_NextYearIsAccepted()
    {
        await using var db = new TestDatabase();
        var vm = await OpenNewAsync(db);

        vm.YearText = (DateTime.Now.Year + 2).ToString();
        Assert.NotNull(vm.YearTextError);

        vm.YearText = (DateTime.Now.Year + 1).ToString();
        Assert.Null(vm.YearTextError);
    }

    [Fact]
    public async Task PlateWithoutLettersOrDigits_ShowsError()
    {
        await using var db = new TestDatabase();
        var vm = await OpenNewAsync(db);

        vm.PlateNumber = " - ";

        Assert.Equal("Plate number must contain letters or digits.", vm.PlateNumberError);
    }

    [Fact]
    public async Task Save_ValidNewVehicle_InsertsItAndGoesBack()
    {
        await using var db = new TestDatabase();
        var vm = await OpenNewAsync(db);
        vm.Brand = " Toyota ";
        vm.Model = "Yaris";
        vm.PlateNumber = "ικα-1234";
        vm.YearText = "2019";
        vm.Comments = "  ";

        await vm.SaveCommand.ExecuteAsync(null);

        var saved = Assert.Single(await db.Repository.GetByCategoryAsync(VehicleCategory.Car));
        Assert.Equal("Toyota", saved.Brand);
        Assert.Equal("IKA1234", saved.PlateNumber);
        Assert.Equal(2019, saved.Year);
        Assert.Null(saved.Comments);
        Assert.Null(saved.PhotoPath);
        Assert.Equal(1, _navigation.BackNavigations);
        Assert.Equal(["Vehicle saved"], _dialogs.Toasts);
    }

    [Fact]
    public async Task Save_DuplicatePlate_ShowsErrorAndStaysOnPage_UntilPlateIsEdited()
    {
        await using var db = new TestDatabase();
        await InsertAsync(db, plate: "IKA1234");
        var vm = await OpenNewAsync(db);
        vm.Brand = "Toyota";
        vm.Model = "Yaris";
        vm.PlateNumber = "ΙΚΑ 1234"; // same plate, Greek letters

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Another vehicle already has this plate number.", vm.PlateNumberError);
        Assert.Equal(0, _navigation.BackNavigations);

        vm.PlateNumber = "IKA1235";
        Assert.Null(vm.PlateNumberError);
    }

    [Fact]
    public async Task Load_ExistingVehicle_FillsTheForm()
    {
        await using var db = new TestDatabase();
        var vehicle = await InsertAsync(db, photo: "photos/volvo.jpg");

        var vm = await OpenExistingAsync(db, vehicle.Id);

        Assert.True(vm.IsExisting);
        Assert.Equal("Edit vehicle", vm.Title);
        Assert.Equal(VehicleCategory.Truck, vm.Category);
        Assert.Equal("Volvo", vm.Brand);
        Assert.Equal("IKA-1234", vm.PlateNumber);
        Assert.Equal("2018", vm.YearText);
        Assert.Equal("/data/photos/volvo.jpg", vm.PhotoPreviewPath);
        Assert.StartsWith("Created ", vm.AuditText);
        Assert.False(vm.HasErrors);
    }

    [Fact]
    public async Task Load_RunsOnlyOnce_SoReappearingDoesNotDiscardEdits()
    {
        await using var db = new TestDatabase();
        var vehicle = await InsertAsync(db);
        var vm = await OpenExistingAsync(db, vehicle.Id);

        vm.Brand = "Scania";
        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Scania", vm.Brand);
    }

    [Fact]
    public async Task Save_ExistingVehicle_UpdatesIt()
    {
        await using var db = new TestDatabase();
        var vehicle = await InsertAsync(db);
        var vm = await OpenExistingAsync(db, vehicle.Id);

        vm.Model = "FH500";
        vm.YearText = "";
        await vm.SaveCommand.ExecuteAsync(null);

        var updated = await db.Repository.GetByIdAsync(vehicle.Id);
        Assert.Equal("FH500", updated!.Model);
        Assert.Null(updated.Year);
        Assert.NotNull(updated.UpdatedAtUtc);
    }

    [Fact]
    public async Task PickPhoto_ShowsPreviewButSavesNothingUntilSave()
    {
        await using var db = new TestDatabase();
        var vm = await OpenNewAsync(db);
        _photos.NextPick = "/cache/picked.jpg";

        await vm.PickPhotoCommand.ExecuteAsync(null);

        Assert.Equal("/cache/picked.jpg", vm.PhotoPreviewPath);
        Assert.True(vm.HasPhoto);
        Assert.Empty(_photos.Saved);
    }

    [Fact]
    public async Task PickPhoto_Cancelled_KeepsCurrentPhoto()
    {
        await using var db = new TestDatabase();
        var vehicle = await InsertAsync(db, photo: "photos/volvo.jpg");
        var vm = await OpenExistingAsync(db, vehicle.Id);
        _photos.NextPick = null;

        await vm.PickPhotoCommand.ExecuteAsync(null);

        Assert.Equal("/data/photos/volvo.jpg", vm.PhotoPreviewPath);
    }

    [Fact]
    public async Task Save_WithReplacedPhoto_StoresNewReferenceAndDeletesOldFile()
    {
        await using var db = new TestDatabase();
        var vehicle = await InsertAsync(db, photo: "photos/old.jpg");
        var vm = await OpenExistingAsync(db, vehicle.Id);
        _photos.NextPick = "/cache/new.jpg";
        await vm.PickPhotoCommand.ExecuteAsync(null);

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("photos/new.jpg", (await db.Repository.GetByIdAsync(vehicle.Id))!.PhotoPath);
        Assert.Equal(["photos/old.jpg"], _photos.Deleted);
    }

    [Fact]
    public async Task Save_WithRemovedPhoto_ClearsReferenceAndDeletesFile()
    {
        await using var db = new TestDatabase();
        var vehicle = await InsertAsync(db, photo: "photos/old.jpg");
        var vm = await OpenExistingAsync(db, vehicle.Id);

        vm.RemovePhotoCommand.Execute(null);
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Null((await db.Repository.GetByIdAsync(vehicle.Id))!.PhotoPath);
        Assert.Equal(["photos/old.jpg"], _photos.Deleted);
    }

    [Fact]
    public async Task Save_WithUnchangedPhoto_DoesNotCopyOrDeleteFiles()
    {
        await using var db = new TestDatabase();
        var vehicle = await InsertAsync(db, photo: "photos/old.jpg");
        var vm = await OpenExistingAsync(db, vehicle.Id);

        vm.Model = "FH500";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("photos/old.jpg", (await db.Repository.GetByIdAsync(vehicle.Id))!.PhotoPath);
        Assert.Empty(_photos.Saved);
        Assert.Empty(_photos.Deleted);
    }

    [Fact]
    public async Task Save_FailingOnDuplicatePlate_DiscardsTheCopiedPhoto()
    {
        await using var db = new TestDatabase();
        await InsertAsync(db, plate: "IKA1234");
        var vm = await OpenNewAsync(db);
        vm.Brand = "Toyota";
        vm.Model = "Yaris";
        vm.PlateNumber = "IKA1234";
        _photos.NextPick = "/cache/new.jpg";
        await vm.PickPhotoCommand.ExecuteAsync(null);

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(["photos/new.jpg"], _photos.Saved);
        Assert.Equal(["photos/new.jpg"], _photos.Deleted);
        Assert.Equal("/cache/new.jpg", vm.PhotoPreviewPath); // still picked, so Save can be retried
    }

    [Fact]
    public async Task Delete_WhenConfirmed_DeletesVehicleAndPhotoAndGoesBack()
    {
        await using var db = new TestDatabase();
        var vehicle = await InsertAsync(db, photo: "photos/volvo.jpg");
        var vm = await OpenExistingAsync(db, vehicle.Id);

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.Null(await db.Repository.GetByIdAsync(vehicle.Id));
        Assert.Equal(["photos/volvo.jpg"], _photos.Deleted);
        Assert.Equal(1, _navigation.BackNavigations);
    }

    [Fact]
    public async Task Delete_WhenCancelled_KeepsVehicle()
    {
        await using var db = new TestDatabase();
        var vehicle = await InsertAsync(db);
        var vm = await OpenExistingAsync(db, vehicle.Id);
        _dialogs.ConfirmResult = false;

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.NotNull(await db.Repository.GetByIdAsync(vehicle.Id));
        Assert.Equal(0, _navigation.BackNavigations);
    }

    [Fact]
    public async Task Load_VehicleThatNoLongerExists_InformsUserAndGoesBack()
    {
        await using var db = new TestDatabase();

        await OpenExistingAsync(db, id: 999);

        Assert.Single(_dialogs.Alerts);
        Assert.Equal(1, _navigation.BackNavigations);
    }

    [Fact]
    public async Task SaveCommand_IsDisabledWhileSaving_SoADoubleTapCannotSaveTwice()
    {
        await using var db = new TestDatabase();
        var navigation = new PausedBackNavigation();
        var vm = new VehicleEditViewModel(db.Repository, navigation, _dialogs, _photos)
        {
            Brand = "Toyota",
            Model = "Yaris",
            PlateNumber = "IKA1234",
        };

        // First tap: the save runs and is held just before it navigates back.
        var firstTap = vm.SaveCommand.ExecuteAsync(null);
        await navigation.Reached.Task;

        // While it runs, the command reports it can't execute, so a Button bound to it is disabled
        // and a second tap does nothing.
        Assert.True(vm.SaveCommand.IsRunning);
        Assert.False(vm.SaveCommand.CanExecute(null));

        navigation.Release.SetResult();
        await firstTap;

        Assert.True(vm.SaveCommand.CanExecute(null));
        Assert.Single(await db.Repository.GetByCategoryAsync(VehicleCategory.Car));
    }

    /// <summary>Pauses GoBackAsync until the test releases it, to observe the command mid-execution.</summary>
    private sealed class PausedBackNavigation : INavigationService
    {
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task GoToAsync(string route, IDictionary<string, object>? parameters = null) => Task.CompletedTask;

        public async Task GoBackAsync()
        {
            Reached.SetResult();
            await Release.Task;
        }
    }
}
