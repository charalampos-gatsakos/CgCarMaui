namespace CgCar.Core.Services;

/// <summary>
/// Picks photos from the device gallery and manages the app's own copies of them.
/// A picked photo lives in a temporary file until the vehicle is saved, so cancelling an edit leaves nothing behind.
/// </summary>
public interface IPhotoService
{
    /// <summary>Opens the gallery. Returns the absolute path of a temporary copy for preview, or null if cancelled.</summary>
    Task<string?> PickPhotoAsync();

    /// <summary>Copies a picked temporary photo into permanent app storage and returns its relative reference.</summary>
    Task<string> SavePhotoAsync(string temporaryPath);

    /// <summary>Absolute path for a stored reference, or null if there is no reference or the file is missing.</summary>
    string? GetFullPath(string? reference);

    /// <summary>Deletes a stored photo. Does nothing for null or unknown references.</summary>
    void DeletePhoto(string? reference);
}
