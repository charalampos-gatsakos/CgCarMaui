using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CgCar.Core.Data;
using CgCar.Core.Models;
using CgCar.Core.Navigation;
using CgCar.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CgCar.Core.ViewModels;

/// <summary>
/// Page 3: create or edit a vehicle. Validation uses DataAnnotations through <see cref="ObservableValidator"/>:
/// each field is validated as it changes, and all fields are validated again when saving.
/// </summary>
public partial class VehicleEditViewModel : ObservableValidator, INavigationParameterReceiver
{
    public const int MinYear = 1900;

    private readonly IVehicleRepository _repository;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly IPhotoService _photos;

    private int? _vehicleId;
    private bool _isLoaded;
    private Vehicle _vehicle = new();

    /// <summary>The photo reference currently stored in the database for this vehicle.</summary>
    private string? _savedPhotoReference;

    /// <summary>Temporary file of a newly picked photo, not yet copied to app storage.</summary>
    private string? _pickedPhotoPath;

    /// <summary>Set when saving fails because the plate is taken; cleared as soon as the plate is edited.</summary>
    private string? _plateNumberConflict;

    public VehicleEditViewModel(
        IVehicleRepository repository,
        INavigationService navigation,
        IDialogService dialogs,
        IPhotoService photos)
    {
        _repository = repository;
        _navigation = navigation;
        _dialogs = dialogs;
        _photos = photos;

        // When a field's errors change, refresh its "<Field>Error" property that the page shows under the field.
        ErrorsChanged += (_, e) => OnPropertyChanged($"{e.PropertyName}Error");
    }

    public IReadOnlyList<VehicleCategory> Categories { get; } = Enum.GetValues<VehicleCategory>();

    [ObservableProperty]
    public partial string Title { get; private set; } = "New vehicle";

    [ObservableProperty]
    public partial bool IsExisting { get; private set; }

    [ObservableProperty]
    public partial string? AuditText { get; private set; }

    [ObservableProperty]
    public partial VehicleCategory Category { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Brand is required.")]
    [MaxLength(50, ErrorMessage = "Brand can be at most 50 characters.")]
    public partial string Brand { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Model is required.")]
    [MaxLength(50, ErrorMessage = "Model can be at most 50 characters.")]
    public partial string Model { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Plate number is required.")]
    [CustomValidation(typeof(VehicleEditViewModel), nameof(ValidatePlateNumber))]
    public partial string PlateNumber { get; set; } = string.Empty;

    /// <summary>Text rather than int? so the Entry can hold partial or invalid input and show a validation message.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(VehicleEditViewModel), nameof(ValidateYear))]
    public partial string YearText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(500, ErrorMessage = "Comments can be at most 500 characters.")]
    public partial string Comments { get; set; } = string.Empty;

    /// <summary>Absolute path of the photo to show: the saved one, or the newly picked temporary one.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    public partial string? PhotoPreviewPath { get; private set; }

    public bool HasPhoto => PhotoPreviewPath is not null;

    public string? BrandError => GetFirstError(nameof(Brand));

    public string? ModelError => GetFirstError(nameof(Model));

    public string? PlateNumberError => GetFirstError(nameof(PlateNumber)) ?? _plateNumberConflict;

    public string? YearTextError => GetFirstError(nameof(YearText));

    public string? CommentsError => GetFirstError(nameof(Comments));

    public void ReceiveParameters(IDictionary<string, object> parameters)
    {
        if (parameters.TryGetValue(NavigationParameters.VehicleId, out var id) && id is int vehicleId)
        {
            _vehicleId = vehicleId;
        }

        if (parameters.TryGetValue(NavigationParameters.Category, out var value) && value is VehicleCategory category)
        {
            Category = category;
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        // Appearing can fire more than once for the same page; loading again would discard the user's edits.
        if (_isLoaded)
        {
            return;
        }

        _isLoaded = true;

        if (_vehicleId is not int id)
        {
            return; // new vehicle: keep the defaults
        }

        try
        {
            var vehicle = await _repository.GetByIdAsync(id);
            if (vehicle is null)
            {
                await _dialogs.AlertAsync("Not found", "This vehicle no longer exists.");
                await _navigation.GoBackAsync();
                return;
            }

            _vehicle = vehicle;
            _savedPhotoReference = vehicle.PhotoPath;

            Category = vehicle.Category;
            Brand = vehicle.Brand;
            Model = vehicle.Model;
            PlateNumber = LicensePlate.Format(vehicle.PlateNumber);
            YearText = vehicle.Year?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            Comments = vehicle.Comments ?? string.Empty;
            PhotoPreviewPath = _photos.GetFullPath(vehicle.PhotoPath);

            IsExisting = true;
            Title = "Edit vehicle";
            AuditText = BuildAuditText(vehicle);
        }
        catch (Exception ex)
        {
            await _dialogs.AlertAsync("Error", $"Could not load the vehicle. {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        try
        {
            var picked = await _photos.PickPhotoAsync();
            if (picked is null)
            {
                return; // user cancelled
            }

            _pickedPhotoPath = picked;
            PhotoPreviewPath = picked;
        }
        catch (Exception ex)
        {
            await _dialogs.AlertAsync("Photo", $"Could not open the gallery. {ex.Message}");
        }
    }

    [RelayCommand]
    private void RemovePhoto()
    {
        _pickedPhotoPath = null;
        PhotoPreviewPath = null;
    }

    /// <summary>
    /// Validates, stores the photo, then saves. AsyncRelayCommand disables the command while it runs,
    /// so double-tapping Save cannot insert the vehicle twice.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            return;
        }

        string? photoReference = null;
        var photoChanged = false;

        try
        {
            // Decide which photo the saved vehicle references: a newly picked one (copied into app storage now),
            // the existing one, or none if it was removed.
            photoReference = _pickedPhotoPath is not null
                ? await _photos.SavePhotoAsync(_pickedPhotoPath)
                : HasPhoto ? _savedPhotoReference : null;
            photoChanged = photoReference != _savedPhotoReference;

            _vehicle.Category = Category;
            _vehicle.Brand = Brand.Trim();
            _vehicle.Model = Model.Trim();
            _vehicle.PlateNumber = PlateNumber; // the repository normalizes it
            _vehicle.Year = string.IsNullOrWhiteSpace(YearText) ? null : int.Parse(YearText.Trim(), CultureInfo.InvariantCulture);
            _vehicle.Comments = string.IsNullOrWhiteSpace(Comments) ? null : Comments.Trim();
            _vehicle.PhotoPath = photoReference;

            await _repository.SaveAsync(_vehicle);
        }
        catch (DuplicatePlateNumberException)
        {
            DiscardCopiedPhoto();
            _plateNumberConflict = "Another vehicle already has this plate number.";
            OnPropertyChanged(nameof(PlateNumberError));
            return;
        }
        catch (Exception ex)
        {
            DiscardCopiedPhoto();
            await _dialogs.AlertAsync("Error", $"Could not save the vehicle. {ex.Message}");
            return;
        }

        if (photoChanged)
        {
            _photos.DeletePhoto(_savedPhotoReference); // the photo that was replaced or removed
        }

        await _dialogs.ToastAsync("Vehicle saved");
        await _navigation.GoBackAsync();

        // On failure, remove the copy made for this attempt. The picked temporary file stays, so Save can be retried.
        void DiscardCopiedPhoto()
        {
            if (photoChanged)
            {
                _photos.DeletePhoto(photoReference);
            }

            _vehicle.PhotoPath = _savedPhotoReference;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!IsExisting)
        {
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            "Delete vehicle",
            $"Delete {_vehicle.Brand} {_vehicle.Model} ({LicensePlate.Format(_vehicle.PlateNumber)})? This cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        try
        {
            await _repository.DeleteAsync(_vehicle.Id);
            _photos.DeletePhoto(_savedPhotoReference);
            await _dialogs.ToastAsync("Vehicle deleted");
            await _navigation.GoBackAsync();
        }
        catch (Exception ex)
        {
            await _dialogs.AlertAsync("Error", $"Could not delete the vehicle. {ex.Message}");
        }
    }

    partial void OnPlateNumberChanged(string value)
    {
        if (_plateNumberConflict is not null)
        {
            _plateNumberConflict = null;
            OnPropertyChanged(nameof(PlateNumberError));
        }
    }

    public static ValidationResult? ValidatePlateNumber(string? value, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult.Success; // the [Required] attribute reports this case
        }

        return LicensePlate.Normalize(value).Length switch
        {
            0 => new ValidationResult("Plate number must contain letters or digits."),
            > 20 => new ValidationResult("Plate number can be at most 20 characters."),
            _ => ValidationResult.Success,
        };
    }

    public static ValidationResult? ValidateYear(string? value, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult.Success; // year is optional
        }

        var maxYear = DateTime.Now.Year + 1; // next year's models are already on sale
        var isValid = int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            && year >= MinYear && year <= maxYear;

        return isValid
            ? ValidationResult.Success
            : new ValidationResult($"Enter a year between {MinYear} and {maxYear}.");
    }

    private string? GetFirstError(string propertyName) =>
        GetErrors(propertyName).FirstOrDefault()?.ErrorMessage;

    private static string BuildAuditText(Vehicle vehicle)
    {
        var created = $"Created {ToLocalTime(vehicle.CreatedAtUtc):g}";
        return vehicle.UpdatedAtUtc is { } updated
            ? $"{created} · Updated {ToLocalTime(updated):g}"
            : created;
    }

    // Timestamps are written as UTC; SQLite doesn't keep DateTime.Kind, so mark them as UTC before converting.
    private static DateTime ToLocalTime(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
}
