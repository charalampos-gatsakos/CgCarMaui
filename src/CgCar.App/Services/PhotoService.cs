using CgCar.Core.Services;

namespace CgCar.App.Services;

/// <summary>
/// <see cref="IPhotoService"/> using MAUI's MediaPicker. Photos are stored under the app data folder in "photos/",
/// and the database keeps only the relative reference ("photos/&lt;guid&gt;.jpg").
/// </summary>
public sealed class PhotoService : IPhotoService
{
    private const string PhotosFolder = "photos";

    // MediaPicker (.NET 10) resizes, compresses and fixes rotation for us, so 12 MP camera photos
    // aren't stored at full size and display the right way up.
    private static readonly MediaPickerOptions PickerOptions = new()
    {
        Title = "Choose a vehicle photo",
        SelectionLimit = 1,
        MaximumWidth = 1600,
        MaximumHeight = 1600,
        CompressionQuality = 85,
        RotateImage = true,
    };

    private static string PhotosDirectory => Path.Combine(FileSystem.AppDataDirectory, PhotosFolder);

    public async Task<string?> PickPhotoAsync()
    {
        // PickPhotoAsync is obsolete in .NET 10; PickPhotosAsync with SelectionLimit = 1 is its replacement.
        var files = await MediaPicker.Default.PickPhotosAsync(PickerOptions);
        var file = files?.FirstOrDefault();
        if (file is null)
        {
            return null;
        }

        // The picker's result can be a content URI (Android) or a short-lived temp file (iOS),
        // so copy it into our cache folder right away. It moves to permanent storage only on Save.
        var extension = Path.GetExtension(file.FileName);
        var temporaryPath = Path.Combine(
            FileSystem.CacheDirectory,
            $"picked-{Guid.NewGuid():N}{(string.IsNullOrEmpty(extension) ? ".jpg" : extension)}");

        await using var source = await file.OpenReadAsync();
        await using var target = File.Create(temporaryPath);
        await source.CopyToAsync(target);

        return temporaryPath;
    }

    public async Task<string> SavePhotoAsync(string temporaryPath)
    {
        Directory.CreateDirectory(PhotosDirectory);

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(temporaryPath)}";

        await using (var source = File.OpenRead(temporaryPath))
        await using (var target = File.Create(Path.Combine(PhotosDirectory, fileName)))
        {
            await source.CopyToAsync(target);
        }

        return $"{PhotosFolder}/{fileName}";
    }

    public string? GetFullPath(string? reference)
    {
        var fullPath = ResolveInsidePhotosDirectory(reference);
        return fullPath is not null && File.Exists(fullPath) ? fullPath : null;
    }

    public void DeletePhoto(string? reference)
    {
        var fullPath = ResolveInsidePhotosDirectory(reference);
        if (fullPath is null)
        {
            return;
        }

        try
        {
            File.Delete(fullPath); // no error if the file is already gone
        }
        catch (IOException)
        {
            // Best effort: a leftover file is harmless and must not fail the delete/save that triggered this.
        }
    }

    /// <summary>
    /// Turns a stored reference into an absolute path, refusing anything that would point outside
    /// the photos folder, so a bad value in the database can never delete other files.
    /// </summary>
    private static string? ResolveInsidePhotosDirectory(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        var photosDirectory = Path.GetFullPath(PhotosDirectory) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(FileSystem.AppDataDirectory, reference));

        return fullPath.StartsWith(photosDirectory, StringComparison.Ordinal) ? fullPath : null;
    }
}
